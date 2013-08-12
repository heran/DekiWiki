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

class DekiControlPanel extends DekiController
{
	const CLOUD_CONFIG_KEY = 'site/limited-admin-permissions';
	const HOOK_INITIALIZE_ACTION = 'ControlPanel:InitializeAction';
	
	/**
	 * True if seat management is enabled. Convenience.
	 * @var bool
	 */
	protected $canManageSeats = false;
	
	/**
	 * True if instance is running in the cloud. Convenience.
	 * @var bool
	 */	
	protected $isRunningCloud = false;

	protected function initializeObjects()
	{
		parent::initializeObjects();
		
		// set the global admin controller for error reporting
		global $wgAdminController;
		// set the active controller
		$wgAdminController = $this;
		
		$License = DekiLicense::getCurrent();
		$this->canManageSeats = $License->hasCapabilitySeats();
		$defaultStatus = DekiSite::isRunningCloud();
		$this->isRunningCloud = wfGetConfig(self::CLOUD_CONFIG_KEY, $defaultStatus);
	}

	protected function initialize()
	{
		$User = DekiUser::getCurrent();
		if ($User->isAnonymous())
		{
			// redirect to control panel login
			if ($this->name != 'login')
			{
				$this->Request->redirect($this->Request->getLocalUrl('login', null, array('returnurl' => $this->Request->getLocalUri())));
				return;
			}
		}
		// make sure the current user has admin rights
		else if (!$User->can('ADMIN'))
		{
			// user does not have control panel access
			DekiMessage::ui(wfMsg('Common.error.no-cp-access'));
			$this->Request->redirect('/');
			return;
		}

		// follow the normal controller code path
		parent::initialize();
	}
	
	protected function initializeAction($action)
	{
		$init = parent::initializeAction($action);
		if (!$init)
		{
			return false;
		}

		// check if plugins want to redirect request
		$redirectTo = '';
		$result = DekiPlugin::executeHook(self::HOOK_INITIALIZE_ACTION, array($this->name, $action, &$redirectTo));
		if ($result == DekiPlugin::HANDLED_HALT)
		{
			$this->Request->redirect(empty($redirectTo) ? '/' : $redirectTo);
			return false;
		}
		return true;
	}
	
}
