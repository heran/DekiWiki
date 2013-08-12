var Deki = Deki || {};
Deki.QuickPopup = {};

Deki.QuickPopup.Show = function(opts) {
	var opts = opts || {};
	
	var title = opts.title || '';
	var width = opts.width || 400;
	var height = opts.height || 300;
	var url = opts.url || null;
	
	if (url) {
		var separator = url.indexOf('?') >= 0 ? '&' : '?';
		var popupUrl = url + separator + 'popup=true' + '#TB_iframe&width=' + width + '&height=' + height;
		tb_show(title, popupUrl);
	} else {
		throw "QuickPopup: No href provided";
	}
};

Deki.QuickPopup.Hide = function() {
	if (parent) {
		parent.tb_remove();
	}
	else {
		tb_remove();
	}
};

Deki.QuickPopup.Redirect = function(url) {
	// popups are inside iframe by default; common case is to redirect outer parent
	var w = parent ? parent.window : window;
	w.location.href = url;
};

