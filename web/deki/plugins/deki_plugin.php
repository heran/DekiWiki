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
 * Defines all the application supported hooks
 * @note Hook names are case-insensitive
 */
class Hooks
{
	/**
	 * General special namespace handler
	 * @param $pageName - requested special page name, e.g. Special:Foo => 'Foo'
	 * @return N/A
	 */
	const SPECIAL_PAGE = 'Special:';
	/**
	 * Application special pages
	 * Special pages are a class of plugins. They are only loaded when a request to Special:
	 * is made. Each special page has the same arguments passed in.
	 * 
	 * @param $pageName - requested page name
	 * @param &$pageTitle - page title to display
	 * @param &$html - page html to display
	 * @param &$subhtml - page subhtml to display
	 * @return N/A
	 */
	const SPECIAL_ABOUT = 'Special:About';
	const SPECIAL_ADMIN = 'Special:Admin';
	const SPECIAL_ARTICLE = 'Special:Article';
	const SPECIAL_CONTRIBUTIONS = 'Special:Contributions';
	const SPECIAL_EVENTS = 'Special:Events';
	const SPECIAL_LIST_RSS = 'Special:ListRss';
	const SPECIAL_LIST_TEMPLATES = 'Special:ListTemplates';
	const SPECIAL_LIST_USERS = 'Special:Listusers';
	const SPECIAL_PACKAGE = 'Special:Package';
	const SPECIAL_PAGE_PROPERTIES = 'Special:PageProperties';
	const SPECIAL_PAGE_RESTRICT = 'Special:PageRestrictions';
	const SPECIAL_PAGE_EMAIL = 'Special:PageEmail';
	const SPECIAL_POPULAR_PAGES = 'Special:PopularPages';
	const SPECIAL_RECENT_CHANGES = 'Special:Recentchanges';
	const SPECIAL_SEARCH = 'Special:Search';
	const SPECIAL_SITEMAP = 'Special:Sitemap';
	/**
	 * Triggered when Special: is requested
	 * @return N/A
	 */
	const SPECIAL_SPECIAL_PAGES = 'Special:SpecialPages';
	const SPECIAL_TAGS = 'Special:Tags';
	const SPECIAL_USER_BAN = 'Special:UserBan';
	const SPECIAL_USER_LOGIN = 'Special:UserLogin';
	const SPECIAL_USER_LOGOUT = 'Special:UserLogout';
	const SPECIAL_USER_PASSWORD = 'Special:UserPassword';
	const SPECIAL_USER_REGISTRATION = 'Special:UserRegistration';
	const SPECIAL_USER_PREFERENCES = 'Special:UserPreferences';
	const SPECIAL_WATCH_LIST = 'Special:Watchlist';
	const SPECIAL_WATCHED_PAGES = 'Special:Watchedpages';
	/**
	 * /Application special pages
	 */

	/**
	 * Method is called before ajaxformat to allow altering the content type
	 * @note Hook is only a prefix. Append own formatter for complete hook. e.g. AjaxInit:Something => formatter=something
	 * 
	 * @param string &$contentType - set the content type for the ajax request (default: application/json)
	 * @param bool &$requireXmlHttpRequest - enforce XmlHttp header requirement (default: true)
	 */
	const AJAX_INIT = 'AjaxInit:';
	/**
	 * Main ajax handler method
	 */
	const AJAX_FORMAT = 'AjaxFormat:';
	
	/**
	 * Used to customize the generator name in <meta name="generator" content="...">
	 * @param string &$generatedBy - text to display in the content section (default: product name)
	 * @param return N/A
	 */
	const HEAD_GENERATOR = 'Head:Generator';
	
	/**
	 * Used to change the display name for a role
	 * @param string &$roleText - current text of the role
	 * @param string $Role - original role object
	 */
	const DISPLAY_ROLE = 'Display:Role';
	
	/**
	 * Triggered when any page is saved.
	 * @note guerrics: Consider moving to a notification hook?
	 * 
	 * @param Article $Article - article that was just saved
	 * @return N/A
	 */
	const PAGE_SAVE = 'Page:Save';
	
