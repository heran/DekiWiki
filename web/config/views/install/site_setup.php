<?php
/* @var $this DekiInstallerView */
?>

<h1>Site Setup</h1>
<fieldset id="form-site-setup"> 
	<h2 class="first">Site Info</h2>
	<div class="table">	
		<?php $this->inputText('Sitename', 'Page.Install.form-sitename', 'Page.Install.form-sitename-desc'); ?>
		<?php $this->inputText('SysopEmail', 'Page.Install.form-adminmail', 'Page.Install.form-adminmail-desc'); ?>
		<?php
			$languages = wfAvailableResourcesLanguages();
			$this->inputOption('SiteLang', $languages, 'Page.Install.form-localization');
		?>
	</div>
	 
	<h2>Admin Info</h2>
	<div class="table">
		<?php $this->inputText('RegistrarFirstName', 'Page.Install.form-admin-first'); ?>
		<?php $this->inputText('RegistrarLastName', 'Page.Install.form-admin-last'); ?>
		<?php $this->inputText('RegistrarPhone', 'Page.Install.form-adminphone'); ?>
		<?php $this->inputPassword('SysopPass', 'Page.Install.form-adminpwd'); ?>
		<?php $this->inputPassword('SysopPass2', 'Page.Install.form-adminpwd2'); ?>
	</div>

	<div class="navButtons">
		<div class="backButton"> 
			<a href="#">Back</a>
		</div>
		<div class="nextButton"> 
			<input type="button" value="Next" class="submit_form"> 
		</div>
	</div>
</fieldset> 
