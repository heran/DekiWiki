// YUI is awesome!
YAHOO.mindtouch.LinkNavigator.prototype.sNavigateUrlAppend = 'type=image';

/**
 * Initializes the dialog
 */
YAHOO.mindtouch.LinkNavigator.prototype.initNavigator = function(oParams)
{
	this.setButtonStatus(false);
	this.oImageTools = new YAHOO.mindtouch.ImageTools(this);
	// use 1 column for the columnav
	this._oConfig.columNavColumns = 1;
	// need to determine was images are internal
	this.oImageTools.sBaseUrl = (this._oConfig.baseUrl) ? this._oConfig.baseUrl : '';
	this.oImageTools.sApiUrl = (this._oConfig.apiUrl) ? this._oConfig.apiUrl : '';

	// focus the text box
	this._oTextBox.focus();

	// set the initial text message for the search area
	this._oAutoContainer.style.visibility = 'hidden'; // hides flashing
	this._clearSearchResults(wfMsg('Dialog.LinkTwo.message-enter-search'));
	this._oAutoContainer.style.visibility = 'visible';
	
	// setup the dialog state
	YAHOO.log('IN Params: ' + YAHOO.lang.dump(oParams));
	//alert('IN Params: ' + YAHOO.lang.dump(oParams));

	if (YAHOO.lang.isObject(oParams))
	{
		// need to add these to the incoming args
		this._nPageId = (oParams.nPageId) ? oParams.nPageId : '';
		this._sUserName = (oParams.sUserName) ? oParams.sUserName : '';
		
		var bImage = YAHOO.lang.isValue(oParams.sSrc);
		if (bImage)
		{
			// check if the link is internal, ie, from the api
			var bInternal = oParams.bInternal || this.oImageTools.isInternal(oParams.sSrc);

			if (bInternal)
			{
				var nPos = oParams.sSrc.indexOf(this.apiUrl);
				this._oTextBox.value = String(oParams.sSrc).substring(nPos);
			}
			else
			{
				this._oTextBox.value = oParams.sSrc;
			}
			
			this._registerLinkInfo(oParams.sAlt, oParams.sSrc);
			
			this.oImageTools.setImageWrap(oParams.sWrap);
			// set the width & height inputs
			this.oImageTools.setCustomInputs(oParams.nWidth, oParams.nHeight);
			
			this.oImageTools.setImageWidth(oParams.nWidth);
			this.oImageTools.setImageHeight(oParams.nHeight);

			// make sure we are working on an internal image
			if (bInternal)
			{
				//this.oImageTools.setImageSize('custom');
				this.showNavigatorFromImage();
				return;
			}
			else
			{
				this.setButtonStatus(true);
			}
		}
	}
	
	YAHOO.util.Event.addListener(this._oTextBox, "keyup", this.textboxChangeEvent, this, true);
	
	if ( !YAHOO.env.ua.ie )
	{
		YAHOO.util.Event.addListener(this._oTextBox, "input", this.textboxChangeEvent, this, true);
	}

	YAHOO.util.Event.addListener(this._oTextBox, "paste", function (sEventName, oArgs, oSelf) {
		var oSelf = this;
		window.setTimeout(function(sEventName, oArgs, oSelf) {return function() {oSelf.textboxChangeEvent.apply(oSelf, [sEventName, oArgs, oSelf])}}(sEventName, oArgs, oSelf), 1);
	}, this, true);
	
	this.showNavigatorFromPage(true);
};

YAHOO.mindtouch.LinkNavigator.prototype.textboxChangeEvent = function(sEventName, oArgs, oSelf)
{
	var sText = this._oTextBox.value;
	
	if ( this._externalLinksRegex.test(sText) )
	{
		this.setButtonStatus(true);
	}
	else
	{
		this.setButtonStatus(false);
	}
	
	this.oImageTools.reset();
};

/*
 * Loads the navigator with the entry point as the start page
 * @param bInternal sets whether the image is internal
 */
