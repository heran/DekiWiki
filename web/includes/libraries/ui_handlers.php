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

class EtagCache
{
	const CACHE_PREFIX = 'cache-';

	// content types
	// todo: update the TEXT_XXXX constants to be TYPE_XXXX
	const TEXT_PLAIN = 'text/plain';
	const TEXT_CSS = 'text/css';
	const TYPE_JAVASCRIPT = 'application/javascript';

	private $cacheDirectory = null;
	// list of file paths to combine and cache
	private $cacheFiles = array();
	
	// keeps track of the file content types, only 1 type allowed
	private $contentType = self::TEXT_PLAIN;
	private $charset = 'utf-8';

	private $lastModified = null;
	// the destination cache file
	private $cacheFile = null;
	private $prepend = array();

	// keeps track of the files that are missing
	private $missingFiles = array();


	public function EtagCache($cacheDirectory, $checkEtag = false)
	{
		$this->cacheDirectory = $cacheDirectory;

		if ($checkEtag)
		{
			$this->checkEtag();
		}
	}
	
	public function setContentType($contentType, $charset)
	{
		$this->contentType = $contentType;
		$this->charset = $charset;
	}

	public function addFile($filepath)
	{
		$this->cacheFiles[] = $filepath;
		
		// invalidate these private variables
		$this->lastModified = null;
		$this->cacheFile = null;
	}
	
	/**
	 * Returns filesystem path to the cache file 
	 * 
	 * @return string
	 */
	public function getCacheFile()
	{
		if (is_null($this->cacheFile))
		{
			$this->cacheFile = $this->cacheDirectory . '/' . self::CACHE_PREFIX . $this->getEtag();
		}

		return $this->cacheFile;
	}
	
	/**
	 * Retrieves an array of all the requested cache files' contents
	 * Files contents are kept separate to allow the files to be preprocessed
	 * before caching them. e.g. css variable substitutions
	 *
	 * @return array
	 */
	public function &getFileContents()
	{
		$contents = array();

		foreach($this->cacheFiles as $file)
		{
			$content = file_get_contents($file);
			$content = $this->removeBOM($content);
			$contents[$file] = $content . PHP_EOL;

			//we'll prepend each file with the output of the sizes
			$this->prepend[$file]= number_format(strlen($content));
		}

		return $contents;
	}
		
	/**
	 * @param array &$contents reference to an array of file contents
	 */
	public function writeFileContents(&$contents)
	{
		if ($fp = @fopen($this->getCacheFile(), 'wb'))
		{
			fwrite($fp, $this->getPrepend());
			foreach ($contents as $file => &$content)
			{
				$prepend = is_string($file) ?
					PHP_EOL .'/* --------- '.strtoupper(basename($file)).' --------- */'. PHP_EOL :
					'';
				fwrite($fp, $prepend.$content);
			}
			unset($content);
			fclose($fp);
		}		
	}

	public function outputContents($contents = null)
	{
		if (!is_null($contents) && is_array($contents)) 
		{
			$contents = implode(PHP_EOL, $contents);	
		}
		if (file_exists($this->getCacheFile()))
		{
			$contents = file_get_contents($this->getCacheFile());
		}
	
		// begin output
		header(sprintf("Content-Type: %s; charset=%s", $this->contentType, $this->charset));

		header('Etag: ' . $this->getEtag());
		header("Last-Modified: " . gmdate("D, d M Y H:i:s", $this->getLastModified()) . " GMT");
		header('Connection: close');

		ob_start();
		// Begin GZip; this can be disabled by setting $wgDisabledGzip to true in LocalSettings.php
		$isGzip = $this->canGzip();
		if ($isGzip)
		{
			header('Content-Encoding: gzip');
			ob_start('ob_gzhandler');
		}
		
		// output any errors about missing files
		if (!empty($this->missingFiles))
		{
			foreach ($this->missingFiles as $file)
			{
				// assumes the output format supports block comments
				echo '/* ETagCache Error: Missing file: ' . basename($file) . ' */' . PHP_EOL;
			}
		}

		echo $contents;

		if ($isGzip)
		{
			ob_end_flush();  // The ob_gzhandler one
			header('Content-Length: ' . ob_get_length());
		}
		ob_end_flush();
	}

