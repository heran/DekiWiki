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

class Comment {
	var $text = '';
	var $ownerid = 0;
	var $commentnum = 0;
	var $pageid = 0;
	var $isloaded = false;

	function Comment() {
		$this->text = '';
		$this->ownerid = 0;
		$this->commentnum = 0;
		$this->pageid = 0;
		$this->isloaded = false;
	}

	function setText($text) {
		$this->text = $text;
	}
	function setNum($num) {
		$this->commentnum = $num;
	}
	function setPageId($pageid) {
		$this->pageid = $pageid;
	}

	function canEdit() {
		if ($this->commentnum == 0 || $this->ownerid == 0) {
			return false;
		}
		global $wgUser;
		return $wgUser->getId() == $this->ownerid;
	}

	function canDelete() {
		global $wgUser;
		if ($wgUser->isAdmin()) {
			return true;
		}
		return $this->canEdit();
	}
	function delete() {
		if (!$this->isloaded) {
			$this->load();
		}
		if (!$this->canDelete()) {
			wfMessagePush('comment', wfMsg('Article.Comments.cannot-modify-comment'));
			return false;
		}
		global $wgDekiPlug;
		$r = $wgDekiPlug->At('pages', $this->pageid, 'comments', $this->commentnum)->Delete();
		if (!MTMessage::HandleFromDream($r)) {
			return false;
		}
		return true;
	}

	/***
	 * static, does the markup generation for the comment, given an array from the API
	 */
	function format($comment, $forreply = false) {
		global $wgUser, $wgLang, $wgTitle;
		$sk = $wgUser->getSkin();

		//load in comment class for canEdit and canDelete perms
		$Comment = new Comment;
		$Comment->load($comment);
		$canEdit = $Comment->canEdit();
		$canDelete = $Comment->canDelete();

		$commentanchor = 'comment'.$comment['number'];

		$dateDeleted = wfArrayVal($comment, 'date.deleted');
		// deleted comments need placeholder
		if (!is_null($dateDeleted))
		{
			$Admin = DekiUser::newFromArray($comment['user.deletedby']);
			return '<div class="comment comment-deleted" id="'.$commentanchor.'"><div class="comment-deleted">'.
				   wfMsg('Article.Comments.comment-was-deleted', $comment['number'], $Admin->toHtml()).
				   '</div></div>';
		}

		$Author = DekiUser::newFromArray($comment['user.createdby']);
		$UserTitle = $Author->getUserTitle();
		$linkuser = $sk->makeLinkObj($UserTitle, $Author->toHtml());

		$date = $wgLang->timeanddate($comment['date.posted'], true);

		$htmlaction = '';
		if ($canEdit || $canDelete) {
			$htmlaction = '<div class="commentActions">';
			if ($canEdit) {
				$htmlaction .= '<a href="'.$wgTitle->getLocalUrl('action=comment&commentnum='.$comment['number']).'" '
					.'onclick="return MTComments.EditComment(\''.$comment['number'].'\');">'.wfMsg('Article.Comments.edit').'</a> ';
			}
			if ($canDelete) {
				$htmlaction .= '<form method="post" class="commentDelete" action="'.$wgTitle->getLocalUrl('action=comment').'">'
					.'<input type="submit" class="commentDelete" value="'. wfMsg('Article.Comments.submit-delete') .'" onclick="return MTComments.DeleteComment(\''.$comment['number'].'\');"/>'
					.'<input type="hidden" name="wpCommentDelete" value="'.$comment['number'].'" />'
					.'</form>';
			}
			$htmlaction.= '</div>';
		}

		$htmlcommentby = wfMsg('Article.Comments.header-comment-meta', $linkuser);
		$htmlupdated =
			!is_null(wfArrayVal($comment, 'date.edited'))
				? ' <span class="commentUpdated">'
					.wfMsg('Article.Comments.edited', $wgLang->timeanddate($comment['date.edited'], true))
					.'</span>'
				: '';

		$text = wfArrayVal($comment, 'content/#text');
		$contenttype = wfArrayVal($comment, 'content/@type');
		
		//special handling for plain-text formats
		if (strncmp(strtolower($contenttype), 'text/plain', strlen('text/plain')) == 0) {
			$text = trim(nl2br(htmlspecialchars($text)));
		}
		else 
		{
			return '<div class="comment"><div class="comment-deleted">'.wfMsg('Article.Comments.unknown', $contenttype, $comment['number']).'</div></div>';
		}

		//format the comment
		return '<div class="comment" id="'.$commentanchor.'">'
			.'<div class="commentNum">'
				.'<a href="'.$wgTitle->getLocalUrl().'#'.$commentanchor.'">'.wfMsg('Article.Comments.number', $comment['number']).'</a>'
			.'</div>'
			.'<div class="commentText">'
				.$htmlaction
				.'<div class="commentMetaData"><span>'
					.$htmlcommentby
				.'</span></div>'
				.'<div id="commentTextForm'.$comment['number'].'"></div>'
				.'<div class="commentContent" id="commentText'.$comment['number'].'">'
					.$text
					.$htmlupdated
					.'<div class="commentPosted">'
						.wfMsg('Article.Comments.posted-date', $date)
					.'</div>'
				.'</div>'
			.'</div>'
			.'<div class="br"></div>'
			.'</div>';
	}
	/***
	 * $r
	 */
	function load($r = null) {
		if ($this->isloaded) {
			return;
		}
		if ($this->commentnum == 0  && is_null($r)) {
			return;
		}
		if (is_null($r)) {
			global $wgDekiPlug;
			$r = $wgDekiPlug->At('pages', $this->pageid, 'comments', $this->commentnum)->Get();
			if (!MTMessage::HandleFromDream($r)) {
				return;
			}
			$this->text = wfArrayVal($r['body'], 'comment/content/#text');
			$this->ownerid = wfArrayVal($r['body'], 'comment/user.createdby/@id');
		}
		else {
			$this->commentnum = wfArrayVal($r, 'number');
			$this->text = wfArrayVal($r, 'content/#text');
			$this->ownerid = wfArrayVal($r, 'user.createdby/@id');
		}
		$this->isloaded = true;
	}

