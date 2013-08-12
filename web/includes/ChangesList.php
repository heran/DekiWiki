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

class ChangesList {
	# Called by history lists and recent changes
	#

	function ChangesList( &$skin ) {
		$this->skin =& $skin;
	}
	
	# Returns text for the start of the tabular part of RC
	function beginRecentChangesList() {
		$this->rc_cache = array() ;
		$this->rcMoveIndex = 0;
		$this->rcCacheIndex = 0 ;
		$this->lastdate = '';
		$this->rclistOpen = false;
		return '';
	}

	/**
 	 * Returns text for the end of RC
	 * If enhanced RC is in use, returns pretty much all the text
	 */
	function endRecentChangesList() {
		$s = $this->recentChangesBlock() ;
		if( $this->rclistOpen ) {
			$s .= "</table></div>\n";
		}
		return $s;
	}

	/**
	 * Enhanced RC ungrouped line
	 */
	function recentChangesBlockLine ( $rcObj ) {
		global $wgStylePath, $wgContLang ;

		# Get rc_xxxx variables
		extract( $rcObj->mAttribs ) ;
		$curIdEq = 'curid='.$rc_page_id;

		# Spacer image
		$r = '' ;

		$r .= '<img src="'.$wgStylePath.'/common/images/Arr_.png" width="12" height="12" border="0" />' ;
		$r .= '<tt>' ;

		if ( $rc_type == RC_MOVE || $rc_type == RC_MOVE_OVER_REDIRECT ) {
			$r .= '&nbsp;&nbsp;&nbsp;';
		} else {
			# M, N and !
			$M = wfMsg('Article.History.abbreviation-minor-edit');
			$N = wfMsg('Article.History.abbreviation-new-page');
			$U = wfMsg('Article.History.abbreviation-unpatrolled');

			if ( $rc_type == RC_NEW ) {
				$r .= '<span class="newpage">'. htmlspecialchars($N) .'</span>';
			} else {
				$r .= '&nbsp;' ;
			}
			if ( $rcObj->unpatrolled ) {
				$r .= '<span class="unpatrolled">'. htmlspecialchars($M) .'</span>';
			} else {
				$r .= '&nbsp;';
			}
		}

		# Timestamp
		$r .= ' '.$rcObj->timestamp.' ' ;
		$r .= '</tt>' ;

		# Article link
		$link = $rcObj->link ;
		if ( $rcObj->watched ) $link = '<strong>'.$link.'</strong>' ;
		$r .= $link ;

		# Diff
		$r .= ' (' ;
		$r .= $rcObj->difflink ;
		$r .= '; ' ;

		# Hist
		$r .= $this->skin->makeKnownLinkObj( $rcObj->getTitle(), wfMsg('Article.History.abbreviation-history'), $curIdEq.'&action=history' );

		# User/talk
		$r .= ') . . '.$rcObj->userlink ;
		$r .= $rcObj->usertalklink ;

		# Comment
		 if ( $rc_comment != '' && $rc_type != RC_MOVE && $rc_type != RC_MOVE_OVER_REDIRECT ) {
			$rc_comment=$this->skin->formatComment($rc_comment, $rcObj->getTitle());
			$r .= $wgContLang->emphasize( ' ('.$rc_comment.')' );
		}

		$r .= "<br />\n" ;
		return $r ;
	}

