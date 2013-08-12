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

if (defined('MINDTOUCH_DEKI')) :

class MindTouchFilesTablePlugin extends DekiPlugin
{
	/**
	 * New hooks this plugin exposes
	 */
	const FILTER_ACTIONS_MENU = 'FilesTable:FilterActionsMenu';
	const FILTER_FILE_ROW = 'FilesTable:FilterFileRow';
	
	const AJAX_FORMATTER = 'FilesTable';
	const SPECIAL_ATTACH_VERSION = 'AttachNewVersion';

	const PLUGIN_FOLDER = 'files_table';

	/**
	 * Initialize the plugin and hooks into the application
	 */
	public static function load()
	{
		// hook
		DekiPlugin::registerHook(Hooks::PAGE_RENDER_FILES, array('MindTouchFilesTablePlugin', 'listingHook'));
		DekiPlugin::registerHook(Hooks::AJAX_FORMAT . self::AJAX_FORMATTER, array('MindTouchFilesTablePlugin', 'ajaxHook'));
		DekiPlugin::registerHook(Hooks::SPECIAL_PAGE . self::SPECIAL_ATTACH_VERSION, array('MindTouchFilesTablePlugin', 'specialHook'));
	}
	
	/**
	 * Called when the ajax formatter for files is hit
	 * 
	 * @param string &$body
	 * @param string &$message
	 * @param bool &$success
	 * @return N/A
	 */
	public static function ajaxHook(&$body, &$message, &$success)
	{
		$Request = DekiRequest::getInstance();
		$action = $Request->getVal('action');
		
		// default to failure
		$success = false;
		
		switch ($action)
		{
			default:
			case 'set_description':
				$fileId = $Request->getInt('file_id');
				$description = $Request->getVal('description');
				$File = DekiFile::newFromId($fileId);
				if (is_null($File))
				{
					$message = wfMsg('Article.Attach.error.file-not-found');
					return;
				}

				$Result = DekiFile::updateDescription($File, $description);
				if ($Result !== true)
				{
					$message = wfMsg('Article.Attach.error.description');
					$body = $Result->getError();
					return;
				}

				$body = $File->getDescription();
				if (empty($body))
				{
					$message = wfMsg('Article.Attach.no-description');
				}
				break;

			// load the revisions for the file
			case 'html_revisions':
				$fileId = $Request->getInt('file_id');
				$File = DekiFile::newFromId($fileId);
				if (is_null($File))
				{
					$message = wfMsg('Article.Attach.error.file-not-found');
					return;
				}
				// TODO: how to reuse table markup generation?
				$body = '';
				$fileRevisions = DekiFile::loadRevisionList($File->getId());
				// needed to determine page move information, traverse top to bottom
				$ParentRevision = array_shift($fileRevisions);

				foreach ($fileRevisions as &$Revision)
				{
					// file revision change information
	 				if ($ParentRevision->wasMoved())
	 				{
	 					$FromInfo = $Revision->getParentInfo();
	 					$ToInfo = $ParentRevision->getParentInfo();

	 					$body .= '<tr class="group group-'. $Revision->getId() .' file-moved">';
	 						$body .= '<td class="col1">'. Skin::iconify('dotcontinue') . '</td>';
		 					$body .= '<td class="col2">'. Skin::iconify('move') . '</td>';
		 					$body .= '<td class="col3" colspan="5">';
		 						$body .= wfMsg('Article.Attach.info.moved', '<em>'.htmlspecialchars($FromInfo->title).'</em>', '<em>'.htmlspecialchars($ToInfo->title).'</em>');
		 					$body .= '</td>';
	 					$body .= '</tr>';
	 				}
	 				// update for next check
	 				$ParentRevision = $Revision;
	 				
					$body .= '<tr class="group group-'. $Revision->getId() .'">';
					$columns = self::getFormattedColumns($Revision);
					$i = 1;
					foreach ($columns as &$column)
					{
						$body .= '<td class="col'. ($i++) .'">'.$column.'</td>';
					}
					$body .= '</tr>';
				}
				unset($Revision);
				unset($ParentRevision);
				break;
			
			/**
			 * Fetches the html for the entire files section
			 */
			case 'html_refresh':
				$pageId = $Request->getInt('page_id');
				$ArticleTitle = Title::newFromID($pageId);
				if (is_null($ArticleTitle))
				{
					$message = 'Page not found';
					return;
				}
				
				// create the objects required to generate the table html
				$Article = new Article($ArticleTitle);

				$pageFiles = $Article->getFiles();
    			$files = array();
    			$fileCount = 0;
    			foreach ($pageFiles as &$result)
    			{
    				$File = DekiFile::newFromPagesArray($result, $ArticleTitle);
    				$files[] = $File;
    			}
    			unset($result);
    			
				$body = self::getFilesTableHtml($Article, $files, $fileCount);
				
				$js = MTMessage::ShowJavascript(false); // any yellowbox exceptions from file uploads
				if (!empty($js))
				{
					$body .= '<script type="text/javascript">'. $js . '</script>';
				}
				
				break;
		}
		
		// if we made it here then it was a successful request
		$success = true;
	}
	
