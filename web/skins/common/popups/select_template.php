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

define('MINDTOUCH_DEKI', true);
require_once('../../../includes/Defines.php');
require_once('../../../LocalSettings.php');
require_once('../../../includes/Setup.php');

$_templates = wfGetTemplatesList();
if (is_array($_templates) && count($_templates) > 0) {
	$select = '<select id="f_template" onChange="changeTemplate(this)">';
	foreach ($_templates as $key => $text) {
		$items = explode("|",$key);
		$name = array_shift($items);
		$isSite = array_shift($items);
		switch ($isSite)
		{
			default:
			case 0:
				$items = '';
				break;
			case 1:
				$text = '+'.$text;
				$items = ' items="' . str_replace('"','""',implode("|",$items)) . '"';
				break;
			case 2:
				// support lazy loading the template tree
				$text = '+'.$text;
				$items = ' items="gui"';
				break;
		}

		$select .= '<option class="'.($isSite ? 'tmplt_site' : 'tmplt_cntnt').'" value="'.$name.'"'.$items.'>'.$text.'</option>';
	}
	$select .= '</select>';
	$class = '';
}
else {
	$select = wfMsg('Dialog.Templates.no-templates');
	$class = ' disabled="true"';
}
echo '<?xml version="1.0" encoding="UTF-8"?>';
?>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN">
<html>
<head>
<title><?php echo wfMsg('Dialog.Templates.page-title') ?></title>
<meta http-equiv="Content-Type" content="text/html; charset=utf-8">
<script type="text/javascript" src="popup.js"></script>
<script type="text/javascript" src="/skins/common/jquery/jquery.min.js"></script>
<link rel="stylesheet" type="text/css" media="screen" href="/skins/common/fonts.css" />
<link rel="stylesheet" type="text/css" href="css/styles.css">
<script type="text/javascript">
	  //<![CDATA[
		var siteTemplate = null;
		var templateContent = null;

		function createLabel(spanID, label)
		{
			return '<span id="' + spanID + '">' + label + '</span>';
		}

		function createCell(row, cellAttr, content)
		{
			var cell = document.createElement("TD");
			row.appendChild(cell);
			if (cellAttr != null)
			{
				for (var id in cellAttr)
				{
					cell[id] = cellAttr[id];
				}
			}
			cell.innerHTML = content;
			return cell;
		}

		function changeTemplate(element)
		{
			templateContent = null;
			Popup.disableButton(Popup.BTN_OK);

			if (!element) return;
			
			var selectedValue = element.value;
			var selectedOption = element.options[element.selectedIndex];
			var siteItems = document.getElementById("id_siteItems");
			
			while (siteItems.firstChild != null)
			{
				siteItems.removeChild(siteItems.firstChild);
			}

			var cls = selectedOption.className;
			if (cls == "tmplt_site")
			{
				var items = selectedOption.attributes['items'].value;
				if (items == "gui") {
					// load the items
					$.ajax({
						url: '/deki/gui/templates.php',
						data: {
							action: "getitems",
							pageId: selectedValue
						},
						async: false,
						success: function(data) {
							items = data.items;
						}	
					});
				}
				
				items = items.replace(new RegExp("//","g"),'%2F%2F');
				items = items.replace(/ /g,'_');
				items = items.split("|");
				var tree = new Object();
				for (var i = 0; i < items.length; ++i)
				{
					var splItems = items[i].split('/');
					var c = tree;
					var p = "";
					for (var j = 0; j < splItems.length; ++j)
					{
						var itm = splItems[j];
						var name = itm.replace(/%2F%2F/g,'//').replace(/_/g,' ');
						if (typeof c[itm] == "undefined")
						{
							var key = itm.replace(new RegExp("[%\+@\*\.]","g"),'_').replace(/-/g,'_');
							c[key] = { __value: name, __path: p + name };
						}
						p += name + "/";
						c = c[itm];
					}
				}
				
				function createHtml(node)
				{
					var itemsHtml = '<ul>';
					for (var itemKey in node)
					{
						if (itemKey == "__value" || itemKey == "__path") continue;
						var item = node[itemKey];
						itemsHtml += '<li>' + item.__value + createHtml(item) + '</li>';
					}
					return itemsHtml == '<ul>' ? "" : itemsHtml + '</ul>';
				};
				
				var itemsHtml = createHtml(tree);
				createCell(siteItems, { className : "label" }, createLabel('f_items','Subpages:'));
				createCell(siteItems, null, itemsHtml);
				var siteName = (parent.YAHOO.env.ua.ie) ? selectedOption.innerText : selectedOption.textContent;
				siteTemplate = { siteID : selectedValue, siteName : siteName.substring(1), tree : tree };
			}
			else
			{
				siteTemplate = null;
			}

			var Connect = parent.YAHOO.util.Connect;

			var callback =
			{
				success : function(o) {
					if ( o.responseText.length > 0 )
					{
						templateContent = o.responseText;
						Popup.enableButton(Popup.BTN_OK);
					}
				}
			}

			Connect.asyncRequest('GET',
				'/deki/gui/templates.php?action=getcontent&pageId='
				+ encodeURIComponent(nContextPageId) + '&templateId=' + encodeURIComponent(selectedValue), callback);
		}

		// run on page load
		var nContextPageId = null;
		
		function Init()
		{
			Popup.init({
				handlers: {
					submit: onOK
				}
			});

			var oParams = Popup.getParams();
			if (oParams)
			{
				nContextPageId = oParams.contextTopicID;
			}

			changeTemplate(document.getElementById("f_template"));
		}

		function onOK()
		{
			if (templateContent == null)
				return true;
			
			var param = new Object();
			param.f_template = templateContent;
			if (siteTemplate)
			{
				param.f_site = siteTemplate;
			}
			return param;
		}

	//]]>
