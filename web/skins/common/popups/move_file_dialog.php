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

$dekiRoot = '../../../';
require_once($dekiRoot . 'includes/Defines.php');
require_once($dekiRoot . 'LocalSettings.php');
require_once($dekiRoot . 'includes/Setup.php');
?>
<?php echo '<?xml version="1.0" encoding="UTF-8"?>' . "\n"; ?>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en" dir="ltr">
    <head>
        <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
        <title><?php echo wfMsg('Dialog.MoveAttach.page-title'); ?></title>

		<link rel="stylesheet" type="text/css" media="screen" href="/skins/common/fonts.css" />
        <link rel="stylesheet" type="text/css" href="css/styles.css" />
		<script type="text/javascript" src="popup.js"></script>

        <link rel="stylesheet" type="text/css" href="/skins/common/icons.css" />

		<script type="text/javascript" src="/skins/common/yui/yahoo-dom-event/yahoo-dom-event.js"></script>
		<script type="text/javascript" src="/skins/common/yui/connection/connection.js"></script>
		<script type="text/javascript" src="/skins/common/yui/datasource/datasource.js"></script>
		<script type="text/javascript" src="/skins/common/yui/autocomplete/autocomplete.js"></script>

        <link rel="stylesheet" type="text/css" href="/skins/common/yui/columnnav/css/carousel.css" />
        <link rel="stylesheet" type="text/css" href="/skins/common/yui/columnnav/css/columnav.css" />
		<script type="text/javascript" src="/skins/common/yui/animation/animation.js"></script>
		<script type="text/javascript" src="/skins/common/yui/container/container.js"></script>
		<script type="text/javascript" src="/skins/common/yui/columnnav/js/carousel.js"></script>
		<script type="text/javascript" src="/skins/common/yui/columnnav/js/columnav.js"></script>
		<script type="text/javascript" src="/skins/common/yui/columnnav/js/json.js"></script>

		<?php /*include this to use the logger*/ //echo '<script type="text/javascript" src="/skins/common/yui/logger/logger.js"></script>'; ?>

		<link rel="stylesheet" type="text/css" href="/skins/common/yui/mindtouch/css/link_navigator.css" />
		<!--[if IE 6]><link rel="stylesheet" type="text/css" media="screen" href="/skins/common/yui/mindtouch/css/link_navigator.ie6.css"/><![endif]-->
		<script type="text/javascript" src="/skins/common/yui/mindtouch/link_navigator.js"></script>
		<script type="text/javascript" src="/skins/common/yui/mindtouch/move_file_navigator.js"></script>


		<script>
			if (YAHOO.lang.isObject(YAHOO.widget.Logger))
			{
				//oLogReader = new YAHOO.widget.LogReader();
				// Enable logging to firebug
				YAHOO.widget.Logger.enableBrowserConsole();
			}

			function dialogInit()
			{		
				// Link Navigator Configuration
				var lnCfg = {textLabel:				'linkTextLabel',
							 panelLabel:			'linkPanelLabel',
							 autoCompleteInput:		'linkSearchText',
							 autoCompleteContainer: 'linkSearch',
							 autoCompleteToNav:		'linkNav-switch',
							 autoCompleteResults:	'linkSearchResults',
							 columNavLoading:		'linkBrowserLoading',
							 columNavContainer:		'linkBrowser',
							 columNavDisplay:		'columnav',
							 columNavPrev:			'columnav-prev',
							 buttonNavHome:			'columnav-home',
							 buttonNavMyPage:		'columnav-mypage',
							 columNavSearchAgain:	'columnav-search',
							 buttonUpdateLink:		'linkButtonUpdate',
							 siteName:				'<?php echo wfEncodeJSString(htmlspecialchars($wgSitename)); ?>',
							 columNavPaneLoading:	'columnav-pane-loading',
							 fileInput:				'linkBrowserText',
							 fileLabel:				'linkBrowserLabel',
							 oPopup:				Popup
							};
				var linkNavigator = new YAHOO.mindtouch.LinkNavigator(lnCfg);
				
				// init dialog
				Popup.init({
					handlers : {
						submit : function() { return linkNavigator.updateLink(); },
						cancel : function() { return null; }
					},
					validate: function() { return true; }
				});

				// localization strings
				// <?php echo wfMsg('Dialog.LinkTwo.button-update-link'); ?>
				// <?php echo wfMsg('Dialog.LinkTwo.button-cancel'); ?>
				//var oParams = Popup.getParams();
				var oParams = { oPopup: Popup,
								nPageId: <?php echo isset($_GET['titleID']) ? (int)$_GET['titleID'] : 'null'; ?>,
								nFileId: <?php echo isset($_GET['attachID']) ? (int)$_GET['attachID'] : 'null'; ?>,
								sUserName: '<?php echo $wgUser->getName(); ?>'
							  };
				// init navigator
				linkNavigator.initNavigator(oParams);
			}
			YAHOO.util.Event.onDOMReady(dialogInit);

			<?php
				// include the localization for javascript
				$messages = array(	'Dialog.LinkTwo.label-search',
									'Dialog.LinkTwo.label-matches',
									'Dialog.LinkTwo.label-link',
									'Dialog.LinkTwo.label-navigate',
									'Dialog.LinkTwo.message-searching',
									'Dialog.LinkTwo.message-no-results',
									'Dialog.LinkTwo.message-links-not-matched',
									'Dialog.LinkTwo.message-found',
									'Dialog.LinkTwo.message-results',
									'Dialog.LinkTwo.message-enter-search',
									'Dialog.Rename.validating-new-title'
								  );
				echo Skin::jsLangVars($messages);
			?>
		</script>

		<style>
			div.linkRow {
				position: relative;
				margin: 15px 0px 15px 0px;
			}
			div.linkLabel {
				position: absolute; top: 0px; right: 450px;
				margin-right: 10px;
				font-weight: bold;
			}
			div.linkField {
				margin-left: 100px;
				white-space: nowrap;
			}
			div.buttons {
				text-align: right;
			}
			#columnav-search {
				display: none;
			}
			#moveMessage {
				float: left;
				display: none;
				width: 300px;
				white-space: normal;
			}
			#moveMessage span.error {
				color: #FF0000;
			}
		</style>
	</head>
	<body>
		<div class="tabs">
			<ul>
				<!-- <li class="search active"><a id="columnav-search" href="#"><span><?php echo wfMsg('Dialog.Common.search'); ?></span></a></li> -->
				<li class="browse active"><a id="linkNav-switch" href="#"><span><?php echo wfMsg('Dialog.Common.browse'); ?></span></a></li>
			</ul>
		</div>
		<div style="position: relative; width: 550px; height: 260px;">
			<div class="linkRow" style="margin: 0px 0px;">
				<div id="linkTextLabel" class="linkLabel" style="padding: 5px 0px;">
					<?php echo wfMsg('Dialog.LinkTwo.label-search'); ?>
				</div>
				
				<div class="linkField">
					<input id="linkSearchText" type="text" class="inputWidth" readonly="readonly" />
				</div>
			</div>

			<div class="linkRow" style="margin: 0px 0px;">
				<div id="linkTextLabel" class="linkLabel" style="padding: 5px 0px;">
					<?php echo wfMsg('Dialog.Rename.title'); ?>
				</div>

				<div class="linkField">
					<input id="fileName" type="text" class="inputWidth" />
				</div>
			</div>

			<div class="linkRow">
				<div id="linkPanelLabel" class="linkLabel">
					<?php echo wfMsg('Dialog.LinkTwo.label-matches'); ?>
				</div>

				<div class="linkField">

					<div id="linkSearch">
						<div id="linkSearchResults" class="linkNavigator"></div>
						<div>
							<a href="javascript:void(0)" id="linkNav-switch"><?php echo wfMsg('Dialog.LinkTwo.link-switch-to-navigator'); ?></a>
						</div>
					</div>

					<div id="linkBrowserLoading" class="linkNavigator" style="display: none;">
						<img src="/skins/common/icons/anim-circle.gif" /> <?php echo wfMsg('Dialog.LinkTwo.message-loading'); ?>
					</div>

					<div id="linkBrowser" style="display: none;">
						<div class="sidebuttons">
							<a href="javascript:void(0)" id="columnav-prev" class="buttonPrevious" title="<?php echo wfMsg('Dialog.Common.action-back');?>">
								<img src="/skins/common/images/nav-back.png" />
							</a>
							<a href="javascript:void(0)" id="columnav-home" class="buttonHome" title="<?php echo wfMsg('Dialog.Common.action-home');?>">
								<img src="/skins/common/images/nav-home.png" />
								<span><?php echo wfMsg('Dialog.Link.button-my-page'); ?></span>
							</a>
							<a href="javascript:void(0)" id="columnav-mypage" class="buttonMyPage" title="<?php echo wfMsg('Dialog.Common.action-mypage');?>">
								<img src="/skins/common/images/nav-user.png" />
								<span><?php echo htmlspecialchars($wgSitename); ?></span>
							</a>
						</div>
						<div id="columnav-pane-loading" class="carousel-loading"><img src="/skins/common/images/anim-large-circle.gif" /></div>
						<!-- columnav -->
						<div id="columnav" class="carousel-component"></div>
						<!-- end columnav -->
						<div>
							<a href="javascript:void(0)" id="columnav-search"><?php echo wfMsg('Dialog.LinkTwo.link-back-to-search'); ?></a>
						</div>
					</div>
				</div>
			</div>
			
			<div id="moveMessage" class="linkField"></div>
		</div>
	</body>
</html>
