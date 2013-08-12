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
 * Handles global site details
 * @TODO guerrics: rip out license related information into DekiLicense
 */
class DekiSite 
{
	const PRODUCT_INACTIVE = 'inactive';
	const PRODUCT_CORE = 'community';
	const PRODUCT_PLATFORM = 'platform';
	const PRODUCT_COMMERCIAL = 'commercial';
	
	const STATUS_COMMUNITY = 'COMMUNITY';
	const STATUS_TRIAL = 'TRIAL';
	const STATUS_COMMERCIAL = 'COMMERCIAL';
	const STATUS_INVALID = 'INVALID';
	const STATUS_INACTIVE = 'INACTIVE';
	const STATUS_EXPIRED = 'EXPIRED';
	
	// @see DekiLicense#getLicensedProduct() for naming changes
	private static $PRODUCTS = array(
		self::PRODUCT_INACTIVE => 'MindTouch',
		self::PRODUCT_CORE => 'MindTouch Core', 
		self::PRODUCT_PLATFORM => 'MindTouch', 
		self::PRODUCT_COMMERCIAL => 'MindTouch'
	);
	
	// @var string - stores the computed site status
	private static $siteStatus = null;
	
	/**
	 * Get the set sitename
	 * @return string
	 */
	public static function getName() 
	{
		global $wgSitename;
		return $wgSitename;	
	}
		
	/**
	 * Returns the technical product version
	 * @return string
	 */
	public static function getProductVersion()
	{
		global $wgProductVersion;
		return $wgProductVersion;
	}
	 
	/**
	 * Returns the user-friendly product name (no link)
	 * @return string
	 */
	public static function getProductName()
	{	
		return self::$PRODUCTS[self::getProductType()]; 
	}
	
	public static function getProductKey()
	{
		global $wgLicenseProductKey;
		return $wgLicenseProductKey;
	}
	
	/**
	 * Returns the user-displayed product version, with a link to MindTouch.com
	 * @return string
	 */
	public static function getProductLink()
	{
		global $wgProductVersion;
		
		$name = self::getProductName();
		
		// title attribute
		$title = $name;

		if (self::isInactive() || self::isCore())
		{
			$title .= wfMsg('Product.version', $wgProductVersion); 
		}
		else
		{
			$title .= wfMsg('Product.version.commercial', $wgProductVersion);
		}
		
		$suffix = ''; 
		if (self::isTrial())
		{
			$suffix = wfMsg('Product.type.trial'); 
		}
		else if (self::isExpired())
		{
			$suffix = wfMsg('Product.type.expired');
		}
		else if (self::isInactive())
		{
			$suffix = wfMsg('Product.type.inactive');
		}
		if (!empty($suffix))
		{
			$suffix = ' <span class="product-suffix">('.$suffix.')</span>'; 
		}
		
		return '<a href="'.ProductURL::HOMEPAGE.'" class="product" title="'.htmlspecialchars($title).'" target="_blank">'.$name.$suffix.'</a>';
	}
	
	/**
	 * Returns URL to product help depending on trial status, etc.
	 * @return string
	 */
	public static function getProductHelpUrl()
	{
		$url = ProductURL::HELP;
		if (DekiLicense::getCurrent()->getLicenseType() == DekiLicense::TYPE_TRIAL)
		{
			$url = ProductURL::HELP_TRIAL;
		}
		
		return $url;
	}
	
	/**
	 * Returns the type of install you're using
	 * @TODO guerrics: consider renaming to getInstallMethod()
	 * 
 	 * @param bool $normalize - if true, the method will be lowercased with spaces => dashes
	 * @return string
	 */
	public static function getInstallType($normalize = false) 
	{
		global $IP;
		global $wgIsVM, $wgIsAMI, $wgIsLinuxPkg, $wgIsEsxVM, $wgIsMSI, $wgIsWAMP;
		
		// @see DekiInstallerEnvironment#setupEnvironment()
		$checkfiles = array(
			'vm.php',
			'msi.php',
			'installtype.ami.php',
			'installtype.package.php',
			'installtype.vmesx.php',
			'installtype.wamp.php'
		);
		foreach ($checkfiles as $file) 
		{
			if (file_exists($IP . '/config/' . $file))
			{
				require_once($IP . '/config/' . $file);
			}
		}
		
		if (isset($wgIsVM) && $wgIsVM)
		{
			$installMethod = 'VM';
		}
		else if (isset($wgIsAMI) && $wgIsAMI)
		{
			$installMethod = 'AMI';
		}
		else if (isset($wgIsLinuxPkg) && $wgIsLinuxPkg)
		{
			$installMethod = 'Package';
		}
		else if (isset($wgIsEsxVM) && $wgIsEsxVM)
		{
			$installMethod = 'VM ESX';
		}
		else if (isset($wgIsMSI) && $wgIsMSI)
		{
			$installMethod = 'MSI';
		}
		else if (isset($wgIsWAMP) && $wgIsWAMP)
		{
			$installMethod = 'WAMP';
		}
		else 
		{
			$installMethod = 'Source';
		}
		
		return $normalize ? strtolower(str_replace(' ', '-', $installMethod)) : $installMethod;
	}
	
