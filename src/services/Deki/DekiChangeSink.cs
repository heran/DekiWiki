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
using MindTouch.Collections;
using MindTouch.Deki.Data;
using MindTouch.Deki.Logic;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki {

    public interface IDekiChangeSink {

        //--- Methods ---
        void InstanceStarted(DateTime eventTime);
        void InstanceStarting(DateTime eventTime);
        void InstanceShutdown(DateTime eventTime);
        void InstanceSettingsChanged(DateTime eventTime);
        void LicenseUpdated(LicenseStateType previousState, LicenseStateType newState, DateTime eventTime);
        void UserCreate(DateTime eventTime, UserBE user);
        void UserUpdate(DateTime eventTime, UserBE user);
        void UserChangePassword(DateTime eventTime, UserBE user);
        void UserLogin(DateTime eventTime, UserBE user);
        void BanCreated(DateTime eventTime, BanBE ban);
        void BanRemoved(DateTime eventTime, BanBE ban);
        void IndexRebuildStart(DateTime eventTime);
        void CommentCreate(DateTime eventTime, CommentBE comment, PageBE parent, UserBE user);
        void CommentUpdate(DateTime eventTime, CommentBE comment, PageBE parent, UserBE user);
        void CommentPoke(DateTime eventTime, CommentBE comment, PageBE parent, UserBE user);
        void CommentDelete(DateTime eventTime, CommentBE comment, PageBE parent, UserBE user);
        void PageMove(DateTime eventTime, PageBE oldPage, PageBE newPage, UserBE user);
        void PagePoke(DateTime eventTime, PageBE page, UserBE user);
        void PageCreate(DateTime eventTime, PageBE page, UserBE user);
        void PageUpdate(DateTime eventTime, PageBE page, UserBE user);
        void PageTagsUpdate(DateTime eventTime, PageBE page, UserBE user);
        void PageRated(DateTime eventTime, PageBE page, UserBE user);
        void PageDelete(DateTime eventTime, PageBE page, UserBE user);
        void PageAliasCreate(DateTime eventTime, PageBE page, UserBE user);
        void PageRevert(DateTime eventTime, PageBE page, UserBE user, int rev);
        void PageViewed(DateTime eventTime, PageBE page, UserBE user);
        void PageMessage(DateTime eventTime, PageBE page, UserBE user, XDoc body, string[] path);
        void PageSecuritySet(DateTime eventTime, PageBE page, CascadeType cascade);
        void PageSecurityUpdated(DateTime eventTime, PageBE page, CascadeType cascade);
        void PageSecurityDelete(DateTime eventTime, PageBE page);
        void AttachmentCreate(DateTime eventTime, ResourceBE attachment, UserBE user);
        void AttachmentUpdate(DateTime eventTime, ResourceBE attachment, UserBE user);
        void AttachmentDelete(DateTime eventTime, ResourceBE attachment, UserBE user);
        void AttachmentMove(DateTime eventTime, ResourceBE attachment, PageBE sourcePage, UserBE user);
        void AttachmentRestore(DateTime eventTime, ResourceBE attachment, UserBE user);
        void AttachmentPoke(DateTime eventTime, ResourceBE attachment);
        void PropertyCreate(DateTime eventTime, ResourceBE prop, UserBE user, ResourceBE.ParentType parentType, XUri parentUri);
        void PropertyUpdate(DateTime eventTime, ResourceBE prop, UserBE user, ResourceBE.ParentType parentType, XUri parentUri);
        void PropertyDelete(DateTime eventTime, ResourceBE prop, UserBE user, ResourceBE.ParentType parentType, XUri parentUri);
    }

    public class DekiChangeSink : IDekiChangeSink {

        //--- Types ---
        private class ChangeData {
            public XUri Channel;
            public XUri Resource;
            public string[] Origin;
            public XDoc Doc;
        }

        //--- Constants ---
        private const string PAGES = "pages";
        private const string FILES = "files";
        private const string COMMENTS = "comments";
        private const string USERS = "users";
        private const string BAN = "ban";
        private const string SITE = "site";
        private const string PROPERTY = "properties";
        private const string TAGS = "tags";

        private const string START = "start";
        private const string STARTED = "started";
        private const string STOP = "stop";
        private const string NO_OP = "noop";
        private const string CREATE = "create";
        private const string MOVE = "move";
        private const string UPDATE = "update";
        private const string DELETE = "delete";
        private const string CREATE_ALIAS = "createalias";
        private const string DEPENDENTS_CHANGED = "dependentschanged";
        private const string MESSAGE = "message";
        private const string RESTORE = "restore";
        private const string REVERT = "revert";
        private const string PASSWORD = "password";
        private const string LOGIN = "login";
        private const string TALK = "talk";
        private const string RE_INDEX = "reindex";
        private const string SECURITY = "security";
        private const string VIEW = "view";
        private const string RATED = "rated";
        private const string SETTINGS = "settings";
        private const string LICENSE = "license";

        //--- Class Fields
        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly string _wikiid;
        private readonly XUri _apiUri;
        private readonly Plug _publishPlug;
        private readonly XUri _channel;
        private readonly ProcessingQueue<ChangeData> _changeQueue;

        //--- Constructors ---
        public DekiChangeSink(string wikiid, XUri apiUri, Plug publishPlug) {
            _wikiid = wikiid;
            _apiUri = apiUri;
            _publishPlug = publishPlug;
            _channel = new XUri(string.Format("event://{0}/deki/", _wikiid));
            _changeQueue = new ProcessingQueue<ChangeData>(Publish, 10);
        }

        //--- Methods ---
        public void InstanceStarted(DateTime eventTime) {
            try {
                XUri channel = _channel.At(SITE, STARTED);
                XUri resource = _apiUri.AsServerUri();
                Queue(eventTime, channel, resource, new[] { _apiUri.AsServerUri().ToString() }, new XDoc("deki-event")
                    .Elem("channel", channel)
                    .Elem("uri", _apiUri.AsServerUri().ToString()));
            } catch(Exception e) {
                _log.WarnExceptionMethodCall(e, "InstanceStarted", "event couldn't be created");
            }
        }

        public void InstanceStarting(DateTime eventTime) {
            try {
                XUri channel = _channel.At(SITE, START);
                XUri resource = _apiUri.AsServerUri();
                Queue(eventTime, channel, resource, new[] { _apiUri.AsServerUri().ToString() }, new XDoc("deki-event")
                    .Elem("channel", channel)
                    .Elem("uri", _apiUri.AsServerUri().ToString()));
            } catch(Exception e) {
                _log.WarnExceptionMethodCall(e, "InstanceStarting", "event couldn't be created");
            }
        }

        public void InstanceShutdown(DateTime eventTime) {
            try {
                XUri channel = _channel.At(SITE, STOP);
                XUri resource = _apiUri.AsServerUri();
                Queue(eventTime, channel, resource, new[] { _apiUri.AsServerUri().ToString() }, new XDoc("deki-event")
                    .Elem("channel", channel)
                    .Elem("uri", _apiUri.AsServerUri().ToString()));
            } catch(Exception e) {
                _log.WarnExceptionMethodCall(e, "InstanceShutdown", "event couldn't be created");
            }
        }

        public void InstanceSettingsChanged(DateTime eventTime) {
            try {
                XUri channel = _channel.At(SITE, SETTINGS,UPDATE);
                XUri resource = _apiUri.AsServerUri();
                Queue(eventTime, channel, resource, new[] { _apiUri.AsServerUri().ToString() }, new XDoc("deki-event")
                    .Elem("channel", channel)
                    .Elem("uri", _apiUri.AsServerUri().ToString()));
            } catch(Exception e) {
                _log.WarnExceptionMethodCall(e, "InstanceSettingsChanged", "event couldn't be created");
            }
        }

        public void LicenseUpdated(LicenseStateType previousState, LicenseStateType newState, DateTime eventTime) {
            try {
                var channel = _channel.At(SITE, LICENSE, UPDATE);
                var resource = _apiUri.AsServerUri();
                Queue(eventTime, channel, resource, new[] { _apiUri.AsServerUri().ToString() }, new XDoc("deki-event")
                    .Elem("channel", channel)
                    .Elem("uri", _apiUri.AsServerUri().ToString())
                    .Start("previous-license").Attr("state", previousState.ToString()).End()
                    .Start("new-license").Attr("state", newState.ToString()).End()
                );
            } catch(Exception e) {
                _log.WarnExceptionMethodCall(e, "LicenseUpdated", "event couldn't be created");
            }
        }

        public void UserCreate(DateTime eventTime, UserBE user) {
            UserChanged(eventTime, user, CREATE);
        }

        public void UserUpdate(DateTime eventTime, UserBE user) {
            UserChanged(eventTime, user, UPDATE);
        }

        private void UserDependentChanged(DateTime eventTime, UserBE user, params string[] path) {
            UserChanged(eventTime, user, ArrayUtil.Concat(new string[] { DEPENDENTS_CHANGED }, path));
        }

        public void UserChangePassword(DateTime eventTime, UserBE user) {
            UserChanged(eventTime, user, UPDATE, PASSWORD);
        }

        public void UserLogin(DateTime eventTime, UserBE user) {
            UserChanged(eventTime, user, LOGIN);
        }

        private void UserChanged(DateTime eventTime, UserBE user, params string[] path) {
            try {
                XUri channel = _channel.At(USERS).At(path);
                XUri resource = UserBL.GetUri(user).WithHost(_wikiid);
                Queue(eventTime, channel, resource, new string[] { resource.AsServerUri().ToString() }, new XDoc("deki-event")
                    .Elem("channel", channel)
                    .Elem("userid", user.ID)
                    .Elem("uri", UserBL.GetUri(user).AsServerUri().ToString())
                    .Elem("path", UserBL.GetUriUiHomePage(user).Path.TrimStart('/')));
            } catch(Exception e) {
                _log.WarnExceptionMethodCall(e, "UserChanged", "event couldn't be created");
            }
        }

        public void BanCreated(DateTime eventTime, BanBE ban) {
            BanEvent(eventTime, ban, CREATE);
        }

        public void BanRemoved(DateTime eventTime, BanBE ban) {
            BanEvent(eventTime, ban, DELETE);
        }

        private void BanEvent(DateTime eventTime, BanBE ban, params string[] channelPath) {
            try {
                XUri channel = _channel.At(BAN).At(channelPath);
                XDoc doc = new XDoc("deki-event")
                    .Elem("channel", channel)
                    .Elem("banid", ban.Id)
                    .Elem("reason", ban.Reason)
                    .Elem("by", ban.ByUserId);
                if(ban.BanAddresses.Count > 0) {
                    doc.Start("addresses");
                    foreach(string address in ban.BanAddresses) {
                        doc.Elem("address", address);
                    }
                    doc.End();
                }
                if(ban.BanUserIds.Count > 0) {
                    doc.Start("users");
                    foreach(uint userId in ban.BanUserIds) {
                        doc.Start("user").Attr("id", userId).End();
                    }
                    doc.End();
                }
                Queue(eventTime, channel, null, new string[] { string.Format("http://{0}/deki", _wikiid) }, doc);
            } catch(Exception e) {
                _log.WarnExceptionMethodCall(e, "BanEvent", "event couldn't be created");
            }
        }

        public void IndexRebuildStart(DateTime eventTime) {
            try {
                XUri channel = _channel.At(SITE).At(RE_INDEX);
                Queue(eventTime, channel, null, new string[] { string.Format("http://{0}/deki", _wikiid) }, new XDoc("deki-event")
                    .Elem("channel", channel));
            } catch(Exception e) {
                _log.WarnExceptionMethodCall(e, "IndexRebuildStart", "event couldn't be created");
            }
        }

        public void CommentCreate(DateTime eventTime, CommentBE comment, PageBE parent, UserBE user) {
            PageDependentChanged(eventTime, parent, user, COMMENTS, CREATE);
            CommentChanged(eventTime, comment, parent, user, CREATE);
        }

        public void CommentUpdate(DateTime eventTime, CommentBE comment, PageBE parent, UserBE user) {
            PageDependentChanged(eventTime, parent, user, COMMENTS, UPDATE);
            CommentChanged(eventTime, comment, parent, user, UPDATE);
        }

        public void CommentPoke(DateTime eventTime, CommentBE comment, PageBE parent, UserBE user) {
            CommentChanged(eventTime, comment, parent, user, NO_OP);
        }

        public void CommentDelete(DateTime eventTime, CommentBE comment, PageBE parent, UserBE user) {
            PageDependentChanged(eventTime, parent, user, COMMENTS, DELETE);
            CommentChanged(eventTime, comment, parent, user, DELETE);
        }

        private void CommentChanged(DateTime eventTime, CommentBE comment, PageBE parent, UserBE user, params string[] channelPath) {
            try {
                XUri channel = _channel.At(COMMENTS).At(channelPath);
                XUri resource = CommentBL.GetUri(comment).WithHost(_wikiid);
                string[] origin = new string[] { CommentBL.GetUri(comment).AsServerUri().ToString() };
                string path = parent.Title.AsUiUriPath() + "#comment" + comment.Number;
                XDoc doc = new XDoc("deki-event")
                    //userid is deprecated and user/id should be used instead
                    .Elem("userid", comment.PosterUserId)
                    .Elem("pageid", comment.PageId)
                    .Elem("uri.page", PageBL.GetUriCanonical(parent).AsServerUri().ToString())
                    .Start("user")
                        .Attr("id", user.ID)
                        .Attr("anonymous", UserBL.IsAnonymous(user))
                        .Elem("uri", UserBL.GetUri(user))
                    .End()
                    .Elem("channel", channel)
                    .Elem("uri", CommentBL.GetUri(comment).AsServerUri().ToString())
                    .Elem("path", path)
                    .Start("content").Attr("uri", CommentBL.GetUri(comment).AsServerUri().At("content").ToString()).End();
                if(comment.Content.Length < 255) {
                    doc["content"].Attr("type", comment.ContentMimeType).Value(comment.Content);
                }
                Queue(eventTime, channel, resource, origin, doc);
            } catch(Exception e) {
                _log.WarnMethodCall("CommentChanged", "event couldn't be created");
            }
        }

        public void PageMove(DateTime eventTime, PageBE oldPage, PageBE newPage, UserBE user) {
            try {
                XUri channel = _channel.At(PAGES, MOVE);
                XUri resource = PageBL.GetUriCanonical(newPage).WithHost(_wikiid);
                var origin = new[] {
                    PageBL.GetUriCanonical(newPage).AsServerUri().ToString(),
                    XUri.Localhost + "/" + oldPage.Title.AsUiUriPath(),
                    XUri.Localhost + "/" + newPage.Title.AsUiUriPath(),
                };
                Queue(eventTime, channel, resource, origin, new XDoc("deki-event")
                    .Elem("channel", channel)
                    .Elem("uri", PageBL.GetUriCanonical(newPage).AsServerUri().ToString())
                    .Elem("pageid", newPage.ID)
                    .Start("user")
                        .Attr("id", user.ID)
                        .Attr("anonymous", UserBL.IsAnonymous(user))
                        .Elem("uri", UserBL.GetUri(user))
                    .End()
                    .Start("content.uri")
                        .Attr("type", "application/xml")
                        .Value(PageBL.GetUriContentsCanonical(newPage).AsServerUri().With("format", "xhtml").ToString())
                    .End()
                    .Elem("revision.uri", PageBL.GetUriRevisionCanonical(newPage).AsServerUri().ToString())
                    .Elem("tags.uri", PageBL.GetUriCanonical(newPage).At("tags").AsServerUri().ToString())
                    .Elem("comments.uri", PageBL.GetUriCanonical(newPage).At("comments").AsServerUri().ToString())
                    .Elem("path", newPage.Title.AsUiUriPath())
                    .Elem("previous-path", oldPage.Title.AsUiUriPath()));
            } catch(Exception e) {
                _log.WarnExceptionMethodCall(e, "PageMove", "event couldn't be created");
            }
        }

        public void PagePoke(DateTime eventTime, PageBE page, UserBE user) {
            PageChanged(eventTime, page, user, null, NO_OP);
        }

        public void PageCreate(DateTime eventTime, PageBE page, UserBE user) {
            PageChanged(eventTime, page, user, null, CREATE);
        }

        public void PageUpdate(DateTime eventTime, PageBE page, UserBE user) {
            PageChanged(eventTime, page, user, null, UPDATE);
        }

        public void PageTagsUpdate(DateTime eventTime, PageBE page, UserBE user) {
            PageChanged(eventTime, page, user, null, TAGS, UPDATE);
        }

        public void PageRated(DateTime eventTime, PageBE page, UserBE user) {
            PageChanged(eventTime, page, user, null, RATED);
        }

        public void PageDelete(DateTime eventTime, PageBE page, UserBE user) {
            PageChanged(eventTime, page, user, null, DELETE);
        }

        public void PageAliasCreate(DateTime eventTime, PageBE page, UserBE user) {
            PageChanged(eventTime, page, user, null, CREATE_ALIAS);
        }

        public void PageRevert(DateTime eventTime, PageBE page, UserBE user, int rev) {
            PageChanged(eventTime, page, user, new XDoc("reverted-to").Value(rev), REVERT);
        }

        public void PageViewed(DateTime eventTime, PageBE page, UserBE user) {
            PageChanged(eventTime, page, user, null, VIEW);
        }

        private void PageDependentChanged(DateTime eventTime, PageBE page, UserBE user, params string[] path) {
            PageChanged(eventTime, page, user, null, ArrayUtil.Concat(new string[] { DEPENDENTS_CHANGED }, path));
        }

        public void PageMessage(DateTime eventTime, PageBE page, UserBE user, XDoc body, string[] path) {
            PageChanged(eventTime, page, user, body, ArrayUtil.Concat(new string[] { MESSAGE }, path));
        }

        private void PageChanged(DateTime eventTime, PageBE page, UserBE user, XDoc extra, params string[] path) {
            try {
                XUri channel = _channel.At(PAGES).At(path);
                XUri resource = PageBL.GetUriCanonical(page).WithHost(_wikiid);
                string[] origin = new string[] { PageBL.GetUriCanonical(page).AsServerUri().ToString(), XUri.Localhost + "/" + page.Title.AsUiUriPath() };
                XDoc doc = new XDoc("deki-event")
                    .Elem("channel", channel)
                    // BUGBUGBUG: This will generally generate a Uri based on the request that caused the event,
                    //            which may not really be canonical
                    .Elem("uri", PageBL.GetUriCanonical(page).AsPublicUri().ToString())
                    .Elem("pageid", page.ID)
                    .Start("user")
                        .Attr("id", user.ID)
                        .Attr("anonymous", UserBL.IsAnonymous(user))
                        .Elem("uri", UserBL.GetUri(user))
                    .End()
                    .Start("content.uri")
                        .Attr("type", "application/xml")
                        .Value(PageBL.GetUriContentsCanonical(page).With("format", "xhtml").AsServerUri().ToString())
                    .End()
                    .Elem("revision.uri", PageBL.GetUriRevisionCanonical(page).AsServerUri().ToString())
                    .Elem("tags.uri", PageBL.GetUriCanonical(page).At("tags").AsServerUri().ToString())
                    .Elem("comments.uri", PageBL.GetUriCanonical(page).At("comments").AsServerUri().ToString())
                    .Elem("path", page.Title.AsUiUriPath());
                if(extra != null) {
                    doc.Add(extra);
                }
                Queue(eventTime, channel, resource, origin, doc);
                if(page.Title.IsTalk) {
                    PageBE front = PageBL.GetPageByTitle(page.Title.AsFront());
                    if((front != null) && (front.ID > 0)) {
                        PageChanged(eventTime, front, user, extra, ArrayUtil.Concat(new string[] { DEPENDENTS_CHANGED, TALK }, path));
                    }
                }
            } catch(Exception e) {
                _log.WarnExceptionMethodCall(e, "PageChanged", "event couldn't be created");
            }
        }

        public void PageSecuritySet(DateTime eventTime, PageBE page, CascadeType cascade) {
            PageSecurityChanged(eventTime, page, cascade, SECURITY, CREATE);
        }

        public void PageSecurityUpdated(DateTime eventTime, PageBE page, CascadeType cascade) {
            PageSecurityChanged(eventTime, page, cascade, SECURITY, UPDATE);
        }

        public void PageSecurityDelete(DateTime eventTime, PageBE page) {
            PageSecurityChanged(eventTime, page, CascadeType.NONE, SECURITY, DELETE);
        }

        private void PageSecurityChanged(DateTime eventTime, PageBE page, CascadeType cascade, params string[] path) {
            try {
                XUri channel = _channel.At(PAGES).At(path);
                XUri resource = PageBL.GetUriCanonical(page).WithHost(_wikiid);
                string[] origin = new string[] { PageBL.GetUriCanonical(page).AsServerUri().ToString(), XUri.Localhost + "/" + page.Title.AsUiUriPath() };
                Queue(eventTime, channel, resource, origin, new XDoc("deki-event")
                    .Elem("channel", channel)
                    .Elem("uri", PageBL.GetUriCanonical(page).AsServerUri().ToString())
                    .Elem("pageid", page.ID)
                    .Start("security")
                        .Attr("cascade", cascade.ToString().ToLower())
                    .End());
            } catch(Exception e) {
                _log.WarnExceptionMethodCall(e, "PageSecurityChanged", "event couldn't be created");
            }
        }

        public void AttachmentCreate(DateTime eventTime, ResourceBE attachment, UserBE user) {
            PageDependentChanged(eventTime, PageBL.GetPageById(attachment.ParentPageId.Value), user, FILES, CREATE);
            AttachmentChanged(eventTime, attachment, CREATE);
        }

        public void AttachmentUpdate(DateTime eventTime, ResourceBE attachment, UserBE user) {
            PageDependentChanged(eventTime, PageBL.GetPageById(attachment.ParentPageId.Value), user, FILES, UPDATE);
            AttachmentChanged(eventTime, attachment, UPDATE);
        }

        public void AttachmentDelete(DateTime eventTime, ResourceBE attachment, UserBE user) {
            PageDependentChanged(eventTime, PageBL.GetPageById(attachment.ParentPageId.Value), user, FILES, DELETE);
            AttachmentChanged(eventTime, attachment, DELETE);
        }

        public void AttachmentMove(DateTime eventTime, ResourceBE attachment, PageBE sourcePage, UserBE user) {
            PageDependentChanged(eventTime, sourcePage, user, FILES, DELETE);
            PageDependentChanged(eventTime, PageBL.GetPageById(attachment.ParentPageId.Value), user, FILES, CREATE);
            AttachmentChanged(eventTime, attachment, MOVE);
        }

        public void AttachmentRestore(DateTime eventTime, ResourceBE attachment, UserBE user) {
            PageDependentChanged(eventTime, PageBL.GetPageById(attachment.ParentPageId.Value), user, FILES, RESTORE);
            AttachmentChanged(eventTime, attachment, RESTORE);
        }

        public void AttachmentPoke(DateTime eventTime, ResourceBE attachment) {
            AttachmentChanged(eventTime, attachment, NO_OP);
        }

        private void AttachmentChanged(DateTime eventTime, ResourceBE attachment, params string[] path) {
            try {
                XUri channel = _channel.At(FILES).At(path);
                XUri attachmentUri = AttachmentBL.Instance.GetUri(attachment);
                XUri resource = attachmentUri.WithHost(_wikiid);
                string[] origin = new string[] { attachmentUri.AsServerUri().ToString() };
                Queue(eventTime, channel, resource, origin, new XDoc("deki-event")
                    .Elem("channel", channel)
                    .Elem("fileid", attachment.MetaXml.FileId ?? 0)
                    .Elem("uri", attachmentUri.AsServerUri().ToString())
                    .Elem("content.uri", attachmentUri.AsServerUri().ToString())
                    .Elem("revision.uri", attachmentUri.At("info").With("revision", attachment.Revision.ToString()).AsServerUri().ToString())
                    .Elem("path", attachmentUri.Path));
            } catch(Exception e) {
                _log.WarnExceptionMethodCall(e, "AttachmentChanged", "event couldn't be created");
            }
        }

        public void PropertyCreate(DateTime eventTime, ResourceBE prop, UserBE user, ResourceBE.ParentType parentType, XUri parentUri) {
            NotifyPropertyParent(eventTime, prop, user, parentType, CREATE);
            PropertyChanged(eventTime, prop, user, parentType, parentUri, CREATE);
        }

        public void PropertyUpdate(DateTime eventTime, ResourceBE prop, UserBE user, ResourceBE.ParentType parentType, XUri parentUri) {
            NotifyPropertyParent(eventTime, prop, user, parentType, UPDATE);
            PropertyChanged(eventTime, prop, user, parentType, parentUri, UPDATE);
        }

        public void PropertyDelete(DateTime eventTime, ResourceBE prop, UserBE user, ResourceBE.ParentType parentType, XUri parentUri) {
            NotifyPropertyParent(eventTime, prop, user, parentType, DELETE);
            PropertyChanged(eventTime, prop, user, parentType, parentUri, DELETE);
        }

        private void NotifyPropertyParent(DateTime eventTime, ResourceBE prop, UserBE user, ResourceBE.ParentType parentType, string action) {
            if(parentType == ResourceBE.ParentType.PAGE && prop.ParentPageId != null) {
                PageBE parentPage = PageBL.GetPageById(prop.ParentPageId.Value);
                if(parentPage != null) {
                    PageDependentChanged(eventTime, parentPage, user, PROPERTY, action);
                }
            } else if(parentType == ResourceBE.ParentType.USER) {

                // Owner of property may not be same as requesting user. 
                // The dependentschanged event is triggered on the property owner.
                if(prop.ParentUserId != null) {

                    // Optimization to avoid a db call when operating on your own user property.
                    if(user.ID != prop.ParentUserId.Value) {
                        user = UserBL.GetUserById(prop.ParentUserId.Value);
                        if(user == null) {
                            _log.WarnFormat("Could not find owner user (id: {0}) of user property (key: {1})", prop.ParentUserId.Value, prop.Name);
                            return;
                        }
                    }
                }

                UserDependentChanged(eventTime, user, PROPERTY, action);
            }

            //TODO (maxm): trigger file property changes
        }

        private void PropertyChanged(DateTime eventTime, ResourceBE prop, UserBE user, ResourceBE.ParentType parentType, XUri parentUri, params string[] path) {
            try {
                string parent = string.Empty;
                switch(parentType) {
                case ResourceBE.ParentType.PAGE:
                    parent = PAGES;
                    break;
                case ResourceBE.ParentType.FILE:
                    parent = FILES;
                    break;
                case ResourceBE.ParentType.USER:
                    parent = USERS;
                    break;
                case ResourceBE.ParentType.SITE:
                    parent = SITE;
                    break;
                }
                XUri channel = _channel.At(parent).At(PROPERTY).At(path);
                XUri resource = prop.PropertyInfoUri(parentUri);
                string[] origin = new string[] { resource.ToString() };
                XDoc doc = new XDoc("deki-event")
                    .Elem("channel", channel)
                    .Elem("name", prop.Name)
                    .Elem("uri", resource)
                    .Start("content")
                        .Attr("mime-type", prop.MimeType.FullType)
                        .Attr("size", prop.Size)
                        .Attr("href", prop.PropertyContentUri(parentUri));
                if(prop.MimeType.MainType == MimeType.TEXT.MainType && prop.Size < 256) {
                    doc.Value(ResourceContentBL.Instance.Get(prop).ToText());
                }
                doc.End();
                if(parentType == ResourceBE.ParentType.PAGE) {
                    doc.Elem("pageid", prop.ParentPageId ?? 0);
                } else if(parentType == ResourceBE.ParentType.USER) {
                    doc.Elem("userid", prop.ParentUserId ?? 0);
                } else if(parentType == ResourceBE.ParentType.FILE) {
                    ResourceBE attachment = ResourceBL.Instance.GetResource(prop.ParentId.Value);
                    doc.Elem("fileid", attachment.MetaXml.FileId ?? 0);
                    PageDependentChanged(eventTime, PageBL.GetPageById(attachment.ParentPageId.Value), user, ArrayUtil.Concat(new string[] { FILES, PROPERTY }, path));
                }
                Queue(eventTime, channel, resource, origin, doc);
            } catch(Exception e) {
                _log.WarnExceptionMethodCall(e, "PropertyChanged", "event couldn't be created");
            }
        }

        private void Queue(DateTime eventTime, XUri channel, XUri resource, string[] origin, XDoc doc) {
            doc.Attr("wikiid", _wikiid).Attr("event-time", eventTime);
            var data = new ChangeData();
            data.Channel = channel;
            data.Resource = resource == null ? null : resource.WithoutQuery().WithoutFragment();
            data.Origin = origin;
            data.Doc = doc;

            if(!_changeQueue.TryEnqueue(data)) {
                _log.WarnFormat("unable to enqueue change data into processing queue");
            }
        }

        private void Publish(ChangeData data) {
            _log.DebugFormat("publishing {0} on channel {1}", data.Resource, data.Channel);
            DreamMessage message = DreamMessage.Ok(data.Doc);
            message.Headers.DreamEventChannel = data.Channel.ToString();
            if(data.Resource != null) {
                message.Headers.DreamEventResource = data.Resource.ToString();
            }
            message.Headers.DreamEventOrigin = data.Origin;
            _publishPlug.Post(message, new Result<DreamMessage>()).WhenDone(
                r => {
                    if(r.HasException) {
                        _log.Warn("unable to publish deki change", r.Exception);
                    } else if(!r.Value.IsSuccessful) {
                        _log.WarnFormat("unable to publish deki change: {0}", r.Value.Status);
                    }
                });

        }
    }
}
