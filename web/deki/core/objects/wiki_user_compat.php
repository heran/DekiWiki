<?php

/**
 * This is a temporary class until all of the interfaces have been fleshed out
 * withint the DekiUser. Covers method calls that have not been handled by
 * the new DekiUser class yet.
 *
 * @deprecated
 */
abstract class User
{
	/**
	 * @deprecated
	 */
	public static function newFromName($name)
	{
		return DekiUser::newFromText($name);
	}
	
	/**
	 * @deprecated
	 */
	function getUserPage()
	{
		return $this->getUserTitle();
	}

	/**
	 * Load a skin if it doesn't exist or return it
	 */
	public function &getSkin() {
		global $IP, $wgStyleDirectory, $wgActiveTemplate;
		if (empty($this->mSkin)) 
		{
			// bug #3987: Change MEDIAWIKI entry point variable to DEKIWIKI or MINDTOUCH ; maintain backwards compat for old skins
			if (!defined('MEDIAWIKI')) 
			{
				define('MEDIAWIKI', true);
			}
			
			$fp = Skin::getTemplateFile($wgActiveTemplate);
			
			//bug 4027; if a template doesn't exist, use the safe defaults
			//what should we do for skins? hitting the disk on every request is expensive ... and i'm pretty sure that it'll be obvious if the
			//wrong skin is selected
			if (!file_exists($fp)) 
			{
				global $wgDefaultTemplate;
				$wgActiveTemplate = $wgDefaultTemplate;
				$fp = Skin::getTemplateFile($wgActiveTemplate);
			}
			require_once( $fp );
			$className = 'Skin'.Skin::getTemplateNameFromPath($fp);			
			$this->mSkin = new $className;
		}

		return $this->mSkin;
	}

	public function isAdmin() {
		return $this->can('ADMIN');
	}

	/***
	 * can*() functions determines whether a user can do a certain action
	 */	
	public function canLogin() { return $this->can('login'); }
	public function canBrowse() { return $this->can('browse'); }
	public function canView() { return $this->can('read'); }
	public function canEdit() { return $this->can('update'); }
	public function canSubscribe() { return $this->can('subscribe'); }
	public function canCreate() { return $this->can('create'); }
	public function canAttach() { return $this->can('update'); }
	public function canMove() { return $this->can('move'); }
	public function canDelete() { return $this->can('delete'); }
	public function canRestrict() { return $this->can('changepermissions'); }
	public function canViewControlPanel() { return $this->can('admin'); }
	public function canAdmin() { return $this->can('admin'); }

	/**
	 * Initialise php session
	 */
	public static function setupSession() {
		global $wgSessionsInMemcached, $wgMemCachedServers;
		global $wgCookiePath, $wgCookieDomain, $wgCookieSecure, $wgCookieHttpOnly;
		
        // Get the session Id passed from SWFUpload
        // We have to do this to work-around the Flash Player Cookie Bug
        if (isset($_POST[session_id()])) {
            session_id($_POST[session_id()]);
        }
		
		if( $wgSessionsInMemcached && !empty($wgMemCachedServers)) {
			$session_save_path = implode(",", $wgMemCachedServers);
			ini_set('session.save_handler', 'memcache');
			ini_set('session.save_path', $session_save_path);
		} elseif( 'files' != ini_get( 'session.save_handler' ) ) {
			# If it's left on 'user' or another setting from another
			# application, it will end up failing. Try to recover.
			ini_set ( 'session.save_handler', 'files' );
		}
		
		session_set_cookie_params(0, $wgCookiePath, $wgCookieDomain, $wgCookieSecure, $wgCookieHttpOnly);
		session_cache_limiter('private, must-revalidate');
		
		@session_start();
	}

	/*
	 * @deprecated
	 */
	public function getOption($key, $default = null)
	{
		return $this->Properties->getOption($key, $default);
	}
	
	/**
	 * Initialize (if necessary) and return a session token value
	 * which can be used in edit forms to show that the user's
	 * login credentials aren't being hijacked with a foreign form
	 * submission.
	 *
	 * @param mixed $salt - Optional function-specific data for hash.
	 *                      Use a string or an array of strings.
	 * @return string
	 * @access public
	 */
	public function editToken( $salt = '' )
	{
		if( !isset( $_SESSION['wsEditToken'] ) ) {
			$token = dechex( mt_rand() ) . dechex( mt_rand() );
			$_SESSION['wsEditToken'] = $token;
		} else {
			$token = $_SESSION['wsEditToken'];
		}
		if( is_array( $salt ) ) {
			$salt = implode( '|', $salt );
		}
		return md5( $token . $salt );
	}

	public function isWatched($title)
	{
		$wl = WatchedItem::fromUserTitle($this, $title);
		return $wl->isWatched();
	}

	/**
	 * Watch an article
	 */
	function addWatch( $title ) {
		$wl = WatchedItem::fromUserTitle( $this, $title );
		$wl->addWatch();
	}

	/**
	 * Stop watching an article
	 */
	function removeWatch( $title ) {
		$wl = WatchedItem::fromUserTitle( $this, $title );
		$wl->removeWatch();
	}
}
