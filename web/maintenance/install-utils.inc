<?php
function install_php_version_checks() 
{
	# We dare not turn output buffer _off_ since this will break completely
	# if PHP is globally configured to run through a gzip filter.
	@ob_implicit_flush( true );

	if( !function_exists( 'version_compare' ) ) {
		# version_compare was introduced in 4.1.0
		//can't localize this, language.php hasn't been loaded yet, since it requires PHP5 to work
		echo "Your PHP version is much too old; 4.0.x will _not_ work. ".REQUIRED_PHP_VERSION." or higher is required. ABORTING.\n";
		return false;
	}
	
	if( version_compare( phpversion(), REQUIRED_PHP_VERSION ) < 0 ) {
		//can't localize this, language.php hasn't been loaded yet, since it requires PHP5 to work
		echo "PHP ".REQUIRED_PHP_VERSION." or higher is required. ABORTING.\n";
		return false;
	}
	
	// Test for PHP bug which breaks PHP 5.0.x on 64-bit...
	// As of 1.8 this breaks lots of common operations instead
	// of just some rare ones like export.
	$borked = str_replace( 'a', 'b', array( -1 => -1 ) );
	if( !isset( $borked[-1] ) ) {
		//can't localize this, language.php hasn't been loaded yet, since it requires PHP5 to work
		echo "PHP 5.0.x is buggy on your 64-bit system; you must upgrade to PHP 5.1.x\n" .
			"or higher. ABORTING. (http://bugs.php.net/bug.php?id=34879 for details)\n";
		return false;
	}

	global $wgCommandLineMode;
	$wgCommandLineMode = true;
	umask( 000 );
	@set_time_limit( 0 );
	return true;
}

function install_apps_version_check() 
{
	$return = true;
	//verify mySQL version
	$client_ver = mysql_get_client_info();
	// remove mysqlnd from client info string, if found
	$client_ver = preg_replace('/^mysqlnd /', '', $client_ver);
	if ( !version_compare($client_ver, REQUIRED_MYSQL_VERSION, ">=")) 
	{
		echo_fail(wfMsg('Page.Install.check-mysqlc-fail', htmlspecialchars($client_ver), REQUIRED_MYSQL_VERSION));
		$return = false;
	}
	
	//verify apache version
	//todo: would version_compare() work better?
	if (function_exists('apache_get_version')) {
		$apache_ver = apache_get_version();
		if (preg_match('/Apache\/([0-9]+\.[0-9]+\.[0-9]+)/', apache_get_version(), $matches)) 
		{
			$apache_ver = $matches[1];	
			if (strcmp($apache_ver, REQUIRED_APACHE_VERSION) < 0) 
			{
				echo_fail(wfMsg('Page.Install.check-apache-fail', htmlspecialchars($apache_ver), REQUIRED_APACHE_VERSION));
				$return = false;
			}
		}
		else {
			echo_warn(wfMsg('Page.Install.check-apache-fail', '', REQUIRED_APACHE_VERSION));
			$return = true;
		}
	}
	return $return;
}

function install_verify_databases($ourdb) 
{
	$phpdatabases = array();
	foreach (array_keys($ourdb) as $db) {
		$compname = $ourdb[$db]['compile'];
		if (extension_loaded($compname) or dl($compname . '.' . PHP_SHLIB_SUFFIX)) {
			array_push($phpdatabases, $db);
			$ourdb[$db]['havedriver'] = 1;
		}
	}
	
	//PHP doesn't likes return empty($phpdatabases)
	if (empty($phpdatabases))
	{
		 return false;
	}
	return true;
}

