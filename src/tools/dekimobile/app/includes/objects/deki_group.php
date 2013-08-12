<?php
/*
 * MindTouch Deki - enterprise collaboration and integration platform
 * Copyright (C) 2006 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
 *  please review the licensing section.
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


class DekiGroup implements IDekiApiObject
{
	private $id = null;
	private $groupname = null;
	// array of user ids, NOT user objects
	protected $users = null;
	// stores the number of group users
	protected $userCount = 0;

	private $Role = null;
	private $AuthService = null;


	/**
	 * @param bool $onlyInternal - specifies if only internal groups should be returned
	 */
	static function getSiteList($onlyInternal = false)
	{
		global $wgAdminPlug;
		
		$Plug = $wgAdminPlug->At('groups');
		if ($onlyInternal)
		{
			$Plug = $Plug->With('authprovider', 1);
		}
		$Result = $Plug->Get();

		if (!$Result->isSuccess())
		{
			throw new Exception('Could not load site groups');
		}

		$siteGroups = $Result->getAll('body/groups/group', array());

		$groups = array();
		foreach ($siteGroups as $group)
		{
			$groups[$group['@id']] = DekiGroup::newFromArray($group);
		}

		return $groups;
	}

	static function &newFromId($id)
	{
		$Group = null;

		if (!empty($id) && ($id > 0))
		{
			$Group = self::load($id);
		}
		
		return $Group;
	}

	static function &newFromText($groupname)
	{
		$Group = self::load($groupname, true);
		return $Group;
	}

	/**
	 * Pass in the api group array to get a group object
	 * Using this method enables the group fields to be expanded later, more functionality
	 */
	static function &newFromArray(&$result)
	{
		if (!isset($result['service.authentication']))
		{
			throw new Exception('Data Corruption: Group ' ."'".$result['@id']."'". ' does not have an authentication provider.');
		}

		$Group = new DekiGroup($result['@id'], $result['groupname'],
							   $result['permissions.group']['role']['@id'],
							   $result['service.authentication']['@id']);

		if (isset($result['users']['@count']))
		{
			$Group->userCount = (int)$result['users']['@count'];
		}

		return $Group;
	}

	private static function load($id, $fromName = false)
	{
		global $wgAdminPlug;

		$Plug = $wgAdminPlug->At('groups', ($fromName ? '=' : '') . $id);

		$Result = $Plug->Get();
		if (!$Result->isSuccess())
		{
			return null;
		}

		$result = $Result->getVal('body/group');
		return self::newFromArray($result);
	}


	public function __construct($id, $groupname, $roleId = null, $authServiceId = null)
	{
		$this->id = $id;
		$this->groupname = $groupname;

		$this->Role = is_null($roleId) ? new DekiRole(0, '') : DekiRole::newFromId($roleId);
		// default to internal group
		$this->AuthService = is_null($authServiceId) ? DekiAuthService::getInternal() : DekiAuthService::newFromId($authServiceId);
	}

	public function getId()				{ return $this->id; }
	public function getName()			{ return $this->groupname; }
	public function &getRole()			{ return $this->Role; }
	public function &getAuthService()	{ return $this->AuthService; }
	public function getUserCount()
	{
		if (is_array($this->users))
		{
			$this->userCount = count($this->users);
		}

		return $this->userCount;
	}

	public function getUsers()
	{
		$this->loadUsers();
		return $this->users;
	}


	public function isInternal() { return $this->AuthService->isInternal(); }


	public function setName($name)
	{
		if ($this->isInternal())
		{
			$this->groupname = $name;
			return true;
		}

		return false;
	}

	public function setRole($Role) { $this->Role = $Role; }
	
	
	/**
	 * int $userId - the user to add to the group
	 */
	public function addUser($userId) { return $this->addUsers(array($userId)); }
	/**
	 * array $users - array of user ids
	 * @return bool - true if the operation was successful
	 */
	public function addUsers($users)
	{
		$this->loadUsers();

		$update = false;
		foreach ($users as $userId)
		{
			if (!isset($this->users[$userId]))
			{
				// add the user
				$this->users[$userId] = $userId;
				$update = true;
			}
		}

		if ($update)
		{
			global $wgAdminPlug;
			$Plug = $wgAdminPlug->At('groups', $this->getId(), 'users');

			return $Plug->Put($this->usersToXml())->isSuccess();
		}

		return false;
	}

	/**
	 * int $userId - the user to remove from the group
	 */
	public function removeUser($userId) { return $this->removeUsers(array($userId)); }
	/**
	 * mixed $users - can be a userId or an array of user ids
	 */
	public function removeUsers($users)
	{
		$this->loadUsers();

		if (!is_array($users))
		{
			$users = array($users);
		}
		
		$update = false;
		foreach ($users as $userId)
		{
			if (isset($this->users[$userId]))
			{
				// remove the user
				unset($this->users[$userId]);
				$update = true;
			}
		}

		if ($update)
		{
			global $wgAdminPlug;
			$Plug = $wgAdminPlug->At('groups', $this->getId(), 'users');

			return $Plug->Put($this->usersToXml())->isSuccess();
		}

		return false;
	}


	/*
	 * Lazy loads the group members
	 */
	private function loadUsers()
	{
		$id = $this->getId();
		if (is_null($this->users) && !is_null($id))
		{
			global $wgAdminPlug;

			$Plug = $wgAdminPlug->At('groups', $id, 'users')->With('limit', 100000);

			$Result = $Plug->Get();
			if (!$Result->isSuccess())
			{
				$this->users = array();
				return false;
			}

			$result = $Result->getAll('body/users/user', array());

			$this->users = array();
			foreach ($result as $user)
			{
				$this->users[$user['@id']] = $user['@id'];
			}
		}

		return true;
	}


	public function create($authUsername = null, $authPassword = null)
	{
		global $wgAdminPlug;

		$Plug = $wgAdminPlug->At('groups');

		if (!is_null($authUsername) || !is_null($authPassword))
		{
			$Plug = $Plug->With('authusername', $authUsername)->With('authpassword', $authPassword);
		}

		return $Plug->Post($this->toArray());
	}

	/**
	 * Updates the group role or name if it is an internal group
	 */
	public function update()
	{
		global $wgAdminPlug;

		$Plug = $wgAdminPlug->At('groups', $this->getId());

		return $Plug->Put($this->toArray());		
	}

	public function delete()
	{
		global $wgAdminPlug;

		$Plug = $wgAdminPlug->At('groups', $this->getId());

		return $Plug->Delete();
	}



	private function usersToXml()
	{
		$users = array();
		foreach ($this->users as $id => $User)
		{
			$users['user'][] = array('@id' => $id);
		}
		
		$users = array('users' => $users);
		return encode_xml($users);
	}

	public function toArray()
	{
		$group = array(
			'groupname' => $this->getName(),
			'service.authentication' => array('@id' => $this->getAuthService()->getId()),
			'permissions.group' => array('role' => $this->getRole()->getName())
		);

		$id = $this->getId();
		if (!is_null($id))
		{
			$group['@id'] = $id;
		}

		$group = array('group' => $group);
		return $group;
	}

	public function toXml()
	{
		return encode_xml($this->toArray());
	}

	public function toHtml()
	{
		return htmlspecialchars($this->getName());
	}
}
