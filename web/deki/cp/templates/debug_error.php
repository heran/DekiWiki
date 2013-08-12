<?php
/**
 * Special case view file for rendering errors in debug mode
 */
// debug handling, render an error pane
$id = 'error'.time();
$Exception = $this->get('error.exception');
?>

<div id="<?php echo $id; ?>">
	<style>
	#<?php echo $id; ?> {
		background-color: #EDEDED;
		font-family: arial, "lucida sans", sans-serif;
		padding: 10px 15px;
	}
	#<?php echo $id; ?> h1 {
		font-size: 16pt;
		font-weight: bold;
		padding: 5px;
	}
	#<?php echo $id; ?> h1.error { background-color: #FF0000; }
	#<?php echo $id; ?> h1.warning { background-color: #FFCC66; }
	#<?php echo $id; ?> h1.notice { background-color: #FFFF66; }

	#<?php echo $id; ?> h2 {
		font-size: 12pt;
		font-weight: bold;
		padding: 5px;
	}
	#<?php echo $id; ?> pre {
		background-color: #fff;
		border: solid 1px #444;
		padding: 8px;
		margin: 5px 10px;
	}
	</style>

	<?php
	$stackTrace = $Exception->getTrace();
	$trace = isset($stackTrace[0]) ? $stackTrace[0] : array();

	$file = isset($trace['file']) ? $trace['file'] : '';
	$line = isset($trace['line']) ? $trace['line'] : '';
	$class = isset($trace['class']) ? $trace['class'] : '';
	$type = isset($trace['type']) ? $trace['type'] : '';
	$function = isset($trace['function']) ? $trace['function'] : '';
	$args = isset($trace['args']) ? $trace['args'] : array();

	if ($line == '')
	{
		$trace = isset($stackTrace[1]) ? $stackTrace[1] : array();
		$file = isset($trace['file']) ? $trace['file'] : '';
		$line = isset($trace['line']) ? $trace['line'] : '';
		$args = isset($trace['args']) ? $trace['args'] : array();
	}

	$code = $Exception->getCode();
	$cssClass = '';
	switch ($code)
	{
		case E_ERROR:
			$code = 'PHP Error';
			$cssClass = 'error';
			break;
		case E_WARNING:
			$code = 'PHP Warning';
			$cssClass = 'warning';
			break;
		case E_NOTICE:
			$code = 'PHP Notice';
			$cssClass = 'notice';
			break;
		//case E_CORE_ERROR:
		default:
			$code = 'Error('. $code .')';
	}
	?>

	<h1 class="<?php echo $cssClass; ?>">
		<?php printf('%s in %s on line %s', $code, $class == 'DekiError' ? basename($file) : $class . $type . $function, $line); ?>
	</h1>

	<pre><?php echo $Exception->getMessage(); ?></pre>

	<pre><?php echo $Exception->getTraceAsString(); ?></pre>

	<?php if (!empty($args)) : ?>
		<h2 onclick="document.getElementById('debug_error_args').style.display='block';" style="cursor: pointer;">Arguments <small>(+)</small></h2>
		<pre id="debug_error_args" style="display: none;"><?php print_r($args); ?></pre>
	<?php endif; ?>

	<?php if (!empty($file) && !empty($line)) : ?>
		<h2>Extracted source (around line <?php echo $line; ?>)</h2>
		<pre><?php
			$source = @file($file);
			if (is_array($source))
			{
				$offset = 6;
				$min = $line - $offset < 0 ? 0 : $line - $offset;
				$max = $line + $offset > count($source) ? count($source) : $line + $offset;

				for ($i = $min; $i < $max; $i++)
				{			
					echo ($i+1 == $line) ? $i+1 . '->>>' : $i+1 . ' ';
					echo htmlspecialchars($source[$i]);
				}
			}
		?></pre>
	<?php endif; ?>

	<!--
	<?php echo $Exception->getTraceAsString(); ?>
	-->

	<h2>Request</h2>
	<pre><?php print_r($_REQUEST); ?></pre>

	<h2>Session</h2>
	<pre><?php print_r($_SESSION); ?></pre>
</div>

<script type="text/javascript">
	document.body.innerHTML = document.getElementById('<?php echo $id; ?>').innerHTML;
	document.body.id = '<?php echo $id; ?>';
</script>
