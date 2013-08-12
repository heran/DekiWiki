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
 * @file Keystrokes plugin.
 */

(function()
{
	var pluginName = 'mindtouchkeystrokes';

	var format = function( tag )
	{
		var editor = this;
		
		editor.fire( 'saveSnapshot' );
		var style = new CKEDITOR.style( editor.config[ 'format_' + tag ] );
		style.apply( editor.document );
		editor.fire( 'saveSnapshot' );
	};
	
	var tab = function( keyCode )
	{
		var editor = this,
			hasShift =  ( keyCode == CKEDITOR.SHIFT + 9 ),
			forceTab = ( keyCode == CKEDITOR.CTRL + 9 );

		var selection = editor.getSelection(),
			range = selection && selection.getRanges( true )[0];

		if ( !range )
			return false;

		var tabSpaces = editor.config.tabSpaces,
			tabText = '';

		while ( tabSpaces-- )
			tabText += '\xa0';

		var element = range.startContainer;
		
		// we need to indent/outdent list items inside table cells
		// instead of jump to the next cell
		// @see #0007861
		var isListItem = false;

		while ( element )
		{
			if ( element.type == CKEDITOR.NODE_ELEMENT )
			{
				if ( element.is( 'td', 'th' ) )
				{
					if ( isListItem )
					{
						var isStartOfBlock = range.checkStartOfBlock();
						if ( isStartOfBlock || ( !isStartOfBlock && forceTab ) )
						{
							break; // while
						}
					}

					var nextCell = hasShift ? element.getPrevious( cells ) : element.getNext( cells );

					if ( nextCell == null )
					{
						var row = hasShift ? element.getParent().getPrevious( rows ) : element.getParent().getNext( rows );

						if ( row )
						{
							nextCell = hasShift ? row.getLast( cells ) : row.getFirst( cells ) ;
						}
					}

					if ( nextCell == null )
					{
						if ( hasShift )
						{
							break; // while
						}

						editor.fire( 'saveSnapshot' );

						var table = element.getAscendant( 'table' ).$,
							cells = element.getParent().$.cells,
							rows = table.rows;

						var newRow = new CKEDITOR.dom.element( table.insertRow( -1 ), editor.document ),
							i, count;

						for ( i = 0, count = cells.length ; i < count; i++ )
						{
							var newCell = newRow.append( new CKEDITOR.dom.element(
									cells[ i ], editor.document ).clone( false, false ) );

							!CKEDITOR.env.ie && newCell.appendBogus();

							if ( i == 0 )
							{
								nextCell = newCell;
							}
						}

						// @todo add the new row with respect to merged cells
						// @link {http://youtrack.developer.mindtouch.com/issue/MT-8028}

						editor.fire( 'saveSnapshot' );
					}

					if ( nextCell != null )
					{
						range.moveToElementEditStart( nextCell );
						range.select();
						return true;
					}
				}
				else if ( element.is( 'tr', 'tbody', 'table' ) )
				{
					return true;
				}
				else if ( element.is( 'dt' ) || ( element.is( 'dd' ) && hasShift ) )
				{
					var bookamrks = selection.createBookmarks();

					element.renameNode( element.getName() == 'dt' ? 'dd' : 'dt' );

					selection.selectBookmarks( bookamrks );
					return true;
				}
				else if ( !hasShift && element.is( 'pre' ) && tabText.length )
				{
					editor.fire( 'saveSnapshot' );

					var tabNode = new CKEDITOR.dom.text( tabText, editor.document );
					range.deleteContents();
					range.insertNode( tabNode );
					range.setStartAt( tabNode, CKEDITOR.POSITION_BEFORE_END );
					range.collapse( true );
					range.select();

					editor.fire( 'saveSnapshot' );

					return true;
				}
				else if ( element.is( 'li' ) )
				{
					isListItem = true;
				}
			}

			element = element.getParent();
		}

		if ( !hasShift )
		{
			var startNode = range.getCommonAncestor( true, true ),
				nextNode;

			if ( startNode && startNode.is( 'h1', 'h2', 'h3', 'h4', 'h5', 'h6' ) )
			{
				nextNode = startNode.getNextSourceNode( false, CKEDITOR.NODE_ELEMENT );

				if ( !nextNode )
				{
					nextNode = new CKEDITOR.dom.element( editor.config.enterMode == CKEDITOR.ENTER_DIV ? 'div' : 'p', editor.document );
					nextNode.insertAfter( startNode );
				}

				range.moveToElementEditStart( nextNode );
				range.select();
				return true;
			}

			if ( tabText.length && !range.checkStartOfBlock() )
			{
				editor.insertHtml( tabText );
				return true;
			}
		}
		
		editor.execCommand( hasShift ? 'outdent' : 'indent' );

		return true;
	};
	
	var cells = function( isReject )
	{
		return function( node )
		{
			var isCell = node && node.is( 'td', 'th' );
			return isReject ^ isCell;
		};
	};
	
	var rows = function( isReject )
	{
		return function( node )
		{
			var isRow = node && node.getName() == 'tr';
			return isReject ^ isRow;
		};
	};
	
	var togglelist = function()
	{
		var editor = this,
			commandName;

		if ( editor.getCommand( 'bulletedlist' ).state == CKEDITOR.TRISTATE_ON )
		{
			commandName = 'numberedlist';
		}
		else
		{
			if ( editor.getCommand( 'numberedlist' ).state == CKEDITOR.TRISTATE_ON )
			{
				var path = new CKEDITOR.dom.elementPath( editor.getSelection().getStartElement() );

				commandName = 'numberedlist';

				var listsCount = 0,
					element,
					name;

				for ( var i = 0 ; i < path.elements.length ; i++ )
				{
					element = path.elements[i];

					if ( element.is( 'ul', 'ol' ) )
					{
						listsCount++;
					}

					if ( listsCount > 1 )
					{
						commandName = 'bulletedlist';
					}
				}
			}
			else
			{
				commandName = 'bulletedlist';
			}
		}

		if ( editor.getCommand( commandName ).state != CKEDITOR.TRISTATE_DISABLED )
		{
			editor.execCommand( commandName );
			return true;
		}

		return false;
	};
	
	var autoreplace =
	{
		exec : function( editor, forceMode )
		{
			this.editor = editor;

			forceMode = editor.config.forceEnterMode || forceMode;

			var mode = forceMode ? editor.config.shiftEnterMode : editor.config.enterMode;

			if ( mode == CKEDITOR.ENTER_BR )
			{
				return false;
			}

			var selection = editor.getSelection(),
				ranges = selection && selection.getRanges( true ),
				range = ranges && ranges[0];
	
			if ( !range || !range.collapsed )
				return false;

			var startNode = range.getCommonAncestor( true, true );

			if ( startNode.is( 'pre' ) )
				return false;

			editor.fire( 'saveSnapshot' );

			var bookmarks = selection && selection.createBookmarks( true );

			this.replace( startNode, '~~~~', this.getNodesToReplace( true ) );
			this.replace( startNode, '~~~', this.getNodesToReplace( false ) );

			// hack for undo in FF
			if ( CKEDITOR.env.gecko )
				startNode.setHtml( startNode.getHtml() );
			
			selection.selectBookmarks( bookmarks );
			editor.fire( 'saveSnapshot' );
			
			return false;
		},
		
		replace : function( startNode, text, nodes )
		{
			var textNode = startNode;
			
			while ( textNode = textNode.getNextSourceNode( false, CKEDITOR.NODE_TEXT, startNode ) )
			{
				var elementPath = new CKEDITOR.dom.elementPath( textNode ),
					isPlain = false,
					i;
				
				for ( i = 0 ; i < elementPath.elements.length ; i++ )
				{
					if ( elementPath.elements[i].hasClass( 'plain' ) )
					{
						isPlain = true;
						break;
					}
				}
				
				if ( isPlain )
					continue;
				
				var pos = textNode.getText().indexOf( text );
				
				if ( pos > -1 )
				{
					var range = new CKEDITOR.dom.range( this.editor.document );
					range.setStart( textNode, pos );
					range.setEnd( textNode, pos + text.length );
					
					textNode = this.replaceRange( range, nodes );
				}
			}
		},
		
		replaceRange : function( range, newNodes )
		{
			range.select();
			range.deleteContents();
			
			var node;
			
			for ( var i in newNodes )
			{
				node = newNodes[i];
				range.insertNode( node );
				range.moveToPosition( node, CKEDITOR.POSITION_AFTER_END );
			}
			
			return node;
		},
		
		getNodesToReplace : function( withDate )
		{
			var nodes = [],
				mindtouchLink = CKEDITOR.plugins.get( 'mindtouchlink' ),
				href = mindtouchLink.internalPrefix + 'User:' + this.editor.config.mindtouch.userName;

			var userLink = this.editor.document.createElement( 'a' );
			userLink.addClass( 'internal' );
			userLink.setAttributes(
				{
					title : 'User:' + this.editor.config.mindtouch.userName,
					href : href
				});
			userLink.data( 'cke-saved-href', href );
			
			userLink.setText( this.editor.config.mindtouch.userName );
			
			nodes.push( userLink );
			
			if ( withDate )
			{
				var date = this.editor.document.createText( ' ' + this.editor.config.mindtouch.today );
				nodes.push( date );
			}
			
			return nodes;
		},
		
		canUndo : false
	};

	var notBr = function( node ) {return !( node.is && node.is( 'br' ) )};

	var filterBr = function( node )
	{
		if ( node && node.is && node.is( 'br' ) )
		{
			node.remove();
		}

		return node;
	};

	var backspace = function()
	{
		var editor = this;

		var selection = editor.getSelection(),
			ranges = selection && selection.getRanges( true ),
			range = ranges && ranges[0];

		if ( !range || !range.collapsed )
			return false;

		var currentLi = range.startContainer.getAscendant( 'li', true );

		if ( currentLi && range.checkStartOfBlock() )
		{
			var firstListElement = getAscendantListElement( currentLi );

			var walkerRange = range.clone();
			walkerRange.setStartAt( firstListElement, CKEDITOR.POSITION_AFTER_START );

			var walker = new CKEDITOR.dom.walker( walkerRange );
			walker.evaluator = function( node )
			{
				return ( node.type == CKEDITOR.NODE_TEXT )
						|| ( node.is && node.is( 'li' ) && node.getChildCount() == 0 )
						|| ( node.is && node.is( 'br' ) && node.getParent().is( 'li' ) && node.getParent().getFirst().equals( node ) );
			};

			var prev = walker.previous();

			if ( prev )
			{
				editor.fire( 'saveSnapshot' );

				if ( prev.is && prev.is( 'br' ) )
				{
					prev = prev.getParent();
				}

				if ( prev.is && prev.is( 'li' ) )
				{
					range.setStartAt( prev, CKEDITOR.POSITION_AFTER_START );
				}
				else
				{
					range.setStartAt( prev, CKEDITOR.POSITION_BEFORE_END );
				}

				range.collapse( true );
				range.select();

				filterBr( currentLi.getLast() );
				filterBr( prev.getAscendant( 'li', true ).getLast() );
				filterBr( prev.getNext() );

				moveListItemToRange( currentLi, range );

				range.collapse( true );
				range.select();

				editor.fire( 'saveSnapshot' );
				return true;
			}
		}

		// webkit does not merge pre blocks on backspace
		// @see #0007666
		if ( CKEDITOR.env.webkit )
		{
			var path = new CKEDITOR.dom.elementPath( range.startContainer );

			if ( path && path.block && path.block.is( 'pre' ) && range.checkStartOfBlock() )
			{
				var previousBlock;
				if ( !( ( previousBlock = path.block.getPreviousSourceNode( true, CKEDITOR.NODE_ELEMENT ) )
						 && previousBlock.is
						 && previousBlock.is( 'pre') ) )
				{
					return false;
				}

				editor.fire( 'saveSnapshot' );

				range.setStartAt( previousBlock, CKEDITOR.POSITION_BEFORE_END );
				range.collapse( true );
				range.select();
				
				var bookmarks = selection.createBookmarks( true );
				mergePres( path.block, previousBlock );
				selection.selectBookmarks( bookmarks );

				editor.fire( 'saveSnapshot' );

				return true;
			}
		}

		return false;
	};

	var del = function()
	{
		var editor = this;

		var selection = editor.getSelection(),
			ranges = selection && selection.getRanges( true ),
			range = ranges && ranges[0];

		if ( !range || !range.collapsed )
			return false;

		var currentLi = range.startContainer.getAscendant( 'li', true );

		if ( !currentLi )
			return false;

		editor.fire( 'saveSnapshot' );

		var bookmark, next;

		if ( !range.checkEndOfBlock() )
		{
			bookmark = range.createBookmark();
			next = bookmark.startNode.getNext( notBr );

			if ( !next.is || !next.is( 'ul', 'ol' ) )
			{
				range.moveToBookmark( bookmark );
				range.select();
				return false;
			}

			if ( !range.checkStartOfBlock() )
			{
				filterBr( next.getPrevious() );
			}
		}

		var firstListElement = getAscendantListElement( currentLi );

		var walkerRange = range.clone();
		walkerRange.setEndAt( firstListElement, CKEDITOR.POSITION_BEFORE_END );

		var walker = new CKEDITOR.dom.walker( walkerRange );
		walker.evaluator = function( node )
		{
			return node.is && node.is( 'li' );
		};

		var nextLi = walker.next();

		if ( nextLi )
		{
			filterBr( currentLi.getLast() );
			filterBr( nextLi.getLast() );
			moveListItemToRange( nextLi, range );
		}

		bookmark ? range.moveToBookmark( bookmark ) : range.collapse( true );
		range.select();

		editor.fire( 'saveSnapshot' );
		return true;
	};

	function moveListItemToRange( li, range )
	{
		var children = li.getChildren(),
			count = children.count(),
			i, child;

		for ( i = count - 1 ; i >= 0 ; i-- )
		{
			child = children.getItem( i );

			// skip last br
			if ( child.is && child.is( 'br' ) && child.getParent().is( 'li' ) && child.getParent().getLast().equals( child ) )
			{
				continue;
			}

			range.insertNode( children.getItem( i ) );
		}

		if ( li.getParent().getChildCount() == 1 )
		{
			li.getParent().remove();
		}
		else
		{
			li.remove();
		}
	}

	function getAscendantListElement( node )
	{
		var listElement = null;
		
		while ( node )
		{
			if ( node.is && node.is( 'ul', 'ol' ) )
			{
				listElement = node;
			}

			node = node.getParent();
		}

		return listElement;
	}

	/**
	 * Merge a <pre> block with a previous sibling if available.
	 *
	 * from style plug-in
	 */
	function mergePres( preBlock, previousBlock )
	{
		// Merge the previous <pre> block contents into the current <pre>
		// block.
		//
		// Another thing to be careful here is that currentBlock might contain
		// a '\n' at the beginning, and previousBlock might contain a '\n'
		// towards the end. These new lines are not normally displayed but they
		// become visible after merging.
		var mergedHtml = replace( previousBlock.getHtml(), /(\n|<br\s*\/*>)$/i, '' ) +
				preBlock.getHtml() ;

		previousBlock.setHtml( mergedHtml );
		preBlock.remove();
	}

	// Wrapper function of String::replace without considering of head/tail bookmarks nodes.
	function replace( str, regexp, replacement )
	{
		var headBookmark = '',
			tailBookmark = '';

		str = str.replace( /(^<span[^>]+data-cke-bookmark.*?\/span>)|(<span[^>]+data-cke-bookmark.*?\/span>$)/gi,
			function( str, m1, m2 ){
					m1 && ( headBookmark = m1 );
					m2 && ( tailBookmark = m2 );
				return '';
			} );
		return headBookmark + str.replace( regexp, replacement ) + tailBookmark;
	}

	var moveCursor = function( arrowKeyCode )
	{
		var editor = this;

		var selection = editor.getSelection(),
			range = selection && selection.getRanges( true )[0];

		if ( !range )
		{
			return false;
		}

		var getCell = function( range )
		{
			var node = range.startContainer;
			while ( node )
			{
				if ( node.is && node.is( 'td', 'th' ) )
				{
					break;
				}

				node = node.getParent();
			}

			return node;
		};

		// override up/down cursor behaviour only for table cells
		var cell = getCell( range );
		if ( !cell )
		{
			return false;
		}

		// we need to keep offset to emulate bevaiour of other browsers
		var offset;
		if ( range.startContainer.type == CKEDITOR.NODE_TEXT )
		{
			offset = range.startOffset;
		}

		window.setTimeout( function()
			{
				var selection = editor.getSelection(),
					range = selection && selection.getRanges( true )[0];

				if ( !range )
				{
					return false;
				}

				var newCell = getCell( range );

				// if cursor was not moved out of the cell
				if ( !newCell || newCell.equals( cell ) )
				{
					return false;
				}

				var row = cell.getParent(),
					siblingRow;

				if ( arrowKeyCode == 38 )
				{
					siblingRow = row && row.getPrevious( rows );
				}
				else if ( arrowKeyCode == 40 )
				{
					siblingRow = row && row.getNext( rows );
				}
				else
				{
					return false;
				}

				// if we are in the last row
				if ( !siblingRow )
				{
					var table = row.getAscendant( 'table' ),
						next = table && table[ ( arrowKeyCode == 40 ) ? 'getNext' : 'getPrevious' ]();

					if ( next )
					{
						range.moveToElementEditStart( next );
						range.collapse( true );
						range.select();
					}

					return true;
				}

				var targetCell = new CKEDITOR.dom.element( siblingRow.$.cells[ cell.$.cellIndex ] );

				// we don't need do anything for tables with one column
				if ( newCell.equals( targetCell ) )
				{
					return false;
				}

				// restoring cursor offset
				if ( !isNaN( offset ) )
				{
					var textNode = targetCell[ ( arrowKeyCode == 40 ) ? 'getFirst' : 'getLast']();

					if ( textNode && textNode.type !== CKEDITOR.NODE_TEXT )
					{
						var children = textNode.getChildren();
						for ( var i = 0 ; i < children.count() ; i++ )
						{
							var child = children.getItem( i );
							if ( child.type === CKEDITOR.NODE_TEXT )
							{
								textNode = child;
								break;
							}
						}
					}

					if ( textNode && textNode.type === CKEDITOR.NODE_TEXT && textNode.getLength() >= offset )
					{
						range.setStart( textNode, offset );
						range.collapse( true );
						range.select();
						return true;
					}
				}

				range.moveToElementEditStart( targetCell );
				range.select();
				return true;

			}, 0 );
	};

	CKEDITOR.plugins.add( pluginName,
	{
		requires : [ 'keystrokes', 'mindtouchlink' ],
		
		init : function( editor )
		{
			editor.addCommand( 'autoreplace', autoreplace );

			editor.on( 'key', function( evt )
				{

					var editor = evt.editor,
						keyCode = evt.data.keyCode;

					switch ( keyCode )
					{
						case 9 /* TAB */:
						case CKEDITOR.SHIFT + 9:
						case CKEDITOR.CTRL + 9:
							if ( tab.call( editor, keyCode ) )
							{
								evt.cancel();
							}
							break;
						case 38 /* UP */:
						case 40 /* DOWN */:
							// @see #0007860
							CKEDITOR.env.webkit && moveCursor.call( editor, keyCode );
							break;
						case CKEDITOR.CTRL + CKEDITOR.SHIFT + 76 /* L */:
							togglelist.call( editor );
							evt.cancel();
							break;
						case 13 /* ENTER */:
						case CKEDITOR.SHIFT + 13:
							var forceMode = ( keyCode == CKEDITOR.SHIFT + 13 );
							editor.execCommand( 'autoreplace', forceMode );
							break;
						case 8 /* BACKSPACE */:
							if ( backspace.call( editor ) )
							{
								evt.cancel();
							}
							break;
						case 46 /* DEL */:
							if ( del.call( editor ) )
							{
								evt.cancel();
							}
							break;
						case CKEDITOR.CTRL + 49 /* 1 */ :
						case CKEDITOR.CTRL + 50 /* 2 */ :
						case CKEDITOR.CTRL + 51 /* 3 */ :
						case CKEDITOR.CTRL + 52 /* 4 */ :
						case CKEDITOR.CTRL + 53 /* 5 */ :
							var tag = 'h' + ( keyCode - CKEDITOR.CTRL - 47 ) ; // h2 - h6
							format.call( editor, tag );
							evt.cancel();
							break;
						case CKEDITOR.CTRL + 48 /* 0 */ :
						case CKEDITOR.CTRL + 78 /* N */ :
							var tag = editor.config.enterMode == CKEDITOR.ENTER_DIV ? 'div' : 'p';
							format.call( editor, tag );
							evt.cancel();
							break;
					}

				}, null, null, 1 );
		}
	});
})();
