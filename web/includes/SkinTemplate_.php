<?php
/*
 * MindTouch Core - open source enterprise collaborative networking
 *  derived from MediaWiki (www.mediawiki.org)
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
 * Template-filler skin base class
 * Formerly generic PHPTal (http://phptal.sourceforge.net/) skin
 * Based on Brion's smarty skin
 * Copyright (C) Gabriel Wicke -- http://www.aulinx.de/
 * Copyright (C) MindTouch, Inc.
 *
 * Todo: Needs some serious refactoring into functions that correspond
 * to the computations individual esi snippets need. Most importantly no body
 * parsing for most of those of course.
 *
 * PHPTAL support has been moved to a subclass in SkinPHPTal.php,
 * and is optional. You'll need to install PHPTAL manually to use
 * skins that depend on it.
 *
 * @package MediaWiki
 * @subpackage Skins
 */

/**
 * This is not a valid entry point, perform no further processing unless
 * MEDIAWIKI is defined
 */
if( defined( 'MINDTOUCH_DEKI' ) ) {

/**
 *
 * @package DekiWiki
 */
class SkinTemplate extends Skin {
	/**
	 * @type const - Defines the number of custom HTML areas available
	 */
	const HTML_AREAS = 0;

	/**#@+
	 * @access private
	 */

	/**
	 * Name of our skin, set in initPage()
	 * It probably need to be all lower case.
	 */
	var $skinname;

	/**
	 * Stylesheets set to use
	 * Sub directory in ./skins/ where various stylesheets are located
	 */
	var $stylename;

	/**
	 * For QuickTemplate, the name of the subclass which
	 * will actually fill the template.
	 *
	 * In PHPTal mode, name of PHPTal template to be used.
	 * '.pt' will be automaticly added to it on PHPTAL object creation
	 */
	var $template;

	/**#@-*/

	/**
	 * Setup the base parameters...
	 * Child classes should override this to set the name,
	 * style subdirectory, and template filler callback.
	 *
	 * @param OutputPage $out
	 */
	function initPage( &$out ) {
		parent::initPage( $out );
		$this->skinname  = 'monobook';
		$this->stylename = 'monobook';
		$this->template  = 'QuickTemplate';
	}

	/**
	 * Create the template engine object; we feed it a bunch of data
	 * and eventually it spits out some HTML. Should have interface
	 * roughly equivalent to PHPTAL 0.7.
	 *
	 * @param string $callback (or file)
	 * @param string $repository subdirectory where we keep template files
	 * @param string $cache_dir
	 * @return object
	 * @access private
	 */
	function &setupTemplate( $classname, $repository=false, $cache_dir=false ) {
		$ret_val = new $classname();
		return $ret_val;
	}

	/***
	 * Defines the page type
	 */
	function getPageTypes() {
		global $wgTitle, $wgUser;
		
		$pagetype = array();
		if ($wgTitle->getNamespace() == NS_ADMIN) {
			$pagetype[] = 'page-admin';
		}
		elseif ($wgTitle->getNamespace() == NS_SPECIAL) {
			$pagetype[] = 'page-special';
		}
		elseif ($wgTitle->getNamespace() == NS_USER) {
			$pagetype[] = 'page-user';
		}
		elseif ($wgTitle->getPrefixedText() == wfHomePageInternalTitle()) {
			$pagetype[] = 'page-home';
		}
		if (!$wgUser->isAnonymous()) {
			$pagetype[] = 'user-loggedin';
		}
		if ($wgUser->isAdmin()) {
			$pagetype[] = 'user-admin';
		}
		$pagetype[] = 'yui-skin-sam';
		
		return implode(' ', $pagetype);
	}
	
	/**
	 * initialize various variables and generate the template
	 *
	 * @param OutputPage $out
	 * @access public
	 */
	function outputPage( &$out ) {
		global $wgTitle, $wgArticle, $wgUser, $wgLang, $wgContLang;
		global $wgScript, $wgStylePath, $wgContLanguageCode;
		global $wgMimeType, $wgOutputEncoding, $wgRequest;
		global $wgLogo;
		global $wgSitename, $wgScriptPath, $wgLogo, $wgHelpUrl;
		global $wgAnonAccCreate;
		global $wgLanguageCode;

		extract( $wgRequest->getValues( 'oldid', 'diff' ) );

		$sk = $wgUser->getSkin();
		$User = DekiUser::getCurrent();
		
		$this->initPage( $out );
		$tpl =& $this->setupTemplate( $this->template, 'skins' );

		$isAnon = $wgUser->isAnonymous();
		
		//information about this skin
		/* deprecate */ $tpl->setRef( 'skin', $this);
		/* deprecate */ $tpl->setRef( "thispage", $wgTitle->getPrefixedDbKey() );
		/* deprecate */ $tpl->setRef('skinname', $this->skinname);
		/* deprecate */ $tpl->setRef('stylename', $this->stylename);
		
		//urls
		$tpl->set('logouturl', $isAnon ? '#' : $this->makeLogoutUrl());
		$tpl->set('loginurl', $isAnon ? $this->makeLoginUrl() : '#');
		$tpl->set('registerurl', $isAnon ? $this->makeLoginUrl() : '#');
		$tpl->set('helpurl', $wgHelpUrl);
				
		//page titles
		$tpl->set('title', $out->getPageTitle());
		$tpl->set('pagetitle', $out->getHTMLTitle() );
		$tpl->set('pagename', $wgTitle->getClassNameText());
		/* deprecate */ $tpl->set('displaypagetitle', $out->getPageTitle());
		$tpl->set('pagetype', $this->getPageTypes());

		//paths to skin files
		$tpl->set('pathcommon', $this->getCommonPath());
		$tpl->set('pathtpl', $this->getTemplatePath()); // skins/$tpl
		$tpl->set('pathskin', $this->getSkinPath()); // skins/$tpl/$skin
				
		//css
		$tpl->set('resetcss', '<link rel="stylesheet" type="text/css" media="screen" href="'.$this->getCommonPath().'/reset.css" />');
		$tpl->set('printcss', $this->getPrintCSS() );
		$tpl->set('screencss', $this->getScreenCSS() );

		//javascripting
		$tpl->set('javascript', Skin::getJavascript().Skin::getEmbeddedJavascript() );
		/* deprecate */ $tpl->set('inlinejavascript', Skin::getEmbeddedJavascript() );
		
		//page info
		$tpl->set('mimetype', $wgMimeType );
		$tpl->set('charset', $wgOutputEncoding );
		$tpl->set('language', $wgLanguageCode);
		$tpl->setRef('lang', $wgContLanguageCode );
		$tpl->set('langname', $wgContLang->getLanguageName($wgContLanguageCode));
		
		//username
		$tpl->setRef('username', $wgUser->getName());
		$tpl->set('userpageurl', !$User->isAnonymous() ? $User->getUrl(): $this->makeSpecialUrl('Userlogin', 'returntomypage=y'));
		$tpl->set('loggedin', !$wgUser->isAnonymous());
		
		// set the count for items on this page
		$pageMetrics = $out->getPageMetrics();
		/* deprecate */ $tpl->set('pageviews', $pageMetrics['views']); #deprecate for viewcount
		/* deprecate */ $tpl->set('pagecharcount', $pageMetrics['charcount']); #deprecate for charcount
		/* deprecate */ $tpl->set('pagerevisions', $pageMetrics['revisions']); #deprecate for revisioncount
		/* deprecate */ $tpl->set('revisioncount', $pageMetrics['revisions']);
		$tpl->set('charcount', $pageMetrics['charcount']);
		$tpl->set('viewcount', $pageMetrics['views']);
		$tpl->set('filecount', '<span id="filecount">'.$out->getFileCount().'</span>');
		$tpl->set('imagecount', '<span id="imagecount">'.$out->getImageCount().'</span>');
		$tpl->set('tagcount', '<span id="tagcount">'.$out->getTagCount().'</span>');
		$tpl->set('commentcount', '<span id="commentcount">'.$out->getCommentCount().'</span>');
		$tpl->set('revisioncount', $wgArticle->mRevisionCount);
		/* deprecate */ $tpl->set('pagerevisioncount', $wgArticle->mRevisionCount);
		/* deprecate */ $tpl->set('filedisplaycount', '<span class="unweight" id="fileCount">('.$out->mFileCount.')</span>'); 
		/* deprecate */ $tpl->set('imagedisplaycount', '<span class="unweight" id="imageCount">('.$out->mImageCount.')</span>');

		//site info
		/* deprecate */ $tpl->set( 'poweredbyico', $this->getPoweredBy() );
		/* deprecate */ $tpl->setRef( 'stylepath', $wgStylePath );
		$tpl->set( 'poweredbytext', $this->getPoweredBy() );
		$tpl->set( 'logo', $this->getSiteLogo() );
		
		//page header & footer
		$tpl->set('headlinks', $out->getHeadLinks()); //for <head> (todo: consolidate all screen/print css here?)
		$tpl->set('pageheader', $this->getPageHeader());
		$tpl->set('pagefooter', $this->getPageFooter());
		
		//define custom HTML areas
		$numRegions = $this->getAreaCount();	
		if ($numRegions > 0)
		{
			$SiteProperties = DekiSiteProperties::getInstance();			
			for ($i = 1; $i <= $numRegions; $i++)
			{
				$tpl->set('customarea'.$i, $SiteProperties->getCustomHtml($i));
			}
		}
		
		//base uri needs to remap to the top
		if ($wgRequest->getVal('baseuri')) 
		{
			$out->addHeadHTML('<base target="_top" />');
		}
		
		//custom head/tail
		$tpl->set('customhead', $out->getHeadHTML());
		$tpl->set('customtail', $out->getTailHTML());
		
		//breadcrumbs
		$tpl->set('hierarchy', $wgArticle->getId() > 0 ? Skin::getHierarchy(): '');
		$tpl->set('hierarchyaslist', $wgArticle->getId() > 0 ? Skin::getHierarchyAsList(): '');
		
		//page last modified information
		$lastmod = $this->lastModified('<span class="disabled">'.wfMsg('Skin.Common.page-cant-be-edited').'</span>');
		$tpl->set('lastmod', $lastmod);
		$tpl->set('lastmodby', wfMsg('System.Common.user-nobody'));
		if (0 != $wgArticle->getID() ) {
			$tpl->set('lastmodhuman', $this->lastModifiedHumanReadable());
			if ($wgArticle->getUser()) {
				$tpl->set('lastmodby', $sk->makeLink( $wgContLang->getNsText(NS_USER) . ':' . $wgArticle->getUserText(), $wgArticle->getUserText() ));
			}
		}
		$tpl->set('pagemodified', $lastmod);
		$tpl->set('pagemodifiedoffset', $this->lastOffset('<span class="disabled">'.wfMsg('Skin.Common.page-cant-be-edited').'</span>'));
		
		// pages sometimes requires subnavigation elements
		$subnav = $out->getSubNavigation();
		$tpl->set('pagesubnav', !empty($subnav) ? '<div class="deki-page-subnav">'.$subnav.'</div>': '');
		
		//this sets all the css classes, hrefs, and onclicks for page actions
		$this->setPageActions($out, $tpl);
		
		//not exactly semantically rigorous
		/* deprecate */ $tpl->set('fileaddlink', Skin::makeNakedLink('#', Skin::iconify('attach').'<span class="text">'. wfMsg('Skin.Common.attach-file-image') .'</span>',
			array('class' => $this->cssclass->pageattach, 'onclick' => $this->onclick->pageattach)));

		//set page actions
		$tpl->set('pagemain', '<a href="'.$this->href->pagemain.'" class="'.$this->cssclass->pagemain.'" title="'.wfMsg('Skin.Common.view').'">'.wfMsg('Skin.Common.view').'</a>');
		$tpl->set('pagehistory', '<a href="'.$this->href->pagehistory.'" class="'.$this->cssclass->pagehistory.'" title="'.htmlspecialchars(wfMsg('Skin.Common.history')).'">'.wfMsg('Skin.Common.history').'</a>');
		$tpl->set('pagerestrict', '<a href="'.$this->href->pagerestrict.'" title="'.htmlspecialchars(wfMsg('Skin.Common.restrict-access')).'" onclick="'.$this->onclick->pagerestrict.'" class="'.$this->cssclass->pagerestrict.'"><span></span>'.wfMsg('Skin.Common.restrict-access').'</a>');
		$tpl->set('pageattach', '<a href="'.$this->href->pageattach.'" title="'.htmlspecialchars(wfMsg('Skin.Common.attach-file')).'" onclick="'.$this->onclick->pageattach.'" class="'.$this->cssclass->pageattach.'"><span></span>'.wfMsg('Skin.Common.attach-file').'</a>');
		$tpl->set('pagemove', '<a href="'.$this->href->pagemove.'" title="'.htmlspecialchars(wfMsg('Skin.Common.move-page')).'" onclick="'.$this->onclick->pagemove.'" class="'.$this->cssclass->pagemove.'"><span></span>'.wfMsg('Skin.Common.move-page').'</a>');
		$tpl->set('pageedit', '<a href="'.$this->href->pageedit.'" title="'.htmlspecialchars(wfMsg('Skin.Common.edit-page')).'" onclick="'.$this->onclick->pageedit.'" class="'.$this->cssclass->pageedit.'"><span></span>'.wfMsg('Skin.Common.edit-page').'</a>');
		$tpl->set('pagesource', '<a href="'.$this->href->pagesource.'" title="'.htmlspecialchars(wfMsg('Skin.Common.view-page-source')).'" onclick="'.$this->onclick->pagesource.'" class="'.$this->cssclass->pagesource.'"><span></span>'.wfMsg('Skin.Common.view-page-source').'</a>');
		$tpl->set('pageprint', '<a href="'.$this->href->pageprint.'" title="'.htmlspecialchars(wfMsg('Skin.Common.print-page')).'" onclick="'.$this->onclick->pageprint.'" class="'.$this->cssclass->pageprint.'"><span></span>'.wfMsg('Skin.Common.print-page').'</a>');
		$tpl->set('pagepdf', '<a href="'.$this->href->pagepdf.'" title="'.htmlspecialchars(wfMsg('Skin.Common.page-pdf')).'" class="'.$this->cssclass->pagepdf.'"><span></span>'.wfMsg('Skin.Common.page-pdf').'</a>');
		$tpl->set('pagedelete', '<a href="'.$this->href->pagedelete.'" title="'.htmlspecialchars(wfMsg('Skin.Common.delete-page')).'" onclick="'.$this->onclick->pagedelete.'" class="'.$this->cssclass->pagedelete.'"><span></span>'.wfMsg('Skin.Common.delete-page').'</a>');
		$tpl->set('pageadd', '<a href="'.$this->href->pageadd.'" title="'.htmlspecialchars(wfMsg('Skin.Common.new-page')).'" class="'.$this->cssclass->pageadd.'"><span></span>'.wfMsg('Skin.Common.new-page').'</a>');
		$tpl->set('pagetoc', '<a href="'.$this->href->pagetoc.'" title="'.htmlspecialchars(wfMsg('Skin.Common.table-of-contents')).'" onclick="'.$this->onclick->pagetoc.'" class="'.$this->cssclass->pagetoc.'"><span></span>'.wfMsg('Skin.Common.table-of-contents').'</a>');
		$tpl->set('pagetalk', '<a href="'.$this->href->pagetalk.'" title="'.htmlspecialchars(wfMsg('Skin.Common.page-talk')).'" onclick="'.$this->onclick->pagetalk.'" class="'.$this->cssclass->pagetalk.'"><span></span>'.wfMsg('Skin.Common.page-talk').'</a>');
		$tpl->set('pagedraft', '<a href="'.$this->href->pagedraft.'" title="'.htmlspecialchars(wfMsg('Skin.Common.page-draft')).'" onclick="'.$this->onclick->pagedraft.'" class="'.$this->cssclass->pagedraft.'"><span></span>'.wfMsg('Skin.Common.page-draft').'</a>');
		$tpl->set('pageemail', '<a href="'.$this->href->pageemail.'" title="'.htmlspecialchars(wfMsg('Skin.Common.email-page')).'" onclick="'.$this->onclick->pageemail.'"><span></span>'.wfMsg('Skin.Common.email-page').'</a>');
		$tpl->set('pagewatch', '<a href="'.$this->href->pagewatch.'" title="'.htmlspecialchars($this->text->pagewatch).'" onclick="'.$this->onclick->pagewatch.'"><span></span>'.$this->text->pagewatch.'</a>');
 		$tpl->set('pageproperties', '<a href="'.$this->href->pageproperties.'" title="'.htmlspecialchars(wfMsg('Skin.Common.page-properties')).'" onclick="'.$this->onclick->pageproperties.'"><span></span>'.wfMsg('Skin.Common.page-properties').'</a>');
 		
 		//page data
 		$backlinks = $out->getBacklinks(true /*as list*/);
		/* deprecate */ $tpl->set('pagebacklinks', $backlinks);
		$tpl->set('backlinks', $backlinks);
		/* deprecate */ $tpl->set('pagemovemessage', $out->getRedirectMessage());
		$tpl->set('pagemoved', $out->getRedirectMessage());
		$tpl->set('pageismoved', $out->getRedirectMessage() != '');
		$tpl->set('pagemovelocation', $out->getRedirectLocation_());
		/* deprecate */ $tpl->set('movedto', $out->getRedirectLocation());
		
		//set content html
		$tpl->set( 'comments', $wgTitle->isEditable() ? '<div id="comments">'.$out->getCommentsHTML().'</div>' : '' );

		//special case
		$tocData = $out->getTarget('toc');
		$pageToc = '<div class="pageToc"><h5>'.wfMsgForContent('Skin.Common.table-of-contents').'</h5><div class="tocdata">'.$tocData.'</div></div>';
		$tpl->set('toc', $pageToc);
		
		//append some extra divs for identifying special/admin pages
		$bodyText = $out->mBodytext;
		if ($wgTitle->getNamespace() == NS_ADMIN) {
			$bodyText = '<div id="pageTypeAdmin">'.$bodyText.'</div>';
		}
		elseif ($wgTitle->getNamespace() == NS_SPECIAL) {
			$bodyText = '<div id="pageTypeSpecial">'.$bodyText.'</div>';
		}
		$bodyText = '<div id="page-top"><div id="pageToc">'.$pageToc.'</div><div id="topic"><div id="pageText">'.$bodyText.'</div></div></div>'.$this->afterContent();

		// load the navigation pane from a plugin
		$navText = '';
		DekiPlugin::executeHook(Hooks::SKIN_NAVIGATION_PANE, array($wgTitle, &$navText));

		/* deprecate */ $tpl->set('bodytext', $bodyText);
		/* deprecate */ $tpl->set('sitenavtext', $navText);
		$tpl->set('body', $bodyText);
		$tpl->set('sitenav', $navText);
		
		if ($wgTitle->isEditable() && (Skin::isViewPage() || Skin::isEditPage()) && 0 != $wgArticle->getID()) {
			$filetext = strlen($out->mFilestext) > 0? $out->mFilestext : '<div class="nofiles">&nbsp;</div>';
			$gallerytext = $out->mGallerytext;
			$tagstext = $out->getTags(true /*as a list*/);
			$tagstext = '<div id="tags"><div id="deki-page-tags">'.(
				empty($tagstext) 
					? '<span class="none">'.wfMsg('Article.Common.page-no-tags').'</span>'
					: $tagstext
				).'</div></div>'; //todo: remove pageTags
	
			$relatedtext = Skin::getRelatedPages();
		}
		else {
			$filetext = '<div class="nofiles">&nbsp;</div>';
			$gallerytext = $relatedtext = $tagstext = '';
		}
		/* deprecate */ $tpl->set('filestext', $filetext);
		/* deprecate */ $tpl->set('gallerytext', $gallerytext);
		/* deprecate */ $tpl->set('tagstext', $tagstext );
		$tpl->set('tagsinline', '<div id="deki-page-tags">'.$out->mTagstext.'</div>' );
		/* deprecate */ $tpl->set('relatedtext', $relatedtext );
		$tpl->set('files', $filetext);
		$tpl->set('gallery', $gallerytext);
		$tpl->set('tags', $tagstext);
		$tpl->set('related', $relatedtext);
		
		//-----------------------------------------------------//
		$tpl->set('searchaction', $this->escapeSearchLink());
		$tpl->set('search', trim($wgRequest->getVal('search')));
		$tpl->set('pageisrestricted', $wgArticle->isRestricted());
		
		//overrides from content
		$targets = $out->getTargets();
		foreach ($targets as $key => $val) 
		{
			//special case; we've already done some magical formatting around toc
			if ($key == 'toc') 
			{
				continue;
			}
			$tpl->set($key, $val);
		}
		
		//overrides from LocalSettings.php
		$this->setTemplateOverrides($tpl);
		
		$res = $tpl->execute();
		
		// result may be an error
		$this->printOrError( $res );
	}

	function setPageActions(&$out, &$tpl) 
	{
		global $wgArticle, $wgTitle, $wgRequest, $wgUser;
		
		$tocData = $out->getTarget('toc');
		if (empty($tocData) || strcmp(strip_tags($tocData), wfMsg('System.API.no-headers')) == 0) {
			$tpl->set('tocexists', false);
			$this->cssclass->pagetoc = 'disabled';
		}
		else {
			$tpl->set('tocexists', true);
			$this->cssclass->pagetoc = '';
		}
		
		//attach files
		if ($wgArticle->userCanAttach()) {
		    $this->onclick->pageattach = 'return doPopupAttach('.$wgTitle->getArticleId().');';
			$this->cssclass->pageattach = '';
		}
		else {
			$this->onclick->pageattach = 'return false';
			$this->cssclass->pageattach = 'disabled';
		}

		//email page
		if ($wgArticle->userCanEmailPage()) {
			$Title = Title::newFromText(Hooks::SPECIAL_PAGE_EMAIL);
			$this->href->pageemail = $Title->getLocalUrl('pageid='.$wgTitle->getArticleId());
			$this->cssclass->pageemail = '';
		}
		else {
			$this->href->pageemail = '#';
			$this->cssclass->pageemail = 'disabled';
		}

		//edit page
		if ($wgArticle->userCanEdit())
		{
			$this->onclick->pageedit = 'Deki.Plugin.Publish(\'Editor.load\'); return false;';
			$this->href->pageedit = $wgTitle->getLocalUrl('action=edit'.($wgRequest->getVal('redirect') == 'no'? '&redirect=no': ''));
			$this->cssclass->pageedit = '';
		}
		else
		{
			$this->cssclass->pageedit = $this->cssclass->pageattach = $this->cssclass->pagetags = 'disabled';
			$this->onclick->pageedit = 'return false';
			$this->href->pageedit = '#';
			$this->cssclass->pageedit = 'disabled disabled-login';
			
		}
		
		// view page source
		if ( ($wgArticle->isViewPage() || ($wgArticle->getId() > 0)
			&& $wgArticle->getTitle()->getNamespace() != NS_SPECIAL) )
		{
			$this->onclick->pagesource = '';
			$this->href->pagesource = $wgTitle->getLocalUrl('action=source'.($wgRequest->getVal('redirect') == 'no'? '&redirect=no': ''));
			$this->cssclass->pagesource = '';
		}
		else
		{
			$this->onclick->pagesource = 'return false';
			$this->href->pagesource = '#';
			$this->cssclass->pagesource = 'disabled';			
		}
		
		//page properties
		if ($wgArticle->userCanSetOptions())
		{
			$this->href->pageproperties = $this->makeSpecialUrl('PageProperties', 'id='. $wgArticle->getID());
			$this->cssclass->pageproperties = '';
			$this->onclick->pageproperties = '';
		}
		else
		{
			$this->href->pageproperties = '#';
			$this->onclick->pageproperties = 'return false';
			$this->cssclass->pageproperties = 'disabled';
		}		
		
		// tag page
		if ($wgArticle->userCanTag()) {
			$this->onclick->pagetags = 'return doPopupTags(Deki.PageId);';
			$this->cssclass->pagetags = '';
		}
		else {
			$this->onclick->pagetags = 'return false';
			$this->cssclass->pagetags = 'disabled';
		}
		
		//print page
		if ($wgArticle->userCanRead()) {
			$this->cssclass->pageprint = '';
			$this->onclick->pageprint = 'return Print.open(\''.wfEncodeJSHTML($wgTitle->getLocalUrl('action=print')).'\');';
			$this->href->pageprint = $wgTitle->getLocalUrl( 'action=print' );
		}
		else {
			$this->cssclass->pageprint = 'disabled';
			$this->onclick->pageprint = 'return false';
			$this->href->pageprint = '#';
		}

		//restrict page
		if ($wgArticle->userCanRestrict())
		{
			$this->cssclass->pagerestrict = '';
			$this->onclick->pagerestrict = '';
			$st = Title::newFromText('PageRestrictions', NS_SPECIAL);
			$this->href->pagerestrict = $st->getLocalUrl('id='.$wgArticle->getId()); 
		}
		else {
			$this->href->pagerestrict = '#';
			$this->cssclass->pagerestrict = 'disabled';
			$this->onclick->pagerestrict = '';
		}

		//move page
		if ($wgArticle->userCanMove()) {
			$this->cssclass->pagemove = '';
			$this->onclick->pagemove = 'return doPopupRename(Deki.PageId, Deki.PageTitle);';
		}
		else {
			$this->cssclass->pagemove = 'disabled';
			$this->onclick->pagemove = 'return false';
		}

		//delete page
		if ($wgArticle->userCanDelete()) {
			$this->cssclass->pagedelete = '';
			$this->onclick->pagedelete = 'return doPopupDelete(Deki.PageId);';
		}
		else {
			$this->cssclass->pagedelete = 'disabled';
			$this->onclick->pagedelete = 'return false';
		}

		//create a page
		if ($wgArticle->userCanCreate() && !$this->isNewPage()) {
			$this->href->pageadd = $wgTitle->getLocalUrl( 'action=addsubpage' );
			$this->cssclass->pageadd = '';
		}
		else {
			$this->href->pageadd = '#';
			$this->cssclass->pageadd = 'disabled disabled-login';
		}
		
		//main page link
		$mt = Title::newFromText($wgTitle->getText(), DekiNamespace::getSubject($wgTitle->getNamespace()));
		$this->href->pagemain = $mt->getLocalUrl(); //always show link for permalinks
		
		if ($wgArticle->isViewPage()) {
			$this->cssclass->pagemain = 'active';
		}
		else {
			$this->cssclass->pagemain = 'inactive';
		}
		
		//page history
		if ($wgTitle->isEditable()) {
			$this->href->pagehistory = $wgRequest->getVal('action') == 'history' ? '#': $wgTitle->getLocalUrl('action=history');
			$this->cssclass->pagehistory = $wgRequest->getVal('action') == 'history' ? 'disabled': '';
		}
		else {
			$this->href->pagehistory = '#';
			$this->cssclass->pagehistory = 'disabled';
		}
		
		//watchlists
		$this->text->pagewatch = '';
		if ($wgArticle->userCanWatch() && !$wgUser->isAnonymous()) {
			if ($wgTitle->userIsWatching()) {
				$this->text->pagewatch = wfMsg('Article.Common.action-unwatch');
				$this->href->pagewatch = $wgTitle->getLocalUrl( 'action=unwatch' );
			}
			else {
				$this->text->pagewatch = wfMsg('Article.Common.action-watch');
				$this->href->pagewatch = $wgTitle->getLocalUrl( 'action=watch' );
			}
			$this->cssclass->pagewatch = '';
		}
		else {
			$this->href->pagewatch = '#';			
			$this->cssclass->pagewatch = 'disabled';
		}
		
		if (!isset($this->href->pagedraft)) 
		{
			$this->href->pagedraft = '#'; 	
		}
		
		if (!isset($this->cssclass->pagedraft)) 
		{
			$this->cssclass->pagedraft = ''; 	
		}
		
		if (!isset($this->onclick->pagedraft)) 
		{
			$this->onclick->pagedraft = ''; 	
		}

		//talk link
		$ntt = Title::newFromText($wgTitle->getText(), DekiNamespace::getTalk($wgTitle->getNamespace()));
		$this->href->pagetalk = !$wgArticle->userCanTalk() ? '#': $ntt->getLocalUrl();
		if (!$wgArticle->userCanTalk()) 
		{
			$this->cssclass->pagetalk = 'disabled';
		}
		else
		{
			$this->cssclass->pagetalk = DekiNamespace::isTalk($wgTitle->getNamespace()) ? 'active': 'inactive';
		}

		$this->onclick->pageemail = '';
		$this->onclick->pagetalk = '';
		$this->onclick->pageadd = '';
		$this->onclick->pagewatch = '';
		$this->onclick->sitetools = 'return menuPosition(\'menuInfo\', this, 0, 5);';
		$this->onclick->pagemore = 'return menuPosition(\'pageMenuContent\', this, 0, -23);';
		$this->onclick->pagetoc = 'return showToc(this);';
		$this->onclick->pagebacklinks = 'return menuPosition(\'menuBacklink\', this, -2, 0, true);';

		# Define hrefs for common operations
		$this->href->pageattach = '#';
		$this->href->pagemove = '#';
		$this->href->pagedelete = '#';
		$this->href->pagetoc = '#';
		$this->href->pagetags = '#';
		$this->href->pagepdf = $wgArticle->getAlternateContent('application/pdf');
		
		# if prince is not installed, empty hrefs are returned
		if (empty($this->href->pagepdf)) 
		{
			$this->href->pagepdf = '#';
			$this->cssclass->pagepdf = 'disabled';	
		}
		else
		{
			$this->cssclass->pagepdf = '';	
		}
	}
	
	/***
	 * Values in LocalSettings.php can override your template variables
	 */
	function setTemplateOverrides(&$tpl) {
		global $wgTemplateOverrides;
		foreach ($wgTemplateOverrides as $key => $val) {
			$tpl->set($key, $val);
		}
	}

	/**
	 * Output the string, or print error message if it's
	 * an error object of the appropriate type.
	 * For the base class, assume strings all around.
	 *
	 * @param mixed $str
	 * @access private
	 */
	function printOrError( &$str ) {
		echo $str;
	}

	/**
	 * @return int - the number of HTML_AREAS defined for this template
	 */
	private function getAreaCount()
	{
		$class = get_class($this);
		$constant = $class.'::HTML_AREAS';
		return defined($constant) ? constant($constant) : 0;
	}
}

/**
 * Generic wrapper for template functions, with interface
 * compatible with what we use of PHPTAL 0.7.
 */
class QuickTemplate {
	/**
	 * @access public
	 */
	function QuickTemplate() {
		$this->data = array();
	}

	/**
	 * @access public
	 */
	function set( $name, $value ) {
		$this->data[$name] = $value;
	}

	/**
	 * @access public
	 */
	function setRef($name, &$value) {
		$this->data[$name] =& $value;
	}

	/**
	 * @access public
	 */
	function setTranslator( &$t ) {
		$this->translator = &$t;
	}

	/**
	 * @access public
	 */
	function execute() {
		echo "Override this function.";
	}
	
	/**
	 * Perform any needed dashboard customizations, disabling of template variables, etc.
	 * @access public
	 */
	function setupDashboard()
	{
		return;
	}

	/**
	 * @access private
	 */
	function text( $str ) {
		if (isset($this->data[$str])) {
			echo htmlspecialchars( $this->data[$str] );
		}
	}


	/**
	 * @access private
	 */
	function textString( $str ) {
		if (isset($this->data[$str])) {
			return htmlspecialchars( $this->data[$str] );
		}
	}

	/**
	 * @access private
	 */
	function html( $str ) {
		if (isset($this->data[$str])) {
			echo $this->data[$str];
		}
	}

	/**
	 * @access private
	 */
	function msg( $str ) {
		echo htmlspecialchars( wfMsg( $str ) );
	}

	function msgHtml( $str ) {
		echo wfMsg( $str );
	}

	function haveData( $str ) {
		return $this->data[$str];
	}
	
	function hasData($str) {
		return !empty($this->data[$str]);
	}

	function haveOnClick($str) {
		$sk = $this->data['skin'];
		return $sk->onclick->$str;
	}

	function haveCssClass($str) {
		$sk = $this->data['skin'];
		return $sk->cssclass->$str;
	}

	function haveHref($str) {
		$sk = $this->data['skin'];
		return $sk->href->$str;
	}

	function haveMsg( $str ) {
		return wfMsg($msg);
	}
		
	// these are a list of the new functions to be used
	function get($str) {
		return $this->data[$str];
	}
	function exists($key, $strict = false) {
		if ($strict) {
			return !is_null($this->data[$key]);
		}
		return !empty($this->data[$key]);
	}
	function css($str) {
		return $this->haveCssClass($str);
	}
	function href($str) {
		return $this->haveHref($str);
	}
	function onclick($str) {
		return $this->haveOnClick($str);
	}
}
} // end of if( defined( 'MINDTOUCH_DEKI' ) )
?>
