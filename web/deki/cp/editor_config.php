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

class EditorConfig extends DekiControlPanel
{
	// needed to determine the template folder
	protected $name = 'editor_config';
	
	public function index() {
		$this->executeAction('listing');
	}
	// main listing view
	public function listing()
	{
		global $wgEditorToolbarSets;
		$toolbarsets = array();
		foreach ($wgEditorToolbarSets as $value)
		{
			$toolbarsets[$value] = $value;
		}
		
		$SiteProperties = DekiSiteProperties::getInstance();
		
		if ($this->Request->isPost()) 
		{
			
			$fckConfig = $this->Request->getVal('config');
			$SiteProperties->setFckConfig($fckConfig);
			$Result = $SiteProperties->update();
			
			$fckConfigToolbar = $this->Request->getVal('toolbar'); 
			if (in_array($fckConfigToolbar, $toolbarsets)) 
			{
				wfSetConfig('editor/toolbar', $fckConfigToolbar == 'Default' ? null : $fckConfigToolbar);
				wfSaveConfig();
				DekiMessage::success($this->View->msg('EditorConfig.success.toolbar', $fckConfigToolbar));
			}
			
			if ($Result->handleResponse())
			{
				DekiMessage::success($this->View->msg('EditorConfig.success.saved'));
				$this->Request->redirect($this->getUrl());
				return;				
			}
		}
		
		// read the user config from site properties
		$fckConfig = $SiteProperties->getFckConfig();
		
		$this->View->set('form.select', DekiForm::multipleInput('select', 'toolbar', $toolbarsets, wfGetConfig('editor/toolbar', 'Default')));
		$this->View->set('form.action', $this->getUrl('/'));
		$this->View->set('form.config', $fckConfig);
		
		$this->View->set('editor.atdEnabled', wfGetConfig('ui/editor/atd-enabled', ATD_DEFAULT_STATUS));
		$this->View->output();
	}	
}

new EditorConfig();
