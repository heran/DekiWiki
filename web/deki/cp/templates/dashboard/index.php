<?php $this->includeCss('dashboard.css'); ?>

<div class="activation">
	<div class="title">
		<h3><?php echo $this->msg('Dashboard.activate'); ?></h3>
	</div>
	<div class="indent staus-<?php $this->html('product.type');?>">
		<div class="status-wrap">
			<div>
				<strong><?php echo $this->msgRaw('Dashboard.label.product') ?>:</strong>
					<?php echo $this->get('product.name'); ?>
			</div>
			
			<?php if ($this->get('product.status')): ?>
			<div>
				<strong><?php echo $this->msgRaw('Dashboard.label.status') ?>:</strong>
					<?php echo $this->get('product.status'); ?>	
			</div>
			<?php endif; ?>
			
			<?php if ($this->get('product.expiration')): ?>
				<div>
					<strong><?php echo $this->msgRaw('Dashboard.label.expiration') ?>:</strong>
						<?php echo $this->get('product.expiration'); ?>
				</div>
			<?php endif; ?>
			
			<div>
				<strong><?php echo $this->msgRaw('Dashboard.label.version')?></strong>:
					<?php echo $this->get('product.version'); ?> (<a href="<?php $this->html('page.versions');?>"><?php echo $this->msg('Dashboard.version.details'); ?></a>)
			</div>
		<?php if ($this->has('upgradetext')): ?>
			<div class="version-update"><?php $this->html('upgradetext'); ?></div>
		<?php endif; ?>
		</div>
	</div>
	<div class="clear">
	<?php foreach ($this->get('license.contacts') as $type) : ?>
		<?php $contact = $this->get('license.' . $type); ?>
		<?php if (!empty($contact)): ?>
		<div class="title">
			<?php echo $this->msg('Activation.license.'.$type); ?>
		</div>
		<div class="indent">
			<dd>
				<?php foreach ($contact as $key => $val) : ?>
					<strong><?php echo $this->msg('Activation.form.'.$key); ?></strong>: <?php echo $val;?><br/>
				<?php endforeach; ?>
			</dd>
		</div>
		<?php endif; ?>
	<?php endforeach; ?>
	</div>
	
</div>

<div class="info">
	<div class="title">
		<h3><?php echo $this->msg('Dashboard.info'); ?></h3>
	</div>
	<div class="indent">
		<?php if ($this->get('product.help.contact')): ?>
			<div class="license"><?php echo $this->get('product.help.contact'); ?></div>
		<?php endif; ?>

		<div class="more"><?php echo $this->get('product.help'); ?></div>
	</div>
</div>


