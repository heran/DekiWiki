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
 * Global functions used everywhere
 * @package MediaWiki
 */

/**
 * Some globals and requires needed
 */
 
/**
 * Total number of articles
 * @global integer $wgNumberOfArticles
 */
$wgNumberOfArticles = -1; # Unset
/**
 * Total number of views
 * @global integer $wgTotalViews
 */
$wgTotalViews = -1;
/**
 * Total number of edits
 * @global integer $wgTotalEdits
 */
$wgTotalEdits = -1;

require_once( 'CompatibilityFunctions.php' );
require_once( 'DatabaseFunctions.php' );
require_once( 'normal/UtfNormalUtil.php' );

/**
 * html_entity_decode exists in PHP 4.3.0+ but is FATALLY BROKEN even then,
 * with no UTF-8 support.
 *
 * @param string $string String having html entities
 * @param $quote_style
 * @param string $charset Encoding set to use (default 'ISO-8859-1')
 */
function do_html_entity_decode( $string, $quote_style=ENT_COMPAT, $charset='ISO-8859-1' ) {
    $fname = 'do_html_entity_decode';
    
    
    static $trans;
    static $savedCharset;
    static $regexp;
    if( !isset( $trans ) || $savedCharset != $charset ) {
        $trans = array_flip( get_html_translation_table( HTML_ENTITIES, $quote_style ) );
        $savedCharset = $charset;
        
        # Note - mixing latin1 named entities and unicode numbered
        # ones will result in a bad link.
        if( strcasecmp( 'utf-8', $charset ) == 0 ) {
            $trans = array_map( 'utf8_encode', $trans );
        }
        
        /**
         * Most links will _not_ contain these fun guys,
         * and on long pages with many links we can get
         * called a lot.
         *
         * A regular expression search is faster than
         * a strtr or str_replace with a hundred-ish
         * entries, though it may be slower to actually
         * replace things.
         *
         * They all look like '&xxxx;'...
         */
        foreach( $trans as $key => $val ) {
            $snip[] = substr( $key, 1, -1 );
        }
        $regexp = '/(&(?:' . implode( '|', $snip ) . ');)/e';
    }

    $out = preg_replace( $regexp, '$trans["$1"]', $string );
    
    return $out;
}

//generate an alphabetical string
function wfRandomStr($length = 8) {
	mt_srand((double)microtime()*1000000);
	$value = ''; $i = 0;
	while ($i<$length) {
		$value .= chr(mt_rand(97,122));
		$i++;
	}
	return $value;
}
/**
 * We want / and : to be included as literal characters in our title URLs.
 * %2F in the page titles seems to fatally break for some reason.
 *
 * @param string $s
 * @return string
*/
function wfUrlencode ( $s ) {
    $s = urlencode( $s );
    $s = preg_replace( '/%3[Aa]/', ':', $s );
    $s = preg_replace( '/%2[Ff]/', '/', $s );

    return $s;
}

/**
 * Return the UTF-8 sequence for a given Unicode code point.
 * Currently doesn't work for values outside the Basic Multilingual Plane.
 *
 * @param string $codepoint UTF-8 code point.
 * @return string HTML UTF-8 Entitie such as '&#1234;'.
 */
function wfUtf8Sequence( $codepoint ) {
    if($codepoint <        0x80) return chr($codepoint);
    if($codepoint <    0x800) return chr($codepoint >>    6 & 0x3f | 0xc0) .
                                     chr($codepoint          & 0x3f | 0x80);
    if($codepoint <  0x10000) return chr($codepoint >> 12 & 0x0f | 0xe0) .
                                     chr($codepoint >>    6 & 0x3f | 0x80) .
                                     chr($codepoint          & 0x3f | 0x80);
    if($codepoint < 0x110000) return chr($codepoint >> 18 & 0x07 | 0xf0) .
                                     chr($codepoint >> 12 & 0x3f | 0x80) .
                                     chr($codepoint >>    6 & 0x3f | 0x80) .
                                     chr($codepoint          & 0x3f | 0x80);

    # There should be no assigned code points outside this range, but...
    return "&#$codepoint;";
}

/**
 * Converts numeric character entities to UTF-8
 *
 * @param string $string String to convert.
 * @return string Converted string.
 */
function wfMungeToUtf8( $string ) {
    global $wgInputEncoding; # This is debatable
    #$string = iconv($wgInputEncoding, "UTF-8", $string);
    $string = preg_replace ( '/&#0*([0-9]+);/e', 'wfUtf8Sequence($1)', $string );
    $string = preg_replace ( '/&#x([0-9a-f]+);/ie', 'wfUtf8Sequence(0x$1)', $string );
    # Should also do named entities here
    return $string;
}

function wfHtmlStringLength($string) {
    $string = do_html_entity_decode( $string );
    $string = preg_replace ( '/&#0*([0-9]+);/e', '_', $string );
    $string = preg_replace ( '/&#x([0-9a-f]+);/ie', '_', $string );
    return strlen($string);
}

/**
 * Converts a single UTF-8 character into the corresponding HTML character
 * entity (for use with preg_replace_callback)
 *
 * @param array $matches
 *
 */
function wfUtf8Entity( $matches ) {
    $codepoint = utf8ToCodepoint( $matches[0] );
    return "&#$codepoint;";
}

/**
 * Converts all multi-byte characters in a UTF-8 string into the appropriate
 * character entity
 */
function wfUtf8ToHTML($string) {
    return preg_replace_callback( '/[\\xc0-\\xfd][\\x80-\\xbf]*/', 'wfUtf8Entity', $string );
}

/**
 * Sends a line to the debug log if enabled or, optionally, to a comment in output.
 * In normal operation this is a NOP.
 *
 * Controlling globals:
 * $wgDebugLogFile - points to the log file
 * $wgProfileOnly - if set, normal debug messages will not be recorded.
 * $wgDebugRawPage - if false, 'action=raw' hits will not result in debug output.
 * $wgDebugComments - if on, some debug items may appear in comments in the HTML output.
 *
 * @param string $text
 * @param bool $logonly Set true to avoid appearing in HTML when $wgDebugComments is set
 */
function wfDebug( $text, $logonly = false ) {
    global $wgOut, $wgDebugLogFile, $wgDebugComments, $wgProfileOnly, $wgDebugRawPage;

    # Check for raw action using $_GET not $wgRequest, since the latter might not be initialised yet
    if ( isset( $_GET['action'] ) && $_GET['action'] == 'raw' && !$wgDebugRawPage ) {
        return;
    }

    if ( isset( $wgOut ) && $wgDebugComments && !$logonly ) {
        $wgOut->debug( $text );
    }
    if ( '' != $wgDebugLogFile && !$wgProfileOnly ) {
        # Strip unprintables; they can switch terminal modes when binary data
        # gets dumped, which is pretty annoying.
        $text = preg_replace( '![\x00-\x08\x0b\x0c\x0e-\x1f]!', ' ', $text );
        @error_log( $text, 3, $wgDebugLogFile );
    }
}

/**
 * Log for database errors
 * @param string $text Database error message.
 */
