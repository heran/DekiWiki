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

class ExtensionsFormatter extends DekiFormatter
{
	protected $contentType = 'text/html';
	protected $requireXmlHttpRequest = false;
	

	public function format()
	{
		$Request = DekiRequest::getInstance();
		
		$action = $Request->getVal('action');

		switch ($action)
		{
			case 'autocomplete':
			case 'ac':
				$this->setContentType('application/json');
				$start = microtime(true);
				$query = $Request->getVal('query');

				$result = $this->searchExtensions($query);
				$end = microtime(true);
				// add the processing time to the result
				$result['execution'] = ($end - $start);
				
				echo json_encode($result);
				return;

			default:
				$Result = DekiPlug::getInstance()->At('site', 'functions')->WithApiKey()->Get();
				echo $Result->getVal('body');
		}
	}
	
	private function searchExtensions($query)
	{
		if (empty($query))
		{
			return array();
		}
		
		// TODO: cache the extensions on the filesystem in serialized PHP
		// search through the cached blob for the query
		
		$Result = DekiPlug::getInstance()->At('site', 'functions')->With('format', 'xml')->Get();
		$extensions = $Result->getAll('body/extensions/extension');
		
		$functions = array();
		foreach ($extensions as &$extension)
		{
			$namespace = !empty($extension['namespace']) ? $extension['namespace'] . '.' : '';
			foreach ($extension['function'] as &$function)
			{
				$functions[] = $namespace . $function['name'];
			}
			unset($function);
		}
		unset($extension);
		// ~200 ms
		
		$length = strlen($query);
		$results = array();
		foreach ($functions as &$function)
		{
			if (strncasecmp($query, $function, $length) == 0)
			{
				$results[] = $function;
			}
		}
		
		return array('results' => &$results);
	}
}

new ExtensionsFormatter();
