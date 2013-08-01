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
 *
 * @package MediaWiki
 */

/**
 * Various globals
 */
define( 'RC_EDIT', 0);
define( 'RC_NEW', 1);
define( 'RC_MOVE', 2);
define( 'RC_LOG', 3);
define( 'RC_MOVE_OVER_REDIRECT', 4);

// MT (steveb): added flag for identifying file operations
define( 'RC_FILE_OP', 50);
define( 'RC_PAGEMETA', 51);


/**
 * Utility class for creating new RC entries
 * mAttribs:
 * 	rc_id           id of the row in the recentchanges table
 * 	rc_timestamp    time the entry was made
 * 	rc_cur_time     timestamp on the cur row
 * 	rc_namespace    namespace #
 * 	rc_title        non-prefixed db key
 * 	rc_type         is new entry, used to determine whether updating is necessary
 * 	rc_minor        is minor
 * 	rc_cur_id       id of associated cur entry
 * 	rc_user	        user id who made the entry
 * 	rc_user_text    user name who made the entry
 * 	rc_comment      edit summary
 * 	rc_this_oldid   old_id associated with this entry (or zero)
 * 	rc_last_oldid   old_id associated with the entry before this one (or zero)
 * 	rc_bot          is bot, hidden
 * 	rc_ip           IP address of the user in dotted quad notation
 * 	rc_new          obsolete, use rc_type==RC_NEW
 * 	rc_patrolled    boolean whether or not someone has marked this edit as patrolled
 * 
 * mExtra:
 * 	prefixedDBkey   prefixed db key, used by external app via msg queue
 * 	lastTimestamp   timestamp of previous entry, used in WHERE clause during update
 * 	lang            the interwiki prefix, automatically set in save()
 *  oldSize         text size before the change
 *  newSize         text size after the change
 * 
 * @todo document functions and variables
 * @package MediaWiki
 */
class RecentChange
{
	var $mAttribs = array(), $mExtra = array();
	var $mTitle = false, $mMovedToTitle = false;

	# Factory methods

	/* static */ function newFromRow( $row )
	{
		$rc = new RecentChange;
		$rc->loadFromRow( $row );
		return $rc;
	}

	/* static */ function newFromCurRow( $row )
	{
		$rc = new RecentChange;
		$rc->loadFromCurRow( $row );
		return $rc;
	}

	# Accessors

	function setAttribs( $attribs )
	{
		$this->mAttribs = $attribs;
	}

	function setExtra( $extra )
	{
		$this->mExtra = $extra;
	}

	function &getTitle()
	{
		if ( $this->mTitle === false ) {
			$this->mTitle = Title::makeTitle( $this->mAttribs['rc_namespace'], $this->mAttribs['rc_title'] );
		}
		return $this->mTitle;
	}

	function getMovedToTitle()
	{
		if ( $this->mMovedToTitle === false ) {
			$this->mMovedToTitle = Title::makeTitle( $this->mAttribs['rc_moved_to_ns'],
				$this->mAttribs['rc_moved_to_title'] );
		}
		return $this->mMovedToTitle;
	}

	# Makes an entry in the database corresponding to an edit
	/*static*/ function notifyEdit( $timestamp, &$title, $minor, &$user, $comment,
		$oldId, $lastTimestamp, $bot = "default", $ip = '', $oldSize = 0, $newSize = 0 )
	{
		if ( $bot == 'default ' ) {
			$bot = 0;
		}

		if ( !$ip ) {
			global $wgIP;
			$ip = empty( $wgIP ) ? '' : $wgIP;
		}

		$rc = new RecentChange;
		$rc->mAttribs = array(
			'rc_timestamp'	=> $timestamp,
			'rc_cur_time'	=> $timestamp,
			'rc_namespace'	=> $title->getNamespace(),
			'rc_title'	=> $title->getDBkey(),
			'rc_type'	=> RC_EDIT,
			'rc_minor'	=> $minor ? 1 : 0,
			'rc_cur_id'	=> $title->getArticleID(),
			'rc_user'	=> $user->getID(),
			'rc_user_text'	=> $user->getName(),
			'rc_comment'	=> $comment,
			'rc_this_oldid'	=> 0,
			'rc_last_oldid'	=> $oldId,
			'rc_bot'	=> $bot ? 1 : 0,
			'rc_moved_to_ns'	=> 0,
			'rc_moved_to_title'	=> '',
			'rc_ip'	=> $ip,
			'rc_patrolled' => 0,
			'rc_new'	=> 0 # obsolete
		);

		$rc->mExtra =  array(
			'prefixedDBkey'	=> $title->getPrefixedDBkey(),
			'lastTimestamp' => $lastTimestamp,
			'oldSize'       => $oldSize,
			'newSize'       => $newSize,
		);
		$rc->save();
	}

