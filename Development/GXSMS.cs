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
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Gurux.Serial;
using Gurux.Common;
using Gurux.Terminal;
using Gurux.SMS.Enums;

namespace Gurux.SMS;
/// <summary>Asynchronous SMS media using Android telephony or AT modem commands on a serial transport.</summary>
public partial class GXSMS : GXTerminal
{
    /// <summary>Opens the embedded PNG icon for this media without loading a UI framework.</summary>
    /// <returns>A new readable stream positioned at the beginning, or null if the embedded icon is unavailable.</returns>
    /// <remarks>The caller owns the stream and must dispose it. Each call returns an independent stream.</remarks>
    public override Stream? OpenIcon()
    {
        return typeof(GXSMS).Assembly.GetManifestResourceStream("Gurux.SMS.MediaIcon.png");
    }

    /// <summary>Creates native SMS media on Android, or an operating system serial modem on other platforms.</summary>
    public GXSMS()
    {
#if ANDROID
        _smsPlatform = new AndroidSmsPlatform();
#endif
    }

    /// <summary>Creates SMS media that owns the modem stream returned by the supplied serial transport factory.</summary>
    /// <param name="streamFactory">Creates an owned readable and writable stream for the supplied serial settings and cancellation token.</param>
    /// <exception cref="ArgumentNullException">The stream factory is null.</exception>
    public GXSMS(Func<SerialPortSettings, CancellationToken, ValueTask<Stream>> streamFactory) : base(streamFactory)
    {
    }

    /// <inheritdoc/>
    protected override bool DialOnOpen => false;
    /// <summary>Gets or sets the optional SIM PIN supplied during modem initialization; native Android SMS ignores it.</summary>
    public string PIN { get; set; } = "";
    /// <summary>Gets or sets the modem's outgoing alphabet; Android chooses encoding and multipart boundaries automatically.</summary>
    public MessageCodeType CodeType { get; set; } = MessageCodeType.Bits7;
    /// <inheritdoc/>
    public override string MediaType => "SMS";

    /// <summary>Stores terminal configuration, optional SIM PIN, outgoing alphabet and SMS operating mode for JSON persistence.</summary>
    public sealed class SmsSettings
    {
        /// <summary>Gets or sets the JSON-serialized configuration of the underlying AT terminal.</summary>
        public string Terminal { get; set; } = "{}";
        /// <summary>Gets or sets the optional SIM PIN supplied during modem initialization.</summary>
        public string PIN { get; set; } = "";
        /// <summary>Gets or sets the alphabet used when encoding outgoing messages.</summary>
        public MessageCodeType CodeType { get; set; }
        /// <summary>Gets or sets which SMS operations are enabled; omitted legacy settings enable both.</summary>
        public SmsMode Mode { get; set; } = SmsMode.SendAndReceive;
    }

    /// <summary>Gets or replaces SMS settings using legacy terminal XML elements together with PIN, CodeType and Mode.</summary>
    /// <remarks>The PIN is included in plain text. Accepts XML fragments or an SMS wrapper; null restores defaults.</remarks>
    /// <exception cref="InvalidOperationException">The underlying serial transport is open, opening or awaiting cleanup.</exception>
    /// <exception cref="XmlException">The supplied XML is malformed.</exception>
    /// <exception cref="FormatException">A numeric, timeout or Boolean setting is malformed.</exception>
    /// <exception cref="ArgumentException">A serial framing or SMS alphabet enumeration cannot be parsed.</exception>
    public override string? Settings
    {
        get => base.Settings + new XElement("PIN", PIN).ToString(SaveOptions.DisableFormatting)
            + new XElement("CodeType", (int)CodeType).ToString(SaveOptions.DisableFormatting)
            + new XElement("Mode", (int)Mode).ToString(SaveOptions.DisableFormatting);
        set
        {
            var root = XElement.Parse("<Settings>" + value + "</Settings>");
            var xml = root.Element("SMS") ?? root;
            string pin = (string?)xml.Element("PIN") ?? string.Empty;
            MessageCodeType codeType = Enum.Parse<MessageCodeType>((string?)xml.Element("CodeType") ?? "Bits7");
            SmsMode selectedMode = Enum.Parse<SmsMode>((string?)xml.Element("Mode") ?? "SendAndReceive");
            if (!Enum.IsDefined(selectedMode)) { throw new ArgumentOutOfRangeException(nameof(value)); }
            ApplySmsSettings(() =>
            {
                base.Settings = string.Concat(xml.Elements().Select(element => element.ToString(SaveOptions.DisableFormatting)));
                PIN = pin;
                CodeType = codeType;
                _mode = selectedMode;
            });
        }
    }

