/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2011 MindTouch Inc.
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
using System.Data;
using System.Reflection;
using System.Linq;

using MindTouch.Dream;
using MindTouch.Data;
using MindTouch.Xml;
using MindTouch.Deki;

using NUnit.Framework;

namespace MindTouch.Deki.CheckDb {

    [TestFixture]
    public class DatabaseTests {

        //--- Types ---
        private class PageInfo {
            public uint Id { get; set; }
            public uint ParentId { get; set; }
            public Title Title { get; set; }
        }

        //--- Class Fields ---
        private static log4net.ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private DataFactory _factory;
        private DataCatalog _catalog;
        private string _dbConfigFile = "mindtouch.deki.checkdb.config.xml";
        private XDoc _dbConfig;
        private string _dbSchemaFile = "mindtouch.deki.checkdb.schema.xml";
        private XDoc _dbSchema;

        //--- Class Methods ---
        public static int Main(string[] args) {
            DatabaseTests test = new DatabaseTests();
            test.RunAllTests();
            return 0;
        }

        public void RunAllTests() {
            Init();
            List<string> errors = new List<string>();
            Type type = typeof(DatabaseTests);
            MethodInfo[] methods = type.GetMethods();
            foreach(MethodInfo method in methods) {
                object[] testAttributes = method.GetCustomAttributes(typeof(NUnit.Framework.TestAttribute), false);
                object[] ignoreAttributes = method.GetCustomAttributes(typeof(NUnit.Framework.IgnoreAttribute), false);
                if(testAttributes.Length > 0 && ignoreAttributes.Length == 0) {
                    // this is a test method, run it
                    try {
                        method.Invoke(this, null);
                    } catch(Exception e) {
                        string message = "";
                        if(e.InnerException != null)
                            message = e.InnerException.Message;
                        else
                            message = e.Message;
                        Console.WriteLine(message);
                        errors.Add(message);
                    }
                }
            }
            if(errors.Count > 0) {
                Console.WriteLine("\n**************************************");
                Console.WriteLine(" checkdb found the following errors:");
                Console.WriteLine("**************************************");
                foreach(string error in errors) {
                    Console.WriteLine(error);
                }
            } else {
                Console.WriteLine("\n**************************************");
                Console.WriteLine(" database verification successful!");
                Console.WriteLine("**************************************");
            }
        }

        [TestFixtureSetUp]
        public void Init() {
            LoadDbConfig();
            LoadDbSchema();
        }

        [Test]
        public void TestDbConnection() {
            //Assumptions: 
            // valid DB connection
            //Actions:
            // check connection with the database
            //Expected result: 
            // successful connection
            bool canConnect = true;
            try {
                _catalog.TestConnection();
            } catch {
                canConnect = false;
            }

            Console.WriteLine("* checking connection to database...");
            Assert.IsTrue(canConnect, string.Format(ERR_CANNOT_CONNECT, _catalog.ConnectionString));
        }

        [Test]
        public void CheckTablesExist() {
            Console.WriteLine("* checking tables");
            foreach(XDoc table in _dbSchema["table"]) {

                string tableName = table["@name"].AsText;
                Console.WriteLine(string.Format("** checking table {0}", tableName));
                string checkTableName = _catalog.NewQuery("SHOW TABLES LIKE ?TABLE_NAME")
                   .With("TABLE_NAME", tableName)
                   .Read();

                //verify the table exists
                Assert.IsTrue(!string.IsNullOrEmpty(checkTableName) && tableName == checkTableName, string.Format(ERR_MISSING_TABLE, tableName));
            }
        }

