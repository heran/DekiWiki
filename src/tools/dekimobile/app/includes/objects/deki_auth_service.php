<?php
/*
 * MindTouch Deki - enterprise collaboration and integration platform
 * Copyright (C) 2006 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
 *  please review the licensing section.
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


class DekiAuthService extends DekiService implements IDekiApiObject
{
	// database key for the local authentication provider
	const INTERNAL_AUTH_ID = 1;
	// @type bool - determines if the provider should be set as the default
	private $setDefaultProvider = false;

	static function getDefaultProviderId()
	{
		global $wgDefaultAuthServiceId;
		return isset($wgDefaultAuthServiceId) ? $wgDefaultAuthServiceId : null;
	}
	
	/*
	 * Fetches an instance of the internal authentication provider
	 */
	static function getInternal()
	{
		return DekiService::load(self::INTERNAL_AUTH_ID);
	}

	static function getSiteList($isRunning = true)
	{
		return DekiService::getSiteList(self::TYPE_AUTH, $isRunning);
	}

	static function &newFromArray(&$result)
	{
		if ($result['type'] != self::TYPE_AUTH)
		{
			throw new Exception('Cannot create an authentication service from an extension');
		}
		$Service = parent::newFromArray($result);
		
		return $Service;
	}

	public function __construct($init, $sid, $uri, $description = null, $config = array(), $prefs = array(), $status = DekiService::STATUS_ENABLED)
	{
		parent::__construct($init, DekiService::TYPE_AUTH, $sid, $uri, $description, $config, $prefs, $status);
	}

	/**
	 * @return bool - true if the auth service is deki's internal provider
	 */
	public function isInternal()
	{
		return ($this->getId() == self::INTERNAL_AUTH_ID);
	}

	/**
	 * @return bool - true if the auth service is the current default provider
	 */
	public function isDefaultProvider()
	{
		global $wgDefaultAuthServiceId;
		return (bool)($this->getId() == $wgDefaultAuthServiceId);
	}

	/**
	 * Sets the internal variable
	 */
	public function setAsDefaultProvider($default)
	{
		$this->setDefaultProvider = $default ? true : false;
	}

	/**
	 * Overload the following methods to allow setting the default provider
	 */
	public function create()
	{
		$Result = parent::create();
		if ($Result->isSuccess())
		{
			$this->updateDefaultProvider();
		}

		return $Result;
	}

	public function update()
	{
		// internal auth provider cannot be updated, only set as default
		if ($this->isInternal())
		{
			$result = array('status' => 200);
			$Result = new DekiResult($result);

			$this->updateDefaultProvider();
		}
		else
		{
			$Result = parent::update();
			if ($Result->isSuccess())
			{
				$this->updateDefaultProvider();
			}
		}

		return $Result;
	}

	/*
	 * Update the config variable to reference this service id
	 */
	private function updateDefaultProvider()
	{
		$isCurrent = $this->isDefaultProvider();
		if (!$isCurrent && $this->setDefaultProvider)
		{
			// set the default provider to this service
			wfSetConfig('wgDefaultAuthServiceId', $this->getId());
			wfSaveConfig();
		}
		else if ($isCurrent && !$this->setDefaultProvider)
		{
			// set the default provider to the local auth provider since
			// this service is currently the default but shouldn't be
			wfSetConfig('wgDefaultAuthServiceId', self::getInternal()->getId());
			wfSaveConfig();
		}
	}

}