YAHOO.mindtouch.LinkNavigator.prototype.showNavigatorFromImage = function()
{
	// check if a pageId is set
	// generate initial url here
	var sUrl = this._linkNavigateUrl;
	var sFile = String(this._oTextBox.value);

	if (this._nPageId > 0)
	{
		sUrl += this._nPageId;// + '&parent=1';
	}

	if (sFile.length > 0)
	{
		// internal file matching regex
		//var rRegex = /.*[^\/]*\/[^\/]*\/files\/([0-9]*)\/.*/;
		var aMatches = this.oImageTools.rParseInternal.exec(sFile);

		if (YAHOO.lang.isValue(aMatches[2]))
		{
			// set that we are loading in an image
			this.oImageTools.bInitialLoad = true;

			sUrl += '&file_id=' + aMatches[2];
		}
	}

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
	oParams.sSrc = this.oImageTools.getImageSource(this._oTextBox.value);
	oParams.sAlt = this._sSystemLinkCaption;
	oParams.sWrap = this.oImageTools.getImageWrap();
	// don't include sizes for original
	if (this.oImageTools.getImageSize() != 'original')
	{
		oParams.sFullSrc = this.oImageTools.getImageSource(this._oTextBox.value, 'original');
		oParams.nWidth = this.oImageTools.getImageWidth();
		oParams.nHeight = this.oImageTools.getImageHeight();
	}
	oParams.bInternal = this.oImageTools.isInternal(oParams.sSrc);

	// add the sWrapClass
	switch (oParams.sWrap)
	{
		case 'left':
			oParams.sWrapClass = 'lwrap';
			break;
		case 'right':
			oParams.sWrapClass = 'rwrap';
			break;
		default:
			oParams.sWrapClass = 'default';
	}

	//YAHOO.log('OUT Params: ' + YAHOO.lang.dump(oParams));
	return oParams;
};

// determines if there is an image to insert
YAHOO.mindtouch.LinkNavigator.prototype.validateLink = function()
{
	var sSrc = this.oImageTools.getImageSource(this._oTextBox.value);
	return (YAHOO.lang.trim(sSrc) != '');
};

YAHOO.mindtouch.LinkNavigator.prototype.clickLink = function(elClicked, sTitle)
{
	var sPath = elClicked.getAttribute('path');
	var nId = elClicked.getAttribute('id');
	var bHasPreview = YAHOO.lang.isValue(elClicked.getAttribute('preview'));

	// make sure the user clicked on an image
	if (bHasPreview)
	{
		this._registerLinkInfo(sTitle, sPath);

		// load the dialog settings
		this.oImageTools.loadImage(elClicked);
		// enable the insert image button
		this.setButtonStatus(true);
	}
	else
	{
		this._oTextBox.value = '';
		this.oImageTools.reset();
		// disable the insert image button
		this.setButtonStatus(false);
	}
};

/* -------------------------------------------------------------------
 * -------------------------------------------------------------------
 * - End LinkNavigator Overrides
 */


YAHOO.mindtouch.ImageTools = function(oLinkNavigator)
{
	// helper
	var Dom = YAHOO.util.Dom;
	this.Dom = Dom;

	// image size constants
	this.SMALL_SIZE = 160;
	this.SMALL_API_IMAGE = 'thumb';
	this.MEDIUM_SIZE = 350;
	this.MEDIUM_API_IMAGE = 'webview';
	this.LARGE_SIZE = 550;
	this.LARGE_API_IMAGE = 'webview';
	
	// saves the currently set width and height
	this.nWidth = null;
	this.nHeight = null;
	// saves the currently loaded image size
	this.nImageWidth = null;
	this.nImageHeight = null;
	// used for the dom custom size events
	this.nLastCustomWidth = -1;
	this.nLastCustomHeight = -1;

	this.elImageViewer = Dom.get('imageView');
	this.elCustomFields = Dom.get('customFields');

	this.oImageWraps = new Object();
	this.oImageWraps['default'] = Dom.get('image-wrap-default');
	this.oImageWraps['left'] = Dom.get('image-wrap-left');
	this.oImageWraps['right'] = Dom.get('image-wrap-right');
	
	this.oImageSizes = new Object();
	this.oImageSizes['small'] = Dom.get('image-size-small');
	this.oImageSizes['medium'] = Dom.get('image-size-medium');
	this.oImageSizes['large'] = Dom.get('image-size-large');
	this.oImageSizes['original'] = Dom.get('image-size-original');
	this.oImageSizes['custom'] = Dom.get('image-size-custom');

	this.elImageSizeWidth = Dom.get('image-size-width');
	this.elImageSizeHeight = Dom.get('image-size-height');

	// Setup events
	var oSelf = this;
	// Custom events
	oLinkNavigator.showColumNavEvent.subscribe(oSelf.afterShowColumNav, oSelf, true);
	oLinkNavigator._autoComp.textboxKeyEvent.subscribe(oSelf.textboxKeyEvent, oSelf, true);

	// Dom events
	for (var key in this.oImageSizes)
	{
		if ( typeof this.oImageSizes[key] == 'object' )
			YAHOO.util.Event.addListener(this.oImageSizes[key], 'click', oSelf._onImageSizeChange, oSelf);
	}
	
	YAHOO.util.Event.addListener(this.elImageSizeWidth, 'blur', oSelf._onImageSizeBlur, oSelf);
	YAHOO.util.Event.addListener(this.elImageSizeHeight, 'blur', oSelf._onImageSizeBlur, oSelf);

	this.reset();
};

