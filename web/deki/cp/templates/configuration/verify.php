<?php $this->includeCss('settings.css'); ?>

<form method="post" action="<?php $this->html('form.action');?>">
	<?php DekiMessage::warn($this->msg('Settings.delete.verify')); ?>

	<ul class="verify">
		<?php $this->html('keys-list'); ?>
	</ul>

	<div class="submit">
		<?php echo DekiForm::singleInput('button', 'submit', 'submit', array('class' => 'restore'), $this->msg('Settings.delete.button'));?>
		<?php echo DekiForm::singleInput('hidden', 'action', 'delete'); ?>
		<span class="or"><?php echo($this->msg('Settings.form.cancel', $this->get('form.backUrl')));?></span>
	</div>
</form>