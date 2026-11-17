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
    /// SMS Message memory type.
    /// </summary>
    public enum MemoryType
    {
        /// <summary>
        /// SMS Message memory type is unknown.
        /// </summary>
        Unknown = -1,
        /// <summary>
        /// SIM's Memory is used.
        /// </summary>
        Sim = 0,
        /// <summary>
        /// Phone's memory is used.
        /// </summary>
        Phone = 1
    }
}
