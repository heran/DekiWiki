<?php $this->includeCss('users.css'); ?>
<?php $this->set('template.action.search', $this->get('search-form.action')); ?>
<?php $this->set('template.subtitle', $this->msg('Users.groups')); ?>

<form method="post" action="<?php $this->html('form.action'); ?>" class="usertogroups indent">

	<p><?php echo $this->msg('Users.groups.select'); ?></p>
	
	<div class="groups groups-large">
		<?php $this->html('form.group-boxes'); ?>
	</div>
	
	<p><?php echo $this->msg('Users.groups.users');?></p>
	
	<?php $this->html('users-table'); ?>

	<div class="submit">
		<?php echo DekiForm::singleInput('button', 'operate', '1', array(), $this->msg('Users.groups.button')); ?>
		<span class="or"><?php echo $this->msgRaw('Users.form.cancel', $this->get('form.back'));?></span> 
	</div>
</form>
