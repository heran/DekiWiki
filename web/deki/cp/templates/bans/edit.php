<?php $this->includeCss('users.css'); ?>
<?php $this->set('template.subtitle', $this->msg('Bans.form.title.edit')); ?>
<?php $this->includeJavascript('bans.listing.js'); ?>

<div class="edit">
	<form method="post" action="<?php $this->html('add-form.action'); ?>" class="ban padding">
		<div class="field">
			<span class="select"><?php echo($this->msg('Bans.form.type'));?></span><br/>
			<?php $this->html('form.type'); ?>
		</div>
	
	
		<div class="field">
				<span id="deki-banuser"><?php echo($this->msg('Bans.form.user'));?></span>
				<span id="deki-banip"><?php echo($this->msg('Bans.form.ip'));?></span>
			<br/>
			<?php $this->html('form.user'); ?>
		</div>
	
		<div class="field">
			<?php echo($this->msg('Bans.form.expires'));?><br/>
			<?php $this->html('form.expires'); ?>
		</div>
	
		<div class="field">
			<?php echo($this->msg('Bans.form.reason'));?><br/>
			<?php $this->html('form.reason'); ?>
		</div>
			
		<div class="submit">
			<?php $this->html('form.submit');?> 
			<span class="or"><?php echo $this->msg('Bans.form.cancel', $this->get('form.back')); ?></span>
		</div>
	
		<?php $this->html('form.id'); ?>
	</form>
</div>