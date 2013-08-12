<?php 
global $wgSitename;
?>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en">

<head>
	<title><?php echo(wfMsg('Page.SiteDown.page-title'));?></title>
	<style type="text/css">
		body {
			padding: 0;
			margin: 0;	
			font-family: "Lucida Grande", Tahoma, Verdana, Arial, Sans-Serif;
			background-color: #D3DDE7;
		}
		#wrap {
			background-color: #fff;
			margin: 60px 0 0 0;
			padding-left: 90px;
			padding: 8px 8px 8px 90px;
		}
		h1 {
			font-size: 24px;
			font-weight: normal;
			padding: 0;
			margin: 0;
			width: 480px;
		}
		p {
			padding-left: 0;
			margin-left: 0;
			width: 480px;
		}
	</style>
</head>
<body>
<div id="wrap">
	<h1><?php echo(wfMsg('Page.SiteDown.page-title'));?></h1> 
	<p><?php echo(wfMsg('Page.SiteDown.message')); ?></p>
	
	<?php 
		if (isset($_POST['wpTextbox1'])) { 
			echo('<p>'. wfMsg('Page.SiteDown.user-was-editing-message') .'</p>');
			echo('<p><textarea style="width: 100%; height: 200px; font: 11px Verdana;">'.htmlentities($_POST['wpTextbox1']).'</textarea></p>');
		}
	?>
</div>
</body>
</html>
