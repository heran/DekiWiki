/*
 * MindTouch Deki - enterprise collaboration and integration platform
 * Copyright (C) 2006-2009 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
 * please review the licensing section.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 * http://www.gnu.org/copyleft/gpl.html
 */

/**
 * The popup dialogs library.
 * This script should be included into the dialog's source.
 * 
 */ 

Popup = function()
{

    /**
     * Property for storage params from the parent window.
     * @property params
     * @private
     * @type Mixed
     */
    var params = null;
    
    var skipDefaultEventHandler = false;
    
    var Dialog = null;
    
    var Event = parent.YAHOO.util.Event;
        
    /**
     * This method creates the form if it does not exist in the source and
     * set the focus to the first form's element.
     * @method setFocus
     * @private
     */
    function setFocus()
    {
		window.focus();
        
        var form = document.getElementsByTagName("form")[0];

        // if form exists then try to focus the first form element
        if ( form )
        {
            // set focus to the first element of the form
            for ( var f = 0; f < form.elements.length; f++ )
            {
                var el = form.elements[f];
                if ( el.focus && ! el.disabled )
                {
                    if ( el.type && el.type != "hidden" ) 
                    {
                        try
                        {
                            el.focus();
                            break;
                        }
                        catch (e) {}
                    }
                }
            }
        }
    }
    
    /**
     * Event handler.
     * @method eventHandler
     * @param {Object} ev The event object
     * @private
     * @retrun Boolean
     */
    function eventHandler(ev)
    {
        ev || ( ev = window.event );
        
        if ( skipDefaultEventHandler && (27 == ev.keyCode || 13 == ev.keyCode) )
        {
            skipDefaultEventHandler = false;
            return true;
        }        

        if ( 27 == ev.keyCode )
        {
            Event.stopEvent(ev);
            Popup.cancel();
            return false;
        }

        if ( 13 == ev.keyCode )
        {
            var target = null;
            var exceptElements = { textarea:1, input:{ button:1, submit:1, reset:1, image:1, file:1 }, select:1 };
             
            if ( ev.target )
                target = ev.target;
            else if ( ev.srcElement )
                target = ev.srcElement;
                
            if ( target.nodeType == 3 ) // defeat Safari bug
                target = target.parentNode;
            
            var exceptElement = exceptElements[target.nodeName.toLowerCase()]; 
            
            if ( ! exceptElement ||
                 (exceptElement && typeof exceptElement == "object" && ! exceptElement[target.type]) )
            {
                Event.stopEvent(ev);
                Popup.submit();
                return false;
            }
        }
        return true;
    }
    
    function calculateSize(sOldSize, sSize, nMinSize, nMaxSize)
    {
        var sNewSize;
        var sFirstChar = sSize.charAt(0);
        
        if ( isNaN(sFirstChar) )
        {
            var sUnit = sOldSize.substr(sOldSize.length - 2);
            var aUntis = { em: 1, ex: 1, px: 1 };
            
            if ( aUntis[sUnit] )
            {
                var nOldValue = parseInt(sOldSize);
                var nCoeff = parseFloat(sSize.substr(1));
                
                switch (sFirstChar) {
                    case "+":
                        nNewValue = nOldValue + nCoeff;
                        break;
                    case "-":
                        nNewValue = nOldValue - nCoeff;
                        break;
                    case "*":
                        nNewValue = nOldValue * nCoeff;
                        break;
                    case "/":
                        nNewValue = nOldValue / nCoeff;
                        break;
                    default:
                        nNewValue = nOldValue;
                }                
                
                if ( nMinSize && (nNewValue < nMinSize) )
                {
                    nNewValue = nMinSize;
                }

                if ( nMaxSize && (nNewValue > nMaxSize) )
                {
                    nNewValue = nMaxSize;
                }
                
                if ( nNewValue <= 0 )
                {
                    nNewValue = nOldValue;
                }
                
                sNewSize = nNewValue + sUnit;
            }
            else
            {
                sNewSize = sOldSize;
            }
        }
        else
        {
            sNewSize = sSize;
        }
        
        return sNewSize;
    }
    
    function calculateDialogHeight(sOldHeight)
    {        
        var sNewHeight = sOldHeight;
        
        if (document.body)
        {
            sNewHeight = document.body.offsetHeight + getHdFtHeight() - 30 + "px";
        }
        
        return sNewHeight;
    }
    
    function calculateDialogWidth(sOldWidth)
    {        
        var sNewWidth = sOldWidth;
        
        if (document.body)
        {
            sNewWidth = document.body.scrollWidth + "px";
        }
        
        return sNewWidth;
    }
        
    function getHdFtHeight()
    {
    	var nTitleHeight = 0, nFooterHeight = 0;
    	
    	if ( Dialog.isOpened )
		{
	        nTitleHeight = Dialog._oHeader.offsetHeight;
	        nFooterHeight = Dialog._oFooter.offsetHeight;
		}
        
        return nTitleHeight + nFooterHeight;
    }
    
    function getParentHeight()
    {
        var height;
        
        if ( typeof( parent.innerHeight ) == 'number' )
        {
            //Non-IE
            height = parent.innerHeight;
        }
        else if ( parent.document.documentElement && parent.document.documentElement.clientHeight )
        {
            //IE 6+ in 'standards compliant mode'
            height = parent.document.documentElement.clientHeight;
        }
        else if ( parent.document.body && parent.document.body.clientHeight )
        {
            //IE 4 compatible
            height = parent.document.body.clientHeight;
        }
        
        return height;
    }
            
    return {
    
        /**
         * Config params.
         * @property config
         * @type Object
         */
        config : {},

        /**
         * Init the dialog. Sets the params from the parent window and
         * adds event for Enter and Esc keys.
         * @param {Object} config Config params: 
         *   - handlres: object contains the submit and/or cancel handle functions
         *     This handlers should return params for passing to the callback function.
         * @method init
         */
        init : function(config)
        {
            Popup.BTN_OK = parent.Deki.Dialog.BTN_OK;
            Popup.BTN_CANCEL = parent.Deki.Dialog.BTN_CANCEL;

            // Reterive params from the parent window
            params = parent.Deki.Dialog.ARGUMENTS;
            Dialog = params.Dialog;
            
            Popup.setConfig(config);

            // Set the title from dialog's <title> tag
            Popup.initTitle();

            var resize = {};
            
            if ( "auto" == Dialog.getParam("width") )
            {
                resize.width = 'auto';
            }
            
            if ( "auto" == Dialog.getParam("height") )
            {
                resize.height = 'auto';
            }
            
            if ( resize.width || resize.height )
            {
                Popup.resize(resize);
            }
            
            setFocus();            
        },
        
        setConfig : function(config)
        {
        	this.config = config || {};
        	
            if ( "undefined" == typeof(this.config.handlers) )
            {
                this.config.handlers = {};
            }
            
            if ( ! (this.config.defaultKeyListeners === false) )
            {
            	var aListeners = Event.getListeners(document, "keypress");
            	
            	if ( !aListeners || aListeners.length === 0 )
            	{
	                // add Enter and Esc events
	                Event.addListener(document, "keypress", eventHandler);
            	}
            }
        },
        
        initTitle : function()
        {
            var title = Dialog.getParam('title', document.title);

            Dialog.setTitle(title);            
            Popup.setStatus(parent.wfMsg('ready'));
          
            var okButton = Popup.getButton(Popup.BTN_OK);
            if ( okButton )
            {
                okButton.set("label", Dialog.getParam('btnOkLabel', title));
                okButton.setStyle("display", "");
            }
        },
        
        /**
         * Get params set on init.
         * @method getParams
         * @retrun Mixed
         */
        getParams : function()
        {
            return params;
        },
        
        /**
         * Submit dialog's handler. Calls the submit handler set on init
         * and closes the dialog.
         * @method submit
         */
        submit : function()
        {
            var result = null;
            var scope = this.config.scope || this;
            
            if ( "function" == typeof(this.config.validate) )
            {
                if ( ! this.config.validate.apply(scope, arguments) )
                {
                    return false;
                }
            }

            if ( "function" == typeof(this.config.handlers.submit) )
            {
                result = this.config.handlers.submit.apply(scope, arguments);
            }
            else
            {
                result = false;
            }
            
            if ( null === result || false === this.config.autoClose )
            {
                return false;
            }
            
            Popup.close(result);
        },
        
        /**
         * Cancel dialog's handler. Calls the cancel handler set on init
         * and closes the dialog.
         * @method cancel
         */
        cancel : function()
        {
            var result = null;
            var scope = this.config.scope || this;
            var cancelButton;
            
            if ( Dialog )
                cancelButton = Dialog.getButton(Popup.BTN_CANCEL);
            
            if ( cancelButton && cancelButton.get("disabled") )
            {
                return false;
            }
            
            if ( "function" == typeof(this.config.handlers.cancel) )
            {
                result = this.config.handlers.cancel.apply(scope, arguments);
            }
            
            if ( false === this.config.autoClose )
            {
            	return false;
            }
            
            Popup.close(result);
        },
        
        /**
         * This method returns the returnedParam to the parent window and
         * closes the dialog. 
         * @param {Mixed} returnedParams Params for return to the callback function. 
         * @method close
         */
        close : function(returnedParams)
        {
            Event.removeListener(document, "keypress", eventHandler);
            
            if ( "undefined" == typeof(returnedParams) )
            {
                returnedParams = null;
            }
            
            Dialog.close(returnedParams);
        },
		
		getDialog : function()
		{
			return Dialog;
		},
        
        getButton : function(sButtonName)
        {
            return Dialog.getButton(sButtonName);
        },
        
        disableButton : function(sButtonName)
        {
            var oYUIButton = Dialog.getButton(sButtonName);
            
            if ( oYUIButton )
            {
                oYUIButton.set("disabled", true);
                if ( oYUIButton.hasClass("default") )
                {
                    oYUIButton.replaceClass("default", "disabled-default");
                }
            }
        },
        
        enableButton : function(sButtonName)
        {
            var oYUIButton = Dialog.getButton(sButtonName);
            
            if ( oYUIButton )
            {
                oYUIButton.set("disabled", false);
                if ( oYUIButton.hasClass("disabled-default") )
                {
                    oYUIButton.replaceClass("disabled-default", "default");
                }
            }
        },
        
        setStatus : function(sStatusText)
        {
            Dialog.setStatus(sStatusText);
        },
        
        skipDefaultEventHandler : function()
        {
            skipDefaultEventHandler = true;
        },
        
        removeDefaultEventHandler : function()
        {
        	return Event.removeListener(document, eventHandler);
        },
        
        resize : function(oDimensions)
        {
            var oConfig = {},
                sOldWidth, sNewWidth,
                sOldHeight, sNewHeight, nNewHeight, sUnit, nHdFtHeight;
                
            if ( !Dialog.isOpened )
            	return;
            
            oDimensions = oDimensions || { width : 'auto', height : 'auto' };

            if ( oDimensions.width )
            {
            	sOldWidth = Dialog._oContainer.style.width;
            	
                if ( oDimensions.width == "auto" )
                {
                    oDimensions.width = calculateDialogWidth(sOldWidth);
                }
                
                sNewWidth = calculateSize(sOldWidth, oDimensions.width, oDimensions.minWidth, oDimensions.maxWidth);
                oConfig.width = sNewWidth;
            }
            
            if ( oDimensions.height )
            {
                sOldHeight = window.frameElement.style.height;
                
                if ( oDimensions.height == "auto" )
                {
                    oDimensions.height = calculateDialogHeight(sOldHeight);
                }
                
                sNewHeight = calculateSize(sOldHeight, oDimensions.height, oDimensions.minHeight, oDimensions.maxHeight);
                
                nNewHeight = parseInt(sNewHeight);
                
                oConfig.unit = sNewHeight.substr(sNewHeight.length - 2);
                
                if ( "px" == oConfig.unit )
                {
                    nHdFtHeight = getHdFtHeight();
                    
                    // restrict max height by the height of the parent window
                    if ( getParentHeight() < (nNewHeight + nHdFtHeight + 30) )
                    {
                        nNewHeight = getParentHeight();
                    }
                    
                    // restrict min height by the height of the title and footer
                    if ( nNewHeight < nHdFtHeight )
                    {
                        nNewHeight = parseInt(sOldHeight);
                    }
                }
                
                oConfig.height = nNewHeight;
            }
            
            Dialog.resize(oConfig);
        },
        
        getDialogContainer : function()
        {
        	return Dialog._oContainer;
        }
    }
}();
