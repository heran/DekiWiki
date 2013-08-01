YAHOO.namespace("YAHOO.mindtouch.LinkNavigator");

/**
 * Link Navigator constructor
 *
 * @note Class is a mash up of the autocomplete widget
 *       and the columnav widget
 */
// TODO: Stop drag event on list items
// ondragstart, onselectstart
//
// TODO: Select highlighted item to expand subpane
YAHOO.mindtouch.LinkNavigator = function(oConfig)
{
	this.Dom = YAHOO.util.Dom;
	this.oPopup = oConfig.oPopup;

	// args
	this._oTextBox = YAHOO.util.Dom.get(oConfig.autoCompleteInput);
	this._elFileLabel = YAHOO.util.Dom.get(oConfig.fileLabel);
	this._oAutoContainer = YAHOO.util.Dom.get(oConfig.autoCompleteContainer);
	//this._oAutoResults = YAHOO.util.Dom.get(oConfig.autoCompleteResults)
	this._oNavLoading = YAHOO.util.Dom.get(oConfig.columNavLoading);
	this._elNavLoadingPane = YAHOO.util.Dom.get(oConfig.columNavPaneLoading);
	this._oNavContainer = YAHOO.util.Dom.get(oConfig.columNavContainer);
	// label for the textbox
	this._oTextLabel =  YAHOO.util.Dom.get(oConfig.textLabel);
	// label for the panel display
	this._oPanelLabel = YAHOO.util.Dom.get(oConfig.panelLabel);
	
	// required for the click home link
	this.sSitename = oConfig.siteName;
	// required for nav to current
	this.nCurrentPageId = oConfig.currentPageId;
	this.sCurrentPageTitle = oConfig.currentPageTitle;
	this.sCurrentPagePath = oConfig.currentPagePath;

	// datasource
	this._dataSource = new YAHOO.util.XHRDataSource('/deki/gui/search.php');
	this._dataSource.responseType = YAHOO.util.XHRDataSource.TYPE_JSON;
	this._dataSource.responseSchema = {
			resultsList: 'body',
			fields: ['search_path', 'search_class', 'search_highlight', 'page_id', 'search_title', 'file_id']
	};

	// Navigation page cache, kept here so it's persistant
	//this._aCache = new Array();
	//this._nMaxCacheEntries = 25;

	// autocomplete
	this._autoComp = new YAHOO.widget.AutoComplete(this._oTextBox, oConfig.autoCompleteResults, this._dataSource);
	this._autoComp.alwaysShowContainer = true;
	this._autoComp.delimChar = null;
	this._autoComp.highlightClassName = 'row-highlight';
	this._autoComp.queryDelay = 0.5;
	// set the minquery length
	this.minQueryLength = 2;
	this.setSearchStatus(true);
	// override some auto complete methods
	this._autoComp.doBeforeExpandContainer = this.doBeforeExpandContainer;
	this._autoComp.formatResult = this._formatResult;
	// add a reference to this object within the Auto Complete (needed for doBeforeExpand..)
	this._autoComp.oLinkNavigator = this;
	
	this._dataSource.maxCacheEntries = 60;
	this._autoComp.queryMatchSubset = false;
	
	var oSelf = this;
	
	// Remove strings from the query that will affect the search results
	this._autoComp.generateRequest = function(sQuery)
	{
		if (sQuery.match(/^file%3A/i))
		{
			sQuery = sQuery.substring(7);
		}
		
		sQuery = "?query=" + sQuery;
		
		if (oSelf.sNavigateUrlAppend.length > 0)
		{
			sQuery += "&" + oSelf.sNavigateUrlAppend;
		}
		
	    return sQuery;
	};
	
	this._oConfig = oConfig;
	//this._createColumNav();
	
	this.elShowBrowser = this.Dom.get(oConfig.autoCompleteToNav);
	this.elShowSearch = this.Dom.get(oConfig.columNavSearchAgain);
	this.elBrowseText = this.Dom.get(oConfig.navigatorText);

	// Setup events

	// Dom events
	YAHOO.util.Event.addListener(oConfig.columNavSearchAgain, 'click', oSelf._onSearchAgainClick, oSelf);
	//YAHOO.util.Event.addListener(oConfig.autoCompleteClear, "click", oSelf._onClearSearchClick, oSelf);
	YAHOO.util.Event.addListener(oConfig.buttonUpdateLink, "click", oSelf._onUpdateLinkClick, oSelf);
	YAHOO.util.Event.addListener(oConfig.autoCompleteToNav, "click", oSelf._onShowNavigatorClick, oSelf);
	// button navigation events
	YAHOO.util.Event.addListener(oConfig.buttonNavCurrent, "click", oSelf._onNavCurrentClick, oSelf);
	YAHOO.util.Event.addListener(oConfig.buttonNavHome, "click", oSelf._onNavHomeClick, oSelf);
	YAHOO.util.Event.addListener(oConfig.buttonNavMyPage, "click", oSelf._onNavMyPageClick, oSelf);
	// when the file input is updated set the textbox
	YAHOO.util.Event.addListener(oConfig.fileInput, "change", oSelf._onFileInputChange, oSelf);
	if (this._oTextBox && this._oTextBox.form)
	{
		YAHOO.util.Event.addListener(this._oTextBox.form, "submit", oSelf._onFormSubmit, oSelf);
	}
	
	// keydown event: handle the backspace
	var sType = (YAHOO.env.ua.opera) ? 'keypress' : 'keydown';	
	YAHOO.util.Event.addListener(oConfig.columNavDisplay, sType, oSelf._onKeypress, oSelf);
	
	// Custom events
	this._autoComp.dataRequestEvent.subscribe(this.dataRequestEvent, oSelf);
	this._autoComp.dataReturnEvent.subscribe(this.dataReturnEvent, oSelf);
	this._autoComp.textboxKeyEvent.subscribe(this.textboxKeyEvent, oSelf);
	this._autoComp.itemSelectEvent.subscribe(this.itemSelectEvent, oSelf);

	this.showColumNavEvent = new YAHOO.util.CustomEvent("showColumNav", this);
};