YAHOO.mindtouch.ImageTools.prototype.rParseInternal = /(\/[^\/]+\/[^\/]+\/files\/([0-9]+)\/=+)(.*)/;

/**
 * Custom linkNavigator event
 * Resets the initial load state
 */
YAHOO.mindtouch.ImageTools.prototype.afterShowColumNav = function(sEventName, oArgs, oSelf)
{
	// check if the custom input is enabled
	this.bInitialLoad = false;
	
	if ( !this.nWidth && !this.nHeight && this.nImageWidth && this.nImageHeight )
	{
		this.setCustomInputs(this.nImageWidth, this.nImageHeight);
	}
};

YAHOO.mindtouch.ImageTools.prototype.textboxKeyEvent = function(sEventName, oArgs, oSelf)
{
	oSelf.reset();
};

YAHOO.mindtouch.ImageTools.prototype.reset = function()
{
	// clear the image viewer
	// TODO: change message for external links
	this.elImageViewer.innerHTML = wfMsg('Dialog.Image.no-preview-available');
	
	// TODO: deselect anything that might be selected in the navigator

	// initialize the dialog state
	this.resetImageSizeInputs(false);
};

/*
 * Dom Events
 */
YAHOO.mindtouch.ImageTools.prototype._onImageSizeChange = function(e, oSelf)
{
	var elTarget = YAHOO.util.Event.getTarget(e);
	var sSize = String(elTarget.id).substring(11);
	oSelf.setImageSizeInput(sSize, true);
};

YAHOO.mindtouch.ImageTools.prototype._onImageSizeBlur = function(e, oSelf)
{
	var elTarget = YAHOO.util.Event.getTarget(e);
	var sAspect = String(elTarget.id).substring(11);

	if (sAspect == 'width')
	{
		var nNewWidth = oSelf.elImageSizeWidth.value;
		if (nNewWidth != oSelf.nLastCustomWidth)
		{
			var nRatio = nNewWidth / oSelf.nImageWidth;
			oSelf.setScaledImageSize(nRatio, null);

			oSelf.nLastCustomWidth = oSelf.getImageWidth();
			oSelf.nLastCustomHeight = oSelf.getImageHeight();

			oSelf.setCustomInputs(oSelf.nLastCustomWidth, oSelf.nLastCustomHeight);
		}
	}
	else
	{
		var nNewHeight = oSelf.elImageSizeHeight.value;
		if (nNewHeight != oSelf.nLastCustomHeight)
		{
			var nRatio = nNewHeight / oSelf.nImageHeight;
			oSelf.setScaledImageSize(null, nRatio);

			oSelf.nLastCustomWidth = oSelf.getImageWidth();
			oSelf.nLastCustomHeight = oSelf.getImageHeight();

			oSelf.setCustomInputs(oSelf.nLastCustomWidth, oSelf.nLastCustomHeight);
		}
	}
};

/*
 * Member functions
 */
