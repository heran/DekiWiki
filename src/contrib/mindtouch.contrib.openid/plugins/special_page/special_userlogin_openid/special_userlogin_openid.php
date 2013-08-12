<?php
if (defined('MINDTOUCH_DEKI')) :
DekiPlugin::registerHook(Hooks::SPECIAL_USER_LOGIN, array('SpecialUserLoginOpenId', 'hook'), 10);

class SpecialUserLoginOpenId extends SpecialPagePlugin
{
	public static function hook($pageName, &$pageTitle, &$html, &$subhtml)
	{
		$Special = new self($pageName, basename(__FILE__, '.php'));

                $Request = DekiRequest::getInstance();
                $returnTo = urlencode($Request->getVal('returntotitle')); 

                $html .= '<p>To log in with OpenID, click <a href="/Special:OpenIdLogin?returntotitle=' . $returnTo . '">here</a>.</p>';
        }
}
endif;
