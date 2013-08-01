<?php
/**
 * General handling for resource properties
 * 
 * @note Only supports mimetype of text/*; charset=utf-8
 */
abstract class DekiProperties
{
	// user specified keys
	const NS_CUSTOM = 'urn:custom.mindtouch.com#';
	// application specified keys
	const NS_DEKI = 'urn:deki.mindtouch.com#';
	// ui specific keys for html & css
	const NS_UI = 'urn:ui.deki.mindtouch.com#';
	// deki meta keys
	const NS_META = 'urn:meta.deki.mindtouch.com#';
	// deki template properties
	const NS_TEMPLATE = 'mindtouch.template#';
	
	/**
	 * main identifier for this property bag
	 * @example /user/$id/properties, /page/$id/properties
	 */
	protected $id = null;
	// @param bool - flag to determine if the remote properties have been loaded
	protected $loaded = false;
	/**
	 * $remoteProperties - store the retrieved remote properties information
	 * it is more verbose than the local properties
	 * 
	 * @type array<DekiProperty> - keys are the full property names
	 */
	private $remoteProperties = array();
	/*
	 * @param array - stores local properties, has same format as remote, less etag
	 * @note if a property is null then it has been marked for deletion
	 */
	private $properties = array();

	/**
	 * @startsection Public CustomProperty Members
	 */
	/**
	 * Checks if a key already exists and won't set if it does
	 */
	public function addCustom($name, $value, $mimeType = null)
	{
		if ($this->has($name, self::NS_CUSTOM))
		{
			return false;
		}
		
		return $this->set($name, $value, self::NS_CUSTOM, $mimeType);
	}
	
	public function removeCustom($name)
	{
		if (!$this->has($name, self::NS_CUSTOM))
		{
			return false;
		}

		return $this->remove($name, self::NS_CUSTOM);
	}
	
	public function hasCustom($name)
	{
		return $this->has($name, self::NS_CUSTOM);
	}
	/*
	 * Custom keys will be prefixed with the custom namespace
	 */
	public function setCustom($name, $value, $mimeType = null)
	{
		return $this->set($name, $value, self::NS_CUSTOM, $mimeType);
	}
	
	public function getCustom($name, $default = null)
	{
		return $this->get($name, $default, self::NS_CUSTOM);
	}
	
	public function getAllCustom()
	{
		return $this->getAllFromNamespace(self::NS_CUSTOM);
	}
	/**
	 * @endsection Public CustomProperty Members
	 */
	
	/*
	 * @return int - main identifier for this property bag
	 */
	protected function getId() { return $this->id; }
	protected function setId($id)
	{
		if ($this->id != $id)
		{
			$this->id = $id;
			$this->loaded = false;
		}
	}
	
	/*
	 * Takes a fully qualified key and returns just the name
	 * @note strips the namespace from a key if it exists
	 * 
	 * @return string - returns the key name or null if not from the specified namespace
	 */
	protected static function getPropertyName($key, $namespace = self::NS_CUSTOM)
	{
		@list($keyNamespace, $keyName) = explode('#', $key, 2);
		$keyNamespace .= '#'; // add the pound back

		if ($keyNamespace == $namespace)
		{
			return $keyName;
		}
		
		return null;
	}
	
	/**
	 * @param string $namespace - DekiProperty namespace
	 * @param string $mimeType - if not specified then current or text/plain will be used
	 */
	protected function set($name, $value, $namespace = self::NS_CUSTOM, $mimeType = null)
	{
		if ($name == '')
		{
			// invalid key name specified
			return false;
		}
		
		// include the namespace for the key
		$name = $namespace . $name;
		
		if (!isset($this->properties[$name]))
		{
			$this->properties[$name] = new DekiProperty($name, $value, $mimeType);
		}
		
		$this->properties[$name]->setContent($value, $mimeType);

		return true;
	}
	
	/**
	 * Determines if a key is already set
	 */
	protected function has($name, $namespace = self::NS_CUSTOM)
	{
		$notSet = 'NOT::'.time(); // generate something "random" to check against
		$val = $this->get($name, $notSet, $namespace);

		return !($val == $notSet);
	}
	
	protected function get($name, $default = null, $namespace = self::NS_CUSTOM)
	{
		// include the namespace for the key
		$key = $namespace . $name;

		if (isset($this->properties[$key]))
		{
			// check if the property is marked for deletion
			return !is_null($this->properties[$key]) ? $this->properties[$key]->getContent() : $default;
		}
		else
		{
			$Property = $this->loadProperty($key);
			return !is_null($Property) ? $Property->getContent() : $default;
		}
	}
	
	// TODO: merge remote information with local information?
	protected function getProperty($name, $namespace = self::NS_CUSTOM)
	{
		// include the namespace for the key
		$key = $namespace . $name;
		
		if (isset($this->properties[$key]))
		{
			// check if the property is marked for deletion
			return !is_null($this->properties[$key]) ? $this->properties[$key] : null;
		}
		else
		{
			return $this->loadProperty($key);
		}		
	}

