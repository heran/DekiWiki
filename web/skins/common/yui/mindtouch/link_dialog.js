/*
 * LinkDialog
 * -- Link.Tab
 * -- -- SearchTab
 * -- -- BrowseTab
 */
YAHOO.namespace("YAHOO.mindtouch");
YAHOO.namespace("YAHOO.mindtouch.widget");
// logging
if (YAHOO.lang.isObject(YAHOO.widget.Logger)) YAHOO.widget.Logger.enableBrowserConsole();


(function()
{
	// shortcuts
	var Lang = YAHOO.lang, Dom = YAHOO.util.Dom, Event = YAHOO.util.Event;
	
	LinkDialog = function(oConfig)
	{
		this.elContainer = Dom.get(oConfig.elContainer);
		var aHtml = [
					 '<div class="tabs"><ul id="'+ this.elTabs +'"></ul></div>',
					 '<div class="loader" id="'+ this.elLoader +'"></div>',
					 '<div class="error" id="'+ this.elError +'"></div>'
					];

		this.elContainer.innerHTML = aHtml.join('');
		
		this.elTabs = Dom.get(this.elTabs);
		this.elLoader = Dom.get(this.elLoader);
		this.elError = Dom.get(this.elError);

		// hide the loader by default
		this.hideAjaxLoader();
		
		// create the tabs
		var aTabs = oConfig.aTabs;
		this.aTabs = new Array();
		for (var i = 0, i_m = aTabs.length; i < i_m; i++)
		{
			this.aTabs[aTabs[i].getName()] = aTabs[i];

			aTabs[i].attach(this.elContainer, this.elTabs);
			aTabs[i].dispatchTabEvent.subscribe(this.dispatchTabListener, this);
			aTabs[i].clickTabEvent.subscribe(this.clickTabListener, this);
			aTabs[i].contentReadyEvent.subscribe(this.contentReadyListener, this);
			aTabs[i].tabErrorEvent.subscribe(this.tabErrorListener, this);
		}

		// setup the default tab
		var nActiveTab = oConfig.nShowTabIndex || 0;
		this.oActiveTab = aTabs[nActiveTab];
	};
	
	var proto = LinkDialog.prototype;
	/*
	 * Class variables
	 */
	// the context that loaded the dialog
	proto.oBaseContext = null;
	// the currently active tab object
	proto.oActiveTab = null;
	// dialog elements
	proto.elTabs	= 'linkdialog-tabs';
	proto.elLoader	= 'linkdialog-loader';
	proto.elError	= 'linkdialog-error';

	/*
	 * Class methods
	 */
	proto.initialize = function(oParams)
	{
		// generate the context from the popup params
		this.oBaseContext = this.getContextFromParams(oParams);
		// show the default tab
		this.oActiveTab.show(this.oBaseContext);
	};
	
	/*
	 * Can be overridden to cater to different editors
	 * Cleans up incoming params as well
	 */
	proto.getContextFromParams = function(oParams)
	{
		var oContext = {// constants, tabs don't touch
						nPageId: null,
						nFileId: null,
						sUserName: null,
						// text the user highlighted
						sSelected: null,
						
						// can be modified by the tabs
						nCurrentPageId: null,
						nCurrentFileId: null,
						sHref: null,
						// text set by the tabs
						sText: null
					   };

		oContext.nPageId = oParams.contextTopicID;
		oContext.nFileId = null;
		oContext.sUserName = oParams.userName;
		

		var sSel = Lang.trim(String(oParams.f_text));
		if (sSel.length > 0)
		{
			oContext.sSelected = sSel;
		}

		oContext.sHref = oParams.f_href;
		// this happens when the user does not select an existing link => new link
		if (oContext.sHref.substr(0, 2) == './')
		{
			oContext.sHref = oContext.sHref.substring(2);
		}

		return oContext;
	};

	/**
	 * Should be overridden for each dialog return type. e.g. link, image
	 *
	 * @return Object containing the information to update
	 */
	proto.getParamsFromContext = function()
	{
		var oContext = this.oActiveTab.getContext();
		var sText = (this.oBaseContext.sSelected) ? this.oBaseContext.sSelected : oContext.sText;
		if (sText.length < 1)
		{
			// if the user did not select any text, and specified external link
			sText = oContext.sHref;
		}
		var oParams = {f_text: sText,
					   f_href: oContext.sHref
					  };

		return oParams;
	};

	// notifies the tab by name
	// this = calling tab, oSelf = LinkDialog
	proto.dispatchTabListener = function(sEventName, oArgs, oSelf)
	{
		var sTabName = oArgs[0];
		var oTab = oSelf.aTabs[sTabName];

		if (oTab)
		{
			oSelf.activateTab(oTab);
		}
	};
	
	// fired when a tab gets a click
	// this = calling tab, oSelf = LinkDialog
	proto.clickTabListener = function(sEventName, oArgs, oSelf)
	{
		oSelf.activateTab(this);
	};

	// fired when a tab's content is ready
	// this = calling tab, oSelf = LinkDialog
	proto.contentReadyListener = function(sEventName, oArgs, oSelf)
	{
		oSelf.hideAjaxLoader();
	};

	// fired when a tab experiences an error
	// this = calling tab, oSelf = LinkDialog
	proto.tabErrorListener = function(sEventName, oArgs, oSelf)
	{
		var sTitle = oArgs[0];
		var sMessage = oArgs[1];
		oSelf.showError(sTitle, sMessage);
	};


	proto.activateTab = function(oTab)
	{
		if (this.oActiveTab != oTab)
		{
			this.oActiveTab.hide();

			// show the loader
			this.showAjaxLoader();
			if (oTab.show(this.oActiveTab.getContext()))
			{
				this.hideAjaxLoader();
			}

			this.oActiveTab = oTab;
		}
	};


	proto.showAjaxLoader = function()
	{
		this.hideError();
		this.elLoader.style.display = 'block';
	};

	proto.hideAjaxLoader = function()
	{
		this.elLoader.style.display = 'none';
	};
	
	// state when something bad happens
	proto.showError = function(sTitle, sMessage)
	{
		// set the default messages
		var sTitle = sTitle || 'Error';
		var sMessage = sMessage || 'Something is broken';

		this.hideAjaxLoader();

		this.oActiveTab.hide();
		this.oActiveTab = null;
		
		this.elError.innerHTML = '<h1>' + sTitle + '</h1><p>' + sMessage + '</p>';
		this.elError.style.display = 'block';
	};

	proto.hideError = function()
	{
		this.elError.style.display = 'none';
	};


	// add to the mindtouch namespace
	YAHOO.mindtouch.LinkDialog = LinkDialog;
})();



