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

define (MINDTOUCH_DEKI, 'true');

require_once( '../../../includes/Defines.php' );
require_once( '../../../LocalSettings.php' );
require_once( '../../../includes/Setup.php' );


$title = wfMsg('Dialog.NoTopic.page-does-not-exist');
$message = wfMsg('Dialog.NoTopic.page-no-longer-exists');

echo('<?xml version="1.0" encoding="UTF-8"?>'); ?>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en" dir="ltr">
    <head>
        <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
        <title><?php echo($title);?></title>
        <script type="text/javascript" src="popup.js"></script>
		<link rel="stylesheet" type="text/css" media="screen" href="/skins/common/fonts.css" />
        <link rel="stylesheet" type="text/css" href="css/styles.css" />
        <script language="javascript">
        
		function init()
	    {
            Popup.init({
                handlers : {
                    submit : function() { return null; } ,
                    cancel : function() { return null; }
                },
                validate: function() { return true; }
            });

            var okButton = Popup.getButton(Popup.BTN_OK);
            okButton.destroy();

            var cancelButton = Popup.getButton(Popup.BTN_CANCEL);
            cancelButton = cancelButton.getElementsByTagName('button')[0];
            if ( cancelButton )
            	cancelButton.innerHTML = parent.wfMsg('close');

            var params = Popup.getParams();
	    }
        </script>
        <script type="text/javascript" src="/skins/common/_jscripts.php"></script>
    </head>
    <body onload="init();">
    	<div class="wrap">
    	<p><?php
    		echo($message);
    	?></p>
        </div>
    </body>
</html>
