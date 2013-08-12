<?php $this->includeCss('maint.css'); ?>
<?php $this->includeCss('package_importer.css'); ?>

<div class="title">
	<h3><?php echo $this->msg('PackageImporter.title.manual'); ?></h3>
</div>

<div class="cblock">
	<form method="post" action="<?php $this->html('form.action');?>">
		<div class="button"><?php echo DekiForm::singleInput('button', 'action', 'update', array(), $this->msg('PackageImporter.button.manual')); ?></div>
		
		<div class="description"><?php echo $this->msg('PackageImporter.description'); ?></div>
	</form>
</div>

<?php if ($this->get('uploadable')): ?>
	<div class="title">
		<h3><?php echo $this->msg('PackageImporter.title.custom'); ?></h3>
	</div>
	
	<div class="cblock">
			<form method="post" enctype="multipart/form-data">
				<?php echo DekiForm::singleInput('file', 'file'); ?>
				<?php echo DekiForm::singleInput('button', 'action', 'custom', array(), $this->msg('PackageImporter.button.import'));?><br/>
				<small><?php echo DekiForm::singleInput('checkbox', 'onetime', 'true', array('checked' => 'checked'), $this->msg('PackageImporter.description.once')); ?></small>
			</form>
	</div>
<?php endif; ?>

<div class="title">
	<h3><?php echo $this->msg('PackageImporter.title.last'); ?></h3>
</div>

<div class="cblock">
	<?php $this->html('package-table'); ?>
</div>
