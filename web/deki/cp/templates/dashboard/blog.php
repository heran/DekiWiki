<?php $this->includeCss('dashboard.css'); ?>
<?php $this->set('template.subtitle', $this->msg('Dashboard.blog.subtitle')); ?>

<?php 
$posts = $this->get('posts');
/* if there are no posts to view */ 
if (empty($posts)): ?>
	<?php echo $this->msg('Dashboard.blog.nonews'); ?>
<?php else: ?>
	<?php foreach ($posts as $post) : ?>	
		<?php
		if (strlen($post['summary']) > 512) 
		{
			$post['summary'] = substr($post['summary'], 0, 509).' [...]';	
		}
		// cut the summary down to size
		$post['summary'] = str_replace('[...]', ' ...', $post['summary']); 
		
		// format the date
		$timestamp = strtotime($post['updated']);
		$datetime = date('F j, Y', $timestamp);
		$month = date('M', $timestamp);
		$day = date('d', $timestamp);
		$year = date('y', $timestamp);
		
		$gravatar_url = 'http://www.gravatar.com/avatar/'.md5(strtolower($post['author_name']).'@mindtouch.com').'?s=60'; 
		?>
		
		<div class="block-blog">
			<div class="icon"><img src="<?php echo $gravatar_url; ?>"/></div>
			<div class="text">
				<h2><a href="<?php echo $post['link']; ?>"><?php echo $post['title'];?></a></h2>
				<p class="content"><?php echo $post['summary']; ?></p>
				<p class="username"><?php echo wfMsg('Dashboard.blog.username', $post['author_name']); ?></p>
				<p class="date"><?php echo wfMsg('Dashboard.blog.date', $datetime); ?></p>
			</div>
		</div>
	<?php endforeach; ?>
<?php endif; ?>
