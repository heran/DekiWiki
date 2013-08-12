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

if (defined('MINDTOUCH_DEKI')) :
 
/**
 * Definitions of the NS_ constants are in Defines.php
 * @private
 */
$wgCanonicalNamespaceNames = array(
	NS_ADMIN			=> 'Admin', 
	NS_MEDIA            => 'Media',
	NS_SPECIAL          => 'Special',
	NS_TALK	            => 'Talk',
	NS_USER             => 'User',
	NS_USER_TALK        => 'User_talk',
	NS_PROJECT          => 'Project',
	NS_PROJECT_TALK     => 'Project_talk',
	NS_IMAGE            => 'Image',
	NS_IMAGE_TALK       => 'Image_talk',
	NS_MEDIAWIKI        => 'MediaWiki',
#	NS_MEDIAWIKI_TALK   => 'MediaWiki_talk',
	NS_TEMPLATE         => 'Template',
	NS_TEMPLATE_TALK    => 'Template_talk',
	NS_HELP             => 'Help',
	NS_HELP_TALK        => 'Help_talk',
	NS_CATEGORY	        => 'Category',
	NS_CATEGORY_TALK    => 'Category_talk',
	NS_ATTACHMENT	    => 'File',
);

/**
 * This is a utility class with only static functions
 * for dealing with namespaces that encodes all the
 * "magic" behaviors of them based on index.  The textual
 * names of the namespaces are handled by Language.php.
 *
 * These are synonyms for the names given in the language file
 * Users and translators should not change them
 */
class DekiNamespace
{
	/**
	 * Check if the given namespace might be moved
	 * @return bool
	 */
	public static  function isMovable($index)
	{
		if ( $index < NS_MAIN || $index == NS_ADMIN  || $index == NS_SPECIAL ) { 
			return false; 
		}
		return true;
	}

	/**
	 * Check if the give namespace is a talk page
	 * @return bool
	 */
	public static function isTalk($index)
	{
		return ( $index == NS_TALK           || $index == NS_USER_TALK     ||
				 /* MT ursm $index == NS_PROJECT_TALK  || */ $index == NS_IMAGE_TALK    ||
				 /* MT ursm $index == NS_MEDIAWIKI_TALK || */ $index == NS_TEMPLATE_TALK ||
				 $index == NS_HELP_TALK      || $index == NS_CATEGORY_TALK 
				 );
	}

	/**
	 * Get the talk namespace corresponding to the given index
	 */
	public static function getTalk($index)
	{
		if (DekiNamespace::isTalk($index)) {
			return $index;
		} else {
			# FIXME
			return $index + 1;
		}
	}

	public static function getSubject($index)
	{
		if (DekiNamespace::isTalk($index)) {
			return $index - 1;
		} else {
			return $index;
		}
	}

	/**
	 * Returns the canonical (English Wikipedia) name for a given index
	 */
	public static function &getCanonicalName($index)
	{
		global $wgCanonicalNamespaceNames;
		return $wgCanonicalNamespaceNames[$index];
	}

	/**
	 * Returns the index for a given canonical name, or NULL
	 * The input *must* be converted to lower case first
	 */
	public static function &getCanonicalIndex($name)
	{
		global $wgCanonicalNamespaceNames, $wgCanonicalAlternateNamespaces;
		static $xNamespaces = false;
		if ( $xNamespaces === false ) {
			$xNamespaces = array();
			foreach ( $wgCanonicalNamespaceNames as $i => $text ) {
				$xNamespaces[strtolower($text)] = $i;
			}			
		}
		if ( array_key_exists( $name, $xNamespaces ) ) {
			return $xNamespaces[$name];
		} else {
			if (!empty($wgCanonicalAlternateNamespaces) && array_key_exists(mb_strtolower($name), $wgCanonicalAlternateNamespaces)) {
				return $wgCanonicalAlternateNamespaces[mb_strtolower($name)];
			}
		    $ret = false;
			return $ret;
		}
	}
}

endif;
