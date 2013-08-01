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

(function($) {
	$(function() {
		Deki.Plugin.PageTags.RefreshDom( {
			view: 'view'
		});

		// track the tags added during this editing session
		Deki.Plugin.PageTags._setInitialTags();
	});
})(Deki.$);

Deki.Plugin.PageTags = {};
Deki.Plugin.PageTags.ID = 'deki-page-tags';
Deki.Plugin.PageTags._saveRequest = null;
Deki.Plugin.PageTags._deleteRequest = null;
Deki.Plugin.PageTags._saveQueue = [];
Deki.Plugin.PageTags._deleteQueue = [];
Deki.Plugin.PageTags._initialTags = {};

Deki.Plugin.PageTags._setInitialTags = function() {
	Deki.Plugin.PageTags._initialTags = {};

	$('ul.tags a[class!=tag-delete]').each(function(){
		Deki.Plugin.PageTags._initialTags[$(this).attr('tagid')] = true;
	});
}

Deki.Plugin.PageTags._highlightTags = function() {
	$('ul.tags a[class!=tag-delete]').each(function() {
		if (!Deki.Plugin.PageTags._initialTags[$(this).attr('tagid')]) {
			$(this).addClass('highlight');
		}
	});
}

Deki.Plugin.PageTags._attachEvents = function(settings) {
	var $link = $('#deki-page-tags-toggleview');

	if ($link.hasClass('disabled')) {
		$link.click(function() {
			MTMessage.Show(wfMsg('error-permission-denied'),
					wfMsg('error-permission-details'));
			return false;
		});
	} else {
		// toggle view on click - setup new handler for new action
		var clickAction = settings.view == 'view' ? 'edit' : 'view';

		$link.unbind('click');
		$link.click(function() {
			Deki.Plugin.PageTags.Refresh(clickAction);
			return false;
		});
	}

	if (settings.view == 'view')
	{
		$('#deki-page-tags-edit-link').show();
	}
	else if (settings.view == 'edit') {

		// hook up close tags button
		$closeButton = $("#deki-page-tags-close");
		$closeButton.click(function() {
			Deki.Plugin.PageTags.Refresh('view');
			return false;
		});

		$('#deki-page-tags-edit-link').hide();

		// hook up autocomplete
		$('#deki-page-tags-add').unautocomplete();

		var autocomplete_url = Deki.Plugin.AJAX_URL;
		$('#deki-page-tags-add').autocomplete(autocomplete_url, {
			autoFill: false,
			mustMatch: false,
			matchSubset: true,
			matchContains: false,
			delay: 200,
			extraParams: {
			formatter: 'pagetags',
			action: 'getsitelist'
		},
		dataType: 'json',
		cacheLength: 1000,
		max: 1000,
		width: 310,
		multiple: false,
		minChars: 2,
		assumePrefixMatch: true,
		selectFirst: false,

		parse: function(result) {
			// result is json object
			var parsed = [];

			if (result.success) {
				var rows = result.body;
				for ( var i = 0; i < rows.length; i++) {
					var row = rows[i];
					parsed[parsed.length] = {
							data: row,
							value: row.title,
							result: row.value
					};
				}
			}

			return parsed;
		},

		// methods to render autocomplete
		formatItem: function(row, i, max) {
			var str = row.title;

			if (row.count > 1) {
				str += '<span class="count">' + row.count + '</span>';
			}

			return str;
		},
		formatMatch: function(row, i, max) {
			return row.title;
		},
		formatResult: function(row) {
			return row.value; // replace with value ("date:2010-10-10") vs
			// title
		}
		});

		$('#deki-page-tags-add').focus();
	}
};

