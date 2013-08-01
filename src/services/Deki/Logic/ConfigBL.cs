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
using System.Reflection;
using MindTouch.Deki.Exceptions;
using MindTouch.Dream;
using MindTouch.Deki.Data;
using MindTouch.Xml;

namespace MindTouch.Deki.Logic {
    public class SiteSettingsRetrievalSettings {
        public bool IncludeHidden { get; set; }
        public bool IncludeAnonymousUser { get; set; }
        public bool IncludeLicense { get; set; }
    }

    public class DekiInstanceSettings : IInstanceSettings {

        //--- Properties ---
        public XDoc License { get { return DekiContext.Current.LicenseManager.LicenseDocumentOrEmpty; } }

        //--- Methods ---
        public string GetValue(string key, string def) {
            return ConfigBL.GetInstanceSettingsValue(key, def);
        }

        public T GetValue<T>(string key, T def) {
            return ConfigBL.GetInstanceSettingsValueAs<T>(key, def);
        }

        public XDoc GetAsDoc() {
            return ConfigBL.GetInstanceSettingsAsDoc(false);
        }

        public bool IsInitialized() {
            return DekiContext.Current.Instance.Status == DekiInstanceStatus.RUNNING;
        }

        public void ClearConfigCache() {
            ConfigBL.ClearConfigCache();
        }
    }

    public static class ConfigBL {

        //--- Constants ---
        public const string IS_CLOUD = "instance/mode";
        public const string UI_LOGO_UPLOADED = "ui/logo-uploaded";
        public const string UI_LOGO_URI = "ui/logo-uri";
        public const string SECURITY_APIKEY = "security/api-key";
        public const string LICENSE_EXPIRATION = "license/expiration";
        public const string LICENSE_STATE = "license/state";
        public const string LICENSE_PRODUCTKEY = "license/productkey";
        public const string LICENSE = "license";
        public const string ANONYMOUS_USER = "anonymous-user";
        public const string READONLY_SUFFIX = "/@readonly";
        public const string HIDDEN_SUFFIX = "/@hidden";
        public const string TEXT_SUFFIX = "/#text";
        public const string MAX_SITEMAP_SIZE_KEY = "pages/max-sitemap-size";
        public const int MAX_SITEMAP_SIZE = 50000;
        private const string CACHE_SETTINGS = "SETTINGS";
        private const string CACHE_SETTINGSDOC = "SETTINGSDOC";
        private const string CACHE_SETTINGSDOC_WITHHIDDEN = "SETTINGSDOCHIDDEN";

        private static readonly string[] SETTINGS_REQUIREDKEYS = new[] { "storage/type", "ui/language" };

        //--- Class Fields ---
        private static readonly log4net.ILog _log = DekiLogManager.CreateLog();

        //--- Class Methods ---
        public static void SetInstanceSettings(XDoc doc) {

            // Remove elements that contain no child elements or attributes
            Utils.RemoveEmptyNodes(doc);
            doc["version"].Remove();
            doc[ConfigBL.ANONYMOUS_USER].Remove();

            // convert document to ConfigValue dictionary
            Dictionary<string, ConfigValue> config = new Dictionary<string, ConfigValue>();
            foreach(KeyValuePair<string, string> entry in doc.ToKeyValuePairs()) {
                if(StringUtil.EndsWithInvariant(entry.Key, TEXT_SUFFIX) || StringUtil.EndsWithInvariant(entry.Key, READONLY_SUFFIX) || StringUtil.EndsWithInvariant(entry.Key, HIDDEN_SUFFIX)) {
                    continue;
                }
                if(config.ContainsKey(entry.Key)) {
                    throw new ConfigUpdateDupeSettingException(entry.Key);
                }
                config.Add(entry.Key, new ConfigValue(entry.Value));
            }

            //TODO MaxM: This will currently overwrite db settings that are overriden with the overridden value rather than leaving it alone.
            //OverrideSettings
            //for every key in given doc. if value is readonly in 
            //GetInstanceSettings()[key]
            //then use value from ConfigDA.ReadInstanceSettings()[key]

            SetInstanceSettings(config);
        }

