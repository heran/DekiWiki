<?php $this->includeCss('settings.css'); ?>

<?php $this->set('template.actions', 
	array(
		'restart' => $this->msg('Extensions.action.restart'), 
		'stop' => $this->msg('Extensions.action.stop'), 
		'delete' => $this->msg('Extensions.action.delete')
	)
); ?>
<?php $this->set('template.action.form', $this->get('form.action')); ?>

<?php $this->html('services-table'); ?>
