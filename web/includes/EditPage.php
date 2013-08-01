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
 * Contain the EditPage class
 * @package MediaWiki
 */

/**
 * Splitting edit page/HTML interface from Article...
 * The actual database and text munging is still in Article,
 * but it should get easier to call those from alternate
 * interfaces.
 *
 * @package MediaWiki
 */

class EditPage {
	var $mArticle;
	var $mTitle;
	var $mNewTitle;
	
	# Form values
	var $save = false, $preview = false;
	var $textbox1 = '', $title = '';
	var $edittime = '', $section = '';
	var $articleid;
	
	/**
	 * @todo document
	 * @param $article
	 */
	function EditPage( $article ) 
	{
		$this->mArticle =& $article;
		global $wgTitle;
		$this->mTitle =& $wgTitle;
	}

	function setSection ( $s ) 
	{
		$this->section = $s;
	}

	/**
	 * This is the function that gets called for "action=edit".
	 */
	function edit()
	{
		global $wgOut, $wgUser, $wgRequest;
		// this is not an article
		$wgOut->setArticleFlag(false);
		$this->importFormData( $wgRequest );
		if ( !$this->mArticle->userCanEdit() && $this->mArticle->getId() > 0 || !$this->mArticle->userCanCreate() && $this->mArticle->getId() == 0 ) 
		{
			return;
		}
		if (!$this->mTitle->canEditNamespace()) 
		{
			wfMessagePush('general', wfMsg('Article.Error.namespace-locked-for-editing'));
			return;
		}	
		if ( $this->save ) 
		{
			$this->editForm( 'save' );
		} else 
		{ 
			$this->editForm( 'initial', true );
		}
	}

	/**
	 * @todo document
	 */
	function importFormData( &$request ) 
	{
		if ( $request->wasPosted() ) 
		{
			# These fields need to be checked for encoding.
			# Also remove trailing whitespace, but don't remove _initial_
			# whitespace from the text boxes. This may be significant formatting.
			$this->textbox1 = rtrim( $request->getText( 'wpTextbox1' ) );
			$this->articleid = $request->getVal('wpArticleId');
			$this->path = $request->getVal('wpPath');
			$this->summary = $request->getVal('wpSummary');

			$this->edittime = $request->getVal( 'wpEdittime' );
			$this->preview = $request->getVal( 'wpPreviewH' ) == '1';
			$this->save    = !$this->preview;
			if( !preg_match( '/^\d{14}$/', $this->edittime )) 
			{
				$this->edittime = null;
			}
			
			$this->mOldArticleId = $request->getVal('wpArticleId');
			
			// get the page title from page
			if ($this->isNewPage())
			{
				$this->title = $request->getVal('new_page_title');
			}
		} 
		else 
		{
			# Not a posted form? Start with nothing.
			$this->textbox1  = '';
			$this->edittime  = '';
			$this->preview   = false;
			$this->save      = false;
			$this->mOldArticleId = 0;
		}

		# Section edit can come from either the form or a link
		$this->section = $request->getVal( 'wpSection', $request->getVal( 'section' ) );

		// allow posted fields to be overridden by a plugin
		if ($request->wasPosted())
		{
			DekiPlugin::executeHook(
				Hooks::EDITOR_PROCESS_FORM,
				array(
					$request->getText('wpTextbox1'), 	// unparsed/raw contents
					&$this->textbox1,					// parsed contents of the page/section
					&$this->title,						// parsed title of the page being edited
					&$this->section,					// section being edited
					&$this->summary,					// edit summary
					&$this->articleid 					// destination articleId
				)
			);
		}
	}
	
	function submit() 
	{
		$this->edit();
	}
	
	/**
	 * Is the user attempting to create a new page?
	 * @return bool
	 */
	public function isNewPage()
	{
		global $wgRequest;
		// determine the article id
		$wpArticleId = is_null($wgRequest->getVal('wpArticleId')) 
			? $this->mArticle->getId()
			: $wgRequest->getVal('wpArticleId');
			
		if ($wgRequest->getVal('subpage') == 'true') 
		{
			$wpArticleId = 0;
		}
		
		return $wpArticleId == 0;
	}

