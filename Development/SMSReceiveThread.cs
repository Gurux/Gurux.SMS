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
using System.Threading;

namespace Gurux.SMS
{
    class SMSReceiveThread
    {
        public ManualResetEvent Closing;
        GXSMS m_Parent;

        public SMSReceiveThread(GXSMS parent)
        {
            Closing = new ManualResetEvent(false);
            m_Parent = parent;
        }

        /// <summary>
        /// Check are there new SMSs.
        /// </summary>
        public void Receive()
        {
            try
            {
                do
                {
                    if (m_Parent.IsSynchronous || m_Parent.m_OnReceived != null)
                    {
                        foreach (GXSMSMessage it in m_Parent.Read())
                        {
                            if (it.Status == MessageStatus.Unread)
                            {
                                try
                                {
                                    if (m_Parent.IsSynchronous)
                                    {
                                        m_Parent.SyncMessage = it;
                                        m_Parent.m_SMSReceived.Set();
                                        break;
                                    }
                                    m_Parent.m_OnReceived(m_Parent, new Gurux.Common.ReceiveEventArgs(it, it.PhoneNumber));
                                }
                                catch (Exception ex)
                                {
                                    m_Parent.NotifyError(ex);
                                }
                            }
                        }
                    }
                }
                while (!Closing.WaitOne(m_Parent.SMSCheckInterval * 1000));
            }
            catch (Exception ex)
            {
                m_Parent.NotifyError(ex);
                if (!Closing.WaitOne(1))
                {
                    m_Parent.Close();
                }
            }
        }
    }
}
