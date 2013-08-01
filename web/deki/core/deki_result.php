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
 * DekiResult
 * Wraps API results with accessors & UI helpers.
 */
class DekiResult
{
	private $result = array();
	private $rootKey = '';
	
	public function __construct(&$result)
	{
		$this->result = $result;

		$debug = class_exists('Config') && defined('Config::DEBUG') ? Config::DEBUG : false;
		if ($debug && isset($this->result['uri']))
		{
			// always show the api error if debugging
			$this->handleResponse(array());
		}
	}
	
	// debugging function
	public function debug($exit = false)
	{
		if (!$exit && function_exists('fb'))
		{
			// firephp debugging enabled
			fb($this->result);
		}
		else
		{
			echo '<pre>';
			print_r($this->result);
			echo '</pre>';
		}

		if ($exit)
		{
			exit();
		}
	}
	
	public function getHeader($name, $default = null) { return $this->getVal('headers/'. $name, $default); }
	public function getStatus($return = 0)	{ return isset($this->result['status']) ? $this->result['status'] : $return; }
	public function getUri($return = '')	{ return isset($this->result['uri']) ? $this->result['uri'] : $return; }
	public function getError($withTitle = false)
	{
		$title = $withTitle ? $this->getErrorTitle() . ' ' : '';

		// pretty API error
		if (!is_null($error = $this->getVal('/body/error/message', null)))
		{
			return $title . $error;
		}

		// exception API error
		if (!is_null($error = $this->getVal('/body/exception/message', null)))
		{
			return $title . $error;
		}
		
		// other error
		return $title . (isset($this->result['error']) ? $this->result['error'] : null);
	}
	
	/**
	 * If there was a connection problem or internal curl error this will be true
	 * @return bool
	 */
	public function isCurlError() { return $this->result['errno'] > 0; }

	public function is($status) { return $this->getStatus() == $status; }

	public function isSuccess()
	{
		$status = $this->getStatus();
		return ($status >= 200 && $status < 300);
	}

	public function handleUnreachable()
	{
		if ($this->getStatus() == 503)
		{
			include(DEKI_ROOT . '/skins/down.php');
			exit();
		}
	}

	public function handleResponse($hideStatus = array(401))
	{
		//a plug response was not returned
		if (!is_array($this->result))
		{
			return false;
		}
		
		// grab the response status
		$status = $this->getStatus();

		// 503: Service not Available usually means Dekihost has crashed and should be restarted
		$this->handleUnreachable();

		
		//200-level is good
		if ($this->isSuccess())
		{
			return true;
		}
		
		if ($status == 0)
		{
			$uri = parse_url($this->result['uri']);
			$message = wfMsg('System.Error.couldnt-connect-api-body', $uri['scheme'], $uri['host']) . '<br/>' . $this->result['uri'];
			DekiMessage::apiResponse(wfMsg('System.Error.couldnt-connect-api-title'), $message);

			return false;
		}
					
		//if the API response contains a title, pass this through, otherwise use a default
		$title = $this->getErrorTitle();
		
		//if the API response contains a message of some sort, pass this through, otherwise use a default
		if (is_null(($message = $this->getVal('/body/error/message'))))
		{
			$message = '';
		}
		
		$body = $this->getVal('body');
		if (is_array($body))
		{
			wfScrubSensitiveArray($body);
			$body = print_r($body, true);
		}

		$response = wfMsg('Dialog.Message.request-uri') . "\n" .
					wfScrubSensitive($this->result['request']['uri']) . "\n\n" .
					wfMsg('Dialog.Message.server-response') . "\n" . 
					$body;
		
		if (!in_array($status, $hideStatus))
		{
			DekiMessage::apiResponse($title, $message, $response);
		}
		return false;
	}

	/**
	 * Accessor for the result array
	 * @return array
	 */
	public function toArray()
	{
		return $this->result;
	}

	/***
	 * Given an array $array, will try to find $key, which is delimited by /
	 * if $key itself is an array of multiple values which has a key of '0', will return the first value
	 * this is useful for getting stuff back from the api and to avoid the "cannot use string offset as array" error, 
	 * see http://www.zend.com/forums/index.php?S=ab6bd42e992e7497c9b0ba4a33b01dd9&t=msg&th=1556
	 */
	public function getVal($key = '', $default = null)
	{
		$key = $this->getKey($key);
		$array = $this->result;

		if ($key == '') {
			return $array;
		}
		$keys = explode('/', $key);
		$count = count($keys);
		$i = 0;
		foreach ($keys as $k => $val) {
			$i++;
			if ($val == '') {
				continue;
			}
			if (isset($array[$val]) && !is_array($array[$val])) {
				// see bugfix 4974; this used to do an empty string check, but that leads to ambiguity between empty string and null
				if ((is_string($array[$val]) || is_int($array[$val])) && $i == $count) {
					 return $array[$val];
				}
				return $default; 
			}
			if (isset($array[$val])) {
				$array = $array[$val];
			}
			else {
				return $default;
			}
			if (is_array($array) && key($array) == '0') {
				$array = current($array);
			}
		}
		return $array;
	}

	public function getAll($key = '', $default = null)
	{
		$key = $this->getKey($key);
		$array = $this->result;

		if ($key == '') {
			return $array;
		}
		$keys = explode('/', $key);
		$count = count($keys);
		$i = 0;
		foreach ($keys as $val) {
			$i++;
			if ($val == '') {
				continue;
			}
			if (!isset($array[$val]) || !is_array($array[$val])) {
				return $default; 
			}
			$array = $array[$val]; 
			if ($i == $count) {
				if (key($array) != '0') {
					$array = array($array);
				}
			}
		}
		return $array;
	}
	
	public function getXml($key = '')
	{
		$val = $this->getVal($key, null);
		return encode_xml($val);
	}

	public function setRootKey($key)
	{
		if (substr($key, -1) == '/')
		{
			$key = substr($key, 0, -1);
		}

		$this->rootKey = $key;
	}

	private function getKey($key)
	{
		if (strncmp($key, '/', 1) == 0)
		{
			// specified a root key
			return $key;
		}
		
		// a relative key was specified
		return empty($this->rootKey) ? $key : $this->rootKey . '/' . $key;
	}

	private function getErrorTitle()
	{
		$status = $this->getStatus();
		$title = '';
	
		if (is_null($title = $this->getVal('body/error/title')))
		{
			if ($status == 401)
			{
				$title = wfMsg('System.Error.login-unauthorized');
			}
			else
			{
				$title = wfMsg('System.Error.error-number', $status);
			}
		}
		else
		{
			$title .= ' ('. $status .')';	
		}

		return $title;
	}
}
