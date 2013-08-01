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

define('DEKI_ADMIN', true);
require_once('index.php');


class EmailSettings extends DekiControlPanel
{
	// needed to determine the template folder
	protected $name = 'email_settings';
	
	public function index()
	{
		$this->executeAction('form');
	}

	// main listing view
	public function form()
	{
		if ($this->Request->isPost() && $this->POST_form()) 
		{
			$this->Request->redirect($this->getUrl());
			return;
		}

		$this->View->set('form.action', $this->getUrl());
		$this->View->set('form.from', wfGetConfig('admin/email'));
		$this->View->set('form.smtp.server', wfGetConfig('mail/smtp-servers'));
		$this->View->set('form.smtp.port', wfGetConfig('mail/smtp-port'));
		$this->View->set('form.smtp.username', wfGetConfig('mail/smtp-username'));
		$this->View->set('form.smtp.password', wfGetConfig('mail/smtp-password'));
		$this->View->set('form.smtp.secure', wfGetConfig('mail/smtp-secure'));
			
		$this->View->output();
	}

	protected function POST_form()
	{
		$action = $this->Request->getVal('submit');

		if ($action == 'email') 
		{
			global $wgSitename;
			$User = DekiUser::getCurrent();
			
			// send ui email
			$uiSubject = $this->View->msg('EmailSettings.test.subject', $wgSitename);
			$uiBody = $this->View->msg('EmailSettings.test.body', $wgSitename);
			$uiBodyHtml = wfMsgFromTemplate('email.html', $subject, $uiBody);
			$emailUi = DekiMailer::sendEmail($User->getEmail(), $uiSubject, $uiBody, $uiBodyHtml);
			
			// send api email
			$apiSubject = $this->View->msg('EmailSettings.test.api.subject', $wgSitename);
			$apiBody = $uiBody;
			$apiBodyHtml = wfMsgFromTemplate('email.html', $apiSubject, $apiBody);
			$Result = DekiMailer::sendTestEmail($User->getEmail(), $apiSubject, $apiBody, $apiBodyHtml);
			$emailApi = $Result->isSuccess();

			// failed to send a ui email
			if (!$emailUi)
			{
				DekiMessage::error($this->View->msg('EmailSettings.error.test', $User->getEmail()));
			}
			
			// failed to send an api email
			if (!$emailApi)
			{
				DekiMessage::error($this->View->msg('EmailSettings.error.test.api', $User->getEmail(), $Result->getError()));	
			}
			
			// both emails were sent successfully
			if ($emailUi && $emailApi)
			{		
				DekiMessage::success($this->View->msg('EmailSettings.success.test', $User->getEmail()));
			}
		}
		else if ($action == 'save') 
		{
			// validate the from email address
			$email = $this->Request->getVal('from');
			if (empty($email))
			{
				// unset the "from" email configuration
				$email = null;
			}
			else
			{
				// do not attempt to validate advanced addresses. i.e. "Foo" <email@address.com>
				if (strpos($email, '<') === false && !wfValidateEmail($email))
				{
					DekiMessage::error($this->View->msg('EmailSettings.error.email'));
					return false;
				}
			}

			wfSetConfig('admin/email', $email);
			wfSetConfig('mail/smtp-servers', $this->Request->getVal('smtp-server'));
			wfSetConfig('mail/smtp-port', $this->Request->getVal('smtp-port'));
			wfSetConfig('mail/smtp-username', $this->Request->getVal('smtp-username'));
			wfSetConfig('mail/smtp-password', $this->Request->getVal('smtp-password'));

			$secure = $this->Request->getVal('smtp-secure');
			wfSetConfig('mail/smtp-secure', $secure == 'none' ? null: $secure);
			wfSaveConfig();

			DekiMessage::success($this->View->msg('EmailSettings.success.settings')); 
		}

		return true;
	}
}

new EmailSettings();
