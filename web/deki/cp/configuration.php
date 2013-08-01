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


class SiteConfigurationController extends DekiControlPanel
{
	// needed to determine the template folder
	protected $name = 'configuration';

	public function index()
	{
		$this->executeAction('settings');
	}
	
	// main listing view
	public function settings()
	{
		if ($this->Request->isPost() && $this->POST_settings())
		{
			$this->Request->redirect($this->getUrl('settings'));
			return;
		}
		
		$this->View->set('form.action', $this->getUrl('settings'));
		$this->View->set('form.input.bannedwords', DekiForm::singleInput('textarea', 'bannedWords', wfGetConfig('ui/banned-words')));
		$this->View->set('form.input.anonymous', DekiForm::singleInput('checkbox', 'anonymous', '1', array('checked' => wfGetConfig('security/allow-anon-account-creation') == '1'), $this->View->msg('Settings.basic.form.anonymous')));
		$this->View->set('form.input.sitename', DekiForm::singleInput('text', 'siteName', wfGetConfig('ui/sitename')));
		
		$this->View->set('form.input.private', DekiSite::isPrivate());
		
		global $wgEnableSearchHighlight;
		$this->View->set('form.input.searchhighlight', DekiForm::singleInput('checkbox', 'searchhighlight', 'true', array('checked' => $wgEnableSearchHighlight == 'true'), $this->View->msg('Settings.basic.form.searchhighlight')));
		
		$timezoneOptions = DekiSite::getTimezoneOptions();
		$this->View->setRef('options.timezone', $timezoneOptions);
		$this->View->set('form.timezone', DekiSite::getTimezoneOffset());

		$this->View->set('form.select.language', DekiForm::multipleInput('select', 'wgLanguageCode', wfAvailableResourcesLanguages(), wfGetConfig('ui/language')));
		$this->View->set('form.select.polyglot', DekiForm::singleInput('text', 'wgLanguagesAllowed', wfGetConfig('languages')));
		
		$helpUrl = wfGetConfig('ui/help-url');
		if (strncmp($helpUrl, 'http://', 7) == 0) 
		{
			$helpUrl = substr($helpUrl, 7);	
		}
		$this->View->set('form.input.help', DekiForm::singleInput('text', 'helpUrl', $helpUrl));

		$this->View->set('form.input.atdspellchecker', DekiForm::singleInput('checkbox', 'atdspellchecker', '1', array('checked' => wfGetConfig('ui/editor/atd-enabled', ATD_DEFAULT_STATUS) === true), $this->View->msg('Settings.basic.form.atdspellchecker')));
		
		// hide options for cloud
		$this->View->set('form.hideOptions', $this->isRunningCloud);
		
		$this->View->output();
	}
	
