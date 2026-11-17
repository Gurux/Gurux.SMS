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
namespace Gurux.SMS;

/// <summary>Provides native SMS service operations independently of windowing frameworks.</summary>
internal interface ISmsPlatform
{
    /// <summary>Checks selected permissions and starts reception when requested.</summary>
    /// <param name="mode">The operations enabled for the new session.</param>
    /// <param name="receive">Receives decoded messages while the registration remains active.</param>
    /// <returns>The registration whose disposal stops native reception.</returns>
    IDisposable Open(SmsMode mode, Action<GXSMSMessage> receive);

    /// <summary>Submits a message, using native splitting and submission acknowledgements.</summary>
    /// <param name="recipient">The destination telephone number.</param>
    /// <param name="text">The complete Unicode text.</param>
    /// <param name="cancellationToken">Cancels acknowledgement waiting; already submitted SMS cannot be recalled.</param>
    /// <returns>The localized submission acknowledgement, without promising recipient delivery.</returns>
    Task<string> SendAsync(string recipient, string text, CancellationToken cancellationToken);
}