function install_verify_php_functions($fatal_functions_if_disabled, $fatal_settings_if_enabled) 
{
	$return = true;
	if (!empty($fatal_functions_if_disabled)) 
	{
		foreach ($fatal_functions_if_disabled as $func_name => $message) 
		{
			if (!function_exists($func_name)) 
			{
				$return = false;
				echo_fail(wfMsg($message));
			}
		}
	}
	
	if (!empty($fatal_settings_if_enabled)) 
	{	
		foreach ($fatal_settings_if_enabled as $func_name => $func_url) 
		{
			if (ini_get($func_name)) 
			{
				$return = false;
				echo_fail(wfMsg('Page.Install.fatal-func-if-on', $func_url, $func_name));
			}
		}
	}
	return $return;
}

function install_verify_apache_modules($fatal_apache_modules_if_disabled) 
{
	$return = true;
	if (function_exists('apache_get_modules')) 
	{
		$apache_mods = apache_get_modules();
		foreach ($fatal_apache_modules_if_disabled as $module) 
		{
			if (!is_array($apache_mods) || array_search($module, $apache_mods) != true)
			{
				echo_fail(wfMsg('Page.Install.check-apachemodule-fail', $module));
				$return = false;
			}
		}
	}
	return $return;
}

function install_warn_php_settings($warn_settings)
{
	if (!empty($warn_settings)) 
	{
		foreach ($warn_settings as $func_name => $func_url) 
		{
			if (ini_get( $func_name )) 
			{
				echo_warn(wfMsg('Page.Install.check-phpsettings-warn', $func_url, $func_name));
			}
		}	
	}
}

//get exectuable paths
function install_get_paths() {
	//first, get paths
	if (array_key_exists('PATH', $_ENV)) {
	 	$path_dirs = explode(PATH_SEPARATOR, $_ENV['PATH']);
	} 
	elseif (array_key_exists('PATH', $_SERVER)) 
	{
		//  PHP on windows uses $_SERVER['PATH']
		$path_dirs = explode(PATH_SEPARATOR, $_SERVER['PATH']);
	} 
	else 
	{
		$path_dirs = array( "/usr/bin", "/opt/csw/bin", "/usr/local/bin", "/sw/bin", "/opt/local/bin" );
	}
	return $path_dirs;
}

function install_session_path() 
{
	# session.save_path doesn't *have* to be set, but if it is, and it's
	# not valid/writable/etc. then it can cause problems
	$sessionSavePath = mw_get_session_save_path();
	$ssp = htmlspecialchars( $sessionSavePath );
	# Warn the user if it's not set, but let them proceed
	if ( !$sessionSavePath ) 
	{
		echo_warn('A value for <tt>session.save_path</tt> has not been set in PHP.ini. If the default value causes problems with saving session data, set it to a valid path which is read/write/execute for the user your web server is running under.');
		return false;
	} 
	if ( is_dir( $sessionSavePath ) && is_writable( $sessionSavePath ) ) 
	{
		return true;
	} 
	else 
	{
		# Something not right? Warn the user, but let them proceed
		echo_warn('Your <tt>session.save_path</tt> value (<tt>{$ssp}</tt>) appears to be invalid or is not writable. PHP needs to be able to save data to	this location for correct session operation.');
	}
	return false;	
}

function install_raise_memory_limit() 
{
	//todo: is this really necessary?
	$memlimit = ini_get( "memory_limit" );
	$conf->raiseMemory = false;
	if( empty( $memlimit ) || $memlimit == -1 ) {
	} else {
		$n = intval( $memlimit );
		if( preg_match( '/^([0-9]+)[Mm]$/', trim( $memlimit ), $m ) ) {
			$n = intval( $m[1] * (1024*1024) );
		}
		if( $n < 20*1024*1024 ) {
			if( false === ini_set( "memory_limit", "20M" ) ) {
			} else {
				$conf->raiseMemory = true;
			}
		}
	}	
}

function copyfile( $sdir, $name, $ddir, $perms = 0664 ) 
{
	copyfileto( $sdir, $name, $ddir, $name, $perms );
}

