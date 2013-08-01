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
using System.Collections.Generic;
using System.Globalization;
using System.Net.Mail;
using Autofac;
using MindTouch.Deki.Data.UserSubscription;
using MindTouch.Xml;

namespace MindTouch.Deki.UserSubscription {
    public class PageSubscriptionInstance : IPageSubscriptionInstance {

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly DateTime _created = DateTime.UtcNow;
        private readonly string _wikiId;
        private readonly string _sitename;
        private readonly string _timezone;
        private readonly string _emailFromAddress;
        private readonly CultureInfo _culture;
        private readonly string _emailFormat;
        private readonly bool _useShortEmailAddress;
        private readonly IPageSubscriptionDataSessionFactory _pageSubscriptionSessionFactory;
        private readonly Dictionary<uint, PageSubscriptionUser> _users = new Dictionary<uint, PageSubscriptionUser>();

        //--- Constructors ---
        public PageSubscriptionInstance(string wikiId, XDoc config, IContainer container) {
            _log.DebugFormat("created PageSubscriptionInstance for wikiid '{0}'", wikiId);
            _wikiId = wikiId;
            _pageSubscriptionSessionFactory = container.Resolve<IPageSubscriptionDataSessionFactory>(new NamedParameter("config", config));

            // derive siteinfo
            _sitename = config["ui/sitename"].AsText;
            if(string.IsNullOrEmpty(_sitename)) {
                _log.WarnFormat("missing ui/sitename for instance {0}", _wikiId);
            }
            _timezone = config["ui/timezone"].AsText;
            var emailFromAddress = config["page-subscription/from-address"].AsText;
            if(string.IsNullOrEmpty(emailFromAddress)) {
                emailFromAddress = config["admin/email"].AsText;
            }
            if(string.IsNullOrEmpty(emailFromAddress)) {
                _log.WarnFormat("missing page-subscription/from-address and admin/email for instance {0}", _wikiId);
            } else {
                var address = new MailAddress(emailFromAddress);
                if(string.IsNullOrEmpty(address.DisplayName)) {
                    address = new MailAddress(emailFromAddress, emailFromAddress);
                }
                _emailFromAddress = address.ToString();
            }
            _emailFormat = config["page-subscription/email-format"].AsText;
            _useShortEmailAddress = config["page-subscription/use-short-email-address"].AsBool ?? false;
            _culture = CultureUtil.GetNonNeutralCulture(config["ui/language"].AsText) ?? CultureInfo.GetCultureInfo("en-us");
        }

        //--- Properties ---
        public string WikiId { get { return _wikiId; } }
        public string Sitename { get { return _sitename; } }
        public string Timezone { get { return _timezone; } }
        public string EmailFromAddress { get { return _emailFromAddress; } }
        public CultureInfo Culture { get { return _culture; } }
        public string EmailFormat { get { return _emailFormat; } }
        public bool UseShortEmailAddress { get { return _useShortEmailAddress; } }
        public bool IsValid {
            get { return !string.IsNullOrEmpty(_sitename) && !string.IsNullOrEmpty(_emailFromAddress) && _created.AddMinutes(1) > DateTime.UtcNow; }
        }

        //--- Methods ---
        public PageSubscriptionUser GetUserInfo(uint userId) {
            PageSubscriptionUser user;
            lock(_users) {
                if(!_users.TryGetValue(userId, out user)) {
                    user = new PageSubscriptionUser(userId);
                    _users[userId] = user;
                }
            }
            return user;
        }

        public IPageSubscriptionDataSession CreateDataSession() {
            return _pageSubscriptionSessionFactory.Create();
        }
    }
}