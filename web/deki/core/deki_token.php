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
 * Handles setting & loading the authtoken
 */
class DekiToken
{
	const KEY_NAME = 'authtoken';
	
	/**
	 * @return string - cookie/session key for the authtoken
	 */
	public static function getKey() { return self::KEY_NAME; }
	
	/**
	 * @return string - null if no authtoken is set
	 */
	public static function get()
	{
		// check the cookie
		$authToken = isset($_COOKIE[self::KEY_NAME]) ? $_COOKIE[self::KEY_NAME] : null;
		
		// no token or invalid specified, remove it
		if (!is_null($authToken) && empty($authToken))
		{
			self::destroy();
			return null;
		}

		// authtoken can be set by the api, api sets it in quotes
		$authToken = str_replace('"', '', $authToken);
		
		return $authToken;
	}

	/**
	 * @param string $authToken
	 * @param optional string $setCookie - if specified, this will sent as the raw cookie header
	 */
	public static function set($authToken, $setCookie = null)
	{
		if (empty($authToken))
		{
			return false;
		}

		// normalize the token to be surrounded by quotes
		$authToken = '"' . str_replace('"', '', $authToken) . '"'; 
		
		global $wgCookieExpiration, $wgCookiePath, $wgCookieDomain, $wgCookieSecure, $wgCookieHttpOnly;
		// only set a cookie if there is an expiration time set
		$expiry = $wgCookieExpiration;
		if ($expiry <= 0)
		{
			// normalize the expiry to zero so we set a session cookie
			$expiry = 0;
		}
		
		set_cookie(self::KEY_NAME, $authToken, $expiry, $wgCookiePath, $wgCookieDomain, $wgCookieSecure, $wgCookieHttpOnly);
		// set in the global so it is available on this request
		$_COOKIE[self::KEY_NAME] = $authToken;

		return true;
	}

	public static function destroy()
	{
		global $wgCookiePath, $wgCookieDomain, $wgCookieSecure, $wgCookieHttpOnly;

		// remove from the global for this request
		unset($_COOKIE[self::KEY_NAME]);
		set_cookie(self::KEY_NAME, '', -9600, $wgCookiePath, $wgCookieDomain, $wgCookieSecure, $wgCookieHttpOnly);
	}

	/**
	 * Verifies that the authtoken's userId matches the current userId
	 */
	public static function validate($userId)
	{
		$authToken = self::get();

		@list($authUserId) = explode('_', $authToken, 2);

		return (strcmp($userId, $authUserId) == 0);
	}
}
