<?php
/*
 * MindTouch Core - open source enterprise collaborative networking
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

class DekiInstaller extends DekiInstallerEnvironment
{
	// stores the key used to pass data via the PHP session to the complete controller
	const COMPLETE_KEY = 'MT.Install.Complete';
	
	protected $db = array(
		'mysql' => array(
			'fullname' => 'MySQL', 
			'havedriver' => 0, 
			'compile' => 'mysql', 
			'rootuser' => 'root'
		)
	);

	protected function initialize()
	{
		// initialize the installer environment
		$this->setupEnvironment();
		
		// needed to determine configuration step visibility
		$this->setupView($this->View);	
		$this->setupLocalization();

		// only check dependencies for the install controller => do not process dependecies when post data is found
		if (($this->name == 'install') && strcmp($_SERVER["REQUEST_METHOD"], 'POST') != 0)
		{
			$errors = $this->checkDependencies();
			if (!empty($errors))
			{
				$this->View->set('dependency.messages', $errors);
			}
		}
		
		$this->setupConfiguration();
		
		// follow the normal controller code path
		parent::initialize();
	}

	protected function setupLocalization()
	{
		global $wgAllMessagesEn, $wgJavascriptMessages;
		global $wgLanguageCode, $wgCacheDirectory, $wgResourcesDirectory;
		global $wgAllMessagesEn, $wgJavascriptMessages;
		global $wgResourceLanguageNames, $wgResourcesDirectory;
		
		global $IP, $wgConfiguring;
		
		//define('MINDTOUCH_DEKI', true);
		//define("MEDIAWIKI_INSTALL", true);
		
		// Attempt to set up the include path, to fix problems with relative includes
		//$IP = dirname( dirname( __FILE__ ) );
		define('MW_INSTALL_PATH', $IP);
		
		$sep = PATH_SEPARATOR;
		$dsep = DIRECTORY_SEPARATOR;
		if( !ini_set( "include_path", ".".$sep.$IP.$sep.$IP.$dsep."includes".$sep.$IP.$dsep."deki".$sep.$IP.$dsep."languages" ) ) {
			set_include_path( ".".$sep.$IP.$sep.$IP.$dsep."includes".$sep.$IP.$dsep."languages" );
		}
		
		$wgConfiguring = true;
		
		require_once('includes/Defines.php');
		include('includes/DefaultSettings.php');
		require_once('core/deki_namespace.php');
		require_once('includes/GlobalFunctions.php');
		//require_once( "includes/Hooks.php" );
		
		// Localization 
		require_once('languages/Language.php');
	}
	
	/**
	 * Generate warning & error messages
	 * @return
	 */
	protected function checkDependencies()
	{
		$errors = array();
		
		/***
		 * DEFINE PHP SETTINGS THAT CAUSE THE INSTALLER TO QUIT
		 * MT royk: these are remnants from MW, but I'm sure they're equally applicable to DW
		 * Key is the PHP settings, value is the URL which explains them (used in localization string)
		 */
		$fatal_settings_if_enabled = array( 
			'magic_quotes_runtime' => 'http://www.php.net/manual/en/ref.info.php#ini.magic-quotes-runtime', 
			'magic_quotes_sybase' => 'http://www.php.net/manual/en/ref.sybase.php#ini.magic-quotes-sybase', 
			'mbstring.func_overload' => 'http://www.php.net/manual/en/ref.mbstring.php#mbstring.overload', 
			'zend.ze1_compatibility_mode' => 'http://www.php.net/manual/en/ini.core.php'
		);
		
		/***
		 * DEFINE PHP FUNCTIONS, IF MISSING, CAUSE THE INSTALLER TO QUIT
		 * Key is the PHP function, value is the localization string key
		 */
		$fatal_functions_if_disabled = array( 
			'utf8_encode' => 'Page.Install.check-xml-fail', 
			'mb_strtoupper' => 'Page.Install.check-mb-fail', 
			'session_name' => 'Page.Install.check-session-fail', 
			'preg_match' => 'Page.Install.check-preg-fail', 
			'mysql_connect' => 'Page.Install.check-mysql-fail', 
			'curl_init' => 'Page.Install.check-curl-fail',
			'gd_info' => 'Page.Install.check-gd-fail', 
		);
		
		/***
		 * DEFINE PHP SETTINGS, IF ENABLED, CAUSE A VISUAL WARNING
		 * Key is the PHP settings, value is the URL which explains them (used in localization string)
		 */
		$warn_settings = array(
			'register_globals' => 'http://php.net/register_globals', 
			'safe_mode' => 'http://www.php.net/features.safe-mode'
		);
		
		/***
		 * DEFINE APACHE MODULES, IF MISSING, CAUSE INSTALLER TO QUIT
		 */
		$fatal_apache_modules_if_disabled = array( 
			'mod_rewrite', 
			'mod_proxy'
		);
		
		/* Check for existing configurations and bug out! */
		if ($this->isInstalled())
		{
			$this->Request->redirect($this->Request->getLocalUrl('installed'));
			return $this->fatalError(wfMsg('Page.Install.setup-complete'));
		}
		
		/* Verify this folder is writable */
		if (!is_writable('.')) 
		{
			// @TODO: fix this message so it doesnt require hardcoded HTML
			return $this->fatalError(
			"<li class=\"fatal\">
			<h2>Can't write config file, aborting</h2>
		
			<p>In order to configure the wiki you have to make the <tt>config</tt> subdirectory
			writable by the web server. Once configuration is done you'll move the created
			<tt>LocalSettings.php</tt> to the parent directory, and for added safety you can
			then remove the <tt>config</tt> subdirectory entirely.</p>
		
			<p>To make the directory writable on a Unix/Linux system:</p>
		
			<pre>
			cd <i>/path/to/wiki</i>
			chmod a+w config
			</pre>
			
			<p>After fixing this, please reload this page.</p>
			</li>"
			, true);
		}
		
		error_reporting(0);

		// mySQL is the only supported database		
		// FAIL for missing settings/packages/libs
		ob_start();
		if (!install_verify_databases($this->db))
		{
			// @TODO: this does not generate a message
			$message = ob_get_contents();
			$fatal = true;
			$errors[] = $this->error($message, 'fatal', true);
		}
		ob_end_clean();
		ob_start();
		if (!install_apps_version_check())
		{
			$message = ob_get_contents();
			$fatal = true;
			$errors[] = $this->error($message, 'fatal', true);
		}
		ob_end_clean();
		ob_start();
		if (!install_verify_php_functions($fatal_functions_if_disabled, $fatal_settings_if_enabled))
		{
			$message = ob_get_contents();
			$fatal = true;
			$errors[] = $this->error($message, 'fatal', true);		
		}
		ob_end_clean();
		ob_start();
		if (!install_verify_apache_modules($fatal_apache_modules_if_disabled)) 
		{
			$message = ob_get_contents();
			$fatal = true;
			$errors[] = $this->error($message, 'fatal', true);
		}
		ob_end_clean();
		// allow all of the above errors to be produced at once to ease remedy
		if ($fatal)
		{
			return $errors;
		}
		
		error_reporting(E_ALL);
		
		// WARN for certain PHP settings
		ob_start();
		install_warn_php_settings($warn_settings);
		$message = ob_get_contents();
		if (!empty($message))
		{
			$errors[] = $this->error($message, 'warn', true);
		}
		ob_end_clean();
		
		// WARN for missing packages
		$pathWarnings = $this->autoDetectDependencyPaths();
		if (!empty($pathWarnings))
		{
			$errors = array_merge($errors, $pathWarnings);
		}
		
		// remnants from mediawiki: do we need this?
		ob_start();
		install_session_path();
		$message = ob_get_contents();
		if (!empty($message))
		{
			$errors[] = $this->error($message, 'warn', true);
		}
		ob_end_clean();
		
		// remnants from mediawiki: do we need to do this?
		ob_start();
		install_raise_memory_limit();
		$message = ob_get_contents();
		if (!empty($message))
		{
			$errors[] = $this->error($message, 'warn', true);
		}
		ob_end_clean();

		// @TODO: html2ps and ps2pdf $conf setting
		// @TODO: handle this better
		
		return $errors;
	}
	
	/**
	 * Initialize the configuration object and set defaults
	 * @return
	 */
	protected function setupConfiguration()
	{
		global $wgPathIdentify, $wgPathConvert, $wgPathHTML2PS, $wgPathPS2PDF, $wgPathMono, $wgPathConf;
		global $wgAttachPath, $wgLucenePath, $wgPathPrince;
		global $IP, $conf;
		global $wgDBadminuser, $wgDBadminpassword;
		
		$conf = new ConfigData();
		
		// set configuration
		$conf->IP = $IP;
		
		// PHP_SELF isn't available sometimes, such as when PHP is CGI but
		// cgi.fix_pathinfo is disabled. In that case, fall back to SCRIPT_NAME
		// to get the path to the current script... hopefully it's reliable. SIGH
		$conf->ScriptPath = preg_replace( '{^(.*)/config.*$}', '$1', ($_SERVER["PHP_SELF"] === '') ? $_SERVER["SCRIPT_NAME"]: $_SERVER["PHP_SELF"] );
		$conf->posted = ($_SERVER["REQUEST_METHOD"] == "POST");
		$conf->Sitename = ucfirst(importPost('Sitename', 'Site Name'));
		$conf->SiteLang = importPost( "SiteLang", 'en-us');
		$conf->EmergencyContact = importPost( "EmergencyContact", '' );
		$conf->SelectedEdition = importPost('SelectedEdition', '');
		
		// bug 7876; if they don't provide the server email, use the administrator's email
		if (empty($conf->EmergencyContact)) {
			$conf->EmergencyContact = importPost( "SysopEmail", ''); 
		}
		
		// no other dbtypes are supported
		$conf->DBtype = 'mysql';
		$conf->ApiKey = generateKey(32);
		$conf->Guid = md5($conf->ApiKey);
		$conf->PathPrefix = "@api";
		$conf->IpAddress = "localhost";
		$conf->HttpPort = "8081";
		$conf->LuceneStore =  $conf->getLucenePath();
		// default to 127.0.0.1, rather than localhost, to fix a PHP 5.3/Windows Vista bug
		$conf->DBserver = importPost( "DBserver", "127.0.0.1" );
		$conf->DBname = importPost( "DBname", "wikidb" );
		$conf->DBuser = importPost( "DBuser", "wikiuser" );
		$conf->DBpassword = importPost( "DBpassword" );
		
		// Do not allow the sysop name to be overridden
		$conf->SysopName = 'Admin';
		$conf->SysopEmail = importPost( "SysopEmail", '' );
		$conf->SysopPass = importPost( "SysopPass" );
		$conf->SysopPass2 = importPost( "SysopPass2" );
		
		// database user and password
		$conf->RootUser = importPost('RootUser', $conf->getDatabaseRootUser());
		$conf->RootPW = importPost('RootPW', $conf->getDatabaseRootPassword());
		$conf->ImageMagickConvert = importPost("ImageMagickConvert", $conf->getImageMagickConvert());
		$conf->ImageMagickIdentify = importPost("ImageMagickIdentify", $conf->getImageMagickIdentify());
		$conf->Mono = importPost("Mono", $conf->getMonoPath());
		$conf->prince = importPost( "prince", $conf->getPrincePath());
		$conf->RegistrarFirstName = importPost( "RegistrarFirstName" );
		$conf->RegistrarLastName = importPost( "RegistrarLastName" );
		$conf->RegistrarPhone = importPost( "RegistrarPhone" );
		$conf->RegistrarCountry = importPost( "RegistrarCountry" );
		$conf->RegistrarCount = importPost( "RegistrarCount" );
		$conf->RegistrarDept = importPost( "RegistrarDept" );
		$conf->RegistrarUsage = isset($_POST['RegistrarUsage']) ? $_POST['RegistrarUsage'] : ''; 
		
		if (is_array($conf->RegistrarUsage)) {
			$conf->RegistrarUsage = implode(', ', array_keys($conf->RegistrarUsage));	
		}
	}
	
	protected function validateConfiguration()
	{
		global $conf;
		
		/* Check for validity */
		$errs = array();
		
		//autogenerate database key
		$conf->DBpassword = generateKey(16);
		
		if ($conf->Sitename == "")
		{
			$errs[] = $this->inputError('Sitename', wfMsg('Page.Install.error-blank-sitename'));
		}
		if (strlen($conf->Sitename) > 64)
		{
			$errs[] = $this->inputError('Sitename', wfMsg('Page.Install.error-sitename-exceeds-max-length'));
		}
		if ($conf->DBuser == "")
		{
			$errs[] = $this->inputError('DBuser', wfMsg('Page.Install.error-blank-db-username'));
		}
		
		// Sysop account must be Admin
		$conf->SysopName = 'Admin';
		
		if ($conf->SysopEmail == "")
		{
			$errs[] = $this->inputError('SysopEmail', wfMsg('Page.Install.error-blank-useremail'));
		}
		// @TODO: database type checks should be removed
		if( ($conf->DBtype == 'mysql') && (strlen($conf->DBuser) > 16) )
		{
			$errs[] = $this->inputError('DBuser', wfMsg('Page.Install.error-db-usernamelong'));
		}
		if ($conf->DBtype != 'mysql')
		{
			$errs[] = $this->inputError('DBtype', wfMsg('Page.Install.error-db-support'));
		}
		
		if (!$conf->isCore())
		{
			if ($conf->RegistrarFirstName == '')
			{
				$errs[] = $this->inputError('RegistrarFirstName', wfMsg('Page.Install.error-noname'));
			}
			if ($conf->RegistrarLastName == '')
			{
				$errs[] = $this->inputError('RegistrarLastName', 'You did not input your last name');
			}
			if ($conf->RegistrarPhone == '')
			{
				$errs[] = $this->inputError('RegistrarPhone', wfMsg('Page.Install.error-nophone'));
			}
		}

		// validate administrative settings
		if ($conf->SysopPass != '')
		{
			if ($conf->SysopPass != $conf->SysopPass2)
			{
				$errs[] = $this->inputError('SysopPass2', wfMsg('Page.Install.error-password-match'));
			}
		}
		else
		{
			$errs[] = $this->inputError('SysopPass', wfMsg('Page.Install.error-blank-password'));
			$errs[] = $this->inputError('SysopPass2', wfMsg('Page.Install.error-password-match'));
		}
		
		// organization size comes later in the install process
		if (!$conf->isCore())
		{
			if ($conf->RegistrarCount == '0')
			{
				$errs[] = $this->inputError('RegistrarCount', wfMsg('Page.Install.error-registrarcount'));
			}
		}
		
		if (!$this->isWindows() && ($conf->Mono == ''))
		{
			$errs[] = $this->inputError('Mono', 'Mono path is required.');
		}

		if ($conf->getImageMagickConvert() == '')
		{
			$errs[] = $this->inputError('ImageMagickConvert', 'ImageMagick "convert" path is required.');
		}

		if ($conf->getImageMagickIdentify() == '')
		{
			$errs[] = $this->inputError('ImageMagickIdentify', 'ImageMagick "identify" path is required.');
		}
		
		// @TODO: move error reporting level changes to self::initialize()
		error_reporting(E_ALL);
		
		return $errs;
	}

	// Generate a settings file for the mindtouch.host.service.exe
	protected function install_mindtouch_win_service_generate($conf)
	{
		// read in mindtouch.host.service.exe.in
		$f = fopen(FILE_HOST_WIN_SERVICE_CONF_IN, "r");
		$contents = fread($f, filesize(FILE_HOST_WIN_SERVICE_CONF_IN));
		fclose($f);
		
		// substitute the variables
		$variables = array(
			'%SCRIPT%' => $conf->IP . '/conf/' . FILE_STARTUP_XML, 
			'%APIKEY%' => $conf->ApiKey, 
			'\\' => '/',
			'htdocs/' => '',
		);
		
		$find = array();
		$replace = array();
		foreach ($variables as $f => $r) 
		{
			$find[] = $f;
			$replace[] = $r;
		}
		
		//faster than individual preg_replace
		$contents = str_replace($find, $replace, $contents);
		return $contents;
	}

	
	/**
	 * Run the product installation
	 * 
	 * @param array $messages - returns the notification messages generated by the installation process
	 * @return bool - true if the installation was successful
	 */
	protected function installMindTouch(&$messages)
	{
		global $IP, $conf, $wgCommandLineMode, $wgRequest;
		
		$messages = array();
		$errors = array();
				
		$conf->Root = ($conf->RootPW != "");

		//generate the PHP and XML for settings that are generated
		$localsettings = install_localsettings_generate( $conf );
		$startupxml = install_mindtouch_xml_generate( $conf );

		$wgCommandLineMode = false;
		
		// since we have a magical $IP definition in LS.php, need to save current IP
		$InstallerIP = $IP;
		
		// verify LocalSettings.php is OK
		chdir( ".." );
		$oklocal = eval( $localsettings );
		if( $oklocal === false ) 
		{
			return $this->fatalError(wfMsg('Page.Install.error-fatal-bug', $localsettings));
		}
		
		// restore the installer's include path
		$IP = $InstallerIP;
		
		$conf->DBtypename = '';
		foreach (array_keys($this->db) as $db) 
		{
			if ($conf->DBtype === $db)
				$conf->DBtypename = $this->db[$db]['fullname'];
		}
		if (!strlen($conf->DBtype)) 
		{
			//$errs["DBpicktype"] = wfMsg('Page.Install.error-db-type');
			return $this->fatalError(wfMsg('Page.Install.error-db-type'));
		}

		if (!$conf->DBtypename) 
		{
			//$errs["DBtype"] = wfMsg('page.Install.error-db-unknown', $conf->DBtype);
			return $this->fatalError(wfMsg('page.Install.error-db-unknown', $conf->DBtype));
		}
		
		//initialize Setup.php
		$wgDBtype = $conf->DBtype;
		$wgCommandLineMode = true;
		
		//require_once($IP . '/includes/Defines.php');
		include($IP . '/includes/DefaultSettings.php');
		//require_once($IP . '/deki/core/deki_namespace.php');
		//require_once($IP . '/includes/GlobalFunctions.php');
		
		require_once('includes/Setup.php');
		chdir( "config" );

		//do the database install
		error_reporting(E_ALL);
		
		// capture any output
		ob_start();
		$dbInstallSuccess = install_database($conf);
		$dbMessages = ob_get_contents();
		ob_end_clean();
		if (!empty($dbMessages))
		{
			if (!$dbInstallSuccess)
			{
				// messages are errors
				return $this->fatalError($dbMessages, true);
			}
			else 
			{
				$messages[] = $this->message($dbMessages, true);
			}
		}
		
		//if database install went well, continue onwards
		
		//Write the settings we have for LocalSettings.php to disk
		ob_start();
		$success = install_write_settings_file(FILE_LOCALSETTINGS, install_php_wrapper($localsettings));
		$message = ob_get_contents();
		ob_end_clean();
		if ($success)
		{
			$messages[] = $this->message($message, true);
		}
		else
		{
			$errors[] = $this->error($message, 'error', true);
		}

		//Write the settings we have for mindtouch.deki.startup.xml to disk
		ob_start();
		$success = install_write_settings_file(FILE_STARTUP_XML, install_mindtouch_xml_generate($conf));
		$message = ob_get_contents();
		ob_end_clean();
		if ($success)
		{
			$messages[] = $this->message($message, true);
		}
		else
		{
			$errors[] = $this->error($message, 'error', true);
		}

		//Write the settings we have for mindtouch.host.conf (or mindtouch.host.bat) to disk
		ob_start();
		$confOutputFile = $this->isWindows() ? FILE_HOST_WIN_BAT : FILE_HOST_XML;
		$success = install_write_settings_file($confOutputFile, install_mindtouch_conf_generate($conf));
		$message = ob_get_contents();
		ob_end_clean();
		if ($success)
		{
			$messages[] = $this->message($message, true);
		}
		else
		{
			$errors[] = $this->error($message, 'error', true);
		}
		
		// if a WAMP install, write out mindtouch.dream.startup.xml
		if ($this->isWAMP())
		{
			ob_start();
			$success = install_write_settings_file(FILE_HOST_WIN_SERVICE_CONF, $this->install_mindtouch_win_service_generate($conf));
			$message = ob_get_contents();
			ob_end_clean();
			if ($success)
			{
				$messages[] = $this->message($message, true);
			}
			else
			{
				$errors[] = $this->error($message, 'error', true);
			}
		}
		/** 
		 * All installation types require a license via email
		 */
		$messages[] = $this->message(wfMsg('Page.Install.license.retrieve'));
		if (!$this->generateLicense($conf))
		{
			$messages[] = $this->error(wfMsg('Page.Install.license.failed'));
		}
		
		// magical work for VM since we know the server environment
		// todo: consolidate this with the output for other OSes
		if ($this->isVM()) 
		{
			// copy the appropriate files and start dekihost
			rename(FILE_LOCALSETTINGS, $IP.'/'.FILE_LOCALSETTINGS);
			rename(FILE_STARTUP_XML, '/etc/dekiwiki/'.FILE_STARTUP_XML);
			rename($confOutputFile, '/etc/dekiwiki/'.$confOutputFile);
			$messages[] = $this->message(wfMsg('Page.Install.dekihost.start'));
			
			//flush();
			exec("sudo /etc/init.d/dekiwiki restart > /dev/null 2>&1");
			sleep(2); //let dekihost start up
			$messages[] = $this->message(wfMsg('Page.Install.dekihost.start'));
			$messages[] = $this->message(wfMsg('Page.Install.completed'));
			//flush();
		}
		else if ($this->isMSI()) 
		{
			global $wgPathConf;
			// the MSI will work by giving write access to specific conf files, and targeting the files for writing
			wfSetFileContent($IP.'/'.FILE_LOCALSETTINGS, wfGetFileContent(FILE_LOCALSETTINGS));
			wfSetFileContent($wgPathConf.'/'.FILE_STARTUP_XML, wfGetFileContent(FILE_STARTUP_XML));
		}
		else if ($this->isWAMP())
		{
			// copy the appropriate files and start dekihost
			rename(FILE_LOCALSETTINGS, $IP.'/'.FILE_LOCALSETTINGS);
			rename(FILE_STARTUP_XML, $IP.'/../conf/'.FILE_STARTUP_XML);
			rename($confOutputFile, $IP.'/../bin/'.$confOutputFile);
			rename(FILE_HOST_WIN_SERVICE_CONF, $IP.'/../bin/'.FILE_HOST_WIN_SERVICE_CONF);
			$messages[] = $this->message(wfMsg('Page.Install.dekihost.start'));
			exec("sc config \"MindTouch Dream\" start= auto");
			exec("net start \"MindTouch Dream\"");
			sleep(2); //let dekihost start up
			$messages[] = $this->message(wfMsg('Page.Install.dekihost.start'));
			$messages[] = $this->message(wfMsg('Page.Install.completed'));
		}
		
		
		if (!empty($errors))
		{
			return $errors;
		}
	}
	
	/**
	 * Attempts to generate the license file
	 * @param ConfigData $conf
	 * @return string
	 */
	protected function generateLicense(&$conf)
	{
		// @TODO: use DreamPlug
		$Plug = new Plug(DekiMvcConfig::LICENSE_GENERATOR_URL, null);
		$response = $Plug
			->With('name', $conf->RegistrarFirstName . ' ' . $conf->RegistrarLastName)
			->With('email', $conf->SysopEmail)
			->With('phone', $conf->RegistrarPhone)
			->With('productkey', $conf->getProductKey())
			->With('product', $conf->getProductType())
			->With('version', $conf->getProductVersion())
			->With('format', 'json')
			->With('from', 'installer')
			->Post();
		
		$generated = $response['status'] == 200;
		if (DekiMvcConfig::DEBUG)
		{
			$result = @json_decode($response['body']);
			print_r($result);
			
			// die here to retry easily
			exit();
		}
		
		// custom config field for licensing
		$conf->LicenseGenerated = $generated;
		return $generated;
	}
}
