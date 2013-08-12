<?php $this->includeCss('settings.css'); ?>

<div class="title">
	<h3><?php echo($this->msg('Settings.add'));?></h3>
</div>

<div class="table">
	<div class="field">
		<?php $this->html('edit.form'); ?>
	</div>
</div>

<div class="title">
	<h3><?php echo($this->msg('Settings.keys.existing'));?></h3>
</div>

<form method="post" action="<?php $this->html('form.action');?>" class="keys indent">
	<div class="commands">
		<div class="selected"><?php echo($this->msg('Settings.select'));?></div>
		<?php echo DekiForm::singleInput('button', 'action', 'verify', array('class' => 'command-delete'), $this->msg('Settings.delete.button')); ?>
	</div>
	
	<?php $this->html('table.editkeys'); ?>
</form>

<div class="title">
	<h3><?php echo($this->msg('Settings.keys.readonly'));?></h3>
</div>

<div class="note"><?php echo($this->msg('Settings.keys.readonly.description'));?></div>

<form class="keys indent" action="<?php $this->html('form.action');?>" >
	<?php $this->html('table.readkeys'); ?>
</form>
