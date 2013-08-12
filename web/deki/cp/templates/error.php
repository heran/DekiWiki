<?php
/**
 * Special case view file for rendering errors in production
 */
$Exception = $this->get('error.exception');
$this->includeCss('error.css');
?>

<div id="application-error">
	<h2>Sorry but there was an error while processing your request.</h2>
	<div class="block">
		Try hitting back on your browse to try again. If the problem persists please contact support with the following information.
	</div>

	<div class="title">
		<h3>Error Report</h3>
	</div>

	<div class="block">
		<p class="error">Error (Code: <?php echo $Exception->getCode(); ?>) <?php echo $Exception->getMessage(); ?></p>
		<textarea readonly="readonly"><?php echo htmlspecialchars($Exception->getTraceAsString()) . "\n"; ?>Error (Code: <?php echo $Exception->getCode(); ?>) <?php echo $Exception->getMessage(); ?></textarea>
	</div>
</div>
