// YUI is awesome!
YAHOO.mindtouch.LinkNavigator.prototype.sNavigateUrlAppend = 'type=page&disabled_page=';

YAHOO.mindtouch.LinkNavigator.prototype.elNewTitle = 'newPageTitle';

// url to validate the move against
YAHOO.mindtouch.LinkNavigator.prototype.sVerifyMoveUrl = '/deki/gui/move.php?method=page';

/**
 * Initializes the dialog
 */
YAHOO.mindtouch.LinkNavigator.prototype.initNavigator = function(oParams)
{
	this.oPopup = oParams.oPopup;
	this.setButtonStatus(false);

	this.elNewTitle = YAHOO.util.Dom.get(this.elNewTitle);

	// focus the text box
	try
	{
		this._oTextBox.focus();
	}
	catch (e) {}

	// set the initial text message for the search area
	this._oAutoContainer.style.visibility = 'hidden'; // hides flashing
	this._autoComp.setHeader(wfMsg('Dialog.LinkTwo.message-enter-search'));
	this._autoComp.setBody('&nbsp;');
	this._oAutoContainer.style.visibility = 'visible';

	// setup the dialog state
	//YAHOO.log('IN Params: ' + YAHOO.lang.dump(oParams));

	this.bInitialLoad = false;
	if (YAHOO.lang.isObject(oParams))
	{
		// need to add these to the incoming args
		this._nPageId = (oParams.nPageId) ? oParams.nPageId : '';
		// attach the page to disable in the navigator
		this.sNavigateUrlAppend += this._nPageId;
		// set the current user's name
		this._sUserName = (oParams.sUserName) ? oParams.sUserName : '';
		this.bInitialLoad = true;
	}

	// Events
	var oSelf = this;
	// Dom Events
	YAHOO.util.Event.addListener(this._oConfig.autoCompleteInput, "input", oSelf.onMoveTextboxKeyEvent, oSelf);
	YAHOO.util.Event.addListener(this.elNewTitle, "input", oSelf.onMoveTextboxKeyEvent, oSelf);
	
	this.showNavigatorFromPage();
};

YAHOO.mindtouch.LinkNavigator.prototype.setCustomLabels = function()
{
	this._oTextLabel.innerHTML = wfMsg('Dialog.LinkTwo.label-move');
};


YAHOO.mindtouch.LinkNavigator.prototype.onMoveTextboxKeyEvent = function(e, oSelf)
{
	oSelf.setSubmitButtonStatus();
};


YAHOO.mindtouch.LinkNavigator.prototype.setSubmitButtonStatus = function()
{
	// determine the button's proper status
	var bCurrent = false;
	// chcek if the new title is blank
	bCurrent |= (String(this.elNewTitle.value).length <= 0);
	// check if the new path is the same as the old path
	var sPath = this._oTextBox.value;
	if (sPath.charAt(sPath.length-1) != '/')
	{
		sPath += '/';
	}
	sPath += this.elNewTitle.value;

	bCurrent |= (sPath == this._sOriginalPath);

	this.setButtonStatus(!bCurrent);
};

/*
 * Loads the navigator with the entry point as the start page
 */
YAHOO.mindtouch.LinkNavigator.prototype.showNavigatorFromPage = function()
{
	// check if a pageId is set
	// generate initial url here
	var sUrl = this._linkNavigateUrl;
	if (this._nPageId > 0)
	{
		sUrl += this._nPageId + '&parent=1';
	}
	/*
	if (YAHOO.lang.isValue(this._sIncomingHref) && (YAHOO.lang.trim(this._sIncomingHref).length > 0))
	{
		sUrl += '&title=' + this._sIncomingHref;
	}
	*/
	
	this.setDataSource(sUrl);

	// display the loading pane for the user
	this.showBrowserLoading();
};


/**
 * Formats the information before passing to the editor handler
 */
