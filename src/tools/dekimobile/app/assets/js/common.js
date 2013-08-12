/*
 * Debugging functions
 *
 */


$(document).ajaxError(function(){
	if (window.console && window.console.error) {
		console.error(arguments);
	}
});


/*
 * Global variables
 *
 */

initialPageLoad = true;
		
var postCommentOptions = { 
	target: '',	 // target element(s) to be updated with server response 
	beforeSubmit:	showPostCommentRequest,	// pre-submit callback 
	success:			 showPostCommentResponse,	// post-submit callback 
	resetForm: true,				// reset the form after successful submit 
	error: showPostCommentError
	
}; 

var deleteCommentOptions = {
	target: '',
	beforeSubmit: showDeleteCommentRequest,
	success: showDeleteCommentResponse,
	error: showDeleteCommentError
};

var postNoteOptions = {
	target: '',
	beforeSubmit: showNoteRequest,
	success: showNoteResponse,
	resetForm: false,
	error: showNoteError
};

var addTagOptions = {
	target: '',
	beforeSubmit: showAddTagRequest,
	success: showAddTagResponse,
	resetForm: true,
	error: showAddTagError

};

var loginOptions = {
	target: '',
	beforeSubmit: showLoginRequest,
	success: showLoginResponse,
	resetForm: true,
	error: showLoginError
};

var logoutOptions = {
	target: '',
	beforeSubmit: showLogoutRequest,
	success: showLogoutResponse,
	resetForm: false,
	error: showLogoutError

};

/*
 *	Form callbacks
 *
 */

function showLoginRequest(formData, jqForm, options)
{
	var loginForm = jqForm[0];
	if(!loginForm.username.value || !loginForm.password.value)
	{
		cleanupErrorMsgs('div.error');
		showErrorMsg('div.login', '<div class="error"> Both a user name and password are required.</div>', "");
		return false;

	}
	return true;

}

function showLoginResponse(responseText, statusText)
{
	if(jQuery.trim(responseText)[0] == '4')
	{
		cleanupErrorMsgs('div.error');
		showErrorMsg('div.login', '<div class="error"> Unknown username or password.</div>', "");
	}
	else
	{
		var returnTo = responseText;
		location.href= returnTo;
	}
}

function showLoginError(error)
{
	showErrorMsg('div.login', '<div class="error">Login failed. Please check your username and password and try again.</div>', "");
}

function showLogoutRequest(formData, jqForm, options)
{
}

function showLogoutResponse(responseText, statusText)
{
	if(!serverError(responseText))
	{
		alert('You have successfully logged out.');
		location.href = responseText; // reload the page
	}
	else
	{
		showErrorMsg('div.js-main','<div class="error">Log out failed. Please try again.</div>', "");
	}
}

function showLogoutError(error)
{
	showErrorMsg('div.js-main','<div class="error">Log out failed. Please try again.</div>', "");
}


function showNoteRequest(formData, jqForm, options)
{
 cleanupErrorMsgs('div.error');
	var form = jqForm[0];
	var noteKnown = noteKnownUser(form.to.value);
	if(!noteKnown)
	{
		showErrorMsg('div.js-main', '<div class="error"> Unknown user "' + form.to.value + '" </div>', "");

		return false;
	}
	else
	{
		form.to.value = noteKnown;
		var i;
		for(i = 0; i < formData.length ; i++)
		{
			if(formData[i].name == 'to')
			{
				formData[i].value = noteKnown;
			}
		}

	}
	if(form.message.value == '')
	{
		if(!confirm('Send a blank message?'))
		{
			return false;
		}
	}
	return true;
	//return false;
}

function showNoteResponse(responseText, statusText)
{
 cleanupErrorMsgs('div.error');
 if(serverError(responseText))
 {
	showErrorMsg('div.js-main', '<div class="error">Your note failed to be sent. Please check the username/email and try again.</div>', "");
		return;
 }
 location.href = responseText;
}

