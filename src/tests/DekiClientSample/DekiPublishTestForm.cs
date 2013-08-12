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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Web;
using System.Xml;
using System.IO;

namespace DekiClientSample {
    public partial class DekiPublishTestForm : Form {
        string dreamBase, wikiApiBase;
        bool authorized;
        DateTime lastEdit;

        public DekiPublishTestForm() {
            InitializeComponent();
            dreamBase = "http://localhost:8081/";
            wikiApiBase = dreamBase + "wiki-api/";
            Authorize(false);
            Attach.Enabled = false;
        }

        CredentialCache GetCredentials() {
            CredentialCache mycache = new CredentialCache();
            mycache.Add(new Uri(wikiApiBase), "Basic", new NetworkCredential(User.Text, Pwd.Text));
            return mycache;
        }
        WebClient GetWebClient() {
            WebClient wc = new WebClient();
            wc.BaseAddress = wikiApiBase;
            wc.Credentials = GetCredentials();
            return wc;
        }

        private void UserPwd_Leave(object sender, EventArgs e) {
            if (User.Text == string.Empty || Pwd.Text == string.Empty) {
                Authorize(false);
                return;
            }
            WebClient wc = GetWebClient();
            try {
                string users = wc.DownloadString("users/");
                Authorize(true);
            } catch (WebException ex) {
                Authorize(false);
                if (((HttpWebResponse)ex.Response).StatusCode != HttpStatusCode.Unauthorized)
                    Authorization.Text += ", " + ex.Message;
            }
        }

        void Authorize(bool authorize) {
            Authorization.Text = authorize ? "authorized" : "unauthorized";
            Publish.Enabled = authorize;
            authorized = authorize;
        }

        private void Title_Leave(object sender, EventArgs e) {
            if (authorized) {
                WebClient wc = GetWebClient();
                try {
                    string data = wc.DownloadString("nav/" + Title.Text + "?column=modified");
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(data);
                    lastEdit = DateTime.Parse(doc.SelectNodes("nav/page/modified")[0].InnerText);
                    this.LastEdit.Text = lastEdit.ToString("r");
                    PageText.Text = wc.DownloadString("page/" + Title.Text);
                    Attach.Enabled = true;
                } catch (WebException ex) {
                    if (((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.NotFound)
                        this.LastEdit.Text = "new topic";
                    else
                        this.LastEdit.Text = ex.Message;
                    lastEdit = DateTime.MinValue;
                    PageText.Text = string.Empty;
                    Attach.Enabled = false;
                }
            }
        }

        private void Publish_Click(object sender, EventArgs e) {
            WebClient wc = GetWebClient();
            string url = "page/" + Title.Text;
            if (lastEdit != DateTime.MinValue)
                url += "?edittime=" + HttpUtility.UrlEncode(lastEdit.ToUniversalTime().ToString("u"));
            wc.UploadString(url, PageText.Text);
        }

        internal class WikiFileInfo {
            byte[] fileFormHeaderBytes;
            byte[] descFormHeaderBytes;
            byte[] descBytes;
            string fileName;
            int fileLength;

            internal WikiFileInfo(string boundary, int fileID, string fileName, string fileDesc) {
                this.fileName = fileName;
                this.fileLength = (int)new FileInfo(fileName).Length;
                string fileFormHeader = "--" + boundary + "\r\n" +
                    "Content-Disposition: form-data; name=\"file_" + fileID + "\"; filename=\"" + Path.GetFileName(fileName) + "\"\r\n" +
                    "Content-Type: application/octet-stream\r\n\r\n";
                this.fileFormHeaderBytes = Encoding.UTF8.GetBytes(fileFormHeader);
                string descFormHeader = "\r\n--" + boundary + "\r\n" +
                    "Content-Disposition: form-data; name=\"filedesc_" + fileID + "\"\r\n\r\n";
                this.descFormHeaderBytes = Encoding.UTF8.GetBytes(descFormHeader);
                this.descBytes = Encoding.UTF8.GetBytes(fileDesc);
            }

            internal int ContentLength {
                get {
                    return fileFormHeaderBytes.Length + fileLength + descFormHeaderBytes.Length + descBytes.Length;
                }
            }

            internal void Write(Stream requestStream) {
                requestStream.Write(fileFormHeaderBytes, 0, fileFormHeaderBytes.Length);
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read)) {
                    byte[] buffer = new byte[Math.Min(0x2000, fs.Length)];
                    for (; ; ) {
                        int read = fs.Read(buffer, 0, buffer.Length);
                        if (read == 0)
                            break;
                        requestStream.Write(buffer, 0, read);
                    }
                }
                requestStream.Write(descFormHeaderBytes, 0, descFormHeaderBytes.Length);
                requestStream.Write(descBytes, 0, descBytes.Length);
            }
        }

        private WikiFileInfo AddFile(WebRequest wr, string boundary, int fileID, string fileName, string fileDesc) {
            WikiFileInfo info = new WikiFileInfo(boundary, fileID, fileName, fileDesc);
            wr.ContentLength += info.ContentLength;
            return info;
        }

        private void Attach_Click(object sender, EventArgs e) {
            WebRequest wr = WebRequest.Create(wikiApiBase + "file/" + Title.Text);
            wr.Credentials = GetCredentials();
            wr.ContentLength = 0;
            wr.Method = "POST";
            string boundary = "---------------------" + DateTime.Now.Ticks.ToString("x");
            wr.ContentType = "multipart/form-data; boundary=" + boundary;
            IList<WikiFileInfo> files = new List<WikiFileInfo>();
            files.Add(AddFile(wr, boundary, 1, FileName.Text, FileDescription.Text));
            byte[] endBoundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            wr.ContentLength += endBoundaryBytes.Length;
            using (Stream stream = wr.GetRequestStream()) {
                foreach (WikiFileInfo file in files)
                    file.Write(stream);
                stream.Write(endBoundaryBytes, 0, endBoundaryBytes.Length);
                stream.Close();
            }
            WebResponse response = wr.GetResponse();
            response.Close();
        }

        private void Browse_Click(object sender, EventArgs e) {
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e) {
            FileName.Text = openFileDialog1.FileName;
        }
    }
}