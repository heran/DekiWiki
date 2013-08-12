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
 * Contain a class for special pages
 * @package MediaWiki
 */

/** */
class SearchEngine {
	var $limit = 10;
	var $offset = 0;
	var $searchTerms = array();
	var $namespaces = array( 0 );
	var $showRedirects = false;
	
	/**
	 * Perform a full text search query and return a result set.
	 *
	 * @param string $term - Raw search term
	 * @param array $namespaces - List of namespaces to search
	 * @return ResultWrapper
	 * @access public
	 */
	function searchText( $term , $count = false) {
		if (!$count) {
			return $this->db->resultObject( $this->db->query( $this->getQuery( $this->filter( $term ), true ) ) );
		}
		else {
			return $this->db->fetchRow($this->db->query( $this->getCount( $this->filter( $term ), true ) ) );
		}
	}

	/**
	 * Perform a title-only search query and return a result set.
	 *
	 * @param string $term - Raw search term
	 * @param array $namespaces - List of namespaces to search
	 * @return ResultWrapper
	 * @access public
	 */
	function searchTitle( $term , $count = false) {
		if (!$count) {
			return $this->db->resultObject( $this->db->query( $this->getQuery( $this->filter( $term ), false ) ) );
		}
		else {
			return $this->db->fetchRow($this->db->query( $this->getCount( $this->filter( $term ), false ) ) );
		}
	}
	
	/**
	 * If an exact title match can be find, or a very slightly close match,
	 * return the title. If no match, returns NULL.
	 *
	 * @static
	 * @param string $term
	 * @return Title
	 * @access private
	 */
	function getNearMatch( $term ) {
		# Exact match? No need to look further.
		$title = Title::newFromText( $term );
		if (is_null($title))
			return null;

		if ( $title->getNamespace() == NS_SPECIAL || 0 != $title->getArticleID() ) {
			return $title;
		}

		# Now try all lower case (i.e. first letter capitalized)
		#
		$title = Title::newFromText( strtolower( $term ) );
		if ( 0 != $title->getArticleID() ) {
			return $title;
		}

		# Now try capitalized string
		#
		$title = Title::newFromText( ucwords( strtolower( $term ) ) );
		if ( 0 != $title->getArticleID() ) {
			return $title;
		}

		# Now try all upper case
		#
		$title = Title::newFromText( strtoupper( $term ) );
		if ( 0 != $title->getArticleID() ) {
			return $title;
		}

		$title = Title::newFromText( $term );

		# Entering an IP address goes to the contributions page
//		if ( ( $title->getNamespace() == NS_USER && User::isIP($title->getText() ) )
//			|| User::isIP( trim( $term ) ) ) {
//			return Title::makeTitle( NS_SPECIAL, "Contributions/" . $title->getDbkey() );
//		}


		# Entering a user goes to the user page whether it's there or not
		if ( $title->getNamespace() == NS_USER ) {
			return $title;
		}
		
		# Quoted term? Try without the quotes...
		if( preg_match( '/^"([^"]+)"$/', $term, $matches ) ) {
			return SearchEngine::getNearMatch( $matches[1] );
		}
		
		return NULL;
	}
	
	function legalSearchChars() {
		return "A-Za-z_'0-9\\x80-\\xFF\\-";
	}

	/**
	 * Set the maximum number of results to return
	 * and how many to skip before returning the first.
	 *
	 * @param int $limit
	 * @param int $offset
	 * @access public
	 */
	function setLimitOffset( $limit, $offset = 0 ) {
		$this->limit = IntVal( $limit );
		$this->offset = IntVal( $offset );
	}
	
	/**
	 * Set which namespaces the search should include.
	 * Give an array of namespace index numbers.
	 *
	 * @param array $namespaces
	 * @access public
	 */
	function setNamespaces( $namespaces ) {
		$this->namespaces = $namespaces;
	}
	
	/**
	 * Make a list of searchable namespaces and their canonical names.
	 * @return array
	 * @access public
	 */
	function searchableNamespaces() {
		global $wgContLang;
		$arr = array();
		foreach( $wgContLang->getNamespaces() as $ns => $name ) {
			if( $ns >= 0 ) {
				$arr[$ns] = $name;
			}
		}
		return $arr;
	}
	
	/**
	 * Fetch an array of regular expression fragments for matching
	 * the search terms as parsed by this engine in a text extract.
	 *
	 * @return array
	 * @access public
	 */
	function termMatches() {
		return $this->searchTerms;
	}
	
	/**
	 * Return a 'cleaned up' search string
	 *
	 * @return string
	 * @access public
	 */
	function filter( $text ) {
		$lc = $this->legalSearchChars();
		return trim( preg_replace( "/[^{$lc}]/", " ", $text ) );
	}
	
	/**
	 * Return a partial WHERE clause to exclude redirects, if so set
	 * @return string
	 * @access private
	 */
	function queryRedirect() {
		if( $this->showRedirects ) {
			return 'AND page_is_redirect=0';
		} else {
			return '';
		}
	}
	
	/**
	 * Return a partial WHERE clause to limit the search to the given namespaces
	 * @return string
	 * @access private
	 */
	function queryNamespaces() {
		$namespaces = implode( ',', $this->namespaces );
		if ($namespaces == '') {
			$namespaces = '0';
		}
		return 'AND page_namespace IN (' . $namespaces . ')';
	}
	
	/**
	 * Return a LIMIT clause to limit results on the query.
	 * @return string
	 * @access private
	 */
	function queryLimit() {
		return $this->db->limitResult( $this->limit, $this->offset );
	}
	
	/**
	 * Construct the full SQL query to do the search.
	 * The guts shoulds be constructed in queryMain()
	 * @param string $filteredTerm
	 * @param bool $fulltext
	 * @access private
	 */
	function getQuery( $filteredTerm, $fulltext) {
		return $this->queryMain( $filteredTerm, $fulltext ) . ' ' .
			$this->queryRedirect() . ' ' .
			$this->queryNamespaces() . ' ' .
			$this->queryLimit();
	}
	
	function getCount( $filteredTerm, $fulltext) {
		return $this->queryMain( $filteredTerm, $fulltext, true );
	}
	
}

/** */
class SearchEngineDummy {
	function search( $term ) {
		return null;
	}
}


?>
