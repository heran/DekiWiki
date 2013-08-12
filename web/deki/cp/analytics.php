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


class Analytics extends DekiControlPanel
{
	// needed to determine the template folder
	protected $name = 'analytics';
	
	public function index() {
		$this->executeAction('listing');
	}
	// main listing view
	public function listing()
	{
		if ($this->Request->getVal('submit')) 
		{
			$akey = $this->Request->getVal('analyticskey', null);
			wfSetConfig('ui/analytics-key', $akey);
			wfSaveConfig();
			$this->Request->redirect($this->getUrl('/'));
			if (is_null($akey)) 
			{
				DekiMessage::success($this->View->msg('Analytics.success-cleared'));
			}
			else
			{
				DekiMessage::success($this->View->msg('Analytics.success'));
			}
			return;	
		}

		$this->View->set('analyticskey', wfGetConfig('ui/analytics-key'));
		$this->View->set('form.action', $this->getUrl('/'));
		$this->View->output();
	}
	
}

new Analytics();
