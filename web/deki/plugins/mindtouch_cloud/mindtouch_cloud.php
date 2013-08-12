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

class MindTouchCloudPlugin extends DekiPlugin
{
	const PLUGIN_FOLDER = 'mindtouch_cloud';
	const CLOUD_CONFIG_KEY = 'site/limited-admin-permissions';
	const CLOUD_FULLADMIN_COOKIE = 'isfulladmin';
	
	// used to avoid interating over banner arrays
	// (date is in terms of time until start -- 14 means 14 days before license expire)
	const BANNER_MIN_DAYS = 30;
	protected static $BANNER_TRIAL = array(
		'user' => array(
			'p1' => '14:0',
			'p2' => '0:-7',
			'p3' => '-8:-99'
		),
		'admin' => array(
			'p1' => '14:0',
			'p2' => '0:-7',
			'p3' => '-8:-99'
		)
	);
	
	protected static $BANNER_COMMERCIAL = array(
		'user' => array(
			'p1' => '14:0',
			'p2' => '0:-7',
			'p3' => '-8:-99'
		),
		'admin' => array(
			'p1' => '14:0',
			'p2' => '0:-7',
			'p3' => '-8:-99'
		)
	);
	
	/**
	 * Define controller & actions are unavailable for cloud
	 * @var array
	 */
	protected static $restricted = array(
		'analytics' => true,
		'authentication' => true,
		'cache_management' => true,
		'configuration' => array('listing'),
		'email_settings' => true,
		'extensions' => true,
		'kaltura_video' => true,
		'package_importer' => true,
		'product_activation' => true,
		'role_management' => true,
		'custom_html' => true,
		'dashboard' => array('videos', 'blog'),
		'skinning' => true
	);
	
	/**
	 * Define the control panel menu navigation
	 * @var array
	 */
	protected static $menuItems = array(
		'dashboard' => array(
			'dashboard' => array('index'), 
		), 
		'users' => array(
			'user_management' => array('listing', 'seated', 'deactivated', 'add', 'add_multiple'), 
			'group_management' => array('listing', 'add'),
			'bans' => array('listing', 'add')
		), 
		'custom' => array(
			'customize' => array(),
			'custom_css' => array()
		), 
		'maint' => array(
			'page_restore' => array(), 
			'file_restore' => array(),
		), 
		'settings' => array(
			'configuration' => array('settings'),
			'editor_config' => array()
		)
		/*
		@note kalida: hide zendesk integration until final CP implementation in place
		,
		'integrations' => array(
			'zendesk' => array()
		)
		*/
	);
	
	public static function init()
	{
		if (!self::isFullAdmin())
		{
			// control panel hooks
			DekiPlugin::registerHook('ControlPanel:Template:RenderTitle', array(__CLASS__, 'renderTitle'));
			DekiPlugin::registerHook('ControlPanel:Template:RenderHead', array(__CLASS__, 'renderHead'));
			DekiPlugin::registerHook('ControlPanel:Template:RenderHeader', array(__CLASS__, 'renderHeader'));
			DekiPlugin::registerHook('ControlPanel:Template:RenderFooter', array(__CLASS__, 'renderFooter'));
			DekiPlugin::registerHook('ControlPanel:Template:RenderMenu', array(__CLASS__, 'renderMenu'));
			DekiPlugin::registerHook('ControlPanel:Template:RenderTabs', array(__CLASS__, 'renderTabs'));
			DekiPlugin::registerHook('ControlPanel:InitializeAction', array(__CLASS__, 'hookInitializeAction'));
		}
		
		// product hooks
		DekiPlugin::registerHook(Hooks::HEAD_GENERATOR, array(__CLASS__, 'hookHeadGenerator'));
		DekiPlugin::registerHook(Hooks::ERROR_SITE_SETTINGS, array(__CLASS__, 'hookErrorSiteSettings'));
		DekiPlugin::registerHook(Hooks::SKIN_RENDER_PAGE_HEADER, array(__CLASS__, 'hookRenderLicenseBanner'));
		DekiPlugin::registerHook(Hooks::DISPLAY_ROLE, array(__CLASS__, 'hookDisplayRole'));
	}

