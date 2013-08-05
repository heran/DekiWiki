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
 * Deuce Template
 *
 * @todo document
 * @subpackage Skins
 */
if (defined('MINDTOUCH_DEKI')) :

/** */
require_once('includes/SkinTemplate_.php');

/**
 * Inherit main code from SkinTemplate, set the CSS and template filter.
 * @todo document
 * @package MediaWiki
 * @subpackage Skins
 */
class SkinDeucenew extends SkinTemplate
{
	/**
	 * @type const - Defines the number of custom HTML areas available
	 */
	const HTML_AREAS = 0;

    /** Using Deuce. */
    function initPage(&$out)
	{
        SkinTemplate::initPage($out);
        $this->skinname  = 'deucenew';
        $this->stylename = 'deucenew';
        $this->template  = 'DeucenewTemplate';
    }

	/**
	 * (non-PHPdoc) customize display for dashboard header: show last modified
	 * @see includes/Skin#dashboardHeader($PageInfo)
	 */
	function renderUserDashboardHeader($Article, $Title)
	{
		global $wgLang;
		$disabled = '<span class="disabled">'.wfMsg('Skin.Common.page-cant-be-edited').'</span>';

		if ($Article->getId() == 0 || !$Article->getTitle()->isEditable() || !$Article->getTimestamp()) {
			return $disabled;
		}

		$html = '';

		$timestamp = $Article->getTimestamp();
		$formattedts = $wgLang->timeanddate( $timestamp, true );
		$historylink = $Article->getTitle()->getLocalUrl('action=history');

		$User = $Article->getDekiUser();
		$UserTitle = $User->getUserTitle();

		$userlink = $this->makeLinkObj($UserTitle, $User->toHtml());

		$html .= '<div class="modified">';
		$html .= wfMsg('Skin.Common.page-last-modified-full', sprintf('<a href="%s" title="%s">%s</a>', $historylink, $formattedts, $formattedts), $userlink);
		$html .= '</div>';

		return $html;
	}
}

/**
 * Enter the Deuce beta skin!
 */
class DeucenewTemplate extends QuickTemplate
{
    /**
     * Template filter callback for Base skin.
     * Takes an associative array of data set from a SkinTemplate-based
     * class, and a wrapper for MediaWiki's localization database, and
     * outputs 	a formatted page.
     *
     * @access private
     */
	function makeSpecialLink($pageKey, $languageKey)
	{
		$t = Title::makeTitle(NS_SPECIAL, $pageKey);
		$url = $t->getLocalUrl();

		$text = wfMsg($languageKey);

		return sprintf('<a href="%s" title="%s"><span></span>%s</a>', $url, $text, $text);
	}

	function makeOptionsLink($page, $languageKey)
	{
		$pkey = 'page'.$page;
		$href = $this->haveHref($pkey);
		$onclick = sprintf("DWMenu.Off('menuoptions');%s", $this->haveOnClick($pkey));
		$liClass = ucfirst($page);
		$class = $this->haveCSSClass($pkey);
		$text = wfMsg($languageKey);

		return sprintf('<li class="page%s"><a href="%s"%s onclick="%s" title="%s">%s<span class="text">%s</span></a>', $liClass, $href, ($class != '' ? ' class="'. $class .'"': ''), $onclick, $text, Skin::iconify($page), $text);
	}

    private function makeToolsLink($ns, $key, $languageKey)
    {
	    global $wgUser;

		$t = Title::makeTitle( $ns, $ns == NS_SPECIAL ? $key: '' );
	    if ($key == 'Contributions'){
		    $href = $t->getLocalURL('target=' . urlencode( $wgUser->getUsername()));
	    }else{
		    $href = $t->getLocalURL();
	    }

	    if (empty($key)){
		 	switch ($ns){
				case NS_ADMIN:
					$class = 'ControlPanel';
				break;
				case NS_TEMPLATE:
					$class = 'ListTemplates';
				break;
				case NS_USER:
					$class = 'ListUsers';
				break;
		 	}
	    }else{
		    $class = ucfirst($key);
	    }

	 	return sprintf('<li class="%s"><a href="%s" title="%s">%s<span class="text">%s</span></a></li>', $class, $href, wfMsg($languageKey), Skin::iconify(strtolower($key)), wfMsg($languageKey) );
	}

