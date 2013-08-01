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

$titleID = $_GET['titleID'];
wfCheckTitleId($titleID, 'userCanEdit');
$wgArticle = new Article(Title::newFromId($titleID));
$tags = $wgArticle->getTags();
$list = array();
if (!is_null($tags)) {
	foreach ($tags as $tag) {
		$list[] = $tag['@value'];
	}
}

echo '<?xml version="1.0" encoding="UTF-8"?>';
?><!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en" dir="ltr">
    <head>
        <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
        <meta http-equiv="cache-control" content="no-cache" />
        <meta http-equiv="pragma" content="no-cache">
        <title><?php echo(wfMsg('Dialog.Tags.page-title')); ?></title>
        
        <script type="text/javascript" src="popup.js"></script>
		<link rel="stylesheet" type="text/css" media="screen" href="/skins/common/fonts.css" />
        <link rel="stylesheet" type="text/css" href="css/styles.css" />
        <link rel="stylesheet" type="text/css" href="css/tags.css" />
        
        <script type="text/javascript">
        
		function init()
	    {
			document.getElementById('pageTags').focus();
            Popup.init({
                handlers : {
                    submit : submitHandler,
                    cancel : function() { return null; }
                },
                validate: function() { return true; }
            });
            
            var params = Popup.getParams();
	    }
        
		function submitHandler()
	    {
	        var params = {
		        pageId : <?php echo($titleID);?>,
				tags: document.getElementById('pageTags').value
	        }
	        return params;
	    }
		
        </script>
    </head>
    <body onload="init();">
    	<div class="wrap">
		    <div><textarea name="tags" id="pageTags"><?php echo(implode("\n", $list));?></textarea><br/><small><?php echo(wfMsg('Dialog.Tags.instructions')); ?></small></div>
		</div>
    </body>
</html>