	/**
	 * @return array - contains all the key names from a namespace, no namespace included
	 */
	protected function getAllFromNamespace($namespace = self::NS_CUSTOM)
	{
		$this->load();
		
		$names = array();
		
		// remote
		foreach ($this->remoteProperties as $key => &$Property)
		{
			$name = self::getPropertyName($key, $namespace);
			if (!is_null($name))
			{
				$names[$name] = $Property->getContent();
			}			
		}

		// local
		foreach ($this->properties as $key => &$Property)
		{
			$name = self::getPropertyName($key, $namespace);
			if (!is_null($name))
			{
				$names[$name] = is_null($Property) ? null : $Property->getContent();
			}
		}
		unset($Property);
		
		return $names;
	}
	
	/**
	 * @param string $name - should be the namespace + key name, used internally
	 * @param enum $namespace - if this is specified then the $name param should be the name
	 * within the specified namespace
	 */
	protected function getEtag($name, $namespace = null)
	{
		$this->load();
		
		// generate the full key name
		$fqName = is_null($namespace) ? $name : $namespace . $name;
		$Property = isset($this->remoteProperties[$fqName]) ? $this->remoteProperties[$fqName] : null;
		
		return !is_null($Property) ? $Property->getEtag() : null;
	}
	
	protected function remove($name, $namespace = self::NS_CUSTOM)
	{
		// include the namespace for the key
		$name = $namespace . $name;
		
		$this->properties[$name] = null;
		return true;
	}
	
	/*
	 * Dangerous function, will blow out the entire property bag
	 */
	protected function removeAll()
	{
		$this->load();
		foreach ($this->remoteProperties as $key => &$Property)
		{
			$this->properties[$key] = null;
		}
		unset($Property);
	}

	/**
	 * Update is used for creating/modifying/deleting properties
	 */
	protected function update()
	{
		// need to load properties before updating to retrieve etags
		$this->load();

		$Plug = $this->getPlug();
		$Result = $Plug->Put($this->toArray(false));

		return $Result;
	}
	
	/**
	 * Implements lazy loading of properties
	 * @param $properties - base property bag (/body/properties)
	 * @return boolean - success
	 */
	protected function load($properties = null)
	{
		if ($this->loaded)
		{
			// already loaded the remote properties
			return true;
		}
		// if there is no id set locally then properties cannot be retrieved remotely
		else if (is_null($this->getId()))
		{
			return false;
		}
		else if (is_null($properties))
		{
			$Plug = $this->getPlug();
			$Result = $Plug->Get();

			if (!$Result->isSuccess())
			{
				return false;
			}
			$properties = $Result->getVal('body/properties', array());
		}

		// load the retrieved properties from api response
		$this->loaded = true;
		$this->remoteProperties = array();
		
		$X = new XArray($properties);
		$itemProperties = $X->getAll('property', array());
		
		foreach ($itemProperties as &$result)
		{
			$Property = DekiProperty::newFromArray($result);
			$this->remoteProperties[$Property->getName()] = $Property;
		}
		unset($itemProperties);
		
		return true;
	}
	
	/**
	 * Makes a direct request to the api to load a single key
	 * First checks if the key's contents where loaded during the load request,
	 * if not then a direct request is made to fetch the property's contents
	 * 
	 * Useful for retreiving non-text/plain properties
	 * @return DekiProperty - null if the property is not found
	 */
	protected function loadProperty($key)
	{
		// make sure the property bag is loaded
		$this->load();

		// TODO: make sure we aren't trying to load a blob property, text only
		if (!isset($this->remoteProperties[$key]))
		{
			return null;
		}
		else if (!$this->remoteProperties[$key]->isLoaded())
		{
			$Plug = $this->getPlug();
			$Result = $Plug->At($key)->Get();
			if (!$Result->isSuccess())
			{
				// TODO: better error handling
				return null;
			}

			// property has been loaded
			$this->remoteProperties[$key]->setContent($Result->getVal('body'));
		}

		return $this->remoteProperties[$key];
	}
	
	/*
	 * @return Plug - plug object to access the properties 
	 */
	abstract protected function getPlug();
	
	/**
	 * Format the properties into an array
	 * 
	 * @param $verbose - determines if all properties should be returned or only modified
	 */
	public function toArray($verbose = true)
	{
		$this->load();
		
		$array = array();
		// keep track of properties that were unmodified
		$unmodified = array();
		
		// loop through the remote then override with local
		foreach ($this->remoteProperties as $name => &$Property)
		{
			$propertyArray = $Property->toArray();

			// check if the remote property should be added
			if (!$verbose)
			{
				// check if there is a local value
				if (!isset($this->properties[$name]))
				{
					continue;
				}
				
				$localValue = is_null($this->properties[$name]) ? null : $this->properties[$name]->getContent();
				$remoteValue = is_null($this->remoteProperties[$name]) ? null : $this->remoteProperties[$name]->getContent();
				
				// check if this key has been modified
				if ($localValue == $remoteValue)
				{
					$unmodified[$name] = true;
					continue;
				}
			}

			if ($propertyArray)
			{
				$array[$name] = $propertyArray;
			}
		}

		// set local properties
		foreach ($this->properties as $name => &$Property)
		{
			if (is_null($Property))
			{
				// property marked for deletion
				$array[$name] = array('@name' => $name);
			}
			else
			{
				$propertyArray = $Property->toArray();

				// only add the property if it has been modified when $verbose == true
				if ($propertyArray && !isset($unmodified[$name]))
				{
					if (isset($array[$name]))
					{
						// merge the local information with the remote
						$array[$name] = array_merge($array[$name], $propertyArray);
					}
					else
					{
						$array[$name] = $propertyArray;
					}
				}
			}
		}
		unset($Property);
		// clean up
		unset($unmodified);

		// create the array representation
		$properties = array();
		foreach ($array as &$node)
		{
			$properties['property'][] = $node;
		}
		unset($node);
		
		return array('properties' => $properties);
	}
	
