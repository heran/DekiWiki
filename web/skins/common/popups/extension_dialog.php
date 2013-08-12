<?php
/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com oss@mindtouch.com
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

define( 'MINDTOUCH_DEKI', true );

$dekiRoot = '../../../';
require_once($dekiRoot . 'includes/Defines.php');
require_once($dekiRoot . 'LocalSettings.php');
require_once($dekiRoot . 'includes/Setup.php');


$result = $wgDekiPlug->At("site", "functions")->With('format','xml')->Get();
if ($result['status'] != Plug::HTTPSUCCESS)
{
	$error = &$result['body']['error'];
?>
<?php echo '<?xml version="1.0" encoding="UTF-8"?>' . "\n"; ?>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en" dir="ltr">
	<head>
		<meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
		<title><?php printf('%s %s', $error['status'], $error['title']); ?></title>
	</head>
	<body>
		<p>
			<?php echo ucfirst($error['message']); ?>
		</p>
	</body>
</html>
<?php
	exit();
	// end execution
}


/*
 * Generate the extension list html
 */
$listHtml = '';

$libraries = array();
if (isset($result['body']['extensions']))
{
	$libraries = &$result['body']['extensions'];
	$libraries = wfArrayValAll($libraries, 'extension', array());
}

$listHtml .= '<ul class="libraries">';

function functionNameSort($a, $b)
{
    return strcasecmp($a['name'], $b['name']);
}

function libraryNameSort($a, $b)
{
	$labelA = isset($a['label']) ? $a['label'] : $a['title'];
	$labelB = isset($b['label']) ? $b['label'] : $b['title'];

	return strcasecmp($labelA, $labelB);
}

// keep built-in on top, assumes built-in is returned first
$builtIn = array_shift($libraries);
// sort the libraries
uasort($libraries, 'libraryNameSort');
array_unshift($libraries, $builtIn);

$idIndex = 0;
foreach ($libraries as $library)
{
	$idIndex++;

	$name = isset($library['label']) ? htmlentities($library['label']) : htmlentities($library['title']); 
        $title = htmlentities($library['title']);
        $description = isset($library['description']) ? htmlentities($library['description']) : '';
	$customDescription = isset($library['description.custom']) ? htmlentities($library['description.custom']) : '';
	$namespace = isset($library['namespace']) ? $library['namespace']: '';
	$helpUri = isset($library['uri.help']) ? $library['uri.help'] : null;
	$logoUri = isset($library['uri.logo']) ? $library['uri.logo'] : null;

	// gererate some html
	$listHtml .= sprintf('<li class="library %s" id="library-%s">', $namespace, $idIndex);
	$listHtml .= sprintf('<a href="#" title="%s" name="%s" desc="%s" customDescription="%s" namespace="%s" logo="%s"><span class="%s"></span>%s</a>',
						 $title, $name, $description, $customDescription, $namespace, $logoUri, $namespace, $name);

	$functions = wfArrayValAll($library, 'function', array());
	// sort the functions alphabetically
	uasort($functions, 'functionNameSort');

	$listHtml .= '<ul class="functions">';
	foreach ($functions as $function)
	{
		$idIndex++;

		$name = (!empty($namespace)) ? htmlentities($namespace .'.'. $function['name']) : htmlentities($function['name']);
		$description = htmlentities($function['description']);
		$property = isset($function['@usage']) && $function['@usage'] == 'property' ? 'property="true"' : '';

		// generate some html
		$nameClass = str_replace('.', '_', $name); // remove the periods from the function name for css purposes
		$listHtml .= sprintf('<li class="function %s" id="function-%s">', $nameClass, $idIndex);
		$listHtml .= sprintf('<a href="#" name="%s" title="%s" %s><span class="%s"></span><span class="name">%s</span><span class="description">%s</span></a>',
								$name, $description, $property, $nameClass, $name, $description);

		$params = wfArrayValAll($function, 'param');
		if (is_null($params))
		{
			// wiki.pagecount
			$params = array();
		}

		$listHtml .= '<ul class="params">';
		foreach ($params as $param)
		{
			$idIndex++;

			$optional = (isset($param['@optional']) && $param['@optional'] == 'true') || isset($param['@default']) ? 'true' : 'false';
			$hint = isset($param['#text']) ? htmlentities($param['#text']): '';
			$name = isset($param['@name']) ? htmlentities($param['@name']): '';
			$type = isset($param['@type']) ? $param['@type']: '';

			$listHtml .= sprintf('<li class="param" id="param-%s"><span class="name">%s</span><span class="type">%s</span><span class="hint">%s</span><span class="optional">%s</span></li>', $idIndex, $name, $type, $hint, $optional);
		}
		$listHtml .= '</ul>';
		$listHtml .= '</li>';
	}
	$listHtml .= '</ul>';
	$listHtml .= '</li>';
}
$listHtml .= '</ul>';


