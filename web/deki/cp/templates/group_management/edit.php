<?php $this->includeCss('users.css'); ?>
<?php $this->includeJavaScript('externalauth.js'); ?>
<?php $this->set('template.subtitle', $this->msg('Groups.edit.title')); ?>
<?php $this->set('template.action.search', $this->get('search-form.action')); ?>

<form method="post" action="<?php $this->html('edit-form.action'); ?>" class="edit padding">
	<div class="field">
		<span class="select"><?php echo $this->msg('Groups.data.name'); ?></span><br/>
	<?php
		echo DekiForm::singleInput(
			'text',
			'group_name',
			$this->get('group.name'),
			array('disabled' => !$this->get('group.isInternal'))
		);
	?>
	</div>

	<div class="field">
		<span class="select"><?php echo($this->msg('Groups.data.role'));?></span><br/>
		<?php $this->html('edit-form.select-roles'); ?>
	</div>
	
	<?php if ($this->has('edit-form.authentication')) : ?>
		<div class="field authentication">
			<span class="select"><?php echo($this->msg('Groups.data.authentication'));?></span><br/>
		
			<?php $this->html('edit-form.authentication'); ?>
		</div>
	<?php endif; ?>

	<div class="submit">
		<?php $this->html('edit-form.submit');?>
		<span class="or"><?php echo $this->msgRaw('PageRestore.form.cancel', $this->get('edit-form.back')); ?></span>
	</div>
</form>