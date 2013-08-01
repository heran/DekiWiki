<?php
$this->includeCss('settings.css');
$this->includeJavascript('services.js');

$this->set('template.subtitle', $this->msg('Authentication.edit.title'));
?>

<div class="debug-export">
	<a href="<?php $this->html('debugexport.href'); ?>"><?php echo $this->msg('Authentication.debug.exportsettings'); ?></a>
</div>

<form method="post" action="<?php $this->html('form.action'); ?>" class="edit">
		<div class="field">
		<?php echo $this->msg('Authentication.form.description');?><br />
		<?php echo DekiForm::singleInput('text', 'description', $this->get('service.description'), array('class' => 'long')); ?>
	</div>

	<div class="field">
		<?php echo $this->msg('Authentication.form.type');?><br />
		<?php echo DekiForm::multipleInput('radio', 'init_type', $this->get('form.initType-options'), $this->get('service.initType')); ?>
	</div>
	
	<div class="field">
		<?php echo $this->msg('Authentication.form.sid');?><br />
		<?php echo DekiForm::singleInput('text', 'sid', $this->get('service.sid'), array('class' => 'long')); ?>
	</div>

	<div class="field">
		<?php echo $this->msg('Authentication.form.uri');?><br />
		<?php echo DekiForm::singleInput('text', 'uri', $this->get('service.uri'), array('class' => 'long')); ?>
	</div>


	<div class="title">
		<h3><?php echo $this->msg('Authentication.form.configuration');?></h3>
	</div>

	<div class="field">
		<?php $this->html('config-table'); ?>
	</div>


	<div class="title">
		<h3><?php echo $this->msg('Authentication.form.preferences');?></h3>
	</div>
	<div class="field">
		<?php $this->html('prefs-table'); ?>
	</div>

	<div class="submit">
		<?php echo DekiForm::singleInput('button', 'action', 'save', array(), $this->msg('Authentication.edit.button')); ?> 
		<?php echo('<span class="or">'.$this->msgRaw('Authentication.form.cancel', $this->get('form.cancel')).'</span>'); ?>
	</div>
</form>