	/**
	 * Triggered when a page is saved
	 * @param Article $Article - article that was just saved
	 * @param string $&url - url to redirect to
	 * @return N/A
	 */
	const PAGE_SAVE_REDIRECT = 'Page:SaveRedirect';
	/**
	 * Control the comments html display
	 * 
	 * @param Title $Title - title corresponding to the rendered article
	 * @param string &$pluginHtml - comments html
	 * @return HANDLED_HALT to halt internal html generation
	 */
	const PAGE_RENDER_COMMENTS = 'Page:RenderComments';
	/**
	 * Control the files html display
	 * 
	 * @param Title $Title - title corresponding to the rendered article
	 * @param string &$pluginHtml - files html
	 * @return HANDLED_HALT to halt internal html generation
	 */
	const PAGE_RENDER_FILES = 'Page:RenderFiles';
	/**
	 * Control the image gallery html display
	 * 
	 * @param Title $Title - title corresponding to the rendered article
	 * @param string &$pluginHtml - image gallery html
	 * @param int &$imageCount - count to display in the skin
	 * @return HANDLED_HALT to halt internal html generation
	 */
	const PAGE_RENDER_IMAGES = 'Page:RenderImages';
	/**
	 * Control the tag html display
	 * 
	 * @param Title $Title - title corresponding to the rendered article
	 * @param string &$pluginHtml - tags html
	 * @return HANDLED_HALT to halt internal html generation
	 */
	const PAGE_RENDER_TAGS = 'Page:RenderTags';
	
	/**
	 * Triggered when the default editor makes a request for javascript files.
	 * 
	 * @param Article $Article - article content that will be loaded
	 * @param array &$editorScripts - url's to javascript files to load
	 * @return anything but UNHANDLED or UNREGISTERED will halt the default editor loading
	 */
	const EDITOR_LOAD = 'Editor:Load';
	/**
	 * Triggered when the default editor makes a request for js files with editor configuration.
	 *
	 * @param array &$scripts - url's to js files to load
	 * @return anything but UNHANDLED or UNREGISTERED will halt the default editor styles loading
	 */
	const EDITOR_CONFIG = 'Editor:Config';
	/**
	 * Triggered when the default editor makes a request for css files.
	 *
	 * @param array &$styles - url's to css files to load
	 * @return anything but UNHANDLED or UNREGISTERED will halt the default editor styles loading
	 */
	const EDITOR_STYLES = 'Editor:Styles';
	/**
	 * Triggered after the editor form is posted back.
	 * @param string $textareaContents -unparsed/raw contents
	 * @param string &$contents - parsed contents of the page/section
	 * @param string &$displayTitle - parsed display title of the page being edited
	 * @param int &$section - section being edited
	 * @param string &$summary - edit summary
	 * @param int &$pageId - destination page id
	 */
	const EDITOR_PROCESS_FORM = 'Editor:ProcessForm';
	/**
	 * Triggered when the default editor begins rendering the editor form html.
	 * 
	 * @param string &$textareaContents - unencoded/raw textarea contents
	 * @param string $forEdit - source contents
	 * @param string $displayTitle - source display title
	 * @param int $section - source section
	 * @param int $pageId - source page id
	 * @param string &$prependHtml - html inside form tag before textarea
	 * @param string &$appendHtml - html inside form tag after textarea
	 * @return N/A
	 */
	const EDITOR_FORM = 'Editor:Form';

	/**
	 * Main Hooks
	 * 
	 * These hooks are trigger for core application actions and general hooks fired from index.php
	 */
	/**
	 * Triggered early in the initialization of index.php. Allows custom url rewriting or entry point handling.
	 * 
	 * @param WebRequest $Request
	 * @param Title $Title
	 * @return N/A
	 */
	const MAIN_PROCESS_TITLE = 'Main:ProcessTitle';
	/**
	 * Triggered after the page has been rendered. Allows final modification to the output buffer.
	 * 
	 * @param OutputPage $Out - Output buffer
	 * @return N/A
	 */
	const MAIN_PROCESS_OUTPUT = 'Main:ProcessOutput';

	/**
	 * Triggered when the user's information has been cleared by the user registration process.
	 * 
	 * string &$username
	 * string &$password
	 * string &$email
	 * @return HANDLED_HALT to halt the internal user creation process, still fires CREATE_USER_COMPLETED
	 */
	const MAIN_CREATE_USER = 'Main:CreateUser';
	/**
	 * Newly created user object is passed in for any additional setup. If this
	 * call is halted then no redirect gets issued and it is up to the plugin
	 * to determine where to redirect the user after creation.
	 * 
	 * @param DekiUser $User
	 * @return HANDLED_HALT to halt the default redirect behavior. Has no effect if CREATE_USER was already halted.
	 */
	const MAIN_CREATE_USER_COMPLETE = 'Main:CreateUserComplete';
	
