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
 * Core user object
 * 
 * @example 
 * 	$User = DekiUser::getCurrent();
 * 	if ($status = DekiUser::login($username, $password) === true)
 */
class DekiUser extends User implements IDekiApiObject
{
	const ANONYMOUS_USER = 'Anonymous';
	const STATUS_ACTIVE = 'active';
	const STATUS_INACTIVE = 'inactive';
	
	const SEAT_STATUS_SEATED = 'seated';
	const SEAT_STATUS_UNSEATED = 'unseated';
	const SEAT_STATUS_OWNER = 'owner';


	private static $instance = null;
	private static $cache = array();
	
	// number of users site wide
	protected static $userCount = null;

	// basic user information
	private $id = null;
	private $username = null;
	
	// extended user information
	private $email = null;
	private $fullname = null;
	private $status = null;
	private $lastLogin = null;
	// core user options
	private $timezone = null;
	private $language = null;
	
	protected $seatStatus = null;
	
	// stores a list of the user's base permissions
	private $permissions = array();
	/**
	 * Stores an array of group ids to group
	 * @note - this is lazy loaded
	 * @var DekiGroup[]
	 */
	private $groups = array();
	/**
	 * Internal lookup table. Translates a group name into a group id.
	 * @var int[]
	 */
	private $groupNames = array();

	// most requests are loaded; only the current user call is lazy (excluding user properties & groups) for performance
	private $loaded = true;

	// role object
	private $Role = null;
	// authentication service id & object
	private $authServiceId = null;
	// guerrics: users with invalid auth => auth service does not exist
	private $hasInvalidAuth = false;
	private $AuthService = null;
	// user properties object
	public $Properties = null;
	
	/*
	 * @return DekiUser - the anonymous user object
	 */
	public static function getAnonymous()
	{
		$Anonymous = self::load(self::ANONYMOUS_USER, true);
		if (!is_null($Anonymous))
		{
			return $Anonymous;
		}
		else
		{
			// there was a problem loading the anonymous user?
			return new DekiUser();
		}
	}
	
	/**
	 * @param string $authToken - if specified, then current user context is loaded via this token
	 * @return DekiUser - currently logged in user
	 */
	public static function getCurrent($authToken = null)
	{
		if (is_null(self::$instance) || !is_null($authToken))
		{		
			if (is_null($authToken))
			{
				$authToken = DekiToken::get();
			}
			else
			{
				DekiToken::set($authToken);
			}

			// grab a plug object
			$DekiPlug = DekiPlug::getInstance();

			// no authtoken is present
			if (empty($authToken))
			{
				global $wgTrustedAuth, $wgTrustedAuthProvider, $wgTrustedAuthCgiVariable, $wgTrustedAuthCgiPattern;

				// configuration defaults
				$cgiVariable = !empty($wgTrustedAuthCgiVariable) ? $wgTrustedAuthCgiVariable : 'REMOTE_USER';
				$cgiPattern = !empty($wgTrustedAuthCgiPattern) ? $wgTrustedAuthCgiPattern : '.*';
				
				// user name set by apache
				$externalUsername = isset($_SERVER[$cgiVariable]) ? $_SERVER[$cgiVariable] : null;
				
				if ($cgiPattern != '.*')
				{
					// clean up the pattern before using
					$pattern = '/'. str_replace(array('/', '\\'), array('\\/', '\\\\'), $wgTrustedAuthCgiPattern) . '/i';
					if (preg_match($pattern, $externalUsername, $matches))
					{
						// grab the last match as the username
						$externalUsername = array_pop($matches);
					}
				}

				// no remote user is available
				if (empty($externalUsername))
				{
					// anonymous user
					self::$instance = self::getAnonymous();
					return self::$instance;
				}
				// config allows logging in with an Apache user
				else if ($wgTrustedAuth == true)
				{
					// auth provider id is required for trusted auth, default to local if not set
					$providerId = !empty($wgTrustedAuthProvider) ? $wgTrustedAuthProvider : 1;

					$Plug = $DekiPlug->At('users', 'authenticate')->WithCredentials($externalUsername, '')->WithApiKey();
					$Plug = $Plug->With('authprovider', $providerId);

					$Result = $Plug->Post();
					if (!$Result->isSuccess())
					{
						DekiMessage::error(wfMsg('System.Error.account-remote-deactived'));
						// return anonymous user
						return self::getAnonymous();
					}
					
					// set the user's authtoken
					// TODO: set authtoken from result headers in case of error still receives a 200
					$authToken = $Result->getVal('body');
					DekiToken::set($authToken);
				}
			}


			// bug 5377: update the global plug with the authtoken for this request
			$DekiPlug = $DekiPlug->SetHeader('X-Authtoken', $authToken);
			DekiPlug::setInstance($DekiPlug);

			// get the current user, based on this cookie
			// bug mt-9889: excluding properties will leave <properties href=""> and can be lazy loaded on demand
			// groups still need to be loaded explicitly
			$Result = $DekiPlug->At('users', 'current')->With('exclude', 'groups,properties')->Get();
			if (!$Result->isSuccess())
			{
				// anonymous user
				self::$instance = self::getAnonymous();
				return self::$instance;
			}

			$result = $Result->getVal('body/user');
			// set the current user object
			$User = DekiUser::newFromArray($result);
			
			// user was lazy loaded to avoid group lookup
			$User->loaded = false;
			self::$instance = $User;
		}

		return self::$instance;
	}
	
