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
        RequireSmsSupport();
        if (mode != SmsMode.ReceiveOnly)
        {
            using var manager = RequireSendingService();
        }
        if (mode != SmsMode.SendOnly)
        {
            RequirePermission(Manifest.Permission.ReceiveSms);
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

    /// <summary>Rejects devices whose telephony hardware or configuration does not support SMS.</summary>
    /// <exception cref="NotSupportedException">The device has no telephony messaging feature or SMS-capable telephony service.</exception>
    private static void RequireSmsSupport()
    {
        string feature = OperatingSystem.IsAndroidVersionAtLeast(33)
            ? PackageManager.FeatureTelephonyMessaging
            : PackageManager.FeatureTelephony;
        using var telephony = Context.GetSystemService(Context.TelephonyService) as TelephonyManager;
        if (Context.PackageManager?.HasSystemFeature(feature) != true || telephony == null
            || !(OperatingSystem.IsAndroidVersionAtLeast(35) ? telephony.IsDeviceSmsCapable : telephony.IsSmsCapable))
        {
            throw new NotSupportedException(Properties.Resources.Get("SmsServiceUnavailable"));
        }
    }

    /// <summary>Checks sending permission, application restrictions, the SMS service and the selected SIM before submission.</summary>
    /// <returns>The SMS manager to dispose after checking or submitting a message.</returns>
    /// <exception cref="UnauthorizedAccessException">Sending permission is missing or Android blocks this application's SMS operation.</exception>
    /// <exception cref="NotSupportedException">The SMS manager or telephony service is unavailable.</exception>
    /// <exception cref="InvalidOperationException">No default SMS subscription is available or its SIM is not ready.</exception>
    /// <remarks>No permissions are requested and no test message is sent. Network and recipient-specific failures are still reported by submission acknowledgements.</remarks>
    private static SmsManager RequireSendingService()
    {
        RequirePermission(Manifest.Permission.SendSms);
        using var appOps = Context.GetSystemService(Context.AppOpsService) as AppOpsManager;
        if (OperatingSystem.IsAndroidVersionAtLeast(23) && appOps != null)
        {
            var access = appOps.CheckOpNoThrow(AppOpsManager.OpstrSendSms!, Android.OS.Process.MyUid(), Context.PackageName!);
            if (access != AppOpsManagerMode.Allowed && access != AppOpsManagerMode.Default)
            {
                throw new UnauthorizedAccessException(Properties.Resources.Get("SmsSendingNotAllowed"));
            }
        }
        var manager = (OperatingSystem.IsAndroidVersionAtLeast(23)
            ? Context.GetSystemService(Java.Lang.Class.FromType(typeof(SmsManager))) as SmsManager
            : SmsManager.Default)
            ?? throw new NotSupportedException(Properties.Resources.Get("SmsServiceUnavailable"));
        try
        {
            using var telephony = Context.GetSystemService(Context.TelephonyService) as TelephonyManager
                ?? throw new NotSupportedException(Properties.Resources.Get("SmsServiceUnavailable"));
            if (OperatingSystem.IsAndroidVersionAtLeast(22))
            {
                int subscriptionId = SmsManager.DefaultSmsSubscriptionId;
                if (subscriptionId < 0)
                {
                    throw new InvalidOperationException(Properties.Resources.Get("SmsSubscriptionUnavailable"));
                }
                if (OperatingSystem.IsAndroidVersionAtLeast(24))
                {
                    using var selectedTelephony = telephony.CreateForSubscriptionId(subscriptionId)
                        ?? throw new NotSupportedException(Properties.Resources.Get("SmsServiceUnavailable"));
                    RequireReadySim(selectedTelephony);
                }
                else
                {
                    RequireReadySim(telephony);
                }
            }
            else
            {
                RequireReadySim(telephony);
            }
            return manager;
        }
        catch
        {
            manager.Dispose();
            throw;
        }
    }

    /// <summary>Rejects absent, locked, disabled or otherwise unready SIM cards.</summary>
    /// <param name="telephony">The telephony service associated with the outgoing SMS subscription.</param>
    /// <exception cref="InvalidOperationException">The selected SIM has not reached the ready state.</exception>
    private static void RequireReadySim(TelephonyManager telephony)
    {
        if (telephony.SimState != SimState.Ready)
        {
            throw new InvalidOperationException(Properties.Resources.Get("SmsSimNotReady"));
        }
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
        cancellationToken.ThrowIfCancellationRequested();
        RequireSmsSupport();
        using var manager = RequireSendingService();
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
