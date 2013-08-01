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


class DekiBan implements IDekiApiObject
{
	private $id = null;
	private $ip = null;
	private $type = null;
	private $reason = null;
	private $created = null;
	private $bantype = null;
	private $expiry = null;
	public $Bannee = null;
	public $Bannor = null;
	private $banmask = null; //see the constructor ... should this be moved elsewhere?

	static function &newFromId($id)
	{
		$Ban = self::load($id);
		return $Ban;
	}

	/**
	 * Pass in the api group array to get a group object
	 * Using this method enables the group fields to be expanded later, more functionality
	 */
	static function &newFromArray(&$result)
	{
		$Result = new DekiResult($result);
		$Ban = new DekiBan(
			$Result->getVal('@id'), 
			$Result->getVal('ban.addresses/address'),
			$Result->getVal('date.modified'), 
			$Result->getVal('date.expires'), 
			$Result->getVal('description'), 
			$Result->getVal('ban.users/user/username'), 
			$Result->getVal('user.createdby/username')
		);
		return $Ban;
	}

	static function &newFromText($text)
	{
		return self::newFromId($name);
	}
	
	private static function load($id)
	{
		global $wgAdminPlug;

		$Plug = $wgAdminPlug->At('site', 'bans', $id);

		$Result = $Plug->Get();
		if (!$Result->isSuccess())
		{
			return null;
		}

		$result = $Result->getVal('body/ban');
		return self::newFromArray($result);
	}

	public function __construct($id = 0, $ip = null, $created = null, $expiry = null, $reason = null, $bannee = null, $bannor = null)
	{
		$this->id = $id;
		$this->ip = $ip;
		$this->created = $created;
		$this->expiry = $expiry;
		$this->reason = $reason;
		$this->expiry = $expiry;
		$this->Bannee = DekiUser::newFromText($bannee);
		$this->Bannor = DekiUser::newFromText($bannor);
		$this->bantype = !is_null($ip) ? 'ip': 'username';
		$this->banmask = 'BROWSE,READ,SUBSCRIBE,UPDATE,CREATE,DELETE,CHANGEPERMISSIONS,CONTROLPANEL,ADMIN'; //configurable?
	}
	
	public function getId() { return $this->id; }
	
	// no such thing as a ban 'name'; let's keep this distinct from the banned username
	public function getName() { return $this->id; }
	
	public function getExpiry($return = null, $asArray = false)
	{ 
		if ($asArray && !is_null($this->expiry)) 
		{
			$expiry = wfTimestamp(TS_UNIX, $this->expiry);
			return array('year' => date('Y', $expiry), 'month' => date('m', $expiry), 'day' => date('d', $expiry));
		}
		return is_null($this->expiry) ? $return: $this->expiry; 
	}
	
	public function setExpiry($expiry, $absts = null)
	{
		$timestamp = wfTimestamp(TS_UNIX);
		switch ($expiry) 
		{
			case 'infinity':
				$timestamp = null;
			break;
			case 'custom':
				$timestamp = $absts;
			break;
			default:
				$timestamp = $timestamp + $expiry;
		}
		$this->expiry = is_null($timestamp) ? null: wfTimestamp(TS_DREAM, $timestamp);
		return $timestamp;
	}
		
	public function getModified()
	{ 
		global $wgLang;
		return $wgLang->date($this->created);
	}
	
	public function setBannedUser($type, $user)
	{
		$this->setBanType($type);
		if ($type == 'ip') 
		{
			$this->ip = $user;
		}
		
		if ($type == 'username') 
		{
			$User = DekiUser::newFromText($user);
			if (!is_null($User)) 
			{
				$this->Bannee = $User;
			}
		}
	}
	
	public function getBannedName()
	{
		if (!is_null($this->Bannee))
		{
			return $this->Bannee->getName();
		} 
		return $this->ip;
	}
	
	public function getReason() { return $this->reason; }
	public function setReason($reason) { $this->reason = $reason; }
	public function setBanType($type) { $this->bantype = $type; }
	public function getBanType($default = null) { return is_null($this->bantype) ? $default: $this->bantype; }
	public function getBannorName() { return $this->Bannor->getName(); }
	
	public function toArray()
	{
		$ban = array(
			'permissions.revoked' => array('operations' => $this->banmask), 
			'description' => $this->getReason()
		);
		if (!is_null($this->getExpiry())) 
		{
			$ban['date.expires'] = $this->getExpiry();	
		}
		if ($this->getBanType() == 'ip') 
		{
			$ban['ban.addresses']['address'] = $this->getBannedName();
			$ban['ban.users'] = array();
		}
		else
		{
			$ban['ban.addresses'] = array();
			$ban['ban.users'] = array('user' => array('@id' => $this->Bannee->getId(), 'username' => $this->Bannee->getName()));
		}
		return array('ban' => $ban);
	}
	
	public function toXml() {}
	public function toHtml()
	{
		$html = $this->getBannedName();
		$reason = $this->getReason();
		if (!is_null($reason))
		{
			$html .= '; '. $reason;
		}

		return htmlspecialchars($html);
	} 
}