	/**
	 * Attempts to log the user in
	 * If the user was logged in successfully, getCurrent() will return the 
	 * logged in user
	 *
	 * @return mixed - status code of the login operation on failure, else true
	 */
	public static function login($username, $password, $authId = null)
	{
		$Plug = DekiPlug::getInstance()->At('users', 'authenticate')->WithCredentials($username, $password);

		if (!is_null($authId))
		{ 
			$Plug = $Plug->With('authprovider', $authId);
		}
		
		// post to users/authenticate to force user creation in case of remote
		$Result = $Plug->Post();

		if ($Result->handleResponse())
		{
			$setCookie = $Result->getHeader('Set-Cookie');
			// TODO: check if setCookie is null, see todo below	
			$authToken = $Result->getVal('body');
			
			// TODO: set authtoken from result headers in case of error still receives a 200
			DekiToken::set($authToken, $setCookie);

			// remove the current instance
			self::$instance = null;

			return true;
		}

		return $Result->getStatus();
	}

	public static function logout()
	{
		DekiToken::destroy();
		return true;
	}
	
	/**
	 * Updates the seat status for a given user
	 *
	 * @param DekiUser $User
	 * @param string $status - see class consts
	 * @return DekiResult
	 */
	public static function updateSeatStatus($User, $status)
	{
		$Plug = DekiPlug::getInstance();
		$Plug = $Plug->At('users', $User->getId(), 'seat')->WithApiKey();
		return $status == self::SEAT_STATUS_SEATED ? $Plug->Put() : $Plug->Delete();
	}

	/**
	 * Retrieve the total number of active users
	 * 
	 * @return int - the number of users on the site
	 */
	public static function getSiteCount()
	{
		if (is_null(self::$userCount))
		{
			self::getSiteList(array(), 1, 1);
		}

		return self::$userCount;
	}

	/**
	 * 
	 * @param array $filters
	 * @param int $page
	 * @param int $limit
	 * @return DekiUser[]
	 */
	public static function getSiteList($filters = array(), $page = 1, $limit = 100)
	{
		$offset = ($page-1)*$limit;
		$Plug = DekiPlug::getInstance()->At('users')->With('offset', $offset)->With('limit', $limit);

		// apply the filters
		foreach ($filters as $filter => $value)
		{
			$Plug = $Plug->With($filter, $value);
		}
		$Result = $Plug->Get();

		if (!$Result->handleResponse())
        {
            return array();
        }

		$users = $Result->getAll('body/users/user');
		// set the number of site users
		self::$userCount = $Result->getVal('body/users/@totalcount', 0);
		
		$siteUsers = array();
		if (!empty($users))
		{
			foreach ($users as &$result)
			{
				$User = DekiUser::newFromArray($result);
				$siteUsers[$User->getId()] = $User;
			}
			unset($result);
		}

		return $siteUsers;
	}

	/**
	 * @param int $id
	 * @return DekiUser
	 */
	public static function newFromId($id, $withApiKey = false, $withAll = false)
	{
		$User = self::load($id, false, $withApiKey, $withAll);
		return $User;
	}

	/**
	 * @param string $username
	 * @return DekiUser
	 */
	public static function newFromText($username, $withApiKey = false, $withAll = false)
	{
		$User = self::load($username, true, $withApiKey, $withAll);
		return $User;
	}