function wfLogDBError( $text ) {
    global $wgDBerrorLog;
    if ( $wgDBerrorLog ) {
        $text = date('D M j G:i:s T Y') . "\t".$text;
        error_log( $text, 3, $wgDBerrorLog );
    }
}

/**
 * @todo document
 */
function logProfilingData() {
    global $wgRequestTime, $wgDebugLogFile, $wgDebugRawPage, $wgRequest;
    global $wgProfiling, $wgProfileStack, $wgProfileLimit, $wgUser;
    $now = wfTime();

    list( $usec, $sec ) = explode( ' ', $wgRequestTime );
    $start = (float)$sec + (float)$usec;
    $elapsed = $now - $start;
    if ( $wgProfiling ) { 
        $prof = wfGetProfilingOutput();
        $forward = '';
        if( !empty( $_SERVER['HTTP_X_FORWARDED_FOR'] ) )
            $forward = ' forwarded for ' . $_SERVER['HTTP_X_FORWARDED_FOR'];
        if( !empty( $_SERVER['HTTP_CLIENT_IP'] ) )
            $forward .= ' client IP ' . $_SERVER['HTTP_CLIENT_IP'];
        if( !empty( $_SERVER['HTTP_FROM'] ) )
            $forward .= ' from ' . $_SERVER['HTTP_FROM'];
        if( $forward )
            $forward = "\t(proxied via {$_SERVER['REMOTE_ADDR']}{$forward})";
        if($wgUser->isAnonymous())
            $forward .= ' anon';
        $log = sprintf( "%s\t%04.3f\t%s\n",
          gmdate( 'YmdHis' ), $elapsed,
          urldecode( $_SERVER['REQUEST_URI'] . $forward ) );
        if ( '' != $wgDebugLogFile && ( $wgRequest->getVal('action') != 'raw' || $wgDebugRawPage ) ) {
            error_log( $log . $prof, 3, $wgDebugLogFile );
        }
    }
}

/**
 * Get a message from anywhere, for the UI elements
 */
function wfMsg( $key ) {
	
	// dynamic replacement for skin keys (bugfix #4382)
	$prefix = 'Skin.Common.';
	if (strcmp(substr($key, 0, strlen($prefix)), $prefix) == 0) 
	{
		$key = wfSkinMsgKey(substr($key, strlen($prefix)));
	}
	
    $args = func_get_args();
    array_shift( $args );
    return wfMsgReal( $key, $args );
}

/**
 * Similar to wfMsg, but key is a template to load (from /resources/templates/)
 * @param string $template - template to load 
 * @param (optional) $args - list of replacements
 * @return string
 */
function wfMsgFromTemplate($template) {
	$args = func_get_args();
	array_shift($args);
	
	$contents = wfLoadTemplateResource($template);
	return wfMsgReplaceText($contents, $args);
}

/**
 * Get a message from a skinning variable
 */
function wfSkinMsgKey($key) 
{
	global $wgActiveTemplate;
	$sk = 'Skin.'.$wgActiveTemplate.'.'.$key;
	if (Language::keyExists(strtolower($sk))) 
	{
		return $sk;
	}
	return 'Skin.Common.'.$key;
}

/**
 * Get a message from anywhere, for the content
 */
function wfMsgForContent( $key ) {
    $args = func_get_args();
    array_shift( $args );
    $forcontent = true;
    return wfMsgReal( $key, $args, $forcontent );
}

/**
 * Get a message, forcing UTF-8 encoding
 * This is mainly a hack for Latin-1 localization conversions.
 */
function wfMsgUTF8( $key ) {
    $args = func_get_args();
    array_shift( $args );
    $msg = wfMsgReal( $key, $args );
    return $msg;
}

/**
 * Get a content message, forcing UTF-8 encoding
 * This is mainly a hack for Latin-1 localization conversions.
 */
function wfMsgForContentUTF8( $key ) {
    $args = func_get_args();
    array_shift( $args );
    $forcontent = true;
    $msg = wfMsgReal( $key, $args, $forcontent );
    return $msg;
}

/**
 * Get a message from the language file, for the UI elements
 */
function wfMsgNoDB( $key ) {
    $args = func_get_args();
    array_shift( $args );
    return wfMsgReal( $key, $args );
}

/**
 * Get a message from the language file, for the content
 */
function wfMsgNoDBForContent( $key ) {
    $args = func_get_args();
    array_shift( $args );
    $forcontent = true;
    return wfMsgReal( $key, $args, $forcontent );
}


/**
 * Really get a message
 */
function wfMsgReal( $key, $args, $forContent=false ) {
    global $wgContLang, $wgLanguageCode;
    global $wgLang;
    static $loaded;
    
    //note, this will *not* support toggling between multiple languages; it will always use the last loaded language
    if (is_null($loaded) || !in_array($wgLanguageCode, $loaded)) {
	    wfLoadLanguageResources();
	    $loaded[] = $wgLanguageCode;
    }
    
    $fname = 'wfMsgReal';
    
    if( $forContent ) {
        $lang = &$wgContLang;
    } else {
        $lang = &$wgLang;
    }

    wfSuppressWarnings();
    if( is_object( $lang ) ) {
        $message = $lang->getMessage( $key );
    } else {
        $message = '';
    }
    wfRestoreWarnings();
    if(!$message)
        $message = Language::getMessage($key);

    # Replace arguments
    $message = wfMsgReplaceText($message, $args);

    $message = str_replace(array('\n', '\r'), array("\n", "\r"), $message);
    return $message;
}

/*
 * Replace $1, $2 ... $9 with corresponding arguments in text
 * @param string $text - text to search for replacements
 * @param array $replacements - array of strings to replace
 * @returns string
 */
function wfMsgReplaceText($text, $replacements = array())
{
	static $replacementKeys = array('$1', '$2', '$3', '$4', '$5', '$6', '$7', '$8', '$9');
	if (count($replacements))
	{
		$text = str_replace($replacementKeys, $replacements, $text);
	}
	return $text;
}

/**
 * Just like exit() but makes a note of it.
 * Commits open transactions except if the error parameter is set
 */
function wfAbruptExit( $error = false ){
    static $called = false;
    if ( $called ){
        exit();
    }
    $called = true;

    if( function_exists( 'debug_backtrace' ) ){ // PHP >= 4.3
        $bt = debug_backtrace();
        for($i = 0; $i < count($bt) ; $i++){
            $file = $bt[$i]['file'];
            $line = $bt[$i]['line'];
            wfDebug("WARNING: Abrupt exit in $file at line $line\n");
        }
    } else {
        wfDebug('WARNING: Abrupt exit\n');
    }
    if ( !$error ) {
	    global $wgDatabase;
        $wgDatabase->close();
    }
    exit();
}

/**
 * @todo document
 */
function wfErrorExit() {
    wfAbruptExit( true );
}

/**
 * Die with a backtrace
 * This is meant as a debugging aid to track down where bad data comes from.
 * Shouldn't be used in production code except maybe in "shouldn't happen" areas.
 *
 * @param string $msg Message shown when dieing.
 */
