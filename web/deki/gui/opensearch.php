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

class OpenSearchFormatter extends DekiFormatter
{
	protected $contentType = null;


	public function format() 
	{
		global $wgDreamServer, $wgDekiApi, $wgRequest;
		$type = $wgRequest->getVal('type');
		$sortby = $wgRequest->getVal('sortby');
		$constraint = $wgRequest->getVal('constraint');

		if (!empty($type) && $type == 'description')
		{
			$Plug = new Plug($wgDreamServer, null);
			$scheme = (isset($_SERVER['HTTPS']) &&  $_SERVER['HTTPS'] == "on") ? 'https': 'http';
			$r = $Plug->At($wgDekiApi)
				->At('site', 'opensearch', 'description')
				->With('dream.in.scheme', $scheme)
				->With('dream.in.host', $_SERVER['HTTP_HOST'])
				->Get();
			if ($r['status'] == 200) 
			{
				header('Content-type: '.$r['type']);
				echo($r['body']);
			}
		}
		//rss feed subscription
		else 
		{
			$query = $wgRequest->getVal('q');
			if (!is_null($query))
			{
				$Plug = new Plug($wgDreamServer, null);
				$r = $Plug->At($wgDekiApi)
					->At('site', 'opensearch')
					->With('q', $query)
					->With('constraint', $constraint)
					->With('sortby', $sortby)
					->With('dream.in.host', $_SERVER['HTTP_HOST'])
					->Get();
				if ($r['status'] == 200) 
				{
					header('Content-type: '.$r['type']);
					echo($r['body']);
				}
			}
		}
	}
}

new OpenSearchFormatter();