        public static void SetInstanceSettings(Dictionary<string, ConfigValue> settings) {

            // check that required fields are present
            foreach(string requiredKey in SETTINGS_REQUIREDKEYS) {
                if(!settings.ContainsKey(requiredKey)) {
                    throw new ConfigMissingRequiredKeyInvalidArgumentException(requiredKey);
                }
            }

            // override global settings (i.e. static instance settings and system-wide settings)
            OverrideSettings(settings);

            // filter out settings that do not need to be saved
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
            foreach(KeyValuePair<string, ConfigValue> entry in settings) {

                //Hidden entries are not saved in the db.
                if(entry.Value.IsHidden) {
                    continue;
                }

                //Add auto-generated values here that should not be saved to the db!
                switch(entry.Key.ToLowerInvariant()) {
                case UI_LOGO_URI:
                case LICENSE_EXPIRATION:
                case LICENSE_STATE:
                    continue;
                }

                // add element
                result.Add(new KeyValuePair<string, string>(entry.Key, entry.Value.Value));
            }

            // check that we have enough settings
            if(settings.Count < 2) {
                throw new ConfigUpdateConfigSettingsInvalidArgumentException();
            }

            // update settings 
            lock(DekiContext.Current.Instance.SettingsSyncRoot) {
                ClearConfigCache();
                DbUtils.CurrentSession.Config_WriteInstanceSettings(result);
            }
            DekiContext.Current.Instance.SettingsLastModified = DateTime.UtcNow;
        }

        public static Dictionary<string, ConfigValue> GetInstanceSettings() {
            Dictionary<string, ConfigValue> result = DekiContext.Current.Instance.Cache.Get<Dictionary<string, ConfigValue>>(CACHE_SETTINGS, null);
            if(result != null) {
                return result;
            }
            lock(DekiContext.Current.Instance.SettingsSyncRoot) {
                result = DekiContext.Current.Instance.Cache.Get<Dictionary<string, ConfigValue>>(CACHE_SETTINGS, null);
                if(result == null) {
                    result = new Dictionary<string, ConfigValue>();

                    //Hidden settings are never entered into the db. This will filter them out.
                    foreach(KeyValuePair<string, ConfigValue> setting in DbUtils.CurrentSession.Config_ReadInstanceSettings()) {
                        if(StringUtil.EndsWithInvariant(setting.Key, HIDDEN_SUFFIX))
                            continue;

                        result[setting.Key] = setting.Value;
                    }
                    OverrideSettings(result);

                    DekiContext.Current.Instance.Cache.Set(CACHE_SETTINGS, result, DateTime.UtcNow.AddSeconds(60));
                }
            }
            return result;
        }

        public static string GetInstanceSettingsValue(string key, string def) {
            Dictionary<string, ConfigValue> config = GetInstanceSettings();
            lock(config) {
                ConfigValue valueWrapper;
                if(config.TryGetValue(key, out valueWrapper)) {
                    if(!string.IsNullOrEmpty(valueWrapper.Value))
                        return valueWrapper.Value;
                }
                return def;
            }
        }

        public static T GetInstanceSettingsValueAs<T>(string key, T def) {
            var value = GetInstanceSettingsValue(key, null);
            if(value == null) {
                return def;
            }
            try {
                return SysUtil.ChangeType<T>(value);
            } catch(Exception e) {
                _log.WarnExceptionFormat(e, "Unable to convert configuration setting '{0}' to type '{1}'", key, typeof(T));
                return def;
            }
        }

        public static T? GetInstanceSettingsValueAs<T>(string key) where T : struct {
            string value = GetInstanceSettingsValue(key, null);
            if(value == null) {
                return null;
            }
            try {
                return SysUtil.ChangeType<T>(value);
            } catch(Exception e) {
                _log.WarnExceptionFormat(e, "Unexpected format of configuration setting '{0}'", key);
                return null;
            }
        }