	public static function hookInitializeAction($controller, $action, &$redirectTo)
	{
        // load custom resource string overrides when on CP
        DekiPlugin::loadResources(self::PLUGIN_FOLDER, 'resources.custom.txt');

		if (isset(self::$restricted[$controller]))
		{
			// check if specific actions are restricted
			if (is_array(self::$restricted[$controller]))
			{
				if (!in_array($action, self::$restricted[$controller]))
				{
					return;
				}
			}
			
			// attempting to access a restricted control panel item
			$redirectTo = '/deki/cp';
			return self::HANDLED_HALT;
		}
	}
	
	public static function hookHeadGenerator(&$generatedBy)
	{
		global $wgUser;
		if ($wgUser->isAdmin())
		{
			$generatedBy .= ' ' . wfGetConfig('version/text');
		}
	}
	
	public static function hookErrorSiteSettings()
	{
		global $wgDekiPluginPath;
		$View = self::createView(self::PLUGIN_FOLDER, 'error');
		
		$htmlHead = '';
		self::renderHead($htmlHead, null, 'error.css');
		$View->set('head', $htmlHead);
		
		echo $View->render();
		return self::HANDLED_HALT;
	}
	
	
	public static function renderHead(&$htmlHead, $Template, $filename = 'cloud.css')
	{
		global $wgDekiPluginPath;
		$assets = $wgDekiPluginPath .'/'.  self::PLUGIN_FOLDER .'/assets';
		$htmlHead .= '<link rel="stylesheet" type="text/css" media="screen" href="'. $assets . '/' . $filename . '" />' . "\n";
	}
	
	public static function renderFooter(&$html, $Template) {
		// include UserFly tracking
		$html .= '<script type="text/javascript">'
		. '$(function() { var userflyHost = (("https:" == document.location.protocol) ? "https://secure.userfly.com" : "http://asset.userfly.com");'
		. '$.getScript(userflyHost + "/users/57519/userfly.js", function() {}); });'
		. '</script>';
	}
	
	public static function renderHeader(&$htmlHeader, $Template)
	{
		/* @var $View DekiPluginView */
		$View = self::createView(self::PLUGIN_FOLDER, 'header');
		
		// render the licensing banner
		$html = '';
		$class = '';
		self::getLicenseBannerText('admin', $html, $class);
		if (!empty($html))
		{
			$View->set('header.banner.html', $html);
			$View->set('header.banner.class', $class);
		}
		$htmlHeader = $View->render();
	}
	
	public static function renderMenu(&$htmlMenu, $Template)
	{
		$htmlMenu = DekiTemplate::menu(self::$menuItems, $Template->get('controller.name'));
	}
	
	public static function renderTabs(&$htmlTabs, $Template)
	{
		$htmlTabs = DekiTemplate::tabs(self::$menuItems, $Template->get('controller.name'));
	}
	
	public static function renderTitle(&$htmlTitle, $Template, $pageTitle)
	{
		// autogenerate a title
		$section = $Template->get('controller.name');
		$group = DekiTemplate::getGroup(self::$menuItems, $section);
		$page = $Template->get('controller.action');
		
		if (empty($group))
		{
			$group = $section;
		}
		$groupTitle = $Template->msg('Common.title.'.$group);
		$htmlTitle .= ($pageTitle ? '' : ' - ') . $groupTitle;
		
		// don't include the subsection for the page title
		if (!$pageTitle)
		{
			$sectionPages = DekiTemplate::$menuItems[$group][$section];
			if (in_array($page, $sectionPages))
			{
				$htmlTitle .= ' - ' . $Template->msg('Common.title.'.$section.'.'.$page);
			}
			else
			{
				$htmlTitle .= ' - ' . $Template->msg('Common.title.'.$section);
			}
		}
	}
	
	/**
	 * MT-9931: Role names are displayed differently on cloud
	 */
	public static function hookDisplayRole(&$roleDisplay, $Role)
	{
		global $wgLang;
		
		$cloudKey = 'MindTouch.Cloud.Naming.role.' . trim(strtolower($Role->getName()));
		$roleDisplay = $wgLang->keyExists($cloudKey) ? wfMsg($cloudKey) : $roleDisplay;
	}
	
