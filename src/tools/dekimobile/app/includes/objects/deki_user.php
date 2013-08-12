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


class DekiUser implements IDekiApiObject
{
	const ANONYMOUS_USER = 'Anonymous';
	const STATUS_ACTIVE = 'active';
	const STATUS_INACTIVE = 'inactive';

	// define the known user option fields here
	const OPTION_QUICKLINKS = 'quicklinks';

	private static $instance = null;

	// basic user information
	private $id = null;
	private $username = null;
	
	// extended user information
	private $email = null;
	private $fullname = null;
	private $status = null;
	private $lastLogin = null;

	// @param array $options - key value pairs of user specific options
	private $options = null;

	private $permissions = array();
	// array of group objects
	private $groups = array();
	// role object
	private $Role = null;
	// authentication service object
	private $AuthService = null;
	

	/**
	 * @return DekiUser - currently logged in user
	 */
	static function getCurrent()
	{
		if (is_null(self::$instance))
		{
			global $wgAdminPlug;

			$Result = $wgAdminPlug->At('users', 'current')->Get();
			
			if (!$Result->isSuccess())
			{
				return new DekiUser(); // TODO: return anonymous user
			}
			
			$result = $Result->getVal('body/user');
			self::$instance = DekiUser::newFromArray($result);
		}

		return self::$instance;
	}
	
	/**
	 * Attempts to log the user in
	 * If the user was logged in successfully, getCurrent() will return the 
	 * logged in user
	 *
	 * @return bool - result of the login operation
	 */
	 // TODO: allow configuration for cookie or session based login?
	static function login($username, $password, $authId = null)
	{
		global $wgAdminPlug;
		$Plug = $wgAdminPlug->At('users', 'authenticate')->WithCredentials($username, $password);

		if ($authId)
    { 
      $Plug = $Plug->With('authenticate', $authId);
    }
		 
		$Result = $Plug->Get();

		if ($Result->isSuccess())
		{
			$hash = $Result->getVal('body');
			setcookie('authtoken', $hash, 0, '/');
			return true;
		}
		return false;			
	}

	static function logout()
	{
		setcookie('authtoken', null, time() - 3600, '/');
		return true;
	}

	static function getSiteList($filters = array(), $page = 1, $limit = 100)
	{
		global $wgAdminPlug;

		$offset = ($page-1)*$limit;
		$Plug = $wgAdminPlug->At('users')->With('offset', $offset)->With('limit', $limit);
		foreach ($filters as $filter => $value)
		{
			$Plug = $Plug->With($filter, $value);
		}
		$Result = $Plug->Get();

		if (!$Result->isSuccess())
		{
			throw new Exception('Could not load site users');
		}

		$users = $Result->getAll('body/users/user');

		$siteUsers = array();
		foreach ($users as &$result)
		{
			$User = DekiUser::newFromArray($result);
			$siteUsers[$User->getId()] = $User;
		}
		unset($result);

		return $siteUsers;
	}

	static function &newFromId($id)
	{
		$User = self::load($id);
		return $User;
	}

	static function &newFromText($username)
	{
		$User = self::load($username, true);
		return $User;
	}

	static function &newFromArray(&$result)
	{
		$Result = new DekiResult($result);

		$User = new DekiUser($Result->getVal('@id'),
							 $Result->getVal('username'),
							 $Result->getVal('fullname'),
							 $Result->getVal('email'),
							 $Result->getVal('status'),
							 $Result->getVal('date.lastlogin')
							 );
		$User->setAuthService($Result->getVal('service.authentication', array()));
		$User->setRole($Result->getVal('permissions.user', array()));
		$User->setPermissions($Result->getVal('permissions.effective/operations/#text'));
		$User->setGroups($Result->getAll('groups/group', array()));

		return $User;
	}

	/*
	 * Get a user by id, null if user not found
	 *
	 * @return DekiUser - returns the user on success, otherwise null
	 */
	private static function load($id, $fromName = false)
	{
		global $wgAdminPlug;
		$UserPlug = $wgAdminPlug->At('users', ($fromName ? '=' : '') . $id);

		$Result = $UserPlug->Get();
		if (!$Result->isSuccess())
		{
			return null;
		}
		
		$result = $Result->getVal('body/user');
		return self::newFromArray($result);
	}


