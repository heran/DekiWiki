<?php
$this->includeCss('users.css');

$this->set('template.action.form', $this->get('operations-form.action'));
$this->set('template.actions', 
	array(
		'delete' => $this->msg('Groups.delete')
	)
);
?>

<?php $this->html('groups-table'); ?>
<?php $this->html('pagination'); ?>
