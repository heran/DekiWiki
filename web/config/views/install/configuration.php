<?php
/* @var $this DekiInstallerView */
?>

<h1>Configuration</h1>
<fieldset id="form-config"> 
	<h2 class="first">Database Configuration</h2> 
	<div class="table"> 
		<?php $this->inputText('DBserver', 'Page.Install.form-dbhost', 'Page.Install.form-dbhost-desc'); ?>
		<?php $this->inputText('DBname', 'Page.Install.form-dbname', 'Page.Install.form-dbname-desc'); ?>
		<?php $this->inputText('DBuser', 'Page.Install.form-dbuser', 'Page.Install.form-dbuser-desc'); ?>
	</div> 
	
	<h2>Existing MySQL credentials</h2> 	
	<div class="table">
		<div class="existing">
			In order to install, MindTouch requires a MySQL user who can create database, stored procedures, and MySQL users. Usually this user is <tt>root</tt>.
			These credentials will not be stored - they will only be used during installation.
		</div>
		<?php $this->inputText('RootUser', 'Page.Install.form-dbsu-name'); ?>
		<?php $this->inputPassword('RootPW', 'Page.Install.form-dbsu-pwd'); ?>
	</div> 
 	
 	<?php if ($this->get('env.showAdvancedConfiguration')) : ?>
	<h2>Advanced Configuration</h2>
	<div class="table">
		<?php $this->inputText('Mono', 'Page.Install.form-adv-mono', 'Page.Install.form-adv-mono-desc'); ?>
		<?php $this->inputText('ImageMagickConvert', 'Page.Install.form-adv-convert', 'Page.Install.form-adv-convert-desc'); ?>
		<?php $this->inputText('ImageMagickIdentify', 'Page.Install.form-adv-identify', 'Page.Install.form-adv-identify-desc'); ?>
		<?php $this->inputText('prince', 'Page.Install.form-adv-prince', 'Page.Install.form-adv-prince-desc'); ?>
	</div>
	<?php endif; ?>
 
	<div class="navButtons">
		<div class="backButton">
			<a href="#">Back</a>
		</div>
		<div class="nextButton">
			<input type="button" value="Next" class="submit_form">
		</div>
	</div>
</fieldset>