	protected function POST_settings()
	{
		// save anonymous user setting
		$anonymous = $this->Request->getVal('anonymous');
		if (strcmp($anonymous, wfGetConfig('security/allow-anon-account-creation')) != 0) 
		{	
			wfSetConfig('security/allow-anon-account-creation', is_null($anonymous) ? 'false': 'true');
			DekiMessage::success($this->View->msg($anonymous ? 'Settings.success.anonymous.enabled': 'Settings.success.anonymous.disabled'));
		}
		
		// save site privacy
		$private = !is_null($this->Request->getVal('private'));
		$current = DekiSite::isPrivate();
		if (!$private && $current || $private && !$current)
		{
			if (DekiSite::changePrivacy($private))
			{
				DekiMessage::success($this->View->msg($private ? 'Settings.success.private.enabled' : 'Settings.success.private.disabled'));
			}
		}
		
		if (!$this->isRunningCloud)
		{
			// these configuration options are not available in cloud
		
			// set the help url
			$helpUrl = $this->Request->getVal('helpUrl'); 
			if (!empty($helpUrl))
			{
				if (strncmp($helpUrl, 'http://', 7) !== 0) 
				{
					$helpUrl = 'http://'.$helpUrl;
				}
				wfSetConfig('ui/help-url', $helpUrl);
			}
			$oldHelpUrl = wfGetConfig('ui/help-url');
			if (!empty($oldHelpUrl) && empty($helpUrl))
			{
				wfSetConfig('ui/help-url', null);
			}
			
			$searchHighlight = $this->Request->getVal('searchhighlight');
			wfSetConfig('ui/search-highlight', $searchHighlight ? 'true': 'false');
			
			$atdEnabled = $this->Request->getVal('atdspellchecker');
			wfSetConfig('ui/editor/atd-enabled', $atdEnabled ? 'true' : 'false');

			// save banned words
			$bannedWords = str_replace(array("\n", "\r"), array(',', ''), $this->Request->getVal('bannedWords'));
			if (strcmp($bannedWords, wfGetConfig('ui/banned-words')) != 0) 
			{
				wfSetConfig('ui/banned-words', $bannedWords);
				DekiMessage::success($this->View->msg('Settings.success.bannedwords'));
			}
	
			// save site language
			$language = $this->Request->getVal('wgLanguageCode');
			// bugfix #6801, grab the language code from config since user can override
			$siteLanguage = wfGetConfig('ui/language', null);
			if ($siteLanguage != $language) 
			{
				global $wgResourceLanguageNames;
				if (!array_key_exists($language, $wgResourceLanguageNames)) 
				{
					DekiMessage::error($this->View->msg('Settings.basic.error.language'));
					return false;
				}
				else 
				{
					wfSetConfig('wgLanguageCode', $language);
					DekiMessage::success($this->View->msg('Settings.success.language'));
				}
			}
			
			// set polyglot languages
			global $wgLanguagesAllowed;
			$language = str_replace(array("\n", "\r"), array(',', ''), $this->Request->getVal('wgLanguagesAllowed'));
			if ($wgLanguagesAllowed != $language) 
			{
				global $wgLanguageList;
	
				if (!isset($wgLanguageList))
				{
					@include(Config::$DEKI_ROOT . '/languages/LanguageList.php');
				}
				
				if (!empty($language)) 
				{
					// let's verify the codes are correct and strip them to prevent a cascading of errors
					$unset = array();
					$languages = explode(',', $language);
					foreach ($languages as $key => $val) 
					{
						if (empty($val)) {
							continue;
						}
						$val = trim(strtolower($val));
						if (!array_key_exists($val, $wgLanguageList))
						{
							unset($languages[$key]);
							$unset[] = $val;
						}
						else 
						{
							$languages[$key] = $val;
						}
					}
					$language = implode(',', $languages);
					if (count($unset) > 0) 
					{
						DekiMessage::error($this->View->msg('Settings.basic.error.polyglot.key', implode(',', $unset)));
						return false;
					}
				}
				wfSetConfig('wgLanguagesAllowed', $language);
				DekiMessage::success($this->View->msg(empty($language) ? 'Settings.success.nopolyglot' : 'Settings.success.polyglot'));
			}
		}
		
		// save site name
		global $wgSitenameLength, $wgSitename;
		$siteName = $this->Request->getVal('siteName');
		if (strcmp($wgSitename, $siteName) != 0) 
		{
			if (strlen($siteName) < 1 || strlen($siteName) > $wgSitenameLength) 
			{
				DekiMessage::error($this->View->msg('Settings.error.sitename.length', $wgSitenameLength));
				return false;
			}
			else
			{
				DekiMessage::success($this->View->msg('Settings.success.sitename'));
				wfSetConfig('wgSitename', $siteName);
			}
		}
		
		// save site timezone
		$timezone = $this->Request->getVal('timezone');
		if (strcmp($timezone, wfGetConfig('ui/timezone')) != 0) 
		{
			wfSetConfig('ui/timezone', $timezone);
			DekiMessage::success($this->View->msg('Settings.success.timezone'));
		}
		
		wfSaveConfig();
		
		return true;
	}
	
