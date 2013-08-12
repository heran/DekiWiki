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


	class MobilePage extends DekiController
	{// handles all page display: contents, comments, tags

		protected $name = 'page';
		protected $user;
		protected $pageSize = 5000; //characters
		protected $datetimeformat = 'm/d/y h:i A'; //formatting dates

		protected $pageName = '';	 // pageid - either a title or id number
		protected $idType = '';		 // title or idnum

		protected $datePrefix = 'date:';

		public function index()
		{
			$this->user = DekiUser::getCurrent();
			$this->View->set('form.search', $this->Request->getLocalUrl('search',null,null));
			$this->View->set('head.title', 'MindTouch Deki Mobile');
			$this->executeAction('listing');
		}

		public function listing()
		{
			$this->setPageIdentifiers();
			$pageId = DekiTitle::getPathName($this->pageName, $this->idType);

			if($this->Request->getVal('ajax'))
			{
				$this->ajaxHandler($pageId);
				exit();
			}

			$this->View->set( 'returnTo', $this->getUrl(null,null,true));
			$this->displayPage($pageId);

			$this->View->set('comments.link','comments.php?');
			$this->View->output();
		}

		protected function ajaxHandler($pageId)
		{
			if ($this->Request->getVal('lastModified'))
			{
				$result2 = $this->Plug->At('pages', $pageId)->get();
				echo $result2->getVal('body/page/date.modified');
				return;
			}

			$tab = ($this->Request->getVal('tab') ? $this->Request->getVal('tab') : 'contents'); 
			if (!$tab || $tab == 'contents')
			{
				echo $this->getAndSetContent($pageId);
				echo $this->getAndSetComments($pageId);
				// echo $this->getAndSetShare($pageId);
				echo $this->commentInputBox($pageId);
				return;
			}
			// images and files
			else if ($tab == 'files')
			{
				echo $this->getAndSetFiles($pageId);
				return;
			}
			// related pages
			else if ($tab == 'related')
			{
				if ($this->Request->getVal('addtag'))
				{
					echo $this->getAddTagForm($pageId);
				}
				else if ($this->Request->getVal('taglist'))
				{
					echo $this->addTags();
				}
				else
				{
					echo $this->getAndSetRelatedPages($pageId);
				}

				return;
			}
		}
	

		protected function setSearchBar()
		{
			$searchbar = 
			'<form method="get" action="'. $this->Request->getLocalUrl('search', null,null) .'">
					<input class="search" type="search" name="searchterm" value=""> </input> 

					<button name="submit" value="submit" class="search">Go</button>
				</form>';
			return $searchbar;
 
		}

		protected function setTabBar()
		{
		
			$items = array('contents'=> array('href' =>	$this->getUrl(null, array('tab' => 'contents'), true), 'label' => 'Content'), 
										 'files' => array('href' => $this->getUrl(null, array('tab' => 'files'), true), 'label' => 'Files'), 
										'related' => array('href' => $this->getUrl(null, array('tab' => 'related'), true), 'label' => 'Related'));


			$tab = ($this->Request->getVal('tab') ? $this->Request->getVal('tab') : 'contents'); 
			$tabBar = '<ul>';
			foreach($items as $id => $attr)
			{
				if($id == $tab)
				{
					$tabBar .= '<li id="' . $id . '"><a id="' . $id . '" class="remote selected" href="' . $attr['href'] .'">' 
									. $attr['label'] . '</a></li>';
				}
				else
				{
					$tabBar .= '<li id="'. $id . '" class="short"><a id="' . $id . '" class="remote" href="' 
									. $attr['href'] .'">' . $attr['label'] . '</a></li>';
				}
			}
			$tabBar .= '</ul>';

			$this->View->set('nav.tabs', $tabBar);
		}


		protected function setSubNav()
		{
			$this->View->set('nav.top.sub',$this->setSearchBar());
		}

		protected function setNavBottom($pageId, $pageFound=true)
		{
			$this->View->set('pageUrl', $this->getUrl(null, null, true));
		}


		protected function setNavTop()
		{
		}

		protected function displayPage($pageId)
		{
			if(strncmp('User:', $this->Request->getVal('title'), 5) == 0)
			{
				$this->View->set('selectUserPage' ,'selected');
			}
			 // navigation
			$this->setNavTop();
			$this->setSubNav();
			$this->setTabBar();
			$this->setNavBottom($pageId);

			$tab = $this->Request->getVal('tab');

			if($tab == 'files')
			{
				$this->View->set('content', $this->getAndSetFiles($pageId));
			}
			else if($tab == 'related')
			{
				$this->View->set('content', $this->getAndSetRelatedPages($pageId));
			}
			else
			{		
				$this->View->set('content', $this->getAndSetContent($pageId));
				$this->View->set('comments', $this->getAndSetComments($pageId));
				$this->View->set('comments-input', $this->commentInputBox($pageId));
				$this->View->set('share', $this->getAndSetShare($pageId));
			}
		}


		protected function makeLink($href,$name)
		{ //will fix this when the api can do internal links properly
 //		 return '<a href="' . $href . '">' .$name . '</a>';
			return '<a href="page.php?idnum=' . $href. '">' .$name . '</a>';
		}

		protected function walkParents($parents, &$parentArray)
		{
			if(!array_key_exists('page.parent', $parents))
			{
				$parentArray[] = $this->makeLink($parents['@id'], $parents['title']);
				return;
			}
			else{
				$parentArray[] = $this->makeLink($parents['@id'], $parents['title']);
				$this->walkParents($parents['page.parent'], $parentArray);
			}
		}

		protected function getAndSetBreadCrumbs($pageId)
		{
			$result = $this->Plug->At('pages',$pageId)->get();

			$breadCrumbs = "";
			if($result->isSuccess())
			{
				$parents = $result->getVal('body/page/page.parent');
				$title = $result->getVal('body/page/title');
				$href = $result->getVal('body/page/@href');
				$parentArray = array();
				if($parents)
					$this->walkParents($parents,$parentArray);

				$parentArraySize = sizeof($parentArray);
				if($parentArraySize > 1)
				{
					$breadCrumbs = $parentArray[$parentArraySize - 1] . ' > ... > ';
					$breadCrumbs .= $parentArray[0] . ' > ' ;
					$breadCrumbs .= '<strong>' .$title . '</strong>';
				}
				else if($parentArraySize == 1)
				{
					$breadCrumbs = $parentArray[$parentArraySize - 1];
					$breadCrumbs .= ' > <strong>' .$title . '</strong>';
				}
				else
				{
					$breadCrumbs = '<strong>' .$title.'</strong>';
				}

			}
			return $breadCrumbs; 
			//$this->View->set('breadcrumbs',$breadCrumbs);
		}

		protected function searchAndReplaceImages($content, $imglist)
		{
			if($imglist == 'none') return $content;
			if($imglist == 'all')
			{
				
			} else {
				$regex = '/<img[^>]+src\s*=\s*"([^"?]*)"[\w\s=]*(\/>|>[\w\s]*<\/img>)/i';
				//preg_match_all($regex, $content, $matches, PREG_PATTERN_ORDER);
				$patterns[0] = '/(<img[^>]+src\s*=\s*")([^"?]*)("[\w\s=]*)(\/>|>[\w\s]*<\/img>)/i';
				// replace with a thumbnail + link to original size if link
				// replace with a link to thumbnail o.w.
				$replacements[0] = '$1$2?size=thumb$3$4\n';
				return preg_replace($patterns, $replacements, $subject);

			}

		}


		protected function formattedContent($title,$contents)
		{
			// contents[0] : actual text; contents[1]: toc
			$toc = '<div class="toc">';
			if(strcmp($contents[1]['#text'], '<em>No headers</em>') != 0)
			{// add the table of contents
				$toc .=	$contents[1]['#text'] ; 
			 // echo '(' . $contents[1]['#text'] .')';
			}
				//add links to files,images,comments,tags
			$toc .= '<ol>';
			if (!$this->user->isAnonymous())
			{
				 $toc .= '<li><a href="' . $this->Request->getLocalUrl('note', 'compose/share/'. $this->pageName . '/' .  $this->idType)  . '">Share This Page</a></li>';
			}
				 $toc .= '<li><a href="#comment ">'
							. (strncmp($title, 'User:', 5) == 0 ? 'Notes' : 'Comments')
							.'</a></li>';

			$toc .= '</ol></div>'; 
				
			$this->View->set('pagetitle',$title);

			$entry =	$toc . $contents[0];
			return $entry;
		}

		protected function getAndSetContent($pageId)
		{
			$Result = $this->Plug->At('pages',$pageId,'contents')->get();
			if ($Result->is(401))
			{
				// user is not authorized to access this page
				$this->View->set('isRestricted', true);
				return '<div class="restricted">You must log in to view this page.</div>';
			}
			else if (!$Result->isSuccess())
			{
				return 'The page "' . $pageId . '" does not exist.';
			}

			// set the page contents
			$content = $Result->getVal('body/content');

			$meta = $this->Plug->At('pages', $pageId)->Get();
			$title = $meta->getVal('body/page/title');
			$this->View->set('head.title', 'MindTouch Deki Mobile | ' . $title);
			$this->setTabBar();

			$breadCrumbs = '<div class="breadcrumbs">' . $this->getAndSetBreadCrumbs($pageId) . '</div>';
			$contents = '<div class="contents">' . $breadCrumbs . $this->formattedContent($title, $content['body']) . '</div>';

			return $contents;
		}


		function commentInputBox($pageId)
		{
			if($this->user->getUsername() == 'Anonymous')
			{
				$inputBox = '<form method="post" class="js-login-addcomment" action="login.php">'
									. '<input type="hidden" name="notification" value="You must login to use that feature."></input>'
									.' <input type="hidden" name="returnTo" value="' . $this->pageUrl() . '#addcomment"></input></form><div class="addcomment">
				<textarea id="comment"	 class="comment disabled" readonly name="addcomment" value="You must login to comment"></textarea>';
				$submitButton = '<button id="ajaxbutton" type=submit> Add </button></div>';
				
				return $inputBox . $submitButton;
			}
			$commentForm ='<a id="addcomment"></a><div class="addcomment"><form class="addCommentForm" action="comment.php" method="post">';
			$inputbox = '<textarea onfocus="clearValue(this);" class="comment" id="comment" name="comment">'."\n\n".'Touch here to add a comment...</textarea>';

			$submitButton = '<button id="ajaxbutton" type=submit> Add </button>';

			
			$commentForm .= '<input type="hidden" name="'
									 .($this->Request->getVal('title') ? 'title' : 'idnum')
									 . '" value="' . ($this->Request->getVal('title') ?	$this->Request->getVal('title') : ($this->Request->getVal('idnum') ? $this->Request->getVal('idnum') : 'home')) . '"></input>';

			$commentForm .= '<input type="hidden" name="ajax" value="true"></input>';
			$commentForm .= $inputbox . $submitButton. '</form></div>';
			return $commentForm; 
		}

		
		protected function getAndSetComments($pageid)
		{ 
			$result = $this->Plug->At('pages', $pageid,'comments')->get();
			$commentsData = $result->getVal('body/comments');
			$commentDiv = '<div class="toggle"><a name="commentlist" id="commentHeading" class="toggleLink" href="'
											.'#comments">'

											. ( (strncmp($pageid, '=User:',5) == 0) ? 'Notes ' : 'Comments ')
											. '<span id="commentcount">('
											. $commentsData['@count'] .')</a></div>'; 

			$comments = '<div id="commentlist" class="expand">';
			if($commentsData['@count'] == 1)
			{
		$comments .= '<div class="comments" id="' . $commentsData['comment']['number'] . '">' 
										. $this->formattedComment($commentsData['comment']) . "</div>\n";
			}
			else
			{
				for($i = 0; $i < $commentsData['@count']; $i++)
				{
						$comments .= '<div class="' . ($i % 2 == 0 ? 'comments' : 'white') . '" id="' 
											. $commentsData['comment'][$i]['number'] . '">'
											. $this->formattedComment($commentsData['comment'][$i]) . '</div>';
				}
			}
			
			if($comments)
				$comments = $commentDiv . $comments . '</div>';
			return $comments;
			
		}

		protected function deleteCommentForm($commentdata)
		{
			$deleteForm = '<form class="deleteCommentForm" method=post action="'. $this->Request->getLocalUrl('comment',null,null) . '">';
			$deleteForm .= '<input type=hidden name=idnum value="' . $commentdata['page.parent']['@id'] . '"></input>';
			$deleteForm .= '<input type=hidden name=commentnumber value="' . $commentdata['number'] . '"></input>';
			$deleteForm .= '<input type=hidden name=ajax value="true"></input>';
			$deleteForm .= '<input class=delete type=submit value=Delete name=delete></input>';
			$deleteForm .= '</form>';
			return $deleteForm;

		}

		 protected function formattedComment($commentdata)
		 {
			$c = " <li class=\"datetime\">".date($this->datetimeformat, wfTimestamp(TS_UNIX, $commentdata['date.posted'])). "</li>";
			$c .= ' <li class="username"><a href="' .$this->getUrl(null, array('title' => 'User:' . $commentdata['user.createdby']['username']), false) . '">'.$commentdata['user.createdby']['username']. '</a></li>';
			
			$c .= ' <li class="text">';
			$c .= $commentdata['content']['#text'];
			$c .= '</li>';
			if($this->user->getUsername() == $commentdata['user.createdby']['username'])
			{
				$c .= '<li class="delete">';
				$c .= $this->deleteCommentForm($commentdata);
				$c .= '</li>';
			}

		  return "<ul>\n" . $c . "</ul>\n";
		 }

		protected function getAndSetFiles($pageid)
		{
			$result = $this->Plug->At('pages',$pageid,'files')->get();
			$fileData = $result->getVal('body/files');
			if($fileData['@count'] == 0)
				return '<div class="no-files"> No files. </div>';
			if($fileData['@count'] == 1)
			{
				$link = 'title="' . $fileData['file']['contents']['@href'] . '"'; // thumbnail
				return '<div class="file action" ' . $link . ' id="fullSizeImg" >' . $this->formattedFile($fileData['file']) . '</div>';
			}
			else
			{
				$fileList = '';
				for($i = 0; $i < $fileData['@count']; $i++)
				{
					$link = 'title="' . $fileData['file'][$i]['contents']['@href'] . '"'; // thumbnail
					$fileList .= '<div id="fullSizeImg" ' . $link .	' class="file action' . ($i % 2 != 0 ? ' gray"' : '"') . '>' 
										. $this->formattedFile($fileData['file'][$i])
										. '</div>';
				}
				return	$fileList;
			}
			
		}



	// -- File display functions
	protected function formattedFile($fileData)
	{
		$dateCreated = date($this->datetimeformat, wfTimestamp(TS_UNIX, $fileData['date.created']));
		$fileName = $fileData['filename'];
		$link = '<a href="' . $fileData['contents']['@href'] . '">' .$fileName . '</a>'; // thumbnail
		$createdBy = $fileData['user.createdby']['username'];
 
    $fileHtml = '<div class="size">' . wfFormatFileSize($fileData['contents']['@size']) . '</div>'
              . '<div class="filename">' . $link . '</div>'
              . '<div class="addedby"><strong>Added by: </strong>' . $createdBy . '</div>'
              . '<div class="datetime">' . $dateCreated . '</div>';
    return $fileHtml;
  }


	// -- Tag display functions
	protected function formattedTag($tag)
	{
			return '<span class="title">' . $tag['title'] . '</span><br>';
					
	} 

	protected function addTagButton()
	{
		if (!$this->user->isAnonymous())
		{
			return '<button class="addtag action" id="addTag"><strong>+ </strong> Add a Tag</button>';

		}
		else
		{
			unset($_GET['ajax']);
			return '<form class="js-login-tags" method="post" action="./login.php">'
						.'<input type="hidden" name="returnTo" value="' . $this->getUrl(null,null,true) . '"></input>'
						.'<input type="hidden" name="notification" value="You must log in to use that feature."></input>'

						. '<button class="addtag disabled"><strong>+ </strong> Add a Tag</button>'
						.'</form>'
						;
		}
	}

	protected function rewriteTagUri($uri, $tagTitle)
	{
		if($special = strstr($uri,'Special:Tags'))
		{
			return $this->Request->getLocalUrl('search', null, array('searchterm' => 'tag:' . $tagTitle ));
		}
		else if($special = strstr($uri, 'Special:Events'))
		{
			
			$date = substr( $uri, strpos($uri,'from=') + 5);
			return $this->Request->getLocalUrl('search', null, array('searchterm' => 'tag:date:' . $date ));
		}
		else
			return $uri;
	}

	/***
	 * Take a date tag, and store it as a date tag in the database
	 */
	function convertToDBDateTag($tag) 
	{
		$timestamp = $this->parseDateTagToTimestamp($tag);
		if ($timestamp === false) {
			return null;
		}
		return date('Y-m-d', $timestamp);	
	}


	/***
	 * Convert a date tag to a timestamp
	 */
	function parseDateTagToTimestamp($tag) 
	{
		if ($tag == $this->datePrefix) {
			return mktime();
		}
		$tag = substr($tag, strlen($this->datePrefix), strlen($tag));
		return strtotime($tag);
	}


	function isDateTag($tag) 
	{
		if (substr($tag, 0, strlen($this->datePrefix)) != $this->datePrefix) {
			return false;
		}
		if (is_null($this->convertToDBDateTag($tag))) {
			return false;
		}
		return true;
	}


	protected function getAndSetTags($pageid)
	{
		$result = $this->Plug->At('pages',$pageid,'tags')->get();
		$tagHtml = '';
		if($result->isSuccess())
		{
			$tags = $result->getVal('body/tags');

			$tagHtml = '<div class="' . ($tags['@count'] == 0 ? 'no-tags' : 'tags' ) . '">'
			. $this->addTagButton()
			.'<div class="down"><a class="toggleLink" name="js-tags" href="#js-tags">Tags</a></div><ul id="js-tags">'; 


			if($tags['@count'] == 0)
			{
				$tagHtml .= '<li class="no-tags">No tags.</li>';
			}
			else if($tags['@count'] == 1)
			{
				$tagHtml .=	'<li class="js-tags gray" title="' . $this->rewriteTagUri($tags['tag']['uri'], $tags['tag']['title']) . '">' . $this->formattedTag($tags['tag']) . '</li>';
			}
			else
			{
				for($i = 0; $i < $tags['@count']; $i++)
				{
					
					$tagHtml .= '<li class="js-tags ' . ( $i % 2 == 0 ?	'gray"' : '"')	
										. ' title="' 
										. $this->rewriteTagUri($tags['tag'][$i]['uri'], $tags['tag'][$i]['title']) . '"'
									 .'>' . $this->formattedTag($tags['tag'][$i]) . '</li>';
				}

			}
			$tagHtml .= '</ul></div>';
		}
		return $tagHtml ;
	}

	protected function getPageSummary($idnum)
	{//unused, but keeping around in case things suddenly change again
		$result = $this->Plug->At('pages', $idnum)->Get();
		$summary = '';
		if($result->isSuccess())
		{
			$summary = $result->getVal('body/page/summary');
		}
		return $summary;
	}

	protected function uniqueArrayById($pages)
	{
		$ids = array();
		$uniquePages = array();
		foreach($pages as $page)
		{
			if(!array_key_exists($page['@id'], $ids ))
			{
				$uniquePages[] = $page;
				$ids[$page['@id']] = true;
			}
		}
		return $uniquePages;

	}

	protected function getAndSetRelatedPages($pageid)
	{
		$tagsResult = $this->Plug->At('pages',$pageid,'tags')->get();

		$inboundResult = $this->Plug->At('pages', $pageid)->get();
		$related = array();
		if($inboundResult->isSuccess())
		{
			// combine inbound links and related pages links into one page
			$inbounds = $inboundResult->getVal('body/page/inbound/page');
			if($inbounds)
			{
				if(array_key_exists('@id', $inbounds))
				{
					$related = array($inbounds);
				}
				else
				{
					$related = $inbounds;
				}
			}
		}
		if($tagsResult->isSuccess())
		{
			$tags = $tagsResult->getVal('body/tags');
			if($tags['@count'] == 1)
			{
				if(isset($tags['tag']['related']))
					if($tags['tag']['related']['@count'] == 1)
						 array_push($related, $tags['tag']['related']['page']);
					else
					{
						 $related = array_merge($related, $tags['tag']['related']['page']);

					}
			}
			else if($tags['@count'] > 1)
			{
				foreach($tags['tag'] as $tag)
				{
					if(isset($tag['related'])) {
						if($tag['related']['@count'] == 1)
						{
							array_push($related, $tag['related']['page']);
						}
						else
						{
							$related = array_merge($related, $tag['related']['page']);
						}
					}
				}
			}
		}
		$related = $this->uniqueArrayById($related);

		$pageDiv = '<div class="' . (sizeof($related) > 0 ? 'pages' : 'no-pages') . '"><div class="down"><a name="js-pages" class="toggleLink" href="#js-pages">Pages</a></div>';
		$pageDiv .= '<ul id="js-pages">';
		if (sizeof($related) == 0) 
		{
			$pageDiv .= '<li class="no-pages">No related pages.</li>';
		}

		foreach($related as $key => $page)
		{
			$url = $this->getUrl(null, array('idnum' => $page['@id']), false);
			$class = 'pages' . ($key % 2 == 0 ? ' gray' : '');			

			$pageDiv .= '<li class="'. $class .'" title="'. $url . '">';
			
				$pageDiv .= '<a href="'. $url .'">'. $page['title'] .'</a>';

			$pageDiv .= '</li>';
		}

		$pageDiv .= '</ul></div>';
		return	$pageDiv . $this->getAndSetTags($pageid);// . $this->getAddTagForm($pageid);
	}


	protected function getAddTagForm($pageId)
	{
		$tagResults = $this->Plug->At('pages', $pageId, 'tags')->Get();
		$tagStr = '';
		if($tagResults->isSuccess())
		{
			$tags = $tagResults->getVal('body/tags');
			if($tags['@count'] == 1)
			{
				$tagStr = $tags['tag']['@value']; 
			}
			else if($tags['@count'] > 1)
			{
			
				$tagTitles = array();
				foreach($tags['tag'] as $key => $tag)
				{
					$tagTitles[] = $tag['@value'];
				}
				$tagStr = join("\n", $tagTitles);
			}
		}
		$atf = '<div class="contents"><div class="tagging"><form class="tagForm" method="post" action="page.php">'
					.'<input type="hidden" name="ajax" value="true"></input>'
					.'<input type="hidden" name="idnum" value="' . $pageId . '"></input>'
					.'<input type="hidden" name="tab" value="related"></input>'
					.'<input type="hidden" name="returnTo" value="' . $this->getUrl(null,null,true) . '"></input>'
				 . 'Enter tags on separate lines:'
				 . '<textarea name="taglist">' . $tagStr . '</textarea>'
				 . '<button>Add</button>'
				 . '<a href="'. $this->getUrl(null, array('idnum'=> $pageId, 'tab'=>'related')) .'" class="cancel">Cancel</a>'
				 . '</form></div>';
		return $atf;

	}

	protected function addTags()
	{
 
		$text = $this->Request->getVal('taglist');
		$tags = split("\n",$text);
		foreach($tags as $tag)
		{
			if($this->isDateTag($tag))
			{
				$tag = $this->datePrefix . $this->convertToDBDateTag($tag);
			}
			if(empty($tag)){
				continue;
			}
			$insert[] = array('@value' => trim($tag));
		}
		$pageId = $this->Request->getVal('idnum');
		$result = $this->Plug->At('pages',$pageId, 'tags')->Put(array('tags' => array('tag' => $insert)));
		if($result->isSuccess())
		{
			echo 'Success';
		}
		else
			echo 'Error';
	}

	protected function getAndSetShare($pageId)
	{
		$shareForm = '<a href="' . $this->Request->getLocalUrl('note', 'compose/share/'. $this->pageName . '/' .  $this->idType)  . '">Share This Page</a>';

		return '<div id="share" class="share">' . $shareForm . '</div>';
	}

// -- utilities
	protected function setPageIdentifiers()
	{
			if($this->Request->getVal('idnum'))
			{
				$this->pageName = $this->Request->getVal('idnum');
				$this->idType = 'idnum';
			}
			else if($this->Request->getVal('title'))
			{
				$this->pageName = $this->Request->getVal('title');
				$this->idType = 'title';

			}
			else
			{
				$this->pageName = 'home';
				$this->idType = 'idnum';
			}

	}

	protected function pageUrl()
	{

		$pageUrl = $this->getUrl(null, array($this->pageName => $this->idType), false); 
		return $pageUrl;

	}

}


	new MobilePage();
	
