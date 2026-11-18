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
using System.Text;
using Gurux.Common;

namespace Gurux.SMS;

/// <summary>Integrates native SMS sessions with the existing media lifecycle and receive contracts.</summary>
public partial class GXSMS
{
    /// <summary>Provides native SMS operations, or null for an AT modem connection.</summary>
    private readonly ISmsPlatform? _smsPlatform;
    /// <summary>Serializes native lifecycle, settings changes and receive callbacks without asynchronous waits.</summary>
    private readonly object _smsLifecycle = new();
    /// <summary>Identifies the currently open native receiver generation.</summary>
    private SmsReceiveSession? _smsSession;
    /// <summary>Stores the enabled operations while the connection is closed.</summary>
    private volatile SmsMode _mode;

    /// <summary>Creates native media against the selected operating-system service.</summary>
    /// <param name="platform">The native SMS service.</param>
    internal GXSMS(ISmsPlatform platform) => _smsPlatform = platform ?? throw new ArgumentNullException(nameof(platform));

    /// <summary>Gets whether this instance uses native SMS rather than AT modem commands.</summary>
    /// <remarks>Android's default constructor selects native SMS. An explicitly supplied serial stream factory retains modem behavior.</remarks>
    public bool UsesNativeSms => _smsPlatform != null;

    /// <summary>Creates a closed settings container with the same validation rules as this transport kind.</summary>
    /// <returns>A native or modem instance that cannot accidentally open a serial settings transport.</returns>
    internal GXSMS CreateSettingsDraft() => UsesNativeSms ? new GXSMS(_smsPlatform!)
        : new GXSMS((_, _) => throw new NotSupportedException(global::Gurux.SMS.Properties.Resources.Get("SmsDraftCannotOpen")));

    /// <summary>Gets or sets the SMS operations allowed while the component is open.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a supported SMS mode.</exception>
    /// <exception cref="InvalidOperationException">The component is open or opening.</exception>
    public SmsMode Mode
    {
        get => _mode;
        set
        {
            if (!Enum.IsDefined(value)) { throw new ArgumentOutOfRangeException(nameof(value)); }
            ApplySmsSettings(() => _mode = value);
        }
    }

    /// <summary>Excludes native startup as well as modem startup while applying settings.</summary>
    /// <param name="apply">The validated settings assignments.</param>
    private void ApplySmsSettings(Action apply)
    {
        lock (_smsLifecycle) { ApplySettings(apply); }
    }

    /// <inheritdoc/>
    public override bool IsOpen
    {
        get
        {
            if (!UsesNativeSms) { return base.IsOpen; }
            lock (_smsLifecycle) { return _smsSession != null; }
        }
    }

    /// <inheritdoc/>
    public override string Name => UsesNativeSms ? MediaType : base.Name;

