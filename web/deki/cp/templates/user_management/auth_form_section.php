
<div class="field" id="deki-external-auth">
	<span class="select"><?php echo $this->msg('Users.form.authentication'); ?></span><br/>
	<?php 
		$options = $this->get('auth-form-section.external-options');
		$hasExternal = count($options) > 0;

		echo DekiForm::multipleInput(
			'radio',
			'auth_type',
			array(
				'local' => $this->msg('Users.form.authentication.local'),
				'external' => $this->msg('Users.form.authentication.external')
			),
			$this->get('auth-form-section.authType'),
			array('disabled' => !$hasExternal)
		);
	?>

	<?php
		if ($hasExternal)
		{
			echo DekiForm::multipleInput('select', 'external_auth_id', $this->get('auth-form-section.external-options'), $this->get('auth-form-section.authId'));
		}
		else
		{
			echo DekiForm::multipleInput('select', 'external_auth_id', array($this->msg('Users.form.authentication.none')), 0, array('disabled' => true));
		}
	?>
	
	<fieldset class="external">
		<legend>
			<?php echo $this->msg('Users.form.authentication.external.title'); ?>
		</legend>
		<span><?php echo $this->msg('Users.form.authentication.external.help'); ?></span>
		<?php echo $this->msg('Users.form.authentication.external.username'); ?> <?php echo DekiForm::singleInput('text', 'external_auth_username'); ?><br/>
		<?php echo $this->msg('Users.form.authentication.external.password'); ?> <?php echo DekiForm::singleInput('password', 'external_auth_password'); ?>
	</fieldset>
</div>
