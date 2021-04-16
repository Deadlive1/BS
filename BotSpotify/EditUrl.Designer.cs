using System;

namespace BotSpotify
{
    partial class EditUrl
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditUrl));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.richTextBoxUrls = new System.Windows.Forms.RichTextBox();
            this.buttonOk = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.contextMenuStripUrl = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItemPaste = new System.Windows.Forms.ToolStripMenuItem();
            this.groupBox1.SuspendLayout();
            this.contextMenuStripUrl.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.richTextBoxUrls);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox1.Location = new System.Drawing.Point(6, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(420, 417);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            // 
            // richTextBoxUrls
            // 
            this.richTextBoxUrls.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBoxUrls.ContextMenuStrip = this.contextMenuStripUrl;
            this.richTextBoxUrls.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxUrls.Location = new System.Drawing.Point(3, 16);
            this.richTextBoxUrls.Name = "richTextBoxUrls";
            this.richTextBoxUrls.Size = new System.Drawing.Size(414, 398);
            this.richTextBoxUrls.TabIndex = 0;
            this.richTextBoxUrls.Text = "";
            // 
            // buttonOk
            // 
            this.buttonOk.Image = global::BotSpotify.Properties.Resources.confirm;
            this.buttonOk.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buttonOk.Location = new System.Drawing.Point(217, 426);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(93, 24);
            this.buttonOk.TabIndex = 2;
            this.buttonOk.Text = "Сохранить";
            this.buttonOk.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.buttonOk.UseVisualStyleBackColor = true;
            this.buttonOk.Click += new System.EventHandler(this.buttonOk_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Image = global::BotSpotify.Properties.Resources.cancel;
            this.buttonCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buttonCancel.Location = new System.Drawing.Point(142, 426);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(69, 24);
            this.buttonCancel.TabIndex = 3;
            this.buttonCancel.Text = "Отмена";
            this.buttonCancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(GetButtonCancel_Click());
            // 
            // contextMenuStripUrl
            // 
            this.contextMenuStripUrl.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItemPaste});
            this.contextMenuStripUrl.Name = "contextMenuStripUrl";
            this.contextMenuStripUrl.Size = new System.Drawing.Size(181, 48);
            // 
            // toolStripMenuItemPaste
            // 
            this.toolStripMenuItemPaste.Name = "toolStripMenuItemPaste";
            this.toolStripMenuItemPaste.Size = new System.Drawing.Size(180, 22);
            this.toolStripMenuItemPaste.Text = "Вставить";
            this.toolStripMenuItemPaste.Click += new System.EventHandler(this.toolStripMenuItemPaste_Click);
            // 
            // EditUrl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(432, 453);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOk);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "EditUrl";
            this.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Добавление ссылок";
            this.Load += new System.EventHandler(this.EditUrl_Load);
            this.groupBox1.ResumeLayout(false);
            this.contextMenuStripUrl.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private EventHandler GetButtonCancel_Click()
        {
            return this.buttonCancel_Click;
        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RichTextBox richTextBoxUrls;
        private System.Windows.Forms.Button buttonOk;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripUrl;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemPaste;
    }
}