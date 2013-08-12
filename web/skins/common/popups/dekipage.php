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
$appRoot = dirname(dirname(dirname(dirname(__FILE__))));

require_once $appRoot . '/includes/Defines.php';
require_once $appRoot . '/LocalSettings.php';
require_once $appRoot . '/includes/Setup.php';

$Request = DekiRequest::getInstance();
$pageId = $Request->getInt('pageId');

$Article = null;

if ( $pageId )
{
	$Title = Title::newFromID($pageId);

	if ( $Title )
	{
		$Article = new Article($Title);
		$Article->loadContent();
	}
}
?>
<?php
if (is_null($Article))
{
	echo '<html><body>';
		echo 'Page with id: "'. htmlspecialchars($pageId) .'" could not be loaded.';
	echo '</body></html>';
	exit();
}
?>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
	<meta http-equiv="Content-Type" content="text/html; charset=UTF-8" />
	<title><?php echo $Article->getTitle()->getDisplayText() ?></title>
	
	<?php echo Skin::getJavascript() ?>
	<script type="text/javascript" src="popup.js"></script>
	
	<script type="text/javascript">
		window.onload = function()
		{
			Popup.init();
		}
	</script>
	
	<?php echo $wgOut->getHeadHTML() ?>
</head>
<body>
	<h1><?php echo $Article->getTitle()->getDisplayText() ?></h1>
	<?php echo $Article->getContent() ?>
</body>
<?php echo $wgOut->getTailHTML() ?>
</html>
