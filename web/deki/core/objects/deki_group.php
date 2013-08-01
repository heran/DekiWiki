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

/**
 * Handles groups
 */
class DekiGroup implements IDekiApiObject
{
	// number of groups site wide
	protected static $groupCount = null;

	private $id = null;
	private $groupname = null;
	// array of user ids, NOT user objects
	protected $users = null;
	// stores the number of group users
	protected $userCount = 0;

	private $Role = null;
	private $AuthService = null;
	private $authServiceId = null;
	private $hasInvalidAuth = false;

	/**
	 * @return int - the number of groups on the site
	 */
	static function getSiteCount()
	{
		if (is_null(self::$groupCount))
		{
			self::getSiteList(array(), 1, 1);
		}

		return self::$groupCount;
	}
	/**
	 * @param array $filters - specifies which additional parameters to send to the api
	 * @param mixed $page - specifies the page number to retrieve; set to 'all' to retrieve all results
	 */
	static function getSiteList($filters = null, $page = 1, $limit = 100)
	{
		if (!is_array($filters))
		{
			// add default sort by name
			$filters = array(
				'sortby' => 'name'
			);
		}
		
		$Plug = DekiPlug::getInstance()->At('groups');
		
		if ($page == 'all')
		{
			$Plug = $Plug->With('limit', 'all'); 
		}
		else
		{
			$Plug = $Plug->With('offset', ((int)$page-1)*$limit)->With('limit', $limit);
		}
			
		foreach ($filters as $filter => $value)
		{
			$Plug = $Plug->With($filter, $value);
		}
		$Result = $Plug->Get();

		if (!$Result->isSuccess())
		{
			throw new Exception('Could not load site groups');
		}

		$groups = $Result->getAll('body/groups/group', array());
		// set the number of site groups
		self::$groupCount = $Result->getVal('body/groups/@totalcount', 0);

		$siteGroups = array();
		if (!empty($groups))
		{
			foreach ($groups as &$result)
			{
				$Group = DekiGroup::newFromArray($result);
				$siteGroups[$Group->getId()] = $Group;
			}
			unset($result);
		}

		return $siteGroups;
	}

	static function newFromId($id)
	{
		$Group = null;

		if (!empty($id) && ($id > 0))
		{
			$Group = self::load($id);
		}
		
		return $Group;
	}

	static function newFromText($groupname)
	{
		$Group = self::load($groupname, true);
		return $Group;
	}

	/**
	 * Pass in the api group array to get a group object
	 * Using this method enables the group fields to be expanded later, more functionality
	 * 
	 * @return DekiGroup
	 */
	static function newFromArray(&$result)
	{
		$X = new XArray($result);
		$Group = new DekiGroup(
			$X->getVal('@id'),
			$X->getVal('groupname'),
			$X->getVal('permissions.group/role/@id'),
			$X->getVal('service.authentication/@id')
		);
		
		$count = $X->getVal('users/@count');
		if (!is_null($count))
		{
			$Group->userCount = (int)$count;
		}

		return $Group;
	}

	private static function load($id, $fromName = false)
	{
		$Plug = DekiPlug::getInstance()->At('groups', ($fromName ? '=' : '') . $id);

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
		$this->setAuthService($authServiceId);
	}

	public function getId()				{ return $this->id; }
	public function getName()			{ return $this->groupname; }
	public function &getRole()			{ return $this->Role; }

	public function &getAuthService()
	{
		if (is_null($this->AuthService))
		{
			$this->AuthService = DekiAuthService::newFromId($this->authServiceId);
			if (is_null($this->AuthService))
			{
				// TODO: log this error?
				// reset the service to internal
				$Service = DekiAuthService::getInternal();
				$this->setAuthService($Service->getId(), $Service);
			}
		}
		
		return $this->AuthService;
	}
	
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


	public function isInternal()
	{
		return $this->authServiceId == DekiAuthService::INTERNAL_AUTH_ID;
	}
	public function isAuthInvalid() { return $this->hasInvalidAuth; }

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
	 * @param int $id - service id to lazy load
	 * @param DekiAuthService $Service - sets the auth service to use
	 */
	public function setAuthService($id, DekiAuthService $Service = null)
	{
		if (is_null($id))
		{
			// unknown auth provider specified
			$this->hasInvalidAuth = true;
			$this->authServiceId = DekiAuthService::INTERNAL_AUTH_ID;
		}
		else
		{
			$this->authServiceId = (int)$id;
			if (!is_null($Service) && $id == $Service->getId())
			{
				$this->AuthService = $Service;
			}
			else
			{
				$this->AuthService = null;
			}
		}
	}
	
	/**
	 * int $userId - the user to add to the group
	 */
	public function addUser($userId) { return $this->addUsers(array($userId)); }
	/**
	 * array $users - array of user ids
	 * @return bool - true if the operation was successful
	 */
	public function addUsers(array $users)
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
			$Plug = DekiPlug::getInstance()->At('groups', $this->getId(), 'users');

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
			$Plug = DekiPlug::getInstance()->At('groups', $this->getId(), 'users');

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
			$Plug = DekiPlug::getInstance()->At('groups', $id, 'users')->With('limit', 100000);

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
		$Plug = DekiPlug::getInstance()->At('groups');

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
		$Plug = DekiPlug::getInstance()->At('groups', $this->getId());

		return $Plug->Put($this->toArray());		
	}

	public function delete()
	{
		$Plug = DekiPlug::getInstance()->At('groups', $this->getId());

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
