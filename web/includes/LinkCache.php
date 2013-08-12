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
 * Cache for article titles (prefixed DB keys) and ids linked from one source
 * @package MediaWiki
 */

/**
 *
 */
# These are used in incrementalSetup()
define ('LINKCACHE_GOOD', 0);
define ('LINKCACHE_BAD', 1);
define ('LINKCACHE_IMAGE', 2);

/**
 *
 * @package MediaWiki
 */
class LinkCache {	
	// Increment $mClassVer whenever old serialized versions of this class
	// becomes incompatible with the new version.
	/* private */ var $mClassVer = 2;

	/* private */ var $mGoodLinks, $mBadLinks, $mActive;
	/* private */ var $mImageLinks, $mCategoryLinks;
	/* private */ var $mPreFilled, $mOldGoodLinks, $mOldBadLinks;
	/* private */ var $mForUpdate;

	/* private */ function getKey( $title ) {
		global $wgDBname;
		return $wgDBname.':lc:title:'.$title;
	}
	
	function LinkCache() {
		$this->mActive = true;
		$this->mPreFilled = false;
		$this->mForUpdate = false;
		$this->mGoodLinks = array();
		$this->mBadLinks = array();
		$this->mImageLinks = array();
		$this->mAttachmentLinks = array();
		$this->mCategoryLinks = array();
		$this->mOldGoodLinks = array();
		$this->mOldBadLinks = array();
	}

	/**
	 * General accessor to get/set whether SELECT FOR UPDATE should be used
	 */
	function forUpdate( $update = NULL ) { 
		return wfSetVar( $this->mForUpdate, $update );
	}
	
	function getGoodLinkID( $title ) {
		if ( array_key_exists( $title, $this->mGoodLinks ) ) {
			return $this->mGoodLinks[$title];
		} else {
			return 0;
		}
	}

	function isBadLink( $title ) {
		return array_key_exists( $title, $this->mBadLinks ); 
	}

	function addGoodLink( $id, $title ) {
		if ( $this->mActive ) {
			$this->mGoodLinks[$title] = $id;
		}
	}

	function addBadLink( $title ) {
		if ( $this->mActive && ( ! $this->isBadLink( $title ) ) ) {
			$this->mBadLinks[$title] = 1;
		}
	}

	function clearBadLink( $title ) {
		unset( $this->mBadLinks[$title] );
		$this->clearLink( $title );
	}
	
	function clearLink( $title ) {
	}

	function suspend() { $this->mActive = false; }
	function resume() { $this->mActive = true; }
	function getGoodLinks() { return $this->mGoodLinks; }
	function getBadLinks() { return array_keys( $this->mBadLinks ); }
	function getImageLinks() { return $this->mImageLinks; }
	function getCategoryLinks() { return $this->mCategoryLinks; }

	function addLink( $title ) {
		$nt = Title::newFromDBkey( $title );
		if( $nt ) {
			return $this->addLinkObj( $nt );
		} else {
			return 0;
		}
	}
	
	function addLinkObj( &$nt ) {
		global $wgDekiPlug;
		$title = $nt->getPrefixedDBkey();
		if ( $this->isBadLink( $title ) ) { return 0; }		
		$id = $this->getGoodLinkID( $title );
		if ( 0 != $id ) { return $id; }

		$fname = 'LinkCache::addLinkObj';
		
		$ns = $nt->getNamespace();
		$t = $nt->getDBkey();
		
		$id = NULL;
		if( ! is_integer( $id ) ) {
			if ( $this->mForUpdate ) {
				$db =& wfGetDB( DB_MASTER );
				if ( !( ALF_NO_LINK_LOCK ) ) {
					$options = array( 'FOR UPDATE' );
				}
			} else {
				$db =& wfGetDB( DB_SLAVE );
				$options = array();
			}
			$id = $db->selectField( 'pages', 'page_id', array( 'page_namespace' => $ns, 'page_title' => $t ), $fname, $options );
			
			if ( !$id ) {
				$id = 0;
        	}
    	}
		if ( 0 == $id ) { $this->addBadLink( $title ); }
		else { $this->addGoodLink( $id, $title ); }
		
		return $id;
	}
		

	function getGoodAdditions() {
		return array_diff( $this->mGoodLinks, $this->mOldGoodLinks );
	}

	function getBadAdditions() {
		#wfDebug( "mOldBadLinks: " . implode( ', ', array_keys( $this->mOldBadLinks ) ) . "\n" );
		#wfDebug( "mBadLinks: " . implode( ', ', array_keys( $this->mBadLinks ) ) . "\n" );
		return array_values( array_diff( array_keys( $this->mBadLinks ), array_keys( $this->mOldBadLinks ) ) );
	}

	function getImageAdditions() {
		return array_diff_assoc( $this->mImageLinks, $this->mOldImageLinks );
	}

	function getGoodDeletions() {
		return array_diff( $this->mOldGoodLinks, $this->mGoodLinks );
	}

	function getBadDeletions() {
		return array_values( array_diff( array_keys( $this->mOldBadLinks ), array_keys( $this->mBadLinks ) ));
	}

	function getImageDeletions() {
		return array_diff_assoc( $this->mOldImageLinks, $this->mImageLinks );
	}

	/**
	 * Clears cache
	 */
	function clear() {
		$this->mGoodLinks = array();
		$this->mBadLinks = array();
		$this->mImageLinks = array();
		$this->mCategoryLinks = array();
		$this->mOldGoodLinks = array();
		$this->mOldBadLinks = array();
		$this->mOldImageLinks = array();
	}

	/**
	 * @access private
	 */
	function saveToLinkscc( $pid ){
		// MT-7345 Database cleanup: Remove linkscc, objectcache, and querycache tables
		//$ser = gzcompress( serialize( $this ), 3 );
		//$db =& wfGetDB( DB_MASTER );
		//$db->replace( 'linkscc', array( 'lcc_pageid' ), array( 'lcc_pageid' => $pid, 'lcc_cacheobj' => $ser ) );
	}

	/**
	 * Delete linkscc rows which link to here
	 * @param $pid is a page id
	 * @static
	 */
	function linksccClearLinksTo( $pid ){
	}

	/**
	 * Delete linkscc rows with broken links to here
	 * @param $title is a prefixed db title for example like Title->getPrefixedDBkey() returns.
	 * @static
	 */
	function linksccClearBrokenLinksTo( $title ){
	}

	/**
	 * @param $pid is a page id
	 * @static
	 */
	function linksccClearPage( $pid ){
	}
}
