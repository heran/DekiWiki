<?php
$this->includeCss('settings.css');
$this->includeJavascript('services.js');
?>
<div class="field">
	<div><?php echo $this->msg('Authentication.form.choose-provider'); ?></div>
	<form method="get" name="preconfigure" action="<?php $this->html('form.action.preconfigured');?>" class="preconfigure">
		<div class="services">
			<?php $this->html('preconfigured-list'); ?> <?php echo DekiForm::singleInput('button', 'submit', 'submit', array(), $this->msg('Authentication.preload.button')); ?>
		</div>
	</form>
</div>

<form class="add-auth" method="post" action="<?php $this->html('form.action'); ?>">
	<div class="field">
		<?php echo $this->msg('Authentication.form.description');?><br />
		<?php echo DekiForm::singleInput('text', 'description', $this->get('service.description'), array('class' => 'long')); ?>
	</div>

	<div class="field">
		<?php echo $this->msg('Authentication.form.type');?><br />
		<?php echo DekiForm::multipleInput('radio', 'init_type', $this->get('form.initType-options'), $this->get('service.initType')); ?>
	</div>
	
	<div class="field">
		<?php echo $this->msg('Authentication.form.sid');?> <small><?php echo $this->msg('Authentication.form.required');?></small><br />
		<?php echo DekiForm::singleInput('text', 'sid', $this->get('service.sid'), array('class' => 'long')); ?>
	</div>

	<div class="field">
		<?php echo $this->msg('Authentication.form.uri');?> <small><?php echo $this->msg('Authentication.form.required');?></small><br />
		<?php echo DekiForm::singleInput('text', 'uri', $this->get('service.uri'), array('class' => 'long')); ?>
	</div>


	<div class="title">
		<h3><?php echo $this->msg('Authentication.form.configuration');?></h3>
	</div>

	<div class="field">
		<?php echo DekiForm::singleInput('checkbox', 'default', null, array(), $this->msg('Authentication.form.default-provider')); ?>
	</div>

	<div class="field">
		<p class="help"><?php echo $this->msg('Authentication.add.help', $this->get('service.description'), $this->get('service.helpUrl'));?></p>
		<?php $this->html('config-table'); ?>
	</div>


	<div class="title">
		<h3><?php echo $this->msg('Authentication.form.preferences');?></h3>
	</div>

	<div class="field">
		<?php $this->html('prefs-table'); ?>
	</div>
	
	<div class="submit">
		<?php echo DekiForm::singleInput('button', 'action', 'save', array(), $this->msg('Authentication.add.button')); ?> 
		<?php echo('<span class="or">'.$this->msgRaw('Authentication.form.cancel', $this->get('form.cancel')).'</span>'); ?>
	</div>
</form>
