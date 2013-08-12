<?php

function install_php_wrapper( $phpContents ) 
{
	// Bugfix #8677: DEFECT: Invalid XML Error On RSS Feed in IE7 due to trailing newline in LS.php 
	// removed trailing PHP tag
	$phpFile = "<" . "?php" . NEWLINE . $phpContents;
	$phpFile = str_replace( "\r\n", "\n", $phpFile );
	return $phpFile;
}


function escapePhpString( $string ) {
	return strtr( $string,
		array(
			"\n" => "\\n",
			"\r" => "\\r",
			"\t" => "\\t",
			"\\" => "\\\\",
			"\$" => "\\\$",
			"\"" => "\\\""
		));
}

class ConfigData 
{
	public function getSysopName() 		{ return $this->getValue('SysopName'); }
	public function getSysopEmail() 	{ return $this->getValue('SysopEmail'); }
	public function getSysopPass() 		{ return $this->getValue('SysopPass'); }
	public function getProductKey()
	{
		$apikey = $this->getValue('ApiKey');
		return !empty($apikey) ? strtoupper(md5($apikey)) : 'MISSING API KEY';
	}
	public function getEdition()
	{
		// if no selection is made, default to core
		return $this->getValue('SelectedEdition', 'core');
	}
	
	// lead fields
	public function getEditionLead()
	{
		switch ($this->getEdition())
		{
			case 'commercial':
				return 'MindTouch TCS';
			case 'platform':
				return 'MindTouch Platform';
			case 'core':
			default:
				return 'MindTouch Core';
		}
	}
	
	// license fields
	public function getProductType()
	{
		switch ($this->getEdition())
		{
			case 'commercial':
				return 'commercial';
			case 'platform':
				return 'platform';
			case 'core':
			default:
				return 'community';
		}		
	}
	
	// version information
	public function getProductVersion()
	{
		global $wgProductVersion;
		if (!isset($wgProductVersion))
		{
			// import default settings into the current scope
			$IP = $this->getValue('IP');
			include $IP . '/includes/DefaultSettings.php';
		}	
		return $wgProductVersion;
	}
	
	public function isCore()
	{
		return $this->getEdition() == 'core';
	}

	public function setSchema($schema) 
	{
		$this->DBschema = $schema;
		switch ( $this->DBschema ) {
			case 'mysql5':
				$this->DBTableOptions = 'ENGINE=InnoDB, DEFAULT CHARSET=utf8';
				$this->DBmysql5 = 'true';
				break;
			case 'mysql5-binary':
				$this->DBTableOptions = 'ENGINE=InnoDB, DEFAULT CHARSET=binary';
				$this->DBmysql5 = 'true';
				break;
			default:
				$this->DBTableOptions = 'TYPE=InnoDB';
				$this->DBmysql5 = 'false';
		}
		# Set the global for use during install
		global $wgDBTableOptions;
		$wgDBTableOptions = $this->DBTableOptions;
	}
	
	/*
	 * Database creds
	 */
	public function getDatabaseRootUser()
	{
		global $wgDBadminuser;
		return $this->getValue('RootUser', $wgDBadminuser);
	}
	
	public function getDatabaseRootPassword()
	{
		global $wgDBadminpassword;
		return $this->getValue('RootPW', $wgDBadminpassword);
	}
	
	/*
	 * Installation paths
	 */
	public function getLucenePath()
	{
		global $wgLucenePath;
		return $this->getValue('LuceneStore', $wgLucenePath);
	}
	
	public function getImageMagickConvert()
	{
		global $wgPathConvert;
		return $this->getValue('ImageMagickConvert', $wgPathConvert);
	}
	
	public function getImageMagickIdentify()
	{
		global $wgPathIdentify;
		return $this->getValue('ImageMagickIdentify', $wgPathIdentify);
	}
	
	public function getMonoPath()
	{
		global $wgPathMono;
		return $this->getValue('Mono', $wgPathMono);
	}
	
	public function getPrincePath()
	{
		global $wgPathPrince;
		return $this->getValue('prince', $wgPathPrince);
	}
	
	// @TODO: can this method be private? or removed?
	public function getEncoded( $data ) 
	{
		// removing latin1 support, no need...
		return $data;
	}
	
	private function getValue($field, $default = null)
	{
		return isset($this->$field) ? $this->$field : $default;
	}
}


function dieout( $text ) 
{
	die( $text . "\n\n</body>\n</html>" );
}

function echo_warn() 
{
	echo(formatmessage(func_get_args(), 'warn'));
}
function echo_list() 
{
	echo(formatmessage(func_get_args(), ''));
}
function echo_success() 
{
	echo(formatmessage(func_get_args(), 'success'));
}
function echo_fail() 
{
	echo(formatmessage(func_get_args(), 'error'));
}
function formatmessage( $args, $type ) 
{
	$list = sprintf('<li class="%s"><strong class="%s">%s</strong>: ', $type, $type, wfMsg('Page.Install.type-'.$type));
	echo(sprintf($list.'%s', count($args) > 1 ? call_user_func_array('sprintf', $args): $args[0]));
}


