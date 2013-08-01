<?php $this->includeCss('maint.css'); ?>
<?php $page = $this->get('page'); ?>

<div class="articleinfo">
	<?php DekiMessage::warn($this->msgRaw('PageRestore.move.description', sprintf('<strong>%s</strong>', htmlspecialchars($page['title']))), true); ?>

	<form id="move-form" method="post" action="<?php $this->html('move-form.action'); ?>">
		<?php echo DekiForm::singleInput('hidden', 'restore', '2'); ?>
		<?php echo DekiForm::singleInput('hidden', 'to', $this->get('move-form.to')); ?>
		<label for="restore-to"><?php echo $this->msg('PageRestore.data.restore-to'); ?>:</label>

		<?php $this->text('move-form.to'); ?>/
		<?php echo DekiForm::singleInput('text', 'newtitle', $this->get('move-form.restore-to'), array('id' => 'restore-to')); ?>
		
		<div class="submit">
			<?php echo DekiForm::singleInput('button', 'submit', 'submit', array(), $this->msg('PageRestore.restore.button')); ?>
			<span class="or"><?php echo $this->msg('PageRestore.form.cancel', $this->get('move-form.cancel')); ?></span>
		</div>
	</form>
</div>


<?php if ($this->has('childTable')) : ?>
<p>
	<?php echo $this->msg('PageRestore.restore.description', sprintf('<strong>%s</strong>', $page['title'])); ?>
	<?php $this->html('childTable'); ?>
</p>
<?php endif; ?>
