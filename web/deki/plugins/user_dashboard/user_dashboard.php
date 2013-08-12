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

// @TODO kalida: insert common user header to dashboard

if (defined('MINDTOUCH_DEKI')) :
define('USER_DASHBOARD', true);

abstract class UserDashboardPlugin extends DekiPlugin
{
	const AJAX_FORMATTER = 'user_dashboard';
	const PLUGIN_FOLDER = 'user_dashboard';
	const CONFIG_USER_DASHBOARDS = 'ui/user-dashboards';
	const DEFAULT_DASHBOARD_COOKIE = 'default_dashboard';

	/**
	 * @var User $User - owner of current dashboard
	 */
	protected $User = null;

	/**
	 * @var PageInfo $UserPageInfo - PageInfo for the User's dashboard page
	 */
	protected $UserPageInfo = null;

	/**
	 * @var string $pluginFolder - Location of this plugin (enable in LocalSettings.php)
	 */
	protected $pluginFolder = null;

	/**
	 * @var string $displayTitle - Text title used on dashboard tabs
	 */
	protected $displayTitle = null;

	/**
	 * @var boolean $privatePlugin - If true, only display plugin to dashboard owner
	 */
	protected $privatePlugin = false;

	/**
	 * @var string[] - internal array of javascript files to load
	 */
	protected $dashboardJavascript = array();

	/**
	 * @var string[] - internal array of css files to load
	 */
	protected $dashboardCss = array();

	public static function init()
	{
		DekiPlugin::registerHook(Hooks::PAGE_SAVE_REDIRECT, array(__CLASS__, 'saveRedirectHook'));
		DekiPlugin::registerHook(Hooks::USER_DASHBOARD, array(__CLASS__, 'dispatch'), 10);
		DekiPlugin::registerHook(Hooks::AJAX_FORMAT . self::AJAX_FORMATTER, array(__CLASS__, 'ajaxHook'));
		DekiPlugin::requirePhp(self::PLUGIN_FOLDER, 'user_dashboard_page.php');
	}
	
	public static function saveRedirectHook($Article, &$url)
	{
		$Title = $Article->getTitle();

		// if saving user page, redirect to homepage view afterwards
		if (NS_USER == $Title->getNamespace() && (strlen($Title->getPartialURL()) > 0)
				&& (strpos($Title->getPrefixedURL(), '/') === false))
		{
			$url = $Title->getLocalUrl() . '?view=home';
		}
	}

	public static function skinHook(&$Template)
	{
		// move defaults to 'name.disabled' in case the skin wants to use them
		$disabledKeys = array('displaypagetitle', 'title');
		
		foreach ($disabledKeys as $key)
		{
			// @note kalida: would like to use get() to access the template var without echo
			// $Template->set($key . '.disabled', $Template->get($key));
			$Template->set($key, '');
		}
				
		$Template->setupDashboard();
	}

	public static function ajaxHook(&$body, &$message, &$success)
	{
		$Request = DekiRequest::getInstance();

		$action = $Request->getVal('action');
		$plugin = $Request->getVal('plugin');
		$userId = $Request->getVal('userId');
		$User = DekiUser::newFromId($userId);

		$body = '';
		$success = false;

		if (is_null($User))
		{
			$message = wfMsg('UserDashboard.error.nouser');
			return;
		}

		if (empty($plugin))
		{
			$message = wfMsg('UserDashboard.error.noplugin');
			return;
		}

		$PageInfo = self::getUserPageInfo($User);
		$plugins = self::loadPlugins($User, $PageInfo);
		$ActivePlugin = self::findPlugin($plugins, $plugin);

		if (is_null($ActivePlugin))
		{
			$message = wfMsg('UserDashboard.error.invalid-plugin', $plugin);
			return;
		}

		switch ($action)
		{
			default:
			case 'view':
				$body = self::renderAjaxResponse($ActivePlugin);
				break;
		}

		$success = true;
	}