	public function toXml()
	{
		return encode_xml($this->toArray());
	}
}

/*
 * Class handles the property values and information
 */
class DekiProperty
{
	// if no content type is specified, this is the default
	const MIME_TYPE_DEFAULT = 'text/plain; charset=utf-8';
	// other useful mime types
	const MIME_TYPE_TEXT = 'text/plain; charset=utf-8';
	const MIME_TYPE_HTML = 'text/html; charset=utf-8';
	const MIME_TYPE_CSS = 'text/css; charset=utf-8';
	const MIME_TYPE_JAVASCRIPT = 'text/javascript; charset=utf-8';
	const MIME_TYPE_XML = 'application/xml; charset=utf-8';

	/**
	 * @type array - stores an array of mime types that are updatable with this class
	 */	
	protected static $MIME_TYPES = array(
		DekiProperty::MIME_TYPE_TEXT,
		DekiProperty::MIME_TYPE_HTML,
		DekiProperty::MIME_TYPE_CSS,
		DekiProperty::MIME_TYPE_JAVASCRIPT
	);
	
	protected $name = null;
	protected $contentType = null;
	protected $content = null;
	protected $contentHref = null;
	/**
	 * Property contents are not auto loaded for non-text types
	 * @type bool
	 */
	protected $contentLoaded = false;

	// @type int - id for the user that last modified
	protected $userId = null;
	// @type DekiUser
	protected $User = null;
	protected $dateModified = null;
	protected $etag = null;


	public static function newFromArray(&$result)
	{
		$Property = new self($result['@name']);

		// previous property versions do not have etags, only the head rev has an etag
		$Property->etag = isset($result['@etag']) ? $result['@etag'] : null;
		$Property->setContentType($result['contents']['@type']);

		// text types with size < 2048 get autoloaded
		if (isset($result['contents']['#text']))
		{
			$Property->setContent($result['contents']['#text']);
		}
		
		if (isset($result['contents']['@href']))
		{
			$Property->contentHref = $result['contents']['@href'];
		}
		
		$Property->dateModified = $result['date.modified'];
		$Property->userId = $result['user.modified']['@id'];

		return $Property;
	}
	
	
	public function __construct($name, $content = null, $mimeType = self::MIME_TYPE_DEFAULT)
	{
		$this->name = $name;
		if (!is_null($content))
		{
			$this->setContent($content);
		}

		$this->setContentType($mimeType);
	}


	public function isLoaded() { return $this->contentLoaded; }
	public function getEtag() { return $this->etag; }
	public function getName() { return $this->name; }
	public function getDateModified() { return $this->dateModified; }
	public function getUser()
	{
		if (is_null($this->User))
		{
			$this->User = DekiUser::newFromId($this->userId);
		}

		return $this->User;
	}
	
	public function &getContent()
	{
		return $this->content;
	}
	
	public function getContentType()
	{
		return $this->contentType;
	}
	
	public function getContentHref()
	{
		return $this->contentHref;
	}
	
	public function setContent($content, $mimeType = null)
	{
		$this->content = $content;
		$this->contentLoaded = true;

		if (!is_null($mimeType))
		{
			$this->setContentType($mimeType);
		}
	}

	public function setContentType($mimeType = null)
	{
		$this->contentType = !empty($mimeType) ? $mimeType : self::MIME_TYPE_DEFAULT;
	}
	
	/**
	 * @return mixed - returns null if the property cannot be updated => unsupported mime-type
	 */
	public function toArray()
	{
		$array = array(
			'@name' => $this->name
		);
		
		$etag = $this->getEtag();
		if (!is_null($etag))
		{
			$array['@etag'] = $etag;
		}
		
		if (!is_null($this->content))
		{
			// special case for xml content types
			if (is_array($this->content) && ($this->contentType == self::MIME_TYPE_XML))
			{
				// need to grab the root node if it's an xml encoded array
				$key = key($this->content);
				$xml = current($this->content);

				$array['contents'] = array(
					'@type' => $this->contentType,
					$key => $xml
				);
			}
			else {
				// updating/setting property
				$array['contents'] = array(
					'@type' => $this->contentType,
					'#text' => $this->content
				);
			}
		}
		
		return $array;
	}
}