	/**
	 * Check whether the user has full admin permissions 
	 */
	protected static function isFullAdmin()
	{
		// use $wg var to avoid site settings (cloud plugin can be called when site settings aren't available)
		global $wgCloudUnrestrictedIps;
		$isUnrestricted = isset($wgCloudUnrestrictedIps) && strpos($wgCloudUnrestrictedIps, DekiRequest::getInstance()->getClientIP(true)) !== false;
		
		// require authorized IP
		if (!$isUnrestricted)
		{
			return false;
		}
		
		// MT-9628: Require additional query param
		$Request = DekiRequest::getInstance(); 
		
		// set cookie when param present; otherwise, retrieve
		if ($Request->has(self::CLOUD_FULLADMIN_COOKIE))
		{
			$isFullAdmin = $Request->getBool(self::CLOUD_FULLADMIN_COOKIE);
			set_cookie(self::CLOUD_FULLADMIN_COOKIE, $isFullAdmin);
		}
		else
		{
			$isFullAdmin = $Request->getCookieVal(self::CLOUD_FULLADMIN_COOKIE, false);
		}
		
		return $isFullAdmin;
	}

	/**
	 * Renders the user facing product expiration banner
	 * 
	 * @param string &$html
	 */
	public static function hookRenderLicenseBanner(&$headerHtml)
	{
		$html = '';
		$class = '';
		self::getLicenseBannerText('user', $html, $class);
		if (!empty($html))
		{
			$headerHtml .= '<div id="deki-license-banner" class="'. $class . '">'. $html .'</div>';
		}
		
		// don't render the default license banner
		return DekiPlugin::HANDLED_HALT;
	}
	
	/**
	 * License messaging for cloud
	 * 
	 * @param enum $location - determines where the message is being displayed: admin, user
	 * @param string $html - html of the message text
	 * @param stirng $class - unique class for the message to style against
	 */
	public static function getLicenseBannerText($location, &$html, &$class)
	{
		global $wgLang;
		$html = '';
		$class = '';

		if (DekiSite::isCore())
		{
			return;
		}
		
		// grab the details for the banner period
		$type = DekiSite::isTrial() ? 'trial' : 'commercial';
		$details = self::getLicensePeriodDetails($type, $location);
		if (is_null($details))
		{
			return;
		}
		
		// generate a numeric array to pass in for localization
		// $1: date, $2: contact rep, $3: $login
		$date = $details['days'] >= 0 ? DekiLicense::getCurrent()->getExpirationDate() : DekiLicense::getCurrent()->getShutdownDate();
		$components = array(
			$wgLang->date($date),
			
			'<a href="' . ProductURL::TRIAL_LICENSE_PURCHASE . '">'. wfMsg('MindTouch.Cloud.Banner.contact') .'</a>',
			'<a href="/Special:UserLogin">'. wfMsg('MindTouch.Cloud.Banner.login') .'</a>'
		);
		
		// set the html & class for the banner
		$html = wfMsgReal('MindTouch.Cloud.Banner.'. $type .'.'. $details['name'] .'.'. $location, $components);
		
		// use the period name for the banner status class
		$class = 'status-'.$details['name'];
	}
	
	/**
	 * Retrieves the details for the current license period. Based on the current time to license expiration.
	 * 
	 * @param enum $type - trial, commercial
	 * @param enum $location - user, admin
	 */
	protected static function getLicensePeriodDetails($type, $location)
	{	
		// get the number of days until the site expires
		$days = DekiSite::daysToExpire();
		
		// optimization
		if (is_null($days) || $days > self::BANNER_MIN_DAYS)
		{
			return null;
		}
		
		$periods = $type == 'trial' ? self::$BANNER_TRIAL[$location] : self::$BANNER_COMMERCIAL[$location];
		
		foreach ($periods as $name => $interval)
		{
			list($start, $end) = explode(':', $interval);
			if ($days <= $start && $days >= $end)
			{
				// found the current period
				return array(
					'days' => $days,
					'start' => $start,
					'end' => $end,
					'name' => $name
				);
			}
		}
		
		// no details for the current period
		return null;
	}
}
MindTouchCloudPlugin::init();
