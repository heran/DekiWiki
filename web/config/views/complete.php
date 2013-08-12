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
						<td class="index completed first step1"><span>1</span></td>
						<td class="title completed first step1">Installation Type</td>
						<td class="index completed step2"><span>2</span></td>
						<td class="title completed step2">Site setup</td>
						<td class="index completed step3"><span>3</span></td>
						<td class="title completed step3">Configuration</td>
						<td class="index completed step4"><span>4</span></td>
						<td class="title completed step4">Your Organization</td>
						<td class="index completed step5"><span>5</span></td>
						<td class="title completed step5">Confirmation</td>
						<td class="index active step6"><span>6</span></td>
						<td class="title active step6">Installation</td>
						<td class="index afterActive last step7"><span>7</span></td>
						<td class="title afterActive last step7">Activation</td>
					<?php else : ?>
						<td class="index completed first step1"><span>1</span></td>
						<td class="title completed first step1">Installation Type</td>
						<td class="index completed step2"><span>2</span></td>
						<td class="title completed step2">Site setup</td>
						<td class="index completed step4"><span>3</span></td>
						<td class="title completed step4">Your Organization</td>
						<td class="index completed step5"><span>4</span></td>
						<td class="title completed step5">Confirmation</td>
						<td class="index active step6"><span>5</span></td>
						<td class="title active step6">Installation</td>
						<td class="index afterActive last step7"><span>6</span></td>
						<td class="title afterActive last step7">Activation</td>
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
						Visit our <a href="<?php ProductURL::INSTALL_HELP; ?>">support center</a>
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
		
		<div id="install-contents" class="install-complete">
			<?php if ($this->has('complete.messages')) : ?>
				<div id="complete-messages">
					<?php $this->outputErrors('Installation complete', 'complete.messages', null); ?>
				</div>			
			<?php endif; ?>
			
			<?php $this->html('view.contents'); ?>
		</div>
		
		<div id="install-iframes"></div>
	</div>
</body>
</html>