function copyfileto( $sdir, $sname, $ddir, $dname, $perms = 0664 ) 
{
	global $wgInstallOwner, $wgInstallGroup;

	$d = "{$ddir}/{$dname}";
	if ( copy( "{$sdir}/{$sname}", $d ) ) {
		if ( isset( $wgInstallOwner ) ) { chown( $d, $wgInstallOwner ); }
		if ( isset( $wgInstallGroup ) ) { chgrp( $d, $wgInstallGroup ); }
		chmod( $d, $perms );
		# print "Copied \"{$sname}\" to \"{$d}\".\n";
	} else {
		print "Failed to copy file \"{$sname}\" to \"{$ddir}/{$dname}\".\n";
		exit();
	}
}

function copydirectory( $source, $dest ) 
{
	$handle = opendir( $source );
	while ( false !== ( $f = readdir( $handle ) ) ) {
		$fullname = "$source/$f";
		if ( $f{0} != '.' && is_file( $fullname ) ) {
			copyfile( $source, $f, $dest );
		}
	}
}

function readconsole( $prompt = '' ) 
{
	static $isatty = null;
	if ( is_null( $isatty ) ) {
		if ( !function_exists( 'posix_isatty' ) || posix_isatty( 0 /*STDIN*/ ) ) {
			$isatty = true;
		} else {
			$isatty = false;
		}
	}

	if ( $isatty && function_exists( 'readline' ) ) {
		return readline( $prompt );
	} else {
		if ( $isatty ) {
			print $prompt;
		}
		if ( feof( STDIN ) ) {
			return false;
		}
		$st = fgets(STDIN, 1024);
		if ($st === false) return false;
		$resp = trim( $st );
		return $resp;
	}
}

function replacevars( $ins ) 
{
	$varnames = array(
		"wgDBserver", "wgDBname", "wgDBintlname", "wgDBuser",
		"wgDBpassword", "wgDBsqluser", "wgDBsqlpassword",
		"wgDBadminuser", "wgDBadminpassword", "wgDBprefix"
	);
	foreach ( $varnames as $var ) {
		global $$var;
		$ins = str_replace( '{$' . $var . '}', $$var, $ins );
		$ins = str_replace( '/*$' . $var . '*/`', '`' . $$var, $ins );
		$ins = str_replace( '/*$' . $var . '*/', $$var, $ins );
	}
	return $ins;
}

// @TODO: document why this method exists!
function dbsource_( $fname, $database = false ) 
{
    $fp = fopen( $fname, "r" );
    if ( false === $fp ) {
        print "Could not open \"{$fname}\".\n";
		exit();
	}
	
	$cmd = "";
	$done = false;
	while ( ! feof( $fp ) ) {
		$line = trim( fgets( $fp, 1024 ) );
		$sl = strlen( $line ) - 1;
		if ( $sl < 0 ) { continue; }
		if ( "-" == $line{0} && "-" == $line{1} ) { continue; }
		if ( ";" == $line{$sl} ) {
			$done = true;
			$line = substr( $line, 0, $sl );
		}
		$cmd .= $line;
		if ( $done ) {
			$cmd = replacevars( $cmd );
			if( $database )
				$res = $database->query( $cmd );
			else
				$res = mysql_query( $cmd );
	
			if ( false === $res ) {
				$err = mysql_error();
				print "Query \"{$cmd}\" failed with error code \"$err\".\n";
				exit();
			}
			$cmd = "";
			$done = false;
		}
	}
	fclose( $fp );
}

