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
 * Base class for extensions & auth services
 */
abstract class DekiService implements IDekiApiObject
{
	const TYPE_AUTH = 'auth';
	const TYPE_EXTENSION = 'ext';

	const STATUS_ENABLED = 'enabled';
	const STATUS_DISABLED = 'disabled';

	const INIT_NATIVE = 'native';
	const INIT_REMOTE = 'remote';


	/*
	 * Caches the loaded services for quick loading
	 */
	static $cache = array();

	private $id = null;
	// @type enum - auth, ext
	private $type = null;
	private $sid = null;
	// @type string
	protected $uri = null;
	private $description = null;
	// @type string
	private $status = null;
	// @type enum - native, remote
	private $init = null;
	// @type string
	private $error = null;


	/*
	 * Store the user definable configs & prefs
	 */
	private $configuration = array();
	private $preferences = array();

	/*
	 * Factory method
	 */
	static function getSiteList($typeFilter = null, $isRunning = null)
	{
		$Plug = DekiPlug::getInstance()->At('site', 'services');
		if (!is_null($typeFilter))
		{
			$Plug = $Plug->With('type', $typeFilter);

			// use the apikey with auth services to always retrieve the list
			if ($typeFilter == DekiService::TYPE_AUTH)
			{
				$Plug = $Plug->WithApiKey();
			}
		}
		$Result = $Plug->Get();

		if (!$Result->isSuccess())
		{
			throw new Exception('Could not load site services'. (!is_null($typeFilter) ? ", type '". $typeFilter ."'" : ''));
		}

		$siteServices = $Result->getAll('body/services/service');

		$services = array();
		foreach ($siteServices as $siteService)
		{
			$Service = self::newFromArray($siteService);

			if (is_null($isRunning) || $isRunning == $Service->isRunning())
			{
				$services[$Service->getId()] = $Service;
			}
		}

		return $services;
	}

	static function newFromId($id)
	{
		$Service = self::load($id);
		return $Service;
	}

	static function newFromText($text)
	{
		throw new Exception('Services cannot be retrieved by name');
	}

	/*
	 * Factory method
	 */
	static function newFromArray(&$result)
	{
		$X = new XArray($result);
		
		if ($X->getVal('type') == self::TYPE_AUTH)
		{
			$Service = new DekiAuthService(
				$X->getVal('init'),
				$X->getVal('sid'),
				$X->getVal('uri'),
				$X->getVal('description'),
				$X->getVal('config'),
				$X->getVal('preferences'),
				$X->getVal('status')
			);
		}
		else
		{
			$Service = new DekiExtension(
				$X->getVal('init'),
				$X->getVal('sid'),
				$X->getVal('uri'),
				$X->getVal('description'),
				$X->getVal('config'),
				$X->getVal('preferences'),
				$X->getVal('status')
			);
		}
		
		$Service->setId($X->getVal('@id'));
		$Service->setError($X->getVal('lasterror'));

		return $Service;
	}

	protected static function load($id)
	{
		if (!isset(self::$cache[$id]))
		{
			$Plug = DekiPlug::getInstance()->At('site', 'services', $id);
			if ($id == 1)
			{
				// need to be able to always retrieve Deki's service
				$Plug = $Plug->WithApiKey();
			}

			$Result = $Plug->Get();
			if (!$Result->isSuccess())
			{
				return null;
			}

			$result = $Result->getVal('body/service');
			
			$Service = self::newFromArray($result);
			self::$cache[$Service->getId()] = $Service;
		}

		return self::$cache[$id];
	}
	

	/*
	 * @param enum $init - native, remote (INIT_NATIVE, INIT_REMOTE)
	 * @param enum $type - auth, ext (TYPE_AUTH, TYPE_EXTENSION)
	 * @param string $sid - required for native services 
	 * @param string $uri - required for remote services
	 * @param string description - info about the service
	 * @param array $config - php xml array configuration settings
	 * @param array $prefs - php xml array preferences
	 * @param string $status - enabled,disabled (STATUS_ENABLED, STATUS_DISABLED)
	 */
	public function __construct($init, $type, $sid, $uri, $description = null, $xmlConfig = array(), $xmlPrefs = array(), $status = DekiService::STATUS_ENABLED)
	{
		$this->type = $type == self::TYPE_AUTH ? self::TYPE_AUTH : self::TYPE_EXTENSION;
		
		$this->setInitType($init, $sid, $uri);
		$this->setDescription($description);
		$this->setStatus($status);

		$this->setConfiguration($xmlConfig);
		$this->setPreferences($xmlPrefs);
	}


	public function getId() { return $this->id; }
	public function getSid() { return $this->sid; }
	public function getUri() { return $this->uri; }

	public function getName() { return $this->description; }
	public function getDescription() { return $this->description; }
	public function getType() { return $this->type; }
	public function getInitType() { return $this->init; }
	public function getError() { return $this->error; }