	private function hasLinksList()
	{
		global $wgOut;
		$links = $wgOut->getBacklinks();
		return !empty($links);
	}

	private function getLogoImage()
	{
		if (is_null(wfGetConfig('ui/logo-uri')))
		{
			return $this->get('pathskin').'/logo.png';
		}
		return wfGetConfig('ui/logo-uri');
	}

	private function makeLinksList()
	{
		global $wgOut;

		$links = $wgOut->getBacklinks();
		$linksHtml = '';

		if (is_array($links)){
			foreach ($links as $link){
				$linksHtml .= sprintf('<a href="%s">%s</a>, ', $link['path'], $link['title']);
			}

			if (!empty($linksHtml)){
				$linksHtml = substr($linksHtml, 0, -2);
			}
		}
		if (empty($linksHtml)){
			$linksHtml = 'No pages link here';
		}
		return $linksHtml;
	}

	private function getCSSPath()
	{
		$path = $this->get('pathskin') . '/css.php';

		// allow custom handlers (like CDN plugin) to rewrite css endpoint
		DekiPlugin::executeHook(Hooks::SKIN_CSS_PATH, array(&$path));

		return $path;
	}

	private function getAdvancedSearchUri()
	{
		$params = array(
			'view' => 'advanced'
		);
		return $this->get('searchaction') . '?' . http_build_query($params);
	}