	# Makes an entry in the database corresponding to page creation
	# Note: the title object must be loaded with the new id using resetArticleID()
	/*static*/ function notifyNew( $timestamp, &$title, $minor, &$user, $comment, $bot = "default", 
	  $ip='', $size = 0 )
	{
		if ( !$ip ) {
			global $wgIP;
			$ip = empty( $wgIP ) ? '' : $wgIP;
		}

		$rc = new RecentChange;
		$rc->mAttribs = array(
			'rc_timestamp'      => $timestamp,
			'rc_cur_time'       => $timestamp,
			'rc_namespace'      => $title->getNamespace(),
			'rc_title'          => $title->getDBkey(),
			'rc_type'           => RC_NEW,
			'rc_minor'          => $minor ? 1 : 0,
			'rc_cur_id'         => $title->getArticleID(),
			'rc_user'           => $user->getID(),
			'rc_user_text'      => $user->getName(),
			'rc_comment'        => $comment,
			'rc_this_oldid'     => 0,
			'rc_last_oldid'     => 0,
			'rc_bot'            => $bot ? 1 : 0,
			'rc_moved_to_ns'    => 0,
			'rc_moved_to_title' => '',
			'rc_ip'             => $ip,
			'rc_patrolled'      => 0,
			'rc_new'	=> 1 # obsolete
		);

		$rc->mExtra =  array(
			'prefixedDBkey'	=> $title->getPrefixedDBkey(),
			'lastTimestamp' => 0,
			'oldSize' => 0,
			'newSize' => $size
		);
		$rc->save();
	}

	# A log entry is different to an edit in that previous revisions are
	# not kept
	/*static*/ function notifyLog( $timestamp, &$title, &$user, $comment, $ip='' )
	{
		if ( !$ip ) {
			global $wgIP;
			$ip = empty( $wgIP ) ? '' : $wgIP;
		}
		$rc = new RecentChange;
		$rc->mAttribs = array(
			'rc_timestamp'	=> $timestamp,
			'rc_cur_time'	=> $timestamp,
			'rc_namespace'	=> $title->getNamespace(),
			'rc_title'	=> $title->getDBkey(),
			'rc_type'	=> RC_LOG,
			'rc_minor'	=> 0,
			'rc_cur_id'	=> $title->getArticleID(),
			'rc_user'	=> $user->getID(),
			'rc_user_text'	=> $user->getName(),
			'rc_comment'	=> $comment,
			'rc_this_oldid'	=> 0,
			'rc_last_oldid'	=> 0,
			'rc_bot'	=> 0,
			'rc_moved_to_ns'	=> 0,
			'rc_moved_to_title'	=> '',
			'rc_ip'	=> $ip,
			'rc_patrolled' => 1,
			'rc_new'	=> 0 # obsolete
		);
		
		$rc->mExtra =  array(
			'prefixedDBkey'	=> $title->getPrefixedDBkey(),
			'lastTimestamp' => 0,
		);
		$rc->save();
	}

