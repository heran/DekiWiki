<?php
/**
 * Canvas.php
 * This is a simplified template which doesn't have the standard control panel markup
 */
?>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">

<head>
	<title><?php echo $this->msg('Common.title', DekiSite::getName()); ?></title>
	<meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
	<link rel="stylesheet" type="text/css" href="./assets/reset.css" />
	<link rel="stylesheet" type="text/css" href="./assets/common.css" />
	<?php foreach ($this->getCssIncludes() as $cssfile) { ?>
		<link rel="stylesheet" type="text/css" href="./assets/<?php echo $cssfile; ?>" />
	<?php } ?>
	
    <!--[if IE 6]>
    	<link rel="stylesheet" type="text/css" href="./assets/ie6.css" />
    <![endif]-->
    
	<script type="text/javascript" src="/skins/common/jquery/jquery.min.js"></script>
	<script type="text/javascript" src="/skins/common/jquery/jquery.plugins.js"></script>
	<script type="text/javascript" src="./assets/common.js"></script>
	<?php foreach ($this->getJavascriptIncludes() as $jsfile) { ?>
		<script type="text/javascript" src="./assets/<?php echo $jsfile; ?>"></script>
	<?php } ?>
</head>
<body id="<?php echo($this->get('controller.name'));?>">

<div class="canvas">
	<?php $this->html('view.contents'); ?>
</div>

</body>
</html>
