<?php
/* @var $this DekiInstallerView */
?>

<h1>Choose Install Type</h1>
<div id="installation-type" class="installation-type">
	<?php if ($this->get('showCommercial')) : ?>
	<div class="product product-commercial">
		<div class="product-tab"></div>
		<div class="install">
			<a href="#" rel="commercial" class="button">Install</a>
		</div>
		<div class="details">
			<h2>MindTouch Technical Communications Suite</h2>
			<div class="description">
				The social layer to your technical documentation.
			</div>
			<div class="features">
				Community scoring, adapative search, self-organized content, curation...
				<a href="<?php echo ProductURL::MINDTOUCH_TCS; ?>" class="external about" target="_blank">read more</a>
			</div>
		</div>
	</div>
	<?php endif; ?>
	
	<div class="product product-platform">
		<div class="product-tab"></div>
		<div class="install">
			<a href="#" rel="platform" class="button">Install</a>
		</div>
		<div class="details">
			<h2>MindTouch Platform</h2>
			<div class="description">
				The social layer to your enterprise systems and applications.
			</div>
			<div class="features">
				Desktop Suite, enterprise connectors, commercially supported...
				<a href="<?php echo ProductURL::MINDTOUCH_PLATFORM; ?>" class="external about" target="_blank">read more</a>
			</div>
		</div>
	</div>
	
	<div class="product product-core">
		<div class="product-tab"></div>
		<div class="install">
			<a href="#" rel="core" class="button">Install</a>
		</div>
		<div class="details">
			<h2>MindTouch Core</h2>
			<div class="description">
				One of the most popular open source projects in the world.
			</div>
			<div class="features">
				Intuitive wiki, development platform, web services framework, standards compliant...
				<a href="<?php echo ProductURL::MINDTOUCH_CORE; ?>" class="external about" target="_blank">read more</a>
			</div>
		</div>
	</div>

	<?php $this->inputHidden('SelectedEdition', array('id' => 'hidden-SelectedEdition')); ?>
</div>

<script type="text/javascript">
	$('#installation-type a.button').click(function(e) {
		var type = $(this).attr('rel');
		MT.log('Selecting edition: ' + type);
		
		$('#hidden-SelectedEdition').val(type);
		MT.Install.NextStep();
		return false;
	});
</script>
