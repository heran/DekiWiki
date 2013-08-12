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


class Authentication extends DekiControlPanel
{
	// needed to determine the template folder
	protected $name = 'authentication';
	
	protected $preconfigured = array(
		'custom' => array(
			'description' => 'Custom',
			'sid' => '',
			'helpUrl' => ProductURL::AUTH_HELP_CUSTOM,
			'configuration' => array(
			)
		),

		'drupal' => array(
			'description' => 'Drupal',
			'sid' => 'http://services.mindtouch.com/deki/draft/2007/05/drupal',
			'helpUrl' => ProductURL::AUTH_HELP_DRUPAL,
			'configuration' => array(
				'db-catalog' => '',
				'db-user' => '',
				'db-password' => ''
			)
		),

		'http' => array(
			'description' => 'HTTP',
			'sid' => 'http://services.mindtouch.com/deki/draft/2007/07/http-authentication',
			'helpUrl' => ProductURL::AUTH_HELP_HTTP,
			'configuration' => array(
				'authentication-uri' => ''
			)
		),

		'joomla' => array(
			'description' => 'Joomla',
			'sid' => 'http://services.mindtouch.com/deki/draft/2007/07/joomla-authentication',
			'helpUrl' => ProductURL::AUTH_HELP_JOOMLA,
			'configuration' => array(
				'db-catalog' => '',
				'db-user' => '',
				'db-password' => ''
			)
		),

		'ldap' => array(
			'description' => 'LDAP',
			'sid' => 'sid://mindtouch.com/ent/2009/03/ldap-authentication',
			'helpUrl' => ProductURL::AUTH_HELP_LDAP,
			'configuration' => array(
				'hostname' => '',
				'bindingdn' => '',
				'searchbase' => '',
				'userquery' => ''
			)
		),

		'osx' => array(
			'description' => 'Mac OSX Server',
			'sid' => 'sid://mindtouch.com/ent/2009/03/ldap-authentication',
			'helpUrl' => ProductURL::AUTH_HELP_LDAP,
			'configuration' => array(
				'searchbase' => 'DC=domainname,DC=local',
				'hostname' => 'servername.domainname.local',
				'userquery' => 'uid=$1',
				'bindingdn' => 'uid=$1,cn=users,dc=your,dc=server,dc=name'
			)
		),

		'ad' => array(
			'description' => 'Microsoft Active Directory',
			'sid' => 'sid://mindtouch.com/ent/2009/03/ldap-authentication',
			'helpUrl' => ProductURL::AUTH_HELP_LDAP,
			'configuration' => array(
				'searchbase' => 'DC=domainname,DC=local',
				'hostname' => 'servername.domainname.local',
				'userquery' => 'samAccountName=$1',
				'bindingdn' => '$1@domainname.local'
			)
		),

		'edir' => array(
			'description' => 'Novell eDirectory',
			'sid' => 'sid://mindtouch.com/ent/2009/03/ldap-authentication',
			'helpUrl' => ProductURL::AUTH_HELP_LDAP,
			'configuration' => array(
				'searchbase' => 'DC=domainname,DC=local',
				'hostname' => 'servername.domainname.local',
				'userquery' => 'CN=$1',
				'bindingdn' => 'CN=$1,DC=sales,DC=acme,DC=com',
				'groupquery' => '(&(cn=$1)(objectClass=group))'
			)
		),

		'open' => array(
			'description' => 'Open LDAP',
			'sid' => 'sid://mindtouch.com/ent/2009/03/ldap-authentication',
			'helpUrl' => ProductURL::AUTH_HELP_LDAP,
			'configuration' => array(
				'searchbase' => 'DC=domainname,DC=local',
				'hostname' => 'servername.domainname.local',
				'userquery' => 'CN=$1',
				'bindingdn' => 'CN=$1,DC=sales,DC=acme,DC=com',
				'groupquery' => '(&(cn=$1)(objectClass=group))'
			)
		),

		'wordpress' => array(
			'description' => 'WordPress',
			'sid' => 'http://services.mindtouch.com/deki/draft/2007/07/wordpress-authentication',
			'helpUrl' => ProductURL::AUTH_HELP_WORDPRESS,
			'configuration' => array(
				'db-catalog' => '',
				'db-user' => '',
				'db-password' => ''
			)
		)
	);

