<?php
/* @var $this DekiPluginView */
?>
<style type="text/css">
#announcement-banner {
	background-color: #dfdfdf;
	border-bottom: solid 1px #ccc;
	padding: 0 10px 5px;
}
#announcement-banner.important span {
	display: block;
	padding: 8px;
	
	border: solid 2px #ff0000;
	border-top: none;
	-moz-border-radius: 0 0 8px 8px;
	border-radius: 0 0 8px 8px;
	
	text-align: center;
	font-weight: bold;
	color: #ff0000;
	background-color: #ffffcc;
}
#announcement-banner.beta span {
	padding: 8px 40px;
	background: #ffffcc url(/deki/plugins/site_announcement/assets/badge-beta.png) no-repeat right top;
}
</style>
<div id="announcement-banner" class="important beta">
	<span><?php $this->text('announce'); ?></span>
</div>
