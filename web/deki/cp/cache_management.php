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


class CacheManagement extends DekiControlPanel
{
	// needed to determine the template folder
	protected $name = 'cache_management';
	protected $ui_prefix = 'cache-';
	
	public function index() {
		$this->executeAction('listing');
	}
	// main listing view
	public function listing()
	{
		if ($this->Request->isPost() && $this->POST_listing()) 
		{
			$this->Request->redirect($this->getUrl());
			return;
		}
		
		// display indexer results if available
		$LuceneResult = $this->Plug->At('site', 'search', 'rebuild')->Get();
		$pending = 0;
		if ($LuceneResult->isSuccess())
		{
			$pending = $LuceneResult->getVal('body/search/pending');
		}
		$this->View->set('lucene.pending', $pending); 

		$this->View->set('form.action', $this->getUrl());

		// cache options
		$cacheMaster = false;
		$License = DekiLicense::getCurrent();
		$this->View->set('cache.disabled', !$License->hasCapabilityCaching());
		$this->View->set('commercial.url', ProductURL::CACHING);
		if ($License->hasCapabilityCaching())
		{
			$this->View->set('form.cache_master.options', $this->getCacheOptions());
			
			// bug 8387
			$cacheMaster = wfGetConfig('cache/master', 'instance');
			$this->View->set('form.cache_master', $cacheMaster);

			$this->View->set('form.cache_roles', wfGetConfig('cache/roles'));
			$this->View->set('form.cache_services', wfGetConfig('cache/services'));
			$this->View->set('form.cache_users', wfGetConfig('cache/users'));
			$this->View->set('form.cache_bans', wfGetConfig('cache/bans'));
			$this->View->set('form.cache_anonymous_output', wfGetConfig('cache/anonymous-output'));
		}

		// output a warning message if legacy cache != master
		if ($cacheMaster == 'memcache')
		{
			DekiMessage::info($this->View->msg('CacheManagement.info.memcache', ProductURL::CONFIGURATION));
		}

		$this->View->output();
	}

	protected function POST_listing()
	{
		$rebuild = $this->Request->getVal('rebuild');
		if ($rebuild == 'ui') 
		{
			// walk through the skins cache and remove all cache-* files
			$this->clearUICache();
			DekiMessage::success($this->View->msg('CacheManagement.success.uicache'));
		}
		else if ($rebuild == 'search')
		{
			// @note guerrics: why is the API key required to rebuild the search index?
			$Result = $this->Plug->At('site', 'search', 'rebuild')->WithApiKey()->Post();
			sleep(1); // let the API receive the rebuild request, so we can see the progress.

			if (!$Result->handleResponse())
			{
				DekiMessage::error($this->View->msg('CacheManagement.error.searchindex'));
				return false;
			}

			DekiMessage::success($this->View->msg('CacheManagement.success.searchindex'));
		}
		else if ($this->Request->getVal('submit') == 'cache') 
		{
			$enum = array_keys($this->getCacheOptions());
			wfSetConfig('cache/master', $this->Request->getEnum('cache_master', $enum, 'false'));

			// per component caching is killed when changing master settings
			wfSetConfig('cache/roles', null);
			wfSetConfig('cache/services', null);
			wfSetConfig('cache/users', null);
			wfSetConfig('cache/bans', null);
			wfSetConfig('cache/anonymous-output', null);
			wfSaveConfig();

			DekiMessage::success($this->View->msg('CacheManagement.success.options'));
		}

		return true;
	}


	/**
	 * Generates the options array for cache master setting
	 * @return array
	 */
	private function getCacheOptions()
	{
		$License = DekiLicense::getCurrent();
		$options = array('false' => $this->View->msg('CacheManagement.master.disabled'));
		if ($License->hasCapabilityCaching())
		{
			$options['request'] = $this->View->msg('CacheManagement.master.request');
			$options['instance'] = $this->View->msg('CacheManagement.master.instance');
		}
		if ($License->hasCapabilityMemCache())
		{
			$options['memcache'] = $this->View->msg('CacheManagement.master.memcache');
		}
		return $options;
	}

	private function clearUICache() 
	{
		global $wgCacheDirectory;
		
		$files = wfGetFileNames($wgCacheDirectory);
		if (empty($files)) 
		{
			return;
		}
		$len = strlen($this->ui_prefix);
		foreach ($files as $filename => $path) 
		{
			if (strcmp(substr($filename, 0, $len), $this->ui_prefix) == 0) 
			{
				@unlink($path);	
			}
		}
	}
}

new CacheManagement();
