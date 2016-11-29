namespace Gurux.SMS
{
partial class Settings
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.NumberPanel = new System.Windows.Forms.Panel();
        this.NumberTB = new System.Windows.Forms.TextBox();
        this.NumberLbl = new System.Windows.Forms.Label();
        this.PinPanel = new System.Windows.Forms.Panel();
        this.PinTB = new System.Windows.Forms.TextBox();
        this.PinLbl = new System.Windows.Forms.Label();
        this.panel1 = new System.Windows.Forms.Panel();
        this.IntervalTX = new System.Windows.Forms.TextBox();
        this.IntervalLbl = new System.Windows.Forms.Label();
        this.StopBitsPanel = new System.Windows.Forms.Panel();
        this.StopBitsLbl = new System.Windows.Forms.Label();
        this.StopBitsCB = new System.Windows.Forms.ComboBox();
        this.ParityPanel = new System.Windows.Forms.Panel();
        this.ParityLbl = new System.Windows.Forms.Label();
        this.ParityCB = new System.Windows.Forms.ComboBox();
        this.DataBitsPanel = new System.Windows.Forms.Panel();
        this.DataBitsCB = new System.Windows.Forms.ComboBox();
        this.DataBitsLbl = new System.Windows.Forms.Label();
        this.BaudRatePanel = new System.Windows.Forms.Panel();
        this.BaudRateCB = new System.Windows.Forms.ComboBox();
        this.BaudRateLbl = new System.Windows.Forms.Label();
        this.PortNamePanel = new System.Windows.Forms.Panel();
        this.TestBtn = new System.Windows.Forms.Button();
        this.PortNameCB = new System.Windows.Forms.ComboBox();
        this.PortNameLbl = new System.Windows.Forms.Label();
        this.NumberPanel.SuspendLayout();
        this.PinPanel.SuspendLayout();
        this.panel1.SuspendLayout();
        this.StopBitsPanel.SuspendLayout();
        this.ParityPanel.SuspendLayout();
        this.DataBitsPanel.SuspendLayout();
        this.BaudRatePanel.SuspendLayout();
        this.PortNamePanel.SuspendLayout();
        this.SuspendLayout();
        //
        // NumberPanel
        //
        this.NumberPanel.Controls.Add(this.NumberTB);
        this.NumberPanel.Controls.Add(this.NumberLbl);
        this.NumberPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.NumberPanel.Location = new System.Drawing.Point(0, 0);
        this.NumberPanel.Name = "NumberPanel";
        this.NumberPanel.Size = new System.Drawing.Size(284, 30);
        this.NumberPanel.TabIndex = 41;
        //
        // NumberTB
        //
        this.NumberTB.Location = new System.Drawing.Point(95, 5);
        this.NumberTB.Name = "NumberTB";
        this.NumberTB.Size = new System.Drawing.Size(177, 20);
        this.NumberTB.TabIndex = 14;
        this.NumberTB.TextChanged += new System.EventHandler(this.NumberTB_TextChanged);
        //
        // NumberLbl
        //
        this.NumberLbl.AutoSize = true;
        this.NumberLbl.Location = new System.Drawing.Point(8, 8);
        this.NumberLbl.Name = "NumberLbl";
        this.NumberLbl.Size = new System.Drawing.Size(81, 13);
        this.NumberLbl.TabIndex = 12;
        this.NumberLbl.Text = "Phone Number:";
        //
        // PinPanel
        //
        this.PinPanel.Controls.Add(this.PinTB);
        this.PinPanel.Controls.Add(this.PinLbl);
        this.PinPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.PinPanel.Location = new System.Drawing.Point(0, 30);
        this.PinPanel.Name = "PinPanel";
        this.PinPanel.Size = new System.Drawing.Size(284, 30);
        this.PinPanel.TabIndex = 42;
        //
        // PinTB
        //
        this.PinTB.Location = new System.Drawing.Point(95, 5);
        this.PinTB.Name = "PinTB";
        this.PinTB.Size = new System.Drawing.Size(177, 20);
        this.PinTB.TabIndex = 14;
        this.PinTB.TextChanged += new System.EventHandler(this.PinTB_TextChanged);
        //
        // PinLbl
        //
        this.PinLbl.AutoSize = true;
        this.PinLbl.Location = new System.Drawing.Point(5, 8);
        this.PinLbl.Name = "PinLbl";
        this.PinLbl.Size = new System.Drawing.Size(62, 13);
        this.PinLbl.TabIndex = 12;
        this.PinLbl.Text = "PIN codeX:";
        //
        // panel1
        //
        this.panel1.Controls.Add(this.IntervalTX);
        this.panel1.Controls.Add(this.IntervalLbl);
        this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
        this.panel1.Location = new System.Drawing.Point(0, 60);
        this.panel1.Name = "panel1";
        this.panel1.Size = new System.Drawing.Size(284, 30);
        this.panel1.TabIndex = 43;
        //
        // IntervalTX
        //
        this.IntervalTX.Location = new System.Drawing.Point(95, 5);
        this.IntervalTX.Name = "IntervalTX";
        this.IntervalTX.Size = new System.Drawing.Size(177, 20);
        this.IntervalTX.TabIndex = 14;
        this.IntervalTX.TextChanged += new System.EventHandler(this.IntervalTX_TextChanged);
        //
        // IntervalLbl
        //
        this.IntervalLbl.AutoSize = true;
        this.IntervalLbl.Location = new System.Drawing.Point(5, 8);
        this.IntervalLbl.Name = "IntervalLbl";
        this.IntervalLbl.Size = new System.Drawing.Size(83, 13);
        this.IntervalLbl.TabIndex = 12;
        this.IntervalLbl.Text = "Check IntervalX";
        //
        // StopBitsPanel
        //
        this.StopBitsPanel.Controls.Add(this.StopBitsLbl);
        this.StopBitsPanel.Controls.Add(this.StopBitsCB);
        this.StopBitsPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.StopBitsPanel.Location = new System.Drawing.Point(0, 210);
        this.StopBitsPanel.Name = "StopBitsPanel";
        this.StopBitsPanel.Size = new System.Drawing.Size(284, 30);
        this.StopBitsPanel.TabIndex = 48;
        //
        // StopBitsLbl
        //
        this.StopBitsLbl.AutoSize = true;
        this.StopBitsLbl.Location = new System.Drawing.Point(5, 7);
        this.StopBitsLbl.Name = "StopBitsLbl";
        this.StopBitsLbl.Size = new System.Drawing.Size(56, 13);
        this.StopBitsLbl.TabIndex = 15;
        this.StopBitsLbl.Text = "Stop BitsX";
        //
        // StopBitsCB
        //
        this.StopBitsCB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                                  | System.Windows.Forms.AnchorStyles.Right)));
        this.StopBitsCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.StopBitsCB.FormattingEnabled = true;
        this.StopBitsCB.Location = new System.Drawing.Point(95, 4);
        this.StopBitsCB.Name = "StopBitsCB";
        this.StopBitsCB.Size = new System.Drawing.Size(177, 21);
        this.StopBitsCB.TabIndex = 14;
        this.StopBitsCB.SelectedIndexChanged += new System.EventHandler(this.StopBitsCB_SelectedIndexChanged);
        //
        // ParityPanel
        //
        this.ParityPanel.Controls.Add(this.ParityLbl);
        this.ParityPanel.Controls.Add(this.ParityCB);
        this.ParityPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.ParityPanel.Location = new System.Drawing.Point(0, 180);
        this.ParityPanel.Name = "ParityPanel";
        this.ParityPanel.Size = new System.Drawing.Size(284, 30);
        this.ParityPanel.TabIndex = 47;
        //
        // ParityLbl
        //
        this.ParityLbl.AutoSize = true;
        this.ParityLbl.Location = new System.Drawing.Point(5, 7);
        this.ParityLbl.Name = "ParityLbl";
        this.ParityLbl.Size = new System.Drawing.Size(40, 13);
        this.ParityLbl.TabIndex = 15;
        this.ParityLbl.Text = "ParityX";
        //
        // ParityCB
        //
        this.ParityCB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                                | System.Windows.Forms.AnchorStyles.Right)));
        this.ParityCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.ParityCB.FormattingEnabled = true;
        this.ParityCB.Location = new System.Drawing.Point(95, 4);
        this.ParityCB.Name = "ParityCB";
        this.ParityCB.Size = new System.Drawing.Size(177, 21);
        this.ParityCB.TabIndex = 14;
        this.ParityCB.SelectedIndexChanged += new System.EventHandler(this.ParityCB_SelectedIndexChanged);
        //
        // DataBitsPanel
        //
        this.DataBitsPanel.Controls.Add(this.DataBitsCB);
        this.DataBitsPanel.Controls.Add(this.DataBitsLbl);
        this.DataBitsPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.DataBitsPanel.Location = new System.Drawing.Point(0, 150);
        this.DataBitsPanel.Name = "DataBitsPanel";
        this.DataBitsPanel.Size = new System.Drawing.Size(284, 30);
        this.DataBitsPanel.TabIndex = 46;
        //
        // DataBitsCB
        //
        this.DataBitsCB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                                  | System.Windows.Forms.AnchorStyles.Right)));
        this.DataBitsCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.DataBitsCB.FormattingEnabled = true;
        this.DataBitsCB.Location = new System.Drawing.Point(95, 4);
        this.DataBitsCB.Name = "DataBitsCB";
        this.DataBitsCB.Size = new System.Drawing.Size(177, 21);
        this.DataBitsCB.TabIndex = 15;
        this.DataBitsCB.SelectedIndexChanged += new System.EventHandler(this.DataBitsCB_SelectedIndexChanged);
        //
        // DataBitsLbl
        //
        this.DataBitsLbl.AutoSize = true;
        this.DataBitsLbl.Location = new System.Drawing.Point(5, 7);
        this.DataBitsLbl.Name = "DataBitsLbl";
        this.DataBitsLbl.Size = new System.Drawing.Size(57, 13);
        this.DataBitsLbl.TabIndex = 12;
        this.DataBitsLbl.Text = "Data BitsX";
        //
        // BaudRatePanel
        //
        this.BaudRatePanel.Controls.Add(this.BaudRateCB);
        this.BaudRatePanel.Controls.Add(this.BaudRateLbl);
        this.BaudRatePanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.BaudRatePanel.Location = new System.Drawing.Point(0, 120);
        this.BaudRatePanel.Name = "BaudRatePanel";
        this.BaudRatePanel.Size = new System.Drawing.Size(284, 30);
        this.BaudRatePanel.TabIndex = 45;
        //
        // BaudRateCB
        //
        this.BaudRateCB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                                  | System.Windows.Forms.AnchorStyles.Right)));
        this.BaudRateCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.BaudRateCB.FormattingEnabled = true;
        this.BaudRateCB.Location = new System.Drawing.Point(95, 5);
        this.BaudRateCB.Name = "BaudRateCB";
        this.BaudRateCB.Size = new System.Drawing.Size(177, 21);
        this.BaudRateCB.TabIndex = 16;
        this.BaudRateCB.SelectedIndexChanged += new System.EventHandler(this.BaudRateCB_SelectedIndexChanged);
        //
        // BaudRateLbl
        //
        this.BaudRateLbl.AutoSize = true;
        this.BaudRateLbl.Location = new System.Drawing.Point(5, 7);
        this.BaudRateLbl.Name = "BaudRateLbl";
        this.BaudRateLbl.Size = new System.Drawing.Size(65, 13);
        this.BaudRateLbl.TabIndex = 10;
        this.BaudRateLbl.Text = "Baud RateX";
        //
        // PortNamePanel
        //
        this.PortNamePanel.Controls.Add(this.TestBtn);
        this.PortNamePanel.Controls.Add(this.PortNameCB);
        this.PortNamePanel.Controls.Add(this.PortNameLbl);
        this.PortNamePanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.PortNamePanel.Location = new System.Drawing.Point(0, 90);
        this.PortNamePanel.Name = "PortNamePanel";
        this.PortNamePanel.Size = new System.Drawing.Size(284, 30);
        this.PortNamePanel.TabIndex = 44;
        //
        // TestBtn
        //
        this.TestBtn.BackColor = System.Drawing.SystemColors.Control;
        this.TestBtn.Cursor = System.Windows.Forms.Cursors.Default;
        this.TestBtn.ForeColor = System.Drawing.SystemColors.ControlText;
        this.TestBtn.Location = new System.Drawing.Point(202, 5);
        this.TestBtn.Name = "TestBtn";
        this.TestBtn.RightToLeft = System.Windows.Forms.RightToLeft.No;
        this.TestBtn.Size = new System.Drawing.Size(73, 25);
        this.TestBtn.TabIndex = 16;
        this.TestBtn.Text = "Test";
        this.TestBtn.UseVisualStyleBackColor = false;
        this.TestBtn.Click += new System.EventHandler(this.TestBtn_Click);
        //
        // PortNameCB
        //
        this.PortNameCB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                                  | System.Windows.Forms.AnchorStyles.Right)));
        this.PortNameCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.PortNameCB.FormattingEnabled = true;
        this.PortNameCB.Location = new System.Drawing.Point(95, 5);
        this.PortNameCB.Name = "PortNameCB";
        this.PortNameCB.Size = new System.Drawing.Size(98, 21);
        this.PortNameCB.TabIndex = 15;
        this.PortNameCB.SelectedIndexChanged += new System.EventHandler(this.PortNameCB_SelectedIndexChanged_1);
        //
        // PortNameLbl
        //
        this.PortNameLbl.AutoSize = true;
        this.PortNameLbl.Location = new System.Drawing.Point(5, 8);
        this.PortNameLbl.Name = "PortNameLbl";
        this.PortNameLbl.Size = new System.Drawing.Size(26, 13);
        this.PortNameLbl.TabIndex = 12;
        this.PortNameLbl.Text = "Port";
        //
        // Settings
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(284, 250);
        this.Controls.Add(this.StopBitsPanel);
        this.Controls.Add(this.ParityPanel);
        this.Controls.Add(this.DataBitsPanel);
        this.Controls.Add(this.BaudRatePanel);
        this.Controls.Add(this.PortNamePanel);
        this.Controls.Add(this.panel1);
        this.Controls.Add(this.PinPanel);
        this.Controls.Add(this.NumberPanel);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "Settings";
        this.ShowIcon = false;
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Terminal Settings";
        this.NumberPanel.ResumeLayout(false);
        this.NumberPanel.PerformLayout();
        this.PinPanel.ResumeLayout(false);
        this.PinPanel.PerformLayout();
        this.panel1.ResumeLayout(false);
        this.panel1.PerformLayout();
        this.StopBitsPanel.ResumeLayout(false);
        this.StopBitsPanel.PerformLayout();
        this.ParityPanel.ResumeLayout(false);
        this.ParityPanel.PerformLayout();
        this.DataBitsPanel.ResumeLayout(false);
        this.DataBitsPanel.PerformLayout();
        this.BaudRatePanel.ResumeLayout(false);
        this.BaudRatePanel.PerformLayout();
        this.PortNamePanel.ResumeLayout(false);
        this.PortNamePanel.PerformLayout();
        this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Panel NumberPanel;
    private System.Windows.Forms.TextBox NumberTB;
    private System.Windows.Forms.Label NumberLbl;
    private System.Windows.Forms.Panel PinPanel;
    private System.Windows.Forms.TextBox PinTB;
    private System.Windows.Forms.Label PinLbl;
    private System.Windows.Forms.Panel panel1;
    private System.Windows.Forms.TextBox IntervalTX;
    private System.Windows.Forms.Label IntervalLbl;
    private System.Windows.Forms.Panel StopBitsPanel;
    private System.Windows.Forms.Label StopBitsLbl;
    private System.Windows.Forms.ComboBox StopBitsCB;
    private System.Windows.Forms.Panel ParityPanel;
    private System.Windows.Forms.Label ParityLbl;
    private System.Windows.Forms.ComboBox ParityCB;
    private System.Windows.Forms.Panel DataBitsPanel;
    private System.Windows.Forms.ComboBox DataBitsCB;
    private System.Windows.Forms.Label DataBitsLbl;
    private System.Windows.Forms.Panel BaudRatePanel;
    private System.Windows.Forms.ComboBox BaudRateCB;
    private System.Windows.Forms.Label BaudRateLbl;
    private System.Windows.Forms.Panel PortNamePanel;
    public System.Windows.Forms.Button TestBtn;
    private System.Windows.Forms.ComboBox PortNameCB;
    private System.Windows.Forms.Label PortNameLbl;


}
}