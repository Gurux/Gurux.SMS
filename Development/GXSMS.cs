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
using System.Text;
using System.ComponentModel;
using Gurux.Common;
using System.IO;
using Gurux.Shared;
#if !NETSTANDARD2_0 && !NETSTANDARD2_1 && !NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1
using System.Windows.Forms;
#endif //!NETSTANDARD2_0 && !NETSTANDARD2_1 && !NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1
using System.Xml;
using System.Diagnostics;
using System.IO.Ports;
using System.Reflection;
using System.Threading;

namespace Gurux.SMS
{
    /// <summary>
    /// A media component that enables sending short messages (SMS).
    /// </summary>
    public class GXSMS : IGXMedia, INotifyPropertyChanged, IDisposable
    {
        private object m_sync = new object();
        internal AutoResetEvent m_SMSReceived = new AutoResetEvent(false);
        internal GXSMSMessage SyncMessage;
        int m_SMSCheckInterval;
        string m_PhoneNumber;
        GXSMSAsyncWorkForm ConnectingForm;
        bool SupportDirectSend;
        int m_ConnectionWaitTime = 3000;
        string m_PIN;
        TraceLevel m_Trace;
        static Dictionary<string, List<int>> BaudRates = new Dictionary<string, List<int>>();
        object m_Eop;
        GXSynchronousMediaBase m_syncBase;
        UInt64 m_BytesSent, m_BytesReceived;
        readonly object m_Synchronous = new object();
        readonly object m_baseLock = new object();
        internal System.IO.Ports.SerialPort m_base = new System.IO.Ports.SerialPort();
        ReceiveThread m_Receiver;
        SMSReceiveThread m_SMSReceiver;
        Thread m_ReceiverThread;
        Thread m_SMSReceiverThread;

        /// <summary>
        /// Get baud rates supported by given serial port.
        /// </summary>
        static public int[] GetAvailableBaudRates(string portName)
        {
            if (BaudRates.ContainsKey(portName.ToLower()))
            {
                return BaudRates[portName.ToLower()].ToArray();
            }
            if (string.IsNullOrEmpty(portName))
            {
                portName = GXSMS.GetPortNames()[0];
            }
            List<int> items = new List<int>();
            BaudRates[portName.ToLower()] = items;
            try
            {
                Int32 value = 0;
                using (SerialPort port = new SerialPort(portName))
                {
                    port.Open();
                    FieldInfo fi = port.BaseStream.GetType().GetField("commProp", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (fi != null)
                    {
                        object p = fi.GetValue(port.BaseStream);
                        value = (Int32)p.GetType().GetField("dwSettableBaud", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(p);
                    }
                }
                if (value != 0)
                {
                    if ((value & 0x1) != 0)
                    {
                        items.Add(75);
                    }
                    if ((value & 0x2) != 0)
                    {
                        items.Add(110);
                    }
                    if ((value & 0x8) != 0)
                    {
                        items.Add(150);
                    }
                    if ((value & 0x10) != 0)
                    {
                        items.Add(300);
                    }
                    if ((value & 0x20) != 0)
                    {
                        items.Add(600);
                    }
                    if ((value & 0x40) != 0)
                    {
                        items.Add(1200);
                    }
                    if ((value & 0x80) != 0)
                    {
                        items.Add(1800);
                    }
                    if ((value & 0x100) != 0)
                    {
                        items.Add(2400);
                    }
                    if ((value & 0x200) != 0)
                    {
                        items.Add(4800);
                    }
                    if ((value & 0x400) != 0)
                    {
                        items.Add(7200);
                    }
                    if ((value & 0x800) != 0)
                    {
                        items.Add(9600);
                    }
                    if ((value & 0x1000) != 0)
                    {
                        items.Add(14400);
                    }
                    if ((value & 0x2000) != 0)
                    {
                        items.Add(19200);
                    }
                    if ((value & 0x4000) != 0)
                    {
                        items.Add(38400);
                    }
                    if ((value & 0x8000) != 0)
                    {
                        items.Add(56000);
                    }
                    if ((value & 0x40000) != 0)
                    {
                        items.Add(57600);
                    }
                    if ((value & 0x20000) != 0)
                    {
                        items.Add(115200);
                    }
                    if ((value & 0x10000) != 0)
                    {
                        items.Add(128000);
                    }
                    if ((value & 0x10000000) != 0) //Programmable baud rate.
                    {
                        items.Add(0);
                    }
                }
            }
            catch
            {
                items.Clear();
            }
            //Add default baud rates.
            if (items.Count == 0)
            {
                items.Add(300);
                items.Add(600);
                items.Add(1800);
                items.Add(2400);
                items.Add(4800);
                items.Add(9600);
                items.Add(19200);
                items.Add(38400);
                items.Add(0); //Programmable baud rate.
            }
            return items.ToArray();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXSMS()
        {
            ConfigurableSettings = AvailableMediaSettings.All;
            m_syncBase = new GXSynchronousMediaBase(180);
            //Events are not currently implemented in Mono's SMS port.
            if (Environment.OSVersion.Platform != PlatformID.Unix)
            {
                m_base.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(GXSMS_DataReceived);
            }
        }

        internal void NotifyError(Exception ex)
        {
            if (m_OnError != null)
            {
                m_OnError(this, ex);
            }
            if (m_Trace >= TraceLevel.Error && m_OnTrace != null)
            {
                m_OnTrace(this, new TraceEventArgs(TraceTypes.Error, ex, null));
            }
        }

        void NotifyMediaStateChange(MediaState state)
        {
            if (m_Trace >= TraceLevel.Info && m_OnTrace != null)
            {
                m_OnTrace(this, new TraceEventArgs(TraceTypes.Info, state, null));
            }
            if (m_OnMediaStateChange != null)
            {
                m_OnMediaStateChange(this, new MediaStateEventArgs(state));
            }
        }

        /// <summary>
        /// What level of tracing is used.
        /// </summary>
        public TraceLevel Trace
        {
            get
            {
                return m_Trace;
            }
            set
            {
                m_Trace = m_syncBase.Trace = value;
            }
        }

        /// <summary>
        /// Gets the underlying System.IO.Stream object for a System.IO.Ports.SerialPort object.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public Stream BaseStream
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.BaseStream;
                }
            }
        }

        internal void GXSMS_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            try
            {
                int count = 0, index = 0;
                byte[] buff = null;
                lock (m_syncBase.receivedSync)
                {
                    int totalCount = 0;
                    index = m_syncBase.receivedSize;
                    while (m_base.IsOpen && (count = m_base.BytesToRead) != 0)
                    {
                        totalCount += count;
                        buff = new byte[count];
                        m_base.Read(buff, 0, count);
                        m_syncBase.AppendData(buff, 0, count);
                        m_BytesReceived += (uint)count;
                    }
                    if (totalCount != 0 && Eop != null) //Search Eop if given.
                    {
                        if (Eop is Array)
                        {
                            foreach (object eop in (Array)Eop)
                            {
                                totalCount = Gurux.Common.GXCommon.IndexOf(m_syncBase.m_Received, Gurux.Common.GXCommon.GetAsByteArray(eop), index, m_syncBase.receivedSize);
                                if (totalCount != -1)
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            totalCount = Gurux.Common.GXCommon.IndexOf(m_syncBase.m_Received, Gurux.Common.GXCommon.GetAsByteArray(Eop), index, m_syncBase.receivedSize);
                        }
                    }
                    m_syncBase.receivedEvent.Set();
                }
            }
            catch (Exception ex)
            {
                if (this.IsSynchronous)
                {
                    m_syncBase.Exception = ex;
                    m_syncBase.receivedEvent.Set();
                }
                else
                {
                    NotifyError(ex);
                }
            }
        }

