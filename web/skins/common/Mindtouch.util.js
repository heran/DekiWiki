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

/* global Deki object */
if (typeof Deki == "undefined") {
	var Deki = {};
}
// setup our jQuery reference
Deki.$ = jQuery;

Deki.url = {};
Deki.url.encode = function(plaintext)
{
	// The Javascript escape and unescape functions do not correspond
	// with what browsers actually do...
	var SAFECHARS = "0123456789" +					// Numeric
					"ABCDEFGHIJKLMNOPQRSTUVWXYZ" +	// Alphabetic
					"abcdefghijklmnopqrstuvwxyz" +
					"-_.!~*'()";					// RFC2396 Mark characters
	var HEX = "0123456789ABCDEF";

	var encoded = "";
	for (var i = 0; i < plaintext.length; i++ ) {
		var ch = plaintext.charAt(i);
	    if (ch == " ") {
		    encoded += "+";				// x-www-urlencoded, rather than %20
		} else if (SAFECHARS.indexOf(ch) != -1) {
		    encoded += ch;
		} else {
		    var charCode = ch.charCodeAt(0);
			if (charCode > 255) {
				encoded += "+";
			} else {
				encoded += "%";
				encoded += HEX.charAt((charCode >> 4) & 0xF);
				encoded += HEX.charAt(charCode & 0xF);
			}
		}
	} // for

	return encoded;
}

Deki.util = {};
Deki.util.Dom = {};
Deki.util.Dom.getDimensions = function(element) {
	var region 	= YAHOO.util.Dom.getRegion(element);
	var width 	= region.right - region.left;
	var height 	= region.bottom - region.top;
	return {"width": width, "height": height};
}
Deki.util.Dom.getText = function(node) {
	if(typeof(node.innerText) != 'undefined') {
		return node.innerText;
	} else {
		return node.textContent;
	}
}
Deki.util.Dom.setInnerHTML = function (el, html) {
    el = YAHOO.util.Dom.get(el);
    if (!el || typeof html !== 'string') {
        return null;
    }

    // Break circular references.
    (function (o) {
        var a = o.attributes, i, l, n, c;
        if (a) {
            l = a.length;
            for (i = 0; i < l; i += 1) {
                n = a[i].name;
                if (typeof o[n] === 'function') {
                    o[n] = null;
                }
            }
        }
        a = o.childNodes;
        if (a) {
            l = a.length;
            for (i = 0; i < l; i += 1) {
                c = o.childNodes[i];

                // Purge child nodes.
                arguments.callee(c);

                // Removes all listeners attached to the element via YUI's addListener.
                YAHOO.util.Event.purgeElement(c);
            }
        }
    })(el);

    // Remove scripts from HTML string, and set innerHTML property
    el.innerHTML = html.replace(/<script[^>]*>((.|[\r\n])*?)<\\?\/script>/ig, "");

    // Return a reference to the first child
    return el.firstChild;
};
Deki.util.Dom.setInnerText = function(el, text) {
  if(typeof(el.innerText) != 'undefined') el.innerText = text;
  else el.textContent = text;
};
Deki.publish = function(c,d) { 
  if((name != null) && (name.indexOf("*") == -1)) Deki._query_store[c] = d;
  window.PageBus.publish(c,d); 
};
Deki.subscribe = function(c,o,f,d) { window.PageBus.subscribe(c,o,f,d) };
Deki._query_store = { };
Deki.query = function(c) { return Deki._query_store[c]; };
Deki.hasValue = function(v, d) { return (v != 'undefined') && (v != null) && (v != '') ? v : ((typeof d != 'undefined') ? d : null); };
