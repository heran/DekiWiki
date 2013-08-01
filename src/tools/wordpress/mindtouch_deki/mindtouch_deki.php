<?php
/***
	Plugin Name: MindTouch Deki
	Plugin URI: http://developer.mindtouch.com/
	Description: This plugin will synchronize all WordPress posts with a MindTouch installation.
	Version: 1.2.0 (Last updated 2009.07.22)
	Author: Roy Kim
	Author URI: http://roykim.net/
*/

/*  Copyright 2008 MindTouch, Inc. (email : royk@mindtouch.com)

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

include_once( ABSPATH . PLUGINDIR . '/mindtouch_deki/dream.php' );
 
/***
 * The hostname of your Deki
 * Examples: http://trunk.mindtouch.com
 */
define('DEKIHOST', ''); 
define('DEKIAPI', '@api/deki'); // this shouldn't need to be changed - keep it the same

/***
 * These are the credentials for the Deki you're writing to
 */
define('DEKIUSERNAME', '');
define('DEKIPASSWORD', '');

/***
 * The default path that the wordpress entries will write to; slashes denote a hierarchy. 
 * Remember to end the slash, or it'll prepend the last entry to your title!
 *
 * In WordPress, you can override this on a per-page basis by adding the tag "dekipath:New/Path/"  
 */
define('DEKIPATH', 'Blog Posts' .  '/'); 
// define('DEKIPATH', date('Y'). '/' . date('m') .  '/' . date('d') . '/'); // maps more closely to WP's url structure

/**
 * The default path for wordpress pages
 * 
 * It's similar to DEKIPATH 
 */
define('DEKIPATHPAGES', 'Blog Pages' .  '/');
// define('DEKIPATHPAGES', DEKIPATH . 'Pages' .  '/'); // for saving pages in DEKIPATH with Pages subpage


// This will be read from the wp tags to allow for selective targeting
define('DEKIWPTRIGGER', 'dekipath:');


/**
 * deki_getPlug() - Get a plug object, which will allow REST-based connections to Deki's API - this also authenticates
 *
 * @return obj DekiPlug object (see dream.php)
 */
function deki_getPlug() {
	static $Plug;
	if (!is_null($Plug)) {
		return $Plug;	
	}	
	$Plug = new DekiPlug(DEKIHOST, 'php', parse_url(DEKIHOST, PHP_URL_HOST));
	$Plug = $Plug->At(DEKIAPI)->WithCredentials(DEKIUSERNAME, DEKIPASSWORD);
	return $Plug;
}

/**
 * deki_publishPost() - called for WP hook publish_post; saves or edits the page
 *
 * @return mixed
 */
