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

class UserDashboardPage extends UserDashboardPlugin
{
	/**
	 * @var string - path to page to render ("Template:Foo")
	 */
	protected $pagePath = null;
	
	/**
	 * @var DekiPageInfo - page info object for this page
	 */
	protected $PageInfo = null;

	/**
	 * Override to customize name, description, etc.
	 * @return N/A
	 */
	protected function initPlugin()
	{
		if (empty($this->pagePath))
		{
			throw new Exception(wfMsg('UserDashboard.error.nopath'));
		}

		// @TODO kalida: utility function to turn path into DekiPageInfo
		$Plug = DekiPlug::getInstance()->At('pages', '=' . $this->pagePath, 'info');
		$Result = $Plug->Get();

		// if page not found, not fatal; display error in plugin tab itself
		if ($Result->handleResponse())
		{
			$this->PageInfo = DekiPageInfo::newFromArray($Result->getVal('body/page'));

			$this->displayTitle = is_null($this->displayTitle) ? $this->PageInfo->title : $this->displayTitle;
		}
	}

	/**
	 * Render html from plugin article
	 * @see deki/plugins/dashboard/DashboardPlugin#getHtml()
	 */
	protected function &getHtml()
	{
		if (is_null($this->PageInfo))
		{
			$html = wfMsg('UserDashboard.error.nopage', $this->pagePath);
			return $html;
		}

		// Get user homepage for context
		$Plug = DekiPlug::getInstance();
		$Plug = $Plug->At('pages', $this->PageInfo->id, 'contents')->With('pageid', $this->UserPageInfo->id);

		$Result = $Plug->Get();

		$html = '';

		if ($Result->handleResponse())
		{
			// bug #8183: include all page components
			$html .= $Result->getVal('body/content/head');
			$html .= $Result->getVal('body/content/body');
			$html .= $Result->getVal('body/content/tail');
		}
		else
		{
			$html .= wfMsg('UserDashboard.error.pagecontent');
		}

		return $html;
	}

	protected function getPluginId()
	{
		return preg_replace('/[^a-zA-Z_0-9]+/', '_', $this->getDisplayTitle());
	}

	protected function getDisplayTitle() {
		return is_null($this->displayTitle) ? $this->pagePath : parent::getDisplayTitle();
	}
}
