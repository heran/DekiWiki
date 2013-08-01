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
 * 
 */

var Deki = Deki || {};

/**
 * @param {Object} oConfig Config params: 
 *   - src: source of the html file of the dialog
 *   - width: width of the dialog
 *   - height: height of the dialog
 *   - buttons: array with set of the buttons.
 *     May contain the follow values: Deki.Dialog.BTN_OK and Deki.Dialog.BTN_CANCEL
 *   - args: variable for pass to the dialog
 *   - callback: function is calling after user submited the dialog
 */
Deki.Dialog = function(oConfig)
{
    this.isOpened = false;
    this.setConfig(oConfig);
}

Deki.Dialog.prototype.setConfig = function(oConfig)
{
    this.oConfig = oConfig || {};
}

Deki.Dialog.prototype.render = function()
{
    if ( YAHOO.lang.isUndefined(this.getParam("src")) )
    {
        throw new Error("Config param src is undefined.");
    }
    
    // Prepare dom
    this._createContainer();
    
    var height = this.getParam("height");
    
    if ( YAHOO.lang.isValue(height) )
    {
        this._oIFrame.style.height = height;
    }
    
    this.setParam("status", false);
    this.setParam("handlers", {});
    this.setParam("close", true);
    this.setParam("zIndex", 9999);
    
    var yuiDialogConfig = {
          fixedcenter : true,
          visible : false,
          modal : true,
          iframe : true,
          zIndex : this.getParam("zIndex"),
          close : this.getParam("close"),
          constraintoviewport : true,
          postmethod : "manual"
    };
    
    var width = this.getParam("width");
    
    if ( YAHOO.lang.isValue(width) )
    {
        yuiDialogConfig.width = width;
    }
    
    var buttonSubmitHandler =
    {
        fn: function()
            {
                var dialog = this.getDialogWindow();
                
                if ( YAHOO.lang.isObject(dialog.Popup) )
                {
                    dialog.Popup.submit();
                }
                else
                {
                    this.close();
                }
            },
        scope: this
    };
    
    var buttonCancelHandler =
    {
        fn: function()
            {
                var dialog = this.getDialogWindow();
                
                try
                {
                    dialog.Popup.cancel();
                }
                catch (ex)
                {
                    this.close();
                }
            },
        scope: this
    };
    
    yuiDialogConfig.buttons = [];
    
    var buttons = this.getParam("buttons");
    
    if ( YAHOO.lang.isValue(buttons) )
    {
        for ( var i = 0; i < buttons.length; i++ )
        {
            var button = {};
            
            switch( buttons[i] )
            {
                case Deki.Dialog.BTN_OK:
                    button = { text: wfMsg('submit'), isDefault: true, handler: buttonSubmitHandler, name: buttons[i] };
                    break;
                case Deki.Dialog.BTN_CANCEL:
                    //read localization key
                    button = { text: wfMsg('cancel'), handler: buttonCancelHandler, name: buttons[i] };
                    break;
                default:
                    button = buttons[i];
                    break;
            }
            
            yuiDialogConfig.buttons.push(button);
        }
    }

    this.oDialog = new YAHOO.mindtouch.widget.Dialog(this._oContainer, yuiDialogConfig);
    
    this.oDialog.showEvent.subscribe(function(type, args, oScope) {
        setTimeout(function() {
        	if ( oScope.isOpened )
        	{
		        // Bugfix: Text input/Textarea caret is not displayed in Firefox
		        // for more information see http://developer.yahoo.com/yui/container/#knownissues
        		oScope._oContent.style.overflow  = 'auto';
        		oScope._oDivLoading.style.display = 'none';
        	}
    	}, 10);
    }, this);
    
    this.oDialog.cancelEvent.subscribe(function() {
        buttonCancelHandler.fn.call(this);
    }, this, true);
    
    this.oDialog.dragEvent.subscribe(function(x, y) {
        this._oIFrame.style.visibility = "hidden";
    }, this, true);
      
    this.oDialog.moveEvent.subscribe(function(x, y) {
        this._oIFrame.style.visibility = "visible";
    }, this, true);
    
    this.oDialog.render();
}

