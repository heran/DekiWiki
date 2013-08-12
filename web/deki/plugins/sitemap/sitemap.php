<?php
if (defined('MINDTOUCH_DEKI'))
{
	DekiPlugin::registerHook(Hooks::MAIN_PROCESS_TITLE, array('DekiSiteMap', 'execute'));
}

class DekiSiteMap extends DekiPlugin
{
	static function execute(&$Request, &$Title)
	{
		global $wgDreamServer, $wgDekiApi;
		/***
		 * MT RoyK: Special sitemap handling for search engines
		 * The sitemap xml file is required to be at the root of the domain, so the API url won't work. Instead, we need to load the contents and output.
		 * So here, we catch the special case and call the API and return the document, gzipped. 
		 * Using mod_rewrite isn't as easy (would need another PHP entry point, need to modify sites-available, and restart, etc.)
		 */
		$lpt = strtolower($Title->getPrefixedText());

		if ('sitemap.gz' == $lpt || 'sitemap.xml' == $lpt)
		{
			//create a new plug instead of using $wgDekiPlug cause we have to suppress the dream.out.format=php
			$Plug = new Plug($wgDreamServer, 'xml');
			$r = $Plug->At($wgDekiApi, 'pages')->With('format', 'sitemap')->Get();
			if ($lpt == 'sitemap.gz')
			{
				ob_start("ob_gzhandler");
				header("Cache-Control: must-revalidate");
				header("Expires: " . gmdate("D, d M Y H:i:s", time() + (3600)) . " GMT");
			}

			header('Content-type: '.$r['type']);
			echo($r['body']);
			exit();
		}
	}
}
