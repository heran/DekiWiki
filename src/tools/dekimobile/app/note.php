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
	define('DEKI_MOBILE', true);
	require_once('index.php');

	class MobileNote extends DekiController 
	{
		protected $name = 'note';
		protected $noteSummaryLen = 100;

		public function index($notification=null)
		{// inbox
			if (DekiUser::getCurrent()->isAnonymous())
			{
				$loginUrl = $this->Request->getLocalUrl('login');
				$this->Request->redirect($loginUrl);
				exit();
			}

			$pageId = DekiTitle::getPathName('User:' . DekiUser::getCurrent()->getUsername(), 'title');
			$result = $this->Plug->At('pages', $pageId, 'comments')->With('sortby','-date.posted')->get();

			$inboxEntries = $result->getAll('body/comments/comment');
			$notification = ($notification == 'success'? '<div class="error">Your note was sent successfully. </div>' : '');
			$this->View->set('view.notification', $notification);
			$this->formatNotes($inboxEntries);
			$this->View->setRef('view.notes', $inboxEntries); 
			$this->View->set('view.tab', 'notes');
			$this->View->output();
			
		}

		public function compose($type=null, $id=null, $idtype=null)
		{// covers 'share' and 'reply' as well

			// make sure the user is logged in
			if (DekiUser::getCurrent()->isAnonymous())
			{
				$params = 'compose/'. $type;
				if (!is_null($id))
				{
					$params .= '/' . $id;
					if (!is_null($idtype))
					{
						$params .= '/' . $idtype;
					}
				}
				$returnUrl = $this->getUrl($params);
				
				// add a message so the user knows why they are required to login
				DekiMessage::info('You must login to use that function.');

				$loginUrl = $this->Request->getLocalUrl('login', null, array('returnTo' => $returnUrl));
				$this->Request->redirect($loginUrl);
				exit();
			}

			$this->View->set('view.recipient', '');

			if ($type == 'reply')
			{//$id is commentId
				$this->View->set('view.recv.note', $this->readNote($id));
//				$this->View->set('form.cancelUrl', $this->Request->getLocalUrl('page', null, array($idtype => $id)));
			}
			else if ($type == 'share')
			{//$id is pageId
				// eventually: $this->Request->getLocalUrl('page', 'contents/$id',....)
				if($idtype != null && $id != null)
				{
					$this->View->set('view.send.note', $this->Request->getLocalUrl('page', null, array($idtype => $id)));
				}
				$this->View->set('form.cancelUrl', $this->Request->getLocalUrl('page', null, array($idtype => $id)));
			}
			else
			{
				$this->View->set('form.cancelUrl', $this->getUrl());
			}
			// defaults to blank compose form if both type and id are null

			$this->View->includeJavascript('jquery.autocomplete.js');
			$this->View->includeCss('jquery.autocomplete.css');
			$this->View->includeJavascript('users.autocomplete.js');
			
	
			$this->View->set('view.tab', 'notes');
			$this->View->output();

		}

		public function ajax($type, $user=null)
		{
			if($type == 'send')
			{
				$text = $this->Request->getVal('message');
				$recipient = $this->Request->getVal('to');
				$pageId = DekiTitle::getPathName('User:' . $recipient, 'title');
				$Result = $this->Plug->At('pages', $pageId, 'comments');
				$Result = $Result->SetHeader('Content-Type', 'text/plain; charset=utf-8');
				$Result = $Result->Post($text);
				if (!$Result->isSuccess())
				{
					echo '404 API error in posting note.';
				}
				else
				{// on success return redirect url
					echo $this->getUrl('index/success');
				}
			}
			else if($type == 'checkUser')
			{
				if($user)
				{
					echo $this->checkUser($user);	
				}
				else
				{
					echo '400 No user specified for user check.';
				}
			}
			else
			{
				echo '404 No ajax action specified.';
			}
			exit;
		}

		protected function checkUser($recipient)
		{

			// check if this is an email address
			if (wfValidateEmail($recipient))
			{
				$Result = $this->Plug->At('users')->Get();
			//	$Result->debug();
				$users = $Result->getVal('body/users');
				if ($users['@count'] == 1)
				{
					if ($users['user']['email'] == $recipient)
					{
						return $users['user']['nick'];
					}
				}
				else
				{
					for ($i = 0; $i < $users['@count']; $i++)
					{
						if ($users['user'][$i]['email'] == $recipient)
						{
							return $users['user'][$i]['nick'];
						}
					}
				}


			}
			else 
			{
				$pageId = DekiTitle::getPathName( $recipient, 'title');
				$result = $this->Plug->At('users', $pageId)->Get();


				if($result->isSuccess()) return $recipient;
			}
			return 'false';
		}

		

		protected function readNote($commentId)
		{
			
			$pageId = DekiTitle::getPathName('User:' . DekiUser::getCurrent()->getUsername(), 'title');
			$result = $this->Plug->At('pages', $pageId, 'comments',$commentId)->Get();
			$note = $result->getVal('body/comment');
			$this->View->set('view.recipient', $note['user.createdby']['username']);
			return 
				array
				(
					'date' => date('m/d/y h:i A' ,  wfTimestamp(TS_UNIX, $note['date.posted'])),
					 'from' => $note['user.createdby']['username'],
					 'text' => $note['content']['#text']
				);

		}

		protected function deleteNoteForm($note)
		{
			$deleteForm = '<form class="deleteCommentForm" method=post action="'. $this->Request->getLocalUrl('comment',null,null) . '">';
			$deleteForm .= '<input type="hidden" name="idnum" value="' . $note['page.parent']['@id'] . '"></input>';
			$deleteForm .= '<input type="hidden" name="commentnumber" value="' . $note['number'] . '"></input>';
			$deleteForm .= '<input type="hidden" name=ajax value="true"></input>';
			$deleteForm .= '<input class="delete" type="submit" value="Delete" name="delete"></input>';
			$deleteForm .= '</form>';
			return $deleteForm;

		}
		
		protected function formatNotes(&$notes)
		{
			if (!is_array($notes)) 
			{
				$notes = array();	
			}
			foreach($notes as &$note)
			{
				$note = array(
					'url' => $this->getUrl('compose/reply/' . $note['number']),
					'date' => date('m/d/y h:i A', wfTimestamp(TS_UNIX, $note['date.posted'])) ,
					'from' => $note['user.createdby']['username'],
					'summary' => substr($note['content']['#text'], 0, $this->noteSummaryLen)
				);
			}
			unset($note);
			
			return true;
		}
	


	}

	new MobileNote;
