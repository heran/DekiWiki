<?php
/*
 * MindTouch Core - open source enterprise collaborative networking
 *  derived from MediaWiki (www.mediawiki.org)
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

require_once("phpmailer/class.phpmailer.php");
require_once("phpmailer/class.smtp.php");

/**
 * This function will perform a direct (authenticated) login to
 * a SMTP Server to use for mail relaying if 'wgSMTP' specifies an
 * array of parameters.
 *
 * @param string $to recipient's email
 * @param string $from sender's email
 * @param string $subject email's subject
 * @param string $body email's text
 * @param bool $silent - if true, no yellow box exception messages will be output
 */
function userMailer($to, $from, $subject, $body, $body_html = '', $silent = false)
{
	// need to check if dekiplugins are being used in the current request
	$allowPlugins = class_exists('DekiPlugin');
	if ($allowPlugins)
	{
		$result = DekiPlugin::executeHook(Hooks::NOTIFY_EMAIL, array(&$to, &$from, &$subject, &$body, &$body_html));
		if ($result == DekiPlugin::HANDLED_HALT)
		{
			// if a plugin returns halt then don't attempt to send the email
			return true;
		}
	}
	
	// internal email handling
	global $wgSMTPServers, $wgSMTPUser, $wgSMTPPwd, $wgSMTPSecure, $wgSMTPPort, $IP;

	$mail = new PHPMailer();
	
	$mail->SetLanguage("en", "phpmailer/language/");
	$mail->CharSet = "UTF-8";
	
	if ($wgSMTPServers)
	{
		$mail->IsSMTP();                                      // set mailer to use SMTP
		$mail->Host = $wgSMTPServers;  // specify main and backup server
		if (!empty($wgSMTPUser))
		{
			$mail->SMTPAuth = true;     // turn on SMTP authentication
			$mail->Username = $wgSMTPUser;  // SMTP username
			$mail->Password = $wgSMTPPwd; // SMTP password
		}
		if (!empty($wgSMTPSecure))
			$mail->SMTPSecure = $wgSMTPSecure;
		if (!empty($wgSMTPPort))
			$mail->Port = $wgSMTPPort;
	}
	$mail->From = $from;
	$mail->FromName = $from;
	$mail->AddAddress($to);                  // name is optional
	
	$mail->Subject = $subject;
	
	if ($body_html != '')
	{
		$mail->IsHTML(true);                                  // set email format to HTML
		$mail->Body = $body_html;
		$mail->AltBody = $body;
	}
	else
	{
		$mail->Body = $body;
	}

	if (!$mail->Send())
	{
		if (!$silent)
		{
			$lMessaging = new MTMessage();
			$lMessaging->Show(wfMsg('System.Error.error'), $mail->ErrorInfo);
		}
		return false;
	}
	
	// mail was sent successfully
	if ($allowPlugins)
	{
		DekiPlugin::executeHook(Hooks::NOTIFY_EMAIL_COMPLETE, array($to, $from, $subject, &$body, &$body_html));
	}
	
	return true;
}

/**
 *
 */
function mailErrorHandler( $code, $string )
{
	global $wgErrorString;
	$wgErrorString = preg_replace( "/^mail\(\): /", "", $string );
}
