
<div id="deki-search-results-error">
	<dl class="error">
		<dt class="message">
			<?php $this->text('error.message'); ?>
		</dt>
		<dd class="query">
			<code><?php $this->text('error.query'); ?></code>
		</dd>
	</dl>
	
	<div class="results-footer">
		<?php $this->html('form'); ?>
	</div>
</div>
