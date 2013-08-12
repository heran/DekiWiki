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
using System.Linq;
using System.Net;
using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Logic {
    public static class BanningBL {

        //--- Constants ---
        private const string BANTOKEN = "bantoken";

        //--- Class Methods ---
        public static void PerformBanCheckForCurrentUser() {
            ulong banMask;
            List<string> banReasons;
            string[] clientIPs = DetermineSourceIPs();
            PerformBanCheck(DekiContext.Current.User, clientIPs, out banMask, out banReasons);
            DekiContext.Current.BanPermissionRevokeMask = banMask;
            DekiContext.Current.BanReasons = banReasons.ToArray();
        }

        public static void PerformBanCheck(UserBE user, string[] clientIps, out ulong banMask, out List<string> banReasons) {
            banMask = ulong.MinValue;
            banReasons = new List<string>();
            ulong? cachedBanMask = GetBanMaskFromCache(user.ID, clientIps);
            if(cachedBanMask != null) {

                //TODO MaxM: Ban reasons isn't currently cached (or used)
                banMask = cachedBanMask.Value;
            } else {
                IList<BanBE> bans = DbUtils.CurrentSession.Bans_GetByRequest(user.ID, clientIps.ToList());
                foreach(BanBE ban in bans) {
                    if(!ban.Expires.HasValue || (ban.Expires.Value >= DateTime.UtcNow)) {
                        banMask |= ban.RevokeMask;
                        banReasons.Add(ban.Reason);
                    }
                }
                CacheBanMask(user.ID, clientIps, banMask);
            }
        }

        public static XDoc RetrieveBans() {
            IList<BanBE> bans = DbUtils.CurrentSession.Bans_GetAll();
            return GetBanListXml(bans);
        }

        public static BanBE GetById(uint id) {
            return DbUtils.CurrentSession.Bans_GetAll().FirstOrDefault(e => e.Id == id);
        }

        public static BanBE SaveBan(XDoc doc) {
            BanBE ban = ReadBanXml(doc);
            if(ArrayUtil.IsNullOrEmpty(ban.BanAddresses) && ArrayUtil.IsNullOrEmpty(ban.BanUserIds)) {
                throw new BanEmptyInvalidArgumentException();
            }
            foreach (var address in ban.BanAddresses) {
                try {
                    if (IPAddress.IsLoopback(IPAddress.Parse(address))) {
                        throw new LoopbackIPAddressException(address);
                    }
                } catch (FormatException) {
                } catch (ArgumentNullException) {
                    throw new InvalidIPAddressException(address);
                }
            }
            if(ban.RevokeMask == 0) {
                throw new BanNoPermsInvalidArgumentException();
            }

            ulong? siteOwner = DekiContext.Current.LicenseManager.GetSiteOwnerUserId();
            if(!ArrayUtil.IsNullOrEmpty(ban.BanUserIds) && (siteOwner != null) && ban.BanUserIds.Contains((uint)siteOwner)) {
                throw new BanningOwnerConflict();
            }

            TokenReset();
            uint banId = DbUtils.CurrentSession.Bans_Insert(ban);
            if(banId == 0) {
                return null;
            }
                ban.Id = banId;
                return ban;
        }

        public static void DeleteBan(BanBE ban) {
            if(ban != null) {
                DbUtils.CurrentSession.Bans_Delete(ban.Id);
                TokenReset();
            }
        }

        public static ulong GetBanPermissionRevokeMaskForUser(UserBE user) {
            ulong banMask = ulong.MinValue;
            if(DekiContext.Current.User != null && DekiContext.Current.User.ID == user.ID) {
                if(DekiContext.Current.BanPermissionRevokeMask != null) {
                    banMask = DekiContext.Current.BanPermissionRevokeMask.Value;
                }
            } else {
                List<string> banReasons;
                PerformBanCheck(user, new string[] { }, out banMask, out banReasons);
            }
            return banMask;
        }

        private static BanBE ReadBanXml(XDoc doc) {
            BanBE b = new BanBE();
            b.BanAddresses = new List<string>();
            b.BanUserIds = new List<uint>();
            try {
                b.Reason = doc["description"].AsText;
                b.RevokeMask = PermissionsBL.MaskFromPermissionList(PermissionsBL.PermissionListFromString(doc["permissions.revoked/operations"].AsText ?? string.Empty));
                b.LastEdit = DateTime.UtcNow;
                b.Expires = doc["date.expires"].AsDate;
                b.ByUserId = DekiContext.Current.User.ID;
                foreach(XDoc val in doc["ban.addresses/address"]) {
                    if(!val.IsEmpty) {
                        b.BanAddresses.Add(val.AsText);
                    }
                }
                foreach(XDoc val in doc["ban.users/user"]) {
                    uint? id = val["@id"].AsUInt;
                    if(id != null) {
                        b.BanUserIds.Add(id ?? 0);
                    }
                }
            } catch(ResourcedMindTouchException) {
                throw;
            } catch(Exception x) {
                throw new MindTouchInvalidOperationException(x.Message, x);
            }
            return b;
        }

        public static XDoc GetBanXml(BanBE ban) {
            return AppendBanXml(new XDoc("ban"), ban);
        }

        public static XDoc GetBanListXml(IList<BanBE> bans) {
            XDoc doc = new XDoc("bans");
            foreach(BanBE b in bans) {
                doc.Start("ban");
                doc = AppendBanXml(doc, b);
                doc.End();
            }
            return doc;
        }

        private static XDoc AppendBanXml(XDoc doc, BanBE ban) {
            UserBE createdBy = UserBL.GetUserById(ban.ByUserId);
            doc.Attr("id", ban.Id);
            doc.Attr("href", DekiContext.Current.ApiUri.At("site", "bans", ban.Id.ToString()));
            if(createdBy != null) {
                doc.Add(UserBL.GetUserXml(createdBy, "createdby", Utils.ShowPrivateUserInfo(createdBy)));
            }
            doc.Elem("date.modified", ban.LastEdit);
            doc.Elem("description", ban.Reason);
            doc.Elem("date.expires", ban.Expires);
            doc.Add(PermissionsBL.GetPermissionXml(ban.RevokeMask, "revoked"));
            doc.Start("ban.addresses");
            if(ban.BanAddresses != null) {
                foreach(string address in ban.BanAddresses) {
                    doc.Elem("address", address);
                }
            }
            doc.End();
            doc.Start("ban.users");
            if(ban.BanUserIds != null) {
                var banUsers = DbUtils.CurrentSession.Users_GetByIds(ban.BanUserIds);
                foreach(UserBE u in banUsers) {
                    doc.Add(UserBL.GetUserXml(u, null, Utils.ShowPrivateUserInfo(createdBy)));
                }

            }
            doc.End();
            return doc;
        }

        private static string[] DetermineSourceIPs() {
            return DreamContext.Current.Request.Headers.GetValues(DreamHeaders.DREAM_CLIENTIP);
        }

        private static string BuildCacheKey(string token, uint userid, string[] clientips) {
            return string.Format("{0}.{1}|{2}", token, userid, string.Join(",", clientips));
        }

        private static void CacheBanMask(uint userid, string[] clientips, ulong banMask) {
            if(DekiContext.Current.Instance.CacheBans) {
                string token = TokenGet();
                DekiContext.Current.Instance.Cache.Set(BuildCacheKey(token, userid, clientips), banMask, DateTime.UtcNow.AddSeconds(30));
            }
        }

        private static ulong? GetBanMaskFromCache(uint userid, string[] clientips) {
            ulong? banmask = null;
            if(DekiContext.Current.Instance.CacheBans) {
                string token = TokenGet();
                banmask = DekiContext.Current.Instance.Cache.Get<ulong?>(BuildCacheKey(token, userid, clientips), null);
            }
            return banmask;
        }

        private static string TokenReset() {
            if(!DekiContext.Current.Instance.CacheBans) {
                return string.Empty;
            }
            string token = DateTime.UtcNow.Ticks.ToString();
            DekiContext.Current.Instance.Cache.Set(BANTOKEN, token);
            return token;
        }

        private static string TokenGet() {
            string token = DekiContext.Current.Instance.Cache.Get<string>(BANTOKEN, null);
            if(token == null) {
                token = TokenReset();
            }
            return token;
        }
    }
}
