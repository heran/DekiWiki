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
if( typeof console == 'undefined' ) {
	console = { log: function(str) {} };
}

if(typeof Deki.Api == 'undefined') {
	Deki.Api = {
		GetPage: function(id, callback) {
			Deki.$.get('/@api/deki/pages/' + id, null, function(xml) {
				var doc = Deki.$(xml);
				var page = {};
				page.title = doc.find('title').text();
				page.path = doc.find('path').text();
				page.ns = doc.find('namespace').text();
				// TODO: author
				page.description = doc.find('description').text();
				page.language = doc.find('language').text();
				// TODO: subpages, inbound, outbound, revisions, comments, properties, tags, files, contents
				
				Deki.Api.CallOrPublish(callback, page);
			}, 'xml');
		},
		
		EditTimeNow: function() {
			var d = new Date();
			var month = d.getUTCMonth() + 1;
			if(month < 10) month = "0" + month;
			var day = d.getUTCDate();
			if(day < 10) day = "0" + day;
			var hours = d.getUTCHours();
			if(hours < 10) hours = "0" + hours;
			var mins = d.getUTCMinutes();
			if(mins < 10) mins = "0" + mins;
			var secs = d.getUTCSeconds();
			if(secs < 10) secs = "0" + secs;
			return d.getUTCFullYear() + month + day + hours + mins + secs;
		},
		
		UpdatePage: function(path, contents, edittime, success, error) {
			if(!edittime) edittime = this.EditTimeNow();
			path = (!path || (path == "")) ? "home" : "=" + Deki.url.encode(Deki.url.encode(path));
			Deki.$.ajax({
				type: "POST",
				url: "/@api/deki/pages/" + path + "/contents?edittime=" + edittime,
				data: contents,
				contentType: "text/plain",
				success: success,
				error: error
			});
		},
		
		CreatePageFromTemplate: function(path, template, success, error) {
			// fetch template
			// lookup body[not(target)]
			// use as content on UpdatePage
		},
		
		Reload: function(dom, params, callback) {
			if(typeof dom == 'string') dom = Deki.$(dom);
			else if(typeof dom.eq != 'function') dom = Deki.$(dom);
			var uri = Deki.Env.PageApi + '/contents?format=xhtml&include=true';
			if(params) {
				uri += '&' + Deki.$.param(params);
			}
			dom.load(uri + ' #' + dom.get(0).id + " > *", null, function() { Deki.Api.CallOrPublish(callback, {}); });
		},
		
		PostText: function(uri, data, success) {
//			alert('PostText: ' + uri);
//			uri = uri.replace(/(\$\{[a-zA-Z0-9\.]+\})/, function(key) {
//				var value = Deki.Env[key];
//				return (typeof value != 'undefined') ? value : key;
//			});
//			alert(uri);
			Deki.$.ajax({ 
				type: 'POST', 
				url: uri, 
				data: data, 
				contentType: 'text/plain;', 
				success: success 
			});
		},
		
		PostMessage: function(page_api, subchannel, data, success) {
			page_api = page_api || Deki.Env.PageApi;
			Deki.$.ajax({ 
				type: 'POST', 
				url: Deki.Env.PageApi + '/message/' + subchannel, 
				data: data, 
				contentType: 'text/plain;', 
				success: success 
			});
		},
		
		CreatePageProperty: function(page_api, key, value, success /* fn(xhr) */, error /* fn(status, text, xhr) */ ) {
			page_api = page_api || Deki.Env.PageApi;
			Deki.$.ajax({ 
				url: page_api + '/properties',
				type: 'POST', 
				data: value, 
				contentType: 'text/plain',
				processData: false,
				beforeSend: function(xhr) { 
					xhr.setRequestHeader('Slug', key); 
					return true; 
				},
				complete: function(xhr) {
					if(xhr.status == 200) {
						Deki.Api.CallOrPublish(success, { etag: xhr.getResponseHeader('ETag'), xhr: xhr });
					} else Deki.Api.CallOrPublish(error, { status: xhr.status, text: xhr.statusText, xhr: xhr });
				}
			});
		},
		
		ReadPageProperty: function(page_api, key, success /* fn(value, href, etag, xhr) */, error /* fn(status, text, xhr) */) {
			page_api = page_api || Deki.Env.PageApi;
			var uri = page_api + '/properties?dream.out.format=json&names=' + Deki.url.encode(key);
			Deki.$.ajax({
				url: uri, 
				type: 'GET',
				cache: false,
				complete: function(xhr) {
					if(xhr.status == 200) {
						var data = eval('(' + xhr.responseText + ')');
						var href = data.property && data.property.contents['@href'];
						if(href) {
							Deki.$.ajax({
								url: href, 
								type: 'GET',
								cache: false,
								complete: function(xhr) {
									if(xhr.status == 200) Deki.Api.CallOrPublish(success, { value: xhr.responseText, href: href, etag: xhr.getResponseHeader('ETag'), xhr: xhr });
									else Deki.Api.CallOrPublish(error, { status: xhr.status, text: xhr.statusText, xhr: xhr });
								}
							}); 
						} else Deki.Api.CallOrPublish(success, { value: null, href: null, etag: null, xhr: xhr })
					} else Deki.Api.CallOrPublish(error, { status: xhr.status, text: xhr.statusText, xhr: xhr });
				}
			});

		},
		
		UpdatePageProperty: function(property_api, value, etag, success /* fn(xhr) */, error /* fn(status, text, xhr) */ ) {
			Deki.$.ajax({ 
				url: property_api + '?dream.in.verb=PUT',
				type: 'POST', 
				data: value, 
				contentType: 'text/plain',
				processData: false,
				beforeSend: function(xhr) { 
					xhr.setRequestHeader('ETag', etag); 
					return true; 
				},
				complete: function(xhr) {
					if(xhr.status == 200) Deki.Api.CallOrPublish(success, { xhr: xhr })
					else Deki.Api.CallOrPublish(error, { status: xhr.status, text: xhr.statusText, xhr: xhr });
				}
			});
		},
		
		Poll: function(interval, containerId, uri) {
	        Deki.$.get(uri, { containerId: containerId }, function(data){
                Deki.Api._callback(interval, containerId, uri, data);
            });
        },
        
        _callback :function (interval, id, uri, data) {
            if( data ) {
                Deki.$('#'+id).empty().append((new XMLSerializer()).serializeToString(data));
            }
            setTimeout(function() {
                Deki.Api.Poll(interval, id, uri);
            },interval);
		},
		
		CallOrPublish: function(fn, arg) {
			if(typeof fn == 'function') {
				fn.call(null, arg);
			} else if(typeof fn == 'string') {
				Deki.publish(fn, arg);
        }
		}
	};
}
