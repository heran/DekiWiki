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

<?php
$hasFlash = DekiMessage::hasFlash();
if ($hasFlash) :
?>
<div class="dekiFlash">
	<?php if ($hasFlash) : ?>
		<?php echo DekiMessage::fetchFlash(); ?>
	<?php endif; ?>
</div>
<?php endif; ?>

<?php 
// this should not be used in favor of flash messages
if( $this->has('notification')): ?>
	<div class="error">
		<?php echo $this->get('notification'); ?>
	</div>
<?php endif; ?>

<div class="login">

	<form class="loginForm" method="post" action="<?php echo $Request->getLocalUrl('login', 'ajax/login');?>">
		<input type="hidden" name="returnTo" value="<?php echo $this->get('returnTo'); ?>"/>
		<div class="label">Username</div>
		<input id="text-username" class="input" type="text" name="username"/>

		<div class="label">Password</div>
		<input class="input" type="password" name="password"/>

		<?php if ($this->has('siteList')) : ?>
		<div class="label">Authentication Provider</div>
			<select name="authid">
			<?php $siteList = $this->get('siteList'); ?>
			<?php foreach ($siteList as $site) : ?>
				<option value="<?php echo $site['id'];?>" <?php echo $this->get('defaultId') == $site['id'] ? 'selected="selected"' : ''; ?>>
					<?php echo $site['name']; ?>
				</option>
			<?php endforeach; ?>
			</select>
		<?php endif; ?>
		<button class="login" name="login" type="submit" value="login"> Login </button>
	</form>


</div>
