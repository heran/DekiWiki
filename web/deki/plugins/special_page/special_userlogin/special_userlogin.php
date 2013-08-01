<?php
/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
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

if (defined('MINDTOUCH_DEKI'))
{
	DekiPlugin::registerHook(Hooks::SPECIAL_USER_LOGIN, 'wfSpecialUserLogin');
}

function wfSpecialUserLogin($pageName, &$pageTitle, &$html)
{
	$Special = new SpecialUserLogin($pageName, basename(__FILE__, '.php'));
	
	// set the page title
	$pageTitle = $Special->getPageTitle();
	$html = $Special->output();
}

class SpecialUserLogin extends SpecialPagePlugin
{
	protected $pageName = 'UserLogin';

	public function output()
	{
		// add some css
		$this->includeSpecialCss('special_userlogin.css');
		$this->includeSpecialJavascript('special_userlogin.js');
		
		$Request = DekiRequest::getInstance();
		$User = DekiUser::getCurrent();
	
		// guerrics: checks for old registration link and forwards to new reg page
		$register = $Request->getBool('register', false);
		if ($register)
		{
			$Title = Title::newFromText('UserRegistration', NS_SPECIAL);
			self::redirectTo($Title);
			return;
		}
		
		if ($Request->isPost())
		{
			// attempt to login the user
			$username = $Request->getVal('username');
			$password = $Request->getVal('password');
			$authId = $Request->getVal('auth_id', DekiAuthService::INTERNAL_AUTH_ID);
			
			$result = DekiPlugin::executeHook(Hooks::MAIN_LOGIN, array($username, $password, $authId));
			if ($result == DekiPlugin::HANDLED_HALT)
			{
				// login is complete?
				$User = DekiUser::getCurrent();
				DekiPlugin::executeHook(Hooks::MAIN_LOGIN_COMPLETE, array($User));
			}
			else
			{
				// default login process
				$status = DekiUser::login($username, $password, $authId);
				
				if ($status === true)
				{
					$User = DekiUser::getCurrent();
					DekiPlugin::executeHook(Hooks::MAIN_LOGIN_COMPLETE, array($User));
				
					// user has been logged in, return to sender
					// administrators who are logging into expired/inactive states should be redirected to the product activation page directly
					// note: we don't need to check user state, because the API already returns a 403
					if (DekiSite::isDeactivated()) 
					{
						self::redirect(wfGetControlPanelUrl('product_activation'));
						return;	
					}
	
					// magically detect where to redirect to
					self::redirectTo();
				}
				else
				{
					// handle the failure status
					switch ($status)
					{
						case '401':
							// check if the user exists
							$Lookup = DekiUser::newFromText($username);
							if (!is_null($Lookup))
							{
								$Title = Title::newFromText('UserPassword', NS_SPECIAL);
								DekiMessage::error(wfMsg('Page.UserLogin.error.password', $Title->getLocalURL()));
							}
							else
							{
								// couldn't find user -- check if anonymous account creation enabled
								$Current = DekiUser::getAnonymous();
								$Service = DekiService::newFromId($authId);
								if (!is_null($Service) && $Service->canCreateAccount($User))
								{
									$query = 'username='.urlencode($username);
									$Title = Title::newFromText('UserRegistration', NS_SPECIAL);
									DekiMessage::error(wfMsg('Page.UserLogin.error.user', $Title->getLocalUrl($query)));
								}
								else
								{
									// bug #7465, #6742: keep error messages generic in case of remote auth
									DekiMessage::error(wfMsg('Page.UserLogin.error'));
								}
							}
							unset($Lookup);
							break;
		
						case '403':
							if (DekiSite::isDeactivated()) 
							{
								DekiMessage::error(wfMsg('Page.UserLogin.status.expired'));
							}
							else
							{
								global $wgSitename;
								DekiMessage::error(wfMsg('Page.UserLogin.user-cannot-access-login', $wgSitename));
							}
							break;
		
						default:
							DekiMessage::error(wfMsg('Page.UserLogin.error'));
					}
				}
			}
		}

		$html = '';
		// make sure the user isn't logged in already
		if ($User->isAnonymous())
		{
			$html .= self::getLoginForm();	
		}
		else
		{
			// user is already logged in, fire a hook
			$result = DekiPlugin::executeHook(Hooks::MAIN_LOGIN_REFRESH, array($User, &$html));
			if ($result == DekiPlugin::HANDLED_HALT)
			{
				// don't output the default html if the plugin halts
				return $html;
			}
			
			// user is already logged in
			$html .= wfMsg('Page.UserLogin.current-user', $User->toHtml());

			$returnTo = $Request->getVal('returntotitle');
			if (!empty($returnTo)) 
			{
				$Title = Title::newFromText($returnTo);
				$html .= '<p>'. wfMsg('Page.UserLogin.return-to', $Title->getFullUrl()) .'</p>';
			} 	
		}
		
		return $html;
	}

