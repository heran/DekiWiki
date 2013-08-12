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

class DekiInstallerEnvironment extends DekiController
{
	public function isVM()
	{
		global $wgIsVM;
		return $wgIsVM;
	}
	
	public function isMSI()
	{
		global $wgIsMSI;
		return $wgIsMSI;
	}
	
	public function isWAMP()
	{
		global $wgIsWAMP;
		return $wgIsWAMP;
	}
	
	public function isWindows()
	{
		if (substr(php_uname(), 0, 7) == 'Windows') {
			return true;
		} else {
			return false;
		}
	}

	protected function setupEnvironment()
	{
		global $IP;
		
		error_reporting( E_ALL );
		//header( "Content-type: text/html; charset=utf-8" );
		@ini_set( "display_errors", true );
		
		//installation-specific includes
		require_once($IP.'/maintenance/install-utils.inc');
		require_once($IP.'/maintenance/install-helpers.inc');
		
		/***
		 * Check PHP version;
		 * We do this check independently from other package checking because it may throw errors on included files
		 * If failed, this script will stop here. 
		 */
		if (!install_php_version_checks()) 
		{
			// @TODO: handle this error case better?
			exit();
		}
		
		// initial our global configuration variables
		$this->setDefaultPaths();
		
		// set default paths to binaries
		$defaultConfigurations = array(
			'vm.php',
			'msi.php',
			'installtype.ami.php',
			'installtype.package.php',
			'installtype.vmesx.php',
			'installtype.wamp.php'
		);
		
		foreach ($defaultConfigurations as $file)
		{
			if (file_exists($IP . '/config/' . $file))
			{
				require_once($IP . '/config/' . $file);
			}
		}
		
		return true;
	}
	
	/**
	 * Determines if the product has been installed already
	 * @return bool
	 */
	protected function isInstalled()
	{
		// TODO: move this check? handle existing installations differently
		if ($this->isMSI()) 
		{
			return file_exists( "../LocalSettings.php" ) && (filesize( "../LocalSettings.php" ) > 0);
		}
		else 
		{
			return file_exists( "../LocalSettings.php");
		}
	}
	
	/**
	 * Initialize and auto-detect environment paths
	 * @return
	 */
	protected function setDefaultPaths()
	{
		global $wgPathIdentify, $wgPathConvert, $wgPathHTML2PS, $wgPathPS2PDF, $wgPathMono, $wgPathConf;
		global $wgAttachPath, $wgLucenePath, $wgPathPrince;
		global $IP;
		
		$wgPathIdentify = null;
		$wgPathConvert = null;
		$wgPathHTML2PS = null;
		$wgPathPS2PDF = null;
		$wgPathPrince = null;
		$wgPathMono = null;
		$wgAttachPath = $IP.'/attachments'; // absolute path used for API config key 'storage/fs/path'
		$wgLucenePath = $IP.'/bin/cache/luceneindex/$1';
		
		// any settings specific to a windows installs
		if ($this->isWindows()) 
		{
			$wgLucenePath = '\\bin\\cache\\luceneindex\\\$1';
		}
		
		$this->autoDetectDependencyPaths(false, true);
	}
	