	/**
	 * Triggered when a user posts login information to Special:UserLogin.
	 * 
	 * @param string $username
	 * @param string $password
	 * @param int $authId
	 * @return HANDLED_HALT to halt internal login processes
	 */
	const MAIN_LOGIN = 'Main:Login';
	/**
	 * User should be fully logged in at this call.
	 * 
	 * @param DekiUser $User - newly logged in user
	 * @return N/A
	 */
	const MAIN_LOGIN_COMPLETE = 'Main:LoginComplete';
	/**
	 * Triggered when a user visits Special:UserLogin and is already logged in.
	 * 
	 * @param DekiUser $User - currently logged in user
	 * @param string $html - html to output or prepend to the default output depending on return value.
	 * @return HANDLED_HALT to avoid outputting the internal display HTML. 
	 */
	const MAIN_LOGIN_REFRESH = 'Main:LoginRefresh';
	
	/**
	 * Triggered before the current user is logged out.
	 * 
	 * @param DekiUser $User - user that is attempting to logout
	 * @return HANDLED_HALT to halt internal logout processes
	 */
	const MAIN_LOGOUT = 'Main:Logout';
	/**
	 * Triggered after the current user has been logged out.
	 * 
	 * @param DekiUser $User - user that completed the logout process
	 * @return N/A
	 */
	const MAIN_LOGOUT_COMPLETE = 'Main:LogoutComplete';

	/**
	 * Triggered when a user posts a comment to a page, action=comment
	 * 
	 * @param Title $Title - page title receiving the comment POST data
	 * @return HANDLED_HALT to avoid the internal comment processing
	 */
	const MAIN_ACTION_COMMENT = 'Main:ActionComment';
	/**
	 * Default action handler, hook to handle unknown actions.
	 * 
	 * @param string $action - name of the requested action, e.g. comment,newpage,mycustomaction
	 * @param Title $Title - currently requested page title
	 * @param Article $Article - currently requested article
	 */
	const MAIN_ACTION = 'Main:Action';
	
	/**
	 * Skin Hooks
	 * 
	 * These are skin components. You won't be able to set headers in any
	 * skin hooks since the headers will already be sent.
	 */
	
	/**
	 * Used to customize the location a skin uses to get its css
	 * @param string &$path - path to use for the css
	 * @param return N/A
	 */
	const SKIN_CSS_PATH = 'Skin:CSSPath';
	
	/**
	 * Nav pane hook allows a custom navigation module to be loaded.
	 * 
	 * @param Title $Title - corresponds to current page
	 * @param string &$navText - text to be injected into the 'sitenavtext' template variable
	 * @return N/A
	 */
	const SKIN_NAVIGATION_PANE = 'Skin:NavigationPane';
	/**
	 * Hook is called before the skin html gets rendered which allows certain template variables
	 * to be overriden or new variables to be added. Hook is dependent upon template creators to
	 * add to the template. See fiesta.php or deuce.php for details on adding this hook.
	 * 
	 * @param SkinTemplate &$template - instance of the current template being rendered
	 * @return N/A
	 */
	const SKIN_OVERRIDE_VARIABLES = 'Skin:OverrideVariables';
	
	/**
	 * Renders the page header for the skin. Used to inject the product expiration banner for users
	 * 
	 * @param string &$html - html to inject into the page header
	 */
	const SKIN_RENDER_PAGE_HEADER = 'Skin:RenderPageHeader';
	
	/**
	 * Notify Hooks
	 * 
	 * Triggered when the user is being notified via email.
	 */
	/**
	 * Triggered when an email is being sent.
	 * 
	 * @param string &$to - receiptiant email address
	 * @param strubg &$from - sender's email address
	 * @param string &$subject - email subject
	 * @param string &$body - email body
	 * @param string &$bodyHtml - alternate html email body
	 * 
	 * @return HANDLED_HALT to halt internal email sending. Will not trigger email complete hook unless
	 * the plugin fires it manually.
	 */
	const NOTIFY_EMAIL = 'Notify:Email';
	/**
	 * Triggered after an email has been sent successfully.
	 * 
	 * @param string $to
	 * @param string $from
	 * @param string $subject
	 * @param string &$body
	 * @param string &$body_html));
	 * 
	 * @return N/A
	 */
	const NOTIFY_EMAIL_COMPLETE = 'Notify:EmailComplete';
	
