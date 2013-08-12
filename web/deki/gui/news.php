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

class NewsFormatter extends DekiFormatter
{
	protected $contentType = 'text/html';
	//protected $requireXmlHttpRequest = true;


	public function format() 
	{
		global $wgRssUrl, $wgCacheDirectory;
		if (empty($wgRssUrl)) 
		{
			echo('<!-- no rss feed set -->');
			return;
		}
		
		//cache RSS output
		define('MAGPIE_CACHE_DIR', $wgCacheDirectory);
		require_once('includes/magpie-0.72/rss_fetch.inc');
		$rss = fetch_rss( $wgRssUrl );
		if (empty($rss->items)) 
		{
			return;
		}
		
		//do very simple HTML output
		echo('<html>');
		echo('<head><title>'.$rss->channel['title'].'</title>');
		echo('<link href="/skins/common/reset.css" rel="stylesheet" type="text/css" />');
		echo('<link href="/deki/cp/assets/news.css" rel="stylesheet" type="text/css" />');
		echo('<base target="_blank" />');
		echo('</head>');
		echo('<body id="updateBody">');
		$i = 0; 
		foreach ($rss->items as $item) 
		{
			$i++;
			if ($i > 3) 
			{
				break;
			}
			echo('<div class="block"><h2><a href="'.$item['link'].'">'.$item['title'].'</a></h2>');
			echo('<p class="date">'.date('Y/m/d', wfTimestamp(TS_UNIX, strtotime($item['updated']))).'</p>');
			if (strlen($item['summary']) > 128) 
			{
				$item['summary'] = substr($item['summary'], 0, 125).' [...]';	
			}
			
			$item['summary'] = str_replace('[...]', ' ...', $item['summary']);
			echo('<p class="content">'.$item['summary'].'</p></div>');
		}
		echo('</html>');
	}
}

new NewsFormatter();
