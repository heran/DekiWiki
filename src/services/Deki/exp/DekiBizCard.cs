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
using System.Text;

using MindTouch.Dream;

namespace MindTouch.Deki {
    [DreamService("MindTouch DekiWiki - DekiBizCard", "Copyright (c) 2006 MindTouch, Inc.", "http://doc.opengarden.org/Deki_API/Reference/Widgets/DekiBizCard")]
    public class DekiBizCard : DekiWikiServiceBase {
        // --- Constants ---
        private const string template_given_name = "  <span class=\"given-name\">${given_name}</span>";
        private const string template_additional_name = "  &nbsp;<span class=\"additional-name\">${additional_name}</span>&nbsp;";
        private const string template_family_name = "  <span class=\"family-name\">${family_name}</span>";
        private const string template_photo = " <div class=\"photo_frame\">${download_1}<span class=\"bg\">&nbsp;</span><img class=\"photo\" alt=\"photo\" src=\"${photo_url}\" />${download_2}</div>";
        private const string template_download_1 = "<a href=\"/dream/wiki-data/content?${page_q}=${page_id}&amp;id=${id}&amp;dream.out.saveas=${save_as}.vcf&amp;dream.out.type=text/x-vcard&amp;dream.out.format=versit&amp;dream.out.select=//vcard\">";
        private const string template_download_2 = "<center><span class=\"vc\">vCard</span></center></a>";
        private const string template_title = " <div class=\"title\">${title}</div>";
        private const string template_org = " <div class=\"org\">${org}</div>";
        private const string template_email = " <a class=\"email\" href=\"mailto:${email}\">${email}</a>";
        private const string template_street_address = "  <div class=\"street-address\">${street_address}</div>";
        private const string template_extended_address = "  <div class=\"extended-address\">${extended_address}</div>";
        private const string template_city = "  <span class=\"locality\">${city}</span>";
        private const string template_region = "  <span class=\"region\">${region}</span>";
        private const string template_postal_code = "  <span class=\"postal-code\">${postal_code}</span>";
        private const string template_country_name = "  <div class=\"country-name\">${country}</div>";
        private const string template_tel = " <div class=\"tel\"><span class=\"value\">${value}</span>&nbsp;<span class=\"type\">${type}</span></div>";
        private const string template_aim = " <a class=\"url\" href=\"aim:goim?screenname=${aim}\">AIM</a>\n";
        private const string template_yim = " <a class=\"url\" href=\"ymsgr:sendIM?${yim}\">YIM</a>\n";

        // --- Members ---
        private string addressBookHtmlTemplate = null;
        private string formHtml = null;

        // --- Functions ---
        private static string NormalizeSpace(string str) {
            if (str == null) str = "";
            str = str.Trim();
            while (true) {
                string tmp = str.Replace("  ", " ");
                if (tmp.Length == str.Length)
                    break;
                str = tmp;
            }
            return str;
        }

        private static XDoc EnsureElement(XDoc doc, string parentKey, string key, string def) {
            XDoc parent = doc[parentKey];
            if (parent.IsEmpty) {
                doc.Start(parentKey);
                if (key == null)
                    doc.Value(def);
                doc.End();
                parent = doc[parentKey];
            }
            if (key == null)
                return parent;
            XDoc child = parent[key];
            if (child.IsEmpty) {
                if (def == null) {
                    child = null;
                } else {
                    parent.Start(key);
                    parent.Value(def);
                    parent.End();
                    child = parent[key];
                }
            } else if (def == null && child.Contents == "") {
                child.Remove();
                child = null;
            }
            return child;
        }

        private static void EnsureAttribute(XDoc doc, string attr, string def) {
            if (doc["@" + attr].IsEmpty)
                doc.Attr(attr, def);
        }

        private static string EncodeVersit(string str) {
            return str; // issues with outlook: str.Replace("\\\\", "\\").Replace(",", "\\,").Replace(";", "\\;").Replace("\n", "\\n");
        }

        private static string DecodeVersit(string str) {
            return str; // issues with outlook: str.Replace("\\,", ",").Replace("\\;", ";").Replace("\\n", "\n").Replace("\\\\", "\\");
        }