        public static void SetInstanceSettingsValue(string key, string value) {
            Dictionary<string, ConfigValue> config = CopyInstanceSettings();
            ConfigValue current;
            if(!config.TryGetValue(key, out current) || (current.Value != value)) {
                config[key] = new ConfigValue(value);
                SetInstanceSettings(config);
            }
        }

        public static void DeleteInstanceSettingsValue(string key) {
            Dictionary<string, ConfigValue> config = CopyInstanceSettings();
            bool found = false;
            foreach(string existingKey in new List<string>(config.Keys)) {
                if((existingKey == key) || (StringUtil.StartsWithInvariant(existingKey, key) && (existingKey[key.Length] == '/'))) {
                    config.Remove(existingKey);
                    found = true;
                }
            }
            if(found) {
                SetInstanceSettings(config);
            }
        }

        private static Dictionary<string, ConfigValue> CopyInstanceSettings() {
            Dictionary<string, ConfigValue> config = GetInstanceSettings();
            lock(config) {
                Dictionary<string, ConfigValue> result = new Dictionary<string, ConfigValue>();
                foreach(KeyValuePair<string, ConfigValue> entry in config) {
                    result.Add(entry.Key, entry.Value);
                }
                return result;
            }
        }

        private static void OverrideSettings(Dictionary<string, ConfigValue> instanceSettings) {
            var context = DekiContext.Current;
            List<KeyValuePair<string, string>> readOnlySettings = new List<KeyValuePair<string, string>>();

            // collect read-only settings that are instance specific
            if(!DekiContext.Current.Instance.Config.IsEmpty) {
                readOnlySettings.AddRange(DekiContext.Current.Instance.Config.ToKeyValuePairs());
            }

            // collect read-only settings that are system-wide
            var instanceManager = context.Deki.Instancemanager;
            XDoc globalconfig = instanceManager.GetGlobalConfig();
            if(globalconfig != null && !globalconfig.IsEmpty) {
                readOnlySettings.AddRange(globalconfig.ToKeyValuePairs());
            }
            readOnlySettings.AddRange(instanceManager.GetGlobalServices());

            //Add custom computed readonly settings here

            //Current license status
            var license = DekiContext.Current.LicenseManager;
            readOnlySettings.Add(new KeyValuePair<string, string>(LICENSE_STATE, license.LicenseState.ToString()));
            if(license.LicenseExpiration != DateTime.MaxValue) {
                readOnlySettings.Add(new KeyValuePair<string, string>(LICENSE_EXPIRATION, license.LicenseExpiration.ToString(XDoc.RFC_DATETIME_FORMAT)));
            }
            string productKey = LicenseBL.Instance.BuildProductKey();
            if(!string.IsNullOrEmpty(productKey)) {
                readOnlySettings.Add(new KeyValuePair<string, string>(LICENSE_PRODUCTKEY, productKey));
            }

            List<string> hiddenKeys = new List<string>();

            // mark all 'readonly' settings as read-only
            foreach(KeyValuePair<string, string> setting in readOnlySettings) {

                // strip apikey from overrides
                if(setting.Key.EqualsInvariant(SECURITY_APIKEY)) {
                    continue;
                }
                // skip elements which have a @hidden attribute
                if(setting.Key.EndsWithInvariant(HIDDEN_SUFFIX)) {
                    hiddenKeys.Add(setting.Key.Substring(0, setting.Key.Length - HIDDEN_SUFFIX.Length));
                    continue;
                }

                //For backwards compatibility to older style config xml's (without the <wikis> element).
                if(setting.Key.StartsWithInvariant("indexer/"))
                    continue;

                instanceSettings[setting.Key] = new ConfigValue(setting.Value, true, false);
            }

            // strip any values that should never come from stored configuration
            instanceSettings.Remove(SECURITY_APIKEY);
            instanceSettings.Remove(IS_CLOUD);

            // if instance has apikey, add it
            if(!string.IsNullOrEmpty(context.Instance.ApiKey)) {
                instanceSettings[SECURITY_APIKEY] = new ConfigValue(context.Instance.ApiKey, true, true);
            }

            // if instance is has a cloud manager, mark it
            instanceSettings[IS_CLOUD] = new ConfigValue(instanceManager.IsCloudManager ? "cloud" : "onpremise", true, true);
            if (!instanceSettings.ContainsKey(MAX_SITEMAP_SIZE_KEY)) {
                instanceSettings[MAX_SITEMAP_SIZE_KEY] = new ConfigValue(MAX_SITEMAP_SIZE.ToString(), true, true);
            }
            foreach(string hiddenKey in hiddenKeys) {
                ConfigValue cv;
                if(instanceSettings.TryGetValue(hiddenKey, out cv))
                    cv.IsHidden = true;
            }

        }

