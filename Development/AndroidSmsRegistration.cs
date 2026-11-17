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
using Android.Content;
using Android.OS;

namespace Gurux.SMS;

/// <summary>Owns an optional context-scoped broadcast receiver and unregisters it exactly once.</summary>
internal sealed class AndroidSmsRegistration : IDisposable
{
    /// <summary>Retains the application context used to register the receiver.</summary>
    private readonly Context context;
    /// <summary>Contains the registered receiver until disposal.</summary>
    private AndroidSmsReceiver? receiver;

    /// <summary>Registers a broadcast callback with explicit export and optional sender permission restrictions.</summary>
    /// <param name="context">The application context.</param>
    /// <param name="action">The single broadcast action to receive, or null for a send-only registration.</param>
    /// <param name="permission">Permission required of a broadcast sender, or null for a private callback.</param>
    /// <param name="exported">Whether the telephony service outside this application must reach the receiver.</param>
    /// <param name="callback">The operation receiving matching broadcasts.</param>
    internal AndroidSmsRegistration(Context context, string? action, string? permission, bool exported, Action<Intent, int> callback)
    {
        this.context = context;
        if (action == null) { return; }
        var pending = new AndroidSmsReceiver(callback);
        try
        {
            using var filter = new IntentFilter(action);
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                context.RegisterReceiver(pending, filter, permission, null, exported ? ReceiverFlags.Exported : ReceiverFlags.NotExported);
            }
            else
            {
                context.RegisterReceiver(pending, filter, permission, null);
            }
            receiver = pending;
        }
        catch { pending.Dispose(); throw; }
    }

    /// <summary>Unregisters the callback and releases its Android peer.</summary>
    public void Dispose()
    {
        var previous = Interlocked.Exchange(ref receiver, null);
        if (previous != null)
        {
            try { context.UnregisterReceiver(previous); }
            finally { previous.Dispose(); }
        }
    }
}
#endif