	// TODO: this only checks if the set has been cached and not if they have been modified
	//		 which leads to problems when the cache directory is not cleared & files updated
	public function isCached()
	{
		// check etag is here so that we don't do extra file system check
		// it could be moved to outputContents without adding much overhead
		$this->checkEtag();

		return file_exists($this->getCacheFile());
	}

	public function canGzip()
	{
		global $wgDisabledGzip;
		$disabled = isset($wgDisabledGzip) ? $wgDisabledGzip : false;

		return substr_count($_SERVER['HTTP_ACCEPT_ENCODING'], 'gzip') && ($disabled !== true);
	}

	private function getPrepend($return = '') 
	{
		if (empty($this->prepend))
		{
			return $return;
		}

		$prepend = array();
		foreach ($this->prepend as $file => $size) 
		{
			$prepend[] = basename($file).' ('.$size.')';
		}
		return '/* '."\n\t".implode(', '."\n\t", $prepend).PHP_EOL.' */'.PHP_EOL;
	}

	private function checkEtag()
	{
		//if etag is sent, see if that file exists and return a 304
		$requestEtag = isset($_SERVER['HTTP_IF_NONE_MATCH']) ? $_SERVER['HTTP_IF_NONE_MATCH'] : null;
		// make sure the request's etag is the same as the internal etag, contents may have changed
		if (!is_null($requestEtag) && ($requestEtag == $this->getEtag()) )
		{		
			global $wgProductVersion;
			@list(,$etagVersion,) = explode('-', $requestEtag, 3);
			// make sure the etag is for the current deki version
			if ( ($wgProductVersion == $etagVersion) &&
				 is_file($this->cacheDirectory . '/' . self::CACHE_PREFIX . $_SERVER['HTTP_IF_NONE_MATCH']) )
			{
				header("HTTP/1.0 304 Not Modified");
				header('Content-Length: 0');
				exit();
			}
		}
	}
	
	public function getEtag()
	{
		global $wgProductVersion;
		return $this->getLastModified() .'-'. $wgProductVersion .'-'. md5(serialize($this->cacheFiles));
	}

	private function getLastModified()
	{
		if (is_null($this->lastModified))
		{
			$this->lastModified = 0;

			foreach ($this->cacheFiles as $key => $file)
			{
				if (!is_file($file))
				{
					// keep track of the missing files
					$this->missingFiles[] = $file;
					
					unset($this->cacheFiles[$key]);
					continue;
				}

				$this->lastModified = max($this->lastModified, filemtime($file));
			}
		}

		return $this->lastModified;
	}

	private function removeBOM($str="")
	{
		if(substr($str, 0,3) == pack("CCC", 0xef, 0xbb, 0xbf))
		{
			$str=substr($str, 3);
		}
		return $str;
	}
}

class CssHandler
{
	const TEMPLATE_DIR = 1;
	const SKIN_DIR = 2;

	private $Cache = null;
	private $skinDirectory = null;
	private $templateDirectory = null;


	public function CssHandler($cssFilepath)
	{
		global $wgCacheDirectory;

		$this->Cache = new EtagCache($wgCacheDirectory);
		$this->Cache->setContentType(EtagCache::TEXT_CSS, 'iso-8859-1');

		$this->skinDirectory = dirname($cssFilepath);
		$this->templateDirectory = dirname($this->skinDirectory);
	}

	// @note used for caching plugin css files
	public function addFile($jsFilepath)
	{
		$this->Cache->addFile($jsFilepath);
	}
	public function addSkin($cssFilename)		{ $this->Cache->addFile($this->skinDirectory . '/' . $cssFilename); }
	public function addTemplate($cssFilename)	{ $this->Cache->addFile($this->templateDirectory . '/' . $cssFilename); }
	
	public function getCache()	{ return $this->Cache; }
	public function getSkinDirectory() { return $this->skinDirectory; }
	public function getTemplateDirectory() { return $this->templateDirectory; }

