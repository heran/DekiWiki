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

define('DEKI_ADMIN', true);
require_once('index.php');


class GroupManagement extends DekiControlPanel
{
	// needed to determine the template folder
	protected $name = 'group_management';


	public function index()
	{
		$this->executeAction('listing');
	}
	
    public function find()
    {
        if ($this->Request->isXmlHttpRequest())
        {
	        $Plug = $this->Plug->At('groups');
            $Plug = $Plug->With('groupnamefilter', $this->Request->getVal('q'));
            $Plug = $Plug->With('limit', 15);
            
            $Result = $Plug->Get();
            if (!$Result->isSuccess())
            {
                return;
            }
            
            $groups = $Result->getAll('/body/groups/group', array());            
            $return = "";

            if (!empty($groups))
            {
                foreach ($groups as $groupArray)
                {
                    $Group = DekiGroup::newFromArray($groupArray);
                    $return .= $Group->getName() . "\n";
                }
            }
            
            $this->View->set("groups", $return);
            $this->View->text("groups");
        }
    }

	public function listing()
	{
		global $wgLang;

		if ($this->Request->isPost())
		{
			$action = $this->Request->getVal('action');
			switch ($action)
			{
				default:
					// search terms posted in, redirect with get params
					$this->Request->redirect($this->getUrl('listing', array('query' => $searchQuery)));
					break;

				case 'delete':
					$groupIds = $this->Request->getArray('group_id');
					if (empty($groupIds))
					{
						DekiMessage::error($this->View->msg('Groups.data.no-selection'));
						$this->Request->redirect($this->getUrl('listing'));
						return;
					}
					$this->executeAction($action);
					return;
			}
		}
		

		// build the listing table
		$Table = new DekiTable($this->name, 'listing', array('id', 'name', 'role', 'service'));
		$Table->setResultsPerPage(Config::RESULTS_PER_PAGE);
		// enable searching for this table
		$Table->setSearchField('groupnamefilter');
		// set the default sorting
		$Table->setDefaultSort('name');
		// space the columns
		$Table->setColWidths('18', '', '150', '100', '100', '');
		$Table->addClass('groups');

		// create the table header
		$Table->addRow();
		$Th = $Table->addHeading(DekiForm::singleInput('checkbox', 'all', '', array()));
		$Th->addClass('last checkbox');
		$Th = $Table->addSortHeading($this->View->msg('Groups.data.name'), 'name');
		$Th->addClass('name');
		$Table->addSortHeading($this->View->msg('Groups.data.role'), 'role');
		
		// authentication header
		if (!$this->isRunningCloud)
		{
			$Table->addSortHeading($this->View->msg('Groups.data.authentication'), 'service');
		}
		$Th = $Table->addHeading('&nbsp;');
		$Th->addClass('edit');
		$Th = $Table->addHeading('<span>'.$this->View->msg('Groups.users').'</span>');
		$Th->addClass('last groupusers');

		// grab the results
		$Plug = $this->Plug->At('groups');
		$Result = $Table->getResults($Plug, '/body/groups/@querycount');
		$Result->handleResponse();

		$groups = $Result->getAll('/body/groups/group', array());
		
		$resultCount = count($groups);
		if ($resultCount == 0)
		{
			$Table->setAttribute('class', 'table none');
			$Table->addRow();
			$Td = $Table->addCol('<div class="none">'.$this->View->msg('Groups.data.empty').'</div>');
			$Td->setAttribute('colspan', 6);
			$Td->setAttribute('class', 'last');
		}
		else
		{
			// @note usability enhancement prescribed by royk
			if ($resultCount == 1 && $this->Request->has('query'))
			{
				$Object = DekiGroup::newFromArray(current($groups));
				$editUrl = $this->getUrl('edit/'.$Object->getId());
				// redirect
				$this->Request->redirect($editUrl);
				return;
			}
			
			foreach ($groups as $groupArray)
			{
				$Group = DekiGroup::newFromArray($groupArray);
	
				$Table->addRow();
				$Td = $Table->addCol(DekiForm::singleInput('checkbox', 'group_id[]', $Group->getId(), array('id' => 'group'.$Group->getId()), $Group->toHtml()));
				$Td->setAttribute('colspan', 2);

				// role column
				$Table->addCol($Group->getRole()->getDisplayName());

				// authenication column
				if (!$this->isRunningCloud)
				{
					if ($Group->isAuthInvalid())
					{
						DekiMessage::error(wfMsg('Common.error.invalid-auth', $Group->toHtml()));
						$Table->addCol('&nbsp;');
					}
					else
					{
						$Table->addCol($Group->getAuthService()->toHtml());
					}
				}
				
				$Td = $Table->addCol('<a href="'. $this->getUrl('edit/' . $Group->getId(), array('page'), true) .'">'. $this->View->msg('Groups.edit') .'</a>');
				$Td->addClass('edit');
				$setUsersUrl = $this->getUrl('users/' . $Group->getId(), array('page'), true);
				if ($Group->isInternal()) 
				{
					$Td = $Table->addCol('<a href="'. $setUsersUrl .'">'
						. $this->View->msg('Groups.data.user.count', $Group->getUserCount()) .'</a>'
					);
				}
				else
				{
					$Td = $Table->addCol('<a href="'. $setUsersUrl .'" title="'.$this->View->msg('Groups.users.noadd').'" class="noexternal">'
						. $this->View->msg('Groups.data.user.count', $Group->getUserCount()) .'</a>'
					);
				}
				$Td->addClass('last groupusers');
			}
		}

		// init autocomplete & template search fields
		$this->setupTemplateSearch();
		$this->View->set('addGroupUrl', $this->getUrl('add'));

		$this->View->set('operations-form.action', $this->getUrl('listing', array('page'), true));

		$this->View->set('groups-table', $Table->saveHtml());
		$this->View->set('searchQuery', $Table->getCurrentSearch());
		$this->View->output();
	}