function showNoteError(error)
{
	showErrorMsg('div.notes', '<div class="error">Your note failed to be sent. Please check the username/email and try again.</div>', "");

}


function showDeleteCommentRequest(formData, jqForm, options)
{// presubmit
	if(confirm('Are you sure you want to delete this comment?'))
	{
		//var queryString = $.param(formData); 
		return true; 
	}
	return false;
}

function showDeleteCommentResponse(responseText, statusText)
{//postsubmit
	cleanupErrorMsgs('div.error');
	if(serverError(responseText))
	{
		showErrorMsg('div.addcomment', '<div class="error"> Error deleting comment. Please try again.</div>', 'comments');
		return;
	}

	resetCommentList(responseText.substr(4)); // strip off http response code
	// rebind event handler to new forms
	$('#commentlist').bind('submit',$('.deleteCommentForm').ajaxForm(deleteCommentOptions));
	initialPageLoad = true;
}

function showDeleteCommentError(error)
{
		showErrorMsg('div.addcomment', '<div class="error"> Error deleting comment. Please try again.</div>', 'comments');
}



function showAddTagRequest(formData, jqForm, options)
{
	var queryString = $.param(formData);
	return true;
}

function showAddTagResponse(responseText, statusText)
{
 //are all of these necessary?
	getTab(tabOptions['related']);	
	
	cleanupErrorMsgs('div.error');
		$('div.message, li.js-pages, li.js-tags').bind('click', highlight);
}

function showAddTagError(error)
{
	$('div.js-main').insertAfter('<div class="error"> Error adding tag. Please try again. </div>');
}




/*
 * Form callback helper functions
 *
 */

function resetCommentList(responseText)
{//deletes comment, recolors comment list 
	//delete deleted comment
	$('#' + responseText).remove();

	//recolor the remaining comments
	nextClass = 'comments';
	count = 0;
	$('#commentlist').children().each( function(i) {
		count = i + 1;
		if( i %2 == 0)
		{	
			$(this).removeClass('white').addClass('comments');
		}
		else 
		{
			$(this).removeClass('comments').addClass('white');
		}
	});

	//change the comment count
	$('#commentcount').text('(' + count + ')' );
}

function noteKnownUser(user)
{
	var knownUser = false;
 	$.ajax({
		type: "GET",
		async: false,
		url: 'note.php',
//		data: "ajax=1&tab=compose&user=" + user,
		data: "params=ajax/checkUser/" + user,
		success: function(contents) {
			if(contents != "false")
			{
				knownUser = contents;

			}
		},
		error: function(error) {
			// fail silently
		}
	});

	return knownUser;
}






/*
 * callbacks for 'add a comment' form
 */

function showPostCommentRequest(formData, jqForm, options) { 
	var queryString = $.param(formData); 
	return true; 
} 
 
function showPostCommentResponse(responseText, statusText)	{ 
	 // if there was an error message previously, remove it
	//$('div.error').remove();
	cleanupErrorMsgs('div.error');
	if(serverError(responseText)) 
	{
		showErrorMsg('div.addcomment', '<div class="error"> Error adding comment. Please try again.</div>', 'comments');
		return;
	}
	// clear the 'add a comment box' (undo clearValue())
	$('#comment').css('color', '#9b9b9b').css('text-align', 'center');
	initialPageLoad = true;

	// make recent comment visible to user (will be in response) 
	$('div#commentlist').append(responseText);
	// rebind event handler to new forms
	$('#commentlist').bind('submit',$('.deleteCommentForm').ajaxForm(deleteCommentOptions));

	// update counter
	count = 0;
	$('#commentlist').children().each( function(i) {
		count = i + 1;
	});
	$('#commentcount').text('(' + count + ')' );
	if($('a.toggleLink').parent().hasClass('toggle'))
	{
			$('a.toggleLink').parent().removeClass('toggle').addClass('down');
						$( '#' + $('a.toggleLink').attr('name')).show();
	}
} 