#
# Read and execute SQL commands from a file
#
function dbsource( $fname, $database = false ) 
{
        $fp = fopen( $fname, "r" );
        if ( false === $fp ) {
                print "Could not open \"{$fname}\".\n";
		exit();
	}
	$cmd = "";
	$done = false;
	while ( ! feof( $fp ) ) {
		$line = trim( fgets( $fp, 1024 ) );
		$sl = strlen( $line ) - 1;
		if ( $sl < 0 ) { continue; }
		if ( "-" == $line{0} && "-" == $line{1} ) { continue; }
		if ( ";" == $line{$sl} ) {
			$done = true;
			$line = substr( $line, 0, $sl );
		}
		if ( "" != $cmd ) { $cmd .= " "; }
		$cmd .= $line;
		if ( $done ) {
			$cmd = replacevars( $cmd );
			if( $database )
				$res = $database->query( $cmd );
			else
				$res = mysql_query( $cmd );
	
			if ( false === $res ) {
				$err = mysql_error();
				print "Query \"{$cmd}\" failed with error code \"$err\".\n";
				exit();
			}
			$cmd = "";
			$done = false;
		}
	}
	fclose( $fp );
}

/* MT: PeteE: dbsource() -like function that works with MySQL stored procedure syntax
*/
function dbsourcesproc( $fname, $database = false ) 
{
        $fp = fopen( $fname, "r" );
	if ( false === $fp ) {
		print "Could not open \"{$fname}\".\n";
		exit();
	}
	$contents = fread($fp, filesize($fname));
	fclose($fp);
	$lines = explode("\n", $contents);
	$cmd = "";
	$done = false;
	$delimiter = ';';
	foreach( $lines as $line) {
		if(!$line) continue;
		$sl = strlen($line);
		if($sl < 0) { continue; }
		if(preg_match('/^\s+(--).*$/', $line)) { continue; } // ingore comment lines
		if ( "-" == $line{0} && "-" == $line{1} ) { continue; }
		if (preg_match('/^DELIMITER (.*)$/', $line, $matches)) {
			$delimiter = $matches[1];
			//print("delimiter = " . $matches[1]);
			$line = '';
		} else  {
			if($delimiter == substr($line, $sl - strlen($delimiter),  $sl)) {
			$line = substr($line, 0, $sl - strlen($delimiter));
			$done = true;
		} else
			$line = substr($line, 0, $sl);
		}

		if ( "" != $cmd ) { $cmd .= " "; }
		$cmd .= $line;
		if ( $done ) {
			$cmd = replacevars( $cmd );
			if( $database ) {
				$res = $database->query( $cmd );
			}
			else {
				$res = mysql_query( $cmd );
			}
			if ( false === $res ) {
				$err = mysql_error();
				print "Query \"{$cmd}\" failed with error code \"$err\".\n";
				exit();
			}
			$cmd = "";
			$done = false;
		}
	}
}
// @TODO: auto-rename for vm installs?
function install_database($conf)
{
	// inject defaults into the current scope
	global $IP;
	include($IP . '/includes/DefaultSettings.php');
	
	$dbclass = 'Database'.ucfirst($conf->DBtype);
	$dbc = new $dbclass;

	$ok = true; // Let's be optimistic
	
	# RoyK: Always use the superuser account
	$conf->Root = true;
	$db_user = $conf->RootUser;
	$db_pass = $conf->RootPW;
	$db_server = $conf->DBserver;

	global $wgDBname, $wgDBuser, $wgDBpassword;
	$wgDBuser = $conf->DBuser;
	$wgDBpassword = $conf->DBpassword;
	$wgDBname = $conf->DBname;
	
	# Attempt to connect
	$wgDatabase = Database::newFromParams($db_server, $db_user, $db_pass, '', 1);
	
	# Check the connection and respond to errors
	if ($wgDatabase->isOpen()) 
	{
		# Seems OK
		$ok = true;
		$wgDBadminuser = $db_user;
		$wgDBadminpassword = $db_pass;
		$myver = $wgDatabase->getServerVersion();
		echo_success(wfMsg('Page.Install.db-connected'));
	} 
	else 
	{
		// There were errors, report them and back out
		$ok = false;
		$errno = mysql_errno();
		$errtx = htmlspecialchars( mysql_error() );
		switch( $errno ) {
			case 1045:
			case 2000:
				echo_fail(wfMsg('Page.Install.db-error-auth'));
				break;
			case 2002:
			case 2003:
			default:
				# General connection problem
				echo_fail(wfMsg('Page.Install.db-error-num', $errno, $errtx));
				break;
		} # switch
		return false; //exit the database install
	} #conn. att.

	if (!$wgDatabase->isOpen())
	{
		$errs["DBserver"] = wfMsg('Page.Install.db-error-connect');
		continue;
	}

	# For error messages
	$wgDBnameDisplay = htmlspecialchars($wgDBname);
	$wgDBuserDisplay = htmlspecialchars($wgDBuser);
	$wgDBpasswordDisplay = htmlspecialchars($wgDBpassword);
	
	# For mySQL queries
	$wgDBname = mysql_real_escape_string($wgDBname);
	$wgDBuser = mysql_real_escape_string($wgDBuser);
	$wgDBpassword = mysql_real_escape_string($wgDBpassword);
	
	@$sel = $wgDatabase->selectDB( $wgDBname );
	if ($sel)
	{
		global $wgIsVM;
		
		// database already exists!
		$renamed = false;
		if ($wgIsVM)
		{
			// attempt to auto-rename
			for ($i = 1; $i < 10; $i++)
			{
				$name = $wgDBname . $i;
				@$sel = $wgDatabase->selectDB(mysql_real_escape_string($name));
				if ($sel)
				{
					continue;
				}
				
				// found an available database name
				$conf->DBname = $name;
				$wgDBname = mysql_real_escape_string($name);
				$wgDBnameDisplay = htmlspecialchars($wgDBname);
				$renamed = true;
				break;
			}
		}

		if (!$renamed)
		{
			echo_fail(wfMsg('Page.Install.db-error-exists', $wgDBnameDisplay));
			return false;
		}
	}

	$err = mysql_errno();
	if( $err == 1102 /* Invalid database name */ ) 
	{
		echo_fail(wfMsg('Page.Install.db-error-invalid', $wgDBnameDisplay));
		return false;
	} 
	elseif( $err != 1049 /* Database doesn't exist */ ) 
	{
		echo_fail(wfMsg('Page.Install.db-error-selection', $wgDBnameDisplay, $err, htmlspecialchars( mysql_error() )));
		return false;
	}
	$res = $wgDatabase->query( "CREATE DATABASE `$wgDBname` DEFAULT CHARACTER SET utf8 DEFAULT COLLATE utf8_general_ci" );
	if( !$res ) 
	{
		echo_fail(wfMsg('Page.Install.db-error-create', $wgDBnameDisplay));
		return false;
	}
	echo_success(wfMsg('Page.Install.db-success', $wgDBnameDisplay));
	
	// got the database
	$wgDatabase->selectDB( $wgDBname );
	
	if( $wgDatabase->tableExists( "pages" ) ) 
	{
		echo_fail(wfMsg('Page.Install.db-error-data'));
		return false;
	} 
	else if ($wgDatabase->tableExists('user') && $wgDatabase->tableExists('user_rights') && $wgDatabase->tableExists('cur')) 
	{
		// Deki Wiki Gooseberry schema, attempt to upgrade
		echo_list(wfMsg('Page.Install.db-gb-upgrade'));
		$wgDatabase->query( "ALTER DATABASE `".$wgDBname."` DEFAULT CHARACTER SET utf8 DEFAULT COLLATE utf8_general_ci;" );

		# Set up the "regular user" account *if we can, and if we need to*

		# Yes, so run the grants
		echo( wfMsg('Page.Install.db-user-grant', $wgDBuserDisplay, $wgDBnameDisplay) );

		// grant the required permissions
		dbsource("../maintenance/users.sql");
		echo(wfMsg('Page.Install.db-done').'</li>');

		// switch to the wikiuser account
		$wgDatabase = Database::newFromParams( $conf->DBserver, $conf->DBuser, $conf->DBpassword, '', 1 );
		$wgDatabase->selectDB( $conf->DBname );
		require_once( "../maintenance/updaters-mindtouch-df2hayes.inc" );
		do_all_updates();
		global $wgAttachPath;
		$path = addslashes($wgAttachPath);
		$wgDatabase->query("UPDATE config SET config_value='$path' WHERE config_key='storage/fs/path'");
		echo_success(wfMsg('Page.Install.db-path', $path));
		
		$wgDatabase->query("INSERT INTO config (config_key, config_value) VALUES ('security/api-key', '". $conf->ApiKey . "')");
		echo_success(wfMsg('Page.Install.db-apikey'));

	} 
	else 
	{
		# Yes, so run the grants
		echo("<li>". wfMsg('Page.Install.db-user-grant', $wgDBuserDisplay, $wgDBnameDisplay) );

		// grant the required permissions
		dbsource("../maintenance/users.sql");
		echo(wfMsg('Page.Install.db-done').'</li>');
		
		// switch to the wikiuser account
		$wgDatabase = Database::newFromParams( $conf->DBserver, $conf->DBuser, $conf->DBpassword, '', 1 );
		$wgDatabase->selectDB( $conf->DBname );

		# FIXME: Check for errors
		echo("<li>".wfMsg('Page.Install.db-creating'));
		dbsource( "../maintenance/tables.sql", $wgDatabase );
		
		global $wgIsVM, $wgIsMSI;
		if ($wgIsMSI)
		{
			// select sql files for MSI installations
			if (!$conf->isCore())
			{
				// notice the underscore... is it a royk method?
				dbsource_("../maintenance/data/commercial.msi.sql", $wgDatabase);
			}
			else
			{
				dbsource("../maintenance/data/msi.sql", $wgDatabase);
			}
		}
		else if ($wgIsVM)
		{
			// select sql files for VM installations
			if (!$conf->isCore())
			{
				dbsource_("../maintenance/data/commercial.sql", $wgDatabase);
			}
			else
			{
				dbsource("../maintenance/data/vm.sql", $wgDatabase);
			}
		}
		else
		{
			// select sql files for source/package installations
			if (!$conf->isCore())
			{
				dbsource("../maintenance/data/trial.sql", $wgDatabase);
			}
			else
			{
				dbsource("../maintenance/data/source.sql", $wgDatabase);
			}
		}
		
		echo(wfMsg('Page.Install.db-done').'</li>');


		if( $conf->SysopName ) {
			$updateSysopQuery = "UPDATE users SET user_name='" . mysql_real_escape_string($conf->getSysopName()) . "',user_email='" . mysql_real_escape_string($conf->getSysopEmail()) . "',user_password=md5(concat('1-', md5('" . mysql_real_escape_string($conf->getSysopPass()) . "'))) WHERE user_id=1";
			$wgDatabase->query($updateSysopQuery);

			echo_success(wfMsg('Page.Install.db-user-create', $conf->SysopName));
		}
		
		// bugfix 5706: First edit of Homepage does not record change properly 
		$homePageId = $wgDatabase->selectField('pages', 'page_id', array('page_title' => '', 'page_namespace' => 0));
		$wgDatabase->query("INSERT INTO recentchanges (rc_timestamp, rc_user, rc_namespace, rc_title, rc_comment, rc_cur_id) VALUES ('" . wfTimestamp(TS_MW) .  "', 1, 0, '', 'page created', " . $homePageId . ")"); 

		// bugfix 4523: Installer needs to set security/admin-user-for-impersonation 
		$wgDatabase->query("INSERT INTO config (config_key, config_value) VALUES ('security/admin-user-for-impersonation', '". mysql_real_escape_string($conf->getSysopName()) . "')");

		// bugfix 7876: Installer needs to set admin/email for page notifications
		$wgDatabase->query("INSERT INTO config (config_key, config_value) VALUES ('admin/email', '". mysql_real_escape_string($conf->EmergencyContact) . "')");

		$wgDatabase->query("INSERT INTO config (config_key, config_value) VALUES ('ui/analytics-key', '". mysql_real_escape_string($wgDefaultAnalytics) . "')");

		$wgDatabase->query("INSERT INTO config (config_key, config_value) VALUES ('ui/skin', '". mysql_real_escape_string($wgActiveSkin) . "'), ('ui/template', '". mysql_real_escape_string($wgActiveTemplate) . "')");

		//insert the mindtouch API key into the config table
		$wgDatabase->query("INSERT INTO config (config_key, config_value) VALUES ('security/api-key', '". mysql_real_escape_string($conf->ApiKey) . "')");
		echo_success(wfMsg('Page.Install.db-apikey'));
		
		//set the site language
		$wgDatabase->query("UPDATE config SET config_value = '".mysql_real_escape_string($conf->SiteLang)."' WHERE config_key = 'ui/language'");
		echo_success(wfMsg('Page.Install.db-lang', $conf->SiteLang));
		
		global $wgAttachPath;
		$path = addslashes($wgAttachPath);
		$wgDatabase->query("INSERT INTO config (config_key, config_value) VALUES ('storage/fs/path', '".$path."')");
		echo_success(wfMsg('Page.Install.db-path', $path));
		
		$wgDatabase->query("UPDATE config SET config_value='". mysql_real_escape_string($conf->Sitename) ."' WHERE config_key='ui/sitename'");
		echo_success(wfMsg('Page.Install.db-sitename', $conf->Sitename));


		return true;
	}
	return false;
}

