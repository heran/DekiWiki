<?php
/**
 * Update database schema
 *
 * @todo document
 * @package MediaWiki
 * @subpackage Maintenance
 */ 

$wgConfiguring = true;
require_once( "commandLine.inc" );

global $IP;
require_once("$IP/includes/Setup.php");

// if DB info is specified on the command line, use those settings
if(!is_null($options['dbServer']) && !is_null($options['dbUser']) && !is_null($options['dbPassword']) && !is_null($options['dbName']) && !is_null($options['hostname'])) {
	$wgDatabase = Database::newFromParams( $options['dbServer'], $options['dbUser'], $options['dbPassword'], $options['dbName']);

} else {
        global $wgDBserver, $wgDBadminuser, $wgDBadminpassword, $wgDBname;
	$wgDatabase = Database::newFromParams( $wgDBserver, $wgDBadminuser, $wgDBadminpassword, $wgDBname);
}

require_once( "updaters-mindtouch-df2hayes.inc" );
do_all_updates();

?>
