<?php
/* @var $this DekiInstallerView */
?>

<div id="form-confirm" class="confirm">
<h1>Confirm Setup</h1>
<div class="install-type">
	<h2 class="first">Install Type <a href="#step1" class="edit" rel="1">Edit</a></h2>
	<?php if ($this->get('showCommercial')) : ?>
	<div class="product product-commercial">
		<div class="product-tab"></div>
		<div class="install">
			<a href="#" rel="commercial" class="button">Install</a>
		</div>
		<div class="details">
			<h2>MindTouch Technical Communications Suite</h2>
			<div class="description">
				The social layer to your technical documentation.
			</div>
			<div class="features">
				Community scoring, adapative search, self-organized content, curation...
				<a href="<?php echo ProductURL::MINDTOUCH_TCS; ?>" class="external about" target="_blank">read more</a>
			</div>
		</div>
	</div>
	<?php endif; ?>
	
	<div class="product product-platform">
		<div class="product-tab"></div>
		<div class="install">
			<a href="#" rel="platform" class="button">Install</a>
		</div>
		<div class="details">
			<h2>MindTouch Platform</h2>
			<div class="description">
				The social layer to your enterprise systems and applications.
			</div>
			<div class="features">
				Desktop Suite, enterprise connectors, commercially supported...
				<a href="<?php echo ProductURL::MINDTOUCH_PLATFORM; ?>" class="external about" target="_blank">read more</a>
			</div>
		</div>
	</div>
	
	<div class="product product-core">
		<div class="product-tab"></div>
		<div class="install">
			<a href="#" rel="core" class="button">Install</a>
		</div>
		<div class="details">
			<h2>MindTouch Core</h2>
			<div class="description">
				One of the most popular open source projects in the world.
			</div>
			<div class="features">
				Intuitive wiki, development platform, web services framework, standards compliant...
				<a href="<?php echo ProductURL::MINDTOUCH_CORE; ?>" class="external about" target="_blank">read more</a>
			</div>
		</div>
	</div>
</div>
 

<fieldset class="confirm">
	<h2 class="first">Site Info <a href="#step2" class="edit" rel="2">Edit</a></h2> 
	<div class="table">
		<?php $this->inputConfirm('Sitename', 'Page.Install.form-sitename'); ?>
		<?php $this->inputConfirm('SysopEmail', 'Page.Install.form-adminmail'); ?>
		<?php $this->inputConfirm('SiteLang', 'Page.Install.form-localization'); ?>
	</div> 
 
	<h2>Admin Info <a href="#step2" class="edit" rel="2">Edit</a></h2> 
	<div class="table">
		<?php $this->inputConfirm('RegistrarFirstName', 'Page.Install.form-admin-first'); ?>
		<?php $this->inputConfirm('RegistrarLastName', 'Page.Install.form-admin-last'); ?>
		<?php $this->inputConfirm('RegistrarPhone', 'Page.Install.form-adminphone'); ?>
		<?php $this->inputConfirm('SysopName', 'Page.Install.form-adminname'); ?>
		<?php //$this->inputPassword('SysopPass', 'Page.Install.form-adminpwd'); ?>
		<?php //$this->inputPassword('SysopPass2', 'Page.Install.form-adminpwd2'); ?>
	</div> 

 	<?php if ($this->get('env.showConfiguration')) : ?>
	<h2>Database Configuration <a href="#step3" class="edit" rel="3">Edit</a></h2> 
	<div class="table">
		<?php $this->inputConfirm('DBserver', 'Page.Install.form-dbhost'); ?>
		<?php $this->inputConfirm('DBname', 'Page.Install.form-dbname'); ?>
		<?php $this->inputConfirm('DBuser', 'Page.Install.form-dbuser'); ?>	
	</div> 
	
	<h2>Existing MySQL credentials <a href="#step3" class="edit" rel="3">Edit</a></h2> 
	<div class="table">
		<?php $this->inputConfirm('RootUser', 'Page.Install.form-dbsu-name'); ?>
		<?php $this->inputConfirm('RootPW', 'Page.Install.form-dbsu-pwd'); ?>
	</div> 
 
	 	<?php if ($this->get('env.showAdvancedConfiguration')) : ?>
			<h2>Advanced Configuration <a href="#step3" class="edit" rel="3">Edit</a></h2> 
			<div class="table"> 
				<?php $this->inputConfirm('Mono', 'Page.Install.form-adv-mono'); ?>
				<?php $this->inputConfirm('ImageMagickConvert', 'Page.Install.form-adv-convert'); ?>
				<?php $this->inputConfirm('ImageMagickIdentify', 'Page.Install.form-adv-identify'); ?>
				<?php $this->inputConfirm('prince', 'Page.Install.form-adv-prince'); ?>
			</div>
		<?php endif; ?>
	<?php endif; ?>
	
	<div class="installation-warning">
		Once you click install you will not be able to cancel the installation process. Be sure to double check the information above for accuracy before proceeding.
	</div>

 	<div class="navButtons">
		<div class="backButton"> 
			<a href="#">Back</a>
		</div>
		<div class="submit">
			<input id="installButton" type="submit" value="Install MindTouch" class="installButton" /> 
		</div>
	</div>
</fieldset>
<?php // end #form-confirm ?>
</div>

<script type="text/javascript">
$(function() {
	$('#form-confirm a.edit').click(function(e) {
		var step = $(this).attr('rel');
		MT.log('Returning to step ' + step);
		MT.Install.GotoStep(step);
		return false;
	});

	// add a loading state to the submit button
	$('#installButton').click(function() {
		var $this = $(this);
		if ($this.hasClass('disabled'))
			return false;
		
		MT.log('User clicked the button to install!');
		$this.addClass('disabled').parent().addClass('loading');
	});

	// @TODO: stop refreshing on each step change
	MT.Install.OnStep('last', updateFields);
	updateFields();

	function updateFields() {
		
		// set the product type
		$('#form-confirm .product').hide();
		var type = $('#hidden-SelectedEdition').val();
		$('#form-confirm .product-'+type).show();
		
		// update all the confirmation fields
		$('#form-confirm .confirm').each(function(i, el) {
			var $this = $(this);
			var name = String($this.attr('name')).substring(1); // -
			var value = $('[name='+name+']').val();
			$this.val(value);
		});		
	};
});
</script>