function importVar( &$var, $name, $default = "" ) 
{
	if( isset( $var[$name] ) ) {
		$retval = $var[$name];
		if ( get_magic_quotes_gpc() ) {
			$retval = stripslashes( $retval );
		}
	} else {
		$retval = $default;
	}
	return $retval;
}

function importPost( $name, $default = "" ) 
{
	return importVar( $_POST, $name, $default );
}

function importCheck( $name, $default = false ) 
{
	return isset( $_POST[$name] ) ? true: $default;
}

function importRequest( $name, $default = "" ) 
{
	return importVar( $_REQUEST, $name, $default );
}

function generateKey( $length = 32 ) 
{
	$pattern = "0123456789aAbBcCdDeEfFgGhHiIjJkKlLmMnNoOpPqQrRsStTuUvVwWxXyYzZ";
	$key = "";
	for($i = 0; $i < $length; $i++) {
		$key .= $pattern{mt_rand(0,strlen($pattern)-1)};
	}
	return $key;
}

//visual helpers

$radioCount = 0;

function aField( &$conf, $field, $text, $type = "text", $value = "", $onclick = '' ) {
	global $radioCount;
	if( $type != "" ) {
		$xtype = "type=\"$type\"";
	} else {
		$xtype = "";
	}

	$id = $field;
	$nolabel = ($type == "radio") || ($type == "hidden");

	if ($type == 'radio')
		$id .= $radioCount++;

	if( $nolabel ) {
		echo "\t\t<label>";
	} else {
		echo "\t\t<label class='column' for=\"$id\">$text</label>\n";
	}

	if( $type == "radio" && $value == $conf->$field ) {
		$checked = "checked='checked'";
	} else {
		$checked = "";
	}
	echo "\t\t<input $xtype name=\"$field\" id=\"$id\" class=\"iput-$type\" $checked ";
	if ($onclick) {
		echo " onclick='toggleDBarea(\"$value\",1)' " ;
	}
	echo "value=\"";
	if( $type == "radio" ) {
		echo htmlspecialchars( $value );
	} else {
		echo htmlspecialchars( $conf->$field );
	}


	echo "\" />\n";
	if( $nolabel ) {
		echo " $text</label>\n";
	}

	global $errs;
	if(isset($errs[$field])) echo "<span class='error'>" . $errs[$field] . "</span>\n";
}

function database_picker($conf) {
	global $ourdb;
	print "\n";
	foreach(array_keys($ourdb) as $db) {
		if ($ourdb[$db]['havedriver']) {
			print "<li>";
			aField( $conf, "DBtype", $ourdb[$db]['fullname'], 'radio', $db, 'onclick');
			print "</li>\n";
		}
	}
	print "\n";
}
/**
 * Method called on first page load with no post state.
 *
 * @param Object $conf
 */
function initialize_product_installation($conf)
{
	$scheme = isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] == 'on' ? 'https' : 'http';

	echo '<div class="analytics" style="position:absolute; top:-500px; left:-500px; width: 1px; height: 1px;">';
		echo '<iframe width="1" height="1" src="'.$scheme.'://assets.mindtouch.com/install/?modtime='.time().'"></iframe>';
	echo '</div>';
	
}

/**
 * Method called after installation completes successfully. Can generate markup.
 *
 * @param Object $conf
 * @return
 */
function finalize_product_installation($conf)
{
	$scheme = isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] == 'on' ? 'https' : 'http';

	$keys = generate_post_keys($conf);
	// add a partial-hash to verify against
	$keys['hash'] = substr(md5(implode(',', $keys)), 0, 5);
	// generate the query string
	$query = http_build_query($keys);
	
	// this'll help us see what % of users make it through installation
	echo '<div class="analytics" style="position:absolute; top:-500px; left:-500px; width: 1px; height: 1px;">';
		echo '<iframe width="1" height="1" src="'.$scheme.'://assets.mindtouch.com/install/?'. $query .'"></iframe>';
	echo '</div>';
}

/**
 * @param ConfigData $conf
 * @return array
 */
function generate_post_keys($conf)
{
	global $IP;
	require_once($IP . '/deki/core/objects/deki_site.php');
	
	// what type of install is it?
	$installType = $conf->getEdition() == 'core' ? 'Core' : 'Trial';
	
	/**
	 * Generate the array of post keys
	 */
	$keys = array(
		// note: keep important keys at the top
		'first_name' => $conf->RegistrarFirstName,
		'last_name' => $conf->RegistrarLastName,
		'email' => $conf->SysopEmail,
		'company' => $conf->Sitename,
		'phone' => $conf->RegistrarPhone,
		'lead_source' => 'install',
		'install_method' => DekiSite::getInstallType(),
		'install_type' => $installType,
		'version' => $conf->getProductVersion(),
		'country' => substr($conf->SiteLang, 3, 2),
		'os' => PHP_OS,
		'department' => $conf->RegistrarDept,
		'size' => $conf->RegistrarCount, 
		'usage' => $conf->RegistrarUsage,
		'edition' => $conf->getEditionLead()
	);
	
	return $keys;
}