</script>
<style type="text/css">
 #id_siteItems td {
	 vertical-align: top;
 }
 ul {
	 padding-left: 1em;
	 margin-left: 1em;
 }
</style>
</head>

<body class="dialog" onload="Init();">
	<div class="wrap">
	<form>
	  <table style="" border="0" id="f_table">
		<tbody>
			<tr id="id_templates">
			<td class="label"><span id="f_template_label"><?php echo wfMsg('Dialog.Templates.list')?></span></td>
			<td><?php echo $select ?></td>
		  </tr>
		  <tr id="id_siteItems"></tr>
		</tbody>
	  </table>
	</form>
	</div>
</body>
</html>

<?php

function wfGetTemplates()
{
	return wfMakeString(wfGetTemplatesList());
}


/**
 * Builds the ridiculous key required for the template dialog
 *
 * @param string $key
 */
function wfBuildTemplateListItem($page, $key = '')
{
	$Page = new XArray($page);
	$href = $Page->getVal('subpages/@href', null);
	$subpages = $Page->getAll('subpages/page', array());
	if (empty($key))
	{
		$key = $Page->getVal('@id') . '|';
		if (empty($subpages))
		{
			$key .= is_null($href) ? '0|' : '2';
		}
		else
		{
			$key .= '1';
		}
	}
	
	foreach ($subpages as &$subpage)
	{
		$Info = DekiPageInfo::newFromArray($subpage);
		
		$paths = $Info->getParents();
		$templatePath = implode(HPS_SEPARATOR, $paths);
		
		$key .= '|' . $templatePath;
		$key = wfBuildTemplateListItem($subpage, $key);
	}
	
	return $key;
}

/**
 * Retrieve the list of content templates
 * @TODO: refactor template listing code path to be more efficient
 * @return array
 */
function wfGetTemplatesList()
{
	// attempt to load content templates
	$pages = array();
	$Result = DekiTemplateProperties::getTemplateXml($pages, DekiTemplateProperties::TYPE_CONTENT);
	
	$return = array();
	if (!empty($pages)) 
	{
		foreach ($pages as $page)
		{
			$key = wfBuildTemplateListItem($page);
			$return[$key] = $page['title'];
		}
		natsort($return);
	}
	
	return $return;
}