	/**
	 * Enhanced RC group
	 */
	function recentChangesBlockGroup ( $block ) {
		global $wgStylePath, $wgContLang ;

		$r = '' ;
		$M = wfMsg('Article.History.abbreviation-minor-edit');
		$N = wfMsg('Article.History.abbreviation-new-page');
		$U = wfMsg('Article.History.abbreviation-unpatrolled');

		# Collate list of users
		$isnew = false ;
		$unpatrolled = false;
		$userlinks = array () ;
		foreach ( $block AS $rcObj ) {
			$oldid = $rcObj->mAttribs['rc_last_oldid'];
			if ( $rcObj->mAttribs['rc_new'] ) {
				$isnew = true ;
			}
			$u = $rcObj->userlink ;
			if ( !isset ( $userlinks[$u] ) ) {
				$userlinks[$u] = 0 ;
			}
			if ( $rcObj->unpatrolled ) {
				$unpatrolled = true;
			}
			$userlinks[$u]++ ;
		}

		# Sort the list and convert to text
		krsort ( $userlinks ) ;
		asort ( $userlinks ) ;
		$users = array () ;
		foreach ( $userlinks as $userlink => $count) {
			$text = $userlink ;
			if ( $count > 1 ) $text .= " ({$count}&times;)" ;
			array_push ( $users , $text ) ;
		}
		$users = ' <font size="-1">['.implode('; ',$users).']</font>' ;

		# Arrow
		$rci = 'RCI'.$this->rcCacheIndex ;
		$rcl = 'RCL'.$this->rcCacheIndex ;
		$rcm = 'RCM'.$this->rcCacheIndex ;
		$toggleLink = "javascript:toggleVisibility('$rci','$rcm','$rcl')" ;
		$arrowdir = $wgContLang->isRTL() ? 'l' : 'r';
		$tl  = '<span id="'.$rcm.'"><a href="'.$toggleLink.'"><img src="'.$wgStylePath.'/common/images/Arr_'.$arrowdir.'.png" width="12" height="12" alt="+" /></a></span>' ;
		$tl .= '<span id="'.$rcl.'" style="display:none"><a href="'.$toggleLink.'"><img src="'.$wgStylePath.'/common/images/Arr_d.png" width="12" height="12" alt="-" /></a></span>' ;
		$r .= $tl ;

		# Main line
		# M/N
		$r .= '<tt>' ;
		if ( $isnew ) {
			$r .= '<span class="newpage">'. htmlspecialchars($N) .'</span>';
		} else {
			$r .= '&nbsp;';
		}
		$r .= '&nbsp;'; # Minor
		if ( $unpatrolled ) {
			$r .= '<span class="unpatrolled">'. htmlspecialchars($U) .'</span>';
		} else {
			$r .= "&nbsp;";
		}

		# Timestamp
		$r .= ' '.$block[0]->timestamp.' ' ;
		$r .= '</tt>' ;

		# Article link
		$link = $block[0]->link ;
		if ( $block[0]->watched ) $link = '<strong>'.$link.'</strong>' ;
		$r .= $link ;

		$curIdEq = 'curid=' . $block[0]->mAttribs['rc_page_id'];
		if ( $block[0]->mAttribs['rc_type'] != RC_LOG ) {
			# Changes
			$r .= ' ('.count($block).' ' ;
			if ( $isnew ) $r .= wfMsg('Article.History.changes');
			else $r .= $this->skin->makeKnownLinkObj( $block[0]->getTitle() , wfMsg('Article.History.changes') ,
				$curIdEq.'&diff=0&oldid='.$oldid ) ;
			$r .= '; ' ;

			# History
			$r .= $this->skin->makeKnownLinkObj( $block[0]->getTitle(), wfMsg('Article.History.versions-history'), $curIdEq.'&action=history' );
			$r .= ')' ;
		}

		$r .= $users ;
		$r .= "<br />\n" ;

		# Sub-entries
		$r .= '<div id="'.$rci.'" style="display:none">' ;
		foreach ( $block AS $rcObj ) {
			# Get rc_xxxx variables
			extract( $rcObj->mAttribs );

			$r .= '<img src="'.$wgStylePath.'/common/images/Arr_.png" width="12" height="12" />';
			$r .= '<tt>&nbsp; &nbsp; &nbsp; &nbsp;' ;
			if ( $rc_new ) {
				$r .= '<span class="newpage">' . htmlspecialchars( $N ) . '</span>';
			} else {
				$r .= '&nbsp;' ;
			}

			if ( $rcObj->unpatrolled ) {
				$r .= '<span class="unpatrolled">!</span>';
			} else {
				$r .= "&nbsp;";
			}

			$r .= '&nbsp;</tt>' ;

			$o = '' ;
			if ( $rc_last_oldid != 0 ) {
				$o = 'oldid='.$rc_last_oldid ;
			}
			if ( $rc_type == RC_LOG ) {
				$link = $rcObj->timestamp ;
			} else {
				$link = $this->skin->makeKnownLinkObj( $rcObj->getTitle(), $rcObj->timestamp , "{$curIdEq}&$o" ) ;
			}
			$link = '<tt>'.$link.'</tt>' ;

			$r .= $link ;
			$r .= ' (' ;
			$r .= $rcObj->curlink ;
			$r .= '; ' ;
			$r .= $rcObj->lastlink ;
			$r .= ') . . '.$rcObj->userlink ;
			$r .= $rcObj->usertalklink ;
			if ( $rc_comment != '' ) {
				$rc_comment=$this->skin->formatComment($rc_comment, $rcObj->getTitle());
				$r .= $wgContLang->emphasize( ' ('.$rc_comment.')' ) ;
			}
			$r .= "<br />\n" ;
		}
		$r .= "</div>\n" ;

		$this->rcCacheIndex++ ;
		return $r ;
	}

	/**
	 * If enhanced RC is in use, this function takes the previously cached
	 * RC lines, arranges them, and outputs the HTML
	 */
	function recentChangesBlock () {
		global $wgStylePath ;
		if ( count ( $this->rc_cache ) == 0 ) return '' ;
		$blockOut = '';
		foreach ( $this->rc_cache AS $secureName => $block ) {
			if ( count ( $block ) < 2 ) {
				$blockOut .= $this->recentChangesBlockLine ( array_shift ( $block ) ) ;
			} else {
				$blockOut .= $this->recentChangesBlockGroup ( $block ) ;
			}
		}

		return '<div>'.$blockOut.'</div>' ;
	}

	/**
	 * Called in a loop over all displayed RC entries
	 * Either returns the line, or caches it for later use
	 */
	function recentChangesLine( &$rc, $watched = false, $i = 0 ) {
		global $wgUser ;
		$line = $this->recentChangesLineOld ( $rc, $watched, $i ) ;
		return $line ;
	}
	