function showPostCommentError(XMLHttpRequest, textStatus, errorThrown)
{
	showErrorMsg('div.addcomment', '<div class="error"> Error adding comment. Please try again.</div>', 'comments');
}

/*
 *
 *	Fancy effects
 *
 */

function resize(img)
{
	var maxSideLen = 100;
	if (img)
	{
		if (img.attr('width') > maxSideLen || img.attr('height') > maxSideLen)
		{
			var scalingFactor = (img.attr('width') > img.attr('height') ? (img.attr('width') / maxSideLen) : (img.attr('height') / maxSideLen));
			img.css('width', (img.attr('width') / scalingFactor) + 'px');
			img.css('height', (img.attr('height') / scalingFactor) + 'px');
			img.wrap('<a href="' + img.attr('src') + '"></a>');
		}
	}
}

function clearValue(e)
{// for clearing the 'add a comment' area onfocus
	if (initialPageLoad)
	{
	 e.value = "";
	 e.style.color = "#000";
	 e.style.textAlign = "left";
	}
	initialPageLoad = false;
}



function highlight(e)
{
	var focus = (e.type == 'click' ? $(this) : e );
	focus.css('background', '#d2d2d2');
	url = focus.attr('title');
	location.href = url;
}

function serverError(response)
{// checks to see if response is a server error
//XXXX misnamed : 4xx client error, 5xx server 

	response = jQuery.trim(response);

	if( response[0] == '4' || response[0] == '5')
	{
		return true;
	}
		
	return false;
}

function cleanupErrorMsgs(errorContainer)
{
	$(errorContainer).remove();
}

function showErrorMsg(where, what, anchor)
{
	$(where).before(what);
	if(location.href.indexOf('#' + anchor) == -1 && anchor != "")
	{
		location.href = location.href + '#' + anchor;
	}
}

function getValue(key)
{

	if(key == 'title' && !location.search)
	{
		return location.pathname.substring(1);
	}

	s = location.search.substr(1);
	
	values = s.split('&');
 
	
	for(i = 0; i < values.length; i++)
	{
		if(values[i].substring(0,key.length) == key)
		{
			return values[i].split("=")[1];
		}
	}
	return null;
}


function getPageId()
{
	if(getValue('idnum'))
	{
		pageId = 'idnum=' + getValue('idnum');
	}
	else if(title = getValue('title') )
	{
		if(title.indexOf('.php') == -1)
		pageId = 'title=' + getValue('title');
		else
			pageId = 'idnum=home';
	}
	else
	{
		pageId = 'idnum=home';
	}
	if(getValue('read'))
	{
		pageId = pageId + '&read=' + getValue('read');
	}
	return pageId;
}


function toggleFunction(toggleLink)
{
	if(toggleLink.parent().hasClass('down'))
	{
		toggleLink.parent().removeClass('down').addClass('toggle');
		$( '#' + toggleLink.attr('name')).hide();
	}
	else
	{
		toggleLink.parent().removeClass('toggle').addClass('down');
		$( '#' + toggleLink.attr('name')).show();
	}
}

function loginThenReturn(name)
{
	$(".js-login-" + name	).submit();
}


