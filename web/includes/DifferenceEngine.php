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
 * See diff.doc
 * @package MediaWiki 
 * @subpackage DifferenceEngine
 */

/**
 * @todo document
 * @access public
 */
class DifferenceEngine {
	/* private */ var $mOldid, $mNewid;
	/* private */ var $mOldtitle, $mNewtitle, $mPagetitle;
	/* private */ var $mOldtext, $mNewtext;
	/* private */ var $mOldUser, $mNewUser;
	/* private */ var $mOldPage, $mNewPage;

	function DifferenceEngine( $old, $new )
	{
		global $wgTitle;
		
		if ( $new == 'head' ) 
		{
			$this->mNewid = 0;			
		}
		$this->mOldid = intval($old);
		$this->mNewid = intval($new);
	}

	function showDiffPage()	
	{
		global $wgUser, $wgTitle, $wgOut, $wgContLang, $wgArticle, $wgRequest;
		$fname = 'DifferenceEngine::showDiffPage';
		
		$t = $wgTitle->getPrefixedText() . " (Diff: {$this->mOldid}, " . "{$this->mNewid})";
		$mtext = wfMsg('Article.History.diff-error-missing-article', $t);

		$wgOut->setArticleFlag( false );
		
		if ( ! $this->loadPageMetadata() ) 
		{
			$wgOut->setPagetitle( wfMsg('Page.Error.page-title') );
			$wgOut->addHTML( $mtext );			
			return;
		}
		
		// need to get the current display title
		$DekiPlug = DekiPlug::getInstance();
		$Result = $DekiPlug->At('pages', $wgArticle->getId(), 'info')->Get();
		$wgOut->setPageTitle( $Result->getVal('body/page/title') );
				
		$wgOut->suppressQuickbar();

		# Set subtitle
		$wgOut->setRobotpolicy( 'noindex,follow' );
		
		$sk = $wgUser->getSkin();
		$contribs = wfMsg('Page.Contributions.page-title');
		
		$oldUserLink = $sk->makeLinkObj( Title::makeTitleSafe( NS_USER, $this->mOldUser ), htmlspecialchars($this->mOldUser) );
		$newUserLink = $sk->makeLinkObj( Title::makeTitleSafe( NS_USER, $this->mNewUser ), htmlspecialchars($this->mNewUser) );
		$oldContribs = $sk->makeKnownLinkObj( Title::makeTitle( NS_SPECIAL, 'Contributions' ), $contribs,
			'target=' . urlencode($this->mOldUser) );
		$newContribs = $sk->makeKnownLinkObj( Title::makeTitle( NS_SPECIAL, 'Contributions' ), $contribs,
			'target=' . urlencode($this->mNewUser) );
			
		$version_link = strtolower($sk->makeKnownLinkObj($wgTitle, wfMsg('Article.Common.version-archive'), 'action=history', '', '', '', true, true));
		if (!$this->mNewid) 
		{
			$rollback = '<form method="post" action="'.$wgTitle->getLocalUrl().'">'
				. DekiForm::singleInput('hidden', 'action', 'rollback')
				. DekiForm::singleInput('hidden', 'revertid', $this->mOldid)
				. wfMsg('Article.History.revert', DekiForm::singleInput('submit', 'submit', wfMsg('Article.History.submit-revert'), array('disabled' => $wgArticle->userCanEdit() ? '': 'disabled')), $version_link)				
				.'</form>';
		} 
		else 
		{
			$rollback = wfMsg('Article.History.revert-must-compare', $version_link);
		}

		$curText = ($this->mNewid == 0) ? wfMsg('Article.History.current'): '';
			
		$wgOut->addHTML($headerText);	
		
		$DekiPlug = DekiPlug::getInstance();
		$DiffResult = $DekiPlug->At('pages', $wgTitle->getArticleId(), 'diff')
			->With('revision', $this->mNewid == 0 ? 'head': $this->mNewid)
			->With('previous', $this->mOldid)
			->With('diff', 'all')
			->Get();
		
		if ( $DiffResult->handleResponse() ) 
		{
			$wgOut->addHTML($rollback);
			
			$basequery = 'diff='.$wgRequest->getVal('diff').'&revision='.$wgRequest->getVal('revision').'&'; 
			
			$banlink = $sk->makeLinkObj(
				Title::newFromText(Hooks::SPECIAL_USER_BAN), 
				wfMsg('Article.History.diff.ban'), 
				'username='.urlencode($this->mOldUser).'&returnto='.urlencode($wgTitle->getLocalUrl($basequery.'action=diff'))
			);
			
			$out = 				
				'<div class="deki-diff">';
			$combinedDiff = $DiffResult->getVal('body/content/combined');
			if(strlen($combinedDiff) > 0) {
				$out = $out
					.'<div class="pageRevision" id="deki-diffcombined">'
					.'<h2 class="deki-title">'.wfMsg('Article.History.diff.combined').'</h2>'
					.'<div class="deki-diffmeta">'. wfMsg('Article.History.comparing-version-modified', $this->mOlddate, $oldUserLink, $curText, $this->mNewdate, $newUserLink) .'</div>'
					. $DiffResult->getVal('body/content/combined') 
					. '</div>';
			}
			$out = $out
				.'<div class="pageRevision" id="deki-diffside">'
				.'<div class="deki-revision" id="deki-diffbefore">' 
				.'<h2 class="deki-title">'.wfMsg('Article.History.diff.before', $this->mOlddatetitle).'</h2>'
				.'<div class="deki-diffmeta">'.wfMsg('Article.History.diff.modifiedby', $oldUserLink, $banlink).'</div>'
				. $DiffResult->getVal('body/content/before') 
				. '</div>'
				
				.'<div class="deki-revision" id="deki-diffafter">' 
				.'<h2 class="deki-title">'.wfMsg('Article.History.diff.after', $this->mPagetitle).'</h2>'
				.'<div class="deki-diffmeta">'.wfMsg('Article.History.diff.modifiedby', $newUserLink, $banlink).'</div>'
				.$DiffResult->getVal('body/content/after') 
				.'</div>'
				.'</div></div>';
		} 
		else 
		{
			$out = '<div class="deki-differror">'. wfMsg('Article.History.diff-error-occurred') .'</div>';
		}
		$wgOut->addHTML( $out );
	}
	
