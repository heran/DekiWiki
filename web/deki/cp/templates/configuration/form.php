
<form method="post" action="<?php $this->html('form.action'); ?>" class="indent">
	<table class="form">
		<tr class="input">
			<td width="200">
				<?php echo $this->msg('Settings.form.key') . '<br/>'; ?>
				<?php echo DekiForm::singleInput('text', 'key', $this->get('form.key'), array('id' => 'key')); ?>
				<?php echo DekiForm::singleInput('hidden', 'edit_key', $this->get('form.key')); ?>
			</td>
			<td>
				<?php echo $this->msg('Settings.form.value') . '<br/>'; ?>
				<?php echo DekiForm::singleInput('text', 'value', $this->get('form.value'), array('id' => 'value')); ?>
			</td>
		</tr>
	</table>

	<div class="submit">
		<?php
			if (is_null($this->get('form.key')))
			{
				echo DekiForm::singleInput('button', 'action', 'update_key', array(), $this->msg('Settings.add.button'));
			}
			else
			{
				echo DekiForm::singleInput('button', 'action', 'update_key', array(), $this->msg('Settings.edit.button'));
				echo '<span class="or">'.$this->msg('Settings.form.cancel', $this->get('href.cancel')).'</span>';
			}
		?>
	</div>
</form>
