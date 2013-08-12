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
	DekiPlugin::registerHook(Hooks::SPECIAL_USER_PREFERENCES, 'wfSpecialUserPreferences');
	DekiPlugin::registerHook('Special:Preferences', 'wfSpecialUserPreferences');
	DekiPlugin::registerHook(Hooks::DATA_GET_SPECIAL_PAGES, array('SpecialUserPreferences', 'getSpecialPagesHook'));
}

// include the base advanced properties class
DekiPlugin::requirePhp('special_page', 'special_advanced_properties.php');

function wfSpecialUserPreferences($pageName, &$pageTitle, &$html)
{
	$Special = new SpecialUserPreferences($pageName, basename(__FILE__, '.php'), true);
	
	// set the page title
	$pageTitle = $Special->getPageTitle();
	$html = $Special->output();
}

class SpecialUserPreferences extends SpecialAdvancedProperties
{
	protected $pageName = 'UserPreferences';
	protected $allowAnonymous = false;


	public static function getSpecialPagesHook(&$pages)
	{
		$Special = new self();
		$name = $Special->getPageTitle();
		$href = $Special->getTitle()->getFullURL();
		
		$pages[$name] = array('name' => $name, 'href' => $href);
	}
	
	
	public function output()
	{
		// add some css
		$this->includeSpecialCss('special_userpreferences.css');
		$this->includeSpecialJavascript('special_userpreferences.js');
		
		$Request = DekiRequest::getInstance();
		$User = DekiUser::getCurrent();

		// configure the advanced properties
		$this->setPropertiesId($User->getId());
		$this->setPropertiesType('user');
		$html = $this->outputProperties($Request, $User->Properties);
		
		// add the wrapping div
		return 
			'<div id="deki-userpreferences">' .
				$html .
			'</div>';
	}
	
	protected function processSimpleForm($Request, $UserProperties)
	{
		if (!DekiForm::hasValidToken()) {
			DekiMessage::error(wfMsg('System.error.error-csrf'));
			return;
		}
		
		do
		{
			$User = DekiUser::getCurrent();
			
			// general information
			$email = $Request->getVal('email');
			// check if the user is trying to change their email address
			if ($User->getEmail() != $email)
			{
				if (!$User->setEmail($email))
				{
					DekiMessage::error(wfMsg('Page.UserPreferences.error.invalid-email'));
					break;
				}
			}

			// internal user options
			$language = $Request->getVal('language');
			$timezone = $Request->getVal('timezone');
			$tztype = $Request->getVal('tztype');
			if ($tztype == 'site') 
			{
				$timezone = null;	
			}
			// validate user options
			$User->setLanguage($language);
			$User->setTimezone(!is_null($timezone) ? validateTimeZone($timezone, -12, 14) : null);
						
			// update the user's general information
			$Result = $User->update();
			if (!$Result->isSuccess())
			{
				DekiMessage::error($Result->getError());
				break;
			}
			
			
			// user properties
			global $wgEnableSearchHighlight;
			// if we don't do this, when search highlighting is disabled site-wide, it'll overwrite values
			if ($wgEnableSearchHighlight)
			{
				$User->Properties->setHighlightOption($Request->getVal('highlight') == 'true');
				$Result = $User->Properties->update();
				if (!$Result->handleResponse())
				{
					DekiMessage::error($Result->getError());
					break;
				}
			}

			
			// passwords
			$oldPassword = $Request->getVal('old_password');
			$newPassword = $Request->getVal('new_password');
			$newPasswordVerify = $Request->getVal('new_password_verify');
					
			if (!empty($oldPassword))
			{
				if ($newPassword != $newPasswordVerify)
				{
					DekiMessage::error(wfMsg('Page.UserRegistration.error.password.match'));
					break;
				}
				else if (strlen($newPassword) < 4)
				{
					DekiMessage::error(wfMsg('Page.UserRegistration.error.password.length'));
					break;
				}

				$Result = $User->changePassword($newPassword, $oldPassword);
				if (!$Result->handleResponse(array(401, 403)))
				{
					DekiMessage::error($Result->getError());
					break;
				}
			}

			// check if any plugins want to update the user properties
			$username = $User->getUsername();
			$result = DekiPlugin::executeHook(
				Hooks::MAIN_CREATE_USER,
				array(
					&$username,
					&$newPassword,
					&$email
				)
			);
			if ($result)
                	{
                        	// user update is complete?
                        	DekiPlugin::executeHook(Hooks::MAIN_CREATE_USER_COMPLETE, array($User));
                	}

			DekiMessage::success(wfMsg('Page.UserPreferences.success'));
			
			// redirect to self
			$Title = Title::newFromText('UserPreferences', NS_SPECIAL);
			self::redirect($Title->getLocalUrl());

			return;
		} while (false);
	}
	
