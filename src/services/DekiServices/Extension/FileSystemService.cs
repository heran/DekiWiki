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

using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch File System Extension", "Copyright (c) 2006-2010 MindTouch Inc.",
        Info = "http://developer.mindtouch.com/App_Catalog/FileSystem",
        SID = new string[] { 
            "sid://mindtouch.com/2008/03/filesystem",
            "http://services.mindtouch.com/deki/draft/2008/03/filesystem" 
        }
    )]
    [DreamServiceConfig("folder", "string", "Specifies the folder to use.")]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DekiExtLibrary(
        Label = "File System",
        Namespace = "filesystem",
        Description = "This extension contains functions for working with the file system.",
        Logo = "$files/filesystem-logo.png"
    )]
    [DekiExtLibraryFiles(Prefix = "MindTouch.Deki.Services.Resources", Filenames = new string[] { "filesystem-logo.png" })]
    public class FileSystemService : DekiExtService {

        //--- Fields ---
        private DirectoryInfo _directoryInfo;

        //--- Functions ---
        [DekiExtFunction(Description = "Show hierarchy of file system starting at a path")]
        public XDoc Tree(
            [DekiExtParam("The top directory (default: root)", true)] string folder, 
            [DekiExtParam("Specifies whether to include all directories or only the top directory (default: false)", true)] bool? topDirectoryOnly,
            [DekiExtParam("Search pattern (default: nil)", true)] string pattern
        ) {
            if(_directoryInfo == null) {
                throw new DreamBadRequestException("folder is misconfigured");
            }

            string id = StringUtil.CreateAlphaNumericKey(8);
            if (null != folder && folder.Contains("..")) {
                throw new ArgumentException("Relative paths are not allowed", "folder");
            }

            // Construct the uri used to expand the root node
            XUri dynamicExpandUri = DreamContext.Current.AsPublicUri(null == folder ? Self.At("expand") : Self.At("expand", XUri.DoubleEncodeSegment(folder.Replace("+", "%2b")))).With("dream.out.format", "json");
            dynamicExpandUri = dynamicExpandUri.With("topDirectoryOnly", topDirectoryOnly.GetValueOrDefault(false).ToString());
            if (null != pattern) {
                dynamicExpandUri = dynamicExpandUri.With("pattern", pattern);
            }
            string yahooTreeScript =
@"(function() {
     var tree;
     function treeInit() {
       tree = new YAHOO.widget.TreeView({id});
       tree.setDynamicLoad(loadNodeData);
       var root = tree.getRoot();
       var rootNode = new YAHOO.widget.TextNode({name}, root, true);
       rootNode.dynamicexpanduri = {dynamicexpanduri};
       tree.draw();
     }
     
     function makeChildNode(node, result) {
       var currentNode = new YAHOO.widget.TextNode(result.name, node, false);
       if (result.dynamicexpanduri != undefined) {
         currentNode.dynamicexpanduri = result.dynamicexpanduri;
       } else {
         currentNode.href = result.href;
         currentNode.labelStyle = result.labelstyle;
         currentNode.isLeaf = true; 
       }
     }

     function loadNodeData(node, fnLoadComplete) {
       if(node.dynamicexpanduri){
         $.get(node.dynamicexpanduri, null, function(r) {
           var response = (typeof r == 'string') ? eval('(' + (r || 0) +  ')') : r;
           if(response.result != undefined) {
             if(response.result.name != undefined) {
               makeChildNode(node, response.result);
             } else {
               for(var key in response.result) {
                 var entry = response.result[key];
                 if (entry.name != undefined) {
                   makeChildNode(node, entry);
                 }
               }
             }
           }
           fnLoadComplete();           
         });
       } else {
         fnLoadComplete();           
       }
     }
     YAHOO.util.Event.onDOMReady(treeInit);
   })();"
                .Replace("{id}", StringUtil.QuoteString(id))
                .Replace("{name}",  StringUtil.QuoteString(null == folder ? _directoryInfo.FullName : (new DirectoryInfo(_directoryInfo.FullName + Path.DirectorySeparatorChar +  folder).FullName)))
                .Replace("{dynamicexpanduri}", StringUtil.QuoteString(dynamicExpandUri.ToString()));

            // Load in the necessary yahoo resources and create a script element with the javascript for this control
            return new XDoc("html").Start("head")
                                        .Start("script").Attr("type", "text/javascript").Value(yahooTreeScript).End()
                                        .Start("script").Attr("src", "http://yui.yahooapis.com/2.5.1/build/treeview/treeview-min.js").End()
                                        .Start("link").Attr("type", "text/css").Attr("rel", "stylesheet").Attr("href", "http://yui.yahooapis.com/2.5.1/build/treeview/assets/skins/sam/treeview.css").End()
                                    .End()
                                   .Start("body").Start("div").Attr("id", id).End().End();
        }

        //--- Features ---
        [DreamFeature("GET:doc/{filename}", "Retrieves the contents of a specified file")]
        [DreamFeatureParam("{filename}", "string", "The file to retrieve")]
        public Yield GetFile(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            if(_directoryInfo == null) {
                throw new DreamBadRequestException("folder is misconfigured");
            }
            
            // Extract the filename
            string filename = context.GetParam("filename", String.Empty);
            filename = XUri.Decode(filename);
            if (filename.Contains("..")) {
                response.Return(DreamMessage.Forbidden("Relative paths are not allowed"));
                yield break;
            }
            FileInfo currentFile= new FileInfo(_directoryInfo.FullName + Path.DirectorySeparatorChar + filename);
            
            // Retrieve the file
            DreamMessage message = GetFile(currentFile.FullName);
            message.Headers.ContentDisposition = new ContentDisposition(true, currentFile.CreationTimeUtc, currentFile.LastWriteTimeUtc, null, currentFile.Name, currentFile.Length);
            response.Return(message);
            yield break;
        }

        [DreamFeature("GET:expand", "Retrieves the contents of the file system root")]
        [DreamFeature("GET:expand/{foldername}", "Retrieves the contents of the file system under foldername")]
        [DreamFeatureParam("{foldername}", "string", "The directory to retrieve.")]
        [DreamFeatureParam("topDirectoryOnly", "bool?", "If true, only display the top directory; default is false.")]
        [DreamFeatureParam("pattern", "string?", "Specifies a file search pattern.")]
        public Yield ExpandFolder(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            if(_directoryInfo == null) {
                throw new DreamBadRequestException("folder is misconfigured");
            }
            
            // Extract the folder to expand
            string foldername = context.GetParam("foldername", String.Empty);
            foldername = XUri.Decode(foldername);
            if (foldername.Contains("..")) {
                response.Return(DreamMessage.Forbidden("Relative paths are not allowed"));
                yield break;
            }

            // Extract the search pattern
            string pattern = context.GetParam("pattern", null);

            DirectoryInfo currentDirectory = new DirectoryInfo(_directoryInfo.FullName + Path.DirectorySeparatorChar + foldername);
            XDoc result = new XDoc("results");

            // If specified, retrieve all the directories under the current directory
            bool topDirectoryOnly = context.GetParam("topDirectoryOnly", false);
            if (!topDirectoryOnly) {
                foreach (DirectoryInfo directory in currentDirectory.GetDirectories()) {
                    string encodedDirectoryName = XUri.DoubleEncodeSegment((foldername + Path.DirectorySeparatorChar + directory.Name).Replace("+","%2b"));
                    XUri dynamicExpandUri = DreamContext.Current.AsPublicUri(Self.At("expand", encodedDirectoryName)).With("dream.out.format", "json");
                    if (null != pattern) {
                        dynamicExpandUri = dynamicExpandUri.With("pattern", pattern);
                    }
                    result.Start("result").Elem("name", directory.Name).Elem("dynamicexpanduri", dynamicExpandUri.ToString()).End();
                }
            }

            // Retrieve files according to the search pattern
            FileInfo[] files;
            if (null != pattern) {
                files = currentDirectory.GetFiles(pattern, SearchOption.TopDirectoryOnly);
            } else {
                files = currentDirectory.GetFiles();
            }
            foreach (FileInfo file in files) {
                string encodedFileName = XUri.DoubleEncodeSegment((foldername + Path.DirectorySeparatorChar + file.Name).Replace("+","%2b"));
                XUri href = DreamContext.Current.AsPublicUri(Self.At("doc", encodedFileName));
                result.Start("result").Elem("name", file.Name).Elem("href", href.ToString()).Elem("labelstyle","iconitext-16 ext-" + file.Extension.TrimStart('.').ToLowerInvariant()).End();
            }

            response.Return(DreamMessage.Ok(result)); 
            yield break;
        } 

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            string folder = config["folder"].AsText;
            if(folder != null) {
                _directoryInfo = new DirectoryInfo(folder);
            }
            result.Return();
        }

        private DreamMessage GetFile(string filename) {
            DreamMessage message;
            try {
                message = DreamMessage.FromFile(filename);
            } catch(FileNotFoundException e) {
                message = DreamMessage.NotFound("file not found");
            } catch(Exception e) {
                message = DreamMessage.BadRequest("invalid path");
            }
            return message;
        }
    }
}