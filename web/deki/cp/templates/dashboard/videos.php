<?php $this->includeCss('dashboard.css'); ?>

<?php 
$columns = 2; 
$col_count = floor(count($this->get('videos')) / $columns); 
$i = 0;
?>
<div class="videos">
<?php foreach ($this->get('videos') as $video) : ?>
	<?php
		$i++;
		if ($i > $col_count) {
			echo('</div><div class="videos">');
			$i = 0;
		}
	?>
	<div class="video">
		<a class="thumb" href="<?php echo $video['link']; ?>">
			<?php echo $video['thumb']; ?>
		
		</a>
		<div class="text">
			<a class="title" href="<?php echo $video['link']; ?>">
				<?php echo $video['title']; ?>
			</a>
			<span class="date"><?php echo date('M d, Y', strtotime($video['date.published'])); ?></span>
		</div>
	</div>
<?php endforeach; ?>
</div>

<div class="clear"></div>