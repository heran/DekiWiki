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
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Tests.RemoteInstanceTests {

    public class RemoteInstanceService {

        //--- Class Fields ---
        private static RemoteInstanceService _instance = new RemoteInstanceService();
        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        //--- Class Methods ---
        public static void Reset() { _instance = new RemoteInstanceService(); }

        //--- Class Properties ---
        public static RemoteInstanceService Instance { get { return _instance; } }

        //--- Fields ---
        public readonly XUri Uri = new XUri("mock://remotemanager/");

        public XDoc DefaultConfig = new XDoc("wikis")
            .Start("config")
                .Attr("id", "default")
                .Start("security").Elem("api-key", Utils.Settings.InstanceApiKey).End()
                .Elem("host", "*")
                .Start("page-subscription").Elem("from-address", "foo@bar.com").End()
                .Elem("db-server", Utils.Settings.DbServer)
                .Elem("db-port", "3306")
                .Elem("db-catalog", Utils.Settings.DbCatalog)
                .Elem("db-user", Utils.Settings.DbUser)
                .Start("db-password").Attr("hidden", "true").Value(Utils.Settings.DbPassword).End()
                .Elem("db-options", "pooling=true; Connection Timeout=5; Protocol=socket; Min Pool Size=2; Max Pool Size=50; Connection Reset=false;character set=utf8;ProcedureCacheSize=25;Use Procedure Bodies=true;")
            .End();

        public string DefaultWikiId = "default";
        public Func<string, string> HostLookupOverride;
        public Func<string, XDoc> LicenseOverride;
        public Func<string, XDoc> ConfigOverride;

        //--- Constructors ---
        public RemoteInstanceService() {
            MockPlug.Deregister(Uri);
            MockPlug.Register(Uri, CallbackHandler);
        }

        //--- Methods ---
        private void CallbackHandler(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response) {
            if(uri.Segments.Length == 0) {
                response.Return(DreamMessage.Ok());
                return;
            }
            var segments = uri.Segments;
            var wikiId = segments[0];
            if(wikiId.StartsWith("=")) {
                var id = (HostLookupOverride == null) ? DefaultWikiId : HostLookupOverride(wikiId.Substring(1));
                response.Return(DreamMessage.Ok(new XDoc("wiki").Attr("id", id)));
                return;
            }
            if(segments.Length == 2 && segments[1] == "license") {
                XDoc license;
                if(LicenseOverride == null) {
                    _log.Debug("returning license from disk");
                    license = XDocFactory.LoadFrom(Utils.Settings.LicensePath, MimeType.TEXT_XML);
                } else {
                    _log.Debug("returning license from override callback");
                    license = LicenseOverride(wikiId);
                }
                response.Return(DreamMessage.Ok(license));
                return;
            }
            var config = (ConfigOverride == null) ? DefaultConfig : ConfigOverride(wikiId);
            response.Return(DreamMessage.Ok(config));
        }
    }
}