// elImage is a dom node from the link navigator
YAHOO.mindtouch.ImageTools.prototype.loadImage = function(elImage)
{
	var nWidth = elImage.getAttribute('width');
	var nHeight = elImage.getAttribute('height');
	var sPath = elImage.getAttribute('path');
	var sPreview = elImage.getAttribute('preview');

	// get the thumbnail
	var elImage = document.createElement('img');
	elImage.setAttribute('src', sPreview);

	// add the image to the viewer
	this.elImageViewer.innerHTML = '';
	this.elImageViewer.appendChild(elImage);

	// set the image size info
	this.nImageWidth = nWidth;
	this.nImageHeight = nHeight;

	// check if this is the initial load of an image
	if (this.bInitialLoad)
	{
		this.bInitialLoad = false;
		sSize = 'custom';

		this.resetImageSizeInputs(true, this.nImageWidth, this.nImageHeight, sSize);
	}
	else
	{
		// set the custom width & height
		this.setCustomInputs(this.nImageWidth, this.nImageHeight);

		// set the image buttons
		this.resetImageSizeInputs(true, this.nImageWidth, this.nImageHeight);
	}
};

/*
 * If bEnabled is true then nWidth is required
 */
YAHOO.mindtouch.ImageTools.prototype.resetImageSizeInputs = function(bEnabled, nWidth, nHeight, sSize)
{
	this.elCustomFields.style.visibility = 'hidden';

	if (bEnabled)
	{
		var sSetSize = (sSize) ? sSize : 'original';
		this.setImageSizeInput(sSetSize);
		
		// disable any inputs
		this.oImageSizes['custom'].disabled = false;
		this.oImageSizes['original'].disabled = false;
		this.oImageSizes['large'].disabled = (nWidth < this.LARGE_SIZE) && (nHeight < this.LARGE_SIZE) ? true : false;
		this.oImageSizes['medium'].disabled = (nWidth < this.MEDIUM_SIZE) && (nHeight < this.MEDIUM_SIZE) ? true : false;
		this.oImageSizes['small'].disabled = false;
	}
	else
	{
		for (var key in this.oImageSizes)
		{
			if ( typeof this.oImageSizes[key] == 'object' )
				this.oImageSizes[key].disabled = true;
		}
	}
};

YAHOO.mindtouch.ImageTools.prototype.setImageSizeInput = function(sSize)
{
	var nNewSize = 0;
	this.elCustomFields.style.visibility = 'hidden';

	switch (sSize)
	{
		default:
			sSize = 'small';
		case 'small':
			nNewSize = this.SMALL_SIZE;
			break;

		case 'medium':
			nNewSize = this.MEDIUM_SIZE;
			break;

		case 'large':
			nNewSize = this.LARGE_SIZE;
			break;

		case 'original':
			this.setImageWidth(this.nImageWidth);
			this.setImageHeight(this.nImageHeight);
			this.oImageSizes[sSize].checked = true;
			return;

		case 'custom':
			this.elCustomFields.style.visibility = 'visible';
			// focus to the width box
			this.elImageSizeWidth.focus();
			this.oImageSizes[sSize].checked = true;
			return;
	}
	// check the selected box
	this.oImageSizes[sSize].checked = true;

	// compute the image ratio
	var nWidthRatio = nNewSize / this.nImageWidth;
	var nHeightRatio = nNewSize / this.nImageHeight;

	this.setScaledImageSize(nWidthRatio, nHeightRatio, true);
};

YAHOO.mindtouch.ImageTools.prototype.setScaledImageSize = function(nWidthRatio, nHeightRatio, bNoEnlarge)
{
	var nRatio = 1;

	if (bNoEnlarge)
	{
		if (nWidthRatio > 1)
		{
			nWidthRatio = 1;
		}

		if (nHeightRatio > 1)
		{
			nHeightRatio = 1;
		}

		nRatio = (nHeightRatio < nWidthRatio) ? nHeightRatio: nWidthRatio;
	}
	else
	{
		// only width or height specified
		if (YAHOO.lang.isValue(nWidthRatio))
		{
			nRatio = nWidthRatio;
		}
		else
		{
			nRatio = nHeightRatio;
		}
	}

	// compute the new width and height
	var nNewWidth = Math.floor(this.nImageWidth * nRatio);
	var nNewHeight = Math.floor(this.nImageHeight * nRatio);

	// update the height info
	this.setImageWidth(nNewWidth);
	this.setImageHeight(nNewHeight);
};