Deki.Dialog.prototype.show = function()
{
    var oScope = this;
    
    if ( this.getParam("args") )
    {
        Deki.Dialog.ARGUMENTS = this.getParam("args");
    }
    
    Deki.Dialog.ARGUMENTS.Dialog = this;
    
    var oButton = this.getButton(Deki.Dialog.BTN_OK);
    if ( oButton )
    {
    	oButton.setStyle("display", "none");
    }
    
    this.oDialog.show();
    
    if ( true === this.getParam("status") )
    {
        this._oStatusBar = document.createElement("div");
        YAHOO.util.Dom.addClass(this._oStatusBar, 'deki-dialog-status');
        
        this.setStatus(wfMsg('loading'));
        this.oDialog.appendToFooter(this._oStatusBar);
    }
    
    this.isOpened = true;
    this._oIFrame.src = this.getParam("src");
    
    return false;
}

/**
 * Close the dialog and call the callback function if it is necessary.
 * @param {Mixed} returnedParams Params for passing to the callback function
 * @method close
 */
Deki.Dialog.prototype.close = function(returnedParams)
{
    if ( YAHOO.lang.isNull(this.oConfig) || YAHOO.lang.isNull(this.oDialog) )
    {
        return;
    }

	// let ie set focus to main window (see #0007707)
	this.oDialog.hideMask();

    var callback = this.getParam("callback");
	var forceCallback = this.getParam("forceCallback");
    var scope = this.getParam("scope") || this;

    if ( YAHOO.lang.isFunction(callback) && ( YAHOO.lang.isValue(returnedParams) || forceCallback ) )
    {
        // Run the callback function and pass to it the returned param
        callback.call(scope, returnedParams);
    }
    
	this.isOpened = false;
	this.destroy();
}

/**
 * Removes all elements from the DOM and destroys the dialog
 * @method destroy
 */
Deki.Dialog.prototype.destroy = function()
{
	this._oForm.removeChild(this._oIFrame);
	this._oContent.removeChild(this._oForm);
	this._oContent.removeChild(this._oDivLoading);
	
    this.oDialog.destroyEvent.subscribe(function() {
        this._oContainer = null;
        this._oIFrame = null;
        this._oHeader = null;
        this._oContent = null;
        this._oFooter = null;
        this._oForm = null;
        this._oDivLoading = null;
    }, this, true);
	
    // Destroy the dialog and all created dom.
    // It's nessecary for repeated show the dialog.
    this.oDialog.destroy();
}

/**
 * Returns the config param of default value if it doesn't exist.
 * @param {string} key Name of the config param
 * @param {mixed} defaultValue
 * @method getParam
 */
Deki.Dialog.prototype.getParam = function(key, defaultValue)
{
    return ( YAHOO.lang.isUndefined(this.oConfig[key]) ) ? defaultValue : this.oConfig[key];
}

/**
 * Sets the config param to the defaultValue if this param doesn't exist.
 * @param {string} key Name of the config param
 * @param {mixed} defaultValue
 * @method setParam
 */
Deki.Dialog.prototype.setParam = function(key, defaultValue)
{
    this.oConfig[key] = this.getParam(key, defaultValue);
}

/**
 * Sets the text of the dialog's title.
 * @param {string} title
 * @method setTitle
 */
Deki.Dialog.prototype.setTitle = function(title)
{
    if (this._oHeader)
    {
        this._oHeader.innerHTML = title;
    }
}

/**
 * Sets the text of the status bar.
 * @param {string} status text in the status bar
 * @method setStatus
 */
Deki.Dialog.prototype.setStatus = function(statusText)
{
    if ( true === this.getParam("status") )
    {
        this._oStatusBar.innerHTML = statusText;
    }
}

Deki.Dialog.prototype.getButtons = function()
{
    if ( this.oDialog )
    {
        return this.oDialog.getButtons();
    }
    else
    {
        return null;
    }
}

Deki.Dialog.prototype.getButton = function(buttonName)
{
    var button = null;
    var buttons = this.getButtons();
    
    if ( buttons )
    {
        for ( var i = 0; i < buttons.length; i++ )
        {
            if ( buttons[i].get("name") == buttonName )
            {
                button = buttons[i];
                break;
            }
        }
    }
    
    return button;
}

Deki.Dialog.prototype.getDialog = function()
{
    return this.oDialog;
}

