<?php
/** @var $this DekiView */
$this->includeCss('customize.css');
$this->includeJavascript('jquery.scrollTo.js');
$this->includeJavascript('customize.js');

?>

<form method="post" enctype="multipart/form-data" action="<?php $this->html('form.action'); ?>" class="logo">
	<div class="setlogo">
		<div class="title">
			<h3><?php echo($this->msg('Skinning.logo'));?></h3>
		</div>
		
		<div class="preview">
			<?php echo($this->msg('Skinning.logo.current'));?>
			<div class="logopreview" style="width: <?php $this->html('logo-maxwidth');?>px; height: <?php $this->html('logo-maxheight');?>px;">
				<span class="dimensions" style="width: <?php $this->html('logo-maxwidth');?>px; height: <?php $this->html('logo-maxheight');?>px;">
					<span><?php echo($this->msg('Skinning.logo.dimensions', $this->get('logo-maxwidth'), $this->get('logo-maxheight')));?></span>
				</span>
				<?php $this->html('logo'); ?>
			</div>
		</div>
		
		<div class="field">
			<div class="file">
				<?php echo DekiForm::singleInput('file', 'logo'); ?>
			</div>
			<small><?php echo($this->msg('Skinning.logo.description', $this->get('logo-maxwidth'), $this->get('logo-maxheight')));?></small>
			<div class="submit">
				<?php echo DekiForm::singleInput('button', 'submit', 'upload', array(), $this->msg('Skinning.logo.button')); ?>
				<?php echo DekiForm::singleInput('button', 'submit', 'default', array('class' => 'gray'), $this->msg('Skinning.logo.default')); ?>
			</div>
			<?php DekiForm::singleInput('hidden', 'action', 'logo'); ?>
		</div>
	</div>
</form>
