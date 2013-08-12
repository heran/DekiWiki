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

/***
 * MindTouch Messaging
 * Outputs success/error messages that autoclose
 */
var MTMessage = function () {};

MTMessage.Active = false;
MTMessage.DefaultTimerValue = 6;
MTMessage.TimeValue = MTMessage.DefaultTimerValue;
MTMessage.Timer = false;

MTMessage.Show = function(headerText, bodyText, msgType, detailsArray) {
	if (MTMessage.Active) 
		return;
	
	if (typeof(msgType) == 'undefined')
		msgType = 'ui-errormsg';
	MTMessage.Active = true;
	MTMessage.SetHeader(headerText);
	MTMessage.SetBody(bodyText);
	if (typeof(detailsArray) == 'undefined'  || detailsArray == '') {
		document.getElementById('MTMessageDetailsLink').style.display = 'none';
	}
	else {
		MTMessage.SetDetails(detailsArray);
	}
	
	var node = document.getElementById('MTMessage');
	document.getElementById('MTMessageStyle').className = 'ui-msg '+msgType;
	document.getElementById('MTMessage').style.display = 'block';
	YAHOO.util.Dom.setStyle('MTMessage', 'opacity', 1);
	document.getElementById('MTMessageUnpaused').style.display = 'inline';
	document.getElementById('MTMessagePaused').style.display = 'none';
	document.getElementById('MTMessage').onmouseover = function() {
		if (MTMessage.Timer) 
			MTMessage.PauseTimer();
	};
	MTMessage.GoTimer();
	return false;
};
MTMessage.Hide = function() {
	MTMessage.TimeValue = MTMessage.DefaultTimerValue;
	if (YAHOO.env.ua.ie > 0 && YAHOO.env.ua.ie < 7) {
		document.getElementById('MTMessage').style.display = 'none'; 
 		MTMessage.Active = false;
 		return false;
	}
	var anim = new YAHOO.util.Anim('MTMessage', { opacity: { to: 0 } }, 1, YAHOO.util.Easing.easeOut);
	anim.onComplete.subscribe(function () { 
		document.getElementById('MTMessage').style.display = 'none'; 
		MTMessage.Active = false;
	});	
	anim.animate();
	return false;
};
MTMessage.PauseTimer = function() {
	MTMessage.Timer = false;
	document.getElementById('MTMessageUnpaused').style.display = 'none';
	document.getElementById('MTMessagePaused').style.display = 'inline';
};
MTMessage.UnPauseTimer = function() {
	document.getElementById('MTMessageUnpaused').style.display = 'inline';
	document.getElementById('MTMessagePaused').style.display = 'none';
	MTMessage.GoTimer();
};
MTMessage.GoTimer = function() {
	MTMessage.Timer = true;
	MTMessage.TimeValue = MTMessage.DefaultTimerValue;
	MTMessage.TimerValue(MTMessage.TimeValue);
};
MTMessage.TimerValue = function() {
	if (!MTMessage.Timer)
		return;
	MTMessage.TimeValue = MTMessage.TimeValue - 1;
	document.getElementById('MTMessageTimer').innerHTML = MTMessage.TimeValue+' ';
	if ((MTMessage.TimeValue) < 1) {
		MTMessage.Hide();
	}
	else {
		setTimeout("MTMessage.TimerValue("+(MTMessage.TimeValue)+")", 1000);
	}
};
MTMessage.SetHeader = function(val) {
	document.getElementById('MTMessageHeader').innerHTML = val;
};
MTMessage.SetBody = function(val) {
	var elText = document.createTextNode(val);
	el = document.getElementById('MTMessageDesc');
	el.innerHTML = '';
	el.appendChild(elText);
};
MTMessage.ShowDetails = function(anode) {
	document.getElementById('MTMessageDetails').style.display = 'block';
	document.getElementById('MTMessageTextarea').select();
	anode.parentNode.innerHTML = anode.innerHTML; //remove link

	return false;
};
MTMessage.SetDetails = function(val) {
    
    var node, container;
    
    container = document.getElementById('MTMessageDetails');    
    
    node = document.getElementById('MTMessageTextarea');
    
    // remove the previous message's details    
    if ( node )
    {
        container.removeChild(node);
    }
    
	node = document.createElement('textarea');
	node.id = 'MTMessageTextarea';
	node.className = 'ui-msgtextarea';
	node.setAttribute('readonly', 'true');
	node.value = eval(val);
	container.appendChild(node);
};