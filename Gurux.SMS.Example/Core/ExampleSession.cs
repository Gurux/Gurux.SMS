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

using System.Globalization;
using System.Text;
using Gurux.Common;
using Gurux.Serial;
using Gurux.SMS;
using Gurux.SMS.Example.Properties;

namespace Gurux.SMS.Example;

/// <summary>Serializes modem operations, cancellation and bounded activity reporting for this application.</summary>
public sealed class ExampleSession : IAsyncDisposable
{
    /// <summary>Clears retained activity without changing received payloads, connection state or traffic counters.</summary>
    public void ClearActivity() => log.Clear();
    /// <summary>Stores received payloads independently of diagnostic activity.</summary>
    private readonly Queue<byte[]> receivedData = new();
    /// <summary>Tracks retained payload bytes to bound display history to 64 KiB.</summary>
    private int receivedDataSize;

    /// <summary>Clears received display history without changing the connection, counters or activity log.</summary>
    public void ClearReceived()
    {
        lock (sync)
        {
            receivedData.Clear();
            receivedDataSize = 0;
        }
    }

    /// <summary>Formats the bounded received payload history in arrival order.</summary>
    /// <param name="hex">Whether every payload must be shown as hexadecimal.</param>
    /// <returns>The received frames separated by platform line endings.</returns>
    public string ReceivedData(bool hex = false)
    {
        lock (sync)
        {
            return string.Join(Environment.NewLine, receivedData.Select(data => ExampleDataDisplay.Format(data, hex)));
        }
    }

    /// <summary>Copies incoming data into the independently bounded received display history.</summary>
    /// <param name="data">The received bytes or textual payload reported by the media.</param>
    private void RecordReceived(object? data)
    {
        byte[] payload = data is byte[] bytes ? bytes : System.Text.Encoding.UTF8.GetBytes(Convert.ToString(data, CultureInfo.CurrentCulture) ?? "");
        if (payload.Length == 0)
        {
            return;
        }
        byte[] retained = payload.AsSpan(Math.Max(0, payload.Length - 65536)).ToArray();
        lock (sync)
        {
            receivedData.Enqueue(retained);
            receivedDataSize += retained.Length;
            while (receivedData.Count > 500 || receivedDataSize > 65536)
            {
                receivedDataSize -= receivedData.Dequeue().Length;
            }
        }
    }

    /// <summary>Allows one modem operation at a time.</summary>
    private readonly SemaphoreSlim operations = new(1, 1);
    /// <summary>Protects cancellation and disposal state.</summary>
    private readonly object sync = new();
    /// <summary>Stores recent modem activity.</summary>
    private readonly SessionLog log = new();
    /// <summary>Cancels operations when the connection closes.</summary>
    private CancellationTokenSource lifetime = new();
    /// <summary>Records whether disposal has started.</summary>
    private bool disposed;
    /// <summary>Indicates that an operation holds the modem gate.</summary>
    private int busy;
    /// <summary>Gets the application's modem transport.</summary>
    public GXSMS Media { get; }
    /// <summary>Gets whether a modem operation is executing.</summary>
    public bool IsBusy => Volatile.Read(ref busy) != 0;
    /// <summary>Gets a stable copy of recent activity.</summary>
    public IReadOnlyList<string> Log => log.Snapshot;

    /// <summary>Creates the modem using the native serial port or a supplied platform stream factory.</summary>
    /// <param name="serialFactory">Optional USB, Bluetooth or test stream opener.</param>
    public ExampleSession(Func<SerialPortSettings, CancellationToken, ValueTask<Stream>>? serialFactory = null)
        : this(serialFactory is null ? new GXSMS() : new GXSMS(serialFactory))
    {
    }

    /// <summary>Observes an existing modem instance and takes responsibility for its disposal.</summary>
    /// <param name="media">The modem owned by the session.</param>
    /// <exception cref="ArgumentNullException">The modem is null.</exception>
    public ExampleSession(GXSMS media)
    {
        Media = media ?? throw new ArgumentNullException(nameof(media));
        Media.OnReceived += Received;
        Media.OnError += Error;
    }

    /// <summary>Records native SMS as Unicode text and modem bytes as diagnostic data without accessing UI objects.</summary>
    /// <param name="sender">The transport raising the callback.</param>
    /// <param name="args">The received data and sender information.</param>
    private void Received(object? sender, ReceiveEventArgs args)
    {
        RecordReceived(args.Data);
        if (args.Data is byte[] bytes)
        {
            if (Media.UsesNativeSms)
            {
                log.Add(string.Format(CultureInfo.CurrentCulture, ExampleResources.Received,
                    args.SenderInfo, Encoding.UTF8.GetString(bytes)));
                return;
            }
            log.Add(ExampleDataDisplay.IsAscii(bytes)
                ? string.Format(CultureInfo.CurrentCulture, ExampleResources.ReceivedBytes, args.SenderInfo, Convert.ToHexString(bytes), ExampleDataDisplay.Format(bytes))
                : string.Format(CultureInfo.CurrentCulture, ExampleResources.Received, args.SenderInfo, ExampleDataDisplay.Format(bytes)));
        }
        else
        {
            log.Add(string.Format(CultureInfo.CurrentCulture, ExampleResources.Received, args.SenderInfo, args.Data));
        }
    }
    /// <summary>Records transport failures for display by the application.</summary>
    /// <param name="sender">The transport raising the failure.</param>
    /// <param name="args">The transport exception.</param>
    private void Error(object? sender, MediaErrorEventArgs args) => log.Add(string.Format(CultureInfo.CurrentCulture, ExampleResources.Error, args.Exception.Message));

