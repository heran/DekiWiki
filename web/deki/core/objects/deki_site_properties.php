<?php
/**
 * Handles setting the user properties
 */
class DekiSiteProperties extends DekiProperties
{
	// these are partial property names, they are generated with the active skin/template
	const PROP_CSS = 'css';
	const PROP_HTML = 'html';
	const NS_PACKAGES = 'mindtouch.packageupdater.imported#';
	
	// full property names
	const PROP_FCK_CONFIG = 'fck.config';

	private static $instance = null;
	
	/**
	 * Used for storing and setting custom html regions
	 * @note array(
	 * 		'skin.theme1' => array(
	 * 			5 => '<html />'
	 * 			7 => '<a />'
	 * 		),
	 * 		'skin.theme2' => array(
	 * 			1 => '<html />'
	 * 		)
	 * )
	 */
	protected $localHtmlRegions = array();

	/**
	 * @return DekiSiteProperties
	 */
	public static function getInstance()
	{
		if (is_null(self::$instance))
		{
			self::$instance = new DekiSiteProperties();
		}
		
		return self::$instance;
	}
	
	/**
	 * Singleton class, use DekiSiteProperties::getInstance();
	 * 
	 * @note guerrics: do not change visibility, this is a singleton
	 */
	protected function __construct()
	{
		// need to set to a value because load does a check against this
		$this->setId(-1);
	}

	
	/**
	 * This returns a list of all packages that have been imported into MindTouch
	 */
	public function getAllPackages()
	{
		$properties = array();
		// @TODO: consider updating this core method to retrieve all properties instead of just contents
		$packages = $this->getAllFromNamespace(self::NS_PACKAGES);
		if (!empty($packages)) 
		{
			foreach ($packages as $package => $value) 
			{
				$properties[$package] = $this->getProperty($package, DekiSiteProperties::NS_PACKAGES); 	
			}
		}
		return $properties; 
	}
	
	/**
	 * These will set the css & html based on the active skin/theme
	 * if the $skin & $theme parameters are omitted
	 */
	public function setCustomCss($css = null, $skin = null, $theme = null)
	{
		$skin = $this->getSkinKey($skin, $theme);
		$name = self::PROP_CSS .'.'. $skin;
		
		if (empty($css))
		{
			$this->remove($name, self::NS_UI);
		}
		else
		{
			$this->set($name, $css, self::NS_UI, DekiProperty::MIME_TYPE_CSS);
		}
	}
	
	public function getCustomCss($skin = null, $theme = null, $default = null)
	{
		$skin = $this->getSkinKey($skin, $theme);
		$name = self::PROP_CSS .'.'. $skin;
		
		return $this->get($name, $default, self::NS_UI);
	}
	
	/**
	 * Helper method for caching the css
	 * @return string - etag for the custom css section
	 */
	public function getCustomCssEtag($skin = null, $theme = null)
	{
		$skin = $this->getSkinKey($skin, $theme);
		$name = self::PROP_CSS .'.'. $skin;
		
		return $this->getEtag($name, self::NS_UI);
	}
	
	/**
	 * Helper method for caching the css
	 * @return string - uri to obtain the custom css content
	 */
	public function getCustomCssUri($skin = null, $theme = null)
	{
		$skin = $this->getSkinKey($skin, $theme);
		$name = self::PROP_CSS .'.'. $skin;
		
		// add the namespace for the api
		$key = self::NS_UI . $name;
		
		return $this->getPlug()->At($key)->GetUri();
	}

	
	public function setCustomHtml($html = null, $region = 1, $skin = null, $theme = null)
	{
		$skin = $this->getSkinKey($skin, $theme);
				
		if (empty($html))
		{
			// remove the region
			$this->localHtmlRegions[$skin][$region] = null;
		}
		else
		{
			if (!isset($this->localHtmlRegions[$skin]))
			{
				$this->localHtmlRegions[$skin] = array();
			}
			
			$this->localHtmlRegions[$skin][$region] = $html;
		}
	}
	
	/**
	 * @param int $region - specifies the html region to use, 1 based
	 */
	public function getCustomHtml($region = 1, $skin = null, $theme = null, $default = null)
	{
		$skin = $this->getSkinKey($skin, $theme);
		
		if (!isset($this->localHtmlRegions[$skin]))
		{
			// load & parse the html regions for the specified skin
			// regions are stored in an xml blob
			$htmlRegions = $this->get(self::PROP_HTML .'.'. $skin, null, self::NS_UI);

			if (!is_null($htmlRegions))
			{
				$X = new XArray($htmlRegions);
				
				$this->localHtmlRegions[$skin] = array();
				foreach ($X->getAll('regions/region', array()) as $htmlRegion)
				{
					$this->localHtmlRegions[$skin][$htmlRegion['@id']] = $htmlRegion['#text'];
				}
			}
		}

		return isset($this->localHtmlRegions[$skin][$region]) ? $this->localHtmlRegions[$skin][$region] : $default;
	}	
	
	public function setFckConfig($js = null)
	{
		if (empty($js))
		{
			$this->remove(self::PROP_FCK_CONFIG, self::NS_UI);
		}
		else
		{
			$this->set(self::PROP_FCK_CONFIG, $js, self::NS_UI, DekiProperty::MIME_TYPE_JAVASCRIPT);
		}
	}
	public function getFckConfig($default = null) { return $this->get(self::PROP_FCK_CONFIG, $default, self::NS_UI); }
	
	/**
	 * Helper method for caching the css
	 * @return string - etag for the custom css section
	 */
	public function getFckConfigEtag() { return $this->getEtag(self::PROP_FCK_CONFIG, self::NS_UI); }
	
	/**
	 * Helper method for caching the css
	 * @return string - uri to obtain the custom css content
	 */
	public function getFckConfigUri() { return $this->getPlug()->At(self::NS_UI . self::PROP_FCK_CONFIG)->GetUri(); }
	
	
	/**
	 * General accessing methods for site properties, use carefully
	 */
	public function removeAll() { return parent::removeAll(); }
	
	public function update()
	{
		// push down the custom html changes
		foreach ($this->localHtmlRegions as $skin => $regions)
		{
			$xml = array();
			
			if (!is_null($regions))
			{
				foreach ($regions as $id => &$html)
				{
					if (!empty($html))
					{
						$xml[] = array(
							'@id' => $id,
							'#text' => $html
						);
					}
				}
				unset($html);
			}
			
			// format into an xml document
			$xml = array(
				'regions' => array(
					'region' => $xml
				)
			);
			
			// set the html regions for this skin
			$this->set(self::PROP_HTML . '.' . $skin, encode_xml($xml), self::NS_UI, DekiProperty::MIME_TYPE_XML);
		}

		return parent::update();
	}
	
	/*
	 * Do not check against user information here since the CSS aggregator
	 * will clear out the authtoken. We set HttpOnly cookies.
	 */
	protected function getPlug()
	{
		return DekiPlug::getInstance()->At('site', 'properties')->WithApiKey();
	}
	
	/**
	 * Helper method to generate the partial name for custom css & html properties
	 * 
	 * @param string $skin - skin template to use (default: active template)
	 * @param string $theme - skin theme to use (default: active skin)
	 */	
	protected function getSkinkey($skin = null, $theme = null)
	{
		if (is_null($skin) || is_null($theme))
		{
			// set to active skin & theme
			global $wgActiveTemplate, $wgActiveSkin;
			$skin = $wgActiveTemplate;
			$theme = $wgActiveSkin;
		}
		
		return $skin .'.'. $theme;
	}
}
