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
require_once( '../../../includes/Defines.php' );
require_once( dirname($_SERVER['SCRIPT_FILENAME']) . '/../../../LocalSettings.php' );
require_once( '../../../includes/Setup.php' );

$titleID = $wgRequest->getVal('titleID');
wfCheckTitleId($titleID, 'userCanRestrict');

$results = $wgDekiPlug->At('pages', $titleID, 'security')->Get();
$list = wfGetRestrictions($results, array($wgUser->getName()));
$restrictionType = wfArrayVal($results, 'body/security/permissions.page/restriction/#text', 'Public');

echo '<?xml version="1.0" encoding="UTF-8"?>';
?><!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en" dir="ltr">
	<head>
		<meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
		<meta http-equiv="cache-control" content="no-cache" />
		<meta http-equiv="pragma" content="no-cache">
		<title><?php echo(wfMsg('Dialog.Restrict.page-title'));?></title>
		<script type="text/javascript" src="popup.js"></script>
		<link rel="stylesheet" type="text/css" href="/skins/common/fonts.css" />
		<link rel="stylesheet" type="text/css" href="css/styles.css" />

		<link rel="stylesheet" type="text/css" media="screen" href="/skins/common/yui/autocomplete/autocomplete.css"/>

		<script type="text/javascript" src="/skins/common/yui/yahoo-dom-event/yahoo-dom-event.js"></script>
		<script type="text/javascript" src="/skins/common/yui/datasource/datasource.js"></script>
		<script type="text/javascript" src="/skins/common/yui/json/json.js"></script>
		<script type="text/javascript" src="/skins/common/yui/animation/animation.js"></script>
		<script type="text/javascript" src="/skins/common/yui/connection/connection.js"></script>
		<script type="text/javascript" src="/skins/common/yui/autocomplete/autocomplete.js"></script>
		<style type="text/css">
			#userBox {
				height: 180px;
				overflow: auto;
			}
			.btn {
				width: 110px;
				margin-top: .3em;
			}

			table td {
				vertical-align: top;
			}

			#inputElements {
				margin-top: .5em;
			}

			#autoComplete input,
			#autoComplete .yui-ac-content{
				width: 150px;
			}
			.disabled {
				color: #888;
			}
		</style>
		<script type="text/javascript">
		function init()
		{
			setInputElementStatus();
			Popup.init({
				handlers : {
					submit : submitHandler,
					cancel : function() { return null; }
				},
				defaultKeyListeners : false,
				validate: function() { return true; }
			});

			var params = Popup.getParams();
		}

		function submitHandler()
		{
			loopSelected();
			var params = {
				titleid : <?php echo($titleID);?>,
				protecttype: document.getElementById('protectType').value,
				userids: document.getElementById('userIds').value,
				cascade: document.getElementById('cascade').checked
			}
			return params;
		}

		function loopSelected() {
			var txtSelectedValuesObj = document.getElementById('userIds');
			var selectedArray = new Array();
			var selObj = document.getElementById('selectNames');
			var i;
			var count = 0;
			for (i=0; i<selObj.options.length; i++) {
				selectedArray[count] = selObj.options[i].value;
				count++;
			}
			txtSelectedValuesObj.value = selectedArray;
		};
		function changeType(elt) {
			document.getElementById('protectType').value = elt.value;
			setInputElementStatus();
		};
		function addUserToList(node) {
			var ac = document.getElementById('autoCompInput');

			var callback =
			{
				success: function(o) {
				
					if ( o.responseText.length > 0 )
					{
						var opt = document.createElement('option');
						opt.innerHTML = ac.value;
						opt.value = o.responseText;
						ac.value = '';
						document.getElementById('selectNames').appendChild(opt);
						document.getElementById('valid').style.visibility = 'hidden';
						ac.focus();
						return false;
					}
					else
					{
						document.getElementById('valid').style.visibility = 'visible';
						ac.select();
						ac.focus();
						return false;
					}
				
				}
			};

			YAHOO.util.Connect.asyncRequest('GET', '/deki/gui/usergroupsearch.php?mode=userorgroup&name=' + encodeURIComponent(ac.value), callback);
			return false;
		};
		function removeUserToList() {
			var selObj = document.getElementById('selectNames');
			var c = selObj.options.length;
			for (i = c; i > 0; i--) {
				var obj = selObj.options[i - 1];
				if (obj && obj.selected) {
					selObj.removeChild(obj);
				}
			}
			return false;
		};

		function removeAllUsers(userId) {
			document.getElementById('userIds').value = '';
			document.getElementById('selectNames').innerHTML = '';
			return false;
		};

		function setInputElementStatus() {
			var node = document.getElementById('inputElements');
			var isPublic = document.getElementById('protectType').value == 'Public';
			node.className = isPublic ? 'disabled': '';
			var nodes = node.getElementsByTagName('input');
			for (var i = 0; i < nodes.length; i++ ) {
				nodes[i].disabled = isPublic;
			}
			var nodes = node.getElementsByTagName('select');
			for (var i = 0; i < nodes.length; i++ ) {
				nodes[i].disabled = isPublic;
			}
		};

		YAHOO.util.Event.onContentReady("autoCompInput",
			function()
			{
				var dataSource	= new YAHOO.util.XHRDataSource("/deki/gui/usergroupsearch.php");
				dataSource.responseType = YAHOO.util.XHRDataSource.TYPE_JSON;
				dataSource.responseSchema = {
					resultsList : "results",
					fields : [
						{ key: "item" }
					]
				};

				var autoComplete 		= new YAHOO.widget.AutoComplete("autoCompInput", "autoCompContainer", dataSource);
				autoComplete.animVert 	= false;

				autoComplete.generateRequest = function(sQuery) {
					return "?mode=usersandgroups&query=" + sQuery;
				};
			}
		);

		</script>
	</head>
	<body onload="init();" class="yui-skin-sam">
		<div class="wrap">
			<div><strong><?php echo(wfMsg('Dialog.Restrict.header-restriction-type'));?></strong></div>
			<div style="padding: 4px 0 0 2px;"><input type="radio" tabindex="1" name="type" onclick="changeType(this);" id="none" value="Public" <?php echo($restrictionType == 'Public'? 'checked': '')?>> <label for="none"><?php echo(wfMsg('Dialog.Restrict.type-public'));?></label><br/>
			<input type="radio" tabindex="2" name="type" onclick="changeType(this);" id="edit" value="Semi-Public" <?php echo($restrictionType == 'Semi-Public'? 'checked': '')?>> <label for="edit"><?php echo(wfMsg('Dialog.Restrict.type-semi-public'));?></label><br/>
			<input type="radio" tabindex="3" name="type" onclick="changeType(this);" id="viewedit" value="Private" <?php echo($restrictionType == 'Private'? 'checked': '')?>> <label for="viewedit"><?php echo(wfMsg('Dialog.Restrict.type-private'));?></label>
		</div>
		<div id="inputElements">
			<table>
				<tr>
					<td rowspan="2">
						<div><strong><?php echo(wfMsg('Dialog.Restrict.header-user-list'));?></strong></div>
						<select multiple="multiple" tabindex="4" size="10" style="width: 180px" id="selectNames">
						<?php
					foreach ($list as $k => $v)
					{
						echo('<option value="'.$k.'">'.$v.'</option>');
					}
						?>
						</select>
					</td>
					<td>
						<form method="none" onsubmit="return false;">
							<div><strong><?php echo(wfMsg('Dialog.Restrict.find'));?></strong></div>
							<div id="autoComplete">
								<input type="text" name="matchuser" id="autoCompInput" />
								<div id="autoCompContainer"></div>
							</div>
							<div><input type="submit" value="<?php echo(wfMsg('Dialog.Restrict.submit-add-user'));?>" onclick="return addUserToList(this);" class="btn"/></div>
							<div id="valid" style="visibility:hidden;"><?php echo(wfMsg('Dialog.Restrict.error-invalid-user'));?></div>
						</form>
					</td>
				</tr>
				<tr>
					<td style="vertical-align: bottom;">
						<div><input type="button" value="<?php echo(wfMsg('Dialog.Restrict.button-remove'));?>" class="btn" onclick="return removeUserToList();" style="margin-bottom: 4px;"/></div>
						<div><input type="button" value="<?php echo(wfMsg('Dialog.Restrict.button-remove-all'));?>" class="btn" onclick="return removeAllUsers();"/></div>
					</td>
				</tr>
			</table>
		</div>

	<div style="padding-left:2px;"><input type="checkbox" name="cascade" id="cascade" /><label for="cascade"><?php echo(wfMsg('Dialog.Restrict.apply-permissions-to-children'));?></label></div>


	<input type="hidden" id="protectType" name="protectType" value="<?php echo($restrictionType);?>" />
	<input type="hidden" id="titleID" name="titleID" value="<?php echo($titleID); ?>" />
	<input type="hidden" id="userName" name="userName" value="<?php echo($_GET['userName']); ?>" />
	<input type="hidden" id="userIds" name="userIds" value="" />

	</div>
	</body>
</html>
