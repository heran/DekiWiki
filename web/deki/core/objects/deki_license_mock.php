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
 * Mock DekiLicense class to test UI functionality.
 * @see DekiLicenseFactory
 * 
 * To enable mock licensing, edit LocalSettings.php:
	$wgDekiLicenseMock = array(
		'days' => -2
	);
	require_once('deki/core/objects/deki_license.php');
	require_once('deki/core/objects/deki_license_mock.php');
	require_once('deki/core/objects/deki_license_factory.php');
	DekiLicenseFactory::getCurrent();
 * );
 */
class DekiLicenseMock extends DekiLicense
{
	protected $days = null;
	
	public static function &getCurrent()
	{
		if (is_null(self::$instance))
		{
			global $wgDekiLicenseMock;
			self::$instance = new self($wgDekiLicenseMock);
		}

		return self::$instance;
	}
	
	/**
	 * Creates a new mock license
	 * 
	 * @param array $licenseDetails
	 */
	public function __construct($config)
	{
		// Do things with the incoming values
		
		// set the number of days till expiration
		$this->days = isset($config['days']) ? $config['days'] : null;
	}
	
	public function getExpirationDate($format = TS_UNIX)
	{
		if (is_null($this->days))
		{
			return parent::getExpirationDate($format);
			
		}
		
		return wfTimestamp($format, time() + ($this->days * 86400));
	}
}
