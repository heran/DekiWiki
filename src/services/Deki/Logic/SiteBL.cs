/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit developer.mindtouch.com;
 * please review the licensing section.
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
using System.Net.Mail;

using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Dream;

namespace MindTouch.Deki.Logic {
    public class SiteBL {

        //--- Fields ---
        private readonly DekiInstance _dekiInstance;
        private readonly UserBE _user;

        //--- Constructor ---
        public SiteBL() {
            var dekiContext = DekiContext.Current;
            _dekiInstance = dekiContext.Instance;
            _user = dekiContext.User;
        }

        //--- Methods ---
        public void SendNoticeToAdmin(string subject, string content, MimeType mime) {

            // TODO (arnec): should this be using the email service?
            UserBE adminUser = UserBL.GetAdmin();
            if(adminUser == null) {
                throw new SiteNoAdminFatalException();
            }
            string smtphost = string.Empty;
            int smtpport = 0;
            if(smtphost == string.Empty) {
                throw new SiteConflictException(DekiResources.SMTP_SERVER_NOT_CONFIGURED());
            }
            if(string.IsNullOrEmpty(adminUser.Email)) {
                throw new SiteConflictException(DekiResources.ADMIN_EMAIL_NOT_SET());
            }
            var smtpclient = new SmtpClient();
            var msg = new MailMessage();
            msg.To.Add(adminUser.Email);
            msg.From = new MailAddress(_user.Email, _user.Name);
            msg.Subject = _dekiInstance.SiteName + ": " + subject;
            msg.Body = content;
            smtpclient.Host = smtphost;
            if(smtpport != 0) {
                smtpclient.Port = smtpport;
            }
            smtpclient.Send(msg);
        }
    }
}
