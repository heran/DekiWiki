/*
 * MindTouch Deki - enterprise collaboration and integration platform
 * Copyright (C) 2006-2008 MindTouch, Inc.
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
using System.Linq;
using System.Data;
using log4net;
using MindTouch.Deki;
using MindTouch.Deki.Data;
using MindTouch.Deki.Storage;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Tools {
    internal class Program {

        //--- Class Fields ---
        private static string _mode;
        private static string _prefix;
        private static string _attachmentPath;
        private static string _public_key;
        private static string _private_key;
        private static string _default_bucket;
        private static string _connectionString;
        private static MindTouch.Data.DataCatalog _catalog;

        //--- Class Methods ---
        static void Main(string[] args) {
            if(args.Length < 6) {
                ShowUsage();
                return;
            }

            ParseArguments(args);

            //Initialize wiki db catalog
            _catalog = new MindTouch.Data.DataCatalog(new MindTouch.Data.DataFactory(MySql.Data.MySqlClient.MySqlClientFactory.Instance, "?"), _connectionString);

            ResourceBE[] attachments = LoadAllAttachments();

            if(StringUtil.EqualsInvariant(_mode, "s3tofs"))
                S3ToFS(attachments);
            else if(StringUtil.EqualsInvariant(_mode, "fstos3"))
                FSToS3(attachments);
        }

        private static void S3ToFS(ResourceBE[] attachmentList) {
            XDoc s3config = new XDoc("config")
                .Start("publickey").Value(_public_key).End()
                .Start("privatekey").Value(_private_key).End()
                .Start("bucket").Value(_default_bucket).End()
                .Start("prefix").Value(_prefix).End();
            S3Storage s3 = new S3Storage(s3config, LogUtils.CreateLog<S3Storage>());

            XDoc fsconfig = new XDoc("config")
                .Start("path").Value(_attachmentPath).End();
            FSStorage fs = new FSStorage(fsconfig);

            transferFiles(attachmentList, s3, fs);
        }

        private static void FSToS3(ResourceBE[] attachmentList) {
            XDoc s3config = new XDoc("config")
                .Start("publickey").Value(_public_key).End()
                .Start("privatekey").Value(_private_key).End()
                .Start("bucket").Value(_default_bucket).End()
                .Start("prefix").Value(_prefix).End();
            S3Storage s3 = new S3Storage(s3config, LogUtils.CreateLog<S3Storage>());

            XDoc fsconfig = new XDoc("config")
                .Start("path").Value(_attachmentPath).End();
            FSStorage fs = new FSStorage(fsconfig);

            transferFiles(attachmentList, fs, s3);
        }

        private static void ParseArguments(string[] args) {
            _mode = args[0];
            _public_key = args[1];
            _private_key = args[2];
            _default_bucket = args[3];
            _prefix = args[4];
            _attachmentPath = args[5];
            _connectionString = args[6];

            if(string.IsNullOrEmpty(_mode) || string.IsNullOrEmpty(_public_key)
                || string.IsNullOrEmpty(_private_key) || string.IsNullOrEmpty(_default_bucket)
                || string.IsNullOrEmpty(_prefix) || string.IsNullOrEmpty(_attachmentPath)
                || string.IsNullOrEmpty(_connectionString)
                && (_mode != "s3tofs" || _mode != "fstos3")) {
                ShowUsage();
                return;
            }
        }

        private static void ShowUsage() {
            Console.WriteLine("Attachment Migration Utility");
            Console.WriteLine("USAGE: migrate <mode> <s3-access-key> <s3-private-key> <s3-bucket-name> <s3-file-prefix> <fs-attachment-dir> ");
            Console.WriteLine("    <mode>              ( s3tofs | fstos3 )");
            Console.WriteLine("        s3tofs -- migrate files from S3 to the filesystem");
            Console.WriteLine("        fstos3 -- migrate files from the filesystem to S3");
            Console.WriteLine("    <s3-access-key>       s3 access key");
            Console.WriteLine("    <s3-private-key>      s3 private key");
            Console.WriteLine("    <s3-bucket-name>      s3 bucket to get/put files into");
            Console.WriteLine("    <s3-file-prefix>      s3 file prefix (ex: 'mywiki.mindtouch.com')");
            Console.WriteLine("    <fs-attachment-dir>   filesystem directory where attachments are stored");
            Console.WriteLine("    <sql-connection-string> MySQL connection string (ex: Server=myserver;Port=3306;Database=wikidb;Uid=wikiuser;Pwd=password;pooling=true; Connection Timeout=5; Protocol=socket; Min Pool Size=6; Max Pool Size=50; ProcedureCacheSize=25; Connection Reset=false;character set=utf)");
        }

        private static void transferFiles(ResourceBE[] attachmentList, IStorageProvider source, IStorageProvider dest) {
            foreach(ResourceBE attachment in attachmentList) {
                StreamInfo sourceInfo;

                try {
                    if(attachment.Revision > 1) {
                        // loop through all the attachments
                        foreach(ResourceBE attRev in GetRevisions(attachment)) {
                            Console.WriteLine("Migrating: " + attachment.Name + ", rev: " + attRev.Revision);
                            sourceInfo = source.GetFile(attRev, SizeType.ORIGINAL, false);
                            if(sourceInfo != null) {
                                dest.PutFile(attRev, SizeType.ORIGINAL, sourceInfo);
                            }
                        }
                    } else {
                        Console.WriteLine("Migrating: " + attachment.Name + ", rev: 1");
                        sourceInfo = source.GetFile(attachment, SizeType.ORIGINAL, false);
                        if(sourceInfo != null) {
                            dest.PutFile(attachment, SizeType.ORIGINAL, sourceInfo);
                        }
                    }
                } catch(Exception e) {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private static ResourceBE[] LoadAllAttachments() {
            ResourceBE[] resources = DbUtils.CurrentSession.Resources_GetByQuery(null, null, new List<ResourceBE.Type> { ResourceBE.Type.FILE }, null, DeletionFilter.ANY, false, null, null).ToArray();
            return Array.ConvertAll(resources, res => (ResourceBE)res);
        }

        private static ResourceBE[] GetRevisions(ResourceBE attach) {
            ResourceBE[] resources = DbUtils.CurrentSession.Resources_GetRevisions(attach.ResourceId, ResourceBE.ChangeOperations.CONTENT, SortDirection.DESC, null).ToArray();
            return Array.ConvertAll(resources, res => (ResourceBE)res);
        }
    }
}
