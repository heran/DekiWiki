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
require_once( '../../../includes/Defines.php' );
require_once( '../../../LocalSettings.php' );
require_once( '../../../includes/Setup.php' );

require_once( 'kaltura/kaltura_settings.php' );
require_once( 'kaltura/KalturaClient.php' );
require_once( 'kaltura/kaltura_logger.php' );
require_once( 'kaltura/kaltura_helpers.php' );

$kalturaRunning = false;
$params = "''";
$url = "''";

$platformUser = "\"" . KalturaHelpers::getSessionUser()->userId . "\"";
$platformConfig = KalturaHelpers::getPlatformConfig();
$kalturaEnabled = KalturaHelpers::getPlatformKey("enabled","");
$kalturaSecret = KalturaHelpers::getPlatformKey("secret","");
$kalturaDialogMode = KalturaHelpers::getPlatformKey("dialog","");
$mixEntry = "''";
$paramsNoMix = "''";
$paramsWithMix = "''";
$margin = 0;

if ($kalturaEnabled != null && ($kalturaEnabled == "1" || $kalturaEnabled == "yes" || $kalturaEnabled == "true"))
{
    try
	{
		$kalturaRunning = true;
		$kalturaPrimary = !($kalturaDialogMode=="basic");
		$margin = 150;
		$kClient = new KalturaClient(KalturaHelpers::getServiceConfiguration());
		$kalturaUser = KalturaHelpers::getPlatformKey("user","");
		$ksId = $kClient -> session -> start($kalturaSecret, KalturaHelpers::getSessionUser()->userId , KalturaSessionType::USER);
		$kClient -> setKs($ksId );
		$url = "'" . KalturaHelpers::getContributionWizardUrl(KalturaHelpers::getPlatformKey("uiconf/uploader",null)) . "'";
		$paramsNoMix =  "'" . KalturaHelpers::flashVarsToString(KalturaHelpers::getContributionWizardFlashVars($ksId)) . "'";
/*		$mix = new KalturaMixEntry();
		$mix -> name = "Auto MindTouch";
		$mix -> editorType = KalturaEditorType::ADVANCED;
		$mix = $kClient -> mixing -> add($mix);
		$mixEntry = "'" . $mix -> id . "'";
		$paramsWithMix =  "'" . KalturaHelpers::flashVarsToString(KalturaHelpers::getContributionWizardFlashVars($ksId, $mix -> id,'','entry')) . "'";*/
		$paramsWithMix = $paramsNoMix;
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
        <title><?php echo(wfMsg('Dialog.Video.page-title')); ?></title>
		<link rel="stylesheet" type="text/css" media="screen" href="/skins/common/fonts.css" />
        <link rel="stylesheet" type="text/css" href="css/styles.css" />
        <link rel="stylesheet" type="text/css" href="http://yui.yahooapis.com/2.7.0/build/reset/reset-min.css">
        <link rel="stylesheet" type="text/css" href="css/kaltura_styles.css">
<!--[if IE 7]>
        <link rel="stylesheet" type="text/css" href="css/kaltura_ie7.css">
<![endif]-->
<!--[if IE 6]>
        <link rel="stylesheet" type="text/css" href="css/kaltura_ie6.css">
<![endif]-->

        <script type="text/javascript" src="popup.js"></script>
        <script type="text/javascript" src="/skins/common/swfobject.js"></script>
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
	    var entryType = <?php echo KalturaEntryType::MEDIA_CLIP ?>;
	    var MIX_TYPE = <?php echo KalturaEntryType::MIX ?>;
	    var CLIP_TYPE = <?php echo KalturaEntryType::MEDIA_CLIP ?>;
	   /* var mixEntry = <?php echo $mixEntry ?>;*/
	    var kalturaUrl =  <?php echo $url  ?>;
	    var kalturaParamsNoMix = <?php echo $paramsNoMix ?> ;
	    var kalturaParamsWithMix = <?php echo $paramsWithMix ?> ; 
	    var platformUser = <?php echo $platformUser ?>;
	    var size= "";	  
            function submitForm() {
	      var param = new Object();
	      if (kalturaData == '')
	      {
        	      param.f_content = document.getElementById('video').value;
        	      param.f_width = document.getElementById('width').value;
        	      param.f_height = document.getElementById('height').value;
	      }
	      else
	      {
        	      param.f_content = "kaltura:///" + kalturaData + size;
	/*	      param.f_source = "kaltura"; */
	      }
        	      return param;
            };
            
	    function switchDisplay()
	    {
		<?php if ($kalturaRunning == true) { ?>
			if (document.getElementById('divKalturaCw').style.display == "none")
			{
				document.getElementById('divMT').style.display = "none";
				document.getElementById('divKalturaCw').style.display = "block";
				var okButton =	Popup.getButton(Popup.BTN_OK);
				okButton.setStyle("display", "none");
				var cnclButton = Popup.getButton(Popup.BTN_CANCEL);
				cnclButton.setStyle("display", "none");

			}
			else
			{
				document.getElementById('divKalturaCw').style.display = "none";
				document.getElementById('divMT').style.display = "block";
				var okButton =	Popup.getButton(Popup.BTN_OK);
				okButton.setStyle("display", "");
				var cnclButton = Popup.getButton(Popup.BTN_CANCEL);
				cnclButton.setStyle("display", "");
			}
		<?php } else { ?>
			if (document.getElementById('id_p_adv').style.display == "none")
			{
				document.getElementById('id_p_adv').style.display = "block";
				document.getElementById('id_p_basic').style.display = "none";
				document.getElementById('lnkAdv').innerHTML = "<a href=\"#\" style=\"\" onclick=\"switchDisplay()\"><?php echo(wfMsg('Dialog.Video.Kaltura.basic-mode')); ?></a>";
				document.getElementById('lnkAdv').className = "mode4";
			}
			else
			{
				document.getElementById('id_p_adv').style.display = "none";
				document.getElementById('id_p_basic').style.display = "block";
				document.getElementById('lnkAdv').innerHTML = "<a href=\"#\" style=\"\" onclick=\"switchDisplay()\"><?php echo(wfMsg('Dialog.Video.Kaltura.advanced-mode')); ?></a>";
				document.getElementById('lnkAdv').className = "mode3";
			}		
		<?php } ?>
			
	    }

	    function onContributionWizardAfterAddEntry(param)
	    {

		if (entryType == MIX_TYPE)
		{
			var baseUrl='/skins/common/popups/kaltura/kaltura_ajax.php';
			var entries='';
			for (i=0; i < param.length; i++)
			{
				var entryN = (param[i].uniqueID == null ? param[i].entryId: param[i].uniqueID);
				entries +=  entryN + ',';
			}	
                        parent.Deki.$.ajax({
                url: baseUrl,
                type: 'POST',
		data: {'entries': entries},
                timeout: 10000,
                error: function()
                {
                        alert("failure");
                },
                success: function(data)
                {
			kalturaData = data + "?edit=yes";
			Popup.submit();
                }
        });

			/*kalturaData = "kaltura.Show(\"" + param[0].entryId + "\",\"" + platformUser + "\")" ;*/
		}
		else
		{
				var entryN = (param[0].uniqueID == null ? param[0].entryId: param[0].uniqueID);
			kalturaData = entryN ;
			Popup.submit();
/*			kalturaData = "kaltura.Show(" + param[0].kshowId + "," + platformUser + ")";*/
		}
	    }

            function Init()
            {
               Popup.init({
                    handlers: {
                        submit: submitForm
                    }
                })

		<?php if ($kalturaRunning == true) { ?>
			document.getElementById('divMT').style.height = "402px";
			document.getElementById('divMT').style.textAlign = "center";
			document.getElementById('bd').style.height="412px";
			document.getElementById('bd').style.width="760px";
		<?php 	if ($kalturaPrimary == false) { ?>
			document.getElementById('lnkAdv').className = "mode2";
			document.getElementById('divMT').style.display = "block";
		<?php 	} else {?>
			document.getElementById('lnkAdv').style.display = "block";
	       		switchDisplay();
		<?php 	} ?>
			Popup.resize({height: "412px", width: "760px"});
		<?php } else {?>
			document.getElementById('divMT').style.height = "218px";
			document.getElementById('divMT').style.display = "block";
			document.getElementById('lnkAdv').className = "mode3";
		<?php } ?>

		<?php if (DekiSite::isRunningCloud()): ?>
			document.getElementById('lnkAdv').style.display = 'none';
		<?php endif; ?>

            };
            
            window.onload = Init;
        </script>
    </head>


    <body id="bd" style="overflow:hidden">
        <form action=""> 
        	<div id="divMT" class="wrap" style="display:none">
            	<div id="id_p_basic" style="margin-top:<?php echo $margin?>px;">
					<div><label for="video"><?php echo(wfMsg('Dialog.Video.url-of-media-file'));?>: </label><input type="text" value="" name="video" id="video" style="width:280px;"/></div>
					<div style="margin-top: .5em">
						<label for="width"><?php echo(wfMsg('Dialog.Video.width'));?>: </label><input type="text" value="" name="width" id="width" style="width:80px;"/>
						<label for="height"><?php echo(wfMsg('Dialog.Video.height'));?>: </label><input type="text" value="" name="height" id="height" style="width:80px;"/>
					</div>
				</div>
            	<p id="id_p_adv" style="margin-top:<?php echo $margin?>px;display:none;"><?php echo(wfMsg('Dialog.Video.Kaltura.register-text')); ?></p> 
				<div id="lnkAdv" class="mode2"><a href="#" style="" onclick="switchDisplay()"><?php echo(wfMsg('Dialog.Video.Kaltura.advanced-mode')); ?></a></div>
			</div>
		<?php if ($kalturaRunning == true) { ?>
		    	<script type="text/javascript">
				function ShowKalturaContributionWizard()
				{
				        if (document.getElementById("chkLarge").checked)
				        {
					       size="\",\"" + document.getElementById("largeWidth").innerHTML + "\",\"365";
				        }
				        if (document.getElementById("chkSmall").checked)
				        {
					        size="\",\"" + document.getElementById("smallWidth").innerHTML + "\",\"260";
				        }
				        if (document.getElementById("chkCustom").checked)
				        {
					       var wdt = document.getElementById("inpWidth").value;
					       var hgt = parseInt(wdt,10)*3/4 + 65;
					       size = "\",\"" +  wdt + "\",\"" + hgt ;
				        }
				        var params = { };
						params.flashvars = (entryType == MIX_TYPE) ? kalturaParamsWithMix : kalturaParamsNoMix;
						params.bgcolor = "#000000";
						params.allowScriptAccess = "always";
						params.allowFullScreen = "TRUE";
						params.allowNetworking = "all";
				        swfobject.embedSWF(kalturaUrl, "divKalturaCw", "760", "402", "9.0.0","/skins/common/expressInstall.swf", null, params, { id: "divKalturaCw" });				        
				}

				function switchVidType(type)
				{
					entryType = type;
					document.getElementById("typeVidNorm").style.display = (type == CLIP_TYPE ? "block" : "none");
					document.getElementById("typeVidEdit").style.display = (type ==  MIX_TYPE ? "block" : "none");
					document.getElementById("inpVidNorm").className = (type == CLIP_TYPE ? "input selected" : "input");
					document.getElementById("inpVidEdit").className = (type ==  MIX_TYPE ? "input selected" : "input");
					document.getElementById("chkVidNormal").checked = (type ==  MIX_TYPE ? false : true);
					document.getElementById("chkVidEdit").checked = (type ==  CLIP_TYPE ? false : true);
				}

				function SwitchAspect(type)
				{
					document.getElementById("aspctNorm").className = (type == 1 ? "selected" : "");
					document.getElementById("aspctWide").className = (type == 2 ? "selected" : "");
					document.getElementById("largeWidth").innerHTML = (type == 2 ? "533" : "400");
					document.getElementById("smallWidth").innerHTML = (type == 2 ? "462" : "260");
				}

				function Cancel()
				{
					Popup.cancel();
				}

		   	</script>
		    	<div id="divKalturaCw" class="video-wrap" style="display:none">
<div class="type">
	<h2><?php echo(wfMsg('Dialog.Video.Kaltura.video-type')); ?></h2>
		<div class="normal">
			<div id="inpVidNorm" class="input selected" onClick="switchVidType(<?php echo KalturaEntryType::MEDIA_CLIP ?>);"><input id="chkVidNormal" name="vidType" type="radio" value="normal"  checked><?php echo(wfMsg('Dialog.Video.Kaltura.normal')); ?></div>
		</div>
		<div class="editable">
			<div id="inpVidEdit" class="input" onClick="switchVidType(<?php echo KalturaEntryType::MIX ?>);"><input name="vidType" id="chkVidEdit" type="radio" value="editable"> <?php echo(wfMsg('Dialog.Video.Kaltura.editable')); ?></div>
		</div>
		<div id="typeVidNorm" class="description-top"><?php echo(wfMsg('Dialog.Video.Kaltura.no-remix-detail')); ?></div>
		<div id="typeVidEdit" class="description-bottom" style="display:none"><?php echo(wfMsg('Dialog.Video.Kaltura.allow-remix-detail')); ?></div>
</div>

<div class="aspect-ratio">
	<h2><?php echo(wfMsg('Dialog.Video.Kaltura.aspect-ratio')); ?></h2>
	<div class="normal">
		<a class="selected" id="aspctNorm" href="#" onclick="SwitchAspect(1);"><?php echo(wfMsg('Dialog.Video.Kaltura.normal')); ?><br/>
		4:3</a>
	</div>
	<div class="widescreen">
		<a id="aspctWide" href="#" onclick="SwitchAspect(2);"><?php echo(wfMsg('Dialog.Video.Kaltura.wide')); ?><br/>
		16:9</a>
	</div>
</div>

<div class="size">
	<h2><?php echo(wfMsg('Dialog.Video.Kaltura.video-size')); ?></h2>
	<span>
	<input name="vidSize"  id="chkLarge" class="radio" type="radio" value="large" checked><?php echo(wfMsg('Dialog.Video.Kaltura.large')) . ' '; ?>(<span id="largeWidth">400</span> x 365)<br/>
	<input name="vidSize" id="chkSmall" class="radio" type="radio" value="small"><?php echo(wfMsg('Dialog.Video.Kaltura.small')) . ' '; ?>(<span id="smallWidth">260</span> x 260)<br/>
	<input name="vidSize" id="chkCustom" class="radio" type="radio" value="custom"><?php echo(wfMsg('Dialog.Video.Kaltura.custom')); ?>
	<div class="custom">
		<?php echo(wfMsg('Dialog.Video.Kaltura.width')); ?> <input  id="inpWidth" type="text"/>
	</div>
	</span>
</div>

<div class="mode"><a href="#" onclick="switchDisplay()"><?php echo(wfMsg('Dialog.Video.Kaltura.basic-mode')); ?></a></div>

<div class="buttons">
	<button onclick="ShowKalturaContributionWizard()" class="next" style="white-space:nowrap;"><?php echo(wfMsg('Dialog.Video.Kaltura.next')); ?></button>
	<button onclick="Cancel();" class="cancel"><?php echo(wfMsg('Dialog.Video.Kaltura.cancel')); ?></button>
</div>
			</div>
        </form>
			
		<?php } ?>
    </body>
</html>
