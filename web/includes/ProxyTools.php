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

if ( !defined( 'MINDTOUCH_DEKI' ) ) {
	die();
}

/**
 * Functions for dealing with proxies
 */


/**
 * Work out the IP address based on various globals
 */
function wfGetIP()
{
	/* collect the originating ips */
	# Client connecting to this webserver
	if ( isset( $_SERVER['REMOTE_ADDR'] ) ) {
		$ipchain = array( $_SERVER['REMOTE_ADDR'] );
	} else {
		# Running on CLI?
		$ipchain = array( '127.0.0.1' );
	}
	$ip = $ipchain[0];

	return $ip;
}

function wfIP2Unsigned( $ip )
{
	$n = ip2long( $ip );
	if ( $n == -1 ) {
		$n = false;
	} elseif ( $n < 0 ) {
		$n += pow( 2, 32 );
	}
	return $n;
}

/**
 * Determine if an IP address really is an IP address, and if it is public, 
 * i.e. not RFC 1918 or similar
 */
function wfIsIPPublic( $ip )
{
	$n = wfIP2Unsigned( $ip );
	if ( !$n ) {
		return false;
	}

	static $privateRanges = false;
	if ( !$privateRanges ) {
		$privateRanges = array(
			array( '10.0.0.0',    '10.255.255.255' ),   # RFC 1918 (private)
			array( '172.16.0.0',  '172.31.255.255' ),   #     "
			array( '192.168.0.0', '192.168.255.255' ),  #     "
			array( '0.0.0.0',     '0.255.255.255' ),    # this network
			array( '127.0.0.0',   '127.255.255.255' ),  # loopback
		);
	}

	foreach ( $privateRanges as $r ) {
		$start = wfIP2Unsigned( $r[0] );
		$end = wfIP2Unsigned( $r[1] );
		if ( $n >= $start && $n <= $end ) {
			return false;
		}
	}
	return true;
}
	
?>
