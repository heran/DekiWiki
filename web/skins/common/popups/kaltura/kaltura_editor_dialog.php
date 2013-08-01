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

$kalturaRunning = false;
$params = "''";
$url = "''";
$platformUser = "\"" . KalturaHelpers::getSessionUser()->userId . "\"";
$kalturaSecret = KalturaHelpers::getPlatformKey("secret","");

if ($kalturaSecret != null && strlen($kalturaSecret) > 0)
{
    try
	{
		$kalturaRunning = true;
		$kClient = new KalturaClient(KalturaHelpers::getServiceConfiguration());
		$kalturaUser = KalturaHelpers::getPlatformKey("user","");
		$ksId = $kClient -> session -> start($kalturaSecret, KalturaHelpers::getSessionUser()->userId, KalturaSessionType::USER, null, 86400, "*");
		$kClient -> setKs($ksId );
		$url = "'" . KalturaHelpers::getSimpleEditorUrl(KalturaHelpers::getPlatformKey("uiconf/editor",null)) . "'";
		$params =  "'" . KalturaHelpers::flashVarsToString(KalturaHelpers::getSimpleEditorFlashVars($ksId,$_GET["entryID"], "entry", "")) . "'";
	}
    catch(Exception $exp)
	{
		die($exp->getMessage());
	}	
}


echo '<?xml version="1.0" encoding="UTF-8"?>';
?><!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en" dir="ltr">
    <head>
        <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
        <meta http-equiv="cache-control" content="no-cache" />
        <meta http-equiv="pragma" content="no-cache">
        <title><?php echo(wfMsg('Dialog.KalturaEditor.title')); ?></title>
        <link rel="stylesheet" type="text/css" href="../css/styles.css" />
        <script type="text/javascript" src="../popup.js"></script>
        <script type="text/javascript" src="../../swfobject.js"></script>
        <style type="text/css">
        	textarea {
	        	width: 300px;
	        	height: 65px;
	        	font: 11px Tahoma, Verdana, Sans-Serif;
        	}
        	html>body textarea {
	        	height: 70px;
        	}
        </style>
        <script type="text/javascript">
            var siteTemplate = null;
            var templateContent = null;
	    var kalturaData = '';
	    var kalturaUrl =  <?php echo $url  ?>;
	    var kalturaParams = <?php echo $params ?> ;  
	    var platformUser = <?php echo $platformUser ?>;
	  
            function submitForm() {
	      var param = new Object();
	      if (kalturaData == '')
	      {
        	      param.f_content = document.getElementById('video').value;
	      }
	      else
	      {
        	      param.f_content = "kaltura:///" + kalturaData;
	/*	      param.f_source = "kaltura"; */
	      }
        	      return param;
            };

            function Init()
            {
               Popup.init({
                    handlers: {
                        submit: submitForm
                    }
                })

            };
            
	   function onSimpleEditorBackClick()
	   {
		Popup.close();
           }
            window.onload = Init;
        </script>
    </head>


    <body>
        <form action=""> 
		<?php if ($kalturaRunning == true) { ?>
		    	<div id="divKalturaCw" style="width:890px;height:546px;">
		    	<script type="text/javascript">
			        var params = { };
					params.flashvars = kalturaParams;
					params.bgcolor = "#000000";
					params.allowScriptAccess = "always";
					params.allowFullScreen = "TRUE";
					params.allowNetworking = "all";
			        swfobject.embedSWF(kalturaUrl, "divKalturaCw", "890", "546", "9.0.0","/skins/common/expressInstall.swf", null, params, { id: "divKalturaCw" });				        
			   	</script>
        </form>
		<?php } ?>
    </body>
</html>