	/**
	 * The edit form is self-submitting, so that when things like
	 * preview and edit conflicts occur, we get the same form back
	 * with the extra stuff added.  Only when the final submission
	 * is made and all is well do we actually save and redirect to
	 * the newly-edited page.
	 *
	 * @param string $formtype Type of form either : save, initial or preview
	 * @param bool $firsttime True to load form data from db
	 */
	function editForm( $formtype, $firsttime = false , $ajax = false ) 
	{
		global $wgOut, $wgUser, $wgLang, $wgContLang, $wgTitle, $wgStylePath, $wgSitename, $wgRequest, $wgArticle;

		$sk = $wgUser->getSkin();
		$saveFailed = false;
		
		//for new pages, the titles have gotta be valid
    	if ('save' == $formtype && $this->articleid == 0) 
    	{
			$this->fulltitle = Article::combineName($this->path, wfEncodeTitle($this->title));
			
			//new pages get saved to the path from their title, which means they need to be validated
	    	$nt = Title::newFromText($this->fulltitle);
	    	if (is_null($nt) || !$nt->isEditable()) 
	    	{
		    	$formtype = '';
				$saveFailed = true;
				if (is_null($nt))
				{
					$titleLength = strlen($this->fulltitle);
					if ($titleLength > 255)
					{
						// provided title is too long
						wfMessagePush('general', wfMsg('Article.Error.title.length', abs(255 - $titleLength)));
					}
					else
					{
						wfMessagePush('general', wfMsg('Article.Error.page-title-contains-illegal-chars'));
					}
		    	}
		    	else
		    	{
		    		wfMessagePush('general', wfMsg('Article.Error.namespace-locked-for-editing'));
		    	}
	    	}
			else if ($nt->getArticleID() > 0)
			{
				$nt = Title::makeTitle($nt->getNamespace(), $this->fulltitle, 0);
			}
    	}
    	
		//if there were no errors with the title change, and it's a save operation
		if ( 'save' == $formtype ) 
		{
			//for new pages
			if ($this->articleid == 0) 
			{
	            // StepanR: Remove old title from breadcrumbs
	            $this->mArticle->removeLinkFromBreadcrumb();

	            //set the page to this new title
	            $wgTitle = $nt;
	            $this->mTitle = $wgTitle;
	            
	            $wgArticle = new Article($wgTitle);
	            $this->mArticle = $wgArticle;

				# Set default content if blank
				if ($this->textbox1 == '')
				{
					$this->textbox1 = wfMsg('Article.Edit.new-article-text', htmlspecialchars($wgSitename));	
				}
				
				// see #0006580
				// $this->mTitle->mArticleID = 0;
				
				// see #0006580
				if (empty($this->title))
				{
					wfMessagePush('general', wfMsg('Article.Error.page-title-is-empty'));
					$saveFailed = true;
				}
				elseif (!$this->mArticle->save( $this->textbox1, null, $this->summary ))
				{
					$saveFailed = true;
				}
				else 
				{
					return;
				}
				
				if ($saveFailed)
				{
					$wgTitle = Title::newFromText($wgRequest->getVal('title'));
					$wgArticle = new Article($wgTitle);
					$this->mTitle = $wgTitle;
					$this->mArticle = $wgArticle;
				}
			}
			else {
				// Tell the article we're updating a section
				$this->mArticle->mTitleText = isset($this->title) ? $this->title: '';
				$this->mArticle->setSection($wgRequest->getVal('wpSection'));
				$this->mArticle->mLoaded = true; //don't load from API; we are setting all values manually
				$this->mArticle->setTimestamp($wgRequest->getVal('wpEdittime') ? $wgRequest->getVal('wpEdittime'): wfTimestampNow());
				if ( $this->mArticle->save( $this->textbox1, (empty($this->section)) ? null : $this->section, $this->summary )) 
				{
					return;
				}
				else 
				{
				  $saveFailed = true;
			  	}
			}
		}
		
		if ( $saveFailed ) 
		{
			$wgOut->setPageTitle( wfMsg('Article.Error.save-failed-for', $this->mTitle->getDisplayText()) );
			$this->edittime = $this->mArticle->getTimestamp();
		}
		
		// First time through - load the content
		if ( 'initial' == $formtype || $firsttime ) 
		{
			if (0 !== $this->mArticle->getID()) 
			{
				$this->mArticle->loadContent('edit', $this->section ? $this->section : null);
				$this->textbox1 = $this->mArticle->getContent( true );
			}
			$this->edittime = $this->mArticle->getTimestamp();
		}
		
		//Tell search engines to ignore ?action=edit pages
		$wgOut->setRobotpolicy( 'noindex,nofollow' );
		
		$q = 'action=submit';
		if ( "no" == $wgRequest->getVal('redirect') || $this->mArticle->mRedirected === true) 
		{ 
			$q .= "&redirect=no"; 
		}
		
		$params = $this->mArticle->getParameters();
		if (!empty($params)) 
		{
			foreach ($params as $k => $v) 
			{
				$q .= '&'.$k.'='.$v;	
			}
		}
		
		// form action
		$formQueryParams = $q;
		// array of html form elements to output
		$formFields = array();
		// stores html to place above the textarea
		$prependHtml = '';
		// stores html to place below the textarea
		$appendHtml = '';
		// stores the raw-unencoded textarea contents
		$forEdit = '';


		//the title object will automatically follow redirects, in the case of it being a redirect, get the redirected page
		if ($this->mArticle->isRedirect()) 
		{
			$this->mTitle = Title::newFromText($this->mArticle->mRedirectedFrom[0]);
		}
		
		/**
		 * Get the page title
		 */
		$displaytitle = wfDecodeTitle(Article::getShortName($this->mTitle->getPrefixedText()));
		if (!empty($this->mArticle->mTitleText)) 
		{
			$displaytitle = $this->mArticle->mTitleText;
		}
		if (empty($displaytitle)) 
		{
			$displaytitle = wfHomePageTitle();
		}
		
		/**
		 * Get the textarea contents
		 */
		$forEdit = $wgContLang->recodeForEdit($this->textbox1);
		
		if (($this->mTitle->getArticleID() == 0 && !$saveFailed) || $wgRequest->getVal('subpage')) 
		{
			global $wgDekiPlug;
			if ($wgRequest->getVal('template')) 
			{
				 $tpTitle = Title::newFromURL( DekiNamespace::getCanonicalName(NS_TEMPLATE).':'.$wgRequest->getVal('template') );
				 
				 $r = $wgDekiPlug->At('pages', '='.urlencode(urlencode($tpTitle->getPrefixedDBkey())), 'contents')->With('mode', 'edit')->With('include', 'true');
				 $params = Article::getParameters();
				 foreach ($params as $key => $val) 
				 {
					$r = $r->With($key, $val);	 
				 }
				 $r = $r->Get();
				 $forEdit = $r['status'] == 200 
				 	? wfArrayVal($r, 'body/content/body')
				 	: wfMsg('Article.Edit.new-article-text', htmlspecialchars($wgSitename));
			}
			else 
			{
				$forEdit = wfMsg('Article.Edit.new-article-text', htmlspecialchars($wgSitename));	
			}
		}
		
		// generate the append & prepend sections
		// two loading animations?
		// TODO: clean this ui experience up
		$prependHtml .= 
			'<p id="formLoading" style="display:none;">'
				.'<img src="'.Skin::getCommonPath().'/icons/anim-wait-circle.gif" alt="" /> '
				.wfMsg('Article.Edit.wait-while-editor-loads')
			.'</p>'
		;

		$prependHtml .= 
			'<div id="quicksavewait" style="display:none;">'
				.'<img src="'.Skin::getCommonPath().'/icons/anim-wait-circle.gif" alt="" /> '
				.'<img src="'.Skin::getCommonPath().'/icons/anim-wait.gif" alt="" />'
			.'</div>'
			.'<div id="quicksavedone" style="display:none;" class="quicksavedone">&nbsp;</div>'
		;

		$prependHtml .=
			'<div id="wpFormButtons" style="display:none;">'
				.'<input type="submit" value="' . wfMsg('Article.Edit.save') . '" name="doSave" />'
				.'<input type="button" value="' . wfMsg('Article.Edit.cancel') . '" name="doCancel" />'
			.'</div>'
		;
	
		$extensionurl = '/deki/gui/extensions.php';
		$appendHtml .= 
			'<div class="deki-extension-list extensionlist">'
				.'<a href="'.$extensionurl.'" target="_blank">'. wfMsg('Article.Edit.extensions-list') .'</a>'
			.'</div>'
		;
		
		$appendHtml .= 
			'<div class="deki-edit-summary">'
				.wfMsg('Article.edit.summary').' <input type="text" name="wpSummary" size="48" />'
			.'</div>'
		;
		
		if ($this->mArticle->mUnsafe) 
		{
			$appendHtml .= 
				'<div class="deki-unsafe-alert">'
					.'<ul>'
						.'<li class="deki-unsafe-message">'. wfMsg('Article.edit.unsafe-message') .'</li>'
						.(!$this->mArticle->userCanScript() ? '<li class="deki-unsafe-warning">'. wfMsg('Article.edit.unsafe-warning') .'</li>': '')
					.'</ul>'
				.'</div>'
			;
		}
		
		/**
		 * Generate the hidden form fields
		 */
		//copied, needs cleanup
        $titleFull = $wgTitle->getPrefixedText();
        $title = Title::newFromText($titleFull);
        if (is_null($title)) 
        {
	        $title = $wgTitle;
	        $titleFull = $title->getPrefixedText();
        }
        Article::splitName($titleFull, $titlePath, $titleName);
        
        if ($titlePath == '')
        {
            $titlePath = HPS_SEPARATOR;
        }
		
        $formFields[] = '<input type="hidden" name="wpPath" value="' . Title::escapeText($titlePath) . '" />';
        //end copied

		// determine the article id
		$wpArticleId = is_null($wgRequest->getVal('wpArticleId')) 
			? $this->mArticle->getId()
			: $wgRequest->getVal('wpArticleId');
			
		if ($wgRequest->getVal('subpage') == 'true') 
		{
			$wpArticleId = 0;
		}
		
		$formFields[] = '<input type="hidden" name="displaytitle" value="'.htmlspecialchars($displaytitle).'" />';
		$formFields[] = '<input type="hidden" value="'.htmlspecialchars($this->section).'" id="wpSection" name="wpSection" />'; 
		$formFields[] = '<input type="hidden" value="'.$this->edittime.'" name="wpEdittime" id="wpEditTime"/>';
		$formFields[] = '<input type="hidden" value="'.$wpArticleId.'" name="wpArticleId" />';
		
		if ( $saveFailed )
		{
			$formFields[] = '<input type="hidden" value="true" name="wpArticleSaveFailed" id="wpArticleSaveFailed" />';
		}
		
		if (!$wgUser->isAnonymous())
		{
			/**
			 * To make it harder for someone to slip a user a page
			 * which submits an edit form to the wiki without their
			 * knowledge, a random token is associated with the login
			 * session. If it's not passed back with the submission,
			 * we won't save the page, or render user JavaScript and
			 * CSS previews.
			 */
			$token = htmlspecialchars($wgUser->editToken());
			$formFields[] = '<input type="hidden" value="'. $token .'" name="wpEditToken" />';
		}
		
		/**
		 * Build the textarea input contents
		 */
		if (empty($this->section))
		{
			// normal editing
			if ($this->isNewPage())
			{
				$newTitle = !empty($this->title) ? $this->title : $displaytitle;
				if ($wgTitle->isTalkPage())
				{
					// Bugfix #8707: It's necessary to deny of changing titles for talk pages 
					// cannot edit titles for talk pages
					$input = '<input type="text" id="deki-new-page-title" value="'. htmlspecialchars($newTitle) .'" disabled="disabled" />';
					$input .= '<input type="hidden" name="new_page_title" value="'. htmlspecialchars($newTitle) .'" />';
				}
				else
				{
					$input = '<input type="text" id="deki-new-page-title" name="new_page_title" value="'. htmlspecialchars($newTitle) .'" />';
				}
				
				// insert the display title form field for new pages
				$formFields[] = 
					'<div class="deki-new-page-title-border">'.
						'<div class="deki-new-page-title">'.
							$input.
						'</div>'.
					'</div>';
			}
		}
		else
		{
			// section editing
		}
		
		// allow editor form generation to be modified
		$textareaContents = $forEdit;
		
		/**
		 * Allow plugins to modify the form output
		 */
		DekiPlugin::executeHook(
			Hooks::EDITOR_FORM,
			array(
				&$textareaContents, // unencoded textarea contents
				$forEdit, 			// source contents
				$displaytitle, 		// source display title
				$this->section, 	// source section
				$this->articleid, 	// source articleId
				// html inside form tag...
				&$prependHtml,		// before textarea
				&$appendHtml		// after textarea
			)
		);

        // add the textarea
		$formFields[] = 
			'<textarea id="editarea" name="wpTextbox1" rows="24" cols="80" style="display:none; width:100%;">'
				. htmlspecialchars($textareaContents)
			.'</textarea>'
		;
		// cleanup
		unset($textareaContents);
		unset($forEdit);
	
		// build the html
		$html = 
			'<div class="b-body">'
				.'<form id="editform"'.($wgRequest->getVal('baseuri') ? ' target="_self"': '').' name="editform" method="post" action="'.$this->mTitle->escapeLocalURL( $formQueryParams, true ).'">'
					.'<fieldset style="border:none; padding:0;">'
						.$prependHtml
						.'<div id="eareaParent">'
							.implode('', $formFields)		
						.'</div>'
						.$appendHtml
					.'</fieldset>'
				.'</form>'
			.'</div>'
		;
		// add the html
		$wgOut->addHTML($html);
	}
}
