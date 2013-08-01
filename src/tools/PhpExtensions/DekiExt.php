<?php
/*
 * MindTouch Deki Script - an embeddable scripting runtime
 * Copyright (C) 2006-2008 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
 *  please review the licensing section.
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 * http://www.gnu.org/copyleft/lesser.html
 */



//include the Incutio library that will convert the return response to xml-rpc
include("IXR_Library.inc.php");

class DekiExtServer extends IXR_Server {

	//class variables, mimics the ones IXR_Server has so it can make calls to
	//the IXR_Server		
    var $data;
    var $callbacks = array();
    var $message;
    var $capabilities;
	var $returnTypes = array();

	//--- class constructor ---
	/*
	 * DekiExtServer constructor
	 * @param array $callbacks - an array of all the functions being passed in
	 * @param array $returntypes - array of the return types of all the functions
	 * @param bool $data -  data from the POST call to the server
	 * 
	 * @note this function behaves like the IXR_Server in the IXR_libaray
	 * with the exception that it calls our DekiExtServe so that it can handle 
	 * types (like xml), that exist in DekiScript, but not in the XML-RPC standard
	 *        
	 */
	function DekiExtServer($returntypes, $callbacks = false, $data = false)
	{
		$this->returnTypes = $returntypes;
		parent::setCapabilities();

		if ($callbacks) 
		{
           	$this->callbacks = $callbacks;
		}

        parent::setCallbacks();
        $this->DekiExtServe($data);
	}

	//--- class methods ---

	/*
	 * DekiExtServe
	 * @param bool $data - data from the POST call to the server
	 *        
	 * @note: this function behaves exactly like the IXR_Server in the IXR_Library
	 * except it also checks if the return type is xml, if it is, it handles the
	 * xml so that it can be read by DekiScript       
	 */
	function DekiExtServe($data = false)
	{
		//error checking: is this call made by a GET or POST?
		//This piece of the code is the same as the IXR_Library code
		if (!$data)
		{
			global $HTTP_RAW_POST_DATA;

			if (!$HTTP_RAW_POST_DATA) 
			{
               die('XML-RPC server accepts POST requests only.');
			}

            $data = $HTTP_RAW_POST_DATA;
		}

		$this->message = new IXR_Message($data);

		if (!$this->message->parse())
	   	{
            $this->error(-32700, 'parse error. not well formed');
		}

		if ($this->message->messageType != 'methodCall')
	   	{
            $this->error(-32600, 'server error. invalid xml-rpc. not conforming to spec. Request must be a methodCall');
		}

		
		//make a callback call to the function with the input parameters
		$result = $this->call($this->message->methodName, $this->message->params);

        // Is the result an error?
		if (is_a($result, 'IXR_Error'))
	   	{
				$this->error($result);
				exit;
        }

		//if the result type is supposed to be in xml, just return without 
		//encoding or wrapping it in XML-RPC standard method response tags
		//(NOTE: this is additional we added so that the IXR_Server could also handle
		//xml return types since xml return types are not part of XML-RPC)
		if($this->returnTypes[$this->callbacks[$this->message->methodName]] == 'xml')
		{
			
        	// Create the XML
       		$xml = $result;
		}
		else //if the code is not of xml return type, return it the standard MethodResponse way.
		{
			// Encode the result
        	$r = new IXR_Value($result);
			$resultxml = $r->getXml();
       		$xml = "<methodResponse><params><param><value>$resultxml</value></param></params></methodResponse>";
		}
        $this->output($xml);
	}
}

class DekiExt
{
	
	//--- private static variables ---
	private static $functionNames = array(); //your function names
	private static $functionArgs = array(); //your function parameter and return information
	
	//the below variables are only needed for GET calls
	
	//title of extension <title>your title here</title>
	private static $title; 
	
	//description of extension <description>your description here</description>
	private static $description;
	
	//copyright info for extension <copyright>your copyright info here</copyright>
	private static $copyright;

	//help page uri for extension <uri.help>your uri help page</uri.help>
	private static $uriLicense;
	
	//help page uri for extension <uri.help>your uri help page</uri.help>
	private static $uriHelp;

	//help page uri for extension <uri.help>your uri help page</uri.help>
	private static $uriLogo;
	
	//namespace for your extension <namespace>your namespace</namespace>
	private static $namespace;

	//namespace for your extension <namespace>your namespace</namespace>
	private static $label;
	
	//uri link that has your php code appended with the [function name].rpc <uri>your uri</uri>
	private static $uri; 

