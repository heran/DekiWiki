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
 * @file Attach Image plugin.
 */

(function()
{
	var pluginName = 'attachimage';

	function getState( editor )
	{
		if ( editor.config.mindtouch.pageId == 0 )
			return CKEDITOR.TRISTATE_DISABLED;

		return CKEDITOR.TRISTATE_OFF;
	}

	function onSelectionChange( evt )
	{
		var editor = evt.editor,
			command = editor.getCommand( pluginName );
		command.state = getState( editor );
		command.fire( 'state' );
	}

	function stripHost( href )
	{
		var host = document.location.protocol + '//' + document.location.host ;

		if ( href.indexOf( host ) == 0 )
		{
			href = href.substring( host.length );
		}

		return href;
	}

	var attachImageCmd =
	{
		canUndo: false,

		exec : function( editor )
		{
			// general params regardless of the image state
			var params = {
				titleID : editor.config.mindtouch.pageId,
				commonPath : editor.config.mindtouch.commonPath
			};
			
			var mindtouchDialog = CKEDITOR.plugins.get( 'mindtouchdialog' );
			mindtouchDialog && mindtouchDialog.openDialog( editor, pluginName,
				{
					url: params.commonPath + '/popups/attach_dialog.php?filter=images',
					width: '650px',
					height: 'auto',
					params: params,
					callback: this._.insertImages,
					scope: editor
				});
			
			return true;
		},

		_ :
		{
			insertImages : function( attachedFiles )
			{
				var editor = this, fileIds = [], i;

				for ( i = 0 ; i < attachedFiles.length ; i++ )
				{
					if ( attachedFiles[ i ] !== false )
					{
						fileIds.push( attachedFiles[ i ] );
					}
				}

				var data =
					{
						'fileIds' : fileIds.join( ',' )
					};

				Deki.$.get( '/deki/gui/attachments.php?action=getbyids', data, function( files )
					{
						if ( CKEDITOR.tools.isArray( files ) )
						{
							for ( i = 0 ; i < files.length ; i++ )
							{
								var file = files[ i ];

								if ( !file.href )
									continue;

								var paragraph = editor.document.createElement( 'p' );
								editor.insertElement( paragraph );

								var element = null;

								if ( file.width && file.height )
								{
									element = editor.document.createElement( 'img' );
									element.setAttribute( 'src', stripHost( file.href ) );
									element.setStyle( 'width', file.width + 'px' );
									element.setStyle( 'height', file.height + 'px' );
									element.setAttribute( 'alt', '' );
								}
								else
								{
									element = editor.document.createElement( 'a' );

									var uri = stripHost( file.href );
									element.setAttribute( 'href', uri );
									element.setAttribute( 'title', uri );
									element.setHtml( uri );
								}

								element.addClass( 'internal' );
								element.appendTo( paragraph );
							}

							if ( Deki.Plugin && Deki.Plugin.FilesTable )
							{
								Deki.Plugin.FilesTable.Refresh( editor.config.mindtouch.pageId );
							}
						}
					}, 'json' );
			}
		}
	};

	CKEDITOR.plugins.add( pluginName,
	{
		lang : [ 'en' ], // @Packager.RemoveLine
		
		init : function( editor )
		{
			// Register the command.
			editor.addCommand( pluginName, attachImageCmd );

			// Register the toolbar button.
			editor.ui.addButton( 'AttachImage',
				{
					label : editor.lang.attachImage,
					command : pluginName,
					icon : editor.config.mindtouch.editorPath + '/images/icons.png',
					iconOffset : 3
				});

			editor.on( 'selectionChange', onSelectionChange );
		}
	});
})();