	/**
	 * Determines auto-magical environment paths. Returns warning messages for dependencies
	 * that could not be found.
	 * 
	 * @param bool $generateMessages - if true, missing dependencies will have warning messages generated
	 * @param bool $setPaths - if true, the auto-detected paths will be set
	 * @return array - returns an array of warning messages (if $generateMessages == true)
	 */
	protected function autoDetectDependencyPaths($generateMessages = true, $setPaths = false)
	{
		$messages = array();
		
		// @TODO guerrics: move to controller member variable?
		$warn_functionality_settable_paths = array(
			array(
				'command' => 'identify',
				'configuration' => 'wgPathIdentify',
				'key' => 'ImageMagickIdentify'
			),
			array(
				'command' => 'convert',
				'configuration' => 'wgPathConvert',
				'key' => 'ImageMagickConvert'
			),
			array(
				'command' => 'prince',
				'configuration' => 'wgPathPrince',
				'key' => 'prince'
			),
			array(
				'command' => 'mono',
				'configuration' => 'wgPathMono',
				'key' => 'Mono'
			)
		);

		global $wgPathIdentify, $wgPathConvert, $wgPathPrince, $wgPathMono;
		// get paths
		$path_dirs = install_get_paths();
		
		$isWindows = $this->isWindows();
		foreach ($warn_functionality_settable_paths as $functionality) 
		{
			$command = $functionality['command'];
			$globalVariable = $functionality['configuration'];
			$key = $functionality['key'];
			
			$found = false;
			//special case: don't check for mono on windows
			if (strcmp($command, 'mono') == 0 && $isWindows) 
			{
				continue;
			}
			
			// if the paths are already set, first do a lookup there
			$predefinedpath = null;
			switch ($command) {
				case 'identify': 
					$predefinedpath = $wgPathIdentify;
				break;
				case 'convert': 
					$predefinedpath = $wgPathConvert;
				break;
				case 'prince': 
					$predefinedpath = $wgPathPrince;
				break;
			}
			if (!is_null($predefinedpath)) {
				if (file_exists($predefinedpath)) {
					$found = true;
				}	
			}
			
			// otherwise, let's try to be smart and figure out where the commands are, based on common standards across distros
			if (!$found) 
			{
				foreach ($path_dirs as $dir) 
				{
					$path  = $isWindows ? ($dir . '\\'.$command.'.exe') : ($dir . '/'.$command);
					if( file_exists( $path ) ) {
						if ($setPaths)
						{
							$$globalVariable = $path;
						}
						$found = true;
						break;
					}
				}
			}
			
			// only emit warnings 
			if (!$found && $generateMessages) 
			{
				$messages[] = $this->error(wfMsg('Page.Install.check-'.strtolower($key).'-warn'), 'warn');
			}
		}
		
		return $generateMessages ? $messages : null;
	}

	/*
	 * Helper methods below
	 */
	protected function initializeObjects()
	{
		$this->Request = DekiRequest::getInstance();
		
		// create a new view
		$this->View = $this->createView($this->viewRoot, $this->templateName);
	}
		
	/**
	 * Duplicates the DekiController#createView functionality in order to provide a custom view class
	 * @TODO: fix DekiMvc so this method does not have to be altered in this way
	 * 
	 * @param string $viewRoot
	 * @param string $templateName
	 * @return DekiInstallerView
	 */
	protected function &createView($viewRoot, $templateName = null)
	{
		// create the view
		$View = new DekiInstallerView($viewRoot);
		if (!is_null($templateName))
		{
			$View->setTemplateFile('/' . $templateName . '.php');
		}
		
		// register common view variables
		$View->set('controller.name', $this->name);
		
		// needed for renderAction()
		$this->setupView($View);

		return $View;
	}
	
	protected function setupView($View)
	{
		// should the configuration step be displayed?
		$View->set('env.showConfiguration', (!$this->isVM() && !$this->isWAMP()) || DekiMvcConfig::DEBUG);
		
		// hide the advanced configuration section for MSI or WAMP installs
		$View->set('env.showAdvancedConfiguration', (!$this->isMSI() && !$this->isWAMP()));
		
		// MT-9709 Remove TCS from MindTouch download pkgs
		$View->set('showCommercial', false);
	}
	
	/**
	 * Method generates environment/dependency error messages to be displayed on render
	 * 
	 * @param string $message
	 * @param enum $type {error, warn, fatal}
	 * @param bool $isHtml - is the message html?
	 * @return array
	 */
	protected function error($message, $type = 'error', $isHtml = false)
	{
		return array(
			'type' => $type,
			'contents' => $message,
			'html' => $isHtml
		);
	}
	
	protected function inputError($name, $message)
	{
		$error = $this->error($message);
		$error['input'] = $name;
		return $error;
	}
	
	/**
	 * Method generates fatal environment erros messages; should kill execution immediately?
	 * 
	 * @param string $message
	 * @param enum $type {error, warn, fatal}
	 * @param bool $isHtml - is the message html?
	 * @return array
	 */
	protected function fatalError($message, $isHtml = false)
	{
		return array(
			$this->error($message, 'fatal', $isHtml)
		);
	}

	/**
	 * Generate an informational message during the install process
	 * 
	 * @param string $message
	 * @param bool $isHtml - is the message html?
	 * @return array
	 */
	protected function message($message, $isHtml = false)
	{
		return $this->error($message, 'info', $isHtml);
	}
}