/**
 * Reinitializes the columnav without problems
 */
YAHOO.mindtouch.LinkNavigator.prototype._createColumNav = function(sDataSource, aCache)
{
	if (!sDataSource)
	{
		sDataSource = null;
	}

	// remove prev button listeners
	//var oPrev = YAHOO.util.Dom.get(this._oConfig.columNavPrev);
	//YAHOO.util.Event.removeListener(oPrev, 'click');

	// reset the clip region
	var oNavClip = YAHOO.util.Dom.get(this._oConfig.columNavDisplay);

	var aMarkup = [
					'	<div class="carousel-clip-region">',
					'		<ul class="carousel-list"></ul>',
					'	</div>'
				  ];

	oNavClip.innerHTML = aMarkup.join("");

	// columnav
	var cn_cfg = {
					numVisible: (this._oConfig.columNavColumns) ? this._oConfig.columNavColumns : 2,
					prevElement: this._oConfig.columNavPrev,
					datasource: sDataSource,
					linkAction: this._onClickLink,
					loadingPane: YAHOO.util.Dom.get(this._oConfig.columNavPaneLoading)
				 };

	this._columNav = new YAHOO.extension.ColumNav(this._oConfig.columNavDisplay, cn_cfg);
	// HACK: add back reference to the link navigator object for the onClickLink event
	this._columNav.linkNavigator = this;
	if (YAHOO.lang.isObject(aCache))
	{
		// copy the old cache to the new object
		// still makes the first request but it will have to do
		this._columNav.aCache = aCache;
	}
};


/**
 * Search Enabled flag
 */
YAHOO.mindtouch.LinkNavigator.prototype.searchEnabled = null;
/**
 * Stores the miniumum length of a query string
 */
YAHOO.mindtouch.LinkNavigator.prototype.minQueryLength = null;
/**
 * Input Element
 */
YAHOO.mindtouch.LinkNavigator.prototype._oTextBox = null;
/**
 * Auto Complete Container Element
 */
YAHOO.mindtouch.LinkNavigator.prototype._oAutoContainer = null;
/**
 * ColumNav Container Element
 */
YAHOO.mindtouch.LinkNavigator.prototype._oNavContainer = null;
/**
 * Stores the time of the last request
 */
YAHOO.mindtouch.LinkNavigator.prototype._lastRequestTime = null;
/**
 * Datasource object
 */
YAHOO.mindtouch.LinkNavigator.prototype._dataSource = null;
/**
 * AutoComplete object
 */
YAHOO.mindtouch.LinkNavigator.prototype._autoComp = null;
/**
 * ColumNav object
 */
YAHOO.mindtouch.LinkNavigator.prototype._columNav = null;
/**
 * External links regex
 */
YAHOO.mindtouch.LinkNavigator.prototype._externalLinksRegex = /(.*:\/\/|\\\\).*/;
// when to show the browse icon
YAHOO.mindtouch.LinkNavigator.prototype._externalBrowseRegex = /((file|smb):\/\/|\\\\).*/;
/**
 * Saves the link caption (user created)
 */
YAHOO.mindtouch.LinkNavigator.prototype._sUserLinkCaption = null;
/**
 * Saves the link page title (system created)
 */
YAHOO.mindtouch.LinkNavigator.prototype._sSystemLinkCaption = null;
/*
 * Url to retrieve data from
 */
YAHOO.mindtouch.LinkNavigator.prototype._linkNavigateUrl = '/deki/gui/linknavigate.php?page='; //'link_navigate.php?id=';
// anything you might want to tack on to the generated url
YAHOO.mindtouch.LinkNavigator.prototype.sNavigateUrlAppend = '';
/*
 * Sets if the last typed url was an external link
 */
YAHOO.mindtouch.LinkNavigator.prototype._wasExternal = false;
/*
 * Saves the requesting page id, where to init the column nav from
 */
YAHOO.mindtouch.LinkNavigator.prototype._nPageId = 0;
/*
 * Saves the requesting user name for the columnav
 */
YAHOO.mindtouch.LinkNavigator.prototype._sUserName = '';
/*
 * Saves incoming href from the user
 */
YAHOO.mindtouch.LinkNavigator.prototype._sIncomingHref = '';


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

