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

/**
 *
 * @package MediaWiki
 * @subpackage SkinMessaging
 */

/**
 * This is not a valid entry point, perform no further processing unless MEDIAWIKI is defined
 */
if( defined('MINDTOUCH_DEKI') ) {
	
	class MTMessage {
		
		/***
		 * Error handling from a Dekihost request
		 * Pass in the result after you call Get(), Put(), Post(), or Delete() methods from Plug
		 * If a status code exists in $hideStatus, visual message will be suppressed
		 */
		function HandleAPIResponse($result, $hideStatus = array(401)) {
			
			//a plug response was not returned
			if (!is_array($result)) {
				return;
			}
			
			//Service not Available usually means Dekihost has crashed and should be restarted
			if ($result['status'] == 503) {
				include('skins/down.php');
				exit();	
			}
			
			//200-level is good
			if ($result['status'] >= 200 && $result['status'] < 300) {
				return true;
			}
			
			if (empty($result['status'])) {
				$uri = parse_url($result['uri']);
				MTMessage::Show(wfMsg('System.Error.couldnt-connect-api-title'), wfMsg('System.Error.couldnt-connect-api-body', $uri['scheme'], $uri['host']) . '<br/>' . $result['uri'], 'ui-errormsg');
				return false;
			}
						
			//if the API response contains a title, pass this through, otherwise use a default
			if (is_null(($title = wfArrayVal($result, 'body/error/title')))) {
				if ($result['status'] == 401) {
					$title = wfMsg('System.Error.login-unauthorized');
				}
				else {
					$title = wfMsg('System.Error.error-number', $result['status']);
				}
			}
			else {
				$title .= ' ('. $result['status'] .')';	
			}
			
			//if the API response contains a message of some sort, pass this through, otherwise use a default
			if (is_null(($message = wfArrayVal($result, 'body/error/message')))) {
				$message = '';
			}
			
			$response = is_array($result['body']) ? print_r($result['body'], true) : $result['body'];
			$response = wfMsg('Dialog.Message.request-uri')."\n".wfScrubSensitive($result['request']['uri'])."\n\n"
				.wfMsg('Dialog.Message.server-response')."\n".$response;
			$response = json_encode($response);
			
			if (!in_array($result['status'], $hideStatus)) {
				MTMessage::Show($title, $message, 'ui-errormsg', $response);
			}
			return false;
		}
		
		/***
		 * Deprecated for HandleAPIResponse()
		 */
		function HandleFromDream($result, $statusCodesToHide = array(401)) {
			return MTMessage::HandleAPIResponse($result, $statusCodesToHide);
		}
		
		/***
		 * Adds a message into the queue to display on next page load
		 * @param 	$header			string	Title of message
		 * @param	$description	string	Body of message
		 * @param	$type			string	The CSS style to use for the message: ui-errormsg and ui-successmsg are the two types
		 * @param	$detail			string	JSON-encoded result from Dream; this will be set automatically if you use HandleFromDream() 
		 * 									on your Dream calls
		 */
		function Show($header, $description, $type = 'ui-errormsg', $detail = '') {
			$_SESSION['MTMessage'][$type] = array(
				'header' => wfEncodeJSString($header), 
				'description' => wfEncodeJSString($description), 
				'detail' => wfEncodeJSString($detail));
		}
		
		function ShowJavascript($onload = true) {
			if (isset($_SESSION['MTMessage']) && !is_array($_SESSION['MTMessage']) || !isset($_SESSION['MTMessage']) || count($_SESSION['MTMessage']) == 0) {
				return;
			}
			$js = '';
			if (isset($_SESSION['MTMessage']['ui-errormsg'])) {
				$val = $_SESSION['MTMessage']['ui-errormsg'];
				unset($_SESSION['MTMessage']['ui-errormsg']);
				$js .= 'MTMessage.Show(\''.htmlspecialchars($val['header']).'\', \''.htmlspecialchars($val['description']).'\', \'ui-errormsg\', \''.$val['detail'].'\');';
			}
			if (isset($_SESSION['MTMessage']['ui-successmsg'])) {
				$val = $_SESSION['MTMessage']['ui-successmsg'];
				unset($_SESSION['MTMessage']['ui-successmsg']);
				$js .= 'MTMessage.Show(\''.htmlspecialchars($val['header']).'\', \''.htmlspecialchars($val['description']).'\', \'ui-successmsg\', \''.$val['detail'].'\');';
			}
			if ($onload)
			{
				return 'YAHOO.util.Event.addListener(window, "load", function () { '.$js.'});';
			}
			else
			{
				return $js;
			}
		}
		function output() {
			return '
			
<div class="ui-msg-wrap" id="MTMessage" style="display: none;">
	<div class="ui-msg ui-errormsg" id="MTMessageStyle">
		<div class="ui-msg-opt">
			<ul>
				<li><a href="#" class="dismiss" onclick="return MTMessage.Hide();">'.wfMsg('Dialog.Message.dismiss-message').'</a></li>
				<li><a href="#" class="details" id="MTMessageDetailsLink" onclick="return MTMessage.ShowDetails(this);">'.wfMsg('Dialog.Message.view-details').'</a></li>
			</ul>
			<div class="ui-msg-autoclose">
				<span id="MTMessageUnpaused" style="display: inline;">'.wfMsg('Dialog.Message.message-will-close-itself').'</span>
				<span id="MTMessagePaused" style="display: none;">'.wfMsg('Dialog.Message.message-time-stopped').'</span>
			</div>
		</div>
		<div class="ui-msg-header" id="MTMessageHeader"></div>
		<div class="ui-msg-desc" id="MTMessageDesc"></div>
		<div class="ui-msg-desc" id="MTMessageDetails" style="display: none;">
			<p>'.wfMsg('Dialog.Message.viewing-details').'</p>
		</div>
	</div>
</div>';
		}
	}
}
?>