	public function add()
	{
		$groups = array();
		if ($this->Request->isPost())
		{
			$groups = preg_split("/[,\n]+/", $this->Request->getVal('groups', ''));
			$roleId = $this->Request->getVal('role_id');

			$authType = $this->Request->getVal('auth_type', 'local');
			$authUsername = null;
			$authPassword = null;

			$authServiceId = DekiAuthService::INTERNAL_AUTH_ID;
			if ($authType == 'external')
			{
				$authServiceId = $this->Request->getInt('external_auth_id');
				$authUsername = $this->Request->getVal('external_auth_username');
				$authPassword = $this->Request->getVal('external_auth_password');
			}
			
			$hasError = false;
			
			foreach ($groups as $key => $groupName)
			{
				$groupName = trim($groupName);
				if (empty($groupName))
				{
					unset($groups[$key]);
					continue;
				}

				$Group = new DekiGroup(null, $groupName, $roleId, $authServiceId);
				$Result = $Group->create($authUsername, $authPassword);
				if ($Result->handleResponse())
				{
					unset($groups[$key]);
					//todo: add a small "Edit this group?" link at the end
					DekiMessage::success($this->View->msg('Groups.success.added', $Group->getName())); 
				}
				else
				{
					$hasError = true;
					DekiMessage::error($this->View->msg('Groups.error.added', $Group->getName(), $Result->getError()));
				}
			}

			if (!$hasError) 
			{
				$this->Request->redirect($this->getUrl('listing', array('sortby' => '-id')));
				return;
			}
		}

		// init autocomplete & template search fields
		$this->setupTemplateSearch();
		// begin setting up the view variables
		$this->View->set('add-form.action', $this->getUrl('add'));
		$this->View->set('add-form.back', $this->getUrl('listing', array('page'), true));
		$this->View->set('add-form.submit', DekiForm::singleInput('button', 'submit', 'submit', array(), $this->View->msg('Groups.add.button')));
		
		// royk: adding the form outputs directly from the controller
		$this->View->set('add-form.input-names', DekiForm::singleInput('textarea', 'groups', implode("\n", $groups)));
		
		// setup the role selection
		$siteRoles = DekiRole::getSiteList();
		$roles = array();
		$defaultRoleId = null;
		global $wgNewAccountRole; //this gets the default role for created users & groups
		
		foreach ($siteRoles as $Role) 
		{
			$roles[$Role->getId()] = $Role->getDisplayName();

			if (strcmp($wgNewAccountRole, $Role->getName()) == 0) 
			{
				$defaultRoleId = $Role->getId();	
			}
		}
		$this->View->set('add-form.select-roles', DekiForm::multipleInput('select', 'role_id', $roles, $defaultRoleId));

		// setup the authentication types
		$this->View->set('form.auth-section', $this->renderAction('auth_form_section'));
		
		$this->View->output();
	}
	
	
	public function edit($id = null)
	{
		// need to find the group
		$Group = DekiGroup::newFromId($id);
		if (is_null($Group))
		{
			DekiMessage::error($this->View->msg('Groups.error.notfound-group'));
			$this->Request->redirect($this->getUrl('listing', array('page'), true));
			return;
		}
		
		// check if we are updating the info
		if ($this->Request->isPost())
		{
			
			do
			{
				// set the role
				$roleId = $this->Request->getInt('role_id', $Group->getRole()->getId());
				$Role = DekiRole::newFromId($roleId);

				if ($Role == null)
				{
					// something went wrong while retrieving the role
					DekiMessage::error($this->View->msg('Groups.error.norole'));
					break;
				}
				else
				{
					$Group->setRole($Role);
				}


				// update the group fields
				$groupName = $this->Request->getVal('group_name', $Group->getName());
				if (empty($groupName))
				{
					DekiMessage::error($this->View->msg('Groups.error.name'));
					break;
				}
				$Group->setName($groupName);


				$Result = $Group->update();
				if (!$Result->handleResponse())
				{
					// there was an error
					DekiMessage::error($this->View->msg('Groups.error.edited'));
					break;
				}
				else
				{
					//todo: add a small "Edit this group?" link at the end
					DekiMessage::success($this->View->msgRaw('Groups.success.edited', $Group->toHtml()));
				}


				// group was updated successfully
				$this->Request->redirect($this->getUrl('listing', array('page'), true));
				return;
			} while (false);
		}

		// init autocomplete & template search fields
		$this->setupTemplateSearch();
		// begin setting up the view variables
		$this->View->set('edit-form.action', $this->getUrl('edit/' . $Group->getId(), array('page'), true));
		$this->View->set('edit-form.back', $this->getUrl('listing', array('page'), true));
		$this->View->set('edit-form.submit', DekiForm::singleInput('button', 'submit', 'submit', array(), $this->View->msg('Groups.edit.button')));
		
		$this->View->set('group.name', $Group->getName());
		$this->View->set('group.isInternal', $Group->isInternal());

		// setup the role selection
		$siteRoles = DekiRole::getSiteList();
		$roles = array();
		foreach ($siteRoles as $Role) 
		{
			$roles[$Role->getId()] = $Role->getDisplayName();
		}
		$this->View->set('edit-form.select-roles', 
			DekiForm::multipleInput('select', 'role_id', $roles, $this->Request->getVal('role_id', $Group->getRole()->getId()))
		);

		// set up the authentication views
		if (!$this->isRunningCloud)
		{
			$this->View->set('edit-form.authentication', 
				$this->View->msg('Groups.form.authentication.edit', 
					$Group->isInternal() 
						? $this->View->msg('Groups.type.local') 
						: $this->View->msg('Groups.type.external').' - '.$Group->getAuthService()->toHtml()
				)
			);
		}
		
		$this->View->output();
	}

