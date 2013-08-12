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
 * Class handles registering a page subscriptions with
 * the Deki pubsub service
 */
class DekiPageAlert
{
	// value the API expects for a tree subscription
	const SUBSCRIBE_TREE = 'infinity';
	
	// no alerts are configured for the page
	const STATUS_OFF = 0;
	// page is directly subscribed
	const STATUS_SELF = 1;
	// page is parent of tree subscription
	const STATUS_TREE = 2;
	// page is subscribed through parent, readonly state
	const STATUS_PARENT = 3;
	// @type enum - see above
	protected $status = 0;
	
	/**
	 * @type string
	 * @note stores the X-Deki-Site/id header from the api, required for the pubsub service
	 * tells the service what wiki is making the subscription
	 */
	protected $siteId = null;
	protected $pageId = null;
	protected $parentIds = array();
	
	// if status == parent then below stores subscribing page id
	protected $subscriberPageId = null;
	
	private $loaded = false;
	
	/**
	 * @param string $siteId - if this is not specified, the global value is used from $wgDekiSiteId
	 */
	public function __construct($pageId, $parentIds = array(), $siteId = null)
	{
		global $wgDekiSiteId;
		$this->siteId = is_null($siteId) ? $wgDekiSiteId : $siteId;

		$this->pageId = $pageId;
		$this->parentIds = $parentIds;
		// set the default status
		$this->status = self::STATUS_OFF;
	}

	/**
	 * This returns the pageId of the subscribing parent if status == PARENT
	 * @return int - returns a page id > 0 if the parent exists
	 */
	public function getSubscriberId()
	{
		$status = $this->getStatus();
		if ($status == self::STATUS_PARENT)
		{
			return $this->subscriberPageId;
		}
		
		// not valid for the current status
		return -1;
	}
	
	/**
	 * Convenience method to determine if a page is subscribed at all
	 * @return bool - true if the page is subscribed via any method
	 */
	public function isSubscribed() { return $this->getStatus() != self::STATUS_OFF; }
	
	public function getStatus()
	{
		$this->load();
		return $this->status;
	}
	
	/**
	 * Sets the page alert status
	 * @param $status - enum
	 * @return DekiResult - if a request is made, an object, otherwise null
	 */
	public function setStatus($status)
	{
		$current = $this->getStatus();

		if ($current == self::STATUS_PARENT || $current == $status)
		{
			// cannot update subscriptions in this state
			// or no change required
			return null;
		}
		else
		{
			$Plug = $this->getPlug();
			$Plug = $Plug->At('pages', $this->pageId);
			
			switch ($status)
			{
				default:
				case self::STATUS_PARENT:
					// cannot update to this state, readonly
					return null;

				case self::STATUS_OFF:
					$Result = $Plug->Delete();
					break;
				
				case self::STATUS_TREE:
					$Plug = $Plug->With('depth', self::SUBSCRIBE_TREE);
				case self::STATUS_SELF:
					$Result = $Plug->Post();
					break;
			}
			
			if ($Result->isSuccess())
			{
				$this->status = $status;
			}
			
			return $Result;
		}
	}
	
	protected function load()
	{
		if ($this->loaded)
		{
			return;
		}
		
		$Plug = $this->getPlug();
		// setup the plug
		$Plug = $Plug->At('subscriptions')->At($this->pageId);
		$Result = $Plug->Get();
		
		// TODO: should this fail silently or show an error message?
		if ($Result->isSuccess())
		{
			$pages = $Result->getAll('body/subscriptions/subscription.page', array());
			$pageSubscriptions = array();
			foreach ($pages as &$page)
			{
				$type = $page['@depth'] == self::SUBSCRIBE_TREE ? self::STATUS_TREE : self::STATUS_SELF;
				$pageSubscriptions[$page['@id']] = $type;
			}
			unset($page);
			
			// reset the parent subscriber page id
			$this->subscriberPageId = null;
			
			// determine this page's subscription status
			if (isset($pageSubscriptions[$this->pageId]))
			{
				$this->status = $pageSubscriptions[$this->pageId];
			}
			else
			{
				// default to no subscription status
				$this->status = self::STATUS_OFF;

				// page might be subscribed via a parent page
				if (!empty($pageSubscriptions))
				{
					// determine if a parent is providing a tree subscription
					// traverse up the parent tree to find nearest tree subscription
					foreach ($this->parentIds as $pageId)
					{
						// check if the page has a subscriptions and if it is a tree subscription
						if (isset($pageSubscriptions[$pageId]) && $pageSubscriptions[$pageId] == self::STATUS_TREE)
						{
							// found the subscribing parent
							$this->subscriberPageId = $pageId;
							$this->status = self::STATUS_PARENT;
							break;
						}
					}
				}
			}
		}		
		
		$this->loaded = true;
	}
	
	/**
	 * Convenience method for obtaining the subscription plug
	 */
	protected function getPlug()
	{
		$Plug = DekiPlug::getInstance();
		$Plug = $Plug->At('pagesubservice')->With('siteId', $this->siteId);
		
		// create a new plug at the subscription end point
		return $Plug;
	}
}