	public static function dispatch($User)
	{
		global $wgArticle, $wgOut, $wgUser;
		
		// hook skin variables for later rendering (only fires when dashboard actually rendered)
		DekiPlugin::registerHook(Hooks::SKIN_OVERRIDE_VARIABLES, array(__CLASS__, 'skinHook'));

		if (is_null($User))
		{
			DekiMessage::error(wfMsg('UserDashboard.error.nouser'));
			$wgOut->redirectHome();
			return;
		}
		
		$PageInfo = self::getUserPageInfo($User);

		if (is_null($PageInfo))
		{
			// no user page -- let plugins attempt to create
			DekiPlugin::executeHook(Hooks::USER_DASHBOARD_NO_USER_PAGE, array($User));
			$PageInfo = self::getUserPageInfo($User);
			
			// still no user page -- redirect to edit
			if (is_null($PageInfo))
			{
				$Title = Title::newFromText(wfEncodeTitle($User->getUsername()), NS_USER);
				$url = $Title->getFullUrl('action=edit');
				self::redirect($url);
				return;
			}
		}

		$plugins = self::loadPlugins($User, $PageInfo);
		$Request = DekiRequest::getInstance();
		$active = $Request->getVal('view');
		
		// if owner, use or set default dashboard cookie
		if ($wgUser->getId() == $User->getId())
		{
			if (is_null($active))
			{
				// no tab specified; attempt to load from cookie
				$active = $Request->getVal(self::DEFAULT_DASHBOARD_COOKIE, null);
			}
			else
			{
				// active tab specified; remember for 10 years (365 days * 10)
				set_cookie(self::DEFAULT_DASHBOARD_COOKIE, $active, 315360000);
			}
		}

		// include before plugin js / css
		DekiPlugin::includeJavascript(self::PLUGIN_FOLDER, 'user_dashboard.js');
		DekiPlugin::includeJavascript(self::PLUGIN_FOLDER, 'parseuri.js');
		DekiPlugin::includeCss(self::PLUGIN_FOLDER, 'user_dashboard.css');

		$ActivePlugin = self::findPlugin($plugins, $active);

		// plugin specified but not found; redirect to default dashboard
		if (is_null($ActivePlugin) && !empty($active))
		{
			if ($wgUser->getId() == $User->getId())
			{
				set_cookie(self::DEFAULT_DASHBOARD_COOKIE, null, -315360000);
			}
			self::redirect($PageInfo->uriUi);
			return;
		}

		$html = self::renderPlugins($plugins, $ActivePlugin);

		// needed for ajax hook
		$html .= '<script type="text/javascript">';
		$html .= 'Deki.Plugin.UserDashboard.userId = ' . $User->getId() . ';' . "\n";
		$html .= '</script>';

		$wgOut->setPageTitle(Skin::pageDisplayTitle());
		$wgOut->addHtml($html);
		
		$wgArticle->renderAdditionalComponents();
	}

	public function __construct($User)
	{
		$this->User = $User;
	}

	/**
	 * Find plugin in using name
	 * @param DekiUserDashboardPlugin[] $plugins
	 * @param $name
	 * @return DekiUserDashboardPlugin
	 */
	protected static function findPlugin($plugins, $name)
	{
		$ActivePlugin = null;
		foreach ($plugins as $Plugin)
		{
			if ($Plugin->pluginFolder == $name || $Plugin->getPluginId() == $name)
			{
				$ActivePlugin = $Plugin;
				break;
			}
		}

		return $ActivePlugin;
	}

	protected static function redirect($url, $responseCode = '302', $now = false)
	{
		global $wgOut;

		$wgOut->redirect($url, $responseCode);
		if ($now)
		{
			$wgOut->output();
			exit();
		}
	}

	protected static function renderAjaxResponse($Plugin)
	{
		global $wgDekiPluginPath;

		$data = array();
		$data['name'] = $Plugin->getPluginId();
		$data['html_contents'] = self::renderPluginContents($Plugin);
		$data['css'] = array();
		$data['js'] = array();

		// absolute path for ajax requested files
		$folder = $wgDekiPluginPath . '/' . $Plugin->getPluginFolder();

		foreach ($Plugin->dashboardCss as $file)
		{
			$data['css'][] = $folder . '/' . $file;
		}

		foreach ($Plugin->dashboardJavascript as $file)
		{
			$data['js'][] = $folder . '/' . $file;
		}

		return $data;
	}

