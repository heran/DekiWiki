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

require_once('gui_index.php');

class CommentsFormatter extends DekiFormatter
{
	protected $contentType = 'application/json';
	protected $requireXmlHttpRequest = true;

	private $Request;

	public function format()
	{
		$this->Request = DekiRequest::getInstance();
		$action = $this->Request->getVal( 'action' );

		switch ( $action )
		{
			case 'show':
				$this->getCommentsHtml();
				break;

			case 'post':
				$this->postComment();
				break;

			case 'edit':
				$this->editComment();
				break;

			case 'delete':
				$this->deleteComment();
				break;

			default:
				header('HTTP/1.0 404 Not Found');
				exit(' '); // flush the headers
		}
	}

	private function getCommentsHtml()
	{
		$titleId = $this->Request->getVal( 'titleId' );
		$commentCount = $this->Request->getVal( 'commentCount' );

		$title = Title::newFromID( $titleId );
		$comments = new CommentPage( $title );

		echo $comments->format( $commentCount );
	}

	private function postComment()
	{
		global $wgOut, $wgTitle;
		
		$titleId = $this->Request->getVal( 'titleId' );
		$comment = $this->Request->getVal( 'comment' );
		$showAll = $this->Request->getVal( 'showAll' );
		$commentNum = $this->Request->getVal( 'commentNum' );

		$nt = Title::newFromID( $titleId );

		//set the global title, since it's used to generate local URLs
		$wgTitle = $nt;

		$Comment = new CommentPage($nt);
		$Comment->ignoreWebRequest = true; //so the form doesn't try to populate from wgRequest
		$Comment->commenttext = $comment; //manually set the comment
		
		if ( !empty($commentNum) )
		{
			$Comment->commentnum = $commentNum;
		}
		$Comment->submit();
		
		// load comments from the API
		// and output the whole comments markup for injection back into the site
		echo $Comment->format( ($showAll == 'true') ? 'all': null );
	}

	private function editComment()
	{
		$this->disableCaching();

		$titleId = $this->Request->getVal( 'titleId' );
		$commentNum = $this->Request->getVal( 'commentNum' );
		
		$nt = Title::newFromId( $titleId );
		$Comment = new CommentPage( $nt );
		$Comment->commentnum = $commentNum;
		echo $Comment->commentForm();
	}

	private function deleteComment()
	{
		$titleId = $this->Request->getVal( 'titleId' );
		$commentNum = $this->Request->getVal( 'commentNum' );
		
		$Comment = new Comment();
		$Comment->pageid = $titleId;
		$Comment->commentnum = $commentNum;
		$Comment->delete();
		global $wgUser;
		echo( '<div class="comment-deleted">' . wfMsg('Article.Comments.comment-was-deleted', $commentNum, $wgUser->getName()) . '</div>');
	}
}

new CommentsFormatter();
