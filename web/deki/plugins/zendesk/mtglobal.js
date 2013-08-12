/**
 * Zendesk MindTouch Connector
 * @version 1.0
 * @package zendesk-connector
 * @copyright MindTouch, Inc.
 */

/**
 * Default Configuration. Expects globals for
 *  - mtURL: server to check against (including trailing slash)
 *  - mtUser{name, email}: user object
 */

// convert username to mt format - in future, could use real name
if (typeof(mtUserName) === "string") {
	// backcompat: use the name
}
else if (typeof(mtUser) === "object" && mtUser.email) {
	mtUserName = mtUser.email.split("@")[0];
}
else if (typeof(currentUser) === "object" && currentUser.email) {
	mtUserName = currentUser.email.split("@")[0];
}
else {
	mtUserName = "";
}

var MindTouch = MindTouch || {};

MindTouch.escape = function(str) {
	return str.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
};

MindTouch.AJAX_ENDPOINT = 'deki/gui/plugin.php?formatter=zendesk&format=jsonp';
MindTouch.NEW_PAGE = 'Special:ZendeskNewPage';
MindTouch.SEARCH_PAGE = 'Special:Search?type=fulltext&search=';
MindTouch.SEARCH_API = '@api/deki/site/search';
MindTouch.SEARCH_STATS_API = '@api/deki/site/query/log/terms'; 
MindTouch.SEARCH_STATS_PAGE = 'Special:Reports?tab=search';
MindTouch.KB_PAGE = 'kb';
MindTouch.TIMEOUT = 7000;

MindTouch.LINK_HELP_POST = 'http://mndt.ch/zdpost';
MindTouch.LINK_HELP_SEARCH = 'http://mndt.ch/zdsearch';
MindTouch.LINK_USER_GUIDE = 'http://mndt.ch/zduserguide';
MindTouch.LINK_POWERED_BY = 'http://mndt.ch/zdpoweredby';

MindTouch.ICON_HELP = 'deki/plugins/zendesk/assets/question-white.png';

MindTouch.HTML_PASSWORD_ENTRY = "<form class='mt-login-form' action='#'><p><strong>Login to MindTouch</strong><br/><br/>Username: "+ MindTouch.escape(mtUserName) +"<br/>Password: <input type='password' id='mt-password' style='width: 120px;' /><div id='mt-login-status'></div><input id='mt-login' type='submit' class='button' name='Login' value='Login' /></p></form>";
MindTouch.HTML_MISSING_ACCOUNT = "<p>Your username <strong>\""+ MindTouch.escape(mtUserName) + "\"</strong> is not registered in MindTouch. <br/><br/>Please contact your MindTouch administrator after you <a href='"+ MindTouch.escape(mtURL) +"Special:UserRegistration' target='_blank'>create your account</a>.</p>";
MindTouch.HTML_KB_LINK = '<a href="' + mtURL + MindTouch.KB_PAGE + '" target="_blank">Browse KB</a>';
MindTouch.HTML_NO_RESULTS = '<ul class="options"><li>No results. <a href="http://mndt.ch/zdsearch" target="_blank">Learn more.</a></li><li>' + MindTouch.HTML_KB_LINK + '</li></ul>';


// populated by the login check
MindTouch.user = {
	isLoggedIn: false,
	username: null
};

