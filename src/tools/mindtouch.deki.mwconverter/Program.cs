/*
 * MindTouch MediaWiki Converter
 * Copyright (C) 2006-2008 MindTouch Inc.
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
using System.Reflection;

using MindTouch.Dream;
using MindTouch.Xml;

namespace Mindtouch.Tools {
    internal class Program {

        //--- Constants ---
        private const string API_KEY = "123";
        private const string CONFIG_FILE = "mindtouch.deki.mwconverter.xml";
        private const string MKS_PATH = "mks://localhost/";

        //--- Class Methods ---
        private static void Main(string[] args) {

            Plug host = null;
            try {
                // create the dream environment
                XDoc dreamConfigDoc = new XDoc("config");
                dreamConfigDoc.Elem("server-name", MKS_PATH);
                dreamConfigDoc.Elem("service-dir", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                dreamConfigDoc.Elem("apikey", API_KEY);
                
                host = (new DreamHost(dreamConfigDoc)).Self.With("apikey", API_KEY);
                host.At("load").With("name", "mindtouch.deki.mwconverter").Post();
                host.At("load").With("name", "mindtouch.deki").Post();
                host.At("load").With("name", "mindtouch.indexservice").Post();
                host.At("load").With("name", "mindtouch.deki.services").Post();

            } catch (Exception e) {
                Console.WriteLine("An unexpected error occurred while creating the dream host.");
                Console.WriteLine(e);
                Environment.Exit(1);
            }

            try {

                // load the configuration information
                XDoc converterConfigDoc = XDocFactory.LoadFrom(CONFIG_FILE, MimeType.XML);
                XDoc dekiConfigDoc = XDocFactory.LoadFrom(converterConfigDoc["//deki/startup-xml"].Contents, MimeType.XML)["//config"];
                dekiConfigDoc["path"].ReplaceValue("converter");
                dekiConfigDoc["sid"].ReplaceValue("http://services.mindtouch.com/deki/internal/2007/12/mediawiki-converter");
                dekiConfigDoc.Add(converterConfigDoc["//mediawiki"]);
                host.At("services").Post(dekiConfigDoc);
            } catch (Exception e) {
                Console.WriteLine("An unexpected error occurred while loading the converter configuration settings.");
                Console.WriteLine(e);
                Environment.Exit(1);
            }
               

            Plug service = Plug.New(host.Uri.AtAbsolutePath("converter"), TimeSpan.MaxValue);
            service.PostAsync();
            Console.ReadLine();
        }
    }
}
