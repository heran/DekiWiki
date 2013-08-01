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
 * @file Extensions plugin.
 */

(function()
{
	var pluginName = 'extensions';

	var extensionsCmd =
	{
		canUndo: false,
		
		exec : function( editor )
		{
			this.editor = editor;
			
			var selection = editor.getSelection(),
				range = selection && selection.getRanges( true )[0],
				selectedText = '';
		
			if ( range && !range.collapsed )
			{
				selection.lock();
				selectedText = range.cloneContents().getFirst().getText();
				selection.unlock( true );
			}
			
			var params =
				{
					'sSelection' : selectedText,
					'elParent'   : selection.getStartElement().getParent()
				};
		
			var mindtouchDialog = CKEDITOR.plugins.get( 'mindtouchdialog' );
			mindtouchDialog.openDialog( editor,  pluginName,
				{
					url: editor.config.mindtouch.commonPath + '/popups/extension_dialog.php',
					width: '700px',
					height: '400px',
					params: params,
					callback: this._.insertExtension,
					scope: this
				});
		},
		
		_ :
		{
			insertExtension : function( params )
			{
				var editor = this.editor;
				
				if ( params.sDekiScript )
				{
					editor.insertHtml( CKEDITOR.tools.htmlEncode( params.sDekiScript ) );
				}
			}
		}
	};
	
	CKEDITOR.plugins.add( pluginName,
	{
		requires : [ 'mindtouchdialog', 'selection' ],
		
		lang : [ 'en' ], // @Packager.RemoveLine
		
		init : function( editor )
		{
			// Register the command.
			editor.addCommand( pluginName, extensionsCmd );
	
			// Register the toolbar button.
			editor.ui.addButton( 'Extensions',
				{
					label : editor.lang.extensions.toolbar,
					command : pluginName,
					icon : editor.config.mindtouch.editorPath + '/images/icons.png',
					iconOffset : 9
				});	
		}
	});
})();
