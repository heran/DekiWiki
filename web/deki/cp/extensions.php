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


class Extensions extends DekiControlPanel
{
	// needed to determine the template folder
	protected $name = 'extensions';


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
		$Table = new DekiTable($this->name, 'listing', array('description', 'type', 'init', 'sid', 'uri'));
		$Table->setResultsPerPage(Config::SERVICES_PER_PAGE);
		// set the default sorting
		$Table->setDefaultSort('description');
		// space the columns
		$Table->setColWidths('16', '', '400', '100', '80', '80');

		// create the table header
		$Table->addRow();
		$Th = $Table->addHeading(DekiForm::singleInput('checkbox', 'all'));
		$Th->addClass('last checkbox');
		$Th = $Table->addSortHeading($this->View->msg('Extensions.data.name'), 'description');
		$Th->addClass('name');
		$Table->addHeading($this->View->msg('Extensions.data.description'));
		$Table->addHeading($this->View->msg('Extensions.data.namespace'));
		$Th = $Table->addHeading($this->View->msg('Extensions.data.status'));
		$Th->addClass('status');
		$Th = $Table->addHeading('&nbsp;');
		$Th->addClass('last edit');
		
		// grab the results
		$Plug = $this->Plug->At('site', 'services')->With('type', DekiService::TYPE_EXTENSION);
		$Result = $Table->getResults($Plug, '/body/services/@querycount');
		$Result->handleResponse();

		$services = $Result->getAll('/body/services/service', array());
		$i = 0;
		if (empty($services))
		{
			$Table->setAttribute('class', 'table none');
			$Table->addRow();
			$Td = $Table->addCol('<div class="none">'.$this->View->msg('Extensions.data.empty').'</div>');
			$Td->setAttribute('colspan', 6);
			$Td->setAttribute('class', 'last');
		}
		else
		{
			foreach ($services as &$serviceArray)
			{
				$Service = DekiService::newFromArray($serviceArray);
				$Tr = $Table->addRow();
				if (!$Service->isRunning())
				{
					$Tr->addClass('stopped');
				}
				
				// if the service has an error, we actually have to affect the displayed row, then spit out the error row
				$hasError = !$Service->isRunning() && $Service->hasError();
				
				// service name
				$Td = $Table->addCol(DekiForm::singleInput('checkbox', 'service_id[]', $Service->getId(), array('id' => 'service'.$Service->getId()), $Service->toHtml()));
				$Td->setAttribute('colspan', 2);
				if ($hasError) 
				{
					$Td->addClass('bottom');
				}
				
				// service description
				$Td = $Table->addCol(htmlspecialchars($Service->getDescription()));
				if ($Service->isProtected())
				{
					$Td->addClass('protected');
				}
				if ($hasError) 
				{
					$Td->addClass('bottom');
				}
				
				// service namespace
				$Td = $Table->addCol('<code>'.htmlspecialchars($Service->getNamespace()).'</code>');
				if ($hasError) 
				{
					$Td->addClass('bottom');
				}
				
				// service status
				$Td = $Table->addCol(
					$Service->isRunning() 
						? '<a href="'. $Service->getUri() .'" title="'. $Service->getUri() .'">'. $this->View->msg('Extensions.status.running') .'</a>'
						: $this->View->msg('Extensions.status.stopped')
				);
				
				$Td->addClass('status');
				if ($hasError) 
				{
					$Td->addClass('error');	
				}
				
				// service edit
				$Td = $Table->addCol(
					'<a href="'. $this->getUrl('edit/' . $Service->getId(), array('page'), true) .'">'.
						 $this->View->msg('Extensions.data.edit') .
					'</a>'
				);
				$Td->addClass('last edit');			
				if ($hasError) 
				{
					$Td->addClass('bottom');
				}
				
				// error debug row
				if ($hasError)
				{
					$Tr = $Table->addRow();
					$Tr->addClass('error');
					$Td = $Table->addCol('<pre>'.htmlspecialchars($Service->getError()).'</pre>');
					$Td->setAttribute('colspan', 6);
				}
			}
			unset($serviceArray);
		}

		$this->View->set('services-table', $Table->saveHtml());

		$this->View->set('addScriptUrl', $this->getUrl('add_script'));
		$this->View->set('addExtensionUrl', $this->getUrl('add'));

		$this->View->set('form.action', $this->getUrl('listing', array('page'), true));