	/**
	 * Triggered when the dashboard is being rendered
	 * @param Title $Title - corresponds to current page
	 * @return N/A
	 */
	const USER_DASHBOARD = 'Dashboard:';
	
	/**
	 * Triggered when the user page is not found
	 * @param User $User - the user who requires a page
	 */
	const USER_DASHBOARD_NO_USER_PAGE = 'Dashboard:NoUserPage';
	
	/**
	 * UI Handler Hooks
	 */
	
	/**
	 * Triggered when CSS files are generated in the handler
	 * @param CssHandler $Handler
	 * @param array $options - any additional options passed to CssHandler process invocation
	 */
	const UI_PROCESS_CSS = 'UI:ProcessCSS';

	/**
	 * Data Hooks
	 * 
	 * Used for obtaining information from active plugins.
	 */
	/**
	 * Retrieve a plugin generated list of pages that the current user can access.
	 * 
	 * @param array &$pages - list of pages
	 * @note $pages is a 2 dimensional array with a format like
	 * 		 $pages['Page Name'] = array('name' => 'Page Name', 'href' => 'http://foo/Special:PageName');
	 * @return N/A
	 */
	const DATA_GET_SPECIAL_PAGES = 'Data:GetSpecialPages';
	
	/**
	 * Dashboard hooks
	 * 
	 * Used to render the per-user dashboard (with profile, contributions, etc.)
	 */
	
	/**
	 * Retrieve a generated list of dashboard plugins
	 * 
	 * @param DashboardPlugin[] &$plugins - list of plugins
	 * @note $plugins is an array of dashboard plugins to load
	 * @return N/A
	 */
	const DATA_GET_USER_DASHBOARD_PLUGINS = 'Data:GetDashboardPlugins';
	
	/**
	* Error hooks
	*/
	/**
	 * Retrieve a generated list of dashboard plugins
	 * 
	 * @return HANDLED_HALT to halt internal email sending. Will not trigger email complete hook unless
	 * the plugin fires it manually.
	 */
	const ERROR_SITE_SETTINGS = 'Error:SiteSettings';
}

/**
 * Class that plugins should derive from. Also handles loading and dispatching plugins
 * @todo Implement common functionality
 */
abstract class DekiPlugin
{
	// special return code, should not be returned by plugins
	const UNREGISTERED = -1;
	// normal return codes for plugins
	const UNHANDLED = 0;
	const HANDLED = 1;
	// when this code is returned additional plugins do not get executed
	const HANDLED_HALT = 2;
	
	/**
	 * Nested array that defines plugins registered for a hook
	 *
	 * @var static $hooks
	 * @type array
	 * @structure
	 *	array(
	 *		'UI_HOOK_NAME' => array(
	 *			'PRIORITY_LEVEL' => array(
	 *				'callback', //function callback() {}
	 *				'callback2',
					array('class', 'func')
	 *			),
	 *			
	 *			8 => array(
	 *			),
	 *			
	 *			10 => array(
	 *			)
	 *		)
	 *	)
	 */
	protected static $hooks = array();
	protected static $cssIncludes = array();
	protected static $javascriptIncludes = array();

	/**
	 * Initialize DekiPlugin, load any necessary site-wide plugins
	 */
	public static function init()
	{
		global $IP, $wgDekiPluginPath;
		
		// plugin dependencies
		require_once($IP . $wgDekiPluginPath . '/' . 'deki_plugin_view.php');
		require_once($IP . $wgDekiPluginPath . '/' . 'special_page_plugin.php');
		require_once($IP . $wgDekiPluginPath . '/' . 'special_mvc_plugin.php');
	}

	/**
	 * Fetches the list of enabled site plugins and loads them.
	 */
	public static function loadSitePlugins()
	{
		// loading the site plugins
		global $IP, $wgDekiPluginPath;
		$pluginDirectory = $IP . $wgDekiPluginPath;

		$plugins = self::getEnabledSitePlugins();
		self::loadFromArray($pluginDirectory, $plugins);
	}

