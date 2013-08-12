<?php $this->includeCss('users.css'); ?>

<form method="post" action="<?php $this->html('form.action'); ?>">
	<?php DekiMessage::warn($this->msg('Bans.delete.verify'), true); ?>

	<ul class="verify">
		<?php $this->html('bans-list'); ?>
	</ul>

	<div class="submit">
		<?php echo DekiForm::singleInput('button', 'operate', '1', array(), $this->msg('Bans.delete.button')); ?>
		<span class="or"><?php echo $this->msg('Bans.form.cancel', $this->get('form.back')); ?></span>
	</div>
</form>