	protected function getSimpleForm($UserProperties)
	{		
		$User = DekiUser::getCurrent();
		
		$html = '';
		// royk: disable advanced form for user preferences
// 		$html =
// 		'<div class="mode">'.
// 			'<a href="'. $this->getLocalUrl('advanced') .'">'.
// 				wfMsg('Page.UserPreferences.advanced').
// 			'</a>'.
// 		'</div>';

		$html .= '<form method="post" class="userpreferences">';
		
		// Bugfix #MT-10219
		$html .= DekiForm::tokenInput();

		$html .= '<div class="section my-information">';
		// general information fields
		$html .= '<h3>' . wfMsg('Page.UserPreferences.form.legend.information') . '</h3>';

		$html .= '<div class="field">';
			$html .= DekiForm::singleInput('text', 'email', $User->getEmail(), array(), wfMsg('Page.UserPreferences.form.email'));
		$html .= '</div>';

		if (DekiLanguage::isSitePolyglot())
		{
			$html .= '<div class="field">';
				$html .= self::getLanguageOptions($User->getLanguage());
			$html .= '</div>';
		}
				
		$html .= '<div class="field" id="deki-timezone">';
			$html .= self::getTimeZoneOptions($User->getTimezone());
		$html .= '</div>';

		global $wgEnableSearchHighlight;
		if ($wgEnableSearchHighlight) 
		{
			$html .= '<div class="field">';
				$html .= '<span class="field">'.DekiForm::singleInput('checkbox', 'highlight', 'true', array('checked' => $User->Properties->getHighlightOption()), wfMsg('Page.UserPreferences.form.searchhighlight')).'</span>';
			$html .= '</div>';
		}
		// end my information
		$html .= '</div>';

		$html .= '<div class="section my-password">';
		// password fields
		$html .= '<h3>' . wfMsg('Page.UserPreferences.form.legend.password') . '</h3>';
		$html .= '<div class="field">';
			$html .= DekiForm::singleInput('password', 'old_password', null, array(), wfMsg('Page.UserPreferences.form.password.old'));
		$html .= '</div>';

		$html .= '<div class="field">';
			$html .= DekiForm::singleInput('password', 'new_password', null, array(), wfMsg('Page.UserPreferences.form.password.new'));
		$html .= '</div>';

		$html .= '<div class="field">';
			$html .= DekiForm::singleInput('password', 'new_password_verify', null, array(), wfMsg('Page.UserPreferences.form.password.verify'));
		$html .= '</div>';
		// end my password
		$html .= '</div>';

		// submit button
		$html .= '<div class="submit">';
			$html .= DekiForm::singleInput('button', 'action', 'save', array(), wfMsg('Page.UserPreferences.form.submit'));
		$html .= '</div>';
		$html .= '</form>';

		return $html;
	}
	
	protected static function getLanguageOptions($userLanguage = null)
	{
		global $wgLanguagesAllowed;
		
		$options = array();
		if (!empty($wgLanguagesAllowed))
		{
			//list($language, $variant) = explode('-', $userLanguage, 2);
			$options = wfAllowedLanguages();
		}
		$options = array('' => wfMsg('Page.UserPreferences.form.language.default')) + $options;
		
		return DekiForm::multipleInput('select', 'language', $options, $userLanguage, array(), wfMsg('Page.UserPreferences.form.language'));		
	}

	protected static function getTimeZoneOptions($userOffset)
	{
		$currentUserOffset = validateTimeZone($userOffset);
		if (is_null($currentUserOffset))
		{
			$currentUserOffset = '+00:00';
		}
		
		$siteTimezone = DekiSite::getTimezoneOffset();
		$timezoneOptions = DekiSite::getTimezoneOptions();
		
		// parse offset information
		preg_match("/([-+])([0-9]+):([0-9]+)/", $siteTimezone, $match);
		$offset = ($match[2] * 3600 + $match[3] * 60) * (strcmp($match[1], '-') == 0 ? -1 : 1);
		$siteDisplay = gmdate('h:i A', gmmktime() + $offset);

		$tztypes = array(
			'site' => wfMsg('Page.UserPreferences.form.tzoptions.site', $siteDisplay), 
			'override' => wfMsg('Page.UserPreferences.form.tzoptions.override')
		);
				
		return 
			'<div class="legend">'.
				wfMsg('Page.UserPreferences.form.timezone').
			'</div>'.
			DekiForm::multipleInput('radio', 'tztype', $tztypes, !is_null($userOffset) ? 'override' : 'site').
			'<div class="tzselect">'.
				DekiForm::multipleInput('select', 'timezone', $timezoneOptions, $currentUserOffset).
			'</div>';
	}
}
