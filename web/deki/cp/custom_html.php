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


class CustomHTML extends DekiControlPanel
{
	// needed to determine the template folder
	protected $name = 'custom_html';
	
	public function index()
	{	
		$this->executeAction('listing');	
	}
	
	// main listing view
	public function listing()
	{
		global $wgActiveTemplate, $wgActiveSkin, $IP;
		// create an instance of the site property bag
		$SiteProperties = DekiSiteProperties::getInstance();
		
		if ($this->Request->isPost()) 
		{
			$custom = $this->Request->getArray('custom');
			foreach ($custom as $key => $html)
			{
				$region = substr($key, 4); // key is like htmlXXX
				$SiteProperties->setCustomHtml($html, (int)$region);
			}
			
			$Result = $SiteProperties->update();
			if ($Result->isSuccess())
			{
				DekiMessage::success($this->View->msg('CustomHTML.success'));
				$this->Request->redirect($this->getUrl('/'));
				return;				
			}
			else
			{
				DekiMessage::error($Result->getError());
			}
		}

		$count = $this->getAreaCount($wgActiveTemplate);
		$this->View->set('skinname', $wgActiveTemplate);
		$this->View->set('skinstyle', $wgActiveSkin);
		$this->View->set('skinpath', $IP.'/skins/'.$wgActiveTemplate.'/'.$wgActiveSkin);
		$this->View->set('skindir', '/skins/'.$wgActiveTemplate.'/'.$wgActiveSkin);
		$this->View->set('count', $count);
		$this->View->set('form.action', $this->getUrl('/'));
		$this->View->set('changeskin.form.action', $this->Request->getLocalUrl('skinning'));

		// set the region contents
		for ($i = 1; $i <= $count; $i++) 
		{
			$this->View->set('html'.$i, $SiteProperties->getCustomHtml($i));
		}	

		$this->View->output();
	}
	
	//todo: this needs to eventually become a property of the skin itself instead of floating in a $wg variable
	public function getAreaCount($templateName) 
	{
		$templateClass = 'Skin'.$templateName;
		if (!class_exists($templateClass))
		{
			// try to include the template class file
			// suppress errors if the template file can not be loaded
			if (!defined('MEDIAWIKI'))
			{
				// required for older skins
				define('MEDIAWIKI', true);
			}
			
			@include_once(Config::$DEKI_ROOT.'/skins/'.$templateName.'/'.$templateName.'.php');
		}

		// checks if the template class is found and if a number of regions has been set, otherwise 0
		return defined($templateClass.'::HTML_AREAS') ? constant($templateClass.'::HTML_AREAS') : 0;
	}
}

new CustomHTML();
