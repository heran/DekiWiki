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

class DekiPropertiesFormatter extends DekiFormatter
{
	protected $contentType = 'application/json';
	protected $requireXmlHttpRequest = true;
	protected $disableCaching = true;

	private $Request;

	public function format()
	{
		$this->Request = DekiRequest::getInstance();
		$action = $this->Request->getVal('action');
		
		$result = '';

		switch ($action)
		{
			case 'edit':
				$result = $this->getEditHtml();
				break;
				
			case 'save':
				$result = $this->saveProperty();
				break;
			
			default:
				header('HTTP/1.0 404 Not Found');
				exit(' '); // flush the headers
		}
		
		echo json_encode($result);
	}

	private function getEditHtml()
	{
		$Table = new DomTable();
		$Tr = $Table->addRow();
		// remove any automagical classes
		$Tr->setAttribute('class', '');
		
		$Td = $Table->addCol('&nbsp;');
		$Td->addClass('name');
		$Td->setAttribute('colspan', 2);
		
		$Td = $Table->addCol(
			'<input type="text" name="name" />'
		);
		$Td->addClass('value');
			
		$Td = $Table->addCol('
			<button class="save"><span>'. wfMsg('Page.Properties.form.edit.save') .'</span></button>
			<button class="cancel"></span>' . wfMsg('Page.Properties.form.edit.cancel') . '</span></button>
		');
		$Td->addClass('edit');
		
		$result = array(
			'success' => true,
			'html' => $Tr->saveHtml()
		);
		
		return $result;
	}

	private function saveProperty()
	{
		$result = array(
			'success' => false
		);
		
		$id = $this->Request->getVal('id');
		$type = $this->Request->getEnum('type', array('page', 'user'), 'page');
		
		$propertyName = $this->Request->getVal('name');
		$propertyValue = $this->Request->getVal('value');
		
		if (!is_null($propertyName) && $propertyName != '')
		{
			$Properties = $type == 'user' ? new DekiUserProperties($id) : new DekiPageProperties($id);
			$Properties->setCustom($propertyName, $propertyValue);
			$Result = $Properties->update();
			
			if ($Result->isSuccess())
			{
				$result['success'] = true;
			}
			else
			{
				// failed
				$result['message'] = $Result->getError();
			}
		}
		else
		{
			$result['message'] = wfMsg('GUI.Properties.error.invalid');
		}

		return $result;
	}
}

new DekiPropertiesFormatter();
