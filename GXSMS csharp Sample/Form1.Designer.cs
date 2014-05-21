//
// --------------------------------------------------------------------------
//  Gurux Ltd
// 
//
//
// Filename:        $HeadURL: svn://utopia/projects/Old/GuruxSMS/GXSMS%20csharp%20Sample/Form1.Designer.cs $
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

using System.Diagnostics;
using System;
using System.Windows.Forms;
using System.Collections;

namespace GXSMSSample
{
	partial class Form1
	{
		[STAThread]
		static void Main()
		{
			System.Windows.Forms.Application.Run(new Form1());
		}
		#region "Windows Form Designer generated code "
		[System.Diagnostics.DebuggerNonUserCode()]public Form1()
		{
			//This call is required by the Windows Form Designer.
			InitializeComponent();
		}
		//Form overrides dispose to clean up the component list.
		[System.Diagnostics.DebuggerNonUserCode()]protected override void Dispose(bool Disposing)
		{
			if (Disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(Disposing);
		}
        //Required by the Windows Form Designer
		public System.Windows.Forms.Timer MsgCounterTimer;
		public System.Windows.Forms.TextBox ReceivedTB;
		public System.Windows.Forms.TextBox SentTB;
		public System.Windows.Forms.Label Label8;
		public System.Windows.Forms.Label Label7;
		public System.Windows.Forms.GroupBox Frame3;
		public System.Windows.Forms.Timer StatusTimer;
		public System.Windows.Forms.TextBox PowerConsumptionTB;
		public System.Windows.Forms.TextBox BatteryCapacityTB;
		public System.Windows.Forms.TextBox NetworkStatusTB;
		public System.Windows.Forms.TextBox BERTB;
        public System.Windows.Forms.TextBox RSSITB;
		public System.Windows.Forms.Label Line3;
		public System.Windows.Forms.Label Label6;
		public System.Windows.Forms.Label Label5;
		public System.Windows.Forms.Label Line2;
		public System.Windows.Forms.Label Label4;
		public System.Windows.Forms.Label Label3;
		public System.Windows.Forms.Label Label2;
		public System.Windows.Forms.GroupBox Frame2;
		public System.Windows.Forms.Button ClearBtn;
		public System.Windows.Forms.Button DeleteBtn;
		public System.Windows.Forms.Button ReadBtn;
		public System.Windows.Forms.ColumnHeader _MsgList_ColumnHeader_1;
		public System.Windows.Forms.ColumnHeader _MsgList_ColumnHeader_2;
		public System.Windows.Forms.ColumnHeader _MsgList_ColumnHeader_3;
		public System.Windows.Forms.ListView MsgList;
		public System.Windows.Forms.TextBox SendText;
		public System.Windows.Forms.Label _Label1_1;
		public System.Windows.Forms.Label _Label1_0;
		public System.Windows.Forms.GroupBox Frame1;
		public System.Windows.Forms.Button OpenBtn;
		public System.Windows.Forms.Button CloseBtn;
		public System.Windows.Forms.Button SendBtn;
        public System.Windows.Forms.Button PropertiesBtn;
		//NOTE: The following procedure is required by the Windows Form Designer
		//It can be modified using the Windows Form Designer.
		//Do not modify it using the code editor.
		[System.Diagnostics.DebuggerStepThrough()]private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.Frame3 = new System.Windows.Forms.GroupBox();
            this.ReceivedTB = new System.Windows.Forms.TextBox();
            this.SentTB = new System.Windows.Forms.TextBox();
            this.Label8 = new System.Windows.Forms.Label();
            this.Label7 = new System.Windows.Forms.Label();
            this.MsgCounterTimer = new System.Windows.Forms.Timer(this.components);
            this.Frame2 = new System.Windows.Forms.GroupBox();
            this.PowerConsumptionTB = new System.Windows.Forms.TextBox();
            this.BatteryCapacityTB = new System.Windows.Forms.TextBox();
            this.NetworkStatusTB = new System.Windows.Forms.TextBox();
            this.BERTB = new System.Windows.Forms.TextBox();
            this.RSSITB = new System.Windows.Forms.TextBox();
            this.Line3 = new System.Windows.Forms.Label();
            this.Label6 = new System.Windows.Forms.Label();
            this.Label5 = new System.Windows.Forms.Label();
            this.Line2 = new System.Windows.Forms.Label();
            this.Label4 = new System.Windows.Forms.Label();
            this.Label3 = new System.Windows.Forms.Label();
            this.Label2 = new System.Windows.Forms.Label();
            this.StatusTimer = new System.Windows.Forms.Timer(this.components);
            this.ClearBtn = new System.Windows.Forms.Button();
            this.DeleteBtn = new System.Windows.Forms.Button();
            this.ReadBtn = new System.Windows.Forms.Button();
            this.Frame1 = new System.Windows.Forms.GroupBox();
            this.MsgList = new System.Windows.Forms.ListView();
            this._MsgList_ColumnHeader_1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this._MsgList_ColumnHeader_2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this._MsgList_ColumnHeader_3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SendText = new System.Windows.Forms.TextBox();
            this._Label1_1 = new System.Windows.Forms.Label();
            this._Label1_0 = new System.Windows.Forms.Label();
            this.OpenBtn = new System.Windows.Forms.Button();
            this.CloseBtn = new System.Windows.Forms.Button();
            this.SendBtn = new System.Windows.Forms.Button();
            this.PropertiesBtn = new System.Windows.Forms.Button();
            this.InfoBtn = new System.Windows.Forms.Button();
            this.Frame3.SuspendLayout();
            this.Frame2.SuspendLayout();
            this.Frame1.SuspendLayout();
            this.SuspendLayout();
            // 
            // Frame3
            // 
            this.Frame3.BackColor = System.Drawing.SystemColors.Control;
            this.Frame3.Controls.Add(this.ReceivedTB);
            this.Frame3.Controls.Add(this.SentTB);
            this.Frame3.Controls.Add(this.Label8);
            this.Frame3.Controls.Add(this.Label7);
            this.Frame3.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Frame3.Location = new System.Drawing.Point(0, 248);
            this.Frame3.Name = "Frame3";
            this.Frame3.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Frame3.Size = new System.Drawing.Size(329, 49);
            this.Frame3.TabIndex = 23;
            this.Frame3.TabStop = false;
            this.Frame3.Text = "Message statistics in last minute";
            // 
            // ReceivedTB
            // 
            this.ReceivedTB.AcceptsReturn = true;
            this.ReceivedTB.BackColor = System.Drawing.SystemColors.Window;
            this.ReceivedTB.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.ReceivedTB.Enabled = false;
            this.ReceivedTB.ForeColor = System.Drawing.SystemColors.WindowText;
            this.ReceivedTB.Location = new System.Drawing.Point(186, 16);
            this.ReceivedTB.MaxLength = 0;
            this.ReceivedTB.Name = "ReceivedTB";
            this.ReceivedTB.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.ReceivedTB.Size = new System.Drawing.Size(105, 20);
            this.ReceivedTB.TabIndex = 25;
            this.ReceivedTB.Text = "0";
            // 
            // SentTB
            // 
            this.SentTB.AcceptsReturn = true;
            this.SentTB.BackColor = System.Drawing.SystemColors.Window;
            this.SentTB.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.SentTB.Enabled = false;
            this.SentTB.ForeColor = System.Drawing.SystemColors.WindowText;
            this.SentTB.Location = new System.Drawing.Point(56, 16);
            this.SentTB.MaxLength = 0;
            this.SentTB.Name = "SentTB";
            this.SentTB.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.SentTB.Size = new System.Drawing.Size(57, 20);
            this.SentTB.TabIndex = 24;
            this.SentTB.Text = "0";
            // 
            // Label8
            // 
            this.Label8.AutoSize = true;
            this.Label8.BackColor = System.Drawing.SystemColors.Control;
            this.Label8.Cursor = System.Windows.Forms.Cursors.Default;
            this.Label8.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Label8.Location = new System.Drawing.Point(122, 16);
            this.Label8.Name = "Label8";
            this.Label8.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Label8.Size = new System.Drawing.Size(56, 13);
            this.Label8.TabIndex = 27;
            this.Label8.Text = "Received:";
            // 
            // Label7
            // 
            this.Label7.AutoSize = true;
            this.Label7.BackColor = System.Drawing.SystemColors.Control;
            this.Label7.Cursor = System.Windows.Forms.Cursors.Default;
            this.Label7.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Label7.Location = new System.Drawing.Point(8, 16);
            this.Label7.Name = "Label7";
            this.Label7.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Label7.Size = new System.Drawing.Size(32, 13);
            this.Label7.TabIndex = 26;
            this.Label7.Text = "Sent:";
            // 
            // MsgCounterTimer
            // 
            this.MsgCounterTimer.Interval = 60000;
            this.MsgCounterTimer.Tick += new System.EventHandler(this.MsgCounterTimer_Tick);
            // 
            // Frame2
            // 
            this.Frame2.BackColor = System.Drawing.SystemColors.Control;
            this.Frame2.Controls.Add(this.PowerConsumptionTB);
            this.Frame2.Controls.Add(this.BatteryCapacityTB);
            this.Frame2.Controls.Add(this.NetworkStatusTB);
            this.Frame2.Controls.Add(this.BERTB);
            this.Frame2.Controls.Add(this.RSSITB);
            this.Frame2.Controls.Add(this.Line3);
            this.Frame2.Controls.Add(this.Label6);
            this.Frame2.Controls.Add(this.Label5);
            this.Frame2.Controls.Add(this.Line2);
            this.Frame2.Controls.Add(this.Label4);
            this.Frame2.Controls.Add(this.Label3);
            this.Frame2.Controls.Add(this.Label2);
            this.Frame2.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Frame2.Location = new System.Drawing.Point(0, 296);
            this.Frame2.Name = "Frame2";
            this.Frame2.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Frame2.Size = new System.Drawing.Size(329, 113);
            this.Frame2.TabIndex = 12;
            this.Frame2.TabStop = false;
            this.Frame2.Text = "Modem status";
            // 
            // PowerConsumptionTB
            // 
            this.PowerConsumptionTB.AcceptsReturn = true;
            this.PowerConsumptionTB.BackColor = System.Drawing.SystemColors.Window;
            this.PowerConsumptionTB.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.PowerConsumptionTB.Enabled = false;
            this.PowerConsumptionTB.ForeColor = System.Drawing.SystemColors.WindowText;
            this.PowerConsumptionTB.Location = new System.Drawing.Point(120, 80);
            this.PowerConsumptionTB.MaxLength = 0;
            this.PowerConsumptionTB.Name = "PowerConsumptionTB";
            this.PowerConsumptionTB.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.PowerConsumptionTB.Size = new System.Drawing.Size(89, 20);
            this.PowerConsumptionTB.TabIndex = 20;
            // 
            // BatteryCapacityTB
            // 
            this.BatteryCapacityTB.AcceptsReturn = true;
            this.BatteryCapacityTB.BackColor = System.Drawing.SystemColors.Window;
            this.BatteryCapacityTB.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.BatteryCapacityTB.Enabled = false;
            this.BatteryCapacityTB.ForeColor = System.Drawing.SystemColors.WindowText;
            this.BatteryCapacityTB.Location = new System.Drawing.Point(120, 32);
            this.BatteryCapacityTB.MaxLength = 0;
            this.BatteryCapacityTB.Name = "BatteryCapacityTB";
            this.BatteryCapacityTB.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.BatteryCapacityTB.Size = new System.Drawing.Size(89, 20);
            this.BatteryCapacityTB.TabIndex = 19;
            // 
            // NetworkStatusTB
            // 
            this.NetworkStatusTB.AcceptsReturn = true;
            this.NetworkStatusTB.BackColor = System.Drawing.SystemColors.Window;
            this.NetworkStatusTB.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.NetworkStatusTB.Enabled = false;
            this.NetworkStatusTB.ForeColor = System.Drawing.SystemColors.WindowText;
            this.NetworkStatusTB.Location = new System.Drawing.Point(232, 32);
            this.NetworkStatusTB.MaxLength = 0;
            this.NetworkStatusTB.Name = "NetworkStatusTB";
            this.NetworkStatusTB.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.NetworkStatusTB.Size = new System.Drawing.Size(89, 20);
            this.NetworkStatusTB.TabIndex = 18;
            // 
            // BERTB
            // 
            this.BERTB.AcceptsReturn = true;
            this.BERTB.BackColor = System.Drawing.SystemColors.Window;
            this.BERTB.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.BERTB.Enabled = false;
            this.BERTB.ForeColor = System.Drawing.SystemColors.WindowText;
            this.BERTB.Location = new System.Drawing.Point(8, 80);
            this.BERTB.MaxLength = 0;
            this.BERTB.Name = "BERTB";
            this.BERTB.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.BERTB.Size = new System.Drawing.Size(89, 20);
            this.BERTB.TabIndex = 16;
            // 
            // RSSITB
            // 
            this.RSSITB.AcceptsReturn = true;
            this.RSSITB.BackColor = System.Drawing.SystemColors.Window;
            this.RSSITB.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.RSSITB.Enabled = false;
            this.RSSITB.ForeColor = System.Drawing.SystemColors.WindowText;
            this.RSSITB.Location = new System.Drawing.Point(8, 32);
            this.RSSITB.MaxLength = 0;
            this.RSSITB.Name = "RSSITB";
            this.RSSITB.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.RSSITB.Size = new System.Drawing.Size(89, 20);
            this.RSSITB.TabIndex = 15;
            // 
            // Line3
            // 
            this.Line3.BackColor = System.Drawing.SystemColors.WindowText;
            this.Line3.Location = new System.Drawing.Point(224, 16);
            this.Line3.Name = "Line3";
            this.Line3.Size = new System.Drawing.Size(1, 88);
            this.Line3.TabIndex = 29;
            // 
            // Label6
            // 
            this.Label6.BackColor = System.Drawing.SystemColors.Control;
            this.Label6.Cursor = System.Windows.Forms.Cursors.Default;
            this.Label6.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Label6.Location = new System.Drawing.Point(120, 64);
            this.Label6.Name = "Label6";
            this.Label6.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Label6.Size = new System.Drawing.Size(97, 17);
            this.Label6.TabIndex = 22;
            this.Label6.Text = "Power Consumption:";
            // 
            // Label5
            // 
            this.Label5.BackColor = System.Drawing.SystemColors.Control;
            this.Label5.Cursor = System.Windows.Forms.Cursors.Default;
            this.Label5.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Label5.Location = new System.Drawing.Point(120, 16);
            this.Label5.Name = "Label5";
            this.Label5.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Label5.Size = new System.Drawing.Size(81, 17);
            this.Label5.TabIndex = 21;
            this.Label5.Text = "Battery Capacity:";
            // 
            // Line2
            // 
            this.Line2.BackColor = System.Drawing.SystemColors.WindowText;
            this.Line2.Location = new System.Drawing.Point(112, 16);
            this.Line2.Name = "Line2";
            this.Line2.Size = new System.Drawing.Size(1, 88);
            this.Line2.TabIndex = 30;
            // 
            // Label4
            // 
            this.Label4.BackColor = System.Drawing.SystemColors.Control;
            this.Label4.Cursor = System.Windows.Forms.Cursors.Default;
            this.Label4.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Label4.Location = new System.Drawing.Point(232, 16);
            this.Label4.Name = "Label4";
            this.Label4.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Label4.Size = new System.Drawing.Size(81, 17);
            this.Label4.TabIndex = 17;
            this.Label4.Text = "Network Status:";
            // 
            // Label3
            // 
            this.Label3.BackColor = System.Drawing.SystemColors.Control;
            this.Label3.Cursor = System.Windows.Forms.Cursors.Default;
            this.Label3.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Label3.Location = new System.Drawing.Point(8, 64);
            this.Label3.Name = "Label3";
            this.Label3.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Label3.Size = new System.Drawing.Size(25, 17);
            this.Label3.TabIndex = 14;
            this.Label3.Text = "BER:";
            // 
            // Label2
            // 
            this.Label2.BackColor = System.Drawing.SystemColors.Control;
            this.Label2.Cursor = System.Windows.Forms.Cursors.Default;
            this.Label2.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Label2.Location = new System.Drawing.Point(8, 16);
            this.Label2.Name = "Label2";
            this.Label2.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Label2.Size = new System.Drawing.Size(33, 17);
            this.Label2.TabIndex = 13;
            this.Label2.Text = "RSSI:";
            // 
            // StatusTimer
            // 
            this.StatusTimer.Interval = 30000;
            this.StatusTimer.Tick += new System.EventHandler(this.StatusTimer_Tick);
            // 
            // ClearBtn
            // 
            this.ClearBtn.BackColor = System.Drawing.SystemColors.Control;
            this.ClearBtn.Cursor = System.Windows.Forms.Cursors.Default;
            this.ClearBtn.ForeColor = System.Drawing.SystemColors.ControlText;
            this.ClearBtn.Location = new System.Drawing.Point(344, 208);
            this.ClearBtn.Name = "ClearBtn";
            this.ClearBtn.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.ClearBtn.Size = new System.Drawing.Size(73, 25);
            this.ClearBtn.TabIndex = 11;
            this.ClearBtn.Text = "Clear";
            this.ClearBtn.UseVisualStyleBackColor = false;
            this.ClearBtn.Click += new System.EventHandler(this.ClearBtn_Click);
            // 
            // DeleteBtn
            // 
            this.DeleteBtn.BackColor = System.Drawing.SystemColors.Control;
            this.DeleteBtn.Cursor = System.Windows.Forms.Cursors.Default;
            this.DeleteBtn.ForeColor = System.Drawing.SystemColors.ControlText;
            this.DeleteBtn.Location = new System.Drawing.Point(344, 168);
            this.DeleteBtn.Name = "DeleteBtn";
            this.DeleteBtn.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.DeleteBtn.Size = new System.Drawing.Size(73, 25);
            this.DeleteBtn.TabIndex = 9;
            this.DeleteBtn.Text = "Delete";
            this.DeleteBtn.UseVisualStyleBackColor = false;
            this.DeleteBtn.Click += new System.EventHandler(this.DeleteBtn_Click);
            // 
            // ReadBtn
            // 
            this.ReadBtn.BackColor = System.Drawing.SystemColors.Control;
            this.ReadBtn.Cursor = System.Windows.Forms.Cursors.Default;
            this.ReadBtn.ForeColor = System.Drawing.SystemColors.ControlText;
            this.ReadBtn.Location = new System.Drawing.Point(344, 136);
            this.ReadBtn.Name = "ReadBtn";
            this.ReadBtn.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.ReadBtn.Size = new System.Drawing.Size(73, 25);
            this.ReadBtn.TabIndex = 8;
            this.ReadBtn.Text = "Read";
            this.ReadBtn.UseVisualStyleBackColor = false;
            this.ReadBtn.Click += new System.EventHandler(this.ReadBtn_Click);
            // 
            // Frame1
            // 
            this.Frame1.BackColor = System.Drawing.SystemColors.Control;
            this.Frame1.Controls.Add(this.MsgList);
            this.Frame1.Controls.Add(this.SendText);
            this.Frame1.Controls.Add(this._Label1_1);
            this.Frame1.Controls.Add(this._Label1_0);
            this.Frame1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Frame1.Location = new System.Drawing.Point(0, 0);
            this.Frame1.Name = "Frame1";
            this.Frame1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Frame1.Size = new System.Drawing.Size(337, 241);
            this.Frame1.TabIndex = 6;
            this.Frame1.TabStop = false;
            // 
            // MsgList
            // 
            this.MsgList.BackColor = System.Drawing.SystemColors.Window;
            this.MsgList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this._MsgList_ColumnHeader_1,
            this._MsgList_ColumnHeader_2,
            this._MsgList_ColumnHeader_3});
            this.MsgList.ForeColor = System.Drawing.SystemColors.WindowText;
            this.MsgList.LabelEdit = true;
            this.MsgList.Location = new System.Drawing.Point(8, 88);
            this.MsgList.Name = "MsgList";
            this.MsgList.Size = new System.Drawing.Size(321, 145);
            this.MsgList.TabIndex = 10;
            this.MsgList.UseCompatibleStateImageBehavior = false;
            this.MsgList.View = System.Windows.Forms.View.Details;
            // 
            // _MsgList_ColumnHeader_1
            // 
            this._MsgList_ColumnHeader_1.Text = "Text";
            this._MsgList_ColumnHeader_1.Width = 170;
            // 
            // _MsgList_ColumnHeader_2
            // 
            this._MsgList_ColumnHeader_2.Text = "Sender";
            this._MsgList_ColumnHeader_2.Width = 170;
            // 
            // _MsgList_ColumnHeader_3
            // 
            this._MsgList_ColumnHeader_3.Text = "Time";
            this._MsgList_ColumnHeader_3.Width = 218;
            // 
            // SendText
            // 
            this.SendText.AcceptsReturn = true;
            this.SendText.BackColor = System.Drawing.SystemColors.Window;
            this.SendText.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.SendText.ForeColor = System.Drawing.SystemColors.WindowText;
            this.SendText.Location = new System.Drawing.Point(8, 32);
            this.SendText.MaxLength = 0;
            this.SendText.Multiline = true;
            this.SendText.Name = "SendText";
            this.SendText.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.SendText.Size = new System.Drawing.Size(321, 33);
            this.SendText.TabIndex = 4;
            // 
            // _Label1_1
            // 
            this._Label1_1.BackColor = System.Drawing.SystemColors.Control;
            this._Label1_1.Cursor = System.Windows.Forms.Cursors.Default;
            this._Label1_1.ForeColor = System.Drawing.SystemColors.ControlText;
            this._Label1_1.Location = new System.Drawing.Point(8, 16);
            this._Label1_1.Name = "_Label1_1";
            this._Label1_1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._Label1_1.Size = new System.Drawing.Size(89, 17);
            this._Label1_1.TabIndex = 0;
            this._Label1_1.Text = "Send:";
            // 
            // _Label1_0
            // 
            this._Label1_0.BackColor = System.Drawing.SystemColors.Control;
            this._Label1_0.Cursor = System.Windows.Forms.Cursors.Default;
            this._Label1_0.ForeColor = System.Drawing.SystemColors.ControlText;
            this._Label1_0.Location = new System.Drawing.Point(8, 72);
            this._Label1_0.Name = "_Label1_0";
            this._Label1_0.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._Label1_0.Size = new System.Drawing.Size(89, 17);
            this._Label1_0.TabIndex = 7;
            this._Label1_0.Text = "Received:";
            // 
            // OpenBtn
            // 
            this.OpenBtn.BackColor = System.Drawing.SystemColors.Control;
            this.OpenBtn.Cursor = System.Windows.Forms.Cursors.Default;
            this.OpenBtn.ForeColor = System.Drawing.SystemColors.ControlText;
            this.OpenBtn.Location = new System.Drawing.Point(344, 8);
            this.OpenBtn.Name = "OpenBtn";
            this.OpenBtn.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.OpenBtn.Size = new System.Drawing.Size(73, 25);
            this.OpenBtn.TabIndex = 1;
            this.OpenBtn.Text = "Open";
            this.OpenBtn.UseVisualStyleBackColor = false;
            this.OpenBtn.Click += new System.EventHandler(this.OpenBtn_Click);
            // 
            // CloseBtn
            // 
            this.CloseBtn.BackColor = System.Drawing.SystemColors.Control;
            this.CloseBtn.Cursor = System.Windows.Forms.Cursors.Default;
            this.CloseBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CloseBtn.Enabled = false;
            this.CloseBtn.ForeColor = System.Drawing.SystemColors.ControlText;
            this.CloseBtn.Location = new System.Drawing.Point(344, 40);
            this.CloseBtn.Name = "CloseBtn";
            this.CloseBtn.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.CloseBtn.Size = new System.Drawing.Size(73, 25);
            this.CloseBtn.TabIndex = 2;
            this.CloseBtn.Text = "Close";
            this.CloseBtn.UseVisualStyleBackColor = false;
            this.CloseBtn.Click += new System.EventHandler(this.CloseBtn_Click);
            // 
            // SendBtn
            // 
            this.SendBtn.BackColor = System.Drawing.SystemColors.Control;
            this.SendBtn.Cursor = System.Windows.Forms.Cursors.Default;
            this.SendBtn.ForeColor = System.Drawing.SystemColors.ControlText;
            this.SendBtn.Location = new System.Drawing.Point(344, 104);
            this.SendBtn.Name = "SendBtn";
            this.SendBtn.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.SendBtn.Size = new System.Drawing.Size(73, 25);
            this.SendBtn.TabIndex = 5;
            this.SendBtn.Text = "Send";
            this.SendBtn.UseVisualStyleBackColor = false;
            this.SendBtn.Click += new System.EventHandler(this.SendBtn_Click);
            // 
            // PropertiesBtn
            // 
            this.PropertiesBtn.BackColor = System.Drawing.SystemColors.Control;
            this.PropertiesBtn.Cursor = System.Windows.Forms.Cursors.Default;
            this.PropertiesBtn.ForeColor = System.Drawing.SystemColors.ControlText;
            this.PropertiesBtn.Location = new System.Drawing.Point(344, 72);
            this.PropertiesBtn.Name = "PropertiesBtn";
            this.PropertiesBtn.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.PropertiesBtn.Size = new System.Drawing.Size(73, 25);
            this.PropertiesBtn.TabIndex = 3;
            this.PropertiesBtn.Text = "Properties";
            this.PropertiesBtn.UseVisualStyleBackColor = false;
            this.PropertiesBtn.Click += new System.EventHandler(this.PropertiesBtn_Click);
            // 
            // InfoBtn
            // 
            this.InfoBtn.BackColor = System.Drawing.SystemColors.Control;
            this.InfoBtn.Cursor = System.Windows.Forms.Cursors.Default;
            this.InfoBtn.ForeColor = System.Drawing.SystemColors.ControlText;
            this.InfoBtn.Location = new System.Drawing.Point(344, 248);
            this.InfoBtn.Name = "InfoBtn";
            this.InfoBtn.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.InfoBtn.Size = new System.Drawing.Size(73, 25);
            this.InfoBtn.TabIndex = 24;
            this.InfoBtn.Text = "Info...";
            this.InfoBtn.UseVisualStyleBackColor = false;
            this.InfoBtn.Click += new System.EventHandler(this.InfoBtn_Click);
            // 
            // Form1
            // 
            this.AcceptButton = this.OpenBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.CancelButton = this.CloseBtn;
            this.ClientSize = new System.Drawing.Size(426, 413);
            this.Controls.Add(this.InfoBtn);
            this.Controls.Add(this.Frame3);
            this.Controls.Add(this.Frame2);
            this.Controls.Add(this.ClearBtn);
            this.Controls.Add(this.DeleteBtn);
            this.Controls.Add(this.ReadBtn);
            this.Controls.Add(this.Frame1);
            this.Controls.Add(this.OpenBtn);
            this.Controls.Add(this.CloseBtn);
            this.Controls.Add(this.SendBtn);
            this.Controls.Add(this.PropertiesBtn);
            this.Cursor = System.Windows.Forms.Cursors.Default;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Location = new System.Drawing.Point(270, 221);
            this.Name = "Form1";
            this.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "GXSMS Sample";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Frame3.ResumeLayout(false);
            this.Frame3.PerformLayout();
            this.Frame2.ResumeLayout(false);
            this.Frame2.PerformLayout();
            this.Frame1.ResumeLayout(false);
            this.Frame1.PerformLayout();
            this.ResumeLayout(false);

		}
		#endregion

        private System.ComponentModel.IContainer components;
        private Button InfoBtn;
   }
}