	/**
	 * @param array $result
	 * @return DekiUser
	 */
	public static function newFromArray(&$result)
	{
		$X = new XArray($result);

		$User = new DekiUser($X->getVal('@id'),
							 $X->getVal('username'),
							 $X->getVal('fullname'),
							 $X->getVal('email'),
							 $X->getVal('status'),
							 $X->getVal('date.lastlogin')
							 );
		$User->setAuthService($X->getVal('service.authentication/@id'));
		$User->setRole($X->getVal('permissions.user', array()));
		$User->setPermissions($X->getVal('permissions.effective/operations/#text'));
		$User->setGroups($X->getAll('groups/group', array()));
		$User->setProperties($X->getVal('properties'), array());
		
		// user seat status
		$User->setSeatStatus($X->getVal('license.seat'));
		
		// royk: see bug #6372; we make assumption all over the code that an un-set value is null, but API returns empty string
		$tz = $X->getVal('timezone'); 
		$User->setTimezone(empty($tz) ? null: $tz);
		$User->setLanguage($X->getVal('language'));

		// cache the resulting user object
		self::$cache[$User->getId()] = $User;
		self::$cache['Name' . $User->getUsername()] = $User;
		
		return $User;
	}

	/*
	 * Get a user by id, null if user not found
	 *
	 * @param bool $withApiKey - if true the user will be loaded with the api key
	 * and retrieve protected information, i.e. the user's email address
	 * 
	 * @return DekiUser - returns the user on success, otherwise null
	 */
	private static function load($id, $fromName = false, $withApiKey = false, $withAll = false)
	{
		// if using the apikey then don't load from cache
		if (!$withApiKey)
		{
			// check the cache
			$cacheKey = ($fromName ? 'Name' : '') . $id;
			if (isset(self::$cache[$cacheKey]))
			{
				return self::$cache[$cacheKey];	
			}
		}

		// MT-9889: Lazy load groups and properties
		$UserPlug = DekiPlug::getInstance()->At('users', ($fromName ? '=' : '') . $id);
		if(!$withAll)
		{
			$UserPlug = $UserPlug->with('exclude', 'groups,properties');
		}
		if ($withApiKey)
		{
			$UserPlug = $UserPlug->WithApiKey();
		}

		$Result = $UserPlug->Get();
		if (!$Result->isSuccess())
		{
			return null;
		}
		
		$result = $Result->getVal('body/user');
		$User = self::newFromArray($result);
		if($withAll)
		{
			$User->loaded = true;
		}
		
		return $User;
	}


	public function __construct($id = null, $username = null, $fullname = null, $email = null, $status = null, $lastLogin = null)
	{
		$this->id = $id;
		$this->username = !is_null($username) ? $username : self::ANONYMOUS_USER;
		$this->setFullname($fullname);

		$this->setStatus($status);
		$this->lastLogin = $lastLogin;
		
		// default to local authentication
		$this->setAuthService(DekiAuthService::INTERNAL_AUTH_ID);
		// default to unknown role
		$this->Role = new DekiRole(0, null);
		// default to empty property bag
		$this->Properties = new DekiUserProperties();

		// setting the email requires a valid auth service
		$this->setEmail($email);
		//
		$this->permissions = array();
		$this->groups = array();
	}
	

	public function getId()				{ return $this->id; }
	/**
	 * Get the display name for the user.
	 */
	public function getName()			{ return !empty($this->fullname) ? $this->fullname : $this->username; }
	public function getUsername()		{ return $this->username; }
	public function getFullname()		{ return $this->fullname; }
	public function getStatus()			{ return $this->status; }
	public function getLastLogin()		{ return $this->lastLogin; }
	public function getPermissions()	{ return $this->permissions; }
	
	/**
	 * This is a bit hairy; there is Title contamination. 
	 */
	public function getUrl() 
	{
		$Title = $this->getUserTitle();
		return $Title->getLocalUrl();
	}

	/**
	 * Generate a title object for the user's page
	 * @return Title
	 */
	public function getUserTitle()
	{
		$Title = Title::newFromText(wfEncodeTitle($this->getUsername()), NS_USER);
		return $Title;
	}

