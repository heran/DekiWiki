/**
 * Copyright (c) 2006-2007, Bill W. Scott
 * All rights reserved.
 *
 * This work is licensed under the Creative Commons Attribution 2.5 License. To view a copy 
 * of this license, visit http://creativecommons.org/licenses/by/2.5/ or send a letter to 
 * Creative Commons, 543 Howard Street, 5th Floor, San Francisco, California, 94105, USA.
 *
 * This work was created by Bill Scott (billwscott.com, looksgoodworkswell.com).
 * 
 * The only attribution I require is to keep this notice of copyright & license 
 * in this original source file.
 *
 * Version 0.5.0 - 06.01.2007
 *
 */
YAHOO.namespace("extension");

/**
* @class 
* The carousel class manages a content list (a set of LI elements within an UL list)  that can be displayed horizontally or vertically. The content can be scrolled back and forth  with or without animation. The content can reference static HTML content or the list items can  be created dynamically on-the-fly (with or without Ajax). The navigation and event handling  can be externalized from the class.
* @param {object|string} carouselElementID The element ID (id name or id object) of the DIV that will become a carousel
* @param {object} carouselCfg The configuration object literal containing the configuration that should be set for this module. See configuration documentation for more details.
* @constructor
*/
YAHOO.extension.Carousel = function(carouselElementID, carouselCfg) {
 		this.init(carouselElementID, carouselCfg);
	};

