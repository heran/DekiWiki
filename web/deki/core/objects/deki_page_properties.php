<?php
/**
 * Handles setting the page properties
 */
class DekiPageProperties extends DekiProperties
{
	/*
	 * Page language is not an actual property
	 */
	private $language = null;
	private $remoteLanguage = null;
	
	/**
	 * Create a new DekiPageProperties from raw api results
	 * @param array $result
	 * @return DekiPageProperties
	 */
	public static function newFromArray(&$result)
	{
		$X = new XArray($result);
		$pageId = $X->getVal('@id', 0);
		$Properties = new DekiPageProperties($pageId);
		
		self::populateObject($Properties, $result);
		
		return $Properties;
	}

	/**
	 * Load Properties for DekiPageProperties object
	 * @param DekiPageProperties $Properties
	 * @param mixed $result - can be array or XArray
	 */
	public static function populateObject(&$Properties, &$result)
	{
		$X = is_object($result) ? $result : new XArray($result);
		
		$count = $X->getVal('@count', -1);
		// make sure we don't load the properties if we don't have to
		if ($count > 0)
		{
			$Properties->load($X->getVal('properties'));
		}
	}
	
	/*
	 * Takes a fully qualified key and returns just the name
	 * @note strips the namespace from a key if it exists
	 * 
	 * @return string - returns the key name or null if not a custom key
	 */
	public static function getPropertyName($key)
	{
		return DekiProperties::getPropertyName($key, self::NS_CUSTOM);
	}
	
	public function __construct($pageId = null)
	{
		$this->setPageId($pageId);
	}
	
	public function setPageId($pageId)
	{
		$this->setId($pageId);
	}
	
	public function getPageId() { return $this->getId(); }
	
	
	/*
	 * Emulates the page language as a property
	 */
	public function getLanguage($validate = true)
	{
		$this->load();
		
		$language = !is_null($this->language) ? $this->language : $this->remoteLanguage;
		if ($validate) 
		{
			if (!array_key_exists($language, wfAllowedLanguages())) 
			{
				// return site default?
				return null;
			}	
		}
		
		return $language;
	}
	public function setLanguage($language) { $this->language = $language; }

		
	public function update()
	{
		// update the page properties
		return parent::update();
	}
	
	/*
	 * Need to override for setting the page language
	 */
	public function toArray($verbose = true)
	{
		$array = parent::toArray($verbose);
		
		// only add the language property if it changed
		if ($verbose || $this->language != $this->remoteLanguage)
		{
			$array['properties']['language'] = $this->getLanguage(false);
		}
		
		return $array;
	}

	
	protected function getPlug()
	{
		return DekiPlug::getInstance()->At('pages', $this->getId(), 'properties');
	}
	
	/**
	 * Set object with properties using *root* property object (body/properties)
	 * @note Need to override for getting the page language
	 * @note Remove override after page language is loaded/stored differently?
	 * @param $properties - properties to load; if null, calls api 
	 */
	protected function load($properties = null)
	{
		$language = null;

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
		
		// set remoteProperties		
		$return = parent::load($properties);
		if ($return)
		{
			// set the page language; note this is not a regular property
			$X = new XArray($properties);
			$this->remoteLanguage = $X->getVal('language/#text');
		}
		
		return $return;
	}
}