/**
 * Fires when the user clicks the link to return to the search window
 */
YAHOO.mindtouch.LinkNavigator.prototype._onSearchAgainClick = function(e, oSelf)
{
	// don't pass this event through
	YAHOO.util.Event.stopEvent(e);

	oSelf.showAutoComplete();
};

YAHOO.mindtouch.LinkNavigator.prototype._clearSearchResults = function(sHeader)
{
	sHeader = sHeader || '';
	
	this._autoComp.setHeader(sHeader);
	this._autoComp.setBody("&nbsp;");
	// workaround for bug #0006704
	this._autoComp._initListEl();
};

/**
 * Fires when the search is sent
 */
YAHOO.mindtouch.LinkNavigator.prototype.dataRequestEvent = function(sEventName, oArgs, oSelf)
{
	// TODO: localize
	var sHeader = '<img src="/skins/common/icons/anim-circle.gif"> ' + wfMsg('Dialog.LinkTwo.message-searching');
	oSelf._clearSearchResults.call(oSelf, sHeader);

	var date = new Date();
	oSelf._lastRequestTime = date.getTime();
};


/**
 * Fires when the search is returned
 */
YAHOO.mindtouch.LinkNavigator.prototype.dataReturnEvent = function(sEventName, oArgs, oSelf)
{
	// requestTime is computed due to autocomplete firing empty dataReturnEvents
	var date = new Date();
	var requestTime = date.getTime() - oSelf._lastRequestTime;
	var aResults = oArgs[2] || null;

	// need to check if the only result is empty because of IE
	if (!YAHOO.lang.isObject(aResults) || (aResults.length == 0))
	{
		oSelf._autoComp._oCurItem = null; // clear out the current item
		oSelf._clearSearchResults.call(oSelf, wfMsg('Dialog.LinkTwo.message-no-results'));
		return;
	}
	
	if (aResults.length > 0)
	{
		// data has results
		var nResults = oArgs[2].length;
		var aMarkup = [
						'<div class="resultsHeader">',
						wfMsg('Dialog.LinkTwo.message-found'),
						' ',
						nResults,
						' ',
						wfMsg('Dialog.LinkTwo.message-results'),
						'</div>'
					  ];
		oSelf._autoComp.setHeader(aMarkup.join(""));
	}
	// set the threshold for invalid dataReturnEvents
	else if (aResults.length == 0 && requestTime > 300)
	{
		// TODO: localize
		oSelf._clearSearchResults.call(oSelf, wfMsg('Dialog.LinkTwo.message-no-results'));
	}
};

/**
 * Fires when the user types in the query box
 */
YAHOO.mindtouch.LinkNavigator.prototype.textboxKeyEvent = function(sEventName, oArgs, oSelf)
{
	var sText = oSelf._oTextBox.value;
	
	if (oSelf.searchEnabled)
	{
		if (oSelf._externalLinksRegex.test(sText))
		{
			oSelf.setSearchStatus(false);
			// need to override this value so that the message is displayed
			oSelf._autoComp._bContainerOpen = true;
			oSelf._clearSearchResults.call(oSelf, wfMsg('Dialog.LinkTwo.message-links-not-matched'));
			oSelf._wasExternal = true;
		}
	}
	else if (oSelf._wasExternal && !oSelf._externalLinksRegex.test(sText))
	{
		oSelf.setSearchStatus(true);
		oSelf._wasExternal = false;
	}
	else
	{
		// deselect anything that might be selected in the navigator
		oSelf.deselect();
	}

	// check if we should show the browse button
	if (oSelf._externalBrowseRegex.test(sText))
	{
		oSelf._elFileLabel.style.display = 'block';
	}
	else
	{
		oSelf._elFileLabel.style.display = 'none';
	}
};


/**
 * Fires when an item from the autocomplete is selected
 */
YAHOO.mindtouch.LinkNavigator.prototype.itemSelectEvent = function(sEventName, oArgs, oSelf)
{
	var aResult = oArgs[2];
	oSelf._registerLinkInfo(aResult[4], aResult[0]);
	oSelf.setButtonStatus(true);
};


/**
 * Registers the browse button for all the autocomplete items in the list
 */
YAHOO.mindtouch.LinkNavigator.prototype.doBeforeExpandContainer = function(oTextbox, oContainer, sQuery, aResults) 
{
	var oAutoComplete = this;

	var nItems = oAutoComplete._nDisplayedItems
	var aItems = this.getListEl().childNodes;

	// Register events for the items
	for(var i = 0; i < nItems; i++)
	{
		// html object
		var oLiItem = aItems[i];
		var oResultItem = aResults[i];
		// json object
		var nPageId = oResultItem.page_id;
		var nFileId = oResultItem.file_id;

		// @see formatResult 
		var oDivItem = oLiItem.childNodes[0];
		var oDivCarousel = document.createElement('div');

		YAHOO.util.Dom.addClass(oDivCarousel, 'browse');
		oDivCarousel.setAttribute('pageId', nPageId);
		oDivCarousel.setAttribute('fileId', nFileId);
		// event when the user clicks the browse link
		YAHOO.util.Event.addListener(oDivCarousel, "click", oAutoComplete.oLinkNavigator._onClickBrowse, oAutoComplete.oLinkNavigator);

		oDivItem.appendChild(oDivCarousel);
	}

    return true;
};