    /// <summary>Runs a modem action under the operation gate and linked cancellation.</summary>
    /// <typeparam name="T">The action result type.</typeparam>
    /// <param name="action">The asynchronous modem operation.</param>
    /// <param name="cancellationToken">Cancels waiting or executing the action.</param>
    /// <returns>The result of the modem action.</returns>
    /// <exception cref="ObjectDisposedException">Disposal has begun.</exception>
    /// <exception cref="OperationCanceledException">The caller or connection cancels the operation.</exception>
    private async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        CancellationToken token;
        lock (sync) { ObjectDisposedException.ThrowIf(disposed, this); token = lifetime.Token; }
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, cancellationToken);
        await operations.WaitAsync(linked.Token).ConfigureAwait(false);
        Interlocked.Exchange(ref busy, 1);
        try { linked.Token.ThrowIfCancellationRequested(); return await action(linked.Token).ConfigureAwait(false); }
        finally { Interlocked.Exchange(ref busy, 0); operations.Release(); }
    }

    /// <summary>Opens and initializes the modem with cancellation support.</summary>
    /// <param name="cancellationToken">Cancels opening or initialization.</param>
    /// <returns>A task completing after the modem opens.</returns>
    public Task OpenAsync(CancellationToken cancellationToken = default) => RunAsync(async token =>
    {
        await Media.OpenAsync(token).ConfigureAwait(false);
        log.Add(ExampleResources.Opened);
        return true;
    }, cancellationToken);

    /// <summary>Cancels pending operations, closes the modem and resets connection cancellation.</summary>
    /// <returns>A task completing after close and active operations finish.</returns>
    public async Task CloseAsync()
    {
        lock (sync) { lifetime.Cancel(); }
        var closing = Media.CloseAsync();
        await operations.WaitAsync().ConfigureAwait(false);
        try
        {
            await closing.ConfigureAwait(false);
            lock (sync) { lifetime.Dispose(); lifetime = new(); }
            log.Add(ExampleResources.Closed);
        }
        finally { operations.Release(); }
    }

    /// <summary>Sends a text message to the specified phone number.</summary>
    /// <param name="text">The complete SMS message text; literal \r and \n sequences become carriage returns and line feeds.</param>
    /// <param name="receiver">The destination phone number.</param>
    /// <param name="cancellationToken">Cancels the modem exchange.</param>
    /// <returns>The modem's send acknowledgement.</returns>
    /// <exception cref="ArgumentException">The destination is blank.</exception>
    public Task<string?> SendAsync(string text, string receiver, CancellationToken cancellationToken = default)
        => RunAsync<string?>(async token =>
        {
            if (string.IsNullOrWhiteSpace(receiver)) { throw new ArgumentException(ExampleResources.RecipientRequired, nameof(receiver)); }
            var result = await Media.SendMessageAsync(receiver, text.Replace(@"\r", "\r", StringComparison.Ordinal).Replace(@"\n", "\n", StringComparison.Ordinal), token).ConfigureAwait(false);
            log.Add(ExampleResources.SmsSent);
            return result;
        }, cancellationToken);

    /// <summary>Reads stored messages without deleting them.</summary>
    /// <param name="cancellationToken">Cancels listing messages.</param>
    /// <returns>The messages currently stored by the modem.</returns>
    public Task<IReadOnlyList<GXSMSMessage>> ListMessagesAsync(CancellationToken cancellationToken = default)
        => RunAsync(token => Media.ListMessagesAsync(token), cancellationToken);

    /// <summary>Watches incoming messages until canceled and records them in the activity log.</summary>
    /// <param name="cancellationToken">Stops watching messages.</param>
    /// <returns>A task representing the watch lifetime.</returns>
    public Task WatchMessagesAsync(CancellationToken cancellationToken = default) => RunAsync(async token =>
    {
        await foreach (var message in Media.WatchMessagesAsync(cancellationToken: token).ConfigureAwait(false))
        {
            log.Add(string.Format(CultureInfo.CurrentCulture, ExampleResources.SmsMessage, message));
        }
        return true;
    }, cancellationToken);

    /// <summary>Stops operations, detaches callbacks and disposes the owned modem once.</summary>
    /// <returns>A task completing after modem disposal.</returns>
    public async ValueTask DisposeAsync()
    {
        lock (sync) { if (disposed) { return; } disposed = true; }
        await CloseAsync().ConfigureAwait(false);
        Media.OnReceived -= Received;
        Media.OnError -= Error;
        await Media.DisposeAsync().ConfigureAwait(false);
    }
}
