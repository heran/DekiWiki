<?php
/*
 * MindTouch Deki - enterprise collaboration and integration platform
 * Copyright (C) 2006-2008 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
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
 
	define('DEKI_MOBILE',true);
	require_once('index.php');

	class MobileComment extends DekiController {

		protected $name = 'comment';

		public function index()
		{
			$this->View->includeCss('comment.css');
			$this->executeAction('listing');
		}

		public function listing()
		{
			
			$this->user = DekiUser::getCurrent();
			$this->View->set('user', $this->user->getUsername());
			$this->View->set('head.title', 'MindTouch Deki Mobile | Comment');
			$this->View->set('search.action',$this->Request->getLocalUrl('search',null,null));
			$this->View->set('form.action', $this->getUrl(null,null,true));


			if($this->Request->getVal('comment'))
				echo $this->postComment();
			else if($this->Request->getVal('delete'))
				echo $this->deleteComment();
			else if($this->Request->getVal('showcomments'))
				echo $this->getAndSetComments();
			else
				$this->Request->redirect('index.php');
		}

		function setActionBar()
		{//XXXX unused, delete

			$cancelUrl = $this->Request->getVal('returnTo');
			if(!$cancelUrl)
			{
				if($title= $this->Request->getVal('title', false))
				{
					$cancelUrl = $this->Request->getLocalUrl('page',null,array('title' => $title));
				}
				else if($idnum = $this->Request->getVal('idnum',false))
				{
					$cancelUrl = $this->Request->getLocalUrl('page', null,array('idnum' => $idnum));
				}
			
			}
			
			$cancelDiv = '<div class="cancel"><a href="'. $cancelUrl . '">Cancel</a></div>';


			$addDiv = '<div class="send"><button type=submit>Add </button></div>';
			$redirect = '<input type="hidden" name="returnTo" value="' . $cancelUrl .	'&showcomments=1"></input>';
			$this->View->set('nav.top.sub', $cancelDiv . $addDiv . $redirect);
		}

		function setCommentInputBox()
		{			
		
			if($text = $this->Request->getVal('text'))
				$inputbox = '<input class="comment" type="text" name="comment" value="'. $text .'"></input>';
			else
				$inputbox = '<input class="comment" type="text" name="comment" value="Touch here to add a comment..."></input>';
			$this->View->set('input.comment', $inputbox);
		 
		}

		function getAndSetComments()
		{
			$pageId = $this->getPathName();
			$result = $this->Plug->At('pages', $pageId, 'comments')->get();

			$commentsData = $result->getVal('body/comments');
			$commentDiv = ''; 
			if($commentsData['@count'] == 0) 
			{
				$this->View->set('comments',$commentDiv . '<div class="commentlist">None so far. Add a comment! </div>');
				return;
			}
	
			$comments = "<div class='commentlist'>";
			if($commentsData['@count'] == 1)
			{
					$comments .= '<div class="comments" id="' . $commentsData['comment']['number']	. '">' 
										. $this->formattedComment($commentsData['comment']) . "</div>\n";
			}
			else
			{
				for($i = 0; $i < $commentsData['@count']; $i++)
				{
				 						$comments .= '<div class="'.	($i % 2 == 0 ? 'comments' : 'white') 
											. '" id="' . $commentsData['comment'][$i]['number']	 . '">' 
											. $this->formattedComment($commentsData['comment'][$i]) . '</div>\n';
				}
			}
			if($comments)
				$comments = $commentDiv . $comments .'</div></div>';
			if($this->Request->getVal('ajax'))
			{
				return $comments;
			}
			else
			{
				$this->View->set('comments',$comments);
			}
		}

		protected function formattedComment($commentdata)
	 	{
		  $c = " <li class=\"datetime\">".date('F jS, Y h:i:s A', wfTimestamp(TS_UNIX, $commentdata['date.posted'])). "</li>\n";
		  $c .= ' <li class="username"><a href="' .$this->getUrl(null, array('title' => 'User:' . $commentdata['user.createdby']['username']), false) . '">'.$commentdata['user.createdby']['username']. '</a></li>';

		  $c .= '<li class="text">';
		  $c .= $commentdata['content']['#text'];
		  $c .= '</li>' . "\n";
		  if ($this->user->getUsername() == $commentdata['user.createdby']['username'])
		  {
			$c .= '<li class="delete">';
			$c .= $this->deleteCommentForm($commentdata);
			$c .= '</li>';
		  }

		  return "<ul>\n" . $c . "</ul>\n";
		 }



		protected function deleteCommentForm($commentdata)
		{
			$deleteForm = '<form class="deleteCommentForm" method=post action="'. $this->Request->getLocalUrl('comment',null,null) . '">';
			$deleteForm .= '<input type="hidden" name="idnum" value="' . $commentdata['page.parent']['@id'] . '"></input>';
			$deleteForm .= '<input type="hidden" name="commentnumber" value="' . $commentdata['number'] . '"></input>';
			$deleteForm .= '<input type="hidden" name=ajax value="true"></input>';
			$deleteForm .= '<input class="delete" type="submit" value="Delete" name="delete"></input>';
			$deleteForm .= '</form>';
			return $deleteForm;

		}



		protected function getPathName()
		{
			if($title = $this->Request->getVal('title',false))
				$pageid = '=' . urlencode(urlencode(DekiTitle::convertForPath($title)));
			else if($idnum = $this->Request->getVal('idnum',false))
				$pageid= $idnum;
			return $pageid;
		}
 

		public function deleteComment()
		{
			$pageid = $_POST['idnum'];
			$commentnum = $_POST['commentnumber'];

			$result = $this->Plug->At('pages',$pageid,'comments',$commentnum)->Delete();

			if($result->isSuccess())
			{
					echo '200 ' . $commentnum;
			}
			else
			{
				echo 'error deleting commentnum' . $commentnum . ' for pageid:' . $pageid;
			}

		

		}

		public function postComment()
		{
			// check if user is logged in?
			$text = $_POST['comment'];

			if($this->Request->getVal('idnum'))
			{
				$pageid = DekiTitle::getPathName($this->Request->getVal('idnum'), 'idnum');
			}
			else if ($this->Request->getVal('title'))
			{
				$pageid = DekiTitle::getPathName($this->Request->getVal('title'), 'title');
			}
			else
			{
				echo 'Error';
				exit;
			}
			$result = $this->Plug->at('pages' ,$pageid, 'comments');
			$result = $result->SetHeader('Content-Type', 'text/plain; charset=utf-8');
			$result = $result->Post($text);
			if(!$result->isSuccess())
			{
				$result->debug();
				echo $result->getVal('body/error/status');
			}
			else
			{
				$count = $this->Plug->At('pages', $pageid, 'comments')->get();
				$countData = $count->getVal('body/comments');
				$commentData = $result->getVal('body/comment');

				echo '<div class="' . ($countData['@count'] %2 != 0 ? 'comments' : 'white') 
								.'" id="' . $commentData['number'] . '">'	
								. $this->formattedComment($commentData)	. '</div>';

			}
			exit;
		}
 }	 
	

	new MobileComment;
