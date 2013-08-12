#!/usr/bin/php5
<?php
/**
 * @title Convert Resource Keys
 * @author Guerric Sloan
 * @date October 16, 2007
 * 
 * @note Updates old flat key format into new namespaces key format
 *
 */
require('includes/ShellScript.php');

define('PRESERVE_ENGLISH', false);
define('BLANK_NEW_KEYS', true);

class ConvertResources extends ShellScript
{
	protected $title = 'Convert Resource Keys';

	protected $prompts = array('What resource file do you want to convert? ' => 'resourceFile',
							   //'What conversion file do you want to use? ' => 'conversionFile',
							   'What directory do you want to save the files to? ' => 'outputDirectory'
							   );
	
	public function run()
	{
		parent::run();
		// contains the key conversion information
		$this->vars['conversionFile'] = 'includes/converter.txt';

		if (!is_file($this->vars['resourceFile']))
			$this->fatal('Could not find the resource file to convert, ' . $this->vars['resourceFile']);
		if (!is_file($this->vars['conversionFile']))
			$this->fatal('Could not find the conversion file, ' . $this->vars['conversionFile']);
		if (!is_dir($this->vars['outputDirectory']))
			$this->fatal('Output directory does not exist or is not a directory, ' . $this->vars['outputDirectory']);

		// load the conversion file
		$fileContents = file($this->vars['conversionFile']);

		// stores the key relationships
		$remap = array();
		// create the new resources array filled with the default english values
		$resources = array();

		foreach ($fileContents as $line)
		{
			list($newKey, $convert) = explode('=>', $line, 2);
			list($oldKey, $value) = explode('=', $convert, 2);

			$newKey = trim($newKey);
			$oldKey = trim($oldKey);
			$value = trim($value);
			
			if (isset($remap[$oldKey]))
			{
				if (!is_array($remap[$oldKey]))
				{
					$currentValue = $remap[$oldKey];
					$remap[$oldKey] = array();
					$remap[$oldKey][] = $currentValue;
				}

				$remap[$oldKey][] = $newKey;
			}
			else
			{
				$remap[$oldKey] = $newKey;
			}

			$keyPosition = strrpos($newKey, '.');
			$namespace = substr($newKey, 0, $keyPosition);
			$newKey = substr($newKey, $keyPosition+1);

			if (!isset($resources[$namespace]))
			{
				$resources[$namespace] = array();
			}

			$resources[$namespace][$newKey] = trim($value);
		}
		
		// load the resource file to convert
		$fileContents = file($this->vars['resourceFile']);

		$unassigned = array();
		// stores which keys are touched by the language file
		$touchedKeys = array();
		$lineNumber = 0;

		if (isset($fileContents[0]))
		{
			// check for utf-8 BOM
			$bom = bin2hex(substr($fileContents[0], 0, 3));
			if ($bom == 'efbbbf')
			{
				// remove the utf-8 bom from the first line
				$fileContents[0] = substr($fileContents[0], 3);
			}
		}

		foreach ($fileContents as $line)
		{
			$lineNumber++;
			$line = trim($line);
			// check if a line is commented
			if ((strlen($line) == 0) || (strncmp($line, ';', 1) == 0))
			{
				continue;
			}

			@list($key, $value) = explode('=', $line, 2);
			if (empty($value))
			{
				$this->error(sprintf('Unexpected or empty value on line %4d: %s', $lineNumber, rtrim($line)));
			}

			if (isset($remap[$key]))
			{
				if (!is_array($remap[$key]))
				{
					$remap[$key] = array($remap[$key]);
				}

				foreach ($remap[$key] as $keyMap)
				{
					$keyPosition = strrpos($keyMap, '.');
					$namespace = substr($keyMap, 0, $keyPosition);
					$newKey = substr($keyMap, $keyPosition+1);

					if (!isset($resources[$namespace]))
					{
						$resources[$namespace] = array();
					}
					
					if ((!PRESERVE_ENGLISH) && ($resources[$namespace][$newKey] != '*MISSING*'))
					{
						$resources[$namespace][$newKey] = trim($value);
					}
					$touchedKeys[$namespace .'.'. $newKey] = true;
				}
			}
			else
			{
				// put the key in the <Unassigned> namespace
				$unassigned[$key] = trim($value);
			}
		}
		
		$directory = (!empty($this->vars['outputDirectory'])) ? $this->vars['outputDirectory'] : '.';
		if (substr($directory, -1) != '/')
			$directory .= '/';

		$newFile = $directory . basename($this->vars['resourceFile'], '.txt') . '-converted.txt';
		$fp = fopen($newFile, 'w');
		if (!$fp)
		{
			$this->fatal('Could not create remap file '. $newFile .'. Please check file write permissions.');
		}
	
		if (BLANK_NEW_KEYS)
		{
			ksort($resources);
			$newKeys = array();
			foreach ($resources as $namespace => $keys)
			{
				ksort($keys);
				foreach ($keys as $key => $value)
				{
					// keep track of which keys are new
					if (!isset($touchedKeys[$namespace .'.'. $key]))
					{
						$newKeys[$namespace][$key] = $value;
					}
				}
			}

			// blank out the new keys
			ksort($newKeys);
			foreach ($newKeys as $namespace => $keys)
			{
				ksort($keys);
				foreach ($keys as $key => $value)
				{
					// add the carriage return to say this key should be commented out
					$resources[$namespace][$key] = "\r" . $resources[$namespace][$key];
				}
			}
		}


		// write the namespaced keys
		ksort($resources);
		$newKeys = array();
		foreach ($resources as $namespace => $keys)
		{
			fwrite($fp, '[' . $namespace . ']' . "\n");
			ksort($keys);
			foreach ($keys as $key => $value)
			{
				// check if the key is new and should be commented out
				if (strncmp($value, "\r", 1) == 0)
				{
					fwrite($fp, ';');
					$value = trim($value);
				}

				fwrite($fp, "\t" . $key . '=' . $value . "\n");
				
				// keep track of which keys are new
				if (!isset($touchedKeys[$namespace .'.'. $key]))
				{
					$newKeys[$namespace][$key] = $value;
				}
			}
			fwrite($fp, "\n");
		}
		fclose($fp);


		// write the new keys file
		$newFile = $directory . basename($this->vars['resourceFile'], '.txt') . '-newkeys.txt';
		$fp = fopen($newFile, 'w');
		if (!$fp)
		{
			$this->fatal('Could not create new keys file '. $newFile .'. Please check file write permissions.');
		}

		// write the namespaced keys
		ksort($newKeys);
		foreach ($newKeys as $namespace => $keys)
		{
			fwrite($fp, '[' . $namespace . ']' . "\n");
			ksort($keys);
			foreach ($keys as $key => $value)
			{
				fwrite($fp, "\t" . $key . '=' . $value . "\n");
			}
			fwrite($fp, "\n");
		}
		fclose($fp);

		$this->println("Complete!\n");
	}
}

ShellScript::load();
?>