function resetBindings()
{// rebind after div.js-main has been modified through ajax
// XXXX make this more specific to avoid double binding
	$('div.expand').hide();
	$('#commentlist').bind('submit', $('.deleteCommentForm').ajaxForm(deleteCommentOptions));
	$('.js-addcomment').bind('submit', $('.addCommentForm').ajaxForm(postCommentOptions));
	$('a.toggleLink').bind('click', toggleFunction);
	$('li.js-pages, li.js-tags').bind('click', highlight);

	$('.tagForm').ajaxForm(addTagOptions); 


	$('.noteForm button').bind('submit', $('.noteForm').ajaxForm(postNoteOptions));
	$('#addTag').bind('click', tabActions['addTag']);
	$('.message').bind('click', tabActions['readNote'] );
	$('.disabled').bind('click', loginThenReturn);

}


 
	function loadingAnimation()
	{
		$('div.js-main').html('<div class="loading"> Loading... <br> <img src="/assets/images/dancing-banana.gif"/></div>');
	}

	function getLastModifiedTime(tabId, url)
	{
		var lastModified = '';
		$.ajax({
				async: false,
				type: "GET",
				url: url,
				data: 'ajax=1&lastModified=1&tab=' + tabId + '&' + getPageId(),
				success: function (dateModified) 
				{
					lastModified = dateModified; 

				},
				error: function(error) 
				{
				}

		});
		return lastModified;
	 }

	function highlightThisTab(id)
	{
			$('.tabs ul li, .tabs2 ul li').each( function() {
			if( $(this).attr('id') == id)
			{
				$(this).removeClass('short');
				$(this).children().addClass('selected');
				//$(this).html('<a class="selected" href="#">' + $(this).text()+ '</a>');
			}
			else
			{
				$(this).addClass('short');
				$(this).children().removeClass('selected');
				//$(this).html('<a href="#">' + $(this).text() + '</a>');
			}
		});

	}
 


	// cache object functions
	var cache = { // functions
			get: getFromCache, 
			put: addToCache,
		timestamp: getLastModified,
		clear: clearCache,
		// data
			contents: { lastModified:'', content:'' }, 
			files: { lastModified:'', content:'' }, 
			related: { lastModified:'', content:'' }, 
			compose: { lastModified:'', content:'' }, 
			note: { lastModified:'', content:'' }, 
			};
	function clearCache()
	{
	cache.contents = { lastModified:'', content:'' }, 
		cache.files = { lastModified:'', content:'' }, 
		cache.related = { lastModified:'', content:'' }, 
		cache.compose =	{ lastModified:'', content:'' }, 
		cache.note = { lastModified:'', content:'' }	
	}

	function getLastModified(id)
	{
		var lastModified = '';
		lastModified = cache[id]["lastModified"];
		return lastModified;
	}

	function getFromCache(id)
	{
		var contents = null;
		if(cache[id]["content"])
		contents = cache[id]['content'];
		return contents;
	}

	function addToCache(id, lastModified, content)
	{
		cache[id]['lastModified'] = lastModified;
		cache[id]['content'] = content;
	}

	function getTab(tabOpts)
	{
		//loadingAnimation();
		highlightThisTab(tabOpts['tabId']);
		var lastModified = 0;
		if(tabOpts['fromCache']	)
		{
			
			lastModified = getLastModifiedTime(tabOpts['tabId'], tabOpts['url']);
			if(lastModified <= cache.timestamp(tabOpts['tabId']))
			{
				tabOpts['successHandler'](cache.get(tabOpts['tabId']));
				setTimeout(function() { window.scrollTo(0,1); }, 0);
				return;
			}
		}
			$.ajax({
				type: tabOpts['type'],
				url:	tabOpts['url'],
				data: tabOpts['data'],
				success: function(content){
					tabOpts['successHandler'](content);
	
					if(tabOpts['fromCache'])
					{//add to cache
						cache.put(tabOpts['tabId'], lastModified, content);
					}
					//if(location.href.indexOf('?') == -1) location.href= location.href + '?tab=' + tabOpts['tabId'];
				},
				error: tabOpts['error'],
				complete: function(){ setTimeout(function(){	window.scrollTo(0,1);	}, 0);}
			});
		return;
	}
 