	public function index()
	{
		$this->executeAction('listing');
	}
	
	public function listing()
	{
		if ($this->Request->isPost())
		{
			$action = $this->Request->getVal('action');
			switch ($action)
			{
				default:
					$this->Request->redirect($this->getUrl('listing', array('page'), true));
					return;

				case 'restart':
				case 'stop':
				case 'start':
					$this->applyBulkAction($action);
					return;

				case 'delete':
					$this->executeAction($action);
					return;
			}
		}

		// build the listing table
		$Table = new DekiTable($this->name, 'listing', array('description', 'sid', 'uri'));
		$Table->setResultsPerPage(Config::RESULTS_PER_PAGE);
		// set the default sorting
		$Table->setDefaultSort('description');
		// space the columns
		$Table->setColWidths('18', '150', '', '80', '80', '80');

		// create the table header
		$Table->addRow();
		$Th = $Table->addHeading(DekiForm::singleInput('checkbox', 'all'));
		$Th->addClass('last checkbox');
		$Th = $Table->addSortHeading($this->View->msg('Authentication.data.name'), 'description');
		$Th->addClass('name');
		$Table->addHeading($this->View->msg('Authentication.data.sid-uri'));
		$Th = $Table->addHeading($this->View->msg('Authentication.data.status'));
		$Th->addClass('status');
		$Th = $Table->addHeading($this->View->msg('Authentication.data.default'));
		$Th->addClass('default');
		$Th = $Table->addHeading('&nbsp;');
		$Th->addClass('edit last');
		
		// grab the results
		$Plug = $this->Plug->At('site', 'services')->With('type', DekiService::TYPE_AUTH);
		$Result = $Table->getResults($Plug, '/body/services/@querycount');
		$Result->handleResponse();

		$defaultId = DekiAuthService::getDefaultProviderId();
		if (is_null($defaultId))
		{
			DekiAuthService::getInternal()->setAsDefaultProvider();
		}

		$services = $Result->getAll('/body/services/service', array());
		foreach ($services as $serviceArray)
		{
			$Service = DekiService::newFromArray($serviceArray);
			$Table->addRow();
			$Td = $Table->addCol(DekiForm::singleInput('checkbox', 'service_id[]', $Service->getId(), array('id' => 'service'.$Service->getId()), $Service->toHtml()));
			$Td->setAttribute('colspan', 2);

			$sidUri = $this->View->msg('Authentication.data.sid') .':&nbsp;'. htmlspecialchars($Service->getSid()) ."<br />";
			$sidUri .= $this->View->msg('Authentication.data.uri') .':&nbsp;<a href="'. $Service->getUri() .'">'. htmlspecialchars($Service->getUri()) .'</a>';
			$Table->addCol($sidUri);

			$Td = $Table->addCol(
				$Service->isRunning() ?
				$this->View->msg('Authentication.status.running') :
				$this->View->msg('Authentication.status.stopped')
			);
			$Td->addClass('status');

			if ($Service->isDefaultProvider())
			{
				$Td = $Table->addCol('<a><span>'. $this->View->msg('Authentication.default.yes') .'</span></a>');
				$Td->addClass('default-yes');
			}
			else
			{
				$url = $this->getUrl('set_default/'.$Service->getId(), array('page'), true);
				$title = htmlspecialchars($this->View->msgRaw('Authentication.default.help', $Service->getName()));
				$Td = $Table->addCol('<a href="'. $url .'" title="'. $title .'"><span>'. $this->View->msg('Authentication.default.no') .'</span></a>');
				$Td->addClass('default-no');
			}
			$Td->addClass('default');

			if ($Service->isInternal())
			{
				// Users cannot edit the internal auth provider
				$Td = $Table->addCol($this->View->msg('Authentication.edit.na'));
			}
			else
			{
				$Td = $Table->addCol('<a href="'. $this->getUrl('edit/' . $Service->getId(), array('page'), true) .'">'. $this->View->msg('Authentication.edit') .'</a>');
			}
			
			$Td->addClass('edit last');
		}
		
		$this->View->set('form.action', $this->getUrl('listing', array('page'), true));
		$this->View->set('services-table', $Table->saveHtml());
		$this->View->output();
	}

