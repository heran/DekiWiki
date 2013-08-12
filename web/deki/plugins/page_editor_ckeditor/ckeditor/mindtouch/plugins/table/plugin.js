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

CKEDITOR.plugins.add( 'tableadvanced',
{
	requires : [ 'styles' ],

	lang : [ 'en' ], // @Packager.RemoveLine

	beforeInit: function( editor )
	{
		CKEDITOR.dialog.add( 'table', this.path + 'dialogs/table.js', true );
		CKEDITOR.dialog.add( 'tableProperties', this.path + 'dialogs/table.js', true );

		CKEDITOR.dialog.add( 'cellProperties', this.path + 'dialogs/tableCell.js', true );
	},
	
	init : function( editor )
	{
		editor.addCommand( 'rowProperties', new CKEDITOR.dialogCommand( 'rowProperties' ) );
		CKEDITOR.dialog.add( 'rowProperties', this.path + 'dialogs/tableRow.js' );

		CKEDITOR.tools.extend( editor.lang.table, editor.lang.tableadvanced );
		CKEDITOR.tools.extend( editor.lang.table.cell, editor.lang.tableadvanced.cell );
		CKEDITOR.tools.extend( editor.lang.table.row, editor.lang.tableadvanced.row );

		var lang = editor.lang.table;

		if ( editor.addMenuItems )
		{
			editor.addMenuGroup( 'tablerowproperties' );

			editor.addMenuItems(
				{
					tablerow :
					{
						label : lang.row.menu,
						group : 'tablerow',
						order : 1,
						getItems : function()
						{
							return {
								tablerow_insertBefore : CKEDITOR.TRISTATE_OFF,
								tablerow_insertAfter : CKEDITOR.TRISTATE_OFF,
								tablerow_delete : CKEDITOR.TRISTATE_OFF,
								tablerow_properties : CKEDITOR.TRISTATE_OFF
							};
						}
					},

					tablerow_properties :
					{
						label : lang.row.title,
						group : 'tablerowproperties',
						command : 'rowProperties',
						order : 20
					}
				} );
		}
	}
} );
