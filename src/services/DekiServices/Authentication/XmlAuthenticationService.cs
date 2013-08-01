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

/*
    This is example of xml file for this service:

    <?xml version="1.0" encoding="utf-8"?>
    <dekiauth>  
       <users>
        <user name="foo">
          <password type="md5">9a618248b64db62d15b300a07b00580b</password>
          <email>foo@somewhere.com</email>
          <fullname>Foo Smith</fullname>
          <status>enabled</status>
        </user>
        <user name="joe">
          <password type="plain">supersecret</password>
          <email>joe@somewhere.com</email>
          <fullname>Joe Smith</fullname>
          <status>enabled</status>
        </user>
      </users>
      <groups>
        <group name="sales">
          <user name="foo"/>
          <user name="joe"/>
        </group>
        <group name="admin">
          <user name="joe"/>
        </group>
      </groups>
    </dekiauth>
 */

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Xml Authentication Service", "Copyright (c) 2006-2010 MindTouch Inc.",
       Info = "http://wiki.developer.mindtouch.com/MindTouch_Deki/Authentication/XmlAuthentication",
        SID = new string[] { 
            "sid://mindtouch.com/2008/08/xml-authentication"
        }
    )]

    [DreamServiceConfig("xmlauth-path", "string?", "Path for xml file (default: \"xmlauth.xml\")")]
    public class XmlAuthenticationService : DekiAuthenticationService {
        string _path = null;

        public override string AuthenticationRealm {
            get { return "Xml"; }
        }

        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            _path = config["xmlauth-path"].AsText ?? "xmlauth.xml";

            result.Return();
        }

        public override bool CheckUserPassword(string user, string password) {
            XDoc doc = XDocFactory.LoadFrom(_path, MimeType.XML);

            XDoc userNode = doc[string.Format("users/user[@name=\"{0}\"]", user)];
            if (!userNode.IsEmpty && (userNode["status"].IsEmpty || userNode["status"].AsText.ToUpper() == "ENABLED")) {
                if (userNode["password"].IsEmpty)
                    throw new DreamInternalErrorException("Password not found.");

                if (userNode["password/@type"].IsEmpty)
                    throw new DreamInternalErrorException("Password type not found.");

                switch (userNode["password/@type"].AsText.ToUpper()) {
                case "MD5": {
                        MD5 md5 = new MD5CryptoServiceProvider();
                        byte[] hash = md5.ComputeHash(Encoding.Default.GetBytes(password));
                        StringBuilder builder = new StringBuilder(hash.Length);
                        for (int i = 0; i < hash.Length; i++)
                            builder.Append(hash[i].ToString("x2"));

                        return StringComparer.OrdinalIgnoreCase.Compare(userNode["password"].AsText, builder.ToString()) == 0;
                    }
                case "PLAIN":
                    return userNode["password"].AsText == password;
                default:
                    throw new DreamInternalErrorException("Unsupported password type.");
                }
            }

            return false;
        }

        public override DekiAuthenticationService.User GetUser(string user) {
            User result = null;
            XDoc doc = XDocFactory.LoadFrom(_path, MimeType.XML);

            XDoc userNode = doc[string.Format("users/user[@name=\"{0}\"]", user)];
            if (!userNode.IsEmpty) {
                List<Group> groups = GetGroups(doc, string.Format("groups/group[user/@name=\"{0}\"]", user));
                if (groups.Count == 0)
                    groups.Add(new Group(string.Empty));
                result = new User(
                    userNode["@name"].IsEmpty ? string.Empty : userNode["@name"].AsText,
                    userNode["email"].IsEmpty ? string.Empty : userNode["email"].AsText,
                    groups.ToArray()
                    );

                result.Custom["fullname"] = userNode["fullname"].IsEmpty ? string.Empty : userNode["fullname"].AsText;
                result.Custom["status"] = userNode["status"].IsEmpty ? "enabled" : userNode["status"].AsText;
            }

            return result;
        }

        public override Group GetGroup(string group) {
            XDoc doc = XDocFactory.LoadFrom(_path, MimeType.XML);

            XDoc groupNode = doc[string.Format("groups/group[@name=\"{0}\"]", group)];
            if (!groupNode.IsEmpty)
                return new Group(groupNode["@name"].IsEmpty ? string.Empty : groupNode["@name"].AsText);

            return null;
        }

        public override Group[] GetGroups() {
            XDoc doc = XDocFactory.LoadFrom(_path, MimeType.XML);

            return GetGroups(doc, "groups/group").ToArray();
        }

        private List<Group> GetGroups(XDoc doc, string xpath) {
            List<Group> groups = new List<Group>();
            foreach (XDoc node in doc[xpath])
                if (!node["@name"].IsEmpty)
                    groups.Add(new Group(node["@name"].AsText));

            return groups;
        }
    }
}
