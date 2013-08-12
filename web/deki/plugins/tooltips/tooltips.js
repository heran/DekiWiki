if (typeof Deki == 'undefined') {
	var Deki = {};
}

if (typeof Deki.Ui == 'undefined') {
	Deki.Ui = {};
}

if (typeof Deki.Gui == 'undefined') {
	Deki.Gui = {};
}

(function(){
	
	$(function() {
		refreshMessageTips();
	});
	
	/**
	 * Immediately generate a tooltip message on an element
	 * @param Object el - DOM or jQuery element
	 * @param Object options - options object, with type one of {'login', 'commercial'}
	 */
	Deki.Ui.MessageTip = function(el, options) {
		options = options || {};
		
		switch (options.type) {
			case 'login':
				addMessageTip(el, wfMsg('login-required'), 'light');
				break;

			case 'commercial':
				addMessageTip(el, wfMsg('commercial-required'), 'dark');
				break;
		}
	};
	
	// tooltips override original handleResponse to display tooltips
	var handleResponseOriginal = Deki.Gui.handleResponse;

	/**
	 * @param Object data - expected fields: context (DOM Object to receive tip)
	 * @return bool
	 */
	Deki.Gui.handleResponse = function(data, XmlHttpRequest, options) {
		if (data.success) {
			return true;
		}
	
		var $el = $(options.context);
	
		switch (data.status) {
	
			// attach tooltips if possible, otherwise yellowbox
			case Deki.Gui.Status.ERROR_LOGIN:
				if ($el.length > 0)
					Deki.Ui.MessageTip($el, {type: 'login'});
				else
					Deki.Ui.Message(wfMsg('login-required'), '');
	
				return false;
	
			case Deki.Gui.Status.ERROR_COMMERCIAL:
				if ($el.length > 0)
					Deki.Ui.MessageTip($el, {type: 'commercial'});
				else 
					Deki.Ui.Message(wfMsg('commercial-required'), '');
		
				return false;
	
			default:
				return handleResponseOriginal(data, XmlHttpRequest, options);
		}
	};
	
	/**
	 * Private methods
	 */
	var $lastMessageTip = null;
	function addMessageTip(el, content, styleName) {
		var $el = $(el);
		
		// clear last message tip, if any
		if ($lastMessageTip) {
			$lastMessageTip.qtip('destroy');
		}
	
		$lastMessageTip = $el;
	
		$el.qtip({
			content: content,
			position: {
				corner: {
					tooltip: 'topRight',
					target: 'bottomLeft'
				}
			},
	
			// show tooltip immediately
			show: { ready: true },
	
			// use custom hide event when body clicked (note: unfocus event not working)
			hide: false,
			api: {
				onRender: function() {
					$('body').click(function() { $el.qtip('destroy'); });
				}
			},
			style: {
				border: {
					width: 2,
					radius: 5
				},
				width: 250,
				padding: 5,
				textAlign: 'center',
				tip: true,
				name: styleName
			}
		});
	};

	function refreshMessageTips() {
		// remove existing handlers and only allow tooltip
		$('.disabled-login a, a.disabled-login').each(function(){
			$(this).removeAttr('onclick').unbind('click')
				.click(function() {
					Deki.Ui.MessageTip($(this), {type: 'login'});
					return false;
				});
		});
	
		$('.disabled-commercial a, a.disabled-commercial').each(function(){
			$(this).removeAttr('onclick').unbind('click')
				.click(function() {
					Deki.Ui.MessageTip($(this), {type: 'commercial'});
					return false;
				});
		});
	};
})();
