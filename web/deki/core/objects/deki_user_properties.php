<?php
/**
 * Handles setting the user properties
 */
class DekiUserProperties extends DekiProperties
{
	/**
	 * Create a new user property object from api result
	 * 
	 * @param array $result
	 * @return DekiUserProperties
	 */
	public static function newFromArray(&$result)
	{
		$X = new XArray($result);
		// (guerrics) hacky hacky hacky hacky, suggestions?
		$href = $X->getVal('@href');
		$userId = basename(dirname($href));
		
		$Properties = new DekiUserProperties($userId);
		$count = $X->getVal('@count', -1);
		// make sure we don't load the properties if we don't have to
		if ($count > 0)
		{	
			$Properties->load($result);
		}
		
		return $Properties;
	}
	
	public function __construct($userId = null)
	{
		$this->setUserId($userId);
	}
	
	public function setUserId($userId)
	{
		$this->setId($userId);
	}
	
	protected function getPlug()
	{
		return DekiPlug::getInstance()->At('users', $this->getId(), 'properties');
	}

	/*
	 * @deprecated do not access options directly, use wrapper methods
	 */
	public function getOption($key, $default = null)
	{
		return $default;
	}
	

	public function getHighlightOption() 
	{
		return $this->get('searchhighlight', null, self::NS_UI) == 'true' ? true: false;
	}
	
	public function setHighlightOption($option) 
	{
		$this->set('searchhighlight', $option == true ? 'true': 'false', self::NS_UI);
	}
	
	public function update()
	{
		return parent::update();
	}
}