	/**
	 * Fetches and caches a list of enabled site plugins.
	 * @note result is cached per request
	 * @return array
	 */
	public static function getEnabledSitePlugins()
	{
		static $plugins;
		if (!isset($plugins))
		{
			$plugins = self::getSitePlugins(true);
		}
		
		return $plugins;
	}
	
	/**
	 * Registers a callback with a hook by name
	 * @see Hooks
	 * 
	 * @param string $name - hook name to attach the callback to
	 * @param mixed $callback - should define a valid callback for call_user_func_array, can be array or string
	 * @param int $priority - defines the level at which a plugin is called if there are multiple registrations for a hook
	 * 
	 * @return
	 */
	public static function registerHook($name, $callback, $priority = 9)
	{
		$name = strtolower($name);
		// slight optimization, avoid sorting on the first pass
		$sort = isset(self::$hooks[$name]);

		self::$hooks[$name][$priority][] = $callback;

		// bugfix #6412: PHP plugins do not honor the priority settings
		if ($sort)
		{
			// sort the plugins according to their priority
			ksort(self::$hooks[$name], SORT_NUMERIC);
		}
	}
	
	/**
	 * Method searches for plugins to fire for a hook
	 *
	 * @param string $name - name of the hook to execute
	 * @param array<mixed> $args - array of arguements to pass to the plugin
	 * 
	 * @return int - status code determines hook status 
	 */
	public static function executeHook($name, $args = array())
	{
		$name = strtolower($name);

		if (isset(self::$hooks[$name]) && !empty(self::$hooks[$name]))
		{
			// default to unhandled
			$status = self::UNHANDLED;

			foreach (self::$hooks[$name] as &$priority)
			{
				foreach ($priority as &$callback)
				{
					// explicitly require a return value of false, simplifies plugin creation
					$result = call_user_func_array($callback, $args);

					if ($result !== self::UNHANDLED)
					{
						// default to handled response for unreturned/unknown codes
						// allows plugins to be created without returning from the method
						$status = self::HANDLED;
					
						if ($result == self::HANDLED_HALT)
						{
							// handler cancelled further processing
							$status = self::HANDLED_HALT;
							break 2;
						}
					}
				}
				unset($callback);
			}
			unset($priority);

			// return status based on plugin return codes
			return $status;
		}
		
		// no plugins have been registered with this hook
		return self::UNREGISTERED;
	}

	/**
	 * Common functionality
	 * i.e. loading a stylesheet
	 * 
	 * TODO: (guerrics) consider making these instance methods, then rename special page include methods
	 */
	/**
	 * Convenience method to include/require another php file/library from a plugin folder
	 * 
	 * @param string $pluginFolder - name of the plugin to include the file from
	 * @param string $file - name of the php file to include, i.e. "foo.php"
	 */
	public static function requirePhp($pluginFolder, $file = null)
	{
		global $IP, $wgDekiPluginPath;
		$file = is_null($file) ? basename($pluginFolder) . '.php' : $file;
		$file = $pluginFolder .'/'. basename($file);
		
		require_once($IP . $wgDekiPluginPath .'/'. $file);
	}

	/**
	 * Method includes css for a plugin
	 * 
	 * @param $pluginFolder - folder that the plugin is located in. i.e. "nav_pane"
	 * @param $file - name of the css file to include. i.e. "plugin.css"
	 * @param $external - if true, then $pluginFolder is ignore and $file is assumed to be a fully
	 * 	qualified url. i.e. "http://www.mindtouch.com/plugin.css"
	 * 
	 * TODO: need to check if html/head has been sent already
	 */
	public static function includeCss($pluginFolder, $file, $external = false)
	{
		global $wgOut;
		// sets the web accessible path to the file
		if (!$external)
		{
			global $wgDekiPluginPath;
			$href = $wgDekiPluginPath .'/'.  $pluginFolder .'/'. $file;
		}
		else
		{
			$href = $file;
		}
		$wgOut->addCss($href);
		self::$cssIncludes[] = $href;
	}
	
	/**
	 * Get html for all included popup stylesheet declarations
	 * @return string
	 */
	public static function getCssIncludes()
	{
		$css = '';
		foreach (self::$cssIncludes as $href)
		{
			$css .= '<link rel="stylesheet" type="text/css" media="screen" href="'. $href . '" />' . "\n";
		}
		return $css;
	}
	