function install_localsettings_generate( $conf ) {
	$conf->PasswordSender = $conf->EmergencyContact;

	// Add slashes to strings for double quoting
	$slconf = array_map( "escapePhpString", get_object_vars( $conf ) );

	$sep = (DIRECTORY_SEPARATOR == "\\") ? ";" : ":";
	$dsep = DIRECTORY_SEPARATOR;
	$include_path = ".".$sep.'$IP'.$sep.'$IP'.$dsep.'includes'.$sep.'$IP'.$dsep.'languages';

	$localsettings = <<<LOCALSETTINGS
// This file was automatically generated by the MindTouch installer.
// If you make manual changes, please keep track in case you need to
// recreate them later.
//
// See includes/DefaultSettings.php for all configurable settings
// and their default values, but don't forget to make changes in _this_
// file, not there.

\$IP = dirname(__FILE__);
ini_set("include_path", "$include_path");
require_once("includes/DefaultSettings.php");

\$wgEmergencyContact = "{$slconf['EmergencyContact']}";
\$wgPasswordSender = "{$slconf['PasswordSender']}";

\$wgDekiApiKey = "{$slconf['ApiKey']}";

\$wgDBserver = "{$slconf['DBserver']}";
\$wgDBname = "{$slconf['DBname']}";
\$wgDBadminuser = "{$slconf['DBuser']}";
\$wgDBadminpassword = "{$slconf['DBpassword']}";

LOCALSETTINGS;

	global $wgIsMSI;
	if ($wgIsMSI) 
	{
		$port = "";
		if ($_SERVER['SERVER_PORT'] != 80)
		{
			$port = ":" . $_SERVER['SERVER_PORT'];
		}
		$localsettings.= "
\$wgDreamServer = \"http://localhost" . $port . "/@api\";
\$wgDekiApi = \"/deki\";	
";	
	}

	// Keep things in Unix line endings internally;
	// the system will write out as local text type.
	return str_replace( "\r\n", "\n", $localsettings );
}