function wfDebugDieBacktrace( $msg = '' ) {
    global $wgCommandLineMode;

    $backtrace = wfBacktrace();
    if ( $backtrace !== false ) {
        if ( $wgCommandLineMode ) {
            $msg .= "\nBacktrace:\n$backtrace";
        } else {
            $msg .= "\n<p>Backtrace:</p>\n$backtrace";
        }
     }
     die( $msg );
}

function wfBacktrace() {
    global $wgCommandLineMode;
    if ( !function_exists( 'debug_backtrace' ) ) {
        return false;
    }
    
    if ( $wgCommandLineMode ) {
        $msg = '';
    } else {
        $msg = "<ul>\n";
    }
    $backtrace = debug_backtrace();
    foreach( $backtrace as $call ) {
        if( isset( $call['file'] ) ) {
            $f = explode( DIRECTORY_SEPARATOR, $call['file'] );
            $file = $f[count($f)-1];
        } else {
            $file = '-';
        }
        if( isset( $call['line'] ) ) {
            $line = $call['line'];
        } else {
            $line = '-';
        }
        if ( $wgCommandLineMode ) {
            $msg .= "$file line $line calls ";
        } else {
            $msg .= '<li>' . $file . ' line ' . $line . ' calls ';
        }
        if( !empty( $call['class'] ) ) $msg .= $call['class'] . '::';
        $msg .= $call['function'] . '()';

        if ( $wgCommandLineMode ) {
            $msg .= "\n";
        } else {
            $msg .= "</li>\n";
        }
    }
    if ( $wgCommandLineMode ) {
        $msg .= "\n";
    } else {
        $msg .= "</ul>\n";
    }

    return $msg;
}


/* Some generic result counters, pulled out of SearchEngine */


/**
 * @todo document
 */
function wfShowingResults( $offset, $limit ) {
    global $wgLang;
    return wfMsg('Article.Common.showing-results', $wgLang->formatNum( $limit ), $wgLang->formatNum( $offset+1 ));
}

/**
 * @todo document
 */
function wfShowingResultsNum( $offset, $limit, $num ) {
    global $wgLang;
    return wfMsg('Article.Common.showing-results-below', $wgLang->formatNum( $limit ), $wgLang->formatNum( $offset+1 ), $wgLang->formatNum( $num ));
}

/**
 * @todo document
 */
function wfViewPrevNext( $offset, $limit, $link, $query = '', $atend = false, $info = null ) {
    global $wgUser, $wgLang;
    $fmtLimit = $wgLang->formatNum( $limit );
    $prev = wfMsg('Article.Common.previous', $fmtLimit );
    $next = wfMsg('Article.Common.next', $fmtLimit );
    if( is_object( $link ) ) {
        $title =& $link;
    } else {
        $title =& Title::newFromText( $link );
        if( is_null( $title ) ) {
            return false;
        }
    }
    
    $sk = $wgUser->getSkin();
    if ( 0 != $offset ) {
        $po = $offset - $limit;
        if ( $po < 0 ) { $po = 0; }
        $q = "limit={$limit}&offset={$po}";
        if ( '' != $query ) { $q .= '&'.$query; }
        $plink = '<a href="' . $title->escapeLocalUrl( $q ) . "\">{$prev}</a>";
    } else { $plink = $prev; }

    $plink = '<span class="prev">'.$plink.'</span>';
    $no = $offset + $limit;
    $q = 'limit='.$limit.'&offset='.$no;
    if ( '' != $query ) { $q .= '&'.$query; }

    if ( $atend ) {
        $nlink = $next;
    } else {
        $nlink = '<a href="' . $title->escapeLocalUrl( $q ) . "\">{$next}</a>";
    }
    $nlink = '<span class="next">'.$nlink.'</span>';

    if (!is_null($info)) {
	    $info = '<span class="info">'.$info.'</span>';
    }
    return '<div class="pagination">'
    	.wfMsg('Article.Common.pagination', $plink, $info, $nlink )
    .'</div>';
}

/**
 * @todo document
 */
function wfNumLink( $offset, $limit, &$title, $query = '' ) {
    global $wgUser, $wgLang;
    if ( '' == $query ) { $q = ''; }
    else { $q = $query.'&'; }
    $q .= 'limit='.$limit.'&offset='.$offset;

    $fmtLimit = $wgLang->formatNum( $limit );
    $s = '<a href="' . $title->escapeLocalUrl( $q ) . "\">{$fmtLimit}</a>";
    return $s;
}

/**
 * Yay, more global functions!
 */
function wfCheckLimits( $deflimit = 0, $optionname = 'rclimit' ) {
    global $wgRequest;
    return $wgRequest->getLimitOffset( $deflimit, $optionname );
}

/**
 * @todo document
 * @return float
 */
function wfTime() {
    $st = explode( ' ', microtime() );
    return (float)$st[0] + (float)$st[1];
}

/**
 * Sets dest to source and returns the original value of dest
 * If source is NULL, it just returns the value, it doesn't set the variable
 */
function wfSetVar( &$dest, $source ) {
    $temp = $dest;
    if ( !is_null( $source ) ) {
        $dest = $source;
    }
    return $temp;
}

/**
 * As for wfSetVar except setting a bit
 */
function wfSetBit( &$dest, $bit, $state = true ) {
    $temp = (bool)($dest & $bit );
    if ( !is_null( $state ) ) {
        if ( $state ) {
            $dest |= $bit;
        } else {
            $dest &= ~$bit;
        }
    }
    return $temp;
}

/**
 * Windows-compatible version of escapeshellarg()
 * Windows doesn't recognise single-quotes in the shell, but the escapeshellarg() 
 * function puts single quotes in regardless of OS
 */
function wfEscapeShellArg( ) {
    $args = func_get_args();
    $first = true;
    $retVal = '';
    foreach ( $args as $arg ) {
        if ( !$first ) {
            $retVal .= ' ';
        } else {
            $first = false;
        }
    
        if ( wfIsWindows() ) {
            $retVal .= '"' . str_replace( '"','\"', $arg ) . '"';
        } else {
            $retVal .= escapeshellarg( $arg );
        }
    }
    return $retVal;
}

function wfSetFileContent($fileName, $content) {
    $handle = @fopen( $fileName, 'w+' );
    if (!is_resource($handle))
        return false;
    fwrite( $handle, $content );
    fclose( $handle );
    return true;
}

function wfGetFileContent($fileName) {        
    $handle = fopen ($fileName, "r");
    $fileSize = filesize($fileName);
    if ($fileSize > 0)
		$contents = fread ($handle, $fileSize);
	else
		$contents = '';
    fclose ($handle);
    return $contents;
}


/**
 * Provide a simple HTTP error.
 */
function wfHttpError( $code, $label, $desc ) {
    global $wgOut;
    $wgOut->disable();
    header( "HTTP/1.0 $code $label" );
    header( "Status: $code $label" );
    $wgOut->sendCacheControl();

    header( 'Content-type: text/html' );
    print "<html><head><title>" .
        htmlspecialchars( $label ) . 
        "</title></head><body><h1>" . 
        htmlspecialchars( $label ) .
        "</h1><p>" .
        htmlspecialchars( $desc ) .
        "</p></body></html>\n";
}