	/**
	 * Render plugins in dashboard
	 * @param UserDashboardPlugin[] $plugins - plugins to render
	 * @param string $active - name of active plugin
	 * @return string - html to render, null on error (i.e. no active plugin)
	 */
	protected static function renderPlugins($plugins, $ActivePlugin = null)
	{
		if (empty($plugins))
		{
			return null;
		}

		$ActivePlugin = is_null($ActivePlugin) ? $plugins[0] : $ActivePlugin;

		$html = '';
		$html .= '<div id="deki-dashboard">';

		$html .= '<div id="deki-dashboard-tab-area" class="dashboard-default">';
		$html .= '<ul class="deki-dashboard-tabs">';

		foreach ($plugins as $Plugin)
		{
			$class = '';

			if ($Plugin == $ActivePlugin)
			{
				$class = ' class="active" ';
			}

			$html .= '<li' . $class . ' id="deki-dashboard-tab-' . $Plugin->getPluginId() . '">'
					. '<a href="' . $Plugin->getUrl() . '" class="deki-dashboard-link">' . htmlspecialchars($Plugin->getDisplayTitle()) . '</a></li>';
		}
		unset($Plugin);

		// clearing div for IE6
		$html .= '<div class="clear"></div>';
		$html .= '</ul>';

		// if no javascript, show tab area by default
		$html .= '<noscript><style type="text/css">#deki-dashboard-tab-area { opacity: 1.0; }</style></noscript>';
		$html .= '</div>';

		$html .= '<div id="deki-dashboard-loading">' . wfMsg('UserDashboard.dashboard.loading') . '</div>';

		$html .= '<div id="deki-dashboard-contents">';
		$html .= self::renderPluginContents($ActivePlugin);
		$html .= '</div>';

		// end deki-dashboard
		$html .= '</div>';

		// external files: use full path to file
		$folder = $ActivePlugin->getPluginFolder();

		foreach ($ActivePlugin->dashboardJavascript as $file)
		{
			DekiPlugin::includeJavascript($folder, $file);
		}

		foreach ($ActivePlugin->dashboardCss as $file)
		{
			DekiPlugin::includeCss($folder, $file);
		}

		return $html;
	}

	protected static function &renderPluginContents($Plugin)
	{
		$html = '';
		$html .= '<div id="deki-dashboard-' . $Plugin->getPluginId() . '">';
		$html .= $Plugin->getHtml();
		$html .= '</div>';

		return $html;
	}

	/**
	 * Get instances of dashboard plugins from allowed list
	 * @param DekiUser $User - owner of current dashboard page
	 * @return UserDashboardPlugin[] - instantiated Plugin objects
	 */
	protected static function loadPlugins($User, $PageInfo)
	{
		global $IP, $wgDekiPluginPath, $wgDefaultDekiUserDashboardPlugins, $wgUser;

		// get plugin list from ui key
		$userPlugins = wfGetConfig(self::CONFIG_USER_DASHBOARDS, null);
		$pluginList = array();

		if (is_null($userPlugins) || empty($userPlugins))
		{
			$pluginList = $wgDefaultDekiUserDashboardPlugins;
		}
		else
		{
			$pluginList = explode(',', $userPlugins);
		}

		// separate out the plugin types
		$pluginsByKey = array();
		$phpPlugins = array();
		$dekiScriptPlugins = array();

		foreach ($pluginList as $plugin)
		{
			$plugin = strtolower(trim($plugin));

			if (empty($plugin) || isset($pluginsByKey[$plugin]))
			{
				continue;
			}

			$pluginsByKey[$plugin] = true;

			// dekiscript plugins are in template: path (9 chars)
			if (strncasecmp($plugin, 'template:', 9) == 0)
			{
				$dekiScriptPlugins[] = $plugin;
			}
			else
			{
				$phpPlugins[] = $plugin;
			}
		}

		// instantiate plugins
		$plugins = array();
		$pluginDirectory = $IP . $wgDekiPluginPath . '/' . basename(__FILE__, '.php');
		DekiPlugin::loadFromArray($pluginDirectory, $phpPlugins);
		DekiPlugin::executeHook(Hooks::DATA_GET_USER_DASHBOARD_PLUGINS, array(&$plugins, $User));

		$plugins = array_merge($plugins, self::loadDekiScriptPages($User, $dekiScriptPlugins));

		// keep final plugin list in config key order, if specified
		if (!is_null($userPlugins))
		{
			$plugins = self::sortPlugins($plugins, $userPlugins);
		}

		// only return plugins that are visible to user
		$viewablePlugins = array();

		foreach ($plugins as $Plugin)
		{
			$Plugin->UserPageInfo = $PageInfo;
			$Plugin->initPlugin();

			if ($Plugin->isVisible($wgUser))
			{
				$viewablePlugins[] = $Plugin;
			}
		}

		return $viewablePlugins;
	}

