/*
 * Copyright (c) 2007, David A. Lindquist (stringify.com)
 * Some Rights Reserved
 *
 * This code is licensed under the Creative Commons Attribution 2.5 License
 * (http://creativecommons.org/licenses/by/2.5/). Please maintain the above
 * license and copyright statements when using this code.
 *
 * $Id: columnav.js 518 2007-02-27 04:50:56Z david $
 */
YAHOO.namespace('extension');

YAHOO.extension.ColumNav = function(id, cfg) {
    this._init(id, cfg);
};

YAHOO.extension.ColumNav.prototype = {

    DOM: YAHOO.util.Dom,
    EVT: YAHOO.util.Event,
    CON: YAHOO.util.Connect,

    ERROR_MSG: 'data unavailable',

    // PUBLIC API

    reset: function() {
        this.carousel.clear();
        this._init(this.id, this.cfg);
    },
    
    goto: function(num) {
	  	this._next(this);
    },

    // PRIVATE API

    _init: function(id, cfg) {
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
    },

    _loadPrevHandler: function(type, args) {
        this.cn._abortRequest();
    },

    _makeRequest: function(url) {
        var callback = {
            'success':  this._handleSuccess,
            'failure':  this._handleFailure,
            'scope':    this,
            'timeout':  5000
        };
        this._abortRequest();
        this.request = this.CON.asyncRequest('GET', url, callback);
    },

    _abortRequest: function() {
        if (this.request && this.CON.isCallInProgress(this.request))
            this.CON.abort(this.request);
    },

    _handleSuccess: function(o) {
        var node;
        var contentType = o.getResponseHeader['Content-Type'];
        if ('application/json' == contentType.replace(/\s+$/,'')) { // IE reports Content-Type
                                                                    // having trailing ASCII 13
            try {
                // requires http://www.json.org/json.js
                node = o.responseText.parseJSON();
            } catch (e) {
                this._handleFailure(o);
                return;
            }
        } else {
            node = o.responseXML.documentElement;
        }
        this._addMenu(node);
    },

    _handleFailure: function(o) {
        var list = document.createElement('ul');
        var item = document.createElement('li');
        var span = document.createElement('span');
        span.className = 'columnav-error';
        span.appendChild(document.createTextNode(this.ERROR_MSG));
        item.appendChild(span);
        list.appendChild(item);
        this._addMenu(list);
    },

    _handleKeypress: function(type, args, o) {
        var key = args[0];
        var evt = args[1];
        var target = this.EVT.getTarget(evt);
        if (this.moving) {
            this.EVT.stopEvent(evt);
            return;
        }
        if (target.tagName != 'A') {
            var links = this._getNodes(this.carousel.carouselList.lastChild,
                                       this._links);
            links[0].focus();
            return;
        }
        switch (key) {
        case 37: // left
            var menu = target.parentNode.parentNode;
            if (this._shouldScrollPrev(menu)) {
                o.carousel.scrollPrev();
                this.moving = true;
            } else {
                var prevMenu = this._prevMenu(menu);
                if (prevMenu)
                    this._focus(prevMenu);
            }
            break;
        case 38: // up
            if (target.previousSibling)
                target.previousSibling.focus();
            break;
        case 39: // right
            this._next(evt);
            break;
        case 40: // down
            if (target.nextSibling)
                target.nextSibling.focus();
            break;
        }
        this.EVT.stopEvent(evt);
    },

    _addMenu: function(node) {
        var menu = this._createMenu(node);
        this.carousel.addItem(this.counter++, menu);
        if (this._shouldScrollNext()) {
            this.carousel.scrollNext();
            this.moving = true;
        } else {
            if (this.counter > 2)
                this._focus(menu);
        }
    },

    _shouldScrollNext: function() {
        var numVisible = this.carousel.cfg.getProperty('numVisible');
        return ((this.counter - 1) - this.numScrolled > numVisible);
    },

    _shouldScrollPrev: function(menu) {
        var menus = this._getNodes(this.carousel.carouselList,
                                   this._childElements);
        var i = 0;
        for ( ; i < menus.length; i++) {
            if (menu == menus[i]) break;
        }
        return (i > 0 && i == this.numScrolled);
    },

    _prevMenu: function(menu) {
        var prevLi = menu.previousSibling;
        if (prevLi)
            return prevLi.getElementsByTagName('div')[0];
        return null;
    },

    _next: function(e) {
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
        if (list)
            this._addMenu(list);
        else if (rel && rel.match(/\bajax\b/))
            this._makeRequest(href);
        else {
            if (this.linkAction(e))
                return true;
        }
        this.EVT.stopEvent(e);
    },

    _removeMenus: function(target) {
        var li = target.parentNode.parentNode;
        var list = this.carousel.carouselList;
        while (li != list.lastChild) {
            list.removeChild(list.lastChild);
            this.counter--;
        }
    },

    _highlight: function(target) {
        var items = this._getNodes(target.parentNode, this._childElements);
        for (var i = 0; i < items.length; i++)
            this.DOM.removeClass(items[i], 'columnav-active');
        this.DOM.addClass(target, 'columnav-active');
    },

    _focus: function(menu) {
	    return;
        var links = this._getNodes(menu, this._links);
        for (var i = 0; i < links.length; i++) {
            if (this.DOM.hasClass(links[i], 'columnav-active')) {
                links[i].focus();
                return;
            }
        }
        links[0].focus();
    },

    _animationCompleteHandler: function(type, args) {
        this.cn.moving = false;
        if (args[0] == 'next') {
            this.cn.numScrolled++;
            this.cn._focus(this.carouselList.lastChild);
        }
        if (args[0] == 'prev') {
            this.cn._removeLastMenu();
            this.cn.numScrolled--;
            if (this.cfg.getProperty('numVisible') == 1)
                this.cn._focus(this.carouselList.lastChild);
        }
    },

    _removeLastMenu: function() {
        var list = this.carousel.carouselList;
        list.removeChild(list.lastChild);
        this.counter--;
    },

    _createMenu: function(node) {
        var menu = document.createElement('div');
        if (node.nodeType) { // DOM
            var items = this._getNodes(node, this._childElements);
            for (var i = 0; i < items.length; i++) {
                var ce = this._getNodes(items[i], this._childElements);
                var a = ce[0];
                var o = { text: a.firstChild.data, list: ce[1] };
                for (var j = 0; j < a.attributes.length; j++) {
                    if (a.attributes[j].specified) {
                        var n = '@' + a.attributes[j].name.toLowerCase();
                        var v = a.attributes[j].value;
                        o[n] = v;
                    }
                }
                menu.appendChild(this._createLink(o));
            }
        } else { // JSON
            var items = node.li || node.ul.li;
            for (var i = 0; i < items.length; i++) {
                var a = items[i].a;
                var o = { text: a['#text'], list: items[i].ul };
                for (var n in a) {
                    if ('@' == n.charAt(0)) {
                        var v = a[n];
                        o[n] = v;
                    }
                }
                menu.appendChild(this._createLink(o));
            }
        }
        return menu;
    },

    _createLink: function(o) {
        var a = document.createElement('a');
        var span = document.createElement('span');
        span.appendChild(document.createTextNode(o['text']));
        a.appendChild(span);

        for (var n in o) {
            if ('@' == n.charAt(0)) {
                var v = o[n];
                if (n == '@class')
                    a.className = v;
                else
                    a.setAttribute(n.substring(1), v);
            }
        }

        a.list = o['list'];
        if (o['list'] || (o['@rel'] && o['@rel'].match(/(?:^|\s+)ajax(?:\s+|$)/)))
            this.DOM.addClass(a, 'columnav-has-menu');
        this.EVT.addListener(a, 'click', this._next, this, true);
        return a;
    },

    _defaultLinkAction: function() { return true; },

    _getNodes: function(root, filter) {
        var node = root;
        var nodes = [];
        var next;
        var f = filter || function() { return true; }
        while (node != null) {
            if (node.hasChildNodes())
                node = node.firstChild;
            else if (node != root && null != (next = node.nextSibling))
                node = next;
            else {
                next = null;
                for ( ; node != root; node = node.parentNode) {
                    next = node.nextSibling;
                    if (next != null) break;
                }
                node = next;
            }
            if (node != null && f(node, root))
                nodes.push(node);
        }
        return nodes;
    },

    _childElements: function(node, root) {
        return (node.nodeType == 1 && node.parentNode == root);
    },

    _links: function(node) { return (node.tagName == 'A'); }
};

YAHOO.extension.ColumnNav = YAHOO.extension.ColumNav;
