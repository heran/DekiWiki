<form method="post" action="<?php $this->html('form.action'); ?>" class="special-page-form">
	<div class="field">
		<?php echo DekiForm::singleInput('text', 'template.title', $this->get('form.template.title'), array(), wfMsg('Page.EditTemplate.label.title')); ?>
		<div class="deki-template-url">
			<?php echo $this->get('form.template.url'); ?>
		</div>
	</div>
	<div class="field">
		<?php echo DekiForm::singleInput('text', 'template.description', $this->get('form.template.description'), array(), wfMsg('Page.EditTemplate.label.description')); ?>
	</div>
	<div class="field">
		<label class="field-title"><?php echo wfMsg('Page.ListTemplates.type'); ?></label>
		<?php echo DekiForm::multipleInput('radio', 'template.type', $this->get('form.types'), $this->get('form.template.type'), array(), wfMsg('Page.EditTemplate.label.type')); ?>
	</div>
	<div class="field">
		<?php echo DekiForm::multipleInput('select', 'template.language', $this->get('form.languages'), $this->get('form.template.language'), array(), wfMsg('Page.EditTemplate.label.language')); ?>
	</div>
	<div id="footer">
		<div class="buttons-bottom">
			<?php echo DekiForm::singleInput('button', 'action', 'update', array(), wfMsg('Page.EditTemplate.popup.submit')); ?>
		</div>
	</div>
</form>

<script type="text/javascript">
$(function() {
	<?php echo $this->get('form.js'); ?>
});
</script>
