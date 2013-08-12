<?php
/* @var $this DekiInstallerView */
?>

<h1>Your Install Is Almost Complete!</h1>

<div>
	<?php if ($this->get('licenseGenerated')) : ?>
		<div class="success">
			Your MindTouch License was generated successfully!
		</div>
		
		<?php if ($this->get('isCore')) : ?>
			<p>
				A community license was sent to your email address <strong><?php $this->text('admin.email'); ?></strong>.
			</p>
		<?php else : ?>
			<p>
				A trial license was sent to your email address <strong><?php $this->text('admin.email'); ?></strong>.
			</p>
		<?php endif; ?>
		
		<p class="indent">
			Make sure to check your spam folder if you do not see your license in the next few minutes.
			If you did not receive your email, please add <strong>licenses@mindtouch.com</strong> to your address book;
			then visit <a href="<?php $this->html('href.trial.full'); ?>"><?php $this->text('href.trial'); ?></a>.
		</p>
		
	<?php else : ?>
		<p class="error">
			Your MindTouch license could not be generated. Please follow the steps below to retrieve your license.
		</p>
		<p class="indent indent-error">
			Please add <strong>licenses@mindtouch.com</strong> to your address book and visit <a href="<?php $this->html('href.trial.full'); ?>" class="external"><?php $this->html('href.trial'); ?></a> to request a license.
		</p>
		
	<?php endif; ?>
</div>

<?php if ($this->has('manualConfiguration')) : ?>
	<div class="waitnotdone">
		<h2><?php echo $this->msg('Page.Install.addtl-title'); ?></h2>
		<p><?php echo $this->msg('Page.Install.addtl-manual'); ?></p>
		<pre class="instructions"><?php $this->text('manualConfiguration'); ?></pre>
	</div>	
<?php endif; ?>

<h2>MindTouch Activation</h2>
<p>
	Clicking on "Continue to MindTouch" will bring you to your MindTouch Control Panel, where you will need to upload your license, to activate your MindTouch installation.
</p>

<form method="post" action="/Special:Userlogin" class="login">
	<input type="hidden" name="username" value="<?php $this->text('admin.username'); ?>" />
	<input type="hidden" name="password" value="<?php $this->text('admin.password'); ?>" />
	<input type="submit" class="submit" value="<?php echo htmlspecialchars($this->msg('Page.Install.visit')); ?>" />
</form>

<div id="mt-api-status">
	<div class="install-not-running">
		<img src="/skins/common/icons/loading.gif"><?php echo $this->has('manualConfiguration') ? 'MindTouch is not running...' : 'MindTouch is starting up...'; ?>
	</div>
	<div class="install-running">
		<img src="/skins/common/icons/accept.png">MindTouch is running!
	</div>
</div>

<?php if ($this->get('suggestUpdateWiki')) : ?>
	<div class="install-complete-footer">
		<?php echo $this->msg('Page.Install.addtl-update'); ?>
	</div>
<?php endif; ?>

<script type="text/javascript">
$(function() {
	MT.Install.ShowIframe('success');
	MT.Install.PollInstallStatus();
});
</script>
<?php
global $conf;
// @TODO: consolidate with trial or campaign sites
finalize_product_installation($conf);
