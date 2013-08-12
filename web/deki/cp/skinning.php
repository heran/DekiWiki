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
 
define('DEKI_ADMIN', true);
require_once('index.php');

class Skinning extends DekiControlPanel
{
	// needed to determine the template folder
	protected $name = 'skinning';
	private $skinCssCache = array();
	const SMALL_SCREENSHOT = 'screenshot_s.png';
	const MEDIUM_SCREENSHOT = 'screenshot_m.png';
	const LARGE_SCREENSHOT = 'screenshot_l.png';

	
	public function index() 
	{
		$this->executeAction('listing');
	}
	// main listing view
	public function listing()
	{
		
		global $wgActiveSkin, $wgActiveTemplate;
		
		if ($this->Request->isPost()) 
		{
			switch ($this->Request->getVal('submit')) 
			{
				// logo: set default
				case 'default': 
					if ($this->clearLogo()) 
					{
						DekiMessage::success($this->View->msg('Skinning.success.logo.reset'));	
					}
				break;
				// logo: add new
				case 'upload':
					$this->uploadLogo();
				break;
				// skin: set new skin
				case 'skin': 
					list($skin, $style) = explode('|', $this->Request->getVal('skinstyle'));
					if ($this->setSkin($skin, $style))
					{
						DekiMessage::success($this->View->msg('Skinning.success.skin', $skin, $style, '/'));	 
					}
					else
					{
						DekiMessage::error($this->View->msg('Skinning.error.skin', $skin, $style));
					}
				break;
			}
			
			$this->Request->redirect($this->getUrl('/'));
			return;
		}
		$skins = $this->getSkins();
		$styles = array();
		foreach ($skins as $name => $dir) 
		{
			$styles[$name] = $this->getSkinStyles($dir);
		}
		
		$screenshots = array();
				
		//loop through and grab the previews
		foreach ($styles as $skin => $styles) 
		{
			foreach ($styles as $style => $stylepath) 
			{
				// TODO: this url should be built by the request object somehow
				$small = Config::WEB_ROOT . '/assets/images/skinning_noskin.png';
				if (file_exists($stylepath .'/'. self::SMALL_SCREENSHOT)) 
				{
					$small = '/skins/'. $skin .'/'. $style .'/'. self::SMALL_SCREENSHOT;
				}

				$screenshots[] = array(
					'selected' => $wgActiveSkin == $style && $wgActiveTemplate == $skin, 
					'small' => $small,
					'medium' => '/skins/'. $skin .'/'. $style .'/'. self::MEDIUM_SCREENSHOT,
					'large' => '/skins/'. $skin .'/'. $style .'/'. self::LARGE_SCREENSHOT,
					'skin' => $skin,
					'style' => $style,
					'class' => $this->getSkinCssClass($skins[$skin])
				);
			}
		}
		
		global $wgLogo;
		$this->View->set('screenshots', $screenshots);
		$this->View->set('logo', '<img src="'.$wgLogo.'" />');
		$this->View->set('logo-maxwidth', wfGetConfig('ui/logo-maxwidth', 280));
		$this->View->set('logo-maxheight', wfGetConfig('ui/logo-maxheight', 72));
		$this->View->set('form.action', $this->getUrl('/'));
		$this->View->set('url.css', $this->Request->getLocalUrl('custom_css'));
		$this->View->set('url.html', $this->Request->getLocalUrl('custom_html'));
		$this->View->output();
	}	
	private function clearLogo() 
	{
		$Result = $this->Plug->At('site', 'logo')->Delete();
		return $Result->handleResponse();
	}
	private function setSkin($skin, $style) 
	{
		wfSetConfig('ui/template', $skin);
		wfSetConfig('ui/skin', $style);

		return wfSaveConfig();
	}

	private function uploadLogo() 
	{
		$file = $this->Request->getFile('logo');
		// Bugfix #5025: Skinning: Uploading an empty logo kills php
		$size = isset($file['size']) ? $file['size'] : 0;
		if ($size <= 0)
		{
			DekiMessage::error($this->View->msg('Skinning.error.nologo'));
			return false;
		}

		$fileName = $file['name'];
		$fileType = $file['type'];
		$fileTemp = $file['tmp_name'];
		if ($fileType == 'application/octet-stream') 
		{
			$fileType = mime_content_type($fileTemp);
		}

		$Result = $this->Plug->At('site', 'logo')->PutFile($fileTemp, $fileType);

		if ($Result->handleResponse())
		{
			DekiMessage::success($this->View->msg('Skinning.success.logo'));
			return true;
		}
		else if ($Result->is(400))
		{
			DekiMessage::error($Result->getError());
		}

		return false;
	}

	private function getSkins() 
	{
		global $wgStyleDirectory;
		$dirs = wfGetDirectories($wgStyleDirectory, array('.svn', 'local', 'common'));
		if (empty($dirs)) 
		{
		 	return array();
		}
		foreach ($dirs as $key => $dir) 
		{
			$directoryFiles = wfGetFileNames($dir, true);
			
			// sees if templatename.php exists - if not, it's most likely not a skin
			if (!array_key_exists(strtolower(basename($dir)).'.php', $directoryFiles)) 
			{
				unset($dirs[$key]);
			}
		}
		ksort($dirs);
		reset($dirs);
		return $dirs;
	}
	
	private function getSkinCssClass($dir)
	{
		if (!isset($this->skinCssCache[$dir]))
		{
			$directoryFiles = wfGetFileNames($dir, true);
			$style = '';

			if (array_key_exists('.obsolete', $directoryFiles)){
				$style = 'skin-obsolete';
			}
			else if (array_key_exists('.beta', $directoryFiles)){
				$style = 'skin-beta';
			}
			else {
				$style = 'skin-default';
			}

			$this->skinCssCache[$dir] = $style;
		}
		
		return $this->skinCssCache[$dir];
	}
	
	private function getSkinStyles($path) 
	{
		$skins = wfGetDirectories($path, array('.svn'));
		ksort($skins);
		reset($skins);
		return $skins;
	}
}

new Skinning();
