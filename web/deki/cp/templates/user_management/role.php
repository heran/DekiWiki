<?php $this->includeCss('users.css'); ?>
<?php $this->set('template.action.search', $this->get('search-form.action')); ?>

<div class="title">
	<h3><?php echo $this->msg('Users.roles'); ?></h3>
</div>

<form method="post" action="<?php $this->html('form.action'); ?>" class="indent usertoroles">

	<p><?php echo $this->msg('Users.roles.users'); ?></p>

	<?php $this->html('form.role-select'); ?>
	
	<p><?php echo $this->msg('Users.roles.selected'); ?></p>
	
	<?php $this->html('users-table'); ?>

	<div class="submit">
		<?php echo DekiForm::singleInput('button', 'operate', '1', array(), $this->msg('Users.roles.button')); ?>
		<span class="or"><?php echo $this->msgRaw('Users.form.cancel', $this->get('form.back')); ?></span>
	</div>
</form>
