
// LinkTab
(function()
{
	// shortcuts
	var Lang = YAHOO.lang, Dom = YAHOO.util.Dom, Event = YAHOO.util.Event;

	Tab = function (sLabel)
	{
		Tab.superclass.constructor.call(this, sLabel);

		this.setName('link');
	};

	YAHOO.lang.extend(Tab, YAHOO.mindtouch.widget.Tab);

	var proto = Tab.prototype;
	/*
	 * Class Variables
	 */
	proto.elInput = 'linktab-search';
	proto.elResults = 'linktab-results';
	
	proto.bSearching = false;
	proto.rExternalLink = /(.*:\/\/|\\\\).*/;
	proto.MIN_QUERY_LENGTH = 2;

	/*
	 * Class Methods
	 */
	proto.createContentDom = function()
	{
		aHtml = [
				 '<label for="'+ this.elInput +'" />Link to:</label>',
				 '<input id="'+ this.elInput +'" type="text" value="" />',
				 '<div id="'+ this.elResults +'"></div>'
				];
		this.elContent.innerHTML = aHtml.join('');
	};

	proto.postCreateDom = function()
	{
		// retrieve the newly created dom nodes
		this.elInput = Dom.get(this.elInput);
		this.elResults = Dom.get(this.elResults);

		// autocomplete datasource
		this.oDataSource = new YAHOO.widget.DS_XHR('/deki/gui/link.php?method=search', ["\n", "\t"]);
		this.oDataSource.responseType = YAHOO.widget.DS_XHR.TYPE_FLAT;
		this.oDataSource.maxCacheEntries = 60;
		this.oDataSource.queryMatchSubset = true;
		
		// create the autocomplete
		this.oAutoComp = new YAHOO.widget.AutoComplete(this.elInput, this.elResults, this.oDataSource);
		this.oAutoComp.alwaysShowContainer = true;
		this.oAutoComp.delimChar = null;
		this.oAutoComp.animVert = false;
		this.oAutoComp.queryDelay = 0.4;
		// start with search disabled
		this.disableSearch();

		// override some auto complete methods
		this.oAutoComp.doBeforeExpandContainer = this.acBeforeExpandContainer;
		this.oAutoComp.oTab = this; // needed for the browse button clicks
		this.oAutoComp.formatResult = this.acCustomFormatter;

		// Custom events
		this.oAutoComp.dataRequestEvent.subscribe(this.acDataRequestListener, this);
		this.oAutoComp.dataReturnEvent.subscribe(this.acDataReturnListener, this);
		this.oAutoComp.textboxKeyEvent.subscribe(this.acTextboxKeyListener, this);
		this.oAutoComp.itemSelectEvent.subscribe(this.acItemSelectListener, this);
		

		// set the initial text message for the search area
		this.setMessage(wfMsg('Dialog.LinkTwo.message-enter-search'));
		this.oAutoComp._toggleContainer(true); // private method, needed to show the container
		// enable search
		this.enableSearch();
	};


	proto.show = function(oContext)
	{
		Tab.superclass.show.call(this, oContext);
		
		if (this.oContext == null)
		{
			// only load the context if none exists, standalone tab
			this.oContext = oContext;

			this.elInput.value = oContext.sHref;
			// select the text
			this.elInput.select();
			this.elInput.focus();

			this.oAutoComp.sendQuery(this.elInput.value);
		}

		return true;
	};

	proto.hide = function()
	{
		Tab.superclass.hide.call(this);
		// TODO: make sure there isn't an autocomplete request in progress
		if (this.bSearching)
		{
			this.setMessage(wfMsg('Dialog.LinkTwo.message-enter-search'));
		}

		return true;
	};

	proto.isSearchEnabled = function() { return (this.oAutoComp.minQueryLength != -1); };
	proto.isLinkExternal = function() { return this.rExternalLink.test(this.elInput.value); };

	proto.enableSearch = function()
	{
		this.oAutoComp.minQueryLength = this.MIN_QUERY_LENGTH;
	};

	proto.disableSearch = function()
	{
		this.oAutoComp.minQueryLength = -1;
	};
	
	proto.setMessage = function(sHeader, sBody)
	{
		var sBody = sBody || "&nbsp;";
		this.oAutoComp.setHeader(sHeader);
		this.oAutoComp.setBody(sBody);
	};

	/*
	 * Fired when the browse button is clicked in the suggested results
	 */
	proto.onClickBrowse = function(e, oSelf)
	{
		var elTarget = Event.getTarget(e);
		oSelf.oContext.nCurrentPageId = elTarget.getAttribute('pageId');
		oSelf.oContext.nCurrentFileId = elTarget.getAttribute('fileId');

		// call the browse tab
		oSelf.dispatchTabEvent.fire('browse');

		// clear out the context
		oSelf.oContext.nCurrentPageId = null;
		oSelf.oContext.nCurrentFileId = null;

		Event.stopEvent(e);
		return false;
	};


	// start AutoComplete specific functions
	/**
	 * Custom formatter function for the list items
	 */
	proto.acCustomFormatter = function(oResultItem, sQuery)
	{
		var oSelf = this;
		var sValue = oResultItem[0];
		var sClass = oResultItem[1];
		var sText = oResultItem[2];
		var nPageId = oResultItem[3];
		var aHtml = null;

		aHtml = [
				 '<div class="row">', // need to add this div for IE
				 '<span class="icon">',
				 '<img src="/skins/common/icons/icon-trans.gif" class="',
				 sClass,
				 '" alt="" />',
				 '</span>',
				 sText,
				 '</div>'
				];

		return (aHtml.join(""));
	};


	/**
	 * Registers the browse button for all the autocomplete items in the list
	 */
	proto.acBeforeExpandContainer = function(oTextbox, oContainer, sQuery, aResults) 
	{
		var oAutoComplete = this;
		var oSelf = oAutoComplete.oTab; // hacky

		var nItems = oAutoComplete._nDisplayedItems
		var aItems = this._aListItems;

		// Register events for the items
		for(var i = 0; i < nItems; i++)
		{
			// html object
			var elLi = aItems[i];
			var aResult = aResults[i];
			var nPageId = aResult[3];
			var nFileId = aResult[5];

			// @see formatResult 
			var elCarouselDiv = document.createElement('div');
			var elLiDiv = elLi.childNodes[0];

			Dom.addClass(elCarouselDiv, 'browse');
			elCarouselDiv.setAttribute('pageId', nPageId);
			elCarouselDiv.setAttribute('fileId', nFileId);
			// event when the user clicks the browse link
			Event.addListener(elCarouselDiv, "click", oSelf.onClickBrowse, oSelf);

			elLiDiv.appendChild(elCarouselDiv);
		}

		return true;
	};


	/**
	 * Fires when the search is sent
	 */
	proto.acDataRequestListener = function(sEventName, oArgs, oSelf)
	{
		oSelf.setMessage('<img src="/skins/common/icons/anim-circle.gif"> ' + wfMsg('Dialog.LinkTwo.message-searching'));
		// set the current search status
		oSelf.bSearching = true;
	};


	/**
	 * Fires when the search is returned
	 */
	proto.acDataReturnListener = function(sEventName, oArgs, oSelf)
	{
		// set the current search status
		oSelf.bSearching = false;

		var aResults = oArgs[2] || null;
		// need to check if length is zero for weird empty return event
		if (!Lang.isObject(aResults) || (aResults.length == 0))
		{
			return;
		}

		// need to check if the only result is empty due to IE bug
		if ( (aResults.length == 1) && (aResults[0] == '<no results>') )
		{
			oSelf.oAutoComp._oCurItem = null; // clear out the current item
			oSelf.setMessage(wfMsg('Dialog.LinkTwo.message-no-results'));
		}
		else
		{
			// data has results
			var aHtml = [
						 '<div class="resultsHeader">',
						 wfMsg('Dialog.LinkTwo.message-found'),
						 ' ',
						 aResults.length,
						 ' ',
						 wfMsg('Dialog.LinkTwo.message-results'),
						 '</div>'
						];
			oSelf.oAutoComp.setHeader(aHtml.join(""));
		}
	};


	/**
	 * Fires when the user types in the query box
	 */
	proto.acTextboxKeyListener = function(sEventName, oArgs, oSelf)
	{
		var bSearch = oSelf.isSearchEnabled();
		var bExternal = oSelf.isLinkExternal();
		if (bSearch)
		{
			if (bExternal)
			{
				oSelf.disableSearch();
				// need to override this value so that the message is displayed
				oSelf.setMessage(wfMsg('Dialog.LinkTwo.message-links-not-matched'));
			}
		}
		else if (!bSearch && !bExternal)
		{
			oSelf.enableSearch();
		}
	};


	/**
	 * Fires when an item from the autocomplete is selected
	 */
	proto.acItemSelectListener = function(sEventName, oArgs, oSelf)
	{
		var aResult = oArgs[2];
		oSelf.oContext.sText = aResult[4];
		oSelf.oContext.sHref = aResult[0];
	};


	// add to the mindtouch widget namespace
	YAHOO.mindtouch.widget.LinkTab = Tab;
})();
