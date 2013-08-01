
// FlickrTab
(function()
{
	// shortcuts
	var Lang = YAHOO.lang, Dom = YAHOO.util.Dom, Event = YAHOO.util.Event;

	Tab = function (sLabel)
	{
		Tab.superclass.constructor.call(this, sLabel);

		this.setName('flickr');
	};

	YAHOO.lang.extend(Tab, YAHOO.mindtouch.widget.Tab);

	var proto = Tab.prototype;
	/*
	 * Class Variables
	 */

	/*
	 * Class Methods
	 */
	proto.createContentDom = function()
	{
		aHtml = ['<label for="flikr_search">Tag:</label>',
				 '<input type="text" value="" id="flickr_search"><div id="flickr_results">',
				 '<p>Enter flickr tags into the box above, separated by commas. Be patient, ',
				 'this example may take a few seconds to get the images.</div>'
				];
		this.elContent.innerHTML = aHtml.join('');
	};

	proto.postCreateDom = function()
	{
		YAHOO.util.Event.onAvailable('flickr_search', function() {
			YAHOO.util.Event.on('flickr_results', 'click', function(ev) {
				var tar = YAHOO.util.Event.getTarget(ev);
				if (tar.tagName.toLowerCase() == 'img') {
					if (tar.getAttribute('fullimage', 2)) {
						var img = tar.getAttribute('fullimage', 2),
							title = tar.getAttribute('fulltitle'),
							owner = tar.getAttribute('fullowner'),
							url = tar.getAttribute('fullurl');
						this.toolbar.fireEvent('flickrClick', {
							type: 'flickrClick',
							img: img,
							title: title,
							owner: owner, 
							url: url
						});
					}
				}
			});
			oACDS = new YAHOO.widget.DS_XHR("/deki/gui/link.php?method=flickr",
				["photo", "title", "id", "owner", "secret", "server"]);
			oACDS.scriptQueryParam = "tags";
			oACDS.responseType = YAHOO.widget.DS_XHR.TYPE_XML;
			oACDS.maxCacheEntries = 0;
			//oACDS.scriptQueryAppend = "method=flickr.photos.search";

			// Instantiate AutoComplete
			oAutoComp = new YAHOO.widget.AutoComplete('flickr_search','flickr_results', oACDS);
			oAutoComp.autoHighlight = false;
			oAutoComp.alwaysShowContainer = true;     
			oAutoComp.formatResult = function(oResultItem, sQuery) {
				// This was defined by the schema array of the data source
				var sTitle = oResultItem[0];
				var sId = oResultItem[1];
				var sOwner = oResultItem[2];
				var sSecret = oResultItem[3];
				var sServer = oResultItem[4];
				var urlPart = 'http:/'+'/static.flickr.com/' + sServer + '/' + sId + '_' + sSecret;
				var sUrl = urlPart + '_s.jpg';
				var lUrl = urlPart + '_m.jpg';
				var fUrl = 'http:/'+'/www.flickr.com/photos/' + sOwner + '/' + sId;
				var sMarkup = '<img src="' + sUrl + '" fullimage="' + lUrl + '" fulltitle="' + sTitle + '" fullid="' +
					sOwner + '" fullurl="' + fUrl + '" class="yui-ac-flickrImg" title="Click to add this image to the editor"><br>';
				return (sMarkup);
			};
		});
	};


	proto.show = function(oContext)
	{
		Tab.superclass.show.call(this, oContext);

		this.oContext = oContext;

		return true;
	};
	
	// add to the mindtouch widget namespace
	YAHOO.mindtouch.widget.FlickrTab = Tab;
})();
