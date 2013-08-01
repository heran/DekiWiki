<?php $this->includeCss('users.css'); ?>
<?php $this->includeJavaScript('externalauth.js'); ?>
<?php $this->set('template.action.search', $this->get('search-form.action')); ?>

<form action="<?php $this->html('add-form.action'); ?>" method="post" class="add-user padding">
	<div class="field">
		<?php echo $this->msg('Users.form.username'); ?> <small><?php echo $this->msg('Users.form.required'); ?></small><br/>
		<?php echo DekiForm::singleInput('text', 'username'); ?>
	</div>

	<div class="field">
		<?php echo $this->msg('Users.form.email'); ?><br/>
		<?php echo DekiForm::singleInput('text', 'email'); ?>
	</div>

	<div class="passwords"> 
		<div class="field password">
			<?php echo $this->msg('Users.form.password'); ?><br/>
			<?php echo DekiForm::singleInput('password', 'password'); ?>
		</div>

		<div class="field verify">
			<?php echo $this->msg('Users.form.password.verify'); ?><br/>
			<?php echo DekiForm::singleInput('password', 'password_verify'); ?>
		</div>
		<div class="password-instructions"><?php echo $this->msg('Users.form.password.instructions'); ?></div>
	</div>

	<?php $this->html('form.role-select-section'); ?>

	<?php $this->html('form.auth-section'); ?>

	<?php if ($this->get('form.group-boxes.count') > 0) : ?>
		<div class="select-title"><?php echo $this->msg('Users.form.select-title'); ?></div>
		<div class="<?php echo $this->get('form.group-boxes.count') > 10 ? 'groups groups-large' : 'groups' ?>">
			<?php $this->html('form.group-boxes'); ?>
		</div>
	<?php endif; ?>

	<div class="submit">
		<?php echo DekiForm::singleInput('button', 'submit', 'submit', array(), $this->msg('Users.add.single.button')); ?>
		<span class="or"><?php echo $this->msgRaw('Users.form.cancel', $this->get('add-form.back'));?></span>
	</div>
</form>