	public function add($preconfigured = null)
	{
		// used for generating the preconfigured list
		$key = is_null($preconfigured) || !isset($this->preconfigured[$preconfigured]) ? 'custom' : $preconfigured;
		$preconfigured = $key == 'custom' ? $key : $preconfigured;
		
		// setup the provider
		$description = $this->preconfigured[$key]['description'];
		$sid = $this->preconfigured[$key]['sid'];
		$helpUrl = $this->preconfigured[$key]['helpUrl'];
		$configuration = $this->preconfigured[$key]['configuration'];
		$preferences = isset($this->preconfigured[$key]['preferences']) ? $this->preconfigured[$key]['preferences'] : array();
		// default to native
		$initType = isset($this->preconfigured[$key]['init']) ? $this->preconfigured[$key]['init'] : 'native';
		
		// get posted configuration values
		$configTable = DekiForm::configTable($configuration);
		$prefsTable = DekiForm::configTable($preferences, 1, 'df_pref_keys', 'df_pref_values');

		if ($this->Request->isPost())
		{
			do
			{
				if ($this->Request->getVal('action') == 'save')
				{
					$initType = $this->Request->getVal('init_type');
					$sid = $this->Request->getVal('sid');
					$uri = $this->Request->getVal('uri');
					$description = $this->Request->getVal('description');
					$setDefault = $this->Request->getBool('default', false);

					$Service = new DekiAuthService($initType, $sid, $uri, $description);
					if ($setDefault)
					{
						$Service->setAsDefaultProvider();
					}

					// set the configuration values
					foreach ($configuration as $key => $value)
					{
						$Service->setConfig($key, $value);
					}

					// set the preferences
					foreach ($preferences as $key => $value)
					{
						$Service->setPref($key, $value);
					}

					// attempt to create the service
					$Result = $Service->create();
					if (!$Result->handleResponse())
					{
						DekiMessage::error($this->View->msgRaw('Authentication.error.added', $Service->toHtml(), $Result->getError()));
						break;
					}
					DekiMessage::success($this->View->msgRaw('Authentication.success.added', $Service->toHtml()));

					// attempt to start the service
					$Result = $Service->restart();
					if (!$Result->handleResponse())
					{
						DekiMessage::error($this->View->msgRaw('Authentication.error.restarted', $Service->toHtml(), $Result->getError()));
						// service was created but not started so allow the user to edit
						$this->Request->redirect($this->getUrl('edit/'.$Service->getId()));
						return;
					}

					$this->Request->redirect($this->getUrl('listing'));
					return;
				}
			} while (false);
		}
	
		// generate the list of preconfigured services
		$preconfigservices = array();
		foreach ($this->preconfigured as $key => $service)
		{
			$preconfigservices['add/'.$key] = $service['description'];
		}

		$this->View->set('form.action', $this->getUrl('add'));
		$this->View->set('form.cancel', $this->getUrl('listing', array('page'), true));

		// build the init options array
		$options = array(
			'native' => $this->View->msg('Authentication.type.native'),
			'remote' => $this->View->msg('Authentication.type.remote')
		);
		$this->View->set('form.initType-options', $options);

		$this->View->set('service.description', $description);
		$this->View->set('service.sid', $sid);
		$this->View->set('service.helpUrl', $helpUrl);
		$this->View->set('service.initType', $initType);

		$this->View->set('config-table', $configTable);
		$this->View->set('prefs-table', $prefsTable);

		$this->View->set('form.action.preconfigured', $this->Request->getLocalUrl('authentication'));
		$this->View->set('preconfigured-list', DekiForm::multipleInput('select', 'params', $preconfigservices, $preconfigured, array())); 

		$this->View->output();
	}

