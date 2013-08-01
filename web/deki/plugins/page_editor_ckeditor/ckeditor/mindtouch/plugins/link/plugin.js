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
 * @file Link plugin.
 */

(function()
{
	var pluginName = 'mindtouchlink';
	
	var linkCmd =
	{
		canUndo: false,
		
		exec : function( editor )
		{
			this.editor = editor;

			var selection = editor.getSelection(),
				element = null,
				plugin = CKEDITOR.plugins.link;

			if ( ( element = plugin.getSelectedLink( editor ) ) && element.hasAttribute( 'href' ) )
			{
				selection.selectElement( element );
			}
			else
			{
				element = null;
			}

			var params = this._.parseLink.apply( this, [ element ] );
			
			var pageTitle = editor.config.mindtouch.pageTitle;
			if (pageTitle.utf8URL)
			{
				pageTitle = pageTitle.utf8URL();
			}
			
			var url =
				[
				 	editor.config.mindtouch.commonPath + '/popups/link_dialog.php',
					'?href=' + params.f_href,
					'&contextID=' + editor.config.mindtouch.pageId,
					'&cntxt=' + pageTitle,
					'&userName=' + editor.config.mindtouch.userName
				];
		
			var mindtouchDialog = CKEDITOR.plugins.get( 'mindtouchdialog' );
			mindtouchDialog && mindtouchDialog.openDialog( editor, pluginName,
				{
					url: url.join( '' ),
					width: '600px',
					height: '285px',
					params: params,
					callback: this._.insertLink,
					scope: this
				});

			return true;
		},
		
		_ :
		{
			parseLink : function( element )
			{
				var editor = this.editor;
				
				var href = ( element  && ( element.data( 'cke-saved-href' ) || element.getAttribute( 'href' ) ) ) || '',
					params = 
						{
							'f_href'		: href,
							'f_text'		: ( element ) ? element.getText() : '',
							'contextTopic'	: editor.config.mindtouch.pageTitle,
							'contextTopicID': editor.config.mindtouch.pageId,
							'userName'		: editor.config.mindtouch.userName
						};

				if ( !element )
				{
					var selection = editor.getSelection(),
						range = selection && selection.getRanges( true )[0],
						selectedText = '';
					
					if ( range && !range.collapsed )
					{
						selection.lock();
						var children = range.cloneContents().getChildren();
						for ( var i = 0 ; i < children.count() ; i++ )
						{
							selectedText += children.getItem( i ).getText();
						}
						selection.unlock( true );
					}

					params.f_href = ( mindtouchLink.isLinkInternal( selectedText ) ) ?
							'./' + encodeURIComponent( selectedText ) : encodeURI( selectedText );

					params.f_text = selectedText;
					params.newlink = true;
				}

				if ( params.f_href.indexOf( mindtouchLink.internalPrefix ) == 0 )
				{
					params.f_href = params.f_href.substr( mindtouchLink.internalPrefix.length );
				}
				
				this._.selectedElement = element;

				return params;
			},
			
			insertLink : function( params )
			{
				var attributes = {},
					editor = this.editor,
					text = ( params.f_text.length ) ? params.f_text : params.f_href;
				
				attributes['data-cke-saved-href'] = mindtouchLink.normalizeUri( params.f_href );
				attributes.href = attributes['data-cke-saved-href'];
				attributes.title = text;

				editor.fire( 'saveSnapshot' );
				
				// bugfix #2643, let parser choose classes
				if ( attributes.title.indexOf( mindtouchLink.internalPrefix ) == 0 )
				{
					attributes.title = attributes.title.substr( mindtouchLink.internalPrefix.length );
					
					if ( this._.selectedElement )
						this._.selectedElement.removeClass( 'external' );
				}
				
				if ( !this._.selectedElement )
				{
					// Create element if current selection is collapsed.
					var selection = editor.getSelection(),
						ranges = selection && selection.getRanges( true ),
						range = ranges && ranges.length == 1 && ranges[0];

					if ( range && range.collapsed )
					{
						var textNode = new CKEDITOR.dom.text( text, editor.document );
						range.insertNode( textNode );
						range.selectNodeContents( textNode );
						selection.selectRanges( ranges );
					}

					// Apply style.
					var style = new CKEDITOR.style( { element : 'a', attributes : attributes } );
					style.type = CKEDITOR.STYLE_INLINE;		// need to override... dunno why.
					style.apply( editor.document );
				}
				else
				{
					// We're only editing an existing link, so just overwrite the attributes.
					var element = this._.selectedElement;

					element.setAttributes( attributes );

					delete this._.selectedElement;
				}

				editor.fire( 'saveSnapshot' );
			}
		}
	};
	
	var quickLinkCmd = CKEDITOR.tools.clone( linkCmd );	
	quickLinkCmd.exec = function( editor )
	{
		this.editor = editor;
		
		var params = this._.parseLink.apply( this, [ null ] );
		this._.insertLink.apply( this, [ params ] );
	};
	
	var mindtouchLink =
	{
		requires : [ 'mindtouchdialog', 'selection' ],
		
		init : function( editor )
		{
			var plugin = this;

			// Register the command.
			editor.addCommand( pluginName, linkCmd );
			editor.addCommand( 'quicklink', quickLinkCmd );
			
			// Register the keystrokes.
			var keystrokes = editor.keystrokeHandler.keystrokes;
			keystrokes[ CKEDITOR.CTRL + 75 /*K*/ ] = pluginName;
			keystrokes[ CKEDITOR.CTRL + 87 /*W*/ ] = 'quicklink';

			// Register the toolbar button.
			editor.ui.addButton( 'MindTouchLink',
				{
					label : editor.lang.link.toolbar,
					command : pluginName,
					icon : editor.config.mindtouch.editorPath + '/images/icons.png',
					iconOffset : 0
				} );

			editor.ui.addButton( 'Unlink',
				{
					label : editor.lang.unlink,
					command : 'unlink',
					icon : editor.config.mindtouch.editorPath + '/images/icons.png',
					iconOffset : 1
				} );

			editor.on( 'doubleclick', function( evt )
				{
					var element = CKEDITOR.plugins.link.getSelectedLink( editor ) || evt.data.element;

					if ( !element.isReadOnly() )
					{
						if ( element.is( 'a' ) && element.getAttribute( 'href' ) )
						{
							if ( editor.execCommand( pluginName ) )
							{
								evt.cancel();
							}
						}
					}
				}, this, null, 1 );
	
			// If the "menu" plugin is loaded, register the menu items.
			if ( editor.addMenuItems )
			{
				editor.addMenuItems(
					{
						link :
						{
							label : editor.lang.link.menu,
							command : 'mindtouchlink',
							icon : editor.config.mindtouch.editorPath + '/images/icons.png',
							iconOffset : 0,
							group : 'link',
							order : 1
						},

						unlink :
						{
							label : editor.lang.unlink,
							command : 'unlink',
							icon : editor.config.mindtouch.editorPath + '/images/icons.png',
							iconOffset : 1,
							group : 'link',
							order : 5
						}
					});
			}
		},
		
		internalPrefix : 'mks://localhost/',
		externalRegex  : /^([a-z]+:)[\/]{2,5}/i,
		emailRegex : /^(mailto:)?[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$/i,

		isLinkExternal : function( href )
		{
			return ( href.match( this.externalRegex ) != null ) &&
					href.indexOf( this.internalPrefix ) != 0;
		},

		isLinkInternal : function ( href )
		{
			return ( ! ( this.isLinkExternal( href ) || this.isLinkNetwork( href ) || this.isEmail( href ) ) );
		},
		
		isLinkNetwork : function( href )
		{
			// don't put this condition in one line
			// since fckpackager processes it incorrectly
			return ( href.indexOf( "\\" ) == 0 ) ||
					( href.indexOf( "//" ) == 0 );
		},

		isEmail : function( email )
		{
			return this.emailRegex.test( email );
		},
		
		normalizeUri : function( uri )
		{
			if ( this.isEmail( uri ) )
			{
				if ( uri.toLowerCase().indexOf( 'mailto:' ) != 0 )
				{
					uri = 'mailto:' + uri;
				}
				
				return uri;
			}

			if ( this.isLinkNetwork( uri ) )
			{
				return 'file:///' + uri.replace( /\\/g, '/' );
			}

			if ( this.isLinkExternal( uri ) )
			{
				return uri;
			}

			// internal link

			if ( uri != "" && uri.indexOf( this.internalPrefix ) != 0 && uri.indexOf( '/' ) != 0 )
			{
				uri = this.internalPrefix + uri;
			}

			uri = uri.replace( / /g, '_' );

			return uri;
		}		
	};
	
	CKEDITOR.plugins.add( pluginName, mindtouchLink );
})();
