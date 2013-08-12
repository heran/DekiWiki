<?php $this->includeCss('customize.css'); ?>

<?php DekiMessage::info($this->msg('CustomHTML.warning'), true); ?>

<div class="padding-wrap">
	<div class="current">
		<form method="get" action="<?php $this->html('changeskin.form.action'); ?>">
			<?php echo($this->msg('CustomHTML.current', $this->get('skinname'), $this->get('skinstyle')));?>
		</form>
	</div>

	<?php if ($this->get('count') > 0) : ?>
	<form method="post" action="<?php $this->html('form.action');?>" class="custom-html">
		<?php for ($i = 1; $i <= $this->get('count'); $i++) : ?>
			<div class="section">
				<div class="example">
					<p><?php echo($this->msg('CustomHTML.example'));?></p>
					<?php if (is_file($this->get('skinpath').'/screenshot_html'. $i .'.png')) : ?>			
						<img src="<?php echo($this->get('skindir').'/screenshot_html'. $i .'.png'); ?>" />
					<?php else : ?>
						<div class="notfound"><?php echo($this->msg('CustomHTML.noscreenshot')); ?></div>
					<?php endif; ?>
				</div>
				<div class="input-html">
					<p><?php echo $this->msg('CustomHTML.section', $i); ?></p>
					<?php echo DekiForm::singleInput(
						'textarea',
						'custom[html'.$i.']',
						$this->get('html'.$i),
						array('onkeydown' => 'return catchTab(this,event)', 
						'wrap' => 'off', 'class' => 'resizable')
					); ?>
				</div>
			</div>
		<?php endfor; ?>
		<div class="submit">
			<?php echo DekiForm::singleInput('button', 'submit', 'submit', array(), $this->msg('CustomHTML.form.button'));?>
		</div>
	</form>
	<?php else: ?>
	<div class="dekiFlash">
		<ul class="noareas">
			<li><?php echo $this->msg('CustomHTML.error.noareas', $this->get('skinname'));?></li>
		</ul>
	</div>
	<?php endif; ?>
</div>