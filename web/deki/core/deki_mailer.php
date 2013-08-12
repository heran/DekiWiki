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

/**
 * DekiMailer
 * Handles interacting with the email service. Currently only used for sending
 * test emails via the API.
 * @TODO guerrics: consolidate user mailer functionality into this class
 * @see http://developer.mindtouch.com/Dream/Services/EmailService
 */
class DekiMailer
{
	const SERVICE_CONFIG_KEY = 'services/mailer';
	
	protected $to = null;
	protected $from = null;
	protected $subject = null;
	protected $text = null;
	protected $html = null;

	/**
	 * Sends an email via the API
	 * 
	 * @param string $to - email address of the recipient
	 * @param string $subject - subject of the email
	 * @param string $body - text only email body
	 * @param optional string $bodyHtml - html email body
	 * @return DekiResult
	 */
	public static function sendTestEmail($to, $subject, $body, $bodyHtml = null)
	{
		$Mail = new self();
		$Mail->setTo($to);
		$Mail->setSubject($subject);
		$Mail->setBody(
			$body,
			$bodyHtml
		);
		
		$Result = $Mail->send();
		return $Result;
	}
	
	/**
	 * Method wraps the UserMailer function until all emails are generated via the API.
	 * 
	 * @param string $to - email address of the recipient
	 * @param string $subject - subject of the email
	 * @param string $body - text only email body
	 * @param optional string $bodyHtml - html email body
	 * @return bool
	 */
	public static function sendEmail($to, $subject, $body, $bodyHtml = null, $silent = false)
	{
		global $IP, $wgPasswordSender;
		require_once($IP . '/includes/UserMailer.php');
		
		return userMailer(
			$to,
			$wgPasswordSender,
			$subject,
			$body,
			$bodyHtml,
			$silent
		);
	}
	
	/**
	 * @param string $from - origin email address
	 * @return
	 */
	protected function __construct($from = null)
	{
		global $wgPasswordSender;
		$this->from = is_null($from) ? $wgPasswordSender : $from;
	}

	/**
	 * Validates and sets the recipient's email address.
	 * 
	 * @param string $to - email address of the recipient
	 * @return bool
	 */
	protected function setTo($email)
	{
		if (!wfValidateEmail($email))
		{
			return false;
		}

		$this->to = $email;
		return true;
	}
	
	/**
	 * Set the email subject
	 * 
	 * @param string $subject
	 * @return
	 */
	protected function setSubject($subject)
	{
		$this->subject = $subject;
	}
	
	/**
	 * Set the body for the email
	 * 
	 * @param string $text - plain text body
	 * @param string $html - html body
	 * @return
	 */
	protected function setBody($text, $html = null)
	{
		// @TODO guerrics: validate the html body
		$this->text = $text;
		$this->html = $bodyHtml;
	}

	/**
	 * Send the email
	 * 
	 * @return DekiResult
	 */
	protected function send()
	{
		global $wgDekiApiKey;
		$Plug = self::getServicePlug()->At('message')->With('apikey', $wgDekiApiKey);

		$result = $Plug->Post($this->toArray());
		return new DekiResult($result);
	}

	/**
	 * Generates the array representation of the mail object to
	 * be sent to the API.
	 * 
	 * @return array
	 */
	protected function &toArray()
	{
		global $wgDekiSiteId;
		$bodies = array();
		
		if (!empty($this->text))
		{
			$bodies[] = array(
				'@html' => 'false',
				'#text' => &$this->text
			);
		}

		if (!empty($this->html))
		{
			$bodies[] = array(
				'@html' => 'true',
				'#text' => &$this->html
			);
		}

		$email = array(
			'@configuration' => $wgDekiSiteId,
			'subject' => &$this->subject,
			'from' => &$this->from,
			'to' => &$this->to,
			'body' => $bodies
		);
		
		$email = array(
			'email' => $email
		);
		
		return $email;
	}
	
	/**
	 * Creates a Plug object at the service endpoint
	 * 
	 * @return DreamPlug
	 */
	protected static function getServicePlug()
	{
		global $wgHostname;
		$mailerUri = wfGetConfig(self::SERVICE_CONFIG_KEY);

		return new DreamPlug($mailerUri, 'php', $wgHostname);
	}
}