        public static XDoc GetInstanceSettingsAsDoc(bool includeHidden) {
            return GetInstanceSettingsAsDoc(new SiteSettingsRetrievalSettings { IncludeHidden = includeHidden });
        }

        public static XDoc GetInstanceSettingsAsDoc(SiteSettingsRetrievalSettings retrieve) {
            var instance = DekiContext.Current.Instance;
            string cachekey = retrieve.IncludeHidden ? CACHE_SETTINGSDOC_WITHHIDDEN : CACHE_SETTINGSDOC;
            XDoc result = instance.Cache.Get<XDoc>(cachekey, null);
            if(result == null) {
                Dictionary<string, ConfigValue> config = GetInstanceSettings();
                List<KeyValuePair<string, string>> items = new List<KeyValuePair<string, string>>();
                lock(config) {
                    foreach(KeyValuePair<string, ConfigValue> entry in config) {
                        if(entry.Value.IsHidden && !retrieve.IncludeHidden) {
                            continue;
                        }

                        // check if overwritten setting was an element
                        int index = entry.Key.LastIndexOf('/');
                        bool isElement = ((index + 1) < entry.Key.Length) && (entry.Key[index + 1] != '@');
                        items.Add(new KeyValuePair<string, string>(entry.Key, entry.Value.Value));
                        if(isElement) {
                            if(entry.Value.IsReadOnly) {

                                // we need to add a 'readonly' attribute
                                items.Add(new KeyValuePair<string, string>(entry.Key + READONLY_SUFFIX, "true"));
                            }
                            if(entry.Value.IsHidden) {

                                // we need to add a 'hidden' attribute
                                items.Add(new KeyValuePair<string, string>(entry.Key + HIDDEN_SUFFIX, "true"));
                            }
                        }
                    }
                }

                //Ensure that attributes are after their associated elements to ensure that the #text and the @attribute are part of the same element rather than creating a new element with just the #text
                //after the attribute. Consider moving this to Dream XDocFactory.From
                items.Sort((left, right) => StringUtil.CompareInvariant(left.Key, right.Key));
                result = XDocFactory.From(items, "config");
                var u = DbUtils.CurrentSession.Users_GetByName(UserBL.ANON_USERNAME);
                var anonDoc = UserBL.GetUserXmlVerbose(u, null, Utils.ShowPrivateUserInfo(u), true, true);
                result.InsertValueAt(ANONYMOUS_USER, anonDoc.ToJson());

                // Add license information
                var licenseManager = DekiContext.Current.LicenseManager;
                var licenseDoc = licenseManager.GetLicenseDocument(false).Clone();
                licenseDoc.Elem("type", licenseDoc["@type"].Contents);
                _log.Debug("Added private information.");
                licenseDoc["support-agreement"].Remove();
                licenseDoc["source-license"].Remove();
                licenseDoc["license.public"].Remove();
                licenseDoc["grants/service-license"].RemoveAll();
                var now = DateTime.UtcNow;
                foreach(var capability in licenseDoc["grants/*[@date.expire]"]) {
                    if(capability["@date.expire"].AsDate < now) {
                        capability.Remove();
                    }
                }
                XDoc originalLicenseElems = result["license"];
                XDoc clonedOriginalLicenseElems = XDoc.Empty;
                if(!originalLicenseElems.IsEmpty) {
                    clonedOriginalLicenseElems = originalLicenseElems.Clone();
                    originalLicenseElems.Remove();
                    licenseDoc.AddNodes(clonedOriginalLicenseElems);
                }
                result.Start(LICENSE).AddNodes(licenseDoc).End();
                AddVersionInfo(result);
                instance.Cache.Set(cachekey, result.Clone(), DateTime.UtcNow.AddSeconds(60));
            } else {

                // TODO: remove the clone once cached settings come out of IKeyValueCache (i.e. are serialized)

                // NOTE(cesarn): We need to clone it because we are stripping and adding
                // elements depending on permissions and what is requested, but we do not 
                // want to change the cached version that contains everything.
                result = result.Clone();
            }
            if(!retrieve.IncludeAnonymousUser) {
                result[ANONYMOUS_USER].Remove();
                _log.Debug("Removing anonymous's user information.");
            }
            if(!retrieve.IncludeLicense) {
                var mandatoryLicenseInfo = new XDoc(LICENSE).Start("state").Attr("readonly", "true").Value(result[LICENSE_STATE].Contents).End();
                if(DekiContext.Current.LicenseManager.LicenseExpiration != DateTime.MaxValue) {
                    mandatoryLicenseInfo.Start("expiration").Attr("readonly", "true").Value(result[LICENSE_EXPIRATION].Contents).End();
                }
                var productKey = LicenseBL.Instance.BuildProductKey();
                if(!String.IsNullOrEmpty(productKey)) {
                    mandatoryLicenseInfo.Start("productkey").Attr("readonly", "true").Value(result[LICENSE_PRODUCTKEY].Contents).End();
                }
                result[LICENSE].Remove();
                _log.Debug("Removing private information elements");
                result.Start(LICENSE).AddNodes(mandatoryLicenseInfo).End();
            }
            result.InsertValueAt("api/@href", DekiContext.Current.Deki.Self.Uri.AsPublicUri().ToString());
            return result;
        }

