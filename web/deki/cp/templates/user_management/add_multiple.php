<?php
$this->includeCss('users.css');
$this->includeJavaScript('externalauth.js');
?>

<form action="<?php $this->html('add-form.action'); ?>" method="post" class="userstogroups padding">
	<p><?php echo $this->msg('Users.add.multiple.description');?></p>
	<div class="field">
		<?php echo DekiForm::singleInput('textarea', 'user_csv', $this->get('add-form.user_csv'), array('class' => 'resizable')); ?>
	</div>
	
	<?php $this->html('form.role-select-section'); ?>

	<?php $this->html('form.auth-section'); ?>

	<?php if ($this->get('form.group-boxes.count') > 0) : ?>
		<div class="select-title"><?php echo $this->msg('Users.form.select-title'); ?></div>
		<div class="<?php echo $this->get('form.group-boxes.count') > 10 ? 'groups groups-large' : 'groups' ?>">
			<?php $this->html('form.group-boxes'); ?>
		</div>
	<?php endif; ?>

	<div class="submit">
		<?php echo DekiForm::singleInput('button', 'submit', 'submit', array(), $this->msg('Users.add.multiple.button'));?>
		<span class="or"><?php echo $this->msgRaw('Users.form.cancel', $this->get('add-form.back'));?></span>
	</div>
</form>

<script type="text/javascript">
Deki.$(document).ready(function()
{
	Deki.$('#textarea-user_csv').defaultValue(
		"<?php echo str_replace("\n", "\\n", $this->msg('Users.add.multiple.default')); // guerrics: wfEncodeJSString escapes newlines ?>"
	);
});
</script>
