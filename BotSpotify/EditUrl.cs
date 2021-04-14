using BotBitcointalk.Domain.DbContext;
using BotSpotify.Domain;
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
    public partial class EditUrl : Form
    {
        private long userId;

        public EditUrl(long userId)
        {
            InitializeComponent();

            this.userId = userId;
        }

        private void EditUrl_Load(object sender, EventArgs e)
        {

        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            try
            {
                if (!String.IsNullOrEmpty(richTextBoxUrls.Text))
                {
                    string[] splits = richTextBoxUrls.Text.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

                    using (BotContext context = new BotContext())
                    {
                        User user = context.Users.FirstOrDefault(x => x.Id == userId);

                        foreach (string split in splits)
                        {
                            UserUrl userUrl = user.UserUrls.FirstOrDefault(x => x.Value == split);
                            if (userUrl == null)
                            {
                                userUrl = new UserUrl();
                                userUrl.User = user;
                                userUrl.Value = split;

                                user.UserUrls.Add(userUrl);
                            }
                        }

                        context.SaveChanges();

                        Singleton.Main.RefreshUrls(user.UserUrls.ToList());

                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void toolStripMenuItemPaste_Click(object sender, EventArgs e)
        {
            try
            {
                string text = Clipboard.GetText();

                if (String.IsNullOrEmpty(text))
                    throw new Exception("В буфере обмена не содержится текста");

                richTextBoxUrls.Text = Clipboard.GetText();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