	//--- class constructor ---
	/*
	 * DekiExtensionPhp constructor
	 * @param string $title - title of the extension xml document
	 * @param array $fileInfo - array with description, copyright, uri.help and namespace mapping
	 * @param array $args - array with key set to functionName(param1:type, param2,type, ... , ...): return_type
	 * 
	 * @note return with a call to the IXR_server that will register the user created functions in the .php
	 *        file that calls this object or will return with generated xml content that follows the
	 *        dekiwiki extension format. Either option depends on if the user calls this object through
	 *         a GET or POST HTTP method. 
	 *        
	 */
	public function dekiExtInit($title, $fileInfo, $args)
	{
		
		self::$functionNames = array_keys($args);
		self::$functionArgs = $args;

		if(strcmp($_SERVER['REQUEST_METHOD'], "GET") == 0)
		{
			if($fileInfo != null)//set the class variables
			{
				self::$description = $fileInfo['description'];
				self::$copyright = $fileInfo['copyright'];
				self::$uriHelp = $fileInfo['uri.help'];
				self::$namespace = $fileInfo['namespace'];
				self::$uriLogo = $fileInfo['uri.logo'];
				self::$uriLicense = $fileInfo['uri.license'];
				self::$label = $fileInfo['label'];
			} 

			self::$title = $title;
			// Bugfix: need to support vhosts that do not have a dedicated IP
			self::$uri = 'http://' . $_SERVER['SERVER_NAME'] . $_SERVER['REQUEST_URI'];

			//call the internal method generateForGet to handle the above information
			$this->generateForGet();
		}
		else if(strcmp($_SERVER['REQUEST_METHOD'], "POST") == 0)
		{
			$this->generateForPost();
		}
		else
		{
			$writeError = xmlwriter_open_memory();
			xmlwriter_start_element($writeError, 'error');
			xmlwriter_write_element($writeError, 'status', '404');
			xmlwriter_write_element($writeError, 'title', 'Not Found');
			xmlwriter_write_element($writeError, 'message', 'resource not found');
			xmlwriter_end_element($writeError);	
			header('HTTP/1.0 404 Not Found');
			xmlwriter_end_element($writeError); //close the php xml writer
			header('Content-type: application/xml'); //make sure the returned content is of type xml, not php
			print xmlwriter_output_memory($writeError); //output the xml you just created as a string	
		}
	}
	
	//--- class methods ---
	
	/*
	 * Handle Post call to DekiExtensionPhp
	 * 
	 * @note this method parses and extracts information from the array passed in from the constructor.
	 * 		Using this information, it makes a call to the IXR_server object that is from IXR_library 
	 * 		included at the top.
	 */
	private function generateForPost()
	{
		
		//initialize the array that will eventually store the result
		$userFunctions = array();
		$userReturns = array();
		foreach(self::$functionNames as $entry)
		{
			
			//let handleMethod parse the function information: 
		
			$param;
			$ret_type;
			$entryResult= $this->handleMethod($entry, false, &$param, &$ret_type);
			
			//if the entry does not exist or			
			//check if the function name is allowed in php
			if( (!preg_match('/^[a-zA-z][a-zA-Z0-9]*$/', self::$functionArgs[$entry])) ||
				($entryResult == null) )
			{
				continue;
			}
		
			//match the function information with the .php functions in your file
			$userFunctions[$entryResult] = self::$functionArgs[$entry]; 
			$userReturns[self::$functionArgs[$entry]] = $ret_type;
		}
		
		//call the IXR_server object to register the functions that are in your .php file. 
		new DekiExtServer($userReturns, $userFunctions);
		

	}
	