function install_mindtouch_xml_generate($conf)
{
	global $IP; 
	
	// read in mindtouch.deki.startup.xml.in
	$f = fopen(FILE_STARTUP_XML_IN, "r");
	$contents = fread($f, filesize(FILE_STARTUP_XML_IN));
	fclose($f);
	
	// substitute the variables
	$host = explode(':', $conf->DBserver);
	if(count($host) > 1) {
		$db_server = $host[0];
		$db_port = $host[1];
	}
	else {
		$db_server = $host[0];
		$db_port = 3306;
	}
	$variables = array(
		'%DEKI-PATH%' => $IP, 
		'%IMAGEMAGICK-CONVERT-PATH%' => $conf->ImageMagickConvert, 
		'%IMAGEMAGICK-IDENTIFY-PATH%' => $conf->ImageMagickIdentify, 
		'%PRINCE-PATH%' => $conf->prince,
		'%STORAGE-PROVIDER-PATH%' => $IP . '/attachments', 
		'%DB-SERVER%' => $db_server,
		'%DB-PORT%' => $db_port,
		'%DB-CATALOG%' => $conf->DBname, 
		'%DB-USER%' => $conf->DBuser, 
		'%DB-PASSWORD%' => htmlspecialchars($conf->DBpassword), 
		'%APIKEY%' => $conf->ApiKey, 
		'%LUCENE-STORE%' => $conf->LuceneStore
	);
	$find = array();
	$replace = array();
	foreach ($variables as $f => $r) 
	{
		$find[] = $f;
		$replace[] = $r;	
	}
	$contents = str_replace($find, $replace, $contents); //faster than individual preg_replace
	return $contents;
}

