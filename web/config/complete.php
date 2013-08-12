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

define('DEKI_MVC', true);
require_once('index.php');

class CompleteController extends DekiInstaller
{
	// needed to determine the template folder
	protected $name = 'complete';
	
	/**
	 * Installation completed. Might contain error messages. i.e. from license generation
	 * @return
	 */
	public function index()
	{
		// need to global $conf for the view
		global $IP, $conf;
		
		if (!isset($_SESSION[self::COMPLETE_KEY]))
		{
			$this->Request->redirect($this->Request->getLocalUrl('install'));
			return;
		}
		
		// grab completion state from the session
		$messages = isset($_SESSION[self::COMPLETE_KEY]['messages']) ? $_SESSION[self::COMPLETE_KEY]['messages'] : array();
		$conf = isset($_SESSION[self::COMPLETE_KEY]['conf']) ? unserialize($_SESSION[self::COMPLETE_KEY]['conf']) : new ConfigData();
		$completed = isset($_SESSION[self::COMPLETE_KEY]['completed']) ? $_SESSION[self::COMPLETE_KEY]['completed'] : (time() - 4000);
		
		// expire the session after an hour so the user can refresh
		if ($completed + 3600 < time())
		{
			// remove the completion state
			unset($_SESSION[self::COMPLETE_KEY]);
		}
		
		// set the output file name
		$confOutputFile = $this->isWindows() ? FILE_HOST_WIN_BAT : FILE_HOST_XML;
		
	    if (!$this->isWindows()) 
	    {
		    $sep = "/";
		    $dekiConfigDir = "/etc/dekiwiki";
	    } 
	    else 
	    {
	        $sep = "\\";
	        $dekiConfigDir = "C:\dekiwiki";
	    }
	    	    
		if ((!$this->isVM() && !$this->isMSI() && !$this->isWAMP()) || DekiMvcConfig::DEBUG)
		{
			$commands = array();
			
			if (!$this->isWindows())
			{
				$commands[] = "cd $IP/config";
				$commands[] = "mkdir $dekiConfigDir";
				$commands[] = 'cp -p '.$confOutputFile." $dekiConfigDir";
				$commands[] = 'cp -p '.FILE_STARTUP_XML." $dekiConfigDir";
				$commands[] = 'cp -p '.FILE_LOCALSETTINGS." $IP/";
				$commands[] = "/etc/init.d/dekiwiki start";
				$commands[] = 'rm '.$confOutputFile;
				$commands[] = 'rm '.FILE_STARTUP_XML;
				$commands[] = 'rm '.FILE_LOCALSETTINGS;
			}
			else
			{
				$commands[] = "cd ".$IP.$sep."config";
				$commands[] = "mkdir ".$dekiConfigDir;
				$commands[] = "copy ".FILE_STARTUP_XML." ".$dekiConfigDir;
				$commands[] = "copy ".FILE_LOCALSETTINGS." ".$IP.$sep;
				$commands[] = "copy ".FILE_HOST_WIN_BAT." ".$IP.$sep."bin";
				$commands[] = "cd ".$IP.$sep."bin";
				$commands[] = FILE_HOST_WIN_BAT;
			}
			$this->View->set('manualConfiguration', implode("\n", $commands));
		}

		// only vm's can use update wiki
		$this->View->set('suggestUpdateWiki', $this->isVM());
		
		// output any messages generated during the installation
		if (!empty($messages))
		{
			$this->View->set('complete.messages', $messages);
		}
		
		$this->View->set('isCore', $conf->isCore());
		$this->View->set('licenseGenerated', isset($conf->LicenseGenerated) ? $conf->LicenseGenerated : false);
		
		$this->View->set('href.trial', DekiMvcConfig::LICENSE_GENERATOR_URL);
		$this->View->set('href.trial.full', DekiMvcConfig::LICENSE_GENERATOR_URL . '?productkey=' . $conf->getProductKey());
		$this->View->set('productkey', $conf->getProductKey());
		
		$this->View->set('admin.email', isset($conf->SysopEmail) ? $conf->getSysopEmail() : 'MISSING SYSOP EMAIL');
		$this->View->set('admin.username', isset($conf->SysopName) ? $conf->getSysopName() : 'MISSING SYSOP NAME');
		$this->View->set('admin.password', isset($conf->SysopPass) ? $conf->SysopPass : 'MISSING SYSOP PASSWORD');
		$this->View->output();
	}
}

// Initialize the install controller
new CompleteController(array(
	'view.root' => DekiMvcConfig::$APP_ROOT . '/' . DekiMvcConfig::VIEWS_FOLDER,
	'template.name' => 'complete'
));
