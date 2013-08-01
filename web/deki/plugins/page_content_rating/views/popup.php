<script type="text/javascript">Deki.PageId = <?php $this->html('pageId') ?>;</script>
<div id="deki-page-rating-comment">
	<form class="special-page-form">
		<div class="deki-page-rating-feedback"><?php echo $this->msg('Page.ContentRating.text.feedback'); ?></div>
		<div class="field">
			  <?php echo DekiForm::singleInput('textarea', 'rating', '', array('tabindex' => '1')); ?>
		</div>
		<div id="footer">
			<div class="buttons-bottom">
				<?php echo DekiForm::singleInput('button', 'cancel', '', array('class' => 'secondary', 'tabindex' => '3'), wfMsg('Page.ContentRating.button.close')); ?>
				<?php echo DekiForm::singleInput('button', 'submit', 'true', array('tabindex' => '2'), wfMsg('Page.ContentRating.button.add')); ?>
			</div>
		</div>
	</form>
</div>
<script type="text/javascript">
// @note kalida: thickbox has a delay before display, so cannot set focus immediately
$(function(){ setTimeout("$('#textarea-rating').focus()", 100); });
</script>