	/**
	 * Method includes javascript for a plugin
	 *
	 * @param $pluginFolder - folder that the plugin is located in. i.e. "nav_pane"
	 * @param $file - name of the javascript file to include. i.e. "plugin.js"
	 * @param $external - if true, then $pluginFolder is ignore and $file is assumed to be a fully
	 * 	qualified url. i.e. "http://www.mindtouch.com/plugin.js"
	 * @param $delayLoad - if true the script will be loaded after page load
	 * 
	 * TODO: need to check if html/head has been sent already
	 */
	public static function includeJavascript($pluginFolder, $file, $external = false, $delayLoad = false)
	{
		global $wgOut;
		// sets the web accessible path to the file
		if (!$external)
		{
			global $wgDekiPluginPath;
			$href = $wgDekiPluginPath .'/'.  $pluginFolder .'/'. $file;
		}
		else
		{
			$href = $file;
		}
		
		// delayLoad
		if($delayLoad)
		{
			$wgOut->addHeadHTML('<script type="text/javascript">'
			. '$(function() {'
			. '$.getScript("' . $href . '", function() {}); });'
			. '</script>');
		}
		else
		{
			$wgOut->addHeadHTML('<script type="text/javascript" src="' . $href . '"></script>' . "\n");
		}
		self::$javascriptIncludes[] =  $href;
	}
	
	/**
	 * Get html for all included javascript inclusions
	 * @return string
	 */
	public static function getJavascriptIncludes()
	{
		$js = '';
		foreach (self::$javascriptIncludes as $href)
		{
			$js .= '<script type="text/javascript" src="' . $href . '"></script>' . "\n";
		}
		return $js;
	}

	/**
	 * Method includes custom resource keys for a plugin.
	 *
	 * @param $pluginFolder - folder that the plugin is located in. i.e. "nav_pane"
	 * @param $file - name of the resource file to include. i.e. "resources.custom.txt"
	 * 
	 * @TODO Implementation is due to the globals abuse in language loading.
	 */
	public static function loadResources($pluginFolder, $file)
	{
		global $wgResourcesDirectory;
		$originalDir = $wgResourcesDirectory;

		// copied from requirePhp
		global $IP, $wgDekiPluginPath;
		$wgResourcesDirectory = $IP . $wgDekiPluginPath .'/'. $pluginFolder;
		wfLoadLanguageResource($file);

		// restore previous directory
		$wgResourcesDirectory = $originalDir;
	}
	
	/**
	 * View generation helper
	 * @TODO kalida: consider adding to special plugin core
	 * @param string $pluginFolder
	 * @param string $viewName
	 * @return DekiPluginView
	 */
	protected function createView($pluginFolder, $viewName)
	{		
		global $wgDekiPluginPath;
		$viewRoot = $pluginFolder;
		
		return new DekiPluginView($viewRoot, $viewName);
	}
	
	/**
	 * /Common functionality
	 */
	
	/**
	 * Protected methods
	 */
	/**
	 * Generates an array of enabled plugins. List is either generate from in-memory arrays
	 * or from the site plugin lists.
	 * @param bool $filterDisabled - if true, disabled plugins will be excluded 
	 * @return array
	 */
	protected static function getSitePlugins($filterDisabled = false)
	{
		// loading the site plugins
		global $IP, $wgDekiPluginPath, $wgDekiPluginMode;
		$pluginDirectory = $IP . $wgDekiPluginPath;
		
		$pluginWhitelist = array();
		if ($wgDekiPluginMode == 'directory')
		{
			// unsafe mode, loads all from the plugin directory
			// traverse the plugin directory for plugins
			$Dir = dir($pluginDirectory);
			
			while (false !== ($entry = $Dir->read()))
			{
				// ignore hidden folders
				if ((strncmp($entry, '.', 1) != 0) && is_dir($pluginDirectory . '/' . $entry))
				{
					$pluginWhitelist[] = $entry;
				}
			}
			$Dir->close();
		}
		else
		{
			// "safe" mode, loads from whitelist
			global $wgDefaultDekiPlugins, $wgDekiPlugins;	
			$pluginWhitelist = array_merge($wgDefaultDekiPlugins, $wgDekiPlugins);
		}

		if ($filterDisabled)
		{
			// remove any disabled plugins
			global $wgDisabledDekiPlugins;
			$plugins = array_diff($pluginWhitelist, $wgDisabledDekiPlugins);
		}
		
		return $plugins;
	}

