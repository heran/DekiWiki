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

require_once('gui_index.php');
// copied from index.php
// (guerrics) handles loading plugins and calling hooks
require_once($IP . $wgDekiPluginPath . '/deki_plugin.php');
// load plugins
DekiPlugin::loadSitePlugins();

/**
 * End point allows plugins to hook in AJAX functionality
 */
class DekiPluginsFormatter extends DekiFormatter
{
	const STATUS_ERROR = 0;
	const STATUS_OK = 200;
	const STATUS_ERROR_LOGIN = 401;
	const STATUS_ERROR_COMMERCIAL = 402;
	
	protected $contentType = 'text/plain';
	protected $requireXmlHttpRequest = false;
	/**
	 * By default, the plugin formatter disables caching to ease confusion.
	 */
	protected $disableCaching = true;

	protected $formatter = '';
	// @var string - determines the expected response format e.g. json, xml, text
	protected $responseFormat = 'json';
	// @var string - load plugins from a special namespace, like 'special:' (default empty)
	protected $namespace = '';
	
	public function __construct()
	{
		$Request = DekiRequest::getInstance();
		$this->formatter = $Request->getVal('formatter', null);
		$this->responseFormat = $Request->getVal('format', 'json');
		$this->namespace = $Request->getVal('namespace', null);

		if (empty($this->formatter))
		{
			exit('No hook specified. Please send a formatter name with your request.');
		}
		
		// set the content type based on the request
		$contentType = $this->contentType;
		switch ($this->responseFormat)
		{
			case 'json':
				$contentType = 'application/json';
				break;
			case 'xml':
				$contentType = 'application/xml';
				break;
			case 'jsonp':
				$contentType = 'application/javascript';
				break;
			default:
		}
		$requireXmlHttpRequest = $this->requireXmlHttpRequest;
		$disableCaching = $this->disableCaching;

		// allow the formatter to override these settings
		DekiPlugin::executeHook(Hooks::AJAX_INIT . $this->formatter, array(&$contentType, &$requireXmlHttpRequest, &$disableCaching));
		
		// apply the adjusted settings for this formatter
		$this->contentType = $contentType;
		$this->requireXmlHttpRequest = $requireXmlHttpRequest;
		$this->disableCaching = $disableCaching;
		
		parent::__construct();
	}
	
	public function format()
	{
		$body = '';
		$message = '';
		$success = false;
		$status = null;
		
		// activate hooks from special pages
		if (strcasecmp($this->namespace, 'special') == 0)
		{
			SpecialPageDispatchPlugin::loadSpecialPages();
		}
		
		$result = DekiPlugin::executeHook(Hooks::AJAX_FORMAT . $this->formatter, array(&$body, &$message, &$success, &$status));
		
		if (is_null($status))
		{
			$status = $success ? self::STATUS_OK : self::STATUS_ERROR;
		}   
		
		if ($result < DekiPlugin::HANDLED)
		{
			$message = 'Unhandled request';
			$success = false;
		}

		// @TODO: handled halting
		switch ($this->responseFormat)
		{
			default:
			case 'json':
				echo json_encode(
					array(
						'success' => (bool)$success,
						'status' => $status,
						'message' => $message,
						'body' => $body
					)
				);
				break;
				
			case 'jsonp':
				echo DekiRequest::getInstance()->getVal('callback', '') . '(';
				echo json_encode(
					array(
						'success' => (bool)$success,
						'status' => $status,
						'message' => $message,
						'body' => $body
					)
				);
				echo ');';
				break;

			case 'xml':
				// TODO: handled halting
				echo encode_xml(
					array('formatter' => array(
							'@success' => (bool)$success,
							'@status' => $status,
							'@message' => $message,
							'body' => $body
						)
					)
				);
				break;
			
			case 'custom':
				if (strlen($body) > 0)
				{
					echo $body;
				}
				break;
		}
	}
}
new DekiPluginsFormatter();
