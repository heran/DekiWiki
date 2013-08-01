/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit developer.mindtouch.com;
 * please review the licensing section.
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 * http://www.gnu.org/copyleft/lesser.html
 */
using System;
using System.IO;
using System.Xml;
using Commons.Xml.Relaxng;
using Commons.Xml.Relaxng.Rnc;
using MindTouch.Dream;

namespace MindTouch.Deki.Script {

    public class ScriptManifestValidator {

        // --- Methods ---
        public ScriptManifestValidationResult Validate(string path) {
            try {
                XmlTextReader xmlReader = new XmlTextReader(path);
                RelaxngValidatingReader validator;

                using(Stream resourceStream
                    = Plug.New("resource://mindtouch.deki.script.check/MindTouch.Deki.Script.ExtensionManifest.rnc").Get().ToStream()) {
                    using(TextReader source = new StreamReader(resourceStream)) {
                        RncParser parser = new RncParser(new NameTable());
                        RelaxngPattern pattern = parser.Parse(source);
                        validator = new RelaxngValidatingReader(xmlReader, pattern);
                    }
                }
                while(validator.Read()) {
                    // do nothing, errors will be reported through exceptions
                }
            } catch(Exception e) {
                return new ScriptManifestValidationResult(true, e.Message);
            }

            return new ScriptManifestValidationResult();
        }
    }

    public class ScriptManifestValidationResult {

        // --- Fields ---
        private readonly bool isInvalid;
        private readonly string validationError;

        //--- Constructors ---
        public ScriptManifestValidationResult() {
        }

        public ScriptManifestValidationResult(bool isInvalid, string validationError) {
            this.isInvalid = isInvalid;
            this.validationError = validationError;
        }

        //--- Properties ---
        public bool IsInvalid { get { return isInvalid; } }
        public string ValidationErrors { get { return validationError; } }
    }
}
