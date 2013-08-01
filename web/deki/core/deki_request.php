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
 * Wrapper for accessing GET,POST variables
 */
class DekiRequest
{
	protected static $instance = null;
	protected $baseUri = '';

	/**
	 * Singleton
	 * @return DekiRequest
	 */
	static function &getInstance()
	{
		if (is_null(self::$instance))
		{
			self::$instance = new self();
		}

		return self::$instance;
	}


	protected function __construct()
	{
		$this->startSession();
		// TODO: frontend is already cleaning up magic quotes, uncomment after removing frontend
		//$this->cleanupMagicQuotes();
		$this->setBaseUri();
		$this->fixButtonValues();
	}
	
	/**
	 * Returns the hostname/domain name for the current request.
	 * @return string
	 */
	public function getHost()
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
		
		return $serverName;
	}

	/**
	 * Return the base uri for the current request. i.e. scheme://hostname:port
	 * @return string
	 */
	public function getBaseUri()	{ return $this->baseUri; }
	
	public function getScheme()
	{
		return $this->isSsl() ? 'https' : 'http';
	}
	
	/*
	 * Returns the complete requested URI including query params
	 * @return string
	 */
	public function getFullUri()
	{
		return $this->baseUri . $this->getLocalUri();
	}

	/*
	 * Returns the local requested URI including query params
	 * @return string
	 */
	public function getLocalUri()
	{
		return $_SERVER['REQUEST_URI'];
	}

	// ugly function to check if the user-agent is IE6
	public function isIE6() { return strpos($_SERVER['HTTP_USER_AGENT'], 'MSIE 6.0') !== false; }
	public function isXmlHttpRequest() { return ($this->getHeader('X_REQUESTED_WITH') == 'XMLHttpRequest'); }
	public function isSsl() { return isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] == 'on'; }
	public function isPost() { return strtoupper($_SERVER['REQUEST_METHOD']) == 'POST'; }

	public function has($key) { return !is_null($this->get($key, null)); }
	public function getFile($key) { return !isset( $_FILES[$key] ) ? null: $_FILES[$key]; }
	
	/**
	 * Attempts to determine the client's public IP address. Excludes private addresses by default.
	 *
	 * @param bool $includePrivate - return local addresses like 192.168.x.x (default false)
	 * @return string
	 */
	public function getClientIP($includePrivate = false) 
	{
		$ip = $_SERVER['REMOTE_ADDR'];
		
		// grab the proxy IPs
		if (isset($_SERVER['HTTP_CLIENT_IP']))
		{
			$proxyIp = $_SERVER['HTTP_CLIENT_IP']; 
		}
		elseif (isset($_SERVER['HTTP_X_FORWARDED_FOR'])) 
		{
			$proxyIp = $_SERVER['HTTP_X_FORWARDED_FOR']; 	
		}
		else
		{
			$proxyIp = null;	
		}
		
		$localRegexp = "/^(127|10|172\.16|192\.168)\./";
		
		if (!is_null($proxyIp))
		{
			$proxyIp = explode(',', $proxyIp);
			$xff = array_map('trim', $proxyIp);
			$xff = array_reverse($proxyIp);
			array_unshift($proxyIp, $ip); //lists IP addresses in order; see http://en.wikipedia.org/wiki/X-Forwarded-For
			
			// find the IP address that is furthest downstream that's public
			foreach ($proxyIp as $addy) 
			{
				if (!preg_match($localRegexp, $addy))
				{
					$ip = $addy;
					break;
				}
			}
		}
		else 
		{
			if (!$includePrivate && preg_match($localRegexp, $ip)) 
			{
				return null;
			}
		}
		return $ip; 
	}
	/**
	 * @note If checking against strings make sure you use lowercase
	 */
	public function getBool($key, $default = false)
	{
		$val = $this->get($key, $default);
		return $val === true || $val === 1 || $val === 'true' || $val === '1';
	}
	public function getInt($key, $default = null)
	{
		$val = $this->get($key, $default);
		return $val == $default ? $default : intval($val);
	}
	
	public function getVal($key = null, $default = null) 
	{ 
		return is_null($key) ? $_REQUEST : $this->get($key, $default);
	}
	
	/**
	 * Explicitly request a cookie value (PHP 5.3 may be configured to hide cookies from $_REQUEST) 
	 */
	public function getCookieVal($key, $default = null)
	{
		if (is_null($key))
		{
			return $_COOKIE;
		}
		
		return isset($_COOKIE[$key]) ? $_COOKIE[$key] : $default;
	}
	
	/**
	 * @return array
	 */
	public function getArray($key, $default = array())
	{
		$array = $this->get($key, null);
		return is_array($array) ? $array : $default;
	}
	
	/**
	 * Allows a value to be restricted to several values with a default
	 * @return mixed
	 */
	public function getEnum($key, $enum = array(), $default = null)
	{
		$value = $this->get($key, null);
		if (!is_null($value))
		{
			if (in_array($value, $enum))
			{
				return $value;
			}
		}
		
		return $default;
	}

	public function remove($key)
	{
		unset($_REQUEST[$key]);
		unset($_GET[$key]);
		unset($_POST[$key]);
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


	private function get($key, $default)
	{ 
		// see bug #4427; PHP magically converts these characters to underscores
		$key = str_replace(array('.', ' ', '['), array('_', '_', '_'), $key);
		
		return isset($_REQUEST[$key]) ? $this->trim($_REQUEST[$key]) : $default; 
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

	private function startSession()
	{
		@session_start();
	}

	public function setBaseUri($schema = null, $uri = null)
	{
		$this->schema = $schema;
		$this->baseUri = $uri;

		if (is_null($this->schema))
		{
			$this->schema = $this->getScheme();
		}

		if (is_null($this->baseUri))
		{
			$this->baseUri = $this->schema .'://'. $this->getHost();
			// determine if the request came on a different port
			if ( isset($_SERVER['SERVER_PORT']) &&
				(($this->schema == 'http' && $_SERVER['SERVER_PORT'] != 80) ||
				($this->schema == 'https' && $_SERVER['SERVER_PORT'] != 443))
			   )
			{
				$this->baseUri .= ':' . $_SERVER['SERVER_PORT'];
			}
		}
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

	private function fixButtonValues()
	{
		foreach ($this->getArray(DekiForm::BUTTON_ARRAY) as $key => $value)
		{
			$_REQUEST[$key] = key($value);
		}

		$this->remove(DekiForm::BUTTON_ARRAY);
	}
}