        private static string RenderCard(XDoc vcard, string page_title, string pageid, string id) {
            // 0:Family Name, 1:Given Name, 2:Additional Names, 3:Honorific Prefixes, and 4:Honorific Suffixes
            IList<string> vcard_n = new List<string>(vcard["vcard/n"].Contents.Split(';'));
            while (vcard_n.Count < 3)
                vcard_n.Add(string.Empty);
            string givenname = DecodeVersit(NormalizeSpace(vcard_n[1]));
            string additionalname = DecodeVersit(NormalizeSpace(vcard_n[2]));
            string familyname = DecodeVersit(NormalizeSpace(vcard_n[0]));
            string url = DecodeVersit(vcard["vcard/url"].Contents);
            string aim = DecodeVersit(vcard["vcard/aim"].Contents);
            string yim = DecodeVersit(vcard["vcard/yim"].Contents);
            string title = DecodeVersit(vcard["vcard/title"].Contents);
            string vorg = DecodeVersit(vcard["vcard/org"].Contents);
            string email = DecodeVersit(vcard["vcard/email"].Contents);
            IList<string> vcard_adr = new List<string>(vcard["vcard/adr"].Contents.Split(';'));
            while (vcard_adr.Count < 7)
                vcard_adr.Add(string.Empty);
            string extended = DecodeVersit(vcard_adr[1]);
            string street = DecodeVersit(vcard_adr[2]);
            string city = DecodeVersit(vcard_adr[3]);
            string region = DecodeVersit(vcard_adr[4]);
            string postal = DecodeVersit(vcard_adr[5]);
            string country = DecodeVersit(vcard_adr[6]);
            string voice = DecodeVersit(vcard["vcard/tel[@type='voice']"].Contents);
            string home = DecodeVersit(vcard["vcard/tel[@type='home']"].Contents);
            string work = DecodeVersit(vcard["vcard/tel[@type='work']"].Contents);
            string fax = DecodeVersit(vcard["vcard/tel[@type='fax']"].Contents);
            string cell = DecodeVersit(vcard["vcard/tel[@type='cell']"].Contents);
            string photo = DecodeVersit(vcard["vcard/photo"].Contents);

            bool implied_fn_opt = (additionalname == "") && (familyname.IndexOf(" ") == -1);
            string fn, fullname;
            if (implied_fn_opt) {
                fn = givenname + " " + familyname;
                fullname = fn;
            } else {
                fn = template_given_name.Replace("${given_name}", givenname) + "\n";
                if (additionalname != "") {
                    fn += template_additional_name.Replace("${additional_name}", additionalname) + "\n";
                }
                fn += template_family_name.Replace("${family_name}", familyname) + "\n";

                fullname = givenname;
                if (additionalname != "")
                    fullname += " " + additionalname;
                fullname += " " + familyname;
            }
            fn = NormalizeSpace(fn);
            fullname = NormalizeSpace(fullname);

            string resultstr = "<div class=\"vcard\"";
            if (id != "")
                resultstr += string.Format(" widgetid=\"{0}\"", id);
            resultstr += ">\n";

            if (photo.StartsWith("mos://localhost/"))
                photo = photo.Substring("mos://localhost".Length);
            if (photo == "")
                photo = "/editor/widgets/generichead.png";
            string download_1 = ((page_title != "" || pageid != "") && id != "") ?
                template_download_1
                    .Replace("${page_q}", pageid != "" ? "pageid" : "title")
                    .Replace("${page_id}", pageid != "" ? pageid : System.Web.HttpUtility.UrlEncode(page_title))
                    .Replace("${id}", id)
                    .Replace("${save_as}",
                    System.Web.HttpUtility.UrlEncode(NormalizeSpace(fullname.Replace(".", " ")))
                ) : "";
            string download_2 = ((page_title != "" || pageid != "") && id != "") ? template_download_2 : "";
            resultstr += template_photo.Replace("${photo_url}", photo).Replace("${download_1}", download_1).Replace("${download_2}", download_2) + "\n";

            if (url.StartsWith("http://")) { // make sure the url at least looks like a url before we load it
                resultstr += " <a class=\"url fn\"";
                if (!implied_fn_opt)
                    resultstr += " n";
                resultstr += "\" href=\"" + url + "\">" + fn + "</a>\n";
            } else {
                resultstr += " <div class=\"" + (implied_fn_opt ? "fn" : "fn n") + "\">";
                resultstr += (implied_fn_opt ? "" : "\n  ") + fn + "</div>\n";
            }
            if (title != "") resultstr += template_title.Replace("${title}", title) + "\n";
            if (vorg != "") resultstr += template_org.Replace("${org}", vorg) + "\n";
            if (street != "" || extended != "" || city != "" || region != "" || postal != "" || country != "") {
                resultstr += " <div class=\"adr\">\n";
                if (street != "") resultstr += template_street_address.Replace("${street_address}", street) + "\n";
                if (extended != "") resultstr += template_extended_address.Replace("${extended_address}", extended) + "\n";
                string csz = "";
                if (city != "") csz += template_city.Replace("${city}", city);
                if (region != "") {
                    if (csz != "") csz += ", \n";
                    csz += template_region.Replace("${region}", region);
                }
                if (postal != "") {
                    if (csz != "") csz += "&nbsp;\n";
                    csz += template_postal_code.Replace("${postal_code}", postal);
                }
                if (country != "") {
                    if (csz != "") csz += "\n";
                    csz += template_country_name.Replace("${country}", country) + "\n";
                } else if (csz != "") csz += "\n";
                resultstr += csz + " </div>\n";
            }
            if (voice != "") resultstr += template_tel.Replace("${value}", voice).Replace("${type}", "voice") + "\n";
            if (home != "") resultstr += template_tel.Replace("${value}", home).Replace("${type}", "home") + "\n";
            if (work != "") resultstr += template_tel.Replace("${value}", work).Replace("${type}", "work") + "\n";
            if (fax != "") resultstr += template_tel.Replace("${value}", fax).Replace("${type}", "fax") + "\n";
            if (cell != "") resultstr += template_tel.Replace("${value}", cell).Replace("${type}", "cell") + "\n";
            if (email != "") resultstr += template_email.Replace("${email}", email) + "\n";
            if (aim != "") resultstr += template_aim.Replace("${aim}", aim) + "\n";
            if (yim != "") resultstr += template_yim.Replace("${yim}", yim) + "\n";
            resultstr += "</div>";
            return resultstr;
        }