	public static function listingHook($Title, &$pluginHtml, $pageId, &$files, &$fileCount)
	{
		global $wgArticle;
		$innerHtml = self::getFilesTableHtml($wgArticle, $files, $fileCount);

		// add the wrapping div
		$pluginHtml = '<div id="pageFiles">'. $innerHtml . '</div>';
	}
		
	/**
	 * Attach new version special page
	 * 
	 * @param $pageName
	 * @param $pageTitle
	 * @param $html
	 * @param $subhtml
	 * @return unknown_type
	 */
	public static function specialHook($pageName, &$pageTitle, &$html, &$subhtml)
	{
		global $wgDekiPluginPath;
		
		$Request = DekiRequest::getInstance();
		$fileId = $Request->getInt('file_id');

		$File = DekiFile::newFromId($fileId);
		
		// validate inputs
		 if (is_null($File))
		{
			// unknown file
			DekiMessage::error(wfMsg('Article.Attach.error.file-not-found'));
			SpecialPagePlugin::redirectHome();
			return;
		}
		
		if ($Request->isPost())
		{
			global $IP;
			require_once($IP . '/deki/core/deki_file_upload.php');
			
			$description = $Request->getVal('upload_description');
			$NewFile = DekiFileUpload::updateFromPost('upload_file', $fileId, $description);

			if (!is_null($NewFile))
			{
				// new version sucessfully uploaded
				FlashMessage::push('files', wfMsg('Article.Attach.file-upload-success', $NewFile->toHtml()), 'success');
				// TODO: guerrics, change after special page popup class is created
				$html = '<script type="text/javascript">parent.Deki.Plugin.FilesTable.Refresh(); parent.tb_remove();</script>';
				return;
			}
			// error, yellowbox handles
		}


		$i = 0;
		$html = '';

		DekiPlugin::includeCss(self::PLUGIN_FOLDER, 'files_table_popup.css');
		
		$html .= '<form id="frmAttachNewVersion" method="post" enctype="multipart/form-data" class="special-page-form">';
			$html .= '<div class="field"><label>'.wfMsg('Article.Attach.attachnew.current').'</label><div class="file"><span class="file">'.$File->toHtml().'</span> <span class="size">'.$File->getSize().'</span> <span class="timestamp">'.wfMsg('Article.Attach.attachnew.modified', $File->getTimestamp()).'</span></div></div>';
			$html .= '<div class="field">' . DekiForm::singleInput('file', 'upload_file', null, null, wfMsg('Article.Attach.attachnew.label.select')) . '</div>';
			$html .= '<div class="field">' . DekiForm::singleInput('text', 'upload_description', null, null, wfMsg('Article.Attach.attachnew.label.description')) . '</div>';
			$html .= '<div id="footer"><div class="buttons-bottom">' . DekiForm::singleInput('button', 'action', 'attach_new_version', null, wfMsg('Article.Attach.attachnew.button')) . '</div></div>';
		$html .= '</form>';
	}