	/**
	 * Retrieves the current site timezone offset
	 * @return string
	 */
	public static function getTimezoneOffset()
	{
		global $wgDefaultTimezone;
		$timezone = wfGetConfig('ui/timezone', $wgDefaultTimezone);
		
		// backwards compat
		if ($timezone == '00:00')
		{
			$timezone = '+00:00';
		}

		return $timezone;
	}

	/**
	 * Fetch an array of timezone options for use with select inputs
	 * @return array
	 */
	public static function getTimeZoneOptions()
	{
		global $wgCustomTimezones;
		$ntimezones = array();
		$ptimezones = array();
		foreach ($wgCustomTimezones as $tz) 
		{
			$tz = validateTimeZone($tz);
			if (strncmp($tz, '-', 1) == 0) 
			{
				$ntimezones[] = $tz;
			}
			else 
			{
				$ptimezones[] = $tz;
			}
		}
		for ($i = -12; $i < 14; $i++) 
		{
			$val = validateTimeZone($i.':00');
			if (strncmp($val, '-', 1) == 0) 
			{
				$ntimezones[] = $val;
			}
			else 
			{
				$ptimezones[] = $val;
			}
		}
		rsort($ntimezones);
		sort($ptimezones);
		$timezones = array_merge($ntimezones, $ptimezones);
		
		$time = gmmktime();
		
		$match = null;
		$options = array();

		foreach ($timezones as $timezone)
		{
			//parse out to do time transition
			preg_match("/([-+])([0-9]+):([0-9]+)/", $timezone, $match);
			$offset = ($match[2] * 3600 + $match[3] * 60) * (strcmp($match[1], '-') == 0 ? -1 : 1);
			
			$displayTimezone = ($timezone == '+00:00') ? '00:00' : $timezone;
			
			$tztime = gmdate('h:i A', $time + $offset);

			$options[$timezone] = wfMsg('System.Common.timezone-display', $tztime, $displayTimezone);
		}
		
		return $options;
	}
	
	/**
	 * Get the product type (as defined by constants above)
	 * @return string
	 */
	public static function getProductType()
	{
		$License = DekiLicense::getCurrent();
		
		$type = $License->getLicenseType();
		if ($type == DekiLicense::TYPE_INACTIVE)
		{
			// since the license has not been applied, no type
			return self::PRODUCT_INACTIVE;
		}
		else
		{
			// is it a core? easy-peasy
			if ($License->getLicenseType() == self::PRODUCT_CORE)
			{
				return self::PRODUCT_CORE;
			}
			else
			{
				// if we're returning a commercial license, check the license first then fall back to platform
				return $License->getCommercialNameKey(self::PRODUCT_PLATFORM);
			}
		}
	}
	
	/***
	 * Refresh the packages by forcing a reimport
	 * @note Does not reimport import-once packages
	 * 
	 * @return DekiResult
	 */
	 public static function refreshPackages()
	 {
		 // tickle package importer service
		global $wgApi, $wgDekiApi, $wgDekiSiteId, $wgRequest;
		
		$DekiPlug = DekiPlug::getInstance();
		$Request = DekiRequest::getInstance();
		
		$dekiPath = $wgApi . ($wgDekiApi{0} != '/' ? '/': '') . $wgDekiApi; 
		
		// tickle the template updater service
		$data = array(
			'update' => array(
				'@wikiid' => $wgDekiSiteId,
				'@force' => 'true',
				'uri' => $Request->getScheme() . '://' . $Request->getHost() . '/' . $dekiPath
			)
		);
		$Result = $DekiPlug->At('packageupdater', 'update')->WithApiKey()->Post($data); 
		
		return $Result;
	 }
	
