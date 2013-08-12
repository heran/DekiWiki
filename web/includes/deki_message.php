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
	//const SESSION_FLASH_KEY = 'DekiFlashMessage';
	//const SESSION_API_KEY = 'DekiApiMessage';

	const ALL_MSG = 'all';
	const INFO_MSG = 'info';
	const SUCCESS_MSG = 'success';
	const ERROR_MSG = 'error';
	const WARN_MSG = 'warn';

	/**
	 * @param string $type - Determines the message type, e.g. error, info, success
	 * @param string $message - Sets the contents of the message
	 * @param string $from - Sets the location to display the messages in
	 */
	static function flash($type, $message, $from = null)
	{
		$from = is_null($from) ? 'general' : $from;
		wfMessagePush($from, $message, $type);
	}

	static function info($message, $from = null)		{ self::flash(self::INFO_MSG, $message, $from); }
	static function success($message, $from = null)	{ self::flash(self::SUCCESS_MSG, $message, $from); }
	static function error($message, $from = null)	{ self::flash(self::ERROR_MSG, $message, $from); }
	static function warn($message, $from = null)		{ self::flash(self::WARN_MSG, $message, $from); }
	
	/*
	 * Only 1 api response will be stored in the session since a redirect should occur
	 */
	static function apiResponse($title, $message, $response = null)
	{
		MTMessage::Show($title, $message, 'ui-errormsg', json_encode($response));
	}
}