tabOptions = {
contents : {
	tabId: 'contents',
	type: "GET",
	url: "/page.php" ,
	data: "ajax=1&tab=contents&" + getPageId(),
	fromCache: true,
	successHandler: function(content) 
	{
		$('.js-main').html(content);
		// rewrite anchors, otherwise link is called localhost
		$('div.toc ol li a').each( function() {
			i = $(this).attr('href').indexOf('#');
			strippedAnchor = $(this).attr('href').substring(i);
			$(this).attr('href', strippedAnchor);

		});

		resetBindings();


	},

	error: function error(XmlHttpRequest, textStatus, errorThrown)
	{
		$('.js-main').html('<div class="error">This page failed to load. Please try again.</div>');
	}
}
,
files: {
	tabId: "files",
	type: "GET",
	url: "/page.php",
	data:"ajax=1&tab=files&" + getPageId(),
	successHandler: function(content)
	{
		$(".js-main").html(content);
		resetBindings();
	},

	error: function(XmlHttpRequest, textStatus, errorThrown)
	{
		$('.js-main').html('<div class="error">Files for this page failed to load. Please try again.</div>');
	}
	,fromCache: false
}
,
related: {
	tabId: "related",
	type: "GET",
	url: "/page.php",
	data: "ajax=1&tab=related&" + getPageId(),
	successHandler: function(content)
	{
		$(".js-main").html(content);
		resetBindings();
	},

	error: function(XmlHttpRequest, textStatus, errorThrown)
	{
		$('.js-main').html('<div class="error">Related pages for this page failed to load. Please try again.</div>');
	}
	,fromCache: false
}
,
compose : {
	tabId: "compose",
	type: "GET",
	url: "/note.php",
	data: "ajax=1&tab=compose",
	successHandler: function(content)
	{
		$('.js-main').html(content);
		resetBindings();
	},
	error: function(error)
	{
		$('js-main').html('<div class="error">Note composer failed to load. Please try again.</div>');
	}


	,fromCache: false
}
,
notes: {
	tabId: "notes",
	type: "GET",
	url: "/note.php",
	data: "ajax=1&tab=notes",
	successHandler: function(content)
	{
		$('.js-main').html(content);
		resetBindings();
	},

	error: function(error)
	{ 
		$('.js-main').html('<div class="error">Notes failed to load. Please try again.</div>');
	}

	,fromCache: false
}

}; // end of tabOptions

function fileHighlight(m)
{
//alert('clicked');
	highlight(m);
}

var tabActions = {

	addTag: getAddTagForm , 
	fullSizeImg: fileHighlight,
	readNote: msgHighlight
}; // end of tab actions


function msgHighlight(m)
{
		m.css('background','#d2d2d2');
		location.href = m.attr('title');
		// may need to take out timeout for back button
		//setTimeout("getTab(tabOptions['notes']); tabOptions['notes']['data'] = old;", 200);
}

function getAddTagForm()
{
		if(!$('.tags button.addtag').hasClass('disabled'))
		{
			tabOptions['related']['data'] = 'ajax=1&tab=related&addtag=true&' + getPageId();
			getTab(tabOptions['related']);	
			tabOptions['related']['data'] = 'ajax=1&tab=related&' + getPageId();
		}

}


// "main"
$(document).ready( function () {
	setTimeout(function() { window.scrollTo(0,1); }, 0);

	// bind form using 'ajaxForm' 
	$('.addCommentForm').ajaxForm(postCommentOptions);
	$('.deleteCommentForm').ajaxForm(deleteCommentOptions);
	$('.noteForm').ajaxForm(postNoteOptions);
	$('.tagForm').ajaxForm(addTagOptions); 
	$('.loginForm').ajaxForm(loginOptions); 
	$('.logoutForm').ajaxForm(logoutOptions); 


	// highlight and redirect on click
	$('div.container, li.js-tags, li.js-pages').click( function(){
		highlight($(this));
	});

	$('div.expand').hide();
	$('a.toggleLink').click( function()
	{
		toggleFunction($(this));
	});


	$('.disabled').click( function() {
		loginThenReturn($(this).attr('name'));
	});

	$('.action').click( function() {
		//actions not covered by forms plugin or tabs
		//load 'add tag'
		//read single note
		tabActions[$(this).attr('id')]($(this));
	});

	$('img').each( function(){
		resize($(this));
	});

});

