<?php
/**
 * Handles setting the template properties
 */
class DekiTemplateProperties extends DekiPageProperties
{	
	const PROP_TYPE = 'type';
	const PROP_DESCRIPTION = 'description';
	const PROP_SCREENSHOT = 'screenshot';
	
	// @note kalida: default is a placehold name for forms and comparisons. not an actual value.
	const TYPE_DEFAULT = 'default';
	// real template types
	const TYPE_PAGE = 'page';
	const TYPE_CONTENT = 'content';
	
	public static $VALID_TYPES = array(
		self::TYPE_DEFAULT,
		self::TYPE_PAGE,
		self::TYPE_CONTENT
	);
	
	
	/**
	 * Find all template pages of a given type (uses property search)
	 * @note kalida: currently, new pages without types will not be returned
	 * 
	 * @param array &$templates - reference to an array of verbose page xml arrays
	 * @param string $type - type of page to filter on
	 * @return DekiResult $Result
	 */
	public static function getTemplateXml(&$templates, $type, $language = null)
	{
		$templates = array();
	
		// find templates marked with the following type
		$query = self::NS_TEMPLATE . self::PROP_TYPE . ':"' . $type . '"';
		
		$constraint = array(
			// only search the template namespace
			'+namespace:template',
			// include all neutral pages by default
			'language:neutral'
		);
		
		if (!is_null($language))
		{
			// if en-us, allow both en and en-us
			$constraint[] = 'language:' . $language;
		
			if (strpos($language, '-') > 0)
			{
				$data = explode('-', $language);
				$lang = $data[0];
				$constraint[] = 'language:' . $lang;
			}
		}
		
		$Plug = DekiPlug::getInstance()
			->At('site', 'search')
			->With('q', $query)
			->With('constraint', implode(' ', $constraint))
			->With('parser', 'lucene')
			->With('nocache'); // bugfix #8676: Search query for page template dialog should use 'nocache' arg
		$Result = $Plug->Get();
		
		$templates = $Result->getAll('body/search/page', array());
		if (!$Result->isSuccess() || empty($templates))
		{
			return $Result;
		}
		
		// sort templates by title
		$pageXml = array();
		$pageTitles = array();
		foreach ($templates as &$page)
		{
			$title = trim($page['title']);
			
			$pageXml[] = $page;
			$pageTitles[] = strtolower($title);
		}
		unset($page);
		
		// usort copies the large page objects; sort by the smaller pageTitles[]
		array_multisort($pageTitles, SORT_ASC, $pageXml);
		$templates = $pageXml;
		
		return $Result;
	}
	
	/**
	 * Create new DekiTemplateProperties from page api results
	 * @param array $result
	 * @return DekiTemplateProperties
	 */
	public static function newFromArray(&$result)
	{
		$X = new XArray($result);
		$TemplateProperties = new self($X->getVal('@id'));
		
		parent::populateObject($TemplateProperties, $result);
		
		return $TemplateProperties;
	}
	
	/**
	 * @param int $pageId - id of template page to use
	 * @return N/A
	 */
	public function __construct($pageId)
	{
		parent::__construct($pageId);
	}

	/*
	 * Takes a fully qualified key and returns just the name
	 * @note strips the namespace from a key if it exists
	 *
	 * @return string - returns the key name or null if not a custom key
	 */
	public static function getPropertyName($key)
	{
		return DekiProperties::getPropertyName($key, self::NS_TEMPLATE);
	}

	/**
	 * Get type of template
	 * @return string
	 */
	public function getType()
	{
		return strtolower(trim($this->get(self::PROP_TYPE, self::TYPE_DEFAULT, self::NS_TEMPLATE)));
	}

	/**
	 * Get text description for template
	 * @param string $default - default description, null by default
	 * @return string
	 */
	public function getDescription($default = null)
	{
		return $this->get(self::PROP_DESCRIPTION, $default, self::NS_TEMPLATE);
	}

	/**
	 * Get url to screenshot for template
	 * @param string $default
	 * @return string
	 */
	public function getScreenshotHref($default = null)
	{
		$ScreenshotProperty = $this->loadProperty(self::NS_TEMPLATE . self::PROP_SCREENSHOT);

		if (is_null($ScreenshotProperty))
		{
			return $default;
		}

		// if screenshot property present but empty, return default
		$href = $ScreenshotProperty->getContentHref();
		return is_null($href) || empty($href) ? $default : $href;
	}
	
	/**
	 * Set type of template
	 * @param string $type - must be one of DekiTemplateProperties::$VALID_TYPES
	 * @return N/A
	 */
	public function setType($type)
	{
		if (!in_array($type, self::$VALID_TYPES))
		{
			throw new Exception("Attempted to set template to an invalid type.");
		}
		
		if ($type == self::TYPE_DEFAULT)
		{
			$this->remove(self::PROP_TYPE, self::NS_TEMPLATE);
		}
		else
		{
			$this->set(self::PROP_TYPE, $type, self::NS_TEMPLATE);
		}
	}

	/**
	 * Set string description for template
	 * @param string $description
	 * @return unknown_type
	 */
	public function setDescription($description)
	{
		$this->set(self::PROP_DESCRIPTION, $description, self::NS_TEMPLATE);
	}
}
