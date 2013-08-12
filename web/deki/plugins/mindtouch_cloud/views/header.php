<?php
/* @var $this DekiPluginView */
$hostname = DekiRequest::getInstance()->getHost();
?>
<?php if ($this->has('header.banner.html')) : ?>
<div id="mindtouch-cloud-expiration" class="<?php $this->html('header.banner.class'); ?>">
	<div class="dekistatus">
		<?php $this->html('header.banner.html'); ?>
	</div>
</div>
<?php endif; ?>

<div id="header-wrap">
	<div id="header" class="wrap">
		<ul class="top-menu">
			<li>
				<span class="welcome"><?php echo $this->msg('Common.tpl.welcome', DekiUser::getCurrent()->getName()); ?></span>
			</li>
			<li>
			    <a href="http://www.mindtouch.com/support">Help</a>
			</li>
			<li>
			    <a href="/Special:Userlogout"><?php echo($this->msg('Common.tpl.logout'));?></a>
			</li>
		</ul>
		<dl>
			<dt>Managing MindTouch site:</dt>
			<dd>
				<a href="/"><?php echo htmlspecialchars($hostname); ?></a>
			</dd>
		</dl>
	</div>
</div>
<div id="subheader-wrap">
	<div id="subheader" class="wrap">
		<div class="heading">
			<ul>
				<li>
					<a href="/">View site</a>
				</li>
			</ul>
			<h1><?php echo htmlspecialchars($hostname); ?></h1>
		</div>
	</div>
</div>
