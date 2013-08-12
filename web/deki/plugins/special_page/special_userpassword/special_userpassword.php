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
	DekiPlugin::registerHook(Hooks::SPECIAL_USER_PASSWORD, 'wfSpecialUserPassword');
}

function wfSpecialUserPassword($pageName, &$pageTitle, &$html)
{
	$Special = new SpecialUserPassword($pageName, basename(__FILE__, '.php'));
	
	// set the page title
	$pageTitle = $Special->getPageTitle();
	$html = $Special->output();
}

class SpecialUserPassword extends SpecialPagePlugin
{
	protected $pageName = 'UserPassword';

	public function output()
	{
		$Request = DekiRequest::getInstance();
		$User = DekiUser::getCurrent();
		if (!$User->isAnonymous())
		{
			$Title = Title::newFromText('UserLogin', NS_SPECIAL);
			self::redirect($Title->getLocalUrl());
			return;
		}

		$this->includeSpecialCss('special_userpassword.css');

		if ($Request->isPost())
		{
			do
			{
				$username = $Request->getVal('username');
				// bugfix #6268: Anonymous user can't run API query to reset password
				$User = null; 
				$Plug = DekiPlug::getInstance();
				$Result = $Plug->At('users', '='. $username)->WithApiKey()->Get();
				if ($Result->isSuccess())
				{
					$User = DekiUser::newFromArray($Result->getVal('body/user'));
				}

				if (is_null($User))
				{
					// unknown user
					DekiMessage::error(wfMsg('Page.UserPassword.error.no-user-by-name', htmlspecialchars($username)));
					break;
				}
				
				$email = $User->getEmail(true);
				if (empty($email))
				{
					// no email address
					DekiMessage::error(wfMsg('Page.UserPassword.error.no-email-for-user', $User->toHtml()));
					break;
				}

				// set a new password
				$newPassword = wfRandomStr();
				$Result = $User->changePassword($newPassword, null, true);
				if (!$Result->handleResponse())
				{
					DekiMessage::error(wfMsg('Page.UserPassword.error.general'));
					break;
				}

				// send the user an email
				global $wgIP, $wgSitename;
				$userName = $User->toHtml();
				$Title = Title::newFromText('UserLogin', NS_SPECIAL);

				$result = DekiMailer::sendEmail(
					$email,
					wfMsg('Page.UserPassword.email.subject', $userName), 
					wfMsg('Page.UserPassword.email.body.text', $userName, $newPassword, $wgSitename, $Title->getFullUrl())
				);

				if (!$result)
				{
					DekiMessage::error(wfMsg('Page.UserPassword.error.email-failed'));
					break;
				}

				DekiMessage::success(wfMsg('Page.UserPassword.success.email-sent', $User->toHtml()));

				// redirect back to the login page
				self::redirect($Title->getLocalUrl());
				return;
			} while (false);
		}

		return self::getForgotForm();
	}

	protected static function getForgotForm()
	{
		$Title = Title::newFromText('UserPassword', NS_SPECIAL);
		// build the markup
		$html = '<form method="post" action="'. $Title->getLocalUrl() .'">';

		$html .= '<div class="field">';
			$html .= DekiForm::singleInput('text', 'username', null, array(), wfMsg('Page.UserLogin.user-name'));
		$html .= '</div>';
		
	
		// submit button
		$html .= '<div class="buttons">';
			$html .= DekiForm::singleInput('button', 'action', 'login', array(), wfMsg('Page.UserPassword.form.submit'));
		$html .= '</div>';
		$html .= '</form>';

		return $html;
	}
}
