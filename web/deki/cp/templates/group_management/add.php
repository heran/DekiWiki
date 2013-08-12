<?php $this->includeCss('users.css'); ?>
<?php $this->includeJavaScript('externalauth.js'); ?>
<?php $this->set('template.action.search', $this->get('search-form.action')); ?>

<div class="edit">
	<form method="post" action="<?php $this->html('add-form.action'); ?>" class="groups padding">
		<?php echo($this->msg('Groups.add.description')); ?>
	
		<div class="field">
			<?php $this->html('add-form.input-names'); ?>
		</div>

		<div class="field">
			<span class="select"><?php echo($this->msg('Groups.data.role'));?></span><br/>
			<?php $this->html('add-form.select-roles'); ?>
		</div>
	
		<?php $this->html('form.auth-section'); ?>
	
		<div class="submit">
			<?php $this->html('add-form.submit');?> 
			<span class="or"><?php echo $this->msgRaw('PageRestore.form.cancel', $this->get('add-form.back')); ?></span>
		</div>
	</form>
</div>