	/**
	 * @param DomTable &$Table reference to table object to add the new line to
	 */
	function recentChangesLineOld(&$rc, $watched = false, $i = 0, DomTable &$Table)
	{
		global $wgTitle, $wgLang, $wgContLang, $wgUser;
		$User = DekiUser::getCurrent();
		$fname = 'Skin::recentChangesLineOld';
		
		static $message;
		if (!isset($message))
		{
			$messages = array('diff' => 'abbreviation-diff',
							  'hist' => 'abbreviation-history',
							  'minoreditletter' => 'abbreviation-minor-edit',
							  'newpageletter' => 'abbreviation-new-page',
							  'blocklink' => 'abbreviation-blocklink',
							  'undo' => 'abbreviation-undo'
							  );
			foreach ($messages as $key => $msg)
			{
				$message[$key] = wfMsg('Article.History.'. $msg);
			}
		}
		
		# Extract DB fields into local scope
		extract( $rc->mAttribs );
		$curIdEq = 'curid=' . $rc_cur_id;

		$id = $rc_expand_id;
		$class = ($i & 1) ? 'bg1' : 'bg2';
		// start a new row for the line
		$Row = $Table->addRow(false);
	
		$contents = '';
		if (!empty($rc->mAttribs['isParent']))
		{
			$Row->addClass('groupparent');
			$contents = '<span class="toctoggle"><a href="#" onclick="return toggleChangesTable(\''
                .$id.'\')" class="internal"><span id="showlink-'.$id.'" '
                .'style="display:none;">'.Skin::iconify('expand').'</span>'
                .'<span id="hidelink-'.$id.'">'.Skin::iconify('contract')
                .'</span></a></span>';
		}
		else if (!empty($rc->mAttribs['isChild']))
		{
			$Row->addClass('group');
			$contents = Skin::iconify('dotcontinue');
			$Row->setAttribute('style', 'display:none;');
			$Row->addClass($id);
		}
		else
		{
			$Row->setAttribute('id', $id);
			$contents = '&nbsp;';	
		}
		$Row->addClass($class);

		// Expand button
		$Col = $Table->addCol($contents)->addClass($class)->addClass('noright');
		
		// Generate the page title contents
		$contents = '';
		if ($rc_type == RC_MOVE || $rc_type == RC_MOVE_OVER_REDIRECT)
		{
			// Undo
			$movePageTitle = Title::makeTitle( NS_SPECIAL, 'Movepage' );
			$movedToTitle = $rc->getMovedToTitle();
			$movedFromTitle = $rc->getTitle();

			// "[[x]] moved to [[y]]"
			$msg = ( $rc_type == RC_MOVE ) ? 'Article.History.x-moved-to-y' : 'Article.History.x-moved-to-y-over-redirect';
			$contents = wfMsg( $msg, $this->skin->makeKnownLinkObj( $rc->getTitle(), $rc->getTitle()->getDisplayText(), 'redirect=no' ),
				$this->skin->makeKnownLinkObj( $movedToTitle, $movedToTitle->getDisplayText() ) );
		}
		else
		{
			wfProfileIn("$fname-page");
			$articleLink = $this->skin->makeKnownLinkObj($rc->getTitle(), $rc->getTitle()->getDisplayText());
			
			if ($watched)
			{
				$articleLink = '<strong>' . $articleLink . '</strong>';
			}
			if ($rc_type == RC_NEW)
			{
				$articleLink = '<span class="type-new">' . $articleLink . '</span>';	
			}

			$contents = ' ' . $articleLink;
			wfProfileOut("$fname-page");
		}

		// Page title
		$Table->addCol($contents)->addClass($class);
		
		wfProfileIn("$fname-rest");
		$date = $wgLang->date( $rc_timestamp, true );
		$time = $wgLang->time( $rc_timestamp, true, false );

		// Timestamp
		$Table->addCol($time)->addClass($class);

		// Edited By
		$displayName = isset($rc_full_name) && !empty($rc_full_name) ? $rc_full_name : $rc_user_name;

		$userPage = Title::makeTitle(NS_USER, wfEncodeTitle($rc_user_name));
		$userLink = $this->skin->makeLinkObj($userPage, htmlspecialchars($displayName));
		
		$Table->addCol($userLink)->addClass($class);

		// Generate the comment contents
		$contents = '&nbsp;';
		if ('' != $rc_comment && '*' != $rc_comment && $rc_type != RC_MOVE && $rc_type != RC_MOVE_OVER_REDIRECT)
		{
			$rc_comment = $this->skin->formatComment($rc_comment, $rc->getTitle());
			$contents = $wgContLang->emphasize($rc_comment);
		}
		
		// if the page was edited, show the diff link appended to the comments
		if ($rc->mAttribs['rc_type'] == 0) 
		{
			$Title = $rc->getTitle();
			$rev = $rc->mAttribs['rc_revision'];
			$link = $Title->getLocalUrl('diff='.($rev).'&revision='.($rev - 1)); 
			$contents = $contents . ' <a href="'.$link.'">#</a>';
		}
		
		// Comments
		$Table->addCol($contents)->addClass($class);

		wfProfileOut("$fname-rest");
	}
}