	/**
	 * Sort plugins based comma-separated list of names, if any
	 * @param UserDashboardPlugin[] $plugins - plugins to sort
	 * @param $orders - comma-separated list of plugin orders
	 * @return UserDashboardPlugin[] - sorted list
	 */
	protected static function sortPlugins($plugins, $orders)
	{
		if (is_null($orders) || empty($plugins))
		{
			return $plugins;
		}

		// plugins found in $orders go first, followed by default order
		$orders = explode(',', $orders);
		$pluginsByKey = array();
		$sorted = array();

		foreach ($plugins as $Plugin)
		{
			// use the plugin folder, not id [which was sanitized for css]
			$id = strtolower(trim($Plugin->pluginFolder));
			
			// store a list of plugins at each key
			if(!isset($pluginsByKey[$id]))
			{
				$pluginsByKey[$id] = array();
			}
			$pluginsByKey[$id][] = $Plugin;
		}

		foreach ($orders as $order)
		{
			$id = strtolower(trim($order));

			// move ordered items to the sorted list
			if (isset($pluginsByKey[$id]))
			{
				$sorted = array_merge($sorted, array_values($pluginsByKey[$id]));
				unset($pluginsByKey[$id]);
			}
		}

		// merge in the remaining default items
		foreach($pluginsByKey as $pluginList)
		{
			$sorted = array_merge($sorted, array_values($pluginList));
		}
		return $sorted;
	}

	/**
	 * Get instances of dekiscript plugins from allowed list
	 * @param DekiUser $User - dashboard owner
	 * @param array $paths - array of paths to load
	 * @return UserDashboardPlugin[] - loaded plugins
	 */
	protected static function loadDekiScriptPages($User, $paths)
	{
		$plugins = array();

		if (is_null($paths))
		{
			return $plugins;
		}

		foreach ($paths as $path)
		{
			$path = trim($path);

			// ignore blanks and duplicates
			if (empty($path) || isset($plugins[$path]))
			{
				continue;
			}

			$Plugin = new UserDashboardPage($User);
			$Plugin->pagePath = $path;
			$Plugin->pluginFolder = $path;

			$plugins[$path] = $Plugin;
		}

		return array_values($plugins);
	}

	/**
	 * Get PageInfo object for a certain user's dashboard
	 * @return PageInfo - PageInfo object, or null if not found
	 */
	protected static function getUserPageInfo($User)
	{
		$Title = Title::newFromText(wfEncodeTitle($User->getUsername()), NS_USER);
		$path = $Title->getPrefixedDbKey();

		$Plug = DekiPlug::getInstance()->At('pages', '=' . $path, 'info');
		$Result = $Plug->Get();

		$PageInfo = null;

		if ($Result->isSuccess())
		{
			$PageInfo = DekiPageInfo::newFromArray($Result->getVal('body/page'));
		}

		return $PageInfo;
	}

	/**
	 * Override for initialization after constructor
	 * @return N/A
	 */
	protected function initPlugin() { }

	/**
	 * Override to customize plugin html output
	 * @return string - html to display
	 */
	protected function getHtml() { return null; }

	/**
	 * Override to customize css id ("deki-dashboard-pluginFolder", "deki-dashboard-tab-pluginFolder")
	 * @return string
	 */
	protected function getPluginId() { return $this->pluginFolder; }

	/**
	 * Override to customize location of plugin files.
	 * @return string - path to plugin folder (user_dashboard/my_plugin)
	 */
	protected function getPluginFolder()
	{
		$folder = self::PLUGIN_FOLDER . '/' . $this->pluginFolder;
		return $folder;
	}

	/**
	 * Override to customize how the displayTitle is formatted. Presentation method (displayed to end user).
	 * @return string
	 */
	protected function getDisplayTitle() {
		return is_null($this->displayTitle) ? $this->pluginFolder : $this->displayTitle;
	}

	/**
	 * Get url to dashboard with this particular plugin loaded
	 * @return string
	 */
	protected function getUrl()
	{
		// @TODO kalida: support passing an array of additional query params
		$url = $this->UserPageInfo->uriUi;

		// ? or &
		$url .= strpos('?', $url) === false ? '?' : '&';
		$url .= 'view=' . $this->getPluginId();

		return $url;
	}

	/**
	 * Returns true if a given user can see this plugin
	 * @param DekiUser $ToUser - the user being checked
	 * @return boolean
	 */
	protected function isVisible($ToUser)
	{
		if (!$this->privatePlugin || ($ToUser->getId() == $this->User->getId()))
		{
			return true;
		}

		return false;
	}

	protected function includeDashboardJavascript($file) { $this->dashboardJavascript[] = $file; }
	protected function includeDashboardCss($file) { $this->dashboardCss[] = $file; }
}

UserDashboardPlugin::init();

endif;
