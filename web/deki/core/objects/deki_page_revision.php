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

class DekiPageRevision
{
	public static function getPageRevisions($pageId, $page = 1, $limit = 100)
	{
		$offset = ($page-1)*$limit;
		$Plug = DekiPlug::getInstance()->At('pages', $pageId, 'revisions')->With('offset', $offset)->With('max', $limit);		
		$Result = $Plug->Get();

		$pageRevisions = array();
		if ($Result->handleResponse())
		{
			$revisions = $Result->getAll('body/pages/page');
			foreach ($revisions as $result)
			{
				$Revision = DekiPageRevision::newFromArray($result);
				$pageRevisions[] = $Revision;
			}
		}
		
		return $pageRevisions;
	}
	
	public static function hide($pageId, $revisionId, $comment = null)
	{
		$Plug = DekiPlug::getInstance();
		$Plug = $Plug->At('pages', $pageId, 'revisions');
		if (!is_null($comment))
		{
			$Plug = $Plug->With('comment', $comment);
		}
		
		$xml = array(
			'page' => array(
				'@id' => $pageId,
				'@revision' => $revisionId,
				'@hidden' => 'true'
			)
		);
		
		$xml = array('revisions' => $xml);
		$Result = $Plug->Post($xml);

		return $Result;
	}
	
	public static function show($pageId, $revisionId)
	{
		$Plug = DekiPlug::getInstance();
		$Plug = $Plug->At('pages', $pageId, 'revisions');
		
		$xml = array(
			'page' => array(
				'@id' => $pageId,
				'@revision' => $revisionId,
				'@hidden' => 'false'
			)
		);
		
		$xml = array('revisions' => $xml);
		$Result = $Plug->Post($xml);

		return $Result;		
	}

	
	public static function newFromArray(&$result)
	{
		$X = new XArray($result);
		
		$Revision = new DekiPageRevision($X->getVal('@id'), $X->getVal('@revision'));
		$Revision->setInfo(
			'edit',
			$X->getVal('user.author/@id'),
			$X->getVal('user.author/nick'),
			$X->getVal('date.edited'),
			$X->getVal('description')
		);
		
		if ($X->getVal('@hidden', null) != null)
		{
			$Revision->setInfo(
				'hidden',
				$X->getVal('user.hiddenby/@id'),
				$X->getVal('user.hiddenby/nick'),
				$X->getVal('date.hidden'),
				$X->getVal('description.hidden')
			);
		}
		
		return $Revision;
	}
	
	protected $pageId = null;
	protected $revisionId = null;

	protected $editUserId = null;
	protected $editUsername = null;
	protected $editDate = null;
	protected $editComment = null;
	
	protected $hiddenUserId = null;
	protected $hiddenUsername = null;
	protected $hiddenDate = null;
	protected $hiddenComment = null;


	public function __construct($pageId, $revisionId)
	{
		$this->pageId = $pageId;
		$this->revisionId = $revisionId;
	}
	

	public function isHidden() { return !is_null($this->hiddenDate); }
	
	public function getId() { return $this->revisionId; }
	public function getPageId() { return $this->pageId; }
	
	/**
	 * Retrieve information about the revision
	 *
	 * @param enum $type ['edit', 'hidden']
	 * 
	 * @return string
	 */
	public function getDate($type = 'edit') { return $type == 'hidden' ? $this->hiddenDate : $this->editDate; }
	public function getUserId($type = 'edit') { return $type == 'hidden' ? $this->hiddenUserId : $this->editUserId; }
	public function getUsername($type = 'edit') { return $type == 'hidden' ? $this->hiddenUsername : $this->editUsername; }
	public function getComment($type = 'edit') { return $type == 'hidden' ? $this->hiddenComment : $this->editComment; }

	public function getUser($type = 'edit')
	{
		$User = DekiUser::newFromId($this->getUserId($type));
		if (is_null($User))
		{
			return DekiUser::getAnonymous();
		}

		return $User;
	}

	/**
	 * @param enum $type ['edit', 'hidden']
	 * @param unknown_type $userId
	 * @param unknown_type $username
	 * @param unknown_type $date
	 * @param unknown_type $comment
	 */
	public function setInfo($type = 'edit', $userId, $username, $date, $comment = null)
	{
		if ($type == 'hidden')
		{
			$this->hiddenUserId = $userId;
			$this->hiddenUsername = $username;
			$this->hiddenDate = $date;
			$this->hiddenComment = $comment;			
		}
		else
		{
			$this->editUserId = $userId;
			$this->editUsername = $username;
			$this->editDate = $date;
			$this->editComment = $comment;			
		}
	}
}
