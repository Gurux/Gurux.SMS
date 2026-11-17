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
using System.Threading.Channels;

namespace Gurux.SMS;

/// <summary>Owns the bounded, in-memory inbox and independent watchers for one native open generation.</summary>
internal sealed class SmsReceiveSession : IDisposable
{
    /// <summary>Serializes inbox and watcher registration access.</summary>
    private readonly object sync = new();
    /// <summary>Retains only the most recent messages received during this session.</summary>
    private readonly Queue<GXSMSMessage> inbox = new();
    /// <summary>Contains independent bounded queues for active asynchronous watchers.</summary>
    private readonly List<Channel<GXSMSMessage>> watchers = [];
    /// <summary>Cancels native submissions and watchers when this session closes.</summary>
    private readonly CancellationTokenSource lifetime = new();
    /// <summary>Assigns stable session-local indexes to received messages.</summary>
    private int nextIndex;
    /// <summary>Records whether this session has stopped receiving.</summary>
    private bool closed;
    /// <summary>Gets the token canceled when the native connection closes.</summary>
    internal CancellationToken Token => lifetime.Token;
    /// <summary>Gets or sets the native registration released when closing.</summary>
    internal IDisposable? Registration { get; set; }

    /// <summary>Stores incoming content and fans it out without blocking the Android broadcast thread.</summary>
    /// <param name="message">The fully assembled incoming message.</param>
    internal void Add(GXSMSMessage message)
    {
        lock (sync)
        {
            if (closed) { return; }
            message.Index = nextIndex++;
            if (inbox.Count == 128) { inbox.Dequeue(); }
            inbox.Enqueue(message);
            foreach (var watcher in watchers)
            {
                if (!watcher.Writer.TryWrite(message))
                {
                    watcher.Writer.TryComplete(new IOException(Properties.Resources.Get("SmsWatcherOverflow")));
                }
            }
        }
    }

    /// <summary>Copies the messages received in the current session without accessing the device inbox.</summary>
    /// <returns>A stable snapshot of the bounded inbox.</returns>
    internal IReadOnlyList<GXSMSMessage> Snapshot()
    {
        lock (sync) { return inbox.ToArray(); }
    }

    /// <summary>Replays retained messages and then awaits new messages until cancellation or close.</summary>
    /// <param name="cancellationToken">Stops this watcher independently of other watchers.</param>
    /// <returns>Decoded messages belonging only to this session.</returns>
    internal async IAsyncEnumerable<GXSMSMessage> WatchAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, Token);
        var channel = Channel.CreateBounded<GXSMSMessage>(128);
        lock (sync)
        {
            linked.Token.ThrowIfCancellationRequested();
            foreach (var message in inbox) { channel.Writer.TryWrite(message); }
            watchers.Add(channel);
        }
        try
        {
            while (await channel.Reader.WaitToReadAsync(linked.Token).ConfigureAwait(false))
            {
                while (channel.Reader.TryRead(out var message))
                {
                    linked.Token.ThrowIfCancellationRequested();
                    yield return message;
                }
            }
        }
        finally
        {
            lock (sync) { watchers.Remove(channel); }
        }
    }

    /// <summary>Stops native reception and cancels all operations tied to this open generation.</summary>
    public void Dispose()
    {
        Channel<GXSMSMessage>[] pending;
        lock (sync)
        {
            if (closed) { return; }
            closed = true;
            pending = watchers.ToArray();
            watchers.Clear();
            inbox.Clear();
        }
        // Keep the token source valid for operations that captured this generation before close.
        lifetime.Cancel();
        foreach (var watcher in pending) { watcher.Writer.TryComplete(); }
        Registration?.Dispose();
    }
}
