<?php
/**
 * Update database schema
 *
 * @todo document
 * @package MediaWiki
 * @subpackage Maintenance
 */ 

/** */
error_reporting(E_ALL & ~E_NOTICE);
require_once( "commandLine.inc" );
require_once( "updaters-mindtouch.inc" );
$wgProfiling = false; 

function usage($checkOptions = false) {
	global $options;
	echo ("Usage: php update-db.php --db-server=myserver --db-user=myuser --db-password=mypassword --db-catalog=mywikidb\n");
	$missing = false;
	if($checkOptions) {
		$requiredOptions = array('db-server', 'db-user', 'db-password', 'db-catalog');
		foreach($requiredOptions as $required) {
			if(!isset($options[$required])) {
				$missing = true;
				echo("Missing: --".$required."\n");
			}
		}
	}
	if($missing || !$checkOptions)
	 	exit(1);
}

// check for a multi-tenant setup
global $wgWikis;
if(is_array($wgWikis)) {
    foreach($wgWikis as $wiki) {
        if($wiki['db-server'] != "") 
            $dbServer = $wiki['db-server'];
        else
            $dbServer = "localhost";

        if($wiki['db-port'] != "") 
            $dbServer .= ':' . $wiki['db-port'];
    
        if($wiki['db-user'] != "") 
            $dbUser = $wiki['db-user'];
        else 
            $dbUser = $wgDBadminuser;

        if($wiki['db-password'] != "") 
            $dbPassword = $wiki['db-password']; 
        else
            $dbPassword = $wgDBadminpassword;

        if($wiki['db-catalog'] != "")
            $dbCatalog = $wiki['db-catalog'];
        else
            $dbCatalog = "wikidb";
                
        // make sure wgDBadminuser is defined since the sprocs.sql files use it
        $wgDBadminuser = $dbUser;
        $wgDatabase = Database::newFromParams( $dbServer, $dbUser, $dbPassword, $dbCatalog );
        echo("\nUpdating " . $wgDatabase->mDBname . " on " . $wgDatabase->mServer . "\n");     
        do_all_updates();
    }
}
else {
    if( !isset($wgDBserver) || !isset($wgDBadminuser) || !isset($wgDBadminpassword) || !isset($wgDBname) ) {
	if(empty($options)) {
		echo("WARNING: database information is not specified in LocalSettings.php.  Please run update-db.php manually with command line arguments.\n");
		usage();
	}
	else  {
		if(isset($options['db-server']) || isset($options['db-user']) || isset($options['db-password']) || isset($options['db-catalog']) )
			usage(true);

		$wgDBserver = $options['db-server'];
		$wgDBadminuser = $options['db-user'];
		$wgDBadminpassword = $options['db-password'];
  		$wgDBname = $options['db-catalog'];
	}
    }
    $wgDatabase = Database::newFromParams( $wgDBserver, $wgDBadminuser, $wgDBadminpassword, $wgDBname);
    echo("\nUpdating " . $wgDatabase->mDBname . " on " . $wgDatabase->mServer . "\n");     
    do_all_updates();
}

print "Done.\n";

?>
