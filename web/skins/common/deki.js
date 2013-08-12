
if (typeof Deki == 'undefined') {
	var Deki = {};
}
var DekiWiki = Deki; // backwards compatibility

// Log
Deki.Log = function(m) {
	if (typeof console != "undefined" && console.log) {
		console.log(m);
	}
};
var dl = Deki.Log;

// Ui
if (typeof Deki.Ui == 'undefined') {
	Deki.Ui = {};
}

// yellowbox exceptions
Deki.Ui.Message = function(title, message, details) {
	MTMessage.Show(title, message, 'ui-errormsg', details);
};

Deki.Ui.DisabledMessage = function() {
	MTMessage.Show(wfMsg('error-permission-denied'), wfMsg('error-permission-details'));
};

// Determine whether this element (probably a link) is disabled (directly or via parent)
Deki.Ui.IsDisabled = function(el) {
	return $(el).hasClass('disabled') || $(el).parents('.disabled').length > 0;
};

// generate a flash message
Deki.Ui.Flash = function(messageHtml, type) {
	// find the message container
	var $flash = $('#sessionMsg');
	var $ul = $flash.find('ul');
	// clear messages
	$ul.empty();
	// add default classes
	$flash.addClass('msg systemmsg');
	$flash.removeClass('errormsg successmsg');
	// set the message type
	var flashClass = 'successmsg';
	switch (type) {
		case 'error':
			flashClass = 'errormsg';
			break;
		default:
	}
	$flash.addClass(flashClass);
	// create the message
	$('<li></li>').html(messageHtml).appendTo($ul);
};

Deki.Ui.EmptyFlash = function() {
	var $flash = $('#sessionMsg');
	$flash.removeAttr('class');
	var $ul = $flash.find('ul');
	// clear messages
	$ul.empty();
};

// Gui
if (typeof Deki.Gui == 'undefined') {
	Deki.Gui = {};
}

Deki.Gui.ROOT_PATH = '/deki/gui';

Deki.Gui.Status = {
		ERROR: 0,
		OK: 200,
		ERROR_LOGIN: 401,
		ERROR_COMMERCIAL: 402
};

//@param Object options - ajax options
Deki.Gui.AjaxRequest = function(options) {
	// defaults
	options.type = options.type || 'get';
	options.timeout = options.timeout || 10000;
	options.dataType = options.dataType || 'json';

	var success = null;
	if (options.success)
		success = options.success;

	// insert success handler
	options.success = function(data, status, XmlHttpRequest) {
		if (Deki.Gui.handleResponse(data, XmlHttpRequest, options)) {
			if (success)
				success(data, status, XmlHttpRequest);
		}
	};

	// insert error handler if none exists
	if (typeof options.error == "undefined") {
		options.error = function(XmlHttpRequest, status, error) {
			Deki.Log(XmlHttpRequest);

			Deki.Ui.Message(wfMsg('error'), wfMsg('internal-error'));
		}; 
	}

	// execute the request
	$.ajax(options);
};

/**
 * @param Object data - expected fields: (bool)success
 * @return bool
 */
Deki.Gui.handleResponse = function(data, XmlHttpRequest, options) {
	if (options.dataType == 'xml') {
		var $data = $(data);
		if ($data.find('formatter[success=1]').length)
			return true;

		Deki.Ui.Message($data.find('formatter').attr('message'), $data.find('body').val());
	} else {
		// assume json
		if (data.success)
			return true;

		Deki.Ui.Message(data.message, data.body ? data.body : '');
	}
};

// Plugin
if (typeof Deki.Plugin == 'undefined') {
	Deki.Plugin = {};
}

Deki.Plugin.AJAX_URL = Deki.Gui.ROOT_PATH + '/plugin.php';

/**
 * @param string formatter - name of the plugin formatter to use
 * @param Object options - ajax options 
 */
Deki.Plugin.AjaxRequest = function(formatter, options) {
	// defaults
	options.url = options.url || Deki.Plugin.AJAX_URL;
	// set formatter format
	if (options.dataType == 'xml') {
		options.data.format = 'xml';
	}

	// field defaults
	var fields = options.data || {};
	fields.formatter = formatter;
	if (!fields.language)
		fields.language = Deki.PageLanguageCode;

	options.data = fields;

	// execute the request
	Deki.Gui.AjaxRequest(options);
};

if (typeof Deki.Plugin.Comments == 'undefined') {
	Deki.Plugin.Comments = {};
}

/**
 * Update element with list of page comments
 * @param Object $el - jQuery element to update (optional, default #comments)
 */
Deki.Plugin.Comments.Update = function($el) {
	
	if (!$el) {
		$el = $('#comments');
	}
	
	$.get('/deki/gui/comments.php',
		{
			'action'  : 'show',
			'titleId' : Deki.PageId,
			'commentCount' : 'all'
		},
		function(data) {
			$el.html(data);
		},
		'html'
	);
};

// @param Function callback - prototype like function(event, arg1, arg2)
Deki.Plugin.Subscribe = function(event, callback) {
	$(document).bind(event, callback);
};

Deki.Plugin.Unsubscribe = function(event, callback) {
	$(document).unbind(event, callback);
};

// @param Array args - must be an array, otherwise unexpected results
Deki.Plugin.Publish = function(event, args) {
	$(document).trigger(event, args);
};