        private static void AddVersionInfo(XDoc settings) {
            var deki = typeof(DekiWikiService).Assembly;
            var version = deki.GetName().Version;
            settings.Start("version")
                .Attr("readonly",true)
                .Elem("major", version.Major)
                .Elem("minor", version.Minor)
                .Elem("build", version.Build)
                .Elem("revision", version.Revision)
                .Elem("text", version.ToString())
                .Elem("date", deki.GetBuildDate());
            var svnRevision = deki.GetAttribute<SvnRevisionAttribute>();
            if(svnRevision != null) {
                settings.Elem("svnrevision", svnRevision.Revision);
            }
            var svnBranch = deki.GetAttribute<SvnBranchAttribute>();
            if(svnBranch != null) {
                settings.Elem("svnbranch", svnBranch.Branch);
            }
            var gitRevision = deki.GetAttribute<GitRevisionAttribute>();
            if(gitRevision != null) {
                settings.Elem("gitrevision", gitRevision.Revision);
            }
            var gitBranch = deki.GetAttribute<GitBranchAttribute>();
            if(gitBranch != null) {
                settings.Elem("gitbranch", gitBranch.Branch);
            }
            var gitUri = deki.GetAttribute<GitUriAttribute>();
            if(gitUri != null) {
                settings.Elem("gituri", gitUri.Uri);
            }
            settings.End();
        }

        public static void ClearConfigCache() {
            var cache = DekiContext.Current.Instance.Cache;
            cache.Delete(CACHE_SETTINGS);
            cache.Delete(CACHE_SETTINGSDOC);
            cache.Delete(CACHE_SETTINGSDOC_WITHHIDDEN);
        }
    }
}