    /// <summary>Gets or replaces JSON-serialized terminal settings, the SIM PIN, outgoing alphabet and operating mode.</summary>
    /// <remarks>The serialized value includes the PIN in plain text and must be treated as sensitive configuration.
    /// Assigning null restores defaults. The underlying transport must be closed with no pending open or cleanup work.</remarks>
    /// <exception cref="InvalidOperationException">The underlying serial transport is open, opening or awaiting cleanup.</exception>
    /// <exception cref="JsonException">The supplied SMS, terminal or serial settings are not valid JSON.</exception>
    /// <exception cref="ArgumentException">The SMS, terminal or serial JSON value is null.</exception>
    public override string? JsonSettings
    {
        get => JsonSerializer.Serialize(new SmsSettings { Terminal = base.JsonSettings!, PIN = PIN, CodeType = CodeType, Mode = Mode }, SmsJsonContext.Default.SmsSettings);
        set
        {
            var settings = value == null ? new SmsSettings() : JsonSerializer.Deserialize(value, SmsJsonContext.Default.SmsSettings) ?? throw new ArgumentException(global::Gurux.SMS.Properties.Resources.Get("InvalidSMSSettings"));
            if (!Enum.IsDefined(settings.Mode)) { throw new ArgumentOutOfRangeException(nameof(value)); }
            ApplySmsSettings(() =>
            {
                base.JsonSettings = settings.Terminal;
                PIN = settings.PIN;
                CodeType = settings.CodeType;
                _mode = settings.Mode;
            });
        }
    }

    /// <summary>Collects mode errors and native timeout errors or modem configuration and SIM PIN errors.</summary>
    /// <returns>All localized configuration errors, or an empty list when the selected mode is valid.</returns>
    public override IReadOnlyList<GXValidationError> Validate()
    {
        var errors = UsesNativeSms ? new List<GXValidationError>() : new List<GXValidationError>(base.Validate());
        if (!Enum.IsDefined(Mode))
        {
            errors.Add(new(nameof(Mode), "InvalidValue", global::Gurux.SMS.Properties.Resources.Get("InvalidSmsMode")));
        }
        if (UsesNativeSms)
        {
            if (ConnectTimeout <= TimeSpan.Zero)
            {
                errors.Add(new(nameof(ConnectTimeout), "OutOfRange", global::Gurux.SMS.Properties.Resources.Get("ConnectionTimeoutMustBePositive")));
            }
        }
        else if (PIN.Length > 0 && (PIN.Length is < 4 or > 8 || PIN.Any(c => !char.IsAsciiDigit(c))))
        {
            errors.Add(new(nameof(PIN), "InvalidValue", global::Gurux.SMS.Properties.Resources.Get("PINMustContain4To8Digits")));
        }
        return errors;
    }

    /// <summary>Validates the mode and, for AT modem connections, terminal settings and the optional SIM PIN.</summary>
    /// <exception cref="ArgumentException">The terminal settings or SIM PIN are invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The serial framing or command timeout is outside its supported range.</exception>
    protected override void ValidateForOpen()
    {
        if (!Enum.IsDefined(Mode)) { throw new ArgumentOutOfRangeException(nameof(Mode)); }
        if (UsesNativeSms)
        {
            if (ConnectTimeout <= TimeSpan.Zero) { throw new ArgumentOutOfRangeException(nameof(ConnectTimeout)); }
            return;
        }
        base.ValidateForOpen();
        if (PIN.Length > 0 && (PIN.Length is < 4 or > 8 || PIN.Any(c => !char.IsAsciiDigit(c))))
        {
            throw new ArgumentException(global::Gurux.SMS.Properties.Resources.Get("PINMustContain4To8Digits"));
        }
    }

    /// <inheritdoc/>
    protected override async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await base.InitializeAsync(cancellationToken).ConfigureAwait(false);
        if (PIN.Length != 0)
        {
            await ExecuteCommandAsync("AT+CPIN=\"" + PIN + "\"", cancellationToken).ConfigureAwait(false);
        }

