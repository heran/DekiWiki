<?php
$this->includeCss('settings.css');
$this->includeJavascript('services.js');

$this->set('template.subtitle',
	$this->get('extension.isScript') ?
	$this->msg('Extensions.edit.script') :
	$this->msg('Extensions.edit.extension')
);
?>

<div class="debug-export">
	<a href="<?php $this->html('debugexport.href'); ?>"><?php echo $this->msg('Extensions.debug.exportsettings'); ?></a>
</div>

<form class="extensions" method="post" action="<?php $this->html('form.action'); ?>" class="edit">
	
	<?php if ($this->get('extension.isScript')) : ?>

		<div class="field">
			<?php echo $this->msg('Extensions.form.name'); ?><br />
			<?php echo DekiForm::singleInput('text', 'name', $this->get('extension.name'), array('class' => 'long')); ?>
		</div>

		<div class="title">
			<h3><?php echo $this->msg('Extensions.form.configuration'); ?></h3>
		</div>

		<div class="field">
			<?php echo $this->msg('Extensions.form.manifest'); ?><br />
			<?php echo DekiForm::singleInput('text', 'manifest', $this->get('extension.manifest'), array('class' => 'long')); ?>
		</div>
	
		<div class="field">
			<?php echo DekiForm::singleInput('checkbox', 'debug', 'true', array('checked' => $this->get('extension.debug') == 'true'), 'Use debug mode (reloads the script manifest for each invocation)' ); ?>	
		</div>

		<div class="field">
			<?php echo DekiForm::singleInput('checkbox', 'pref_protected', 'true', array('checked' => $this->get('pref.protected')), $this->msg('Extensions.form.protected')); ?>
		</div>

	<?php else : ?>

		<div class="field">
			<?php echo $this->msg('Extensions.form.type'); ?>
			<div class="fields">
				<?php echo DekiForm::multipleInput('radio', 'init_type', $this->get('form.init-options'), $this->get('extension.init')); ?>
			</div>
		</div>
		
		<div class="field">
			<?php echo $this->msg('Extensions.form.name'); ?><br />
			<?php echo DekiForm::singleInput('text', 'name', $this->get('extension.name'), array('class' => 'long')); ?>
		</div>
		
		<div class="field">
			<?php echo $this->msg('Extensions.form.sid'); ?> <small><?php echo $this->msg('Extensions.form.required'); ?></small><br />
			<?php echo DekiForm::singleInput('text', 'sid', $this->get('extension.sid'), array('class' => 'long')); ?>
		</div>
		
		<div class="field">
			<?php echo $this->msg('Extensions.form.uri'); ?> <small><?php echo $this->msg('Extensions.form.required'); ?></small><br />
			<?php echo DekiForm::singleInput('text', 'uri', $this->get('extension.uri'), array('class' => 'long')); ?>
		</div>

		<div class="title">
			<h3><?php echo $this->msg('Extensions.form.configuration'); ?></h3>
		</div>

		<div class="field">
			<?php echo DekiForm::singleInput('checkbox', 'pref_protected', 'true', array('checked' => $this->get('pref.protected')), $this->msg('Extensions.form.protected')); ?>
		</div>

	<?php endif; ?>

	<div class="field extension-native">
		<?php $this->html('config-table'); ?>
	</div>

	<div class="title">
		<h3><?php echo $this->msg('Extensions.form.preferences'); ?></h3>
	</div>
	
	<div class="field">
		<em><?php echo $this->msg('Extensions.form.preferences.help'); ?></em>
	</div>
		
	<div class="field">
		<h4><?php echo $this->msg('Extensions.form.title'); ?></h4>
		<div>
			<?php echo DekiForm::singleInput('text', 'pref_title', $this->get('pref.title'), array('class' => 'long')); ?>
			<div class="default">
				Default: <?php $this->text('pref.default.title'); ?>
			</div>
		</div>
	</div>
    
        <div class="field">
		<h4><?php echo $this->msg('Extensions.form.label'); ?></h4>
		<div>
			<?php echo DekiForm::singleInput('text', 'pref_label', $this->get('pref.label'), array('class' => 'long')); ?>
			<div class="default">
				Default: <?php $this->text('pref.default.label'); ?>
			</div>
		</div>
	</div>
	
	<div class="field">
		<h4><?php echo $this->msg('Extensions.form.description'); ?></h4>
		<div>
			<?php echo DekiForm::singleInput('text', 'pref_description', $this->get('pref.description'), array('class' => 'long')); ?>
			<div class="default">
				Default: <?php $this->text('pref.default.description'); ?>
			</div>
		</div>
	</div>
	
	<div class="field">
		<h4><?php echo $this->msg('Extensions.form.namespace'); ?></h4>
		<div>
			<?php echo DekiForm::singleInput('text', 'pref_namespace', $this->get('pref.namespace'), array('class' => 'long')); ?>
			<div class="default">
				Default: <?php $this->text('pref.default.namespace'); ?>
			</div>
		</div>
	</div>
	
	<div class="field">
		<h4><?php echo $this->msg('Extensions.form.logouri'); ?></h4>
		<div>
			<?php echo DekiForm::singleInput('text', 'pref_logo_uri', $this->get('pref.logoUri'), array('class' => 'long')); ?>
			<div class="default">
				Default: <?php $this->text('pref.default.logoUri'); ?>
			</div>
		</div>
	</div>

	<div class="field">
		<h4><?php echo $this->msg('Extensions.form.functions'); ?></h4>
		<div>
			<?php echo DekiForm::singleInput('text', 'pref_functions', $this->get('pref.functions'), array('class' => 'long')); ?>
			<div class="default">
				Default: <?php $this->text('pref.default.functions'); ?>
			</div>
		</div>
	</div>
	
	<div class="submit">
		<?php echo DekiForm::singleInput('button', 'action', 'save', array(), $this->get('extension.isScript') ? $this->msg('Extensions.edit.script.save'): $this->msg('Extensions.edit.extension.save')); ?> 
		<span class="or"><?php echo $this->msgRaw('Extensions.form.cancel', $this->get('form.cancel')); ?></span>
	</div>
</form>
