<?php $this->includeCss('users.css'); ?>
<?php $this->set('template.action.search', $this->get('search-form.action')); ?>

<form method="post" action="<?php $this->html('form.action'); ?>">
	<?php DekiMessage::warn($this->msg('Users.activate.verify'), true); ?>

	<ul class="verify">
		<?php $this->html('users-list'); ?>
	</ul>
	
	<div class="submit">
		<?php echo DekiForm::singleInput('button', 'operate', '1', array(), $this->msg('Users.activate.button')); ?>
		<span class="or"><?php echo $this->msgRaw('Users.form.cancel', $this->get('form.back')); ?></span>
	</div>
</form>
