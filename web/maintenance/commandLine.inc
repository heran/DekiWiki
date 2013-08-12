<?php
/**
 * @todo document
 * @package MediaWiki
 * @subpackage Maintenance
 */

/** */
# Abort if called from a web server
if ( isset( $_SERVER ) && array_key_exists( 'REQUEST_METHOD', $_SERVER ) ) {
	print "This script must be run from the command line\n";
	exit();
}

define("MINDTOUCH_DEKI",true);

# Process command line arguments
# $options becomes an array with keys set to the option names
# $optionsWithArgs is an array of GNU-style options that take an argument. The arguments are returned
# in the values of $options.
# $args becomes a zero-based array containing the non-option arguments

if ( !isset( $optionsWithArgs ) ) {
	$optionsWithArgs = array();
}

$IP = realpath( dirname( __SELF__ ) . "/.." );
//$IP = getcwd(); // use getcwd so the right path is returned if we're using symlinks
// < DekiFarm
chdir( $IP );

$options = array();
$args = array();

for( $arg = reset( $argv ); $arg !== false; $arg = next( $argv ) ) {
	if ( substr( $arg, 0, 2 ) == '--' ) {
		# Long options
		$option = substr( $arg, 2 );
		if ( in_array( $option, $optionsWithArgs ) ) {
			$param = next( $argv );
			if ( $param === false ) {
				die( "$arg needs an value after it\n" );
			}
			$options[$option] = $param;
		} else {
			$bits = explode( '=', $option, 2 );
			if( count( $bits ) > 1 ) {
				$option = $bits[0];
				$param = $bits[1];
			} else {
				$param = 1;
			}
			$options[$option] = $param;
		}
	} elseif ( $arg{0} == '-' ) {
		# Short options
		for ( $p=1; $p<strlen( $arg ); $p++ ) {
			$option = $arg{$p};
			if ( in_array( $option, $optionsWithArgs ) ) {
				$param = next( $argv );
				if ( $param === false ) {
					die( "$arg needs an value after it\n" );
				}
				$options[$option] = $param;
			} else {
				$options[$option] = 1;
			}
		}
	} else {
		$args[] = $arg;
	}
}

if (!isset($wgJustArgs)) {

# General initialisation

$wgCommandLineMode = true;
# Turn off output buffering if it's on
@ob_end_flush();
$sep = strchr( $include_path = ini_get( "include_path" ), ";" ) ? ";" : ":";

if (!isset( $wgUseNormalUser ) ) {
	$wgUseNormalUser = false;
}

$settingsFile = "$IP/LocalSettings.php";

if ( ! is_readable( $settingsFile ) ) {
    print "A copy of your installation's LocalSettings.php\n" .
    "must exist in the source directory.\n";
    exit();
}
$wgCommandLineMode = true;
$DP = $IP;
require_once( "$IP/includes/Defines.php" );
require_once( $settingsFile );
ini_set( "include_path", ".$sep$IP$sep$IP/includes$sep$IP/languages$sep$IP/maintenance" );

if ( is_readable( "$IP/AdminSettings.php" ) ) {
    require_once( "$IP/AdminSettings.php" );
}

# Turn off output buffering again, it might have been turned on in the settings files
@ob_end_flush();
# Same with these
$wgCommandLineMode = true;

ini_set( 'memory_limit', -1 );

//require_once( "Setup.php" );
require_once( "GlobalFunctions.php" );
require_once( "Database.php" );
require_once( "maintenance/install-utils.inc" );
set_time_limit(0);

}

?>
