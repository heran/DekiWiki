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
if ($wgRequest->getVal('cntxtID')) 
{
	if ($wgRequest->getVal('cntxtID') > 0) 
	{
		wfCheckTitleId($wgRequest->getVal('cntxtID'), 'userCanRead');
	}
}
else
{
	if ($wgRequest->getVal('contextID') > 0) 
	{
		wfCheckTitleId($wgRequest->getVal('contextID'), 'userCanRead');
	}
}

$dialogTitle = ($wgRequest->getVal('update')) ? wfMsg('Dialog.Image.page-title-update') : wfMsg('Dialog.Image.page-title');

?>
<?php echo '<?xml version="1.0" encoding="UTF-8"?>' . "\n"; ?>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en" dir="ltr">
    <head>
        <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
        <title><?php echo $dialogTitle; ?></title>
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
		<script type="text/javascript" src="/skins/common/yui/mindtouch/image_navigator.js"></script>


		<script>
			if (YAHOO.lang.isObject(YAHOO.widget.Logger))
			{
				//oLogReader = new YAHOO.widget.LogReader();
				// Enable logging to firebug
				YAHOO.widget.Logger.enableBrowserConsole();
			}

			function dialogInit()
			{
				// init dialog
				Popup.init();

				// Link Navigator Configuration
				var lnCfg = {
					 textLabel:				'linkTextLabel',
					 panelLabel:			'linkPanelLabel',
					 autoCompleteInput:		'linkSearchText',
					 autoCompleteContainer: 'linkSearch',
					 autoCompleteToNav:		'linkNav-switch',
					 autoCompleteResults:	'linkSearchResults',
					 columNavLoading:		'linkBrowserLoading',
					 columNavContainer:		'linkBrowser',
					 columNavDisplay:		'columnav',
					 columNavPrev:			'columnav-prev',
					 buttonNavCurrent:		'columnav-current',
					 buttonNavHome:			'columnav-home',
					 buttonNavMyPage:		'columnav-mypage',
					 columNavSearchAgain:	'columnav-search',
					 buttonUpdateLink:		'linkButtonUpdate',
					 siteName:				'<?php echo wfEncodeJSString(htmlspecialchars($wgSitename)); ?>',
					 columNavPaneLoading:	'columnav-pane-loading',
					 fileInput:				'linkBrowserText',
					 fileLabel:				'linkBrowserLabel',
					 baseUrl:				'<?php echo $wgServer; ?>',
					 apiUrl:				'<?php echo sprintf('/%s/%s', $wgApi, $wgDeki); ?>',
					 navigatorText:			'linkBrowseText',
					 <?php
						$pageId = isset($_GET['contextID']) ? (int)$_GET['contextID'] : null;
						$Title = Title::newFromID($pageId);
						if (!is_null($Title)) 
						{
							// root page check
							if ($Title->getPrefixedText() == '')
							{
								$pagePath = '/';
								$pageTitle = $wgSitename;
							}
							else
							{
								$pagePath = $Title->getPrefixedText();
								$pageTitle = $Title->getText();
							}
						}
						else 
						{
							$pageId = 0;
							$pagePath = '';
							$pageTitle = '';
						}
					 ?>
					 currentPageId:			<?php echo $pageId; ?>,
					 currentPagePath:		"<?php echo wfEscapeString($pagePath); ?>",
					 currentPageTitle:		"<?php echo wfEscapeString($pageTitle); ?>",
					 oPopup:				Popup
				};

				try
				{
					var linkNavigator = new YAHOO.mindtouch.LinkNavigator(lnCfg);
					
					// configure dialog
					Popup.setConfig({
						handlers : {
							submit : function() { return linkNavigator.updateLink(); },
							cancel : function() { return null; }
						},
						validate: function() { return true; }
					});
	
					var oParams = Popup.getParams();
	
					// init navigator
					linkNavigator.initNavigator(oParams);
				}
				catch (ex)
				{
					alert('Dialog initialization failed.');
					Popup.disableButton(Popup.BTN_OK);
				}
			}
			
			if ( YAHOO.env.ua.ie )
			{
				// @see MT-9604
				YAHOO.util.Event.addListener(window, 'load', dialogInit);
			}
			else
			{
				YAHOO.util.Event.onDOMReady(dialogInit);
			}
			
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
									'Dialog.Image.no-preview-available'
								  );
				echo Skin::jsLangVars($messages);
			?>
		</script>

		<style>
			body {
				overflow: hidden;
			}
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
			
			div.customSize {
				padding-top: 5px;
			}

			#imageViewer {
				border: solid 1px black;
				border-left: none;
				width: 215px;
				/*height: 175px;*/
				background-color: #FFF;
				overflow: auto;
				position: absolute;
				float: left;
				top: 0px;
				left: 222px;
			}
			#imageView {
				text-align: center;
				vertical-align: middle;
				color: #999;
			}
		</style>
	</head>
	<body>
		<div class="tabs">
			<ul>
				<li class="search"><a id="columnav-search" href="#"><span><?php echo wfMsg('Dialog.Common.search'); ?></span></a></li>
				<li class="browse"><a id="linkNav-switch" href="#"><span><?php echo wfMsg('Dialog.Common.browse'); ?></span></a></li>
			</ul>
			<div class="clearfix"></div>
		</div>
		<div style="position: relative; width: 550px;">
			<div class="linkRow">
				<div id="linkTextLabel" class="linkLabel">
					<?php echo wfMsg('Dialog.LinkTwo.label-search'); ?>
				</div>

				<div class="linkField">
					<form style="display: inline;">
						<input id="linkSearchText" type="text" class="inputWidth" />
						<div id="linkBrowseText" class="inputWidth" style="display: none; padding: 2px; height: 15px; overflow: hidden;"></div>
						<label id="linkBrowserLabel" class="fileButton" title="<?php echo(wfMsg('Dialog.LinkTwo.label-network-share'));?>">
							<input id="linkBrowserText" type="file" />
						</label>
					</form>
				</div>
			</div>

			<div class="linkRow">
				<div id="linkPanelLabel" class="linkLabel">
					<?php echo wfMsg('Dialog.LinkTwo.label-matches'); ?>
				</div>

				<div class="linkField">

					<div id="linkSearch">
						<div id="linkSearchResults" class="linkNavigator"></div>
					</div>

					<div id="linkBrowserLoading" class="linkNavigator" style="display: none;">
						<img src="/skins/common/icons/anim-circle.gif" /> <?php echo wfMsg('Dialog.LinkTwo.message-loading'); ?>
					</div>

					<div id="linkBrowser" style="display: none;">
						<div class="sidebuttons">
							<a href="javascript:void(0)" id="columnav-prev" class="buttonPrevious" title="<?php echo wfMsg('Dialog.Common.action-back');?>">
								<img src="/skins/common/images/nav-back.png" />
							</a>
							<a href="javascript:void(0)" id="columnav-current" class="buttonCurrent" title="<?php echo wfMsg('Dialog.Common.action-current');?>">
								<img src="/skins/common/images/nav-current-page.png" />
								<span><?php echo wfMsg('Dialog.Link.button-my-page'); ?></span>
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

						<div id="imageViewer">
							<table width="100%" height="100%"><tr><td id="imageView"></td></tr></table>
						</div>
					</div>
				</div>
			</div>

			<div class="linkRow">
				<div class="linkLabel">
					<?php echo wfMsg('Dialog.Image.image-size'); ?>:
				</div>
				<div class="linkField">
					<input type="radio" name="image-size" id="image-size-small" />
					<label for="image-size-small"><?php echo wfMsg('Dialog.Image.image-size-small'); ?></label>

					<input type="radio" name="image-size" id="image-size-medium" />
					<label for="image-size-medium"><?php echo wfMsg('Dialog.Image.image-size-medium'); ?></label>

					<input type="radio" name="image-size" id="image-size-large" />
					<label for="image-size-large"><?php echo wfMsg('Dialog.Image.image-size-large'); ?></label>

					<input type="radio" name="image-size" id="image-size-original" selected />
					<label for="image-size-original"><?php echo wfMsg('Dialog.Image.image-size-original'); ?></label>

					<div class="customSize">
						<input type="radio" name="image-size" id="image-size-custom" />
						<label for="image-size-custom"><?php echo wfMsg('Dialog.Image.image-size-custom'); ?></label>
	
						<div id="customFields" style="display: inline;">
							<label for="image-size-width"><?php echo wfMsg('Dialog.Image.width'); ?>:</label>
							<input type="text" size="4" id="image-size-width" title="<?php echo wfMsg('Dialog.Image.width'); ?>"/>px
							<label for="image-size-height"><?php echo wfMsg('Dialog.Image.height'); ?>:</label>
							<input type="text" size="4" id="image-size-height" title="<?php echo wfMsg('Dialog.Image.height'); ?>" />px
						</div>
					</div>
				</div>
			</div>


			<div class="linkRow">
				<div class="linkLabel">
					<?php echo wfMsg('Dialog.Image.alignment'); ?>:
				</div>
				<div class="linkField">
					<input type="radio" name="image-wrap" id="image-wrap-default" checked />
					<label for="image-wrap-default"><img src="/skins/common/icons/image-wrap-default.png" alt="<?php echo wfMsg('Dialog.Image.alignment-default'); ?>" /></label>
					
					<input type="radio" name="image-wrap" id="image-wrap-left" />
					<label for="image-wrap-left"><img src="/skins/common/icons/image-wrap-left.png" alt="<?php echo wfMsg('Dialog.Image.alignment-left'); ?>" /></label>

					<input type="radio" name="image-wrap" id="image-wrap-right" />
					<label for="image-wrap-right"><img src="/skins/common/icons/image-wrap-right.png" alt="<?php echo wfMsg('Dialog.Image.alignment-right'); ?>" /></label>
				</div>
				
				<div class="clearfix"></div>
			</div>

		</div>
	</body>
</html>
