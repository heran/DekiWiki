<?php $this->includeCss('settings.css'); ?>
<?php $this->set('template.subtitle', $this->msg('Settings.edit')); ?>

<div class="dekiFlash">
	<ul class="info first">
		<li><?php echo($this->msg('Settings.warning'));?></li>
	</ul>
</div>

<div class="field table">
	<?php $this->html('form'); ?>
</div>
