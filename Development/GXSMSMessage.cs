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
using Gurux.SMS.Enums;
using System;

namespace Gurux.SMS
{
    /// <summary>
    /// Stores the decoded content and storage metadata of an SMS message.
    /// </summary>
    public class GXSMSMessage
    {
        /// <summary>
        /// Canonical received PDU, used to identify unchanged stored messages.
        /// </summary>
        public string Pdu { get; internal set; } = string.Empty;
        /// <summary>Gets or sets the decoded message text.</summary>
        public string Data { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the sender or recipient telephone number.
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;
        /// <summary>
        /// Gets the service centre timestamp of a decoded incoming message.
        /// </summary>
        /// <remarks>
        /// This property is set only with read messages.
        /// </remarks>
        public DateTime Time { get; internal set; }
        /// <summary>
        /// Gets the service centre number decoded from the incoming PDU.
        /// </summary>
        /// <remarks>
        /// This property is set only with read messages.
        /// </remarks>
        public string ServiceCenterNumber { get; internal set; } = string.Empty;
        /// <summary>
        /// Gets or sets the message alphabet; the default value selects GSM 7-bit encoding.
        /// </summary>
        public MessageCodeType CodeType { get; set; }
        /// <summary>
        /// Gets the message status reported by the modem storage listing.
        /// </summary>
        /// <remarks>
        /// This property is set only with read messages.
        /// </remarks>
        public MessageStatus Status { get; internal set; }
        /// <summary>
        /// Gets the storage index assigned by the modem when reading or listing messages.
        /// </summary>
        /// <remarks>
        /// This property is set only with read messages.
        /// </remarks>
        public int Index { get; internal set; }
        /// <summary>
        /// Gets the storage location associated with the message.
        /// </summary>
        public MemoryType Memory { get; internal set; }
    }
}