        #region -- Handlers

        public override string AuthenticationRealm { get { return "DekiWiki"; } }

        [DreamFeature("addressbook", "/", "GET", "", "http://doc.opengarden.org/Deki_API/Reference/Widgets/DekiBizCard")]
        public DreamMessage GetAddressBookHandler(DreamContext context, DreamMessage message) {
            user user = Authenticate(context, message, DekiUserLevel.User);
            page page = Authorize(context, user, DekiAccessLevel.Read, "pageid");
            string title = page.PrefixedName;
            if (this.addressBookHtmlTemplate == null)
                this.addressBookHtmlTemplate = Plug.New(Env.RootUri).At("mount", "deki-widgets").At("addressbook.html").Get().Text;
            string addressBookHtml = WidgetService.ReplaceVariables(addressBookHtmlTemplate, new MyDictionary("%%TITLE%%", title));
            return DreamMessage.Ok(MimeType.HTML, addressBookHtml);
        }

        [DreamFeature("normalize", "/", "POST", "", "http://doc.opengarden.org/Deki_API/Reference/Widgets/DekiBizCard")]
        public DreamMessage PostNormalizeHandler(DreamContext context, DreamMessage message) {
            XDoc vcard = message.Document;
            EnsureElement(vcard, "style", null, "standard");
            EnsureElement(vcard, "vcard", "prodid", "-//mindtouch.com//DekiWiki 1.0//EN");
            EnsureElement(vcard, "vcard", "version", "3.0");
            EnsureElement(vcard, "vcard", "source", "(source of hCard)");

            XDoc nDoc = EnsureElement(vcard, "vcard", "n", ";;;;");
            EnsureAttribute(nDoc, "charset", "utf-8");
            List<string> vcard_n = new List<string>(nDoc.Contents.Split(';'));
            if (vcard_n.Count < 5) {
                while (vcard_n.Count < 5)
                    vcard_n.Add(string.Empty);
                nDoc.ReplaceValue(string.Join(";", vcard_n.ToArray()));
            }
            string givenname = DecodeVersit(NormalizeSpace(vcard_n[1]));
            string additionalname = DecodeVersit(NormalizeSpace(vcard_n[2]));
            string familyname = DecodeVersit(NormalizeSpace(vcard_n[0]));

            string fn;
            if ((additionalname == "") && (familyname.IndexOf(" ") == -1)) {
                fn = givenname + " " + familyname;
            } else {
                fn = givenname;
                if (additionalname != "")
                    fn += " " + additionalname;
                fn += " " + familyname;
            }
            fn = NormalizeSpace(fn);
            XDoc fnDoc = EnsureElement(vcard, "vcard", "fn", fn);
            EnsureAttribute(fnDoc, "charset", "utf-8");
            if (fnDoc.Contents != EncodeVersit(fn))
                fnDoc.ReplaceValue(EncodeVersit(fn));
            string name = EncodeVersit(fn + "'s hCard");
            XDoc nameDoc = EnsureElement(vcard, "vcard", "name", name);
            if (nameDoc.Contents != name)
                nameDoc.ReplaceValue(name);

            XDoc adrDoc = EnsureElement(vcard, "vcard", "adr", ";;;;;;");
            EnsureAttribute(adrDoc, "charset", "utf-8");
            List<string> vcard_adr = new List<string>(adrDoc.Contents.Split(';'));
            if (vcard_adr.Count < 7) {
                while (vcard_adr.Count < 7)
                    vcard_adr.Add(string.Empty);
                adrDoc.ReplaceValue(string.Join(";", vcard_adr.ToArray()));
            }

            EnsureElement(vcard, "vcard", "email", null);

            XDoc orgDoc = EnsureElement(vcard, "vcard", "org", null);
            if (orgDoc != null)
                EnsureAttribute(orgDoc, "charset", "utf-8");

            XDoc titleDoc = EnsureElement(vcard, "vcard", "title", null);
            if (titleDoc != null)
                EnsureAttribute(titleDoc, "charset", "utf-8");

            XDoc photoDoc = EnsureElement(vcard, "vcard", "photo", null);
            if (photoDoc != null) {
                EnsureAttribute(photoDoc, "value", "uri");
                if (photoDoc.Contents.StartsWith("/"))
                    photoDoc.ReplaceValue("mos://localhost" + photoDoc.Contents);
            }

            foreach (XDoc tel in vcard["vcard/tel"]) {
                if (tel["@type"].Contents == "")
                    tel.Remove();
            }

            return DreamMessage.Ok(vcard);
        }