function deki_savePost($postId)
{
	$dekiPath = DEKIPATH;
	$hierarchyPath = '';
	
	$Plug = deki_getPlug();	
	
	$post = get_post($postId);
	
	// is revision?
	if ( 'revision' == $post->post_type ) {
		$postId = $post->post_parent;
		$post = get_post($postId);
	}
	
	$tags = wp_get_post_tags($postId);
	$categories = wp_get_post_categories($postId);
	$user = get_userdata($post->post_author);
	
	if ( "page" == $post->post_type ) {
		
		$ancestors = $post->ancestors;
		
		if ( count($ancestors) > 0 ) {
			foreach ( $ancestors as $ancestorPageId ) {
				$ancestorPage = get_page($ancestorPageId);
				
				$ancestorTitle = apply_filters( 'the_title', str_replace('/', '//', $ancestorPage->post_title) );
				$hierarchyPath .= $ancestorTitle . '/';
			}
		}
		
		$dekiPath = DEKIPATHPAGES;
	}
	
	// this plugin will combine all categories + tags from wp and convert them to dekitags - uniques will be stripped
	$dekitags = array();
	if (!empty($categories)) {
		foreach ($categories as $categoryid) {
			
			// ignore the "uncategorized" category
			if ($categoryid == 1) {
				continue;	
			}
			$category = get_category($categoryid);
			$dekitags[] = $category->cat_name;
		}	
	}
	if (!empty($tags)) {
		foreach ($tags as $Tag) {
			
			// if we see a dekipath: tag, then we're overwriting the target path
			if (strncmp($Tag->name, DEKIWPTRIGGER, strlen(DEKIWPTRIGGER)) == 0) {
				$dekiPath = substr($Tag->name, strlen(DEKIWPTRIGGER));
				continue;	
			}
			$dekitags[] = $Tag->name;	
		}	
	}
	
	// custom tags we add to each deki page
	$dekitags[] = 'wp-id:'.$postId; // this is crucial: this is how deki associates wordpress pages with deki pages
	$dekitags[] = 'wp-author:'.$user->user_login; // not necessary, but cool if you're federating multiple wps
	$dekitags[] = 'wp-from:'.get_option('siteurl'); // necessarily if you have multiple WPs to prevent wp-id collisions
	
	$dekiPageId = deki_getPost($postId);
	
	// need to escape slashes that are part of the page title, or they'll be interpreted as hierarchies
	$pagepath = $dekiPath . $hierarchyPath . str_replace('/', '//', $post->post_title);
	
	// format the page contents for saving
	$pagecontents  = apply_filters('the_content', $post->post_content);	
	$pagecontents .= '<p><small><em><a href="'.$post->guid.'" class="external">'.$post->guid.'</a></em></small></p>';
	
	if ($dekiPageId == 0) {
		// deki doesn't have this page yet, so let's create a new page
		$Result = $Plug->At('pages', '=' . urlencode(urlencode($pagepath)), 'contents')
			->With('abort', 'exists')
			->With('title', $post->post_title)
			->Post($pagecontents);
		
		if (!$Result->isSuccess()) {
			// todo: throw error for conflict
			return;	
		}
		
		$dekiPageId = $Result->getVal('body/edit/page/@id', 0);
	} else {
		// edit an existing entry
		$Result = $Plug->At('pages', $dekiPageId, 'contents')
			->With('edittime', gmdate( 'Ymdhis', mktime() )) 
			->With('title', $post->post_title)
			->Post($pagecontents);
		
		if (!$Result->isSuccess()) {
			//todo: error messages
			return;	
		}
		// move the page if the path has changed
		$oldpath = $Result->getVal('body/edit/page/path');
		if (strcmp(str_replace('_', ' ', $oldpath), $pagepath) !== 0) {
			$Request = $Plug->At('pages', $dekiPageId, 'move')->With('to', $pagepath)->Post();
		}
		
		// get existing tags so we don't blow any of them out of the water
		$Result = $Plug->At('pages', $dekiPageId, 'tags')->Get();
		$tags = $Result->getAll('body/tags/tag');
		foreach ($tags as $tag) {
			$dekitags[] = $tag['@value'];	
		}
	}
	
	// strip unique tags & save all tags to the deki page
	$dekitags = array_unique($dekitags);
	$tagarray = array();
	foreach ($dekitags as $tag) {
		$tagarray[] = array('@value' => $tag);
	}
	$Result = $Plug->At('pages', $dekiPageId, 'tags')->Put(array('tags' => array('tag' => $tagarray)));
	
	// Manage access for page
	$dekiUser = deki_getUser(DEKIUSERNAME);
	if ( $dekiUser ) {
		
		// get id of deki's user
		$dekiUserId = $dekiUser->getVal('@id');
		
		if ( $dekiUserId ) {
		
			// restrict access for page until post is not published
			$restrictType = ( $post->post_status == "publish" ) ? "Public" : "Private";
			
			$permissions = $dekiUser->getVal('permissions.user');
			$role = $permissions['role']['#text'];
			
			$grants = array();
			$grants[] = array('permissions' => array('role' => $role), 'user' => array('@id' => $dekiUserId));
			
			$xml = array(
				'security' => array(
					'permissions.page' => array('restriction' => $restrictType),
					'grants' => array('@restriction' => $restrictType, array('grant' => $grants))
				)
			);
			
			$Result = $Plug->At('pages', $dekiPageId, 'security') 	
				->Put($xml);		
		}
	}
}

/**
 * deki_getPost() - retrieve a post from deki given a wordpress ID
 * 
 * NOTE: this utilizes deki's site search; new pages take up to 60 seconds to be indexed, so edits/deletes may not 
 * pick up immediately with this connector; this should be resolved when we reimplement the tags data model not to 
 * piggyback off of search.
 *
 * @param int $postId wordpress post id
 * @return int deki page id
 */
function deki_getPost($postId) {
	$dekiPageId = 0;
	$Plug = deki_getPlug();
	
	// does this post already exist in deki ?
	$Result = $Plug->At('site', 'search')
		->With('q', 'tag:"wp-id:'.$postId.'" AND tag:"wp-from:'.get_option('siteurl').'"')
		->With('sortby', '-date')
		->Get();
	
	if ($Result->getStatus() == 200) {
		$searchpage = $Result->getAll('body/search/page', null);
		
		if (!is_null($searchpage)) {
			$page = current($searchpage);
			$DreamResult = new DreamResult($page);
			$dekiPageId = $DreamResult->getVal('@id');
		}
	}
	return $dekiPageId;
}

/**
 * deki_getUser() - retrieve a user from deki given a username
 * 
 * @param string $userName deki's username
 * @return DreamResult object with info about founded user
 */
function deki_getUser($userName) {
	$dekiUser = null;
	$Plug = deki_getPlug();
	
	$Result = $Plug->At('users')
		->With('usernamefilter', $userName)
		->Get();

	if ($Result->getStatus() == 200) {
		$searchuser = $Result->getAll('body/users/user', null);
		
		if (!is_null($searchuser)) {
			$user = current($searchuser);
			$dekiUser = new DreamResult($user);
		}
	}
	
	return $dekiUser;
}

/**
 * deki_deletePost() - delete a wordpress post
 *
 * @param int $postId wordpress post id
 * @return bool
 */
function deki_deletePost($postId) {
	$Plug = deki_getPlug();
	$dekiPageId = deki_getPost($postId);
	if ($dekiPageId == 0) {
		return false;
	}
	$Result = $Plug->At('pages', $dekiPageId)->Delete();
	return $Result->getStatus() == 200;
}

// add wordpress hooks
add_action("publish_post", "deki_savePost");
add_action("delete_post", "deki_deletePost");