	/**
	 * Method is public to allow other plugins to obtain the href
	 * 
	 * @param int $fileId
	 * @param bool $popup
	 * @return string
	 */
	public static function getAttachVersionHref($fileId, $popup = false)
	{
		$query = 'file_id='. $fileId . ($popup ? '&TB_iframe=true&width=300&height=200' : '');
		return Title::newFromText(MindTouchFilesTablePlugin::SPECIAL_ATTACH_VERSION, NS_SPECIAL)->getFullURL($query);
	}
	
	/**
	 * Method used for generating the table html and refreshing the display
	 * 
	 * @param Article $Article
	 * @return string
	 */
	protected static function &getFilesTableHtml($Article, &$files, &$fileCount)
	{
		$html = wfMessagePrint('files'); //success & error messsages

		if (empty($files))
		{
			$html .= '<div class="nofiles">&nbsp;</div>';
			return $html;
		}

		// set the file count for the skins
		$fileCount = count($files);

		// build the files table
		$Table = new DomTable();
		$Table->setColWidths('16', '16', '', '80', '145', '115', '75');
		$Table->addRow(false);
		$Col = $Table->addHeading(wfMsg('Article.Attach.table-header-file'), 3);
		$Table->addHeading(wfMsg('Article.Attach.table-header-size'));
		$Table->addHeading(wfMsg('Article.Attach.table-header-date'));
		$Table->addHeading(wfMsg('Article.Attach.table-header-attached-by'));
		$Table->addHeading('&nbsp;');

		$userCanAttach = $Article->userCanAttach();
		foreach ($files as &$File)
		{
			$result = DekiPlugin::executeHook(self::FILTER_FILE_ROW, array($Table, $File));
			if ($result == DekiPlugin::HANDLED_HALT)
			{
				// plugin has updated the table, prevent default
				continue;
			}

 			$Row = $Table->addRow();
 			$Row->setAttribute('id', 'deki-file-row-'. $File->getId());
 			
 			if ($File->hasRevisions())
 			{
 				$Row->addClass('groupparent');
 			}
 			
 			$columns = self::getFormattedColumns($File, $userCanAttach);
 			foreach ($columns as $columnHtml)
 			{
 				$Table->addCol($columnHtml);
 			} 			
		}
		unset($File);
				
		$html .= '<div class="filescontent" id="attachFiles"><div class="table" id="attachTable">'.$Table->saveHtml().'</div></div>';

		return $html;
	}

