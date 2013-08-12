<?php
/**
 * @title Shell Script
 * @author Guerric Sloan
 * @date October 16, 2007
 * 
 * @note Base class for command line PHP scripts
 *
 */

class ShellScript
{
	protected $prompts = array();
	protected $vars = array();
	protected $title = '';

	public $argc, $argv;


	public function ShellScript()
	{
		error_reporting(E_ALL);

		$this->argc = $_SERVER['argc'];
		$this->argv = &$_SERVER['argv'];
	}

	/**
	 * Uses the script filename as the class to instantiate
	 */
	static final function load()
	{
		$file = basename($_SERVER['argv'][0]);
		$argex = explode('.', $file);
		$class = (count($argex) > 0) ? $argex[0] : $file;
		
		$script = new $class();
		$script->run();
	}

	public function run()
	{
		$this->title();
		$this->getPrompts();
	}

	// display a script title message
	protected function title($text = '')
	{
		$text = (!empty($text)) ? $text : $this->title;
		
		$len = strlen($text);
		$repeat = str_repeat('-', $len+6);

		printf("%s\n", $repeat);
		printf("-- %s --\n", $text);
		printf("%s\n\n", $repeat);
	}
	
	protected function println($text = '')
	{
		printf("%s\n", $text);
	}

	protected function notice($text)
	{
		printf("NOTICE: %s\n", $text);
	}

	protected function error($text)
	{
		printf("ERROR: %s\n", $text);
	}

	protected function fatal($text)
	{
		printf("FATAL ERROR: %s\n\n", $text);
		die();
	}

	// this problably doesn't work due to windows shell calling the script
	protected function getPrompts()
	{
		if (!empty($this->prompts))
		{
			// check if the required variables we sent as args
			$argsRequired = count($this->prompts);

			if (($this->argc-1) == $argsRequired)
			{
				// put the args
				$arg = 1;
				foreach ($this->prompts as $prompt => $var)
				{
					$this->vars[$var] = $this->argv[$arg];
					$arg++;
				}

				return;			
			}

			printf("Please specify the required inputs below\n\n");
			foreach ($this->prompts as $prompt => $var)
			{
				echo (is_int($prompt)) ? $var . ': ' : $prompt;

				$this->vars[$var] = $this->in();
			}
			printf("\n");
		}
	}

	protected function in()
	{
		return trim(fgets(STDIN));
	}

	protected function writeFile($filePath, $contents, $withKeys = false)
	{
		$fp = fopen($filePath, 'w');
		if (!$fp)
		{
			die(': Could not open file for writing, ' . $filePath);
		}

		if (is_array($contents))
		{
			foreach ($contents as $key => $line)
			{
				if (is_array($line))
				{
					$line = implode(' ', $line);
				}

				if ($withKeys)
				{
					fwrite($fp, $key .'='. $line . "\n");
				}
				else
				{
					fwrite($fp, $line . "\n");
				}

			}
		}
		else
		{
			fwrite($fp, $contents);
		}

		fclose($fp);
	}
}

?>
