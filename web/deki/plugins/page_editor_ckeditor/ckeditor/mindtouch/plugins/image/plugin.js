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
 * @file Image plugin.
 */

(function()
{
	var pluginName = 'mindtouchimage';

	var imageCmd =
	{
		canUndo: false,
		
		exec : function( editor )
		{
			this.editor = editor;
			
			var imageElement = editor.getSelection().getSelectedElement();
			
			if ( !( imageElement && imageElement.getName() == 'img' && !imageElement.data( 'cke-realelement' ) ) &&
					!( imageElement && imageElement.getName() == 'input' && imageElement.getAttribute( 'type' ) == 'image' ) )
				imageElement = null;

			// build the params object to pass to the dialog
			var params = {};
			
			if ( imageElement )
			{
				// determine the image wrapping
				var wrap = '';

				if ( imageElement.hasClass( 'rwrap' ) )
				{
					wrap = 'right';
				}
				else if ( imageElement.hasClass( 'lwrap' ) )
				{
					wrap = 'left';
				}
				else
				{
					wrap = 'default';
				}
				
				var width = parseInt( imageElement.getComputedStyle( 'width' ) ) ||
						parseInt( imageElement.getStyle( 'width' ) ) ||
						parseInt( imageElement.getAttribute( 'width' ) ) || 0;
				
				var height = parseInt( imageElement.getComputedStyle( 'height' ) ) ||
						parseInt( imageElement.getStyle( 'height' ) ) ||
						parseInt( imageElement.getAttribute( 'height' ) ) || 0;

				params =
					{
						'bInternal' : imageElement.hasClass( 'internal' ),
						'sSrc' : imageElement.data( 'cke-saved-src' ) || imageElement.getAttribute( 'src' ),
						'sAlt' : imageElement.getAttribute( 'alt' ),
						'sWrap' : wrap,
						'nWidth' : width,
						'nHeight' : height
					};
			}

			// general params regardless of the image state
			params.nPageId = editor.config.mindtouch.pageId;
			params.sUserName = editor.config.mindtouch.userName;
			
			var url = editor.config.mindtouch.commonPath +
				'/popups/image_dialog.php?contextID=' +
				editor.config.mindtouch.pageId
			;
			
			if ( imageElement )
			{
				url += "&update=true";
			}
		
			var mindtouchDialog = CKEDITOR.plugins.get( 'mindtouchdialog' );
			mindtouchDialog.openDialog( editor, pluginName,
				{
					url: url,
					width: '600px',
					height: '370px',
					params: params,
					callback: this._.insertImage,
					scope: this
				});
		},
		
		_ :
		{
			insertImage : function( params )
			{
				var editor = this.editor;
				
				var imageElement = editor.document.createElement( 'img' );
				imageElement.setAttribute( 'alt', '' );
	
				// try block for IE and bad images
				try
				{
					// set the image source
					imageElement.data( 'cke-saved-src', params.sSrc );
					imageElement.setAttribute( 'src', params.sSrc );

					// set the image attributes
					if ( params.nWidth || params.nHeight )
					{
						imageElement.setStyle( 'width', params.nWidth + 'px' );
						imageElement.setStyle( 'height', params.nHeight + 'px' );
					}
	
					if ( params.sAlt )
					{
						imageElement.setAttribute( 'alt', params.sAlt );
					}
	
					var sInternalClass = ( params.bInternal ) ? 'internal ' : '';
					// >MT: Bugfix: 0002630: left floating image is not aligned properly
					imageElement.setAttribute( 'class', sInternalClass + params.sWrapClass );
	
					switch ( params.sWrap )
					{
						case 'left':
						case 'right':
							imageElement.setAttribute( 'align', params.sWrap );
							break;
						default :
							if ( imageElement.hasAttribute( 'align' ) )
							{
								imageElement.removeAttribute( 'align' );
							}
							break;
					}
					
					if ( params.sFullSrc && params.sFullSrc.length > 0 )
					{
						var linkElement = imageElement && imageElement.getAscendant( 'a' );
						
						if ( !linkElement )
							linkElement = editor.document.createElement( 'a' );
						
						linkElement.setAttribute( 'href', params.sFullSrc );
						linkElement.data( 'cke-saved-href', params.sFullSrc ) ;
						
						if ( params.sAlt )
							linkElement.setAttribute( 'title', params.sAlt ) ;
						
						editor.insertElement( linkElement );
						linkElement.append( imageElement, false );
					}
					else
						editor.insertElement( imageElement );
				}
				catch (e) {}
			}
		}
	};
	
	CKEDITOR.plugins.add( pluginName,
	{
		requires : [ 'mindtouchdialog' ],
		
		init : function( editor )
		{
			// Register the command.
			editor.addCommand( pluginName, imageCmd );
	
			// Register the toolbar button.
			editor.ui.addButton( 'MindTouchImage',
				{
					label : editor.lang.common.image,
					command : pluginName
				});
	
			// If the "menu" plugin is loaded, register the menu items.
			if ( editor.addMenuItems )
			{
				editor.addMenuItems(
					{
						image :
						{
							label : editor.lang.image.menu,
							command : 'mindtouchimage',
							group : 'image'
						}
					});
			}

			editor.on( 'doubleclick', function( evt )
				{
					var element = evt.data.element;

					if ( element.is( 'img' ) && !element.data( 'cke-realelement' ) )
					{
						if ( editor.execCommand( pluginName ) )
						{
							evt.cancel();
						}
					}
				}, this, null, 1 );
		}
	});
})();