	function save() {
		if (trim($this->text) == '') {
			wfMessagePush('comment', wfMsg('Article.Comments.didnt-input-comment'));
			return false;
		}

		global $wgDekiPlug;

		if ($this->commentnum == 0) {
			$r = $wgDekiPlug->At('pages', $this->pageid, 'comments');
			$r = $r->SetHeader('Content-Type', 'text/plain; charset=utf-8');
			$r = $r->Post($this->text);
		}
		else {
			if (!$this->canEdit()) {
				wfMessagePush('comment', wfMsg('Article.Comments.cannot-modify-comment'));
				return false;
			}
			$r = $wgDekiPlug->At('pages', $this->pageid, 'comments', $this->commentnum, 'content');
			$r = $r->SetHeader('Content-Type', 'text/plain; charset=utf-8')->Put($this->text);
		}

		//post comment, if successful, redirect
		if (MTMessage::HandleFromDream($r)) {
			wfMessagePush('comment', $this->commentnum > 0 ? wfMsg('Article.Comments.edit-success') : wfMsg('Article.Comments.post-success'), 'success');
			return true;
		}
	}
}

class CommentPage {
	var $mTitle;

	var $save = false, $preview = false;
	var $commenttext = '';
	var $reply = null;
	var $ignoreWebRequest = false;
	var $commentnum = 0;

	function CommentPage( $title = null ) {
		$this->mTitle = $title;
	}

