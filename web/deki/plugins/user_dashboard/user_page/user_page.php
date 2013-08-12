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

DekiPlugin::registerHook(Hooks::DATA_GET_USER_DASHBOARD_PLUGINS, array('UserHomePage', 'getDashboardPluginsHook'));

class UserHomePage extends UserDashboardPage
{	
	protected $pluginFolder = 'user_page';
	
	public static function getDashboardPluginsHook(&$plugins, $User)
	{
		$Plugin = new self($User);
		$plugins[] = $Plugin;
	}
	
	public function initPlugin()
	{	
		global $wgUser;
		
		if (!is_null($this->User) && $wgUser->getId() == $this->User->getId())
		{
			$this->displayTitle = wfMsg('UserDashboard.dashboard.myhome');
		}
		else
		{
			$this->displayTitle = wfMsg('UserDashboard.dashboard.home');
		}

		$this->pagePath = 'User:' . $this->User->getUsername();
		
		parent::initPlugin();
	}
	
	public function getPluginId()
	{
		return 'home';
	}
	
	public function getHtml() 
	{
		global $wgUser;
		$sk = $wgUser->getSkin();
		
		$html = '';
		
		$Title = Title::newFromId($this->PageInfo->id);
		$Article = new Article($Title);
		
		$html .= $sk->renderUserDashboardHeader($Article, $Title);
		$html .= parent::getHtml();
		
		return $html;
	}
}
