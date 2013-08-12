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

$titleID =  $wgRequest->getVal('titleID');
$userName =  $wgRequest->getVal('userName');
$nt = Title::newFromID($titleID);

wfCheckTitleId($titleID, 'userCanDelete');

$title = $nt->getDisplayText();
$hasChildren =  Article::areChildren(Title::newFromID($titleID));
echo '<?xml version="1.0" encoding="UTF-8"?>';
?><!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en" dir="ltr">
    <head>
        <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
        <title><?php echo(wfMsg('Dialog.Delete.page-title'));?></title>
        <script type="text/javascript" src="popup.js"></script>
        <link rel="stylesheet" type="text/css" media="screen" href="/skins/common/fonts.css" />
        <link rel="stylesheet" type="text/css" href="css/styles.css" />
        <link rel="stylesheet" type="text/css" href="css/delete.css" />
        <style type="text/css">
        	div.wrap {
	        	padding-top: 10px;
        	}
        	p,
        	div.cascade { 
	        	margin: 0; padding: 3px 0;
        	}
        </style>
        <script language="javascript">
		function init()
	    {
            Popup.init({
                handlers : {
                    submit : submitHandler,
                    cancel : function() { return null; }
                },
                validate: function() { return true; }
            });
            
            var params = Popup.getParams();
	    };
	    
	    var cascade = false;
	    
		function submitHandler()
	    {
	        var params = {
		        titleid : <?php echo($titleID);?>,
				cascade: document.getElementById('cascade') && document.getElementById('cascade').checked
	        }
	        return params;
	    };
	    
		function disableSubmitButtons() 
		{
			var submit = document.getElementById('submitButtons');
			var submits = submit.getElementsByTagName('input');
			for (var i = 0; i < submits.length; i++) {
				submits[i].disabled = true;
			}
		};
        </script>
    </head>
    <body onload="init();">
    	<div class="wrap">
        <form>
            <p><?php echo(wfMsg('Dialog.Delete.attempting-to-delete-page', $title)); ?></p>
            <p><?php echo(wfMsg('Dialog.Delete.warning'));?></p>
            
            <?php
            	if ($hasChildren) 
            	{
	            	echo('<div class="cascade"><input type="checkbox" id="cascade" /><label for="cascade">'.wfMsg('Dialog.delete.delete-children').'</label></div>');
            	}
            ?>
            <input type="hidden" id="wpTopicID" value="<?php echo $titleID ?>" />
        </form>
        </div>
    </body>
</html>
