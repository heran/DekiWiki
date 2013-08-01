<?php $this->includeCss('maint.css'); ?>
<?php $this->set('template.action.form', $this->get('restore-form.action')); ?>
<?php $this->set('template.actions', 
	array(
		'restore' => $this->msg('FileRestore.restore.button'),
		'wipe' => $this->msg('FileRestore.delete.button')
	)
); ?>

<?php $this->html('listingTable'); ?>