/**
 * Convenience function; returns MediaWiki timestamp for the present time.
 * @return string
 */
function wfTimestampNow() {
    # return NOW
    return wfTimestamp( TS_MW, time() );
}

/**
 * Sorting hack for MySQL 3, which doesn't use index sorts for DESC
 */
function wfInvertTimestamp( $ts ) {
    return strtr(
        $ts,
        '0123456789',
        '9876543210'
    );
}

/**
 * Reference-counted warning suppression
 */
function wfSuppressWarnings( $end = false ) {
    static $suppressCount = 0;
    static $originalLevel = false;

    if ( $end ) {
        if ( $suppressCount ) {
            $suppressCount --;
            if ( !$suppressCount ) {
                error_reporting( $originalLevel );
            }
        }
    } else {
        if ( !$suppressCount ) {
            $originalLevel = error_reporting( E_ALL & ~( E_WARNING | E_NOTICE ) );
        }
        $suppressCount++;
    }
}

/**
 * Restore error level to previous value
 */
function wfRestoreWarnings() {
    wfSuppressWarnings( true );
}

# Autodetect, convert and provide timestamps of various types

/** Standard unix timestamp (number of seconds since 1 Jan 1970) */
define('TS_UNIX',0);
/** MediaWiki concatenated string timestamp (yyyymmddhhmmss) */
define('TS_MW',1);    
/** Standard database timestamp (yyyy-mm-dd hh:mm:ss) */
define('TS_DB',2);
/** Standard UTC time as used by Dream: "yyyy-MM-ddTHH:mm:ssZ" */
define('TS_DREAM',3);

/**
 * @todo document
 */
function wfTimestamp($outputtype=TS_UNIX,$ts=0) {
    if (preg_match("/^(\d{4})\-(\d\d)\-(\d\d) (\d\d):(\d\d):(\d\d)$/",$ts,$da)) {
        # TS_DB
        $uts=gmmktime((int)$da[4],(int)$da[5],(int)$da[6],
                (int)$da[2],(int)$da[3],(int)$da[1]);
    } elseif (preg_match("/^(\d{4})(\d\d)(\d\d)(\d\d)(\d\d)(\d\d)$/",$ts,$da)) {
        # TS_MW
        $uts=gmmktime((int)$da[4],(int)$da[5],(int)$da[6],
                (int)$da[2],(int)$da[3],(int)$da[1]);
    } elseif (preg_match("/^(\d{1,13})$/",$ts,$datearray)) {
        # TS_UNIX
        $uts=$ts;
    } elseif (preg_match("/^(\d{4})\-(\d\d)\-(\d\d)T(\d\d):(\d\d):(\d\d)Z$/",$ts,$da)) {
        $uts = gmmktime((int)$da[4],(int)$da[5],(int)$da[6],
                (int)$da[2],(int)$da[3],(int)$da[1]);;
    }

    if ($ts == 0)
    {
        $uts = time();
    }
    switch($outputtype) {
	    case TS_UNIX:
	        return $uts;
	        break;
	    case TS_MW:
	        return gmdate( 'YmdHis', $uts );
	        break;
	    case TS_DB:
	        return gmdate( 'Y-m-d H:i:s', $uts );
	        break;
	    case TS_DREAM: 
	    	return gmdate( 'Y-m-d', $uts).'T'. gmdate('h:i:s', $uts ).'Z';
	    	break;
	    default:
	        return;
    }
}

/**
 * Check where as the operating system is Windows
 *
 * @todo document
 * @return bool True if it's windows, False otherwise.
 */
function wfIsWindows() {   
    if (substr(php_uname(), 0, 7) == 'Windows') {   
        return true;   
    } else {   
        return false;   
    }   
} 

/**
 * Swap two variables
 */
function swap( &$x, &$y ) {
    $z = $x;
    $x = $y;
    $y = $z;
}

function wfIncrStats( $key ) {
    global $wgDBname, $wgMemc;
    $key = "$wgDBname:stats:$key";
    if ( is_null( $wgMemc->incr( $key ) ) ) {
        $wgMemc->add( $key, 1 );
    }
}

/***
 * http://www.ilovejackdaniels.com/php/email-address-validation
 */
function wfValidateEmail($email) {
	if (!ereg("[^@]{1,64}@[^@]{1,255}", $email)) {     
		return false;
	}
	# Split it into sections to make life easier
	$email_array = explode("@", $email);
	$local_array = explode(".", $email_array[0]);
	for ($i = 0; $i < sizeof($local_array); $i++) {
		if (!ereg("^(([A-Za-z0-9!#$%&'*+/=?^_`{|}~-][A-Za-z0-9!#$%&'*+/=?^_`{|}~\.-]{0,63})|(\"[^(\\|\")]{0,62}\"))$", $local_array[$i])) {
			return false;
		}
	}  
	# Check if domain is IP. If not, it should be valid domain name
	if (!ereg("^\[?[0-9\.]+\]?$", $email_array[1])) {
		$domain_array = explode(".", $email_array[1]);
		if (sizeof($domain_array) < 2) {
			return false; // Not enough parts to domain
		}
		for ($i = 0; $i < sizeof($domain_array); $i++) {
			if (!ereg("^(([A-Za-z0-9][A-Za-z0-9-]{0,61}[A-Za-z0-9])|([A-Za-z0-9]+))$", $domain_array[$i])) {
				return false;
			}
		}
	}
	return $email;
}


/**
  * MT ursm
  *
  * wfFileNameEncode
  * 
  */
function wfFileNameEncode($special) {
    $res = array();
    foreach ($special as $val) {
        $res [] = '%' . dechex(ord($val));
    }
    return $res;
}

/**
  * MT ursm
  *
  * wfGetTopicURLFromFileName
  * 
  */
function wfGetTopicURLFromFileName(&$fileName) {
    return rawurldecode($fileName);
}

function wfFormatSize($size) {
    if ( $size < 2*1024 )
        return wfMsg('System.Common.nbytes', $size );
    if ( $size < 2*1024*1024 )
        return wfMsg('System.Common.nkbytes', round( $size / 10.24 )/100.0 );
    if ( $size < 2*1024*1024*1024 )
        return wfMsg('System.Common.nmbytes', round( $size / ( 1024*10.24 ) )/100.0 );
    return  wfMsg('System.Common.ngbytes', round( $size / ( 1024*1024*10.24 ) )/100.0 );
}

function wfGetFileNames($dir, $strtolower = false) {
    $files = array();
    if (is_dir($dir) && $dh = opendir($dir)) {
        while (($file = readdir($dh)) !== false) {
            if ($file == '.' || $file == '..')
                continue;
            
            $fullName = "$dir" . DIRECTORY_SEPARATOR . "$file";
            if (is_file($fullName))
                $files[$strtolower ? strtolower($file) : $file] = $fullName;
        }
        closedir($dh);
    }
    return $files;
}