/*
 * Formats the external link from the browse dialog
 */
YAHOO.mindtouch.LinkNavigator.prototype._onFileInputChange = function(e, oSelf)
{
	var elTarget = YAHOO.util.Event.getTarget(e);
	var sLink = elTarget.value;
	if (String(sLink).substr(0, 2) != '\\\\')
	{
		// do some magical formatting on the link
		sLink = 'file:///' + sLink.replace(/\\/g, '/');
	}
	oSelf._oTextBox.value = sLink;

	// update the dialog
	oSelf.textboxKeyEvent(null, null, oSelf);
};

/*
 * Needed to show the search window on enter press
 */
YAHOO.mindtouch.LinkNavigator.prototype._onFormSubmit = function(e, oSelf)
{
	YAHOO.util.Event.stopEvent(e);
	if (oSelf.searchEnabled == false)
	{
		oSelf.showAutoComplete();

		var sText = oSelf._oTextBox.value;
		if (sText.length > oSelf.minQueryLength)
		{
			oSelf._autoComp._sendQuery(oSelf._oTextBox.value);
		}
	}
};


/**
 * Event to show the navigator at root without searching
 */
YAHOO.mindtouch.LinkNavigator.prototype._onShowNavigatorClick = function(e, oSelf)
{
	// don't pass this event through
	YAHOO.util.Event.stopEvent(e);

	// handle the event
	oSelf.showNavigatorFromPage();
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
		sUrl += this._nPageId;// + '&parent=1';
	}
	if (YAHOO.lang.isValue(this._sIncomingHref) && (YAHOO.lang.trim(this._sIncomingHref).length > 0))
	{
		sUrl += '&title=' + encodeURIComponent(this._sIncomingHref);
	}
	this.setDataSource(sUrl);

	// display the loading pane for the user
	this.showBrowserLoading();
};


/**
 * Event to close out the window & return the link information
 */
YAHOO.mindtouch.LinkNavigator.prototype._onUpdateLinkClick = function(e, oSelf)
{
	oSelf.updateLink();
};

/**
 * Fires when the clear search button is clicked
 */
YAHOO.mindtouch.LinkNavigator.prototype._onClearSearchClick = function(e, oSelf)
{
	//oSelf.setSearchStatus(false);
	oSelf._oTextBox.value = '';
	oSelf._clearSearchResults.call(oSelf);
	oSelf._oTextBox.focus();
	oSelf._oTextBox.select();
};

/**
 * Fires when the browse icon is clicked in an autocomplete item row
 */
YAHOO.mindtouch.LinkNavigator.prototype._onClickBrowse = function(e, oSelf)
{
	// handle the event
	var oTarget = YAHOO.util.Event.getTarget(e);
	var nPageId = oTarget.getAttribute('pageId');
	var nFileId = oTarget.getAttribute('fileId');
	var sUrl = oSelf._linkNavigateUrl + nPageId;
	if (nFileId)
	{
		sUrl += '&file_id=' + nFileId;
	}
	oSelf.setDataSource(sUrl);

	// display the loading pane for the user
	oSelf.showBrowserLoading();

	// don't pass this event through
	YAHOO.util.Event.stopEvent(e);
};

/**
 * Fires when an object in the column nav is clicked
 */
YAHOO.mindtouch.LinkNavigator.prototype._onClickLink = function(e, oSelf)
{
	// this == columNav
	var oTarget = YAHOO.util.Event.getTarget(e);
	// stores the selected pages title
	var sTitle = null;

	if (oTarget.tagName == 'SPAN')
	{
		//sTitle = oTarget.innerHTML;
		//sTitle = oTarget.parentNode.getAttribute('title');
		oTarget = oTarget.parentNode;
	}
	else
	{
		//sTitle = YAHOO.util.Dom.getFirstChild(oTarget).innerHTML;
	}
	
	sTitle = oTarget.getAttribute('title');
	oSelf.linkNavigator.clickLink(oTarget, sTitle);

	return false;
};


YAHOO.mindtouch.LinkNavigator.prototype.clickLink = function(elClicked, sTitle)
{
	// save the last element that was clicked here
	this.elLastClicked = elClicked;

	var sPath = elClicked.getAttribute('path');
	if (sPath)
	{
		this._registerLinkInfo(sTitle, sPath);
	}
};

YAHOO.mindtouch.LinkNavigator.prototype._onKeypress = function(e, oSelf)
{
	// backspace
	if ( e.keyCode == 8 && !e.altKey && !e.shiftKey && !e.ctrlKey )
	{
		YAHOO.util.Event.stopEvent(e);
		oSelf._columNav.carousel._scrollPrev(e, oSelf._columNav.carousel);
	}
}

/**
 * Fires when the home button is clicked in the columnav
 */