        /*
        <vcard>
            <prodid>-//mindtouch.com//DekiWiki 1.0//EN</prodid>
            <source>mos://localhost/User:JohnS/</source>
            <version>3.0</version>
            <name>John C. Smith's hCard</name>

            <fn charset="utf-8">John C. Smith</fn>
            <n charset="utf-8">Smith;John;C;;</n>
            <org charset="utf-8">MyCorp, Inc.</org>
            <title charset="utf-8">My Fancy Title</title>
            <adr charset="utf-8">;;12345 MyStreet Ave.;MyCity;MN;55101;USA</adr>
            <email>john.smith@email.com</email>
            <tel type="work">(000) 000-0000</tel>
            <tel type="fax">(000) 000-0000</tel>
            <tel type="cell">(000) 000-0000</tel>
            <photo value="uri">mos://localhost/File:User:JohnS/Photo.png</photo>
        </vcard>
         */
        [DreamFeature("render", "/", "POST", "", "http://doc.opengarden.org/Deki_API/Reference/Widgets/DekiBizCard")]
        public DreamMessage PostRenderHandler(DreamContext context, DreamMessage message) {
            string pageid = context.Uri.GetParam("pageid", "");
            string title = context.Uri.GetParam("title", "");
            if (context.Uri.GetParam("mode", "card") == "card") {
                XDoc vcard = message.Document;

                string id = context.Uri.GetParam("id", "");

                string resultstr = RenderCard(vcard, title, pageid, id);
                return DreamMessage.Ok(MimeType.HTML, resultstr);
            } else {
                XDoc widgets = message.Document;
                string resultstr = "<div>";
                foreach (XDoc widget in widgets["//widget"]) {
                    resultstr += RenderCard(widget["dekibizcard"], title, pageid, widget["@id"].Contents);
                }
                resultstr += "</div>";
                return DreamMessage.Ok(XDoc.FromXml(resultstr));
            }
        }