        /// <summary>
        /// Used baud rate for communication.
        /// </summary>
        /// <remarks>Can be changed without disconnecting.</remarks>
        [Browsable(true)]
        [DefaultValue(9600)]
        [MonitoringDescription("BaudRate")]
        public int BaudRate
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.BaudRate;
                }
            }
            set
            {
                lock (m_baseLock)
                {
                    bool change = m_base.BaudRate != value;
                    m_base.BaudRate = value;
                    if (change)
                    {
                        NotifyPropertyChanged("BaudRate");
                    }
                }
            }
        }

        /// <summary>
        /// True if the port is in a break state; otherwise, false.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool BreakState
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.BreakState;
                }
            }
            set
            {
                bool change;
                lock (m_baseLock)
                {
                    change = m_base.BreakState != value;
                    m_base.BreakState = value;
                }
                if (change)
                {
                    NotifyPropertyChanged("BreakState");
                }
            }
        }

        /// <summary>
        /// Gets the number of bytes in the receive buffer.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public int BytesToRead
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.BytesToRead;
                }
            }
        }

        /// <summary>
        /// Gets the number of bytes in the send buffer.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int BytesToWrite
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.BytesToWrite;
                }
            }
        }

        /// <summary>
        /// Gets the state of the Carrier Detect line for the port.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool CDHolding
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.CDHolding;
                }
            }
        }

        /// <summary>
        /// Gets the state of the Clear-to-Send line.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool CtsHolding
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.CtsHolding;
                }
            }
        }

        /// <summary>
        /// Gets or sets the standard length of data bits per byte.
        /// </summary>
        [MonitoringDescription("DataBits")]
        [DefaultValue(8)]
        [Browsable(true)]
        public int DataBits
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.DataBits;
                }
            }
            set
            {
                bool change;
                lock (m_baseLock)
                {
                    change = m_base.DataBits != value;
                    m_base.DataBits = value;
                }
                if (change)
                {
                    NotifyPropertyChanged("DataBits");
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether null bytes are ignored when transmitted between the port and the receive buffer.
        /// </summary>
        [Browsable(true)]
        [DefaultValue(false)]
        [MonitoringDescription("DiscardNull")]
        public bool DiscardNull
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.DiscardNull;
                }
            }
            set
            {
                bool change;
                lock (m_baseLock)
                {
                    change = m_base.DiscardNull != value;
                    m_base.DiscardNull = value;
                }
                if (change)
                {
                    NotifyPropertyChanged("DiscardNull");
                }
            }
        }

        /// <summary>
        /// Gets the state of the Data Set Ready (DSR) signal.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool DsrHolding
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.DsrHolding;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value that enables the Data SMS Ready (DTR) signal during SMS communication.
        /// </summary>
        [DefaultValue(false)]
        [MonitoringDescription("DtrEnable")]
        [Browsable(true)]
        public bool DtrEnable
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.DtrEnable;
                }
            }
            set
            {
                bool change;
                lock (m_baseLock)
                {
                    change = m_base.DtrEnable != value;
                    m_base.DtrEnable = value;
                }
                if (change)
                {
                    NotifyPropertyChanged("DtrEnable");
                }
            }
        }

        /// <summary>
        /// Gets or sets the byte encoding for pre- and post-transmission conversion of text.
        /// </summary>
        [MonitoringDescription("Encoding")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public Encoding Encoding
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.Encoding;
                }
            }
            set
            {
                bool change;
                lock (m_baseLock)
                {
                    change = m_base.Encoding != value;
                    m_base.Encoding = value;
                }
                if (change)
                {
                    NotifyPropertyChanged("Encoding");
                }
            }
        }

        /// <summary>
        /// Gets or sets the handshaking protocol for SMS port transmission of data.
        /// </summary>
        [Browsable(true)]
        [MonitoringDescription("Handshake")]
        public Handshake Handshake
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.Handshake;
                }
            }
            set
            {
                bool change;
                lock (m_baseLock)
                {
                    change = m_base.Handshake != value;
                    m_base.Handshake = value;
                }
                if (change)
                {
                    NotifyPropertyChanged("Handshake");
                }
            }
        }

        /// <summary>
        /// Gets a value indicating the open or closed status of the System.IO.Ports.SerialPort object.
        /// </summary>
        [Browsable(false)]
        public bool IsOpen
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.IsOpen;
                }
            }
        }

        /// <summary>
        /// Gets or sets the parity-checking protocol.
        /// </summary>
        [Browsable(true)]
        [MonitoringDescription("Parity")]
        public Parity Parity
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.Parity;
                }
            }
            set
            {
                bool change;
                lock (m_baseLock)
                {
                    change = m_base.Parity != value;
                    m_base.Parity = value;
                }
                if (change)
                {
                    NotifyPropertyChanged("Parity");
                }
            }
        }

        /// <summary>
        /// Gets or sets the byte that replaces invalid bytes in a data stream when a parity error occurs.
        /// </summary>
        [Browsable(true)]
        [MonitoringDescription("ParityReplace")]
        [DefaultValue(63)]
        public byte ParityReplace
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.ParityReplace;
                }
            }
            set
            {
                bool change;
                lock (m_baseLock)
                {
                    change = m_base.ParityReplace != value;
                    m_base.ParityReplace = value;
                }
                if (change)
                {
                    NotifyPropertyChanged("ParityReplace");
                }
            }
        }

        /// <summary>
        /// Gets or sets the phone PIN number.
        /// </summary>
        public string PIN
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_PIN;
                }
            }
            set
            {
                bool change;
                lock (m_baseLock)
                {
                    change = m_PIN != value;
                    m_PIN = value;
                }
                if (change)
                {
                    NotifyPropertyChanged("PIN");
                }
            }
        }

        /// <summary>
        /// Gets or sets how long (ms.) modem answer is waited when connection is made.
        /// </summary>
        public int ConnectionWaitTime
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_ConnectionWaitTime;
                }
            }
            set
            {
                bool change;
                lock (m_baseLock)
                {
                    change = m_ConnectionWaitTime != value;
                    m_ConnectionWaitTime = value;
                }
                if (change)
                {
                    NotifyPropertyChanged("ConnectionWaitTime");
                }
            }
        }

        /// <summary>
        /// Gets or sets the port for communications, including but not limited to all available COM ports.
        /// </summary>
        [MonitoringDescription("PortName")]
        [Browsable(true)]
        [DefaultValue("COM1")]
        public string PortName
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.PortName;
                }
            }
            set
            {
                bool change;
                lock (m_baseLock)
                {
                    change = m_base.PortName != value;
                    m_base.PortName = value;
                }
                if (change)
                {
                    NotifyPropertyChanged("PortName");
                }
            }
        }

        /// <summary>
        /// Gets or sets the size of the System.IO.Ports.SerialPort input buffer.
        /// </summary>
        [DefaultValue(4096)]
        [MonitoringDescription("ReadBufferSize")]
        [Browsable(true)]
        public int ReadBufferSize
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.ReadBufferSize;
                }
            }
            set
            {
                bool change;
                lock (m_baseLock)
                {
                    change = m_base.ReadBufferSize != value;
                    m_base.ReadBufferSize = value;
                }
                if (change)
                {
                    NotifyPropertyChanged("ReadBufferSize");
                }
            }
        }

        /// <summary>
        /// Gets or sets the number of milliseconds before a time-out occurs when a read operation does not finish.
        /// </summary>
        [MonitoringDescription("ReadTimeout")]
        [Browsable(true)]
        [DefaultValue(-1)]
        public int ReadTimeout
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.ReadTimeout;
                }
            }
            set
            {
                bool change;
                lock (m_baseLock)
                {
                    change = m_base.ReadTimeout != value;
                    m_base.ReadTimeout = value;
                }
                if (change)
                {
                    NotifyPropertyChanged("ReadTimeout");
                }
            }
        }

        /// <summary>
        /// Gets or sets the number of bytes in the internal input buffer before a System.IO.Ports.SerialPort.DataReceived event occurs.
        /// </summary>
        [MonitoringDescription("ReceivedBytesThreshold")]
        [DefaultValue(1)]
        [Browsable(true)]
        public int ReceivedBytesThreshold
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.ReceivedBytesThreshold;
                }
            }
            set
            {
                bool change;
                lock (m_baseLock)
                {
                    change = m_base.ReceivedBytesThreshold != value;
                    m_base.ReceivedBytesThreshold = value;
                }
                if (change)
                {
                    NotifyPropertyChanged("ReceivedBytesThreshold");
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the Request to Send (RTS) signal is enabled during SMS communication.
        /// </summary>
        [MonitoringDescription("RtsEnable")]
        [DefaultValue(false)]
        [Browsable(true)]
        public bool RtsEnable
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.RtsEnable;
                }
            }
            set
            {
                bool change;
                lock (m_baseLock)
                {
                    change = m_base.RtsEnable != value;
                    m_base.RtsEnable = value;
                }
                if (change)
                {
                    NotifyPropertyChanged("RtsEnable");
                }
            }
        }

        /// <summary>
        /// Gets or sets the standard number of stopbits per byte.
        /// </summary>
        [MonitoringDescription("StopBits")]
        [Browsable(true)]
        public StopBits StopBits
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.StopBits;
                }
            }
            set
            {
                bool change;
                lock (m_baseLock)
                {
                    change = m_base.StopBits != value;
                    m_base.StopBits = value;
                }
                if (change)
                {
                    NotifyPropertyChanged("StopBits");
                }
            }
        }

        /// <summary>
        /// Gets or sets the size of the SMS port output buffer.
        /// </summary>
        [Browsable(true)]
        [DefaultValue(2048)]
        [MonitoringDescription("WriteBufferSize")]
        public int WriteBufferSize
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.WriteBufferSize;
                }
            }
            set
            {
                bool change;
                lock (m_baseLock)
                {
                    change = m_base.WriteBufferSize != value;
                    m_base.WriteBufferSize = value;
                }
                if (change)
                {
                    NotifyPropertyChanged("WriteBufferSize");
                }
            }
        }

        /// <summary>
        /// Gets or sets the number of milliseconds before a time-out occurs when a write operation does not finish.
        /// </summary>
        [MonitoringDescription("WriteTimeout")]
        [Browsable(true)]
        [DefaultValue(-1)]
        public int WriteTimeout
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_base.WriteTimeout;
                }
            }
            set
            {
                bool change;
                lock (m_baseLock)
                {
                    change = m_base.WriteTimeout != value;
                    m_base.WriteTimeout = value;
                }
                if (change)
                {
                    NotifyPropertyChanged("WriteTimeout");
                }
            }
        }

        /// <summary>
        /// Closes the port connection, sets the System.IO.Ports.SerialPort.IsOpen property to false, and disposes of the internal System.IO.Stream object.
        /// </summary>
        public void Close()
        {
            bool bOpen;
            lock (m_baseLock)
            {
                bOpen = m_base.IsOpen;
            }
            if (bOpen)
            {
                try
                {
                    m_syncBase.Close();
                    NotifyMediaStateChange(MediaState.Closing);
                    if (m_SMSReceiver != null)
                    {
                        m_SMSReceiver.Closing.Set();
                    }
                    if (m_SMSReceiverThread != null && m_SMSReceiverThread.IsAlive)
                    {
                        m_SMSReceiverThread.Join();
                    }
                }
                catch (Exception ex)
                {
                    NotifyError(ex);
                    throw;
                }
                finally
                {
                    try
                    {
                        if (m_Receiver != null)
                        {
                            m_Receiver.Closing.Set();
                        }
                        lock (m_baseLock)
                        {
                            m_base.Close();
                        }
                        if (m_ReceiverThread != null && m_ReceiverThread.IsAlive)
                        {
                            m_ReceiverThread.Join();
                        }
                    }
                    catch
                    {
                        //Ignore all errors on close.
                    }
                    NotifyMediaStateChange(MediaState.Closed);
                }
            }
        }

        /// <summary>
        /// Discards data from the SMS driver's receive buffer.
        /// </summary>
        public void DiscardInBuffer()
        {
            lock (m_baseLock)
            {
                m_base.DiscardInBuffer();
            }
        }

        /// <summary>
        /// Discards data from the SMS driver's transmit buffer.
        /// </summary>
        public void DiscardOutBuffer()
        {
            lock (m_baseLock)
            {
                m_base.DiscardOutBuffer();
            }
        }

        /// <summary>
        /// Gets an array of serial port names for the current computer.
        /// </summary>
        /// <returns></returns>
        public static string[] GetPortNames()
        {
            return System.IO.Ports.SerialPort.GetPortNames();
        }

        /// <summary>
        /// User defined available ports.
        /// </summary>
        /// <remarks>If this is not set ports are retrieved from current system.</remarks>
        public string[] AvailablePorts
        {
            get;
            set;
        }

        string GetError(string reply)
        {
            int pos = reply.IndexOf("ERROR:");
            if (pos != -1)
            {
                return reply.Substring(pos + 6).Trim();
            }
            return reply.Trim();
        }

        /// <summary>
        /// Opens a new SMS port connection.
        /// </summary>
        /// <remarks>
        /// If connection is succeeded but Modem data is not move try to set following:
        /// DTE/Modem flow control
        /// AT&amp;K0  Disable flow control.
        /// AT&amp;Q0  Direct Asynchronous mode
        /// </remarks>
        public void Open()
        {
            Close();
            try
            {
                m_syncBase.Open();
                NotifyMediaStateChange(MediaState.Opening);
                if (m_Trace >= TraceLevel.Info && m_OnTrace != null)
                {
                    string eop = "None";
                    if (Eop is byte[])
                    {
                        eop = BitConverter.ToString(Eop as byte[], 0);
                    }
                    else if (Eop != null)
                    {
                        eop = Eop.ToString();
                    }
                    m_OnTrace(this, new TraceEventArgs(TraceTypes.Info, "Settings: Port: " + this.PortName +
                                " Baud Rate: " + BaudRate + " Data Bits: " + DataBits.ToString() + " Parity: "
                                + Parity.ToString() + " Stop Bits: " + StopBits.ToString() + " Eop:" + eop, null));
                }
                lock (m_baseLock)
                {
                    m_base.Open();
                    m_base.WriteTimeout = m_ConnectionWaitTime;
                    m_base.DiscardOutBuffer();
                    m_base.DiscardInBuffer();
                }
                //Events are not currently implemented in Mono's serial port.
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    m_Receiver = new ReceiveThread(this);
                    m_ReceiverThread = new Thread(new ThreadStart(m_Receiver.Receive));
                    m_ReceiverThread.IsBackground = true;
                    m_ReceiverThread.Start();
                }
                this.DtrEnable = this.RtsEnable = true;
                try
                {
                    //Send AT
                    lock (m_baseLock)
                    {
                        if (InitializeCommands != null)
                        {
                            foreach (string it in InitializeCommands)
                            {
                                SendCommand(it + "\r\n", true);
                            }
                        }
                        string reply;
                        //Send AT
                        reply = SendCommand("AT\r", false);
                        if (string.Compare(reply, "OK", true) != 0)
                        {
                            m_base.DiscardOutBuffer();
                            m_base.DiscardInBuffer();
                            reply = SendCommand("AT\r", false);
                            if (string.Compare(reply, "OK", true) != 0)
                            {
                                reply = SendCommand("+++", "+++", true);
                                if (string.IsNullOrEmpty(reply))
                                {
                                    throw new Exception("Invalid reply.");
                                }
                                reply = SendCommand("AT\r", true);
                                if (string.Compare(reply, "OK", true) != 0)
                                {
                                    throw new Exception("Invalid reply.");
                                }
                            }
                        }
                        //Set PDU mode.
                        reply = SendCommand("AT+CMGF=0\r", false);
                        if (reply != "OK")
                        {
                            throw new Exception("Modem is not supporting PDU.");
                        }

                        //Enable error reporting. It's OK if this fails.
                        reply = SendCommand("AT+CMEE\r", false);
                        //Enable verbode error code,
                        reply = SendCommand("AT+CMEE=2\r", false);
                        if (reply != "OK")
                        {
                            //Enable numeric error codes
                            reply = SendCommand("AT+CMEE=1\r", false);
                        }
                        reply = SendCommand("AT+CPIN=?\r", false);
                        bool pinSupported = reply == "OK";
                        //Is PIN Code supported.
                        if (pinSupported)
                        {
                            //Check PIN-Code
                            //reply = SendCommand("AT+CPIN?\r", "+CPIN: READY\r\n\r\nOK\r\n", false);
                            reply = SendCommand("AT+CPIN?\r", false);
                            if (reply.Contains("ERROR:"))
                            {
                                throw new Exception("Failed to read PIN code.\r\n" + GetError(reply));
                            }
                            //If PIN code is needed.
                            if (reply != "+CPIN: READY")
                            {
                                if (string.IsNullOrEmpty(m_PIN))
                                {
                                    throw new Exception("PIN is needed.");
                                }
                                reply = SendCommand(string.Format("AT+CPIN=\"{0}\"\r", m_PIN), false);
                                if (reply != "OK")
                                {
                                    throw new Exception("Failed to set PIN code." + GetError(reply));
                                }
                                //Ask PIN Code again.
                                reply = SendCommand("AT+CPIN?\r", false);
                                if (reply != "OK")
                                {
                                    throw new Exception("Failed to set PIN code." + GetError(reply));
                                }
                            }
                        }
                        //Is direct SMS sending supported.
                        reply = SendCommand("at+cmgs=?\r", false);
                        SupportDirectSend = reply.Contains("OK");
                        //Start SMS checker.
                        if (m_SMSCheckInterval != 0)
                        {
                            m_SMSReceiver = new SMSReceiveThread(this);
                            m_SMSReceiverThread = new Thread(new ThreadStart(m_SMSReceiver.Receive));
                            m_SMSReceiverThread.IsBackground = true;
                            m_SMSReceiverThread.Start();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Close();
                    throw ex;
                }
                NotifyMediaStateChange(MediaState.Open);
            }
            catch
            {
                Close();
                throw;
            }
        }

        string Connect(string cmd)
        {
            string eop = "\r\n";
            Gurux.Common.ReceiveParameters<string> p = new Gurux.Common.ReceiveParameters<string>()
            {
                WaitTime = m_ConnectionWaitTime,
                Eop = eop
            };
            SendBytes(ASCIIEncoding.ASCII.GetBytes(cmd));
            StringBuilder sb = new StringBuilder();
            int index = -1;
            bool connected = false;
            String str = "";
            while (index == -1)
            {
                if (!m_syncBase.Receive(p))
                {
                    throw new Exception("Connection failed.");
                }
                sb.Append(p.Reply);
                str = sb.ToString();
                connected = str.LastIndexOf("CONNECT") != -1;
                if (connected)
                {
                    index = str.LastIndexOf("\r");
                }
                else
                {
                    if (str.LastIndexOf("NO CARRIER") != -1)
                    {
                        throw new Exception("Connection failed: no carrier (when telephone call was being established). ");
                    }
                    if (str.LastIndexOf("ERROR") != -1)
                    {
                        throw new Exception("Connection failed: error (when telephone call was being established).");
                    }
                    if (str.LastIndexOf("BUSY") != -1)
                    {
                        throw new Exception("Connection failed: busy (when telephone call was being established).");
                    }
                }
                p.Reply = null;
                //After first success read one byte at the time.
                p.Eop = null;
                p.Count = 1;
            }
            string reply = sb.ToString();
            return reply;
        }

        void SendBytes(byte[] value)
        {
            lock (m_baseLock)
            {
                if (m_Trace == TraceLevel.Verbose && m_OnTrace != null)
                {
                    m_OnTrace(this, new TraceEventArgs(TraceTypes.Sent, value, null));
                }
                m_BytesSent += (uint)value.Length;
                //Reset last position if Eop is used.
                lock (m_syncBase.receivedSync)
                {
                    m_syncBase.lastPosition = 0;
                }
                m_base.Write(value, 0, value.Length);
            }
        }

        string SendCommand(string cmd, bool throwError)
        {
            return SendCommand(cmd, null, throwError);
        }

        string SendCommand(string cmd, string eop, bool throwError)
        {
            Gurux.Common.ReceiveParameters<string> p = new Gurux.Common.ReceiveParameters<string>()
            {
                WaitTime = m_ConnectionWaitTime,
                Eop = eop == null ? "\r\n" : eop
            };
            if (p.Eop.Equals(""))
            {
                p.Eop = null;
                p.Count = cmd.Length;
            }
            SendBytes(ASCIIEncoding.ASCII.GetBytes(cmd));
            StringBuilder sb = new StringBuilder();
            int index = -1;
            string reply = "";
            while (index == -1)
            {
                if (!Receive(p))
                {
                    if (throwError)
                    {
                        throw new Exception("Failed to receive answer from the modem. Check serial port.");
                    }
                    return "";
                }
                sb.Append(p.Reply);
                reply = sb.ToString();
                //Remove echo.
                if (sb.Length >= cmd.Length && reply.StartsWith(cmd))
                {
                    sb.Remove(0, cmd.Length);
                    reply = sb.ToString();
                    //Remove echo and return if we are not expecting reply.
                    if (eop == "")
                    {
                        return "";
                    }
                }
                if (eop != null)
                {
                    index = reply.LastIndexOf(eop);
                }
                else if (reply.Length > 5)
                {
                    index = reply.LastIndexOf("\r\nOK\r\n");
                    if (index == -1)
                    {
                        index = reply.LastIndexOf("ERROR:");
                    }
                    //If there is a message before OK show it.
                    else if (index != 0)
                    {
                        reply = reply.Remove(index);
                        index = 0;
                    }
                }
                p.Reply = null;
            }
            if (index != 0 & eop == null)
            {
                reply = reply.Remove(0, index);
            }
            reply = reply.Trim();
            return reply;
        }

        #region Events
        /// <summary>
        /// GXNet component sends received data through this method.
        /// </summary>
        [Description("GXNet component sends received data through this method.")]
        public event ReceivedEventHandler OnReceived
        {
            add
            {
                m_OnReceived += value;
            }
            remove
            {
                m_OnReceived -= value;
            }
        }

        /// <summary>
        /// Errors that occur after the connection is established, are sent through this method.
        /// </summary>
        [Description("Errors that occur after the connection is established, are sent through this method.")]
        public event Gurux.Common.ErrorEventHandler OnError
        {
            add
            {

                m_OnError += value;
            }
            remove
            {
                m_OnError -= value;
            }
        }

        /// <summary>
        /// Media component sends notification, when its state changes.
        /// </summary>
        [Description("Media component sends notification, when its state changes.")]
        public event MediaStateChangeEventHandler OnMediaStateChange
        {
            add
            {
                m_OnMediaStateChange += value;
            }
            remove
            {
                m_OnMediaStateChange -= value;
            }
        }

        /// <summary>
        /// Called when the client is establishing a connection with a Net Server.
        /// </summary>
        [Description("Called when the client is establishing a connection with a Net Server.")]
        public event ClientConnectedEventHandler OnClientConnected
        {
            add
            {
                m_OnClientConnected += value;
            }
            remove
            {
                m_OnClientConnected -= value;
            }
        }

        /// <summary>
        /// Called when the client has been disconnected from the network server.
        /// </summary>
        [Description("Called when the client has been disconnected from the network server.")]
        public event ClientDisconnectedEventHandler OnClientDisconnected
        {
            add
            {
                m_OnClientDisconnected += value;
            }
            remove
            {
                m_OnClientDisconnected -= value;
            }
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                m_OnPropertyChanged += value;
            }
            remove
            {
                m_OnPropertyChanged -= value;
            }
        }

        /// <inheritdoc cref="TraceEventHandler"/>
        [Description("Called when the Media is sending or receiving data.")]
        public event TraceEventHandler OnTrace
        {
            add
            {
                m_OnTrace += value;
            }
            remove
            {
                m_OnTrace -= value;
            }
        }

        private void NotifyPropertyChanged(String info)
        {
            if (m_OnPropertyChanged != null)
            {
                m_OnPropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }


        //Events
        TraceEventHandler m_OnTrace;
        PropertyChangedEventHandler m_OnPropertyChanged;
        MediaStateChangeEventHandler m_OnMediaStateChange;
        ClientConnectedEventHandler m_OnClientConnected;
        ClientDisconnectedEventHandler m_OnClientDisconnected;
        internal Gurux.Common.ErrorEventHandler m_OnError;
        internal ReceivedEventHandler m_OnReceived;

        #endregion //Events

        /// <inheritdoc cref="IGXMedia.ConfigurableSettings"/>
        public AvailableMediaSettings ConfigurableSettings
        {
            get
            {
                return (Gurux.SMS.AvailableMediaSettings)((IGXMedia)this).ConfigurableSettings;
            }
            set
            {
                ((IGXMedia)this).ConfigurableSettings = (int)value;
            }
        }

        /// <inheritdoc cref="IGXMedia.Tag"/>
        public object Tag
        {
            get;
            set;
        }

        /// <inheritdoc cref="IGXMedia.MediaContainer"/>
        IGXMediaContainer IGXMedia.MediaContainer
        {
            get;
            set;
        }

        /// <inheritdoc cref="IGXMedia.SyncRoot"/>
        [Browsable(false), ReadOnly(true)]
        public object SyncRoot
        {
            get
            {
                //In some special cases when binary serialization is used this might be null
                //after deserialize. Just set it.
                if (m_sync == null)
                {
                    m_sync = new object();
                }
                return m_sync;
            }
        }

        /// <inheritdoc cref="IGXMedia.Synchronous"/>
        public object Synchronous
        {
            get
            {
                return m_Synchronous;
            }
        }

        /// <inheritdoc cref="IGXMedia.IsSynchronous"/>
        public bool IsSynchronous
        {
            get
            {
                bool reserved = System.Threading.Monitor.TryEnter(m_Synchronous, 0);
                if (reserved)
                {
                    System.Threading.Monitor.Exit(m_Synchronous);
                }
                return !reserved;
            }
        }

        /// <inheritdoc cref="IGXMedia.ResetSynchronousBuffer"/>
        public void ResetSynchronousBuffer()
        {
            lock (m_syncBase.receivedSync)
            {
                m_syncBase.receivedSize = 0;
            }
        }

        #region IGXMedia Members

        /// <summary>
        /// Sent byte count.
        /// </summary>
        /// <seealso cref="BytesReceived">BytesReceived</seealso>
        /// <seealso cref="ResetByteCounters">ResetByteCounters</seealso>
        [Browsable(false)]
        public UInt64 BytesSent
        {
            get
            {
                return m_BytesSent;
            }
        }

        /// <summary>
        /// Received byte count.
        /// </summary>
        /// <seealso cref="BytesSent">BytesSent</seealso>
        /// <seealso cref="ResetByteCounters">ResetByteCounters</seealso>
        [Browsable(false)]
        public UInt64 BytesReceived
        {
            get
            {
                return m_BytesReceived;
            }
        }

        /// <summary>
        /// Gets or sets how ofter new SMSs are check.
        /// </summary>
        public int SMSCheckInterval
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_SMSCheckInterval;
                }
            }
            set
            {
                bool change;
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("SMS Check Interval is given in seconds.");
                }
                lock (m_baseLock)
                {
                    change = m_SMSCheckInterval != value;
                    m_SMSCheckInterval = value;
                }
                if (change)
                {
                    NotifyPropertyChanged("SMSCheckInterval");
                }
            }
        }

        /// <summary>
        /// Gets or sets the phone number.
        /// </summary>
        public string PhoneNumber
        {
            get
            {
                lock (m_baseLock)
                {
                    return m_PhoneNumber;
                }
            }
            set
            {
                bool change;
                lock (m_baseLock)
                {
                    change = m_PhoneNumber != value;
                    m_PhoneNumber = value;
                }
                if (change)
                {
                    NotifyPropertyChanged("PhoneNumber");
                }
            }
        }

        /// <summary>
        /// Resets BytesReceived and BytesSent counters.
        /// </summary>
        /// <seealso cref="BytesSent">BytesSent</seealso>
        /// <seealso cref="BytesReceived">BytesReceived</seealso>
        public void ResetByteCounters()
        {
            m_BytesSent = m_BytesReceived = 0;
        }

        void Gurux.Common.IGXMedia.Copy(object target)
        {
            GXSMS Target = (GXSMS)target;
            BaudRate = Target.BaudRate;
            StopBits = Target.StopBits;
            Parity = Target.Parity;
            DataBits = Target.DataBits;
        }

        /// <inheritdoc cref="IGXMedia.Eop"/>
        public object Eop
        {
            get
            {
                return m_Eop;
            }
            set
            {
                bool change = m_Eop != value;
                m_Eop = value;
                if (change)
                {
                    NotifyPropertyChanged("Eop");
                }
            }
        }

        /// <summary>
        /// Modem initial settings.
        /// </summary>
        string[] InitializeCommands
        {
            get;
            set;
        }

        /// <summary>
        /// Media settings as a XML string.
        /// </summary>
        public string Settings
        {
            get
            {
                string tmp = "";
                if (!string.IsNullOrEmpty(PIN))
                {
                    tmp += "<PIN>" + PIN + "</PIN>";
                }
                if (!string.IsNullOrEmpty(PhoneNumber))
                {
                    tmp += "<Number>" + PhoneNumber + "</Number>";
                }
                if (this.SMSCheckInterval != 0)
                {
                    tmp += "<Interval>" + SMSCheckInterval + "</Interval>";
                }
                if (!string.IsNullOrEmpty(PortName))
                {
                    tmp += "<Port>" + PortName + "</Port>";
                }
                if (BaudRate != 9600)
                {
                    tmp += "<Bps>" + BaudRate + "</Bps>";
                }
                if (this.StopBits != System.IO.Ports.StopBits.None)
                {
                    tmp += "<StopBits>" + (int)StopBits + "</StopBits>";
                }
                if (this.Parity != System.IO.Ports.Parity.None)
                {
                    tmp += "<Parity>" + (int)Parity + "</Parity>";
                }
                if (this.DataBits != 8)
                {
                    tmp += "<ByteSize>" + DataBits + "</ByteSize>";
                }
                if (this.InitializeCommands != null)
                {
                    tmp += "<Init>" + string.Join(";", this.InitializeCommands) + "</Init>";
                }
                return tmp;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.ConformanceLevel = ConformanceLevel.Fragment;
                    string str;
                    int result;
                    using (XmlReader xmlReader = XmlReader.Create(new System.IO.StringReader(value), settings))
                    {
                        while (xmlReader.Read())
                        {
                            if (xmlReader.IsStartElement())
                            {
                                switch (xmlReader.Name)
                                {
                                    case "Init":
                                        InitializeCommands = xmlReader.ReadString().Split(new char[] { ';' });
                                        break;
                                    case "PIN":
                                        PIN = xmlReader.ReadString();
                                        break;
                                    case "Number":
                                        PhoneNumber = xmlReader.ReadString();
                                        break;
                                    case "Interval":
                                        SMSCheckInterval = Convert.ToInt32(xmlReader.ReadString());
                                        break;
                                    case "Port":
                                        PortName = xmlReader.ReadString();
                                        break;
                                    case "Bps":
                                        BaudRate = Convert.ToInt32(xmlReader.ReadString());
                                        break;
                                    case "StopBits":
                                        str = xmlReader.ReadString();
                                        if (int.TryParse(str, out result))
                                        {
                                            StopBits = (StopBits)result;
                                        }
                                        else
                                        {
                                            StopBits = (StopBits)Enum.Parse(typeof(StopBits), str);
                                        }

                                        break;
                                    case "Parity":
                                        str = xmlReader.ReadString();
                                        if (int.TryParse(str, out result))
                                        {
                                            Parity = (Parity)result;
                                        }
                                        else
                                        {
                                            Parity = (Parity)Enum.Parse(typeof(System.IO.Ports.Parity), str);
                                        }
                                        break;
                                    case "ByteSize":
                                        DataBits = Convert.ToInt32(xmlReader.ReadString());
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Current SMS settings as a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(PortName);
            sb.Append(' ');
            sb.Append(BaudRate);
            sb.Append(' ');
            sb.Append(DataBits);
            sb.Append(Parity);
            sb.Append((int)StopBits);
            sb.Append(" PIN:");
            sb.Append(PIN);
            sb.Append(" Number:");
            sb.Append(PhoneNumber);
            sb.Append(" Interval:");
            sb.Append(SMSCheckInterval);
            return sb.ToString();
        }

        string Gurux.Common.IGXMedia.MediaType
        {
            get
            {
                return "SMS";
            }
        }

        bool Gurux.Common.IGXMedia.Enabled
        {
            get
            {
                return GetPortNames().Length != 0;
            }
        }

        string Gurux.Common.IGXMedia.Name
        {
            get
            {
                return this.PortName;
            }
        }

#if !NETSTANDARD2_0 && !NETSTANDARD2_1 && !NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1
        /// <summary>
        /// Shows the serial port Properties dialog.
        /// </summary>
        /// <param name="parent">Owner window of the Properties dialog.</param>
        /// <returns>True, if the user has accepted the changes.</returns>
        /// <seealso cref="PortName">PortName</seealso>
        /// <seealso cref="BaudRate">BaudRate</seealso>
        /// <seealso cref="DataBits">DataBits</seealso>
        /// <seealso href="PropertiesDialog.html">Properties Dialog</seealso>
        public bool Properties(Form parent)
        {
            return new Gurux.Shared.PropertiesForm(this.PropertiesForm, Gurux.SMS.Properties.Resources.SettingsTxt, IsOpen).ShowDialog(parent) == DialogResult.OK;
        }

        /// <summary>
        /// Returns a new instance of the Settings form.
        /// </summary>
        public System.Windows.Forms.Form PropertiesForm
        {
            get
            {
                return new Settings(this);
            }
        }

#endif //!NETSTANDARD2_0 && !NETSTANDARD2_1 && !NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1
        /// <summary>
        /// Sends SMS message asynchronously. <br/>
        /// No reply from the receiver, whether or not the operation was successful, is expected.
        /// </summary>
        public void Send(GXSMSMessage message)
        {
            ((Gurux.Common.IGXMedia)this).Send(message, null);
        }

        /// <summary>
        /// Wait new message.
        /// </summary>
        /// <param name="waittime"></param>
        /// <returns></returns>
        public GXSMSMessage Receive(int waittime)
        {
            if (!m_SMSReceived.WaitOne(waittime * 1000))
            {
                return null;
            }
            return SyncMessage;
        }

        void SendMessage(string message, string receiver, MessageCodeType type)
        {
            if (string.IsNullOrEmpty(receiver))
            {
                throw new ArgumentException("Invalid receiver");
            }
            //Remove spaces.
            receiver = receiver.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Trim();
            //Send EOF
            SendBytes(new byte[] { 26 });
            //Code PDU.
            string data = GXSMSPdu.Code(receiver, message, type);
            long len = (data.Length / 2) - 1;
            string cmd;
            if (!SupportDirectSend)//Save SMS before send.
            {
                cmd = string.Format("AT+CMGW={0}\r", len);
            }
            else
            {
                cmd = string.Format("AT+CMGS={0}\r", len);
            }
            string reply = SendCommand(cmd, new string(new char[] { (char)0x20 }), false);
            if (reply != ">")
            {
                throw new Exception("Short message send failed.");
            }
            reply = SendCommand(data, "", false);
            //Send EOF
            reply = SendCommand(ASCIIEncoding.ASCII.GetString(new byte[] { 26 }), false);
            if (!SupportDirectSend)//Save SMS before send.
            {
                if (!reply.StartsWith("+CMGW:"))
                {
                    throw new Exception("Short message send failed.\r\n" + GetError(reply));
                }
            }
            else
            {
                if (!reply.StartsWith("+CMGS:"))
                {
                    throw new Exception("Short message send failed.\r\n" + GetError(reply));
                }
            }
        }

        /// <inheritdoc cref="IGXMedia.Receive"/>
        public bool Receive<T>(Gurux.Common.ReceiveParameters<T> args)
        {
            if (!IsOpen)
            {
                throw new InvalidOperationException("Media is closed.");
            }
            return m_syncBase.Receive(args);
        }


        void Gurux.Common.IGXMedia.Send(object data, string receiver)
        {
            if (data is GXSMSMessage)
            {
                GXSMSMessage msg = data as GXSMSMessage;
                lock (m_baseLock)
                {
                    if (m_Trace == TraceLevel.Verbose && m_OnTrace != null)
                    {
                        m_OnTrace(this, new TraceEventArgs(TraceTypes.Sent, msg.PhoneNumber + " : " + msg.Data, null));
                    }
                    //Reset last position if Eop is used.
                    lock (m_syncBase.receivedSync)
                    {
                        m_syncBase.lastPosition = 0;
                    }
                    //Use default phone number if new is not set.
                    string number = m_PhoneNumber;
                    if (!string.IsNullOrEmpty(msg.PhoneNumber))
                    {
                        number = msg.PhoneNumber;
                    }
                    if (string.IsNullOrEmpty(number))
                    {
                        throw new ArgumentException("Invalid phone number.");
                    }
                    SendMessage(msg.Data, number, msg.CodeType);
                }
            }
            else if (data is string)
            {

            }
            else
            {
                throw new ArgumentException("Invalid data to send");
            }
        }

        void Gurux.Common.IGXMedia.Validate()
        {

        }

        int Gurux.Common.IGXMedia.ConfigurableSettings
        {
            get;
            set;
        }

        void OnAsyncStateChange(object sender, GXAsyncWork work, object[] parameters, AsyncState state, string text)
        {
            try
            {
                if (state == AsyncState.Cancel)
                {
                    m_base.DiscardOutBuffer();
                    m_base.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                MessageBox.Show(ex.Message);
            }
        }

        void TestAsync(object sender, GXAsyncWork work, object[] parameters)
        {
            lock (m_baseLock)
            {
                m_base.Open();
                m_base.WriteTimeout = m_ConnectionWaitTime;
                m_base.DtrEnable = m_base.RtsEnable = true;
            }
            try
            {
                //Send AT
                lock (m_baseLock)
                {
                    if (string.Compare(SendCommand("AT\r", false), "OK", false) == 0)
                    {
                        work.Result = true;
                        return;
                    }
                }
            }
            //If connection is closed by user.
            catch (System.IO.IOException)
            {
                work.Result = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            finally
            {
                lock (m_baseLock)
                {
                    if (m_base.IsOpen)
                    {
                        m_base.Close();
                    }
                }
            }
            work.Result = false;
        }

        /// <summary>
        /// Test is modem available
        /// </summary>
        /// <returns></returns>
        public bool Test(Form parent)
        {
            try
            {
                GXSMSAsyncWorkForm dlg2 = new GXSMSAsyncWorkForm();
                GXSMSAsyncWorkForm dlg = new GXSMSAsyncWorkForm();
                dlg.Text = dlg.ConnectingLbl.Text = "Checking modem.";
                GXAsyncWork work = new GXAsyncWork(dlg, OnAsyncStateChange,
                    TestAsync, null, "Checking is modem available.", null);
                work.Start();
                work.Wait(m_ConnectionWaitTime);
                //If we try to check from the COM port where is no modem thread is locked.
                if (work.Result == null)
                {
                    return false;
                }
                return (bool)work.Result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                if (ConnectingForm != null)
                {
                    ConnectingForm.Close();
                    ConnectingForm = null;
                }
            }
        }

        /// <summary>
        /// Are messages removed after read from the SIM or phone memory.
        /// </summary>
        /// <remarks>
        /// Default is false.
        /// </remarks>
        public bool AutoDelete
        {
            get;
            set;
        }

        /// <summary>
        /// Used memory type. Default value is MemoryType.Sim.
        /// </summary>
        /// <remarks>
        /// Default is false.
        /// </remarks>
        public MemoryType Memory
        {
            get;
            set;
        }

        string DeleteMessage(int index)
        {
            string reply = SendCommand(string.Format("AT+CMGD={0}\r", index), false);
            return reply;
        }

        /// <summary>
        /// Delete SMS from the selected index.
        /// </summary>
        /// <returns></returns>
        public void Delete(int index)
        {
            string reply = DeleteMessage(index);
            if (reply != "OK")
            {
                throw new Exception(string.Format("Delete failed from index {0}.\r\n{1}", index, reply));
            }
        }

        /// <summary>
        /// Delete ALL SMS from the phone.
        /// </summary>
        /// <returns></returns>
        public void DeleteAll()
        {
            int count, maximum;
            GetMemoryCapacity(out count, out maximum);
            //If there are no messages to remove.
            if (count == 0)
            {
                return;
            }
            lock (m_baseLock)
            {
                for (int pos = 1; pos != maximum + 1; ++pos)
                {
                    string reply = DeleteMessage(pos);
                    if (reply != "OK")
                    {
                        throw new Exception(string.Format("Delete failed from index {0}.\r\n{1}", pos, reply));
                    }
                }
            }
        }

        /// <summary>
        /// Read all messages from the selected memory.
        /// </summary>
        /// <returns></returns>
        public GXSMSMessage[] Read()
        {
            int count, maximum;
            GetMemoryCapacity(out count, out maximum);
            //If there are no messages to read.
            if (count == 0)
            {
                return new GXSMSMessage[0];
            }
            List<GXSMSMessage> messages = new List<GXSMSMessage>();
            lock (m_baseLock)
            {
                for (int pos = 1; pos != maximum + 1; ++pos)
                {
                    string reply = SendCommand(string.Format("AT+CMGR={0}\r", pos), false);
                    if (reply.StartsWith("+CMGR:"))
                    {
                        reply = reply.Remove(0, 6);
                        string[] tmp = reply.Split(new char[] { ',' });
                        GXSMSMessage msg = new GXSMSMessage();
                        msg.Index = pos;
                        string status = tmp[0].Replace("\"", "").Trim();
                        if (status.StartsWith("STO"))
                        {
                            if (status.Contains("UNSENT"))
                            {
                                msg.Status = MessageStatus.Unsent;
                            }
                            else if (status.Contains("UNREAD"))
                            {
                                msg.Status = MessageStatus.Unread;
                            }
                            else if (status.Contains("Read"))
                            {
                                msg.Status = MessageStatus.Read;
                            }
                            else if (status.Contains("Sent"))
                            {
                                msg.Status = MessageStatus.Sent;
                            }
                        }
                        else
                        {
                            msg.Status = (MessageStatus)int.Parse(status);
                        }
                        //If this is not a empty message
                        if (tmp.Length != 1)
                        {
                            string[] m = tmp[2].Split(new string[] { "\r\n" }, StringSplitOptions.None);
                            if (m.Length != 2)
                            {
                                continue;
                            }
                            GXSMSPdu.Encode(m[1], msg);
                            //If this message is not read yet.
                            if (msg.Status == MessageStatus.Unread && msg.PhoneNumber == "")
                            {
                                continue;
                            }
                        }
                        messages.Add(msg);
                        //If all messages are read.
                        if (messages.Count == count)
                        {
                            break;
                        }
                    }
                }
            }
            return messages.ToArray();
        }

        /// <summary>
        /// Returns used and maximum SMS capacity.
        /// </summary>
        /// <param name="count">SMS in memory</param>
        /// <param name="maximum">Maximum SMS count.</param>
        /// <returns></returns>
        public void GetMemoryCapacity(out int count, out int maximum)
        {
            lock (m_baseLock)
            {
                string reply = SendCommand("AT+CPMS?\r", false);
                //for read/delete", "for write/send" , "for receive
                //"SM": SIM message store
                //"ME": ME message store
                //"MT": any of the storages associated with ME
                if (reply.StartsWith("ERROR:"))
                {
                    throw new Exception("ReadSMSCapacity failed.\r\n" + GetError(reply));
                }
                int ret = reply.LastIndexOf("\"SM\",");
                if (ret == -1)
                {
                    throw new Exception("ReadSMSCapacity failed.");
                }
                string[] results = reply.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (results.Length != 9)
                {
                    throw new Exception("ReadSMSCapacity failed.");
                }
                count = int.Parse(results[1]);
                maximum = int.Parse(results[2]);
            }
        }

        /// <summary>
        /// Returns network state.
        /// </summary>
        /// <returns></returns>
        public NetworkState GetNetworkState()
        {
            lock (m_baseLock)
            {
                string reply = SendCommand("AT+CREG=?\r", false);
                if (reply.StartsWith("ERROR"))
                {
                    return NetworkState.Denied;
                }
                reply = SendCommand("AT+CREG?\r", false);
                if (reply.StartsWith("ERROR:"))
                {
                    throw new Exception("GetNetworkState failed:\r\n" + GetError(reply));
                }

                int ret = reply.LastIndexOf("+CREG:");
                if (ret == -1)
                {
                    throw new Exception("GetNetworkState failed.");
                }
                string[] results = reply.Split(new char[] { ',', ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (results.Length != 3)
                {
                    throw new Exception("GetSignalQuality failed.");
                }
                return (NetworkState)int.Parse(results[2]);
            }
        }

        /// <summary>
        /// Returns received signal strength indication (RSSI) and channel bit error rate (BER).
        /// </summary>
        /// <param name="rssi">Received Signal Strength Indication</param>
        /// <param name="ber">Bit Error Rate</param>
        public void GetSignalQuality(out int rssi, out int ber)
        {
            rssi = ber = 0;
            lock (m_baseLock)
            {
                //If modem don't support this.
                string reply = SendCommand("AT+CSQ=?\r", false);
                if (reply.StartsWith("ERROR"))
                {
                    return;
                }
                reply = SendCommand("AT+CSQ\r", false);
                if (reply.Contains("ERROR:"))
                {
                    throw new Exception("GetSignalQuality failed:\r\n" + GetError(reply));
                }

                int ret = reply.LastIndexOf("CSQ:");
                if (ret == -1)
                {
                    throw new Exception("GetSignalQuality failed.");
                }
                string[] results = reply.Split(new char[] { ',', ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (results.Length != 3)
                {
                    throw new Exception("GetSignalQuality failed.");
                }
                rssi = int.Parse(results[1]);
                ber = int.Parse(results[2]);
            }
        }

        /// <summary>
        /// Returns battery charge level and avarage power consumption.
        /// </summary>
        /// <remarks>
        /// Battery capacity 0, 20, 40, 60, 80, 100 percent of remaining capacity (6 steps)
        /// 0 indicates that either the battery is exhausted or the capacity value is not available.
        /// Average power consumption i mA.
        /// </remarks>
        /// <param name="batteryCapacity"></param>
        /// <param name="averagePowerConsumption"></param>
        public void GetBatteryCharge(out int batteryCapacity, out int averagePowerConsumption)
        {
            batteryCapacity = averagePowerConsumption = 0;
            lock (m_baseLock)
            {
                //If modem don't support this.
                string reply = SendCommand("AT^SBC=?\r", false);
                if (reply.StartsWith("ERROR"))
                {
                    return;
                }
                reply = SendCommand("AT^SBC?\r", false);
                if (reply == "")
                {
                    throw new Exception("GetBatteryCharge failed.");
                }

                int ret = reply.LastIndexOf("SBC:");
                if (ret == -1)
                {
                    throw new Exception("GetBatteryCharge failed.");
                }
                string[] results = reply.Split(new char[] { ',', ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (results.Length != 4)
                {
                    throw new Exception("GetBatteryCharge failed.");
                }
                batteryCapacity = int.Parse(results[2]);
                averagePowerConsumption = int.Parse(results[3]);
            }
        }

        /// <summary>
        /// Return all available information from the modem.
        /// </summary>
        /// <returns></returns>
        public string[] GetInfo()
        {
            List<string> info = new List<string>();
            lock (m_baseLock)
            {
                //Get manufacturer.
                string reply = SendCommand("AT+CGMI\r", false);
                if (!reply.StartsWith("ERROR:"))
                {
                    info.Add("Manufacturer: " + reply);
                }
                else
                {
                    info.Add("Manufacturer: Unknown");
                }
                //Get Model.
                reply = SendCommand("AT+CGMM\r", false);
                if (!reply.StartsWith("ERROR:"))
                {
                    info.Add("Model: " + reply);
                }
                else
                {
                    info.Add("Model: Unknown");
                }
                //Get supported features.
                reply = SendCommand("AT+GCAP\r", false);
                if (!reply.StartsWith("ERROR:"))
                {
                    info.Add("Features: " + reply);
                }
                //Serial port speed.
                reply = SendCommand("AT+IPR?\r", false);
                int pos = reply.IndexOf("+IPR:");
                if (pos != -1)
                {
                    info.Add("Serial port speed: " + reply.Substring(pos + 5));
                }
            }
            return info.ToArray();
        }

        /// <summary>
        /// Amount of sent messages.
        /// </summary>
        public int MessagesSent
        {
            get;
            private set;
        }

        /// <summary>
        /// Amount of received messages.
        /// </summary>
        public int MessagesReceived
        {
            get;
            private set;
        }

        /// <summary>
        /// Resets sent and received message counters.
        /// </summary>
        public void ResetMessageCounters()
        {
            MessagesSent = MessagesReceived = 0;
        }

        /// <summary>
        /// Returns the amount of pending short messages.
        /// </summary>
        /// <returns></returns>
        public int GetPendingMessageCount()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Closes the connection.
        /// </summary>
        public void Dispose()
        {
            if (this.IsOpen)
            {
                Close();
            }
        }

        #endregion
    }
}