        await ExecuteCommandAsync("AT+CMGF=0", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Submits SMS through Android telephony or the modem prompt exchange when sending is enabled.</summary>
    /// <param name="recipient">The destination telephone number, optionally prefixed with a plus sign.</param>
    /// <param name="text">The message text to encode using <see cref="CodeType"/>.</param>
    /// <param name="cancellationToken">Cancels the pending operation.</param>
    /// <returns>The modem response or native submission acknowledgement; recipient delivery is not guaranteed.</returns>
    /// <exception cref="InvalidOperationException">Sending is disabled, the component is closed, or the native SMS subscription or SIM is not ready.</exception>
    /// <exception cref="UnauthorizedAccessException">Android SMS sending permission has not been granted, has been revoked, or sending is blocked for this application.</exception>
    /// <exception cref="NotSupportedException">The Android device or its native services do not support SMS.</exception>
    /// <exception cref="TimeoutException">Android does not acknowledge submission within <see cref="GXTerminal.ConnectTimeout"/>.</exception>
    /// <exception cref="ArgumentException">The recipient or message cannot be encoded using the selected alphabet.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The alphabet is unsupported or the encoded message exceeds the single-message limit.</exception>
    /// <exception cref="IOException">The modem rejects the submission or its response exceeds the supported limit.</exception>
    /// <exception cref="OperationCanceledException">Cancellation, a command timeout or transport shutdown interrupts submission.</exception>
    public Task<string> SendMessageAsync(string recipient, string text, CancellationToken cancellationToken = default)
    {
        RequireOperation(true);
        if (UsesNativeSms) { return SendNativeAsync(recipient, text, cancellationToken); }
        string pdu = SmsPdu.EncodeSubmit(recipient, text, CodeType);
        return ExecutePromptAsync("AT+CMGS=" + (pdu.Length / 2 - 1), Encoding.ASCII.GetBytes(pdu + "\u001a"), cancellationToken);
    }

    /// <inheritdoc/>
    public override async ValueTask SendAsync(ReadOnlyMemory<byte> data, string? receiver = null, CancellationToken cancellationToken = default) => await SendMessageAsync(receiver ?? PhoneNumber, Encoding.UTF8.GetString(data.Span), cancellationToken).ConfigureAwait(false);
    /// <summary>Reads a received modem message, or a message retained during the current native session.</summary>
    /// <param name="index">The nonnegative modem storage index or current native session index.</param>
    /// <param name="cancellationToken">Cancels the pending operation.</param>
    /// <returns>The decoded message with its modem storage or native session index.</returns>
    /// <exception cref="InvalidOperationException">Reception is disabled or the component is closed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The storage index is negative.</exception>
    /// <exception cref="IOException">The modem reports an error or returns no message PDU.</exception>
    /// <exception cref="FormatException">The returned PDU is malformed or truncated.</exception>
    /// <exception cref="NotSupportedException">The PDU type or coding scheme is unsupported.</exception>
    /// <exception cref="OperationCanceledException">Cancellation, a command timeout or transport shutdown interrupts the read.</exception>
    public async Task<GXSMSMessage> ReadMessageAsync(int index, CancellationToken cancellationToken = default)
    {
        RequireOperation(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (UsesNativeSms)
        {
            return RequireNativeSession().Snapshot().FirstOrDefault(message => message.Index == index)
                ?? throw new ArgumentOutOfRangeException(nameof(index));
        }
        string response = await ExecuteCommandAsync("AT+CMGR=" + index, cancellationToken).ConfigureAwait(false);
        string pdu = response.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(line => line.Length > 0 && line.Length % 2 == 0 && line.All(Uri.IsHexDigit)) ?? throw new IOException(global::Gurux.SMS.Properties.Resources.Get("TheModemDidNotReturnAMessagePDU"));
        var message = SmsPdu.DecodeDeliver(pdu);
        message.Index = index;
        return message;
    }

    /// <summary>Lists received modem messages or the last 128 messages received during the current native session.</summary>
    /// <remarks>Native listing never reads or changes the Android device inbox and requires no READ_SMS permission.</remarks>
    /// <param name="cancellationToken">Cancels the pending operation.</param>
    /// <returns>The incoming modem messages or bounded snapshot of this native session's received messages.</returns>
    /// <exception cref="InvalidOperationException">Reception is disabled or the component is closed.</exception>
    /// <exception cref="IOException">The modem reports an error or returns a malformed message listing.</exception>
    /// <exception cref="FormatException">An incoming message PDU is malformed or truncated.</exception>
    /// <exception cref="NotSupportedException">An incoming message PDU uses an unsupported type or coding scheme.</exception>
    /// <exception cref="OperationCanceledException">Cancellation, a command timeout or transport shutdown interrupts the listing.</exception>
    public async Task<IReadOnlyList<GXSMSMessage>> ListMessagesAsync(CancellationToken cancellationToken = default)
    {
        RequireOperation(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (UsesNativeSms) { return RequireNativeSession().Snapshot(); }
        string response = await ExecuteCommandAsync("AT+CMGL=4,1", cancellationToken).ConfigureAwait(false);
        string[] lines = response.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var messages = new List<GXSMSMessage>();
        for (int i = 0; i < lines.Length; i++)
        {
            if (!lines[i].StartsWith("+CMGL:", StringComparison.Ordinal))
            {
                continue;
            }

            string[] fields = lines[i][6..].Split(',');
            if (fields.Length < 2 || !int.TryParse(fields[0].Trim(), out int index) || index < 0 || !int.TryParse(fields[1].Trim(), out int status))
            {
                throw new IOException(global::Gurux.SMS.Properties.Resources.Get("InvalidCMGLMessageHeader"));
            }

            if (++i >= lines.Length)
            {
                throw new IOException(global::Gurux.SMS.Properties.Resources.Get("CMGLHeaderHasNoMessagePDU"));
            }

            if (status is not (0 or 1))
            {
                continue;
            } // Stored outgoing SMS-SUBMIT records are not incoming messages.

            var message = SmsPdu.DecodeDeliver(lines[i].Trim());
            message.Index = index;
            message.Status = (MessageStatus)status;
            messages.Add(message);
        }

        return messages;
    }

    /// <summary>Watches native incoming SMS, or polls the modem while suppressing unchanged stored messages per watcher.</summary>
    /// <remarks>There is no background worker or deletion. Modem state is bounded by its inbox. Native watchers
    /// replay up to 128 retained messages and then receive live broadcasts until cancellation or component close.</remarks>
    /// <param name="pollInterval">The interval between modem inbox queries, or null for five seconds; native reception does not poll.</param>
    /// <param name="cancellationToken">Cancels the pending operation.</param>
    /// <returns>Native incoming messages or modem messages whose index and PDU have changed since the previous poll.</returns>
    /// <exception cref="InvalidOperationException">Reception is disabled or the component is closed.</exception>
    /// <exception cref="IOException">A native watcher falls behind by more than its bounded queue capacity.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The polling interval is not positive.</exception>
    /// <exception cref="OperationCanceledException">Cancellation, a command timeout or transport shutdown interrupts polling.</exception>
    public async IAsyncEnumerable<GXSMSMessage> WatchMessagesAsync(TimeSpan? pollInterval = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        RequireOperation(false);
        TimeSpan interval = pollInterval ?? TimeSpan.FromSeconds(5);
        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(pollInterval));
        }

        if (UsesNativeSms)
        {
            await foreach (var message in RequireNativeSession().WatchAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return message;
            }
            yield break;
        }

        var previous = new HashSet<(int Index, string Pdu)>();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var messages = await ListMessagesAsync(cancellationToken).ConfigureAwait(false);
            var current = new HashSet<(int Index, string Pdu)>();
            foreach (var message in messages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var key = (message.Index, message.Pdu);
                current.Add(key);
                if (!previous.Contains(key))
                {
                    yield return message;
                }
            }

            previous = current;
            await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Explicitly deletes the message stored at the specified modem index.</summary>
    /// <param name="index">The nonnegative storage index assigned by the modem.</param>
    /// <param name="cancellationToken">Cancels the pending operation.</param>
    /// <returns>The modem deletion response.</returns>
    /// <exception cref="NotSupportedException">This instance uses native Android SMS; device inbox deletion is not supported.</exception>
    /// <exception cref="InvalidOperationException">Reception is disabled.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The storage index is negative.</exception>
    /// <exception cref="IOException">The modem reports a deletion failure.</exception>
    /// <exception cref="OperationCanceledException">Cancellation, a command timeout or transport shutdown interrupts deletion.</exception>
    public Task<string> DeleteMessageAsync(int index, CancellationToken cancellationToken = default)
    {
        RequireOperation(false);
        if (UsesNativeSms) { throw new NotSupportedException(global::Gurux.SMS.Properties.Resources.Get("SmsNativeDeleteUnsupported")); }
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return ExecuteCommandAsync("AT+CMGD=" + index, cancellationToken);
    }
}
