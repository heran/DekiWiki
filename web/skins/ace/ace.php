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

/**
 * Ace
 *
 * @todo document
 * @package MediaWiki
 * @subpackage Skins
 */

if( !defined( 'MINDTOUCH_DEKI' ) )
    die();

/** */
require_once('includes/SkinTemplate.php');

/**
 * Inherit main code from SkinTemplate, set the CSS and template filter.
 * @todo document
 * @package MediaWiki
 * @subpackage Skins
 */
class SkinAce extends SkinTemplate {
	/**
	 * @type const - Defines the number of custom HTML areas available
	 */
	const HTML_AREAS = 2;

    /** Using ace. */
    function initPage( &$out ) {
        SkinTemplate::initPage( $out );
        $this->skinname  = 'ace';
        $this->stylename = 'ace';
        $this->template  = 'AceTemplate';
    }
    
	/**
	 * (non-PHPdoc) customize display for dashboard header
	 * @see includes/Skin#dashboardHeader($PageInfo)
	 */
	function renderUserDashboardHeader($Article, $Title)
	{
		global $wgUser;
			
		$html = '';
		
		if (defined('PAGE_ALERTS')) {
			$enablePageAlerts = !$wgUser->isAnonymous() && $wgUser->canSubscribe();
			$html .= DekiPageAlertsPlugin::getPageAlertsButton($Article, $enablePageAlerts);
		}
		
		return $html;
	}
	
}
    
class AceTemplate extends QuickTemplate {

	function setupDashboard()
	{
		// move page alerts into header
		$this->set('page.alerts', '');
	}
	
