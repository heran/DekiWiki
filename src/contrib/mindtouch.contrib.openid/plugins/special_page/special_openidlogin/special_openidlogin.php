<?php
/* vim: set ts=8: */

/*
 * OpenID relying party support for MindTouch Core
 * Copyright � 2009 Craig Box
 * craig.box@gmail.com
 *
 * Version 0.3.2, 2010-01-31
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

include_once("OpenIdProperties.php");

if (defined('MINDTOUCH_DEKI')) :

DekiPlugin::registerHook('Special:OpenIdLogin', array('OpenIdLogin', 'create'));

class OpenIdLogin extends SpecialPagePlugin
{
	const SPECIAL_OPENID_LOGIN = 'OpenIdLogin';
	protected $pageName = 'OpenIdLogin';

	public static function create($pageName, &$pageTitle, &$html)
	{
		$Special = new self($pageName, basename(__FILE__, '.php'));
		$pageTitle = 'Log in with OpenID';
		$html = $Special->output();
	}

	public static function init()
	{

	}

	protected function &output()
	{
		$this->includeSpecialCss('css/style.css');
		$this->includeSpecialJavascript('js/jquery.openid.js');

		$Request = DekiRequest::getInstance();
		$User = DekiUser::getCurrent();

		global $wgDekiOpenIdService;

		// After we are redirected here from the endpoint, we have a bunch of
		// query parameters.  If one of them is "openid.mode", then we have been through
		// an OP. We send its response to the API to check, and if it is positive and
		// correct, we go back to the API to create/log in that user.

		if (!is_null($Request->getVal('openid.mode')))
		{
			$url = $Request->getFullUri();

			if ($Request->isPost())
			{
				$query_string = file_get_contents('php://input');
			}
			else
			{
				$query_string = $_SERVER['QUERY_STRING'];
			}

			$Plug = DekiPlug::NewPlug($wgDekiOpenIdService);
			$Result = $Plug->At('validate')
			->With('url', $url)
			->With('query', $query_string)
			->Post();

			if ($Result->isSuccess())
			{
				// Success, do trusted authentication.  NOTE: we can't call
				// into this directly from PHP, as curl will eat our URL
				// when it Base64 encodes the string "username:password"; the
				// username is a URL, which contains a colon, so the username
				// will only show up as 'http(s)'.
				global $wgDekiApiKey;

				if ($Result->getVal('body/openid/identifier')) {
					$claimed_id = $Result->getVal('body/openid/identifier');
				} else {
					DekiMessage::error('No identifier was returned from the API.');
					return self::displaySelector();
				}
				// Check the site property to see if there is a user matching

				$openIdProperties = new OpenIdProperties;
				$foundUsername = $openIdProperties->getUsername($claimed_id);
				
				if (empty($foundUsername)) 
				{
				        global $wgAnonAccCreate;
                                        if ($wgAnonAccCreate) 
					{
						$returnTo = $Request->getVal('returntotitle');
						self::redirect(Title::newFromText("OpenIDCreateUser", NS_SPECIAL)
						->getFullURL() . "?openid=" . urlencode($claimed_id) . "&hash=" . md5($wgDekiApiKey . $claimed_id) . "&returntotitle=" . urlencode($returnTo));
					} 
					else
					{
						DekiMessage::error("Your OpenID is not associated with an account and account creation is disabled.");
						return self::displaySelector();
					}
				} 
				else
				{
					$authTokenResult = DekiPlug::getInstance()->At("users", "authenticate")
					->With('apikey', $wgDekiApiKey)
					->WithCredentials($foundUsername, '')
					->With('authprovider', 1)
					->Post();
					
					if ($authTokenResult->isSuccess())
					{
						$authToken = $authTokenResult->getVal('body');
						DekiToken::set($authToken);
						self::redirectTo();
						return;
					}
					else
					{
						if ($Result->getError())
						{
							DekiMessage::error($Result->getError());
						}
						else
						{
							DekiMessage::error("Cannot connect to OpenID service (user creation; is trusted authentication enabled?)");
						}
						return self::displaySelector();
					}	
				}
			}
			else
			{
				if ($Result->getError())
				{
					DekiMessage::error($Result->getError());
				}
				else
				{
					DekiMessage::error("Cannot connect to OpenID service (validating response from OP)");
				}
				return self::displaySelector();
			}
		}

		// You are here if the form has been submitted.
		if ($Request->isPost())
		{
			// On post-back, we take the submitted endpoint and send it to the API
			// for validation.  This will check if the endpoint is valid, and if so,
			// return a URL which we then redirect the user to.

			// get the title of this page, so we can set this page as the redirect URL
			$Title = $this->getTitle();
			$returnTo = $Request->getVal('returnto');

			// Send the endpoint to POST:openid/authenticate
			$url = $Request->getVal('url');
			$Plug = DekiPlug::NewPlug($wgDekiOpenIdService);
			$Result = $Plug->At('authenticate')
			->With('url', $url)
			->With('returnurl', $Title->getFullUrl() . '?returntotitle=' . urlencode($returnTo))
			->Post();

			// success: either a GET URL or a form to display will be returned.
			// failure: display error message and page.
			if ($Result->isSuccess())
			{
				if ($Result->getVal('body/openid/@endpoint'))
				{
					// endpoint contains a location to redirect to
					self::redirect($Result->getVal('body/openid/@endpoint'));
					return;
				}
				elseif ($Result->getVal('body/openid/@form'))
				{
					// form contains HTML to display to the client, which will be
					// automatically submitted with JavaScript
					return $Result->getVal('body/openid/@form');
				}
			}

			// if we have not redirected to an endpoint by either method, display an error.
			if ($Result->getError())
			{
				DekiMessage::error($Result->getError());
			}
			else
			{
				DekiMessage::error("Cannot connect to OpenID service (identifier validation & endpoint discovery)");
			}
			return self::displaySelector();
		}
		// if called with no parameters, display the OpenID selector widget.
		return self::displaySelector();
	}

	public function displaySelector()
	{
		$Title = $this->getTitle();
		$Request = DekiRequest::getInstance();

		return '<script type="text/javascript"><!--//' . PHP_EOL . '$(function() { $(\'#openid\').openid({}); });  //--></script>'
			.'<form class="MT-addon-form" method="post" action="' . $Title->getLocalUrl() . '" id="openid">'
				.'<div class="MT-addon-form-row">'
					.'<input type="hidden" name="returnto" value="'
						.htmlspecialchars($Request->getVal('returntotitle'))
					.'"/>'
				.'</div>'
			.'</form>';
	}
}
endif;
