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


/*
 * Only one controller is allowed per page load
 */
class DekiController
{
	// needed to determine the template folder
	protected $name = 'controller';

	// common objects
	protected $Request = null;
	protected $Plug = null;
	protected $View = null;


	public function __construct()
	{
		// setup the controller
		$this->initialize();
	}
	
	private function initialize()
	{
		global $wgAdminPlug, $wgAdminController;
		// set the active controller
		$wgAdminController = $this;

		$this->Request = DekiRequest::getInstance();
		// parse the incoming request
		$action = $this->Request->getAction();
		// attach the plug
		$this->Plug = $wgAdminPlug;

		$this->View = $this->createView();

		if ($this->initializeAction($action))
		{
			$this->executeAction($action, $this->Request->getParams());
		}
	}
	
	/**
	 * Convenience method for linking to other controller actions
	 * Wraps the DekiRequest object's getLocalUrl method
	 *
	 * @param bool $preserve - determines if all the current GET variables should be sent down with the request
	 */
	protected final function getUrl($params = '', $get = array(), $preserve = false)
	{
		if ($preserve)
		{
			foreach ($_GET as $key => $val)
			{
				if (!isset($get[$key]))
				{
					$get[$key] = $val;
				}
			}
		}

		return $this->Request->getLocalUrl($this->name, $params, $get);
	}

	protected function &createView($withTemplate = true)
	{
		// create the view
		$View = new DekiView(Config::$APP_ROOT . '/templates');
		if ($withTemplate)
		{
			$View->setTemplateFile('/' . Config::TEMPLATE_NAME . '.php');
		}

		// register commone view variables
		$View->set('controller.name', $this->name);

		return $View;
	}

	/**
	 * Method is call before the action is executed
	 * @return bool - if true the action is executed
	 */
	protected function initializeAction($action)
	{
		// determine if the action can be executed, public only
		try
		{
			$Class = new ReflectionClass(get_class($this));
			$method = $Class->getMethod($action);
			if (!is_callable(array($this, $action)) || !$method->isPublic())
			{
				throw new Exception('Method not found');
			}
		}
		catch (Exception $e)
		{
			// action/request handler not found
			DekiRequest::error('404');
			exit();
		}
		
		return true; 
	}

	// moved to a separate function so you can call another action from an action
	// e.g. index() { $this->executeAction('somethingelese'); }
	protected final function executeAction($action, $params = array())
	{
		// setup the view file
		$this->View->setViewFile('/' . $this->name . '/' . $action . '.php');
		// let the view know what action is being executed
		$this->View->set('controller.action', $action);

		call_user_func_array(array($this, $action), $params);
	}
	
	/**
	 * Function is a little hacky but will return the output of an action
	 * In the future possibly refactor how the view is rendered, e.g.
	 * don't explicitly call $View->output() in each method allowing the 
	 * controller to delegate the output
	 *
	 * @see executeAction
	 */
	protected final function &renderAction($action, $params = array())
	{
		// save the current view
		$CurrentView = $this->View;
		// create a new view for the action to render
		$this->View = $this->createView(false);

		// buffer to grab the contents
		ob_start();
		$this->executeAction($action, $params);
		// grab the contents of the rendering
		$contents = ob_get_contents();
		ob_end_clean();
		
		unset($this->View);
		// set the view back
		$this->View = $CurrentView;

		return $contents;
	}
}


class DekiView
{
	private $variables = array();
	private $cssfiles = array();
	private $jsfiles = array();
	private $templateDirectory = null;
	private $templateFile = null;
	private $viewFile = null;
	// determines if the view file is being rendered & thus buffered
	private $rendering = false;

	public function __construct($templateDirectory)
	{
		$this->templateDirectory = $templateDirectory;
	}

	public function setTemplateFile($file) { $this->templateFile = $this->templateDirectory . $file; }
	public function setViewFile($file) { $this->viewFile = $this->templateDirectory . $file; }
	public function isRendering() { return $this->rendering; }

	public function includeCss($file) 
	{ 
		if (in_array($file, $this->cssfiles)) 
		{
			return;
		}
		$this->cssfiles[] = $file;
	}
	public function getCssIncludes() { return $this->cssfiles; }
	
	public function includeJavascript($file) 
	{ 
		if (in_array($file, $this->jsfiles)) 
		{
			return;
		}
		$this->jsfiles[] = $file;
	}
	public function getJavascriptIncludes() { return $this->jsfiles; }
	
	// methods for setting template variables
	public function set($key, $value)		{ $this->variables[$key] = $value; }
	// sets a reference
	public function setRef($key, &$value)	{ $this->variables[$key] = $value; }


	// returns a variable, might be a reference
	public function get($key, $default = null)
	{
		return isset($this->variables[$key]) ? $this->variables[$key] : $default;
	}

	public function has($key)
	{
		$val = $this->get($key);
		return !is_null($val) && !empty($val);
	}

	public function html($key) { echo $this->get($key, ''); }
	public function text($key) { echo htmlspecialchars($this->get($key, '')); }

	// wraps wfMsg until the admin localization is done
	public function msg()
	{
		$args = func_get_args();
		// make the inputs safe for html
		for ($i = 1, $i_m = count($args); $i < $i_m; $i++)
		{
			$args[$i] = htmlspecialchars($args[$i]);
		}
		
		return call_user_func_array('wfMsg', $args);
	}
	
	// wraps wfMsg until the admin localization is done
	// does not do HTML encoding of parameters
	public function msgRaw()
	{
		$args = func_get_args();
		return call_user_func_array('wfMsg', $args);
	}
	
	

	public function output()
	{
		if (!is_null($this->templateFile))
		{
			// create a variable for the view's contents
			$this->setRef('view.contents', $this->render(true));
			// render the template
			require($this->templateFile);
		}
		else
		{
			$this->render();
		}
	}
	
	protected function &render($return = false)
	{
		$contents = '';
		if ($return)
		{
			$this->rendering = true;
			ob_start();
			require($this->viewFile);
			$contents = ob_get_contents();
			ob_end_clean();
			$this->rendering = false;
		}
		else
		{
			require($this->viewFile);
		}

		return $contents;
	}

}
