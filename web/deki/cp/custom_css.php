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

class CustomCSS extends DekiControlPanel
{
	// needed to determine the template folder
	protected $name = 'custom_css';
	protected $body_screenshot = 'screenshot_css_content.png';
	protected $template_screenshot = 'screenshot_css_template.png';
	
	public function index() {
		$this->executeAction('listing');
	}
	
	public function export() 
	{
		$SiteProperties = DekiSiteProperties::getInstance();
		$customcss = $SiteProperties->getCustomCss();
		header('Content-type: text/css');
		header('Content-disposition: attachment; filename=deki.custom.css');
		header('Content-length: '.strlen($customcss));
		echo($customcss);
		flush();
		return;
	}
	
	// main listing view
	public function listing()
	{
		$SiteProperties = DekiSiteProperties::getInstance();
			
		if ($this->Request->getVal('submit')) 
		{	
			do 
			{
				//
				$styles = $this->Request->getVal('css_template', null);
				if ($this->Request->getVal('submit') == 'upload') 
				{
					$file_type = $_FILES['css']['type'];
					$file_src = $_FILES['css']['tmp_name'];
					$file_error = $_FILES['css']['error'];
					if ($_FILES['css']['size'] == 0) 
					{
						// guerrics: should we dump the post?
						break;
					}
					if ($file_error > 0) 
					{
						DekiMessage::error($this->View->msg('CustomCSS.error.upload'));
					}
					
					// how specific should we get?
					if (!strexist('text/', $file_type)) 
					{
						DekiMessage::error($this->View->msg('CustomCSS.error.type', $file_type));
					}
					
					$css = wfGetFileContent($file_src);
					if ($this->Request->getVal('type') == 'append') 
					{
						$DekiUser = DekiUser::getCurrent();
						$styles .= PHP_EOL . PHP_EOL
							. '/* '
							. $this->View->msg('CustomCSS.append', $DekiUser->getName(), $_FILES['css']['name'], date('Y/m/d'))
							. ' */' . PHP_EOL
							. $css;
					}
					else if ($this->Request->getVal('type') == 'replace') 
					{
						$styles = $css;
					}
				}
				$SiteProperties->setCustomCss($styles);
				
				$Result = $SiteProperties->update();
				
				if ($Result->isSuccess())
				{
					DekiMessage::success($this->View->msg('CustomCSS.success'));
					$this->Request->redirect($this->getUrl('/'));
					return;
				}
				DekiMessage::error($Result->getError());
			} while(false); 
		}

		//todo: getting skinning variables shouldn't have to be done through the global scope
		global $wgActiveTemplate, $wgActiveSkin, $IP;
		$skinPath = $IP.'/skins/'.$wgActiveTemplate.'/'.$wgActiveSkin;
		$this->View->set('bodyScreenshotExists', is_file($skinPath.'/'.$this->body_screenshot));
		$this->View->set('templateScreenshotExists', is_file($skinPath.'/'.$this->template_screenshot));
		
		$this->View->set('skinname', $wgActiveTemplate);
		$this->View->set('skinstyle', $wgActiveSkin);
		
		$this->View->set('page.export', $this->getUrl('export'));
		$this->View->set('css.template', $SiteProperties->getCustomCss());
		
		$this->View->set('form.action', $this->getUrl('/'));
		
		//todo: these paths should be generated in a better manner
		$this->View->set('bodyScreenshot', '/skins/'.$wgActiveTemplate.'/'.$wgActiveSkin.'/'.$this->body_screenshot);
		$this->View->set('templateScreenshot', '/skins/'.$wgActiveTemplate.'/'.$wgActiveSkin.'/'.$this->template_screenshot);

		$this->View->output();
	}	
}

new CustomCSS();
