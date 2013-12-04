//
// --------------------------------------------------------------------------
//  Gurux Ltd
// 
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License 
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2. 
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gurux.SMS
{
    public class GXSMSMessage
    {
        /// <summary>
        /// SMS Data to send.
        /// </summary>
        public string Data
        {
            get;
            set;
        }

        /// <summary>
        /// Phone number where SMS is send or received.
        /// </summary>
        public string PhoneNumber
        {
            get;
            set;
        }

        /// <summary>
        /// When SMS is send.
        /// </summary>
        /// <remarks>
        /// This property is set only with read messages.
        /// </remarks>
        public DateTime Time
        {
            get;
            internal set;
        }


        /// <summary>
        /// Used Service Center number. 
        /// </summary>
        /// <remarks>
        /// This property is set only with read messages.
        /// </remarks>
        public string ServiceCenterNumber
        {
            get;
            internal set;
        }

        /// <summary>
        /// In which code type is used to code SMS data. 7 bit is default.
        /// </summary>
        public MessageCodeType CodeType
        {
            get;
            set;
        }

        /// <summary>
        /// SMS Msg status.
        /// </summary>
        /// <remarks>
        /// This property is set only with read messages.
        /// </remarks>
        public MessageStatus Status
        {
            get;
            internal set;
        }

        /// <summary>
        /// SMS Message index in the memory.
        /// </summary>
        /// <remarks>
        /// This property is set only with read messages.
        /// </remarks>
        public int Index
        {
            get;
            internal set;
        }

        /// <summary>
        /// SMS Message index in the memory.
        /// </summary>
        public MemoryType Memory
        {
            get;
            internal set;
        }
    }
}
