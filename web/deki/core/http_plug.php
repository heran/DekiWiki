<?php
/*
 * MindTouch HttpPlug - simple HTTP requests
 * Copyright (C) 2006-2010 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit wiki.developer.mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/**
 * MindTouch HttpPlug - simple HTTP requests
 */
class HttpPlug
{
	const DEFAULT_HOST = 'localhost';
	
	const HEADER_CONTENT_TYPE = 'Content-Type';
	const HEADER_AUTHORIZATION = 'Authorization';
	const HEADER_CONTENT_LENGTH = 'Content-Length';
	
	const VERB_DELETE = 'DELETE';
	const VERB_GET = 'GET';
	const VERB_HEAD = 'HEAD';
	const VERB_POST = 'POST';
	const VERB_PUT = 'PUT';

	/**
	 * @note guerrics: feel free to directly set this value
	 * @var int $timeout - sets the request timeout length
	 */
	public $timeout = 300;
	
	/**
	 * @var string $classname - PHP hack to determine the class to instantiate. Allows plug to be extended
	 */
	protected $classname = null;
	
	// URI components
	protected $scheme = null;
	protected $user = null;
	protected $password = null;
	protected $host = null;
	protected $port = null;
	protected $path = null;
	protected $query = null;
	protected $fragment = null;
	
	/**
	 * @var array $headers - stores the headers for the request
	 */
	protected $headers = array();

	/**
	 * Helper for encoding a PHP arrays into XML
	 * 
	 * @param mixed $data - input data to encode
	 * @param string $outer - optional output tag, used for recursion
	 * @return string - xml representation of the array
	 */
	public static function EncodeXml($data, $outer = null)
	{
		$result = '';
		
		if (is_array($data))
		{
			foreach ($data as $key => $value)
			{
				if (strncmp($key, '@', 1) == 0)
				{
					// skip attributes
				}
				else
				{
					$tag = $outer ? $outer : $key;
					if (is_array($value) && (count($value) > 0) && isset($value[0]))
					{
					 	// numeric array found => child nodes
						$result .= self::EncodeXml($value, $key);
					}
					else if (is_array($value))
					{
						// attribute list found
						$attrs = '';
						foreach ($value as $attr_key => $attr_value)
						{
							if (strncmp($attr_key, '@', 1) == 0)
							{
								$attrs .= ' ' . substr($attr_key, 1) . '="' . htmlspecialchars($attr_value) . '"';
							}
						}
						$result .= '<' . $tag . $attrs . '>' . self::EncodeXml($value) . '</' . $tag . '>';
					}
					else if ($tag != '#text')
					{
						$result .= '<' . $tag . '>' . self::EncodeXml($value) . '</' . $tag . '>';
					}
					else
					{
						$result .= htmlspecialchars($value);
					}
				}
			}
		}
		else if (is_string($data))
		{
			return htmlspecialchars($data);
		}
		else
		{
			// @TODO: how we should handle this case?
			$result = $data;
		}
		
		return $result;
	}

	/**
	 * Helper method enables an array to have to multiple values per key. Creates
	 * nested arrays when more than 1 value is assigned per key.
	 * 
	 * @param array &$multiKey
	 * @param string $key 
	 * @param string $value
	 * @param bool $append
	 */
	protected static function setMultiValueArray(&$multi, $key, $value, $append = false)
	{
		if ($append && isset($multi[$key]))
		{
			if (!is_array($multi[$key]))
			{
				$current = $multi[$key];
				$multi[$key] = array();
				$multi[$key][] = $current;
			}
			
			$multi[$key][] = $value;
		}
		else
		{
			$multi[$key] = $value;
		}
	}

	/**
	 * Helper method to flatten a Plug header array
	 * 
	 * @return string[] - array of headers
	 */
	protected static function flattenPlugHeaders(&$headers)
	{
		$flat = array();
		if (!empty($headers))
		{
			foreach ($headers as $name => $value)
			{
				if (is_array($value))
				{
					foreach ($value as $multi)
					{
						$flat[] = $name .': '. $multi;
					}
				}
				else
				{
					$flat[] = $name .': '. $value;	
				}
			}
		}
		return $flat;
	}

