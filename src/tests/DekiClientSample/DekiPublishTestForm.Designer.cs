/*
 * MindTouch DekiWiki - a commercial grade open source wiki
 * Copyright (C) 2006 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
 *  please review the licensing section.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 * http://www.gnu.org/copyleft/gpl.html
 */

namespace DekiClientSample {
    partial class DekiPublishTestForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.lblTitle = new System.Windows.Forms.Label();
            this.Title = new System.Windows.Forms.TextBox();
            this.lblText = new System.Windows.Forms.Label();
            this.PageText = new System.Windows.Forms.RichTextBox();
            this.lblPwd = new System.Windows.Forms.Label();
            this.Pwd = new System.Windows.Forms.TextBox();
            this.lblUser = new System.Windows.Forms.Label();
            this.User = new System.Windows.Forms.TextBox();
            this.lblLastEdit = new System.Windows.Forms.Label();
            this.LastEdit = new System.Windows.Forms.Label();
            this.Publish = new System.Windows.Forms.Button();
            this.lblFileName = new System.Windows.Forms.Label();
            this.lblFileDesc = new System.Windows.Forms.Label();
            this.FileName = new System.Windows.Forms.TextBox();
            this.FileDescription = new System.Windows.Forms.TextBox();
            this.Browse = new System.Windows.Forms.Button();
            this.Attach = new System.Windows.Forms.Button();
            this.Authorization = new System.Windows.Forms.Label();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new System.Drawing.Point(11, 67);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(27, 13);
            this.lblTitle.TabIndex = 5;
            this.lblTitle.Text = "Title";
            // 
            // Title
            // 
            this.Title.Location = new System.Drawing.Point(44, 64);
            this.Title.Name = "Title";
            this.Title.Size = new System.Drawing.Size(206, 20);
            this.Title.TabIndex = 6;
            this.Title.Leave += new System.EventHandler(this.Title_Leave);
            // 
            // lblText
            // 
            this.lblText.AutoSize = true;
            this.lblText.Location = new System.Drawing.Point(12, 94);
            this.lblText.Name = "lblText";
            this.lblText.Size = new System.Drawing.Size(28, 13);
            this.lblText.TabIndex = 9;
            this.lblText.Text = "Text";
            // 
            // PageText
            // 
            this.PageText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.PageText.Location = new System.Drawing.Point(44, 91);
            this.PageText.Name = "PageText";
            this.PageText.Size = new System.Drawing.Size(589, 183);
            this.PageText.TabIndex = 10;
            this.PageText.Text = "";
            // 
            // lblPwd
            // 
            this.lblPwd.AutoSize = true;
            this.lblPwd.Location = new System.Drawing.Point(11, 41);
            this.lblPwd.Name = "lblPwd";
            this.lblPwd.Size = new System.Drawing.Size(28, 13);
            this.lblPwd.TabIndex = 2;
            this.lblPwd.Text = "Pwd";
            // 
            // Pwd
            // 
            this.Pwd.Location = new System.Drawing.Point(44, 38);
            this.Pwd.Name = "Pwd";
            this.Pwd.PasswordChar = '*';
            this.Pwd.Size = new System.Drawing.Size(206, 20);
            this.Pwd.TabIndex = 3;
            this.Pwd.Leave += new System.EventHandler(this.UserPwd_Leave);
            // 
            // lblUser
            // 
            this.lblUser.AutoSize = true;
            this.lblUser.Location = new System.Drawing.Point(11, 15);
            this.lblUser.Name = "lblUser";
            this.lblUser.Size = new System.Drawing.Size(29, 13);
            this.lblUser.TabIndex = 0;
            this.lblUser.Text = "User";
            // 
            // User
            // 
            this.User.Location = new System.Drawing.Point(44, 12);
            this.User.Name = "User";
            this.User.Size = new System.Drawing.Size(206, 20);
            this.User.TabIndex = 1;
            this.User.Leave += new System.EventHandler(this.UserPwd_Leave);
            // 
            // lblLastEdit
            // 
            this.lblLastEdit.AutoSize = true;
            this.lblLastEdit.Location = new System.Drawing.Point(257, 67);
            this.lblLastEdit.Name = "lblLastEdit";
            this.lblLastEdit.Size = new System.Drawing.Size(48, 13);
            this.lblLastEdit.TabIndex = 7;
            this.lblLastEdit.Text = "Last Edit";
            // 
            // LastEdit
            // 
            this.LastEdit.AutoSize = true;
            this.LastEdit.Location = new System.Drawing.Point(311, 67);
            this.LastEdit.Name = "LastEdit";
            this.LastEdit.Size = new System.Drawing.Size(31, 13);
            this.LastEdit.TabIndex = 8;
            this.LastEdit.Text = "none";
            // 
            // Publish
            // 
            this.Publish.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Publish.Location = new System.Drawing.Point(44, 281);
            this.Publish.Name = "Publish";
            this.Publish.Size = new System.Drawing.Size(75, 23);
            this.Publish.TabIndex = 11;
            this.Publish.Text = "Publish";
            this.Publish.UseVisualStyleBackColor = true;
            this.Publish.Click += new System.EventHandler(this.Publish_Click);
            // 
            // lblFileName
            // 
            this.lblFileName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblFileName.AutoSize = true;
            this.lblFileName.Location = new System.Drawing.Point(11, 340);
            this.lblFileName.Name = "lblFileName";
            this.lblFileName.Size = new System.Drawing.Size(23, 13);
            this.lblFileName.TabIndex = 14;
            this.lblFileName.Text = "File";
            // 
            // lblFileDesc
            // 
            this.lblFileDesc.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblFileDesc.AutoSize = true;
            this.lblFileDesc.Location = new System.Drawing.Point(11, 314);
            this.lblFileDesc.Name = "lblFileDesc";
            this.lblFileDesc.Size = new System.Drawing.Size(32, 13);
            this.lblFileDesc.TabIndex = 12;
            this.lblFileDesc.Text = "Desc";
            // 
            // FileName
            // 
            this.FileName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.FileName.Location = new System.Drawing.Point(44, 337);
            this.FileName.Name = "FileName";
            this.FileName.Size = new System.Drawing.Size(475, 20);
            this.FileName.TabIndex = 15;
            // 
            // FileDescription
            // 
            this.FileDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.FileDescription.Location = new System.Drawing.Point(44, 311);
            this.FileDescription.Name = "FileDescription";
            this.FileDescription.Size = new System.Drawing.Size(475, 20);
            this.FileDescription.TabIndex = 13;
            // 
            // Browse
            // 
            this.Browse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Browse.Location = new System.Drawing.Point(525, 335);
            this.Browse.Name = "Browse";
            this.Browse.Size = new System.Drawing.Size(27, 23);
            this.Browse.TabIndex = 16;
            this.Browse.Text = "...";
            this.Browse.UseVisualStyleBackColor = true;
            this.Browse.Click += new System.EventHandler(this.Browse_Click);
            // 
            // Attach
            // 
            this.Attach.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Attach.Location = new System.Drawing.Point(558, 335);
            this.Attach.Name = "Attach";
            this.Attach.Size = new System.Drawing.Size(75, 23);
            this.Attach.TabIndex = 17;
            this.Attach.Text = "Attach";
            this.Attach.UseVisualStyleBackColor = true;
            this.Attach.Click += new System.EventHandler(this.Attach_Click);
            // 
            // Authorization
            // 
            this.Authorization.AutoSize = true;
            this.Authorization.Location = new System.Drawing.Point(260, 41);
            this.Authorization.Name = "Authorization";
            this.Authorization.Size = new System.Drawing.Size(68, 13);
            this.Authorization.TabIndex = 4;
            this.Authorization.Text = "unauthorized";
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog1_FileOk);
            // 
            // DekiPublishTestForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(647, 369);
            this.Controls.Add(this.Authorization);
            this.Controls.Add(this.Browse);
            this.Controls.Add(this.Attach);
            this.Controls.Add(this.Publish);
            this.Controls.Add(this.LastEdit);
            this.Controls.Add(this.lblLastEdit);
            this.Controls.Add(this.PageText);
            this.Controls.Add(this.User);
            this.Controls.Add(this.FileDescription);
            this.Controls.Add(this.Pwd);
            this.Controls.Add(this.lblUser);
            this.Controls.Add(this.FileName);
            this.Controls.Add(this.lblFileDesc);
            this.Controls.Add(this.Title);
            this.Controls.Add(this.lblPwd);
            this.Controls.Add(this.lblFileName);
            this.Controls.Add(this.lblText);
            this.Controls.Add(this.lblTitle);
            this.Name = "DekiPublishTestForm";
            this.Text = "Deki Topic Publish Test";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.TextBox Title;
        private System.Windows.Forms.Label lblText;
        private System.Windows.Forms.RichTextBox PageText;
        private System.Windows.Forms.Label lblPwd;
        private System.Windows.Forms.TextBox Pwd;
        private System.Windows.Forms.Label lblUser;
        private System.Windows.Forms.TextBox User;
        private System.Windows.Forms.Label lblLastEdit;
        private System.Windows.Forms.Label LastEdit;
        private System.Windows.Forms.Button Publish;
        private System.Windows.Forms.Label lblFileName;
        private System.Windows.Forms.Label lblFileDesc;
        private System.Windows.Forms.TextBox FileName;
        private System.Windows.Forms.TextBox FileDescription;
        private System.Windows.Forms.Button Browse;
        private System.Windows.Forms.Button Attach;
        private System.Windows.Forms.Label Authorization;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
    }
}

