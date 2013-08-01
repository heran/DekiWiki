<?php
/**
 * @var $this DekiPluginView
 */
?>
<div id="deki-search-results">
	<?php if (!$this->has('results')) : ?>
		<div class="ui-state-empty">
			<?php
				if ($this->has('href.allNamespaces'))
				{
					echo $this->msg('Page.Search.no-exact-search-results-in-ns', $this->get('results.query'), $this->get('href.allNamespaces'));
				}
				else if ($this->has('href.allLanguages'))
				{
					echo $this->msg('Page.Search.no-exact-search-results-in-lang', $this->get('results.query'), $this->get('href.allLanguages'));
				}
				else
				{
					echo $this->msg('Page.Search.no-exact-search-results', $this->get('results.query'));
				}
			?>
		</div>
	<?php else : ?>
		<?php $this->html('form.header'); ?>
		<div class="results-heading">
			<div class="details">
				<?php echo $this->msg('Page.Search.results.viewing', $this->get('results.start'), $this->get('results.end'), $this->get('results.count')); ?>
			</div>
			<div class="subscribe">
				<a href="<?php $this->html('href.subscribe'); ?>"><?php echo $this->msg('Page.Search.subscribe'); ?></a>
			</div>
		</div>
	
		<ul class="results">
		<?php
			/* @var $Result DekiLoggedSearchResult */
			foreach ($this->get('results') as $Result) : ?>
			<li class="result type-<?php echo $Result->getType(); ?> ns-<?php echo $Result->getPageNamespace('main'); ?>">
				<div class="title">
					<?php echo $Result->getIcon(); ?>
					<?php echo $Result->getTitleLink($Result->getHighlightedUrl($this->get('results.query'))); ?>
					<span class="location">
						<?php echo $Result->getLocation(); ?>
					</span>
				</div>
		
				<div class="meta">
					<span class="info updated" title="<?php echo htmlspecialchars($Result->getLastUpdated()); ?>">
						<?php echo $Result->getLastUpdatedDiff(); ?>
					</span>
					
					<?php if ($Result->getWordCount() > 0): ?>
					-
					<span class="info wordcount">
						<?php echo $Result->getWordCount(); ?>
					</span>
					<?php endif; ?>

					<?php if ($Result->isFileResult()) : ?>
					-
					<span class="info size">
						<?php echo $Result->getSize(); ?>
					</span>
					<?php endif; ?>
				</div>
		
				<div class="preview">
					<?php if ($Result->isCommentResult()): ?>
						<div class="result-comment">
							<?php echo wfMsg('Page.Search.comment', $Result->getUserLink(), nl2br(htmlspecialchars($Result->getTextPreview()))); ?>
						</div>
					<?php else: ?>
						<div>
							<?php $this->view($Result->getTextPreview(125)); ?>
						</div>
					<?php endif; ?>
					
					<?php if ($Result->isFileResult()) : ?>
						<?php $File = DekiFilePreview::newFromId($Result->getId()); ?>
						<?php if (!is_null($File)): ?>
							<?php if ($File->hasPreview()) : ?>
							<div class="image">
								<a href="<?php echo $File->getWebview(); ?>" rel="search" class="lightbox">
									<?php echo $File->getThumbImage(); ?>
								</a>
								<div>
									<?php $this->view($File->getDescription()); ?>
								</div>
							</div>
							<?php else: ?>
							<div>
								<?php $this->view($File->getDescription()); ?>
							</div>
							<?php endif; ?>
						<?php endif; ?>
					<?php endif; ?>
				</div>
				
				<div class="url" title="<?php $this->view($Result->getUrlDisplay()); ?>">
					<?php $this->view($Result->getUrlDisplay()); ?>
				</div>
			</li>
		<?php endforeach; ?>
		</ul>
	
		<?php $this->html('pagination'); ?>
	<?php endif; ?>

	<div class="results-footer">
		<?php $this->html('form.footer'); ?>
		
		<div class="parsed-query">
			<span class="label"><?php echo $this->msg('Page.Search.results.query'); ?></span>
			<span><?php $this->text('results.parsedQuery'); ?></span>
		</div>
	</div>
</div>