	/***
	 * public entry point, called for action=comment
	 */
	function comment() {
		global $wgRequest, $wgOut;

		//entry point for deleting comments
		if ($wgRequest->getVal('wpCommentDelete') > 0) {
			$this->commentnum = htmlspecialchars($wgRequest->getVal('wpCommentDelete'));
			$this->delete();
			wfMessagePush('comment', wfMsg('Article.Comments.delete-success'), 'success');
			$anchor = '#comments';
			$wgOut->redirect($this->mTitle->getLocalUrl().$anchor);
			return;
		}

		//set the commentnum if specified for viewing/editing; this is used to load data in the form, or to put instead of post
		if ($wgRequest->getVal('commentnum') ) {
			$this->commentnum = htmlspecialchars($wgRequest->getVal('commentnum'));
		}

		//manually override loading importFormData(), useful for ajax calls
		if ($this->ignoreWebRequest === false) {
			$this->importFormData( $wgRequest );
		}

		if ( $this->save ) {
			$wgOut->addHTML($this->commentForm('save'));
		} else {
			$wgOut->addHTML($this->commentForm('initial'));
		}
	}

	function delete() {
		$Comment = new Comment;
		$Comment->setPageId($this->mTitle->getArticleId());
		if ($this->commentnum > 0) {
			$Comment->setNum($this->commentnum);
			$Comment->load();
		}
		return $Comment->delete();
	}

	/***
	 * populates class variables
	 */
	function importFormData( &$request ) {

		if( $request->wasPosted() ) {
			$this->commentnum = $request->getVal('wpCommentNum');
			$this->commenttext = $request->getText('wpComment');
			$this->save = true;
		} else {
			# Not a posted form? Start with nothing.
			$this->commenttext  = '';
			$this->reply = null;
			$this->save = false;
		}
	}

	/***
	 * shows the comment form and either saves or shows it
	 */
	function commentForm($formtype = 'initial') {
		global $wgOut, $wgUser;

		$wgArticle = new Article($this->mTitle);
		$html = '';

		//if we can comment, let's show the form
		if ($wgArticle->userCanComment()) {
			if ($formtype == 'save') 
			{
				$this->save();
				$anchor = ($this->commentnum > 0) ? '#comment'.$this->commentnum: '#comments';
				$wgOut->redirect($this->mTitle->getLocalUrl().$anchor);
				return;
			}
			elseif ($formtype == 'initial') 
			{
				$html .= wfMessagePrint('comment'); //output success message
				if ($this->commentnum > 0) {
					$c = new Comment;
					$c->setPageId($this->mTitle->getArticleId());
					$c->setNum($this->commentnum);
					$c->load();
					$this->commenttext = $c->text;
				}
			}

			$formId = ( $this->commentnum > 0 ) ? 'commentEditForm' : 'commentAddForm';
			$html .= '<form method="post" id="' . $formId .  '" class="commentForm" action="'.$this->mTitle->escapeLocalURL( 'action=comment').'">';
			if ($this->commentnum > 0) {
				$html .= '<input type="hidden" name="wpCommentNum" id="wpCommentNum" value="'.$this->commentnum.'" />';
				$cancel = '<a href="'.$this->mTitle->getLocalUrl().'#comments" id="commentCancel'.$this->commentnum.'">'. wfMsg('Article.Comments.cancel') .'</a>';
			}
			else {
				$cancel = '';
			}
			$html .= '<div class="commentHeader">'.wfMsg($this->commentnum > 0 ? 'Article.Comments.header-edit-comment': 'Article.Comments.header-add-comment').'</div>'
				.'<div><textarea class="commentText" name="wpComment">'.$this->commenttext.'</textarea></div>'
				.'<div><input type="submit" class="commentSubmit" name="commentSubmit" value="'.wfMsg($this->commentnum > 0 ? 'Article.Comments.header-edit-comment': 'Article.Comments.header-add-comment').'" name="submit" /> '.$cancel.'</div>'
				.'</form>';
		}
		else {
			$sk = $wgUser->getSkin();
			$st = Title::newFromText('Userlogin', NS_SPECIAL);
			$key = $wgUser->isAnonymous() ? 'must-login-to-post' : 'must-use-authorized-account';
			$html .= '<div class="commentForm">'.
					 wfMsg('Article.Comments.' . $key,
						   '<a href="'. $sk->makeLoginUrl() .'">'.
								wfMsg('Article.Comments.must-login-to-post-link') .
						   '</a>') .'</div>';
		}

		return $html;
	}

