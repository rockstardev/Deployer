using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ClickOnceApp
{
    public partial class ClickOnceForm : Form
    {
        public ClickOnceForm()
        {
            InitializeComponent();
        }

        private void ClickOnceForm_Load(object sender, EventArgs e)
        {
            MessageBox.Show(Properties.Settings.Default.Setting);
            this.Close();
        }
    }
}