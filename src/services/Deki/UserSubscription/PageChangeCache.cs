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
using System.Globalization;
using System.Text;

using MindTouch.Deki.Data;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.UserSubscription {
    using Yield = IEnumerator<IYield>;

    public class PageChangeCache {

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly Plug _deki;
        private readonly Action<string, Action> _cacheItemCallback;
        private readonly Dictionary<string, PageChangeCacheData> _cache = new Dictionary<string, PageChangeCacheData>();

        //--- Constructors ---
        public PageChangeCache(Plug deki, TimeSpan ttl) :
            this(deki, (key, clearAction) => TaskTimer.New(ttl, timer => clearAction(), null, TaskEnv.None)) { }

        public PageChangeCache(Plug deki, Action<string, Action> cacheItemCallback) {
            if(deki == null) {
                throw new ArgumentNullException("deki");
            }
            if(cacheItemCallback == null) {
                throw new ArgumentNullException("cacheItemCallback");
            }
           _cacheItemCallback = cacheItemCallback;
            _deki = deki;
        }

        //--- Methods ---
        public Yield GetPageData(uint pageId, string wikiId, DateTime time, CultureInfo culture, string timezone, Result<PageChangeData> result) {

            // Note (arnec): going back 10 seconds before event, because timestamps in a request are not currently synced
            Result<PageChangeCacheData> cacheResult;
            yield return cacheResult = Coroutine.Invoke(GetCache, pageId, wikiId, time, culture, new Result<PageChangeCacheData>());
            PageChangeCacheData cacheData = cacheResult.Value;
            if(cacheData == null) {
                result.Return((PageChangeData)null);
                yield break;
            }
            StringBuilder plainBody = new StringBuilder();
            plainBody.AppendFormat("{0}\r\n[ {1} ]\r\n\r\n", cacheData.Title, cacheData.PageUri);
            XDoc htmlBody = new XDoc("html")
                .Start("p")
                    .Start("b")
                        .Start("a").Attr("href", cacheData.PageUri).Value(cacheData.Title).End()
                    .End()
                    .Value(" ( Last edited by ")
                    .Start("a").Attr("href", cacheData.WhoUri).Value(cacheData.Who).End()
                    .Value(" )")
                    .Elem("br")
                    .Start("small")
                        .Start("a").Attr("href", cacheData.PageUri).Value(cacheData.PageUri).End()
                    .End()
                    .Elem("br")
                    .Start("small")
                        .Start("a").Attr("href", cacheData.UnsubUri).Value("Unsubscribe").End()
                    .End()
                .End()
                .Start("p")
                    .Start("ol");
            string tz = "GMT";
            TimeSpan tzOffset = TimeSpan.Zero;
            if(!string.IsNullOrEmpty(timezone)) {
                tz = timezone;
                string[] parts = timezone.Split(':');
                int hours;
                int minutes;
                int.TryParse(parts[0], out hours);
                int.TryParse(parts[1], out minutes);
                tzOffset = new TimeSpan(hours, minutes, 0);
            }
            foreach(PageChangeCacheData.Item item in cacheData.Items) {
                string t = item.Time.Add(tzOffset).ToString(string.Format("ddd, dd MMM yyyy HH':'mm':'ss '{0}'", tz), culture);
                plainBody.AppendFormat(" - {0} by {1} ({2})\r\n", item.ChangeDetail, item.Who, t);
                plainBody.AppendFormat("   [ {0} ]\r\n", item.RevisionUri);

                htmlBody.Start("li")
                            .Value(item.ChangeDetail)
                            .Value(" ( ")
                            .Start("a").Attr("href", item.RevisionUri).Value(t).End()
                            .Value(" by ")
                            .Start("a").Attr("href", item.WhoUri).Value(item.Who).End()
                            .Value(" )")
                        .End();
                plainBody.Append("\r\n");
            }
            htmlBody
                    .End()
                .End()
                .Elem("br");
            result.Return(new PageChangeData(plainBody.ToString(), htmlBody));
            yield break;
        }

        private Yield GetCache(uint pageId, string wikiId, DateTime time, CultureInfo culture, Result<PageChangeCacheData> result) {

            // Note (arnec): going back 10 seconds before event, because timestamps in a request are not currently synced
            string keytime = time.ToString("yyyyMMddHHmm");
            string since = time.Subtract(TimeSpan.FromSeconds(10)).ToString("yyyyMMddHHmmss");
            PageChangeCacheData cacheData;
            string key = string.Format("{0}:{1}:{2}:{3}", pageId, wikiId, keytime, culture);
            _log.DebugFormat("getting data for key: {0}", key);
            lock(_cache) {
                if(_cache.TryGetValue(key, out cacheData)) {
                    result.Return(cacheData);
                    yield break;
                }
            }

            // fetch the page data
            Result<DreamMessage> pageResponse;
            yield return pageResponse = _deki
                .At("pages", pageId.ToString())
                .WithHeader("X-Deki-Site", "id=" + wikiId)
                .With("redirects", "0").GetAsync();
            if(!pageResponse.Value.IsSuccessful) {
                _log.WarnFormat("Unable to fetch page '{0}' info: {1}", pageId, pageResponse.Value.Status);
                result.Return((PageChangeCacheData)null);
                yield break;
            }
            XDoc page = pageResponse.Value.ToDocument();
            string title = page["title"].AsText;
            XUri pageUri = page["uri.ui"].AsUri;
            string pageUriString = CleanUriForEmail(pageUri);
            string unsubUri = CleanUriForEmail(pageUri
                .WithoutPathQueryFragment()
                .At("index.php")
                .With("title", "Special:PageAlerts")
                .With("id", pageId.ToString()));

            // fetch the revision history
            Result<DreamMessage> feedResponse;
            yield return feedResponse = _deki
                .At("pages", pageId.ToString(), "feed")
                .WithHeader("X-Deki-Site", "id=" + wikiId)
                .With("redirects", "0")
                .With("format", "raw")
                .With("since", since)
                .GetAsync();
            if(!feedResponse.Value.IsSuccessful) {
                _log.WarnFormat("Unable to fetch page '{0}' changes: {1}", pageId, feedResponse.Value.Status);
                result.Return((PageChangeCacheData)null);
                yield break;
            }

            // build the docs
            XDoc feed = feedResponse.Value.ToDocument()["change"];
            if(feed.ListLength == 0) {
                _log.WarnFormat("Change feed is empty for page: {0}", pageId);
                result.Return((PageChangeCacheData)null);
                yield break;
            }
            string who = feed["rc_user_name"].AsText;
            string whoUri = CleanUriForEmail(pageUri.WithoutPathQueryFragment().At(XUri.EncodeSegment("User:" + who)));
            cacheData = new PageChangeCacheData();
            cacheData.Title = title;
            cacheData.PageUri = pageUriString;
            cacheData.Who = who;
            cacheData.WhoUri = whoUri;
            cacheData.UnsubUri = unsubUri;
            foreach(XDoc change in feed.ReverseList()) {
                string changeDetail = change["rc_comment"].AsText;
                string revisionUri = CleanUriForEmail(pageUri.With("revision", change["rc_revision"].AsText));
                who = change["rc_user_name"].AsText;
                whoUri = CleanUriForEmail(pageUri.WithoutPathQueryFragment().At(XUri.EncodeSegment("User:" + who)));
                PageChangeCacheData.Item item = new PageChangeCacheData.Item();
                item.Who = who;
                item.WhoUri = whoUri;
                item.RevisionUri = revisionUri;
                item.ChangeDetail = changeDetail;
                item.Time = DbUtils.ToDateTime(change["rc_timestamp"].AsText);
                cacheData.Items.Add(item);
            }
            lock(_cache) {

                // even though we override the entry if one was created in the meantime
                // we do the existence check so that we don't set up two expiration timers;
                if(!_cache.ContainsKey(key)) {
                    _cacheItemCallback(key, () => {
                        lock(_cache) {
                            _cache.Remove(key);
                        }
                    });
                }
                _cache[key] = cacheData;
            }
            result.Return(cacheData);
            yield break;
        }

        private string CleanUriForEmail(XUri uri) {
            uri = uri.AsPublicUri();
            string schemehostport = uri.SchemeHostPort;
            string pathQueryFragment = uri.PathQueryFragment;
            return schemehostport + pathQueryFragment.Replace(":", "%3A");
        }
    }
}
