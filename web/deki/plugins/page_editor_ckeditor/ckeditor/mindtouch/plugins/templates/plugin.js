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
 * @file Templates plugin.
 */

(function()
{
	var pluginName = 'mindtouchtemplates';

	var templatesCmd =
	{
		canUndo: false,
		
		exec : function( editor )
		{
			this.editor = editor;
			
			var params =
				{
					'contextTopicID' : editor.config.mindtouch.pageId
				};
		
			var mindtouchDialog = CKEDITOR.plugins.get( 'mindtouchdialog' );
			mindtouchDialog.openDialog( editor, pluginName,
				{
					url: editor.config.mindtouch.commonPath + '/popups/select_template.php',
					width: '400px',
					height: '110px',
					params: params,
					callback: this._.insertTemplate,
					scope: this
				});
		},
		
		_ :
		{
			insertTemplate : function( params )
			{
				var editor = this.editor;
				
				if ( params.f_template )
				{
					editor.insertHtml( params.f_template );
				}
			}
		}
	};
	
	CKEDITOR.plugins.add( pluginName,
	{
		requires : [ 'mindtouchdialog' ],

		lang : [ 'en' ], // @Packager.RemoveLine
		
		init : function( editor )
		{
			// Register the command.
			editor.addCommand( pluginName, templatesCmd );
	
			// Register the toolbar button.
			editor.ui.addButton( 'MindTouchTemplates',
				{
					label : editor.lang.mindtouchtemplates.button,
					command : pluginName,
					icon : editor.config.mindtouch.editorPath + '/images/icons.png',
					iconOffset : 16
				});	
		}
	});
})();
