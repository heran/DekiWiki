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
 * DekiPlugin View
 * Class provides a utility to avoid string concatenation when building markup.
 * @TODO guerrics: allow the assets folder to be customized
 */
class DekiPluginView
{
	/**
	 * Folder names to discover view files
	 * @var string
	 */
	const ASSETS_FOLDER = 'assets';
	const DEFAULT_VIEW_FOLDER = 'views';
	const CUSTOM_VIEW_FOLDER = 'custom';
	/**
	 * @var string - use this constant when constructing the view to avoid creating an additional folder
	 * @note using this value will disable custom views
	 */
	const NO_VIEW_FOLDER = '';

	/**
	 * @var string - relative path from plugin root containing folder views & custom
	 */
	protected $viewRoot = null;
	/**
	 * @var string - name of the view to render
	 */
	protected $viewName = null;
	/**
	 * @var string - name of the views folder
	 */
	protected $viewFolder = null;
	/**
	 * @var string - name of the custom views folder
	 */
	protected $customFolder = null;
	/**
	 * @var array - stores the view variables
	 */
	protected $bag = array();
	
	
	/**
	 * Create a new plugin view
	 * @example $View = new DekiPluginView('special_page/foo_page', 'index');
	 * @example $View = new DekiPluginView('my_plugin', 'my_view');
	 * 
	 * @param string $viewRoot - root folder containing the view folders: views & custom
	 * @param string $viewName - name of the view file to render
	 * @param optional string $viewFolder - name of the view folder (default: 'views')
	 * @param optional string $customFolder - name of the custom view folder (default: 'custom')
	 */
	public function __construct($viewRoot, $viewName, $viewFolder = self::DEFAULT_VIEW_FOLDER, $customFolder = self::CUSTOM_VIEW_FOLDER)
	{
		$this->viewRoot = $viewRoot;
		$this->viewName = $viewName;
		$this->viewFolder = $viewFolder;
		$this->customFolder = $customFolder;
	}
	
	/**
	 * Obtain the contents of the rendered view
	 * 
	 * @return string
	 */
	public function &render()
	{
		global $IP, $wgDekiPluginPath;

		// create the path to the view file
		$viewRoot = $IP . $wgDekiPluginPath .'/'. $this->viewRoot .'/';
		// set the default paths
		$defaultRoot = $viewRoot . $this->viewFolder;
		$customRoot = $viewRoot . $this->customFolder;

		$viewFile = basename($this->viewName . '.php');
		
		// is there a views folder?
		if ($this->viewFolder == self::NO_VIEW_FOLDER)
		{
			$viewFile = $defaultRoot . $viewFile;
		}
		// check for a custom view
		else if (is_file($customRoot .'/'. $viewFile))
		{
			// custom view exists
			$viewFile = $customRoot .'/'. $viewFile;
		}
		else
		{
			// default view
			$viewFile = $defaultRoot .'/'. $viewFile;
		}

		// start rendering the view
		ob_start();
		include($viewFile);
		$html = ob_get_contents();
		ob_end_clean();

		return $html;
	}

	/**
	 * Set a $value for the view by $key
	 * 
	 * @param string $key
	 * @param mixed $val
	 */
	public function set($key, $val)
	{
		$this->bag[$key] = $val;
	}

	/**
	 * Set a reference to a $value for the view by $key
	 * 
	 * @param string $key
	 * @param mixed $val
	 */
	public function setRef($key, &$val)
	{
		$this->bag[$key] = &$val;
	}
	
	/**
	 * Not implemented
	 * 
	 * @param string $name - name of the css file to include
	 * @return
	 */
	protected function includeCss($name)
	{
		throw new Exception('Not implemented');
		// @TODO guerrics: need to verify the same sheet hasn't already been loaded
		//$folder = $this->viewRoot .'/'. self::ASSETS_FOLDER;
		//DekiPlugin::includeCss($folder, $name.'.css');
	}

	/**
	 * Retrieves a variable from the view bag
	 * @note if the variable does not exist then $default is returned
	 *
	 * @param string $key
	 * @param mixed $default
	 * @return mixed
	 */
	protected function get($key, $default = null)
	{
		return isset($this->bag[$key]) ? $this->bag[$key] : $default;
	}

	/**
	 * Safely outputs a string variable for adhoc view variables
	 * 
	 * @param string $var
	 */
	protected function view($var) { echo htmlspecialchars($var); }

	/**
	 * Safely outputs a string contained in the view bag
	 * 
	 * @param string $key
	 */
	protected function text($key)  { echo htmlspecialchars($this->get($key)); }

	/**
	 * Outputs the raw contents of a view variable
	 * 
	 * @param string $key
	 */
	protected function html($key) { echo $this->get($key); }

	/**
	 * Outputs a localized string
	 * @see DekiMvc#msg()
	 */
	protected function msg($key)
	{
		$args = func_get_args();
		// make the inputs safe for html
		for ($i = 1, $i_m = count($args); $i < $i_m; $i++)
		{
			$args[$i] = htmlspecialchars($args[$i]);
		}
		
		return call_user_func_array('wfMsg', $args);
	}

	/**
	 * Tests if a key is set
	 * @note if the key points to an array and the array is empty then returns false
	 * 
	 * @param $key
	 * @return bool
	 */
	protected function has($key)
	{
		if (!isset($this->bag[$key]))
		{
			return false;
		}

		// empty check for arrays
		return !is_array($this->bag[$key]) || !empty($this->bag[$key]);
	}
}
