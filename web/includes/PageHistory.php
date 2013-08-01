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
 * @todo document
 * @package MediaWiki
 */
class PageHistory {
	var $mArticle, $mTitle, $mSkin;
	var $lastline, $lastdate;
	var $linesonpage;
	// saves the state for checked radios
	protected $secondChecked = false;
	
	function PageHistory( $article ) {
		$this->mArticle =& $article;
		$this->mTitle =& $article->mTitle;
	}

	# This shares a lot of issues (and code) with Recent Changes

	function history() {
		global $wgUser, $wgOut, $wgLang, $wgRequest;
		global $wgLimitCount;

		$fname = 'PageHistory::history';
		$wgOut->setArticleFlag( false );
		$wgOut->setArticleRelated( true );
		$wgOut->setRobotpolicy( 'noindex,nofollow' );

		if( $this->mTitle->getArticleID() == 0 ) {
			$wgOut->addHTML( wfMsg('Article.History.no-versions-for-page') );			
			return;
		}

		$limit = $wgLimitCount;
		$offset = $wgRequest->getVal('offset', 0);
		
		$namespace = $this->mTitle->getNamespace();
		$title = $this->mTitle->getText();
		$pageId = $this->mTitle->getArticleId();
		
		// need to get the display title
		$Article = new Article($this->mTitle); 
		$wgOut->setPageTitle( wfEncodeTitle($Article->getDisplayTitle()) );
		
		// handle revision hiding
		$Request = DekiRequest::getInstance();
		if ($Request->isPost())
		{
			$revchange = $Request->getVal('revchange');
			@list($action, $revisionId) = explode(':', $revchange, 2);
			$pageId = $Article->getID();
			
			switch ($action)
			{
				case 'show':
					$Result = DekiPageRevision::show($pageId, $revisionId);
					//$successKey = 'Article.History.revisions.show';
					break;
				case 'hide':
					$Result = DekiPageRevision::hide($pageId, $revisionId);
					//$successKey = 'Article.History.revisions.hide';
					break;
				default:
					$wgOut->redirect($this->mTitle->getFullURL());
					return;
			}
			
			if ($Result->handleResponse())
			{
				//DekiMessage::success(wfMsg($successKey));
			}
			else
			{
				DekiMessage::error($Result->getError());
			}
			
			// redirect to the page history listing
			$wgOut->redirect($this->mTitle->getFullURL('action=history&offset='.$offset.'&limit='.$limit));
			return;
		}
		
		// munge offset into page
		$page = floor($offset/$limit)+1;
		$revisions = DekiPageRevision::getPageRevisions($pageId, $page, $limit+1);
		$revisionCount = 0;
		
		// update the revision counts from API result set
		if (!empty($revisions))
		{
			$revisionCount = count($revisions);
			if ($revisionCount < $limit)
			{
				$this->linesonpage = $revisionCount;
			}
			else
			{
				$this->linesonpage = $revisionCount - 1;
			}
		}
		
		$atend = ($revisionCount < $limit);

		$this->mSkin = $wgUser->getSkin();
		$numbar = wfViewPrevNext($offset, $limit, $this->mTitle->getPrefixedText(),	'action=history', $atend);
		
		$html = $numbar;
		if ($revisionCount > 0) 
		{
			$submitpart1 = '<input class="historysubmit" type="submit" title="'. wfMsg('Article.History.submit-revision-comparison-tip') .'" value="'.wfMsg('Article.History.submit-revision-comparison').'"';
			$this->submitbuttonhtml1 = $submitpart1 . ' />';
			$this->submitbuttonhtml2 = $submitpart1 . ' id="historysubmit" />';
		}
		$html .= $this->beginHistoryList();
		
		// display the history entries
		$lineNumber = 1;
		foreach ($revisions as $Revision)
		{
			$html .= $this->revisionLine($Revision, $lineNumber, ($page == 1 && $lineNumber == 1));
			$lineNumber++;
		}
		
		$html .= $this->endHistoryList(!$atend);
		$html .= $numbar;
		$wgOut->addHTML($html);
	}

