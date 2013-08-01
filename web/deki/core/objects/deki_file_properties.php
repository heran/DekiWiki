<?php
/**
 * Handles setting the page properties
 */
class DekiFileProperties extends DekiProperties
{
	private $description = null;
	
	public static function newFromArray($fileId, &$result)
	{
		$Properties = new self($fileId);
		$Properties->load($result);
		
		return $Properties;
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
	
	public function __construct($fileId = null)
	{
		$this->setFileId($fileId);
	}
	
	public function setFileId($fileId)
	{
		$this->setId($fileId);
	}
	public function getFileId() { return $this->getId(); }
	
	/*
	 * Emulates the page language as a property
	 */
	public function getDescription($return = '')
	{
		if (is_null($this->description)) 
		{
			$this->description = $this->get('description', $return, self::NS_DEKI);
		}
		return $this->description;
	}
	
	public function setDescription($description) 
	{ 
		$this->set('description', $description, self::NS_DEKI);
	}

	/**
	 * Exposes the DekiProperty objects from the meta property namspace
	 * 
	 * @return DekiProperty - null if the property is not found
	 */
	public function getMetaProperty($key)
	{
		return $this->getProperty($key, self::NS_META);
	}


	protected function getPlug()
	{
		return DekiPlug::getInstance()->At('files', $this->getId(), 'properties');
	}
	
	public function update()
	{
		return parent::update();
	}
	
	/**
	 * Need to override for getting the page language
	 * Remove override after page language is loaded/stored differently?
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
			$properties = $Result->getAll('body/properties', array());

			$language = $Result->getVal('body/properties/language/#text');
		}

		// set remoteProperties
		return parent::load($properties);
	}
}
