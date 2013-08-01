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
 * Compatibility functions for older versions of PHP
 */

if( !function_exists('stream_get_contents') ) 
{
    function stream_get_contents($stream)
    {
        $ret = '';
        while (!feof($stream))
        {
            $ret .= fread($stream, 8192);
        }
        return $ret;
    }
}

if (!function_exists('getallheaders')) 
{
	function getallheaders() 
	{
		$headers = array();
		foreach ($_SERVER as $name => $value) 
		{
			if (substr($name, 0, 5) == 'HTTP_') 
			{
				$headers[str_replace(' ', '-', ucwords(strtolower(str_replace('_', ' ', substr($name, 5)))))] = $value;
			}
		}
		return $headers;
	}
}
if (!function_exists('mime_content_type')) 
{
	if (!function_exists('finfo_open')) 
	{
		function mime_content_type($f) 
		{
			return trim(exec('file -bi '.escapeshellarg($f)));
		}
	}
	else 
	{
	    function mime_content_type($filename) 
	    {
	        $finfo = finfo_open(FILEINFO_MIME);
	        $mimetype = finfo_file($finfo, $filename);
	        finfo_close($finfo);
	        return $mimetype;
	    }
    }
}

// json_decde function only exists in PHP >= 5.2.1
if ( !function_exists('json_decode'))
{
    require_once 'JSON.php';
    function json_decode($content, $assoc = false)
    {
	    $json = $assoc 
	    	? new Services_JSON(SERVICES_JSON_LOOSE_TYPE)
	    	: new Services_JSON;
        return $json->decode($content);
    }
}

// json_encode function only exists in PHP >= 5.2.1
if ( !function_exists("json_encode") ) 
{
    require_once 'JSON.php';
    function json_encode($a)
    {
        $json = new Services_JSON;
        return $json->encode($a);
    }
}

?>
