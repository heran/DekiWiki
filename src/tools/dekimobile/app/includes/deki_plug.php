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

class DekiPlug extends DreamPlug
{
	static $instance = null;
	
	static function &getInstance()
	{
		if (!is_object(self::$instance))
		{
			throw new Exception('DekiPlug has not be initialized yet.');
		}

		return self::$instance;
	}

	static function setInstance($Plug)
	{
		self::$instance = $Plug;
	}
	
	/**
	 * Method retrieves the apikey
	 * hack hack hack
	 * TODO: guerrics, use a config class to retrieve the key for site/settings
	 */
	static function getApiKey()
	{
		global $wgDekiApiKey;

		return isset($wgDekiApiKey) && !empty($wgDekiApiKey) ? $wgDekiApiKey : Config::$API_KEY;
	}


	public function Delete($input = null)
	{
		return new DekiResult(parent::Delete($input));
	}

	public function Post($input = null)
	{
		return new DekiResult(parent::Post($input));
	}

	public function Put($input = null)
	{
		return new DekiResult(parent::Put($input));
	}

	public function PutFile($input = null)
	{
		return new DekiResult(parent::PutFile($input));
	}

	public function Get()
	{
		return new DekiResult(parent::Get());
	}
	
	public function At(/* $path[] */) 
	{
		$result = new $this->classname($this, false);

		foreach (func_get_args() as $path) 
		{
			$result->path .= '/';
			if (is_array($path))
			{
				$result->path .= current($path);
			}
			else
			{
				// auto-double encode entities prefixed with an equal sign
				$result->path .= (
					strpos($path, '=') === 0 ?
					'='.urlencode(urlencode(substr($path, 1))) :
					urlencode(urlencode($path))
				);
			}
		}
		return $result;
	}

	protected function ApplyCredentials($curl)
	{
		// apply manually given credentials
		if (isset($this->user) || isset($this->password))
		{
			$this->headers['Authorization'] = 'Basic ' . base64_encode($this->user . ':' . $this->password);
		}
		else if (function_exists("getallheaders")) 
		{
			$headers = getallheaders();

			// Deki specific authorization
			// check if there is an authentication token
			if (isset($_COOKIE['authtoken'])) 
			{
				$authToken = $_COOKIE['authtoken'];
			}
			else if (isset($headers['X-Authtoken'])) 
			{
				$authToken = $headers['X-Authtoken'];
			}
			
			if (isset($authToken)) 
			{
				$authToken = trim($authToken, '"');
				$this->headers['X-Authtoken'] = $authToken;
			} 
			else if (isset($headers['Authorization'])) 
			{
				// Use encoded credentials from the php request header. (e.g. Basic c3lzb3A6c3lzb3A=)
				$this->setHeader('Authorization', $headers['Authorization']);
			}
		}
	}
}