	# Load the text of the articles to compare.  If newid is 0, then compare
	# the old article in oldid to the current article; if oldid is 0, then
	# compare the current article to the immediately previous one (ignoring
	# the value of newid).
	#
	function loadPageMetadata()	
	{
		global $wgTitle, $wgLang;
		
		$id = $wgTitle->getArticleID();
		
		// get the page meta XML
		$DekiPlug = DekiPlug::getInstance();
		$PageResult = $DekiPlug->At('pages', $id)->Get();
		$revcount = $PageResult->getVal('body/page/revisions/@count', 0);
		
		# The get value diff can have an absolute value - see if this abs value is head; 
		# if so, reset to zero; this allows the revert button to show.
		if ($this->mNewid == $revcount) 
		{
			$this->mNewid = 0;	
		}
		
		$DekiPagePlug = $DekiPlug->At('pages', $id);
		
		//if we're viewing against head
		if ( 0 != $this->mNewid && 0 != $this->mOldid ) 
		{
			$DekiPagePlug = $DekiPagePlug->At('revisions')->With('revision', $this->mNewid);
		}
		
		//execute API calls
		$DekiPageResult = $DekiPagePlug->Get();
		if (!$DekiPageResult->handleResponse()) 
		{
			return false;	
		}
		
		//first set the "new" page
		if ( 0 == $this->mNewid || 0 == $this->mOldid ) 
		{			
			$newLink = $wgTitle->escapeLocalUrl();
			
			$this->mNewPage = &$wgTitle;
			$this->mPagetitle = htmlspecialchars( wfMsg('Article.History.current-version') );
			$this->mNewtitle = '<a href="'.$newLink.'">'.$this->mPagetitle.'</a>';			
			$this->mNewdate = '<a href="'.$newLink.'">'.$wgLang->timeanddate( wfTimestamp( TS_MW, $DekiPageResult->getVal('body/page/date.edited')), true ).'</a>';
			$this->mNewUser = $DekiPageResult->getVal('body/page/user.author/username');
		}
		else 
		{
			$t = $wgLang->timeanddate( wfTimestamp( TS_MW, $DekiPageResult->getVal('body/page/date.edited')), true );
			$newLink = $wgTitle->escapeLocalUrl ('revision=' . $this->mNewid);
			
			$this->mNewPage = Title::newFromText( $DekiPageResult->getVal('body/page/title') );
			$this->mPagetitle = htmlspecialchars( wfMsg('Article.Common.version-as-of', $t) );
			$this->mNewtitle = '<a href="'.$newLink.'">'.$this->mPagetitle.'</a>';	
			$this->mNewdate = '<a href="'.$newLink.'">'.$t.'</a>';
			$this->mNewUser = $DekiPageResult->getVal('body/page/user.author/username');
		}
		
		//now, set the "old" page		
		if ( 0 == $this->mOldid ) 
		{
			$this->mOldid = 1;
		}
		$DekiOldPage = $DekiPlug->At('pages', $id, 'revisions')->With('revision', $this->mOldid)->Get();
		
				
		if (!$DekiOldPage->handleResponse()) 
		{
			return false;
		}
		
		$this->mOldPage = Title::newFromText( $DekiOldPage->getVal('body/page/path') );		

		$t = $wgLang->timeanddate( wfTimestamp( TS_MW, $DekiOldPage->getVal('body/page/date.edited')), true ); //todo
		$oldLink = $this->mOldPage->escapeLocalUrl ('revision=' . $this->mOldid);
		$this->mOlddate = '<a href="'.$oldLink.'">'.$t.'</a>';
		$this->mOlddatetitle = $t;
		$this->mOldUser = $DekiOldPage->getVal('body/page/user.author/username');
		
		return true;
	}
}
