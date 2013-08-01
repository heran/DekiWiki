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


class RoleManagement extends DekiControlPanel
{
	// needed to determine the template folder
	protected $name = 'role_management';


	public function index()
	{
		if ($this->Request->isPost())
		{
			do
			{
				
				$roleId = $this->Request->getInt('role_id', -1);
				$role = $this->Request->getVal('role', array());
				$roleName = $this->Request->getVal('role_name');
				$operations = isset($role[$roleId]) ? array_keys($role[$roleId]) : array();

				if ($roleId == -1)
				{
					// messed up post
					$this->Request->redirect($this->getUrl());
					return;
				}

				if ($roleId == 0)
				{
					// new role

					if (empty($roleName))
					{
						DekiMessage::error($this->View->msg('Roles.error.blank'));
						break;
					}

					// get the site roles as an array
					$siteRoles = DekiRole::getSiteList();
					$roles = array();
					foreach ($siteRoles as $Role) 
					{
						$roles[] = strtolower($Role->getName());
					} 
					if (in_array($roleName, $roles)) 
					{
						DekiMessage::error($this->View->msg('Roles.error.exists'));
						break;
					}
					
					$Role = new DekiRole(null, $roleName, $operations);
					$Result = $Role->create();
					if (!$Result->handleResponse())
					{
						DekiMessage::error($this->View->msg('Roles.error.general'));
						break;
					}
					DekiMessage::success($this->View->msgRaw('Roles.success.created', $Role->getName()));
				}
				else
				{
					// update existing role

					// load the role
					$Role = DekiRole::newFromId($roleId);
					$Role->setOperations($operations);
					$Result = $Role->update();
					if (!$Result->handleResponse())
					{
						DekiMessage::error($this->View->msg('Roles.error.general', $Result->getError()));
						break;
					}
					DekiMessage::success($this->View->msg('Roles.success.updated', $Role->getName()));
				}

				$this->Request->redirect($this->getUrl());
				return;
			} while(false);
		}


		$siteRoles = DekiRole::getSiteList();
		$siteOperations = DekiRole::getSiteOperations();

		// build the role table
		$Table = new DomTable();
		// set the column widths
		$colwidths = array_fill(0, count($siteRoles)+1, 120);
		$colwidths[] = 140;
		call_user_func_array(array($Table, 'setColWidths'), $colwidths);

		// creating the headings
		$Table->addRow();
		$Table->addHeading('&nbsp;');
		foreach ($siteRoles as $Role)
		{
			$heading = $Role->getName();
			$displayName = $Role->getDisplayName();
			
			if ($heading != $displayName)
			{
				$heading .= ' (' . $displayName . ')';
			}
			
			$Table->addHeading($heading);
		}
		$html = '<span>'.$this->View->msg('Roles.add').'</span><br/>';
		$html .= DekiForm::singleInput('text', 'role_name', '', array('id' => 'role'));
		$Th = $Table->addHeading($html);
		$Th->addClass('add last');
		
		// add each operation checkbox
		foreach ($siteOperations as $operation)
		{
			$Table->addRow();
			$Td = $Table->addCol($operation);
			$Td->addClass('operations');
			foreach ($siteRoles as &$Role)
			{
				$html = DekiForm::singleInput(
					'checkbox',
					'role['.$Role->getId().']['. $operation .']',
					$operation,
					array('checked' => $Role->has($operation))
				);

				$Table->addCol($html);
			}
			unset($Role);

			// new role checkboxes
			$Td = $Table->addCol(DekiForm::singleInput('checkbox', 'role[0]['. $operation .']', $operation));
			$Td->addClass('last');
		}
		
		// add the save buttons
		$Tr = $Table->addRow();
		$Tr->addClass('save');
		$Table->addCol('');
		foreach ($siteRoles as &$Role)
		{
			$Table->addCol(DekiForm::singleInput('button', 'role_id', $Role->getId(), array(), $this->View->msg('Roles.edit.button')));
		}
		unset($Role);

		// add new role button
		$Table->addCol(DekiForm::singleInput('button', 'role_id', '0', array(), $this->View->msg('Roles.add.button'))); 

		$this->View->set('roles-form.action', $this->getUrl());
		$this->View->set('roles-table', $Table->saveHtml());
		$this->View->set('userManagementUrl', $this->Request->getLocalUrl('user_management'));
		$this->View->set('groupManagementUrl', $this->Request->getLocalUrl('group_management'));

		$this->View->output();
	}

}

new RoleManagement();