    /**
     * Template filter callback for Ace skin.
     * Takes an associative array of data set from a SkinTemplate-based
     * class, and a wrapper for MediaWiki's localization database, and
     * outputs a formatted page.
     *
     * @access private
     */
    function execute() {
        global $wgUser, $wgTitle, $wgRequest, $wgArticle, $editor, $wgOut;
        $sk = $wgUser->getSkin();                
        $isArticle = $editor || $wgArticle->getID() > 0 || $wgArticle->mTitle->isEmptyNamespace();
        
        // allow variable overrides
        DekiPlugin::executeHook(Hooks::SKIN_OVERRIDE_VARIABLES, array(&$this));

echo('<?xml version="1.0" encoding="UTF-8"?>');
?>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" 
	"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
 <html xmlns="http://www.w3.org/1999/xhtml" xml:lang="<?php $this->text('lang') ?>" lang="<?php $this->text('lang') ?>" dir="<?php $this->text('dir')?>">
 <head>
    <script type="text/javascript">var _starttime = new Date().getTime();</script>
    <meta http-equiv="Content-Type" content="<?php $this->text('mimetype') ?>; charset=<?php $this->text('charset') ?>" />
    <?php $this->html('headlinks') ?>
    <title><?php $this->text('pagetitle'); ?></title>
    
    <!-- default css -->
    <?php $this->html('screencss'); ?>
    <?php $this->html('printcss'); ?>
        
    <!-- specific screen stylesheets-->
    <?php if (!Skin::isPrintPage()) { ?>
    <link rel="stylesheet" type="text/css" media="screen" href="<?php echo(Skin::getSkinPath().'/css.php');?>"/>
    <?php } else { ?>
    <link rel="stylesheet" type="text/css" media="screen" href="<?php echo(Skin::getTemplatePath().'/_print.css');?>" />    
    <link rel="stylesheet" type="text/css" media="screen" href="<?php echo(Skin::getSkinPath().'/_content.css');?>" />
    <link rel="stylesheet" type="text/css" media="screen" href="<?php $this->html('pathcommon'); ?>/prince.content.css" />
    <?php } ?>
    
    <!-- specific print stylesheets -->
    <link rel="stylesheet" type="text/css" media="print" href="<?php echo(Skin::getTemplatePath().'/_print.css');?>" />
    <link rel="stylesheet" type="text/css" media="print" href="<?php echo(Skin::getSkinPath().'/_content.css');?>" />
   
    <!-- IE6 & IE7 specific stuff -->
    <!--[if IE]><meta http-equiv="imagetoolbar" content="no" /><![endif]-->
    <?php if (!Skin::isPrintPage()) { ?>
    <!--[if IE 7]>
        <style type="text/css">@import "<?php echo(Skin::getSkinPath()); ?>/_ie7.css";</style>
    <![endif]-->
    <!--[if IE 6]>
        <style type="text/css">@import "<?php echo(Skin::getSkinPath()); ?>/_ie.css";</style>
    <![endif]-->
    
    <!-- default scripting -->
    
    <?php echo($this->printInternetExplorerPngImages()); ?>
    <?php } ?>
    
    <?php $this->html('javascript'); ?>
    
    <script type="text/javascript">
    <?php
	    //custom IE6 fixes for this skin
	    if (!isset($_SESSION['firstLoad'])) {
	        echo("\t".'var _cur_TZ = \''. $wgUser->getTimezone() .'\';'."\n");
	        $_preload = array();
	        
	        $_skinImages = array('mt-top-s.png', 'mt-body-s.png', 'mt-bottom-s.png', 'mt-top-m.png', 'mt-body-m.png', 'mt-bottom-m.png', 
	            'tt-top-l.png', 'tt-top-n.png', 'tt-top-r.png', 'tt-body.png', 'tt-bottom.png');
	        foreach ($_skinImages as $image) {
	            $_preload[] = Skin::getSkinPath().'/'.$image;
	        }
	        
	        foreach ($_preload as $key => $image) { ?>
	            var image<?php echo($key); ?> = new Image();
	            image<?php echo($key); ?>.src = '<?php echo($image);?>';
	        <?php } 
	        $_SESSION['firstLoad'] = true;
	    }
	   ?>
    </script>
    
    <?php $this->html('inlinejavascript'); ?>
	
	<!-- styles overwritten via control panel - load this css last -->
	<?php $this->html('customhead'); ?> 
	
</head>

<body class="<?php $this->html('pagetype'); ?>">
<?php $this->html('pageheader'); ?>

<div class="wrap global <?php $this->html('pagename'); ?>" id="wrap"> 
	<div class="custom"><?php $this->html('customarea1'); ?></div>
 	<div class="w_top_logo">
 		<table cellspacing="0" cellpadding="0" border="0">
 			<tr>
 				<td valign="top" style="width:100%;"><div class="customer-logo"><?php $this->html('logo'); ?></div></td>
 				<td valign="bottom"><div class="loggedin"><img src="<?php echo(Skin::getSkinPath());?>/icon-sharkfin.gif" alt="" />
		 			<span class="loggedintext">
			 		<?php if (!$wgUser->isAnonymous()) { ?>
		 			<span class="loggedinwho"><?php echo(wfMsg('Skin.Common.logged-in'));?>&nbsp;<?php
		 				echo('<a href="'.$this->haveData('userpageurl').'">');
						$this->text('username');
						echo('</a></span>&nbsp;&nbsp;');
		 				echo('<a href="'.$this->haveData('logouturl').'">'.wfMsg('Page.UserLogout.page-title').'</a>');
		 			} else { ?> 
		 			<span class="loggedinwho"><?php echo(wfMsg('Skin.Common.you-not-logged-in'));?></span>&nbsp;&nbsp; <?php
			 			echo('<a href="'.$this->haveData('loginurl').'">'.wfMsg('Page.UserLogin.page-title').'</a>');
			 			if ($this->hasData('registerurl')) {
				 			echo(' | <a href="'.$this->haveData('registerurl').'">'.wfMsg('Skin.Ace.login-register').'</a>');
			 			}
					}?>
					</span></div>
				</td>
			</tr>
		</table>
	</div>
 	<div class="w_top">
		<?php echo($this->printSiteBar()); ?>
	</div>
	
 	<div class="w_body">
	 	<table width="100%" cellspacing="0" cellpadding="0" class="columnTable">
	 		<tr>
	 			<td class="left" valign="top">
					<div class="w_left">
						<div class="content">
							<div class="breadcrumbs"><h5><?php echo(wfMsg('Skin.Ace.recent-pages'));?></h5><div id="breadcrumb"><?php echo(showBreadcrumbTrail()); ?></div></div>
					 		<fieldset class="search">
						 		<form action="<?php $this->text('searchaction') ?>" id="searchform">
							         <input id="searchInput" class="searchText" tabindex="1" name="search" type="text" accesskey="<?php echo wfMsg('Skin.Common.search-access-key') ?>"<?php 
							        if( isset( $this->data['search'] ) ) {
							          ?> value="<?php $this->text('search') ?>"<?php } ?> />
										<?php $this->html('search.namespaces'); ?>
							          <input type="submit" name="go" class="searchButton" id="searchGoButton" value="<?php echo wfMsg('Skin.Common.submit-find') ?>" />
								</form>
							</fieldset>
						  <?php echo($this->haveData('sitenavtext')); ?>
						</div>
					</div>
	 				<div><img src="<?php echo(Skin::getCommonPath());?>/icons/icon-trans.gif" width="185" height="1" alt=""/></div>
				</td>
				<td valign="top" class="right">
			
		<div class="w_content" id="content">
			<div class="pagebar">
				<div class="pagebar_options">
					<?php echo($this->printPageBar()); ?>
					<?php echo($this->printMoreMenu()); ?>
				</div>
				
				<div class="pagebar_items">
					<div class="pagebar_items_left"></div>
					<div class="pagebar_items_body">
						<div class="modified"><?php echo($this->haveData('pagemodified')); ?></div>
						<div class="pagebar_items_2">		
							<?php echo($this->printAttachIcon($this->haveData('filecount')));?>
							<?php echo($this->printTocIcon()); ?>
							<?php echo($this->printLinksHereIcon()); ?>						
							<?php echo($this->printRestrictIcon()); ?>
							<?php echo($this->printWatchIcon());?>
							<?php echo($this->printRedirectIcon()); ?>
						</div>
					</div>
				</div>
			</div>
		</div>
		
 		<div id="pageContent">
 			<?php $this->html('pagesubnav'); ?>
 			<div id="topic">
				<?php if ($this->hasData('page.alerts')) : ?>
					<div class="hideforedit">
						<?php $this->html('page.alerts'); ?>
					</div>
				<?php endif; ?>
				
				<?php if ($this->hasData('page.rating.header')) : ?>
					<div class="hideforedit">
						<?php $this->html('page.rating.header'); ?>
					</div>
				<?php endif; ?>
				 			
	 			<?php if ($this->hasData('hierarchy') && $wgTitle->isEditable() && (Skin::isViewPage() || Skin::isEditPage())) { ?>
	 				<div class="hierarchy">
	 					<?php $this->html('hierarchy'); ?>
	 				</div>
	 			<?php } ?>
	 			
		 		<?php if ($this->haveData('page.title')) : ?>
				<div class="t-title">
					<h1 id="title">
						<?php $this->html('page.title'); ?>
					</h1>
				</div>
				<?php endif; ?>
	 		
		 		<div class="t-body" id="topic-body">
		 				 			
		 			<div><a name="a-title"></a></div>		 		
				    <h3 id="siteSub"><?php echo wfMsg('Skin.Common.tag-line'); ?></h3>
		 			<?php echo(wfMessagePrint()); ?>
		 			<?php if (!$isArticle) { ?>
		 				<div class="b-body">
		 			<?php } ?>
	 				
		 			
		 			<div class="b-body">
		 			
		 				<?php $this->html('bodytext'); ?>
						<?php if ($wgTitle->isEditable() && (Skin::isViewPage() || Skin::isEditPage())) { ?>
						
						<?php if ($this->hasData('page.rating')) { ?>
						<div class="pageRating">
							<strong><?php echo(wfMsg('Page.ContentRating.skin.page.rating'));?></strong>
							<?php $this->html('page.rating'); ?>
						</div>
						<?php } ?>
						
			 			<div class="pageTags">
				 			<strong><?php echo(wfMsg('Skin.Ace.tags-list'));?></strong> <?php $this->html('tagsedit'); ?>
			 				<?php $this->html('tagstext'); ?>
			 			</div>
			 			
			 			<?php if ($this->hasData('related')) { ?>
						<div class="pageRelated">
							<strong><?php echo(wfMsg('Skin.Common.related-pages'));?></strong>
							<?php $this->html('related'); ?>
						</div>
						<?php } ?>
						
	 					<?php } ?>
	 					<?php 
	 					if ($this->hasData('languages') && $wgTitle->isEditable()) { 
		 					echo('<div class="pageInfo languages"><strong>'.wfMsg('Article.Common.languages').'</strong>');
		 					$this->html('languages');
		 					echo('</div>');
		 				} 
		 				?>
		 			</div>
		 			
		 			<?php if ((Skin::isViewPage() || Skin::isEditPage()) && $wgTitle->isEditable()) { ?>
		 			<div class="b-attachments" id="attachments">		 			
		 				<?php
			            echo('<div class="filesheader"><div class="filesformlink">'
			 					.$this->haveData('fileaddlink')
			                	.'</div><div class="filesheaderbg">'
			                	.'<div class="filesheaderright">'
			            		.'<div class="filesheadertext">'.Skin::iconify('file').' <span class="text">'.wfMsg('Skin.Common.header-files-count', $this->haveData('filedisplaycount')).'</span></div>'
			                	.'</div></div></div>');
			            ?>
		 				<?php $this->html('filestext');	?>

						<?php if ($this->haveData('gallerytext')) : ?>
						<div class="gallery">
			 				<?php $this->html('gallerytext'); ?>
			 			</div>
						<?php endif; ?>

		 				<?php
		 				echo('<a name="attachImages"></a>'
			                	.'<div class="filesheader">'
			                	.'<div class="filesheaderbg">'
			                	.'<div class="filesheaderright">'
			                	.'<div class="filesheadertext">'.Skin::iconify('comments').' <span class="text">'.wfMsg('Skin.Ace.header-comments-count', '('.$this->haveData('commentcount').')').'</span></div>'
			                	.'</div></div></div>');
		 				?>	
		 				<?php $this->html('comments'); ?>
		 			</div>
		 			
		 			<?php } ?>
		 			
		 			<?php if (!$isArticle) { ?>
		 				</div>
		 			<?php } ?>
		 					</div>
				 		</div>
					</div>
				</div>
				</td>
			</tr>
			<tr class="bottom">
				<td valign="top" class="left"></td>
				<td valign="top" class="right"><br/><div class="w_bot_fileshadow">&nbsp;</div>
				<?php $this->html('poweredbytext'); ?></td>
			</tr>
		</table>
	</div>	
</div>

<div class="w_wbot">

    <div id="popupMessage"></div> <?php // for inline messages from nav pane ?>
    
    <script type="text/javascript">var _endtime = new Date().getTime(); var _size = <?php echo(ob_get_length())?>;</script>
</div>

<div onclick="menuBubble=true;" id="menuInfo" class="dmenu" style="display:none;">
	<div class="dmenu-top"></div>
	<div class="dmenu-body">
		<?php echo($this->printSiteToolBar()); ?>
	</div>
	<div class="dmenu-bottom"></div>
</div>

<div onclick="menuBubble=true;" class="dmenu" id="pageMenuContent" style="display:none;">
	<div class="dmenu-top"></div>
	<div class="dmenu-body">			
		<?php echo($this->printMoreMenuItems());?>
	</div>
	<div class="dmenu-bottom"></div>
</div>

<div onclick="menuBubble=true;" class="dmenu" id="menuBacklink" style="display:none;">
	<div class="dmenu-top_m"></div>
	<div class="dmenu-body_m">
		<?php $this->html('pagebacklinks'); ?>
	</div>
	<div class="dmenu-bottom_m"></div>
</div>

<div onclick="menuBubble=true;" class="dmenu" id="menuPageContent" style="display:none;">
		<div class="dmenu-top_m"></div>
		<div class="dmenu-body_m" id="menuToc">
			<?php $this->html('toc'); ?>
		</div>
		<div class="dmenu-bottom_m"></div>
</div>

<?php $this->html('pagefooter'); ?>
<?php $this->html('customarea2'); ?>
<?php $this->html('reporttime'); ?>
</body>
<?php $this->html('customtail'); ?>  
</html><?php
    }
    
    
function printSiteBar() {
	global $wgUser, $wgHelpUrl;
	
	$_baseAttr = array('class' => 'item');
	
	$sk = new Skin;
	if (!$wgUser->isAnonymous()) {
		$html = $this->formatSiteBarButton(
			wfMsg('Skin.Common.header-my-page'), 
			'#', 
			array_merge($_baseAttr, array('href' => $this->haveData('userpageurl')))
		);
	}
	else {
		$lt = Title::makeTitle( NS_SPECIAL, 'Userlogin' );
		$html = $this->formatSiteBarButton(
			wfMsg('Skin.Common.header-my-page'), 
			'#', 
			array_merge($_baseAttr, array('href' => $lt->getLocalURL( 'returntomypage=y' ))));
	}
		
	$html .= $this->formatSiteBarButton(
		ucwords(wfMsg('Page.RecentChanges.page-title')), 
		Title::makeTitle( NS_SPECIAL, 'Recentchanges' ), 
		$_baseAttr);
	
	$html .= $this->formatSiteBarButton(
		ucwords(wfMsg('Skin.Common.reports')), 
		Title::makeTitle( NS_SPECIAL, 'Reports' ), 
		$_baseAttr);
	
	$html .= $this->formatSiteBarButton(
		wfMsg('Skin.Common.header-tools'), 
		'#', 
		array('onclick' => 'return menuPosition(\'menuInfo\', this, 0, 5);', 'class' => 'menuArrow'));
	
	$html .= $this->formatSiteBarButton(
		wfMsg('Skin.Common.header-help'), 
		'#',
		array_merge($_baseAttr, array('href' => $wgHelpUrl)));
		
	global $wgHostedVersion, $wgAccountLevel;
	if ($wgHostedVersion == true && $wgAccountLevel != 'pro') {
		$html .= $this->formatSiteBarButton(wfMsg('Skin.Common.gopro'), '#', array('href' => 'http://wik.is/pro/', 'class' => 'item'));
	}

	return '<div class="w_top"><ul class="options">'.$html.'</ul></div>';
}

function formatSiteBarButton($label, $link = '#', $_attr = array()) {
	return "\t\t".'<li>'.Skin::makeNakedLink($link, htmlspecialchars($label), $_attr).'</li>';
}

function printPageBar() {
	global $wgTitle, $wgRequest, $wgArticle, $wgUser;
	$canEdit = $wgArticle->userCanEdit();
	$canView = $wgArticle->userCanRead();
	$canCreate = $wgArticle->userCanCreate();
	$oldid = $wgRequest->getVal( 'oldid' );
	$isRestricted = $wgArticle->isRestricted();
	$diff = $wgRequest->getVal( 'diff' );
	$oid = ( $oldid && ! isset( $diff ) ) ? '&oldid='.IntVal( $oldid ) : false;
	$sk = $wgUser->getSkin();
	
	$html = $this->formatPageBarButton(
		wfMsg('Skin.Ace.button-edit-page'), 
		$this->haveHref('pageedit'),
		array(
			'class' => $canEdit ? '': 'disabled',
			'style' => '27px', 
			'onclick' => $canEdit 
					? $this->haveOnClick('pageedit')
					: 'return false', 
			'title' => wfMsg('Skin.Ace.button-edit-page-title')), 
		'edit');
		
	$html.= $this->formatPageBarButton(
		wfMsg('Skin.Ace.button-new-page'), 
		$this->haveHref('pageadd'),
		array(
			'class' => $canCreate && !$sk->isNewPage() ? '': 'disabled',
			'style' => '52px', 
			'onclick' => $canCreate ? $this->haveOnClick('pageadd') : '',
			'title' => wfMsg('Skin.Ace.button-new-page-title')), 
		'addSubpage');	
		
	$html.= $this->formatPageBarButton(
		wfMsg('Skin.Ace.button-print-page'), 
		$this->haveHref('pageprint'),
		array(
			'class' => $canView ? '': 'disabled',
			'style' => '27px', 
			'onclick' => 
				$canView
					? "menuOff('pageMenuContent');".$this->haveOnClick('pageprint')
					: 'return false', 
			'title' => wfMsg('Skin.Ace.button-print-page-title')), 
		'print');
		
	return $html;
}

function formatPageBarButton($label, $link ='#', $_attr = array(), $key = '') {
	$_attr['href'] = !isset($_attr['href']) ? $link: $_attr['href'];
	$text = Skin::iconify($key.($_attr['class'] != '' ? '-'.$_attr['class']: ''))
		.'<br/><span class="text">'.$label.'</span>';
		return '<div class="pbar_options">'
			.Skin::makeNakedLink('#', $text, $_attr)							
		.'</div>';
}

function printMoreMenu() {
	global $wgTitle;
	if ($wgTitle->isEditable()) {
		$html = '<div id="pageMenu">'.Skin::makeNakedLink('#', '<span class="downarrow">'.wfMsg('Skin.Common.more').'</span>', array('class' => 'pbar_link', 'onclick' => 'return menuPosition(\'pageMenuContent\', this, 0, -23);')).'</div>';
	}
	else {
		$html = '<div id="pageMenu" class="disabledMore">'.Skin::makeNakedLink('#', '<span class="text">'.wfMsg('Skin.Common.more').'</span>'.Skin::iconify('menuarrow-disabled'), array('title' => wfmsg('Skin.Ace.no-more-options'), 'onclick' => 'return false')).'</div>';
	}
	return $html;
}

function printAttachIcon($filecount = 0) {
	global $wgTitle;
	
	if ($wgTitle->isEditable()) {
		$html = Skin::makeNakedLink('#', Skin::iconify('folder').'<span class="text" id="pageFilesCount">'.$filecount.'</span>', array('href' => '#attachForm', 'title' => wfMsg('Skin.Ace.attached-files-count', $filecount)));
	}
	else {
		$html = Skin::makeNakedLink('#', Skin::iconify('folder-disabled').'<span class="text">0</span>', array('class' => 'disabled', 'href' => '#attachForm', 'title' => wfMsg('Skin.Ace.attached-files-count', 0)));
	}
						
	return '<div class="pbar_soptions">'.$html.'</div>';
}

function printTocIcon() {
	global $wgTitle;
	
	if ($wgTitle->isEditable() && Skin::getPageAction() == 'view') {
		$text = Skin::iconify('toc').Skin::iconify('menuarrow');
		$_link = array('href' => '#', 'onclick' => $this->haveOnClick('pagetoc'), 
			'title' => wfMsg('Skin.Common.table-of-contents'));
		$html = Skin::makeNakedLink('#', $text, $_link);
	} else { 
		$text = Skin::iconify('toc-disabled').Skin::iconify('menuarrow-disabled');
		$_link = array('href' => '#', 'onclick' => 'return false', 'class' => 'disabled', 'title' => 'There is no table of contents for this page');
		$html = Skin::makeNakedLink('#', $text, $_link);
	} 
	return '<div class="pbar_soptions">'.$html.'</div>';
}

function printLinksHereIcon() {
	global $wgOut;
	$count = count($wgOut->getBacklinks());
	if ($count > 0 && Skin::getPageAction() == 'view') { 
		$text = Skin::iconify('referring').Skin::iconify('menuarrow');
		$_link = array('href' => '#', 'onclick' => 'return menuPosition(\'menuBacklink\', this, -2, 0, true);', 'title' => wfMsg('Skin.Ace.referring', $count));
		$html = Skin::makeNakedLink('#', $text, $_link);
	} else { 		
		$text = Skin::iconify('referring-disabled').Skin::iconify('menuarrow-disabled');
		$_link = array('href' => '#', 'onclick' => 'return false', 'class' => 'disabled');
		$html = Skin::makeNakedLink('#', $text, $_link);
	}
	return '<div class="pbar_soptions">'.$html.'</div>';
}

function printRestrictIcon() { 
	global $wgUser, $wgArticle;
	if ($wgArticle->isRestricted()) { 
		$text = Skin::iconify('restrict');
		$SpecialPage = Title::newFromText(Hooks::SPECIAL_PAGE_RESTRICT);
		$_link = array('href' => $SpecialPage->getLocalUrl('id='.$wgArticle->getId()), 'title' => wfMsg('Article.Common.page-is-restricted'));
		return '<div class="pbar_soptions">'.Skin::makeNakedLink('#', $text, $_link).'</div>';
	}	
}

function printWatchIcon(){
	global $wgTitle;
	if ($wgTitle->userIsWatching()) { 
		$text = Skin::iconify('watch');
		$_link = array('title' => wfMsg('Skin.Ace.watching-this-page'));
		return '<div class="pbar_soptions">'.Skin::makeNakedLink(Title::makeTitle( NS_SPECIAL, 'Watchedpages' ), $text, $_link).'</div>';
	} 
}

function printRedirectIcon() {		
	global $wgOut;
	if ($wgOut->getRedirectMessage() != '') {
		$text = Skin::iconify('alert').'<span class="text-redirect">'.wfMsg('Skin.Ace.redirect').'</span>';
		$_link = array('href' => '#', 'href' => $wgOut->getRedirectLocation(), 'class' => 'redirect', 'title' => $wgOut->getRedirectMessage());
		return '<div class="pbar_soptions">'.Skin::makeNakedLink('#', $text, $_link).'</div>';
	} 		
}

function printInternetExplorerPngImages() {
	$html = '<!--[if IE 6]><style type="text/css">';
    /* IE PNG-24 transparency rendering
     * Internet explorer needs help with rendering PNG-24 transparency
     * IE also does not recognize relative paths to CSS files if you put these declarations in the CSS, 
     * it will not render. We need to declare a relative path to the PHP file
    */
    $_pngCrop = array(
        '.ttshadow div.tt_bottom' => 'tt-bottom.png', 
        '.dmenu div.dmenu-top_m' => 'mt-top-m.png', 
        '.dmenu div.dmenu-bottom_m' => 'mt-bottom-m.png',
        '.dmenu div.dmenu-top' => 'mt-top-s.png', 
        '.inlinedialogue div.id-botl' => 'il-bl.png', 
        '.inlinedialogue div.id-botr' => 'il-br.png',
        '.dmenu div.dmenu-bottom' => 'mt-bottom-s.png');
    $_pngScale = array(
        '.ttshadow div.tt_content' => 'tt-body.png', 
        '.dmenu div.dmenu-body_m' => 'mt-body-m.png',
        '.dmenu div.dmenu-body' => 'mt-body-s.png',
        '.inlinedialogue td.id-bottom' => 'il-bbody.png', 
        '.ttshadow div.tt_top' => 'tt-top-n.png'            
    );
    foreach ($_pngScale as $class => $file) {
        $html .= '* html '.$class.' { background: none; filter:progid:DXImageTransform.Microsoft.AlphaImageLoader(src=\''.Skin::getSkinPath().'/'.$file.'\',sizingMethod=\'scale\'); }';
    }  
    foreach ($_pngCrop as $class => $file) {
        $html .= '* html '.$class.' {  background: none; filter:progid:DXImageTransform.Microsoft.AlphaImageLoader(src=\''.Skin::getSkinPath().'/'.$file.'\',sizingMethod=\'crop\');  }';
    }       
    $html .= '</style><![endif]-->';
    return $html;
}

function printMoreMenuItems() {
	global $wgTitle, $wgUser, $wgArticle;
	
	$canEdit = $wgArticle->userCanEdit();
	
	$html = '';
	if ($wgTitle->isEditable()) {
		$isUserAndArticle = !$wgUser->isAnonymous() && $wgArticle->getId() > 0;
		if (!$wgUser->isWatched($wgTitle)) {
			$html .= $this->formatMenuItem(wfMsg('Skin.Ace.watch'), 
				$isUserAndArticle > 0 
					? $wgTitle->getLocalUrl( 'action=watch' )
					: '#', 
				array('class' => $isUserAndArticle ? '' : 'disabled', 'onclick' => 'menuOff(\'pageMenuContent\');'), 'watch');
		}
		else {
			$html .= $this->formatMenuItem(wfMsg('Skin.Ace.unwatch'), 
				$isUserAndArticle 
					? $wgTitle->getLocalUrl( 'action=unwatch' )
					: '#', 
				array('class' => $isUserAndArticle ? '' : 'disabled', 'onclick' => 'menuOff(\'pageMenuContent\');'), 
				'watch');
		}
	}
	else {
		$html .= $this->formatMenuItem(wfMsg('Skin.Ace.watch'), '#', array('class' => 'disabled', 'onclick' => 'menuOff(\'pageMenuContent\');'), 'watch');
	}
	
	$html .= $this->formatMenuItem(
		wfMsg('Skin.Common.attach-file-image'), 
		'#', 
		array(
			'href' => '#attachForm',
			'onclick' => 'menuOff(\'pageMenuContent\');'.$this->haveOnClick('pageattach'),
			'class' => $this->haveCssClass('pageattach')), 
		'attach');
	
	$html .= $this->formatMenuItem(
		wfMsg('Skin.Common.page-pdf'), 
		$this->haveHref('pagepdf'), 
		array(
			'class' => $this->haveCssClass('pagepdf'), 
		), 
		'pdf');
	
	$html .= $this->formatMenuItem(
		wfMsg('Skin.Common.restrict-access'), 
		$this->haveHref('pagerestrict'), 
		array(
			'class' => $this->haveCssClass('pagerestrict'), 
    		'onclick' => 'menuOff(\'pageMenuContent\');'.$this->haveOnClick('pagerestrict')
    	), 
		'restrict');
	
	$html .= $this->formatMenuItem(
			wfMsg('Skin.Ace.move'), 
			'#', 
			array(
				'class' => $this->haveCssClass('pagemove'),
				'onclick' => 'menuOff(\'pageMenuContent\');'.$this->haveOnClick('pagemove')
			), 
			'move');
	
	$html .= $this->formatMenuItem(
		wfMsg('Skin.Ace.delete'), 
		'#', 
		array(
			'class' => $this->haveCssClass('pagedelete'),
			'onclick' => 'menuOff(\'pageMenuContent\');'.$this->haveOnClick('pagedelete')
		), 
		'delete');	
			
	//tagging
	$html .= $this->formatMenuItem(
		wfMsg('Skin.Common.tags'), 
		'#', 
		array(
			'class' => $this->haveCssClass('pagetags'),
			'onclick' => 'menuOff(\'pageMenuContent\');'.$this->haveOnClick('pagetags'),
		), 
		'tag');	
				
	//email
	$html .= $this->formatMenuItem(
		wfMsg('Skin.Common.email-page'), 
		'#', 
		array(
			'class' => $this->haveCssClass('pageemail'),
			'onclick' => 'menuOff(\'pageMenuContent\');'.$this->haveOnClick('pageemail'),
			'href' => $this->haveHref('pageemail')
		), 
		'pageemail');	
	
	$html.= $this->formatMenuItem(
		wfMsg('Skin.Common.page-properties'), 
		$this->haveHref('pageproperties'), 
		array(
			'class' => $this->haveCssClass('pageproperties'), 
			'onclick' => 'menuOff(\'pageMenuContent\');'.$this->haveOnClick('pageproperties')
		), 
		'pageproperties'
	);
	$html.= $this->formatMenuItem(
		wfMsg('Skin.Common.page-talk'), 
		$this->haveHref('pagetalk'), 
		array(
			'class' => $this->haveCssClass('pagetalk'), 
			'onclick' => 'menuOff(\'pageMenuContent\');'.$this->haveOnClick('pagetalk')
		), 
		'pagetalk'
	);
	return '<ul>'.$html.'</ul>';
}
function formatMenuItem($label, $link ='#', $_attr = array(), $key = '') {
	$_attr['href'] = !isset($_attr['href']) ? $link: $_attr['href'];
	$label = Skin::iconify($key.(isset($_attr['class']) && $_attr['class'] != '' ? '-'.$_attr['class']: ''))
   		.'<span class="text">'.htmlspecialchars($label).'</span>';
	return '<li>'.Skin::makeNakedLink('#', $label, $_attr).'</li>';
}

function formatSpecialMenuItem($label, $key ='', $_attr = array()) {
	$t = Title::makeTitle( NS_SPECIAL, $key);
	$_attr['href'] = $t->getLocalUrl();
	$key = strtolower($key);
	$label = Skin::iconify($key.(isset($_attr['class']) && $_attr['class'] != '' ? '-'.$_attr['class']: ''))
   		.'<span class="text">'.htmlspecialchars($label).'</span>';
	return '<li>'.Skin::makeNakedLink('#', $label, $_attr).'</li>';
}

function printSiteToolBar() {
	global $wgUser;
	$html = '';
	if (!$wgUser->isAnonymous()) {
		$html.= $this->formatSpecialMenuItem(
			wfMsg('Page.WatchedPages.page-title'), 
			'Watchedpages', 
			array());
			
		$t = Title::makeTitle( NS_SPECIAL, 'Contributions' );
		$html.= $this->formatMenuItem(
			wfMsg('Page.Contributions.page-title'), 
			'#', 
			array('href' => $t->getLocalURL('target=' . urlencode( $wgUser->getUsername()))), 
			'mycontris');
			
		$html.= $this->formatSpecialMenuItem(
			wfMsg('Page.UserPreferences.page-title'), 
			'Preferences', 
			array());
		$html.= '<li class="separator"></li>';
	}
	$sk = $wgUser->getSkin();	
	if ($wgUser->canViewControlPanel()) {
		$t = Title::makeTitle(NS_ADMIN, '');
		$html .= $this->formatMenuItem(
			wfMsg('Admin.ControlPanel.page-title'), 
			$t->getLocalUrl(),
			array(), 
			'controlpanellink');
	}	
	$html.= $this->formatSpecialMenuItem(
		wfMsg('Page.ListRss.page-title'), 
		'ListRss', 
		array());
	
	$html .= $this->formatMenuItem(
		wfMsg('Page.ListUsers.page-title'), 
		'#', 
		array('href' => $sk->makeNSUrl('', '', NS_USER)), 
		'listusers');
	    
	$html .= $this->formatMenuItem(
		wfMsg('Page.ListTemplates.page-title'), 
		'#', 
		array('href' => $sk->makeNSUrl('', '', NS_TEMPLATE),), 
		'templatesroot');
		
	$html.= '<li class="separator"></li>';
	
	$html.= $this->formatSpecialMenuItem(
		wfMsg('Page.SiteMap.page-title'), 
		'Sitemap', 
		array());
			
	$html.= $this->formatSpecialMenuItem(
		wfMsg('Page.PopularPages.page-title'), 
		'Popularpages', 
		array());
	
	$html .=  sprintf('<li class="%s"><a href="%s" target="_blank" title="%s">%s<span class="text">%s</span></a></li>', 
		'deki-desktop-suite', 
		ProductURL::DESKTOP_SUITE, 
		wfMsg('Skin.Common.desktop-suite'), 
		Skin::iconify('deki-desktop-suite'), 
		wfMsg('Skin.Common.desktop-suite')
	);
	
	return '<ul>'.$html.'</ul>';
}
}
?>