        [Test]
        public void CheckColumns() {

            foreach(XDoc table in _dbSchema["table"]) {
                string tableName = table["@name"].AsText;
                // loop over the columns
                Console.WriteLine(string.Format("* checking columns in table {0}", tableName));
                foreach(XDoc column in table["column"]) {
                    string columnName = column["@name"].AsText;
                    DataSet results = _catalog.NewQuery(string.Format("SHOW COLUMNS FROM {0} WHERE Field=?COLUMN", tableName))
                        .With("COLUMN", columnName)
                        .ReadAsDataSet();
                    Console.WriteLine(string.Format("** checking column {0}", columnName));
                    Assert.IsTrue(results.Tables[0].Rows.Count == 1, string.Format(ERR_MISSING_COLUMN, columnName));

                    // validate default values
                    string defaultValue = column["@default"].AsText;
                    if(!string.IsNullOrEmpty(defaultValue)) {
                        Assert.IsTrue(defaultValue == (string)results.Tables[0].Rows[0]["Default"],
                            string.Format(ERR_INCORRECT_DEFAULT_VALUE, column["@name"].AsText));
                    }
                }
                foreach(XDoc index in table["indexes/index"]) {
                    string indexName = index["@name"].AsText;
                    string[] indexColumns = index["@columns"].AsText.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    DataSet results = _catalog.NewQuery(string.Format("SHOW INDEXES FROM {0} WHERE Key_name=?INDEX", tableName))
                        .With("INDEX", indexName).ReadAsDataSet();
                    foreach(string indexColumn in indexColumns) {
                        bool foundColumn = false;
                        foreach(DataRow row in results.Tables[0].Rows) {
                            if((string)row["Column_name"] == indexColumn)
                                foundColumn = true;
                        }
                        Assert.IsTrue(foundColumn, string.Format(ERR_INCORRECT_INDEX, indexName));
                    }
                }
            }
        }
        [Test]
        public void CheckWikiDbPermissions() {
            // NOTE: mysql doesn't allow us to get specific grants without being an admin user so we need to do an approximation.
            // It might also be the case that wikiuser was given more specific grants (not GRANT ALL) in which case this test
            // will produce a false negative
            string wikiGrant = string.Format("GRANT ALL PRIVILEGES ON `{0}`.* TO '{1}'", _dbConfig["db-catalog"].AsText, _dbConfig["db-user"].AsText);
            bool foundGrant = false;
            _catalog.NewQuery("SHOW GRANTS").Execute(delegate(IDataReader reader) {


                while(reader.Read()) {
                    string grant = (string)reader[0];
                    if(grant.Length >= wikiGrant.Length && grant.Substring(0, wikiGrant.Length) == wikiGrant) {
                        foundGrant = true;
                        break;
                    }
                }
            });
            Assert.IsTrue(foundGrant);
        }

        [Test]
        public void CheckCatalogCharsetUtf8() {
            _catalog.NewQuery("SHOW VARIABLES LIKE 'character_set_database'").Execute(delegate(IDataReader reader) {
                if(reader.Read()) {
                    string charset = (string)reader["Value"];
                    Assert.IsTrue(!string.IsNullOrEmpty(charset) && charset.ToLower() == "utf8",
                        string.Format(ERR_WRONG_CATALOG_CHARSET, charset));
                }
            });
        }

        [Test]
        public void CheckCatalogCollationUtf8() {
            _catalog.NewQuery("SHOW VARIABLES LIKE 'collation_database'").Execute(delegate(IDataReader reader) {
                if(reader.Read()) {
                    string collation = (string)reader["Value"];
                    Assert.IsTrue(!string.IsNullOrEmpty(collation) && collation.ToLower() == "utf8_general_ci",
                        string.Format(ERR_WRONG_CATALOG_COLLATION, collation));
                }
            });
        }
        [Test]
        public void CheckConnectionCharsetMatchesDatabaseCharset() {
            string charset_database = null;
            string charset_connection = null;
            _catalog.NewQuery("SHOW VARIABLES LIKE 'character_set_database'").Execute(delegate(IDataReader reader) {
                if(reader.Read()) {
                    charset_database = (string)reader["Value"];
                    Assert.IsTrue(!string.IsNullOrEmpty(charset_database));
                }
            });
            _catalog.NewQuery("SHOW VARIABLES LIKE 'character_set_connection'").Execute(delegate(IDataReader reader) {
                if(reader.Read()) {
                    charset_connection = (string)reader["Value"];
                    Assert.IsTrue(!string.IsNullOrEmpty(charset_connection));
                }
            });
            Assert.IsTrue(charset_database == charset_connection, string.Format(ERR_CHARSET_MISMATCH, charset_connection, charset_database));
        }
        [Test]
        public void CheckConnectionCollationMatchesDatabaseCollation() {
            string collation_database = null;
            string collation_connection = null;
            _catalog.NewQuery("SHOW VARIABLES LIKE 'collation_database'").Execute(delegate(IDataReader reader) {
                if(reader.Read()) {
                    collation_database = (string)reader["Value"];
                    Assert.IsTrue(!string.IsNullOrEmpty(collation_database));
                }
            });
            _catalog.NewQuery("SHOW VARIABLES LIKE 'collation_connection'").Execute(delegate(IDataReader reader) {
                if(reader.Read()) {
                    collation_connection = (string)reader["Value"];
                    Assert.IsTrue(!string.IsNullOrEmpty(collation_connection));
                }
            });
            Assert.IsTrue(collation_database == collation_connection, string.Format(ERR_COLLATION_MISMATCH, collation_connection, collation_database));
        }