	protected static function getLoginForm()
	{
		$Request = DekiRequest::getInstance();
		
		$siteServices = DekiAuthService::getSiteList();
		$defaultProviderId = DekiAuthService::getDefaultProviderId();
		
		$services = array();
		// optionally hide the local auth if set
		// custom wg variable to hide the local authentication provider safely
		global $wgHideLocalAuth;
		$hideLocal = isset($wgHideLocalAuth) ? (bool)$wgHideLocalAuth : false;

		foreach ($siteServices as $Service)
		{
			if ($Service->isInternal() && $hideLocal)
			{
				if ($defaultProviderId == $Service->getId())
				{
					$defaultProviderId = null;
				}
				// don't add the local auth
				continue;
			}

			if (is_null($defaultProviderId))
			{
				$defaultProviderId = $Service->getId();
			}
			$services[$Service->getId()] = $Service->getName();
		}
		
		// check if the default provider is availabile
		if (!isset($services[$defaultProviderId]) && !empty($services))
		{
			// default provider is not available, assign first available
			reset($services);
			$defaultProviderId = key($services);
		}
		
		// sort the providers
		asort($services);
		
		$Title = Title::newFromText('UserRegistration', NS_SPECIAL);
		$createAccountUrl = $Title->getLocalUrl();
		$Title = Title::newFromText('UserPassword', NS_SPECIAL);
		$forgotPasswordUrl = $Title->getLocalUrl();


		if (empty($services))
		{
			// no auth services are enabled, impossible to login
			$html = wfMsg('Page.UserLogin.no-auth-services');
		}
		else
		{
			$Title = Title::newFromText('UserLogin', NS_SPECIAL);
			// build the markup
			$html = '<form method="post" action="'. $Title->getLocalUrl() .'" class="user-login">';

			// display the available auth services
			$html .= '<fieldset>';
			
			if (count($services) > 1) 
			{
				$html .= '<legend>'. wfMsg('Page.UserLogin.form.login-service') . '</legend>';
				$html .= DekiForm::multipleInput('radio', 'auth_id', $services, $defaultProviderId);
				$html .= '</fieldset>';
			}
			else 
			{
				$html .= DekiForm::singleInput('hidden', 'auth_id', $defaultProviderId);	
			}
			
			$html .= '<div class="field">';
				$html .= DekiForm::singleInput('text', 'username', null, array('tabindex' => '1', 'autocomplete' => 'on'), wfMsg('Page.UserLogin.user-name'));
			// only display the account creation link if enabled
			global $wgAnonAccCreate;
			if (isset($wgAnonAccCreate) && $wgAnonAccCreate)
			{
				$html .= '<div class="create-account">';
					$html .= '<a href="'. $createAccountUrl .'">'. wfMsg('Page.UserLogin.create-account') .'</a>';
				$html .= '</div>';
			}
			// end the username field
			$html .= '</div>';
			
			$html .= DekiForm::singleInput('hidden', 'returntotitle', $Request->getVal('returntotitle'));
			$html .= DekiForm::singleInput('hidden', 'returntourl', $Request->getVal('returntourl'));
			
			// password field
			$html .= '<div class="field">';
				$html .= DekiForm::singleInput('password', 'password', null, array('tabindex' => '2'), wfMsg('Page.UserLogin.user-password'));
				$html .= '<div class="forgot-password">';
					$html .= '<a href="'. $forgotPasswordUrl .'">'. wfMsg('Page.UserLogin.forgot-password') .'</a>';
				$html .= '</div>';
			$html .= '</div>';
			
			// submit button
			$html .= DekiForm::singleInput('button', 'action', 'login', array('tabindex' => '3'), wfMsg('Page.UserLogin.submit-login'));
			$html .= '</form>';
		}

		return $html;
	}

}
