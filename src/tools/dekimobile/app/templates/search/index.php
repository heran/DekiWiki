<?php
/*
 * MindTouch Deki - enterprise collaboration and integration platform
 * Copyright (C) 2006-2008 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
 * please review the licensing section.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 * http://www.gnu.org/copyleft/gpl.html
 */
?>
<div class="subnav">
	<form method="get" action="<?php $this->html('form.action'); ?>">
		<input type="search" name="<?php $this->html('form.search.name'); ?>" class="search" value="<?php $this->text('form.search'); ?>" />
		<button type="submit" class="search">Go</button>
	</form>
</div>

<?php if ($this->has('search.results')) : ?>

	<?php $this->html('search.pagination'); ?>

	<div class="result">
		<?php foreach ($this->get('search.results') as $result) : ?>
			<div class="container <?php echo $result['class']; ?>" title="<?php echo $result['url']; ?>">
				<div class="title">
					<a href="<?php echo $result['url']; ?>"><?php echo $result['title']; ?></a>
				</div>
				<div class="preview">
					<?php echo $result['preview']; ?>
				</div>
			</div>
		<?php endforeach; ?>
	</div>
	
	<?php $this->html('search.pagination'); ?>

<?php elseif ($this->has('form.search')) : ?>

	<div class="no-results">There are no search results for "<?php $this->text('form.search'); ?>" </div>

<?php else : ?>

	<h2 class="">
		Please enter a search term above.
	</h2>

<?php endif; ?>
