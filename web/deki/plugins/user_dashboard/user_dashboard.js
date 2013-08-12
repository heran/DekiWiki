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

var Deki = Deki || {};

if (typeof Deki.Plugin == 'undefined') {
	Deki.Plugin = {};
}

$(function() {
	
	Deki.Plugin.UserDashboard._cacheDefaultPlugin();
	
	// tab area hidden, so we can update without flicker 
	// has 0 opacity in default class (display: none interferes with overflow detection)
	$tabarea = $('#deki-dashboard-tab-area');
	$tabarea.removeClass('dashboard-default');

	Deki.Plugin.UserDashboard._overflowTabs();
	Deki.Plugin.UserDashboard._loadPluginFromHash(function(){
		// remove default (html-only) stylings and make tab area visible
		$tabarea.css('opacity', '1.0');
	});

	// @todo kalida: disable AJAX loading of dashboard links (bug #8276)
	// Deki.Plugin.UserDashboard.HookDashboardLinks(Deki.Plugin.UserDashboard.ID);
});

Deki.Plugin.UserDashboard = {};
Deki.Plugin.UserDashboard.ID = 'deki-dashboard';
Deki.Plugin.UserDashboard._queryKey = "view";
Deki.Plugin.UserDashboard._anchorPrefix = Deki.Plugin.UserDashboard._queryKey + "-";
Deki.Plugin.UserDashboard._pluginCache = {};
Deki.Plugin.UserDashboard._currentRequest = null;

// Ajax-load dashboard links like <a href="/User:Foo?view=plugin_name"
// class="deki-dashboard-link">
// @param id - css id to be searched for links
Deki.Plugin.UserDashboard.HookDashboardLinks = function(id) {
	var $el = $('#' + id);

	if ($el) {
		Deki.Plugin.UserDashboard._attachEvents($el);
	}
};

Deki.Plugin.UserDashboard._overflowTabs = function() {
	var $row = $('ul.deki-dashboard-tabs').eq(0);
	var $tabs = $row.find('li');
	var size = $tabs.size();
	
	for (var i = 1; i < size; i++) {
		// if overflowing row, move elements to latest one
		if ($tabs.eq(i).offset().left < $tabs.eq(i -1).offset().left) {
			var $newRow = $('<ul></ul>').addClass('deki-dashboard-tabs');
			$tabs.slice(i).appendTo($newRow);
			$('<div></div>').addClass('clear').appendTo($newRow);

			// grow upwards
			$row.before($newRow);
			$row = $newRow;
		}
	}
};

// ajaxify dashboard links within root item
Deki.Plugin.UserDashboard._attachEvents = function($root) {
	$root = $root || $('body');

	var parsedDefault = parseUri(window.location);

	$root.find('a.deki-dashboard-link').each(function(){

		// already seen this link
		if ($(this).data('UserDashboard.links')) {
			return;
		}

		$(this).data('UserDashboard.links', true);

		// extract plugin name from link
		var href = $(this).attr('href');
		var plugin = Deki.Plugin.UserDashboard._getPluginName(href);

		if (plugin) {

			// link to current page with new anchor
			href = parsedDefault.path;
			href += (parsedDefault.query ? '?' + parsedDefault.query : '');
			href += '#' + Deki.Plugin.UserDashboard._anchorPrefix + plugin;

			$(this).attr('href', href).click(function(){
				Deki.Plugin.UserDashboard._loadPlugin(plugin);
			});
		}
	});
};

Deki.Plugin.UserDashboard._loadPluginFromHash = function(callback) {
	var parsed = parseUri(window.location);

	// url anchor format # + AnchorPrefix + plugin
	if (parsed.anchor) {
		var name = parsed.anchor.replace(Deki.Plugin.UserDashboard._anchorPrefix, '');
		var $plugin = Deki.Plugin.UserDashboard._getTab(name);

		if ($plugin.length) {
			// hide existing tabs to avoid flicker when switching
			Deki.Plugin.UserDashboard._showTab();
			Deki.Plugin.UserDashboard._loadPlugin(name, callback);
			return;
		}
	}
	
	// activate the default plugin - reorder it as needed
	var active = Deki.Plugin.UserDashboard._getActivePluginName();
	Deki.Plugin.UserDashboard._showTab(active);
	
	if (callback) {
		callback();
	}
};

