// YUI is awesome!
YAHOO.mindtouch.LinkNavigator.prototype.sNavigateUrlAppend = 'type=page&disabled_page=';

//YAHOO.mindtouch.LinkNavigator.prototype.elNewTitle = 'fileName';
YAHOO.mindtouch.LinkNavigator.prototype.elFileName = 'fileName';

// url to validate the move against
YAHOO.mindtouch.LinkNavigator.prototype.sVerifyMoveUrl = '/deki/gui/move.php?method=file';
YAHOO.mindtouch.LinkNavigator.prototype.sFileInfoUrl = '/deki/gui/move.php?method=fileinfo&file_id=';
/**
 * Initializes the dialog
 */
YAHOO.mindtouch.LinkNavigator.prototype.initNavigator = function(oParams)
{
	this.oPopup = oParams.oPopup;
	this.setButtonStatus(true);

	this.elFileName = YAHOO.util.Dom.get(this.elFileName);

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
		this._nOriginalPageId = this._nPageId;
		this._nFileId = (oParams.nFileId) ? oParams.nFileId : '';
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
	
	this.getFileInfo();
	this.showNavigatorFromPage();
};


YAHOO.mindtouch.LinkNavigator.prototype.onMoveTextboxKeyEvent = function(e, oSelf)
{
	oSelf.setSubmitButtonStatus();
};


YAHOO.mindtouch.LinkNavigator.prototype.setSubmitButtonStatus = function()
{
	// keep button enabled
	this.setButtonStatus(true);
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
	oParams.nPageId = this._nOriginalPageId;
	oParams.nNewPageId = this._nPageId;
	oParams.nFileId = this._nFileId;
	oParams.sUserName = this._sUserName;
	oParams.filename = this.elFileName.value;
	//oParams.sNewPath = this._oTextBox.value;
	
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
	var sContentType = YAHOO.mindtouch.getContentType(o);
	if (sContentType == 'application/json')
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
		// success
		// parent.window.location.reload();
		//parent.window.location.href = oData.body;
		var oScope = this;
		setTimeout(function()
			{
				Popup.close( oScope._nPageId );
			}, 0);
	}
	else
	{
		this.setButtonStatus(true);
		// error
		this.setMessage(oData.message, true);
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
	
	var nPageId = elClicked.getAttribute('pid');
	var sPath = elClicked.getAttribute('path');
	// sTitle comes in with htmlentities, fix it with this line
	var sNewTitle = elClicked.getAttribute('title');

	if (sPath)
	{
		if (this.bInitialLoad)
		{
			this.bInitialLoad = false;
			// set the oringal page path for use when updatinglink
			this._sOriginalPath = sPath;
		}
		
		// make sure we always show a trailing slash for clarity
		if (sPath.charAt(sPath.length-1) != '/')
		{
			sPath += '/';
		}
		
		this._registerLinkInfo(' ', sPath);
		this._nPageId = nPageId;
	}
	this.elFileName.focus();
	this.setSubmitButtonStatus();
};

YAHOO.mindtouch.LinkNavigator.prototype.getFileInfo = function()
{
	// send a post request to verify the move is allowed
	var oCallback =	{
					 success: this.getFileInfoSuccess,
					 failure: function() {},
					 scope: this,
					 timeout: 60000
					};
	var request = YAHOO.util.Connect.asyncRequest('GET', this.sFileInfoUrl + this._nFileId, oCallback);
};

YAHOO.mindtouch.LinkNavigator.prototype.getFileInfoSuccess = function(o)
{
	var oData = null;
	var sContentType = YAHOO.mindtouch.getContentType(o);
	if (sContentType == 'application/json') // IE reports Content-Type having trailing ASCII 13
	{
		try
		{
			// requires http://www.json.org/json.js
			oData = o.responseText.parseJSON();
			this.elFileName.value = oData.body;
		}
		catch (e)
		{
			this.verifyMoveFailure(o);
			return;
		}
	}
};
