<?php
$this->includeCss('users.css');

$this->set('template.action.form', $this->get('operations-form.action'));
$this->set('template.actions', 
	array(
		'activate' => $this->msg('Users.activate.button'), 
		'deactivate' => $this->msg('Users.deactivate.button'), 
		'group' => $this->msg('Users.groups.button'), 
		'role' => $this->msg('Users.roles.button')
	)
);
?>

<?php $this->html('users-table'); ?>
<?php $this->html('pagination'); ?>
