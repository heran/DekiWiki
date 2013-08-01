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
	function tableToTextDialog( editor )
	{
		return {
			title : editor.lang.tableconvert.toText,
			minWidth : 300,
			minHeight : CKEDITOR.env.ie ? 80 : 50,
			onShow : function()
			{
				// Detect if there's a selected table.
				var selection = editor.getSelection(),
					ranges = selection.getRanges( true ),
					selectedTable = null;

				if ( ( selectedTable = editor.getSelection().getSelectedElement() ) )
				{
					if ( selectedTable.getName() != 'table' )
						selectedTable = null;
				}
				else if ( ranges.length > 0 )
				{
					var rangeRoot = ranges[0].getCommonAncestor( true );
					selectedTable = rangeRoot.getAscendant( 'table', true );
				}

				// Save a reference to the selected table
				this._.selectedElement = selectedTable;
			},
			onOk : function()
			{
				var table = this._.selectedElement;

				var selection = editor.getSelection(),
					range = selection && selection.getRanges( true )[0];

				if ( !range )
				{
					return false;
				}

				var rows = table.$.rows,
					paragraphs = [], p, rowText, i;

				var separator = this.getContentElement( 'convert', 'selSeparateAt' ).getValue(),
					otherChar = this.getContentElement( 'convert', 'txtOther' ).getValue();

				for ( i = 0 ; i < rows.length ; i++ )
				{
					for ( var j = 0 ; j < rows[i].cells.length ; j++ )
					{
						if ( separator == "paragraph" || j == 0 )
						{
							p = editor.document.createElement( 'p' );
							rowText = '';
						}

						var cell = rows[i].cells[j];

						if ( j > 0 && separator != "paragraph" )
						{
							switch ( separator )
							{
								case "tabs":
									rowText += "&nbsp;&nbsp;&nbsp;&nbsp;";
									break;
								case "semicolons":
									rowText += ";";
									break;
								default:
									rowText += otherChar;
									break;
							}
						}

						rowText += cell.innerHTML;

						if ( separator == "paragraph" || j == rows[i].cells.length - 1 )
						{
							p.setHtml( rowText);
							paragraphs.push( p );
						}
					}
				}

				var node = table;

				for ( i = 0 ; i < paragraphs.length ; i++ )
				{
					paragraphs[i].insertAfter( node );
					node = paragraphs[i];
				}

				table.remove();

				range.setStartAt( paragraphs[0], CKEDITOR.POSITION_AFTER_START );
				range.setEndAt( node, CKEDITOR.POSITION_BEFORE_END );

				selection.selectRanges( [ range ] );

				return true;
			},
			contents : [
				{
					id : 'convert',
					label : editor.lang.tableconvert.toText,
					elements :
					[
						{
							type : 'hbox',
							widths : [ '80%', '20%' ],
							styles : [ 'vertical-align:top' ],
							children :
							[
								{
									type : 'select',
									id : 'selSeparateAt',
									'default' : 'tabs',
									label : editor.lang.tableconvert.separateAt,
									labelLayout : 'horizontal',
									widths : [ '50%', '50%' ],
									items :
									[
										[ editor.lang.tableconvert.tabs, 'tabs' ],
										[ editor.lang.tableconvert.semicolons, 'semicolons' ],
										[ editor.lang.tableconvert.paragraphs, 'paragraphs' ],
										[ editor.lang.tableconvert.other, 'other' ]
									],
									onChange : function()
									{
										var other = this.getDialog().getContentElement( 'convert', 'txtOther' );

										if ( this.getValue() == 'other' )
										{
											other.getElement().show();
											other.focus();
										}
										else
										{
											other.getElement().hide();
										}
									}
								},
								{
									type : 'text',
									id : 'txtOther',
									label : '',
									style : 'width:5em',
									setup : function()
									{
										this.getElement().hide();
									}
								}
							]
						}
					]
				}
			]
		};
	}

	CKEDITOR.dialog.add( 'tableToText', function( editor )
		{
			return tableToTextDialog( editor );
		} );
})();
