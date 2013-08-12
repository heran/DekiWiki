
if (typeof Deki.Plugin == 'undefined') {
	Deki.Plugin = {};
}

if (typeof Deki.Plugin.FilesTable == 'undefined') {
	Deki.Plugin.FilesTable = {};
}

$(function()
{
	// general purpose onclick menu hiding
	$(document).click(function() {
		$('ul.deki-file-menu').hide();
	});
	
	Deki.Plugin.FilesTable._attachEvents();
});

Deki.Plugin.FilesTable.ID = 'pageFiles';
Deki.Plugin.FilesTable.EVENT_REFRESH_TABLE = 'FilesTable.onRefreshTable';
Deki.Plugin.FilesTable.EVENT_REFRESH_ROW = 'FilesTable.onRefreshRow';

Deki.Plugin.FilesTable.Refresh = function(pageId) {

	// allow custom endpoints
	var refreshUrl = Deki.Plugin.AJAX_URL;

	// load the revisions
	var fields = {
		'formatter': 'filestable',
		'page_id': pageId || Deki.PageId,
		'action': 'html_refresh'
	};

	$.ajax({
		type: 'get',
		url: refreshUrl,
		dataType: 'json',
		data: fields,
		success: function(data, status) {
			if (!data.success) {
				MTMessage.Show(data.message, data.message);
				return;
			}
			
			var $container = $('#' + Deki.Plugin.FilesTable.ID);
			$container.html(data.body);
			Deki.Plugin.FilesTable._attachEvents();

			// notify subscribers
			Deki.Plugin.Publish(Deki.Plugin.FilesTable.EVENT_REFRESH_TABLE, [$container]);
		}
	});	
};

