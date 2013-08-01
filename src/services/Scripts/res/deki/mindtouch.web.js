/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
 * please review the licensing section.
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 * http://www.gnu.org/copyleft/lesser.html
 */

//--- Define Console (if missing) ---
if(typeof console == 'undefined') {
	console = { log: function() {} };
}

//--- Define MindTouch Namespace ---
if(typeof MindTouch == 'undefined') {
	MindTouch = { };
}

//--- Define MindTouch.Web Namespace ---
if(typeof MindTouch.Web == 'undefined') {
	MindTouch.Web = {
		Version: { major: 0, minor: 2 }
	};
}

//--- Define REST Functions ---
MindTouch.Web.Get = function(uri, headers, callback /* fn(xhr) */) {

    // ensure the proper scheme is passed into the requested uri
    var scheme = uri.match(/^https?(?=:)/);
    if(scheme && scheme.length && uri.indexOf('dream.in.scheme=') < 0) {
        uri += ((uri.indexOf('?') < 0) ? '?' : '&') + 'dream.in.scheme=' + scheme[0];
    }

	// initiate AJAX request
	$.ajax({

		// set the request location
		url: uri,

		// set the request HTTP verb
		type: 'GET',
		cache: false,

		// add custom header which checks if the property was modified since we read it
		beforeSend: function(xhr) {
			if(headers) {
				$.each(headers, function(header, header_value) {
					if((typeof header_value != 'object') && (typeof header_value != 'function')) {
						xhr.setRequestHeader(header, header_value);
					}
				});
			}
			return true;
		},

		// set callback
		complete: callback || function(xhr) { 
			if(!MindTouch.Web.IsSuccessful(xhr)) { 
				alert('AJAX ' + ((headers && headers['X-HTTP-Method-Override']) || 'GET') + ' request failed for ' + uri + ' (status: ' + xhr.status + ' - ' + MindTouch.Web.GetStatusText(xhr.status) + ')'); 
			} 
		}
	});
};

MindTouch.Web.Post = function(uri, value, mimetype, headers, callback /* fn(xhr) */) {

    // ensure the proper scheme is passed into the requested uri
    var scheme = uri.match(/^https?(?=:)/);
    if(scheme && scheme.length && uri.indexOf('dream.in.scheme=') < 0) {
        uri += ((uri.indexOf('?') < 0) ? '?' : '&') + 'dream.in.scheme=' + scheme[0];
    }

	// initiate AJAX request
	$.ajax({

		// set the request location
		url: uri,

		// set the request HTTP verb
		type: 'POST',

		// set the value of the updated property
		data: value,
		contentType: mimetype,
		processData: false,

		// add custom header which checks if the property was modified since we read it
		beforeSend: function(xhr) {
			if(headers) {
				$.each(headers, function(header, header_value) {
					if((typeof header_value != 'object') && (typeof header_value != 'function')) {
						xhr.setRequestHeader(header, header_value);
					}
				});
			}
			return true;
		},

		// set callback
		complete: callback || function(xhr) { if(!MindTouch.Web.IsSuccessful(xhr)) { alert('AJAX ' + ((headers && headers['X-HTTP-Method-Override']) || 'POST') + ' request failed for ' + uri + ' (status: ' + xhr.status + ' - ' + MindTouch.Web.GetStatusText(xhr.status) + ')'); } }
	});
};

MindTouch.Web.Head = function(uri, headers, callback /* fn(xhr) */) {

	// set http method override header, which allows us to tunnel the request
	headers = headers || { };
	headers['X-HTTP-Method-Override'] = 'HEAD';

	// tunnel HEAD through a GET request since most browsers don't support HEAD
	this.Get(uri, value, mimetype, headers, callback);
};

MindTouch.Web.Put = function(uri, value, mimetype, headers, callback /* fn(xhr) */) {

	// set http method override header, which allows us to tunnel the request
	headers = headers || { };
	headers['X-HTTP-Method-Override'] = 'PUT';

	// tunnel PUT through a POST request since most browsers don't support PUT
	this.Post(uri, value, mimetype, headers, callback);
};

MindTouch.Web.Delete = function(uri, headers, callback /* fn(xhr) */) {

	// set http method override header, which allows us to tunnel the request
	headers = headers || { };
	headers['X-HTTP-Method-Override'] = 'DELETE';

	// tunnel DELETE through a POST request since most browsers don't support DELETE
	this.Post(uri, null, null, headers, callback);
};

MindTouch.Web.IsSuccessful = function(xhr) {
	return (xhr.status >= 200 && xhr.status < 300) || (xhr.status == 304 /* Not Modified */);
};

MindTouch.Web.GetETag = function(xhr) {
    var etag = xhr.getResponseHeader('ETag');
	
    // fix etag if content encoding was used
    var encoding = xhr.getResponseHeader('Content-Encoding');
    if(encoding && (encoding.length > 0)) {
        etag = etag.replace('-' + encoding, '');
    }
    return etag;
};

