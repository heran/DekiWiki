<?php if($this->get('form.advanced')): ?>
	<div class="deki-advanced-search-form" <?php if(!$this->get('form.advanced.show')): ?>style="display: none;"<?php endif; ?>>
		<form method="get" action="<?php echo $this->html('form.action'); ?>">
			<h3>Find results that have...</h3>
			<div class="field">
				<?php echo DekiForm::singleInput('text', 'all', null, array('class' => 'long'), $this->msg('Page.Search.form.advanced.all')); ?>
			</div>

			<div class="field">
				<?php echo DekiForm::singleInput('text', 'exact', null, array('class' => 'long'), $this->msg('Page.Search.form.advanced.exact')); ?>
			</div>

			<div class="field">
				<?php echo DekiForm::singleInput('text', 'any', null, array('class' => 'short'), $this->msg('Page.Search.form.advanced.any')); ?>
			</div>
			<div class="field">
				<?php echo DekiForm::singleInput('text', 'tags', null, array('class' => 'long'), $this->msg('Page.Search.form.advanced.tag')); ?>
			</div>
			<div class="clear"></div>

			<h3>But also...</h3>
			<div class="field">
				<?php echo DekiForm::singleInput('text', 'notwords', null, array('class' => 'long'), $this->msg('Page.Search.form.advanced.notwords')); ?>
			</div>
			<div class="field">
				<?php echo DekiForm::singleInput('text', 'author', null, array('class' => 'short'), $this->msg('Page.Search.form.advanced.author')); ?>
			</div>
			<div class="field">
				<?php echo DekiForm::multipleInput('select', 'type', $this->get('form.options.type'), null, null, $this->msg('Page.Search.form.advanced.type')); ?>
			</div>
			<?php if ($this->has('form.languages')) : ?>
				<div class="field">
					<?php echo DekiForm::multipleInput('select', 'language', $this->get('form.languages'), $this->get('form.language'), null, $this->msg('Page.Search.form.advanced.language')); ?>
				</div>
			<?php endif; ?>
			<div class="field">
				<?php echo DekiForm::multipleInput('select', 'ns', $this->get('form.namespaces'), $this->get('form.namespace'), null, $this->msg('Page.Search.form.advanced.namespace')); ?>
			</div>
			<div class="clear"></div>
			<hr />
			<input type="submit" value="<?php echo $this->msg('Page.Search.submit-search'); ?>" />
		</form>
	</div>
<?php endif; ?>

<div class="deki-search-form">
	<?php if($this->get('form.advanced.toggle')): ?>
			<a href="#" id="deki-advanced-toggle"><?php echo $this->msg('Dialog.JS.advanced-search') ?></a>
	<?php endif ?>
	<form method="get" action="<?php $this->html('form.action'); ?>">
		<div class="inputs" <?php if($this->get('form.advanced.show')): ?>style="display: none;"<?php endif; ?>>
			<?php if ($this->has('form.queryId')) : ?>
				<?php echo DekiForm::singleInput('hidden', 'qid', $this->get('form.queryId')); ?>
			<?php endif; ?>
			<?php echo DekiForm::singleInput('text', 'search', null, array('autocomplete' => true)); ?>

			<input type="submit" value="<?php echo $this->msg('Page.Search.submit-search'); ?>" />
		</div>
		<div class="clear"></div>
		<?php if ($this->get('form.filters')) : ?>
		<div class="filters">
			
			<?php if ($this->has('commercial')): ?>
				<span class="sort"><?php echo $this->msg('Page.Search.sort'); ?></span>
				<ul class="sortby">
					<li class="<?php $this->html('sort.ranking'); ?>">
						<a href="<?php $this->html('href.sort.ranking'); ?>" <?php echo !$this->has('commercial') ? 'class="disabled-commercial"': ''; ?>><?php echo $this->msg('Page.Search.sort.ranking'); ?></a>
					</li>
					
					<li class="<?php $this->html('sort.title'); ?>">
						<a href="<?php $this->html('href.sort.title'); ?>" <?php echo !$this->has('commercial') ? 'class="disabled-commercial"': ''; ?>><?php echo $this->msg('Page.Search.sort.title'); ?></a>
					</li>
					<li class="<?php $this->html('sort.modified'); ?>">
						<a href="<?php $this->html('href.sort.modified'); ?>" <?php echo !$this->has('commercial') ? 'class="disabled-commercial"': ''; ?>><?php echo $this->msg('Page.Search.sort.modified'); ?></a>
					</li>
				</ul>
			<?php elseif ($this->has('commercial.messaging')) : ?>
				<div class="results-ranking">
					<a href="<?php echo $this->get('commercial.url'); ?>"><?php echo $this->msg('Page.Search.commercial'); ?></a>
				</div>
			<?php endif; ?>
		</div>
		<?php endif; ?>
	</form>
</div>