	function beginHistoryList() {
		global $wgTitle, $wgRequest;
		$this->lastdate = $this->lastline = '';

		// preserve paging for rev hiding
		$offset = $wgRequest->getInt('offset', 0);
		$limit = $wgRequest->getInt('limit', 50);
		
		$s = '<p>' . wfMsg('Article.History.select-versions-to-compare', wfMsg('Article.History.submit-revision-comparison')) . '</p>';
		$s .= '<form action="' . $wgTitle->escapeLocalURL('action=history&offset='.$offset.'&limit='.$limit) . '" method="post">';
		$s .= '<input type="hidden" name="title" value="'.htmlspecialchars( rawurldecode( $wgTitle->getPrefixedDbkey() ) ).'" />';
		$s .= '<input type="hidden" name="action" value="historysubmit" />';

		$s .= !empty($this->submitbuttonhtml1) ? $this->submitbuttonhtml1."\n":'';
		$s .= '<div class="table"><table border="0" cellspacing="0" cellpadding="0" class="table" id="pagehistory" width="100%">'."\n"
				.'<colgroup span="6"><col width="12%" /><col width="24%" />'
				.'<col width="19%" /><col width="33%" /><col width="8%" /></colgroup>'
				.'<tr><th>'. wfMsg('Article.History.header-compare') .'</th><th>'. wfMsg('Article.History.header-view-version') .'</th><th>'. wfMsg('Article.History.header-edited-by') .'</th>'
				.'<th>'. wfMsg('Article.History.header-edit-summary') .'</th>';
		
		// revision hiding
		$s .= '<th>&nbsp;</th>';
		$s .= '</tr>'."\n";

		return $s;
	}

	function endHistoryList( $skip = false ) {
		$last = wfMsg('Article.History.abbreviation-last');

		$s = $skip ? '' : preg_replace( "/!OLDID![0-9]+!/", $last, $this->lastline );
		$s .= '</table></div>';
		$s .= !empty($this->submitbuttonhtml2) ? $this->submitbuttonhtml2 : '';
		$s .= '</form>';
		return $s;
	}
	
