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

class DekiMvcRequest extends DekiRequest
{
	const PARAMS_GET_KEY = 'params';
	protected $webRoot = '';

	protected $params = null;
	protected $action = null;

	/**
	 * @param bool $reload - force the request to be a deki mvc request
	 */
	static function getInstance($reload = false)
	{
		if (is_null(self::$instance) || $reload)
		{
			self::$instance = new DekiMvcRequest();
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

	protected function __construct()
	{
		parent::__construct();

		$this->parseQueryParams();
	}

	// /action/param1/param2/param3?get1=&get2=
	// get variables are NOT params
	public function getParams()		{ return $this->params; }
	public function getAction()		{ return $this->action; }

	// local url builder
	/**
	 * @param string $page - the actual script file name without the .php extension
	 * @param string $params - should include the action and any request params, e.g. /view/32
	 * @param array $get - key => val pairs of GET params to append
	 * @param bool $preserve - if true, any get params with null values will be taken from $_GET
	 */
	public function getLocalUrl($page, $params = null, $get = array(), $preserve = false)
	{
		if (!is_null($params))
		{
			$get[self::PARAMS_GET_KEY] = $params;
		}
		
		$queryString = '';
		if (!empty($get))
		{
			$append = array();
			foreach ($get as $key => $val)
			{
				if ($preserve && is_numeric($key))
				{
					$key = $val;
					$val = $this->getVal($key, null);
					if (is_null($val))
					{
						continue;
					}
				}
				else if (!is_string($key) || is_array($val) || is_object($val))
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
		// remove any encoded &amp;
		header('Location: ' . str_replace('&amp;', '&', $url));
	}


	public function setWebRoot($webRoot = '')
	{
		$this->webRoot = $webRoot;
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
}
