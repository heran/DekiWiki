<?php $this->includeCss('settings.css'); ?>

<form method="post" action="<?php $this->html('form.action');?>" class="editorconfig">
	<div class="dekiFlash">
		<ul class="info first">
			<li><?php echo($this->msg('EditorConfig.description'));?></li>
		</ul>
	</div>
	
	<div class="field">
		<?php echo $this->msg('EditorConfig.form.toolbar'); ?><br/>
		<?php echo $this->html('form.select'); ?>
		<a href="#" id="preview-editor"><?php echo($this->msg('EditorConfig.preview-config'));?></a>
		<?php echo($this->msg('EditorConfig.or'));?>
		<a href="#" id="paste-config"><?php echo($this->msg('EditorConfig.paste-config'));?></a>
	</div>
	
	<div id="eareaParent"></div>

	<div class="field">
		<?php echo $this->msg('EditorConfig.form.config'); ?><br/>
		<?php echo DekiForm::singleInput('textarea', 'config', $this->get('form.config'), array('class' => 'resizable')) ; ?>
	</div>
	
	<div class="submit">
		<?php echo DekiForm::singleInput('button', 'submit', 'submit', array(), $this->msg('EditorConfig.form.button')); ?>
	</div>
</form>

<script type="text/javascript" src="/deki/plugins/page_editor_ckeditor/ckeditor/ckeditor_basic.js"></script>
<script type="text/javascript" src="/deki/plugins/page_editor_ckeditor/config.js"></script>
<script type="text/javascript">
Deki.atdEnabled = <?php echo $this->js('editor.atdEnabled'); ?>;
var aLt = aLt || [];
aLt['EditorConfig.preview-config'] = '<?php echo($this->msg('EditorConfig.preview-config'));?>';
aLt['EditorConfig.preview-hide'] = '<?php echo($this->msg('EditorConfig.preview-hide'));?>';
aLt['EditorConfig.preview-loading'] = '<?php echo($this->msg('EditorConfig.preview-loading'));?>';
</script>
<?php $this->includeJavascript('editor.js'); ?>