	// MT (steveb): added new notify method
	# Makes an entry in the database corresponding to a file attachment
	# Note: the title object must be loaded with the new id using resetArticleID()
	/*static*/ function notifyFileOperation( $timestamp, &$title, &$user, $comment, $bot = "default", $ip='', $op = RC_FILE_OP )
	{
		if ( !$ip ) {
			global $wgIP;
			$ip = empty( $wgIP ) ? '' : $wgIP;
		}

		$rc = new RecentChange;
		$rc->mAttribs = array(
			'rc_timestamp'      => $timestamp,
			'rc_cur_time'       => $timestamp,
			'rc_namespace'      => $title->getNamespace(),
			'rc_title'          => $title->getDBkey(),
			'rc_type'           => $op,
			'rc_minor'          => 0,
			'rc_cur_id'         => $title->getArticleID(),
			'rc_user'           => $user->getID(),
			'rc_user_text'      => $user->getName(),
			'rc_comment'        => $comment,
			'rc_this_oldid'     => 0,
			'rc_last_oldid'     => 0,
			'rc_bot'            => $bot ? 1 : 0,
			'rc_moved_to_ns'    => 0,
			'rc_moved_to_title' => '',
			'rc_ip'             => $ip,
			'rc_patrolled'      => 0,
			'rc_new'	=> 0 # obsolete
		);

		$rc->mExtra =  array(
			'prefixedDBkey'	=> $title->getPrefixedDBkey(),
			'lastTimestamp' => 0
		);
		$rc->save();
	}

	# A log entry is different to an edit in that previous revisions are
	# not kept
	/*static*/ function notifyPageMeta( $timestamp, &$title, &$user, $comment, $ip='' )
	{
		if ( !$ip ) {
			global $wgIP;
			$ip = empty( $wgIP ) ? '' : $wgIP;
		}
		$rc = new RecentChange;
		$rc->mAttribs = array(
			'rc_timestamp'	=> $timestamp,
			'rc_cur_time'	=> $timestamp,
			'rc_namespace'	=> $title->getNamespace(),
			'rc_title'	=> $title->getDBkey(),
			'rc_type'	=> RC_PAGEMETA,
			'rc_minor'	=> 0,
			'rc_cur_id'	=> $title->getArticleID(),
			'rc_user'	=> $user->getID(),
			'rc_user_text'	=> $user->getName(),
			'rc_comment'	=> $comment,
			'rc_this_oldid'	=> 0,
			'rc_last_oldid'	=> 0,
			'rc_bot'	=> 0,
			'rc_moved_to_ns'	=> 0,
			'rc_moved_to_title'	=> '',
			'rc_ip'	=> $ip,
			'rc_patrolled' => 1,
			'rc_new'	=> 0 # obsolete
		);
		
		$rc->mExtra =  array(
			'prefixedDBkey'	=> $title->getPrefixedDBkey(),
			'lastTimestamp' => 0,
		);
		$rc->save();
	}

	# Initialises the members of this object from a mysql row object
	function loadFromRow( $row )
	{
		$this->mAttribs = get_object_vars( $row );
		$this->mExtra = array();
	}

	# Makes a pseudo-RC entry from a cur row, for watchlists and things
	function loadFromCurRow( $row )
	{		
		$this->mAttribs = array(
			'rc_timestamp' => $row->page_timestamp,
			'rc_cur_time' => $row->page_timestamp,
			'rc_user' => $row->page_user_id,
			'rc_user_text' => DekiUser::newFromId($row->page_user_id)->getName(),
			'rc_namespace' => $row->page_namespace,
			'rc_title' => $row->page_title,
			'rc_comment' => $row->page_comment,
			'rc_minor' => !!$row->page_minor_edit,
			'rc_type' => $row->page_is_new ? RC_NEW : RC_EDIT,
			'rc_cur_id' => $row->page_id,
			'rc_this_oldid'	=> 0,
			'rc_last_oldid'	=> 0,
			'rc_bot'	=> 0,
			'rc_moved_to_ns'	=> 0,
			'rc_moved_to_title'	=> '',
			'rc_ip' => '',
			'rc_patrolled' => '1',  # we can't support patrolling on the Watchlist
			                        # currently because it uses cur, not recentchanges
			'rc_new' => $row->page_is_new # obsolete
		);

		$this->mExtra = array();
	}


	/**
	 * Gets the end part of the diff URL assoicated with this object
	 * Blank if no diff link should be displayed
	 */
	function diffLinkTrail( $forceCur )
	{
		if ( $this->mAttribs['rc_type'] == RC_EDIT ) {
			$trail = "curid=" . (int)($this->mAttribs['rc_cur_id']) .
				"&oldid=" . (int)($this->mAttribs['rc_last_oldid']);
			if ( $forceCur ) {
				$trail .= '&diff=0' ;
			} else {
				$trail .= '&diff=' . (int)($this->mAttribs['rc_this_oldid']);
			}
		} else {
			$trail = '';
		}
		return $trail;
	}
}