	/***
	 * Actually sends the comment to the API for saving and pushes success/error messages out
	 */
	function save() {
		$Comment = new Comment;
		$Comment->setPageId($this->mTitle->getArticleId());
		if ($this->commentnum > 0) {
			$Comment->setNum($this->commentnum);
			$Comment->load();
		}
		$Comment->setText($this->commenttext);
		return $Comment->save();
	}

	function submit() {
		$this->save = true;
		$this->comment();
	}

	/***
	 * Replying to specific comments: unsupported feature
	 */
	function isReply() {
		return !is_null($this->getReply());
	}
	function getReply() {
		return $this->reply;
	}
	function setReply($reply) {
		$this->reply = $reply;
	}
	
	function format($commentcount = null)
	{
		global $wgOut, $wgDekiPlug, $wgCommentCount, $wgCommentCountAll, $wgRequest;
	
		// no commenting feature
		if ($wgCommentCount == 0 || $this->mTitle == null || $this->mTitle->getArticleID() == 0)
		{
			return;
		}

		$html = '';
		$this->commentnum = 0;
		$this->commenttext = '';
		
		//if not passed directly into the function call
		if (is_null($commentcount))
		{
			$commentcount = $wgRequest->getVal('commentcount') == 'all' ? $wgCommentCountAll: $wgCommentCount;
		}

		if (strcmp($commentcount, 'all') == 0)
		{
			$commentcount = $wgCommentCountAll;
		}
	
		if ($commentcount > 0 || is_null($commentcount))
		{
			$r = $wgDekiPlug->At('pages', $this->mTitle->getArticleID(), 'comments')->With('sortby', '-date.posted')->With('limit', $commentcount)->With('redirects', 0);
			if ($commentcount == $wgCommentCountAll)
			{
				$r = $r->With('filter', 'any');
			}
			$r = $r->Get();
			
			if (!MTMessage::HandleFromDream($r, array(403)))
			{
				return;
			}
	
			//comment form
			$count = count(wfArrayValAll($r['body'], 'comments/comment'));
			$total = wfArrayVal($r['body'], 'comments/@totalcount');
			$comment = wfArrayVal($r['body'], 'comments');
			$comments = wfArrayValAll($comment, 'comment');
			$thisurl = $this->mTitle->getFullUrl();
			$replyto = htmlspecialchars($wgRequest->getVal('replyto'));
		}
	
		//no comments, then return nothing
		if ((empty($count) && isset($count)) || !isset($comments) || (isset($comments) && count($comments) == 0))
		{
			return $this->commentForm();
		}
	
		$areMoreComments = $total > $count;
		$viewall = '';
	
		//set comment count to skinning variable
		$wgOut->setCommentCount($total); //skinning variable
	
		if ($count != $total)
		{
			$viewall = '<a href="' . $this->mTitle->getLocalUrl('commentcount=all') . '#comments'
				. '" onclick="return MTComments.GetComments(\'all\');" id="commentViewAll">'
				. wfMsg('Article.Comments.view-all') . '</a>';
		}
		else
		{
			$viewall = wfMsg('Article.Comments.view-all');
		}
		
		$viewall = '<div class="commentMore">'
			. wfMsg('Article.Comments.viewing-comments', $count, $total, $total - $count) . ' '
			. $viewall . '</div>';
	
		$html .= $viewall;
		// API returns reverse chron; we want to display chron
		$comments = array_reverse($comments);
	
		foreach ($comments as $comment)
		{
			$html .= Comment::format($comment);
		}
	
		//create comment form, and set the output page to load comments
		$html = '<div class="comments">' . $html . $viewall . '</div>' . $this->commentForm();
	
		return $html;
	}
}
?>