    /// <inheritdoc/>
    /// <exception cref="UnauthorizedAccessException">The host has not obtained the native permissions required by the selected mode, or Android blocks SMS sending.</exception>
    /// <exception cref="NotSupportedException">The Android device does not support SMS or its required native services are unavailable.</exception>
    /// <exception cref="InvalidOperationException">Sending is enabled but no SMS subscription is selected or its SIM is not ready.</exception>
    /// <remarks>Native opening checks the selected operations without requesting permissions or sending a test message. Successful opening does not guarantee network availability or recipient delivery.</remarks>
    /// <exception cref="OperationCanceledException">The caller cancels startup.</exception>
    public override Task OpenAsync(CancellationToken cancellationToken = default)
    {
        if (!UsesNativeSms) { return base.OpenAsync(cancellationToken); }
        cancellationToken.ThrowIfCancellationRequested();
        lock (_smsLifecycle)
        {
            if (_smsSession != null) { return Task.CompletedTask; }
            var errors = Validate();
            if (errors.Count != 0)
            {
                ValidateForOpen();
                throw new ArgumentException(errors[0].Message);
            }
            var session = new SmsReceiveSession();
            try
            {
                session.Registration = _smsPlatform!.Open(Mode, message => NativeReceived(session, message));
                cancellationToken.ThrowIfCancellationRequested();
                _smsSession = session;
                ReceiveBuffer.Open();
                SetState(MediaState.Open);
            }
            catch
            {
                _smsSession = null;
                ReceiveBuffer.Close();
                session.Dispose();
                throw;
            }
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Task CloseAsync()
    {
        if (!UsesNativeSms) { return base.CloseAsync(); }
        CloseNativeSession();
        return Task.CompletedTask;
    }

    /// <summary>Synchronously unregisters the native session without waiting for asynchronous operations.</summary>
    private void CloseNativeSession()
    {
        lock (_smsLifecycle)
        {
            var session = _smsSession;
            if (session == null) { return; }
            _smsSession = null;
            ReceiveBuffer.Close();
            try { session.Dispose(); }
            finally { SetState(MediaState.Closed); }
        }
    }

    /// <summary>Accepts decoded input only from the currently open generation.</summary>
    /// <param name="session">The registration that received the broadcast.</param>
    /// <param name="message">The assembled Unicode SMS and sender.</param>
    private void NativeReceived(SmsReceiveSession session, GXSMSMessage message)
    {
        lock (_smsLifecycle)
        {
            if (!ReferenceEquals(_smsSession, session) || Mode == SmsMode.SendOnly) { return; }
            try
            {
                session.Add(message);
                PublishReceived(Encoding.UTF8.GetBytes(message.Data), message.PhoneNumber);
            }
            catch (Exception error)
            {
                if (!ReferenceEquals(_smsSession, session)) { return; }
                try { ReportError(error); }
                finally
                {
                    if (ReferenceEquals(_smsSession, session)) { CloseNativeSession(); }
                }
            }
        }
    }

    /// <summary>Rejects SMS operations disabled by the configured mode.</summary>
    /// <param name="sending">Whether to check sending instead of reception.</param>
    /// <exception cref="InvalidOperationException">The operation is disabled.</exception>
    private void RequireOperation(bool sending)
    {
        if (sending ? Mode == SmsMode.ReceiveOnly : Mode == SmsMode.SendOnly)
        {
            throw new InvalidOperationException(global::Gurux.SMS.Properties.Resources.Get(sending ? "SmsSendingDisabled" : "SmsReceivingDisabled"));
        }
    }

    /// <summary>Captures the open native generation for an SMS operation.</summary>
    /// <param name="sending">Whether the operation requires sending instead of reception.</param>
    /// <returns>The current session.</returns>
    /// <exception cref="InvalidOperationException">The native component is closed.</exception>
    private SmsReceiveSession RequireNativeSession(bool sending = false)
    {
        lock (_smsLifecycle)
        {
            RequireOperation(sending);
            return _smsSession ?? throw new InvalidOperationException(global::Gurux.SMS.Properties.Resources.Get("SmsMediaClosed"));
        }
    }

    /// <summary>Submits Unicode text and cancels acknowledgement waiting when its session closes.</summary>
    /// <param name="recipient">The destination number.</param>
    /// <param name="text">The message text.</param>
    /// <param name="cancellationToken">Cancels submission or acknowledgement waiting.</param>
    /// <returns>The native submission acknowledgement.</returns>
    private async Task<string> SendNativeAsync(string recipient, string text, CancellationToken cancellationToken)
    {
        var session = RequireNativeSession(true);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, session.Token);
        linked.CancelAfter(ConnectTimeout);
        linked.Token.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(recipient) || string.IsNullOrEmpty(text))
        {
            throw new ArgumentException(global::Gurux.SMS.Properties.Resources.Get("SmsRecipientAndTextRequired"));
        }
        try
        {
            string result = await _smsPlatform!.SendAsync(recipient, text, linked.Token).ConfigureAwait(false);
            AddBytesSent(Encoding.UTF8.GetByteCount(text));
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && !session.Token.IsCancellationRequested)
        {
            throw new TimeoutException(global::Gurux.SMS.Properties.Resources.Get("SmsSubmissionTimeout"));
        }
    }
}
