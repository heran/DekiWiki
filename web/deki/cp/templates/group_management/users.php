<?php $this->includeCss('users.css'); ?>
<?php $this->set('template.subtitle', $this->msg('Groups.manage.title')); ?>
<?php $this->set('template.action.search', $this->get('search-form.action')); ?>

<?php if ($this->get('group.isInternal')) : ?>
	<div class="set">
		<form method="post" action="<?php $this->html('set-form.action'); ?>" class="addtogroup">
			<div class="title">
				<h3><?php echo($this->msg('Groups.users.add', $this->get('group.name')));?></h3>
			</div>
			<div class="field">
				<p><?php echo($this->msg('Groups.users.description'));?></p>
				<div class="find">
					<div><?php echo($this->msg('Groups.users.search'));?></div>
					<?php echo DekiForm::singleInput('text', 'set_value', '', array('class' => 'set')); ?>
				</div>
				<div class="submit">
					<?php echo DekiForm::singleInput('button', 'action', 'add_user', array(), $this->msg('Groups.users.add.user')); ?>
					<?php echo DekiForm::singleInput('button', 'action', 'add_group', array(), $this->msg('Groups.users.add.group')); ?>
					<span class="or">
						<?php echo $this->msgRaw('Groups.form.cancel', $this->get('set-form.back')); ?>
					</span>
				</div>
			</div>
		</form>
	</div>
<?php else : ?>
	<div class="dekiFlash">
		<ul class="info first">
			<li><?php echo($this->msg('Groups.users.noadd'));?></li>
		</ul>
	</div>
<?php endif; ?>

<div class="users">
	<div class="title">
		<h3><?php echo($this->msg('Groups.users.ingroup', $this->get('group.name')));?></h3>
	</div>

	<form method="post" action="<?php $this->html('operations-form.action'); ?>" class="addtouserlist">
		<?php if ($this->get('group.isInternal')) : ?>
			<div class="commands">
				<?php echo($this->msg('Groups.items'));?>
				<?php echo DekiForm::singleInput('button', 'action', 'remove', array('class' => 'remove'), $this->msg('Groups.Users.remove')); ?>
			</div>
		<?php endif; ?>

		<?php $this->html('users-table'); ?>
	</form>

	<?php $this->html('pagination'); ?>
</div>