	/**
	 * Generate the CSS files
	 * options:
	 *      - template (template name, i.e. "deuce")
	 *      - skin (skin name, i.e. "lighty-blue")
	 *      - folder (path to copy generated file to (so image references, etc. can be referenced correctly)
	 * @param array $options - settings to use (for CDN, if any)
	 */
	public function process($options = null)
	{
		global $wgCDN, $IP;

		$contents = null;
		
		// first load hits the filesystem twice, but subsequent loads only hit it once
		if (!$this->Cache->isCached())
		{
			$contents = $this->Cache->getFileContents();
			
			foreach ($contents as &$content)
			{
				// perform css variable substitutions
				$this->replaceCssVariables($content);

				if (!$this->Cache->canGzip())
				{
					// only compress the css if gzipping is disabled
					$this->compressCss($content);
				}
			}
			unset($content);
			
			$this->Cache->writeFileContents($contents);
		}
		
		// Responsibility of caller to load DekiPlugin so hook can fire
		if (class_exists('DekiPlugin'))
		{
			DekiPlugin::executeHook(Hooks::UI_PROCESS_CSS, array($this, $options));
		}
		
		$this->Cache->outputContents($contents);
	}

	private function compressCss(&$buffer)
	{
		$buffer = preg_replace('!/\*[^*]*\*+([^/][^*]*\*+)*/!', '', $buffer);
		$buffer = str_replace(array("\r\n", "\r", "\n", "\t", '  '), '', $buffer);
		$buffer = str_replace('{ ', '{', $buffer);
		$buffer = str_replace(' }', '}', $buffer);
		$buffer = str_replace('; ', ';', $buffer);
		$buffer = str_replace(', ', ',', $buffer);
		$buffer = str_replace(' {', '{', $buffer);
		$buffer = str_replace('} ', '}', $buffer);
		$buffer = str_replace(': ', ':', $buffer);
		$buffer = str_replace(' ,', ',', $buffer);
		$buffer = str_replace(' ;', ';', $buffer);
	}

	/**
	 * Implements the css variables spec, but does not support multiple variable blocks
	 * for different css mediums. e.g. print, screen, etc
	 *
	 * @see http://disruptive-innovations.com/zoo/cssvariables/
	 *
	 * @param string &$buffer reference to the css document contents
	 */
	private function replaceCssVariables(&$buffer)
	{
		// find the css variables section
		$pattern = '/\s*@variables\s*\{\s*([^\}]*)\s*\}/';
		$matches = array();
		// allow multiple variable blocks
		$hasVariables = preg_match_all($pattern, $buffer, $matches);

		// only look for variable replacements if they are defined
		if ($hasVariables > 0)
		{
			// remove the variables definition to avoid any css parse errors
			$buffer = preg_replace($pattern, ' ', $buffer);

			$substitutions = array();
			foreach ($matches[1] as $match)
			{
				$definitions = explode(';', $match);
				foreach ($definitions as $definition)
				{
					$definition = trim($definition);
					if (!empty($definition))
					{
						list($name, $value) = explode(':', $definition, 2);
						$substitutions['var(' . $name . ')'] = $value;
					}
				}
			}

			// perform the replacements, str_ireplace for case insensitive
			$buffer = str_replace(array_keys($substitutions), array_values($substitutions), $buffer);
		}
	}
}



class JsHandler
{
	private $Cache = null;
	private $permissions = array();
	private $commonDirectory = null;
	private $yuiDirectory = null;
	private $editorDirectory = null;
	// a string of javascript to prepend to the output
	private $inlineJavascript = null;
	private $appendJavascript = null;


	public function JsHandler()
	{
		global $wgCacheDirectory, $wgStyleDirectory;
		global $IP, $wgEditors, $wgDefaultEditor, $wgEditor;
		
		$wgEditor = ( isset($wgEditor) && isset($wgEditors[$wgEditor]) ) ? $wgEditor : $wgDefaultEditor;

		$this->Cache = new EtagCache($wgCacheDirectory, true); // will exit here if etag cache exists
		$this->Cache->setContentType(EtagCache::TYPE_JAVASCRIPT, 'iso-8859-1');

		// The permission flags passed determine what JS files are called
		$this->permissions = $this->getPermissionFlags();

		$this->commonDirectory = $wgStyleDirectory . '/common';
		$this->yuiDirectory = $this->commonDirectory . '/yui';

		$editorPath = '/editor' . $wgEditors[$wgEditor]['directory'];
		$this->editorDirectory = $IP . $editorPath;
	}

	// @note used for caching plugin javascript files
	public function addFile($jsFilepath)
	{
		$this->Cache->addFile($jsFilepath);
	}

	public function addCommon($jsFilename)
	{
		$this->Cache->addFile($this->commonDirectory .'/'. $jsFilename);
	}