// returns plugin name from uri
// @param uri - string or object (result of parseUri)
Deki.Plugin.UserDashboard._getPluginName = function(uri) {
	var parsed = typeof(uri) === "string" ? parseUri(uri) : uri;

	// User:Foo?view=home
	var key = Deki.Plugin.UserDashboard._queryKey;
	if (parsed && parsed.query && parsed.queryKey[key]) {
		return parsed.queryKey[key];
	}

	// User:Foo#view-home
	var prefix = Deki.Plugin.UserDashboard._anchorPrefix;
	if (parsed && parsed.anchor && parsed.anchor.indexOf(prefix) == 0) {
		return parsed.anchor.replace(prefix, '');
	}

	return null;
};

Deki.Plugin.UserDashboard._getTab = function(name) {
	return $('#deki-dashboard-tab-' + name);
};

Deki.Plugin.UserDashboard._getContent = function(name) {
	return $('#deki-dashboard-' + name);
};

Deki.Plugin.UserDashboard._startLoading = function(name) {
	$('#deki-dashboard').addClass('loading');

	$('ul.deki-dashboard-tabs li').removeClass('active');
	Deki.Plugin.UserDashboard._getTab(name).addClass('active');
};

Deki.Plugin.UserDashboard._stopLoading = function() {
	$('#deki-dashboard').removeClass('loading');
};

Deki.Plugin.UserDashboard._getActivePluginName = function() {
	var $active = $('ul.deki-dashboard-tabs li.active a.deki-dashboard-link').eq(0);

	if ($active.length > 0) {
		return Deki.Plugin.UserDashboard._getPluginName($active.attr('href'));
	}

	return null;
};

// load existing html into the cache
Deki.Plugin.UserDashboard._cacheDefaultPlugin = function() {
	var active = Deki.Plugin.UserDashboard._getActivePluginName();

	// mark as cached
	if (active) {
		Deki.Plugin.UserDashboard._pluginCache[active] = {
			name: active,
			appended: true
		};
	}
};

Deki.Plugin.UserDashboard._loadPlugin = function(name, callback) {
	var data = Deki.Plugin.UserDashboard._pluginCache[name];

	if (Deki.Plugin.UserDashboard._currentRequest) {
		Deki.Plugin.UserDashboard._currentRequest.abort();
		Deki.Plugin.UserDashboard._stopLoading();
	}

	// load after ajax request, or directly from cache
	if (!data) {
		Deki.Plugin.UserDashboard._startLoading(name);

		Deki.Plugin.UserDashboard._currentRequest = Deki.Plugin.UserDashboard._ajaxRequest( {
			'action': 'view',
			'plugin': name,
			// note: generated in html
			'userId': Deki.Plugin.UserDashboard.userId
		}, {}, function(response) {
			data = response.body;
			Deki.Plugin.UserDashboard._pluginCache[name] = data;
			Deki.Plugin.UserDashboard._renderPlugin(data);
			Deki.Plugin.UserDashboard._stopLoading();
			Deki.Plugin.UserDashboard._currentRequest = null;

			if (callback) {
				callback();
			}

		}, function(){
			Deki.Plugin.UserDashboard._stopLoading();
		});
	} else {
		Deki.Plugin.UserDashboard._renderPlugin(data);
		if (callback) {
			callback();
		}
	}
};

