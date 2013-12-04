using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Gurux.Common;

namespace Gurux.SMS
{
    partial class GXSMSAsyncWorkForm : Form
    {        
        public GXSMSAsyncWorkForm()
        {
            InitializeComponent();
            Bitmap bm = Gurux.SMS.Properties.Resources.gxsmsLB;
            bm.MakeTransparent(Color.Magenta);
            panel1.BackgroundImage = bm;
        }

        private void CancelBtn_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
