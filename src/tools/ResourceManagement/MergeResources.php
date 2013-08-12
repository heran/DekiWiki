#!/usr/bin/php5
<?php
/**
 * @title Merge Resource Keys
 * @author Guerric Sloan
 * @date October 16, 2007
 *
 * @note Updates old flat key format into new namespaces key format
 *
 */
require('includes/ShellScript.php');

define('PRESERVE_ENGLISH', false);
define('BLANK_NEW_KEYS', true);

class MergeResources extends ShellScript
{
	protected $title = 'Merge Resource Keys';

	protected $prompts = array('What resource file is the base? ' => 'resourceFile',
							   'What resource file has new keys? ' => 'updateFile',
							   'What directory do you want to save the files to? ' => 'outputDirectory'
							   );

	public function run()
	{
		parent::run();

		$resources = array();
		$this->parseResources($this->vars['resourceFile'], $resources);

		$updates = array();
		$this->parseResources($this->vars['updateFile'], $updates);

		foreach ($updates as $namespace => $keys)
		{
			foreach ($keys as $key => $value)
			{
				$resources[$namespace][$key] = utf8_encode($value);
			}
		}

		// write the new keys file
		$directory = (!empty($this->vars['outputDirectory'])) ? $this->vars['outputDirectory'] : '.';
		$newFile = $directory . basename($this->vars['resourceFile'], '.txt') . '-updated.txt';
		$fp = fopen($newFile, 'w');
		if (!$fp)
		{
			$this->fatal('Could not create updated keys file '. $newFile .'. Please check file write permissions.');
		}

		// write the namespaced keys
		ksort($resources);
		foreach ($resources as $namespace => $keys)
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

	private function camelize($namespace)
	{
		$ex = explode('.', $namespace);

		$ns = '';
		foreach ($ex as $value)
		{
			$ns .= ucfirst($value) . '.';
		}

		return substr($ns, 0, -1);
	}


	private function parseResources($file, &$resourceArray)
	{
		$contents = file($file);

		if (isset($contents[0]))
		{
			// check for utf-8 BOM
			$bom = bin2hex(substr($contents[0], 0, 3));
			if ($bom == 'efbbbf')
			{
				// remove the utf-8 bom from the first line
				$contents[0] = substr($contents[0], 3);
			}
		}

		$namespace = '';
		foreach ($contents as $line)
		{
			$line = trim($line);
			if (!empty($line) && (strncmp($line, ';', 1) != 0))
			{
				if (strncmp($line, '[', 1) == 0)
				{
					$namespace = strtolower(substr($line, 1, -1));

					if (!isset($resourceArray[$namespace]))
					{
						$resourceArray[$namespace] = array();
					}
				}
				else
				{
					@list($key, $value) = explode('=', $line, 2);
					//if ($value == '')
					//	$this->fatal($line);

					$key = strtolower(trim($key));
					$value = trim($value);
					if (isset($resourceArray[$namespace][$key]))
					{
						$this->println(sprintf("DUPLICATE KEY ENCOUNTERED: \t\t%s", $namespace .'.'. $key));
					}
					$resourceArray[$namespace][$key] = $value;
				}
			}
		}
	}
}

ShellScript::load();
?>