YAHOO.mindtouch.LinkNavigator.prototype.updateLink = function()
{
	var oParams = new Object();
	oParams.nPageId = this._nPageId;
	oParams.sUserName = this._sUserName;
	oParams.sNewTitle = this.elNewTitle.value;
	oParams.sNewPath = this._oTextBox.value;
	
	//YAHOO.log('OUT PARAMS: ' + YAHOO.lang.dump(oParams));
	
	this.setMessage(wfMsg('Dialog.Rename.validating-new-title'));

	// need to verify the new page title
	var sPostData = this.encodePostData(oParams);

	// send a post request to verify the move is allowed
	var oCallback =	{
					 success: this.verifyMoveSuccess,
					 failure: this.verifyMoveFailure,
					 scope: this,
					 timeout: 60000
					};
	var request = YAHOO.util.Connect.asyncRequest('POST', this.sVerifyMoveUrl, oCallback, sPostData);
	this.setButtonStatus(false);

	return null;
};

YAHOO.mindtouch.LinkNavigator.prototype.encodePostData = function(oParams)
{
	var aPostData = new Array();
	for (var key in oParams)
	{
		if (typeof oParams[key] != "function")
		{
			aPostData.push(key);
			aPostData.push('=');
			aPostData.push(encodeURIComponent(oParams[key]));
			aPostData.push('&');
		}
	}
	
	if (aPostData.length > 0)
	{
		aPostData.pop();
		return aPostData.join('');
	}

	return '';
};

YAHOO.mindtouch.LinkNavigator.prototype.verifyMoveFailure = function(o)
{
	this.setButtonStatus(true);
	this.setMessage(wfMsg('internal-error'), true);
};

YAHOO.mindtouch.LinkNavigator.prototype.verifyMoveSuccess = function(o)
{
	var oData = null;
	var contentType = YAHOO.mindtouch.getContentType(o);

	if ('application/json' == contentType)
	{
		try
		{
			// requires http://www.json.org/json.js
			oData = o.responseText.parseJSON();
		}
		catch (e)
		{
			this.verifyMoveFailure(o);
			return;
		}
	}
	else
	{
		this.verifyMoveFailure();
		return;
	}

	if (oData.status == "200")
	{
		// close the dialog
		//this.oPopup.close(new Object());

		// success
		window.top.location.href = oData.body;
	}
	else
	{
		// error
		this.setMessage(oData.body, true);
		this.setButtonStatus(false);

		// don't close the dialog
	}
};

YAHOO.mindtouch.LinkNavigator.prototype.setButtonStatus = function(bEnabled)
{
	if (bEnabled)
	{
		this.oPopup.enableButton(this.oPopup.BTN_OK);
	}
	else
	{
		this.oPopup.disableButton(this.oPopup.BTN_OK);
	}
};

YAHOO.mindtouch.LinkNavigator.prototype.setMessage = function(sMessage, bError)
{
	if (YAHOO.lang.isValue(sMessage))
	{
		this.oPopup.setStatus(sMessage);
	}
	else
	{
		this.oPopup.setStatus('');
	}
};

/*
 * Override for each editor, this is Xinha
 */
YAHOO.mindtouch.LinkNavigator.prototype.returnToEditor = function(bSuccess)
{
	parent.hidePopWin(false);
};


YAHOO.mindtouch.LinkNavigator.prototype.clickLink = function(elClicked, sTitle)
{
	if (!this.bInitialLoad && YAHOO.util.Dom.hasClass(elClicked, 'disabled'))
	{
		return;
	}

	// save the last element that was clicked here
	this.elLastClicked = elClicked;

	var sPath = elClicked.getAttribute('path');
	// sTitle comes in with htmlentities, fix it with this line
	var sNewTitle = elClicked.getAttribute('title');

	if (sPath)
	{
		if (this.bInitialLoad)
		{
			this.bInitialLoad = false;
			// set the oringal page path for use when updatinglink
			sPath = elClicked.getAttribute('pathA');
			sNewTitle = elClicked.getAttribute('pathB');

			this._sOriginalPath = sPath;
			this.elNewTitle.value = sNewTitle;
		}
		
		// make sure we always show a trailing slash for clarity
		if (sPath.charAt(sPath.length-1) != '/')
		{
			sPath += '/';
		}
		
		this._registerLinkInfo(this.elNewTitle.value, sPath);
		this.setSubmitButtonStatus();
	}
};
