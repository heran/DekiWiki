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
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN"
"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html>
<head >
	<title> <?php $this->html('head.title'); ?> </title>

	<meta id="viewport" name="viewport" content="width=320; initial-scale=1.0; maximum-scale=1.0; user-scalable=0;" />
	<meta http-equiv="Content-Type" content="text/html; charset=utf-8" />

	<link rel="apple-touch-icon" href="/assets/images/apple-touch-icon.png">

	<link rel="stylesheet" type="text/css" href="/assets/reset.css" /> 
	<link rel="stylesheet" type="text/css" href="/assets/common.css" />
	<link rel="stylesheet" type="text/css" href="/assets/content.css" /> 
	<link rel="stylesheet" type="text/css" href="/assets/action.css" /> 

	<?php foreach ($this->getCssIncludes() as $cssfile) : ?>
	<link rel="stylesheet" type="text/css" href="/assets/<?php echo $cssfile; ?>" />
	<?php endforeach; ?>

	<script type="text/javascript" src="/assets/js/jquery-1.2.6.min.js"></script>
	<script type="text/javascript" src="/assets/js/jquery.form.js"></script>
	<script type="text/javascript" src="/assets/js/jQuery.jCache.js"></script>
	<script type="text/javascript" src="/assets/js/common.js"></script>
	<?php foreach ($this->getJavascriptIncludes() as $jsfile) : ?>
		<script type="text/javascript" src="./assets/<?php echo $jsfile; ?>"></script>
	<?php endforeach; ?>
</head>
<body>
<?php
$Request = DekiRequest::getInstance();
$User = DekiUser::getCurrent();
?>

<?php if (!$this->get('disableNav')) : ?>

	<div class="navtop">
		<form method="post" name="userlogin" class="js-login-user" action="login.php">
			<?php echo DekiForm::singleInput('hidden', 'notification', 'You must login to use that feature.'); ?>
			<?php echo DekiForm::singleInput('hidden', 'returnTo', 'userpage'); ?>
		</form>
		<form method="post" name="js-login-mail"  class="js-login-mail" action="login.php">
			<?php echo DekiForm::singleInput('hidden', 'notification', 'You must login to use that feature.'); ?>
			<?php echo DekiForm::singleInput('hidden', 'returnTo', $Request->getLocalUrl('note')); ?>
		</form>

		<div class="mail <?php $this->html('selectMailPage'); ?> <?php if ($User->isAnonymous()) :?> disabled <?php endif;?>">
			<?php if ($User->isAnonymous()) : ?>
				<a href="#" name="mail" class="disabled"></a>
			<?php else : ?>
				<a href="<?php echo $Request->getLocalUrl('note'); ?>"></a>
			<?php endif; ?>
		</div>
		<div class="user <?php $this->html('selectUserPage'); ?><?php if ($User->isAnonymous()) :?> disabled <?php endif;?>">
			<?php if ($User->isAnonymous()) : ?>
				<a href="#" name="user" class="disabled"></a>
			<?php else : ?>
				<a href="<?php echo $Request->getLocalUrl('page', null, array('title' => 'User:'.$User->getUsername())); ?>"></a>
			<?php endif; ?>
		</div>

		<div class="logo"><a href="/"></a></div>
	</div>

<?php else : ?>

	<div class="logo"><a href="/"></a></div>

<?php endif; ?>


	<div class="js-view">
		<?php $this->html('view.contents'); ?>
	</div>

<?php if (!$this->get('disableNav')) : ?>
	<div class="navbottom">
		<div class="buttons">
			<form <?php if(!$User->isAnonymous()):?> class="logoutForm" <?php endif; ?> method="post" action="<?php echo (!$User->isAnonymous() ? $Request->getLocalUrl('login', 'ajax/logout'): $Request->getLocalUrl('login')); ?>"> 
				<input type="hidden" name="returnTo" value="<?php echo $this->get('pageUrl'); ?>" />
				
				<?php if ($User->isAnonymous()) : ?>
					<button type="submit">Log in</button>
				<?php else : ?>
					<input type="hidden" name="logout" value="logout" />
					<button type="submit">Log out</button>
				<?php endif; ?>

			</form>
		</div>
		<div class="copyright">
			Copyright &copy; 2008 MindTouch, Inc.
		</div>
	</div>
<?php endif; ?>

</body>
</html>
