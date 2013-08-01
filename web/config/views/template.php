<?php
/* @var $this DekiInstallerView */
$this->includeJavascript('install.js');
?>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">

<head>
	<title>MindTouch Installation</title> 
	
	<meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
	<link rel="stylesheet" type="text/css" href="/skins/common/reset.css">
	<link href="./assets/install.css" rel="stylesheet" type="text/css" /> 
	
	<?php foreach ($this->getCssIncludes() as $cssfile) : ?>
		<link rel="stylesheet" type="text/css" href="./assets/<?php echo $cssfile; ?>" />
	<?php endforeach; ?>
	
	<script type="text/javascript" src="/skins/common/jquery/jquery.min.js"></script>
	<?php foreach ($this->getJavascriptIncludes() as $jsfile) : ?>
		<script type="text/javascript" src="./assets/<?php echo $jsfile; ?>"></script>
	<?php endforeach; ?>
</head>
<body id="<?php $this->text('controller.name'); ?>">

	<div class="wrap"> 
		<div class="header">
			<div class="logo"> 
				<img src="./assets/images/mindtouch-logo.png" alt="MindTouch" />
			</div>
			<h1>Installation</h1>
		</div>
		
		<div class="navTable">
			<table>
				<tbody>
					<tr>
					<?php if ($this->get('env.showConfiguration')) : ?>
						<td class="index first step1"><span>1</span></td>
						<td class="title first step1">Installation Type</td>
						<td class="index step2"><span>2</span></td>
						<td class="title step2">Site setup</td>
						<td class="index step3"><span>3</span></td>
						<td class="title step3">Configuration</td>
						<td class="index step4"><span>4</span></td>
						<td class="title step4">Your Organization</td>
						<td class="index step5"><span>5</span></td>
						<td class="title step5 confirmation">Confirmation</td>
						<td class="index step6"><span>6</span></td>
						<td class="title step6">Installation</td>
						<td class="index last step7"><span>7</span></td>
						<td class="title last step7">Activation</td>
					<?php else : ?>
						<td class="index first step1"><span>1</span></td>
						<td class="title first step1">Installation Type</td>
						<td class="index step2"><span>2</span></td>
						<td class="title step2">Site setup</td>
						<td class="index step3"><span>3</span></td>
						<td class="title step3">Your Organization</td>
						<td class="index step4"><span>4</span></td>
						<td class="title step4 confirmation">Confirmation</td>
						<td class="index step5"><span>5</span></td>
						<td class="title step5">Installation</td>
						<td class="index last step6"><span>6</span></td>
						<td class="title last step6">Activation</td>
					<?php endif; ?>
					</tr>
				</tbody>
			</table>
		</div>
		
		<div class="help-wrap">
			<div class="help">
				<h1>Need Help?</h1>
				<ul>
					<li class="help-web">
						Visit our <a href="<?php echo ProductUrl::INSTALL_HELP; ?>">support center</a>
					</li>
					<li class="help-phone">
						(866) 646-3868 <span>US Toll Free</span>
					</li>
					<li>
						+1 (619) 795-8459 <span>International</span>
					</li>
				</ul>
			</div>
		</div>
		
		<div id="install-dependencies">
			<noscript>
				<h1>Checking environment... </h1>
				<h1 class="error">Oops, there was a problem with your installation. <a href=".">Retry dependencies</a></h1>
				
				<fieldset>
					<ul class="envCheck">
						<li class="error"><b>Error</b>: JavaScript must be enabled to install MindTouch. Please enable JavaScript.</li>
					</ul>
				</fieldset>
			</noscript>
			
			<?php if ($this->has('dependency.messages')) : ?>
				<div id="environment-messages">
					<?php $this->outputErrors('Checking environment...', 'dependency.messages', true); ?>
				</div>
			<?php endif; ?>
		</div>
		
		<div id="install-contents" class="<?php echo $this->has('input.messages') ? 'input-messages' : ''; ?>" style="display: none; min-height: 100px;">
			<?php if ($this->has('input.messages')) : ?>
				<div id="input-messages">
					<?php $this->outputErrors('Processing installation...', 'input.messages'); ?>
				</div>
			<?php elseif ($this->has('installation.messages')) : ?>
				<div id="installation-messages">
					<?php $this->outputErrors('Processing installation...', 'installation.messages'); ?>
				</div>
			<?php endif; ?>
			
			<?php $this->html('view.contents'); ?>
		</div>
		
		<div id="install-iframes"></div>
	</div>
</body>
</html>