YAHOO.mindtouch.LinkNavigator.prototype._onNavCurrentClick = function(e, oSelf)
{
	// handle the event
	var sNavigateUrl = oSelf._linkNavigateUrl + oSelf.nCurrentPageId;
	if ((oSelf._oNavContainer.style.display != 'none') && (oSelf._columNav._sLastRequestUrl != sNavigateUrl))
	{
		// nav to entry point page
		// set the url
		oSelf._registerLinkInfo(oSelf.sCurrentPageTitle, oSelf.sCurrentPagePath);

		oSelf.showBrowserLoading();
		oSelf.setDataSource(sNavigateUrl);
	}

	// don't pass this event through
	YAHOO.util.Event.stopEvent(e);
};

/**
 * Fires when the home button is clicked in the columnav
 */
YAHOO.mindtouch.LinkNavigator.prototype._onNavHomeClick = function(e, oSelf)
{
	// handle the event
	var sNavigateUrl = oSelf._linkNavigateUrl;
	if ((oSelf._oNavContainer.style.display != 'none') && (oSelf._columNav._sLastRequestUrl != sNavigateUrl))
	{
		// nav home
		// set the url
		oSelf._registerLinkInfo(oSelf.sSitename, '/');

		oSelf.showBrowserLoading();
		oSelf.setDataSource(sNavigateUrl);
	}

	// don't pass this event through
	YAHOO.util.Event.stopEvent(e);
};

/**
 * Fires when the my page button is clicked in the columnav
 */
YAHOO.mindtouch.LinkNavigator.prototype._onNavMyPageClick = function(e, oSelf)
{
	// handle the event
	var sNavigateUrl = oSelf._linkNavigateUrl + 'user' + '&name=' + oSelf._sUserName;
	if ((oSelf._oNavContainer.style.display != 'none') && (oSelf._columNav._sLastRequestUrl != sNavigateUrl))
	{
		// nav to my page
		// set the url
		var sPage = 'User:' + oSelf._sUserName;
		oSelf._registerLinkInfo(sPage, sPage);

		oSelf.showBrowserLoading();
		// user page url generated here
		oSelf.setDataSource(sNavigateUrl);
	}

	// don't pass this event through
	YAHOO.util.Event.stopEvent(e);
};


/**
 * Custom formatter function for the list items
 */
YAHOO.mindtouch.LinkNavigator.prototype._formatResult = function(oResultItem, sQuery)
{
	var oSelf = this;
	var sValue = oResultItem[0];
	var sClass = oResultItem[1];
	var sText = oResultItem[2];
	var nPageId = oResultItem[3];
	//var sTitle = oResultItem[4];
	var aMarkup = null;

	var sSafeHint = String(sValue)
					.replace(/&(?!\w+([;\s]|$))/g, "&amp;")
					.replace(/</g, "&lt;")
					.replace(/>/g, "&gt;")
	;
	
	aMarkup = [
				'<div class="row" title="'+ sSafeHint +'">',
				'<span class="icon">',
				'<img src="/skins/common/icons/icon-trans.gif" class="',
				'mt-ext-' + sClass,
				'" alt="" />',
				'</span>',
				sText,
				'<span class="hint">',
				sSafeHint,
				'</span>',
				'</div>'
			  ];

	return (aMarkup.join(""));
};


/**
 * Updates the link information for updateLinkClick
 */
YAHOO.mindtouch.LinkNavigator.prototype._registerLinkInfo = function(sCaption, sHref)
{
	this._sSystemLinkCaption = sCaption;
	this._oTextBox.value = sHref;
	// set the browse div info
	if (this.elBrowseText)
	{
		var elTextNode = document.createTextNode(sHref);
		this.elBrowseText.innerHTML = '';
		this.elBrowseText.appendChild(elTextNode);
	}
};


/**
 * Public Link Navigator functions
 */

/**
 * Initializes the search with some parameters
 *
 * @param oParams the incoming parameters from the dialog
 */
YAHOO.mindtouch.LinkNavigator.prototype.initNavigator = function(oParams)
{
	// set the search button enabled
	this.Dom.addClass(this.elShowSearch.parentNode, 'active');

	// set the initial text message for the search area
	this._clearSearchResults(wfMsg('Dialog.LinkTwo.message-enter-search'));
	
	var oArgs = this.getEditorArgs(oParams);

	if (YAHOO.lang.isObject(oArgs))
	{
		if (oArgs.bNewLink)
		{
			this._sIncomingHref = null;
			this._oTextBox.value = oArgs.sSelectedText;
		}
		else
		{
			this._sIncomingHref = oArgs.sSelectedHref;
			this._oTextBox.value = oArgs.sSelectedHref;
		}

		this._nPageId = oArgs.nPageId;
		this._sUserName = oArgs.sUserName;
	}

	// if there was something specified, start searching by it
	if (YAHOO.lang.isUndefined(this.minQueryLength) || (String(this._oTextBox.value).length > this.minQueryLength))
	{
		// fire the dom event
		this.textboxKeyEvent(null, null, this);
		this._autoComp._sendQuery(this._oTextBox.value);
		
		this._registerLinkInfo('', this._oTextBox.value);
	}

	// focus the text box
	this._oTextBox.focus();
	this._oTextBox.select();
};