YAHOO.extension.Carousel.prototype = {


	/**
	 * Constant denoting that the carousel size is unbounded (no limits set on scrolling)
	 * @type number
	 */
	UNBOUNDED_SIZE: 1000000,
	
	/**
	 * Initializes the carousel object and all of its local members.
     * @param {object|string} carouselElementID The element ID (id name or id object) 
     * of the DIV that will become a carousel
     * @param {object} carouselCfg The configuration object literal containing the 
     * configuration that should be set for this module. See configuration documentation for more details.
	 */
	init: function(carouselElementID, carouselCfg) {

		var oThis = this;
		
		/**
		 * For deprecation.
		 * getItem is the replacement for getCarouselItem
		 */
		this.getCarouselItem = this.getItem;
		
		// CSS style classes
		var carouselListClass = "carousel-list";
		var carouselClipRegionClass = "carousel-clip-region";
		var carouselNextClass = "carousel-next";
		var carouselPrevClass = "carousel-prev";

 		this.carouselElemID = carouselElementID;
 		this.carouselElem = YAHOO.util.Dom.get(carouselElementID);

 		this.prevEnabled = true;
 		this.nextEnabled = true;
 		
 		// Create the config object
 		this.cfg = new YAHOO.util.Config(this);

		/**
		 * orientation property. 
		 * Either "horizontal" or "vertical". Changes carousel from a 
		 * left/right style carousel to a up/down style carousel.
		 */
		this.cfg.addProperty("orientation", { 
				value:"horizontal", 
				handler: function(type, args, carouselElem) {
					oThis.orientation = args[0];
					oThis.reload();
				},
				validator: function(orientation) {
				    if(typeof orientation == "string") {
				        return ("horizontal,vertical".indexOf(orientation.toLowerCase()) != -1);
				    } else {
						return false;
					}
				}
		} );		

		/**
		 * size property. 
		 * The upper hand for scrolling in the 'next' set of content. 
		 * Set to a large value by default (this means unlimited scrolling.) 
		 */
		this.cfg.addProperty("size", { 
				value:this.UNBOUNDED_SIZE,
				handler: function(type, args, carouselElem) {
					oThis.size = args[0];
					oThis.reload();
				},
				validator: oThis.cfg.checkNumber
		} );

		/**
		 * numVisible property. 
		 * The number of items that will be visible.
		 */
		this.cfg.addProperty("numVisible", { 
				value:3,
				handler: function(type, args, carouselElem) {
					oThis.numVisible = args[0];
					oThis.load();
				},
				validator: oThis.cfg.checkNumber
		} );

		/**
		 * firstVisible property. 
		 * Sets which item should be the first visible item in the carousel. Use to set which item will
		 * display as the first element when the carousel is first displayed. After the carousel is created,
		 * you can manipulate which item is the first visible by using the moveTo() or scrollTo() convenience
		 * methods.
		 */
		this.cfg.addProperty("firstVisible", { 
				value:1,
				handler: function(type, args, carouselElem) {
					oThis.moveTo(args[0]);
				},
				validator: oThis.cfg.checkNumber
		} );

		/**
		 * scrollInc property. 
		 * The number of items to scroll by. Think of this as the page increment.
		 */
		this.cfg.addProperty("scrollInc", { 
				value:3,
				handler: function(type, args, carouselElem) {
					oThis.scrollInc = args[0];
				},
				validator: oThis.cfg.checkNumber
		} );
		
		/**
		 * animationSpeed property. 
		 * The time (in seconds) it takes to complete the scroll animation. 
		 * If set to 0, animated transitions are turned off and the new page of content is 
		 * moved immdediately into place.
		 */
		this.cfg.addProperty("animationSpeed", { 
				value:0.25,
				handler: function(type, args, carouselElem) {
					oThis.animationSpeed = args[0];
				},
				validator: oThis.cfg.checkNumber
		} );

		/**
		 * animationMethod property. 
		 * The <a href="http://developer.yahoo.com/yui/docs/animation/YAHOO.util.Easing.html">YAHOO.util.Easing</a> 
		 * method.
		 */
		this.cfg.addProperty("animationMethod", { 
				value:  YAHOO.util.Easing.easeOut,
				handler: function(type, args, carouselElem) {
					oThis.animationMethod = args[0];
				}
		} );
		
		/**
		 * animationCompleteHandler property. 
		 * JavaScript function that is called when the Carousel finishes animation 
		 * after a next or previous nagivation. 
		 * Only invoked if animationSpeed > 0. 
		 * Two parameters are passed: type (set to 'onAnimationComplete') and 
		 * args array (args[0] = direction [either: 'next' or 'previous']).
		 */
		this.cfg.addProperty("animationCompleteHandler", { 
				value:null,
				handler: function(type, args, carouselElem) {
					if(oThis.animationCompleteEvt) {
						oThis.animationCompleteEvt.unsubscribe(oThis.animationCompleteHandler, oThis);
					}
					oThis.animationCompleteHandler = args[0];
					if(oThis._isValidObj(oThis.animationCompleteHandler)) {
						oThis.animationCompleteEvt = new YAHOO.util.CustomEvent("onAnimationComplete", oThis);
						oThis.animationCompleteEvt.subscribe(oThis.animationCompleteHandler, oThis);
					}
				}
		} );
		
		/**
		 * autoPlay property. 
		 * Specifies how many milliseconds to periodically auto scroll the content. 
		 * If set to 0 (default) then autoPlay is turned off. 
		 * If the user interacts by clicking left or right navigation, autoPlay is turned off. 
		 * You can restart autoPlay by calling the <em>startAutoPlay()</em>. 
		 * If you externally control navigation (with your own event handlers) 
		 * then you may want to turn off the autoPlay by calling<em>stopAutoPlay()</em>
		 */
		this.cfg.addProperty("autoPlay", { 
				value:0,
				handler: function(type, args, carouselElem) {
					oThis.autoPlay = args[0];
					if(oThis.autoPlay > 0)
						oThis.startAutoPlay();
					else
						oThis.stopAutoPlay();
				}
		} );
		
		/**
		 * wrap property. 
		 * Specifies whether to wrap when at the end of scrolled content. When the end is reached,
		 * the carousel will scroll backwards to the item 1 (the animationSpeed parameter is used to 
		 * determine how quickly it should animate back to the start.)
		 * Ignored if the <em>size</em> attribute is not explicitly set 
		 * (i.e., value equals YAHOO.extension.Carousel.UNBOUNDED_SIZE)
		 */
		this.cfg.addProperty("wrap", { 
				value:false,
				handler: function(type, args, carouselElem) {
					oThis.wrap = args[0];
				},
				validator: oThis.cfg.checkBoolean
		} );
		
		/**
		 * navMargin property. 
		 * The margin space for the navigation controls. This is only useful for horizontal carousels 
		 * in which you have embedded navigation controls. 
		 * The <em>navMargin</em> allocates space between the left and right margins 
		 * (each navMargin wide) giving space for the navigation controls.
		 */
		this.cfg.addProperty("navMargin", { 
				value:0,
				handler: function(type, args, carouselElem) {
					oThis.navMargin = args[0];
				},
				validator: oThis.cfg.checkNumber
		} );
		
		// For backward compatibility. Deprecated.
		this.cfg.addProperty("prevElementID", { 
			value: null,
			handler: function(type, args, carouselElem) {
				if(oThis.carouselPrev) {
					YAHOO.util.Event.removeListener(oThis.carouselPrev, "click", oThis._scrollPrev);
				} 
				oThis.prevElementID = args[0];
				if(oThis.prevElementID == null) {
					oThis.carouselPrev = YAHOO.util.Dom.getElementsByClassName(carouselPrevClass, 
														"div", oThis.carouselElem)[0];
				} else {
					oThis.carouselPrev = YAHOO.util.Dom.get(oThis.prevElementID);
				}
				YAHOO.util.Event.addListener(oThis.carouselPrev, "click", oThis._scrollPrev, oThis);
			}
		});
		
		/**
		 * prevElement property. 
		 * An element or elements that will provide the previous navigation control.
		 * prevElement may be a single element or an array of elements. The values may be strings denoting
		 * the ID of the element or the object itself.
		 * If supplied, then events are wired to this control to fire scroll events to move the carousel to
		 * the previous content. 
		 * You may want to provide your own interaction for controlling the carousel. If
		 * so leave this unset and provide your own event handling mechanism.
		 */
		this.cfg.addProperty("prevElement", { 
				value:null,
				handler: function(type, args, carouselElem) {
					if(oThis.carouselPrev) {
						YAHOO.util.Event.removeListener(oThis.carouselPrev, "click", oThis._scrollPrev);
					} 
					oThis.prevElementID = args[0];
					if(oThis.prevElementID == null) {
						oThis.carouselPrev = YAHOO.util.Dom.getElementsByClassName(carouselPrevClass, 
															"div", oThis.carouselElem)[0];
					} else {
						oThis.carouselPrev = YAHOO.util.Dom.get(oThis.prevElementID);
					}
					YAHOO.util.Event.addListener(oThis.carouselPrev, "click", oThis._scrollPrev, oThis);
				}
		} );
		
		// For backward compatibility. Deprecated.
		this.cfg.addProperty("nextElementID", { 
			value: null,
			handler: function(type, args, carouselElem) {
				if(oThis.carouselNext) {
					YAHOO.util.Event.removeListener(oThis.carouselNext, "click", oThis._scrollNext);
				} 
				oThis.nextElementID = args[0];
				if(oThis.nextElementID == null) {
					oThis.carouselNext = YAHOO.util.Dom.getElementsByClassName(carouselNextClass, 
														"div", oThis.carouselElem);
				} else {
					oThis.carouselNext = YAHOO.util.Dom.get(oThis.nextElementID);
				}
				if(oThis.carouselNext) {
					YAHOO.util.Event.addListener(oThis.carouselNext, "click", oThis._scrollNext, oThis);
				} 
			}
		});
		
		/**
		 * nextElement property. 
		 * An element or elements that will provide the next navigation control.
		 * nextElement may be a single element or an array of elements. The values may be strings denoting
		 * the ID of the element or the object itself.
		 * If supplied, then events are wired to this control to fire scroll events to move the carousel to
		 * the next content. 
		 * You may want to provide your own interaction for controlling the carousel. If
		 * so leave this unset and provide your own event handling mechanism.
		 */
		this.cfg.addProperty("nextElement", { 
				value:null,
				handler: function(type, args, carouselElem) {
					if(oThis.carouselNext) {
						YAHOO.util.Event.removeListener(oThis.carouselNext, "click", oThis._scrollNext);
					} 
					oThis.nextElementID = args[0];
					if(oThis.nextElementID == null) {
						oThis.carouselNext = YAHOO.util.Dom.getElementsByClassName(carouselNextClass, 
															"div", oThis.carouselElem);
					} else {
						oThis.carouselNext = YAHOO.util.Dom.get(oThis.nextElementID);
					}
					if(oThis.carouselNext) {
						YAHOO.util.Event.addListener(oThis.carouselNext, "click", oThis._scrollNext, oThis);
					} 
				}
		} );
		
		/**
		 * loadInitHandler property. 
		 * JavaScript function that is called when the Carousel needs to load 
		 * the initial set of visible items. Two parameters are passed: 
		 * type (set to 'onLoadInit') and an argument array (args[0] = start index, args[1] = last index).
		 */
		this.cfg.addProperty("loadInitHandler", { 
				value:null,
				handler: function(type, args, carouselElem) {
					if(oThis.loadInitHandlerEvt) {
						oThis.loadInitHandlerEvt.unsubscribe(oThis.loadInitHandler, oThis);
					}
					oThis.loadInitHandler = args[0];
					if(oThis.loadInitHandlerEvt) {
						oThis.loadInitHandlerEvt = new YAHOO.util.CustomEvent("onLoadInit", oThis);
						oThis.loadInitHandlerEvt.subscribe(oThis.loadInitHandler, oThis);
					}
				}
		} );
		
		/**
		 * loadNextHandler property. 
		 * JavaScript function that is called when the Carousel needs to load 
		 * the next set of items (in response to the user navigating to the next set.) 
		 * Two parameters are passed: type (set to 'onLoadNext') and 
		 * args array (args[0] = start index, args[1] = last index).
		 */
		this.cfg.addProperty("loadNextHandler", { 
				value:null,
				handler: function(type, args, carouselElem) {
					if(oThis.loadNextHandlerEvt) {
						oThis.loadNextHandlerEvt.unsubscribe(oThis.loadNextHandler, oThis);
					}
					oThis.loadNextHandler = args[0];
					if(oThis.loadNextHandlerEvt) {
						oThis.loadNextHandlerEvt = new YAHOO.util.CustomEvent("onLoadNext", oThis);
						oThis.loadNextHandlerEvt.subscribe(oThis.loadNextHandler, oThis);
					}
				}
		} );
				
		/**
		 * loadPrevHandler property. 
		 * JavaScript function that is called when the Carousel needs to load 
		 * the previous set of items (in response to the user navigating to the previous set.) 
		 * Two parameters are passed: type (set to 'onLoadPrev') and args array 
		 * (args[0] = start index, args[1] = last index).
		 */
		this.cfg.addProperty("loadPrevHandler", { 
				value:null,
				handler: function(type, args, carouselElem) {
					if(oThis.loadPrevHandlerEvt) {
						oThis.loadPrevHandlerEvt.unsubscribe(oThis.loadPrevHandler, oThis);
					}
					oThis.loadPrevHandler = args[0];
					if(oThis.loadPrevHandlerEvt) {
						oThis.loadPrevHandlerEvt = new YAHOO.util.CustomEvent("onLoadPrev", oThis);
						oThis.loadPrevHandlerEvt.subscribe(oThis.loadPrevHandler, oThis);
					}
				}
		} );
		
		/**
		 * prevButtonStateHandler property. 
		 * JavaScript function that is called when the enabled state of the 
		 * 'previous' control is changing. The responsibility of 
		 * this method is to enable or disable the 'previous' control. 
		 * Two parameters are passed to this method: <em>type</em> 
		 * (which is set to "onPrevButtonStateChange") and <em>args</em>, 
		 * an array that contains two values. 
		 * The parameter args[0] is a flag denoting whether the 'previous' control 
		 * is being enabled or disabled. The parameter args[1] is the element object 
		 * derived from the <em>prevElement</em> parameter.
		 * If you do not supply a prevElement then you will need to track
		 * the elements that you would want to enable/disable while handling the state change.
		 */
		this.cfg.addProperty("prevButtonStateHandler", { 
				value:null,
				handler: function(type, args, carouselElem) {
					if(oThis.prevButtonStateHandler) {
						oThis.prevButtonStateHandlerEvt.unsubscribe(oThis.prevButtonStateHandler, oThis);
					}
					oThis.prevButtonStateHandler = args[0];
					if(oThis.prevButtonStateHandler) {
						oThis.prevButtonStateHandlerEvt = new YAHOO.util.CustomEvent("onPrevButtonStateChange", oThis);
						oThis.prevButtonStateHandlerEvt.subscribe(oThis.prevButtonStateHandler, oThis);
					}
				}
		} );
		
		/**
		 * nextButtonStateHandler property. 
		 * JavaScript function that is called when the enabled state of the 
		 * 'next' control is changing. The responsibility of 
		 * this method is to enable or disable the 'next' control. 
		 * Two parameters are passed to this method: <em>type</em> 
		 * (which is set to "onNextButtonStateChange") and <em>args</em>, 
		 * an array that contains two values. 
		 * The parameter args[0] is a flag denoting whether the 'next' control 
		 * is being enabled or disabled. The parameter args[1] is the element object 
		 * derived from the <em>nextElement</em> parameter.
		 * If you do not supply a nextElement then you will need to track
		 * the elements that you would want to enable/disable while handling the state change.
		 */
		this.cfg.addProperty("nextButtonStateHandler", { 
				value:null,
				handler: function(type, args, carouselElem) {
					if(oThis.nextButtonStateHandler) {
						oThis.nextButtonStateHandlerEvt.unsubscribe(oThis.nextButtonStateHandler, oThis);
					}
					oThis.nextButtonStateHandler = args[0];
					if(oThis.nextButtonStateHandler) {
						oThis.nextButtonStateHandlerEvt = new YAHOO.util.CustomEvent("onNextButtonStateChange", oThis);
						oThis.nextButtonStateHandlerEvt.subscribe(oThis.nextButtonStateHandler, oThis);
					}
				}
		} );
		
		
 		if(carouselCfg) {
 			this.cfg.applyConfig(carouselCfg);
 		}
 		
		// this.itemWidth = this.cfg.getProperty("itemWidth");
		// this.itemHeight = this.cfg.getProperty("itemHeight");
		
 		this.scrollInc = this.cfg.getProperty("scrollInc");
		this.navMargin = this.cfg.getProperty("navMargin");
		this.loadInitHandler = this.cfg.getProperty("loadInitHandler");
		this.loadNextHandler = this.cfg.getProperty("loadNextHandler");
		this.loadPrevHandler = this.cfg.getProperty("loadPrevHandler");
		this.prevButtonStateHandler = this.cfg.getProperty("prevButtonStateHandler");
		this.nextButtonStateHandler = this.cfg.getProperty("nextButtonStateHandler");
		this.animationCompleteHandler = this.cfg.getProperty("animationCompleteHandler");
		this.size = this.cfg.getProperty("size");
		this.wrap = this.cfg.getProperty("wrap");
		this.animationMethod = this.cfg.getProperty("animationMethod");
		this.orientation = this.cfg.getProperty("orientation");
		this.nextElementID = this.cfg.getProperty("nextElementID");
		if(!this.nextElementID) 
			this.nextElementID = this.cfg.getProperty("nextElement");
		
		this.prevElementID = this.cfg.getProperty("prevElementID");
		if(!this.prevElementID) 
			this.prevElementID = this.cfg.getProperty("prevElement");

		this.autoPlay = this.cfg.getProperty("autoPlay");
		this.autoPlayTimer = null;
		this.numVisible = this.cfg.getProperty("numVisible");
		this.firstVisible = this.cfg.getProperty("firstVisible");
		this.lastVisible = this.firstVisible;
		this.lastPrebuiltIdx = 0;
		this.currSize = 0;
		 		
 		// prefetch elements
 		this.carouselList = YAHOO.util.Dom.getElementsByClassName(carouselListClass, 
												"ul", this.carouselElem)[0];
							
		if(this.nextElementID == null) {
			this.carouselNext = YAHOO.util.Dom.getElementsByClassName(carouselNextClass, 
												"div", this.carouselElem)[0];
		} else {
			this.carouselNext = YAHOO.util.Dom.get(this.nextElementID);
		}

		if(this.prevElementID == null) {
 			this.carouselPrev = YAHOO.util.Dom.getElementsByClassName(carouselPrevClass, 
												"div", this.carouselElem)[0];
		} else {
			this.carouselPrev = YAHOO.util.Dom.get(this.prevElementID);
		}
		
		this.clipReg = YAHOO.util.Dom.getElementsByClassName(carouselClipRegionClass, 
												"div", this.carouselElem)[0];
												
		// add a style class dynamically so that the correct styles get applied for a vertical carousel
		if(this.isVertical()) {
			YAHOO.util.Dom.addClass(this.carouselList, "carousel-vertical");
		}
		
		// initialize the animation objects for next/previous
 		this.scrollNextAnim = new YAHOO.util.Motion(this.carouselList, this.scrollNextParams, 
   								this.cfg.getProperty("animationSpeed"), this.animationMethod);
 		this.scrollPrevAnim = new YAHOO.util.Motion(this.carouselList, this.scrollPrevParams, 
   								this.cfg.getProperty("animationSpeed"), this.animationMethod);
		
		// If they supplied a nextElementID then wire an event listener for the click
		if(this.carouselNext) {
			YAHOO.util.Event.addListener(this.carouselNext, "click", this._scrollNext, this);
		} 
		
		// If they supplied a prevElementID then wire an event listener for the click
		if(this.carouselPrev) {
			YAHOO.util.Event.addListener(this.carouselPrev, "click", this._scrollPrev, this);
		}
				
		// Wire up the various event handlers that they might have supplied
		if(this.loadInitHandler) {
			this.loadInitHandlerEvt = new YAHOO.util.CustomEvent("onLoadInit", this);
			this.loadInitHandlerEvt.subscribe(this.loadInitHandler, this);
		}
		if(this.loadNextHandler) {
			this.loadNextHandlerEvt = new YAHOO.util.CustomEvent("onLoadNext", this);
			this.loadNextHandlerEvt.subscribe(this.loadNextHandler, this);
		}
		if(this.loadPrevHandler) {
			this.loadPrevHandlerEvt = new YAHOO.util.CustomEvent("onLoadPrev", this);
			this.loadPrevHandlerEvt.subscribe(this.loadPrevHandler, this);
		}
		if(this.animationCompleteHandler) {
			this.animationCompleteEvt = new YAHOO.util.CustomEvent("onAnimationComplete", this);
			this.animationCompleteEvt.subscribe(this.animationCompleteHandler, this);
		}
		if(this.prevButtonStateHandler) {
			this.prevButtonStateHandlerEvt = new YAHOO.util.CustomEvent("onPrevButtonStateChange", 
							this);
			this.prevButtonStateHandlerEvt.subscribe(this.prevButtonStateHandler, this);
		}
		if(this.nextButtonStateHandler) {
			this.nextButtonStateHandlerEvt = new YAHOO.util.CustomEvent("onNextButtonStateChange", this);
			this.nextButtonStateHandlerEvt.subscribe(this.nextButtonStateHandler, this);
		}
		
		// Since loading may take some time, wire up a listener to fire when at least the first
		// element actually gets loaded
  		YAHOO.util.Event.onAvailable(this.carouselElemID + "-item-1", this._calculateSize, this);
  		
  		// Call the initial loading sequence
		this._loadInitial();	

	},
	
	// /////////////////// Public API //////////////////////////////////////////

	/**
	 * Clears all items from the list and resets to the carousel to its original initial state.
	 */
	clear: function() {
		this.moveTo(1);
		this._removeChildrenFromNode(this.carouselList);
		this.stopAutoPlay();
		this.firstVisible = 1;
		this.lastVisible = 1;
		this.lastPrebuiltIdx = 0;
		this.currSize = 0;
		this.size = this.cfg.getProperty("size");
	},
	
	/**
	 * Clears all items from the list and calls the loadInitHandler to load new items into the list. 
	 * The carousel size is reset to the original size set during creation.
	 * @param {number}	numVisible	Optional parameter: numVisible. 
	 * If set, the carousel will resize on the reload to show numVisible items.
	 */
	reload: function(numVisible) {
		// this should be deprecated, not needed since can be set via property change
	    if(this._isValidObj(numVisible)) {
	    	this.numVisible = numVisible;
	    }
		this.clear();
		YAHOO.util.Event.onAvailable(this.carouselElemID + "-item-1", this._calculateSize, this);  		
		this._loadInitial();
	},

	load: function() {
		YAHOO.util.Event.onAvailable(this.carouselElemID + "-item-1", this._calculateSize, this);  		
		this._loadInitial();
	},
	
	/**
	 * Clears all items from the list and calls the loadInitHandler to load new items into the list. 
	 * The carousel size is reset to the original size set during creation.
	 * With patch from Dan Hobbs for handling unordered loading.
	 * @param {number}	idx	which item in the list to potentially create. 
	 * If item already exists it will not create a new item.
	 * @param {string}	innerHTML	The innerHTML string to use to create the contents of an LI element.
	 */
	addItem: function(idx, innerHTMLOrElem) {
		
        var liElem = this.getItem(idx);

		// Need to create the li
		if(!this._isValidObj(liElem)) {
			liElem = this._createItem(idx, innerHTMLOrElem);
			this.carouselList.appendChild(liElem);
			
		} else if(this._isValidObj(liElem.placeholder)) {		
	    	var newLiElem = this._createItem(idx, innerHTMLOrElem);
			this.carouselList.replaceChild(newLiElem, liElem);
			liElem = newLiElem;
		}
		
		/**
		 * Not real comfortable with this line of code. It exists for vertical
		 * carousels for IE6. For some reason LI elements are not displaying
		 * unless you after the fact set the display to block. (Even though
	     * the CSS sets vertical LIs to display:block)
	     */
		if(this.isVertical())
			setTimeout( function() { liElem.style.display="block"; }, 1 );		
				
		return liElem;

	},

	/**
	 * Inserts a new LI item before the index specified. Uses the innerHTML to create the contents of the new LI item
	 * @param {number}	refIdx	which item in the list to insert this item before. 
	 * @param {string}	innerHTML	The innerHTML string to use to create the contents of an LI element.
	 */
	insertBefore: function(refIdx, innerHTML) {
		if(refIdx < 1) {
			refIdx = 1;
		}
		
		var insertionIdx = refIdx - 1;
		
		if(insertionIdx > this.lastPrebuiltIdx) {
			this._prebuildItems(this.lastPrebuiltIdx, refIdx); // is this right?
		}
		
		var liElem = this._insertBeforeItem(refIdx, innerHTML);
		
		// depends on recalculation of this.size above
		if(this.firstVisible > insertionIdx || this.lastVisible < this.size) {
			if(this.nextEnabled === false) {
				this._enableNext();
			}
		}

		return liElem;
	},
	
	/**
	 * Inserts a new LI item after the index specified. Uses the innerHTML to create the contents of the new LI item
	 * @param {number}	refIdx	which item in the list to insert this item after. 
	 * @param {string}	innerHTML	The innerHTML string to use to create the contents of an LI element.
	 */
	insertAfter: function(refIdx, innerHTML) {
	
		if(refIdx > this.size) {
			refIdx = this.size;
		}
		
		var insertionIdx = refIdx + 1;			
		
		// if we are inserting this item past where we have prebuilt items, then
		// prebuild up to this point.
		if(insertionIdx > this.lastPrebuiltIdx) {
			this._prebuildItems(this.lastPrebuiltIdx, insertionIdx+1);
		}

		var liElem = this._insertAfterItem(refIdx, innerHTML);		

		if(insertionIdx > this.size) {
			this.size = insertionIdx;
			if(this.nextEnabled === false) {
				this._enableNext();
			}
		}
		
		// depends on recalculation of this.size above
		if(this.firstVisible > insertionIdx || this.lastVisible < this.size) {
			if(this.nextEnabled === false) {
				this._enableNext();
			}
		}

		return liElem;
	},	

	/**
	 * Simulates a next button event. Causes the carousel to scroll the next set of content into view.
	 */
	scrollNext: function() {
		this._scrollNext(null, this);
		
		// we know the timer has expired.
		//if(this.autoPlayTimer) clearTimeout(this.autoPlayTimer);
		this.autoPlayTimer = null;
		if(this.autoPlay !== 0) {
			this.autoPlayTimer = this.startAutoPlay();
		}
	},
	
	/**
	 * Simulates a prev button event. Causes the carousel to scroll the previous set of content into view.
	 */
	scrollPrev: function() {
		this._scrollPrev(null, this);
	},
	
	/**
	 * Scrolls the content to place itemNum as the start item in the view 
	 * (if size is specified, the last element will not scroll past the end.). 
	 * Uses current animation speed & method.
	 * @param {number}	newStart	The item to scroll to. 
	 */
	scrollTo: function(newStart) {
		this._position(newStart, true);
	},

	/**
	 * Moves the content to place itemNum as the start item in the view 
	 * (if size is specified, the last element will not scroll past the end.) 
	 * Ignores animation speed & method; moves directly to the item. 
	 * Note that you can also set the <em>firstVisible</em> property upon initialization 
	 * to get the carousel to start at a position different than 1.	
	 * @param {number}	newStart	The item to move directly to. 
	 */
	moveTo: function(newStart) {
		this._position(newStart, false);
	},

	/**
	 * Starts up autoplay. If autoPlay has been stopped (by calling stopAutoPlay or by user interaction), 
	 * you can start it back up by using this method.
	 * @param {number}	interval	optional parameter that sets the interval 
	 * for auto play the next time that autoplay fires. 
	 */
	startAutoPlay: function(interval) {
		// if interval is passed as arg, then set autoPlay to this interval.
		if(this._isValidObj(interval)) {
			this.autoPlay = interval;
		}
		
		// if we already are playing, then do nothing.
		if(this.autoPlayTimer !== null) {
			return this.autoPlayTimer;
		}
				
		var oThis = this;  
		var autoScroll = function() { oThis.scrollNext(); };
		this.autoPlayTimer = setTimeout( autoScroll, this.autoPlay );
		
		return this.autoPlayTimer;
	},

	/**
	 * Stops autoplay. Useful for when you want to control what events will stop the autoplay feature. 
	 * Call <em>startAutoPlay()</em> to restart autoplay.
	 */
	stopAutoPlay: function() {
		if (this.autoPlayTimer !== null) {
			clearTimeout(this.autoPlayTimer);
			this.autoPlayTimer = null;
		}
	},
	
	/**
	 * Returns whether the carousel's orientation is set to vertical.
	 */
	isVertical: function() {
		return (this.orientation != "horizontal");
	},
	
	
	/**
	 * Check to see if an element (by index) has been loaded or not. If the item is simply pre-built, but not
	 * loaded this will return false. If the item has not been pre-built it will also return false.
	 * @param {number}	idx	Index of the element to check load status for. 
	 */
	isItemLoaded: function(idx) {
		var liElem = this.getItem(idx);
		
		// if item exists and is not a placeholder, then it is already loaded.
		if(this._isValidObj(liElem) && !this._isValidObj(liElem.placeholder)) {
			return true;
		}
		
		return false;
	},
	
	/**
	 * Lookup the element object for a carousel list item by index.
	 * @param {number}	idx	Index of the element to lookup. 
	 */
	getItem: function(idx) {
		var elemName = this.carouselElemID + "-item-" + idx;
 		var liElem = YAHOO.util.Dom.get(elemName);
		return liElem;	
	},
	
	show: function() {
		YAHOO.util.Dom.setStyle(this.carouselElem, "display", "block");
		this.calculateSize();
	},
	
	hide: function() {
		YAHOO.util.Dom.setStyle(this.carouselElem, "display", "none");
	},

	calculateSize: function() {
 		var ulKids = this.carouselList.childNodes;
 		var li = null;
		for(var i=0; i<ulKids.length; i++) {
		
			li = ulKids[i];
			if(li.tagName == "LI" || li.tagName == "li") {
				break;
			}
		}
		
		var pl = this._getStyleVal(li, "paddingLeft");
		var pr = this._getStyleVal(li, "paddingRight");
		var ml = this._getStyleVal(li, "marginLeft");
		var mr = this._getStyleVal(li, "marginRight");
		var liPaddingWidth = pl + pr + ml + mr;

		YAHOO.util.Dom.removeClass(this.carouselList, "carousel-vertical");
		YAHOO.util.Dom.removeClass(this.carouselList, "carousel-horizontal");
		if(this.isVertical()) {
			YAHOO.util.Dom.addClass(this.carouselList, "carousel-vertical");
			var pt = this._getStyleVal(li, "paddingTop");
			var pb = this._getStyleVal(li, "paddingBottom");
			var mt = this._getStyleVal(li, "marginTop");
			var mb = this._getStyleVal(li, "marginBottom");
			var liPaddingHeight = pt + pb + mt + mb;
			var ulPaddingHeight = this._getStyleVal(this.carouselList, "paddingTop") +
			  					this._getStyleVal(this.carouselList, "paddingBottom") +
			  					this._getStyleVal(this.carouselList, "marginTop") +
			  					this._getStyleVal(this.carouselList, "marginBottom");
			// get the height from the height computed style not the offset height
			// The reason is that on IE the offsetHeight when some part of the margin is
			// explicitly set to 'auto' can cause accessing that value to crash AND
			// on FF, in certain cases the actual value used for the LI's height is fractional
			// For example, while li.offsetHeight might return 93, YAHOO.util.Dom.getStyle(li, "height") 
			// would return "93.2px". This fractional value will affect the scrolling, so it must be
			// factored in for FF.
			// The caveat is that for IE, you will need to set the LI's height explicitly
			// REPLACED: this.scrollAmountPerInc = (li.offsetHeight + liPaddingHeight);
			// WITH:
			var liHeight = this._getStyleVal(li, "height", true);
			this.scrollAmountPerInc = (liHeight + liPaddingHeight);
			
			var liWidth = this._getStyleVal(li, "width");
			this.clipReg.style.width = (liWidth + liPaddingWidth) + "px";
			this.clipReg.style.height = (this.scrollAmountPerInc * this.numVisible + ulPaddingHeight) + "px";
			this.carouselElem.style.width = (liWidth + liPaddingWidth) + "px";			

			// if we set the initial start > 1 then this will adjust the scrolled location
			var currY = YAHOO.util.Dom.getY(this.carouselList);	
			YAHOO.util.Dom.setY(this.carouselList, currY - this.scrollAmountPerInc*(this.firstVisible-1));

		} else {
			YAHOO.util.Dom.addClass(this.carouselList, "carousel-horizontal");

			var liWidth = li.offsetWidth; 
			this.scrollAmountPerInc = (liWidth + liPaddingWidth);
			this.carouselElem.style.width = ((this.scrollAmountPerInc*this.numVisible)+this.navMargin*2) + "px";
			this.clipReg.style.width = (this.scrollAmountPerInc*this.numVisible)+"px";

			// if we set the initial start > 1 then this will adjust the scrolled location
			var currX = YAHOO.util.Dom.getX(this.carouselList);
			YAHOO.util.Dom.setX(this.carouselList, currX - this.scrollAmountPerInc*(this.firstVisible-1));
		}
				
	},
	
	// /////////////////// PRIVATE API //////////////////////////////////////////
	_getStyleVal : function(li, style, returnFloat) {
		var styleValStr = YAHOO.util.Dom.getStyle(li, style);
		
		var styleVal = returnFloat ? parseFloat(styleValStr) : parseInt(styleValStr, 10);
		if(style=="height" && isNaN(styleVal)) {
			styleVal = li.offsetHeight;
			//console.log("height && NaN: " + styleVal);
		} else if(isNaN(styleVal)) {
			styleVal = 0;
		}
		return styleVal;
	},
	
	_calculateSize: function(me) {
		me.calculateSize();
		YAHOO.util.Dom.setStyle(me.carouselElem, "visibility", "visible");
	},

	// From Mike Chambers: http://weblogs.macromedia.com/mesh/archives/2006/01/removing_html_e.html
	_removeChildrenFromNode: function(node)
	{
		if(!this._isValidObj(node))
		{
      		return;
		}
   
		var len = node.childNodes.length;
   
		while (node.hasChildNodes())
		{
			node.removeChild(node.firstChild);
		}
	},
	
	_prebuildLiElem: function(idx) {
		var liElem = document.createElement("li");
		liElem.id = this.carouselElemID + "-item-" + idx;
		// this is default flag to know that we're not really loaded yet.
		liElem.placeholder = true;   
		this.carouselList.appendChild(liElem);
		
		this.lastPrebuiltIdx = (idx > this.lastPrebuiltIdx) ? idx : this.lastPrebuiltIdx;
	},
	
	_createItem: function(idx, innerHTMLOrElem) {
		var liElem = document.createElement("li");
		liElem.id = this.carouselElemID + "-item-" + idx;

		// if String then assume innerHTML, else an elem object
		if(typeof(innerHTMLOrElem) === "string") {
			liElem.innerHTML = innerHTMLOrElem;
		} else {
			liElem.appendChild(innerHTMLOrElem);
		}
		
		return liElem;
	},
	
	// idx is the location to insert after
	_insertAfterItem: function(refIdx, innerHTMLOrElem) {
		return this._insertBeforeItem(refIdx+1, innerHTMLOrElem);
	},
	
	
	_insertBeforeItem: function(refIdx, innerHTMLOrElem) {

		var refItem = this.getItem(refIdx);
		
		if(this.size != this.UNBOUNDED_SIZE) {
			this.size += 1;
		}
				
		for(var i=this.lastPrebuiltIdx; i>=refIdx; i--) {
			var anItem = this.getItem(i);
			if(this._isValidObj(anItem)) {
				anItem.id = this.carouselElemID + "-item-" + (i+1);
			}
		}

		var liElem = this._createItem(refIdx, innerHTMLOrElem);
		
		var insertedItem = this.carouselList.insertBefore(liElem, refItem);
		this.lastPrebuiltIdx += 1;
		
		return liElem;
	},
	
	// TEST THIS... think it has to do with prebuild
	insertAfterEnd: function(innerHTMLOrElem) {
		return this.insertAfter(this.size, innerHTMLOrElem);
	},
		
	_position: function(newStart, showAnimation) {
		// do we bypass the isAnimated check?
		if(newStart > this.firstVisible) {
			var inc = newStart - this.firstVisible;
			this._scrollNextInc(this, inc, showAnimation);
		} else {
			var dec = this.firstVisible - newStart;
			this._scrollPrevInc(this, dec, showAnimation);
		}
	},
	
	
	// event handler
	_scrollNext: function(e, carousel) {
		if(carousel.scrollNextAnim.isAnimated()) {
			return false; // might be better to set ourself waiting for animation completion and
			// then just do this function. that will allow faster scroll responses.
		}

		// if fired by an event and wrap is set and we are already at end then wrap
		var currEnd = carousel.firstVisible + carousel.numVisible-1;
		if(carousel.wrap && currEnd == carousel.size) {
			carousel.scrollTo(1);
		} else if(e !== null) { // event fired this so disable autoplay
			carousel.stopAutoPlay();
			carousel._scrollNextInc(carousel, carousel.scrollInc, (carousel.cfg.getProperty("animationSpeed") !== 0));
		} else {
			carousel._scrollNextInc(carousel, carousel.scrollInc, (carousel.cfg.getProperty("animationSpeed") !== 0));
		}


	},
	
	// probably no longer need carousel passed in, this should be correct now.
	_scrollNextInc: function(carousel, inc, showAnimation) {

		var currFirstVisible = carousel.firstVisible;
		
		var newEnd = carousel.firstVisible + inc + carousel.numVisible - 1;
		newEnd = (newEnd > carousel.size) ? carousel.size : newEnd;
		var newStart = newEnd - carousel.numVisible + 1;
		inc = newStart - carousel.firstVisible;
		carousel.firstVisible = newStart;

		// if the prev button is disabled and start is now past 1, then enable it
		if((carousel.prevEnabled === false) && (carousel.firstVisible > 1)) {
			carousel._enablePrev();
		}
		// if next is enabled && we are now at the end, then disable
		if((carousel.nextEnabled === true) && (newEnd == carousel.size)) {
			carousel._disableNext();
		}
		
		if(inc > 0) {
			if(carousel._isValidObj(carousel.loadNextHandler)) {
				carousel.lastVisible = carousel.firstVisible + carousel.numVisible - 1;
				
				carousel.currSize = (carousel.lastVisible > carousel.currSize) ?
											carousel.lastVisible : carousel.currSize;
											
				var alreadyCached = carousel._areAllItemsLoaded(currFirstVisible, 
										carousel.lastVisible);
				carousel.loadNextHandlerEvt.fire(carousel.firstVisible, carousel.lastVisible, alreadyCached);
			}
			
			if(showAnimation) {
	 			var nextParams = { points: { by: [-carousel.scrollAmountPerInc*inc, 0] } };
	 			if(carousel.isVertical()) {
	 				nextParams = { points: { by: [0, -carousel.scrollAmountPerInc*inc] } };
	 			}
 		
	 			carousel.scrollNextAnim = new YAHOO.util.Motion(carousel.carouselList, 
	 							nextParams, 
   								carousel.cfg.getProperty("animationSpeed"), carousel.animationMethod);
				if(carousel._isValidObj(carousel.animationCompleteHandler)) {
					carousel.scrollNextAnim.onComplete.subscribe(this._handleAnimationComplete, [carousel, "next"]);
				}
				carousel.scrollNextAnim.animate();
			} else {
				if(carousel.isVertical()) {
					var currY = YAHOO.util.Dom.getY(carousel.carouselList);
										
					YAHOO.util.Dom.setY(carousel.carouselList, 
								currY - carousel.scrollAmountPerInc*inc);
				} else {
					var currX = YAHOO.util.Dom.getX(carousel.carouselList);
					YAHOO.util.Dom.setX(carousel.carouselList, 
								currX - carousel.scrollAmountPerInc*inc);
				}
			}
			
		}
		
		return false;
	},
	
	_handleAnimationComplete: function(type, args, argList) {
		var carousel = argList[0];
		var direction = argList[1];
		
		carousel.animationCompleteEvt.fire(direction);

		
	},
	
	// If EVERY item is already loaded in the range then return true
	// Also prebuild whatever is not already created.
	_areAllItemsLoaded: function(first, last) {
		var itemsLoaded = true;
		for(var i=first; i<=last; i++) {
			var liElem = this.getItem(i);
			
			// If the li elem does not exist, then prebuild it in the correct order
			// but still flag as not loaded (just prebuilt the li item.
			if(!this._isValidObj(liElem)) {
				this._prebuildLiElem(i);
				itemsLoaded = false;
			// but if the item exists and is a placeholder, then
			// note that this item is not loaded (only a placeholder)
			} else if(this._isValidObj(liElem.placeholder)) {
				itemsLoaded = false;
			}
		}
		return itemsLoaded;
	}, 
	
	_prebuildItems: function(first, last) {
		for(var i=first; i<=last; i++) {
			var liElem = this.getItem(i);
			
			// If the li elem does not exist, then prebuild it in the correct order
			// but still flag as not loaded (just prebuilt the li item.
			if(!this._isValidObj(liElem)) {
				this._prebuildLiElem(i);
			}
		}
	}, 

	_scrollPrev: function(e, carousel) {
		if(carousel.scrollPrevAnim.isAnimated()) {
			return false;
		}
		carousel._scrollPrevInc(carousel, carousel.scrollInc, (carousel.cfg.getProperty("animationSpeed") !== 0));
	},
	
	_scrollPrevInc: function(carousel, dec, showAnimation) {

		var currLastVisible = carousel.lastVisible;
		var newStart = carousel.firstVisible - dec;
		newStart = (newStart <= 1) ? 1 : (newStart);
		var newDec = carousel.firstVisible - newStart;
		carousel.firstVisible = newStart;
		
		// if prev is enabled && we are now at position 1, then disable
		if((carousel.prevEnabled === true) && (carousel.firstVisible == 1)) {
			carousel._disablePrev();
		}
		// if the next button is disabled and end is < size, then enable it
		if((carousel.nextEnabled === false) && 
						((carousel.firstVisible + carousel.numVisible - 1) < carousel.size)) {
			carousel._enableNext();
		}

		// if we are decrementing
		if(newDec > 0) {			
			if(carousel._isValidObj(carousel.loadPrevHandler)) {
				carousel.lastVisible = carousel.firstVisible + carousel.numVisible - 1;

				carousel.currSize = (carousel.lastVisible > carousel.currSize) ?
											carousel.lastVisible : carousel.currSize;

				var alreadyCached = carousel._areAllItemsLoaded(carousel.firstVisible, 
									currLastVisible);
				carousel.loadPrevHandlerEvt.fire(carousel.firstVisible, carousel.lastVisible, alreadyCached);
			}

			if(showAnimation) {
	 			var prevParams = { points: { by: [carousel.scrollAmountPerInc*newDec, 0] } };
	 			if(carousel.isVertical()) {
	 				prevParams = { points: { by: [0, carousel.scrollAmountPerInc*newDec] } };
	 			}
 		
	 			carousel.scrollPrevAnim = new YAHOO.util.Motion(carousel.carouselList,
	 							prevParams, 
   								carousel.cfg.getProperty("animationSpeed"), carousel.animationMethod);
				if(carousel._isValidObj(carousel.animationCompleteHandler)) {
					carousel.scrollPrevAnim.onComplete.subscribe(this._handleAnimationComplete, [carousel, "prev"]);
				}
				carousel.scrollPrevAnim.animate();
			} else {
				if(carousel.isVertical()) {
					var currY = YAHOO.util.Dom.getY(carousel.carouselList);
					YAHOO.util.Dom.setY(carousel.carouselList, currY + 
							carousel.scrollAmountPerInc*newDec);				
				} else {
					var currX = YAHOO.util.Dom.getX(carousel.carouselList);
					YAHOO.util.Dom.setX(carousel.carouselList, currX + 
							carousel.scrollAmountPerInc*newDec);
				}
			}
		}
		
		return false;
	},
	
	/**
	 * _loadInitial looks at firstItemVisible for the start (not necessarily 1)
	 */
	_loadInitial: function() {
		this.lastVisible = this.firstVisible + this.numVisible - 1;

		this.currSize = (this.lastVisible > this.currSize) ?
									this.lastVisible : this.currSize;

		// Since firstItemVisible can be > 1 need to check for disabling either
		// previous or next controls
		if(this.firstVisible == 1) {
			this._disablePrev();
		}
		if(this.lastVisible == this.size) {
			this._disableNext();
		}
		
		// Load from 1 to the last visible
		// The _calculateSize method will adjust the scroll position
		// for starts > 1
		if(this._isValidObj(this.loadInitHandler)) {
			var alreadyCached = this._areAllItemsLoaded(1, this.lastVisible);
			this.loadInitHandlerEvt.fire(1, this.lastVisible, alreadyCached);
		}
		
		if(this.autoPlay !== 0) {
			this.autoPlayTimer = this.startAutoPlay();
		}		
    },
		
	_disablePrev: function() {
		this.prevEnabled = false;
		if(this._isValidObj(this.prevButtonStateHandlerEvt)) {
			this.prevButtonStateHandlerEvt.fire(false, this.carouselPrev);
		}
		if(this._isValidObj(this.carouselPrev)) {
			YAHOO.util.Event.removeListener(this.carouselPrev, "click", this._scrollPrev);
		}
	},
	
	_enablePrev: function() {
		this.prevEnabled = true;
		if(this._isValidObj(this.prevButtonStateHandlerEvt)) {
			this.prevButtonStateHandlerEvt.fire(true, this.carouselPrev);
		}
		if(this._isValidObj(this.carouselPrev)) {
			YAHOO.util.Event.addListener(this.carouselPrev, "click", this._scrollPrev, this);
		}
	},
		
	_disableNext: function() {
		if(this.wrap) {
			return;
		}
		
		this.nextEnabled = false;
		if(this._isValidObj(this.nextButtonStateHandlerEvt)) {
			this.nextButtonStateHandlerEvt.fire(false, this.carouselNext);
		}
		if(this._isValidObj(this.carouselNext)) {
			YAHOO.util.Event.removeListener(this.carouselNext, "click", this._scrollNext);
		}
	},
	
	_enableNext: function() {
		this.nextEnabled = true;
		if(this._isValidObj(this.nextButtonStateHandlerEvt)) {
			this.nextButtonStateHandlerEvt.fire(true, this.carouselNext);
		}
		if(this._isValidObj(this.carouselNext)) {
			YAHOO.util.Event.addListener(this.carouselNext, "click", this._scrollNext, this);
		}
	},
		
	_isValidObj: function(obj) {

		if (null == obj) {
			return false;
		}
		if ("undefined" == typeof(obj) ) {
			return false;
		}
		return true;
	}
};