	 /**
	  * Determines if the current site is private
	  */
	public static function isPrivate()
	{
		$Role = DekiUser::getAnonymous()->getRole();
		$None = DekiRole::getNone();
		return strcmp($Role->getName(), $None->getName()) == 0;
	}
	
	/**
	 * Enable or disable anonymous access to the site
	 * @param bool $private
	 */
	public static function changePrivacy($private)
	{
		$Anonymous = DekiUser::getAnonymous();
		$Current = $Anonymous->getRole();
		if ($private)
		{
			$Role = DekiRole::getNone();
		}
		else
		{
			$Role = DekiRole::getViewer();
		}
		
		if (strcmp($Role->getName(), $Current->getName()) == 0)
		{
			return true;
		}
		
		// set the none role for anonymous
		$Anonymous->setRole($Role);
		$Result = $Anonymous->update();
		return $Result->isSuccess();
	}
	 
	/**
	 * Get the status of the site as reported by site/settings
	 * @return string
	 */
	public static function getStatus()
	{
		if (is_null(self::$siteStatus))
		{
			self::$siteStatus = wfGetConfig('license/state/#text', self::STATUS_COMMUNITY);
			
			// override the returned status if the site is in expiration
			if (self::willExpire() < 0)
			{
				self::$siteStatus = self::STATUS_EXPIRED;
			}
		}
		
		return self::$siteStatus;
	}
	/**
	 * Is this a core install?
	 * @return bool
	 */
	public static function isCore() { return self::getStatus() == self::STATUS_COMMUNITY; }
	/**
	 * Is this a trial instance?
	 * @return bool
	 */
	public static function isTrial() { return self::getStatus() == self::STATUS_TRIAL; }
	/**
	 * Is this a commercial instance?
	 * @return bool
	 */
	public static function isCommercial()  { return in_array(self::getStatus(), array(self::STATUS_COMMERCIAL, self::STATUS_TRIAL)); }
	/**
	 * Is this an invalid instance?
	 * @return bool
	 */
	public static function isInvalid() { return self::getStatus() == self::STATUS_INVALID; }
	/**
	 * Is this an inactive instance?
	 * @return bool
	 */
	public static function isInactive() { return self::getStatus() == self::STATUS_INACTIVE; }
	/**
	 * Is this an expired instance?
	 * @return bool
	 */
	public static function isExpired() { return self::getStatus() == self::STATUS_EXPIRED; }
	/**
	 * Is the site in any type of non-functioning state due to licensing restrictions?
	 * @return bool
	 */
	public static function isDeactivated() { return self::isInvalid() || self::isExpired() || self::isInactive(); }
	
	/**
	* Is the site configured to run as a cloud instance? 
	* @return bool
	*/
	public static function isRunningCloud()
	{
		// check if plugin enabled
		return class_exists('MindTouchCloudPlugin');
	}

	/**
	 * @deprecated
	 * Will this site expire soon? (And if so, in how many days?)
	 * @note days can be negative if the site is already expired!
	 * 
	 * @return mixed - false or int (days to expire)
	 */
	public static function willExpire() 
	{
		global $wgUser;
		
		$expiry = wfGetConfig('license/expiration/#text', null);
		if (is_null($expiry)) 
		{
			return false;
		}
		
		if ($wgUser->isAdmin())
		{
			global $wgShowBannerToAdmins; 
			$days = $wgShowBannerToAdmins; 
		}
		else if (!$wgUser->isAnonymous())
		{
			global $wgShowBannerToUsers; 
			$days = $wgShowBannerToUsers; 
		}
		else
		{
			global $wgShowBannerToAnon; 
			$days = $wgShowBannerToAnon; 
		}
		
		$timestamp = wfTimestamp(TS_UNIX, $expiry);
		$diff = $timestamp - mktime();
		
		if ($diff > ($days * 86400)) 
		{
			return false;
		}
		
		return ceil($diff / 86400);
	}

	/**
	 * Method determines the number of days before the site expires
	 * @return int - null if the site will never expire
	 */
	public static function daysToExpire() 
	{
		$License = DekiLicenseFactory::getCurrent();
		$expiry = $License->getExpirationDate(TS_DREAM);
		if (is_null($expiry))
		{
			return null;
		}
		
		// compute the number of days
		$timestamp = wfTimestamp(TS_UNIX, $expiry);
		$diff = $timestamp - mktime();
		return ceil($diff / 86400);
	}
}