	/**
	 * @param mixed $uri - of type string, array, or Plug object
	 */
	public function __construct($uri)
	{
		// set the actual classname
		$this->classname = get_class($this);
		
		// initialize from uri string
		if (is_string($uri))
		{
			$uri = parse_url($uri);
		}
		
		// initialize from uri array
		if (is_array($uri))
		{
			$this->scheme = isset($uri['scheme']) ? $uri['scheme'] : null;
			$this->user = isset($uri['user']) ? $uri['user'] : null;
			$this->password = isset($uri['pass']) ? $uri['pass'] : null;
			$this->host = isset($uri['host']) ? $uri['host'] : null;
			$this->port = isset($uri['port']) ? $uri['port'] : null;
			$this->path = isset($uri['path']) ? $uri['path'] : null;
			$this->query = isset($uri['query']) ? $uri['query'] : null;
			$this->fragment = isset($uri['fragment']) ? $uri['fragment'] : null;
		}
		// initialize from Plug object
		else if (is_object($uri))
		{
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
		
		// default host if not provided
		if (empty($this->host))
		{
			$this->host = self::DEFAULT_HOST;
		}
	}

	/**
	 * Returns a list of the headers that have been set
	 * 
	 * @return array
	 */
	public function GetHeaders()
	{
		return self::flattenPlugHeaders($this->headers);
	}

	/**
	 * Retrieves the fully generate uri
	 * 
	 * @param bool $includeCredentials - if true, any set username and password will be included
	 * @return string - uri
	 */
	public function GetUri($includeCredentials = false)
	{
		$uri = $this->scheme ? $this->scheme . ':' . ((strtolower($this->scheme) == 'mailto') ? '' : '//') : '';
		
		// @note user & password are passed via Authorization headers, see #invokeApplyCredentials
		if ($includeCredentials)
		{
			$uri .= $this->user ?  $this->user . ($this->password ? ':' .  $this->password : '') . '@' : '';
		}
		$uri .= $this->host ? $this->host : '';
		$uri .= $this->port ? ':' . $this->port : '';
		
		// ensure a trailing slash is provided
		if ( (substr($uri, -1) != '/') && (strncmp($this->path, '/', 1) != 0) )
		{
			$uri .= '/';
		}
		$uri .= $this->path ? $this->path : '';
		$uri .= $this->query ? '?'. $this->query : '';
		$uri .= $this->fragment ? '#' . $this->fragment : '';
		
		return $uri;
	}

	/**
	 * Uri builder
	 * 
	 * @param $path[] - method takes any number of path components
	 * @return HttpPlug
	 */
	public function At(/* $path[] */)
	{
		$Plug = new $this->classname($this);
		$args = func_get_args();
		
		// MT-7254 PHP Plug accepts trailing slashes
		if (!empty($args) && $Plug->path == '/')
		{
			$Plug->path = '';
		}
		foreach ($args as $path)
		{
			$Plug->path .= '/' . ltrim($path, '/');
		}
		return $Plug;
	}

	/***
	 * Appends to the query string GET variables
	 * TODO: we should not write directly to string here; we should operate on an array and convert the array when we POST; 
	 * this'll allow us to dynamic transformations of URLs
	 * 
	 * @param string $name - variable name
	 * @param string $value - variable value
	 * @return HttpPlug
	 */
	public function With($name, $value = false)
	{
		$Plug = new $this->classname($this);

		if ($Plug->query)
		{
			$Plug->query .= '&' . urlencode($name) . ($value !== false ? '=' . urlencode($value) : '');
		}
		else
		{
			$Plug->query = urlencode($name) . ($value !== false ? '=' . urlencode($value) : '');
		}

		return $Plug;
	}

	/**
	 * Sets a header value to pass with the request
	 * 
	 * @param $name - header name
	 * @param $value - header value
	 * @param bool $append - if true, then the headers are appended
	 * @return HttpPlug
	 */
	public function WithHeader($name, $value, $append = false)
	{
		$Plug = new $this->classname($this);
		self::setMultiValueArray($Plug->headers, $name, $value, $append);
		return $Plug;
	}

	/**
	 * Adds standard HTTP auth credentials for the request
	 * 
	 * @param string $user - user name to use for authorization
	 * @param string $password
	 * @return HttpPlug
	 */
	public function WithCredentials($user, $password)
	{
		$Plug = new $this->classname($this);
		$Plug->user = $user;
		$Plug->password = $password;

		return $Plug;
	}

	/**
	 * Performs a GET request
	 * 
	 * @return array - request response
	 */
	public function Get()
	{
		return $this->invoke(self::VERB_GET);
	}

	/**
	 * Performs a HEAD request
	 * 
	 * @return array
	 */
	public function Head()
	{
		return $this->invoke(self::VERB_HEAD);
	}

	/**
	 * Performs a POST request
	 * 
	 * @param mixed $input - if array, gets encoded as xml. otherwise treated at post fields.
	 * @return array - request response
	 */
	public function Post($input = null)
	{
		if (is_array($input))
		{
			return $this->invokeXml(self::VERB_POST, $input);
		}
		else
		{
			return $this->invokeFields(self::VERB_POST, $input);
		}
	}

	public function PostFields($formFields)
	{
		return $this->invokeFields(self::VERB_POST, $formFields);
	}
	
	/**
	 * Performs a PUT request
	 * 
	 * @param array $input - if array, gets encoded as xml
	 * @return array - request response
	 */
	// @TODO guerrics: method does not seem to work. hangs apache.
	public function Put($input = null)
	{
		$contentLength = is_null($input) ? 0 : strlen($input);
		
		// explicitly set content-length for put requests
		$Plug = $this->WithHeader(self::HEADER_CONTENT_LENGTH, $contentLength);
		return $Plug->invokeXml(self::VERB_PUT, $input);
	}
	
	public function PutFields($formFields)
	{
		return $this->invokeFields(self::VERB_PUT, $formFields);
	}
	
	public function PutFile($path, $mimeType = null)
	{
		return $this->invoke(self::VERB_PUT, $path, $mimeType, true);
	}	

	/**
	 * Performs a DELETE request
	 * 
	 * @param array $xml
	 * @return array - request response
	 */
	public function Delete($input = null)
	{
		return $this->invokeXml(self::VERB_DELETE, $input);
	}

	/**
	 * 
	 * @param string $verb
	 * @param mixed $xml - XML encoded array or XML string
	 * @return array - request response
	 */
	protected function invokeXml($verb, $xml)
	{
		if (is_array($xml))
		{
			$xml = self::EncodeXml($xml);
		}

		// @note guerrics: adding empty check since dream dies on empty xml bodies
		$contentType = !empty($xml) && empty($this->headers[self::HEADER_CONTENT_TYPE]) ? 'application/xml' : null;
		return $this->invoke($verb, $xml, $contentType);
	}
	
	/**
	 * 
	 * @param $verb
	 * @param $formFields
	 * @return array - request response
	 */
	protected function invokeFields($verb, $formFields)
	{
		return $this->invoke($verb, $formFields);
	}
	
	/**
	 * 
	 * @param string $verb
	 * @param string $content
	 * @param string $contentType
	 * @param bool $contentFromFile - if true, then $content is assumed to be a file path
	 * @return array - request response
	 */
	protected function invoke($verb, $content = null, $contentType = null, $contentFromFile = false)
	{
		// create the request info
		$request = array(
			'uri' => $this->GetUri(),
		
			// grab unflattened headers
			'headers' => $this->headers
		);
		$curl = curl_init();
		curl_setopt($curl, CURLOPT_URL, $request['uri']);
		curl_setopt($curl, CURLOPT_RETURNTRANSFER, 1);
		
		// @TODO: proxy configuration
		//curl_setopt($curl, CURLOPT_PROXY, 'X.X.X.X:8888');
		
		curl_setopt($curl, CURLOPT_TIMEOUT, $this->timeout);
		curl_setopt($curl, CURLOPT_FOLLOWLOCATION, 1);
		curl_setopt($curl, CURLOPT_MAXREDIRS, 10);
		curl_setopt($curl, CURLOPT_CUSTOMREQUEST, $verb);
		curl_setopt($curl, CURLOPT_SSL_VERIFYPEER, false);

		// explicitly set content length for empty bodies
		if (is_null($content) || $content === false || (is_string($content) && strlen($content) == 0))
		{
			self::setMultiValueArray($request['headers'], self::HEADER_CONTENT_LENGTH, 0);
		}
		
		// set the content type if provided
		if (!is_null($contentType))
		{
			self::setMultiValueArray($request['headers'], self::HEADER_CONTENT_TYPE, $contentType);
		}
		
		// apply credentials before flattening the headers
		$this->invokeApplyCredentials($curl, $request['headers']);
		
		// custom behavior based on the request type
		switch ($verb)
		{
			case self::VERB_PUT:
				if ($contentFromFile && is_file($content))
				{
					// read in content from file
					curl_setopt($curl, CURLOPT_PUT, true);
					curl_setopt($curl, CURLOPT_INFILE, fopen($content, 'r'));
					curl_setopt($curl, CURLOPT_INFILESIZE, filesize($content));
				}
				break;
				
			case self::VERB_POST:
				/**
				 * The full data to post in a HTTP "POST" operation. To post a file, prepend a filename with @ and use the full path. 
				 * This can either be passed as a urlencoded string like 'para1=val1&para2=val2&...' or as an array with the field name as 
				 * key and field data as value. If value is an array, the Content-Type header will be set to multipart/form-data. 
				 */
				if ($contentFromFile && is_file($content))
				{
					// @TODO guerrics: test this functionality
					curl_setopt($curl, CURLOPT_POST, true);
					$content = '@'.$content;
				}
				curl_setopt($curl, CURLOPT_POSTFIELDS, $content);
				break;
				
			default:
		}
		
		// add the request headers
		if (!empty($request['headers']))
		{
			// flatten headers
			$request['headers'] = self::flattenPlugHeaders($request['headers']);
			curl_setopt($curl, CURLOPT_HTTPHEADER, $request['headers']);
		}
		
		// retrieve the response headers
		curl_setopt($curl, CURLOPT_HEADER, true);
		
		// execute request
		$this->invokeRequest($curl, $verb, $content, $contentType, $contentFromFile, $request);
		$httpMessage = curl_exec($curl);
		$this->invokeResponse($curl, $verb, $content, $contentType, $contentFromFile, $httpMessage);
		
		// create the response info
		$response = array(
			'headers' => array(),
			'status' => curl_getinfo($curl, CURLINFO_HTTP_CODE),
			'type' => curl_getinfo($curl, CURLINFO_CONTENT_TYPE),
			'errno' => curl_errno($curl),
			'error' => curl_error($curl)
		);
		curl_close($curl);
		
		// header parsing
		// make sure ther response is not empty before trying to parse
		// also make sure there isn't a curl error
		if ( ($response['status'] != 0) && ($response['errno'] == 0) )
		{
			// split response into header and response body
			do
			{
				list($headers, $httpMessage) = explode("\r\n\r\n", $httpMessage, 2);
				$headers = explode("\r\n", $headers);
				
				// First line of headers is the HTTP response code, remove it
				$httpStatus = array_shift($headers);
				
				// check if there is another header chunk to parse
			} while ($httpStatus == 'HTTP/1.1 100 Continue');
			
			// set the response body
			$response['body'] = &$httpMessage;
			
			// put the rest of the headers in an array
			foreach ($headers as $headerLine)
			{
				list($header, $value) = explode(': ', $headerLine, 2);
				
				// @TODO guerrics: fix this to allow multiple values per-header
				$response['headers'][$header] = trim($value);
			}
		}
		
		return $this->invokeComplete($request, $response);
	}

	/**
	 * 
	 * @param ch $curl
	 * @param array $headers
	 * @return
	 */
	protected function invokeApplyCredentials($curl, &$headers)
	{
		// apply manually given credentials
		if (isset($this->user) || isset($this->password))
		{
			self::setMultiValueArray(
				$headers,
				'Authorization',
				'Basic ' . base64_encode($this->user . ':' . $this->password)
			);
		}
		else if (function_exists("getallheaders"))
		{
			$requestHeaders = getallheaders();
			if (isset($requestHeaders[self::HEADER_AUTHORIZATION]))
			{
				// Use encoded credentials from the php request header. (e.g. Basic c3lzb3A6c3lzb3A=)
				self::setMultiValueArray(
					$headers,
					'Authorization',
					$requestHeaders[self::HEADER_AUTHORIZATION]
				);
			}
		}
	}

	/**
	 * Availabile for children
	 *
	 * @param ch $curl
	 * @param string $verb
	 * @param string $content
	 * @param string $contentType
	 * @param bool $contentFromFile
	 * @param array $request
	 * @return
	 */
	protected function invokeRequest(&$curl, &$verb, &$content, &$contentType, &$contentFromFile, &$request) {}

	/**
	 * Available for children
	 *
	 * @param ch $curl
	 * @param string $verb
	 * @param string $content
	 * @param string $contentType
	 * @param bool $contentFromFile
	 * @param string $httpMessage - complete http response
	 * @return
	 */
	protected function invokeResponse(&$curl, &$verb, &$content, &$contentType, &$contentFromFile, &$httpMessage) {}

	/**
	 * Format the invoke return
	 *
	 * @param array $request
	 * @param array $response
	 * @return array
	 */
	protected function invokeComplete(&$request, &$response)
	{
		// @TODO guerrics: conditionally return the request information?
		$response['request'] = $request;
		return $response;
	}
}