/*
 * Function should be overridden for each editor
 * Below is the implementation for Xinha
 *
 * @return Object with member vars:
 * sSelectedText, sSelectedHref, nPageId, sPageTitle, sUserName, bNewLink
 */
// deprecated, shouldn't need to use due to standardized dialog javascript
YAHOO.mindtouch.LinkNavigator.prototype.getEditorArgs = function(oDialogArgs)
{
	var oArgs = new Object();
	
	if (oDialogArgs)
	{
		oArgs.sSelectedText = oDialogArgs.f_text;
		oArgs.sSelectedHref = String(oDialogArgs.f_href);
		if (oArgs.sSelectedHref.substr(0, 2) == './')
		{
			oArgs.sSelectedHref = oArgs.sSelectedHref.substring(2);
		}
		oArgs.sPageTitle = oDialogArgs.contextTopic;
		oArgs.nPageId = oDialogArgs.contextTopicID;
		oArgs.sUserName = oDialogArgs.userName;
		oArgs.bNewLink = true;

		// @note (guerric) test fails if the link text is the href
		var sDecodedHref = unescape(oArgs.sSelectedHref);
		if (oArgs.sSelectedText == oArgs.sSelectedHref || oArgs.sSelectedText == sDecodedHref || oDialogArgs.newlink)
		{
			// user highlighted new text
			if (oArgs.sSelectedText)
			{
				oArgs.bNewLink = true;	
			}
		}
		else
		{
			// user is updating an existing link
			if (oArgs.sSelectedHref == '')
			{
				oArgs.sSelectedHref = '/';
			}
			oArgs.bNewLink = false;
		}

		return oArgs;
	}

	return null;
};

/**
 * Formats the information before passing to the editor handler
 */
YAHOO.mindtouch.LinkNavigator.prototype.updateLink = function()
{
	// pass data back to the calling window
	var sCaption = this._sUserLinkCaption;
	var sHref = this._oTextBox.value;

	if ( (sCaption == null) || (sCaption.length == 0) )
	{
		if (this._wasExternal)
		{
			sCaption = sHref;
		}
		else
		{
			// fall over to the default page title
			sCaption = this._sSystemLinkCaption || sHref;
		}
	}
	
	if (this._externalBrowseRegex.test(sHref) && (sHref.indexOf(' ') > -1))
	{
		// unencoded spaces in the external link, blind encode
		sHref = encodeURI(sHref);
	}
	
	var oParams = new Object();
	oParams.sCaption = sCaption;
	oParams.sHref = sHref;

	//YAHOO.log('OUT: ' + YAHOO.lang.dump(oParams));
	return this.returnToEditor(oParams);
};

// determines if there is a valid link to insert
YAHOO.mindtouch.LinkNavigator.prototype.validateLink = function()
{
	return true;
};

/*
 * Override for each editor, this is Xinha
 * oParams member vars: sHref, sCaption
 */
// deprecated, shouldn't need to use due to standardized popup javascript
YAHOO.mindtouch.LinkNavigator.prototype.returnToEditor = function(oParams)
{
	var param = null;
	if (oParams.sCaption)
	{
		param = {
			f_href : oParams.sHref,
			f_text : oParams.sCaption
		};
	}

	return param;
};

/**
 * Shows/Hides respective elements
 */
YAHOO.mindtouch.LinkNavigator.prototype.showAutoComplete = function()
{
	this.setSearchStatus(true);
	this._oAutoContainer.style.display = 'block';
	this._oNavLoading.style.display = 'none';
	this._oNavContainer.style.display = 'none';
	this._oTextLabel.innerHTML = wfMsg('Dialog.LinkTwo.label-search');
	this._oPanelLabel.innerHTML = wfMsg('Dialog.LinkTwo.label-matches');
	// change the class of the tabs
	this.Dom.removeClass(this.elShowBrowser.parentNode, 'active');
	this.Dom.addClass(this.elShowSearch.parentNode, 'active');
	// show the search input and hide the browse div
	if (this.elBrowseText)
	{
		this._oTextBox.style.display = 'block';
		this.elBrowseText.style.display = 'none';
	}
};

YAHOO.mindtouch.LinkNavigator.prototype.showBrowserLoading = function()
{
	this._oAutoContainer.style.display = 'none';
	this._oNavLoading.style.display = 'block';
	this._oNavContainer.style.display = 'none';
	// change the class of the tabs
	this.Dom.addClass(this.elShowBrowser.parentNode, 'active');
	this.Dom.removeClass(this.elShowSearch.parentNode, 'active');
	// hide the search input and show the browse div
	if (this.elBrowseText)
	{
		this._oTextBox.style.display = 'none';
		this.elBrowseText.style.display = 'block';

		// force the external link button to be hidden
		this._elFileLabel.style.display = 'none';
	}
};

YAHOO.mindtouch.LinkNavigator.prototype.deselect = function()
{
	// do nothing
};

