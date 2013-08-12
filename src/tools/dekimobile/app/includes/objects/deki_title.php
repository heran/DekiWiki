<?php
/*
 * MindTouch Deki - enterprise collaboration and integration platform
 * Copyright (C) 2006-2008 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
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
class DekiTitle 
{
	// takes the query param from apache and converts to the api-ready version
	function convertForPath($title) 
	{
	    $search = array( '%', '[', ']', '{', '}', '|', '+', '<', '>', '#');
	    $replace = array('%25', '%5B', '%5D', '%7B', '%7D', '%7C', '%2B', '%3C', '%3E', '%23');
	    return str_replace($search, $replace, $title);
	}


  // gets pathname for API
  function getPathName($pageName, $idType)
  {
    $pageId = '';
    if($idType == 'idnum')
    {
      $pageId = $pageName;    
    }
    else if($idType == 'title')
    {
//      $pageId = '=' . urlencode(urlencode(DekiTitle::convertForPath($pageName)));
      $pageId = '=' . $pageName;
    }
    else
    {// return user to homepage
      $pageId = 'home';
    }
    return $pageId;
  }

}
?>