	public function __construct($id = null, $username = null, $fullname = null, $email = null, $status = null, $lastLogin = null)
	{
		$this->setInfo($id, $username, $fullname, $email);
		$this->setStatus($status);
		$this->lastLogin = $lastLogin;
		
		// default to local authentication
		$this->AuthService = DekiAuthService::getInternal();
		// default to unknown role
		$this->Role = new DekiRole(0, null);
		//
		$this->permissions = array();
		$this->groups = array();
	}
	

	public function getId()				{ return $this->id; }
	public function getName()			{ return $this->username; }
	public function getUsername()		{ return $this->username; }
	public function getEmail()			{ return $this->email; }
	public function getFullname()		{ return $this->fullname; }
	public function getStatus()			{ return $this->status; }
	public function getLastLogin()		{ return $this->lastLogin; }

	public function getOption($key, $default = null)
	{
		// TODO: remove database dependency
		$this->loadOptions();
		return isset($this->options[$key]) ? $this->options[$key] : $default;
	}

	public function &getRole()			{ return $this->Role; }
	public function &getAuthService()	{ return $this->AuthService; }
	public function getGroupNames()
	{
		$groupNames = array();
		// need to build an array of group ids
		foreach ($this->groups as $id => &$Group)
		{
			$groupNames[] = $Group->getName();
		}
		unset($Group);

		return $groupNames;
	}
	public function getGroupIds() 
	{
		$groupIds = array();
		// need to build an array of group ids
		foreach ($this->groups as $id => &$Group)
		{
			$groupIds[] = $id;
		}
		unset($Group);

		return $groupIds;
	}

	public function isInternal() { return $this->getAuthService()->isInternal(); }
	public function isGroupMember($groupId) { return isset($this->groups[$groupId]); }
	public function isAnonymous() { return strtolower($this->username) == strtolower(self::ANONYMOUS_USER); }
	public function isDisabled() { return $this->status == self::STATUS_INACTIVE; }

	// checks if a user has a certain permission
	public function can($perm) { return in_array(strtoupper($perm), $this->permissions); }

	public function setInfo($id, $username, $fullname, $email)
	{
		$this->id = $id;
		$this->username = $username;
		$this->email = $email;
		$this->fullname = $fullname;
	}

	public function setName($username)
	{
		if ($this->getAuthService()->isInternal())
		{
			$this->username = $username;
			return true;
		}

		return false;
	}
	
	public function setEmail($email = null)
	{
		// emails are required for internal users
		// if set for external then validate the email address
		if (($this->isInternal() && !$this->isAnonymous()) || (!$this->isInternal() && !empty($email)))
		{
			// validate the email address
			if (empty($email) || !wfValidateEmail($email))
			{
				// invalid email address
				return false;
			}
		}
		
		// set the email address
		$this->email = $email;
		return true;
	}


	public function setOption($key, $value)
	{
		// TODO: remove database dependency
		$this->loadOptions();
		$this->options[$key] = $value;
	}

	private function loadOptions()
	{
		if (is_null($this->options))
		{
			// the following stuff is for non-anonymous users only
			$UserManagement = new DreamUserManagement();
			$SUser = $UserManagement->GetById($this->getId());
			// set the options array
			$this->options = is_array($SUser->mOptions) ? $SUser->mOptions : array();
		}
	}

	/**
	 * TODO TODO: This horrible function should be removed. Requires database & ui code.
	 */
	public function updateOptions()
	{
		if (is_null($this->options))
		{
			return false;
		}
		$User = new WikiUser('', $this->getId());
		$User->setOptions($this->options);

		$UserManagement = new DBUsersManagement();
		$success = $UserManagement->UpdateUsers($User);

		return $success;
	}

	public function setStatus($status)
	{
		$this->status = $status == self::STATUS_INACTIVE ? self::STATUS_INACTIVE : self::STATUS_ACTIVE;
	}

	public function setAuthService($service)
	{
		if (is_object($service))
		{
			$this->AuthService = $service;
		}
		else if (!empty($service))
		{
			$this->AuthService = DekiAuthService::newFromId($service['@id']);
		}
		else
		{
			// default to internal auth
			$this->AuthService = DekiAuthService::getInternal();
		}
	}

	/*
	 * @param mixed $role - can be a Role object or an array
	 */
	public function setRole($role)
	{
		if (is_object($role))
		{
			$this->Role = $role;
		}
		else if (!empty($role))
		{
			$this->Role = DekiRole::newFromArray($role);
		}
		else
		{
			// default to unknown role
			$this->Role = new DekiRole(0, '');
		}
	}

