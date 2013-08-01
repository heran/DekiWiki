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
 * MindTouch DreamPlug - Facilitates interacting with dream services
 */
class DreamPlug extends HttpPlug
{
	// dream.out.format
	const DREAM_FORMAT_PHP = 'php';
	const DREAM_FORMAT_JSON = 'json';
	const DREAM_FORMAT_XML = 'xml';
	
	/**
	 * Determines which headers should be forwarded with every request
	 * @note maps HTTP header to PHP defines
	 * @var array
	 */
	public static $dreamDefaultHeaders = array(
		'X-Forwarded-For' => 'HTTP_X_FORWARDED_FOR',
		'X-Forwarded-Host' => 'HTTP_HOST',
		'Referer' => 'HTTP_REFERER',
		'User-Agent' => 'HTTP_USER_AGENT'
	);

	/**
	 * Creates a new plug object with Dream defaults
	 *
	 * @param string $uri
	 * @param string $format
	 * @param string $hostname
	 * @param array $defaultHeaders
	 */
	public static function NewPlug($uri, $format = self::DREAM_FORMAT_PHP, $hostname = null, $defaultHeaders = null)
	{
		$classname = __CLASS__;
		$Plug = new $classname($uri);
		self::initializeDreamPlug($Plug, $format, $hostname, $defaultHeaders);
		return $Plug;
	}

	/**
	 * Dream specific urlencode method
	 * @see Bugfix#7500: Unable to save a new page with a dot (.) at the end of the title on IIS
	 * 
	 * @param string $string - string to urlencode
	 * @param bool $doubleEncode - if true, the string will be urlencoded twice
	 * @return string
	 */
	public static function UrlEncode($string, $doubleEncode = false)
	{
		// encode trailing dots (. => %2E)
		for ($i = strlen($string) - 1, $dots = 0; $i >= 0; $dots++, $i--)
		{
			if (substr($string, $i, 1) != '.')
			{
				break;
			}
		}
		$string = urlencode(substr($string, 0, $i + 1)) . str_repeat('%2E', $dots);
		
		// we don't need to apply our custom encodings on the second pass
		if ($doubleEncode)
		{
			$string = urlencode($string);
		}
		
		return $string;
	}
	
	 /**
	 * Method sets default headers and forwarded headers
	 * @return
	 */
	public static function SetDefaultHeaders(&$headers, $defaults = array())
	{
		foreach ($defaults as $header => $key)
		{
			if (isset($_SERVER[$key]))
			{
				self::setMultiValueArray($headers, $header, $_SERVER[$key]);
			}
		}

		// append REMOTE_ADDR to X-Forwarded-For if it exists
		if (isset($_SERVER['REMOTE_ADDR']))
		{
			self::setMultiValueArray(
				$headers,
				'X-Forwarded-For',
				isset($headers['X-Forwarded-For'])	? $headers['X-Forwarded-For'].', '.$_SERVER['REMOTE_ADDR'] : $_SERVER['REMOTE_ADDR']
			);
		}
	}
	
	/**
	 * Set the DreamPlug defaults
	 * 
	 * @param DreamPlug $Plug
	 * @param array $defaultHeaders
	 */
	// @TODO: clean up
	protected static function initializeDreamPlug(&$Plug, $format = null, $hostname = null, $defaultHeaders = null)
	{
		// include default & white-listed headers
		self::SetDefaultHeaders($Plug->headers, !is_null($defaultHeaders) ? $defaultHeaders : self::$dreamDefaultHeaders);
		
		// set the default dream query params
		if ($Plug->query)
		{
			$Plug->query .= '&';
		}
		else
		{
			$Plug->query = '';
		}
		
		if (empty($hostname) && isset($_SERVER['HTTP_HOST']))
		{
			$hostname = $_SERVER['HTTP_HOST'];
		}
		
		if ($format)
		{
			$Plug->query .= 'dream.out.format=' . rawurlencode($format);
		}
		
		// if a hostname was previously set, reuse it, otherwise take the new one
		$Plug->query .= '&dream.in.host=' . rawurlencode(!empty($hostname) ? $hostname : $Plug->hostname);
		
		// @note hack hack, pass in scheme until dream.in.uri is available
		// parse the scheme from the frontend request
		if (isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] == "on")
		{
			$scheme = 'https';
		}
		else
		{
			$scheme = 'http';
		}
		$Plug->query .= '&dream.in.scheme=' . $scheme;
		
		if (isset($_SERVER['REMOTE_ADDR']))
		{
			$Plug->query .= '&dream.in.origin=' . rawurlencode($_SERVER['REMOTE_ADDR']);
		}
	}

	/**
	 * Performs a PUT request
	 * @note override HttpPlug#Put in favor of dream verb rewriting
	 * 
	 * @param array $input - if array, gets encoded as xml
	 * @return array - request response
	 */	
	public function Put($input = null)
	{
		$Plug = $this->With('dream.in.verb', 'PUT');
		return $Plug->invokeXml(self::VERB_POST, $input);
	}

	/**
	 * Format the invoke return
	 * 
	 * @param array $request
	 * @param array $response
	 * @return array
	 */
	protected function invokeComplete(&$request, &$response)
	{
		$contentType = isset($response['type']) ? $response['type'] : '';
		
		// check if we need to deserialize
		if (strpos($contentType, '/php'))
		{
			$response['body'] = unserialize($response['body']);
		}
		return parent::invokeComplete($request, $response);
	}
}