	/**
	 * @return bool - true if the service is native (written in c#, loaded locally)
	 */
	public function isNative() { return $this->init == 'native'; }
	public function isEnabled() { return $this->status == self::STATUS_ENABLED; }
	public function isRunning()
	{
		if (is_null($this->id))
		{
			return false;
		}

		return !is_null($this->uri) && $this->isEnabled();
	}
	public function isExtension() { return $this->getType() == self::TYPE_EXTENSION; }
	public function hasError() { return !is_null($this->error); }


	public function setDescription($description) { $this->description = $description; }
	public function setInitType($init, $sid, $uri)
	{
		$this->uri = empty($uri) ? null : $uri;

		if ($init == self::INIT_REMOTE)
		{
			$this->init = self::INIT_REMOTE;
			$this->sid = null;
			
		}
		else
		{
			$this->init = self::INIT_NATIVE;
			$this->sid = $sid;
		}
	}
	public function setStatus($status) { $this->status = $status == self::STATUS_DISABLED ? self::STATUS_DISABLED : self::STATUS_ENABLED; }

	protected function setId($id) { $this->id = $id; }
	protected function setError($error) { $this->error = !empty($error) ? $error : null; }


	public function getConfiguration() { return $this->configuration; }
	public function clearConfiguration() { $this->setConfiguration(array()); }
	protected function setConfiguration($config)
	{
		$this->configuration = array();

		if (!empty($config))
		{
			$Result = new DekiResult($config);
			$config = $Result->getAll('value', array());

			foreach ($config as $value)
			{
				$this->setConfig($value['@key'], $value['#text']);
			}
		}
	}

	
	public function getPreferences() { return $this->preferences; }
	public function clearPreferences() { $this->setPreferences(array()); }
	protected function setPreferences($prefs)
	{
		$this->preferences = array();
		
		if (!empty($prefs))
		{
			$Result = new DekiResult($prefs);
			$prefs = $Result->getAll('value', array());

			foreach ($prefs as $value)
			{
				$this->setPref($value['@key'], $value['#text']);
			}
		}
	}

	/*
	 * If $value is null then the key is unset
	 */
	public function setConfig($key, $value)
	{
		if (is_null($value))
		{
			unset($this->configuration[$key]);
			return;
		}
		$this->configuration[$key] = $value;
	}

	/*
	 * If $value is null then the key is unset
	 */
	public function setPref($key, $value)
	{
		if (is_null($value))
		{
			unset($this->preferences[$key]);
			return;
		}
		$this->preferences[$key] = $value;
	}

	public function getConfig($key, $default = null)
	{
		return isset($this->configuration[$key]) ? $this->configuration[$key] : $default;
	}
	public function getPref($key, $default = null)
	{
		return isset($this->preferences[$key]) ? $this->preferences[$key] : $default;
	}


	/**
	 * Creates a new service
	 */
	public function create()
	{
		$Plug = DekiPlug::getInstance()->At('site', 'services');

		$Result = $Plug->Post($this->toArray());
		if ($Result->isSuccess())
		{
			$this->setId($Result->getVal('body/service/@id'));
			if (!$this->isNative())
			{
				$this->uri = $Result->getVal('body/service/uri');
			}
		}

		return $Result;
	}

	/**
	 * Updates an existing service
	 */
	public function update()
	{
		$Plug = DekiPlug::getInstance()->At('site', 'services', $this->getId());
		return $Plug->Put($this->toArray());
	}

	/**
	 * Starts/restart the service
	 * @return DekiResult
	 */
	public function restart() { return $this->start(); }
	public function start()
	{
		$Plug = DekiPlug::getInstance()->At('site', 'services', $this->getId(), 'start');

		return $Plug->Post();
	}

	/**
	 * Stops the service
	 * @return DekiResult
	 */
	public function stop()
	{
		$Plug = DekiPlug::getInstance()->At('site', 'services', $this->getId(), 'stop');

		return $Plug->Post();
	}

	/**
	 * Deletes the service
	 */
	public function delete()
	{
		$Plug = DekiPlug::getInstance()->At('site', 'services', $this->getId());

		return $Plug->Delete();
	}
	

	public function toArray()
	{
		$service = array(
			'type' => $this->type,
			'description' => $this->description,
			'init' => $this->init,
			'status' => $this->status,
			'config' => $this->arrayToValues($this->getConfiguration()),
			'preferences' => $this->arrayToValues($this->getPreferences())
		);
		
		if (!is_null($this->sid))
		{
			$service['sid'] = $this->sid;
		}

		if (!is_null($this->uri) && !$this->isNative())
		{
			$service['uri'] = $this->uri;
		}

		if (!is_null($this->id))
		{
			$service['@id'] = $this->id;
		}
		
		$service = array('service' => $service);
		return $service;
	}
	
	public function toXml()
	{
		return encode_xml($this->toArray());
	}

	public function toHtml()
	{
		return htmlspecialchars($this->getName());
	}

	/**
	 * Converts an array to an XML formatted array of value nodes
	 * Used for converting the prefs & config arrays
	 */
	private function arrayToValues($array)
	{
		$values = array();
		foreach ($array as $key => $value)
		{
			$values[] = array(
				'@key' => $key,
				'#text' => $value
			);
		}
		if (!empty($values))
		{
			$values = array('value' => $values);
		}

		return $values;
	}
}
