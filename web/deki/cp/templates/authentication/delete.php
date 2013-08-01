<?php $this->includeCss('users.css'); ?>

<form method="post" action="<?php $this->html('form.action'); ?>">
	<?php DekiMessage::warn($this->msg('Authentication.delete.verify'), true); ?>
	
	<ul class="verify">
		<?php $this->html('service-list'); ?>
	</ul>

	<div class="submit">
		<?php echo DekiForm::singleInput('button', 'operate', '1', array(), $this->msg('Authentication.delete.button')); ?>
		<span class="or"><?php echo $this->msgRaw('Authentication.form.cancel', $this->get('form.cancel')); ?></span>
	</div>
</form>
