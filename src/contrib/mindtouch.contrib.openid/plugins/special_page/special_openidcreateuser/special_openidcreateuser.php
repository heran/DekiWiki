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

include_once(dirname(__FILE__) . "/../special_openidlogin/OpenIdProperties.php");

if (defined('MINDTOUCH_DEKI')) :

DekiPlugin::registerHook('Special:OpenIdCreateUser', array('OpenIdCreateUser', 'create'));

class OpenIdCreateUser extends SpecialPagePlugin
{
	const SPECIAL_OPENID_LOGIN = 'OpenIdCreateUser';
	protected $pageName = 'OpenIdCreateUser';

	public static function create($pageName, &$pageTitle, &$html)
	{
		$Request = DekiRequest::getInstance();
		$Special = new self($pageName, basename(__FILE__, '.php'));
		global $wgDekiApiKey;
		
		// validate hash
		$openid = urldecode($Request->getVal('openid'));
		$hash = $Request->getVal('hash');
		
		if ($hash != md5($wgDekiApiKey . $openid))
		{
			$pageTitle = 'Error';
			DekiMessage::error('You must have signed in with OpenID to use this page.');
			return;
		}
		$pageTitle = 'Select a username';
		$html = $Special->output();
	}

	public static function init()
	{

	}

	protected function &output()
	{
		$this->includeSpecialJavascript('js/special_openidcreateuser.js');
				
		$Request = DekiRequest::getInstance();
		$User = DekiUser::getCurrent();
		global $wgDekiApiKey;
		
		// You are here if the form has been submitted with a correct hash.
		if ($Request->isPost())
		{
			// This page will only ever create a new user.  You should not be able to 
			// use a name that already exists.
			$username = $Request->getVal('username');
			$openid = urldecode($Request->getVal('openid'));
						
			if (strlen($username) == 0) {
				DekiMessage::error("You must enter a username.");
				return self::displayForm();		
			}
			
			$Result = DekiPlug::getInstance()->At("users", "=" . $username)->Get();
			if ($Result->isSuccess()) 
			{
				DekiMessage::error("The selected username is already taken.");
				return self::displayForm();			
			}
			
			// If the title is going to be invalid, we should stop them submitting that too.
			if (preg_match("#^\/|^\.\.$|^\.$|^\./|^\.\./|/\./|/\.\./|/\.$|/\..$|\/$#", $username))
			{
				DekiMessage::error("The selected username is invalid.");
				return self::displayForm();
			}
						
			// Create the user
			$Result = DekiPlug::getInstance()->At("users", "authenticate")
				->With('apikey', $wgDekiApiKey)
				->With('authprovider', 1)
				->WithCredentials($username, '')
				->Post();

			if ($Result->isSuccess())
			{
				$openIdProperties = new OpenIdProperties;
				$openIdProperties->setUsername($openid, $username);
				
				$authToken = $Result->getVal('body');
				DekiToken::set($authToken);
				self::redirectTo();
				return;
			}
			else
			{
				DekiMessage::error($Result->getError());
				return self::displayForm();
			}
		}
		
		return self::displayForm();
		
	}

	public function displayForm() {

		DekiPlugin::loadResources('special_page/special_openidcreateuser', 'resources.custom.txt');

		$Request = DekiRequest::getInstance();
		$openid = $Request->getVal('openid');
		
		global $wgDekiApiKey;
		
		$html .= '<div class="MT-addon-openIdCreateUser" class="MT-addon-openIdCreateUser">'
				.'<p>Your OpenID is not yet associated with a user account.</p>'
				.'<form id="createuser" method="post" class="createuser MT-addon-form">'
					.'<div class="field MT-addon-form-row">'
						.'<label>Username:</label>'
						.DekiForm::singleInput('text', 'username', null, array())
							.'&nbsp;'
						.'<span id="available"></span>'
					.'</div>'
					.'<br/>'
					.'<div class="MT-addon-form-row MT-addon-form-buttons">'
						.'<button class="input-button" value="save" name="deki_buttons[action][save]" type="submit" disabled="">'
							.'<span>'
							. wfMsg('OpenIdCreateUser.create')
							.'</span>'
						.'</button>'
						.DekiForm::singleInput('hidden', 'openid', urlencode($openid))
						.DekiForm::singleInput('hidden', 'hash', md5($wgDekiApiKey . $openid))
					.'</form>'
				."<script language=\"JavaScript\">$('#createuser').find (':submit').attr ('disabled', 'disabled');</script>";
			'</div>';
		return $html;
	}
}
endif;
