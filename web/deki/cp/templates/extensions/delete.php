<?php $this->includeCss('settings.css'); ?>

<form method="post" action="<?php $this->html('form.action'); ?>">
	<?php DekiMessage::warn($this->msg('Extensions.delete.verify'), true); ?>

	<ul class="verify">
		<?php $this->html('extension-list'); ?>
	</ul>

	<div class="submit">
		<?php echo DekiForm::singleInput('button', 'operate', '1', array(), $this->msg('Extensions.delete.button')); ?>
		<span class="or"><?php echo $this->msgRaw('Extensions.form.cancel', $this->get('form.cancel')); ?></span>
	</div>
</form>