	public function users($id = null)
	{
		// need to find the group
		$Group = DekiGroup::newFromId($id);
		if (is_null($Group))
		{
			DekiMessage::error($this->View->msg('Groups.error.notfound-group'));
			$this->Request->redirect($this->getUrl('listing', array('page'), true));
			return;
		}

		// check if we are updating the info for internal groups only
		if ($this->Request->isPost() && $Group->isInternal())
		{
			do
			{
				$action = $this->Request->getVal('action');
				$value = $this->Request->getVal('set_value');
				$users = $this->Request->getArray('user_id');
				
				switch ($action)
				{
					case 'remove':
						if (empty($users))
						{
							DekiMessage::error($this->View->msg('Groups.data.no-selection'));
							break;
						}

						$Group->removeUsers($users);
						DekiMessage::success($this->View->msg('Groups.success.user-removed'));
						break;

					case 'add_group':
						// adding an entire group
						$AddGroup = DekiGroup::newFromText($value);
						if (is_null($AddGroup))
						{
							// could not find the group
							DekiMessage::error($this->View->msg('Groups.error.notfound-group'));
							break 2;
						}

						$Group->addUsers($AddGroup->getUsers());
						DekiMessage::success($this->View->msg('Groups.success.users', $AddGroup->getName()));
						break;

					default:
					case 'add_user':
						// adding a new user
						$User = DekiUser::newFromText($value);
						if (is_null($User))
						{
							// could not find the group
							DekiMessage::error($this->View->msg('Groups.error.notfound-user'));
							break 2;
						}

						$Group->addUser($User->getId());
						DekiMessage::success($this->View->msg('Groups.success.user', $User->getName()));
						break;
				}

				$this->Request->redirect($this->getUrl('users/'. $Group->getId(), array('page'), true));
				return;
			} while (false);
		}



		// build the listing table
		$Table = new DekiTable(
			$this->name,
			'users/'. $Group->getId(),
			array('id', 'username', 'nick', 'email', 'fullname', 'date.lastlogin', 'status'),
			'users-page'
		);
		$Table->setResultsPerPage(Config::RESULTS_PER_PAGE);
		// set the default sorting
		$Table->setDefaultSort('username');
		// space the columns
		$Table->setColWidths('18', '150', '');

		// create the table header
		$Table->addRow();
		$Th = $Table->addHeading(DekiForm::singleInput('checkbox', 'all', '', array()));
		$Th->addClass('last checkbox');
		$Th = $Table->addSortHeading($this->View->msg('Groups.data.username'), 'username');
		$Th->addClass('name');
		$Th = $Table->addSortHeading($this->View->msg('Groups.data.email'), 'email');
		$Th->addClass('last');
		
		// grab the results
		$Plug = $this->Plug->At('groups', $Group->getId(), 'users');
		$Result = $Table->getResults($Plug, '/body/users/@querycount');
		if (!$Result->handleResponse())
		{
			$this->Request->redirect($this->getUrl('listing'));
			return;
		}
		
		$users = $Result->getAll('body/users/user', array());
		if (empty($users)) 
		{
			$Table->setAttribute('class', 'table none');
			$Table->addRow();
			$Td = $Table->addCol('<div class="none">'.$this->View->msg('Groups.data.empty.users').'</div>');
			$Td->setAttribute('colspan', 3);
			$Td->setAttribute('class', 'last');
		}
		else 
		{
			foreach ($users as $userArray)
			{
				$User = DekiUser::newFromArray($userArray);

				$Table->addRow();
				$Td = $Table->addCol(
					DekiForm::singleInput(
						'checkbox', 'user_id[]', $User->getId(), array('id' => 'userId'.$User->getId()), $User->toHtml()
					)
				);
				$Td->setAttribute('colspan', 2);
				$Td = $Table->addCol($User->getEmail());
				$Td->addClass('last');
			}
		}

		// init autocomplete & template search fields
		$this->setupTemplateSearch();
		$this->View->set('set-form.action', $this->getUrl('users/'. $Group->getId(), array('page'), true));
		$this->View->set('set-form.back', $this->getUrl('listing', array('page'), true));

		$this->View->set('operations-form.action', $this->getUrl('users/'.$Group->getId(), array('page'), true));
		

		$this->View->set('users-table', $Table->saveHtml());

		$this->View->set('group.name', $Group->getName());
		$this->View->set('group.isInternal', $Group->isInternal());
		$this->View->output();

	}

