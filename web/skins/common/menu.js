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
 
function DWMenu() {};
DWMenu.Bubble = false;
DWMenu.Opened = false;
DWMenu.OpenedNow = false;
DWMenu.Selected = false;

DWMenu.LinkClick = function(divid, xoffset) {
	if (DWMenu.Opened && DWMenu.Opened != divid) 
		DWMenu.Toggle(DWMenu.Opened, xoffset);
	DWMenu.Toggle(divid, xoffset);
};

DWMenu.Toggle = function(divid, xoffset) {
	var node = Deki.$('#'+divid);
	//todo nullref
	var display = node.css('display');
	node.toggle();
	if (display == 'block') {
		DWMenu.Opened = false;
	}
	else {
		node.css('visibility', 'hidden'); //for ie6
		var width 	= node.width();
		var position 	= YAHOO.util.Dom.getXY(node);
		if (typeof(winX) != 'null') {
			if ((position[0] + width) > (winX - 36)) {
				if (typeof(offsetX) == 'undefined') offsetX = 0;
				node.css('left', (position[0] - width + offsetX + 5)+'px');
			}
		}
		node.css('visibility', '');
		DWMenu.Opened = divid;
		DWMenu.OpenedNow = true;
	}
}

DWMenu.Position = function (divid, elt, offsetX, offsetY) {
	if (!document.getElementById(divid)) return;
	
	var $elt = Deki.$(elt);
	var offset = $elt.offset();
	offsetY = offsetY + $elt.outerHeight();
	
	if (typeof(offsetX) == 'number') var x = offset.left + offsetX;
	if (typeof(offsetY) == 'number') var y = offset.top + offsetY;
	Deki.$("#" + divid).css("left", x+'px').css("top", y+'px');
	DWMenu.LinkClick(divid, $elt.width());	
	return false;
};

DWMenu.Off = function(divid) {
	var display = Deki.$('#'+divid).css('display');
	if (display == 'block') {
		Deki.$('#'+divid).toggle();
		DWMenu.Opened = false;
	}
};

DWMenu.BodyClick = function() {
	if (!DWMenu.OpenedNow && !DWMenu.Bubble && DWMenu.Opened) {
		if (!DWMenu.Selected) {
			DWMenu.Toggle(DWMenu.Opened);
		}
	}
	if (DWMenu.Bubble)
		DWMenu.Bubble = false;	
	if (DWMenu.OpenedNow)
		DWMenu.OpenedNow = false;	
};

//deprecated
var menuLinkClick = function(divid, offsetX) {
	DWMenu.LinkClick(divid, offsetX);
};
var menuToggle = function(navid, offsetX) {
	DWMenu.Toggle(navid, offsetX);
};
var menuPosition = function (navid, elt, offsetX, offsetY) {
	return DWMenu.Position(navid, elt, offsetX, offsetY);
};
var menuOff = function(navid) {
	DWMenu.Off(navid);
};
var menuBodyClick = function() {
	DWMenu.BodyClick();
};
