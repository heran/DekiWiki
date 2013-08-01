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

require_once( '../../skins/common/popups/kaltura/kaltura_settings.php');
require_once( '../../skins/common/popups/kaltura/KalturaClient.php');
require_once( '../../skins/common/popups/kaltura/kaltura_logger.php');
require_once( '../../skins/common/popups/kaltura/kaltura_helpers.php');

class KalturaVideo extends DekiControlPanel
{
	// needed to determine the template folder
	protected $name = 'kaltura_video';
	
	public function index() {
		$this->executeAction('form');
	}
	// main listing view
	public function form()
	{
		if ($this->Request->isPost()) 
		{
			if ($this->Request->getVal('submit') != '') 
			{
				$name = $this->Request->getVal('kaltura-name');
				$company = $this->Request->getVal('kaltura-company');
				$email = $this->Request->getVal('kaltura-email');
				$phone = $this->Request->getVal('kaltura-phone');
				$describe = $this->Request->getVal('kaltura-describe');
				$url = $this->Request->getVal('kaltura-url');
				$content = $this->Request->getVal('kaltura-content');
				$adult = $this->Request->getVal('kaltura-adult');
				$descrption = $this->Request->getVal('kaltura-description');
				$terms = $this->Request->getVal('accept_terms');
				
				if (strlen($name) == 0 || strlen($email) == 0 || strlen($describe) == 0 || 
					count($content) == 0 || strlen($adult) == 0 || strlen($descrption) == 0 || $terms != "on")
				{
					DekiMessage::error($this->View->msg('KalturaVideo.form.registration.mandatory'));
					$keys = array('kaltura-description' => $descrption, 'kaltura-adult' => $adult, 'kaltura-content' => $content,
					'kaltura-url' => $url, 'kaltura-describe' => $describe, 'kaltura-phone' => $phone, 'kaltura-email' => $email,
					'kaltura-company' => $company, 'kaltura-name' => $name);
					$this->Request->redirect($this->getUrl('/', $keys));
					return;
				}
				$contentCategory = '';
				foreach($content as $key => $val)
				{
					if (empty($contentCategory))
					{
						$contentCategory = $val;						
					}
					else
					{
						$contentCategory .= ',' . $val;						
					}
				}
				try
                {
				   wfSetConfig('kaltura/uiconf/uploader', KalturaSettings_CW_UICONF_ID);
				   wfSetConfig('kaltura/uiconf/player-mix', KalturaSettings_PLAY_MIX_UICONF_ID);
				   wfSetConfig('kaltura/uiconf/player-nomix', KalturaSettings_PLAY_NOMIX_UICONF_ID);
				   wfSetConfig('kaltura/uiconf/editor', KalturaSettings_SE_UICONF_ID);
				   wfSetConfig('kaltura/server-uri',KalturaSettings_SERVER_URL);
				   wfSetConfig('kaltura/enabled',1);

                   KalturaHelpers::register($name, $email, $secret, $adminSecret, $partner, $phone, 
		 					    $description, $describe, $url, $contentCategory,
							    ($adult=='yes' ? true : false));

				   wfSetConfig('kaltura/user', $name);
				   wfSetConfig('kaltura/secret', $secret);
				   wfSetConfig('kaltura/secret-admin', $adminSecret);
				   wfSetConfig('kaltura/partner-id', $partner);

				   DekiMessage::success($this->View->msg('KalturaVideo.form.registration.success'));
				   DekiMessage::info($this->View->msg('KalturaVideo.form.registration.restart'));
                 }
                 catch(Exception $exp)
                 {
				  if (stristr($exp->getMessage(), $email) != FALSE)
				  {
					  DekiMessage::error($this->View->msg('KalturaVideo.form.registration.failure.email'));
				  }
				  else
				  {
					  DekiMessage::error($this->View->msg('KalturaVideo.form.registration.failure.unknown'). $exp->getMessage());
				  }
                 }
                 wfSaveConfig();					
			}
			
			$this->Request->redirect($this->getUrl('/'));
			return;
		}
		
		$this->View->set('form.action', $this->getUrl('/'));
		
		$User = DekiUser::getCurrent();
		$email = $User->getEmail(); 

		$this->View->set('form.email', $email);
			
		$this->View->output();
	}
}


new KalturaVideo();
