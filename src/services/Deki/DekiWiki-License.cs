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

using MindTouch.Deki.Logic;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;
using MindTouch.Deki.Util;

namespace MindTouch.Deki {
    using Yield = IEnumerator<IYield>;

    public partial class DekiWikiService {

        //--- Features ---

        [DreamFeature("GET:license", "Retrieve server license. Requires ADMIN permission to retrieve private information, otherwise only the public information is obtained.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureParam("format", "string?", "Response format for license aggreement (only availble for admins; one of \"html\", \"xml\"; default: \"xml\")")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        public Yield GetLicense(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            bool isAdmin = PermissionsBL.IsUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            XDoc license = DekiContext.Current.LicenseManager.GetLicenseDocument(!isAdmin);
            DreamMessage result = null;
            if(license != null) {
                string format = context.GetParam("format", "xml");
                switch(format) {
                case "xml":

                    // nothing to do
                    break;
                case "html": {

                        // convert support agreement to html text
                        XDoc doc = license["support-agreement"];
                        if(!doc.IsEmpty) {
                            string html = doc.ToInnerXHtml();
                            doc.RemoveNodes();
                            doc.Value(html);
                        }

                        // convert source license to thml text
                        doc = license["source-license"];
                        if(!doc.IsEmpty) {
                            string html = doc.ToInnerXHtml();
                            doc.RemoveNodes();
                            doc.Value(html);
                        }
                    }
                    break;
                default:
                    throw new DreamBadRequestException(string.Format("invalid output format: {0}", format));
                }
                result = DreamMessage.Ok(license);
            } else {
                result = DreamMessage.NotFound("License not present");
            }
            response.Return(result);
            yield break;
        }

        [DreamFeature("PUT:license", "Update server license.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid license, input parameter, or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "ADMIN access is required to set or update a license")]
        internal Yield PutLicense(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            XDoc newLicense = null;
            XDoc oldLicense = null;
            try {
                newLicense = request.ToDocument();
            } catch(Exception x) {
                throw new DekiLicenseException(DekiLicenseException.ReasonKind.INVALID_LICENSE, x.Message);
            }
            if(Instancemanager.IsCloudManager) {
                var apikey = context.GetParam("apikey");
                if(!apikey.EqualsInvariant(MasterApiKey)) {
                    throw new DreamForbiddenException("Must provide Master apikey to change license for cloud managed instance");
                }
                oldLicense = newLicense["old-license/license.private"];
                if(oldLicense.IsEmpty) {
                    throw new DreamBadRequestException("missing 'old-license'");
                }
                newLicense = newLicense["new-license/license.private"];
                if(newLicense.IsEmpty) {
                    throw new DreamBadRequestException("missing 'new-license'");
                }
            }

            // before we do any license changes, let's make sure all services have started
            DekiContext.Current.Instance.CheckServicesAreReady();

            // update license
            DekiContext.Current.LicenseManager.UpdateLicense(oldLicense, newLicense);
            response.Return(DreamMessage.Ok());
            yield break;
        }
    }
}
