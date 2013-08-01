/*
 * OpenID relying party support for MindTouch Core
 * Copyright © 2009, 2010 Craig Box
 * craig.box@gmail.com
 *
 * Version 0.4.0, 2010-05-10
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
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using DotNetOpenId;
using DotNetOpenId.Extensions.AttributeExchange;
using DotNetOpenId.Extensions.SimpleRegistration;
using DotNetOpenId.RelyingParty;
using log4net;
using MindTouch.Deki;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Contrib.OpenID {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch OpenID Relying Party Service", "Copyright © 2009, 2010 Craig Box",
        Info = "http://developer.mindtouch.com/App_Catalog/OpenID",
        SID = new[] { "sid://contrib.mindtouch.com/deki/2009/11/openid" }
    )]
    [DreamServiceConfig("valid-id-pattern", "string?", "Regular expression pattern for identifiers that will be accepted by this service.")] 
    public class OpenIDService : DekiExtService {

        // --- Fields ---
        private String _validIdPattern;

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            // set up a Plug to deki for the eventual trusted authentication
            Plug.New(config["uri.deki"].AsUri);
            _validIdPattern = config["valid-id-pattern"].AsText;

            result.Return();
        }

        [DreamFeature("POST:authenticate", "Authenticate a user with MindTouch via OpenID.")]
        [DreamFeatureParam("url", "string", "The OP identifier used to authenticate.")]
        [DreamFeatureParam("returnurl", "string", "The URL to instruct the provider to return to after authentication.")]
        [DreamFeatureParam("realm", "string?", "The realm to claim an OpenID identifier for.")]
        public Yield UserLogin(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            
            string userSuppliedIdentifier = context.GetParam("url", null);
            if (String.IsNullOrEmpty(userSuppliedIdentifier)) {
                _log.Info("No identifier was specified");
                throw new DreamBadRequestException("No identifier was specified.");
            }

            XUri returnUri = new XUri(context.GetParam("returnurl", null));
            String realm = context.GetParam("realm", null);
            if (String.IsNullOrEmpty(realm)) {
                realm = returnUri.WithoutPathQueryFragment().ToString();
            }

            IAuthenticationRequest openIdRequest;

            // dummy parameters required by DotNetOpenId 2.x; in 3.x, you can
            // just pass null to the OpenIdRelyingParty constructor.
            Uri identifierUri = new Uri(userSuppliedIdentifier);
            NameValueCollection queryCol = System.Web.HttpUtility.ParseQueryString(identifierUri.Query);
            OpenIdRelyingParty openid = new OpenIdRelyingParty(null, identifierUri, queryCol);
           
            // creating an OpenID request will authenticate that 
            // the endpoint exists and is an OpenID provider.
            _log.DebugFormat("Creating OpenID request: identifier {0}, return URL {1}, realm {2}", userSuppliedIdentifier, returnUri.ToString(), realm); 

            try {
                openIdRequest = openid.CreateRequest(
                    userSuppliedIdentifier,
                    realm,
                    returnUri.ToUri());
            } catch (OpenIdException ex) {
                _log.WarnFormat("'{0}' rejected as OpenID identifier: {1}", userSuppliedIdentifier, ex.Message);
                throw new DreamBadRequestException(string.Format("'{0}' is not a valid OpenID identifier. {1}", userSuppliedIdentifier, ex.Message));
            }

            // Ask for the e-mail address on this request.
            // Use both SREG and AX, to increase the odds of getting it.
            openIdRequest.AddExtension(new ClaimsRequest{
                Email = DemandLevel.Require,
            });

            var fetch = new FetchRequest();
            fetch.AddAttribute(new AttributeRequest(WellKnownAttributes.Contact.Email, true));
            openIdRequest.AddExtension(fetch);

            // The RedirectingResponse either contains a "Location" header for 
            // a HTTP GET, which will return in the response as 'endpoint', or
            // a HTML FORM which needs to be displayed to the user, which will
            // return in the response as 'form'.
            IResponse wr = openIdRequest.RedirectingResponse;

            XDoc result = new XDoc("openid");
            if (String.IsNullOrEmpty(wr.Headers["Location"])) {
                System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
                string formBody = enc.GetString(wr.Body);
                _log.DebugFormat("OpenID redirect by HTML FORM: {0}", formBody);  
                result.Attr("form", formBody);  
            } else {
                string redirectUrl = wr.Headers["Location"];
                _log.DebugFormat("OpenID redirect URL: {0}", redirectUrl);
                result.Attr("endpoint", redirectUrl);
            }

            response.Return(DreamMessage.Ok(result));
            yield break;
        }
        
        
        [DreamFeature("POST:validate", "Validate a returned response from an OpenID provider.")]
        [DreamFeatureParam("url", "string", "The URL that was returned to by the OpenID provider.")]
        [DreamFeatureParam("query", "string", "The query string, which will be parsed for openid.* parameters.")]
        public Yield ValidateOpenIdResponse(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            XUri publicUri = new XUri(context.GetParam("url", null));
            NameValueCollection queryColl = System.Web.HttpUtility.ParseQueryString(context.GetParam("query", null));

            // process the response, including validating the endpoint of the claimed identifier.
            OpenIdRelyingParty openid = new OpenIdRelyingParty(null, publicUri, queryColl);
            var openIdResponse = openid.Response;

            if (openIdResponse != null) {
                switch (openIdResponse.Status) {
                    case AuthenticationStatus.Authenticated:
                       
                        // Throw an exception if there is a regex for acceptable
                        // identifiers defined, and the ID does not match.
                        if (!String.IsNullOrEmpty(_validIdPattern)) {
                            Regex identifierAccept = new Regex(_validIdPattern);
                            if (!identifierAccept.IsMatch(openIdResponse.ClaimedIdentifier)) {
                                _log.InfoFormat("Identifier {0} denied access by valid-id-pattern regular expression {1}", openIdResponse.ClaimedIdentifier, _validIdPattern);
                                throw new DreamBadRequestException("This service is configured to deny access to this OpenID identifier.");
                            }
                        }

                        var claimsResponse = openIdResponse.GetExtension<ClaimsResponse>();
                        var fetchResponse = openIdResponse.GetExtension<FetchResponse>();

                        XDoc result = new XDoc("openid");
                        result.Attr("validated", true);
                        result.Elem("identifier", openIdResponse.ClaimedIdentifier);
			
                        // SREG response
                        if (claimsResponse != null) {
                            string email = claimsResponse.Email;
                            if (email != null) {
                                result.Elem("email", email);
                                _log.DebugFormat("E-mail address from SREG: {0}", email);
                            }
                        }
                        // AX response
                        if (fetchResponse != null) {
                            foreach (AttributeValues v in fetchResponse.Attributes) {
                                if (v.TypeUri == WellKnownAttributes.Contact.Email) {
                                    IList<string> emailAddresses = v.Values;
                                    string email = emailAddresses.Count > 0 ? emailAddresses[0] : null;
                                    result.Elem("email", email);
                                    _log.DebugFormat("E-mail address from AX: {0}", email);
                                }
                            }
                        }
                        response.Return(DreamMessage.Ok(result));
                        break;
                    case AuthenticationStatus.Canceled:
                        _log.InfoFormat("Authentication was cancelled by the user.");
                        throw new DreamBadRequestException("Authentication was cancelled by the user.");
                    case AuthenticationStatus.Failed:
                        _log.InfoFormat("Authentication failed: " + openIdResponse.Exception.Message);
                        throw new DreamBadRequestException("Authentication failed: " + openIdResponse.Exception.Message);
                    default:
                        _log.WarnFormat("Authentication error: " + openIdResponse.Exception.Message);
                        throw new DreamBadRequestException("Authentication error: " + openIdResponse.Exception.Message);
                }
            } else {
                _log.Warn("OpenID response was null");
                throw new DreamBadRequestException("No OpenID response was returned.");
            }
            yield break;
        }      
    }
}
