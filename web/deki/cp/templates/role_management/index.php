<?php $this->includeCss('users.css'); ?>

<div class="instructions"><?php echo($this->msg('Roles.description', $this->get('userManagementUrl'), $this->get('groupManagementUrl')));?></div>

<form method="post" action="<?php $this->html('roles-form.action'); ?>" class="roles">
	<?php $this->html('roles-table'); ?>
</form>