	/**
	 * Advanced configuration editing
	 * 
	 * @return
	 */
	public function listing()
	{
		if ($this->Request->isPost() && $this->POST_listing())
		{
			$this->Request->redirect($this->getUrl('listing'));
			return;
		}
		
		$readonlykeys = array();
		$editablekeys = array();
		
		//with config keys, there are readonly values, so grab them appropriately
		$this->split_keys($this->get_config(true), $readonlykeys, $editablekeys);
		
		//editable keys
		$EditTable = new DomTable();
		$EditTable->setColWidths('400', '200', '100');
		$EditTable->addRow();
		$EditTable->addHeading(DekiForm::singleInput('checkbox', 'all', '', array(), $this->View->msg('Settings.form.key')) /* todo: jquery all checkboxes */);
		$Th = $EditTable->addHeading($this->View->msg('Settings.form.value'));
		$Th = $EditTable->addHeading('&nbsp;');
		$Th->setAttribute('class', 'last edit');
		$i = 0;
		foreach ($editablekeys as $key => $val) 
		{
			$i++;
			$EditTable->addRow();
			$Td = $EditTable->addCol(DekiForm::singleInput('checkbox', 'key[]', $key, array('id' => 'config'.$i), $key));
			$Td = $EditTable->addCol(htmlspecialchars($val));
			$Td = $EditTable->addCol(
				DekiForm::singleInput('button', 'key', $key, array('class' => 'command-editkey'), $this->View->msg('Settings.edit'))
			);
			$Td->setAttribute('class', 'last edit');
		}
		
		//read only keys
		$ReadTable = new DomTable();
		$ReadTable->setColWidths('400', '300');
		$ReadTable->addRow();
		$ReadTable->addHeading($this->View->msg('Settings.form.key'));
		$Tr = $ReadTable->addHeading($this->View->msg('Settings.form.value'));
		$Tr->addClass('last');
		
		foreach ($readonlykeys as $key => $val) 
		{
			$ReadTable->addRow();
			$ReadTable->addCol(htmlspecialchars($key));
			$Td = $ReadTable->addCol(htmlspecialchars($val));
			$Td->addClass('last');
		}
		
		DekiMessage::info($this->View->msg('Settings.warning'));
		$this->View->set('edit.form', $this->renderAction('form'));
		$this->View->set('table.editkeys', $EditTable->saveHtml());
		$this->View->set('table.readkeys', $ReadTable->saveHtml());
		$this->View->set('form.action', $this->getUrl('listing'));

		$this->View->output();
	}

	protected function POST_listing()
	{
		switch ($this->Request->getVal('action'))
		{
			case 'verify':
				$this->executeAction('verify');
				return true;

			case 'add':
				$this->executeAction('edit');
				return true;

			default:
				// assuming that the user is editing a key
				$this->executeAction('edit');
				return true;
		}
		
		return false;
	}
	
	/**
	 * Handle key creation and editing
	 * @note posted data required
	 */
	public function edit() 
	{		
		if ($this->Request->getVal('action') == 'update_key')
		{
			$newKey = $this->Request->getVal('key');
			$newValue = $this->Request->getVal('value');
			$editKey = $this->Request->getVal('edit_key');
			
			do
			{
				// validate the input
				if (empty($newKey))
				{
					DekiMessage::error($this->View->msg('Settings.error.keyname'));
					break;
				}
				
				// remove the previous key, bugfix #6462
				if (!empty($editKey))
				{
					wfSetConfig($editKey, null);
				}
				
				// set the new key
				wfSetConfig($newKey, $newValue);
				$return = wfSaveConfig();
				
				// check for success
				if (wfArrayVal($return, 'status', 0) != 200) 
				{
					DekiMessage::error(wfArrayVal($return, 'body/error/message'));
					break;
				}

				DekiMessage::success($this->View->msg('Settings.success.updated'));
				$this->Request->redirect($this->getUrl('listing'));
				return;
			} while (false);
		}

		$key = $this->Request->getVal('key');
		$value = !empty($key) ? wfGetConfig($key) : null;
		$this->View->set('form', $this->renderAction('form', array($key, $value)));

		$this->View->output();
	}
	
