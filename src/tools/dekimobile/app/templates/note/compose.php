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
 
 $Request = DekiRequest::getInstance(); ?>

<div class="tabs2">
	<ul>
		<li id="notes"><a class="remote short" href="<?php echo $Request->getLocalUrl('note'); ?> ">Notes</a></li>
		<li id="compose"><a class="remote selected" >Compose</a></li>
	</ul>
</div>

<div class="js-main">

<?php if ($this->has('view.recv.note')) : ?>
	<?php $note = $this->get('view.recv.note'); ?>
	<div class="message-reply">
		<div class="date"> <?php echo $note['date'] ?> </div>
		<div class="from"> <?php echo $note['from'];?> </div>
		<div class="text"><?php echo $note['text']; ?></div>
	</div>
<?php endif; ?>

<div class="compose <?php echo ($this->has('view.recipient') ? 'input' : ''); ?>">
	<form class="noteForm" method="post" action="<?php echo $Request->getLocalUrl('note', 'ajax/send');?>">
		<?php if(!$this->has('view.recipient')): ?>
			<p>Enter a username or email address below. </p>
			<div> To </div>
		<?php endif;?>
		<input id="text-username" class="to" name="to" type="<?php  echo ($this->has('view.recv.note') ? 'hidden' :  'text') ?>" value="<?php echo $this->get('view.recipient'); ?>"></input>
		<div>
			<?php if ($this->get('view.recipient')): ?>
				Reply 
			<?php else: ?>
				Note 
			<?php endif; ?>
		</div> 
		<textarea name="message"><?php echo $this->get('view.send.note'); ?></textarea>
		<button type="submit">Send</button>
		<?php if ($this->has('form.cancelUrl')) : ?>
			<a class="cancel" href="<?php $this->html('form.cancelUrl'); ?>">Cancel</a>
		<?php endif; ?>

	</form>
</div>

</div>