// Zendesk uses jQuery ($j) and prototype ($). Access jQuery as $ within the closure
(function($, $p){
		
/**
 * Utility methods
 */
function makeHttps(url){
	return url.replace("http://", "https://");
}

/**
 * Remote User Authentication
 */

// Check username and set MindTouch.user as appropriate.
MindTouch.checkUserStatus = function(username, onComplete) {
	var url = mtURL + MindTouch.AJAX_ENDPOINT + '&action=status' + '&username=' + username + "&callback=?";
	
	$.ajax({
		url: makeHttps(url),
		dataType: 'json',
		success: function(data){
			MindTouch.user.isLoggedIn = data.body.user.isLoggedIn;
			MindTouch.user.username = data.body.user.username;
		
			if (onComplete) { onComplete(); }
		}
	});
};

// Attempt to login (Auth cookies are still set with AJAX requests)
MindTouch.login = function(username, password, onSuccess, onFailure) {
	var url = mtURL + MindTouch.AJAX_ENDPOINT + '&action=login' + '&username=' + username + '&password=' + password + "&callback=?";
	
	$.ajax({
		url: makeHttps(url),
		dataType: 'json',
		success: function(data){
			MindTouch.user.isLoggedIn = data.success;
			MindTouch.user.username = username;

			if (data.success) {
				if (onSuccess) { onSuccess(); }
			}
			else {
				if (onFailure) { onFailure(); }
			}
		}
	});
};

/**
 * Widget rendering
 */
MindTouch.render = function() {
	MindTouch.renderPostWidget();
	MindTouch.renderSearchWidget();
	
	// disabled for now -- v2 feature
	// MindTouch.renderSearchStatsWidget();
};

MindTouch.attachHelpIcon = function($el, url) {
	if ($el.data('helpicon')) {
		return;
	}
	
	
	var $title = $el.siblings('h3');
	if ($title.length == 0) {
		return;
	}
	
	var $image = $('<img></img>').attr('src', makeHttps(mtURL + MindTouch.ICON_HELP));
	var $link = $('<a class="mt-help-icon" target="_blank"></a>').attr('href', url).append($image);
	
	$title.append($link);
	
	$el.data('helpicon', true);
};

MindTouch.attachFooter = function($el) {
	$parent = $el.parent();
	if ($parent.data('footer')) {
		return;
	}
	
	var $bottomBar = $('<div class="mt-widget-footer">Powered by <a href="' + MindTouch.LINK_POWERED_BY + '" target="_blank">MindTouch</a></div>');	
	$parent.append($bottomBar);
	
	$parent.data('footer', true);
};

MindTouch.renderLoginForm = function($el, onSuccess) {
	var loginFn = function() {
		MindTouch.login(mtUserName, $('#mt-password').val(), onSuccess, function() {
			$('#mt-login-status').text('Incorrect password').css('color', 'red');
			
			setTimeout(function(){
				$('#mt-login-status').text('');
			}, 2000);
		});
		return false;
	};
	
	var $form = $(MindTouch.HTML_PASSWORD_ENTRY);
	$form.submit(loginFn);
	
	$el.html($form);
};

MindTouch.renderLoginLink = function($el, text, onSuccess) {
	if (!MindTouch.user.username) {
		$el.html(MindTouch.HTML_MISSING_ACCOUNT);
		return;
	}
	
	var $link = $('<a></a>').text(text).click(function(){
		MindTouch.renderLoginForm($el, onSuccess);
	});
	$el.html($link);
};

MindTouch.renderPostWidget = function() {
	var $widget = $('#mt-widget-post');
	
	if ($widget.length == 0 || $widget.data('loaded')) {
		return;
	}
	
	MindTouch.attachHelpIcon($widget, MindTouch.LINK_HELP_POST);
	MindTouch.attachFooter($widget);
	
	if (!MindTouch.user.isLoggedIn) {
		MindTouch.renderLoginLink($widget, 'Login to post to MindTouch', MindTouch.render);
		return;
	}

	var $form = $('<form target="_blank" action="#" method="post" id="mt-widget-post-form"></form>');
	$widget.html($form);
	
	$.ajax({
		url: '/tickets/' + ticket_id + '.xml',
		dataType: 'text',
		success: function(data){
			
			$parsed = $(data);

			$form.attr('action', mtURL + MindTouch.NEW_PAGE);
			$button = $('<input type="submit" value="Post Article" class="button">');
			$ticketid = $('<input type="hidden" name="ticket_id">').val(ticket_id);
			$title = $('<input type="hidden" name="newpage_title">').val($parsed.find('subject').text());
			$body = $('<input type="hidden" name="newpage_body">').val($parsed.find('description').text());
			$tags = $('<input type="hidden" name="newpage_tags">').val($parsed.find('current-tags').text());
			$xml = $('<input type="hidden" name="newpage_xml">').val(data);

			$form.append($button).append($ticketid).append($title).append($body).append($tags).append($xml);
		}
	});
	
	MindTouch.attachFooter($widget);
	$widget.data('loaded', true);
};

MindTouch.renderSearchWidget = function() {
	var $widget = $('#mt-widget-search');
	
	if ($widget.length == 0 || $widget.data('loaded')) {
		return;
	}
	
	var escapeSearch = function(term) {
		
		var escaped = [
			'\\',
			'"',
			':',
			'+',
			'-',
			'&&',
			'||',
			'!',
			'(',
			')',
			'{',
			'}',
			'[',
			']',
			'^',
			'"',
			'~',
			'*',
			'?'
		];
		
		// split on invalid chars, add escaping slash, merge
		$.each(escaped, function(i, c) {
			term = $.map(term.split(c), function(el){
				if (el == "") { el = "\\" + c; }
				return el;
			}).join('');
		});
		
		return term;
	};
	
	MindTouch.searchTimeout = null;
	var performSearch = function(term, $el) {
		
		$spinner = $('<img src="https://developer.mindtouch.com/skins/common/icons/anim-circle.gif" alt="Searching" />');
		$el.html($spinner);
		
		// note: dream.out.pre is =? so it's a JSONP request
		var url = mtURL + MindTouch.SEARCH_API + "?dream.out.format=jsonp&dream.out.pre=?&limit=5&sortby=-date,-rank&q=" + encodeURI(escapeSearch(term)) + "&constraint=+namespace:main";
		
		// default to no results if doesn't return
		var timeout = setTimeout(function(){
			$el.html(MindTouch.HTML_NO_RESULTS);
		}, MindTouch.TIMEOUT);
		
		$.ajax({
		  url: makeHttps(url),
		  dataType: 'json',
		  success: function(data){
				clearTimeout(timeout);
				
				if (!data.search || !data.search.page) {
					$el.html(MindTouch.HTML_NO_RESULTS);
					return;
				}
				
				// results can be an object or single item
				var pages = data.search.page instanceof Array ? data.search.page : [data.search.page];
				var $ul = $('<ul class="options"></ul>');
				$.each(pages, function(i, page){
					if (page.title) {
						var uri = page["uri.ui"] + "";
						$li = $('<li class="link"></li>');
						$a = $('<a target="search"></a>').attr('title', page.title).attr('href', uri).text(page.title);
						$li.append($a);
						$ul.append($li);
					}
				});
				
				$li = $('<li class="mt-see-results"></li>').append(
					$('<a target="_blank"></a>')
						.attr('href', mtURL + MindTouch.SEARCH_PAGE + encodeURIComponent($inputBox.val()))
						.html('See All Results')
				).append(' | ').append(MindTouch.HTML_KB_LINK);
				
				$ul.append($li);
				
				$el.html($ul);
			}
		});
	};

	$results = $('<div id="mt-widget-search-results"></div>');
	$inputBox = $('<input type="text" name="mt-widget-serach" class="mt-widget-search" />').keyup(function() {
		if (MindTouch.searchTimeout != null) {
			clearTimeout(MindTouch.searchTimeout);
		}
	
		var $this = $(this);
		MindTouch.searchTimeout = setTimeout(function(){
			MindTouch.searchTimeout = null;
			performSearch($this.val(), $results);	
		}, 250);
	});
	
	$button = $('<input type="submit" name="Search" value="Search" class="button" />').click(function(){
		$inputBox.trigger('keyup');
	});

	// clear any existing messages
	$widget.html('');
	$widget.append($inputBox);
	$widget.append($button);
	$widget.append('<br/>');
	$widget.append($results);
		
	MindTouch.attachFooter($widget);
	MindTouch.attachHelpIcon($widget, MindTouch.LINK_HELP_SEARCH);
	
	// set default input box
	$params = $.queryParameters();
	$titleBox = $('#ticket_subject');
	
	if ($params['query']) {
		$inputBox.val($params['query']);
	} else if ($titleBox.length > 0) {
		$inputBox.val($titleBox.val());
	}
	
	$inputBox.trigger('keyup');
	$widget.data('loaded', true);
};

MindTouch.renderSearchStatsWidget = function() {
	var $widget = $('#mt-widget-searchstats');
	
	if ($widget.length == 0 || $widget.data('loaded')) {
		return;
	}
	
	if (!MindTouch.user.isLoggedIn) {
		MindTouch.renderLoginLink($widget, 'Login to see popular searches', MindTouch.render);
		return;
	}
	
	var $results = $('<div id="mt-widget-searchstats-results"></div>');
	
	var url = mtURL + MindTouch.SEARCH_STATS_API + "?dream.out.format=jsonp&dream.out.pre=?&limit=5";
	$.ajax({
		url: makeHttps(url),
		dataType: 'json',
		success: function(data){
			if (data.terms && data.terms["@count"] > 0) {
				$ul = $('<ul class="options"></ul>');
				$.each(data.terms.term, function(i, term){
					var text = term["#text"];
					var count = term["@count"];

					if (text && count) {
						$li = $('<li class="link"></li>');
						$a = $('<a target="_blank"></a>').attr('href', mtURL + MindTouch.SEARCH_PAGE + encodeURIComponent(text)).text(MindTouch.escape(text) +" (" + count +")");
						$li.append($a);
						$ul.append($li);
					}
				});

			} else {
				$ul = "No results.";
			}
		
			$results.html($ul);
		}
	});
	
	$widget.html($results);
	$bottomBar = $('<div class="mt-widget-footer"><a href="' + mtURL + MindTouch.SEARCH_STATS_PAGE + '" target="_blank">Search Analytics</a></div>');
	$widget.append($bottomBar);
	
	$widget.data('loaded', true);
};

/** 
 * Global setup
 */

// check status & render after widgets are loaded
$p(document).observe('widgets:load', function(){
	var style = "";
	style += " .mt-widget-footer {border-top: 1px solid #ccc; padding-top: 5px; margin-top: 3px; font-size: 10px; font-family: Verdana, Sans-serif;} .mt-widget-footer a {color: #912b1d; text-decoration: underline;}";
	style += " #mt-widget-post .mt-widget-footer { border: none; }";
	style += ' input.mt-widget-search, input.mt-widget-search:focus {width: 100px; margin-right: 5px; padding-left: 20px; -moz-border-radius: 5px; -webkit-border-radius: 5px; border-radius: 5px; background: url("/images/searchinput.gif") no-repeat scroll -210px 50% white; }';
	style += ' #mt-widget-search-results {margin-top: 5px;} .mt-see-results {font-weight: bold;}';
	style += ' .mt-help-icon {margin-left: 5px;}';
	style += ' .mt-guide-icon img {margin-right: 3px;}';
	style += ' .mt-login-form {position: relative;} #mt-login {margin-top: -10px;} #mt-login-status{position: absolute; margin: -10px 0px 0px 60px; width: 120px; } '
	
	$("<style type='text/css'>" + style + "</style>").appendTo("head");
	
	MindTouch.checkUserStatus(mtUserName, MindTouch.render);
});

})(jQuery, $);

