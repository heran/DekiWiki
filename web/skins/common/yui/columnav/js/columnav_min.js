/*
 * Copyright (c) 2007, David A. Lindquist <david.lindquist@gmail.com>
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 *
 * $Id: columnav_min.js 175 2007-08-04 06:03:03Z david $
 */

YAHOO.namespace('extension');YAHOO.extension.ColumNav=function(id,cfg){this._init(id,cfg);};YAHOO.extension.ColumNav.prototype={DOM:YAHOO.util.Dom,EVT:YAHOO.util.Event,CON:YAHOO.util.Connect,DEFAULT_ERROR_MSG:'Data unavailable',reset:function(){this.carousel.clear();this._init(this.id,this.cfg);},toString:function(){return'<ColumNav '+this.id+'>';},_init:function(id,cfg){this.id=id;this.cfg=cfg;this.datasource=cfg.datasource||cfg.source;this.request=null;this.counter=1;this.numScrolled=0;this.isMoving=false;if(cfg.animationSpeed===0)
cfg.animationSpeed=Number.MIN_VALUE;this.prevButtonStateHandler=cfg.prevButtonStateHandler;this.animationCompleteHandler=cfg.animationCompleteHandler;var prevElement=cfg.prevElement||cfg.prevId;if(prevElement)
this.EVT.addListener(prevElement,'click',this._prev,this,true);var me=this;this.carousel=new YAHOO.extension.Carousel(id,{animationCompleteHandler:function(type,args){me._animationCompleteHandler(type,args,me)},animationMethod:cfg.animationMethod,animationSpeed:cfg.animationSpeed,numVisible:cfg.numVisible||1,prevButtonStateHandler:function(type,args){me._prevButtonStateHandler(type,args,me)},prevElement:prevElement,scrollInc:1});if(this.carousel.cfg.getProperty('numVisible')>1)
this.DOM.addClass(this.carousel.carouselElem,'columnav-multiple');if(typeof cfg.requestHandler=='function'){this.onRequest=new YAHOO.util.CustomEvent('onRequest',this);this.onRequest.subscribe(cfg.requestHandler,this);}
if(typeof cfg.responseHandler=='function'){this.onResponse=new YAHOO.util.CustomEvent('onResponse',this);this.onResponse.subscribe(cfg.responseHandler,this);}
if(typeof cfg.nextHandler=='function'||typeof cfg.linkAction=='function')
{var sig=cfg.nextHandler?YAHOO.util.CustomEvent.LIST:YAHOO.util.CustomEvent.FLAT;this.onNext=new YAHOO.util.CustomEvent('onNext',this,false,sig);this.onNext.subscribe(cfg.nextHandler||cfg.linkAction,this);}
if(typeof cfg.prevHandler=='function'){this.onPrev=new YAHOO.util.CustomEvent('onPrev',this);this.onPrev.subscribe(cfg.prevHandler,this);}
if(typeof cfg.paneHandler=='function'){this.onPane=new YAHOO.util.CustomEvent('onPane',this);this.onPane.subscribe(cfg.paneHandler,this);}
var notOpera=(navigator.userAgent.match(/opera/i)==null);var kl=new YAHOO.util.KeyListener(this.carousel.carouselElem,{ctrl:notOpera,keys:[37,38,39,40]},{fn:this._handleKeypress,scope:this,correctScope:true});kl.enable();var ds=this.datasource;if(ds&&typeof ds=='object')
this._addPane(ds);else if(typeof ds=='string')
this._makeRequest(ds,null);else
this._handleFailure({argument:'Invalid datasource'},true);},_makeRequest:function(url,target){var callback={success:this._handleSuccess,failure:this._handleFailure,scope:this,timeout:Number(this.cfg.requestTimeout)||5000,argument:'Ajax request failed'};this._abortRequest();if(this.onRequest)
this.onRequest.fire(url,target);this.request=this.CON.asyncRequest('GET',url,callback);},_abortRequest:function(){if(this.request&&this.CON.isCallInProgress(this.request))
this.CON.abort(this.request);},_handleSuccess:function(o){var content;var contentType=o.getResponseHeader['Content-Type'];if('application/json'==contentType.replace(/\s+$/,'')){try{content=o.responseText.parseJSON();}catch(e){o.argument='JSON parsing failed';this._handleFailure(o);return;}}else{if(o.responseXML==null){o.argument='Malformed XML response';this._handleFailure(o);return;}
var content=o.responseXML.documentElement;if('ul'!=content.tagName.toLowerCase())
content=o.responseText;}
if(this.onResponse)
this.onResponse.fire('success');this._addPane(content);},_handleFailure:function(o,suppressEvent){var list=document.createElement('ul');var item=document.createElement('li');var span=document.createElement('span');var error=o.argument||this.DEFAULT_ERROR_MSG;span.className='columnav-error';span.appendChild(document.createTextNode(error));item.appendChild(span);list.appendChild(item);if(this.onResponse&&!suppressEvent)
this.onResponse.fire('failure');this._addPane(list);},_handleKeypress:function(type,args,o){var key=args[0];var evt=args[1];var target=this.EVT.getTarget(evt);var pane=target;while(!this.DOM.hasClass(pane,'columnav-menu')&&!this.DOM.hasClass(pane,'columnav-nonmenu'))
{pane=pane.parentNode;}
var isNonMenu=this.DOM.hasClass(pane,'columnav-nonmenu');if(this.isMoving||isNonMenu){this.EVT.stopEvent(evt);return;}
if(target.tagName!='A'){var links=this._getNodes(this.carousel.carouselList.lastChild,this._links);links[0].focus();return;}
switch(key){case 37:if(this._shouldScrollPrev(pane)){this._prev(evt);o.carousel.scrollPrev();this.isMoving=true;}else{var prevPane=this._prevPane(pane);if(prevPane)
this._focus(prevPane);}
break;case 38:if(target.previousSibling)
target.previousSibling.focus();break;case 39:this._next(evt);break;case 40:if(target.nextSibling)
target.nextSibling.focus();break;}
this.EVT.stopEvent(evt);},_addPane:function(content){var pane,cls;if(typeof content=='string'){pane=content;cls='columnav-nonmenu';}else{if(content.tagName&&'ul'!=content.tagName.toLowerCase()){pane=(document==content.ownerDocument)?content.cloneNode(true):content;cls='columnav-nonmenu';}else{pane=this._createMenu(content);cls='columnav-menu';}}
this.carousel.addItem(this.counter,pane,cls);if(this.onPane)
this.onPane.fire(this.counter);if(this._shouldScrollNext()){this.carousel.scrollNext();this.isMoving=true;}else{if(this.counter>1)
this._focus(pane);}
this.counter++;},_shouldScrollNext:function(){var numVisible=this.carousel.cfg.getProperty('numVisible');return(this.counter-this.numScrolled>numVisible);},_shouldScrollPrev:function(pane){var panes=this._getNodes(this.carousel.carouselList,this._childElements);var i=0;for(;i<panes.length;i++){if(pane==panes[i])break;}
return(i>0&&i==this.numScrolled);},_prevPane:function(pane){var prevLi=pane.previousSibling;if(prevLi)
return prevLi.getElementsByTagName('div')[0];return null;},_prev:function(e){this._abortRequest();if(this.onPrev)
this.onPrev.fire(this.EVT.getTarget(e),e);},_next:function(e){if(this.isMoving){this.EVT.stopEvent(e);return;}
var target=this.EVT.getTarget(e);if(target.tagName=='SPAN')
target=target.parentNode;this._removePanes(target);var href=target.getAttribute('href');var rel=target.getAttribute('rel');var next=target.next;if(href!==null)
this._highlight(target);var go=true;if(this.onNext){var arg=this.cfg.nextHandler?target:e;go=this.onNext.fire(arg,e);}
if(next){if(go)this._addPane(next);}else if(rel&&rel.match(/\bajax\b/)){if(go)this._makeRequest(href,target);}else{if(go)return true;}
this.EVT.stopEvent(e);},_removePanes:function(target){var li=target.parentNode.parentNode;var list=this.carousel.carouselList;while(li!=list.lastChild){list.removeChild(list.lastChild);this.counter--;}},_highlight:function(target){var items=this._getNodes(target.parentNode,this._childElements);for(var i=0;i<items.length;i++)
this.DOM.removeClass(items[i],'columnav-active');this.DOM.addClass(target,'columnav-active');},_focus:function(el){if(this.DOM.hasClass(el,'columnav-nonmenu')){if(el.firstChild.focus)el.firstChild.focus();else el.focus();}else{var links=this._getNodes(el,this._links);for(var i=0;i<links.length;i++){if(this.DOM.hasClass(links[i],'columnav-active')){links[i].focus();return;}}
if(links[0])
links[0].focus();}},_animationCompleteHandler:function(type,args,me){this.isMoving=false;if(args[0]=='next'){this.numScrolled++;this._focus(this.carousel.carouselList.lastChild);}
if(args[0]=='prev'){this._removeLastPane();this.numScrolled--;if(this.carousel.cfg.getProperty('numVisible')==1)
this._focus(this.carousel.carouselList.lastChild);}
if(typeof this.animationCompleteHandler=='function')
this.animationCompleteHandler(type,args,me);},_prevButtonStateHandler:function(type,args,me){if(typeof this.prevButtonStateHandler=='function')
this.prevButtonStateHandler(type,args,me);},_removeLastPane:function(){var list=this.carousel.carouselList;list.removeChild(list.lastChild);this.counter--;},_createMenu:function(node){var menu=document.createElement('div');var title;if(node.nodeType){var items=this._getNodes(node,this._childElements);for(var i=0;i<items.length;i++){var ce=this._getNodes(items[i],this._childElements);var a=ce[0];var o={text:a.firstChild.data,next:ce[1]};for(var j=0;j<a.attributes.length;j++){if(a.attributes[j].specified){var n='@'+a.attributes[j].name.toLowerCase();var v=a.attributes[j].value;o[n]=v;}}
menu.appendChild(this._createLink(o));}
title=node.getAttribute('title');}else{var items=node.li||node.ul.li;for(var i=0;i<items.length;i++){var a=items[i].a;var o={text:a['#text'],next:items[i].ul};for(var n in a){if('@'==n.charAt(0)){var v=a[n];o[n]=v;}}
menu.appendChild(this._createLink(o));}
title=node['@title'];if(!title&&node.ul)
title=node.ul['@title'];}
if(title)
menu.setAttribute('title',title);return menu;},_createLink:function(o){var a=document.createElement('a');var span=document.createElement('span');span.appendChild(document.createTextNode(o['text']));a.appendChild(span);for(var n in o){if('@'==n.charAt(0)){var v=o[n];if(n=='@class')
a.className=v;else
a.setAttribute(n.substring(1),v);}}
a.next=o['next'];if(o['next']||(o['@rel']&&o['@rel'].match(/(?:^|\s+)ajax(?:\s+|$)/)))
this.DOM.addClass(a,'columnav-has-next');this.EVT.addListener(a,'click',this._next,this,true);return a;},_getNodes:function(root,filter){var node=root;var nodes=[];var next;var f=filter||function(){return true;}
while(node!=null){if(node.hasChildNodes())
node=node.firstChild;else if(node!=root&&null!=(next=node.nextSibling))
node=next;else{next=null;for(;node!=root;node=node.parentNode){next=node.nextSibling;if(next!=null)break;}
node=next;}
if(node!=null&&f(node,root))
nodes.push(node);}
return nodes;},_childElements:function(node,root){return(node.nodeType==1&&node.parentNode==root);},_links:function(node){return(node.tagName=='A');},ERROR_MSG:this.DEFAULT_ERROR_MSG,_addMenu:function(node){this._addPane(node);},_prevMenu:function(menu){return this._prevPane(menu);},_removeMenus:function(target){this._removePanes(target);},_removeLastMenu:function(){this._removeLastPane();}};YAHOO.extension.ColumnNav=YAHOO.extension.ColumNav;