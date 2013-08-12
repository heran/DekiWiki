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
 * Base Template
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
class SkinFiesta extends SkinTemplate {
	/**
	 * @type const - Defines the number of custom HTML areas available
	 */
	const HTML_AREAS = 8;

    /** Using BasePlus. */
    function initPage( &$out ) {
        SkinTemplate::initPage( $out );
        $this->skinname  = 'fiesta';
        $this->stylename = 'fiesta';
        $this->template  = 'FiestaTemplate';
        $this->customareas = 6;
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
    
class FiestaTemplate extends QuickTemplate {
	
	function setupDashboard()
	{
		// move page alerts into dashboard header
		$this->set('page.alerts', '');
	}
		
    /**
     * Template filter callback for Base skin.
     * Takes an associative array of data set from a SkinTemplate-based
     * class, and a wrapper for MediaWiki's localization database, and
     * outputs a formatted page.
     *
     * @access private
     */
     	
    function execute() {
        global $wgLogo, $wgUser, $wgTitle, $wgRequest, $wgArticle, $wgOut, $editor, $wgScriptPath, $wgContLang, $wgMenus, $IP;
		global $wgFiestaCMSMode;
        $sk = $wgUser->getSkin();                
        $isArticle = $editor || $wgArticle->getID() > 0 || $wgArticle->mTitle->isEmptyNamespace();
        
        $this->cmsmode = false;
        
        if (isset($wgFiestaCMSMode)) 
        {
        	$this->cmsmode = isset($wgFiestaCMSMode) ? (bool) $wgFiestaCMSMode : false;  /*Toggle on/off Fiesta CMS mode*/
    	} 
    	else if (!is_null(wfGetConfig('ui/fiesta-cmsmode', null))) 
    	{
	        $this->cmsmode = wfGetConfig('ui/fiesta-cmsmode', null) === true;
        }
        
        // allow variable overrides
        DekiPlugin::executeHook(Hooks::SKIN_OVERRIDE_VARIABLES, array(&$this));

echo('<?xml version="1.0" encoding="UTF-8"?>');
?>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
 <html xmlns="http://www.w3.org/1999/xhtml" xml:lang="<?php $this->text('lang') ?>" lang="<?php $this->text('lang') ?>" dir="<?php $this->text('dir')?>">
 <head>
    <script type="text/javascript">var _starttime = new Date().getTime();</script>
    <meta http-equiv="Content-Type" content="<?php $this->text('mimetype') ?>; charset=<?php $this->text('charset') ?>" />
    <?php $this->html('headlinks') ?>
    <title><?php $this->text('pagetitle'); ?></title>
    
    <!-- default css -->
    <link rel="stylesheet" type="text/css" media="screen" href="<?php $this->html('pathtpl'); ?>/_reset.css"/>
    <?php $this->html('screencss'); ?>
    <?php $this->html('printcss'); ?>
    
    <!-- default scripting -->
    <?php $this->html('javascript'); ?>
    
    <!-- specific screen stylesheets-->
    <?php if (!Skin::isPrintPage()) { ?>
    <link rel="stylesheet" type="text/css" media="screen" href="<?php $this->html('pathskin'); ?>/css.php"/>
    <?php } else { ?>
    <link rel="stylesheet" type="text/css" media="screen" href="<?php $this->html('pathskin'); ?>/_content.css" />   
    <link rel="stylesheet" type="text/css" media="screen" href="<?php $this->html('pathtpl'); ?>/print.css" />    
    <link rel="stylesheet" type="text/css" media="screen" href="<?php $this->html('pathcommon'); ?>/prince.content.css" />
    <?php } ?>
    
    <!-- specific print stylesheets -->
    <link rel="stylesheet" type="text/css" media="print" href="<?php $this->html('pathtpl'); ?>/print.css" />
   
    <!-- IE6 & IE7 specific stuff -->
    <!--[if IE]><meta http-equiv="imagetoolbar" content="no" /><![endif]-->
    
    <?php $this->html('inlinejavascript'); ?>
	
	<!-- styles overwritten via control panel - load this css last -->	
	<?php $this->html('customhead'); ?>
</head>

<body<?php if (!$wgUser->isAnonymous()) { echo(' id="loggedin"'); } ?> class="<?php $this->html('pagetype'); ?>  <?php $this->html('language');?>">
<?php $this->html('pageheader'); ?>

<div class="global">
	<div class="custom custom7">
		<?php $this->html('customarea7'); ?>
	</div>
	<div class="globalWrap<?php if ($this->cmsmode==true && $wgUser->isAnonymous()){echo " globalWrapCMS";}?>">
		<?php if ($this->cmsmode == false || !$wgUser->isAnonymous()) {?>
		<div class="header">
		
			<div class="mastPre"></div>
			<div class="mast">
				
				<div class="siteLogo">
					<?php $this->html('logo'); ?>		
				</div>
			</div>
			<div class="mastPost"></div>
			
			<div class="siteNavPre"></div>
			<div class="siteNav">
				<div class="userAuthPre"></div>
					<?php $this->FiestaUserAuth(); ?>
					
					<?php if ($this->hasData('registerurl')) { ?>
					<div class="userRegister">
						<a href="<?php $this->html('registerurl'); ?>"><?php echo(wfMsg('Page.UserRegistration.page-title'));?></a>
					</div>
					<?php } ?>
				<div class="userAuthPost"></div>
				
				<div class="custom custom2">
					<?php $this->html('customarea2'); ?>
				</div>
				
					
				<div class="navPre"></div>
					<?php $this->html('sitenavtext'); ?>
				<div class="navPost"></div>
					
				<div class="custom">
					<?php $this->html('customarea3'); ?>
				</div>
			</div>
			<div class="siteNavPost"></div>
		</div>
		<?php } ?>
		<div class="body">
			<div class="bodyHeader">
				<div class="pre"></div>
				<div class="post"></div>
			</div>
			<div class="page">
				<div class="custom custom1">
				<?php $this->html('customarea1'); ?>
				</div>
				
				<?php if ($this->cmsmode==false || !$wgUser->isAnonymous()) {?>
				<div class="siteNav">
					<div class="pre"></div>
					<?php $this->FiestaSiteSearch(); ?>
					<ul>
						<li class="userPage"><a href="<?php $this->html('userpageurl')?>"><span></span><?php echo(wfMsg('Skin.Common.header-my-page')); ?></a></li>
						<?php if ($wgUser->canViewControlPanel()) { ?>
						<li class="siteControlPanel"><a href="<?php echo($sk->makeAdminUrl(''));?>"><span></span><?php echo(wfMsg('Admin.ControlPanel.page-title'));?></a></li>
						<?php } ?>
						<li class="siteChanges"><a href="<?php echo($sk->makeSpecialUrl('Recentchanges'));?>"><span></span><?php echo(wfMsg('Page.RecentChanges.page-title'));?></a></li>
						<li class="siteTools"><a href="#" onclick="return DWMenu.Position('menuTools', this, 0, 0);"><span></span><?php echo(wfMsg('Skin.Common.header-tools'));?></a></li>
						<li class="siteHelp"><a href="<?php $this->html('helpurl'); ?>"><span></span><?php echo(wfMsg('Skin.Common.header-help'));?></a></li>
						<?php 
						global $wgHostedVersion, $wgAccountLevel;
						if ($wgHostedVersion == true && $wgAccountLevel != 'pro') { 
							echo('<li class="siteHelp"><a href="http://wik.is/pro/"><span></span>'.wfMsg('Skin.Common.gopro').'</a></li>');
						}
						?>
					</ul>
					<div class="post"></div>
				</div>
				<div class="pageBar">
					<div class="pre"></div>
					<div class="pageRevision">
						<!-- last modified -->
						<?php echo($this->haveData('pagemodified')); ?>
						<!-- end last modified -->					
					</div>
					<div class="pageNav">
						<ul>
							<li class="pageEdit"><?php $this->html('pageedit');?></li>
							<li class="pageAdd"><?php $this->html('pageadd');?></li>
							<li class="pageRestrict"><?php $this->html('pagerestrict');?></li>
							<li class="pageAttach"><?php $this->html('pageattach');?></li>
							<li class="pageMove"><?php $this->html('pagemove');?></li>
							<li class="pageDelete"><?php $this->html('pagedelete');?></li>
							<li class="pagePrint"><?php $this->html('pageprint');?></li>
							<li class="pageMore"><a href="#" onclick="return DWMenu.Position('menuPageOptions', this, 0, 0);"><span></span><?php echo(wfMsg('Skin.Common.more')); ?></a></li>
							<li class="navSplit"></li>
							<li class="pageToc"><?php $this->html('pagetoc');?></li>
						</ul>
					</div>
					<div class="post"></div>
				</div>
				<?php }?>
				<div class="pageContentFrame">
				<div class="custom custom4">
				<?php $this->html('customarea4'); ?>
				</div>
				
				<div id="pageContent" class="pageContent">
				
					<?php if ($this->hasData('hierarchy') && $wgTitle->isEditable() && (Skin::isViewPage() || Skin::isEditPage())) { ?>
		 				<div class="hierarchy">
		 					<?php $this->html('hierarchy'); ?>
		 				</div>
	 				<?php } ?>

					<?php if ($this->haveData('page.title')) : ?>
					<div class="pageTitle">
						<?php if ($this->hasData('page.alerts')  && $this->cmsmode == false) : ?>
							<div class="hideforedit">
				    			<?php $this->html('page.alerts'); ?>
				    		</div>
				    	<?php endif; ?>
						<?php if ($this->haveData('pageismoved')) { 
							echo('<a class="pageMoved hideforedit" href="'.$this->haveData('pagemovelocation').'">'.$this->haveData('pagemovemessage').'</a>'); 
						}?>
						
						<?php if ($this->hasData('page.rating.header')) : ?>
							<div class="hideforedit">
								<?php $this->html('page.rating.header'); ?>
							</div>
						<?php endif; ?>
						
						<h1 id="title">
							<span <?php if ($this->haveData('pageisrestricted')) { echo(' class="pageRestricted" '); }?>>
							<?php $this->html('page.title'); ?>
							</span>
						</h1>
					</div>
					
					<?php endif; ?>			
					
					<?php $this->html('pagesubnav'); ?>
					
					<div class="pageStatus">
 					    <?php echo(wfMessagePrint()); ?>
 					</div>
 					
 							
					<div class="<?php $this->html('pagename'); ?>">								
	 				<?php $this->html('bodytext'); ?>
	 				</div>
				</div>
				</div>
				<div class="DW-clear"></div>
				<?php if ($this->cmsmode==false || !$wgUser->isAnonymous()) {?>
				<?php if ($wgTitle->canEditNamespace() && (Skin::isViewPage() || Skin::isEditPage())) { ?>
				<div class="pageInfo">
					<dl>
						<?php if ($this->hasData('page.rating')) { ?>
						<dt class="pageRatings"><span><?php echo(wfMsg('Page.ContentRating.skin.page.rating'));?></span></dt>
						<dd class="pageRatings"><?php $this->html('page.rating'); ?></dd>
						<?php } ?>
					
						<dt class="pageTags"><span><?php echo(wfMsg('Skin.Common.tags'));?> <?php $this->html('tagsedit'); ?></span></dt>
						<dd class="pageTags">
			 				<?php $this->html('tagstext'); ?>
		 				</dd>
						
						<dt class="pageIncomingLinks"><span><?php echo(wfMsg('Skin.Common.what-links-here'));?></span></dt>
						<dd class="pageIncomingLinks"></dd>
					</dl>
				</div>
				
				<?php 
 					if ($this->hasData('languages') && $wgTitle->isEditable()) { 
	 					echo('<div class="pageInfo languages"><strong>'.wfMsg('Article.Common.languages').'</strong>');
	 					$this->html('languages');
	 					echo('</div>');
	 				} 
		 		?>
				
				<div class="file">
					<h2><span><?php echo(wfMsg('Skin.Common.header-files-count', $this->haveData('filecount'))); ?></span></h2>
					<div class="fileAdd">
						<?php $this->html('fileaddlink'); ?>
					</div>
					<div class="fileList">
		 				<?php $this->html('filestext');	?>
					</div>
				</div>
				
				<?php if ($this->haveData('gallerytext')) : ?>
				<div class="gallery">
	 				<?php $this->html('gallerytext'); ?>
	 			</div>
				<?php endif; ?>
				
 				<?php $this->html('comments'); ?>
 						
				<?php } ?>
				<?php } ?>
				<div class="custom custom5">
					<?php $this->html('customarea5'); ?>
				</div>
			</div>
			<div class="bodyFooter">
					<div class="pre"></div>
					<div class="post"></div>
				</div>
		</div>
		<div class="MindTouch"><?php $this->html('poweredbytext'); ?></div>
	</div>
	<div class="custom custom8">
		<?php $this->html('customarea8'); ?>
	</div>
</div>

<?php // for inline messages from nav pane ?>

<script type="text/javascript">var _endtime = new Date().getTime(); var _size = <?php echo(ob_get_length())?>;</script>

<div onclick="DWMenu.Bubble=true;" class="menu" id="menuTools" style="display:none;">
	<ul><?php
		if (!$wgUser->isAnonymous()) { 
			$this->FiestaSiteTools('Watchedpages', 'Page.WatchedPages.page-title');
			$this->FiestaSiteTools('Contributions', 'Page.Contributions.page-title');
			$this->FiestaSiteTools('Preferences', 'Page.UserPreferences.page-title');
		}
		$this->FiestaSiteTools('ListRss', 'Page.ListRss.page-title');
		$this->FiestaSiteTools('Listusers', 'Page.Listusers.page-title');
		$this->FiestaSiteTools('ListTemplates', 'Page.ListTemplates.page-title');
		$this->FiestaSiteTools('Sitemap', 'Page.SiteMap.page-title');
		$this->FiestaSiteTools('Popularpages', 'Page.Popularpages.page-title');
		
		echo sprintf('<li class="%s"><a href="%s" target="_blank" title="%s"><span></span>%s</a></li>', 
			'deki-desktop-suite', 
			ProductURL::DESKTOP_SUITE, 
			wfMsg('Skin.Common.desktop-suite'), 
			wfMsg('Skin.Common.desktop-suite')
		);
	?></ul>	
</div>

<div onclick="DWMenu.Bubble=true;" class="menu" id="menuPageOptions" style="display:none;">
	<ul><?php
		$this->FiestaPageMenuControl('edit', 'Skin.Common.edit-page');
		$this->FiestaPageMenuControl('add', 'Skin.Common.new-page');
		$this->FiestaPageMenuControl('pdf', 'Skin.Common.page-pdf');
		$this->FiestaPageMenuControl('restrict', 'Skin.Common.restrict-access');
		$this->FiestaPageMenuControl('attach', 'Skin.Common.attach-file');
		$this->FiestaPageMenuControl('move', 'Skin.Common.move-page');
		$this->FiestaPageMenuControl('delete', 'Skin.Common.delete-page');
		$this->FiestaPageMenuControl('print', 'Skin.Common.print-page');
		$this->FiestaPageMenuControl('tags', 'Skin.Common.tags-page');
		$this->FiestaPageMenuControl('email', 'Skin.Common.email-page');
		$this->FiestaPageMenuControl('properties', 'Skin.Common.page-properties');
		$this->FiestaPageMenuControl('talk', 'Skin.Common.page-talk');
		if ($wgTitle->userIsWatching()) { 
			$this->FiestaPageMenuControl('watch', 'Skin.Common.unwatch-page');
		}
		else {
			$this->FiestaPageMenuControl('watch', 'Skin.Common.watch-page');
		}
	?>
</div>

<div onclick="DWMenu.Bubble=true;" class="menu" id="menuBacklink" style="display:none;">
	<?php $this->html('pagebacklinks'); ?>
</div>

<div onclick="DWMenu.Bubble=true;" class="menu" id="menuPageContent" style="display:none;">
	<?php $this->html('toc'); ?>
</div>

<?php $this->html('pagefooter'); ?>

</body>
<?php $this->html('customtail'); ?>  
<?php $this->html('customarea6'); ?>
</html><?php
    }
    
    function FiestaSiteTools($key, $languageKey) {
	    global $wgUser;
	    $sk = $wgUser->getSkin();
		$t = Title::makeTitle( NS_SPECIAL, $key );
	    $href = $sk->makeSpecialUrl($key);
	    if ($key == 'Contributions') {
		    $href = $t->getLocalURL('target=' . urlencode( $wgUser->getUsername()));
	    }
	    elseif ($key == 'ListTemplates') {
		 	$t = Title::makeTitle('', NS_TEMPLATE);  
		 	$href = $sk->makeNSUrl('', '', NS_TEMPLATE);
	    }
	    elseif ($key == 'Listusers') {
		 	$t = Title::makeTitle('', NS_USER);  
		 	$href = $sk->makeNSUrl('', '', NS_USER);
	    }
	    else {
		    $href = $t->getLocalURL();   
	    }
	 	echo("\t".'<li class="site'.ucfirst($key).'"><a href="'.$href.'" title="'. wfMsg($languageKey) .'"><span></span>'. wfMsg($languageKey) .'</a></li>'."\n");
	}
    
    function FiestaSiteSearch() {
?>	
	<div class="siteSearch">
		<fieldset class="search">
	 		<form action="<?php $this->text('searchaction') ?>">
		        <span><?php echo(wfMsg('Page.Search.search'));?> </span><input tabindex="1" id="searchInput" class="inputText" name="search" type="text" value="<?php $this->text('search'); ?>" />
				<input type="hidden" name="type" value="fulltext" />
		        <input type="submit" name="go" class="inputSubmit" value="<?php echo wfMsg('Skin.Common.submit-find'); ?>" />
			</form>
		</fieldset>
	</div>
<?php		
	}
	
	function FiestaUserAuth() {
		global $wgUser;
	?>
		<div class="userAuth">
			<?php echo('<span>'.wfMsg('Skin.Common.logged-in').'</span>'); ?>
			<span>Logged in as:</span>					
	 		<?php if (!$wgUser->isAnonymous()) { 
		  		 	echo('<a href="'.$this->haveData('userpageurl').'" class="userPage">');
					$this->text('username');
					echo('</a>');
		 			echo('<a href="'.$this->haveData('logouturl').'" class="userLogout">'.wfMsg('Page.UserLogout.page-title').'</a>');
				} else { 
	 				echo('<a href="'.$this->haveData('loginurl').'" class="userLogin">'.wfMsg('Page.UserLogin.page-title').'</a>');
				}
			?>
		</div>
	<?php	
	}
	
	function FiestaPageMenuControl($key, $languageKey) {
		$pkey = 'page'.$key;
		$href = $this->haveHref($pkey);
		$onclick = 'menuOff(\'menuPageOptions\');'.$this->haveOnClick($pkey);
		$class = $this->haveCSSClass($pkey);
		echo("\t".'<li class="page'.ucfirst($key).' '.$class.'"><a href="'.$href.'"'.($class != '' ? ' class="'.$class.'"': '').' onclick="'.$onclick.'" title="'.wfMsg($languageKey).'"><span></span>'.wfMsg($languageKey).'</a></li>'."\n");
	}
}
?>
