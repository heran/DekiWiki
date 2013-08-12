using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using MindTouch.Dream;

namespace MindTouch.Tools.TWConverter
{
    class Program
    {

        private const string DreamAPIUrlTagName = "DreamAPIUrl";
        private const string DekiUserNameTagName = "DekiUserName";
        private const string DekiUserPasswordTagName = "DekiUserPassword";
        private const string PublishContribResultFilesPathTagName = "PublishContribResultFilesPath";
        private const string TWikiPathTagName = "TWikiPath";
        private const string TWikiWebsToConvertTagName = "TWikiWebsToConvert";
        private const string TWikiWebTagName = "TWikiWeb";
        private const string ExportPagesDekiPathTagName = "ExportPagesDekiPath";
        private const string WebNameAttributeName = "name";

        private const string ConfigSettingNotSpecified = "\"{0}\" is not specified in {1}.";
        private const string DirectoryNotFound = "Invalid \"{0}\". Directory \"{1}\" not found.";

        private static void ShowMessage(StreamWriter logWriter, string message, params string[] args)
        {
            message = string.Format(message, args);
            if (logWriter != null)
            {
                logWriter.WriteLine(message);
            }
            Console.WriteLine(message);
        }

        private static void ShowErrorMessage(StreamWriter logWriter, string message, params string[] args)
        {
            ShowMessage(logWriter, message, args);
            Console.ReadLine();
        }

        private static void ShowTagNotSpecified(StreamWriter logWriter, string settingTagName, string configFileName)
        {
            ShowErrorMessage(logWriter, ConfigSettingNotSpecified, settingTagName, configFileName);
        }

        private static void ShowDirectoryNotFound(StreamWriter logWriter, string settingTagName, string directoryPath)
        {
            ShowErrorMessage(logWriter, DirectoryNotFound, settingTagName, directoryPath);
        }

