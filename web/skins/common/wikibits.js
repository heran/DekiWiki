/*
 * MindTouch Deki - enterprise collaboration and integration platform
 * Copyright (C) 2006-2009 MindTouch, Inc.
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

 //node is the current checkbox
 function select_checkboxes(node) {
	 //select all checkboxes that is in the same form as this checkbox
	 var boxes = Deki.$("input[@type='checkbox']", Deki.$(node).parents('form'));
	 return node.checked ? boxes.check(): boxes.uncheck();
 }
         
 // make the specified div a windowed control in IE6
 // this masks an iframe (which is a windowed control) onto the div,
 // turning the div into a windowed control itself
 function makeWindowed(p_div)
 {
    var is_ie6 =
       document.all && 
       (navigator.userAgent.toLowerCase().indexOf("msie 6.") != -1);
    if (is_ie6)
    {
       var html =
          "<iframe style=\"position: absolute; display: block; " +
          "z-index: -1; width: 100%; height: 100%; top: 0; left: 0;" +
          "filter: mask(); background-color: #ffff00; \"></iframe>";
       if (p_div) p_div.innerHTML += html;
       // force refresh of div
       var olddisplay = p_div.style.display;
       p_div.style.display = 'none';
       p_div.style.display = olddisplay;
    };
 }
 
function showToc(node) {
	return DWMenu.Position('menuPageContent', node, -2, 0);
}
/***
 * hooks an onclick to all links in the table of contents dropdown which closes the window
 */
function hookTOCLinks() {
	Deki.$('.pageToc ol a').click(function() {	DWMenu.BodyClick(); });
};

function breadcrumbLoad(z) {
	document.getElementById('breadcrumb').innerHTML = z;
};

function array_search(needle, haystack) {
	for (var i = 0; i < haystack.length; i++ ) {
		if (haystack[i] == needle)
			return i;	
	}	
	return false;
};

// in [-]HH:MM format...
// won't yet work with non-even tzs
function fetchTimezone() {
	// FIXME: work around Safari bug
	var localclock = new Date();
	// returns negative offset from GMT in minutes
	var tzRaw = localclock.getTimezoneOffset();
	return formatTimezone(tzRaw);
};

function formatTimezone(tzSecs) {
	var tzHour = Math.floor( Math.abs(tzSecs) / 60);
	var tzMin = Math.abs(tzSecs) % 60;
	return  ((tzSecs >= 0) ? "-" : "") + ((tzHour < 10) ? "0" : "") + tzHour +
		":" + ((tzMin < 10) ? "0" : "") + tzMin;
};

function openWindow(href,menu) {
	window.open(href, 'popupwindow', 'width='+(winX - 100)+',height='+(winY - 100)+',scrollbars,resizable' + (menu ? ',menubar=yes' : ''));
	return false;
};

// RecentChanges JS
function toggleChangesTable(divid) {
	if (Deki.$('#showlink-'+divid).is(':visible'))
	{
		Deki.$('#showlink-'+divid).hide();
		Deki.$('#hidelink-'+divid).show();
		Deki.$('table tr.'+divid).hide();
	}
	else
	{
		Deki.$('#showlink-'+divid).show();
		Deki.$('#hidelink-'+divid).hide();
		Deki.$('table tr.'+divid).show();		
	}
    return false;
};
