<?php
/* @var $this DekiView */
$this->includeJavaScript('start.js');
$this->includeJavaScript('jquery-history/jquery.history.js');

$iframeStepMap = array();
?>

<form id="start" method="post" action="<?php $this->html('form.action'); ?>" class="<?php echo $this->get('form.posted') ? 'posted' : ''; ?>" style="display: none;">
	<?php foreach ($this->get('steps') as $step) : ?>
		<div class="install-step step<?php echo $step['index']; ?>">
			<?php
				$this->html($step['key']);
				$iframeStepMap[$step['index']] = $step['key'];
			?>
		</div>
	<?php endforeach; ?>
	<script type="text/javascript">
		var iframeStepMap = <?php echo json_encode($iframeStepMap); ?>;
	</script>

<?php if (DekiMvcConfig::DEBUG) : ?>
	<style type="text/css">
	.debug-navigation {
		position: fixed;
		bottom: 0;
		left: 0;
		right: 0;
	}
	</style>
	<div class="debug-navigation">
		<div class="navButtons"> 
			<div class="backButton"> 
				<a href="#">Back</a>
			</div> 
			<div class="nextButton"> 
				<input type="button" value="Next" class="submit_form"> 
			</div> 
		</div> 	
	</div>
<?php endif; ?>
</form>

<?php
// @TODO: consolidate with trial or campaign sites
global $conf;
initialize_product_installation($conf);
