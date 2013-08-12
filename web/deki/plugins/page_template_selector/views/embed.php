<style type="text/css">#deki-pagetemplates-embed.loading { display: none; }</style>
<div id="deki-pagetemplates-embed" class="loading">
	<ul id="deki-pagetemplates-layouts">
		<li class="page-item page-item-default">
				<?php $this->html('templates.default'); ?>
		</li>
	<?php $templates = $this->get('templates.rendered'); ?>
	<?php foreach ($templates as $template) : ?>
			<li class="page-item">
				<?php echo $template; ?>
			</li>
		<?php endforeach; ?>

		<?php for ($i = count($templates); $i < 8; $i++) : ?>
			<li class="page-item-empty"></li>
		<?php endfor; ?>
	</ul>
	<div id="deki-pagetemplates-message">
			<?php $this->html('templates.available'); ?>
	</div>
</div>
<input type="hidden" name="templatepath" id="deki-pagetemplates-templatepath" />
