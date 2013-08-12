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
 
	$Request = DekiRequest::getInstance();
	$notes = $this->get('view.notes'); 
?>
<div class="tabs2">
	<ul>
		<li id="notes"><a class="remote selected">Notes</a></li>
		<li id="compose"><a class="remote short" href="<?php echo $Request->getLocalUrl('note','compose'); ?> ">Compose</a></li>
	</ul>
</div>

<div class="js-main">
	<div class="note">
	<?php if ($this->has('view.notification')): ?>
		<div class="error">
			<?php $this->html('view.notification'); ?>
		</div>
	<?php endif; ?>
	<div class="
		<?php if (sizeof($notes) > 0) : ?> notes 
		<?php else: ?> no-notes <?php endif;?>
	">
	
	<?php foreach ($notes as $note): ?>
		<div class="message action" id="readNote" title="<?php echo $note['url'];?>">
			<div class="date"><?php echo $note['date']; ?></div>
			<div class="from"><?php echo $note['from']; ?></div>
			<div class="line1"><?php echo $note['summary']; ?></div>
		</div>

	<?php endforeach; ?>

	</div>
	</div>
</div>
