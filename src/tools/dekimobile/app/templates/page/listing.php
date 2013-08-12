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
		<?php $this->html('nav.top.sub'); ?>
	</div>

	<div class="tabs">
		<?php $this->html('nav.tabs'); ?>
	</div>

	<div class="js-main">
		<?php $this->html('content'); ?>
		<?php if (!$this->has('isRestricted')) : ?>
			<?php $this->html('share'); ?>
			<?php $this->html('comments'); ?>
			<?php $this->html('comments-input'); ?>
		<?php endif; ?>
	</div>