function install_mindtouch_conf_generate($conf)
{
	global $IP; 
	
	if (!wfIsWindows()) 
	{
		$inputFile = FILE_HOST_XML_IN;
	}
	else 
	{
		$inputFile = FILE_HOST_WIN_BAT_IN;
	}
	// read in mindtouch.deki.startup.xml.in
	$f = fopen($inputFile, "r");
	$contents = fread($f, filesize($inputFile));
	fclose($f);
	
	// substitute the variables
	$variables = array(
		'%IP%' => $conf->IP,
		'%BIN_DIR%' => $conf->IP . '/bin', 
		'%APIKEY%' => $conf->ApiKey, 
		'%MONO%' => $conf->Mono, 
		'%GUID%' => $conf->Guid, 
	);
	
	$find = array();
	$replace = array();
	foreach ($variables as $f => $r) 
	{
		$find[] = $f;
		$replace[] = $r;	
	}
	$contents = str_replace($find, $replace, $contents); //faster than individual preg_replace
	return $contents;
}

/***
 * Write a settings file
 * Given a filename, will attempt to write the settings file - if the settings file already exists, it will be moved before the new copy is written
 */
function install_write_settings_file( $file, $contents ) 
{
	if (file_exists($file)) {
		$fileparts = explode('.', $file);
		$ext = array_pop($fileparts);
		$filebase = implode('.', $fileparts);
		// rename the existing file so it's not lost
		date_default_timezone_set('UTC');
		rename($file, $filebase . date("mdyhis") . '.'.$ext);
	}
	$f = fopen( $file, 'xt' );

	if( $f == false ) {
		echo_fail( wfMsg('Page.Install.error-reading-file', $file, $contents) );
		return false;
	}
	if (!fwrite( $f, $contents ) ) {
		echo_fail( wfMsg('Page.Install.error-writing-file', $file, $contents) );
		return false;
	}
	fclose( $f );
	chmod($file, 0640);
	echo_success('Wrote '.$file.' to disk successfully.');
	return true;
}

/**
 * Get the value of session.save_path
 *
 * Per http://uk.php.net/manual/en/ref.session.php#ini.session.save-path,
 * this might have some additional preceding parts which need to be
 * ditched
 *
 * @return string
 */
function mw_get_session_save_path() {
	$path = ini_get( 'session.save_path' );
	$path = substr( $path, strrpos( $path, ';' ) );
	return $path;
}
?>