//simple ajax wrapper: puts in defaults for tag plugin; has success and error callbacks
Deki.Plugin.PageTags.AjaxRequest = function(fields, settings, callback, error) {
	fields = fields || {};
	fields.formatter = 'pagetags';
	fields.language = Deki.PageLanguageCode;

	settings = settings || {};
	settings.type = settings.type || 'get';
	settings.url = settings.url || Deki.Plugin.AJAX_URL;
	settings.timeout = settings.timeout || 10000;
	settings.data = settings.data || fields;
	settings.dataType = settings.dataType || 'json';
	settings.error = function() {
		MTMessage.Show(wfMsg('error'), wfMsg('internal-error'));
		if (error) {
			error();
		}
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

Deki.Plugin.PageTags._buttonLabel = null;
Deki.Plugin.PageTags._startLoading = function() {
	$('#deki-page-tags-add').addClass('ac_loading');

	// button [text()] or input [val()] depending on ie6
	var $button = $('#deki-page-tags button.input-button');
	var $input = $('#deki-page-tags input.input-button');

	if (Deki.Plugin.PageTags._buttonLabel == null) {
		Deki.Plugin.PageTags._buttonLabel = $button.size() > 0 ? $button.text() : $input.val();
	}

	if ($button.size() > 0 ) {
		$button.text(wfMsg('adding-tags'));
	}

	if ($input.size() > 0) {
		$input.val(wfMsg('adding-tags'));
	}
}

Deki.Plugin.PageTags._stopLoading = function() {
	$('#deki-page-tags-add').removeClass('ac_loading');

	var $button = $('#deki-page-tags button.input-button');
	var $input = $('#deki-page-tags input.input-button');

	if ($button.size() > 0) {
		$button.text(Deki.Plugin.PageTags._buttonLabel);
	}

	if ($input.size() > 0) {
		$input.val(Deki.Plugin.PageTags._buttonLabel);
	}
}

Deki.Plugin.PageTags.RefreshDom = function(settings) {
	Deki.Plugin.PageTags._attachEvents(settings);

	// change link text
	var $link = $('#deki-page-tags-toggleview');
	var clickAction = settings.view == 'view' ? 'edit' : 'view';

	$link.text($link.attr(clickAction + 'text'));
};

// insert page contents into container; if only updating tag list, don't rebind events
Deki.Plugin.PageTags._setupPage = function(action, data, $container, isUpdate)
{
	isUpdate = isUpdate || false;

	$container.html(data.body);

	if (action == 'view') {
		Deki.Plugin.PageTags._setInitialTags();
		Deki.Plugin.PageTags.RefreshDom( {
			view: 'view'
		});
		return;
	}

	if (action == 'edit') {

		if (!isUpdate) {
			Deki.Plugin.PageTags.RefreshDom( {
				view: 'edit'
			});
		}

		var $input = $container.find('input[type=text]');

		// complete page load; refresh event handlers, etc.
		if (!isUpdate) {
			$container.find('form').bind('submit', function(ev) {
				var tag = $input.val();
				$input.val('');

				// request in progress; add tag to queue and abort
				if (Deki.Plugin.PageTags._saveRequest != null)
				{
					Deki.Plugin.PageTags._saveRequest.abort();
				}

				Deki.Plugin.PageTags._saveQueue.push(tag);
				Deki.Plugin.PageTags._startLoading();

				Deki.Plugin.PageTags._saveRequest = Deki.Plugin.PageTags.Save(Deki.Plugin.PageTags._saveQueue, function(data) {
					// optimization: saves return tag list html, refresh only that portion
					Deki.Plugin.PageTags._setupPage('edit', data, $('#deki-page-tags-edit'), true);
					Deki.Plugin.PageTags._stopLoading();
					Deki.Plugin.PageTags._saveRequest = null;
					Deki.Plugin.PageTags._saveQueue = [];
				}, function() {
					Deki.Plugin.PageTags._stopLoading();
					Deki.Plugin.PageTags._saveRequest = null;
					Deki.Plugin.PageTags._saveQueue = [];
					Deki.Plugin.PageTags.Refresh('edit');
				});

				return false;
			});
		}

		$input.focus();

		// bind to tag deletes; elements that have a tagid [may be span or a]
		$container.find('ul.tags li :first-child[tagid]').each(function(index) {

			// add tagDelete button to end of wrapping li...
			var $parent = $(this).parent();
			var $deleteIcon = $('<a href=""></a>');
			$deleteIcon.addClass("tag-delete")
				.attr("title", wfMsg('remove-tag'))
				.attr("tagid", $(this).attr("tagid"))
					// ... with handler to remove tag & refresh view
				.click(function() {
					$deleteIcon.addClass('loading');

					// request in progress; add tag to queue and abort
					if (Deki.Plugin.PageTags._deleteRequest != null)
					{
						Deki.Plugin.PageTags._deleteRequest.abort();
					}

					Deki.Plugin.PageTags._deleteQueue.push($(this).attr("tagid"));

					Deki.Plugin.PageTags._deleteRequest = Deki.Plugin.PageTags.Delete(Deki.Plugin.PageTags._deleteQueue,
						function(data) {
							$deleteIcon.removeClass('loading');
							// deletes return tag list html; refresh only that portion
							Deki.Plugin.PageTags._setupPage('edit', data, $('#deki-page-tags-edit'), true);
							Deki.Plugin.PageTags._deleteQueue = [];
						},
						function() {
							// error: refresh
							Deki.Plugin.PageTags.Refresh('edit');
							Deki.Plugin.PageTags._deleteQueue = [];
						}
					);
					return false;
				});

			$parent.prepend($deleteIcon);
		});

		Deki.Plugin.PageTags._highlightTags();
	}
}

Deki.Plugin.PageTags.Refresh = function(action) {
	Deki.Plugin.PageTags.AjaxRequest( {
		'pageId': Deki.PageId,
		'action': action
	}, {}, function(data) {
		Deki.Plugin.PageTags._setupPage(action, data, $('#' + Deki.Plugin.PageTags.ID), false);
	});
};

Deki.Plugin.PageTags.Delete = function(tagIdArray, callback, error) {
	return Deki.Plugin.PageTags.AjaxRequest( {
		'pageId': Deki.PageId,
		'action': 'delete',
		'tagIds': tagIdArray.join(',')
	}, {
		type: 'POST',
		timeout: 10000
	}, callback, error);
};

//submit list of tags from input box to api
Deki.Plugin.PageTags.Save = function(tagArray, callback, error) {
	return Deki.Plugin.PageTags.AjaxRequest( {
		'pageId': Deki.PageId,
		'action': 'save',
		'tags': tagArray.join('\n')
	}, {
		type: 'POST',
		timeout: 10000
	}, callback, error);
};

//support old tag dialog; assume params['tags'] has newline-separated tags
Deki.Plugin.PageTags.BulkSave = function(params, callback, error) {
	params = params || {}
	params['pageId'] = Deki.PageId;
	params['action'] = 'bulksave';

	Deki.Plugin.PageTags.AjaxRequest(params, {
		type: 'POST',
		timeout: 10000
	}, callback, error);
};


