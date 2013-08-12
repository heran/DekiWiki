#!/usr/bin/php5
<?php
/**
 * @title Diff Resources
 * @author Guerric Sloan
 * @date October 16, 2007
 * 
 * @note Determines what KEYS are new, updated, or deleted between resource files
 *
 */
require('includes/ShellScript.php');

class DiffResources extends ShellScript
{
	protected $title = 'Diff Resource Files';
	protected $prompts = array('What resource file is new? ' => 'newFile',
							   'What resource file is old? ' => 'oldFile'
							   );
	
	public function run()
	{
		parent::run();

		if (!is_file($this->vars['newFile']))
			$this->fatal('Could not find the new resource file, ' . $this->vars['newFile']);
		if (!is_file($this->vars['oldFile']))
			$this->fatal('Could not find the old resource file, ' . $this->vars['oldFile']);
	
		$newResources = array();
		$this->parseResources($this->vars['newFile'], $newResources);

		$oldResources = array();
		$this->parseResources($this->vars['oldFile'], $oldResources);

		$this->notice(sprintf("Showing changes in '%s' from '%s'\n", $this->vars['newFile'], $this->vars['oldFile']));

		// find the new keys
		$diff = array_diff_key($newResources, $oldResources);
		foreach ($diff as $key => $value)
		{
			$this->println(sprintf("NEW/MISSING:\t\t%s", $key));

		}
		$this->println();

		/*
		// find the updated keys
		foreach ($newResources as $key => $value)
		{
			if (isset($oldResources[$key]) && ($newResources[$key] != $oldResources[$key]))
			{
				$this->println(sprintf("UPDATED:\t\t%s", $key));
			}
		}
		$this->println();
		*/

		// find the deleted keys
		$diff = array_diff_key($oldResources, $newResources);
		foreach ($diff as $key => $value)
		{
			$this->println(sprintf("OBSOLETE:\t\t%s", $key));

		}

		$this->println();
		$this->notice("Complete!\n");
	}

	private function parseResources($file, &$resourceArray)
	{
		$contents = file($file);

		$namespace = '';
		foreach ($contents as $line)
		{
			$line = trim($line);
			if (!empty($line) && (strncmp($line, ';', 1) != 0))
			{
				if (strncmp($line, '[', 1) == 0)
				{
					$namespace = strtolower(substr($line, 1, -1));
				}
				else
				{
					@list($key, $value) = explode('=', $line, 2);
					//if ($value == '')
					//	$this->fatal($line);

					$key = strtolower(trim($key));
					$value = trim($value);
					if (isset($resourceArray[$namespace .'.'. $key]))
					{
						$this->println(sprintf("DUPLICATE KEY ENCOUNTERED: \t\t%s", $namespace .'.'. $key));
					}
					$resourceArray[$namespace .'.'. $key] = $value;
				}
			}
		}
	}
}

ShellScript::load();
?>