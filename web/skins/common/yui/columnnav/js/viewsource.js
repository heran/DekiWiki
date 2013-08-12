/*
 * Copyright (c) 2007, David A. Lindquist (stringify.com)
 * Some Rights Reserved
 *
 * This code is licensed under the Creative Commons Attribution 2.5 License
 * (http://creativecommons.org/licenses/by/2.5/). Please maintain the above
 * license and copyright statements when using this code.
 *
 * $Id: viewsource.js 462 2007-02-18 23:03:53Z david $
 */
YAHOO.namespace('extension');

YAHOO.extension.SourceViewer = function(id, cfg) {
    this._init(id, cfg);
};

YAHOO.extension.SourceViewer.prototype = {

    ERROR_MSG: 'source unavailable',

    // PUBLIC API

    toggle: function() {
        if (this.codeblock.style.display == 'block') {
            this._hide();
        } else {
            if (this._req_needed) {
                var conn = YAHOO.util.Connect;
                var callback = {
                    'success': this._handleSuccess,
                    'failure': this._handleFailure,
                    'scope':   this,
                    'timeout': 5000
                };
                if (this.request && conn.isCallInProgress(this.request))
                    conn.abort(this.request);
                this.request = conn.asyncRequest('GET', this.url, callback);
            }
            this._show();
        }
    },

    // PRIVATE API

    _req_needed: true,

    _init: function(id, cfg) {
        this.id = id;
        this.cfg = cfg; // make this a YAHOO.util.Config object?

        this.url = cfg.url;
        this.request = null;
        this.text = 'View ' + (cfg.modifier || '') + ' source';
        this.container = document.getElementById(id);
        if (!this.container || !cfg.url)
            return;
        var p = document.createElement('p');
        var a = document.createElement('a');
        var div = document.createElement('div');
        var pre = document.createElement('pre');
        var code = document.createElement('code');
        a.setAttribute('href','javascript:void(0)');
        div.setAttribute('id', id + '-source');
        div.style.display = 'none'; // setAttribute does not seem
                                    // to work here for IE
        a.appendChild(document.createTextNode(this.text));
        p.appendChild(a);
        code.appendChild(document.createTextNode('Loading...'));
        pre.appendChild(code);
        div.appendChild(pre);
        YAHOO.util.Event.addListener(a, 'click', this.toggle, null, this);
        this.container.appendChild(p);
        this.container.appendChild(div);
        this.link = a;
        this.codeblock = div;
    },

    _show: function() {
        this.codeblock.style.display = 'block';
        this.link.firstChild.data = this.text.replace('View', 'Hide');
    },

    _hide: function() {
        this.codeblock.style.display = 'none';
        this.link.firstChild.data = this.text.replace('Hide', 'View');
    },

    _handleSuccess: function(o) {
        var code = this.codeblock.getElementsByTagName('code')[0];
        var header = '[source of: <a href="' + this.url + '">' + this.url + '</a>]';
        code.innerHTML = header + this._encode('\n\n' + o.responseText);
        this._req_needed = false;
    },

    _handleFailure: function(o) {
        var code = this.codeblock.getElementsByTagName('code')[0];
        code.innerHTML = this.ERROR_MSG;
    },

    _encode: function(str) {
        str = str.replace(/&/g, '&amp;');
        str = str.replace(/</g, '&lt;');
        str = str.replace(/>/g, '&gt;');
        /*@cc_on
          str = str.replace(/\n\n/g, '<br/>&nbsp;<br/>');
          str = str.replace(/\n/g, '<br/>');
          str = str.replace(/ /g, '&nbsp;');
          @*/
        return str;
    }
};
