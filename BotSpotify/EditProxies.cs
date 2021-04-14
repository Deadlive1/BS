using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BotSpotify
{
    public partial class EditProxies : Form
    {
        public EditProxies()
        {
            InitializeComponent();
        }

        private void EditProxies_Load(object sender, EventArgs e)
        {
            richTextBoxProxies.Text = Properties.Settings.Default.Proxies;
        }

        private void richTextBoxProxies_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Proxies = richTextBoxProxies.Text;
            Properties.Settings.Default.Save();
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