	/**
	 * Loads plugins from an array. Enforces disabled plugin restrictions.
	 *
	 * @param string $pluginDirectory - plugin root directory
	 * @param array $plugins - array of plugin names to load
	 */
	protected static function loadFromArray($pluginDirectory, $plugins = array())
	{
		// remove any disabled plugins
		global $wgDisabledDekiPlugins;
		
		// @TODO royk this is a specific surgical change for 2010
		// the license will explicitly state the case to disable any upsell messaging
		$License = DekiLicense::getCurrent();
		if (!$License->hasCapabilityRating() && !$License->displayCommercialMessaging())
		{
			if (!in_array('page_content_rating', $wgDisabledDekiPlugins))
			{
				$wgDisabledDekiPlugins[] = 'page_content_rating';
			}
		}
		
		$enabledPlugins = array_diff($plugins, $wgDisabledDekiPlugins);
		
		$missingPlugins = array();

		foreach ($enabledPlugins as $plugin)
		{
			$name = basename($plugin);
			$path = $pluginDirectory .'/'. $name .'/'. $name .'.php';

			if (file_exists($path))
			{
				require_once($path);
			}
			else
			{
				$missingPlugins[] = $plugin;
			}
		}

		if (!empty($missingPlugins))
		{
			$list = implode(', ', $missingPlugins);
			MTMessage::Show(wfMsg('System.Error.plugin-error'), wfMsg('System.Error.plugins-not-found', $list));
		}
	}
	/**
	 * /Protected methods
	 */
}

/**
 * Class handles loading and queuing files for caching via the js & css aggregators
 */
class DekiPluginResource
{
	protected static $load = array();

	/**
	 * Loads the cacheable resource files from each enabled plugin
	 */
	public static function loadSiteResources()
	{
		// loading the site's cacheable resources
		global $IP, $wgDekiPluginPath;
		$pluginDirectory = $IP . $wgDekiPluginPath;

		$plugins = DekiPlugin::getEnabledSitePlugins();
		self::loadFromArray($pluginDirectory, $plugins);
	}

	/**
	 * Includes a javascript file into the javascript cache
	 *
	 * @param $pluginFolder - folder that the plugin is located in. i.e. "nav_pane"
	 * @param $file - name of the resource file to include. i.e. "foo.js"
	 */
	public static function loadJavascript($pluginFolder, $file)
	{
		self::loadType('js', $pluginFolder, $file);
	}

	/**
	 * Includes a css file into the css cache
	 *
	 * @param $pluginFolder - folder that the plugin is located in. i.e. "nav_pane"
	 * @param $file - name of the resource file to include. i.e. "foo.js"
	 */	
	public static function loadCss($pluginFolder, $file)
	{
		self::loadType('css', $pluginFolder, $file);
	}


	/**
	 * Accessor for retrieving a list of files added by the plugins
	 * @return array
	 */
	public static function getLoadedJavascript()
	{
		return isset(self::$load['js']) ? self::$load['js'] : array();
	}

	public static function getLoadedCss()
	{
		return isset(self::$load['css']) ? self::$load['css'] : array();
	}


	/**
	 * Helper routine to add files for caching
	 * @param $type
	 * @param $pluginFolder
	 * @param $file
	 * @return unknown_type
	 */
	protected static function loadType($type, $pluginFolder, $file)
	{
		global $IP, $wgDekiPluginPath;
		$pluginDirectory = $IP . $wgDekiPluginPath;
		$path = $pluginDirectory .'/'.  $pluginFolder .'/'. $file;
		self::$load[$type][] = $path;
	}
	
	/**
	 * Loads cacheable information from an array. Enforces disabled plugin restrictions.
	 *
	 * @param string $pluginDirectory - plugin root directory
	 * @param array $plugins - array of plugin names to load
	 */
	protected static function loadFromArray($pluginDirectory, $plugins = array())
	{
		// remove any disabled plugins
		global $wgDisabledDekiPlugins;
		$enabledPlugins = array_diff($plugins, $wgDisabledDekiPlugins);
		
		foreach ($enabledPlugins as $plugin)
		{
			$name = basename($plugin);
			$file = $pluginDirectory .'/'. $name .'/'. 'plugin_resources.php';
			if (is_file($file))
			{
				require_once($file);
			}
		}
	}
}

/**
 * TODO: implement wfRunHooks so mediawiki plugins can attempt to run here
 * or modify to be compatible with them.
 */

DekiPlugin::init();