		$this->View->output();
	}
	
	public function add_script()
	{
		$this->executeAction('add', array(true));
	}

	public function add($isScript = false)
	{
		$isScript = (bool)$isScript;
		$configuration = array();
		// get posted configuration values
		$configTable = DekiForm::configTable($configuration, $isScript ? 0 : 3);

		if ($this->Request->isPost())
		{
			do
			{
				// determine what the user is trying to do
				$action = $this->Request->getVal('action');

				if ($action != 'save')
				{
					// only handle saving the service information
					break;
				}
				
				if ($isScript)
				{
					// adding a script
					$initType = DekiService::INIT_NATIVE;
					$sid = DekiExtension::$DEKI_SCRIPT_SIDS[0];
					$uri = '';
				}
				else
				{
					// adding an extension
					$initType = $this->Request->getEnum(
						'init_type',
						array(DekiService::INIT_NATIVE, DekiService::INIT_REMOTE),
						DekiService::INIT_NATIVE
					);
					$sid = $this->Request->getVal('sid');
					$uri = $this->Request->getVal('uri');
				}
				$description = $this->Request->getVal('name');

				$Service = new DekiExtension($initType, $sid, $uri, $description);

				// update the configuration keys
				$Service->clearConfiguration();
				foreach ($configuration as $key => $value)
				{
					$Service->setConfig($key, $value);
				}
				// add script specific configs
				if ($isScript)
				{
					$scriptManifest = $this->Request->getVal('manifest');
					$Service->setConfig('manifest', $scriptManifest);
					$Service->setConfig('debug', $this->Request->getVal('debug', 'false'));
				}
				
				// update the preferences
				// need to check if prefs are empty so we don't send empty nodes to api
				$title = $this->Request->getVal('pref_title');
				$Service->setPref('title', $title == '' ? null : $title);
                                $label = $this->Request->getVal('pref_label');
                                $Service->setPref('label', $label == '' ? null : $label);
				$description = $this->Request->getVal('pref_description');
				$Service->setPref('description', $description == '' ? null : $description);
				$namespace = $this->Request->getVal('pref_namespace');
				$Service->setPref('namespace', $namespace == '' ? null : $namespace);
				$logoUri = $this->Request->getVal('pref_logo_uri');
				$Service->setPref('uri.logo', $logoUri == '' ? null : $logoUri);
				$functions = $this->Request->getVal('pref_functions');
				$Service->setPref('functions', $functions == '' ? null : $functions);
				$protected = $this->Request->getBool('pref_protected', false);
				$Service->setProtected($protected);

				// create the service
				$Result = $Service->create();

				if (!$Result->handleResponse())
				{
					$key = $isScript ? 'Extensions.error.script.created' : 'Extensions.error.extension.created';
					DekiMessage::error($this->View->msg($key, $Result->getError()));
					break;
				}
				DekiMessage::success($this->View->msgRaw('Extensions.success.created', $Service->toHtml()));

				// restart the service
				$Result = $Service->restart();
				if (!$Result->handleResponse())
				{
					DekiMessage::error($this->View->msgRaw('Extensions.error.restarted', $Service->toHtml()));
					// service was created but not started so allow the user to edit
					$this->Request->redirect($this->getUrl('edit/'.$Service->getId()));
					return;
				}
				DekiMessage::success($this->View->msgRaw('Extensions.success.restarted', $Service->toHtml()));

				$this->Request->redirect($this->getUrl('listing'));
				return;
			} while (false);
		}
		
		// begin setting the view variables
		$this->View->set('form.action', $isScript ? $this->getUrl('add_script') : $this->getUrl('add'));
		$this->View->set('form.cancel', $this->getUrl('listing', array('page'), true));

		$this->View->set('extension.name', $this->Request->getVal('name'));
		$this->View->set('extension.sid', $this->Request->getVal('sid'));
		$this->View->set('extension.uri', $this->Request->getVal('uri'));
		$this->View->set('extension.init', $this->Request->getVal('init_type', DekiService::INIT_NATIVE));
		$this->View->set('extension.isScript', $isScript);

		if ($isScript)
		{
			$this->View->set('extension.manifest', $this->Request->getVal('manifest'));
			$this->View->set('extension.debug', $this->Request->getVal('debug', null));
		}
		else
		{
			// build the init options array
			$options = array(
				'native' => $this->View->msg('Extensions.type.native'),
				'remote' => $this->View->msg('Extensions.type.remote')
			);
			$this->View->set('form.init-options', $options);
		}

		$this->View->set('config-table', $configTable);

		$this->View->set('pref.title', $this->Request->getVal('pref_title'));
		$this->View->set('pref.description', $this->Request->getVal('pref_description'));
		$this->View->set('pref.namespace', $this->Request->getVal('pref_namespace'));
		$this->View->set('pref.logoUri', $this->Request->getVal('pref_logo_uri'));
		$this->View->set('pref.functions', $this->Request->getVal('pref_functions'));


		$this->View->output();
	}

	public function edit($id = null)
	{
		$Service = DekiService::newFromId($id);
		if (is_null($Service) || ($Service->getType() != DekiService::TYPE_EXTENSION))
		{
			DekiMessage::error($this->View->msg('Extensions.error.not-found'));
			$this->Request->redirect($this->getUrl());
			return;
		}
		$isScript = $Service->isDekiScript();


		$configuration = $Service->getConfiguration();
		$scriptManifest = '';
		$scriptDebug = '';
		// remove the manifest for scripts since it is a custom field
		if ($isScript)
		{
			if (isset($configuration['manifest']))
			{
				// save the manifest for the form
				$scriptManifest = $configuration['manifest'];
				unset($configuration['manifest']);
			}

			if (isset($configuration['debug']))
			{
				// save the manifest for the form
				$scriptDebug = $configuration['debug'];
				unset($configuration['debug']);
			}
			else
			{
				// default to false debug state
				$scriptDebug = 'false';
			}
		}
		$configTable = DekiForm::configTable($configuration, $isScript ? 0 : 3);


		if ($this->Request->isPost())
		{
			do
			{
				// determine what the user is trying to do
				$action = $this->Request->getVal('action');

				if ($action != 'save')
				{
					// only handle saving the service information
					break;
				}

				// update the service name
				$Service->setDescription($this->Request->getVal('name'));

				// update the service details
				$Service->setInitType(
					$isScript ? DekiService::INIT_NATIVE : $this->Request->getVal('init_type'),
					$this->Request->getVal('sid', DekiExtension::$DEKI_SCRIPT_SIDS[0]),
					$this->Request->getVal('uri')
				);

				// update the configuration keys
				$Service->clearConfiguration();
				foreach ($configuration as $key => $value)
				{
					$Service->setConfig($key, $value);
				}

				// add script specific configs
				if ($isScript)
				{
					$scriptManifest = $this->Request->getVal('manifest', $scriptManifest);
					$Service->setConfig('manifest', $scriptManifest);
					$Service->setConfig('debug', $this->Request->getVal('debug', 'false'));
				}
				
				// update the preferences
				// need to check if prefs are empty so we don't send empty nodes to api
				$title = $this->Request->getVal('pref_title');
				$Service->setPref('title', $title == '' ? null : $title);
                                $label = $this->Request->getVal('pref_label');
				$Service->setPref('label', $label == '' ? null : $label);
				$description = $this->Request->getVal('pref_description');
				$Service->setPref('description', $description == '' ? null : $description);
				$namespace = $this->Request->getVal('pref_namespace');
				$Service->setPref('namespace', $namespace == '' ? null : $namespace);
				$logoUri = $this->Request->getVal('pref_logo_uri');
				$Service->setPref('uri.logo', $logoUri == '' ? null : $logoUri);
				$functions = $this->Request->getVal('pref_functions');
				$Service->setPref('functions', $functions == '' ? null : $functions);
				$protected = $this->Request->getBool('pref_protected', false);
				$Service->setProtected($protected);

				// update the service
				$Result = $Service->update();
				if (!$Result->handleResponse())
				{
					DekiMessage::error($this->View->msgRaw('Extensions.error.edited', $Service->toHtml()));
					break;
				}
				DekiMessage::success($this->View->msgRaw('Extensions.success.edited', $Service->toHtml()));

				// restart the service
				$Result = $Service->restart();
				if (!$Result->handleResponse())
				{
					DekiMessage::error($this->View->msgRaw('Extensions.error.restarted', $Service->toHtml()));
					break;
				}
				DekiMessage::success($this->View->msgRaw('Extensions.success.restarted', $Service->toHtml()));

				$this->Request->redirect($this->getUrl('/'));
				return;
			} while (false);
		}
		
		$this->View->set('form.action', $this->getUrl('edit/'. $Service->getId()));
		$this->View->set('form.cancel', $this->getUrl('listing', array('page'), true));

		$this->View->set('extension.name', $this->Request->getVal('name', $Service->getName()));
		$this->View->set('extension.sid', $this->Request->getVal('sid', $Service->getSid()));
		$this->View->set('extension.uri', $Service->isNative() ? '' : $Service->getUri());
		$this->View->set('extension.init', $this->Request->getVal('init_type', $Service->getInitType()));
		$this->View->set('extension.isScript', $isScript);

		if ($isScript)
		{
			$this->View->set('extension.manifest', $this->Request->getVal('manifest', $Service->getConfig('manifest')));
			$this->View->set('extension.debug', $this->Request->getVal('debug', $Service->getConfig('debug')));
		}
		else
		{
			// build the init options array
			$options = array(
				'native' => $this->View->msg('Extensions.type.native'),
				'remote' => $this->View->msg('Extensions.type.remote')
			);
			$this->View->set('form.init-options', $options);
		}

		$this->View->set('config-table', $configTable);

		$this->View->set('pref.default.title', $Service->getDefaultTitle());
		$this->View->set('pref.title', $this->Request->getVal('pref_title', $Service->getUserTitle()));
		
 		$this->View->set('pref.default.label', $Service->getDefaultLabel());
		$this->View->set('pref.label', $this->Request->getVal('pref_label', $Service->getUserLabel()));               
                
		$this->View->set('pref.default.description', $Service->getDefaultDescription());
		$this->View->set('pref.description', $this->Request->getVal('pref_description', $Service->getUserDescription()));
		
		$this->View->set('pref.default.namespace', $Service->getDefaultNamespace());
		$this->View->set('pref.namespace', $this->Request->getVal('pref_namespace', $Service->getUserNamespace()));
		
		$this->View->set('pref.default.logoUri', $Service->getDefaultLogoUri());
		$this->View->set('pref.logoUri', $this->Request->getVal('pref_logo_uri', $Service->getUserLogoUri()));
		
		$this->View->set('pref.default.functions', $Service->getDefaultFunctions());
		$this->View->set('pref.functions', $this->Request->getVal('pref_functions', $Service->getUserFunctions()));

		$this->View->set('pref.protected', $this->Request->getBool('pref_protected', $Service->isProtected()));

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
		if (is_null($Service) || !$Service->isExtension())
		{
			DekiMessage::error($this->View->msg('Extensions.error.not-found'));
			$this->Request->redirect($this->getUrl());
			return;
		}
		// quick and dirty, direct plug call
		$Plug = DekiPlug::getInstance();
		
		$Result = $Plug->At('site', 'services', $id)->Get();
		
		// start outputting
		header('Content-type: application/xml');
		header('Content-Disposition: attachment; filename="extension.'. $id .'.settings.xml"');
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

	public function delete()
	{
		$serviceIds = $this->Request->getArray('service_id');
		$operate = $this->Request->getBool('operate');
		
		if (empty($serviceIds)) 
		{
			$this->Request->redirect($this->getUrl('/', array('page'), true));
			return;	
		}
		$html = '';
		foreach ($serviceIds as $serviceId)
		{
			$Service = DekiService::newFromId($serviceId);
			if (!is_null($Service) && $Service->isExtension())
			{
				if ($operate)
				{
					$Result = $Service->delete();
					if ($Result->handleResponse())
					{
						DekiMessage::success($this->View->msgRaw('Extensions.success.deleted', $Service->toHtml()));
					}
					else
					{
						DekiMessage::error($this->View->msgRaw('Extensions.error.deleted', $Service->toHtml()));
					}
				}
				else
				{
					$html .= '<li>'. $Service->toHtml() .'<input type="hidden" name="service_id[]" value="'. $Service->getId() .'" />'.'</li>';
				}
			}
		}

		if ($operate)
		{
			$this->Request->redirect($this->getUrl('listing', array('page'), true));
			return;
		}
		
		
		$this->View->set('form.action', $this->getUrl('delete/'. $Service->getId(), array('page'), true));
		$this->View->set('form.cancel', $this->getUrl('listing', array('page'), true));

		$this->View->set('extension-list', $html);
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
			if (!is_null($Service) && $Service->isExtension())
			{
				switch ($action)
				{
					default:
					case 'stop':
						$Result = $Service->stop();
						if ($Result->handleResponse())
						{
							DekiMessage::success($this->View->msgRaw('Extensions.success.stopped', $Service->toHtml()));
						}
						else
						{
							DekiMessage::error($this->View->msgRaw('Extensions.error.stopped', $Service->toHtml()));
						}
						break;
					case 'start':
					case 'restart':
						$Result = $Service->restart();
						if ($Result->handleResponse())
						{
							DekiMessage::success($this->View->msgRaw('Extensions.success.restarted', $Service->toHtml()));
						}
						else
						{
							DekiMessage::error($this->View->msgRaw('Extensions.error.restarted', $Service->toHtml()));
						}
						break;
				}
			}
		}

		$this->Request->redirect($this->getUrl('listing', array('page'), true));
		return;
	}
}

new Extensions();