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
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Autofac;
using Autofac.Builder;
using log4net;
using MindTouch.Data;
using MindTouch.Deki.Data.UserSubscription;
using MindTouch.Deki.UserSubscription;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Deki {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Change Subscription Service", "Copyright (c) 2006-2010 MindTouch Inc.",
       Info = "http://developer.mindtouch.com/Deki/Services/DekiChangeSubscription",
       SID = new[] { "sid://mindtouch.com/deki/2008/11/changesubscription" }
    )]
    public class DekiChangeSubscriptionService : DreamService {

        //--- Types ---
        private class UserException : Exception {

            //--- Constructors ---
            public UserException(string message) : base(message) { }
        }

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly Dictionary<string, IPageSubscriptionInstance> _instanceSubscriptions = new Dictionary<string, IPageSubscriptionInstance>();
        private Plug _emailer;
        private Plug _deki;
        private Plug _subscriptionLocation;
        private string _apikey;
        private PlainTextResourceManager _resourceManager;
        private NotificationDelayQueue _notificationQueue;
        private PageChangeCache _cache;

        //--- Features ---
        [DreamFeature("POST:pages/{pageid}", "Subscribe to a resource")]
        [DreamFeatureParam("depth", "string?", "0 for specific page, 'infinity' for sub-tree subscription. Defaults to 0")]
        public Yield SubscribeToChange(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var wikiId = GetWikiIdFromRequest(request);
            var pageId = context.GetParam<uint>("pageid");
            var depth = context.GetParam("depth", "0") == "0" ? false : true;
            Result<PageSubscriptionUser> userResult;
            yield return userResult = Coroutine.Invoke(GetRequestUser, request, new Result<PageSubscriptionUser>()).Catch();
            if(userResult.HasException) {
                ReturnUserError(userResult.Exception, response);
                yield break;
            }
            var userInfo = userResult.Value;
            DreamMessage pageAuth = null;
            yield return _deki
                .At("pages", pageId.ToString(), "allowed")
                .With("permissions", "read,subscribe")
                .WithHeaders(request.Headers)
                .Post(new XDoc("users").Start("user").Attr("id", userInfo.Id).End(), new Result<DreamMessage>())
                .Set(x => pageAuth = x);
            if(!pageAuth.IsSuccessful || pageAuth.ToDocument()["user/@id"].AsText != userInfo.Id.ToString()) {
                throw new DreamForbiddenException("User not permitted to subscribe to page");
            }
            var dataSession = GetDataSession(wikiId);
            dataSession.Subscribe(userInfo.Id, pageId, depth);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("DELETE:pages/{pageid}", "Unsubscribe from a resource")]
        public Yield UnsubscribeFromChange(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var wikiId = GetWikiIdFromRequest(request);
            var pageId = context.GetParam<uint>("pageid");
            Result<PageSubscriptionUser> userResult;
            yield return userResult = Coroutine.Invoke(GetRequestUser, request, new Result<PageSubscriptionUser>()).Catch();
            if(userResult.HasException) {
                ReturnUserError(userResult.Exception, response);
                yield break;
            }
            var userInfo = userResult.Value;
            var dataSession = GetDataSession(wikiId);
            dataSession.UnsubscribeUser(userInfo.Id, pageId);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("GET:subscribers/{userid}", "Retrieve page subscriptions for a user (user id or current)")]
        [DreamFeature("GET:subscriptions", "Retrieve page subscriptions for the current user (does not check authorization")]
        [DreamFeatureParam("pages", "string?", "A comma separated list of the pages to check. If omitted, returns all subscriptions")]
        public Yield GetSubscriptions(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var wikiId = GetWikiIdFromRequest(request);
            var user = context.GetParam("userid", string.Empty);
            Result<PageSubscriptionUser> userResult;
            if (!string.IsNullOrEmpty(user) && user != "current") {
                uint userId;
                try {
                    userId = Convert.ToUInt32(user);
                }
                catch {
                    throw new DreamBadRequestException(string.Format("'{0}' is an invalid user id", user));
                }
                yield return userResult = Coroutine.Invoke(GetUser, userId, wikiId, new Result<PageSubscriptionUser>())
                    .Catch();
            }
            else {
                yield return userResult = Coroutine.Invoke(GetRequestUser, request, new Result<PageSubscriptionUser>())
                    .Catch();
            }
            if(userResult.HasException) {
                ReturnUserError(userResult.Exception, response);
                yield break;
            }
            var userInfo = userResult.Value;
            var pages = new List<uint>();
            var pageList = context.GetParam("pages", "");
            var subscribedPages = 0;
            if(!string.IsNullOrEmpty(pageList)) {
                foreach(var pageId in pageList.Split(',')) {
                    uint id;
                    if(uint.TryParse(pageId, out id)) {
                        subscribedPages++;
                        pages.Add(id);
                    }
                }
            }
            _log.DebugFormat("found {0} subscribed pages for request hierarchy", subscribedPages);
            var dataSession = GetDataSession(wikiId);
            var subscriptions = dataSession.GetSubscriptionsForUser(userInfo.Id, pages);
            var subscriptionDoc = new XDoc("subscriptions");
            foreach(var tuple in subscriptions) {
                subscriptionDoc.Start("subscription.page").Attr("id", tuple.PageId).Attr("depth", tuple.IncludeChildPages ? "infinity" : "0").End();
            }
            response.Return(DreamMessage.Ok(subscriptionDoc));
            yield break;
        }

        [DreamFeature("GET:subscriptions/{pageid}", "Check whether a specific page is subscribed directly or undirectly and authorized")]
        [DreamFeatureParam("pageid", "uint", "page id to check")]
        public Yield GetSubscription(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var wikiId = GetWikiIdFromRequest(request);
            Result<PageSubscriptionUser> userResult;
            yield return userResult = Coroutine.Invoke(GetRequestUser, request, new Result<PageSubscriptionUser>()).Catch();
            if(userResult.HasException) {
                ReturnUserError(userResult.Exception, response);
                yield break;
            }
            var userInfo = userResult.Value;
            var currentPageId = context.GetParam<uint>("pageid");
            XDoc pageDoc = null;
            yield return _deki
                .At("pages", currentPageId.ToString())
                .WithHeaders(request.Headers)
                .Get(new Result<XDoc>())
                .Set(x => pageDoc = x);
            var perms = (pageDoc["security/permissions.effective/operations"].AsText ?? "").Split(',');
            var canSubscribe = false;
            foreach(var perm in perms) {
                if("SUBSCRIBE".EqualsInvariantIgnoreCase(perm.Trim())) {
                    canSubscribe = true;
                }
            }
            if(!canSubscribe) {
                response.Return(DreamMessage.Forbidden(string.Format("User is not authorized to subscribe to page {0}.", currentPageId)));
                yield break;
            }
            var pages = GetPageList(currentPageId, pageDoc["page.parent"]);
            var dataSession = GetDataSession(wikiId);
            var subscriptions = dataSession.GetSubscriptionsForUser(userInfo.Id, pages)
                .Where(x => x.PageId == currentPageId || x.IncludeChildPages);
            var subscriptionDoc = new XDoc("subscriptions");
            foreach(var tuple in subscriptions) {
                subscriptionDoc.Start("subscription.page").Attr("id", tuple.PageId).Attr("depth", tuple.IncludeChildPages ? "infinity" : "0").End();
            }
            response.Return(DreamMessage.Ok(subscriptionDoc));
        }

        [DreamFeature("POST:updateuser", "Update user (user pubsub endpoint)")]
        internal Yield UpdateUser(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var ev = request.ToDocument();
            var channel = ev["channel"].AsUri;
            var action = channel.Segments[2];
            var userId = ev["userid"].AsUInt;
            if(userId.HasValue) {
                var wikiId = ev["@wikiid"].AsText;
                var instance = GetInstanceInfo(wikiId);
                var userInfo = instance.GetUserInfo(userId.Value);
                if(action.EqualsInvariantIgnoreCase("delete")) {

                    // user deletion event, wipe all subscriptions
                    var dataSession = GetDataSession(instance);
                    dataSession.UnsubscribeUser(userId.Value, null);
                } else {
                    userInfo.Invalidate();
                }
            } else {
                _log.WarnFormat("update user event didn't have a user id: {0}", ev.ToString());
            }
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("POST:notify", "receive a notification to be distributed to users")]
        internal Yield Notify(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var ev = request.ToDocument();
            var wikiid = ev["@wikiid"].AsText;
            var pageId = ev["pageid"].AsUInt ?? 0;
            var eventDate = ev["@event-time"].AsDate ?? DateTime.MinValue;
            var channel = ev["channel"].AsUri;
            var action = channel.Segments[2];
            if(pageId == 0 || eventDate == DateTime.MinValue) {
                _log.DebugFormat("unable to get a pageId or event-time out of the event on channel '{0}'", channel);
                response.Return(DreamMessage.Ok());
                yield break;
            }
            var instance = GetInstanceInfo(wikiid);
            var dataSession = GetDataSession(instance);
            if(action.EqualsInvariantIgnoreCase("delete")) {
                _log.DebugFormat("page was deleted, can't notify on deletes at this time", channel);
                dataSession.UnsubscribePage(pageId);
                response.Return(DreamMessage.Ok());
                yield break;
            }
            DreamMessage msg = null;
            yield return _deki.With("apikey", _apikey)
                .WithHeader("X-Deki-Site", "id=" + wikiid)
                .At("pages", pageId.ToString())
                .Get(new Result<DreamMessage>())
                .Set(x => msg = x);
            if(!msg.IsSuccessful) {
                _log.DebugFormat("unable to fetch page {0} - {1}:\r\n{2}", pageId, msg.Status, msg.HasDocument ? msg.ToDocument().ToPrettyString() : "");
                response.Return(DreamMessage.Ok());
                yield break;
            }
            var pageDoc = msg.ToDocument();
            var pages = GetPageList(pageId, pageDoc["page.parent"]);
            var eventUserId = ev["user/@id"].AsUInt ?? 0;
            var subscriptions = dataSession.GetSubscriptionsForPages(pages);
            if(!subscriptions.Any()) {

                // no one subscribed to that page
                response.Return(DreamMessage.Ok());
                yield break;
            }
            _log.DebugFormat("queueing notifications for channel '{0}' and page '{1}'", channel, pageId);
            var authDoc = new XDoc("users");
            foreach(var sub in subscriptions.Where(x => x.PageId == pageId || x.IncludeChildPages)) {
                authDoc.Start("user").Attr("id", sub.UserId).End();
            }
            DreamMessage pageAuth = null;
            yield return _deki.With("apikey", _apikey)
                .At("pages", pageId.ToString(), "allowed")
                .With("permissions", "read,subscribe")
                .WithHeader("X-Deki-Site", "id=" + wikiid)
                .Post(authDoc, new Result<DreamMessage>())
                .Set(x => pageAuth = x);
            if(!pageAuth.IsSuccessful) {
                _log.WarnFormat("unable to get page authorizations from instance '{0}'", wikiid);
                response.Return(DreamMessage.Ok());
                yield break;
            }
            var authorizedUsers = pageAuth.ToDocument()["user/@id"].Select(x => x.AsUInt ?? 0).ToArray();

            foreach(var userId in authorizedUsers) {
                if(userId == eventUserId) {
                    _log.DebugFormat("Not delivering to user {0} since the user generated the event", userId);
                    continue;
                }
                var userInfo = instance.GetUserInfo(userId);
                if(!userInfo.IsValid) {
                    DreamMessage userMsg = null;
                    yield return _deki.At("users", userId.ToString())
                        .With("apikey", _apikey)
                        .WithHeader("X-Deki-Site", "id=" + wikiid)
                        .Get(new Result<DreamMessage>())
                        .Set(x => userMsg = x);
                    if(!userMsg.IsSuccessful) {
                        _log.DebugFormat("unable to fetch user document for user {0}, skipping notification delivery for user", userId);
                        continue;
                    }
                    XDoc userDoc = userMsg.ToDocument();
                    try {
                        PopulateUser(userInfo, userDoc);
                    } catch(UserException e) {
                        _log.DebugFormat("not delivering to user {0}: {1}", userInfo.Id, e.Message);
                        continue;
                    }
                }
                _log.DebugFormat("queueing userid {0}", userInfo.Id);
                _notificationQueue.Enqueue(wikiid, userInfo.Id, pageId, eventDate);
            }
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("POST:__sendnotification", "Internal only endpoint")]
        private Yield SendEmail(DreamContext context, DreamMessage request, Result<DreamMessage> result) {
            var updateRecord = NotificationUpdateRecord.FromDocument(request.ToDocument());
            _log.DebugFormat("trying to dispatch email to user {0} for wiki '{1}'", updateRecord.UserId, updateRecord.WikiId);
            var instance = GetInstanceInfo(updateRecord.WikiId);
            if(!instance.IsValid) {
                _log.WarnFormat("unable to get required data from site settings, cannot send email. Missing either ui/sitename or page-subscription/from-address ");
                result.Return(DreamMessage.Ok());
                yield break;
            }
            var userInfo = instance.GetUserInfo(updateRecord.UserId);
            if(!userInfo.IsValid) {

                // need to refetch user info
                DreamMessage userMsg = null;
                yield return _deki.At("users", updateRecord.UserId.ToString())
                    .With("apikey", _apikey)
                    .WithCookieJar(Cookies)
                    .WithHeader("X-Deki-Site", "id=" + updateRecord.WikiId)
                    .Get(new Result<DreamMessage>())
                    .Set(x => userMsg = x);
                if(!userMsg.IsSuccessful) {
                    _log.DebugFormat("unable to fetch user {0}, skipping delivery: {1}", updateRecord.UserId, userMsg.Status);
                    result.Return(DreamMessage.Ok());
                    yield break;
                }
                var userDoc = userMsg.ToDocument();
                try {
                    PopulateUser(userInfo, userDoc);
                } catch(UserException e) {
                    _log.DebugFormat("unable to populate user {0}, skipping delivery: {1}", updateRecord.UserId, e.Message);
                    result.Return(DreamMessage.Ok());
                    yield break;
                }
            }
            var culture = userInfo.Culture.GetNonNeutralCulture(instance.Culture);
            var subject = string.Format("[{0}] {1}", instance.Sitename, _resourceManager.GetString("Notification.Page.email-subject", culture, "Site Modified"));
            var emailAddress = (!instance.UseShortEmailAddress && !string.IsNullOrEmpty(userInfo.Username))
                ? new MailAddress(userInfo.Email, userInfo.Username).ToString()
                : userInfo.Email;
            var email = new XDoc("email")
                .Attr("configuration", instance.WikiId)
                .Elem("to", emailAddress)
                .Elem("from", instance.EmailFromAddress)
                .Elem("subject", subject)
                .Start("pages");
            var header = _resourceManager.GetString("Notification.Page.email-header", culture, "The following pages have changed:");
            var plainBody = new StringBuilder();
            plainBody.AppendFormat("{0}\r\n\r\n", header);
            var htmlBody = new XDoc("body")
                .Attr("html", true)
                .Elem("h2", header);
            foreach(Tuplet<uint, DateTime> record in updateRecord.Pages) {
                var pageId = record.Item1;
                email.Elem("pageid", pageId);
                PageChangeData data = null;
                var timezone = userInfo.Timezone.IfNullOrEmpty(instance.Timezone);
                yield return Coroutine.Invoke(_cache.GetPageData, pageId, instance.WikiId, record.Item2, culture, timezone, new Result<PageChangeData>()).Set(x => data = x);
                if(data == null) {
                    _log.WarnFormat("Unable to fetch page change data for page {0}", pageId);
                    continue;
                }
                htmlBody.AddAll(data.HtmlBody.Elements);
                plainBody.Append(data.PlainTextBody);
            }
            email.End();
            if(!instance.EmailFormat.EqualsInvariantIgnoreCase("html")) {
                email.Elem("body", plainBody.ToString());
            }
            if(!instance.EmailFormat.EqualsInvariantIgnoreCase("plaintext")) {
                email.Add(htmlBody);
            }
            _log.DebugFormat("dispatching email for user '{0}'", userInfo.Id);
            yield return _emailer.WithCookieJar(Cookies).PostAsync(email).Catch();
            result.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("GET:pages/{pageid}/subscribers", "Get users subscribed to a resource")]
        [DreamFeatureParam("pageid", "uint", "page id to check")]
        public Yield GetSubscribedUsers(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var wikiId = GetWikiIdFromRequest(request);
            var pageId = context.GetParam<uint>("pageid");
            DreamMessage pageMsg = null;
            yield return _deki.At("pages", pageId.ToString()).WithHeaders(request.Headers).Get(new Result<DreamMessage>())
                .Set(x => pageMsg = x);
            if(!pageMsg.IsSuccessful) {
                response.Return(pageMsg);
            }
            var pages = GetPageList(pageId, pageMsg.ToDocument()["page.parent"]);
            var subscriptions = GetDataSession(wikiId).GetSubscriptionsForPages(pages);
            var userIds =
                (from sub in subscriptions.Where(x => x.PageId == pageId || x.IncludeChildPages) select sub.UserId).
                    Distinct();
            var userDoc = new XDoc("subscribers");
            foreach(var userId in userIds) {
                userDoc.Start("subscriber")
                    .Attr("id", userId)
                    .Attr("href", _deki.At("users", userId.ToString()).Uri)
                    .End();
            }
            response.Return(DreamMessage.Ok(userDoc));
            yield break;
        }

        [DreamFeature("PUT:pages/{pageid}/subscribers/{userid}", "Subscribe a user to a resource")]
        [DreamFeatureParam("depth", "string?", "0 for specific page, 'infinity' for sub-tree subscription. Defaults to 0")]
        public Yield SubscribeUserToChange(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            Result<PageSubscriptionUser> userResult;
            yield return userResult = Coroutine.Invoke(GetRequestUser, request, new Result<PageSubscriptionUser>()).Catch();
            if(userResult.HasException) {
                ReturnUserError(userResult.Exception, response);
                yield break;
            }
            var userInfo = userResult.Value;
            var pageId = context.GetParam<uint>("pageid");
            var userId = context.GetParam<uint>("userid");
            if(userId != userInfo.Id && !userInfo.IsAdmin) {
                response.Return(DreamMessage.Forbidden("Administrator access is required."));
                yield break;
            }
            var wikiId = GetWikiIdFromRequest(request);
            DreamMessage userMsg = null;
            yield return _deki.At("users", userId.ToString()).Get(new Result<DreamMessage>()).Set(x => userMsg = x);
            if(!userMsg.IsSuccessful) {
                response.Return(userMsg);
            }
            DreamMessage pageAuth = null;
            yield return _deki
                .At("pages", pageId.ToString(), "allowed")
                .With("permissions", "read,subscribe")
                .WithHeaders(request.Headers)
                .Post(new XDoc("users").Start("user").Attr("id", userInfo.Id).End(), new Result<DreamMessage>())
                .Set(x => pageAuth = x);
            if(!pageAuth.IsSuccessful || pageAuth.ToDocument()["user/@id"].AsText != userInfo.Id.ToString()) {
                throw new DreamForbiddenException("User not permitted to subscribe to page");
            }
            GetDataSession(wikiId).Subscribe(userId, pageId, context.GetParam("depth", "0") == "0" ? false : true);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, IContainer container, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            // set up plug for phpscript that will handle the notifications
            _emailer = Plug.New(config["uri.emailer"].AsUri);

            // set up plug deki, so we can validate users
            _deki = Plug.New(config["uri.deki"].AsUri);

            // get the apikey, which we will need as a subscription auth token for subscriptions not done on behalf of a user
            _apikey = config["apikey"].AsText;
            _cache = new PageChangeCache(_deki.With("apikey", _apikey), TimeSpan.FromSeconds(config["page-cache-ttl"].AsInt ?? 2));

            if(!container.IsRegistered<IPageSubscriptionInstance>()) {
                var builder = new ContainerBuilder();
                builder.Register<PageSubscriptionInstance>().As<IPageSubscriptionInstance>().FactoryScoped();
                builder.Build(container);
            }

            // TODO (arnec): this should be hitting the API to retrieve resources

            // resource manager for email template
            var resourcePath = Config["resources-path"].AsText;
            if(!string.IsNullOrEmpty(resourcePath)) {
                _resourceManager = new PlainTextResourceManager(Environment.ExpandEnvironmentVariables(resourcePath));
            } else {

                // creating a test resource manager
                _log.WarnFormat("'resource-path' was not defined in Config, using a test resource manager for email templating");
                var testSet = new TestResourceSet {
                    {"Notification.Page.email-subject", "Page Modified"}, 
                    {"Notification.Page.email-header", "The following pages have changed:"}
                };
                _resourceManager = new PlainTextResourceManager(testSet);
            }

            // set up subscription for pubsub
            var subscriptionSet = new XDoc("subscription-set")
                .Elem("uri.owner", Self.Uri.AsServerUri().ToString())
                .Start("subscription")
                    .Elem("channel", "event://*/deki/users/*")
                    .Add(DreamCookie.NewSetCookie("service-key", InternalAccessKey, Self.Uri).AsSetCookieDocument)
                    .Start("recipient")
                        .Attr("authtoken", _apikey)
                        .Elem("uri", Self.Uri.AsServerUri().At("updateuser").ToString())
                    .End()
                .End()
                .Start("subscription")
                    .Elem("channel", "event://*/deki/pages/create")
                    .Elem("channel", "event://*/deki/pages/update")
                    .Elem("channel", "event://*/deki/pages/delete")
                    .Elem("channel", "event://*/deki/pages/revert")
                    .Elem("channel", "event://*/deki/pages/move")
                    .Elem("channel", "event://*/deki/pages/tags/update")
                    .Elem("channel", "event://*/deki/pages/dependentschanged/comments/create")
                    .Elem("channel", "event://*/deki/pages/dependentschanged/comments/update")
                    .Elem("channel", "event://*/deki/pages/dependentschanged/comments/delete")
                    .Elem("channel", "event://*/deki/pages/dependentschanged/files/create")
                    .Elem("channel", "event://*/deki/pages/dependentschanged/files/update")
                    .Elem("channel", "event://*/deki/pages/dependentschanged/files/delete")
                    .Elem("channel", "event://*/deki/pages/dependentschanged/files/properties/*")
                    .Elem("channel", "event://*/deki/pages/dependentschanged/files/restore")
                    .Add(DreamCookie.NewSetCookie("service-key", InternalAccessKey, Self.Uri).AsSetCookieDocument)
                    .Start("recipient")
                        .Attr("authtoken", _apikey)
                        .Elem("uri", Self.Uri.AsServerUri().At("notify").ToString())
                    .End()
                .End();
            Result<DreamMessage> subscribe;
            yield return subscribe = PubSub.At("subscribers").PostAsync(subscriptionSet);
            string accessKey = subscribe.Value.ToDocument()["access-key"].AsText;
            XUri location = subscribe.Value.Headers.Location;
            Cookies.Update(DreamCookie.NewSetCookie("access-key", accessKey, location), null);
            _subscriptionLocation = Plug.New(location.AsLocalUri().WithoutQuery());
            _log.DebugFormat("set up initial subscription location at {0}", _subscriptionLocation.Uri);

            // set up notification accumulator queue
            TimeSpan accumulationMinutes = TimeSpan.FromSeconds(config["accumulation-time"].AsInt ?? 10 * 60);
            _log.DebugFormat("Initializing queue with {0:0.00} minute accumulation", accumulationMinutes.TotalMinutes);
            _notificationQueue = new NotificationDelayQueue(accumulationMinutes, SendEmail);
            result.Return();
        }

        protected override Yield Stop(Result result) {
            yield return _subscriptionLocation.DeleteAsync().Catch();
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }

        public override DreamFeatureStage[] Prologues {
            get { return new[] { new DreamFeatureStage("ensure-wiki-id-header", PrologueSiteIdHeader, DreamAccess.Public), }; }
        }

        public override DreamFeatureStage[] Epilogues {
            get { return new[] { new DreamFeatureStage("log-called-feature", EpilogueLog, DreamAccess.Public), }; }
        }

        private Yield PrologueSiteIdHeader(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            if(context.Feature.MainStage.Access == DreamAccess.Public) {
                if(string.IsNullOrEmpty(request.Headers["X-Deki-Site"])) {
                    string wikiId = context.GetParam("siteid", null);
                    if(string.IsNullOrEmpty(wikiId)) {
                        throw new DreamBadRequestException("request must contain either an X-Deki-Site header or siteid query parameter");
                    }
                    request.Headers.Add("X-Deki-Site", "id=" + wikiId);
                }
            }
            response.Return(request);
            yield break;
        }

        private void ReturnUserError(Exception exception, Result<DreamMessage> response) {
            var dreamException = exception as DreamResponseException;
            if(dreamException != null) {
                response.Throw(new DreamAbortException(dreamException.Response));
            } else if(exception is UserException) {
                response.Throw(new DreamBadRequestException(exception.Message));
            } else {
                response.Throw(exception);
            }
        }

        private Yield EpilogueLog(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            _log.InfoFormat("Feature [{0}] completed", context.Uri.Path);
            response.Return(request);
            yield break;
        }

        private string GetWikiIdFromRequest(DreamMessage request) {
            return HttpUtil.ParseNameValuePairs(request.Headers["X-Deki-Site"])["id"];
        }

        private Yield GetRequestUser(DreamMessage request, Result<PageSubscriptionUser> result) {

            // can assume that the header has a value, since our prologue would have barfed already otherwise
            XDoc userDoc = null;
            var wikiId = GetWikiIdFromRequest(request);
            yield return _deki.At("users", "current").WithHeaders(request.Headers).Get(new Result<XDoc>()).Set(x => userDoc = x);
            var userId = userDoc["@id"].AsUInt.Value;
            var instance = GetInstanceInfo(wikiId);
            var userInfo = instance.GetUserInfo(userId);
            PopulateUser(userInfo, userDoc);
            result.Return(userInfo);
        }

        private Yield GetUser(uint userId, string wikiId, Result<PageSubscriptionUser> result) {
            XDoc userDoc = null;
            yield return _deki.At("users", userId.ToString()).WithHeader("X-Deki-Site", "id=" + wikiId)
                .Get(new Result<XDoc>()).Set(x => userDoc = x);
            var instance = GetInstanceInfo(wikiId);
            PageSubscriptionUser userInfo = instance.GetUserInfo(userId);
            PopulateUser(userInfo, userDoc);
            result.Return(userInfo);
        }

        private IPageSubscriptionInstance GetInstanceInfo(string wikiid) {
            IPageSubscriptionInstance subscriptionInstance;

            // Note (arnec): currently locking all instances while instantiating settings for one
            lock(_instanceSubscriptions) {
                if(_instanceSubscriptions.TryGetValue(wikiid, out subscriptionInstance) && subscriptionInstance.IsValid) {
                    return subscriptionInstance;
                }

                // need to create or refresh instance data
                var checkForLegacySubscriptionStorage = subscriptionInstance == null;
                var settings = _deki.With("apikey", _apikey).At("site", "settings").WithHeader("X-Deki-Site", "id=" + wikiid).Get();
                if(!settings.IsSuccessful) {
                    throw new ArgumentException(string.Format("unable to fetch site data for instance '{0}', cannot create request session: {1}", wikiid, settings.Status));
                }
                var siteDoc = settings.ToDocument();
                subscriptionInstance = DreamContext.Current.Container.Resolve<IPageSubscriptionInstance>(
                    new NamedParameter("wikiId", wikiid),
                    new NamedParameter("config", siteDoc));
                _instanceSubscriptions[wikiid] = subscriptionInstance;

                // check whether we need to worry about a legacy store
                if(checkForLegacySubscriptionStorage) {

                    // get legacy persisted subscription storage
                    var wikiUsers = Storage.At("subscriptions", wikiid).WithTrailingSlash().Get(new Result<DreamMessage>()).Wait();
                    if(wikiUsers.IsSuccessful) {
                        var hasLegacyStore = false;
                        IPageSubscriptionDataSession session = null;
                        foreach(var userDocname in wikiUsers.ToDocument()["file/name"]) {
                            var userFile = userDocname.AsText;
                            if(!userFile.EndsWith(".xml")) {
                                _log.WarnFormat("Found stray file '{0}' in wiki '{1}' store, ignoring", userFile, wikiid);
                                continue;
                            }
                            var userResult = Storage.At("subscriptions", wikiid, userFile).Get();
                            try {
                                hasLegacyStore = true;
                                var userDoc = userResult.ToDocument();
                                var userid = userDoc["@userid"].AsUInt ?? 0;
                                if(session == null) {
                                    session = GetDataSession(subscriptionInstance);
                                }
                                foreach(var sub in userDoc["subscription.page"]) {
                                    session.Subscribe(userid, sub["@id"].AsUInt ?? 0, sub["@depth"].AsText != "0");
                                }
                            } catch(InvalidDataException e) {
                                _log.Error(string.Format("Unable to retrieve legacy subscription store for user {0}/{1}", wikiid, userFile), e);
                            }
                        }
                        if(hasLegacyStore) {
                            Storage.At("subscriptions", wikiid).Delete(new Result<DreamMessage>()).Wait();
                        }
                    }
                }
            }
            return subscriptionInstance;
        }

        private IPageSubscriptionDataSession GetDataSession(string wikiid) {
            var session = DreamContext.Current.GetState<IPageSubscriptionDataSession>() ?? GetDataSession(GetInstanceInfo(wikiid));
            return session;
        }

        private IPageSubscriptionDataSession GetDataSession(IPageSubscriptionInstance instance) {
            var context = DreamContext.Current;
            var session = context.GetState<IPageSubscriptionDataSession>();
            if(session == null) {
                session = instance.CreateDataSession();
                context.SetState(session);
            }
            return session;
        }

        private void PopulateUser(PageSubscriptionUser userInfo, XDoc userDoc) {
            var email = userDoc["email"].AsText;
            if(string.IsNullOrEmpty(email)) {
                throw new UserException("no email for user");
            }
            userInfo.Email = email;
            userInfo.Username = userDoc["fullname"].AsText.IfNullOrEmpty(userDoc["username"].AsText);
            var language = userDoc["language"].AsText;
            if(!string.IsNullOrEmpty(language)) {
                userInfo.Culture = CultureUtil.GetNonNeutralCulture(language);
            }
            var timezone = userDoc["timezone"].AsText;
            if(!string.IsNullOrEmpty(timezone)) {

                // only update timezone if the user has it defined
                userInfo.Timezone = timezone;
            }
            var perms = (userDoc["permissions.effective/operations"].AsText ?? "").Split(',');
            userInfo.IsAdmin = false;
            foreach(var perm in perms) {
                if("ADMIN".EqualsInvariantIgnoreCase(perm.Trim())) {
                    userInfo.IsAdmin = true;
                }
            }
        }

        private Yield SendEmail(NotificationUpdateRecord updateRecord, Result result) {
            yield return Self.WithCookieJar(Cookies).At("__sendnotification").Post(updateRecord.ToDocument(), new Result<DreamMessage>());
            result.Return();
        }

        private List<uint> GetPageList(uint pageId, XDoc parent) {
            var pages = new List<uint> { pageId };
            while(!parent.IsEmpty) {
                uint id;
                if(uint.TryParse(parent["@id"].AsText, out id)) {
                    pages.Add(id);
                }
                parent = parent["page.parent"];
            }
            return pages;
        }
    }
}