	/**
	 * Verify config key deletions
	 * @note posted data required
	 */
	public function verify() 
	{
		$keys = $this->Request->getArray('key');
		if (!$this->Request->isPost() || empty($keys)) 
		{
			DekiMessage::error($this->View->msg('Settings.data.no-selection'));
			$this->Request->redirect($this->getUrl('listing'));
			return;
		}
		
		switch ($this->Request->getVal('action'))
		{
			case 'delete':
				$keys = $this->Request->getArray('key');
				foreach ($keys as $configKey) 
				{
					wfSetConfig($configKey, null);
				}
				$return = wfSaveConfig();
				if (wfArrayVal($return, 'status', 0) != 200) 
				{
					DekiMessage::error(wfArrayVal($return, 'body/error/message'));
				}
				else
				{
					DekiMessage::success($this->View->msg('Settings.success.deleted'));
				}
				$this->Request->redirect($this->getUrl('listing'));
				return;

			default:
		}

		$existingkeys = $this->get_config(true);
		$delete = array(); // keys to be deleted
		
		//let's do a sanity check to make sure these keys exist before attempting to unset them
		$html = '';
		foreach ($keys as $key) 
		{
			if (array_key_exists($key, $existingkeys)) 
			{
				$html .= '<li>' . htmlspecialchars($key) . DekiForm::singleInput('hidden', 'key[]', $key) . '</li>';
			}
		}

		$this->View->set('keys-list', $html);
		$this->View->set('form.action', $this->getUrl('verify', array('action' => 'delete')));
		$this->View->set('form.backUrl', $this->getUrl('listing'));
		
		$this->View->output();
	}
	
	/**
	 * Renders the config key editing form. Never called directly.
	 * 
	 * @param string $key
	 * @param string $value
	 * @return
	 */
	protected function form($key = null, $value = null) 
	{
		if ($value === true)
		{
			$value = 'true';
		}
		else if ($value === false)
		{
			$value = 'false';
		}

		$this->View->set('form.action', $this->getUrl('edit'));
		$this->View->set('form.key', $key);
		$this->View->set('form.value', $value);
		$this->View->set('href.cancel', $this->getUrl('listing'));

		$this->View->output();
	}
	
	private function split_keys($config, &$readonlykeys, &$editablekeys) 
	{
		if (empty($config)) 
		{
			return $readonlykeys;
		}
		foreach ($config as $k => $v) 
		{
			//read only keys should be hidden, or made to seem to be configurable
			$path = explode('/', $k);
			$end = end($path); //get the last key for comparison
			
			//@readonly comes in before #text, so hide it from view
			if (strcmp($end, '@readonly') == 0) 
			{
				array_pop($path);
				$readonlykeys[implode('/', $path)] = '';
				continue;
			}
			
			//if we see a @readonly value which already stores this key, don't display it here
			if (strcmp($end, '#text') == 0) 
			{
				array_pop($path);
				$k = implode('/', $path);
				if (array_key_exists($k, $readonlykeys)) 
				{
					$readonlykeys[$k] = $v;
					continue;
				}
			}
			$editablekeys[$k] = $v;
		}
	}
	
	private function get_config($asflat = false) 
	{
		$PlugResult = $this->Plug->At('site', 'settings')->Get();
		$PlugResult->handleResponse();
		if ($PlugResult->getStatus() != 200) 
		{
			return false;	
		}
		$config = $PlugResult->getAll('body/config', array());
		if ($asflat) 
		{
			return $this->config_to_flat_keys($config);
		}
		return $config;
	}
	
	//takes the array returned from dekihost and converts it into a flat array
	private function config_to_flat_keys($config, $parent = '') 
	{
		static $path, $keys;
		
		foreach ($config as $key => $val) 
		{
			$path = empty($parent) ? $key: $parent.'/'.$key;
			if (is_array($val)) 
			{
				$keys = $this->config_to_flat_keys($val, $path);
			}
			else 
			{
				$keys[$path] = $val;
			}
		}
		return $keys;
	}
}

new SiteConfigurationController();
