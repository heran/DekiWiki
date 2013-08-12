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
 * Contain a feed class as well as classes to build rss / atom ... feeds
 * Available feeds are defined in Defines.php
 * @package MediaWiki
 */


class MTFeed {
	var $FeedType;
	var $Since;
	var $Max;
	var $Offset;
	var $Format;
	var $UserId;
	var $PageId;
	var $Contents;
	var $Language;

	function MTFeed( $FeedType, $Since = '', $Max = 0, $Offset = 0, $Format = 'atom', $UserId = 0, $PageId = 0, $Language = null, $Format = 'daily') {
		$this->FeedType = $FeedType;
		$this->Since = $Since;
		$this->Max = $Max;
		$this->Offset = $Offset;
		$this->Format = $Format;
		$this->UserId = $UserId;
		$this->PageId = $PageId;
		$this->Language = $Language;
		$this->Format = $Format;
	}

	function getFeedContents() {
		if(!$this->Contents) {
			global $wgDreamServer, $wgDekiApi, $wgServer, $wgStylePath;
				
			$r = new Plug($wgDreamServer, 'xml');
			$r = $r->At($wgDekiApi);

			switch($this->FeedType) {
				case 'contributions':
					$r = $r->At('users', $this->UserId, 'feed');
					break;
				case 'watchlist':
					$r = $r->At('users', $this->UserId, 'favorites', 'feed');
					break;
				case 'pagechanges':
					$r = $r->At('pages', $this->PageId, 'feed');
					break;
				case 'subpagechanges':
					$r = $r->At('pages', $this->PageId, 'feed')->With('depth', 'infinity');
					break;
				case 'recentchanges':
				default:
					$r = $r->At('site','feed');
					break;
			}
				
			if ($this->Since != '')
			$r = $r->With('since', $this->Since);
			if ($this->Max > 0)
			$r = $r->With('limit', $this->Max);
			if ($this->Offset > 0)
			$r = $r->With('offset', $this->Offset);
			if (!is_null($this->Language))
			$r = $r->With('language', $this->Language);
			if (!is_null($this->Format))
			$r = $r->With('format', $this->Format);
				
			$result = $r->Get();
				
			if (MTMessage::HandleFromDream($result)) {
				$this->Contents = $result['body'];
				$this->Contents = '<?xml version="1.0" encoding="utf-8"?>'.
					'<?xml-stylesheet type="text/css" href="' .
				htmlspecialchars( "$wgServer$wgStylePath/common/feed.css" ) . '"?' . ">".
				$this->Contents;
			} else {
				// print some error
				return;
			}
		}
		return $this->Contents;
	}
	function httpHeaders() {
		global $wgOut;

		# We take over from $wgOut, excepting its cache header info
		$wgOut->disable();
		header('Content-type: application/atom+xml; charset=utf-8');
		if (DekiRequest::getInstance()->isIE6())
		{
			header('Expires: 0');
			header('Pragma: cache');
			header('Cache-Control: private');
		}
		else
		{
			$wgOut->sendCacheControl();
		}
	}
	function output() {
		global $wgDisabledGzip;

		$isGzip = extension_loaded('zlib') && isset($_SERVER['HTTP_ACCEPT_ENCODING']) && substr_count($_SERVER['HTTP_ACCEPT_ENCODING'], 'gzip') && !$wgDisabledGzip;
		
		if ($isGzip)
		{
			ob_start();
			ob_start('ob_gzhandler');
		}
		else
		{
			ob_start();
		}
		
		$this->httpHeaders();
		echo $this->getFeedContents();
		
		if ($isGzip) {
			ob_end_flush();
		}
		header('Content-Length: ' . ob_get_length());
		ob_end_flush();
	}
}