Deki.Dialog.prototype.resize = function(oParams)
{
    var animHeight, sUnit;
    
    oParams = oParams || {};
    sUnit = oParams.unit || "px";
    
    var center = function(ev, anim, d)
    {
        d.oDialog.center();

        nIE = YAHOO.env.ua.ie;

        if (nIE == 6 ||
            (nIE == 7 && d._oIFrame.contentWindow.document.compatMode == "BackCompat"))
        {
        
            d.oDialog.sizeUnderlay();
        }
    }
    
    if ( oParams.width )
    {
        if ( YAHOO.lang.isNumber(oParams.width) )
        {
            oParams.width += oParams.unit;
        }
        
        this._oContainer.style.width = oParams.width;
        
        if ( ! oParams.height )
        {
            center(null, null, this);
        }
    }
    
    if ( oParams.height )
    {
        animHeight = new YAHOO.util.Anim(this.getDialogWindow().frameElement, {height: {to : oParams.height, unit : sUnit}}, 0.2);
        animHeight.onComplete.subscribe(center, this);
        animHeight.animate();
    }    
}

/**
 * Adds neccesary elements to the dom.
 * @method _createContainer
 * @private
 */
Deki.Dialog.prototype._createContainer = function()
{
    var body = document.getElementsByTagName('body')[0];
    
    if ( ! YAHOO.util.Dom.hasClass(body, Deki.Dialog.SKIN_CLS) )
    {
        YAHOO.util.Dom.addClass(body, Deki.Dialog.SKIN_CLS)
    }
    
    this._oContainer = document.createElement('div');
    this._oContainer.id = 'dekidialog' + Deki.Dialog.Count;
    Deki.Dialog.Count++;

    this._oHeader = document.createElement('div');
    this._oHeader.className = "hd";
    this._oHeader.innerHTML = wfMsg('loading');
    
    this._oContent = document.createElement('div');
    this._oContent.className = "bd";

    this._oForm = document.createElement('form');
    this._oForm.method = "post";
            
    this._oIFrame = document.createElement('iframe');
    this._oIFrame.style.width = "100%";
    this._oIFrame.frameBorder = 0;
    this._oIFrame.allowTransparency = true;
    this._oIFrame.style.backgroundColor = "transparent";
    this._oIFrame.scrolling = "auto";
	this._oIFrame.src = 'javascript:false;'; // to avoid SSL security warning, see #0006298
    this._oForm.appendChild(this._oIFrame);
    
    this._oDivLoading = document.createElement('div');
    this._oDivLoading.className = 'deki-dialog-loading';
    this._oDivLoading.innerHTML = '<img src="' + Deki.PathCommon + '/icons/anim-circle.gif" />&nbsp;&nbsp;' + wfMsg('loading');
    
    this._oContent.appendChild(this._oForm);
    this._oContent.appendChild(this._oDivLoading);
    
    this._oFooter = document.createElement('div');
    this._oFooter.className = "ft";
    
    this._oContainer.appendChild(this._oHeader);
    this._oContainer.appendChild(this._oContent);
    this._oContainer.appendChild(this._oFooter);
    
    body.appendChild(this._oContainer);
}

Deki.Dialog.prototype.getDialogWindow = function()
{
    return this._oIFrame.contentWindow;
}

Deki.Dialog.Page = function(oConfig)
{
    oConfig = oConfig || {};
    
    if ( YAHOO.lang.isUndefined(oConfig.pageId) )
    {
        throw new Error("Page ID is undefined.");
    }
    
    oConfig.src =  Deki.PathCommon + '/popups/dekipage.php?pageId=' + oConfig.pageId;
	
	this.constructor.superclass.constructor.call( this, oConfig );
}

YAHOO.lang.extend( Deki.Dialog.Page, Deki.Dialog );

/**
 * Constant contains the class name of the skin.
 * It sets to the body element.
 * @property Deki.Dialog.SKIN_CLS
 * @static
 * @final
 */
Deki.Dialog.SKIN_CLS = "yui-skin-sam";

/**
 * Constant defines the Submit button in the dialog.
 * @property Deki.Dialog.BTN_OK
 * @static
 * @final
 */
Deki.Dialog.BTN_OK = "OK";

/**
 * Constant defines the Cancel button in the dialog.
 * @property Deki.Dialog.BTN_CANCEL
 * @static
 * @final
 */
Deki.Dialog.BTN_CANCEL = "CANCEL";

Deki.Dialog.ARGUMENTS = {};

Deki.Dialog.Count = 1;
