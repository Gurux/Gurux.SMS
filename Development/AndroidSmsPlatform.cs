// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------
#if ANDROID
using System.Globalization;
using Gurux.SMS.Enums;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Provider;
using Android.Telephony;

namespace Gurux.SMS;

/// <summary>Uses Android telephony for Unicode SMS submission and context-scoped incoming broadcasts.</summary>
internal sealed class AndroidSmsPlatform : ISmsPlatform
{
    /// <summary>Gets the process application context without retaining an activity.</summary>
    private static Context Context => Application.Context;

    /// <inheritdoc/>
    public IDisposable Open(SmsMode mode, Action<GXSMSMessage> receive)
    {
        if (mode != SmsMode.ReceiveOnly) { RequirePermission(Manifest.Permission.SendSms); }
        if (mode != SmsMode.SendOnly) { RequirePermission(Manifest.Permission.ReceiveSms); }
        if (Context.PackageManager?.HasSystemFeature(PackageManager.FeatureTelephony) != true)
        {
            throw new NotSupportedException(Properties.Resources.Get("SmsServiceUnavailable"));
        }
        return new AndroidSmsRegistration(Context,
            mode == SmsMode.SendOnly ? null : Telephony.Sms.Intents.SmsReceivedAction,
            Manifest.Permission.BroadcastSms, true, (intent, _) =>
            {
                if (intent.Action != Telephony.Sms.Intents.SmsReceivedAction) { return; }
                var parts = Telephony.Sms.Intents.GetMessagesFromIntent(intent);
                if (parts == null || parts.Length == 0) { return; }
                try
                {
                    var first = parts[0];
                    receive(new GXSMSMessage
                    {
                        Data = string.Concat(parts.Select(part => part.MessageBody)),
                        PhoneNumber = first.OriginatingAddress ?? string.Empty,
                        Time = DateTimeOffset.FromUnixTimeMilliseconds(first.TimestampMillis).LocalDateTime,
                        ServiceCenterNumber = first.ServiceCenterAddress ?? string.Empty,
                        Status = MessageStatus.Unread
                    });
                }
                finally
                {
                    foreach (var part in parts) { part.Dispose(); }
                }
            });
    }

    /// <summary>Checks a permission without showing UI or requesting unrelated SMS capabilities.</summary>
    /// <param name="permission">The Android permission required by the selected operation.</param>
    /// <exception cref="UnauthorizedAccessException">The host application has not obtained this permission.</exception>
    private static void RequirePermission(string permission)
    {
        if (Context.CheckCallingOrSelfPermission(permission) != Permission.Granted)
        {
            throw new UnauthorizedAccessException(string.Format(CultureInfo.CurrentCulture,
                Properties.Resources.Get("SmsPermissionRequired"), permission));
        }
    }

    /// <inheritdoc/>
    public async Task<string> SendAsync(string recipient, string text, CancellationToken cancellationToken)
    {
        RequirePermission(Manifest.Permission.SendSms);
        cancellationToken.ThrowIfCancellationRequested();
        using var manager = (OperatingSystem.IsAndroidVersionAtLeast(23)
            ? Context.GetSystemService(Java.Lang.Class.FromType(typeof(SmsManager))) as SmsManager
            : SmsManager.Default)
            ?? throw new NotSupportedException(Properties.Resources.Get("SmsServiceUnavailable"));
        var parts = manager.DivideMessage(text);
        if (parts == null || parts.Count == 0) { throw new ArgumentException(Properties.Resources.Get("SmsRecipientAndTextRequired")); }
        var submission = new SmsSubmission(parts.Count);
        string action = Context.PackageName + ".GURUX_SMS_SENT." + Guid.NewGuid().ToString("N");
        using var registration = new AndroidSmsRegistration(Context, action, null, false,
            (intent, result) => submission.CompletePart(intent.GetIntExtra("part", -1), result));
        var pending = new List<PendingIntent>();
        try
        {
            for (int index = 0; index < parts.Count; index++)
            {
                using var intent = new Intent(action).SetPackage(Context.PackageName);
                intent.PutExtra("part", index);
                var flags = PendingIntentFlags.OneShot;
                if (OperatingSystem.IsAndroidVersionAtLeast(23)) { flags |= PendingIntentFlags.Immutable; }
                pending.Add(PendingIntent.GetBroadcast(Context, index, intent, flags)!);
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (parts.Count == 1) { manager.SendTextMessage(recipient, null, parts[0], pending[0], null); }
            else { manager.SendMultipartTextMessage(recipient, null, parts, pending, null); }
            await submission.Completion.WaitAsync(cancellationToken).ConfigureAwait(false);
            return Properties.Resources.Get("SmsSubmitted");
        }
        finally
        {
            foreach (var intent in pending)
            {
                try { intent.Cancel(); }
                finally { intent.Dispose(); }
            }
        }
    }
}
#endif
