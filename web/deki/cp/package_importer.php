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


class PackageImporter extends DekiControlPanel
{
	// needed to determine the template folder
	protected $name = 'package_importer';
	protected $ui_prefix = 'cache-';
	
	const FOLDER_PUBLIC = 'public';
	const FOLDER_SEMIPUBLIC = 'semi-public';
	const FOLDER_PRIVATE = 'private';
	
	public function index() 
	{
		$this->executeAction('update');
	}
	
	protected function POST_update() 
	{
		switch ($this->Request->getVal('action'))
		{
			case 'update':
				$this->refreshPackages();
				break;
			
			case 'custom':
				$file = $this->Request->getFile('file');
				if ($file['error'] != 0)
				{
					DekiMessage::error($this->View->msg('PackageImporter.error.upload')); 
					return false;
				}
				
				// try to move the file to the package location
				if (!move_uploaded_file($file['tmp_name'], $this->getPackagePath(self::FOLDER_PUBLIC, $file['name'])))
				{
					DekiMessage::error($this->View->msg('PackageImporter.error.upload'));
					return false;
				}
				
				$this->refreshPackages();
				if ($this->Request->getBool('onetime'))
				{
					@unlink($this->getPackagePath(self::FOLDER_PUBLIC, $file['name']));
				}
				break;
			
			default:
		}	

		return true;
	}
		
	// main listing view
	public function update()
	{		
		$SiteProperties = DekiSiteProperties::getInstance(); 
		
		if ($this->Request->isPost() && $this->POST_update()) 
		{
			$this->Request->redirect($this->getUrl('/'));
			return;
		}
		
		// get all currently imported packages
		$packages = $SiteProperties->getAllPackages();
		if (!empty($packages)) 
		{
			$Table = new DomTable();
			$Table->setColWidths('60%', '20%', '20%');
			
			$Table->addRow();
			$Table->addHeading($this->View->msg('PackageImporter.col.name'));
			$Table->addHeading($this->View->msg('PackageImporter.col.created'));
			$Table->addHeading($this->View->msg('PackageImporter.col.imported'));
			
			foreach ($packages as $package => $PackageProperty) 
			{
				$X = new XArray($PackageProperty->getContent()); 
				$Tr = $Table->addRow();
				$Tr->addClass(file_exists($this->getPackagePath(self::FOLDER_PUBLIC, $package)) ? '': 'one-time'); 
				$Table->addCol($package); 
				$Table->addCol($X->getVal('package/date.created')); 
				$Table->addCol($PackageProperty->getDateModified());
			}
			$this->View->set('package-table', $Table->saveHtml()); 
		}
		
		$this->View->set('uploadable', is_writable($this->getPackagePath()));
		$this->View->set('package.path', $this->getPackagePath());
		
		$this->View->output();
	}

	protected function refreshPackages()
	{
		// if we're successfully, give a list of packages that have been updated
		$Result = DekiSite::refreshPackages();
		if (!$Result->handleResponse())
		{
			return false;
		}
		
		$packages = $Result->getAll('body/packages/package', null);
		if (!empty($packages))
		{
			foreach ($packages as $package) 
			{
				$X = new XArray($package);
				if ($X->getVal('status/@code') == 'ok')
				{
					$preserve = $X->getVal('@preserve-local') == 'true' ? '': $this->View->msg('PackageImporter.overwritten'); 
					DekiMessage::success($this->View->msg('PackageImporter.success', $X->getVal('name'), $preserve)); 
				}
			}
		}
		
		return true;
	}

	// helper function to locate packages on disk
	protected function getPackagePath($permissions = self::FOLDER_PUBLIC, $name = null) 
	{
		global $IP, $wgDekiSiteId; 
		return $IP . '/packages/' . $wgDekiSiteId . '/' . $permissions . (!is_null($name) ? '/' . $name : ''); 
	}
}

new PackageImporter();