	public function delete()
	{
		if (!$this->Request->isPost())
		{
			$this->Request->redirect($this->getUrl('listing'));
			return;
		}

		$groupIds = $this->Request->getVal('group_id', array());
		$operate = $this->Request->getBool('operate');
		
		if ($operate)
		{
			// delete the checked groups
			foreach ($groupIds as $key => $groupId)
			{
				$Group = DekiGroup::newFromId($groupId);
				if (!is_null($Group))
				{
					$Result = $Group->delete();
					if ($Result->handleResponse())
					{
						DekiMessage::success($this->View->msg('Groups.success.deleted', $Group->getName()));
					}
					else
					{
						DekiMessage::error($this->View->msg('Groups.error.deleted', $Group->getName()));	
					}
				}
				else
				{
					// group could not be found
					DekiMessage::error($this->View->msg('Groups.error.deleted', $groupId));	
				}
			}
			
			$this->Request->redirect($this->getUrl('listing'));
			return;
		}
		
		$html = '';
		foreach ($groupIds as $groupId)
		{
			$Group = DekiGroup::newFromId($groupId);
			if (!is_null($Group))
			{
				$html .= '<li>'. $Group->toHtml() .'<input type="hidden" name="group_id[]" value="'. $Group->getId() .'" />'.'</li>';
			}
		}
		
		// view variables
		$this->View->set('form.action', $this->getUrl('delete'));
		$this->View->set('form.back', $this->getUrl('listing', array('page'), true));

		$this->View->set('group-list', $html);
		$this->View->output();
	}

	
	private function getUserOptions()
	{
		$siteUsers = DekiUser::getSiteList(array('usernamefilter' => ''));
		$html = '';

		foreach ($siteUsers as $User)
		{
			$id = $User->getId();
			$html .= '<option value="'. $id .'">' . $User->getUsername() . '</option>';
		}

		return $html;
	}
	