YAHOO.mindtouch.LinkNavigator.prototype.showColumNav = function()
{
	this.setSearchStatus(false);
	
	if (this._oNavContainer.style.display == 'none')
	{
		// fixes problem with this._columNav not being declared yet
		var oCn = this._columNav;
		this._oAutoContainer.style.display = 'none';
		this._oNavLoading.style.display = 'none';
		this._oNavContainer.style.display = 'block';
		this._oTextLabel.innerHTML = wfMsg('Dialog.LinkTwo.label-link');
		this._oPanelLabel.innerHTML = wfMsg('Dialog.LinkTwo.label-navigate');
		this.setCustomLabels();
		
		// @note (guerrics) code below opens the next window pane
		var oMenu = oCn.carousel.getItem(1);
        var aLinks = oCn._getNodes(oMenu, oCn._links);
		for (var i = 0; i < aLinks.length; i++)
		{
			if (oCn.DOM.hasClass(aLinks[i], 'columnav-active'))
			{
				// perform the click link action!
				// need to get the span text
				//var sTitle = YAHOO.util.Dom.getFirstChild(aLinks[i]).innerHTML;
				var sTitle = aLinks[i].getAttribute('title');
				this.clickLink(aLinks[i], sTitle);
				
				var oTarget = aLinks[i];
				var oList = oTarget.list;
				if (oList)
					oCn._addMenu(oList);
				break;
			}
		}
	}
	// custom event
	this.showColumNavEvent.fire(this);
};

YAHOO.mindtouch.LinkNavigator.prototype.setCustomLabels = function()
{
	// override with children
};

/**
 * Enable/Disable the autocompletion
 */
YAHOO.mindtouch.LinkNavigator.prototype.setSearchStatus = function(bEnabled)
{
	if (bEnabled)
	{
		this.searchEnabled = true;
		this._autoComp.minQueryLength = this.minQueryLength;
	}
	else
	{
		this.searchEnabled = false;
		this._autoComp.minQueryLength = -1;
	}
};


/**
 * Reset the ColumNav data source
 */
YAHOO.mindtouch.LinkNavigator.prototype.setDataSource = function(sDataSource)
{
	if ((String(sDataSource).length > 0) && (this.sNavigateUrlAppend.length > 0))
	{
        sDataSource += "&" + this.sNavigateUrlAppend;
    }

	// need to reset the columnav otherwise undesirable results follow
	// reset the object
	var aCache = null;
	if (this._columNav)
	{
		this._columNav.carousel.clear();
		// copy the old cache
		aCache = this._columNav.aCache;
	}

	this._columNav = null;
	
	// set the datasource
	this._createColumNav(sDataSource, aCache);
};

/**
 * ============================================================================
 * ============================================================================
 * ColumNav Overrides
 *
 */
/**
 * Function needs to be overriden to set the previous button state to enabled
 */
YAHOO.extension.ColumNav.prototype._init = function(id, cfg)
{
	this.id = id;
	this.cfg = cfg; // make this a YAHOO.util.Config object?

	this.datasource = cfg.datasource || cfg.source;
	this.linkAction = cfg.linkAction || this._defaultLinkAction;
	this.request = null;
	this.counter = 1;
	this.numScrolled = 0;
	this.moving = false;
	if (cfg.animationSpeed === 0)
		cfg.animationSpeed = Number.MIN_VALUE; // so animationCompleteHandler
											   // will always run
	this.carousel = new YAHOO.extension.Carousel(id,
						{
							'animationCompleteHandler': this._animationCompleteHandler,
							'animationSpeed':           cfg.animationSpeed,
							'loadPrevHandler':          this._loadPrevHandler,
							'numVisible':               cfg.numVisible || 1,
							'prevElement':              cfg.prevElement || cfg.prevId,
							'scrollInc':                1
						});
	this.carousel.cn = this;

	// MT
	this.elLoadingPane = cfg.loadingPane;
	this.aCache = new Array(); // array to store cache requests in
	this.nMaxCacheEntries = 25; // number of requests to cache

	if (this.carousel.prevEnabled !== true)
	{
		this.carousel._enablePrev();
	}
	// /MT

	var notOpera = (navigator.userAgent.match(/opera/i) == null);
	var kl = new YAHOO.util.KeyListener(this.carousel.carouselElem,
										{ ctrl: notOpera, keys: [37, 38, 39, 40] },
										{ fn: this._handleKeypress,
										  scope: this,
										  correctScope: true });
	kl.enable();

	var ds = this.datasource;
	if (ds && typeof ds == 'object')
		this._addMenu(ds);
	else if (typeof ds == 'string')
		this._makeRequest(ds);
	else
		this._handleFailure({});
};

/**
 * Overridden to make the link action executed on every click
 */
YAHOO.extension.ColumNav.prototype._next = function(e)
{
	if (this.moving) {
		this.EVT.stopEvent(e);
		return;
	}
	var target = this.EVT.getTarget(e);
	if (target.tagName == 'SPAN')
		target = target.parentNode;
	this._removeMenus(target);
	var href = target.getAttribute('href');
	var rel = target.getAttribute('rel');
	var list = target.list;
	if (href !== null)
		this._highlight(target);

	// execute the link action on every click & add scope
	this.linkAction(e, this);

	if (list)
		this._addMenu(list);
	else if (rel && rel.match(/\bajax\b/))
		this._makeRequest(href);
	else {
		//if (this.linkAction(e))
			//return true;
	}
	this.EVT.stopEvent(e);
};

