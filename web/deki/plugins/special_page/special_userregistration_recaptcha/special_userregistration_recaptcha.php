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
	require_once('recaptchalib.php');
	DekiPlugin::registerHook(Hooks::SPECIAL_USER_REGISTRATION, 'wfSpecialUserRegistrationRecaptcha', 8);
}

function wfSpecialUserRegistrationRecaptcha($pageName, &$pageTitle, &$html)
{
	$Special = new SpecialUserRegistrationRecaptcha($pageName, basename(__FILE__, '.php'));
	// set the page title
	$pageTitle = $Special->getPageTitle();
	$html = $Special->output();
	if ($html === false) {
		return; 
	}
	return DekiPlugin::HANDLED_HALT;
}

class SpecialUserRegistrationRecaptcha extends SpecialUserRegistration
{
	protected $pageName = 'UserRegistration';

	public function output()
	{
		global $wgRecaptchaPublicKey, $wgRecaptchaPrivateKey; 
		
		if (!isset($wgRecaptchaPublicKey) && !isset($wgRecaptchaPrivateKey)) 
		{
			DekiMessage::error(wfMsg('Page.UserRegistrationRecaptcha.error-setup')); 
			return false; 	
		}
		// add some css
		$this->includeSpecialCss('special_userregistration_recaptcha.css');

		$Request = DekiRequest::getInstance();
		$User = DekiUser::getCurrent();

		if (!$User->isAnonymous())
		{
			// if user already has an account, redirect
			self::redirectTo();
			return;
		}

		global $wgAnonAccCreate;
		if (isset($wgAnonAccCreate) && !$wgAnonAccCreate)
		{
			DekiMessage::error(wfMsg('Page.UserRegistration.error.anonymous-disabled'));
			self::redirectTo();
			return;
		}

		if ($Request->isPost())
		{
			do
			{
				$username = $Request->getVal('username');
				$email = $Request->getVal('email');
				$password = $Request->getVal('password');
				$passwordVerify = $Request->getVal('password_verify');
				
				$Recaptcha = recaptcha_check_answer($wgRecaptchaPrivateKey, $Request->getClientIP(), $Request->getVal('recaptcha_challenge_field'), $Request->getVal('recaptcha_response_field'));
				
				if (is_null($Request->getClientIP())) 
				{
					$Request->remove('captcha_input');
					DekiMessage::error(wfMsg('Page.UserRegistrationRecaptcha.error-remote')); 
					break;
				}	
							
				if (!$Recaptcha->is_valid)
				{
					// remove the captcha text from the post
					$Request->remove('captcha_input');
					DekiMessage::error(wfMsg('Page.UserRegistration.error.captcha'));
					break;
				}

				// validate username, email, & password
				if (!self::validateUsername($username) || !self::validateEmail($email) || !self::validatePassword($password, $passwordVerify))
				{
					break;
				}

				// everything is good, create the user
				
				// check if any plugins want to handle the user creation
				$result = DekiPlugin::executeHook(
					Hooks::MAIN_CREATE_USER,
					array(
						&$username,
						&$password,
						&$email
					)
				);
				
				if ($result == DekiPlugin::HANDLED_HALT)
				{
					// user creation is complete?
					$User = DekiUser::getCurrent();
					DekiPlugin::executeHook(Hooks::MAIN_CREATE_USER_COMPLETE, array($User));
					break;
				}
				
				// default creation process
				// create the user object
				$User = new DekiUser(null, $username, null, $email);
				$Result = $User->create(null, null, $password);
				
				if ($Result->getStatus() == 409) 
				{
					DekiMessage::error(wfMsg('Page.UserRegistration.error.exists', htmlspecialchars($username)));
					break;
				}
				elseif (!$Result->handleResponse())
				{
					// problem when trying to create the user
					DekiMessage::error(wfMsg('Page.UserRegistration.error.creation'));
					break;
				}

				// new user was created successfully
				DekiMessage::success(wfMsg('Page.UserRegistration.success.created', $User->toHtml()));
				
				// login
				DekiUser::login($username, $password);
								
				// send the user an email
				$result = self::sendUserWelcomeEmail($User, $password);				
				if (!$result)
				{
					DekiMessage::error(wfMsg('Page.UserRegistration.error.email.send'));
					break;
				}
				// /end user email
				
				// calling complete here allows users to redirect to their own destination
				$User = DekiUser::getCurrent();
				$result = DekiPlugin::executeHook(Hooks::MAIN_CREATE_USER_COMPLETE, array($User));
				if ($result == DekiPlugin::HANDLED_HALT)
				{
					break;
				}
				
				// determine where to redirect the user
				global $wgRedirectToUserPageOnCreate;
				if (isset($wgRedirectToUserPageOnCreate) && $wgRedirectToUserPageOnCreate)
				{
					self::redirect($User->getUrl());
				}
				else
				{
					self::redirectTo();
				}

				return;
			} while (false);
		}


		$html = self::getNewAccountForm();

		return $html;
	}
	
	protected static function getNewAccountForm()
	{
		global $wgRecaptchaPublicKey;
		$Title = Title::newFromText('UserRegistration', NS_SPECIAL);
		// build the markup
		$html = '<form class="user-registration" method="post" action="'. $Title->getLocalUrl() .'">';
		
		// username field
		$html .= '<div class="field">';
			$html .= DekiForm::singleInput('text', 'username', null, array(), wfMsg('Page.UserRegistration.form.username'));
		$html .= '</div>';

		// email field
		$html .= '<div class="field">';
			$html .= DekiForm::singleInput('text', 'email', null, array(), wfMsg('Page.UserRegistration.form.email'));
		$html .= '</div>';

		// password field
		$html .= '<div class="field">';
			$html .= DekiForm::singleInput('password', 'password', null, array(), wfMsg('Page.UserRegistration.form.password'));
		$html .= '</div>';
		
		// password confirmation
		$html .= '<div class="field">';
			$html .= DekiForm::singleInput('password', 'password_verify', null, array(), wfMsg('Page.UserRegistration.form.password.verify'));
		$html .= '</div>';

		// captcha
		$html .= '<div class="captcha">';
		
		$Request = DekiRequest::getInstance();
		$html .= recaptcha_get_html($wgRecaptchaPublicKey, $recaptcha_error, $Request->isSsl());
		$html .= '</div>';
		
		// submit button
		$html .= DekiForm::singleInput('button', 'action', 'login', array(), wfMsg('Page.UserRegistration.form.submit'));
		$html .= '</form>';

		return $html;
	}
}
