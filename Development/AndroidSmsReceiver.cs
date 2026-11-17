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

namespace Gurux.SMS;

/// <summary>Forwards dynamically registered Android broadcasts to a single SMS operation.</summary>
internal sealed class AndroidSmsReceiver : BroadcastReceiver
{
    /// <summary>Receives the broadcast and its ordered result code.</summary>
    private readonly Action<Intent, int> callback;

    /// <summary>Creates a receiver without manifest registration or application startup behavior.</summary>
    /// <param name="callback">The native session or submission receiving broadcasts.</param>
    internal AndroidSmsReceiver(Action<Intent, int> callback) => this.callback = callback;

    /// <inheritdoc/>
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent == null) { return; }
        try { callback(intent, (int)ResultCode); }
        catch (Exception error)
        {
            // Host event handlers must not terminate the process from an Android framework callback.
            Android.Util.Log.Error("Gurux.SMS", error.ToString());
        }
    }
}
#endif
