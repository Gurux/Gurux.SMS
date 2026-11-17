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

namespace Gurux.SMS
{
    /// <summary>
    /// Selects GSM 7-bit, 8-bit or UCS-2 encoding for SMS data.
    /// </summary>
    public enum MessageCodeType
    {
        /// <summary>
        /// Uses the GSM default alphabet with up to 160 septets; extension characters consume two septets.
        /// </summary>
        Bits7 = 0,
        /// <summary>
        /// Encodes characters in the byte range as an 8-bit payload of up to 140 octets.
        /// </summary>
        Bits8 = 4,
        /// <summary>
        /// Encodes up to 70 UCS-2 characters in 140 octets; surrogate characters are unsupported.
        /// </summary>
        Unicode = 8
    }
}
