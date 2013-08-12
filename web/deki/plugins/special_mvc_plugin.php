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

abstract class SpecialMvcPlugin extends SpecialPagePlugin
{
	const DEFAULT_ACTION = 'index';

	/**
	 * @var DekiRequest
	 */
	protected $Request;

	/**
	 * @var ReflectionClass
	 */
	protected $Reflection;

	/**
	 * @var DekiPluginView
	 */
	protected $View;

	/**
	 * @var string
	 */
	protected $html;
	protected $subhtml;
	protected $action;
	protected $directory;

	public function __construct(
		$requestedName = null,
		$workingFileName = null,
		$checkAccess = false,
		&$pageTitle,
		&$html,
		&$subhtml
	)
	{
		global $IP, $wgDekiPluginPath;
		parent::__construct($requestedName, $workingFileName, $checkAccess);
		$pageTitle = $this->getPageTitle();
		$this->directory = $workingFileName;
		$this->html = &$html;
		$this->subhtml = &$subhtml;
		$this->Request = DekiRequest::getInstance();
		$this->Reflection = new ReflectionClass(get_class($this));
	}

	/**
	 * Main execution method. Call without an action to retrieve action from url
	 * @NOTE performs magic method marshalling based on request type. i.e. POST requests goto methodPost
	 *
	 * @param (optional) string $action - be careful if calling from index() without this arg
	 * @return
	 */
	public function requestAction($action = null)
	{
		$requestedAction = $this->Request->getVal('action');
		if (empty($requestedAction) && empty($action))
		{
			$this->action = self::DEFAULT_ACTION;
		}
		else if (empty($requestedAction) && !empty($action))
		{
			$this->action = $action;
		}
		else if (!empty($requestedAction))
		{
			$this->action = $requestedAction;
		}
		$this->View = new DekiPluginView($this->directory, $this->action);
		$postAction = $this->action . 'Post';

		// check if a post handler exists
		if ($this->Request->isPost() && $this->canProcess($postAction, false))
		{
			$result = $this->executeAction($postAction);
			if ($result === true)
			{
				return $result;
			}
		}

		if ($this->canProcess($this->action))
		{
			$this->set('mvc.action', $this->action);
			return $this->executeAction($this->action);
		}
		else
		{
			SpecialPagePlugin::redirectHome();
		}
	}

	/**
	 * Executes an action
	 *
	 * @param $action
	 * @return
	 */
	protected function executeAction($action)
	{
		if($this->canProcess($action, false))
		{
			return call_user_func_array(array($this, $action), array());
		}
	}

	/**
	 * Calls a method and returns the rendered view
	 *
	 * @param string $action
	 * @return string
	 */
	protected function &renderAction($action)
	{
		$html = null;
		if($this->canProcess($action, false))
		{
			$Special = clone $this;
			$Special->requestAction($action);
			$html = $Special->renderView();
		}
		return $html;
	}

	/**
	 * Render view, load default assets, and return html
	 *
	 * @return string
	 */
	protected function renderView()
	{
		$assets = $this->directory . '/assets';
		$css = $this->action . '.css';
		if($this->assetExists($css))
		{
			$this->includeCss($assets, $css);
		}
		$js = $this->action . '.js';
		if($this->assetExists($js))
		{
			$this->includeJavascript($assets, $js);
		}
		return $this->View->render();
	}

	/**
	 * Render partial view and return html
	 *
	 * @param string $partial
	 * @return DekiPluginView
	 */
	protected function getPartial($partial)
	{
		return new DekiPluginView($this->directory, $partial, 'partials');
	}

	/**
	 * Determines if an action can be processed based on existence & visibility.
	 *
	 * @param string $action
	 * @param bool $requirePublic - if true, the method must be public to process
	 * @return bool
	 */
	protected function canProcess($action, $requirePublic = true)
	{
		try
		{
			$method = $this->Reflection->getMethod($action);
			if ( !is_callable(array($this, $action)) || ($requirePublic && !$method->isPublic()) )
			{
				return false;
			}
			return true;
		}
		catch (Exception $e)
		{
			return false;
		}
	}

	/**
	 * Convenience method for generating urls to other page actions
	 *
	 * @param string $action
	 * @param optional array $get
	 * @return string - full url to the action
	 */
	protected function getUrl($action = null, $get = array())
	{
		$Title = $this->getTitle();
		if (!empty($action))
		{
			$get['action'] = $action;
		}

		$queryString = http_build_query($get, null, '&');
		return ($queryString) ? $Title->getFullURL($queryString) : $Title->getFullURL();
	}

	/**
	 * Set a $value for the view by $key
	 *
	 * @param string $key
	 * @param mixed $val
	 */
	protected function set($key, $val)
	{
		return $this->View->set($key, $val);
	}

	/**
	 * Set a reference to a $value for the view by $key
	 *
	 * @param string $key
	 * @param mixed $val
	 */
	protected function setRef($key, &$val)
	{
		$this->View->setRef($key, $val);
	}

	/**
	 * Wraps the special page css inclusion to direct into assets folder
	 *
	 * @param string $file - name of the css file to include, should be in the same folder as the plugin
	 * @param bool $external - if true, the specified path is taken as web accessible
	 */
	protected function includeSpecialCss($file, $external = false)
	{
		if (!$external)
		{
			$file = 'assets/'. $file;
		}
		parent::includeSpecialCss($file, $external);
	}

	/**
	 * Wraps the special page javascript inclusion to direct into assets folder
	 *
	 * @param string $file - name of the js file to include, should be in the same folder as the plugin
	 * @param bool $external - if true, the specified path is taken as web accessible
	 */
	protected function includeSpecialJavascript($file, $external = false)
	{
		if (!$external)
		{
			$file = 'assets/'. $file;
		}
		parent::includeSpecialJavascript($file, $external);
	}

	/**
	 * @param string $asset
	 * @return bool
	 */
	protected function assetExists($asset)
	{
		global $IP;
		return file_exists($IP . $this->getWebPath() . '/assets/' . $asset);
	}

	public function __destruct()
	{
		if($this->canProcess($this->action, false))  { $this->html = $this->renderView(); }
	}
}