	private function getRoleSelect($selectedRoleId = null)
	{
		// setup the role selection
		$siteRoles = DekiRole::getSiteList();

		$data = array();
		foreach ($siteRoles as $Role) 
		{
			$data[$Role->getId()] = $Role->getDisplayName();
		}

		return DekiForm::multipleInput('select', 'role_id', $data, $selectedRoleId);
	}

	/*
	 * Renders a form section for setting authentication
	 */
	protected function auth_form_section($authType = 'local', $authId = null)
	{
		if ($this->isRunningCloud)
		{
			// MT-9621 Control Panel > Remove LDAP authentication everywhere (Add/Edit/Add Multiple/etc)
			return;
		}
		
		$this->View->set('auth-form-section.authType', $authType);
		$this->View->set('auth-form-section.authId', $authId);
		
		// build the data for the auth select
		$siteAuth = DekiAuthService::getSiteList();
		$data = array();
		
		foreach ($siteAuth as $Service)
		{
			$id = $Service->getId();
			if ($id != 1)
			{
				$data[$id] = $Service->getDescription();
			}
		}
		$this->View->setRef('auth-form-section.external-options', $data);

		$this->View->output();
	}
	
	
	/**
	 * Initializes the template variables for searching and adds
	 * the autocomplete for this controller
	 */
	protected function setupTemplateSearch()
	{
		$this->View->includeCss('jquery.autocomplete.css');
		$this->View->includeJavascript('jquery.autocomplete.js');
		$this->View->includeJavascript('groups.autocomplete.js');
		
		$this->View->set('template.search.action', $this->getUrl('listing'));
		$this->View->set('template.search.title', $this->View->msg('Groups.search.label'));
	}
}

new GroupManagement();
