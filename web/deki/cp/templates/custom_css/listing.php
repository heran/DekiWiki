<?php $this->includeCss('customize.css'); ?>

<?php DekiMessage::info($this->msg('CustomCSS.warning'), true); ?>

<div class="padding-wrap">
	<div class="current"><?php echo($this->msg('CustomCSS.current', $this->get('skinname'), $this->get('skinstyle')));?></div>

	<form method="post" action="<?php $this->html('form.action');?>" enctype="multipart/form-data">
		<div class="section">
			<div class="input">
				<div class="export"><a href="<?php $this->html('page.export'); ?>"><?php echo $this->msg('CustomCSS.export'); ?></a></div>		
				<p><?php echo($this->msg('CustomCSS.csstemplate'));?></p>
				<?php echo DekiForm::singleInput('textarea', 'css_template', $this->get('css.template'), array('onkeydown' => 'return catchTab(this,event)', 'wrap' => 'off', 'class' => 'resizable')); ?>
			</div>
		</div>
	<!-- Disabled per INT-1302 
		<fieldset>
			<legend><?php echo $this->msg('CustomCss.upload.title'); ?></legend>
			<div class="radio">
				<?php echo DekiForm::multipleInput('radio', 'type', array('append' => $this->msg('CustomCss.upload.append'), 'replace' => $this->msg('CustomCss.upload.replace')), 'append'); ?>
			</div>
			<?php echo DekiForm::singleInput('file', 'css', 'submit'); ?>
			<?php echo DekiForm::singleInput('button', 'submit', 'upload', array(), $this->msg('CustomCss.upload.button')); ?>
		</fieldset>
	-->
		<div class="submit">
			<?php echo DekiForm::singleInput('button', 'submit', 'submit', array(), $this->msg('CustomCSS.form.button')); ?>
		</div>
	</form>

</div>