	/*
	 * Handles Get call to DekiExtensionPhp
	 * 
	 * @note this method parses the given information by the user and turns it into a Dekiscript extension,
	 * 		which is formatted in xml.
	 */
	private function generateForGet()
	{
		//initalize the php xml writer
		$writer = xmlwriter_open_memory();
		
		//set indent so the xml tags are formatted 
		xmlwriter_set_indent($writer, true);
			
		//error checking for missing values that are required in order to create the correct xml
		//extension document
		if( (self::$title == null)|| (count(self::$functionNames) == 0) || (count(self::$functionArgs) == 0))
		{
			xmlwriter_start_element($writer, 'error');
			xmlwriter_write_element($writer, 'status', '404');
			xmlwriter_write_element($writer, 'title', 'Not Found');
			
			if(self::$title == null)
			{
				xmlwriter_write_element($writer, 'message', 'title not found');
			}
			else if(count(self::$functionArgs) == 0)
			{
				xmlwriter_write_element($writer, 'message', 'function array not found');
			}
			else if(count(self::$functionNames) == 0)
			{
				xmlwriter_write_element($writer, 'message', 'function array key not found');
			}
			else
			{
				xmlwriter_write_element($writer, 'message', 'resource not found');
			}
			
			xmlwriter_end_element($writer);		
			header("HTTP/1.1 404 Not Found");
		}
		else
		{
			xmlwriter_start_element($writer, 'extension');
			xmlwriter_write_element($writer, 'title', self::$title);
	
			//checks the optional tags to see if they exist, if not, then don't include them in the extension
			if(self::$description != null)
			{
				xmlwriter_write_element($writer, 'description', self::$description);
			}
			if(self::$copyright != null)
			{
				xmlwriter_write_element($writer, 'copyright', self::$copyright);
			}
			if(self::$uriHelp != null)
			{
				xmlwriter_write_element($writer, 'uri.help', self::$uriHelp);
			}
			if(self::$uriLicense != null)
			{
				xmlwriter_write_element($writer, 'uri.license', self::$uriLicense);
			}
			if(self::$uriLogo != null)
			{
				xmlwriter_write_element($writer, 'uri.logo', self::$uriLogo);
			}
			if(self::$namespace != null)
			{
				xmlwriter_write_element($writer, 'namespace', self::$namespace);
			}	
			if(self::$label != null)
			{
				xmlwriter_write_element($writer, 'label', self::$label);
			}
			//create the tags for each of the function names, their parameters and their return type
			foreach(self::$functionNames as $entry)
			{
				$functionParams = array();
				$returnType = 'any'; //default type
				
				//let handleMethod extract the function name, parameters, and return type
				$result = $this->handleMethod($entry, false, &$functionParams, &$returnType);
				
				if($result != null)
				{
					xmlwriter_start_element($writer, 'function');
					xmlwriter_write_element($writer, 'name', $result); //set the function name					
					xmlwriter_start_element($writer, 'uri');
					xmlwriter_write_attribute($writer, 'protocol', 'xmlrpc');
					xmlwriter_text($writer, self::$uri . '/' . $result. '.rpc');
					xmlwriter_end_element($writer);
					
					//take the returned parameter information [param_name]:[param_type] and parse
					//it so that you  have a tag with the parameter's name and a type attribute
					foreach($functionParams as $paramEntry)
					{
						$paramParts = split(':', trim($paramEntry));
						xmlwriter_start_element($writer, 'param');
						xmlwriter_write_attribute($writer, 'name', trim($paramParts[0]));
						
						//if there is a type set the attribute to the user's given type, if not, set to 'any'
						xmlwriter_write_attribute($writer, 'type', 
							($this->checkType(trim($paramParts[1])) ? $this->checkType(trim($paramParts[1])): 'any'));
						
						xmlwriter_end_element($writer);
					}	
					//set the function return type
					xmlwriter_start_element($writer, 'return');
					
					//if there is a type set the attribute to the user's given type, if not, set to 'any'
					xmlwriter_write_attribute($writer, 'type',
						($this->checkType($returnType)? $this->checkType($returnType): 'any'));
					
					xmlwriter_end_element($writer);				
					xmlwriter_end_element($writer);
				}
			}
		}	
		xmlwriter_end_element($writer); //close the php xml writer
		header('Content-type: application/xml'); //make sure the returned content is of type xml, not php
		print xmlwriter_output_memory($writer); //output the xml you just created as a string	
	}

	/*
	 * Handles the array keys
	 * 
	 * @param string $arg is all of the function information in one string
	 * @param bool $isPost is a flag to indicate if extra calculation is required in this method
	 * @param array $returnParam is an array that will contain all of the parameters listed in $arg
	 * @param string $returnType is the return type of the function, extracted from $arg. This is defaulted
	 * 		to the 'any' type.
	 * @return string is the function name extracted from $arg
	 */
	private function handleMethod($arg, $isPost, $returnParam, $returnType = 'any')
	{
		$results = array();
		
		//using regex, separate out the content on both sides of the parenthesis and inside the parenthesis
		$results = split('[(.*)]', trim($arg));
		
		//the function name that exists on the left side of the left-parenthesis
		$funcName = trim($results[0]);
		
		//if the function that called handleMethod is Get
		if(!$isPost)
		{
			//set the return type information (removes the : before the return type)
			if(trim($results[2]) != '')
			{
				$returnType = trim(preg_replace('[:]', '', $results[2]));
			}
			
			//set the parameter information in this '[param_name]:[param_type]' format
			if(trim($results[1]) != '')
			{
				$returnParam = explode(',', trim($results[1]));	
			}	
		}
		
		//returns the function Name
		return $funcName;				
	}	
	
	/*
	 * checks types
	 * 
	 * @param string $typeVal is the type of the parameter or return value
	 * @return string if $typeVal matches one of the types in the switch statement, returns null if it doesn't
	 */
	private function checkType($typeVal)
	{
		$cleanTypeVal = trim(strtolower($typeVal));
		
		//uses a switch to check if the type set for the parameters and the return exist in DekiScript
		switch($cleanTypeVal)
		{
			case 'nil':
				break;
			case 'bool':
				break;
			case 'num':
				break;
			case 'str':
				break;
			case 'uri':
				break;
			case 'map':
				break;
			case 'list':
				break;
			case 'xml':
				break;
			default:
				return null;
		}
		return $cleanTypeVal;
	}


}

function DekiExt($title, $fileInfo, $funcArray)
{
	$extension = new DekiExt();
	$extension->dekiExtInit($title, $fileInfo, $funcArray);

}

?>