function wfGetDirectories($dir, $toignore = array()) {
    $dirs = array();
    if (is_dir($dir) && $dh = opendir($dir)) {
        while (($file = readdir($dh)) !== false) {
            if ($file == '.' || $file == '..')
                continue;
            
            $fullName = "$dir" . DIRECTORY_SEPARATOR . "$file";
            if (is_dir($fullName) && !in_array($file, $toignore)) 
                $dirs[$file] = $fullName;
        }
        closedir($dh);
    }
    return $dirs;
}

function wfSaveEdit($articleId, $titleName, $text, $time, $section) {   
    global $wgTitle, $wgArticle;

    require_once( 'EditPage.php' );
    
    $wgTitle = Title::newFromId($articleId);
    $wgArticle = new Article($wgTitle);
    $edit = New EditPage($wgArticle);
    
    $text = urldecode($text);
    $edit->aid = $articleId;
    $edit->textbox1 = $text;
    $edit->edittime = $time;
    $edit->minoredit = true;
    $edit->section = $section;
    $edit->editForm('save');
    $updated = new Article($wgTitle);
    $updatedTime = $updated->getTimestamp();
        
    return $updatedTime .'|1|'. wfMsg('Article.Common.article-save-success'); //second parameter determines success or error
}

// MT (ursm)
function wfEscapleRegEx($str) {
    return str_replace(
        array('[', '^', '$', '.', '|',  '?', '*', '+',  '(', ')', "'", '\\'),
        array('\[','.', '.', '.', '\|', '.', '.', '\+', '.', '.', '.', '\\\\\\\\'),
        $str);
}

// MT (ursm)
function wfCreateICaseRegEx($str) {
    $len = strlen($str);
    $strLwr = strtolower($str);
    $strUpr = strtoupper($str);
    $strReg = '';
    for ($i = 0; $i < $len; ++$i) {
        $a = wfEscapleRegEx($strUpr{$i});
        $b = wfEscapleRegEx($strLwr{$i});
        $strReg .= $a != $b ? '[' . $a . $b . ']' : $a;
    }
    return $strReg;
}

//deprecated function; use FlashMessage
function wfMessagePrint($type = 'general') {
	return FlashMessage::output($type);
}
//deprecated function; use FlashMessage
function wfMessageOutput($msg, $id = false, $class = 'error') {
    return FlashMessage::format($msg, $id, $class);
}
//deprecated function; use FlashMessage
function wfMessagePush($name, $message, $type = 'error') {
	return FlashMessage::push($name, $message, $type);
}

class FlashMessage {
	/***
	 * for future releases, we shouldn't write directly into the $_SESSION variable
	 * it'd be more ideal to write into a global variable, which committed to $_SESSION for 
	 * transport on redirects (see OutputPage::output & Sajax)
	 * it's also possible we can transport via the GET parameters, which would reduce the dependency on $_SESSIONS altogether
	 */
	 
	//stub
	function commit() {	}
	//stub
	function retrieve() { }
	function push($name, $message, $type = 'error') {
	    $_SESSION['msg'][$name][$type][] = $message;
	}
	function format($msg, $id = false, $class = 'error') {
		return '<div class="'.$class.'msg systemmsg" '.($id ? 'id="'.$id.'"': '').'><div class="inner">'.$msg.'</div></div>';
	}
	function output($type = 'general') {
	    $html = '';
	    if (isset($_SESSION['msg']) && isset($_SESSION['msg'][$type]) && $_item = $_SESSION['msg'][$type]) {
	        foreach ($_item as $key => $value) {
	        	//KA - reset $msg
	        	$msg = '';
	            if (is_array($value)) {
	                foreach ($value as $error) {
	                    $msg .= '<li>'.$error.'</li>';    
	                }
	            }
	            else {
	                $msg = '<li>'.$value.'</li>';
	            }
	            unset($_SESSION['msg'][$type][$key]);
	            $html .= FlashMessage::format('<ul class="flashMsg">'.$msg.'</ul>', 'sessionMsg', $key);
	        }
	    }
	    else if ($type == 'general')
	    {
	    	// always output a message container
	    	$html .= '<div id="sessionMsg"><div class="inner"><ul></ul></div></div>';
	    }
	    return $html;
	}
}

function wfGetHomePageId() {
	global $wgDekiPlug;
	$r = $wgDekiPlug->At('pages', 'HOME', 'info')->Get();
	if ($r['status'] == 200) {
		return wfArrayVal($r, 'body/page/@id');
	}
	return 0;
}

function validateTimeZone( $s ) {
    if ( $s !== '' ) {
        if ( strpos( $s, ':' ) ) {
            # HH:MM
            $array = explode( ':' , $s );
            $hour = intval( $array[0] );
            $minute = intval( $array[1] );
        } else {
            $minute = intval( $s * 60 );
            $hour = intval( $minute / 60 );
            $minute = abs( $minute ) % 60;
        }
        $hour = min( $hour, 15 );
        $hour = max( $hour, -15 );
        $minute = min( $minute, 59 );
        $minute = max( $minute, 0 );
        $s = sprintf( "%+03d:%02d", $hour, $minute );
    }
    return $s;
}

/**
 * Encodes a string for javascript output. Assumes using single quotes for wrapping
 * @param string $string
 * @return string
 */
function wfEncodeJSString($string) {
    $string = str_replace(array('\\',"'"), array('\\\\',"\'"), $string);
	$string = str_replace ("\n", " ", $string);
    return $string;
}

function wfEscapeString($s) {	
	$find = array('\\', '"');
	$replace = array('\\\\', '\"');

	return str_replace($find, $replace, $s);
}
function wfEncodeJSHTML($string) {
    return htmlspecialchars(wfEncodeJSString($string));    
}

/*
    # = %23
    % = %25
    [ = %5B
    ] = %5D
    { = %7B
    | = %7C
    } = %7D
    + = %2B
    < = %3C
    > = %3E
    / = //
*/
function wfEncodeTitle($shortTitleName, $encodeSlash = true, $encodeFragment = true) {
    global $wgInputEncoding;
    $shortTitleName = do_html_entity_decode( $shortTitleName, ENT_COMPAT, $wgInputEncoding );
    $shortTitleName = preg_replace ("/\xC2\xA0/", " ", $shortTitleName);
    
    $search = array('%','[',']','{','}','|','+','<','>');
    $replace = array('%25','%5B','%5D','%7B','%7D','%7C','%2B','%3C','%3E');
    if ($encodeFragment) {
        $search[] = '#';
        $replace[] = '%23';
    }
    if ($encodeSlash) {
        $search[] = '/';
        $replace[] = '//';
    }
    return str_replace($search,$replace,$shortTitleName);
}

function wfDecodeTitle($shortTitleName, $decodeSlash = true) {
    $search = array('%23','%5B','%5D','%7B','%7D','%7C','%2B','%3C','%3E','%25');
    $research = array('#','[',']','{','}','|','+','<','>','%');
    if ($decodeSlash) {
        $search[] = '//';
        $research[] = '/';
    }
    return str_replace($search, $research, $shortTitleName);
}

