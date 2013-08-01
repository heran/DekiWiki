<form class="special-page-form">
<?php $this->html('selector.embed'); ?>

<div id="footer">
	<div class="deki-pagetemplates-footer-content">
		<div id="deki-pagetemplates-help">
			<?php $this->html('templates.help'); ?>
		</div>
		
		<a href="<?php $this->html('templates.baseuri'); ?>" id="deki-pagetemplates-create" target="_parent" class="button">
			<?php echo $this->msg('Page.PageTemplateSelector.label.create'); ?>
		</a>
	</div>
</div>
</form>

<script type="text/javascript">
// @note kalida: thickbox has a delay before display, so cannot set focus immediately
$(function(){
	setTimeout("$('#deki-pagetemplates-create').focus()", 100);
	
	// Wire the create link to trigger the default action
	$('#deki-pagetemplates-create').click(function() {
		Deki.Plugin.PageTemplateSelector.DefaultAction();
		return false;
	});
});
</script>
