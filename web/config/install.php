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

define('DEKI_MVC', true);
require_once('index.php');

class InstallController extends DekiInstaller
{
	// needed to determine the template folder
	protected $name = 'install';
	
	public function index()
	{
		$this->executeAction('start');
	}
	
	public function start()
	{
		// clear out any existing completion state
		unset($_SESSION[self::COMPLETE_KEY]);
		
		if ($this->Request->isPost())
		{
			$errors = $this->validateConfiguration();
			$this->View->set('input.messages', $errors);
			
			if (count($errors) == 0)
			{
				// Install!
				$messages = array();
				$errors = $this->installMindTouch($messages);
				$this->View->set('installation.messages', $errors);
				
				if (count($errors) == 0)
				{
					global $conf;
					
					// installation completed
					$_SESSION[self::COMPLETE_KEY] = array(
						'messages' => $messages,
						'conf' => serialize($conf),
						'completed' => time()
					);

					$this->Request->redirect($this->Request->getLocalUrl('complete'));
					return;
				}
			}
		}
		
		$this->View->set('form.action', $this->getUrl('start'));
		$this->View->set('form.posted', $this->Request->isPost());
		
		$this->View->set('select_type', $this->renderAction('select_type'));
		$this->View->set('site_setup', $this->renderAction('site_setup'));
		$this->View->set('configuration', $this->renderAction('configuration'));
		$this->View->set('organization', $this->renderAction('organization'));
		$this->View->set('confirm_setup', $this->renderAction('confirm_setup'));
		
		// set the order to render steps
		$steps = array();
		
		$steps[] = array(
			'index' => count($steps)+1,
			'key' => 'select_type'
		);
		
		$steps[] = array(
			'index' => count($steps)+1,
			'key' => 'site_setup'
		);
		
		// show the configuration step?
		if ((!$this->isVM() && !$this->isWAMP()) || DekiMvcConfig::DEBUG)
		{
			$steps[] = array(
				'index' => count($steps)+1,
				'key' => 'configuration'
			);
		}
		
		$steps[] = array(
			'index' => count($steps)+1,
			'key' => 'organization'
		);
		
		$steps[] = array(
			'index' => count($steps)+1,
			'key' => 'confirm_setup'
		);
		
		$this->View->set('steps', $steps);
		
		$this->View->output();
	}
	
	public function select_type() { $this->View->output(); }
	public function site_setup() { $this->View->output(); }
	public function configuration() { $this->View->output(); }
	public function organization() { $this->View->output(); }
	public function confirm_setup() { $this->View->output(); }
}

// Initialize the install controller
new InstallController(array(
	'view.root' => DekiMvcConfig::$APP_ROOT . '/' . DekiMvcConfig::VIEWS_FOLDER,
	'template.name' => DekiMvcConfig::TEMPLATE_NAME
));
