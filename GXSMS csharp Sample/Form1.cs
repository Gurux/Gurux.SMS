//
// --------------------------------------------------------------------------
//  Gurux Ltd
// 
//
//
// Filename:        $HeadURL: svn://utopia/projects/Old/GuruxSMS/GXSMS%20csharp%20Sample/Form1.cs $
//
// Version:         $Revision: 3104 $,
//                  $Date: 2010-12-03 13:43:16 +0200 (pe, 03 joulu 2010) $
//                  $Author: kurumi $
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
// More information of Gurux Short Message (SMS) : http://www.gurux.org/GXSMS
//
// This code is licensed under the GNU General Public License v2. 
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using System;
using System.Windows.Forms;
using Gurux.SMS;
using Gurux.Common;

namespace GXSMSSample
{
	internal partial class Form1 : System.Windows.Forms.Form
	{
        Gurux.SMS.GXSMS gxsms1 = new Gurux.SMS.GXSMS();
        /// <summary>
        /// Clear message list, but don't remove messages from the modem.
        /// </summary>
        /// <param name="eventSender"></param>
        /// <param name="eventArgs"></param>
		private void ClearBtn_Click(System.Object eventSender, System.EventArgs eventArgs)
		{
			try
			{
				MsgList.Items.Clear();
			}
            catch (Exception Ex)
            {
                MessageBox.Show(this, Ex.Message);
            }
        }

        #region Close
        /// <summary>
        /// Closes SMS connection to the modem.
		/// </summary>
		/// <param name="eventSender"></param>
		/// <param name="eventArgs"></param>
		private void CloseBtn_Click(System.Object eventSender, System.EventArgs eventArgs)
		{
			try
			{
				gxsms1.Close();
			}
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }
        }
        #endregion //Close

        /// <summary>
        /// Delete all SMS messages from the modem.
		/// </summary>
		/// <param name="eventSender"></param>
		/// <param name="eventArgs"></param>
		private void DeleteBtn_Click(System.Object eventSender, System.EventArgs eventArgs)
		{
			try
			{
                if (MessageBox.Show("Delete all SMS Messages?", "Delete", MessageBoxButtons.YesNo) == DialogResult.Yes) 
				{
					gxsms1.DeleteAll();
					MessageBox.Show("All items are deleted.");
				}
			}
            catch (Exception Ex)
            {
                MessageBox.Show(this, Ex.Message);
            }
		}
		
		/// <summary>
		/// Update UI before page is shown.
		/// </summary>
		/// <param name="eventSender"></param>
		/// <param name="eventArgs"></param>
		private void Form1_Load(System.Object eventSender, System.EventArgs eventArgs)
		{
			try
			{
                gxsms1.Settings = GXSMSSample.Properties.Settings.Default.MediaSetting;
                gxsms1.OnReceived += new Gurux.Common.ReceivedEventHandler(gxsms1_OnReceived);
                gxsms1.OnError += new Gurux.Common.ErrorEventHandler(gxsms1_OnError);
                gxsms1.OnMediaStateChange += new Gurux.Common.MediaStateChangeEventHandler(gxsms1_OnMediaStateChange);
				//Initialize UI controls.
                gxsms1_OnMediaStateChange(gxsms1, new MediaStateEventArgs(Gurux.Common.MediaState.Closed));
			}
            catch (Exception Ex)
            {
                MessageBox.Show(this, Ex.Message);
            }
        }

        #region OnError
        /// <summary>
        /// Show occured error.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ex"></param>
        private void gxsms1_OnError(object sender, Exception ex)
        {
            try
            {
                gxsms1.Close();
                MessageBox.Show(this, ex.Message);
            }
            catch (Exception Ex)
            {
                MessageBox.Show(this, Ex.Message);
            }
        }
        #endregion //OnError

        #region OnMediaStateChange
        /// <summary>
		/// Update UI when media state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void gxsms1_OnMediaStateChange(object sender, MediaStateEventArgs e)
        {
			try
			{
                bool IsOpen = e.State == Gurux.Common.MediaState.Open;
				OpenBtn.Enabled = ! IsOpen;
				SendBtn.Enabled = IsOpen;
				CloseBtn.Enabled = IsOpen;
				SendText.Enabled = IsOpen;
				StatusTimer.Enabled = IsOpen;
				MsgCounterTimer.Enabled = IsOpen;
				ReadBtn.Enabled = IsOpen;
				DeleteBtn.Enabled = IsOpen;
                InfoBtn.Enabled = IsOpen;
				//read network status if media is opened.
				if (IsOpen)
				{
					StatusTimer_Tick(StatusTimer, new System.EventArgs());
				}
				else
				{
					RSSITB.Text = "";
					BERTB.Text = "";
					BatteryCapacityTB.Text = "";
					PowerConsumptionTB.Text = "";
					NetworkStatusTB.Text = "";
				}
			}
            catch (Exception Ex)
            {
                MessageBox.Show(this, Ex.Message);
            }
        }
        #endregion //OnMediaStateChange