        static void Main(string[] args)
        {
            string assemblyFileName = System.IO.Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            string logFileName = Path.GetFileNameWithoutExtension(assemblyFileName) + ".log";
            using (System.IO.StreamWriter logWriter = new StreamWriter(logFileName))
            {
                logWriter.AutoFlush = true;

                try
                {
                    string configFileName = assemblyFileName + ".xml";
                    if (!System.IO.File.Exists(configFileName))
                    {
                        ShowErrorMessage(logWriter, "File: \"{0}\" not found.", configFileName);
                        return;
                    }

                    XDoc settings = XDocFactory.LoadFrom(configFileName, MimeType.XML);
                    if (settings.IsEmpty)
                    {
                        ShowErrorMessage(logWriter, "Invalid settings file.");
                        return;
                    }

                    string dreamAPIUrl = settings[DreamAPIUrlTagName].AsText;
                    if (string.IsNullOrEmpty(dreamAPIUrl))
                    {
                        ShowTagNotSpecified(logWriter, DreamAPIUrlTagName, configFileName);
                        return;
                    }

                    string dekiUserName = settings[DekiUserNameTagName].AsText;
                    if (string.IsNullOrEmpty(dekiUserName))
                    {
                        ShowTagNotSpecified(logWriter, DekiUserNameTagName, configFileName);
                        return;
                    }

                    string dekiUserPassword = settings[DekiUserPasswordTagName].AsText;
                    if (string.IsNullOrEmpty(dekiUserPassword))
                    {
                        ShowTagNotSpecified(logWriter, DekiUserPasswordTagName, configFileName);
                        return;
                    }

                    string publishContribFilesPath = settings[PublishContribResultFilesPathTagName].AsText;
                    if (string.IsNullOrEmpty(publishContribFilesPath))
                    {
                        ShowTagNotSpecified(logWriter, PublishContribResultFilesPathTagName, configFileName);
                        return;
                    }
                    if (!Directory.Exists(publishContribFilesPath))
                    {
                        ShowDirectoryNotFound(logWriter, PublishContribResultFilesPathTagName, publishContribFilesPath);
                        return;
                    }

                    string exportPagesPath = settings[ExportPagesDekiPathTagName].AsText;
                    if (exportPagesPath == null)
                    {
                        exportPagesPath = string.Empty;
                    }

                    string tWikiPath = settings[TWikiPathTagName].AsText;
                    if (string.IsNullOrEmpty(tWikiPath))
                    {
                        ShowTagNotSpecified(logWriter, TWikiPathTagName, configFileName);
                        return;
                    }
                    if (!Directory.Exists(tWikiPath))
                    {
                        ShowDirectoryNotFound(logWriter, TWikiPathTagName, tWikiPath);
                        return;
                    }

                    string[] websToConvert;
                    XDoc websToConvertDoc = settings[TWikiWebsToConvertTagName];
                    if ((websToConvertDoc == null) || (websToConvertDoc.IsEmpty) || (websToConvertDoc["//" + TWikiWebTagName].IsEmpty))
                    {
                        string[] publishContribExportSubDirs = Directory.GetDirectories(publishContribFilesPath);
                        if (publishContribExportSubDirs.Length == 0)
                        {
                            ShowErrorMessage(logWriter, "No webs to convert in \"{0}\"", publishContribFilesPath);
                            return;
                        }
                        List<string> websToConvertList = new List<string>();
                        foreach (string publishContribExportSubDir in publishContribExportSubDirs)
                        {
                            websToConvertList.Add(publishContribExportSubDir);
                        }
                        websToConvert = websToConvertList.ToArray();
                    }
                    else
                    {
                        List<string> websToConvertList = new List<string>();
                        foreach (XDoc webDoc in websToConvertDoc["//" + TWikiWebTagName])
                        {
                            string webName = webDoc["@" + WebNameAttributeName].AsText;
                            if (string.IsNullOrEmpty(webName))
                            {
                                ShowErrorMessage(logWriter, "\"{0}\" attribute not specified for \"{1}\" tag.",
                                    WebNameAttributeName, TWikiWebTagName);
                                return;
                            }
                            string webPath = Path.Combine(publishContribFilesPath, webName);
                            if (!Directory.Exists(webPath))
                            {
                                ShowErrorMessage(logWriter, "\"{0}\" web not found in \"{1}\".",
                                    webName, publishContribFilesPath);
                                return;
                            }
                            websToConvertList.Add(webName);
                        }
                        websToConvert = websToConvertList.ToArray();
                    }

                    string tWikiDataDirectory = Path.Combine(tWikiPath, "data");
                    if (!Directory.Exists(tWikiDataDirectory))
                    {
                        ShowErrorMessage(logWriter, "TWiki data directory not found in \"{0}\".",
                            tWikiDataDirectory);
                        return;
                    }

                    string htpasswdFilePath = Path.Combine(tWikiDataDirectory, ".htpasswd");
                    if (!File.Exists(htpasswdFilePath))
                    {
                        ShowMessage(logWriter, "\"{0}\" file not found in \"{1}\". Skip users and groups conversion.", ".htpasswd",
                            htpasswdFilePath);
                        htpasswdFilePath = null;
                    }

                    string tMainWebWikiDataPath = Path.Combine(tWikiDataDirectory, "Main");
                    if (!Directory.Exists(tMainWebWikiDataPath))
                    {
                        ShowMessage(logWriter, "Main web not found in \"{0}\". Skip group convertions.", tMainWebWikiDataPath);
                        tMainWebWikiDataPath = null;
                    }

                    string tWikiPubPath = Path.Combine(tWikiPath, "pub");
                    if (!Directory.Exists(tWikiPubPath))
                    {
                        ShowMessage(logWriter, "TWiki \"pub\" directory not found in \"{0}\". Skip attachment convertions",
                            tWikiPubPath);
                        tWikiPubPath = null;
                    }

                    bool success = TWConverter.Convert(dreamAPIUrl, dekiUserName, dekiUserPassword, publishContribFilesPath,
                        exportPagesPath, tWikiPubPath, htpasswdFilePath, tMainWebWikiDataPath,
                        tWikiDataDirectory, websToConvert, "rsrc", Console.Out, logWriter);

                    if (success)
                    {
                        ShowMessage(logWriter, "Conversion successfully completed!");
                    }
                }
                catch (DreamResponseException e)
                {
                    ShowMessage(logWriter, "An unexpected error has occurred:");
                    ShowMessage(logWriter, e.Response.ToString());
                    ShowMessage(logWriter, e.ToString());
                }
                catch (Exception e)
                {
                    ShowMessage(logWriter, "An unexpected error has occurred:");
                    ShowMessage(logWriter, e.ToString());
                }
                Console.ReadLine();
            }
        }
    }
}
