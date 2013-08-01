<?php $this->includeCss('maint.css'); ?>

<form method="post" action="<?php $this->html('form.action');?>" class="caches">
	<div class="section">
		<div class="title">
			<h3><?php echo($this->msg('CacheManagement.searchindex'));?></h3>
		</div>
		<div class="field">
			<div class="button">
				<?php echo DekiForm::singleInput('button', 'rebuild', 'search', array(), $this->msg('CacheManagement.searchindex.button')); ?>
			</div>
			<div class="def"><?php echo($this->msg('CacheManagement.searchindex.description'));?></div>
			<?php if ($this->get('lucene.pending') > 0) : ?>
			<div class="progress">
				<?php echo $this->msgRaw('CacheManagement.searchindex.progress', '<span class="items">'.$this->get('lucene.pending').'</span>'); ?>
			</div>
			<?php endif; ?>
		</div>
	</div>
	
	<div class="section">
		<div class="title">
			<h3><?php echo($this->msg('CacheManagement.uicache'));?></h3>
		</div>
		<div class="field">
			<div class="button">
				<?php echo DekiForm::singleInput('button', 'rebuild', 'ui', array(), $this->msg('CacheManagement.uicache.button')); ?>
			</div>
			<div class="def"><?php echo($this->msg('CacheManagement.uicache.description'));?></div>
		</div>
	</div>
	<?php if ($this->get('cache.disabled')) : ?>
		<div class="section">
			<div class="title">
				<h3><?php echo $this->msg('CacheManagement.options.title'); ?></h3>
			</div>

			<div class="description">
				<?php echo $this->msg('CacheManagement.options.text'); ?>
			</div>

			<div class="field">
				<div class="description">
					<?php
						$link =  '<a href="'.$this->get('commercial.url').'" target="_blank">'.$this->msg('CacheManagement.core.more').'</a>';
						echo $this->msgRaw('CacheManagement.core', $link);
					?>
				</div>
			</div>
		</div>
	<?php else: ?>
		<div class="section">
			<div class="title">
				<h3><?php echo $this->msg('CacheManagement.options.title'); ?></h3>
			</div>

			<div class="description">
				<?php echo $this->msg('CacheManagement.options.text'); ?>
			</div>

			<div class="field">
				<div class="button">
					<?php echo DekiForm::multipleInput('select', 'cache_master', $this->get('form.cache_master.options'), $this->get('form.cache_master')); ?>
				</div>
				<div class="def">
					<dl class="status">
						<dt><?php echo $this->msg('CacheManagement.master.disabled'); ?></dt>
							<dd><?php echo $this->msg('CacheManagement.master.disabled.info'); ?></dd>
						<dt><?php echo $this->msg('CacheManagement.master.request'); ?></dt>
							<dd><?php echo $this->msg('CacheManagement.master.request.info'); ?></dd>
						<dt><?php echo $this->msg('CacheManagement.master.instance'); ?></dt>
							<dd><?php echo $this->msg('CacheManagement.master.instance.info'); ?></dd>
						<dt><?php echo $this->msg('CacheManagement.master.memcache'); ?></dt>
							<dd><?php echo $this->msg('CacheManagement.master.memcache.info'); ?></dd>
					</dl>
				</div>
			</div>

			<div class="submit">
				<?php echo DekiForm::singleInput('button', 'submit', 'cache', array(), $this->msg('CacheManagement.options.button')); ?>
			</div>
		</div>
	<?php endif; ?>
</form>
