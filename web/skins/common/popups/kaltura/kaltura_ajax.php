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

define( 'MINDTOUCH_DEKI', true );
require_once( '../../../../includes/Defines.php' );
require_once( '../../../../LocalSettings.php' );
require_once( '../../../../includes/Setup.php' );

require_once( 'kaltura_settings.php' );
require_once( 'KalturaClient.php' );
require_once( 'kaltura_logger.php' );
require_once( 'kaltura_helpers.php' );

try
{

	$kClient = new KalturaClient(KalturaHelpers::getServiceConfiguration());
	$kalturaUser = KalturaHelpers::getPlatformKey("user","");
	$kalturaSecret = KalturaHelpers::getPlatformKey("secret","");


	$ksId = $kClient -> session -> start($kalturaSecret, KalturaHelpers::getSessionUser()->userId, KalturaSessionType::USER);
	$kClient -> setKs($ksId);
	$mix = new KalturaMixEntry();
	$mix -> name = "Editable video";
	$mix -> editorType = KalturaEditorType::SIMPLE;
	$mix = $kClient -> mixing -> add($mix);

	$arrEntries = explode(',',$_POST['entries']);

	foreach($arrEntries as $index => $entryId)
	{
		if (!empty($entryId))
		{
			$kClient->mixing->appendMediaEntry($mix -> id, $entryId);
		}
	}
	echo $mix -> id;
}
catch(Exception $exp)
{

	die($exp->getMessage());
}


?>
