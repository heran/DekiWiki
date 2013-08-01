<?php
# MindTouch Dream - a distributed rest framework 
# Copyright (C) 2006 MindTouch, Inc 
# www.mindtouch.com (http://www.mindtouch.com/) <oss@mindtouch.com (mailto:oss@mindtouch.com)>
#
# For community documentation or downloads visit www.opengarden.org (http://www.opengarden.org/), 
# please review the licensing section.
#
# This library is free software; you can redistribute it and/or
# modify it under the terms of the GNU Lesser General Public
# License as published by the Free Software Foundation; either
# version 2.1 of the License, or (at your option) any later version.
# 
# This library is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
# Lesser General Public License for more details.
# 
# You should have received a copy of the GNU Lesser General Public
# License along with this library; if not, write to the Free Software
# Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
# http://www.gnu.org/copyleft/lesser.html

class Plug {

	// Http answers
	const HTTPSUCCESS = 200;
	const HTTPNOTFOUND = 404;
	const HTTPAUTHFAILED = 401;
	
	//--- Fields ---
	/**
	 * String $classname - determines the class to instantiate, allows plug to be extended
	 */
	protected $classname = null;
	/**#@+
	 * @access private
	 */
	var $scheme;
	var $user;
	var $password;
	var $host;
	var $port;
	var $path;
	var $query;
	var $fragment;
	var $timeout = 300;
	var $headers;# = Array("Content-Type" => "application/xml");
	/**#@-*/


	//--- Constructors ---
	/**
	 * Constructor
	 *
	 * @param mixed $uri
	 * @param string $output, optional, default = 'php'
	 * @param string $hostname, optional, default = null
	 */
	function Plug($uri = null, $output = 'php', $hostname = null) {
		// set the actual classname
		$this->classname = get_class($this);

		// initialize from uri string
		if(is_string($uri)) {
			$uri = parse_url($uri);
		}
		
		$this->headers = array();

		// initialize from Plug object
		if (is_object($uri)) {
			$this->scheme = $uri->scheme;
			$this->user = $uri->user;
			$this->password = $uri->password;
			$this->host = $uri->host;
			$this->port = $uri->port;
			$this->path = $uri->path;
			$this->query = $uri->query;
			$this->fragment = $uri->fragment;
			$this->timeout = $uri->timeout;
			$this->headers = $uri->headers;
		}

		// initialize from uri array
		if(is_array($uri)) {
			$this->scheme = isset($uri['scheme']) ? $uri['scheme'] : null;
			$this->user = isset($uri['user']) ? $uri['user'] : null;
			$this->password = isset($uri['pass']) ? $uri['pass'] : null;
			$this->host = isset($uri['host']) ? $uri['host'] : null;
			$this->port = isset($uri['port']) ? $uri['port'] : null;
			$this->path = isset($uri['path']) ? $uri['path'] : null;
			$this->query = isset($uri['query']) ? $uri['query'] : null;
			$this->fragment = isset($uri['fragment']) ? $uri['fragment'] : null;
		}

		// set php output option
		//bugfix 3445: Plug does not append dream.in.* values when PHP output is not set
		//when you invoke plug w/o the PHP output, we need these values appended automatically.
		// To prevent multiple values from being appended after calling At(),  With(), etc check $output != false.
		if ($output != false) {
			if($this->query) {
				$this->query .= '&';
			} else {
				$this->query = '';
			}
			if (empty($hostname)) {
				$hostname = $_SERVER['HTTP_HOST'];
			}
			$this->query .= 'dream.out.format=' . rawurlencode($output);
			$this->query .= '&dream.in.host=' . rawurlencode($hostname);
			
			//hack hack, pass in scheme until dream.in.uri is available
			// parse the scheme from the frontend request
			if(isset($_SERVER['HTTPS']) &&  $_SERVER['HTTPS'] == "on")
				$scheme = 'https';
			else
				$scheme = 'http';

			$this->query .= '&dream.in.scheme=' . $scheme;
			
			if (isset($_SERVER['REMOTE_ADDR'])) {
				$this->query .= '&dream.in.origin=' . rawurlencode($_SERVER['REMOTE_ADDR']);
			}
		}
		
		//todo: hack hack, this should not be in here, fix this later
		$this->headers['X-Forwarded-For'] = $_SERVER['REMOTE_ADDR'];
		if(isset($_SERVER['HTTP_HOST']))
 			$this->headers['X-Forwarded-Host'] = $_SERVER['HTTP_HOST'];
		if(isset($_SERVER['SERVER_NAME']))
			$this->headers['X-Forwarded-Server'] = $_SERVER['SERVER_NAME'];
	}
	
	//--- Methods ---
	function At(/* $path[] */) {
		$result = new $this->classname($this, false);

		foreach(func_get_args() as $path) {
			$result->path .= '/' . $path;
		}
		return $result;
	}

