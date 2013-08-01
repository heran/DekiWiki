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

define('DEKI_ADMIN', true);
require_once('index.php');
global $IP;
// TODO: guerric, refactor this file to use DekiFile
require_once($IP . '/includes/Attach.php');


class FileRestore extends DekiControlPanel
{
	// needed to determine the template folder
	protected $name = 'file_restore';


	public function index()
	{
		$this->executeAction('listing');
	}
	
	public function post()
	{
		global $wgUser, $wgLang, $wgOut, $wgTitle;
		
		$this->Request->redirect($this->getUrl('/listing'));

		if ($this->Request->isPost())
		{
			$files = $this->Request->getVal('iFile');

			if (!empty($files))
			{
				foreach ($files as $fileId)
				{
					$Attachment = Attachment::fromId($fileId);
					switch ($this->Request->getVal('action')) 
					{
						case 'restore': 
							$Attachment->restore();
						break;
						case 'wipe': 
							$Attachment->wipe();						
						break;	
					}
				}
				
				// show messages once?
				switch ($this->Request->getVal('action')) 
				{
					case 'restore': 
						DekiMessage::success($this->View->msg('FileRestore.success.restored'));
					break;
					case 'wipe': 
						DekiMessage::success($this->View->msg('FileRestore.success.deleted'));			
					break;	
				}
			}
			else
			{
				$action = strtolower($this->Request->getVal('action'));
				if ($action != 'restore' && $action != 'wipe') 
				{
					$action = 'restore';
				}
				DekiMessage::error($this->View->msg('FileRestore.data.no-selection', $this->View->msg('FileRestore.action-'.$action)));	
			}
		}
	}

	public function listing()
	{
		global $wgUser, $wgLang;

		$sk = $wgUser->getSkin();
		$sorted = $this->getDeletedFiles();

		$sort = $this->Request->getVal('sort');
		self::sortDeletedFiles($sorted, empty($sort) ? 'removed' : $sort);
		
		if ($this->Request->getVal('type') == 'desc')
		{
			krsort($sorted);
		}
		
		$Table = new DomTable();
		$Table->setColWidths('', '150', '100', '100');
		$Table->addRow();
		$Th = $Table->addHeading(DekiForm::singleInput('checkbox', 'all', '', array()).' <a href="'. $this->getUrl('', array('sort' => 'name', 'type' => strtoupper($this->Request->getVal('type')) == 'ASC' ? 'desc': 'asc')) .'">'. $this->View->msg('FileRestore.data.name') .'</a>');
		$Table->addHeading($this->View->msg('FileRestore.data.location'));
		$Table->addHeading('<a href="'. $this->getUrl('', array('sort' => 'removed', 'type' => strtoupper($this->Request->getVal('type')) == 'ASC' ? 'desc': 'asc')) .'">'. $this->View->msg('FileRestore.data.timestamp') .'</a>');
		$Th = $Table->addHeading($this->View->msg('FileRestore.data.deletedby'));
		$Th->setAttribute('class', 'last');
		
		if (empty($sorted)) 
		{
			$Table->setAttribute('class', 'table none');
			$Table->addRow();
			$Td = $Table->addCol('<div class="none">'.$this->View->msg('FileRestore.data.empty').'</div>');
			$Td->setAttribute('colspan', 4);
			$Td->setAttribute('class', 'last');
		}
		else 
		{
			foreach ($sorted as $file)
			{					
				$Attachment = Attachment::loadFromArray($file); // attach.php
			
				$onDeletedPage = is_null(wfArrayVal($file, 'page.parent/@id'));
				
				if (!$onDeletedPage) 
				{ 
					$t = Title::newFromText($file['page.parent']['path']);	
					$pageLink = $sk->makeKnownLink($t->getPrefixedText(), htmlspecialchars($t->getPathlessText()));
				}
				
				$desc = $Attachment->getDescription();
				$info = '<div class="info">'
						.'<strong>'.$this->View->msg('FileRestore.data.filesize').'</strong> '
						.$Attachment->getFileSize().'<br/>'
						.'<strong>'.$this->View->msg('FileRestore.data.description').'</strong> '
						.(empty($desc) ? '<span class="none">No description</span>': $desc).'<br/>'
						.'<strong>'.$this->View->msg('FileRestore.data.attached').'</strong> '
						.$wgLang->date( $file['date.created'], true ).' by '.$sk->makeLinkObj( Title::makeTitle( NS_USER, $file['user.createdby']['username'] ), $file['user.createdby']['username']).'<br/>'
						.'<strong>'.$this->View->msg('FileRestore.data.preview').'</strong> '
						.$Attachment->getFileLink()
					.'</div>';
					
				$Table->addRow();
				$Td = $Table->addCol(DekiForm::singleInput('checkbox', 'iFile[]', $file['@id'], array('id' => 'f'.$file['@id']), $Attachment->getFileName()) . $info);
 				$Table->addCol($onDeletedPage ? '<del>'. $this->View->msg('FileRestore.data.deleted') .'</del>': $pageLink);
 				$Table->addCol($wgLang->date( $file['date.deleted'], true ));
 				$Td = $Table->addCol($sk->makeLinkObj( Title::makeTitle( NS_USER, $file['user.deletedby']['username'] ), $file['user.deletedby']['username']));
 				$Td->setAttribute('class', 'last');
			}
		}
		
		$this->View->set('listingTable', $Table->saveHtml());
		$this->View->set('restore-form.action', $this->getUrl('/post'));
		$this->View->output();
		// end listing
	}

	protected function getDeletedFiles()
	{
		global $wgDekiPlug;
		$Result = $this->Plug->At('archive', 'files')->Get();
		if (!$Result->handleResponse())
		{
			return array();
		}
		
		$files = $Result->getAll('body/files.archive/file.archive');
		if (is_null($files))
		{
			return array();
		}

		return $files;
	}

	// ripped from Attach.php
	protected static function sortDeletedFiles(&$_images, $by)
	{
		if (!is_array($_images) || count($_images) == 0) {
			$_images = array();
			return;
		}
		$_lookup = array();
		$_data = array();
		foreach ($_images as $key => $val) {
			switch ($by) {
				case 'date-desc': 
				case 'date-asc': 
				case 'date': 
					$lookup = $val['date.created'];
				break;   
				case 'name': 
					$lookup = strtolower($val['filename']);
				break;
				case 'desc': 
					$lookup = $val['description'];
				break;
				case 'removed':
					$lookup = $val['date.deleted'];
				break;
			}
			$_lookup[$val['@id']] = $lookup;
			$_data[$val['@id']] = $val;
		}
		switch ($by) {
			case 'date-desc': 
				arsort($_lookup);
			break;   
			default: 
				asort($_lookup);
			break;
		}
		reset($_lookup);
		$_ordered = array();
		foreach ($_lookup as $key => $val) {
			$_ordered[] = $_data[$key];
		}
		$_images = $_ordered;
	}
}

new FileRestore();