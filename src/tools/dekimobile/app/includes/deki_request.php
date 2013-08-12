<?php
/*
 * MindTouch Deki - enterprise collaboration and integration platform
 *  derived from MediaWiki (www.mediawiki.org)
 * Copyright (C) 2006 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
 *  please review the licensing section.
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


/*
 * Wrapper for accessing GET,POST variables
 * and for handling URL creation
 */
class DekiRequest
{
	const PARAMS_GET_KEY = 'params';

	private static $instance = null;

	private $baseUri = '';
	private $webRoot = '';


	// Singleton
	static function &getInstance()
	{
		if (is_null(self::$instance))
		{
			self::$instance = new self;
		}

		return self::$instance;
	}

	/*
	 * @param $code the HTTP error code to throw
	 */
	static function error($code)
	{
		switch ($code)
		{
			default:
				$code = 404;
			case 404:
				header('Status: HTTP/1.1 404 Not Found');
				break;
			case 501:
				header('Status: HTTP/1.1 501 Not Implemented');
				break;
		}

		echo('Error: ' . $code);
	}

	private function __construct()
	{
		$this->startSession();
		// TODO: frontend is already cleaning up magic quotes, uncomment after removing frontend
		//$this->cleanupMagicQuotes();
		$this->setBaseUri();
		$this->parseQueryParams();
		$this->fixButtonValues();
	}

	// /action/param1/param2/param3?get1=&get2=
	// get variables are NOT params
	public function getParams()		{ return $this->params; }
	public function getAction()		{ return $this->action; }
	public function getBaseUri()	{ return $this->baseUri; }
	public function isXmlHttpRequest() { return ($this->getHeader('X_REQUESTED_WITH') == 'XMLHttpRequest'); }
	public function isPost() { return strtoupper($_SERVER['REQUEST_METHOD']) == 'POST'; }

	public function getFile($key) { return !isset( $_FILES[$key] ) ? null: $_FILES[$key]; }
	public function getBool($key, $default = false)	{ return $this->get($key, $default) ? true : false; }
	public function getInt($key, $default = null)	{ return intval($this->get($key, $default)); }
	public function getVal($key = null, $default = null) 
	{ 
		return is_null($key) ? $_REQUEST: $this->get($key, $default);
	}
	/**
	 * @return array
	 */
	public function getArray($key, $default = array())
	{
		$array = $this->get($key, null);
		return is_array($array) ? $array : $default;
	}
	private function get($key, $default)
	{ 
		// see bug #4427; PHP magically converts these characters to underscores
		$key = str_replace(array('.', ' ', '['), array('_', '_', '_'), $key);
		
		return isset($_REQUEST[$key]) ? $this->trim($_REQUEST[$key]) : $default; 
	}
	
    public function getHeader($header)
    {
        if (empty($header))
        {
            throw new Exception('An HTTP header name is required');
        }

        // Try to get it from the $_SERVER array first
        $temp = 'HTTP_' . strtoupper(str_replace('-', '_', $header));
        if (!empty($_SERVER[$temp]))
        {
            return $_SERVER[$temp];
        }

        // This seems to be the only way to get the Authorization header on
        // Apache
        if (function_exists('apache_request_headers'))
        {
            $headers = apache_request_headers();
            if (!empty($headers[$header]))
            {
                return $headers[$header];
            }
        }

        return false;
    }
	
	/**
	 * @param mixed $s - the variable to trim whitespace from, can be an array
	 */
	private function trim($s)
	{
		if (is_array($s))
		{
			foreach ($s as &$val)
			{
				$val = $this->trim($val);
			}
			unset($val);

			return $s;
		}
		else
		{
			return trim($s);
		}

	}

	// local url builder
	/**
	 * @param string $page - the actual script file name without the .php extension
	 * @param string $params - should include the action and any request params, e.g. /view/32
	 * @param array $get - key => val pairs of GET params to append
	 */
	public function getLocalUrl($page, $params = null, $get = array())
	{
		if (!is_null($params))
		{
			$get[self::PARAMS_GET_KEY] = $params;
		}
		
		$append = array();
		$queryString = '';
		
		if (!empty($get))
		{
			foreach ($get as $key => $val)
			{
				if (!is_string($key) || is_array($val) || is_object($val))
				{
					continue;
				}
				$append[] = urlencode($key) .'='. urlencode($val);
			}
			$queryString = '?' . implode('&amp;', $append);
		}

		return $this->baseUri . $this->webRoot . '/' . $page . '.php' . $queryString;
	}

	public function redirect($url)
	{
		header('Location: ' . $url);
	}


	public function setBaseUri($schema = null, $uri = null)
	{
		$this->schema = $schema;
		$this->baseUri = $uri;

		if (is_null($this->schema))
		{
			$this->schema = isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] == 'on' ? 'https' : 'http';
		}

		if (is_null($this->baseUri))
		{
			if (isset($_SERVER['SERVER_NAME']))
			{
				$serverName = $_SERVER['SERVER_NAME'];
			}
			else if (isset($_SERVER['HOSTNAME']))
			{
				$serverName = $_SERVER['HOSTNAME'];
			}
			else
			{
				$serverName = 'localhost';
			}

			$this->baseUri = $this->schema .'://'. $serverName;
			// determine if the request came on a different port
			if (isset($_SERVER['SERVER_PORT']) &&
				(($this->schema == 'http' && $_SERVER['SERVER_PORT'] != 80) ||
				($this->schema == 'https' && $_SERVER['SERVER_PORT'] != 443))
			   )
			{
				$this->baseUri .= ':' . $_SERVER['SERVER_PORT'];
			}
		}
	}

	public function setWebRoot($webRoot = '')
	{
		$this->webRoot = $webRoot;
	}

	private function startSession()
	{
		@session_start();
	}

	private function cleanupMagicQuotes()
	{
		if ( (get_magic_quotes_runtime() == 1) || (get_magic_quotes_gpc() == 1) )
		{
			// magic quotes are on :(
			// define what globals to auto strip here
			$this->stripEntitySlashes($_REQUEST);
			$this->stripEntitySlashes($_GET);
			$this->stripEntitySlashes($_POST);
		}
	}

	// helper method for stripping slashes
	private function stripEntitySlashes(&$entity)
	{
		if (is_string($entity))
		{
			$entity = stripslashes($entity);
		}
		else if (is_array($entity))
		{
			foreach ($entity as &$val)
			{
				$this->stripEntitySlashes($val);
			}
			unset($val);
		}
	}

	private function parseQueryParams()
	{
		$params = isset($_GET[self::PARAMS_GET_KEY]) ? $_GET[self::PARAMS_GET_KEY] : '';
		unset($_GET[self::PARAMS_GET_KEY]);

		// check for leadings params slash
		if (strncmp($params, '/', 1) == 0)
		{
			$params = substr($params, 1);
		}
		$this->params = explode('/', $params);

		foreach ($this->params as &$val)
		{
			$val = trim($val);
		}
		unset($val);

		$this->action = trim(array_shift($this->params));
		if (empty($this->action))
		{
			$this->action = 'index';
		}
	}

	private function fixButtonValues()
	{
		foreach ($this->getArray(DekiForm::BUTTON_ARRAY) as $key => $value)
		{
			$_REQUEST[$key] = key($value);
		}
		unset($_REQUEST[DekiForm::BUTTON_ARRAY]);
	}
}