?>
<?php echo '<?xml version="1.0" encoding="UTF-8"?>' . "\n"; ?>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en" dir="ltr">
    <head>
        <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
        <title>Insert Extension</title>

        <link rel="stylesheet" type="text/css" href="css/styles.css" />
		<script type="text/javascript" src="popup.js"></script>

        <link rel="stylesheet" type="text/css" href="/skins/common/icons.css" />

		<script type="text/javascript" src="/skins/common/yui/yahoo-dom-event/yahoo-dom-event.js"></script>
		<script type="text/javascript" src="/skins/common/yui/animation/animation.js"></script>
		
		<script type="text/javascript" src="/skins/common/yui/connection/connection.js"></script>
		<script type="text/javascript" src="/skins/common/yui/datasource/datasource.js"></script>
		<script type="text/javascript" src="/skins/common/yui/autocomplete/autocomplete.js"></script>
		

		<?php /*include this to use the logger*/ //echo '<script type="text/javascript" src="/skins/common/yui/logger/logger.js"></script>'; ?>
		<script type="text/javascript" src="/skins/common/yui/mindtouch/extension.js"></script>

		<script>
			if (YAHOO.lang.isObject(YAHOO.widget.Logger))
			{
				//oLogReader = new YAHOO.widget.LogReader();
				// Enable logging to firebug
				YAHOO.widget.Logger.enableBrowserConsole();
			}

			function dialogInit()
			{		
				var oConfig = {
					extensionList:	'extensions',
					libraryDiv:		'libraries',
					functionDiv:	'functions',
					editorDiv:		'parameters'
				  };
				var xt = new YAHOO.mindtouch.ExtensionDialog(oConfig);

				// init dialog
				Popup.init({
					handlers : {
						submit : function() { return xt.submit(); },
						cancel : function() { return null; }
					},
					validate: function() { return xt.validate(); }
				});

				var oParams = Popup.getParams();
				oParams.oPopup = Popup;
				xt.init(oParams);
			}
			// add the onload event
			YAHOO.util.Event.addListener(window, 'load', dialogInit);
		</script>

		<style>
			html, body {
				margin: 0;
				padding: 0;
			}
			h1 {
				font-size: 12px;
				margin: 0 0 5px 0;
			}
			h2 {
				font-size: 11px;
				font-weight: normal;
				margin: 0 0 10px 0;
			}

			div.pane {
				height: 370px;
			}
			div.extensions {
				display: none;
			}
			/* first pane which lists all the libraries installed */
			div.libraries {
				float: left;
				width: 150px;
				text-align: center;
				padding: 5px 0px;
				margin: 0;
				overflow: auto;
			}
			div.libraries ul {
				margin: 0;
				padding: 0;
			}
			div.libraries ul li {
				width: 100px;
				list-style: none;
				text-align: center;
				padding: 7px 15px;
				background: transparent url(/skins/common/images/extension-library-bg.png) no-repeat 15px 8px;
			}
			div.libraries ul li ul {
				display: none;
			}
			div.libraries ul li:hover {
				background-color: #DDDDFF;
			}
			div.libraries ul li.selected {
				background-color: #FFFFCC;
			}
			div.libraries ul li a {
				display: block;
				font-weight: bold;
				text-decoration: none;
				/*font-size: 10px;*/
			}
			div.libraries ul li span {
				display: block;
				width: 100px;
				height: 75px;
				margin-bottom: 7px;
				cursor: pointer;
				background: transparent url(/skins/common/images/extension-default-icon.png) no-repeat center center;
			}

			/*
			div.libraries ul li.syntax a span {
				background: transparent url(mindtouch_extension.png) no-repeat center center;
			}
			*/

			/* second pane which displays a library's functions */
			div.functions {
				float: left;
				width: 200px;
				padding: 5px 15px;
				margin: 0;
				overflow: auto;
				clear: none;
			}
			div.functions h1 {}
			div.functions ul {
				margin: 0;
				padding: 0;
			}
			div.functions ul li {
				list-style: none;
				padding: 5px 5px 5px 15px;
			}
			div.functions ul li:hover,
			div.functions ul li.selected:hover {
				background-color: #DDDDFF;
			}
			div.functions ul li.selected {
				background-color: #FFFFCC;
			}
			div.functions ul li a {
				display: block;
				text-decoration: none;
			}
			div.functions ul li a span {
				display: block;
			}
			div.functions ul li a span.name {
				font-weight: bold;
				text-decoration: underline;
			}
			div.functions ul li a span.description {
				color: #000;
				text-decoration: none;
			}

			/* third pane which contains the input fields */
			div.parameters {
				position: relative;
				float: left;
				width: 220px;
				padding: 5px 15px;
				margin: 0;
				overflow: auto;
			}
			div.parameters ul {
				padding: 0;
				margin: 0 0 10px 0;
			}
			div.parameters ul li {
				list-style: none;
			}
			div.parameters ul li.label {
				font-weight: bold;
			}
			div.parameters ul li.input {
				position: relative;
			}
			div.parameters ul li.input span.toggle {
				position: absolute;
				top: 0px;
				font-weight: bold;
				cursor: pointer;
				white-space: nowrap;
			}

			div.parameters ul li.input input {
				width: 185px;
				padding: 2px 2px;
				border: solid 1px #999;
			}
			div.parameters ul li.input textarea {
				width: 185px;
				height: 75px;
				padding: 2px 2px;
				padding-right: 20px;
				border: solid 1px #666;
			}
			div.parameters ul li.input .required {
				border: solid 1px #000;
			}
			div.parameters ul li.sublabel {
				font-style: italic;
			}

			div.controls {
				width: 645px;
				clear: both;
				border-top: solid 1px #999;
				height: 10px;
				text-align: right;
				margin: 0;
				padding: 15px 10px 0 0;
			}
			div.controls button.insert {
				margin-right: 5px;
			}
			/* autocomplet styles */
			div.yui-ac-container .yui-ac-bd {
				position: absolute;
				background-color: #fff;
				border: solid 1px #ccc;
				padding: 2px;
			}
			div.yui-ac-container .yui-ac-bd li {
				padding: 2px 5px;
			}
			div.yui-ac-container .yui-ac-highlight {
				background-color: #DDDDFF;
				cursor: pointer;
			}
		</style>
	</head>
	<body>
		<form style="display: inline;">
			<div style="position: relative; width: 99%;">
				<div id="extensions" class="extensions pane">
					<?php echo $listHtml . "\n"; ?>
				</div>

				<div id="libraries" class="libraries pane"></div>

				<div id="functions" class="functions pane">
					<h1>Select a library from the left</h1>
					<p>
						After selecting a library, the functions will be displayed here so you can select one to insert.
					</p>

					<p>
						Please note that this dialog is just a helper for inserting extensions. If you type something invalid within a
						parameter field, this dialog will not detect it.
					</p>

					<p>
						Errors will be returned when you save your wiki page with
						the extension embedded.
					</p>
				</div>

				<div id="parameters" class="parameters pane"></div>
			</div>
		</form>
	</body>
</html>
