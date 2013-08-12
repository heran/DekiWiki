<?php
/* @var $this DekiView */
$this->includeCss('settings.css');
$this->includeCss('activation.css');
?>

<div class="contact">
	<div class="title">
		<h3><?php echo $this->msg('Activation.contact'); ?></h3>
	</div>
	
	<div id="sales" class="<?php echo $this->get('highlight.sales') ? 'highlight': ''; ?>">
		<h4><?php echo $this->msg('Activation.sales'); ?></h4>
		<p><?php echo $this->msg('Activation.sales.contact'); ?></p>
	</div>
	
	<div id="support" class="<?php echo $this->get('highlight.support') ? 'highlight': ''; ?>">
		<h4><?php echo $this->msg('Activation.support'); ?></h4>
		<p><?php echo $this->msg('Activation.support.contact', $this->get('supporturl')); ?></p>
	</div>
</div>


<div class="activation">
	<div class="field">
		<div class="title">
			<h3><?php echo $this->msg('Activation.upload'); ?></h3>
		</div>
		<form method="post" action="<?php $this->get('form.action');?>" enctype="multipart/form-data" class="activation">
			<?php echo $this->msg('Activation.form.label');?> <?php echo DekiForm::singleInput('file', 'license'); ?>
			<?php echo DekiForm::singleInput('button', 'submit', 'submit', array(), $this->msg('Activation.form.button')); ?>
			<div class="description"><?php 
				if (DekiSite::isCore()) 
				{
					echo $this->msg('Activation.form.description.community');
				}
				elseif (DekiSite::isInactive())
				{
					global $wgProductTrialUrl;
					echo $this->msg('Activation.form.description.inactive', $wgProductTrialUrl, $this->get('license.productkey'));
				}
				elseif (DekiSite::isExpired())
				{
					echo $this->msg('Activation.form.description.expired');
				}
			?></div>
		</form>
	</div>
	
	<div class="field">
		<div class="title">
			<h3><?php echo $this->msg('Activation.form.product-key'); ?></h3>
		</div>
		<div class="productkey">
			<input type="text" readonly="true" value="<?php $this->html('license.productkey'); ?>" />
		</div>
	</div>

	<div class="field">
		<div class="title">
			<h3><?php echo $this->msg('Activation.activated'); ?></h3>
		</div>
		
		<dl>
			<dt><?php echo $this->msg('Activation.license.type');?></dt>
			<?php if (!$this->get('site.expired')): ?>
				<dd><?php $this->html('license.type'); ?></dd>
			<?php else: ?>
				<dd class="expired"><?php $this->html('license.type.expired'); ?></dd>
			<?php endif; ?>
			
			<?php if (!is_null($this->get('license.usercount'))): ?>
			<dt><?php echo $this->msg('Activation.usercount'); ?></dt>
			<dd><?php $this->html('usercount');?> / <?php $this->html('license.usercount'); ?></dd>
			<?php endif; ?>
			
			<?php if (!$this->get('site.inactive')) : ?>
				<?php if (!is_null($this->get('license.expires'))) : ?>
					<dt><?php echo $this->msg('Activation.form.expiration');?></dt>
					<dd><?php $this->html('license.expires');?></dd>
					<dt><?php echo $this->msg('Activation.form.date');?></dt>
					<dd><?php $this->html('license.date');?></dd>
				<?php else : ?>
					<dd><?php echo $this->msg('Activation.license.perpetual');?></dd>
				<?php endif; ?>
			<?php endif; ?>
			
			<?php $primarycontacts = $this->get('license.primary'); ?>
			<?php if (!empty($primarycontacts)): ?>
				<dt><?php echo $this->msg('Activation.license.primary'); ?></dt>
				<dd>
					<?php foreach ($primarycontacts as $key => $val) : ?>
						<strong><?php echo $this->msg('Activation.form.'.$key); ?></strong>: <?php echo $val;?><br/>
					<?php endforeach; ?>
				</dd>
			<?php endif; ?>
			
			<?php $secondarycontact = $this->get('license.secondary'); ?>
			<?php if (!empty($secondarycontact)): ?>
				<dt><?php echo $this->msg('Activation.license.secondary'); ?></dt>
				<dd>
					<?php foreach ($secondarycontact as $key => $val) : ?>
						<strong><?php echo $this->msg('Activation.form.'.$key); ?></strong>: <?php echo $val;?><br/>
					<?php endforeach; ?>
				</dd>
			<?php endif; ?>
			
			<?php $licensee = $this->get('license.licensee'); ?>
			<?php if (!empty($licensee)) : ?>
				<dt><?php echo $this->msg('Activation.license.licensee'); ?></dt>
				<dd>
					<?php foreach ($licensee as $key => $val) : ?>
						<?php if (!is_string($key) || !is_string($val)) : ?>
							<?php continue; ?>
						<?php endif; ?>
						<strong><?php echo $this->msg('Activation.form.'.$key); ?></strong>: <?php echo $key == 'address'? nl2br($val): $val;?><br/>
					<?php endforeach; ?>
					
					<?php $usercount = $this->get('license.usercount'); ?>
					<?php if ($usercount > 0): ?>
						<strong><?php echo $this->msg('Activation.form.active-users'); ?></strong>: <?php echo $usercount; ?><br/>
					<?php endif; ?>
					
					<?php $sitecount = $this->get('license.sitecount'); ?>
					<?php if ($sitecount > 0): ?>
						<strong><?php echo $this->msg('Activation.form.active-sites'); ?></strong>: <?php echo $sitecount; ?><br/>
					<?php endif; ?>
					
					<?php $hosts = $this->get('license.hosts'); ?>
					<?php if (!empty($hosts)): ?>
					<strong><?php echo $this->msg('Activation.form.host'); ?></strong>: <?php echo implode(', ', $hosts); ?><br/>
					<?php endif; ?>
				</dd>
					
			<?php endif; ?>
			
			<dt><?php echo $this->msg('Activation.capability.active-seats');?></dt>
			<dd><?php echo $this->has('license.seat.count') 
				? '<span class="enabled">' . $this->get('license.seat.count') . '</span>'
				: '<span class="disabled">' . $this->msg('Activation.capability.disabled') . '</span>';?></dd>

			<dt><?php echo $this->msg('Activation.capability.rating');?></dt>
			<dd><?php echo $this->get('license.has.rating') 
				? '<span class="enabled">' . $this->msg('Activation.capability.enabled') . '</span>'
				: '<span class="disabled">' . $this->msg('Activation.capability.disabled') . '</span>';?></dd>
			
			<dt><?php echo $this->msg('Activation.capability.search');?></dt>
			<dd><?php echo $this->get('license.has.search') 
				? '<span class="enabled">' . $this->msg('Activation.capability.enabled') . '</span>' 
				: '<span class="disabled">' . $this->msg('Activation.capability.disabled') . '</span>';?></dd>
			
			<dt><?php echo $this->msg('Activation.capability.memcache');?></dt>
			<dd><?php echo $this->get('license.has.memcache') 
				? '<span class="enabled">' . $this->msg('Activation.capability.enabled') . '</span>' 
				: '<span class="disabled">' . $this->msg('Activation.capability.disabled') . '</span>';?>
			</dd>
			
			<dt><?php echo $this->msg('Activation.capability.caching');?></dt>
			<dd><?php echo $this->get('license.has.caching') 
				? '<span class="enabled">' . $this->msg('Activation.capability.enabled') . '</span>' 
				: '<span class="disabled">' . $this->msg('Activation.capability.disabled') . '</span>';?></dd>
			
			<?php if (!$this->get('license.is.core')): ?>
				<dt><?php echo $this->msg('Activation.capability.anon');?></dt>
				<dd><?php echo $this->get('license.has.anon') 
					? '<span class="enabled">' . $this->msg('Activation.capability.enabled') . '</span>' 
					: '<span class="disabled">' . $this->msg('Activation.capability.disabled') . '</span>';?>
				</dd>
			<?php endif; ?>
			
			<?php if ($this->get('license.has.sids')) : ?>
				<dt><?php echo $this->msg('Activation.sid'); ?></dt>
				<dd><?php
					echo '<ul>';
					foreach ($this->get('license.sids') as $sid => $expiration)
					{
						$expires = !empty($expiration) 
							? '<span class="expiration">'.$this->msg('Activation.sid.expires', $expiration).'</span>'
							: '';
						echo '<li>'.$sid.$expires.'</li>';
					}
					echo '</ul>';
				?></dd>
			<?php endif; ?>
		</dl>
		
		<?php $license = $this->get('license.terms'); ?>
		<?php if (!empty($license)) : ?>
			<div class="title">
				<h3><?php echo $this->msg('Activation.license'); ?></h3>
			</div>
			<div class="license">
				<?php $this->html('license.terms'); ?>
			</div>
		<?php endif; ?>
	</div>
	
	<div class="field">
		<div class="title">
			<h3><?php echo $this->msg('Activation.princexml'); ?></h3>
		</div>
		<div class="prince">
			<a href="<?php echo $this->msg('Activation.princexml.url');?>">
				<img src="/skins/common/images/prince.png" alt="<?php echo $this->msg('Activation.princexml'); ?>"/>
			</a>
		</div>
		<p><?php echo $this->msg('Activation.princexml.description', $this->msg('Activation.princexml.url')); ?></p>
	</div>
</div>