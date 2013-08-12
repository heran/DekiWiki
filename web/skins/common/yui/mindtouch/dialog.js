/**
 * Extend of YAHOO.widget.Dialog
 * -- handle of close button
 * -- additional params for buttons config value
 */

YAHOO.namespace("YAHOO.mindtouch");
YAHOO.namespace("YAHOO.mindtouch.widget");

(function () {

    YAHOO.mindtouch.widget.Dialog = function (el, userConfig) {
    
        YAHOO.mindtouch.widget.Dialog.superclass.constructor.call(this, el, userConfig);
    
    };
    
    var Event = YAHOO.util.Event,
        Dom = YAHOO.util.Dom,
        Lang = YAHOO.lang,
        Dialog = YAHOO.mindtouch.widget.Dialog;
        
    function removeButtonEventHandlers() {

        var aButtons = this._aButtons,
            nButtons,
            oButton,
            i;

        if (Lang.isArray(aButtons)) {
            nButtons = aButtons.length;

            if (nButtons > 0) {
                i = nButtons - 1;
                do {
                    oButton = aButtons[i];

                    if (YAHOO.widget.Button && oButton instanceof YAHOO.widget.Button) {
                        oButton.destroy();
                    }
                    else if (oButton.tagName.toUpperCase() == "BUTTON") {
                        Event.purgeElement(oButton);
                        Event.purgeElement(oButton, false);
                    }
                }
                while (i--);
            }
        }
    }

    YAHOO.extend(Dialog, YAHOO.widget.Dialog, {
        
        configClose: function (type, args, obj)
        {
            var val = args[0];
        
            function doCancel(e, obj) {
                obj.cancelEvent.fire();
            }
        
            if (val) {
                if (! this.close) {
                    this.close = document.createElement("div");
                    Dom.addClass(this.close, "container-close");
        
                    this.close.innerHTML = "&#160;";
                    this.innerElement.appendChild(this.close);
                    Event.on(this.close, "click", doCancel, this);
                } else {
                    this.close.style.display = "block";
                }
            } else {
                if (this.close) {
                    this.close.style.display = "none";
                }
            }
        },
        
        /**
        * The default event handler for the "buttons" configuration property
        * @method configButtons
        * @param {String} type The CustomEvent type (usually the property name)
        * @param {Object[]} args The CustomEvent arguments. For configuration 
        * handlers, args[0] will equal the newly applied value for the property.
        * @param {Object} obj The scope object. For configuration handlers, 
        * this will usually equal the owner.
        */
        configButtons: function (type, args, obj) {

            var Button = YAHOO.widget.Button,
                aButtons = args[0],
                oInnerElement = this.innerElement,
                oButton,
                oButtonEl,
                oYUIButton,
                nButtons,
                oSpan,
                oFooter,
                i;

            removeButtonEventHandlers.call(this);

            this._aButtons = null;

            if (Lang.isArray(aButtons)) {

                oSpan = document.createElement("span");
                oSpan.className = "button-group";
                nButtons = aButtons.length;

                this._aButtons = [];
                this.defaultHtmlButton = null;

                for (i = 0; i < nButtons; i++) {
                    oButton = aButtons[i];

                    if (Button) {

                        oYUIButton = new Button({ label: oButton.text, name: oButton.name });
                        oYUIButton.appendTo(oSpan);

                        oButtonEl = oYUIButton.get("element");

                        if (oButton.isDefault) {
                            oYUIButton.addClass("default");
                            this.defaultHtmlButton = oButtonEl;
                        }

                        if (Lang.isFunction(oButton.handler)) {

                            oYUIButton.set("onclick", { 
                                fn: oButton.handler, 
                                obj: this, 
                                scope: this 
                            });

                        } else if (Lang.isObject(oButton.handler) && Lang.isFunction(oButton.handler.fn)) {

                            oYUIButton.set("onclick", { 
                                fn: oButton.handler.fn, 
                                obj: ((!Lang.isUndefined(oButton.handler.obj)) ? oButton.handler.obj : this), 
                                scope: (oButton.handler.scope || this) 
                            });

                        }

                        this._aButtons[this._aButtons.length] = oYUIButton;

                    } else {

                        oButtonEl = document.createElement("button");
                        oButtonEl.setAttribute("type", "button");

                        if (oButton.isDefault) {
                            oButtonEl.className = "default";
                            this.defaultHtmlButton = oButtonEl;
                        }

                        oButtonEl.innerHTML = oButton.text;

                        if (Lang.isFunction(oButton.handler)) {
                            Event.on(oButtonEl, "click", oButton.handler, this, true);
                        } else if (Lang.isObject(oButton.handler) && 
                            Lang.isFunction(oButton.handler.fn)) {
    
                            Event.on(oButtonEl, "click", 
                                oButton.handler.fn, 
                                ((!Lang.isUndefined(oButton.handler.obj)) ? oButton.handler.obj : this), 
                                (oButton.handler.scope || this));
                        }

                        oSpan.appendChild(oButtonEl);
                        this._aButtons[this._aButtons.length] = oButtonEl;
                    }

                    oButton.htmlButton = oButtonEl;

                    if (i === 0) {
                        this.firstButton = oButtonEl;
                    }

                    if (i == (nButtons - 1)) {
                        this.lastButton = oButtonEl;
                    }
                }

                this.setFooter(oSpan);

                oFooter = this.footer;

                if (Dom.inDocument(this.element) && !Dom.isAncestor(oInnerElement, oFooter)) {
                    oInnerElement.appendChild(oFooter);
                }

                this.buttonSpan = oSpan;

            } else { // Do cleanup
                oSpan = this.buttonSpan;
                oFooter = this.footer;
                if (oSpan && oFooter) {
                    oFooter.removeChild(oSpan);
                    this.buttonSpan = null;
                    this.firstButton = null;
                    this.lastButton = null;
                    this.defaultHtmlButton = null;
                }
            }

            // Everything which needs to be done if content changed
            // TODO: Should we be firing contentChange here?

            this.setFirstLastFocusable();

            this.cfg.refireEvent("iframe");
            this.cfg.refireEvent("underlay");
        }
    });
}());