        [Test]
        public void CheckHomePageExists() {
            uint id = _catalog.NewQuery("SELECT page_id from pages where page_namespace=0 and page_title=''").ReadAsUInt() ?? 0;
            Assert.IsTrue(id > 0, ERR_HOME_PAGE_NOT_FOUND);
        }

        [Test]
        public void CheckOrphanedParentPages() {
            List<string> orphanedPages = new List<string>();
            _catalog.NewQuery("SELECT page_id, page_title, page_parent from pages where page_namespace=0 and page_parent != 0 and page_is_redirect=0")
                .Execute(delegate(IDataReader reader) {
                while(reader.Read()) {
                    uint page_id = (uint)reader["page_id"];
                    int parentPage = (int?)reader["page_parent"] ?? 0;
                    if(parentPage == 0 || !PageExists((uint)parentPage))
                        orphanedPages.Add(page_id.ToString());
                }
            });

            string.Join(",", orphanedPages.ToArray());
            Assert.IsTrue(orphanedPages.Count == 0,
                string.Format(ERR_ORPHANED_PAGES, string.Join(",", orphanedPages.ToArray())));
        }
        [Test]
        public void CheckForOrphanedFiles() {
            List<string> oprhanedFiles = new List<string>();
            _catalog.NewQuery("SELECT res_id, resrev_parent_page_id, file_id from resources r JOIN resourcefilemap rfm on r.res_id=rfm.resource_id where r.res_type=2 and r.res_deleted=0")
                .Execute(delegate(IDataReader reader) {
                while(reader.Read()) {
                    uint res_id = (uint)reader["res_id"];
                    uint page_id = (uint?)reader["resrev_parent_page_id"] ?? 0;
                    uint file_id = (uint)reader["file_id"];
                    if(page_id == 0 || !PageExists(page_id))
                        oprhanedFiles.Add(file_id.ToString());
                }
            });

            Assert.IsTrue(oprhanedFiles.Count == 0,
                string.Format(ERR_ORPHANED_FILES, string.Join(",", oprhanedFiles.ToArray())));
        }
        [Test]
        public void CheckTalkPageLanguage() {
            List<string> invalidTalkPageIds = new List<string>();
            // Talk: pages don't have a parent_page id so we match based on the page_title
            _catalog.NewQuery("SELECT page_id, page_title, page_language FROM pages WHERE page_namespace=1")
                    .Execute(delegate(IDataReader reader) {
                while(reader.Read()) {
                    uint page_id = SysUtil.ChangeType<uint?>(reader["page_id"]) ?? 0;
                    string page_title = SysUtil.ChangeType<string>(reader["page_title"]) ?? "";
                    string page_language = SysUtil.ChangeType<string>(reader["page_language"]) ?? "";

                    _catalog.NewQuery("SELECT page_language FROM pages where page_title=?PAGE_TITLE AND page_namespace=0").With("PAGE_TITLE", page_title)
                        .Execute(delegate(IDataReader reader2) {
                        while(reader2.Read()) {
                            string main_page_language = SysUtil.ChangeType<string>(reader2["page_language"]) ?? "";
                            if(main_page_language != page_language)
                                invalidTalkPageIds.Add(page_id.ToString());
                        }
                    }
                    );
                }
            }
            );
            Assert.IsTrue(invalidTalkPageIds.Count == 0,
                string.Format(ERR_INVALID_TALK_LANGUAGE, string.Join(",", invalidTalkPageIds.ToArray())));
        }

        [Test]
        public void Mysql_can_handle_large_title_in_clause() {
            var titles = new List<string>();
            for(var i = 0; i < 1000; i++) {
                titles.Add("'" + StringUtil.CreateAlphaNumericKey(255) + "'");
            }
            Assert.Greater(
                _catalog.NewQuery(string.Format("SELECT count(*) FROM pages WHERE page_title in ('',{0})", string.Join(",", titles.ToArray())))
                    .ReadAsInt() ?? 0,
                0);
        }

        [Test]
        public void Mysql_can_handle_large_id_in_clause() {
            var homepageId = _catalog.NewQuery("SELECT page_id from pages where page_namespace=0 and page_title=''").ReadAsUInt() ?? 0;
            var ids = new List<string>();
            ids.Add(homepageId.ToString());
            for(var i = 0; i < 100000; i++) {
                ids.Add("" + 100000 + i);
            }
            Assert.Greater(
                _catalog.NewQuery(string.Format("SELECT count(*) FROM pages WHERE page_id in ({0})", string.Join(",", ids.ToArray())))
                    .ReadAsInt() ?? 0,
                0);
        }