function wfEncodeString($specialChars, $str) {
    array_unshift($specialChars, '%');
    $replace = wfFileNameEncode($specialChars);
    return str_replace($specialChars, $replace, $str);
}

function wfConvertToString($obj) {
    $ret = '';
    if (is_object($obj)) {
        foreach ($obj as $key => $val) {
            if ($ret) $ret .= ';';
            $ret .= wfEncodeString(array(';','='),wfConvertToString($key)) . '=' . wfEncodeString(array(';','='), wfConvertToString($val));
        }
        return "@$ret";
    }
    if (is_array($obj)) {
        foreach ($obj as $val) {
            if ($ret) $ret .= '|';
            $ret .= wfEncodeString(array('|'), wfConvertToString($val));
        }
        return "#$ret";
    }
    if (is_null($obj))
        return "0";
    if ($obj === true)
        return "t";
    if ($obj === false)
        return "f";
    if (is_integer($obj))
        return "i$obj";
    if (is_double($obj))
        return "d$obj";
    return 's' . rawurlencode($obj);
}

function isWindows() {
   	return (DIRECTORY_SEPARATOR != '/');
}

/***
 * wfShowTree()
 */
function wfShowTree($id, $ns, $tree, $sk) {
	global $wgDekiPlug, $wgOut;
	if ($ns == NS_MAIN) {
		return $wgDekiPlug->At('pages')->With('format', 'html')->Get();	
	}
	switch ($ns) {
		case NS_TEMPLATE: 
		case NS_USER:
			$pageTitle = '='.urlencode(urlencode(DekiNamespace::getCanonicalName($ns).':'));
			$r = $wgDekiPlug->At('pages', $pageTitle, 'tree')->With('format', 'html')->With('startpage', 'false')->Get();
			break;	
		default:
			$r = $wgDekiPlug->At('pages', $pageTitle, 'tree')->With('format', 'html')->Get();
			break;	
	}
	if (!MTMessage::HandleFromDream($r) || empty($r['body'])) {
		switch (DekiNamespace::getCanonicalName($ns))
		{
			case 'Template':
				return wfMsg('Page.ListTemplates.page-has-no-text');
			default:
				return wfMsg('Article.Common.page-has-no-text');
		}
	}
	return $r['body'];
}

function wfShowNSTree($ns, $addTitle = false) {
	//output
	global $wgOut, $wgUser, $wgSitename;
	$tree = '';
	$sk = $wgUser->getSkin();
	if ($addTitle) {
    	switch ($ns) {
	    	case NS_MAIN:
	        	$wgOut->addHTML('<p><strong><a href="'.$sk->makeUrl(wfHomePageInternalTitle()).'">'.$wgSitename.'</a></strong></p>');
	        	break;
	        case NS_TEMPLATE:
	        	$nt = Title::newFromText(DekiNamespace::getCanonicalName(NS_TEMPLATE).':');
	        	$wgOut->addHTML('<p><strong><a href="'.$nt->getLocalURL().'">'.$addTitle.'</a></strong></p>');
	        	break;
	        case NS_USER:
	        	$nt = Title::newFromText(DekiNamespace::getCanonicalName(NS_USER).':');
	        	$wgOut->addHTML('<p><strong><a href="'.$nt->getLocalURL().'">'.$addTitle.'</a></strong></p>');
	        	break;
    	}
	}
	$wgOut->addHTML(wfShowTree(0, $ns, $tree, $sk));
}

function wfTruncateFilename($filename = '', $length = 16) {
	if (mb_strlen($filename) < $length) {
		return $filename;	
	}
	return mb_substr($filename, 0, $length - 3).'...';
}

function wfGetGroupSuffix() 
{
	return wfMsg('System.Common.group-suffix');	
}

function wfPrintR($array) {
	echo('<pre>'.print_r($array, true).'</pre>');
}

function wfGetRestrictionGroupName($name) 
{
	return $name.wfGetGroupSuffix();	
}
function wfGetRestrictions($results, $nolist = array()) 
{
 	$grantlist = array();
 	$grants = wfArrayValAll($results, 'body/security/grants/grant', $grantlist);
 	foreach ($grants as $grant) 
 	{
	 	$groupid = wfArrayVal($grant, 'group/@id');
	 	$userid = wfArrayVal($grant, 'user/@id');
	 	if ($groupid > 0) 
	 	{
		 	$name = wfGetRestrictionGroupName(wfArrayVal($grant, 'group/groupname'));
		 	$key = 'g'.$groupid;
	 	}
	 	if ($userid > 0) 
	 	{
		 	$name = wfArrayVal($grant, 'user/username');
		 	$key = 'u'.$userid;
	 	}
	 	$grantlist[$key] = $name;
 	}
 	if (!empty($grantlist)) {
	 	asort($grantlist);
 	}
 	return empty($grantlist) ? $nolist: $grantlist;
}

function wfHomePageTitle() {
	global $wgSitename;
	return $wgSitename;
}

function wfHomePageInternalTitle() {
	return '';
}

function wfCreateUserPage($userName) {
	if ($userName == '') {
		return $userName;
	}
	$nt = Title::newFromText($userName, NS_USER);
    if (is_object($nt) && $nt->getArticleID() == 0) {
        $article = new Article($nt);
        $article->insertNewArticle( wfMsg('System.API.new-user-page-text'), '', true, true );
    }
}

/***
 * not a true nonce, but something we know is unique and isn't easily guessed
 * basically the timestamp serves as the salt, which is then concatenated with the username for the hash
 */
function wfCreateUserRegistrationNonce($timestamp, $username) {
	return md5($timestamp.$username);
}

function wfCheckTitleId($titleId, $action = null, $redirect = 'notopic.php') {
	$nt = Title::newFromID($titleId);
	
	if (is_null($nt)) 
	{
		header('Location: '.$redirect);
		exit();
	}
	
	if (!is_null($action)) 
	{
		$wgArticle = new Article($nt);
		if (!$wgArticle->$action()) 
		{
			header('Location: '.$redirect);
			exit();
		}
	}
	
	$lang = $wgArticle->getLanguage();
	if (!empty($lang)) 
	{
		global $wgLanguageCode;
		$wgLanguageCode = $lang;
	}
}

//uesd primarily for config setting
function wfSetArrayVal(&$array, $key, $value = null) {
	$keys = explode('/', $key);
	$count = count($keys);
	$i = 0;
	foreach ($keys as $key) {
		$i++;
		
		//last value
		if ($i == $count) {
			if (is_null($value)) {
				unset($array[$key]);
				return;
			}
			if (!isset($array[$key]) || is_string($array[$key])) {
				$array[$key] = $value;	
			}
			//if you're attempting to write a string value to a key that is an array, this operation will fail
		}
		else {
			if (!isset($array[$key])) {
				$array[$key] = array();
			}
		}
		if (is_string($array[$key])) {
			return;
		}
		$array = &$array[$key];
	}
}
/***
 * Given an array $array, will try to find $key, which is delimited by /
 * if $key itself is an array of multiple values which has a key of '0', will return the first value
 * this is useful for getting stuff back from the api and to avoid the "cannot use string offset as array" error, 
 * see http://www.zend.com/forums/index.php?S=ab6bd42e992e7497c9b0ba4a33b01dd9&t=msg&th=1556
 */
