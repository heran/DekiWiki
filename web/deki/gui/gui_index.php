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

/**
 * Include the entire front-end for now
 */
define('MINDTOUCH_DEKI', true);
require_once('../../includes/Defines.php');
require_once('../../LocalSettings.php');
require_once('../../includes/Setup.php');


class DekiFormatter
{
	protected $contentType = 'text/plain';
	protected $charset = 'UTF-8';
	protected $requireXmlHttpRequest = false;
	protected $disableCaching = false;

	public function __construct()
	{
		$this->checkXmlHttpRequest();
		$this->setContentType($this->contentType, $this->charset);
		
		if ($this->disableCaching)
		{
			$this->disableCaching();
		}
		
		$this->format();
	}
	
	protected function checkXmlHttpRequest()
	{
		if ($this->requireXmlHttpRequest)
		{
			$Request = DekiRequest::getInstance();
			if (!$Request->isXmlHttpRequest())
			{
				// TODO: how to handle?
				// requesting client is not an XmlHttpRequest
				header('Location: /');
				exit(' ');
			}
		}
	}

	protected function setContentType($contentType = null, $charset = null)
	{
		if (!is_null($contentType))
		{
			$type = empty($contentType) ? 'text/plain' : $contentType;
			$charset = empty($charset) ? 'UTF-8' : $charset;
			header('Content-Type: ' . $type . '; charset=' . $charset);
		}
	}
	
	protected function disableCaching()
	{
		header("Expires: Mon, 26 Jul 1997 05:00:00 GMT");  // disable IE caching
		header("Last-Modified: " . gmdate( "D, d M Y H:i:s") . " GMT");
		header("Cache-Control: no-cache, must-revalidate");
		header("Pragma: no-cache");
	}
	
	/**
	 * @stub method called upon formatter creation
	 */
	protected function format() {}
}