// @access private
Deki.Plugin.FilesTable._attachEvents = function($context) {
	var $table = $context || $('#attachTable');
	
	// hook events
	$table.find('a.deki-file-actions').click(clickFileActions);
	$table.find('a.deki-file-revisions').click(clickFileRevisions);
	showWebDavLinks();

	// editable descriptions
	$table.find('.deki-editable').editable({
		url: Deki.Plugin.AJAX_URL,
		field: 'description',
		multiLine: true,
		fields: {
			'formatter': 'filestable',
			'action': 'set_description',
			'file_id': null
		},
		onDisplayValue: function($el, value) {
			if ($el.hasClass('nodescription')) {
				return '';
			} else {
				return value;
			}
		},
		onGenerateRequest: function($el, options) {
			// @note 22 => deki-file-description-XXX
			var fileId = String($el.attr('id')).substr(22);
			options.fields.file_id = fileId;
		},
		onSuccess: function($el, old, response) {
			if (Deki.Gui.handleResponse(response)) {
				if (response.body == '') {
					$el.addClass('nodescription');
					return response.message;
				} else {
					$el.removeClass('nodescription');
					return response.body;
				}
			}

			return old;
		}
	});

	// helpers
	function clickFileRevisions(e) {
		var $this = $(this);
		// deki-file-revisions-XXX
		var fileId = String($this.attr('id')).substr(20);

		if ($this.data('loadedRevisions')) {
			toggleRevisions($this, fileId);
			return false;
		}

		// load the revisions
		var fields = {
			'formatter': 'filestable',
			'file_id': fileId,
			'action': 'html_revisions'
		};
		// allow additional fields
		if ($this.attr('href') != '#') {
			var moreFields = String($this.attr('href')).substring(1);
			moreFields = parseQueryString(moreFields);
			$.extend(fields, moreFields);
		}

		// add loading class
		$this.addClass('loading');

		$.ajax({
			type: 'get',
			url: Deki.Plugin.AJAX_URL,
			dataType: 'json',
			data: fields,
			success: function(data, status) {
				$this.removeClass('loading');
				if (!data.success) {
					MTMessage.Show(data.message, data.message);
					return;
				}
				
				$this.data('loadedRevisions', true);
				var $tr = $this.parent().parent();
				$tr.after(data.body);
				var $rows = $table.find('tr.group-'+fileId);
				// set the new classes
				$rows.addClass($tr.attr('class')).removeClass('groupparent');
				// bind new events
				Deki.Plugin.FilesTable._attachEvents($rows);
				// notify subscribers
				Deki.Plugin.Publish(Deki.Plugin.FilesTable.EVENT_REFRESH_ROW, [$rows]);
				
				toggleRevisions($this, fileId);
			},
			error: function() {
				$this.removeClass('loading');
			}
		});
		
		return false;
	};
	
	function clickFileActions(e) {
		var $this = $(this);
		// deki-file-actions-XXX
		var fileId = String($this.attr('id')).substr(18);

		var $menu = $('#deki-file-menu-'+fileId);
		if ($menu.length < 1) {
			// attach to menuFiller
			$menu = $this.siblings('ul.deki-file-menu');
			$menu.attr('id', 'deki-file-menu-'+fileId);
			$('#menuFiller').append($menu);
			$menu.hide();
		}

		bindMenuEvents(fileId, $menu);
		toggleMenu($menu, $this);
		
		return false;
	};
	
	function toggleRevisions($icon, fileId) {
		$hide = $icon.find('.hide');
		$show = $icon.find('.show');
		if ($hide.is(':visible')) {
			$table.find('.group-'+fileId).hide();
			$hide.hide();
			$show.show();
		} else {
			$table.find('.group-'+fileId).show();
			$hide.show();
			$show.hide();
		}
	};

	function bindMenuEvents(fileId, $menu) {
		if (!$menu.data('fileId')) {
			$menu.data('fileId', fileId);
	
			if ($menu.hasClass('disabled')) {
				// disabled event
				$menu.find('li').click(function() { return false; });
				return;
			}
			
			// general quickpopups
			$menu.find('.quickpopup').click(function() {
				if ($(this).hasClass('disabled'))
					return false;

				toggleMenu($menu);
				var el = $(this).find('a').get();
				Deki.Plugin.FilesTable.QuickPopupFrom(el);
				return false;
			});
			
			// specific popups	
			$menu.find('.move').click(function() {
				toggleMenu($menu);
				doPopupMoveAttach(Deki.PageId, fileId);
				return false;
			});
			
			$menu.find('.description').click(function() {
				toggleMenu($menu);
				$('#deki-file-description-'+fileId).click();
					return false;
			});
	
			$menu.find('.delete').click(function() {
				toggleMenu($menu);
				doPopupDeleteAttach(Deki.PageId, fileId);
				return false;
			});
		}
	};
	
	function toggleMenu($menu, $menuLink) {
		if ($menu.is(':visible')) {
			// body click to hide open menus
			$(document).click();
		} else {
			$(document).click();
			// align the menu to the right
			var linkOffset = $menuLink.offset();
			$menu.css('left', (linkOffset.left + $menuLink.outerWidth()) - $menu.outerWidth());
			$menu.css('top', linkOffset.top + $menuLink.outerHeight());

			$menu.show();
		}
	};

	function parseQueryString(s) {
		var fields = {};
		var keyVals = s.split(/&/);
		$.each(keyVals, function(i, val) {
			split = String(val).split(/=/);
			fields[split[0]] = split[1];
		});
		return fields;
	};

	// office write back capabilities
	function showWebDavLinks() {
		$.each(jQuery.browser, function(browser) {
			if (browser == 'msie') {
				$table.find('a.deki-webdavdoc').css('display', 'inline'); 	
			}
		});
	};
};

Deki.Plugin.FilesTable.QuickPopupFrom = function(el, width, height)
{
	Deki.QuickPopup.Show({
		title: $(el).attr('title') || null,
		'width': width,
		'height': height,
		url: $(el).attr('href')
	});
};

// todo: scope
function loadOfficeDoc(url) {
	if (window.ActiveXObject) {
		var ed; 
		try {
			ed = new ActiveXObject('SharePoint.OpenDocuments.1');
		} catch(err) {
			window.alert('Unable to create an ActiveX object to open the document. This is most likely because of the security settings for your browser.');
			return false;
		}
		
		if (ed) {
			ed.EditDocument(url);
			return false;
		} else {
			window.alert('Cannot instantiate the required ActiveX control to open the document. This is most likely because you do not have Office installed or you have an older version of Office.');
			return false;
		}
	} else {
		window.alert('Internet Explorer is required to use this feature');
	}
	return false;
}
