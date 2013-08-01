<?php


if (defined('MINDTOUCH_DEKI')) :

class ATDSpellCheckerPlugin extends DekiPlugin
{
	const CONFIG_ATD_STATUS = 'ui/editor/atd-enabled';
	const CONFIG_ATD_IGNORE_TYPES = 'ui/editor/atd-ignore-types';

	const AJAX_FORMATTER = 'atdspellchecker';

	public static function init()
	{
		DekiPlugin::registerHook(Hooks::EDITOR_LOAD, array(__CLASS__, 'atdStatus'));
		DekiPlugin::registerHook(Hooks::AJAX_FORMAT . self::AJAX_FORMATTER, array(__CLASS__, 'ajaxHook'));
	}

	public static function atdStatus($Article, &$editorScripts, &$script)
	{
		$atdEnabled = wfGetConfig(self::CONFIG_ATD_STATUS, ATD_DEFAULT_STATUS);
		$atdEnabled = ($atdEnabled === true) ? 'true' : 'false';

		$script .= 'Deki.atdEnabled = ' . $atdEnabled . ';';

		if ('true' === $atdEnabled)
		{
			$atdIgnoreTypes = wfGetConfig(self::CONFIG_ATD_IGNORE_TYPES);

			if ($atdIgnoreTypes !== null)
			{
				$script .= 'Deki.atdIgnoreTypes = "' . addslashes($atdIgnoreTypes) . '";';
			}
		}

		// load default editor
		return self::UNHANDLED;
	}

	public static function ajaxHook(&$body, &$message, &$success)
	{
		$Request = DekiRequest::getInstance();

		if (!$Request->isPost())
		{
			$message = 'HTTP POST Required';
			$success = false;
			return;
		}

		$url = $Request->getVal('url');
		$lang = $Request->getVal('lang');
		$data = $Request->getVal('data');

		$apiKey = md5($Request->getHost());


		switch ($lang)
		{
			case 'fr': $atdHost = 'http://fr.service.afterthedeadline.com'; break;
			case 'de': $atdHost = 'http://de.service.afterthedeadline.com'; break;
			case 'pt': $atdHost = 'http://pt.service.afterthedeadline.com'; break;
			case 'es': $atdHost = 'http://es.service.afterthedeadline.com'; break;
			default: $atdHost = 'http://service.afterthedeadline.com'; break;
		}

		$postData = 'data=' . rawurlencode($data) . '&key=' . $apiKey;

		$result = self::post($postData, $atdHost, $url);

		if (isset ($result['body']))
		{
			if ($result['status'] == 200)
			{
				$body = $result['body'];
				$success = true;
			}
			else
			{
				$message = 'HTTP Error: ' . $result['status'];
				$success = false;
			}
		}
		else
		{
			$message = $result['error'];
			$success = false;
		}
	}

	protected static function post($request, $host, $path)
	{
		$HttpPlug = new HttpPlug($host);
		$HttpPlug = $HttpPlug->At($path)
				->WithHeader('Content-Type', 'application/x-www-form-urlencoded')
				->WithHeader('Content-Length', strlen($request))
				->WithHeader('User-Agent', 'AtD/0.1');

		return $HttpPlug->Post($request);
	}
}

ATDSpellCheckerPlugin::init();

endif;
