<?php
/*
 * MindTouch Deki - enterprise collaboration and integration platform
 * Copyright (C) 2006-2008 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
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
	define('DEKI_MOBILE', true);
	require_once('index.php');

	class DekiLogin extends DekiController
	{
		protected $name = 'login';

		public function index()
		{// basic login form

			$User = DekiUser::getCurrent();
			if (!$User->isAnonymous())
			{
				$this->Request->redirect($this->Request->getLocalUrl('index'));
			}
			$this->View->includeJavascript('jquery.autocomplete.js');
			$this->View->includeCss('jquery.autocomplete.css');
			$this->View->includeJavascript('users.autocomplete.js');
			$this->View->includeCss('login.css');
			$this->View->set('head.title', 'MindTouch Deki Mobile | Login');
			$this->View->set('disableNav', true);

			$this->setSiteList();
			$returnTo = ($this->Request->getVal('returnTo') ? $this->Request->getVal('returnTo') :  $this->Request->getLocalUrl('index'));

			$this->View->set('returnTo', $returnTo);
			$this->View->output();
		}

		public function ajax($type = null)
		{	
			switch ($type)
			{
				case 'login':
					echo $this->login();
					break;
				
				case 'logout':
					echo $this->logout();
					break;
				
				case 'find':
					$this->findUsers();
					break;

				default:
					echo '404 Not found';
			}

			exit();
		}

		protected function login()
		{
			$userName = $this->Request->getVal('username');
			$password = $this->Request->getVal('password');
			$authId = $this->Request->getVal('authid');
			$returnTo = ($this->Request->getVal('returnTo') ? $this->Request->getVal('returnTo') : $this->Request->getUrl('index'));
			if($returnTo == 'userpage') 
			{
				$returnTo = $this->Request->getLocalUrl('page',null, array('title' => 'User:' . $userName));
			}
			if (DekiUser::login($userName, $password, $authId))
			{
				return $returnTo;
			}
	
			return '404 Login failed.';
		}

		protected function logout()
		{
			DekiUser::logout();
			$returnTo = ($this->Request->getVal('returnTo') ? $this->Request->getVal('returnTo') : $this->Request->getLocalUrl('index'));
			return $returnTo;
		}

		protected function findUsers()
		{
			$return = '';
		
			try
			{
				$filters = array('usernamefilter' => $this->Request->getVal('q'));
				$siteUsers = DekiUser::getSiteList($filters, 1, 10);

				if (!empty($siteUsers))
				{
					foreach ($siteUsers as &$User)
					{
						$return .= $User->getName() . "\n";
					}
				}	
			}	
			catch (Exception $e) {}
		
			$this->View->set('autocomplete.users', $return);
			$this->View->text('autocomplete.users');
		}

		protected function setSiteList()
		{
			$siteList = DekiAuthService::getSiteList();
			$defaultId = DekiAuthService::getDefaultProviderId();
			$defaultId = is_null($defaultId) ? DekiAuthService::INTERNAL_AUTH_ID : $defaultId;

			if ($siteList)
			{
				$options = array();
				foreach($siteList as $siteKey => $siteInfo)
				{
					$options[] = array('id' => $siteInfo->getId(), 'name' => $siteInfo->getDescription());
				}

				if (count($options) > 1)
				{
					$this->View->setRef('siteList', $options);
					$this->View->set('defaultId', $defaultId);
				}
			}
		}



	}

	new DekiLogin;
