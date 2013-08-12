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
 * @deprecated - Do not use these methods. Still utilized for File: namespace
 */
if( !defined( 'ATTACH_PHP' ) ) {
    define( 'ATTACH_PHP', true );

	
	//get a file's info by its fileId
	function wfGetAttachById($attachId) {
		global $wgDekiPlug;
		$r = $wgDekiPlug->At('files', $attachId, 'info')->Get();
		if (MTMessage::HandleAPIResponse($r)) {
			return $r;
		}
		return null;
	}
	
	/***
	 * Action when a files upload form has been submitted
	 */
	function wfFilesAttach() {
        global $wgTitle, $wgUser, $wgRequest;
      
        $articleId = $wgTitle->getArticleId();

        $result = array();
		
        foreach ($_FILES as $userfile => $file)
        {
            if ( ! $wgRequest->getFileName('Filedata') )
            {
                $fileNum = str_replace('file_' , '', $userfile);
                $fileDescription = $wgRequest->getVal('filedesc_' . $fileNum);
            }
            else
            {
                // We can't use other name if we use the SWFUpload
                // because it'll cause the problems with compatibility
                $fileDescription = $wgRequest->getVal('filedescription');
            }
            
            $result[] = wfUploadFile($userfile, $fileDescription, $articleId);            
        }
        return $result;
	}
	
	function wfUploadFile($uploadedFile, $fileDescription, $articleId) {
        global $wgRequest;
        
        $fileName = $wgRequest->getFileName($uploadedFile); //filename of uploaded file
        if ( is_null($fileName) || $fileName === '' )
        {
        	return false;
        }
	    
        if ($wgRequest->getFileError($uploadedFile) > 0 ) {
           MTMessage::Show(wfMsg('Article.Attach.file-exceeds-max-size', ini_get('upload_max_filesize')), $fileName);
           return false;
        }
        
        // fix a bug in PHP where ' gets translated into \' for <input> filenames
        $fileName = str_replace('\\', '', $fileName);
        $fileTempname = $wgRequest->getFileTempname($uploadedFile); //location of temp file
        
        if (filesize($fileTempname) == 0) {
	        MTMessage::Show(wfMsg('Article.Attach.file-has-no-size', $fileName), '');
	        return false;
        }
        $Attachment = new Attachment;
        $Attachment->filename = $fileName;
        $Attachment->filetype = $wgRequest->getFileType($uploadedFile);
        $Attachment->description = $fileDescription;
        return $Attachment->upload($articleId, $fileTempname);
	}
	
	function wfFileRevisions($fileId) {
        global $wgDekiPlug;
        $result = $wgDekiPlug->At('files', $fileId, 'revisions')->Get();
		return wfArrayValAll($result, 'body/files/file');
    }
    
    /***
     * MT royk
     * given a pageID, will return an array of files (includes revisions)
     * @note guerrics: gallery functionality removed
     */
    function wfGetAttachments($pageId = 0) {
	    static $files = false;
	    global $wgArticle;
	    
	    //if we haven't loaded files yet, grab from API
	    if ($files === false) {
		    if ($wgArticle->getId() == 0) {
				$wgArticle = new Article(Title::newFromId($pageId));
			}
			$files = $wgArticle->getFiles();
		}
		
		return $files;
    }

       
    /***
     * Does the HTML markup generation for the files table (does not contain images)
     */
    function wfFileAttachmentTable( $titleId = null ) {
        global $wgUser, $wgTitle, $wgOut, $wgArticle;
		
        if ( !is_null( $titleId ) ) {
	        $nt = Title::newFromID( $titleId );
        }
        else {
	        $nt = $wgArticle->getTitle();
        }
		$html = '<div id="pageFiles">';
		$html .= wfMessagePrint('files'); //success & error messsages
				
        $articleID = $nt->getArticleID();
        $files = wfGetAttachments($articleID, 'files');
        
        $numFiles = count($files);
        $wgOut->setFileCount($numFiles);
        
        $sk = $wgUser->getSkin();
        
        if ($numFiles == 0) {
            $html .= '<div class="nofiles">&nbsp;</div></div>';
            return $html;
        }
        $temp = array();
        
        //loop through and get all revisions if they exist
        foreach ($files as $key => $val) {
            $temp[] = $val;
            if ($val['revisions']['@count'] > 1) {
		        $revisions = wfFileRevisions($val['@id']);
		        if (is_array($revisions) && count($revisions) > 0) {
			        array_pop($revisions);
			        $revisions = array_reverse($revisions);
		            foreach ($revisions as $v) {
			         	$temp[] = $v;   
		            }
	            }
            }
        }
        $files = $temp;
        
		$Table = new DomTable();
		$Table->setColWidths('16', '16', '', '80', '145', '115', '75');
		$Table->addRow();
		$Col = $Table->addHeading(wfMsg('Article.Attach.table-header-file'), 3);
		$Table->addHeading(wfMsg('Article.Attach.table-header-size'));
		$Table->addHeading(wfMsg('Article.Attach.table-header-date'));
		$Table->addHeading(wfMsg('Article.Attach.table-header-attached-by'));
		$Table->addHeading('&nbsp;');
		
        $isLoggedIn = !$wgUser->isAnonymous() && $wgArticle->userCanEdit()? 'true' : 'false';
        $isRestricted = !$wgArticle->userCanEdit() ? 'true': 'false';
        $canUpdate = $wgArticle->userCanAttach();
        $lastFileId = 0;
        $j = 0;
		
        foreach ($files as &$file)
		{
			$Attachment = Attachment::loadFromArray($file);
			$isRevision = $Attachment->getId() == $lastFileId;
			$userLink = is_object($Attachment->getUser()) ? $sk->makeLinkObj( Title::makeTitle(NS_USER, wfEncodeTitle($Attachment->getUser()->getName())), htmlspecialchars($Attachment->getUser()->getName())): '';
 			$Row = $Table->addRow(false);
 			$moreicon = '&nbsp;';

 			if (!$isRevision) 
 			{
	 			$Row->setAttribute('class', ((++$j & 1) ? 'bg1' : 'bg2'));
	 			if ($Attachment->hasRevisions()) 
	 			{
		 			$Row->addClass('groupparent');
		 			$moreicon = '<a href="#" onclick="return toggleAttachments(\''
                        .wfEncodeJSString($Attachment->getId()).'\')" class="internal"><span id="showlink-'.$Attachment->getId().'" '
                        .'style="display:none;">'.Skin::iconify('expand').'</span>'
                        .'<span id="hidelink-'.$Attachment->getId().'">'.Skin::iconify('contract')
                        .'</span></a>';
	 			}
 			}
 			else  
 			{
	 			$Row->addClass('attach_'.$Attachment->getId().' '.($j & 1 ? 'bg1' : 'bg2'));
	 			$Row->addClass('group');
	 			$Row->setAttribute('style', 'display: none');
                $moreicon = Skin::iconify('dotcontinue');
 			}
 			
 			$onclick = "return FileMenu.show(this, '".$Attachment->getClassId()."', '".$Attachment->getId()."', ".$isLoggedIn.", false, false, '', ".$isRestricted.");";
 			$Table->addCol($moreicon);
 			$Table->addCol($Attachment->getFileLink($Attachment->getFileIcon()));
 			$Table->addCol($Attachment->getFileLink().($canUpdate ? '<small>'.$Attachment->getWebDavEditLink().'</small>': '').$Attachment->getDescriptionHTML('div'));
 			$Table->addCol($Attachment->getFileSize());
 			$Table->addCol($Attachment->getTimestamp());
 			$Table->addCol($userLink);
 			$Table->addCol($isRevision ? '&nbsp;': '<a href="#" class="downarrow actionmenu" onclick="'.$onclick.'">'.wfMsg('Article.Attach.menu-actions').'</a>');
 			
 		    $lastFileId = $Attachment->getId();
		}
		unset($file);
				
		$html .= '<div class="filescontent" id="attachFiles"><div class="table" id="attachTable">'.$Table->saveHtml().'</div></div>';
		$html.= '</div>'; //for the id="pageFiles" div that's created at the beginning of this function
        return $html;
	}

	
	/******
	 * Represents a single attachment
	 * Populate this object with the fromArray() method, which takes the array returned from the API
	 */
	class Attachment {
		var $id = null;
		var $filehref = null;
		var $filename = null;
		var $filesize = null;
		var $filetype = null;
		var $fileext = null;
		var $description = null;
		var $datecreated = null;
		var $createdby = null;
		var $revisioncount = null;
		// numeric revision count, e.g. 2, for the second revision of a file
		public $revisionid = null;
		var $classid = null; //used for UI
		
		/***
		 * Populates an attachment object with data returned from the API
		 */
		function loadFromArray($attachment) {
			$Attachment = new Attachment;
			$Attachment->id = wfArrayVal($attachment, '@id');
			$Attachment->filehref = wfArrayVal($attachment, 'contents/@href');
			$Attachment->filename = wfArrayVal($attachment, 'filename');
			$Attachment->filesize = wfArrayVal($attachment, 'contents/@size');
			$Attachment->description = wfArrayVal($attachment, 'description');
			$Attachment->filetype = wfArrayVal($attachment, 'contents/@type');
			$Attachment->revisioncount = wfArrayVal($attachment, 'revisions/@count');
			$Attachment->revisionid = !is_null(wfArrayVal($attachment, '@revision')) ? wfArrayVal($attachment, '@revision'): null;
			$Attachment->datecreated = wfArrayVal($attachment, 'date.created');
			$Attachment->classid = md5(strtolower(wfArrayVal($attachment, 'filename')));
			
			$previews = wfArrayVal($attachment, 'contents.preview');
			if (!is_null($previews)) {
				foreach ($previews as $preview) {
					$Attachments->previews[$preview['@rel']] = $preview;	
				}	
			}
			$userName = wfArrayVal($attachment, 'user.createdby/username');
			if (!is_null($userName)) {
				$Attachment->createdby = DekiUser::newFromText($userName);
			}
			return $Attachment;
		}
		
        function upload($pageId, $fileTempname) {
			global $wgDekiPlug;
			
			/***
			 * If a file gets uploaded with no extension, then the content-type will be application/octet-stream, 
			 * which is too generic. In this case, try to grab the mimetype specifically
			 */
			if ($this->getFileType() == 'application/octet-stream') {
				global $wgMimeTypes;
				
				// see bug #3933; libmagic is buggy, so we have to manually maintain a list based on extension
				$this->filetype = array_key_exists($this->getFileExtension(), $wgMimeTypes) 
					? $wgMimeTypes[$this->getFileExtension()]
					: mime_content_type($fileTempname);
			}

			$params = array(
				'file_temp'=> $fileTempname,
				'file_type' => $this->getFileType()
			);

			$r = $wgDekiPlug->At('pages', $pageId, 'files', '='.urlencode(urlencode($this->getFileName())));
			
            // send the authtoken for flash uploader
			if ( isset($_SESSION['swfupload']) ) {
			    $r = $r->SetHeader('X-Authtoken', $_SESSION['swfupload']);
			    DekiToken::set($_SESSION['swfupload']);
			}
			
			$result = $r->PutFile($params);
						
			if ($result['status'] == 400) 
			{
				
				$message = wfArrayVal($result, 'body/error/message');
				if (empty($message)) 
				{
					$message = wfMsg('System.API.Error.file_type_not_allowed', $this->getFileExtension($this->getFileName()));	
				}
				wfMessagePush('files', $message);
				return false;
			}
			
			if (!MTMessage::HandleFromDream($result)) 
			{
				return false;
			}
			if (!is_null(wfArrayVal($result, 'body/file/contents.preview'))) 
			{
				wfMessagePush('files', wfMsg('Article.Attach.image-upload-success', wfArrayVal($result, 'body/file/filename')), 'success');
			}
			else 
			{
				wfMessagePush('files', wfMsg('Article.Attach.file-upload-success', wfArrayVal($result, 'body/file/filename')), 'success');
			}
			
			$file_id = wfArrayVal($result, 'body/file/@id');
			
			if ($this->getDescription(false) != '') 
			{
				$Properties = new DekiFileProperties($file_id);
				$Properties->setDescription($this->getDescription(false));
				$Properties->update();
			}
			
			return $file_id;
		}
		
		//Push to recycling bin
		function delete() {
	        global $wgDekiPlug; 
			$r = $wgDekiPlug->At('files', $this->getId())->Delete();
			return $r['status'] == 200;
		}
		
		//Permanently delete this file
		function wipe() {
			global $wgDekiPlug;
			$r = $wgDekiPlug->At('archive', 'files', $this->getId())->Delete();
			return MTMessage::HandleFromDream($r);
		}		
		
		//Move out of the recycling bin
        function restore() {
	        global $wgDekiPlug;	        
			$r = $wgDekiPlug->At('archive', 'files', 'restore', $this->getId())->Post();			
			return MTMessage::HandleFromDream($r);
		}		
		
		function getClassId() {
			return $this->classid;
		}
		function getDescription($escape = true) {
			if ($escape) {
				return htmlspecialchars($this->description);
			}
			return $this->description;
		}
		function getDescriptionHTML($tag = 'div') {
			$classId = $this->getClassId();
			return '<'.$tag.' id="'.(isset($classId) ? 'class_'.$classId: '').'">'
            .'<span class="desctext" id="fileDescDisplay_'.$this->getId().'">'
            .(($this->getDescription(true) == '') 
            	? '<span class="nodescription">'.wfMsg('Article.Attach.no-description').'</span>' 
            	: $this->getDescription(true))
            .'</span></'.$tag.'>';
		}
		function getFileName($escape = false) {
			if ($escape) {
				return htmlspecialchars($this->filename);
			}
			return $this->filename;
		}
		function getFileSize($formatted = true) {
			if ($formatted) {
				return wfFormatSize($this->filesize);
			}
			return $this->filesize;
		}
		function getFileType() {
			return $this->filetype;
		}
		function getFileHref($append = '') {
			if (!empty($append)) 
			{
				$this->filehref .= strexist('?', $this->filehref) ? '&'.$append: '?'.$append;
			}
			return $this->filehref;
		}
		function getFilePreviewHref($type = 'thumb') {
			if (strpos($this->getFileHref(), '?') !== false) {
				return $this->getFileHref().'&size='.$type;
			}
			return 	$this->getFileHref().'?size='.$type;
		}
		function getId() {
			return $this->id;
		}
		function getTimestamp($formatted = true) {
			if ($formatted) {
				global $wgLang;
				return $wgLang->timeanddate( $this->datecreated, true );
			}
			return $this->datecreated;
		}
		//returns the WikiUser object
		function getUser() {
			return $this->createdby;
		}
		function getRevision() {
			return $this->revisionid;
		}
		function getRevisionCount() {
			return $this->revisioncount;
		}
		function hasRevisions() {
			return $this->getRevisionCount() > 1;
		}
		function getFileLink($text = null) {
			if (is_null($text)) {
				$text = $this->getFileName(true);
			}
            return '<a href="'.$this->getFileHref().'" class="filelink" title="'.$this->getFileName(true).'">'.$text.'</a>';
        }
		function getFileExtension() {
			if (!is_null($this->fileext)) {
				return $this->fileext;
			}
			if (strpos($this->getFilename(), '.') === false) {
				$this->fileext = '';
			}
		    $this->fileext = strtolower(end(explode('.', $this->getFilename())));
		    return $this->fileext;
		}		
        function getFileIcon() {
	        global $wgStyledExtensions;
	        $styled_extension = in_array($this->getFileExtension(), $wgStyledExtensions) 
	        	? $this->getFileExtension()
	        	: 'unknown';
	        return Skin::iconify('mt-ext-'.$styled_extension);
       	}
       	
       	function getWebDavEditLink() 
       	{
	    	global $wgWebDavExtensions;
	       	$fileext = $this->getFileExtension();
	       	if (is_null($fileext) || !in_array($fileext, array_merge($wgWebDavExtensions) )) {
		       	return '';
	       	}

	       	return ' <a href="#" class="deki-webdavdoc" onclick="return loadOfficeDoc(\''.$this->getFileHref('authtoken='.DekiToken::get()).'\');">'.wfMsg('Article.Attach.edit').'</a>';
       	}
       	
		function fromId($attachId) {
			$Attachment = new Attachment;
			$Attachment->id = $attachId;
			return $Attachment;	
		}
	}
	
	/***
	 * THIS IS LEGACY CODE WHICH SHOULD BE REMOVED; they are still being kept in cause there's a few places they're still being called
	 */
	class AttachFile {
        		
        /**
		  * MT ursm
		  *
		  */
        function execute(&$title, $action) {
			global $wgDreamAttachmentsRemove;
			switch( $action ) {
				case 'view':
				case 'thumb':
					AttachFile::view($title);
					return true;
			}
			return false;
        }
                
        function getFileFromTitle(&$title, &$articleID, &$attachmentName, &$oldid, $exitOnMissing = true, $returnMostRecent = false) {
            global $wgRequest;
            $attachmentName = rawurldecode($title->getAttachmentName($articleID));
                        
            /* When PHP receives a file, in the format File:/file.jpg, which would normally correspond to file.jpg on the home page, 
             * the item gets passed into the title object. However, the title object assumes that a forward slash is a bad character, 
             * and it knocks it out. If that's the case, the $articleID will no longer be set, and this will fail
             */
            if ($articleID == 0) {
	            $articleID = wfGetHomePageId();
            }
            if ($articleID > 0) {
	            global $wgDekiPlug;
	            $r = $wgDekiPlug->At('pages', $articleID, 'files', '='.urlencode(urlencode($attachmentName)), 'info')->Get();
	            if ($r['status'] != 200) {
		            $r = $wgDekiPlug->At('pages', $articleID, 'files', '='.urlencode(urlencode(str_replace('_', ' ', $attachmentName))), 'info');
		            $r = $r->Get();		            
		            if ($r['status'] != 200) {
			            return false;
		            }
	            }
	            $file = $r['body']['file'];
	            return $file;
            }            
            return false;
        }

        function view(&$title) {
            global $wgServer, $wgRequest;
            global $wgDreamServer, $wgDekiApi;
			$file = AttachFile::getFileFromTitle($title, $articleID, $attachmentName, $oldid,  true, true);
			if ($file === false) {
				wfHttpError( 404, "File not found", $title->getPrefixedText() . " not found." );
		        exit();
			}
            header('Location: '.$file['contents']['@href']);
            exit();
        }
        
        function getFileExtension($filename) {
	        $extension = '';
	        if (strpos($filename, '.') !== false) {
		        $extension = strtolower(end(explode('.', $filename)));
	        }
	        return $extension;
        }
        
        function getFileTitle($full_name, $topicName) {
            global $wgCanonicalNamespaceNames;
			            
            $nt = Title::newFromText($wgCanonicalNamespaceNames[NS_ATTACHMENT] . ':a');
            $nt->mDbkeyform = trim($topicName,'/') . '/' . $full_name;
            $nt->mTextform = str_replace('_',' ', $nt->mDbkeyform);
            return $nt;
        }
    }	
}
