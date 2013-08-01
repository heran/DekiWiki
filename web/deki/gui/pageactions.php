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

require_once('gui_index.php');

class PageActions extends DekiFormatter
{
	protected $contentType = 'application/json';
	protected $requireXmlHttpRequest = true;

	private $Request;

	public function format()
	{
		$this->Request = DekiRequest::getInstance();
		
		$action = $this->Request->getVal( 'action' );

		switch ( $action )
		{
			case 'setrestrictions':
				$success = $this->setPageRestrictions();
				$result = array(
					'success' => $success
				);
				break;

			case 'delete':
				$result = $this->deletePage();
				break;
			
			default:
				header('HTTP/1.0 404 Not Found');
				exit(' '); // flush the headers
		}
		
		echo json_encode($result);
	}

	private function setPageRestrictions()
	{
		global $wgDekiPlug, $wgUser, $wgDefaultRestrictRole;
		
		$pageId = $this->Request->getVal( 'titleid' );
		$restrictType = $this->Request->getVal( 'protecttype' );
		$listIds = $this->Request->getVal( 'userids' );
		$cascade = $this->Request->getVal( 'cascade' );
		
		$listIds = empty($listIds) ? array(): explode(',', $listIds);
		
		$users = array();
		$groups = array();
		
		foreach ($listIds as $id)
		{
			if (strncmp($id, 'u', 1) == 0)
			{
				$users[] = substr($id, 1);
			}
			if (strncmp($id, 'g', 1) == 0)
			{
				$groups[] = substr($id, 1);
			}
		}
	
		//can't lock yourself out of the page!
		if (empty($users) && empty($groups))
		{
			$users[] = $wgUser->getId();
		}
	
		$grants = array();
		$groups = array_unique($groups);
		$users = array_unique($users);
	
		foreach ($users as $userId)
		{
			$grants[] = array('permissions' => array('role' => $wgDefaultRestrictRole), 'user' => array('@id' => $userId));
		}
		foreach ($groups as $groupId)
		{
			$grants[] = array('permissions' => array('role' => $wgDefaultRestrictRole), 'group' => array('@id' => $groupId));
		}
	
		//generate the XML document to PUT for grant list
		$xml = array(
			'security' => array(
				'permissions.page' => array('restriction' => $restrictType),
				'grants' => array('@restriction' => $restrictType, array('grant' => $grants))
			)
		);
	
		$Plug = $wgDekiPlug->At('pages', $pageId, 'security');

		if ($cascade == 'true')
		{
			$Plug = $Plug->With('cascade', 'delta');
		}

		$Plug = $Plug->Put($xml);
		
		if (MTMessage::HandleFromDream($Plug))
		{
			wfMessagePush('general', wfMsg('Article.Common.permissions-updated'), 'success');
		}
		
		return true;
	}

	private function deletePage()
	{
		$titleId = $this->Request->getVal( 'titleid' );
		$includeChildren = $this->Request->getBool( 'cascade' );
		
		$title = Title::newFromID($titleId);
		
	    if (!$title)
	    {
	        return array(
	        	'success' => false,
	        	'message' => 'Topic already deleted'
	        );
	    }
	    
	    $article = new Article($title);
	    
	    if (!$article->userCanDelete())
	    {
	        return array(
	        	'success' => false,
	        	'message' => 'Topic cannot be deleted'
	        );
	    }
	    
	    $error = $article->doDelete($redirectTitle, $includeChildren);
	    
	    return array(
	    	'success' => true,
	    	'redirectTo' => $redirectTitle 
	    );
	}
}

new PageActions();