	/***
	 * Appends to the query string GET variables
	 * TODO: we should not write directly to string here; we should operate on an array and convert the array when we POST; 
	 * this'll allow us to dynamic transformations of URLs
	 */
	function With($name, $value = false) {
		$result = new $this->classname($this, false);

		if($result->query) {
			$result->query .= '&' . urlencode($name) . ($value !== false ? '=' . urlencode($value) : '');
		} else {
			$result->query = urlencode($name) . ($value !== false ? '=' . urlencode($value) : '');
		}
		return $result;
	}
	
	function SetHeader( $name, $value ){
		$result = new $this->classname($this, false);
		$result->headers[$name] = $value;
		return $result;
	}
	
	function GetHeaders() {
		$lHeaders = array();
		if (is_array($this->headers) && sizeof($this->headers) > 0) {
			foreach ($this->headers as $lHeaderName => $lHeaderValue) {
				$lHeaders[] = "$lHeaderName: $lHeaderValue";
			}
			return $lHeaders;
		} else {
			return false;
		}
	}
	
	function WithCredentials($user, $password) {
		$result = new $this->classname($this, false);
		$result->user= $user;
		$result->password = $password;
		return $result;
	}
	
	function Get(){
		return $this->Invoke('GET', null, false);
	}
		
	function GetMessage() {
		return $this->Invoke('GET');
	}
	
	function Post($input = null) {
		if (is_array($input)) {
			return $this->InvokeXml('POST', $input);
		}
		else {
			return $this->InvokeFields('POST', $input);
		}
	}	

	function PostMessage($xml = null) {
		return $this->InvokeXml('POST', $xml);
	}	

	function PostFields($formFields ){
		return $this->InvokeFields('POST', $formFields);
	}
		
	function Put($xml = null) {
		$r = $this->With('dream.in.verb', 'PUT');
		return $r->InvokeXml('POST', $xml);
	}
	
	function PutFields($formFields){
		return $this->InvokeFields('PUT', $formFields);
	}
	
	function PutFile($content = array()) {
		return $this->Invoke('PUT', $content, false);
	}	
		
	function Delete($xml = null) {
		//Mono has a Content-length: 0 bug, so we can use Dream's faux-method handling
		$r = $this->With('dream.in.verb', 'DELETE');
		return $r->InvokeXml('POST', $xml);
	}
	
	function InvokeXml( $verb, $xml){
		if(is_array($xml)) {
			$xml = deki_encode_xml($xml);
		}
		if (empty($this->headers['Content-Type'])) {
			$this->headers['Content-Type'] = 'application/xml';
		}
		return $this->Invoke($verb, $xml, false);
	}
		
	function InvokeFields($verb, $formFields ){
		return $this->Invoke($verb, $formFields);
	}

	function Invoke($verb, $content = null, $callback = false) {
		$uri = $this->GetUri();

		// prepare request
		$curl = curl_init();
		curl_setopt($curl, CURLOPT_URL, $uri);
		curl_setopt($curl, CURLOPT_RETURNTRANSFER, 1);
		curl_setopt($curl, CURLOPT_TIMEOUT, $this->timeout);
		curl_setopt($curl, CURLOPT_FOLLOWLOCATION, 1);
		curl_setopt($curl, CURLOPT_MAXREDIRS, 10);
		curl_setopt($curl, CURLOPT_CUSTOMREQUEST, $verb);
		curl_setopt($curl, CURLOPT_SSL_VERIFYPEER, false);

		// immitate user agent if one is present
		if(array_key_exists('HTTP_USER_AGENT', $_SERVER)) {
			curl_setopt($curl, CURLOPT_USERAGENT, $_SERVER['HTTP_USER_AGENT']);
		}

		// empty() will mean that string "0" will be set to 0 content-length; !$content is too fuzzy
		if (is_null($content) || $content === false || (is_string($content) && strlen($content) == 0)) {
			$this->headers['Content-Length'] = 0;
		}
		
		$this->ApplyCredentials($curl);
		if ($verb == 'PUT') {
			curl_setopt($curl, CURLOPT_PUT, true);
			if (is_file($content['file_temp'])) {
				curl_setopt($curl, CURLOPT_INFILE, fopen($content['file_temp'], "r"));
				curl_setopt($curl, CURLOPT_INFILESIZE, filesize($content['file_temp']));
				$this->headers["Content-Type"] =  $content['file_type'];
			}
			$content = array();
		} elseif ($verb == 'POST') {
			curl_setopt($curl, CURLOPT_POSTFIELDS, $content);
		}
		
		if (sizeof($this->headers) > 0) {
			curl_setopt($curl, CURLOPT_HTTPHEADER, $this->GetHeaders());
		}
		
		// execute request
		$result = array();
		$reply = curl_exec($curl);
		$status = curl_getinfo($curl, CURLINFO_HTTP_CODE);
		$type = curl_getinfo($curl, CURLINFO_CONTENT_TYPE);
		$result['errno'] = curl_errno($curl);
		$result['error'] = curl_error($curl);
		curl_close($curl);
		
		// check if we need to deserialize
		if(strpos($type, '/php')) {
			$reply = unserialize($reply);
		}
		$result['request'] = array('uri' => $this->getUri(), 'body' => $content);
		$result['uri'] = $uri;
		$result['body'] = $reply;
		$result['status'] = $status;
		$result['type'] = $type;
		
		// check if we need to invoke a callback
		if($callback) {
			return call_user_func($callback, $result);
		} else {
			return $result;
		}
	}

