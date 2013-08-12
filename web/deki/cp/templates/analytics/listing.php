<?php $this->includeCss('settings.css'); ?>

<div class="directions">
	<p><?php echo($this->msg('Analytics.form.desc'));?></p>
</div>

<form method="post" action="<?php $this->html('form.action');?>" action="analytics">
	<div class="action">
		<ul>
			<li><?php echo $this->msg('Analytics.form.accountid');?></li>
			<li><input type="text" name="analyticskey" value="<?php $this->html('analyticskey');?>"/></li>
			<li><?php echo DekiForm::singleInput('button', 'submit', 'submit', array(), $this->msg('Analytics.form.save')); ?></li>
		</ul>
	</div>
</form>

<div class="find">
	<div class="title">
		<h3><?php echo $this->msg('Analytics.description0');?></h3>
	</div>
	<ul>
		<li><?php echo $this->msg('Analytics.description1');?></li>
		<li><?php echo $this->msg('Analytics.description2');?></li>
	</ul>
	<img src= "./assets/images/analytics-screenshot.gif" />
</div>