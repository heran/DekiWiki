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
 * @file Video plugin.
 */

(function()
{
	var pluginName = 'video';

	var videoCmd =
	{
		canUndo: false,
		
		exec : function( editor )
		{
			this.editor = editor;
			
			var mindtouchDialog = CKEDITOR.plugins.get( 'mindtouchdialog' );
			mindtouchDialog.openDialog( editor, pluginName,
				{
					url: editor.config.mindtouch.commonPath + '/popups/popup-video.php',
					width: '400px',
					height: 'auto',
					params: null,
					callback: this._.insertVideo,
					scope: this
				});
		},
		
		_ :
		{
			insertVideo : function( params )
			{
				var editor = this.editor;
				
				if ( params.f_content )
				{
					var args = [
						'source: "' + params.f_content + '"'
					];

					if ( params.f_width.length )
					{
						args.push( 'width: "' + params.f_width + '"' );
					}
					else if ( params.f_height.length )
					{
						args.push( 'width: nil' );
					}

					if ( params.f_height.length )
					{
						args.push( 'height: "' + params.f_height + '"' );
					}

					editor.insertHtml( '{{ media{ ' + args.join( ', ' ) + ' } }}' );
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
			editor.addCommand( pluginName, videoCmd );
	
			// Register the toolbar button.
			editor.ui.addButton( 'Video',
				{
					label : editor.lang.video.toolbar,
					command : pluginName,
					icon : editor.config.mindtouch.editorPath + '/images/icons.png',
					iconOffset : 10
				});	
		}
	});
})();
