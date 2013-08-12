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
 * @file Transformations plugin.
 */

(function()
{
	var transformation = function( definition )
	{
		CKEDITOR.tools.extend( this, definition );

		if ( this.tags && !CKEDITOR.tools.isArray( this.tags ) )
		{
			this.tags = this.tags.toLowerCase().split( ',' );
		}
	};
	
	transformation.prototype =
	{
		apply : function( elementPath )
		{
			for ( var i = 0, element ; i < elementPath.elements.length ; i++ )
			{
				element = elementPath.elements[i];

				if ( this.isElementApplicable( element ) )
				{
					for ( var attr in this.attributes )
					{
						if ( attr == 'class' )
						{
							element.addClass( this.attributes[ attr ] );
						}
						else
						{
							element.setAttribute( attr, this.attributes[ attr ] );
						}
					}
				}
			}
		},
		
		remove : function( elementPath )
		{
			for ( var i = 0, element ; i < elementPath.elements.length ; i++ )
			{
				element = elementPath.elements[i];

				if ( this.isElementApplicable( element ) )
				{
					for ( var attr in this.attributes )
					{
						if ( attr == 'class' )
						{
							element.removeClass( this.attributes[ attr ] );
						}
						else
						{
							element.removeAttribute( attr );
						}
					}
				}
			}
		},
	
		isApplicable : function( elementPath )
		{
			for ( var i = 0, element ; i < elementPath.elements.length ; i++ )
			{
				element = elementPath.elements[i];

				if ( this.isElementApplicable( element ) )
				{
					return true;
				}
			}

			return false;
		},

		isElementApplicable : function( element )
		{
			return ( CKEDITOR.tools.indexOf( this.tags, element.getName() ) > -1 && !element.isReadOnly() );
		},
		
		isActive : function( elementPath )
		{
			var isActive = false;

			for ( var i = 0, element ; i < elementPath.elements.length ; i++ )
			{
				element = elementPath.elements[i];

				if ( this.isElementApplicable( element ) )
				{
					for ( var attr in this.attributes )
					{
						if ( ( attr == 'class' && element.hasClass( this.attributes[ attr ] ) )
							|| ( element.getAttribute( attr ) == this.attributes[ attr ] ) )
						{
							isActive = true;
						}
						else
						{
							isActive = false;
							break;
						}
					}

					if ( isActive )
					{
						break;
					}
				}
			}

			return isActive;
		}
	};

	CKEDITOR.plugins.add( 'transformations',
	{
		requires : [ 'combobutton', 'selection' ],
		
		lang : [ 'en' ], // @Packager.RemoveLine
		
		init : function( editor )
		{
			var config = editor.config,
				transformations = {},
				transformationsLoaded = false,
				button, icon = {};

			editor.on( 'themeLoaded', function( ev )
				{
					var id;

					for ( var i = 0 ; i < editor.toolbox.toolbars.length ; i++ )
					{
						var toolbar = editor.toolbox.toolbars[ i ];
						for ( var j = 0 ; j < toolbar.items.length ; j++ )
						{
							var item = toolbar.items[ j ];
							if ( item.combo && item.combo.className && item.combo.className == 'cke_transformations' )
							{
								id = item.id;
								break;
							}
						}

						if ( id )
							break;
					}

					if ( id )
					{
						button = CKEDITOR.document.getById( id ).getFirst();
						icon.image = button.getStyle( 'background-image' );
						icon.pos = button.getStyle( 'background-position' );

						button.setStyles(
							{
								'background-image' : 'url(' + editor.config.mindtouch.commonPath + '/icons/anim-wait-circle.gif)',
								'background-position' : '0% 0%'
							});
					}
				});

			var loadTransformations = function( callback )
			{
				callback = typeof callback == 'function' ? callback : null;

				if ( transformationsLoaded )
				{
					callback && callback();
					return;
				}

				Deki.Plugin.AjaxRequest( 'json_site_functions',
					{
						url : Deki.Plugin.AJAX_URL + '?tt=' + Deki.EditorExtensionsToken,
						data :
						{
							'method' : 'transformations'
						},
						success : function( data, status )
						{
							var transforms = data.body;

							for ( var i = 0 ; i < transforms.length ; i++ )
							{
								var transform = transforms[ i ];
								var name = transform.name || transform.func;

								var transformDefinition =
									{
										name : name,
										title : transform.title,
										tags : transform.tags,
										attributes :
										{
											'class' : 'deki-transform',
											'function' : transform.func
										}
									};

								transformations[ name ] = new transformation( transformDefinition );
							}

							transformationsLoaded = true;

							if ( button )
							{
								button.setStyles(
									{
										'background-image' : icon.image,
										'background-position' : icon.pos
									});
							}

							callback && callback();
						}
					} );
			};
			
			editor.ui.addComboButton( 'Transformations',
				{
					label : editor.lang.transformations.label,
					title : editor.lang.transformations.panelTitle,
					className : 'cke_transformations',
					icon : editor.config.mindtouch.editorPath + '/images/icons.png',
					iconOffset : 11,
					modes : {},

					panel :
					{
						css : editor.skin.editor.css.concat( config.contentsCss ),
						multiSelect : false,
						attributes : { 'aria-label' : editor.lang.panelTitle }
					},

					init : function()
					{
						var combo = this;

						loadTransformations( function()
							{
								var text;

								combo.startGroup( editor.lang.transformations.panelTitle );

								text = editor.lang.list.none;
								combo.add( 'none', '<span>' + text + '</span>', text );

								for ( var name in transformations )
								{
									var transform = transformations[ name ];
									var func = transform.attributes[ 'function' ];

									text = ( func.length ) ? func : transform.title;

									// Add the tag entry to the panel list.
									combo.add( name, '<span>' + text + '</span>', text );
								}

								combo.commit();
								combo.onOpen();

								editor.selectionChange()
							});
					},

					onClick : function( value )
					{
						editor.focus();
						editor.fire( 'saveSnapshot' );

						var transform = transformations[ value ],
							selection = editor.getSelection();

						if ( !selection )
							return;

						var elementPath = new CKEDITOR.dom.elementPath( selection.getStartElement() );

						if ( value == 'none' )
						{
							transform = transformations[ this.getValue() ];
						}

						if ( transform && transform.isActive( elementPath ) )
						{
							transform.remove( elementPath );
							this.setState( CKEDITOR.TRISTATE_OFF );
						}
						else
						{
							var prevTransform = transformations[ this.getValue() ];
							prevTransform && prevTransform.remove( elementPath );

							transform.apply( elementPath );

							this.setState( CKEDITOR.TRISTATE_ON );
						}

						// Don't use list.blur() method so far as it is not part of the core
						var list = this._.list;
						try
						{
							list.element.getDocument().getById( list._.items[ value ] ).getFirst().$.blur();
						}
						catch( ex ) {}

						editor.fire( 'saveSnapshot' );
					},

					onRender : function()
					{
						editor.on( 'selectionChange', function( ev )
							{
								var elementPath = ev.data.path;

								var updateState = function()
								{
									var state = CKEDITOR.TRISTATE_DISABLED;

									for ( var value in transformations )
									{
										var transform = transformations[ value ];

										if ( transform.isApplicable( elementPath ) )
										{
											if ( transform.isActive( elementPath ) )
											{
												if ( value != this.getValue() )
												{
													this.setValue( value );
												}
												this.setState( CKEDITOR.TRISTATE_ON );
												return;
											}
											state = CKEDITOR.TRISTATE_OFF;
										}
									}

									this.setValue( 'none' );
									this.setState( state );
								};

								updateState.call( this );
							},
						this );
					},

					onOpen : function()
					{
						if ( CKEDITOR.env.ie )
							editor.focus();

						var selection = editor.getSelection();

						if ( !selection )
							return;

						var element = selection.getSelectedElement(),
							elementPath = new CKEDITOR.dom.elementPath( element || selection.getStartElement() );

						this.showAll();
						this.unmarkAll();

						this.mark( 'none' );

						for ( var name in transformations )
						{
							var transform = transformations[ name ];

							if ( transform.isApplicable( elementPath ) )
							{
								if ( transform.isActive( elementPath ) )
								{
									this.mark( name );
								}
							}
							else
							{
								this.hideItem( name );
							}
						}
					}

				});

			editor.on( 'instanceReady', loadTransformations );
		}
	});
})();