// allow other success events
YAHOO.extension.ColumNav.prototype._makeRequest = function(url, callbackSuccess)
{
	// show the pane loader
	this.elLoadingPane.style.display = 'block';
	this.elLoadingPane.style.zIndex = 1000;

	// this is not ideal but allows the callback to know the request url
	this._sLastRequestUrl = url;
	// set the success function
	var fnSuccess = callbackSuccess || this._handleSuccess;

	// check if the request is cached
	for (var i = 0; i < this.aCache.length; i++)
	{
		if (this.aCache[i].sUrl == url)
		{
			// cache hit
			//add the function scope
			fnSuccess.apply(this, [this.aCache[i].oResponse, true]);
			return;
		}
	}

	var callback = {
		'success':  fnSuccess,
		'failure':  this._handleFailure,
		'scope':    this,
		'timeout':  60000
	};
	this._abortRequest();
	this.request = this.CON.asyncRequest('GET', url, callback);
};

YAHOO.extension.ColumNav.prototype._updateCache = function(o)
{
	if (YAHOO.lang.isObject(o))
	{
		// update cache entries
		var oCache = new Object();
		oCache.sUrl = this._sLastRequestUrl;
		oCache.oResponse = o;
		this.aCache.push(oCache);

		if (this.aCache.length > this.nMaxCacheEntries)
		{
			this.aCache.shift(); // remove the oldest cache entry
		}
	}
};
YAHOO.mindtouch.getContentType = function(o)
{
	// bugfix for german IE
	var contentType = o.getResponseHeader['Content-Type'] ?
					  o.getResponseHeader['Content-Type'] :
					  o.getResponseHeader['Content-type'];

	var aSplit = contentType.split(';'); // only grab the content type not the charset

	return String(aSplit[0]).replace(/\s+$/, ''); // IE reports Content-Type having trailing ASCII 13
}

/**
 * Overrideen to display the columNav on data load success
 */
YAHOO.extension.ColumNav.prototype._handleSuccess = function(o, bFromCache)
{
	var node;
	var contentType = YAHOO.mindtouch.getContentType(o);

	if ('application/json' == contentType) {
		try {
			// requires http://www.json.org/json.js
			node = o.responseText.parseJSON();
		} catch (e) {
			this._handleFailure(o);
			return;
		}
		
		if (YAHOO.lang.isUndefined(bFromCache))
		{
			this._updateCache(o, bFromCache);
		}
	} else {
		node = o.responseXML.documentElement;
	}

	this._addMenu(node);

	this.linkNavigator.showColumNav();

	// hide the pane loader
	this.elLoadingPane.style.display = 'none';
};

YAHOO.extension.ColumNav.prototype._handleLoadPrevSuccess = function(o, bFromCache)
{
	var node;
	var contentType = YAHOO.mindtouch.getContentType(o);

	if ('application/json' == contentType) {
		try {
			// requires http://www.json.org/json.js
			node = o.responseText.parseJSON();
		} catch (e) {
			this._handleFailure(o);
			return;
		}

		if (YAHOO.lang.isUndefined(bFromCache))
		{
			this._updateCache(o, bFromCache);
		}
	} else {
		node = o.responseXML.documentElement;
	}
	this._addPrevMenu(node);

	// hide the pane loader
	this.elLoadingPane.style.display = 'none';
};


YAHOO.extension.ColumNav.prototype._addPrevMenu = function(node)
{
	var menu = this._createMenu(node);
	this.counter++;
	this.carousel.insertBefore(1, menu); // 1 based

	// TODO: select the menu element
	this._focus(menu);
	
	// this was causing redundant prev button registrations
	if (this.carousel.prevEnabled === false)
	{
		this.carousel._enablePrev();
	}
};


/**
 * ============================================================================
 * ============================================================================
 * Carousel Overrides
 */
YAHOO.extension.Carousel.prototype._scrollPrev = function(e, oSelf)
{
	if(oSelf.scrollPrevAnim.isAnimated()) {
		return false;
	}

	// MT
	// check if we need to try and load the previous frame
	if (oSelf.firstVisible == 1)
	{
		YAHOO.util.Event.stopEvent(e);
		// user tried to scroll back on the first element

		// e target is the previous button
		var oMenu = oSelf.getItem(1).getElementsByTagName('div')[0];
		// get the first anchor element in the menu
		var oAnchor = oMenu.getElementsByTagName('a')[0];

		var sRel = oAnchor.getAttribute('rel');
		var sHref = oAnchor.getAttribute('href');

		if (sRel && sRel.match(/\bprevajax\b/))
		{
			// going up!
			if (oSelf.prevEnabled === true)
			{
				oSelf._disablePrev();
			}
			oSelf.cn._makeRequest(sHref, oSelf.cn._handleLoadPrevSuccess);
		}
	}
	else
	{
		oSelf._scrollPrevInc(oSelf, oSelf.scrollInc, (oSelf.cfg.getProperty("animationSpeed") !== 0));
		// revert the previous button state
		if (oSelf.prevEnabled !== true)
		{
			oSelf._enablePrev();
		}
	}
	// /MT
};