function wfArrayVal($array, $key = '', $default = null) {
	if ($key == '') {
		return $array;
	}
	$keys = explode('/', $key);
	$count = count($keys);
	$i = 0;
	foreach ($keys as $k => $val) {
		$i++;
		if ($val == '') {
			continue;
		}
		if (isset($array[$val]) && !is_array($array[$val])) {
			// see bugfix 4974; this used to do an empty string check, but that leads to ambiguity between empty string and null
			if ((is_string($array[$val]) || is_int($array[$val])) && $i == $count) {
				 return $array[$val];
			}
			return $default; 
		}
		if (isset($array[$val])) {
			$array = $array[$val];
		}
		else {
			return $default;
		}
		if (is_array($array) && key($array) == '0') {
			$array = current($array);
		}
	}
	return $array;
}

function wfArrayValAll($array, $key = '', $default = null) {
	if ($key == '') {
		return $array;
	}
	$keys = explode('/', $key);
	$count = count($keys);
	$i = 0;
	foreach ($keys as $val) {
		$i++;
		if ($val == '') {
			continue;
		}
		if (!isset($array[$val]) || !is_array($array[$val])) {
			return $default; 
		}
		$array = $array[$val]; 
		if ($i == $count) {
			if (key($array) != '0') {
				$array = array($array);
			}
		}
	}
	return $array;
}

//does the same thing as parse_url, but returns query as a key/val array instead of one string
function wfParseUrl($url) {
	if ($url == '') {
		return false;
	}
	$url = parse_url($url);
	if (isset($url['query']) && $url['query'] != '') {
		$q = explode('&', $url['query']);
		$qv = array(); //query values
		foreach ($q as $v) {
			list($key, $val) = explode('=', $v);
			$qv[$key] = $val;
		}
		$url['query'] = $qv;
	}
	return $url;
}

//opposite of parse_url()
function wfUnparseUrl($parsed)
{
	if (!is_array($parsed))
	{
		return false;
	}
	
	if (isset($parsed['query']) && is_array($parsed['query']))
	{
		$q = array();
		foreach ($parsed['query'] as $key => $val)
		{
			$q[] = $key.'='.$val;
		}
	
		$parsed['query'] = implode('&', $q);   
	}
	    
	$uri = isset($parsed['scheme']) ? $parsed['scheme'].':'.((strtolower($parsed['scheme']) == 'mailto') ? '':'//'): '';
	$uri .= isset($parsed['user']) ? $parsed['user'].($parsed['pass']? ':'.$parsed['pass']:'').'@':'';
	$uri .= isset($parsed['host']) ? $parsed['host'] : '';
	$uri .= isset($parsed['port']) ? ':'.$parsed['port'] : '';
	$uri .= isset($parsed['path']) ? $parsed['path'] : '';
	$uri .= isset($parsed['query']) ? '?'.$parsed['query'] : '';
	$uri .= isset($parsed['fragment']) ? '#'.$parsed['fragment'] : '';
	  
	return $uri;
}

function wfScrubSensitiveArray(&$a)
{
	foreach ($a as $key => &$value)
	{
		if (is_array($value))
		{
			wfScrubSensitiveArray($value);
		}
		else if ($key == 'uri')
		{
			$value = wfScrubSensitive($value);
		}
	}
	unset($value);
}

//given a URL, will scrub out sensitive API info
function wfScrubSensitive($url) {
	$u = wfParseUrl($url);
	
	//scrub passwords
	if (isset($u['pass']))
	{
		$u['pass'] = wfMsg('System.Common.sensitive-data-replacement');
	}
	if (isset($u['query']['password']))
	{
		 $u['query']['password'] = wfMsg('System.Common.sensitive-data-replacement');
	}
	if (isset($u['query']['authpassword']))
	{
		 $u['query']['authpassword'] = wfMsg('System.Common.sensitive-data-replacement');
	}
	
	//scrub apikey
	if (isset($u['query']['apikey']))
	{
		$u['query']['apikey'] = wfMsg('System.Common.sensitive-data-replacement');
	}
	
	return wfUnparseUrl($u);
}

//used in updaters-mindtouch.inc
function wfPatchLocalSettingsForApiKey($apiKey) {
	global $IP;
	$localsettings = $IP.'/LocalSettings.php';
	$ls = wfGetFileContent(	$localsettings);
	
	//certain updates of the beta release didn't have the PHP closing tags 
	if (strpos($ls, '?>') === false) {
		$ls = $ls."\n".'?>';	
	}
	$ls = str_replace('?>', '$wgDekiApiKey = \''.$apiKey.'\';'."\n".'?>', $ls);
	wfSetFileContent($localsettings, trim($ls));
}

function wfSelectForm($name, $data, $setValue = false, $params = array()) {
	global $wgRequest;
	
	$field = '';
	
	if (!isset($params['class'])) {
		$params['class'] = 'input-select';
	}
	
	if (isset($wgRequest)) {
		if (!$setValue && $wgRequest->getVal($name) != '') {
			$setValue = $wgRequest->getVal($name);
		}
	}
	
	if (!isset($params['id'])) {
		$params['id'] = 'select-'.$name;	
	}
	
	if (count($params) > 0) {
		foreach ($params as $key => $value) {
			$field.= " $key=\"".$value."\"";	
		}	
	}
	$html = '<select name="'.$name.'" '.$field.'>';
	if (count($data) > 0) {
		foreach($data as $key => $value) {
			if (!is_array($value)) {
				$selected = ($key == $setValue)? ' selected = "selected"': '';
				$html .= '<option value="'.$key.'"'.$selected.'>'.$value.'</option>';	
			}
			else {
				$html .= '<optgroup label="'.$key.'">';
				foreach ($value as $key => $dvalue) {
					$selected = ($key == $setValue)? " selected": "";
					$html .= '<option value="'.$key.'"'.$selected.'>'.$dvalue.'</option>';	
				}
				$html.= '</optgroup>';
			}
		}
	}	
	$html.= '</select>';
	return $html;
}

/***
 * Helper function for creating an <input> form
 */
