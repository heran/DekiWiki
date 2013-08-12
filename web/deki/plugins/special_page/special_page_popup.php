<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="<?php $this->text('lang'); ?>" lang="<?php $this->text('lang'); ?>" dir="<?php $this->text('dir'); ?>">
<head>
	<title> <?php $this->text('head.title'); ?> </title>
	<meta http-equiv="Content-Type" content="<?php $this->text('head.mimetype'); ?>; charset=<?php $this->text('head.charset'); ?>" />
	<link rel="stylesheet" type="text/css" media="screen" href="<?php $this->html('head.commonpath'); ?>/reset.css" />
	<link rel="stylesheet" type="text/css" media="screen" href="/skins/common/fonts.css" />
	<link rel="stylesheet" type="text/css" media="screen" href="/deki/plugins/special_page/special_page_form.css" />
	<link rel="stylesheet" type="text/css" media="screen" href="/deki/plugins/special_page/special_page_popup.css" />
	<link rel="stylesheet" type="text/css" media="screen" href="/skins/common/messaging.css" />
	<?php if ($this->has('head.includes.css')) { $this->html('head.includes.css'); } ?>
	<?php if ($this->has('head.includes.javascript')) { $this->html('head.includes.javascript'); } ?>
	
	<script type="text/javascript" src="/skins/common/js.php"></script>
	<script type="text/javascript" src="/deki/plugins/special_page/special_page_popup.js"></script>
	<script type="text/javascript">
		<?php $this->html('head.javascript'); ?>
	</script>
</head>
<body id="SpecialPagePopup" class="page-special-popup">
	<?php $this->html('messages'); ?>
	<div id="container">
		<div id="container-wrap">
			<?php $this->html('contents'); ?>
		</div>
	</div>
</body>
</html>