	/**
	 * @param bool $fromApi - forces the email address to be loaded from the api
	 * using the api key unless the email is already set
	 * 
	 * @note $fromApi was added for the forgot password feature which needs to load another
	 * user's email address.
	 */
	public function getEmail($fromApi = false)
	{
		if (is_null($this->email) && $fromApi)
		{
			$User = self::load($this->getId(), false, true);
			$this->email = $User->getEmail();	
		}
		
		return $this->email;
	}
	

	/**
	 * @return DekiRole
	 */
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

	public function getGroupNames()
	{
		$this->lazyLoad();
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
		$this->lazyLoad();
		$groupIds = array();
		// need to build an array of group ids
		foreach ($this->groups as $id => &$Group)
		{
			$groupIds[] = $id;
		}
		unset($Group);

		return $groupIds;
	}

	/**
	 * @param $default - the default value to return if the user does not have a setting. If set to
	 * (bool)true, then the site timezone will be returned (if set)
	 */
	public function getTimezone($default = null)
	{	
		if (is_null($this->timezone))
		{
			global $wgDefaultTimezone;
			return $default === true ? $wgDefaultTimezone : $default;
		}
		
		return $this->timezone;
	}
	
	/**
	 * @param $default - if true, returns site default
	 */
	public function getLanguage($default = null)
	{
		if ($default === true) 
		{
			global $wgLanguageCode;
			$default = $wgLanguageCode; 	
		}
		return !is_null($this->language) ? $this->language : $default;
	}
	
	/**
	 * @note Method does not check if seat management is enabled.
	 * @return string {null, SEAT_STATUS_SEATED, SEAT_STATUS_OWNER, SEAT_STATUS_UNSEATED}
	 */
	public function isSeated()
	{
		switch ($this->seatStatus)
		{
			case self::SEAT_STATUS_SEATED:
			case self::SEAT_STATUS_OWNER:
				return true;
			case self::SEAT_STATUS_UNSEATED:
			default:
				return false;
		}
	}
	
	/**
	 * Check if the user is the site owner for seat management
	 * @return bool
	 */
	public function isSiteOwner()
	{
		return $this->seatStatus == self::SEAT_STATUS_OWNER;
	}
	
	// TODO: (guerrics) consolidate into a parent class for handling auth loading
	public function isInternal()
	{
		return $this->authServiceId == DekiAuthService::INTERNAL_AUTH_ID;
	}
	public function isAuthInvalid() { return $this->hasInvalidAuth; }
	
	/**
	 * Determines if the user is a member of group by id or name.
	 * 
	 * @param mixed $groupId
	 * @param bool $checkName - if true, groupdId is the group name
	 * @return bool
	 */
	public function isGroupMember($groupId, $checkName = false)
	{
		$this->lazyLoad();
		return $checkName ? isset($this->groupNames[$groupId]) : isset($this->groups[$groupId]);
	}

	public function isAnonymous() { return strtolower($this->username) == strtolower(self::ANONYMOUS_USER); }
	public function isDisabled() { return $this->status == self::STATUS_INACTIVE; }

	// checks if a user has a certain permission
	public function can($perm) { return in_array(strtoupper($perm), $this->permissions); }
	
	public function canHighlightTerms() 
	{
		/***
		 * May seem odd to allow sites to override this behavior, but sites like WaPo have complained about 
		 * these pages being indexed by google - it also seems to cause intermittent problems with Varnish, when the 
		 * automatically highlighted phrases from Google are highlighted. 
		 * 
		 * In this case, user precedence should be overridden by an administrative decision to disable this feature. 
		 */
		 
		global $wgEnableSearchHighlight; 
		if (!$wgEnableSearchHighlight) 
		{
			return false;
		}
		
		//an unnecessary optimization?
		if (!$this->isAnonymous()) 
		{
			return $this->Properties->getHighlightOption();	
		}
		return true;
	}
	
	public function setName($username)
	{
		if ($this->getAuthService()->isInternal() && !$this->isAnonymous())
		{
			$this->username = $username;
			return true;
		}

		return false;
	}
	public function setFullname($name) { $this->fullname = $name; }
	
