<?php $this->includeCss('maint.css'); ?>
<?php $page = $this->get('page'); ?>

<div class="warning">
	<?php echo $this->msgRaw('PageRestore.data.info', htmlspecialchars($page['title']), $page['date.deleted'], $page['user.deleted']); ?>
</div>

<div class="articleinfo">
	<form id="restore-form" method="post" action="<?php $this->html('form.action'); ?>">
		<?php echo DekiForm::singleInput('hidden', 'restore', '1'); ?>
		<div class="submit">
			<?php echo DekiForm::singleInput('button', 'submit', 'submit', array(), $this->msg('PageRestore.restore.button')); ?>
			<span class="or"><?php echo $this->msg('PageRestore.form.cancel', $this->get('form.cancel')); ?></span>
		</div>
	</form>
</div>

<div class="preview">
	<div class="body">
		<h1><?php echo htmlspecialchars($page['title']); ?></h1>
		<?php echo $page['preview']; ?>
	</div>
</div>