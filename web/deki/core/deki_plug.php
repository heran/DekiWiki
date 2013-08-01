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
 * MindTouch DekiPlug - Facilitates interacting with MindTouch REST API
 * @note returns results wrapped in DekiResult objects
 */
class DekiPlug extends DreamPlug
{
	const HEADER_AUTHTOKEN = 'X-Authtoken';
	const HEADER_DATA_STATS = 'X-Data-Stats';
	
	// @var DekiPlug - stores the Plug for the current request
	protected static $instance = null;

	// used for calculating request profiling information
	// @TODO guerrics: move these meta values into the response array
	private $requestTimeStart = null;
	private $requestTimeEnd = null;
	private $requestVerb = null;
	
	/**
	 * Duplicated method from DreamPlug in order to create proper class
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
	 * Retrieve the DekiPlug object that was set for this request
	 * 
	 * @return DekiPlug
	 */
	public static function &GetInstance()
	{
		if (!is_object(self::$instance))
		{
			throw new Exception(__CLASS__ . ' has not been initialized');
		}
		return self::$instance;
	}
	
	/**
	 * Set the plug object for this request
	 * 
	 * @param DekiPlug $Plug
	 * @return
	 */
	public static function SetInstance($Plug)
	{
		self::$instance = $Plug;
	}
	
	/**
	 * Method retrieves the apikey
	 * @note used in the control panel with an auxillary config
	 * @TODO guerrics: use a config class to retrieve the key for site/settings
	 */
	public static function GetApiKey()
	{
		global $wgDekiApiKey;
		
		// @TODO guerrics: remove reference to Config::$API_KEY
		return isset($wgDekiApiKey) && !empty($wgDekiApiKey) ? $wgDekiApiKey : Config::$API_KEY;
	}

	/**
	 * The api requires double urlencoded titles. This method will do it automatically for you.
	 * @see #AtRaw() for creating unencoded path components
	 * 
	 * @param string[] $path - path components to add to the request 
	 * @return DekiPlug
	 */
	public function At(/* $path[] */) 
	{
		$result = new $this->classname($this, false);

		foreach (func_get_args() as $path) 
		{
			$result->path .= '/';

			// auto-double encode, check for '=' sign
			if (strncmp($path, '=', 1) == 0)
			{
				$result->path .= '=' . self::UrlEncode(substr($path, 1), true);
			}
			else
			{
				$result->path .= self::UrlEncode($path, true);
			}
		}
		return $result;
	}
	
	/**
	 * Appends a single path parameter to the plug, unencoded.
	 * @note Do not use this method unless you have to(you probably don't).
	 * A real need occurs when initially creating the plug baseuri and an
	 * unencoded "@api" is required.
	 * @see #At() for creating urlencoded paths
	 * 
	 * @return DekiPlug
	 */
	public function AtRaw($path)
	{
		$result = new $this->classname($this, false);

		$result->path .= '/' . $path;

		return $result;	
	}
	
	/**
	 * Add the apikey to the request
	 * 
	 * @return DekiPlug
	 */
	public function WithApiKey()
	{
		return $this->With('apikey', self::getApiKey());
	}
	
	/**
	 * Compatibility function
	 * @deprecated
	 * 
	 * @return DekiPlug
	 */
	public function SetHeader($name, $value)
	{
		return $this->WithHeader($name, $value);
	}
	
	/**
	 * 
	 * @param ch $curl
	 * @param array $headers
	 * @return
	 */
	// @TODO: clean up
    protected function invokeApplyCredentials($curl, &$headers)
	{
		// apply manually given credentials
		if (isset($this->user) || isset($this->password))
		{
			$headers[self::HEADER_AUTHORIZATION] = 'Basic ' . base64_encode($this->user . ':' . $this->password);
		}
		else if (function_exists("getallheaders")) 
		{
			$requestHeaders = getallheaders();
			$authToken = null;

			// Deki specific authorization
			// check if there is an authentication token
			$authToken = DekiToken::get();
			if (!is_null($authToken)) 
			{
				// got the token
			}
			else if (isset($requestHeaders[self::HEADER_AUTHTOKEN])) 
			{
				$authToken = $requestHeaders[self::HEADER_AUTHTOKEN];
			}
			
			if (!is_null($authToken)) 
			{
				$authToken = trim($authToken, '"');
				$headers[self::HEADER_AUTHTOKEN] = $authToken;
			} 
			else if (isset($requestHeaders[self::HEADER_AUTHORIZATION])) 
			{
				$headers[self::HEADER_AUTHORIZATION] = $requestHeaders[self::HEADER_AUTHORIZATION];
			}
		}
	}

	/**
	 * @param ch $curl
	 * @param string $verb
	 * @param string $content
	 * @param string $contentType
	 * @param bool $contentFromFile
	 * @param array $request
	 * @return
	 */
	protected function invokeRequest(&$curl, &$verb, &$content, &$contentType, &$contentFromFile, &$request)
	{
		$this->requestTimeStart = wfTime();
		$this->requestTimeEnd = null;
		$this->requestVerb = $verb;
	}

	/**
	 * @param ch $curl
	 * @param string $verb
	 * @param string $content
	 * @param string $contentType
	 * @param bool $contentFromFile
	 * @param string $httpMessage
	 * @return
	 */
	protected function invokeResponse(&$curl, &$verb, &$content, &$contentType, &$contentFromFile, &$httpMessage)
	{
		$this->requestTimeEnd = wfTime();
	}

	/**
	 * Format the invoke return
	 * 
	 * @param array $request
	 * @param array $response
	 * @return DekiResult
	 */
	protected function invokeComplete(&$request, &$response)
	{
		global $wgPlugProfile;
		if (!is_array($wgPlugProfile))
		{
			$wgPlugProfile = array();
		}
		$wgPlugProfile[] = array(
			'verb' => $this->requestVerb,
			'url' => $this->getUri(),
			'diff' => ($this->requestTimeEnd - $this->requestTimeStart),
			'stats' => isset($response['headers'][self::HEADER_DATA_STATS]) ?  $response['headers'][self::HEADER_DATA_STATS] : ''
		);
		$result = parent::invokeComplete($request, $response);
		return new DekiResult($result);
	}
}
