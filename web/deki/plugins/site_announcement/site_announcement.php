<?php
/**
 * Simple plugin for sending site wide messages.
 *
 * @note $wgAccouncement = 'Your message to announce!';
 * 
 */
class SiteAnnouncementPlugin extends DekiPlugin
{
	const PLUGIN_FOLDER = 'site_announcement';

	/**
	 * Initialize the plugin and hooks into the application
	 */
	public static function init()
	{
		DekiPlugin::registerHook(Hooks::SKIN_OVERRIDE_VARIABLES, array(__CLASS__, 'skinHook'));
	}
	
	/**
	 * Injects the banner into the skin header
	 * 
	 * @param $Template
	 * @return
	 */
	public static function skinHook(&$Template)
	{
		global $wgAnnouncement;
		if (isset($wgAnnouncement) && !empty($wgAnnouncement))
		{
			$View = self::createView(self::PLUGIN_FOLDER, 'banner');
			$View->set('announce', $wgAnnouncement);
		
			$html = $View->render();
			
			$header = $Template->haveData('pageheader');
			$header = $html . $header;
			$Template->set('pageheader', $header);
		}
	}	
}
SiteAnnouncementPlugin::init();
