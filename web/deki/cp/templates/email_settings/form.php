<?php $this->includeCss('settings.css'); ?>

<form method="post" action="<?php $this->html('form.action'); ?>" class="email">
	
	<div class="title">
		<h3><?php echo $this->msg('EmailSettings.form.title'); ?></h3>
	</div>
	
	<div class="field">
		<?php echo $this->msg('EmailSettings.form.from'); ?><br/>
		<?php echo DekiForm::singleInput('text', 'from', $this->get('form.from')); ?> 
		<?php 
		$from = $this->get('form.from'); 
		if (empty($from)) {
			echo '<span class="warning">'. $this->msg('EmailSettings.warning').'</span>'; 
		}
		?>
		<div class="test-email">
			<?php echo DekiForm::singleInput('button', 'submit', 'email', array(), $this->msg('EmailSettings.form.test')); ?>
		</div>
	</div>
	
	<div class="title">
		<h3><?php echo $this->msg('EmailSettings.form.smtp.title'); ?></h3>
	</div>
	
	<div class="description">
		<p><?php echo $this->msg('EmailSettings.external.description'); ?></p>
	</div>
	
	<div class="field">
		<?php echo $this->msg('EmailSettings.form.smtp.server'); ?><br/>
		<?php echo DekiForm::singleInput('text', 'smtp-server', $this->get('form.smtp.server')); ?>
	</div>
	
	<div class="field">
		<?php echo $this->msg('EmailSettings.form.smtp.port'); ?><br/>
		<?php echo DekiForm::singleInput('text', 'smtp-port', $this->get('form.smtp.port')); ?>
		
	</div>
	
	<div class="field">
		<?php echo $this->msg('EmailSettings.form.smtp.username'); ?><br/>
		<?php echo DekiForm::singleInput('text', 'smtp-username', $this->get('form.smtp.username')); ?>
	</div>
	
	<div class="field">
		<?php echo $this->msg('EmailSettings.form.smtp.password'); ?><br/>
		<?php echo DekiForm::singleInput('password','smtp-password', $this->get('form.smtp.password')); ?>
	</div>
	
	<div class="field">
		<?php echo $this->msg('EmailSettings.form.smtp.secure'); ?><br/>
		<?php echo DekiForm::multipleInput('select', 'smtp-secure', array(
			'none' => $this->msg('EmailSettings.form.smtp.none'), 
			'tls' => $this->msg('EmailSettings.form.smtp.tls'), 
			'ssl' => $this->msg('EmailSettings.form.smtp.ssl')
		), $this->get('form.smtp.secure')); ?>
	</div>
	
	<div class="submit">
		<?php echo DekiForm::singleInput('button', 'submit', 'save', array(), $this->msg('EmailSettings.form.submit')); ?>
	</div>
</form>