Deki.Plugin.UserDashboard._renderPlugin = function(data) {

	// not yet appended to page; include it
	if (!data['appended']) {
		data['appended'] = true;

		// plugin css immediately after dashboard framework css
		$dashboardCss = $('link[href*=user_dashboard.css]');

		$.each(data['css'], function(index, value) {

			// generate directly for IE compatibility
			var css = '<link rel="stylesheet" href="' + value + '" type="text/css" />';
			$dashboardCss.after(css);
		});

		// if user double-clicks tab, will double render
		Deki.Plugin.UserDashboard._getContent(data.name).remove();
		$('#deki-dashboard-contents').append(data['html_contents']);

		// ajaxify dashboard links within content
		$content = Deki.Plugin.UserDashboard._getContent(data.name);
		Deki.Plugin.UserDashboard._attachEvents($content);

		// js after content loaded
		$.each(data['js'], function(index, value) {
			$.getScript(value);
		});
	}

	// hide any error messages
	$('#sessionMsg').remove();
	Deki.Plugin.UserDashboard._showTab(data.name);
};

// show or hide tabs; pass null to hide all
Deki.Plugin.UserDashboard._showTab = function(name) {
	$('#deki-dashboard-contents').children('div').hide();
	$('ul.deki-dashboard-tabs').removeClass('active');
	$('ul.deki-dashboard-tabs li').removeClass('active');

	if (name) {

		// active the current tab
		Deki.Plugin.UserDashboard._getContent(name).show();
		var $tab = Deki.Plugin.UserDashboard._getTab(name);

		$tab.addClass('active');
		$tab.parent().addClass('active');

		// move to bottom of dashboard
		$tab.parent().appendTo($('#deki-dashboard-tab-area'));

		// set offsets for tab items
		var offset = {
			bottom: 0,
			bottomStep: 0,
			bottomMax: 0,

			right: 0,
			rightStep: 0,
			rightMax: 0
		};

		$('#deki-dashboard-tab-area ul').each(function(i, el){
			
			var $el = $(el);
			var $li = $el.find('li:first');

			// determine increment and max offset from css
			if (i == 0) {
				// reset any offsets, apply the style, measure, and remove the style
				$el.css('bottom', '');
				offset.bottomStep = parseInt($el.addClass('offset-default').css('bottom'));
				$el.removeClass('offset-default');
				
				$li.css('right', '');
				offset.rightStep = parseInt($li.addClass('offset-default').css('right'));
				$li.removeClass('offset-default');

				$el.css('bottom', '');
				offset.bottomMax = parseInt($el.addClass('offset-max').css('bottom'));
				$el.removeClass('offset-max');
				
				$li.css('right', '');
				offset.rightMax = parseInt($li.addClass('offset-max').css('right'));
				$li.removeClass('offset-max');
			}
			else {
				// increment but don't exceed offset
				offset.bottom = Math.min(offset.bottomMax, offset.bottom + offset.bottomStep);
				offset.right = Math.min(offset.rightMax, offset.right + offset.rightStep);
			}

			// set offsets for ul and li
			$el.css('bottom', offset.bottom + 'px');
			$el.find('li').removeClass('offset-default').css('right', offset.right + 'px');
		});

		// offset the content area to compensate
		$('#deki-dashboard-contents').css('position', 'relative').css('bottom', offset.bottom + 'px');
	}
};

// simple ajax wrapper: puts in defaults for tag plugin; has success and error
// callbacks
Deki.Plugin.UserDashboard._ajaxRequest = function(fields, settings, callback, error) {
	fields = fields || {};
	fields.formatter = 'user_dashboard';
	fields.language = Deki.PageLanguageCode;

	settings = settings || {};
	settings.type = settings.type || 'get';
	settings.url = settings.url || Deki.Plugin.AJAX_URL;
	settings.timeout = settings.timeout || 10000;
	settings.data = settings.data || fields;
	settings.dataType = settings.dataType || 'json';
	settings.error = function() {
		MTMessage.Show(wfMsg('error'), wfMsg('internal-error'));
	};

	settings.success = function(data, status) {
		if (!data.success) {
			MTMessage.Show(data.message, data.message);

			if (error) {
				error();
			}

			return;
		}

		if (callback) {
			callback(data);
		}
	};

	return $.ajax(settings);
};
