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
 * Wrapper for CKEditor for convenient text manipulation
 * @class
 *
 * @include "EventDispatcher.js"
 */
(function()
{
	/**
	 * Object constructor
	 * @class
	 * @param {Object} CKEditor instance
	 */
	CKEditorViewer = function( editor )
	{
		this.editor = editor;
		this.dispatcher = new EventDispatcher();

		this.currentTextNode = null;
		this.nextTextNode = null;

		this.documentPosition = null;
		this.cursorPosition = null;
	};

	CKEditorViewer.prototype =
	{

		getCurrentText : function()
		{
			var editor = this.editor,
				selection = editor.getSelection(),
				range = selection && selection.getRanges( 1 )[ 0 ];

			// work with collapsed ranges only
			if ( !range || !range.collapsed )
			{
				return '';
			}

			var marker = editor.document.createElement( 'span' );
			marker.addClass( 'cke_codeassist' );

			range.insertNode( marker );
			range.moveToPosition( marker, CKEDITOR.POSITION_AFTER_END );

			// join all previous text nodes into one
			var node = marker,
				textNode;

			while ( ( node = node.getPrevious() ) && node.type === CKEDITOR.NODE_TEXT )
			{
				if ( textNode )
				{
					node.$.nodeValue += textNode.getText();
					textNode.remove();
				}

				textNode = node;
			}

			this.currentTextNode = textNode;
			this.cursorPosition = marker.getDocumentPosition( editor.document );

			range.setStartBefore( marker );
			marker.remove();
			range.collapse( 1 );
			range.select();

			return textNode ? textNode.getText() : '';
		},

		getNextText : function()
		{
			if ( !this.currentTextNode )
			{
				return '';
			}

			// join all next text nodes into one
			var node = this.currentTextNode,
				textNode;

			while ( ( node = node.getNext() ) && node.type === CKEDITOR.NODE_TEXT )
			{
				if ( textNode )
				{
					node.$.nodeValue = textNode.getText() + node.getText();
					textNode.remove();
				}

				textNode = node;
			}

			this.nextTextNode = textNode;

			return textNode ? textNode.getText() : '';
		},

		getCaretPos : function() {},
		setCaretPos : function() {},

		/**
		 * Returns absolute character coordinates.
		 * You can use it to position popup element
		 * @return {Object} Object with <code>x</code> and <code>y</code> properties
		 */
		getAbsoluteCharacterCoords: function()
		{
			if ( !this.documentPosition )
			{
				this.documentPosition = this.editor.document.getDocumentElement().getDocumentPosition( CKEDITOR.document );
			}

			if ( !this.cursorPosition )
			{
				this.cursorPosition = {x : 0, y : 0};
			}

			var coords =
				{
					x : this.documentPosition.x + this.cursorPosition.x,
					y : this.documentPosition.y + this.cursorPosition.y
				};

			return coords;
		},

		/**
		 * @return {Element}
		 */
		getElement: function()
		{
			return this.editor.container.$;
		},

		/**
		 * Replaces text substring with new value
		 * @param {String} text
		 * @param {Number} start
		 * @param {Number} end
		 */
		replaceText: function( text, start, end )
		{
			if ( !this.currentTextNode )
			{
				return;
			}

			this.editor.fire( 'saveSnapshot' );

			var has_start = ( typeof start != 'undefined' ),
				has_end = ( typeof end != 'undefined' ),
				cur_text = this.currentTextNode.getText();

			if ( !has_start && !has_end )
			{
				start = 0;
				end = cur_text.length;
			}
			else if ( !has_end )
			{
				end = start;
			}

			var len = end - start, // length of text to replace
				cur_word_len = cur_text.length - start, // length of text to replace before cursor
				next_word_len = len - cur_word_len, // length of text to replace after cursor
				new_text = cur_text.substring( 0, start ) + text;

			if ( next_word_len && this.nextTextNode )
			{
				this.nextTextNode.$.nodeValue = this.nextTextNode.getText().substring( next_word_len );
			}

			this.currentTextNode.$.nodeValue = new_text;

			var range = new CKEDITOR.dom.range( this.editor.document );
			range.setStart( this.currentTextNode, new_text.length );
			range.collapse( 1 );

			var selection = this.editor.getSelection();
			selection && selection.selectRanges( [ range ] );

			this.editor.fire( 'saveSnapshot' );
		},

		addEvent: function( type, fn )
		{
			var items = type.split( /\s+/ ),
				elem = this.editor.document.$;

			for ( var i = 0, il = items.length; i < il; i++ )
			{
				switch ( items[i].toLowerCase() )
				{
					case 'modify':
						this.dispatcher.addEventListener( 'modify', fn );
						break;
					case 'keypress':
					case 'keydown':
						// use CKEditor key event to handle these events
						break;
					default:
						tx_utils.addEvent( elem, type, fn );
						break;
				}
			}
		},

		removeEvent: function( type, fn )
		{
			var items = type.split( /\s+/ ),
				elem = this.editor.document.$;

			for ( var i = 0, il = items.length; i < il; i++ )
			{
				if ( items[i].toLowerCase() == 'modify' )
				{
					this.dispatcher.removeEventListener( 'modify', fn  );
				}
				else
				{
					tx_utils.removeEvent( elem, type, fn );
				}
			}
		}
	};
})();