	/**
	 * @param DekiPageRevision $Revision - revision details for the history line
	 * @param int $line - counter for the current line, allows alternating class assignment
	 * @param bool $headRevision - true if the revision being displayed is head
	 */
	public function revisionLine(DekiPageRevision $Revision, $line = 0, $headRevision = false)
	{
		global $wgUser, $wgLang;
		// used for messaging optimization
		static $message;

		if (!isset($message))
		{
			$messages = array(
				'cur' => 'abbreviation-view-version',
				'last' => 'abbreviation-last',
				'selectolderversionfordiff' => 'select-older-comparison',
				'selectnewerversionfordiff' => 'select-newser-comparison',
				'minoreditletter' => 'abbreviation-minor-edit'
			);

			foreach ($messages as $key => $msg)
			{
				$message[$key] = wfMsg('Article.History.'. $msg);
			}
		}
		
		// td class
		// TODO: refactor listing into domtable
		$columnClass = $line % 2 ? 'bg1' : 'bg2';
			
		$row = '';
		
		$revisionId = $Revision->getId();
		if ($Revision->isHidden())
		{
			if (!$wgUser->canAdmin())
			{
				// user cannot see the current revision
				return;
			}
			// short, hidden revision listing
			$row = '<td class="'. $columnClass .'">&nbsp;</td>';
			
			// Revision hidden by USER on DATE
			$username = $Revision->getUsername('hidden');
			$date = $Revision->getDate('hidden');
			$User = $Revision->getUser('hidden');
			$UserTitle = $User->getUserTitle();

			$ul = $this->mSkin->makeLinkObj($UserTitle, $User->toHtml());
			$dt = $wgLang->timeanddate($date, true);
			
			$row .= '<td colspan="3" class="'. $columnClass .'">'. wfMsg('Article.History.revision.hidden', $ul, $dt) .'</td>';

			// revision hiding
			$hideUrl = $this->mTitle->getFullURL('revision='.$revisionId);
			$label = wfMsg('Article.History.revision.show');
			$button = DekiForm::singleInput('button', 'revchange', 'show:'.$revisionId, array(), $label);
			$row .= '<td class="'. $columnClass .'">'. $button .' '. DekiRequest::getInstance()->getVal('revchange') . '</td>';
		}
		else
		{
			// revision listing
			$arbitrary = '';
			if ($this->linesonpage > 1)
			{
				$checkmark = '';
				if ($headRevision)
				{
					$arbitrary = '<input type="radio" style="visibility:hidden" name="revision" value="'.$revisionId.'" title="'.$message['selectolderversionfordiff'].'" />';
					$checkmark = ' checked="checked"';
				}
				else
				{
					// default check the second diff
					if ($line >= 2 && !$this->secondChecked)
					{
						$checkmark = ' checked="checked"';
						$this->secondChecked = true;
					}

					$arbitrary = '<input type="radio" name="revision" value="'.$revisionId.'" title="'.$message['selectolderversionfordiff'].'"'.$checkmark.' />';
					$checkmark = '';
				}
				
				$arbitrary .= '<input type="radio" name="diff" value="'.$revisionId.'" title="'.$message['selectnewerversionfordiff'].'"'.$checkmark.' />';
			}
			$row = '<td class="'. $columnClass .'">' . $arbitrary . '</td>';
			
			// revision date
			$dt = $wgLang->timeanddate($Revision->getDate(), true);
			if ($headRevision)
			{
				$curlink = $this->mSkin->makeKnownLinkObj(
					$this->mTitle, $dt, '', '', '', '', true, true
				);
			}
			else
			{
				$curlink = $this->mSkin->makeKnownLinkObj(
					$this->mTitle, $dt, 'revision='.$revisionId, '', '', '', true, true
				);
			}
			$row .= '<td class="'. $columnClass .'">' . $curlink . '</td>';
		
			// edited by
			$User = $Revision->getUser();
			$username = $Revision->getUsername();
			if (is_null($User))
			{
				$contribsPage =& Title::makeTitle(NS_SPECIAL, 'Contributions');
				$ul = $this->mSkin->makeKnownLinkObj(
					$contribsPage,
					htmlspecialchars($username),
					'target='.urlencode($username)
				);
			}
			else
			{
				$Title = $User->getUserTitle();
				$ul = $this->mSkin->makeLinkObj($Title, $User->toHtml());
			}
			$row .= '<td class="'. $columnClass .'">'. $ul .'</td>';
			
			// edit summary
			$comment = $Revision->getComment();
			if (!empty($comment) && '*' != $comment)
			{
				$comment = $this->mSkin->formatcomment($comment, $this->mTitle);
				$row .= '<td class="'. $columnClass .'">'. $comment .'</td>';
			}
			else
			{
				$row .= '<td class="'. $columnClass .'">&nbsp;</td>';	
			}

			if (!$headRevision && $wgUser->canDelete())
			{
				// revision hiding
				$hideUrl = $this->mTitle->getFullURL('revision='.$revisionId);
				$label = wfMsg('Article.History.revision.hide');
				$button = DekiForm::singleInput('button', 'revchange', 'hide:'.$revisionId, array(), $label);
				$row .= '<td class="'. $columnClass .'">'. $button .' '. DekiRequest::getInstance()->getVal('revchange') . '</td>';
			}
			else
			{
				// blank cell
				// TODO: hide entire column when user doesn't have delete
				$row .= '<td class="'. $columnClass .'">&nbsp;</td>';	
			}
		}
		
		return '<tr>' . $row . '</tr>';
	}
}