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
 * @file Insert menu button plugin.
 */

(function()
{
	function getButtonDefinition( name, editor )
	{
		var uiItem = editor.ui._.items[ name ];

		if ( typeof uiItem == 'undefined' || uiItem.type != CKEDITOR.UI_BUTTON )
		{
			return null;
		}

		var definition =
		{
			command : uiItem.command
		};

		definition = CKEDITOR.tools.extend( {}, definition, uiItem.args[ 0 ] );

		return definition;
	}

	function getUIMenuItems( editor, menuGroup, menuItems )
	{
		editor.addMenuGroup( menuGroup );
		var uiMenuItems = {};

		for ( var i = 0 ; i < menuItems.length ; i++ )
		{
			var menuItem = menuItems[ i ],
				menuItemDefinition = getButtonDefinition( menuItem, editor );

			if ( menuItemDefinition )
			{
				// override button group
				menuItemDefinition.group = menuGroup;
				uiMenuItems[ menuItem.toLowerCase() ] = menuItemDefinition;
			}
		}

		return uiMenuItems;
	}

	function getUIMenuItemsState( editor, uiMenuItems )
	{
		var menuItems = {};

		for ( var itemName in uiMenuItems )
		{
			var state = CKEDITOR.TRISTATE_DISABLED,
				commandName = uiMenuItems[ itemName ].command,
				command = commandName && editor.getCommand( commandName );

			if ( command )
			{
				state = command.state;
			}

			menuItems[ itemName ] = state;
		}

		return menuItems;
	}

	CKEDITOR.plugins.add( 'menubuttons',
	{
		requires : [ 'menu', 'menubutton', 'mindtouch' ],

		lang : [ 'en' ], // @Packager.RemoveLine

		init : function( editor )
		{
			var uiInsertMenuItems = getUIMenuItems( editor, 'insertButton', editor.config.menu_insertItems );
			editor.addMenuItems( uiInsertMenuItems );

			editor.ui.add( 'InsertMenu', CKEDITOR.UI_MENUBUTTON,
				{
					label : editor.lang.menubuttons.insert,
					title : editor.lang.menubuttons.insert,
					className : 'cke_button_insert',
					modes : { 'wysiwyg' : 1 },
					onMenu : function()
					{
						return getUIMenuItemsState( editor, uiInsertMenuItems );
					}
				});

			var uiViewMenuItems = getUIMenuItems( editor, 'viewButton', editor.config.menu_viewItems );
			editor.addMenuItems( uiViewMenuItems );

			editor.ui.add( 'ViewMenu', CKEDITOR.UI_MENUBUTTON,
				{
					label : editor.lang.menubuttons.view,
					title : editor.lang.menubuttons.view,
					className : 'cke_button_view',
					modes : { 'wysiwyg' : 1, 'source' : 1 },
					onMenu : function()
					{
						var menuItem;
						var label;
						var itemName;
						var i;

						for ( i = 0 ; i < editor.config.menu_viewItems.length ; i++ )
						{
							itemName = editor.config.menu_viewItems[i].toLowerCase();
							menuItem = editor.getMenuItem( itemName );

							switch ( itemName )
							{
								case 'source':
									label = ( editor.mode == 'source' ) ? editor.lang.mindtouch.wysiwyg : editor.lang.source;
									break;
								case 'showblocks':
									var state = editor.getCommand( 'showblocks' ).state;
									label = ( state == CKEDITOR.TRISTATE_ON ) ? editor.lang.mindtouch.hideBlocks : editor.lang.showBlocks;
									break;
							}

							if ( menuItem && label )
							{
								menuItem.label = label;
							}
						}

						return getUIMenuItemsState( editor, uiViewMenuItems );
					},
					onRender : function()
					{
						var me = this;

						var updateState = function()
						{
							for ( var i = 0 ; i < editor.config.menu_viewItems.length ; i++ )
							{
								var button = getButtonDefinition( editor.config.menu_viewItems[ i ], editor );
								var command = button && editor.getCommand( button.command );
								if ( command && command.state == CKEDITOR.TRISTATE_ON )
								{
									me.setState( CKEDITOR.TRISTATE_ON );
									return;
								}
							}

							me.setState( CKEDITOR.TRISTATE_OFF );
						};

						editor.on( 'afterCommandExec', function()
							{
								updateState();
							} );
						editor.on( 'mode', function()
							{
								updateState();
							} );
					}
				});
		}
	} );
})();

CKEDITOR.config.menu_insertItems =
	[
		'Comment', 'DekiScript', 'JEM', 'CSS'
	];

CKEDITOR.config.menu_viewItems =
	[
		'Source', 'ShowBlocks'
	];
