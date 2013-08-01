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

if (defined('MINDTOUCH_DEKI'))
{
	DekiPlugin::registerHook(Hooks::MAIN_PROCESS_TITLE, 'wfParamSwitches');
}

function wfParamSwitches(&$Request, &$Title)
{
	// @note for sharepoint integration
	// allow setting the skin via GET parameters skin.name & skin.style
	if ($Request->getVal('skin.name') && $Request->getVal('skin.style')) 
	{
		global $wgActiveTemplate, $wgActiveSkin;

		$wgActiveTemplate = $Request->getVal('skin.name');
		$wgActiveSkin = $Request->getVal('skin.style');	
	}

	// @note for sharepoint integration
	// before we try to restore a session, place a higher priority on the authtoken GET parameter 
	if (isset($_GET[DekiToken::KEY_NAME])) //do not use the request object, we need to specifically target GET parameters
	{
		global $wgOut;

		// blindly save the authtoken
		DekiToken::set($_GET[DekiToken::KEY_NAME]);
		
		// remove the authtoken GET parameter, or we'll end up looping forever
		$url = wfParseUrl($Request->getFullRequestURL());
		unset($url['query'][DekiToken::KEY_NAME]);

		// do we need to redirect or can the user just be updated?
		$wgOut->redirect(wfUnparseUrl($url));
	}
}
