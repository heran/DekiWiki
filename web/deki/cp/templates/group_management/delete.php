<?php $this->includeCss('users.css'); ?>
<?php $this->set('template.action.search', $this->get('search-form.action')); ?>

<form method="post" action="<?php $this->html('form.action'); ?>">
	<?php DekiMessage::warn($this->msg('Groups.delete.verify'), true); ?>

	<ul class="verify">
		<?php $this->html('group-list'); ?>
	</ul>

	<div class="submit">
		<?php echo DekiForm::singleInput('button', 'operate', '1', array(), $this->msg('Groups.delete.button')); ?>
		<span class="or"><?php echo $this->msgRaw('Groups.form.cancel', $this->get('form.back')); ?></span>
	</div>
</form>
