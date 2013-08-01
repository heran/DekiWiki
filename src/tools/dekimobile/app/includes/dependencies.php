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
 
// TODO: implement
function wfMsg() {}
function wfScrubSensitive() {}

/** Standard unix timestamp (number of seconds since 1 Jan 1970) */
define('TS_UNIX',0);
/** MediaWiki concatenated string timestamp (yyyymmddhhmmss) */
define('TS_MW',1);    
/** Standard database timestamp (yyyy-mm-dd hh:mm:ss) */
define('TS_DB',2);
/** Standard UTC time as used by Dream: "yyyy-MM-ddTHH:mm:ssZ" */
define('TS_DREAM',3);

/**
 * @todo document
 */
function wfTimestamp($outputtype=TS_UNIX,$ts=0) {
    if (preg_match("/^(\d{4})\-(\d\d)\-(\d\d) (\d\d):(\d\d):(\d\d)$/",$ts,$da)) {
        # TS_DB
        $uts=gmmktime((int)$da[4],(int)$da[5],(int)$da[6],
                (int)$da[2],(int)$da[3],(int)$da[1]);
    } elseif (preg_match("/^(\d{4})(\d\d)(\d\d)(\d\d)(\d\d)(\d\d)$/",$ts,$da)) {
        # TS_MW
        $uts=gmmktime((int)$da[4],(int)$da[5],(int)$da[6],
                (int)$da[2],(int)$da[3],(int)$da[1]);
    } elseif (preg_match("/^(\d{1,13})$/",$ts,$datearray)) {
        # TS_UNIX
        $uts=$ts;
    } elseif (preg_match("/^(\d{4})\-(\d\d)\-(\d\d)T(\d\d):(\d\d):(\d\d)Z$/",$ts,$da)) {
        $uts = gmmktime((int)$da[4],(int)$da[5],(int)$da[6],
                (int)$da[2],(int)$da[3],(int)$da[1]);;
    }

    if ($ts == 0)
    {
        $uts = time();
    }
    switch($outputtype) {
	    case TS_UNIX:
	        return $uts;
	        break;
	    case TS_MW:
	        return gmdate( 'YmdHis', $uts );
	        break;
	    case TS_DB:
	        return gmdate( 'Y-m-d H:i:s', $uts );
	        break;
	    case TS_DREAM: 
	    	return gmdate( 'Y-m-d', $uts).'T'. gmdate('h:i:s', $uts ).'Z';
	    	break;
	    default:
	        return;
    }
}

function wfFormatFileSize($size)
{
    if ( $size < 2*1024 )
	{
        return $size . ' bytes';
	}
	else if ($size < 2*1024*1024)
	{
        return (round($size / 10.24 )/100.0) . ' kb';
	}
	else if ($size < 2*1024*1024*1024)
	{
        return (round($size / ( 1024*10.24 ) )/100.0) . ' MB';
	}
	else
	{
		return (round($size / ( 1024*1024*10.24 ) )/100.0) . ' GB';
	}
}

/***
 * http://www.ilovejackdaniels.com/php/email-address-validation
 */
function wfValidateEmail($email) {
	if (!ereg("[^@]{1,64}@[^@]{1,255}", $email)) {     
		return false;
	}
	# Split it into sections to make life easier
	$email_array = explode("@", $email);
	$local_array = explode(".", $email_array[0]);
	for ($i = 0; $i < sizeof($local_array); $i++) {
		if (!ereg("^(([A-Za-z0-9!#$%&'*+/=?^_`{|}~-][A-Za-z0-9!#$%&'*+/=?^_`{|}~\.-]{0,63})|(\"[^(\\|\")]{0,62}\"))$", $local_array[$i])) {
			return false;
		}
	}  
	# Check if domain is IP. If not, it should be valid domain name
	if (!ereg("^\[?[0-9\.]+\]?$", $email_array[1])) {
		$domain_array = explode(".", $email_array[1]);
		if (sizeof($domain_array) < 2) {
			return false; // Not enough parts to domain
		}
		for ($i = 0; $i < sizeof($domain_array); $i++) {
			if (!ereg("^(([A-Za-z0-9][A-Za-z0-9-]{0,61}[A-Za-z0-9])|([A-Za-z0-9]+))$", $domain_array[$i])) {
				return false;
			}
		}
	}
	return $email;
}


