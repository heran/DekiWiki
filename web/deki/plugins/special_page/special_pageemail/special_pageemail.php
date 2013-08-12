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

// include the form helper
if (defined('MINDTOUCH_DEKI'))
{
	DekiPlugin::registerHook(Hooks::SPECIAL_PAGE_EMAIL, 'wfSpecialPageEmail');
}

function wfSpecialPageEmail($pageName, &$pageTitle, &$html, &$subhtml)
{
	$SpecialPageEmail = new SpecialPageEmail($pageName, basename(__FILE__, '.php'));
	
	// set the page title
	$pageTitle = $SpecialPageEmail->getPageTitle();
	$SpecialPageEmail->output($html, $subhtml);
}

class SpecialPageEmail extends SpecialPagePlugin
{
	protected $pageName = 'PageEmail';
	protected $allowAnonymous = false;
	
	public function output(&$html, &$subhtml)
	{
		$html = '';
		$this->includeSpecialCss('special_pageemail.css');
		$this->Request = DekiRequest::getInstance();
		$this->User = DekiUser::getCurrent();
		$this->Title = Title::newFromId($this->Request->getVal('pageid', 0));
		
		if (is_null($this->Title)) 
		{
			self::redirectHome();
			return;
		}
		
		// bugfix #8480: Email link feature should be blocked for banned users
		if ($this->User->isDisabled())
		{
			self::redirectHome();
			return;
		}
		
		if ($this->Request->isPost())
		{
			if ($this->emailPage())
			{
				self::redirect($this->Title->getLocalUrl());
			}
		}
		
		$html .= $this->getEmailForm();
		$subhtml = wfMsg('Page.PageEmail.return-to', $this->Title->getLocalUrl(), $this->Title->getPrefixedText()); 
		
		return $html;
	}
	
	private function emailPage()
	{
		global $wgSitename, $wgDekiPlug, $wgDekiApiKey;

		$titleId = $this->Request->getVal( 'pageid' );
		$recipient = $this->Request->getVal( 'email' );
		$fromname = htmlspecialchars($this->Request->getVal( 'name' ));
		$note = htmlspecialchars($this->Request->getVal( 'note' ));

		$Title = Title::newFromId($titleId);
		if (is_null($Title) || empty($fromname))
		{
			return false;
		}

		if (isset($note) && $note != '')
		{
			$note = wfMsg('Page.PageEmail.user-says', $fromname)."\n".$note."\n";
		}

		$recipient = strpos($recipient, ',') !== false ? explode(',', $recipient): array($recipient);
		$recipient = array_unique($recipient);

		$mailed = array();

		$result = true;

		foreach ($recipient as $key => $email)
		{
			$email = trim($email);
			if (empty($email))
			{
				unset($recipient[$key]);
				continue;
			}

			//valiate the email address
			if (!wfValidateEmail($email))
			{

				//mt:royk - hidden feature - try a username (maybe this'll be useful)
				//this will collide with users who use email-like usernames
				$r = $wgDekiPlug->At('users', '='.urlencode(urlencode($email)))->With('apikey', $wgDekiApiKey)->Get();
				$apiEmail = wfArrayVal($r, 'body/user/email'); //need to keep $email
				if ($r['status'] != 200 || is_null($apiEmail))
				{
					unset($recipient[$key]);
					continue;
				}
				$recipient[$key] = wfMsg('Page.PageEmail.user', $email);
				$email = $apiEmail; //set $email
			}

			//unique values might exist if you use uesrnames
			if (in_array($email, $mailed))
			{
				continue;
			}

			//send the email out
			$result = DekiMailer::sendEmail($email,
				wfMsg('Page.PageEmail.subject', $fromname, $wgSitename),
				wfMsg('Page.PageEmail.body', $fromname, $wgSitename, $Title->getFullUrl(), $note));
			$mailed[] = $email;
		}

		//no recipient error message
		if (!is_array($recipient) || count($recipient) == 0 || empty($recipient))
		{
			wfMessagePush('general', wfMsg('Page.PageEmail.norecipients-error'), 'error');
			return false;
		}

		//successfully sent email
		wfMessagePush('general', wfMsg('Page.PageEmail.email-success', implode(', ', $recipient)), 'success');
		return $result;
	}
	
	protected function getEmailForm()
	{
		$Title = Title::newFromText(Hooks::SPECIAL_PAGE_EMAIL);
		$html = '<form method="post" action="'. $Title->getLocalUrl('pageid='.$this->Request->getVal('pageid')) .'" class="deki-special-pageemail">';
		
		$html .= '<div class="deki-email-message">'.wfMsg('Page.PageEmail.header-emailing-url', $this->Title->getFullUrl()).'</div>';
		
		$html .= '<h2>'.wfMsg('Page.PageEmail.header-to').'</h2>';
		
		$html .= '<div class="field">';
			$html .= DekiForm::singleInput('text', 'email', null, array('autocomplete' => 'on'), wfMsg('Page.PageEmail.recipient-email')) 
				. wfMsg('Page.PageEmail.required');
		$html .= '</div>';
		
		$html .= '<h2>'.wfMsg('Page.PageEmail.header-from').'</h2>';
		
		$html .= '<div class="field">';
			$html .= DekiForm::singleInput('text', 'name', $this->User->getName(), array(), wfMsg('Page.PageEmail.your-name'));
		$html .= '</div>';
		
		$html .= '<div class="field">';
			$html .= wfMsg('Page.PageEmail.your-note').'<br/>';
			$html .= DekiForm::singleInput('textarea', 'note', null, array());
		$html .= '</div>';
	
		$html .= DekiForm::singleInput('hidden', 'pageid', $this->Request->getVal('pageid')); 
		
		// submit button
		$html .= DekiForm::singleInput('button', 'action', 'login', array(), wfMsg('Page.PageEmail.form-submit'));
		$html .= '</form>';

		return $html;
	}
}