	function GetUri() {
		$uri = $this->scheme ? $this->scheme . ':' . ((strtolower($this->scheme) == 'mailto') ? '' : '//') : '';
		$uri .= $this->host ? $this->host : '';
		$uri .= $this->port ? ':' . $this->port : '';
		$uri .= $this->path ? $this->path : '';
		$uri .= $this->query ? '?'. $this->query : '';
		$uri .= $this->fragment ? '#' . $this->fragment : '';
		return $uri;
	}
        
    function ApplyCredentials($curl) {
        
		// apply manually given credentials
		if(isset($this->user) || isset($this->password)) {
			$this->headers['Authorization'] = 'Basic '.base64_encode($this->user . ':' . $this->password);
		} else {
			if(function_exists("getallheaders")) {
				$headers = getallheaders();
			
				// BUGBUGBUG (steveb): this is DEKI specific and should not be in dream plug!
			
				// check if there is an authentication token
				if (isset($_COOKIE['authtoken'])) 
				{
					$authToken = $_COOKIE['authtoken'];
				}
				elseif (isset($headers['X-Authtoken'])) 
				{
					$authToken = $headers['X-Authtoken'];
				}
				
				if (isset($authToken)) 
				{
					$authToken = trim($authToken, '"');
					$this->headers['X-Authtoken'] = $authToken;
				} 
				elseif( isset($headers['Authorization'])) 
				{
					// Use encoded credentials from the php request header. (e.g. Basic c3lzb3A6c3lzb3A=)
					$this->setHeader('Authorization', $headers['Authorization']);
				}
			}
		}
    }        
}

class DekiPlug extends Plug
{
	//protected $classname = 'DekiPlug';

	public function Post($input = null)
	{
		return new DreamResult(parent::Post($input));
	}

	public function Put($input = null)
	{
		return new DreamResult(parent::Put($input));
	}

	public function Get()
	{
		return new DreamResult(parent::Get());
	}	
	public function Delete()
	{
		return new DreamResult(parent::Delete());
	}	
}


class DreamResult
{
	private $result = array();
	private $rootKey = '';
	
	public function __construct(&$result)
	{
		$this->result = $result;
	}
	
	// debugging function
	public function debug($exit = false)
	{
		echo '<pre>';
		print_r($this->result);
		echo '</pre>';

		if ($exit)
		{
			exit();
		}
	}

	public function getStatus($return = 0)
	{
		return isset($this->result['status']) ? $this->result['status'] : $return;
	}
	
	public function getUri($return = '')
	{
		return isset($this->result['uri']) ? $this->result['uri'] : $return;
	}

	public function isSuccess()
	{
		$status = $this->getStatus();
		return ($status >= 200 && $status < 300);
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
				if ((is_string($array[$val]) || is_int($array[$val])) && $array[$val] != '' && $i == $count) {
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
}

function deki_is_num_array($data) {
	return is_array($data) && (count($data) > 0) && isset($data[0]);
}

function deki_encode_xml($data, $outer = null) {
	$result = '';
	if(is_array($data)) {
		foreach($data as $key => $value) {
			if(strncmp($key, '@', 1) == 0) {

				// skip attributes
			} else {
				$tag = $outer ? $outer : $key;
				if(deki_is_num_array($value)) {
					$result .= deki_encode_xml($value, $key);
				} elseif(is_array($value)) {
					$attrs = '';
					foreach($value as $attr_key => $attr_value) {
						if(strncmp($attr_key, '@', 1) == 0) {
							$attrs .= ' ' . substr($attr_key, 1) . '="' . htmlspecialchars($attr_value) . '"';
						}
					}
					$result .= '<' . $tag . $attrs . '>' . deki_encode_xml($value) . '</' . $tag . '>';
				} elseif($tag != '#text') {
					$result .= '<' . $tag . '>' . deki_encode_xml($value) . '</' . $tag . '>';
				} else {
					$result .= htmlspecialchars($value);
				}
			}
		}
	} elseif(is_string($data)) {
		return htmlspecialchars($data);
	} else {
	
		// TODO (steveb): how we should handle this case?
		$result = $data;
	}
	return $result;
}