YAHOO.mindtouch.ImageTools.prototype.setImageSize = function(sSize)
{
	for (var key in this.oImageSizes)
	{
		if (key == sSize)
		{
			this.oImageSizes[key].checked = true;
			return;
		}
	}
	this.oImageSizes['small'].checked = true;	
};

YAHOO.mindtouch.ImageTools.prototype.getImageSize = function()
{
	for (var key in this.oImageSizes)
	{
		if (this.oImageSizes[key].checked)
		{
			return key;
		}
	}
};

YAHOO.mindtouch.ImageTools.prototype.setImageWrap = function(sWrap)
{
	for (var key in this.oImageWraps)
	{
		if (key == sWrap)
		{
			this.oImageWraps[key].checked = true;
			return;
		}
	}
	this.oImageWraps['default'].checked = true;
};

YAHOO.mindtouch.ImageTools.prototype.getImageWrap = function()
{
	for (var key in this.oImageWraps)
	{
		if (this.oImageWraps[key].checked)
		{
			return key;
		}
	}
};

YAHOO.mindtouch.ImageTools.prototype.setCustomInputs = function(nWidth, nHeight)
{
	this.elImageSizeWidth.value = nWidth;
	this.elImageSizeHeight.value = nHeight;
};

YAHOO.mindtouch.ImageTools.prototype.setImageWidth = function(nWidth) {this.nWidth = nWidth;};
YAHOO.mindtouch.ImageTools.prototype.getImageWidth = function() {return this.nWidth;};
YAHOO.mindtouch.ImageTools.prototype.setImageHeight = function(nHeight) {this.nHeight = nHeight;};
YAHOO.mindtouch.ImageTools.prototype.getImageHeight = function() {return this.nHeight;};

YAHOO.mindtouch.ImageTools.prototype.isInternal = function(sSrc)
{
	var bAbsolute = (YAHOO.lang.trim(String(sSrc)).indexOf(this.sBaseUrl + this.sApiUrl) == 0); // -1 means not found
	var bRelative = (YAHOO.lang.trim(String(sSrc)).indexOf(this.sApiUrl) == 0);

	return bAbsolute | bRelative;
};
/*
 * Encodes the image source for internal links, external is on its own
 */
YAHOO.mindtouch.ImageTools.prototype.getImageSource = function(sSrc, sUseImageSize)
{
	// attach the specific image preview based on the size
	if (this.isInternal(sSrc))
	{
		// added the api size to the url
		var nSizePos = sSrc.indexOf('size=');
		var nQueryPos = sSrc.indexOf('?');

		// parse out the part that needs to be encoded
		var sEncode = (nQueryPos != -1) ?  sSrc.substr(0, nQueryPos) : sSrc;
		var aMatches = sEncode.match(this.rParseInternal);
		if (aMatches.length > 1)
		{
			sSrc = aMatches[1] + encodeURIComponent(aMatches[3]);
		}

		// only set the size if it has not been set yet. don't try to be a hero
		if (nSizePos == -1)
		{
			var sApiSize = '';
			
			var sUseSize = sUseImageSize ? sUseImageSize : this.getImageSize();
			switch (sUseSize)
			{
				case 'small':
					sApiSize = this.SMALL_API_IMAGE;
					break;
				case 'medium':
					sApiSize = this.MEDIUM_API_IMAGE;
					break;
				case 'large':
					sApiSize = this.LARGE_API_IMAGE;
					break;
				case 'custom':
					var nWidth = this.getImageWidth();
					
					if (nWidth <= this.LARGE_SIZE)
					{
						sApiSize = this.LARGE_API_IMAGE;
					}
					break;

				case 'original':
				default:
			}
			
			if (sApiSize.length > 0)
			{
				sSrc += (nQueryPos == -1) ? '?' : '&';
				sSrc += 'size=' + sApiSize;
			}
		}
	}

	return sSrc;
};