    function execute()
    {
		global $wgLogo, $wgUser, $wgTitle, $wgRequest, $wgArticle, $wgOut, $editor, $wgScriptPath, $wgContLang, $wgMenus, $IP;
		global $wgHelpUrl;

		$sk = $wgUser->getSkin();
		$isArticle = $editor || $wgArticle->getID() > 0 || $wgArticle->mTitle->isEmptyNamespace();

		// allow variable overrides
		DekiPlugin::executeHook(Hooks::SKIN_OVERRIDE_VARIABLES, array(&$this));
?>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="<?php $this->text('lang') ?>" lang="<?php $this->text('lang') ?>" dir="<?php echo $this->html('dir') != '' ? $this->html('dir') : 'ltr'; ?>">
<head>
<title><?php $this->text('pagetitle'); ?></title>
<meta http-equiv="Content-Type" content="<?php $this->text('mimetype') ?>; charset=<?php $this->text('charset') ?>" />

<?php $this->html('headlinks'); ?>
<?php $this->html('resetcss'); ?>
<?php echo $this->html('screencss'); ?>
<!--[if IE 7]><link href="<?php $this->html('pathskin'); ?>/ie7.css" rel="stylesheet" type="text/css" /><![endif]-->
<!--[if IE 6]><link href="<?php $this->html('pathskin'); ?>/ie6.css" rel="stylesheet" type="text/css" /><![endif]-->
<link href="<?php echo $this->getCSSPath();?>" media="screen" rel="stylesheet" type="text/css" />
<?php $this->html('printcss'); ?>
<link rel="stylesheet" type="text/css" media="print" href="<?php $this->html('pathtpl'); ?>/print.css" />
<!--<script type="text/javascript" src="<?php $this->html('pathskin'); ?>/jquery-1.8.3.min.js"></script>-->
<!--<script type="text/javascript" src="<?php $this->html('pathskin'); ?>/topmenu.js"></script>-->
<?php /* Default Script */ ?>
<?php $this->html('javascript'); ?>

<?php $this->html('customhead'); ?>
</head>
<body<?php if (!$wgUser->isAnonymous()) { echo(' id="loggedin"'); } ?> class="<?php $this->html('pagetype'); ?>">
<?php $this->html('pageheader'); ?>


<!--<div id="header">
	<div class="wrap">
		<div class="logo">
			<a href="/"><img src="<?php echo $this->getLogoImage(); ?>" /></a>
		</div>
		<a class="advanced" href="<?php echo $this->getAdvancedSearchUri() ?>"><?php echo wfMsg('Skin.common.header-advanced-search') ?></a>
		<div class="search">
			<form action="<?php $this->text('searchaction') ?>">
				<fieldset>
					<label for="searchInput">
						<span class="text"><?php echo(wfMsg('Page.Search.search'));?></span>
					</label>
					<input type="text" name="search" tabindex="1" class="search" id="searchInput" value="<?php $this->text('search'); ?>" />
					<input type="submit"><?php echo wfMsg('Skin.Common.submit-search'); ?></input>
				</fieldset>
			</form>
		</div>
		<div class="clear"></div>
	</div>
</div>-->

<header>
      <div class="container header-container strict ">
        <div class="ddown user" style="float: left;"> <a href="/User:Admin" class="graylink"> <img class="bordered-thumb" src="skins/common/icons/icon-user-s.gif"> <i class="ellipsis">Admin</i> </a>
          <ul>
            <li><a href="<?php echo $this->haveData('userpageurl'); ?>"><?php echo wfMsg('Skin.Common.header-my-page'); ?></a></li>
            <li><a href="<?php echo $sk->makeSpecialUrl('Recentchanges') ;?>"><?php echo wfMsg('Page.RecentChanges.page-title'); ?></a></li>
            <li>
			<?php if(!$wgUser->isAnonymous()){ ?>
				<a href="<?php echo $this->haveData('userpageurl'); ?>" class="mypage"><span class="deki-deuce-loggedin"><?php echo wfMsg('Skin.Common.logged-in'); ?></span> <span class="username"><?php echo $this->text('username'); ?></span></a></li>
	  		 	<li><a href="<?php echo $this->haveData('logouturl'); ?>" class="logout"><?php echo wfMsg('Page.UserLogout.page-title'); ?></a>

			<?php } else{ ?>
				<a href="<?php echo $this->haveData('loginurl'); ?>" class="login"><?php echo wfMsg('Page.UserLogin.page-title'); ?></a>
			<?php } ?>
			</li>
          </ul>
        </div>

        <div class="user ddown" style="float: left;"> <a href="javascript:void(0)" class="graylink"> <i class="ellipsis">工具</i> </a>
          <ul>
            <?php
				if (!$wgUser->isAnonymous())
				{
					echo $this->makeToolsLink(NS_SPECIAL, 'Watchedpages', 'Page.WatchedPages.page-title');
					echo $this->makeToolsLink(NS_SPECIAL, 'Contributions', 'Page.Contributions.page-title');
					echo $this->makeToolsLink(NS_SPECIAL, 'Preferences', 'Page.UserPreferences.page-title');
				}
				if ($wgUser->isAdmin())
				{
					echo $this->makeToolsLink(NS_ADMIN, 'controlpanel', 'Admin.ControlPanel.page-title');
				}
				echo $this->makeToolsLink(NS_TEMPLATE, 'templatelist', 'Page.ListTemplates.page-title');
				echo $this->makeToolsLink(NS_USER, 'userlist', 'Page.Listusers.page-title');
				echo $this->makeToolsLink(NS_SPECIAL, 'Popularpages', 'Page.Popularpages.page-title');
				echo sprintf('<li class="%s"><a href="%s" target="_blank" title="%s">%s<span class="text">%s</span></a></li>',
					'deki-desktop-suite',
					ProductURL::DESKTOP_SUITE,
					wfMsg('Skin.Common.desktop-suite'),
					Skin::iconify('deki-desktop-suite'),
					wfMsg('Skin.Common.desktop-suite')
				);
			?>
            <li><a href="<?php echo $this->haveData('helpurl'); ?>"><?php echo wfMsg('Skin.Common.header-help'); ?></a></li>
          </ul>
        </div>
        <a class="top-link-my-courses graylink" href="/">首页</a>
        <a class="top-link-course-list graylink"  style="float:left;" href="/Special:Reports"><?php echo wfMsg('Skin.Common.reports'); ?></a>
        <a id="logo" href="/"><img src="<?php echo $this->getLogoImage(); ?>" /></a>
        <a class="top-link-course-list graylink"  style="float:right; box-shadow:none; padding-right:0px;" href="<?php echo $this->getAdvancedSearchUri() ?>"><?php echo wfMsg('Skin.common.header-advanced-search') ?></a>
        <div class="ud-search">
          <form id="searchbox" action="http://u25.wmios.com/course/index.php">
            <input type="text" placeholder="搜索" autocomplete="off" name="search" id="quick-search" class="ui-autocomplete-input" role="textbox" aria-autocomplete="list" aria-haspopup="true">
            <input type="submit">
          </form>
        </div>
        </div>
    </header>

<div class="wrap center_nr">

	<!--<div id="sitenav">
		<div class="site">
			<ul>
				<li class="home"><a href="/">Home</a></li>
				<li class="mypage"></li>
				<li class="whatsnew"></li>
				<?php if ($wgUser->isAdmin()): ?>
				<li class="reports"><a href="/Special:Reports"><?php echo wfMsg('Skin.Common.reports'); ?></a></li>
				<?php endif; ?>
				<li class="tools"><a href="#" onclick="return DWMenu.Position('menutools', this, 0, 0);"><span class="more"><?php echo wfMsg('Skin.Common.header-tools'); ?></span></a></li>
				<li class="help"><a href="<?php echo $this->haveData('helpurl'); ?>"><?php echo wfMsg('Skin.Common.header-help'); ?></a></li>
			</ul>
		</div>
		<div class="user">
			<ul>
			<?php if(!$wgUser->isAnonymous()){ ?>
				<li><a href="<?php echo $this->haveData('userpageurl'); ?>" class="mypage"><span class="deki-deuce-loggedin"><?php echo wfMsg('Skin.Common.logged-in'); ?></span> <span class="username"><?php echo $this->text('username'); ?></span></a></li>
	  		 	<li><a href="<?php echo $this->haveData('logouturl'); ?>" class="logout"><?php echo wfMsg('Page.UserLogout.page-title'); ?></a></li>

			<?php } else{ ?>
				<li><a href="<?php echo $this->haveData('loginurl'); ?>" class="login"><?php echo wfMsg('Page.UserLogin.page-title'); ?></a></li>
			<?php } ?>
			</ul>
		</div>
		<div class="clear"></div>
	</div>-->

	<div class="bodyheader">
		<div class="spacer">&nbsp;</div>
	</div>

	<div id="body">
		<div class="nav">
			<?php $this->html('sitenavtext'); ?>
		</div>
		<div class="body">
			<div class="pagebar">
				<div class="options">
					<ul>
						<li class="pageedit"><?php $this->html('pageedit');?></li>
						<li class="pagecreate"><?php $this->html('pageadd');?></li>
						<li class="pagemore last"><?php
							if ($wgTitle->isEditable())
							{
								echo sprintf('<a href="#" onclick="return DWMenu.Position(\'menuoptions\', this, 0, 0);"><span class="more">%s</span></a>', wfMsg('Skin.Common.more'));
							}
							else
							{
								echo sprintf('<a href="#" class="disabled" onclick="return false;"><span class="more">%s</span></a>', wfMsg('Skin.Common.more'));
							}
						?></li>
					</ul>
				</div>
				<?php if ($wgTitle->isEditable()) : ?>
				<div class="info">
					<dl>
						<dd class="pagemain"><?php $this->html('pagemain'); ?></dd>
						<dd class="pagetalk"><?php $this->html('pagetalk'); ?></dd>
						<?php if ($this->hasData('page.alerts')) : ?>
							<dd class="last pagealerts"><?php $this->html('page.alerts'); ?></dd>
						<?php endif; ?>
					</dl>
				</div>
				<?php endif; ?>
				<div class="clear"></div>
			</div>

			<?php if ($wgTitle->isEditable()) : ?>
				<div class="breadcrumbs hideforedit">
					<div class="pagemetalinks">
						<ul>
							<li class="pagetoc"><a href="#" onclick="Deki.$('#pagetoc').toggle();return false;"><?php echo wfMsg('Skin.Common.header-toc'); ?></a></li>
						</ul>
					</div>
					<?php if ($this->hasData('hierarchyaslist') && $wgTitle->isEditable() && (Skin::isViewPage() || Skin::isEditPage())) { ?>
						<?php if (!$wgTitle->isTalkPage()) { ?>
			 				<div class="hierarchy">
			 					<?php $this->html('hierarchyaslist'); ?>
			 				</div>
		 				<?php } else { ?>
			 				<div class="deki-returnto">
				 				<a href="<?php echo $this->haveHref('pagemain');?>" class="returnto"><?php echo wfMsg('Skin.Deuce.return-to-page', htmlspecialchars($wgTitle->getText())); ?></a>
			 				</div>

			 			<?php } ?>
		 			<?php } ?>
		 			<div class="br"></div>
				</div>
			<?php endif; ?>

			<?php
			// eventually we need to resolve how the top bar shows up if the page is editable - only special pages has this, so it's safe
			?>
			<?php $this->html('pagesubnav'); ?>

			<div class="content">

				<div class="toc" id="pagetoc" style="display:none;">
					<?php $this->html('toc'); ?>
				</div>
				<?php if ($this->exists('title')) : ?>
				<div class="title">
					<?php if ($this->hasData('page.rating.header')) : ?>
						<div class="hideforedit">
							<?php $this->html('page.rating.header'); ?>
						</div>
					<?php endif; ?>

					<h1 id="title">
					<?php
					if ($this->get('pageisrestricted'))
					{
						echo('<div class="restricted"><a href="'.$this->href('pagerestrict').'" onclick="'.$this->onclick('pagerestrict').'">'.Skin::iconify('key').'</a></div>');
					}
					if ($wgTitle->isTalkPage())
					{
						echo('<div class="talkpage"><a href="'.$wgTitle->getLocalUrl().'">'.Skin::iconify('pagetalk').'</a></div>');
					}
					?>
					<?php $this->html('page.title'); ?>

					<?php if ($this->get('pageismoved')) : ?>
						<span class="redir"><?php echo(wfMsg('Article.Common.redirected-from', $this->get('pagemovelocation'))); ?></span>
					<?php endif; ?>
					</h1>

					<?php if ($wgTitle->isEditable()) : ?>
					<div class="modified hideforedit">
						<?php $this->html('pagemodified'); ?> <span class="pagehistory">| <?php $this->html('pagehistory'); ?></span>
					</div>
					<?php endif; ?>

				</div>
				<?php endif; ?>

				<?php echo wfMessagePrint(); ?>

				<div class="text">
					<?php $this->html('bodytext'); ?>
					<div class="br"></div>
				</div>
			</div>

			<?php if ($wgTitle->canEditNamespace() && (Skin::isViewPage() || Skin::isEditPage())) : ?>
			<div class="pageinfo">
				<dl>
					<?php if ($this->exists('page.rating')) : ?>
						<dt>
							<?php echo wfMsg('Page.ContentRating.skin.page.rating'); ?>
						</dt>
						<dd class="ratings">
							<?php $this->html('page.rating'); ?>
						</dd>
					<?php endif; ?>


					<?php if ($this->exists('tags')) : ?>
						<dt>
							<?php echo wfMsg('Skin.Common.tags'); ?> <?php $this->html('tagsedit'); ?>
						</dt>
						<dd class="tags">
							<?php $this->html('tagsinline'); ?>
						</dd>
					<?php endif; ?>

					<?php if ($this->exists('related')) : ?>
						<dt><?php echo wfMsg('Skin.Common.related-pages'); ?></dt>
						<dd><?php $this->html('related'); ?></dd>
					<?php endif; ?>

					<?php if ($this->exists('backlinks')) : ?>
						<dt><?php echo wfMsg('Skin.Deuce.what-links-here'); ?></dt>
						<dd><?php $this->html('backlinks'); ?></dd>
					<?php endif; ?>

					<?php if ($this->exists('languages')) : ?>
						<dt><?php echo wfMsg('Skin.Common.other-languages'); ?></dt>
						<dd><?php $this->html('languages');?></dd>
					<?php endif; ?>

					<dt><?php echo(wfMsg('Skin.Common.page-stats'));?></dt>
					<dd><?php echo(wfMsg('Skin.Common.page-stats-string', $this->haveData('pageviews'), $this->haveData('pagerevisions'), $this->haveData('pagecharcount')));?><dd>
				</dl>
			</div>

			<div class="comments pagemeta" id="anchor-comments">
				<h2><?php echo wfMsg('Skin.Common.header-comments');?></h2>
				<?php $this->html('comments'); ?>
				<div class="br"></div>
			</div>

			<div class="attachments pagemeta" id="anchor-files">
				<p class="add<?php echo $wgArticle->userCanEdit() ? '' : ' disabled'; ?>"><?php $this->html('pageattach');?></p>
				<h2><?php echo wfMsg('Skin.Common.header-attachments'); ?></h2>
				<div class="br"></div>

				<?php $this->html('filestext');	?>
				<div class="br"></div>
			</div>

			<?php if ($this->haveData('gallerytext')) : ?>
			<div class="pagemeta" id="anchor-gallery">
 				<?php $this->html('gallerytext'); ?>
 			</div>
			<?php endif; ?>

			<?php endif; ?>
		</div>
		<div class="br"></div>
	</div>

	<div class="bodyfooter">
		<div class="spacer">&nbsp;</div>
	</div>
</div>
<div id="footer"><p>©2013 WMIOS</p></div>
<?php // PopupWindows ?>
<div class="popups">
	<div id="popupMessage"></div>
		<div id="popupMessage"></div> <?php // for inline messages from nav pane ?>

	<script type="text/javascript">var _endtime = new Date().getTime(); var _size = <?php echo(ob_get_length())?>;</script>

	<div onclick="DWMenu.Bubble=true;" class="menu" id="menutools" style="display:none;">
		<div class="header">&nbsp;</div>
		<div class="body">
			<ul>
			<?php
				if (!$wgUser->isAnonymous())
				{
					echo $this->makeToolsLink(NS_SPECIAL, 'Watchedpages', 'Page.WatchedPages.page-title');
					echo $this->makeToolsLink(NS_SPECIAL, 'Contributions', 'Page.Contributions.page-title');
					echo $this->makeToolsLink(NS_SPECIAL, 'Preferences', 'Page.UserPreferences.page-title');
					echo sprintf('<li class="%s"><span>%s</span></li>', 'spacer', '&nbsp;');
				}
				if ($wgUser->isAdmin())
				{
					echo $this->makeToolsLink(NS_ADMIN, 'controlpanel', 'Admin.ControlPanel.page-title');
					echo sprintf('<li class="%s"><span>%s</span></li>', 'spacer', '&nbsp;');
				}
				echo $this->makeToolsLink(NS_TEMPLATE, 'templatelist', 'Page.ListTemplates.page-title');
				echo $this->makeToolsLink(NS_USER, 'userlist', 'Page.Listusers.page-title');
				echo $this->makeToolsLink(NS_SPECIAL, 'Popularpages', 'Page.Popularpages.page-title');
				echo sprintf('<li class="%s"><a href="%s" target="_blank" title="%s">%s<span class="text">%s</span></a></li>',
					'deki-desktop-suite',
					ProductURL::DESKTOP_SUITE,
					wfMsg('Skin.Common.desktop-suite'),
					Skin::iconify('deki-desktop-suite'),
					wfMsg('Skin.Common.desktop-suite')
				);
			?>
			</ul>
		</div>
		<div class="footer">&nbsp;</div>
	</div>

	<div onclick="DWMenu.Bubble=true;" class="menu" id="menuoptions" style="display:none;">
		<div class="header">&nbsp;</div>
		<div class="body">
			<ul>
				<?php
				// TODO: use skintemplates builtin around line 458
				echo $this->makeOptionsLink('edit', 'Skin.Common.edit-page');
				echo $this->makeOptionsLink('add', 'Skin.Common.new-page');
				echo $this->makeOptionsLink('pdf', 'Skin.Common.page-pdf');
				echo $this->makeOptionsLink('restrict', 'Skin.Common.restrict-access');
				echo $this->makeOptionsLink('attach', 'Skin.Common.attach-file');
				echo $this->makeOptionsLink('email', 'Skin.Common.email-page');
				echo $this->makeOptionsLink('move', 'Skin.Common.move-page');
				echo $this->makeOptionsLink('delete', 'Skin.Common.delete-page');
				echo $this->makeOptionsLink('tags', 'Skin.Common.tags-page');
				echo $this->makeOptionsLink('properties', 'Skin.Common.page-properties');
				echo $this->makeOptionsLink('source', 'Skin.Common.view-page-source');

				if ($wgTitle->userIsWatching())
				{
					echo $this->makeOptionsLink('watch', 'Skin.Common.unwatch-page');
				}
				else
				{
					echo $this->makeOptionsLink('watch', 'Skin.Common.watch-page');
				}
			?>
			</ul>
		</div>
		<div class="footer">&nbsp;</div>
	</div>


	<div onclick="DWMenu.Bubble=true;" class="menu" id="menuPageContent" style="display: none;">
		<div class="header">&nbsp;</div>
		<div class="body"><?php $this->html('toc'); ?></div>
		<div class="footer">&nbsp;</div>
	</div>
	<?php // {[ MESSAGE OUTPUT SHOULD BE EXTERNAL TEMPLATE ]} // ?>
	<?php $this->html('pagefooter'); ?>
</div>
</body>
<?php $this->html('customtail'); ?>
<script type="text/javascript" src="<?php $this->html('pathskin'); ?>/topmenu.js"></script>
</html>
<?php
	} // /function execute();
}

endif;