        [DreamFeature("edit", "/", "POST", "", "http://doc.opengarden.org/Deki_API/Reference/Widgets/DekiBizCard")]
        public void PostEditHandler(DreamContext context) {
            context.Redirect(Plug.New(
                Env.RootUri.At("widget", "load", "dekibizcard")
                .With("mode", "edit")
                .With("id", context.Uri.GetParam("id", "-1"))
            ));
        }

        [DreamFeature("form", "/", "GET", "", "http://doc.opengarden.org/Deki_API/Reference/Widgets/DekiBizCard")]
        public DreamMessage PostFormHandler(DreamContext context, DreamMessage message) {
            if (this.formHtml == null) {
                string formTemplate = Plug.New(Env.RootUri).At("mount", "deki-widgets").At("widget-editor-form.html").Get().Text;
                string form = Plug.New(Env.RootUri).At("mount", "deki-widgets").At("dekibizcard-form.html").Get().Text;
                this.formHtml = WidgetService.ReplaceVariables(formTemplate, new MyDictionary(
                    "%%TITLE%%", "BizCard Editor",
                    "%%HEAD%%", "<script type=\"text/javascript\" src=\"/editor/widgets/dekibizcard.js\"></script>\n<style type=\"text/css\" src=\"/editor/widgets/bizcard.css\" ></style>",
                    "%%FORM%%", form
                ));
            }
            return DreamMessage.Ok(MimeType.HTML, this.formHtml);
        }

        System.Xml.Xsl.XslTransform _xslTransform;
        System.Xml.Xsl.XslTransform xslTransform {
            get {
                if (_xslTransform == null) {
                    Plug widgetStorage = Plug.New(Env.RootUri).At("mount", "deki-widgets");
                    _xslTransform = new System.Xml.Xsl.XslTransform();
                    _xslTransform.Load(widgetStorage.At("xhtml2vcard.xsl").Get().Document.AsXmlNode.ParentNode);
                }
                return _xslTransform;
            }
        }

        XDoc GetVCard(XDoc input) {
            try {
                using (System.IO.MemoryStream outStream = new System.IO.MemoryStream()) {
                    System.IO.TextWriter writer = new System.IO.StreamWriter(outStream, Encoding.UTF8);
                    xslTransform.Transform(input.AsXmlNode.ParentNode, null, writer);
                    string vcard = Encoding.UTF8.GetString(outStream.ToArray()).Trim();
                    if (vcard == "")
                        return XDoc.Empty;
                    return XDoc.FromVersit(vcard, "dekibizcard");
                }
            } catch {
                return XDoc.Empty;
            }
        }

        [DreamFeature("hcardtoedit", "/", "POST", "", "http://doc.opengarden.org/Deki_API/Reference/Widgets/DekiBizCard")]
        public DreamMessage PosthCardToEditHandler(DreamContext context, DreamMessage message) {
            XDoc vcard = GetVCard(message.Document);
            if (vcard.IsEmpty) {
                return DreamMessage.Ok(vcard);
            }
            Plug widgetToEdit = Plug.New(Env.RootUri.At("wiki-data", "dekibizcard", "edit"));
            return DreamMessage.Ok(widgetToEdit.Post(vcard).Document);
        }

        [DreamFeature("hcardtoxspan", "/", "POST", "", "http://doc.opengarden.org/Deki_API/Reference/Widgets/DekiBizCard")]
        public DreamMessage PosthCardToXSpanHandler(DreamContext context, DreamMessage message) {
            XDoc vcard = message.Document;
            if (vcard.IsEmpty) {
                return DreamMessage.Ok(vcard);
            }
            return DreamMessage.Ok(XDoc.FromXml(vcard.ToXSpan()));
        }

        #endregion
    }
}