	public function addYui($yuiLibrary, $jsFilename = null)
	{
		if (is_null($jsFilename))
		{
			$jsFilename = $yuiLibrary . '.js';
		}
		$this->Cache->addFile($this->yuiDirectory .'/'. $yuiLibrary .'/'. $jsFilename);
	}

	public function addEditor($jsFilename = null)
	{
		$this->Cache->addFile($this->editorDirectory .'/'. $jsFilename);
	}
	
	public function addInline($javascript)
	{
		$this->inlineJavascript = $javascript;
	}
	
	public function appendInline($javascript)
	{
		$this->appendJavascript = $javascript;
	}

	public function canAdmin()	{ return $this->can('ADMIN'); }
	public function canUpdate() { return $this->can('UPDATE'); }
	public function canSubscribe() { return $this->can('SUBSCRIBE'); }
	public function can($perm)	{ return in_array(strtoupper($perm), $this->permissions); }


	public function process()
	{
		
		$contents = null;
		
		// first load hits the filesystem twice, but subsequent loads only hit it once
		if (!$this->Cache->isCached())
		{
			$contents = $this->Cache->getFileContents();
			
			if (!is_null($this->inlineJavascript))
			{
				// add the inline contents to the front
				array_unshift($contents, $this->inlineJavascript);
			}
			
			if (!is_null($this->appendJavascript))
			{
				// add the inline contents to the end
				$contents[] = $this->appendJavascript;
			}

			// compress the javascript if no gzip?

			$this->Cache->writeFileContents($contents);
		}

		$this->Cache->outputContents($contents);
	}


	private function getPermissionFlags()
	{
		global $wgDefaultPermFlags;
		if (isset($_GET['perms'])) {
			$perms = explode(',', $_GET['perms']);
			foreach ($perms as $k => $v) {
				$perms[$k] = trim(strip_tags(strtoupper($v)));
			}
		}
		else {
			$perms = array();
		}
		return $perms;
	}
}


/**
 * Allows caching remote data locally, specifically used for
 * caching data from the API
 */
class RemoteCacheHandler
{
	private $Cache = null;
	
	private $cachePrefix = '';
	private $cacheDirectory = null;
	

	public function __construct($contentType, $charset = 'utf-8')
	{
		global $wgCacheDirectory, $wgDekiSiteId;
		// required to cache the remote resources locally
		$this->cacheDirectory = $wgCacheDirectory;
		$this->cachePrefix = EtagCache::CACHE_PREFIX . $wgDekiSiteId .'.';
		
		// required to aggregate all the resources into one request
		$this->Cache = new EtagCache($wgCacheDirectory);
		$this->Cache->setContentType($contentType, $charset);
	}
	
	public function getCache()	{ return $this->Cache; }

	public function addFile($filepath) { $this->Cache->addFile($filepath); }
	
	public function addResouce($uri, $etag = null)
	{
		// todo: if the etag is not sent in then a head request should be made on the resource
		// make sure the cache file exists
		$file = $this->cacheRemoteResource($uri, $etag);
		// add to the etag cache
		$this->Cache->addFile($file);
	}
	
	public function process()
	{	
		$contents = null;
		
		if (!$this->Cache->isCached())
		{
			$contents = $this->Cache->getFileContents();
			$this->Cache->writeFileContents($contents);
		}

		$this->Cache->outputContents($contents);
	}

	
	protected function getCacheFile($uri, $etag)
	{
		global $wgProductVersion;
		
		return $this->cacheDirectory . '/' . 
			$this->cachePrefix . 
			$wgProductVersion .
			// etag portion of the filename
			'-' . md5($uri . $etag);
	}
	
	/**
	 * Writes the remote resource to file if it isn't cached locally
	 */
	protected function cacheRemoteResource($uri, $etag)
	{
		$cacheFile = $this->getCacheFile($uri, $etag);
		// since the locally cached resources are based on the etag, we know
		// the file contents have not been updated if the file exists
		if (!file_exists($cacheFile))
		{
			if ($fp = @fopen($cacheFile, 'wb'))
			{
				$Plug = DekiPlug::NewPlug($uri);
				$Result = $Plug->Get();
	
				if ($Result->isSuccess())
				{
					fwrite($fp, $Result->getVal('body'));
				}
				fclose($fp);
			}
		}
		
		return $cacheFile;
	}
}
