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

/**
 * plugin for getting site functions
 *
 */
class JsonSiteFunctionsPlugin extends DekiPlugin
{
	/**
	 * Formatters
	 */
	const AJAX_FORMATTER = 'json_site_functions';

	/**
	 * Methods
	 */
	const METHOD_FUNCTION = 'function';
	const METHOD_FUNCTIONS = 'functions';
	const METHOD_TRANSFORMATIONS = 'transformations';

	/**
	 * Register hooks
	 */
	public static function init()
	{
		DekiPlugin::registerHook(Hooks::EDITOR_LOAD, array(__CLASS__, 'editorLoad'));
		
		DekiPlugin::registerHook(Hooks::AJAX_INIT . self::AJAX_FORMATTER, array(__CLASS__, 'ajaxInit'));
		DekiPlugin::registerHook(Hooks::AJAX_FORMAT . self::AJAX_FORMATTER, array(__CLASS__, 'ajaxHook'));
	}

	public static function ajaxInit(&$contentType, &$requireXmlHttpRequest, &$disableCaching)
	{
		$disableCaching = false;
		
		$expires = 60 * 60 * 24 * 365; // 365 days
		header('Pragma: public');
		header('Cache-Control: maxage=' . $expires . ', public');
		header('Expires: ' . gmdate('D, d M Y H:i:s', time() + $expires) . ' GMT');
	}
	
	public static function editorLoad($Article, &$editorScripts, &$script)
	{
		try
		{
			$extensions = self::getExtensions();
			$crc = dechex(crc32(serialize($extensions)));
			$script .= "Deki.EditorExtensionsToken = '" . $crc . "';";
		}
		catch (Exception $e) {}

		return self::HANDLED;
	}

	/**
	 * Hook for AJAX requests
	 *
	 * @param string $body
	 * @param string $message
	 * @param boolean $success
	 */
	public static function ajaxHook(&$body, &$message, &$success)
	{
		try
		{
			$extensions = self::getExtensions();
		}
		catch (Exception $e)
		{
			$success = false;
			$message = $e->getMessage();
			return;
		}
		
		$etag = md5(serialize($extensions));

		if (isset($_SERVER['HTTP_IF_NONE_MATCH']) && $_SERVER['HTTP_IF_NONE_MATCH'] == $etag)
		{
			header("HTTP/1.0 304 Not Modified");
			header('Content-Length: 0');
			exit();
		}

		header('Etag: ' . $etag);
		header("Last-Modified: " . gmdate("D, d M Y H:i:s", time()) . " GMT");

		$Request = DekiRequest::getInstance();
		$method = $Request->getVal('method');

		$body = array();
		$success = true;

		foreach ($extensions as $extension)
		{
			$X = new XArray($extension);

			$namespace = $X->getVal('namespace');
			$functions = $X->getAll('function', array());

			if ($method == 'transformations')
			{
				uasort($functions, array(__CLASS__, 'sortFunctionByName'));
			}

			foreach ($functions as $function)
			{
				$X = new XArray($function);

				$name = is_null($namespace) ? $X->getVal('name') : $namespace . '.' . $X->getVal('name');

				switch ($method)
				{
					case self::METHOD_FUNCTION:
						if ($Request->getVal('function') !== $name)
						{
							break;
						}
					case self::METHOD_FUNCTIONS:
						$params = $X->getAll('param', array());
						$return = $X->getVal('return');

						$paramsWithType = array();
						foreach ($params as $param)
						{
							$paramStr = $param['@name'] . ' : ' . $param['@type'];
							if (isset($param['@optional']) && $param['@optional'] == 'true')
							{
								$paramStr = '[' . $paramStr . ']';
							}
							array_push($paramsWithType, $paramStr);
						}

						$paramsStr = '(' . implode(', ', $paramsWithType) . ')';
						if ($return)
						{
							$paramsStr .= ' : ' . $return['@type'];
						}

						if ($method == self::METHOD_FUNCTION)
						{
							$body = array(
								'name' => $name,
								'info' => array(
									'paramsStr' => $paramsStr,
									'params' => $params,
									'description' => $X->getVal('description', '')
								)
							);
							return;
						}
						else
						{
							$body[] = array(
								'name' => $name,
								'info' => array(
									'paramsStr' => $paramsStr,
									'description' => $X->getVal('description', '')
								)
							);
						}
						break;
					case self::METHOD_TRANSFORMATIONS:
						if (isset($function['@transform']))
						{
							// function is a content transformation
							// guerrics: should we have a display title?
							$body[] = array(
								'func' => $name,
								'tags' => $function['@transform']
							);
						}
						break;
					default;
						break;
				}
			}
		}
	}
	
	private static function getExtensions()
	{
		$Result = DekiPlug::getInstance()->At('site', 'functions')->With('format', 'xml')->Get();

		if (!$Result->isSuccess())
		{
			$error = $Result->getVal('body/error');
			$message = !empty($error['message']) ? $error['title'] .': '. $error['message'] :
								wfMsg('System.Error.error') .': '. $Result->getStatus();
			
			throw new Exception($message);
		}

		$extensions = $Result->getAll('body/extensions/extension', array());
		
		return $extensions;
	}

	/**
	 * Sorting method for api data
	 *
	 * @param array $a - transformation function data
	 * @param array $b - transformation function data
	 * @return integer
	 */
	private static function sortFunctionByName($a, $b)
	{
		return strcmp($a['name'], $b['name']);
	}
}

JsonSiteFunctionsPlugin::init();