(function()
{
	// shortcuts
	var Lang = YAHOO.lang, Dom = YAHOO.util.Dom, Event = YAHOO.util.Event;

	Tab = function(sLabel)
	{
		this.sLabel = sLabel;

		this.elContent = document.createElement('div');
		this.elTab = document.createElement('li');
	};

	var proto = Tab.prototype;
	
	/*
	 * Class variables
	 */
	proto.sName = ''; /* unique name for the tab */
	proto.sLabel = '';
	// tab li element
	proto.elTab = null
	// root div content element
	proto.elContent = null;
	
	// object for saving the context
	proto.oContext = null;

	/*
	 * Class methods
	 */
	proto.attach = function(elContainer, elTabs)
	{
		this.createTabDom();
		this.createContentDom();
		
		this.elContent.id = this.getName() + '-content';
		this.elContent.style.display = 'none';
		elContainer.appendChild(this.elContent);

		this.elTab.id = this.getName() + '-tab';
		elTabs.appendChild(this.elTab);
		
		this.registerEvents();
		this.postCreateDom();
	};

	proto.onClickTab = function(e, oSelf)
	{
		this.clickTabEvent.fire();

		Event.stopEvent(e);
		return false;
	};
	
	/*
	 * Tab creation methods
	 */
	// create the dom elements and attach to this doc frag
	proto.createTabDom = function()
	{
		var elAnchor = document.createElement('a');
		elAnchor.setAttribute('href', '#');
		Event.addListener(elAnchor, 'click', this.onClickTab, this, true);
		elAnchor.innerHTML = '<span>' + this.getLabel() + '</span>';

		Dom.addClass(this.elTab, this.getName());
		this.elTab.appendChild(elAnchor);
	};

	proto.createContentDom = function() {};

	// expose the tab related custom events
	proto.registerEvents = function()
	{
		this.clickTabEvent = new YAHOO.util.CustomEvent('clickTabEvent', this);
		this.contentReadyEvent = new YAHOO.util.CustomEvent('contentReadyEvent', this);
		this.dispatchTabEvent = new YAHOO.util.CustomEvent('dispatchTabEvent', this);
		this.tabErrorEvent = new YAHOO.util.CustomEvent('tabErrorEvent', this);
	};

	// tab dom can be operated on now	
	proto.postCreateDom = function() {}; // implemented by children
	
	/**
	 * Displays the tab
	 * Children must implement to handle context
	 *
	 * Returning false will have the dialog display a loader until
	 * contentReadyEvent is fired
	 */
	proto.show = function(oContext)
	{
		this.elContent.style.display = 'block';
		Dom.addClass(this.elTab, 'active');
		return true;
	};

	proto.hide = function()
	{
		this.elContent.style.display = 'none';
		Dom.removeClass(this.elTab, 'active');
		return true;
	};

	proto.toString = function() { return '<Mindtouch.Tab ' + this.getName() + ' />'; };
	proto.getLabel = function() { return this.sLabel; };
	
	proto.setName = function(sName) { this.sName = sName; };
	proto.getName = function() { return this.sName; };

	proto.getContext = function() { return this.oContext; };

	// add to the mindtouch widget namespace
	YAHOO.mindtouch.widget.Tab = Tab;
})();