function wfInputForm($type, $name, $value = null, $params = array(), $labeltext = '') 
{		
	
	if (is_null($value)) 
	{
		global $wgRequest;
		if (!is_null($wgRequest)) 
		{
			if ($wgRequest->getVal($name)) 
			{
				$value = $wgRequest->getVal($name);
			}
		}
	}
	//submit options
	if ($type == 'submit') 
	{
		$value.= ' &#187;';	
	}
		
	//text and password options
	if ($type == 'text' || $type == 'password') 
	{
		if (!isset($params['size'])) 
		{
			$params['size'] = '24';
		}	
	}
	
	//textarea options
	if ($type == 'textarea') 
	{
		if (!isset($params['rows'])) 
		{
			$params['rows'] = '10';
		}
		if (!isset($params['cols'])) 
		{
			$params['cols'] = '36';
		}
	}
	
	//checkbox options
	if ($type == 'checkbox') 
	{
		if (array_key_exists('checked', $params) && ($params['checked'] == '1' || $params['checked'] == 'checked')) 
		{
			$params['checked'] = 'checked';
		}
		else 
		{
			unset($params['checked']);
		}
		if (is_null($value)) 
		{
			$value = 'checked';
		}
	}
	
	//define class and id
	if (!isset($params['class']) && $type != 'hidden') 
	{
		$params['class'] = 'input-'.$type;
	}
 	if (!isset($params['id']) && $name != '') 
 	{
 		$params['id'] = ($type == 'radio') ? $type.'-'.$name.'-'.$value: $type.'-'.$name;
 	}
	
	$html = array();
	$paras = array();
	
	if (isset($params['disabled']) && $params['disabled'] === true) 
	{
		$params['disabled'] = 'disabled';
		$params['class'] = $params['class'].' disabled';
		unset($params['onclick']);
	}
	else 
	{
		unset($params['disabled']);
	}
	
	foreach ($params as $key => $param) 
	{
		$paras[]= $key.'="'.$param.'"';
	}
	$_escape_types = array('textarea', 'text');
	if (in_array($type, $_escape_types)) 
	{ 
		$value = htmlspecialchars($value);	
	}
	
	if ($type == 'button') 
	{
		$html = '<button name="'.$name.'" value="'.$value.'">'.htmlspecialchars($labeltext).'</button>';
	}
	elseif ($type != 'textarea') 
	{
		$html = '<input type="'.$type.'" '.(!is_null($value) ? 'value="'.$value.'"': '').' '
			.($name != '' ? 'name="'.$name.'"': '').' '.implode(' ', $paras).'/>';
	}
	elseif ($type == 'textarea') 
	{
		$html = '<textarea name="'.$name.'" '.implode(' ', $paras).'>'.$value.'</textarea>';
	}
	if ($labeltext != '' && $type != 'button') 
	{
		$html.= ' <label for="'.$params['id'].'">'.$labeltext.'</label>';	
	}
	return $html;
}

function set_cookie($Name, $Value = null, $CookieExpiration = 0, $Path = null, $Domain = null, $Secure = false, $HTTPOnly = true)
{
	if (version_compare(PHP_VERSION, '5.2.0', '<'))
	{
		// prior to 5.2, HTTPOnly was not supported
		if ($CookieExpiration != 0)
		{
			$gmtExpiration = gmdate('D, d-M-Y h:i:s', (time() - date('Z', time())) + $CookieExpiration);
		}
		
		header('Set-Cookie: ' . $Name . '=' . $Value
			. ($CookieExpiration != 0 ? '; expires='. $gmtExpiration .' GMT': '')
		    . (empty($Path)   ? '' : '; path=' . $Path)
		    . (empty($Domain) ? '' : '; domain=' . $Domain)
		    . (!$Secure       ? '' : '; secure')
		    . (!$HTTPOnly     ? '' : '; HttpOnly'), false);		
	}
	else
	{
		if ($CookieExpiration != 0)
		{
			$CookieExpiration = time() + $CookieExpiration;
		}

		// use PHP's setcookie function, calculates expiry from client time
		setrawcookie($Name, $Value, $CookieExpiration, $Path, $Domain, $Secure, $HTTPOnly);
	}
}


/***
 * Logos can exist in multiple states: (1) default logo, (2) default logo for a skin, or (3) custom uploaded logo
 * Precedence should given in reverse order
 */
function wfGetSiteLogo() 
{
	global $wgLogo, $wgActiveTemplate, $wgActiveSkin, $IP, $wgLogoDefault;
	
	//if we're still using an uploaded logo
	$deflen = strlen($wgLogoDefault);
	if (substr($wgLogo, strlen($wgLogo) - $deflen) != $wgLogoDefault) 
	{
		return $wgLogo;
	}
	
	//if this custom skin contains a logo.png, override the main logo
	if (is_file(Skin::getSkinDir().'/logo.png')) 
	{
		$wgLogo = Skin::getSkinPath().'/logo.png';
	}	
	return $wgLogo;
}

/**
 * Insert spaces into long strings. Used for view normalization purposes (e.g. table breakage)
 *
 * @param string $aLongString - long string, it will be checked for min length
 * @return string - initial string with spaces or string itself if too short
 */
function wfInsertSpaces($aLongString)
{
	$lMaxLength = 30;
	if ($lMaxLength < mb_strlen($aLongString))
	{
		$lSubStr = str_split($aLongString, $lMaxLength);
		$aLongString = implode(' ', $lSubStr);
	}
	return $aLongString;
}

function strexist($needle, $haystack) {
	return strpos($haystack, $needle) !== false;
}

/**
 * Generates an absolute url to the control panel
 * @param string $pageName - should be the name of a control panel controller, i.e. product_activation
 */
function wfGetControlPanelUrl($pageName = null)
{
	global $wgControlPanelPath;
	
	$url = $wgControlPanelPath;

	if (!is_null($pageName))
	{
		$url .= '/' . $pageName . '.php';
	}

	return $url;
}

/**
 * Takes a full meta tag and parses it into its components
 * @note guerrics: quick and dirty parsing, feel free to reimplement.
 * intentionally did not use a regex. 
 * 
 * @param string $tag
 * @return array(
 * 	'name' => string,
 * 	'content' => string,
 * 	'http' => bool
 * )
 */
function wfParseMetaTag($tag)
{
	$meta = array(
		'name' => '',
		'content' => '',
		'http' => false
	);
	
	$parts = explode('"', $tag);
	for ($i = 0, $iM = count($parts); $i < $iM; $i+=2)
	{
		$key = $parts[$i];
		$value = isset($parts[$i+1]) ? $parts[$i+1] : '';

		switch (substr($key, -4))
		{
			// http-equiv
			case 'uiv=':
				$meta['http'] = true;
			// name
			case 'ame=':
				$meta['name'] = $value;
				break;
			// content
			case 'ent=':
				$meta['content'] = $value;
			default:
		}
	}

	return $meta;
}

/**
 * Parses head contents and removes the meta tags
 * 
 * @param string $head - head portion to parse(will not be modified)
 * @param array<string> &$metaTags - array to return tags in
 * 
 * @return string - new head without meta tags
 */
function wfParseMetaTags(&$head, array &$metaTags)
{
	$metaPattern = "#(<meta[^>]+/>)#i";
	$splits = preg_split($metaPattern, $head, -1, PREG_SPLIT_DELIM_CAPTURE);

	$newHead = '';
	foreach ($splits as &$split)
	{
		if (strncasecmp($split, '<meta', 5) == 0)
		{
			// meta tag
			$metaTags[] = $split;
		}
		else
		{
			// not a meta tag
			$newHead .= $split;
		}
	}
	
	return $newHead;
}
