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
using log4net;
using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Logic;
using MindTouch.Deki.WikiManagement;
using MindTouch.Dream;

namespace MindTouch.Deki {

    public interface ICurrentUserContext {
        UserBE User { get; set; }
    }

    public class DekiContext : ICurrentUserContext, IDisposable {

        //--- Constants
        public const string LOGGING_CONTEXT_KEY = "DekiContextLoggingContext";

        //--- Static Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Class Properties ---
        public static DekiContext Current {
            get {
                return GetContext(DreamContext.Current);
            }
        }

        public static DekiContext CurrentOrNull {
            get {
                var context = DreamContext.CurrentOrNull;
                return context == null ? null : context.GetState<DekiContext>();
            }
        }

        //--- Class Methods ---
        public static DekiContext GetContext(DreamContext dreamContext) {
            var context = dreamContext.GetState<DekiContext>();
            if(context == null) {
                throw new DekiContextAccessException("DekiContext.Current is not set, because the current DreamContext does not contain a reference");
            }
            return context;
        }

        //--- Fields ---
        public readonly DekiWikiService Deki;
        public readonly string RequestHost;
        private readonly IPlainTextResourceManager _resourceManager;
        private readonly DateTime _now;
        private readonly DekiInstance _instance;

        public string AuthToken;
        public ulong? BanPermissionRevokeMask;
        public string[] BanReasons;
        private XUri _apiUri;
        private bool? _requestContainsApiKey = null;
        private DekiResources _resources;
        private ILicenseManager _licenseManager;
        private UserBE _user;

        /// <summary>
        /// Contains simple key/value stats to be returned as a header in the http response
        /// </summary>
        public readonly Dictionary<string, string> Stats = new Dictionary<string, string>();

        //--- Constructors ---
        internal DekiContext(DekiWikiService deki, DekiInstance instance, string requestHost, DateTime requestTime, IPlainTextResourceManager resourceManager) {
            if(deki == null) {
                throw new ArgumentNullException("deki");
            }
            this.Deki = deki;
            this.RequestHost = requestHost;
            _resourceManager = resourceManager;
            _now = requestTime;
            _instance = instance;

            // Note (arnec): this context string formattingis duplicated in DekiWikiService.Start, because we have two separate
            // context logger types. This one ought to be deprecated over time.
            DreamContext.Current.SetState(LOGGING_CONTEXT_KEY, "[" + _instance.Id + "] ");
        }

        //--- Properties ---
        public UserBE User {
            get { return _user; }
            set {
                _user = value;
                if(_user != null) {

                    // Note (arnec): this context string formattingis duplicated in DekiWikiService.Start, because we have two separate
                    // context logger types. This one ought to be deprecated over time.
                    DreamContext.Current.SetState(LOGGING_CONTEXT_KEY, string.Format("[{0}:{1}({2})] ", _instance.Id, _user.ID, _user.Name));
                }
            }
        }

        public DateTime Now { get { return _now; } }
        public bool HasInstance { get { return _instance != null; } }

        public DekiResources Resources {
            get {

                // Note (arnec): [DreamContext.Current]: Every time I try to get out, they pull me back in!
                var culture = DreamContext.Current.Culture;
                if(_resources == null || _resources.Culture != culture) {
                    _resources = new DekiResources(_resourceManager, culture);
                }
                return _resources;
            }
        }

        public DekiInstance Instance {
            get {
                if(_instance == null) {
                    throw new NoSuchInstanceInvalidArgumentException();
                }
                return _instance;
            }
        }

        internal ILicenseManager LicenseManager {
            get {

                //Note (arnec): license manager is lazily initialized because of Instance vs. Dekicontext.Current creation race conditions
                if(_licenseManager == null) {
                    _licenseManager = _instance.CreateCurrentLicenseManager();
                }
                return _licenseManager;
            }
        }

        /// <summary>
        /// Gets the URI for the current instance of the Deki-Api service
        /// </summary>
        public XUri ApiUri {
            get {
                if(_apiUri == null) {

                    // create api uri based on current request context
                    _apiUri = DreamContext.Current.AsPublicUri(Deki.Self);
                }
                return _apiUri;
            }
        }

        /// <summary>
        /// Gets the URI for the current instance of the Deki-Api service
        /// </summary>
        public Plug ApiPlug {
            get {
                Plug result = Plug.New(ApiUri);

                // check if authentication header needs to be added
                if(!string.IsNullOrEmpty(AuthToken)) {
                    result = result.WithHeader(DekiWikiService.AUTHTOKEN_HEADERNAME, AuthToken);
                }
                return result;
            }
        }

        /// <summary>
        /// Gets the URI for the front end interface for the current wiki instance.
        /// </summary>
        public Plug UiUri {
            get {
                //TODO Max: This assumes that the api is at the same hostname as the UI.. Which may or may not be true in the future.
                //May want to add an override to this in config. (SiteUri ?)
                return Plug.New(ApiUri.WithoutPathQueryFragment());
            }
        }

        /// <summary>
        /// Returns true if the request contains a valid apikey which comes from either the per instance config table or a master key.
        /// Having a valid api key bypasses all permission checks
        /// </summary>
        public bool IsValidApiKeyInRequest {
            get {
                if(_requestContainsApiKey == null) {
                    _requestContainsApiKey = PermissionsBL.ValidateRequestApiKey();
                }
                return _requestContainsApiKey ?? false;
            }
        }

        public void Dispose() { }
    }

    public class DekiContextAccessException : DreamRequestFatalException {

        //--- Constructors ---
        public DekiContextAccessException(string message) : base(message) { }
    }
}