	/**
	 * Moved to method for reuse in the ajax endpoint
	 * 
	 * @param DekiFile $File
	 * @param bool $userCanUpdate - only required for head revisions (determines action menu status: enabled/disabled)
	 * @return array - each element corresponds to a table column for the file
	 */
	protected static function getFormattedColumns($File, $userCanUpdate = false)
	{
		global $wgUser; // required for getSkin()
		
		$columns = array();

		// expand/contract icon
		// show an icon to display the older file revisions
		if ($File->hasRevisions())
		{
	  		$columns[] = !$File->isRevision()
				? '<a href="#" id="deki-file-revisions-'.$File->getId().'" class="internal deki-file-revisions">'.
					'<span class="hide" style="display: none;">'. Skin::iconify('expand') .'</span>'.
				  	'<span class="show">'. Skin::iconify('contract') .'</span>'.
					'</a>'
				: Skin::iconify('dotcontinue')
			;
		}
		else
		{
			$columns[] = '&nbsp;';
		}

		// file icon
 		$columns[] = $File->getLink($File->getIcon());

 		// file display
 		$id = '';
 		$classes = array(
 			'deki-file-description',
 			// including desctext class for backwards compat
 			'desctext'
 		);
 		
 		if (!$File->isRevision() && $userCanUpdate)
 		{
 			// make the description editable
 			$classes[] = 'deki-editable';
 			$id = 'deki-file-description-'.$File->getId();
 		}
 		
  		$description = $File->getDescription();
  		if (empty($description))
  		{
  			$classes[] = 'nodescription';
  			$description = wfMsg('Article.Attach.no-description');
  		}
 		
 		$columns[] =
	 		$File->getLink()
	 		.($userCanUpdate ? '<small>'.$File->getWebDavEditLink().'</small>' : '')
	 		.'<div>'
	 		.'<span id="'. $id .'" class="'. implode(' ' , $classes) .'">'
	 			.htmlspecialchars($description)
	 		.'</span>'
	 		.'</div>'
		;
		
		// file details
		$columns[] = $File->getSize();
		$columns[] = $File->getTimestamp();
		
		// user information
		$User = $File->getCreator();
		$Skin = $wgUser->getSkin();
		$Title = $User->getUserTitle();
		$columns[] = $User ? $Skin->makeLinkObj($Title, htmlspecialchars($User->getName())) : '&nbsp;';
		
		// actions menu
		if (!$File->isRevision())
		{
			$menuItems = array();
			
			// new version
			$href = self::getAttachVersionHref($File->getId());
			$menuItems[] = array(
				'class' => 'new quickpopup',
				'text' => '<a href="'. $href .'" title="'.wfMsg('Article.Attach.attachnew.title').'">'. Skin::iconify('attachedit') .'<span class="label">'. wfMsg('Article.Attach.menu.attachnew') .'</span></a>'
			);
			// edit description
			$menuItems[] = array(
				'class' => 'description',
				'text' => '<a href="#">'. Skin::iconify('attachedit') .'<span class="label">'. wfMsg('Article.Attach.menu.description') .'</span></a>'
			);
			
			// move file
			$menuItems[] = array(
				'class' => 'move',
				'text' => '<a href="#">'. Skin::iconify('attachmove') .'<span class="label">'. wfMsg('Article.Attach.menu.move') .'</span></a>'
			);
			
			// delete file
			$menuItems[] = array(
				'class' => 'delete',
				'text' => '<a href="#">'. Skin::iconify('attachdel') .'<span class="label">'. wfMsg('Article.Attach.menu.delete') .'</span></a>'
			);

 			/**
 			 * @params DekiFile $File
 			 * @param bool $disabled - true if the menu items are disabled
 			 * @param array &$menuItems
 			 * @param string &$html
 			 */
 			$html = '';
 			$result = DekiPlugin::executeHook(self::FILTER_ACTIONS_MENU, array($File, !$userCanUpdate, &$menuItems, &$html));
 			if ($result == DekiPlugin::HANDLED_HALT)
 			{
 				// plugin halted further filters, add the html
 				$columns[] = $html;
 				return;
 			}

			// build the menu html
 			// set the disabled class
 			$disabled = $userCanUpdate ? '' : ' disabled';

 			$html .= '<a href="#" id="deki-file-actions-'.$File->getId().'" class="deki-file-actions downarrow actionmenu'. $disabled .'">'.wfMsg('Article.Attach.menu-actions').'</a>';
 			// dmenu class is for backwards compat
 			$html .= '<ul class="deki-file-menu dmenu'. $disabled .'">';
 			foreach ($menuItems as &$item)
 			{
 				$html .= '<li class="menu-item '. $item['class'] . $disabled .'">'. $item['text'] .'</li>';
 			}
 			$html .= '</ul>';
  
 			$columns[] = $html;
 		}
 		else
 		{
 			$columns[] = '&nbsp;';
 		}
 		
 		return $columns;
	}
}
// initialize the plugin
MindTouchFilesTablePlugin::load();

endif;