	public function edit($id)
	{
		$Service = DekiService::newFromId($id);
		if (is_null($Service) || $Service->isExtension())
		{
			DekiMessage::error($this->View->msg('Authentication.error.notfound'));
			$this->Request->redirect($this->getUrl('listing'));
			return;
		}

		$configuration = $Service->getConfiguration();
		$preferences = $Service->getPreferences();
		// get posted configuration values
		$configTable = DekiForm::configTable($configuration);
		$prefsTable = DekiForm::configTable($preferences, 1, 'df_pref_keys', 'df_pref_values');

		if ($this->Request->isPost())
		{
			do
			{
				$action = $this->Request->getVal('action');
				if ($action != 'save')
				{
					break;
				}

				// update the service information
				$initType = $this->Request->getVal('init_type');
				$sid = $this->Request->getVal('sid', '');
				$uri = $this->Request->getVal('uri', '');
				$description = $this->Request->getVal('description');

				$Service->setInitType($initType, $sid, $uri);
				$Service->setDescription($description);

				$Service->clearConfiguration();
				foreach ($configuration as $key => $value)
				{
					$Service->setConfig($key, $value);
				}

				$Service->clearPreferences();
				foreach ($preferences as $key => $value)
				{
					$Service->setPref($key, $value);
				}

				// attempt to update
				$Result = $Service->update();
				if (!$Result->handleResponse())
				{
					DekiMessage::error($this->View->msgRaw('Authentication.error.updated', $Service->toHtml(), $Result->getError()));
					break;
				}
				DekiMessage::success($this->View->msgRaw('Authentication.success.updated', $Service->toHtml()));

				// attempt to start the service
				$Result = $Service->restart();
				if (!$Result->handleResponse())
				{
					DekiMessage::error($this->View->msgRaw('Authentication.error.restarted', $Service->toHtml(), $Result->getError()));
					break;
				}
				DekiMessage::success($this->View->msg('Authentication.success.restarted', $Service->toHtml()));

				$this->Request->redirect($this->getUrl('edit/'. $Service->getId()));
				return;
			} while (false);
		}

		$this->View->set('form.action', $this->getUrl('edit/'. $id));
		$this->View->set('form.cancel', $this->getUrl('listing', array('page'), true));

		$this->View->set('config-table', $configTable);
		$this->View->set('prefs-table', $prefsTable);

		// build the init options array
		$options = array(
			'native' => $this->View->msg('Authentication.type.native'),
			'remote' => $this->View->msg('Authentication.type.remote')
		);
		$this->View->set('form.initType-options', $options);

		$this->View->set('service.sid', $Service->getSid());
		$this->View->set('service.uri', $Service->isNative() ? '' : $Service->getUri());
		$this->View->set('service.description', $Service->getDescription());
		$this->View->set('service.initType', $Service->getInitType());

		$this->View->set('debugexport.href', $this->getUrl('export_settings/'.$Service->getId()));
		
		$this->View->output();
	}

	/**
	 * Duplicated method in auth & extensions
	 * @param int $id - extension to export settings for
	 */
	public function export_settings($id = null)
	{
		$Service = DekiService::newFromId($id);
		if (is_null($Service) || $Service->isExtension())
		{
			DekiMessage::error($this->View->msg('Authentication.error.notfound'));
			$this->Request->redirect($this->getUrl());
			return;
		}
		// quick and dirty, direct plug call
		$Plug = DekiPlug::getInstance();
		
		$Result = $Plug->At('site', 'services', $id)->Get();
		
		// start outputting
		header('Content-type: application/xml');
		header('Content-Disposition: attachment; filename="auth.'. $id .'.settings.xml"');
		// check for errors
		if (!$Result->isSuccess())
		{
			echo '<error>'. $Result->getError() . '</error>';
		}
		else
		{
			echo $Result->getXml('body');
		}
	}
	
	public function set_default($id = null)
	{
		$Service = DekiService::newFromId($id);
		if (is_null($Service) || $Service->isExtension())
		{
			DekiMessage::error($this->View->msg('Authentication.error.notfound'));
		}
		else
		{
			// set this service as the default provider
			$Service->setAsDefaultProvider();
			$Result = $Service->update();
			if ($Result->isSuccess())
			{
				DekiMessage::success($this->View->msgRaw('Authentication.success.set-default', $Service->toHtml(), $Result->getError()));
			}
			else
			{
				DekiMessage::error($this->View->msgRaw('Authentication.error.set-default', $Service->toHtml(), $Result->getError()));
			}

			// need to start the service after updating
			$Result = $Service->restart();
			if (!$Result->isSuccess())
			{
				DekiMessage::error($this->View->msgRaw('Authentication.error.restarted', $Service->toHtml(), $Result->getError()));
			}
		}

		$this->Request->redirect($this->getUrl('listing'));
		return;
	}

