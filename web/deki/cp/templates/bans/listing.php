<?php $this->includeCss('users.css'); ?>
<?php $this->includeJavascript('bans.listing.js'); ?>

<!--
<div class="title"><h3><?php echo($this->msg('Bans.add'));?></h3></div>
<form method="post" action="<?php $this->html('form.action');?>" class="bans">
	<?php $this->html('form.contents');?>
	<div class="submit">
		<?php echo(DekiForm::singleInput('button', 'submit', 'submit', array(), $this->msg('Bans.add.button'))); ?>
	</div>
</form>
--> 

<?php $this->set('template.actions', 
	array(
		'delete' => $this->msg('Bans.delete.button')
	)
); ?>


<?php $this->html('listing'); ?>
