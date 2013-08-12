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
 * Class for queuing and retrieving session messages
 */
class DekiMessage
{
	const SESSION_FLASH_KEY = 'DekiFlashMessage';
	const SESSION_API_KEY = 'DekiApiMessage';

	const ALL_MSG = 'all';
	const INFO_MSG = 'info';
	const SUCCESS_MSG = 'success';
	const ERROR_MSG = 'error';
	const WARN_MSG = 'warn';

	/**
	 * @param string $type - Determines the message type, e.g. error, info, success
	 * @param string $message - Sets the contents of the message
	 * @param bool $output - If true, outputs the message directly instead of to session
	 */
	static function flash($type, $message, $output = false)
	{
		if ($output)
		{
			echo '<div class="dekiFlash">';
				echo '<ul class="'. $type .' first">';
					echo '<li>'. $message .'</li>';
				echo '</ul>';
			echo '</div>';
		}
		else
		{
			$_SESSION[self::SESSION_FLASH_KEY][$type][] = $message;
		}
	}
	static function info($message, $output = false)		{ self::flash(self::INFO_MSG, $message, $output); }
	static function success($message, $output = false)	{ self::flash(self::SUCCESS_MSG, $message); }
	static function error($message, $output = false)	{ self::flash(self::ERROR_MSG, $message, $output); }
	// @deprecated
	static function verify($message, $output = false)	{ self::warn($message, $output); }
	static function warn($message, $output = false)		{ self::flash(self::WARN_MSG, $message, $output); }
	
	/*
	 * Only 1 api response will be stored in the session since a redirect should occur
	 */
	static function apiResponse($title, $message, $response = null)
	{
		$_SESSION[self::SESSION_API_KEY] = array(
			'title' => $title,
			'message' => $message,
			'response' => $response
		);
	}
	
	// sets a front end flash message
	// TODO: emulate wfMessagePush after setup.php is removed
	static function ui($message, $type = 'success') { wfMessagePush('general', $message, $type); }
	
	static function hasFlash() { return isset($_SESSION[self::SESSION_FLASH_KEY]) && !empty($_SESSION[self::SESSION_FLASH_KEY]); }
	static function hasApiResponse() { return isset($_SESSION[self::SESSION_API_KEY]) && !empty($_SESSION[self::SESSION_API_KEY]); }

	static function fetchFlash()
	{
		$html = '';

		if (self::hasFlash())
		{
			$first = true;
			foreach ($_SESSION[self::SESSION_FLASH_KEY] as $type => &$messages)
			{
				$class = $type;
				if ($first)
				{
					$class .= ' first';
					$first = false;
				}
				if ($type == self::ERROR_MSG && self::hasApiResponse())
				{
					$class .= ' withresponse';
				}

				$html .= '<ul class="'. $class .'">';
				foreach ($messages as $message)
				{
					$html .= '<li>'. $message .'</li>';
				}
				unset($_SESSION[self::SESSION_FLASH_KEY][$type]);
				$html .= '</ul>';
			}
			unset($messages);
		}

		return $html;
	}

	static function fetchApiResponse($asArray = false)
	{
		$html = '';
		if (self::hasApiResponse())
		{
			$response = $_SESSION[self::SESSION_API_KEY];
			if ($asArray)
			{
				$html = $response;
			}
			else
			{
				$html .= '<h3 class="title">' . htmlspecialchars($response['title']) . '</h3>';
				$html .= '<p class="message">' . htmlspecialchars($response['message']) . '</p>';

				if (!empty($response['response']))
				{
					$html .= '<textarea>' . htmlspecialchars(print_r($response, true)) . '</textarea>';
				}
			}

			unset($_SESSION[self::SESSION_API_KEY]);
		}

		return $html;
	}

}