        #region OnReceived
        /// <summary>
        /// Show received SMS message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>        
        private void gxsms1_OnReceived(object sender, ReceiveEventArgs e)
        {
			try
			{
                GXSMSMessage msg = e.Data as GXSMSMessage;
                ListViewItem it = MsgList.Items.Add(msg.Data);
                it.SubItems.Add(msg.PhoneNumber);
                it.SubItems.Add(msg.Time.ToString());
			}
            catch (Exception Ex)
            {
                MessageBox.Show(this, Ex.Message);
            }
        }
        #endregion //OnReceived

        /// <summary>
        /// Update message counters once per minute.
        /// </summary>
        /// <param name="eventSender"></param>
        /// <param name="eventArgs"></param>
		private void MsgCounterTimer_Tick(System.Object eventSender, System.EventArgs eventArgs)
		{
            try
            {
                ReceivedTB.Text = gxsms1.MessagesReceived.ToString();
                SentTB.Text = gxsms1.MessagesSent.ToString();
                gxsms1.ResetMessageCounters();
            }
            catch (Exception Ex)
            {
                MessageBox.Show(this, Ex.Message);
            }
        }

        #region Open
        /// <summary>
        /// Open SMS connection.
		/// </summary>
		private void OpenBtn_Click(System.Object eventSender, System.EventArgs eventArgs)
		{
			try
			{
				gxsms1.Open();
			}
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }
        }
        #endregion //Open

        #region Properties
        /// <summary>
        /// Show GXSMS media properties.
		/// </summary>
		private void PropertiesBtn_Click(System.Object eventSender, System.EventArgs eventArgs)
		{
            try
            {
                if (gxsms1.Properties(this))
                {
                    //Save settings.
                    GXSMSSample.Properties.Settings.Default.MediaSetting = gxsms1.Settings;
                    GXSMSSample.Properties.Settings.Default.Save();
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }			
        }
        #endregion //Properties

        /// <summary>
        /// Read all received messages from the modem.
        /// </summary>
        /// <param name="eventSender"></param>
        /// <param name="eventArgs"></param>
		private void ReadBtn_Click(System.Object eventSender, System.EventArgs eventArgs)
		{
			try
			{
				Gurux.SMS.GXSMSMessage[] messages = gxsms1.Read();			
                foreach (Gurux.SMS.GXSMSMessage it in messages)
                {
                    gxsms1_OnReceived(gxsms1, new ReceiveEventArgs(it, null));
                }               
			}
            catch (Exception Ex)
            {
                MessageBox.Show(this, Ex.Message);
            }
        }

        #region Send
        /// <summary>
        /// Send SMS message.
		/// </summary>
		/// <param name="eventSender"></param>
		/// <param name="eventArgs"></param>
		private void SendBtn_Click(System.Object eventSender, System.EventArgs eventArgs)
		{
			try
			{                
                Gurux.SMS.GXSMSMessage msg = new Gurux.SMS.GXSMSMessage();                
				msg.Data = SendText.Text;
				gxsms1.Send(msg);
                MessageBox.Show(this, "SMS sent.");
			}
            catch (Exception Ex)
            {
                MessageBox.Show(this, Ex.Message);
            }
        }
        #endregion //Send

        /// <summary>
        /// Show network status. This might take lot of time so this is updated every 30 second.
		/// </summary>
		/// <param name="eventSender"></param>
		/// <param name="eventArgs"></param>
		private void StatusTimer_Tick(System.Object eventSender, System.EventArgs eventArgs)
		{
			try
			{
				int RSSI = 0, BER = 0, BatteryCapacity = 0, AveragePowerConsumption = 0;
				gxsms1.GetSignalQuality(out RSSI, out BER);
				gxsms1.GetBatteryCharge(out BatteryCapacity, out AveragePowerConsumption);
				RSSITB.Text = Convert.ToString(RSSI);
				BERTB.Text = Convert.ToString(BER);
				BatteryCapacityTB.Text = Convert.ToString(BatteryCapacity);
				PowerConsumptionTB.Text = Convert.ToString(AveragePowerConsumption);
                switch (gxsms1.GetNetworkState())
                {
                    case NetworkState.NotRegistered:
                        NetworkStatusTB.Text = "Not Registered";
                    break;
                    case NetworkState.Home:
                        NetworkStatusTB.Text = "Home";
                    break;
                    case NetworkState.Searching:
                        NetworkStatusTB.Text = "Searching";
                    break;
                    case NetworkState.Denied:
                        NetworkStatusTB.Text = "Denied";
                    break;
                    case NetworkState.Unknown:
                        NetworkStatusTB.Text = "Unknown";
                    break;
                    case NetworkState.Roaming:
                        NetworkStatusTB.Text = "Roaming";
                    break;
                }
			}
			catch(Exception Ex)
			{
				MessageBox.Show(this, Ex.Message);
                RSSITB.Text = "";
                BERTB.Text = "";
                BatteryCapacityTB.Text = "";
                PowerConsumptionTB.Text = "";
                NetworkStatusTB.Text = "";
			}
		}

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            gxsms1.Close();
        }

        private void InfoBtn_Click(object sender, EventArgs e)
        {
            try
            {
                MessageBox.Show(string.Join("\r\n", gxsms1.GetInfo()));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }
	}
}