	public function delete()
	{
		$serviceIds = $this->Request->getArray('service_id');
		$operate = $this->Request->getBool('operate');
		
		if (empty($serviceIds)) 
		{
			$this->Request->redirect($this->getUrl('listing', array('page'), true));
			return;	
		}

		/**
		 * Flag determines if a message should be displayed warning about deleting
		 * auth providers with remaining users and groups
		 */
		$hasUsersOrGroups = false;
		
		$html = '';
		$emptyServiceList = true; // flag sets whether any services passed in exist
		foreach ($serviceIds as $serviceId)
		{
			$Service = DekiService::newFromId($serviceId);
			if (!is_null($Service) && !$Service->isExtension())
			{
				$emptyServiceList = false; // found a valid service to delete
				if ($operate)
				{
					$Result = $Service->delete();
					if ($Result->handleResponse())
					{
						DekiMessage::success($this->View->msgRaw('Authentication.success.deleted', $Service->toHtml()));
					}
					else
					{
						DekiMessage::error($this->View->msgRaw('Authentication.error.deleted', $Service->toHtml(), $Result->getError()));
					}
				}
				else
				{
					// check if the specified service still has users and groups associated with it
					// there isn't anyway to retrieve querycounts from getSiteList
					// get the user count
					$Plug = DekiPlug::getInstance()->At('users')->With('authprovider', $Service->getId())->With('offset', 0)->With('limit', 1);
					$Result = $Plug->Get();
					$numUsers = $Result->getVal('body/users/@querycount', 0);
					// get the group count
					$Plug = DekiPlug::getInstance()->At('groups')->With('authprovider', $Service->getId())->With('offset', 0)->With('limit', 1);
					$Result = $Plug->Get();					
					$numGroups = $Result->getVal('body/groups/@querycount', 0);
					
					$html .= '<li>';
					$html .= $Service->toHtml();
					
					if ($numUsers > 0 || $numGroups > 0)
					{
						$hasUsersOrGroups = true;
						// set a message for this provider
						$html .= $this->View->msgRaw(
							'Authentication.delete.verify.details',
							$this->Request->getLocalUrl('user_management', 'listing', array('sortby' => 'service')),
							$numUsers,
							$this->Request->getLocalUrl('group_management', 'listing', array('sortby' => 'service')),
							$numGroups
						);
					}
					// add the hidden input
					$html .= '<input type="hidden" name="service_id[]" value="'. $Service->getId() .'" />';
					$html .= '</li>';
				}
			}
		}
		// services have been deleted, redirect
		if ($operate)
		{
			$this->Request->redirect($this->getUrl('listing', array('page'), true));
			return;
		}
		
		// make sure we have some services to delete
		if ($emptyServiceList)
		{
			$this->Request->redirect($this->getUrl('listing', array('page'), true));
			return;
		}

		if ($hasUsersOrGroups)
		{
			DekiMessage::info($this->View->msg('Authentication.delete.verify.associated'));	
		}
		
		
		$this->View->set('form.action', $this->getUrl('delete', array('page'), true));
		$this->View->set('form.cancel', $this->getUrl('listing', array('page'), true));

		$this->View->set('service-list', $html);
		$this->View->output();
	}


	/*
	 * Private helper methods
	 */
	private function applyBulkAction($action)
	{
		$serviceIds = $this->Request->getArray('service_id');
		foreach ($serviceIds as $serviceId)
		{
			$Service = DekiService::newFromId($serviceId);
			if (!is_null($Service) && !$Service->isExtension())
			{
				switch ($action)
				{
					default:
					case 'stop':
						$Result = $Service->stop();
						if ($Result->handleResponse())
						{
							DekiMessage::success($this->View->msgRaw('Authentication.success.stopped', $Service->toHtml()));
						}
						else
						{
							DekiMessage::error($this->View->msgRaw('Authentication.error.stopped', $Service->toHtml(), $Result->getError()));
						}
						break;
					case 'start':
					case 'restart':
						$Result = $Service->restart();
						if ($Result->handleResponse())
						{
							DekiMessage::success($this->View->msgRaw('Authentication.success.restarted', $Service->toHtml()));
						}
						else
						{
							DekiMessage::error($this->View->msgRaw('Authentication.error.restarted', $Service->toHtml(), $Result->getError()));
						}
						break;
				}
			}
		}

		$this->Request->redirect($this->getUrl('listing', array('page'), true));
		return;
	}
}

new Authentication();