        [Test]
        public void CheckParentPageRecords() {
            int failed = 0;
            var pages = new List<PageInfo>();

            // get all the pages and load them into a list
            _catalog.NewQuery("SELECT page_id, page_title, page_namespace, page_parent from pages where page_is_redirect=0")
                .Execute(delegate(IDataReader reader) {
                while(reader.Read()) {
                    pages.Add(new PageInfo {
                        Id = (uint)reader["page_id"],
                        ParentId = (uint)(int)reader["page_parent"],
                        Title = Title.FromDbPath((NS)(byte)reader["page_namespace"], (string)reader["page_title"], null)
                    });
                }
            });

            // for each page look up the parent and make sure it has the expected path
            foreach(var page in pages) {

                // check if parent id exists
                if(page.ParentId != 0) {
                    var parentsById = (from parent in pages where parent.Id == page.ParentId select parent).ToList();
                    if(parentsById.Count > 1) {
                        _log.Error(String.Format("Page {0} ({1}) has {2} parents. This should not happen.", page.Id, page.Title.AsPrefixedDbPath(), parentsById.Count));
                        failed++;
                        continue;
                    } else if(parentsById.Count == 0) {
                        _log.Error(String.Format("Page {0} ({1}) has no parents according to the database. This should not happen.", page.Id, page.Title.AsPrefixedDbPath()));
                        failed++;
                        continue;
                    }
                    var parentById = parentsById[0];

                    // if parent page exists check if the parent is really the parent of the child
                    if(parentsById[0].Title.AsPrefixedDbPath() != page.Title.GetParent().AsPrefixedDbPath()) {
                        _log.Error(string.Format("Parent of page {0} ({1}) should have parent with path \n{2}. Instead found parent has path \n{3}", page.Id, page.Title.AsPrefixedDbPath(), page.Title.GetParent().AsPrefixedDbPath(), parentsById[0].Title.AsPrefixedDbPath()));
                        failed++;
                    }
                }
            }
            Assert.AreEqual(0, failed, "number of failed pages");
        }

        private void LoadDbConfig() {
            _dbConfig = XDocFactory.LoadFrom(_dbConfigFile, MimeType.XML);
            _factory = new MindTouch.Data.DataFactory(MySql.Data.MySqlClient.MySqlClientFactory.Instance, "?");
            _catalog = new DataCatalog(_factory, _dbConfig);

        }

        private void LoadDbSchema() {
            _dbSchema = XDocFactory.LoadFrom(_dbSchemaFile, MimeType.XML);
        }

        private bool PageExists(uint pageId) {
            uint? id = _catalog.NewQuery("SELECT page_id from pages where page_id=?PAGEID").With("PAGEID", pageId).ReadAsUInt();
            if(id == null)
                return false;
            return true;
        }

        public static string ERR_ORPHANED_PAGES = "ERROR: the following pages are orphans: {0}";
        public static string ERR_HOME_PAGE_NOT_FOUND = "ERROR: home page not found";
        public static string ERR_WRONG_CATALOG_CHARSET = "ERROR: the database character set is {0}, should be 'utf8'";
        public static string ERR_WRONG_CATALOG_COLLATION = "ERROR: the database collation is {0}, should be 'utf8_general_ci'";
        public static string ERR_CHARSET_MISMATCH = "ERROR: the connection character set '{0}' does not match the database character set '{1}'";
        public static string ERR_COLLATION_MISMATCH = "ERROR: the connection collation '{0}' does not match the database collation '{1}'";
        public static string ERR_INCORRECT_INDEX = "ERROR: index {0} is incorrectly defined";
        public static string ERR_INCORRECT_DEFAULT_VALUE = "ERROR: incorrect default value for column: {0}";
        public static string ERR_MISSING_COLUMN = "ERROR: missing column: {0}";
        public static string ERR_MISSING_TABLE = "ERROR: table {0} does not exsit";
        public static string ERR_CANNOT_CONNECT = "ERROR: cannot connect to mysql using connection string:  {0}";
        public static string ERR_ORPHANED_FILES = "ERROR: the following files are orphans: {0}";
        public static string ERR_INVALID_TALK_LANGUAGE = "ERROR: the following talk pages have a different page_language than their parent page: {0}";

    }
}
