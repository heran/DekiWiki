<script type="text/javascript">var _starttime = new Date().getTime();</script>
<meta http-equiv="Content-Type" content="<?php $this->text('mimetype') ?>; charset=<?php $this->text('charset') ?>" />
<?php $this->html('headlinks') ?>
<title><?php $this->text('pagetitle'); ?></title>

<!-- default css -->
<link rel="stylesheet" type="text/css" media="screen" href="<?php $this->html('pathtpl'); ?>/_reset.css"/>
<?php $this->html('screencss'); ?>
<?php $this->html('printcss'); ?>

<!-- default scripting -->
<?php $this->html('javascript'); ?>
<script type="text/javascript" src="<?php $this->html('pathskin'); ?>/javascript.js"></script>

<!--[if lt IE 7.]>
<script defer type="text/javascript" src="pngfix.js"></script>
<![endif]-->


<!-- specific screen stylesheets-->
<!-- specific screen stylesheets-->
<?php if (!Skin::isPrintPage()) { ?>
<link rel="stylesheet" type="text/css" media="screen" href="<?php $this->html('pathskin'); ?>/css.php"/>
<link rel="stylesheet" type="text/css" media="print" href="<?php $this->html('pathskin'); ?>/_print.css" />   
<link rel="stylesheet" type="text/css" media="print" href="<?php $this->html('pathskin'); ?>/_content.css" />  
<?php } else { ?>
<link rel="stylesheet" type="text/css" media="screen" href="<?php $this->html('pathskin'); ?>/_content.css" />   
<link rel="stylesheet" type="text/css" media="screen" href="<?php $this->html('pathskin'); ?>/_print.css" />    
<link rel="stylesheet" type="text/css" media="print" href="<?php $this->html('pathskin'); ?>/_print.css" />    
<link rel="stylesheet" type="text/css" media="print" href="<?php $this->html('pathskin'); ?>/_content.css" />  
<?php } ?>

<?php $this->html('inlinejavascript'); ?>

<!-- styles overwritten via control panel - load this css last -->	
<?php $this->html('customhead'); ?> 