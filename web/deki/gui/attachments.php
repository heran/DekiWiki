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

class AttachmentsFormatter extends DekiFormatter
{
	protected $contentType = 'application/json';
	protected $requireXmlHttpRequest = true;

	private $Request;

	public function format()
	{
		$this->Request = DekiRequest::getInstance();

		$action = $this->Request->getVal( 'action' );

		$result = '';

		switch ($action)
		{
			case 'getbyids':
				$result = $this->getByIds();
				break;
			case 'delete':
				$result = $this->delete();
				break;
			default:
				header('HTTP/1.0 404 Not Found');
				exit(' '); // flush the headers
		}
		
		$this->disableCaching();

		echo $result;
	}

	private function getByIds()
	{
		global $wgDekiPlug;

		$fileIds = $this->Request->getVal( 'fileIds' );

		$fileIds = explode( ',', $fileIds );
		$fileIds = array_filter( $fileIds, array( $this, 'filterEmptyIds' ) );

		$files = array();

		foreach ( $fileIds as $fileId )
		{
			$Preview = DekiFilePreview::newFromId($fileId);

			if (!is_null($Preview))
			{
				$fileInfo = array(
					'href' => $Preview->getHref()
				);
				
				if ($Preview->hasPreview())
				{
					$fileInfo['width'] = $Preview->getWidth();
					$fileInfo['height'] = $Preview->getHeight();
				}
				
				$files[] = $fileInfo;
			}
		}

		return json_encode( $files );
	}

	private function delete()
	{
		$fileId = $this->Request->getInt('fileId');
		$result = DekiFile::delete($fileId);

		if ($result === true)
		{
			wfMessagePush('files', wfMsg('Skin.Common.file-deleted'), 'success');
		}
		else
		{
			// bubble exceptions
			$Result->handleResponse();
		}
	}

	private function filterEmptyIds($id)
	{
		return ! empty($id);
	}
}

new AttachmentsFormatter();
