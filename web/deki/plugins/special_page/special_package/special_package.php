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

if (defined('MINDTOUCH_DEKI'))
{
	DekiPlugin::registerHook(Hooks::SPECIAL_PACKAGE, array('SpecialPackage', 'create'));
}

class SpecialPackage extends SpecialPagePlugin
{
	protected $pageName = 'Package';
	
	public static function create($pageName, &$pageTitle, &$html)
	{
		$Special = new self($pageName, basename(__FILE__, '.php'));
		
		// set the page title
		$pageTitle = $Special->getPageTitle();
		$html = $Special->output();
	}
	
	public function &output()
	{
		global $wgTitle;
		$Request = DekiRequest::getInstance(); 
		if ($Request->isPost()) 
		{
			$Plug = DekiPlug::getInstance();
			if ($Request->getVal('fromurl'))
			{
				$Result = $Plug->At('package', 'import')->With('uri', $Request->getVal('url'))->Post(); 
			}
			if ($Request->getVal('fromfile'))
			{
				$file = $Request->getFile('package'); 
				$Result = $Plug->At('package', 'import')->Post(file_get_contents($file['tmp_name'])); 
			}
			if ($Result->getStatus() == '200') 
			{
				DekiMessage::success('Package successfully installed.');	
			}
		}
		$html = '<form method="post" action="'.$wgTitle->getLocalUrl().'" enctype="multipart/form-data">'
			.'<legend><title>Upload Package</title>'
			.'<p>Upload package: '.DekiForm::singleInput('file', 'package').' '.DekiForm::singleInput('button', 'fromfile', 'submit', array(), 'Install Package')
			.'</legend></form>';
			
		$html.= '<form method="post" action="'.$wgTitle->getLocalUrl().'">'
			.'<legend><title>Get Package from URL</title>'
			.'<p>From URL: '.DekiForm::singleInput('text', 'url').' '.DekiForm::singleInput('button', 'fromurl', 'submit', array(), 'Install Package')
			.'</legend></form>';
		return $html;
	}
}
