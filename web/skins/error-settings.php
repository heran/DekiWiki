<?php 
header('HTTP/1.1 500 Internal Server Error');

global $wgSitename, $wgDreamServer, $wgDekiApi, $wgDekiApiKey;
?>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en">

<head>
	<title><?php echo(wfMsg('Page.SettingsNotLoaded.page-title'));?></title>
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
		small 
		{
			color: #555;	
		}
	</style>
</head>
<body>
<div id="wrap">
	<h1><?php echo(wfMsg('Page.SettingsNotLoaded.page-title'));?></h1> 
	<p><?php echo(wfMsg('Page.SettingsNotLoaded.message')); ?></p>
	<p><small><?php echo(wfMsg('Page.SettingsNotLoaded.status', $Result->getStatus())); ?></small></p>
	<?php 
	$status = $Result->getStatus();
	if ($status == 503) 
	{
		$currentApiLocation = $wgDreamServer.'/'.$wgDekiApi;
		echo('<p><small>'.wfMsg('Page.SettingsNotLoaded.tryapi').'</small></p>'
			.'<blockquote><small>'.wfMsg('Page.SettingsNotLoaded.apilocation', $currentApiLocation).'<br/>');
		
		//these are usually the default locations to try
		$try = array('http://localhost:8081/deki', 'http://'.$_SERVER['HTTP_HOST'].'/@api/deki');
		foreach ($try as $location) 
		{
			if (strcmp($currentApiLocation, $location) == 0) 
			{
				continue;
			}
			$wgDekiPlug = new Plug($location);
			$result = $wgDekiPlug->At('users', 'current')->Get();
			if ($result['status'] == 200) 
			{
				echo(wfMsg('Page.SettingsNotLoaded.tryapi-success', $location).'<br/>');	
			}
			else 
			{
				echo(wfMsg('Page.SettingsNotLoaded.tryapi-failure', $location, wfArrayVal($result, 'status', 0)).'<br/>');	
			}
		}
		echo('</small></blockquote>');
	}
	else { 
		$errorShown = false;
		
		//thrown by dekihost
		$message = $Result->getVal('body/error/message'); 
		if (!is_null($message)) 
		{
			$errorShown = true;
			echo('<p><small>'.wfMsg('Page.SettingsNotLoaded.apimessage', $message).'</small></p>');
		}
		
		//dream exception
		$message = $Result->getVal('body/exception');
		if (!is_null($message)) 
		{
			$errorShown = true;
			$stack = $Result->getAll($message, 'stacktrace/frame');
			echo('<p><small>'.wfMsg('Page.SettingsNotLoaded.dreamexception', $message['message'], $message['source'], implode('<br/>', $stack)).'</small></p>');
		}

		// curl error
		$message = $Result->getVal('error');
		if (!$errorShown && !is_null($message))
		{
			echo('<p><small>'. htmlspecialchars($message) .'</small></p>');
		}
	}
	?>
	
	<?php 
		if (isset($_POST['wpTextbox1'])) 
		{ 
			echo('<p>'. wfMsg('Page.SettingsNotLoaded.user-was-editing-message') .'</p>');
			echo('<p><textarea style="width: 100%; height: 200px; font: 11px Verdana;">'.htmlentities($_POST['wpTextbox1']).'</textarea></p>');
		}
	?>
</div>
</body>
</html>
