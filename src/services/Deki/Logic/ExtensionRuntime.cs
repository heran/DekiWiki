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
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using log4net;
using MindTouch.Deki.Data;
using MindTouch.Deki.Script;
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Runtime;
using MindTouch.Deki.Script.Runtime.TargetInvocation;
using MindTouch.Dream;
using MindTouch.Extensions.Time;
using MindTouch.Xml;

namespace MindTouch.Deki.Logic {
    internal class ExtensionRuntime : DekiScriptRuntime {

        //--- Class Methods ---
        public static bool IsSafeMode(PageBE page) {
            return !PermissionsBL.IsUserAllowed(UserBL.GetUserById(page.UserID), page, Permissions.UNSAFECONTENT);
        }

        //--- Fields ---
        private readonly TimeSpan _evaluationTimeout;
        private readonly ILog _log;
        private readonly IInstanceSettings _settings;

        //--- Constructors ---
        public ExtensionRuntime(IInstanceSettings settings) {
            _settings = settings;
            _log = DekiLogManager.CreateLog();
            _evaluationTimeout = settings.GetValue("script/timeout", 60).Seconds();
        }

        //--- Properties ---
        protected override TimeSpan EvaluationTimeout { get { return _evaluationTimeout; } }
        protected override ILog Log { get { return _log; } }

        //--- Methods ---
        public override Plug PreparePlug(Plug plug) {
            plug = base.PreparePlug(plug);
            plug = plug.WithPreHandler(PreProcessRequest);
            return plug;
        }

        public override DekiScriptLiteral ResolveMissingName(string name) {
            var result = base.ResolveMissingName(name);
            if(result != null) {
                return result;
            }

            // check if name refers to a template name
            Title title = Title.FromUriPath(name);

            // If the page title is prefixed with :, do not assume the template namespace
            if(title.Path.StartsWith(":")) {
                title.Path = title.Path.Substring(1);
            } else if(title.IsMain) {
                title.Namespace = NS.TEMPLATE;
            }
            PageBE page = PageBL.GetPageByTitle(title);
            if((page == null) || (page.ID == 0)) {
                return null;
            }
            return DekiScriptExpression.Constant(DekiContext.Current.Deki.Self.At("template"), new DekiScriptList().Add(DekiScriptExpression.Constant(title.AsPrefixedDbPath())));
        }

        public override DekiScriptInvocationTargetDescriptor ResolveRegisteredFunctionUri(XUri uri) {
            var result = base.ResolveRegisteredFunctionUri(uri);
            if(result != null) {
                return result;
            }
            var deki = DekiContext.Current.Instance;
            var found = (from extension in deki.RunningServices.ExtensionServices
                         from function in extension.Extension.Functions where function.Uri == uri
                         select function).FirstOrDefault();
            if(found != null) {

                // TODO (steveb): we shouldn't have to create a descriptor on the fly
                return new DekiScriptInvocationTargetDescriptor(DreamAccess.Public, false, false, found.Name, new DekiScriptParameter[0], DekiScriptType.ANY, null, null, null);
            }
            return null;
        }

        public override int GetMaxOutputSize(DekiScriptEvalMode mode) {
            switch(mode) {
            case DekiScriptEvalMode.EvaluateEditOnly:
                return int.MaxValue;
            case DekiScriptEvalMode.Evaluate:
            case DekiScriptEvalMode.EvaluateSafeMode:
            case DekiScriptEvalMode.EvaluateSaveOnly:
            case DekiScriptEvalMode.None:
            case DekiScriptEvalMode.Verify:
            default:
                return _settings.GetValue("pages/max-page-size", MAX_OUTPUT_SIZE);
            }
        }

        private DreamMessage PreProcessRequest(string verb, XUri uri, XUri normalizedUri, DreamMessage message) {
            DreamContext current = DreamContext.Current;
            DekiContext deki = DekiContext.Current;
            DekiInstance instance = deki.Instance;

            // set preferred culture
            message.Headers.AcceptLanguage = string.Format("{0}, *;q=0.5", current.Culture.Name);

            // add the 'deki' header
            message.Headers[DekiExtService.DEKI_HEADER] = instance.Token;

            // convert implicit environment into a message headers
            DekiScriptMap implicitEnv = DreamContext.Current.GetState<DekiScriptMap>();
            if(implicitEnv != null) {
                foreach(KeyValuePair<string, DekiScriptLiteral> outer in implicitEnv.Value) {
                    DekiScriptMap map = outer.Value as DekiScriptMap;
                    if((map != null) && !map.IsEmpty) {
                        StringBuilder header = new StringBuilder();
                        foreach(KeyValuePair<string, DekiScriptLiteral> inner in map.Value) {
                            string value = inner.Value.AsString();
                            if(value != null) {
                                if(header.Length > 0) {
                                    header.Append(", ");
                                }
                                header.AppendFormat("{0}.{1}={2}", outer.Key, inner.Key, value.QuoteString());
                            }
                        }

                        // add header
                        string headerValue = header.ToString();
                        message.Headers.Add(DekiExtService.IMPLICIT_ENVIRONMENT_HEADER, headerValue);
                    }
                }
            }

            // add digital signature
            DSACryptoServiceProvider dsa = instance.PrivateDigitalSignature;
            if(dsa != null) {
                MemoryStream data = new MemoryStream();

                // get message bytes
                byte[] bytes = message.AsBytes();
                data.Write(bytes, 0, bytes.Length);

                // retrieve headers to sign
                string[] headers = message.Headers.GetValues(DekiExtService.IMPLICIT_ENVIRONMENT_HEADER);
                if(!ArrayUtil.IsNullOrEmpty(headers)) {
                    Array.Sort(headers, StringComparer.Ordinal);
                    bytes = Encoding.UTF8.GetBytes(string.Join(",", headers));
                    data.Write(bytes, 0, bytes.Length);
                }

                // add request date
                string date = DateTime.UtcNow.ToString(XDoc.RFC_DATETIME_FORMAT);
                bytes = Encoding.UTF8.GetBytes(date);
                data.Write(bytes, 0, bytes.Length);

                // sign data
                byte[] signature = dsa.SignData(data.GetBuffer());
                message.Headers.Add(DekiExtService.IMPLICIT_SIGNATURE_HEADER, string.Format("dsig=\"{0}\", date=\"{1}\"", Convert.ToBase64String(signature), date));
            }
            return message;
        }
    }
}