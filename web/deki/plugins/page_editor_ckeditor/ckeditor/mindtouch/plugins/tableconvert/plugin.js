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

(function()
{
	var textToTableCmd =
	{
		exec : function( editor )
		{
			var selection = editor.getSelection(),
				range = selection && selection.getRanges( true )[0];

			if ( !range )
				return;

			editor.fire( 'saveSnapshot' );

			var iterator = range.createIterator(),
				node, block, paragraphs = [];

			while ( ( block = iterator.getNextParagraph() ) )
			{
				paragraphs.push( block );
			}

			if ( paragraphs.length && paragraphs[ paragraphs.length - 1 ].hasNext() )
			{
				node = paragraphs[ paragraphs.length - 1 ].getNext();
			}

			var table = editor.document.createElement( 'table' );
			table.setAttribute( 'cellSpacing', 1 );
			table.setAttribute( 'cellPadding', 1 );
			table.setAttribute( 'border', 1 );

			if ( node )
			{
				table.insertBefore( node );
			}
			else
			{
				editor.document.getBody().append( table );
			}


			var i, cell, ranges = [];

			selection.reset();

			for ( i = 0 ; i < paragraphs.length ; i++ )
			{
				cell = new CKEDITOR.dom.element( table.$.insertRow(-1).insertCell(-1) );
				cell.setHtml( paragraphs[i].remove().getHtml() );

				range = new CKEDITOR.dom.range( editor.document );
				range.setStartAt( cell, CKEDITOR.POSITION_AFTER_START );
				range.setEndAt( cell, CKEDITOR.POSITION_BEFORE_END );

				ranges.push( range );
			}

			selection.selectRanges( ranges );

			// Save the undo snapshot after all changes are affected.
			setTimeout( function()
			{
				editor.fire( 'saveSnapshot' );
			}, 0 );

			editor.selectionChange();
			editor.focus();
		},

		canUndo : false
	};

	CKEDITOR.plugins.add( 'tableconvert',
	{
		lang : [ 'en' ], // @Packager.RemoveLine

		init : function( editor )
		{
			editor.addCommand( 'tableToText', new CKEDITOR.dialogCommand( 'tableToText' ) );
			editor.addCommand( 'textToTable', textToTableCmd );

			CKEDITOR.dialog.add( 'tableToText', this.path + 'dialogs/totext.js' );

			// If the "menu" plugin is loaded, register the menu items.
			if ( editor.addMenuItems )
			{
				editor.addMenuItems(
					{
						tabletotext :
						{
							label : editor.lang.tableconvert.toText,
							command : 'tableToText',
							group : 'table',
							order : 3
						},

						texttotable :
						{
							label : editor.lang.tableconvert.toTable,
							command : 'textToTable',
							group : 'table',
							order : 3
						}
					} );
			}

			// If the "contextmenu" plugin is loaded, register the listeners.
			if ( editor.contextMenu )
			{
				editor.contextMenu.addListener( function( element, selection )
					{
						if ( !element || element.isReadOnly() )
							return null;

						var isTable	= element.is( 'table' ) || element.hasAscendant( 'table' );

						if ( isTable )
						{
							return {
								tabletotext : CKEDITOR.TRISTATE_OFF
							};
						}

						return {
							texttotable : CKEDITOR.TRISTATE_OFF
						};
					} );
			}
		}
	} );
})();