	public function setEmail($email = null)
	{
		if (!is_string($email))
		{
			// means the user's email is hidden, array('@hidden' => true)
			return false;
		}
		
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

	/**
	 * Must call update() after calling these methods
	 */
	public function disable()
	{
		$this->setStatus(self::STATUS_INACTIVE);
		return true;
	}
	
	public function enable()
	{
		$this->setStatus(self::STATUS_ACTIVE);
		return true;		
	}
	// actual status changing method
	protected function setStatus($status)
	{
		$this->status = $status == self::STATUS_INACTIVE ? self::STATUS_INACTIVE : self::STATUS_ACTIVE;
	}

	
	/**
	 * Core user options
	 */
	public function setTimezone($timezone) { $this->timezone = $timezone; }
	public function setLanguage($language)
	{
		if ($language == '')
		{ // maintain null for getLanguage()
			$language = null;
		}
		
		$this->language = $language;
	}
	
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

		$Plug = DekiPlug::getInstance()->At('users');
		$Plug = $Plug->WithApiKey(); //see bug #6560
		
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
		$Plug = DekiPlug::getInstance()->At('users');
		$Plug = $Plug->WithApiKey();
		
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
		// determines if the user need to be logged in again
		$renewLogin = false;
		
		$Plug = DekiPlug::getInstance();
		$Plug = $Plug->At('users', $this->getId(), 'password')->SetHeader('Content-Type', 'text/plain; charset=utf-8');
		if (!is_null($currentPassword))
		{
			$Plug = $Plug->With('currentpassword', $currentPassword);
			// if the change is successful we need to login the user again
			$renewLogin = true;
		}
		
		if (!is_null($setAlternate) && $setAlternate)
		{
			$Plug = $Plug->With('altpassword', 'true');
			// when setting the alternate, if not admin, then need to specify the api key
			// required for the forgot password feature
			if (!DekiUser::getCurrent()->can('ADMIN'))
			{
				$Plug = $Plug->WithApiKey();
			}
		}

		$Result = $Plug->Put($newPassword);
		if ($Result->isSuccess() && $renewLogin)
		{
			// generate a new authtoken
			DekiUser::login($this->getUsername(), $newPassword, $this->getAuthService()->getId());
		}
		
		return $Result;
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
				$name = $Group->getName();

				$success &= $Group->removeUser($this->getId());

				unset($this->groups[$id]);
				unset($this->groupNames[$name]);
			}
		}

		foreach ($newGroups as $id)
		{
			if (!isset($this->groups[$id]))
			{
				$Group = DekiGroup::newFromId($id);

				if (!is_null($Group))
				{
					$name = $Group->getName();

					$success &= $Group->addUser($this->getId());

					$this->groups[$id] = $Group;
					$this->groupNames[$name] = $id;
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
	
	/**
	 * Set the seat information from the result
	 * @param mixed $seatXml
	 */
	protected function setSeatStatus($seatXml)
	{
		if (is_null($seatXml))
		{
			// seat management is not enabled
			$this->seatStatus = null;
		}
		else if (is_array($seatXml) && isset($seatXml['@owner']) && ($seatXml['@owner'] == 'true'))
		{
			$this->seatStatus = self::SEAT_STATUS_OWNER;
		}
		else
		{
			$this->seatStatus = $seatXml == 'true' ? self::SEAT_STATUS_SEATED : self::SEAT_STATUS_UNSEATED;
		}
	}
	
	protected function setGroups($groups = array())
	{
		$this->groups = array();

		if (!empty($groups))
		{
			foreach ($groups as $group)
			{
				$DekiGroup = DekiGroup::newFromArray($group);
				$id = $DekiGroup->getId();
				$name = $DekiGroup->getName();

				$this->groups[$id] = $DekiGroup;
				$this->groupNames[$name] = $id;
			}
		}
	}
	
	protected function setProperties($properties = array())
	{
		$this->Properties = DekiUserProperties::newFromArray($properties);
	}
	
	protected function lazyLoad()
	{
		if (!$this->loaded)
		{
			// populate the fields that were skipped originally
			$UserPlug = DekiPlug::getInstance()->At('users', $this->id);
			$Result = $UserPlug->Get();
			if (!$Result->isSuccess())
			{
				return null;
			}
			$result = $Result->getVal('body/user');
			$X = new XArray($result);
			$this->setGroups($X->getAll('groups/group', array()));
			
			$this->loaded = true;
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
			'status' => $this->getStatus(),
			// core user options
			'timezone' => $this->getTimezone(),
			'language' => DekiLanguage::isSitePolyglot() ? $this->getLanguage(): ''
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