	/**
	 * @see update
	 * @return DekiResult object
	 */
	public function create($authUsername = null, $authPassword = null, $password = null)
	{
		// make sure no user id is set
		$this->id = null;

		global $wgAdminPlug;

		$Plug = $wgAdminPlug->At('users');
		// only internal users can set their password
		if (!empty($password) && $this->isInternal())
		{
			$Plug = $Plug->With('accountpassword', $password);
		}

		if (!is_null($authUsername) || !is_null($authPassword))
		{
			$Plug = $Plug->With('authusername', $authUsername)->With('authpassword', $authPassword);
		}

		$Result = $Plug->Post($this->toArray());

		if ($Result->isSuccess())
		{
			$this->id = $Result->getVal('/body/user/@id');
		}

		return $Result;
	}

	/**
	 * Posts to the API with the user's details
	 *
	 * @param $authUsername - username for accessing the remote auth provider
	 * @param $authPassword - password for accessing the remote auth provider
	 * @param $password - sets the user's password, only works when creating a new user
	 *
	 * @return DekiResult object
	 */
	public function update($authUsername = null, $authPassword = null, $password = null)
	{
		global $wgAdminPlug;

		$Plug = $wgAdminPlug->At('users');
		// only internal users can set their password
		if (!empty($password) && $this->isInternal())
		{
			$Plug = $Plug->With('accountpassword', $password);
		}

		if (!is_null($authUsername) || !is_null($authPassword))
		{
			$Plug = $Plug->With('authusername', $authUsername)->With('authpassword', $authPassword);
		}

		// TODO: revert to a put!
		return $Plug->Post($this->toArray());
	}

	/*
	 * Use this to update the user's password
	 * If the calling user is an admin they do not need to specify a current password
	 *
	 * @param string $newPassword - sets the user's new password
	 * @param string $currentPassword - only required if the user is trying to change their own password
	 * @param bool $setAlternate - if true then the user's temporary password is set (admins only)
	 */
	public function changePassword($newPassword, $currentPassword = null, $setAlternate = false)
	{
		global $wgAdminPlug;

		$Plug = $wgAdminPlug->At('users', $this->getId(), 'password')->SetHeader('Content-Type', 'text/plain');
		if (!is_null($currentPassword))
		{
			$Plug = $Plug->With('currentpassword', $currentPassword);
		}
		if (!is_null($setAlternate) && $setAlternate)
		{
			$Plug = $Plug->With('altpassword', 'true');
		}

		return $Plug->Put($newPassword);
	}

	
	/*
	 * Determines what groups should be added and removed for this user
	 * @param array $newGroups - a list of group ids which a user belongs to
	 * TODO: figureout how to return status information
	 */
	public function updateGroups($setGroups = array())
	{
		$userGroups = $this->getGroupIds();
		// diff the new groups and the user's current to see what groups are added
		$newGroups = array_diff($setGroups, $userGroups);
		// diff other way to determine what groups are removed
		$deletedGroups = array_diff($userGroups, $setGroups);
		
		$success = true;

		foreach ($deletedGroups as $id)
		{
			if (isset($this->groups[$id]))
			{
				$Group = &$this->groups[$id];
				$success &= $Group->removeUser($this->getId());
				unset($this->groups[$id]);
			}
		}

		foreach ($newGroups as $id)
		{
			if (!isset($this->groups[$id]))
			{
				$Group = DekiGroup::newFromId($id);

				if (!is_null($Group))
				{
					$success &= $Group->addUser($this->getId());
					$this->groups[$id] = $Group;
				}
			}
		}

		return $success;
	}


	protected function setPermissions($perms)
	{
		if (is_null($perms))
		{
			$this->permissions = array();
		}
		else
		{
			$this->permissions = explode(',', $perms);
		}
	}
	
	protected function setGroups($groups = array())
	{
		$this->groups = array();

		if (!empty($groups))
		{
			foreach ($groups as $group)
			{
				$this->groups[$group['@id']] = DekiGroup::newFromArray($group);
			}
		}
	}


	public function toArray()
	{
		$user = array(
			'username' => $this->getUsername(),
			'email' => $this->getEmail(),
			'fullname' => $this->getFullname(),
			'service.authentication' => array('@id' => $this->getAuthService()->getId()),
			'permissions.user' => array('role' => $this->getRole()->getName()),
			'status' => $this->getStatus()
		);

		$id = $this->getId();
		if (!is_null($id))
		{
			$user['@id'] = $id;
		}

		$user = array('user' => $user);
		return $user;
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