MindTouch.Web.GetStatusText = function(status) {
	switch(status) {
	case 100: return 'Continue';
	case 101: return 'Switching Protocols';
	case 200: return 'Ok';
	case 201: return 'Created';
	case 202: return 'Accepted';
	case 203: return 'Non-Authoritative Information';
	case 204: return 'No Content';
	case 205: return 'Reset Content';
	case 206: return 'Partial Content';
	case 207: return 'Multi-Status';
	case 300: return 'Multiple Choices';
	case 301: return 'Moved Permanently';
	case 302: return 'Found';
	case 303: return 'See Other';
	case 304: return 'Not Modified';
	case 305: return 'Use Proxy';
	case 306: return '(Reserved)';
	case 307: return 'Temporary Redirect';
	case 400: return 'Bad Request';
	case 401: return 'Unauthorized';
	case 402: return 'Payment Required';
	case 403: return 'Forbidden';
	case 404: return 'Not Found';
	case 405: return 'Method Not Allowed';
	case 406: return 'Not Acceptable';
	case 407: return 'Proxy Authentication';
	case 408: return 'Request Timeout';
	case 409: return 'Conflict';
	case 410: return 'Gone';
	case 411: return 'Length Required';
	case 412: return 'Precondition Failed';
	case 413: return 'Request Entity Too Large';
	case 414: return 'Request-URI Too Long';
	case 415: return 'Unsupported Media Type';
	case 416: return 'Requested Range Not Satisfiable';
	case 417: return 'Expectation Failed';
	case 422: return 'Unprocessable Entity';
	case 423: return 'Locked';
	case 424: return 'Failed Dependency';
	case 500: return 'Internal Server Error';
	case 501: return 'Not Implemented';
	case 502: return 'Bad Gateway';
	case 503: return 'Service Unavailable';
	case 504: return 'Gateway Timeout';
	case 505: return 'HTTP Version Not Supported';
	case 507: return 'Insufficient Storage';
	}
	return '(Unknown)';
};

//--- Define MindTouch.Text Namespace ---
if(typeof MindTouch.Text == 'undefined') {
	MindTouch.Text = { }
}

MindTouch.Text.Utf8Encode = function(string) {
	var utftext = '';
	for(var n = 0; n < string.length; n++) {
		var c = string.charCodeAt(n);
		if(c < 128) {
			utftext += String.fromCharCode(c);
		} else if((c > 127) && (c < 2048)) {
			utftext += String.fromCharCode((c >> 6) | 192);
			utftext += String.fromCharCode((c & 63) | 128);
		} else {
			utftext += String.fromCharCode((c >> 12) | 224);
			utftext += String.fromCharCode(((c >> 6) & 63) | 128);
			utftext += String.fromCharCode((c & 63) | 128);
		}

	}
	return utftext;
};

MindTouch.Text.Utf8Decode = function(utftext) {
	var string = '';
	var i = 0;
	var c = c1 = c2 = 0;
	while(i < utftext.length) {
		c = utftext.charCodeAt(i);
		if (c < 128) {
			string += String.fromCharCode(c);
			i++;
		} else if((c > 191) && (c < 224)) {
			c2 = utftext.charCodeAt(i+1);
			string += String.fromCharCode(((c & 31) << 6) | (c2 & 63));
			i += 2;
		} else {
			c2 = utftext.charCodeAt(i+1);
			c3 = utftext.charCodeAt(i+2);
			string += String.fromCharCode(((c & 15) << 12) | ((c2 & 63) << 6) | (c3 & 63));
			i += 3;
		}

	}
	return string;
};

MindTouch.Text.Base64Encode = function(text) {

    // check if input string contains characters outside of the ASCII range
    if(/([^\u0000-\u00ff])/.test(text)) {
        throw new Error('Can\'t base64 encode non-ASCII characters.');
    } 
    
    // loop over each character and encode it
    var digits = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/', i = 0, cur, prev, byteNum, result = [];      
    while(i < text.length) {
        cur = text.charCodeAt(i);
        byteNum = i % 3;
        switch(byteNum){
        case 0: // first byte
            result.push(digits.charAt(cur >> 2));
            break;
        case 1: // second byte
            result.push(digits.charAt((prev & 3) << 4 | (cur >> 4)));
            break;
        case 2: // third byte
            result.push(digits.charAt((prev & 0x0f) << 2 | (cur >> 6)));
            result.push(digits.charAt(cur & 0x3f));
            break;
        }
        prev = cur;
        i++;
    }
    
    // check for trailing characters that were not encoded
    if(byteNum == 0) {
        result.push(digits.charAt((prev & 3) << 4));
        result.push('==');
    } else if (byteNum == 1) {
        result.push(digits.charAt((prev & 0x0f) << 2));
        result.push('=');
    }
    return result.join('');
};

MindTouch.Text.Base64Decode = function(text) {

    // remove any whitespace characteres from input
    text = text.replace(/\s/g,'');

    // check if input contains any invalid base64 characters
    if(!(/^[a-z0-9\+\/\s]+\={0,2}$/i.test(text)) || text.length % 4 > 0){
        throw new Error('Not a base64-encoded string.');
    }   

    // loop over each character and decode it
    var digits = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/', cur, prev, digitNum, i = 0, result = [];
    text = text.replace(/=/g, '');
    while(i < text.length){
        cur = digits.indexOf(text.charAt(i));
        digitNum = i % 4;
        switch(digitNum){
        //case 0: first digit - do nothing, not enough info to work with
        case 1: //second digit
            result.push(String.fromCharCode(prev << 2 | cur >> 4));
            break;
        case 2: //third digit
            result.push(String.fromCharCode((prev & 0x0f) << 4 | cur >> 2));
            break;
        case 3: //fourth digit
            result.push(String.fromCharCode((prev & 3) << 6 | cur));
            break;
        }
        prev = cur;
        i++;
    }
    return result.join('');
};
