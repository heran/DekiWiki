
// BrowseTab
(function()
{
	// shortcuts
	var Lang = YAHOO.lang, Dom = YAHOO.util.Dom, Event = YAHOO.util.Event;

	Tab = function (sLabel)
	{
		Tab.superclass.constructor.call(this, sLabel);

		this.setName('browse');
		this.oContext = {};
	};

	YAHOO.lang.extend(Tab, YAHOO.mindtouch.widget.Tab);

	var proto = Tab.prototype;
	/*
	 * Class Variables
	 */
	proto.elDisplay		  = 'browsetab-display';
	proto.elBrowser		  = 'browsetab-browser';
	proto.elButtons		  = 'browsetab-buttons';
	proto.elPrev		  = 'browsetab-prev';
	proto.elLoader		  = 'browsetab-loader';

	proto.DATA_SOURCE = '/deki/gui/link.php?method=navigate';

	/*
	 * Class Methods
	 */
	proto.createContentDom = function()
	{
		// TODO: localize
		aHtml = [
				 '<label class="link" for="'+ this.elDisplay +'">Link to:</label>',
				 '<div id="' + this.elDisplay + '"></div>',
				 
				 '<div class="actions">',
					'<a href="javascript:void(0);" id="' + this.elPrev + '"><span>' + 'Back' + '</span></a>',
				 '</div>',
				 '<label class="navigate" for="'+ this.elBrowser +'">Navigate:</label>',
				 '<div id="' + this.elBrowser + '" class="carousel-component"></div>',

				 '<div id="' + this.elButtons + '">',
					 '<ul>',
						 '<li class="current"><a href="#"><img src="/skins/common/icons/icon-trans.gif" /><span>'+ 'Current Page' +'</span></a></li>',
						 '<li class="home"><a href="#"><img src="/skins/common/icons/icon-trans.gif" /><span>'+ 'Root' +'</span></a></li>',
						 '<li class="user"><a href="#"><img src="/skins/common/icons/icon-trans.gif" /><span>'+ 'My Page' +'</span></a></li>',
					 '</ul>',
				 '</div>',

				 '<div id="' + this.elLoader + '"></div>'
				];
		this.elContent.innerHTML = aHtml.join('');
		// hide for the initial load
		this.elContent.style.display = 'none';

		return;
	};

	proto.postCreateDom = function()
	{
		// retrieve the newly created dom nodes
		this.elDisplay = Dom.get(this.elDisplay);
		this.elBrowser = Dom.get(this.elBrowser);
		this.elButtons = Dom.get(this.elButtons);
		this.elPrev = Dom.get(this.elPrev);
		this.elNewTitle = Dom.get(this.elNewTitle);
		this.elNewTitleInput = Dom.get(this.elNewTitleInput);
		this.elLoader = Dom.get(this.elLoader);


		// setup the custom cn functionality
		YAHOO.extension.ColumNav.prototype.DATA_SOURCE = this.DATA_SOURCE;
		// create the columnav
		this.createBrowser();


		// Dom Events
		var aItems = this.elButtons.getElementsByTagName('li');
		if (aItems.length > 0)
		{
			Event.addListener(aItems[0], 'click', this.onClickCurrentPage, this, true);
			Event.addListener(aItems[1], 'click', this.onClickHomePage, this, true);
			Event.addListener(aItems[2], 'click', this.onClickUserPage, this, true);
		}
	};


	proto.createBrowser = function(sDataSource)
	{
		//var sDataSource = sDataSource || this.DATA_SOURCE;

		var aHtml = [
					 '<div class="carousel-clip-region">',
					 '<ul class="carousel-list"></ul>',
					 '</div>'
					];
		this.elBrowser.innerHTML = aHtml.join("");

		// columnav
		var oConfig = {
						numVisible:			2,
						prevElement:		this.elPrev,
						datasource:			null,
						requestHandler:		this.cnRequestHandler,
						responseHandler:	this.cnResponseHandler,
						nextHandler:		this.cnNextHandler,
						prevHandler:		this.cnPrevHandler,
						handlerObject:		this,					// object to pass to the handlers
						requestTimeout:		60000					// 60 second timeout
					  };

		this.oColumNav = new YAHOO.extension.ColumNav(this.elBrowser.id, oConfig); // cn expects string id
	};

	proto.initBrowser = function(sDataSource)
	{
		this.oColumNav.cfg.datasource = sDataSource;

		var aHtml = [
					 '<div class="carousel-clip-region">',
					 '<ul class="carousel-list"></ul>',
					 '</div>'
					];
		this.elBrowser.innerHTML = aHtml.join("");

		// set the initial page path
		//this.setContextDetails(this.oContext.sPagePath, this.oContext.sPagePath);
		
		this.oColumNav.reset();
	};
	
	proto.getDataSourceUrl = function(nPageId, nFileId, sUserName)
	{
		var nPageId = nPageId || null;
		var nFileId = nFileId || null;
		var sUserName = sUserName || null;
		
		var aHref = new Array();
		if (nPageId)
			aHref.push('pageId=' + nPageId);
		if (nFileId)
			aHref.push('fileId=' + nFileId);
		if (sUserName)
			aHref.push('name=' + encodeURIComponent(sUserName));

		return this.DATA_SOURCE + aHref.join('&');
	};

	proto.show = function(oContext)
	{
		// do not show the tab right away
		//Tab.superclass.show.call(this, oContext);

		// clear the old context
		this.setContextDetails();
		// set the new values
		this.oContext = oContext;

		var nPageId = oContext.nCurrentPageId ? oContext.nCurrentPageId : oContext.nPageId;
		var nFileId = oContext.nCurrentFileId ? oContext.nCurrentFileId : oContext.nFileId;

		var sDataSource = this.getDataSourceUrl(nPageId, nFileId, oContext.sUserName);
		this.initBrowser(sDataSource);
		
		// makes the link dialog show a loader
		return false;
	};


	// set the link details
	proto.setContextDetails = function(sText, sHref)
	{
		var sText = sText || '';
		var sHref = sHref || '';

		YAHOO.log('Setting context: ' + sText + ' ' + sHref);
		this.elDisplay.innerHTML = sHref;

		this.oContext.sHref = sHref;
		this.oContext.sText = sText;
	};


	// Event handlers
	proto.onClickCurrentPage = function(e, oSelf)
	{
		// clear the current context
		this.setContextDetails();
		oSelf.initBrowser(oSelf.getDataSourceUrl(oSelf.oContext.nPageId, oSelf.oContext.nFileId));
		return false;
	};
	proto.onClickHomePage = function(e, oSelf)
	{
		// clear the current context
		this.setContextDetails();
		oSelf.initBrowser(oSelf.getDataSourceUrl());
		return false;
	};
	proto.onClickUserPage = function(e, oSelf)
	{
		// clear the current context
		this.setContextDetails();
		oSelf.initBrowser(oSelf.getDataSourceUrl('user', null, oSelf.oContext.sUserName));
		return false;	
	};

	// this = cn
	// oSelf = tab
	proto.cnRequestHandler = function(sEventName, oArgs, oSelf)
	{
		// show ajax loader
		oSelf.elLoader.style.display = 'block';
	};
	
	// this = cn
	// oSelf = tab
	proto.cnResponseHandler = function(sEventName, oArgs, oSelf)
	{
		// we have loaded the content
		oSelf.contentReadyEvent.fire();

		// show this tab's contents
		Tab.superclass.show.call(oSelf, oSelf.oContext);

		// hide ajax loader
		oSelf.elLoader.style.display = 'none';

		var sStatus = oArgs[0];
		var oResponse = oArgs[1];

		switch (sStatus)
		{
			case 'success':
				// the root level does not have a reponse header, can't move prev
				if ((oSelf.elDisplay.innerHTML == '') && oResponse.header)
				{
					oSelf.setContextDetails(oResponse.header.parentPath, oResponse.header.parentPath);
				}
				break;
			default:
		}
	};
	

	// this = cn
	// oSelf = tab
	proto.cnNextHandler = function(sEventName, oArgs, oSelf)
	{
		var elTarget = oArgs[0];
		var elParent = elTarget.parentNode;

		var sTitle = String(elTarget.getAttribute('title'));
		var sPath = String(elTarget.getAttribute('path'));
		var sParentPath = String(elParent.getAttribute('parentPath'));
		
		// TODO: need to append the parent path for pages, not files
		if (false && Dom.hasClass(elTarget, 'page') && (sParentPath.length > 0))
		{
			sPath = sParentPath + '/' + sPath;
			YAHOO.log('With Parent Path: ' + sPath);
		}
		oSelf.setContextDetails(sTitle, sPath);

		return Dom.hasClass(elTarget, 'columnav-has-next');
	};

	proto.cnPrevHandler = function(sEventName, oArgs, oSelf)
	{

	};

	// add to the mindtouch widget namespace
	YAHOO.mindtouch.widget.BrowseTab = Tab;
})();
