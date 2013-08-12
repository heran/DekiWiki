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

CKEDITOR.dialog.add( 'cellProperties', function( editor )
	{
		var langTable = editor.lang.table,
			langCell = langTable.cell,
			langCommon = editor.lang.common,
			validate = CKEDITOR.dialog.validate,
			widthPattern = /^(\d+(?:\.\d+)?)(px|%)$/,
			heightPattern = /^(\d+(?:\.\d+)?)px$/,
			bind = CKEDITOR.tools.bind,
			spacer = { type : 'html', html : '&nbsp;' };

		/**
		 *
		 * @param dialogName
		 * @param callback [ childDialog ]
		 */
		function getDialogValue( dialogName, callback )
		{
			var onOk = function()
			{
				releaseHandlers( this );
				callback( this, this._.parentDialog );
				this._.parentDialog.changeFocus( true );
			};
			var onCancel = function()
			{
				releaseHandlers( this );
				this._.parentDialog.changeFocus();
			};
			var releaseHandlers = function( dialog )
			{
				dialog.removeListener( 'ok', onOk );
				dialog.removeListener( 'cancel', onCancel );
			};
			var bindToDialog = function( dialog )
			{
				dialog.on( 'ok', onOk );
				dialog.on( 'cancel', onCancel );
			};
			editor.execCommand( dialogName );
			if ( editor._.storedDialogs.colordialog )
				bindToDialog( editor._.storedDialogs.colordialog );
			else
			{
				CKEDITOR.on( 'dialogDefinition', function( e )
				{
					if ( e.data.name != dialogName )
						return;

					var definition = e.data.definition;

					e.removeListener();
					definition.onLoad = CKEDITOR.tools.override( definition.onLoad, function( orginal )
					{
						return function()
						{
							bindToDialog( this );
							definition.onLoad = orginal;
							if ( typeof orginal == 'function' )
								orginal.call( this );
						};
					} );
				});
			}
		}

		// Synchronous field values to other impacted fields is required
		function commitInternally( targetFields )
		{
			var dialog = this.getDialog(),
				 element = dialog._element && dialog._element.clone()
						 || new CKEDITOR.dom.element( 'td', editor.document );

			// Commit this field and broadcast to target fields.
			this.commit( element );

			targetFields = [].concat( targetFields );
			var length = targetFields.length, field;
			for ( var i = 0; i < length; i++ )
			{
				field = dialog.getContentElement.apply( dialog, targetFields[ i ].split( ':' ) );
				field && field.setup && field.setup( element );
			}
		}

		// Registered 'CKEDITOR.style' instances.
		var styles = {} ;

		return {
			title : langCell.title,
			minWidth : CKEDITOR.env.ie && CKEDITOR.env.quirks ? 400 : 350,
			minHeight : CKEDITOR.env.ie && CKEDITOR.env.quirks?  230 : 200,
			contents : [
				{
					id : 'info',
					label : langCell.title,
					accessKey : 'I',
					elements :
					[
						{
							type : 'hbox',
							widths : [ '40%', '5%', '40%' ],
							children :
							[
								{
									type : 'vbox',
									padding : 0,
									children :
									[
										{
											type : 'hbox',
											widths : [ '70%', '30%' ],
											children :
											[
												{
													type : 'text',
													id : 'width',
													width: '100%',
													label : langCommon.width,
													validate : validate[ 'number' ]( langCell.invalidWidth ),

													// Extra labelling of width unit type.
													onLoad : function()
													{
														var widthType = this.getDialog().getContentElement( 'info', 'widthType' ),
															labelElement = widthType.getElement(),
															inputElement = this.getInputElement(),
															ariaLabelledByAttr = inputElement.getAttribute( 'aria-labelledby' );

														inputElement.setAttribute( 'aria-labelledby', [ ariaLabelledByAttr, labelElement.$.id ].join( ' ' ) );
													},

													setup : function( element )
													{
														var widthAttr = parseInt( element.getAttribute( 'width' ), 10 ),
																widthStyle = parseInt( element.getStyle( 'width' ), 10 );

														!isNaN( widthAttr ) && this.setValue( widthAttr );
														!isNaN( widthStyle ) && this.setValue( widthStyle );
													},
													commit : function( element )
													{
														var value = parseInt( this.getValue(), 10 ),
																unit = this.getDialog().getValueOf( 'info', 'widthType' );

														if ( !isNaN( value ) )
															element.setStyle( 'width', value + unit );
														else
															element.removeStyle( 'width' );

														element.removeAttribute( 'width' );
													},
													'default' : ''
												},
												{
													type : 'select',
													id : 'widthType',
													label : langTable.widthUnit,
													labelStyle: 'visibility:hidden',
													'default' : 'px',
													items :
													[
														[ langTable.widthPx, 'px' ],
														[ langTable.widthPc, '%' ]
													],
													setup : function( selectedCell )
													{
														var widthMatch = widthPattern.exec( selectedCell.getStyle( 'width' ) || selectedCell.getAttribute( 'width' ) );
														if ( widthMatch )
															this.setValue( widthMatch[2] );
													}
												}
											]
										},
										{
											type : 'hbox',
											widths : [ '70%', '30%' ],
											children :
											[
												{
													type : 'text',
													id : 'height',
													label : langCommon.height,
													width: '100%',
													'default' : '',
													validate : validate[ 'number' ]( langCell.invalidHeight ),

													// Extra labelling of height unit type.
													onLoad : function()
													{
														var heightType = this.getDialog().getContentElement( 'info', 'htmlHeightType' ),
															labelElement = heightType.getElement(),
															inputElement = this.getInputElement(),
															ariaLabelledByAttr = inputElement.getAttribute( 'aria-labelledby' );

														inputElement.setAttribute( 'aria-labelledby', [ ariaLabelledByAttr, labelElement.$.id ].join( ' ' ) );
													},

													setup : function( element )
													{
														var heightAttr = parseInt( element.getAttribute( 'height' ), 10 ),
																heightStyle = parseInt( element.getStyle( 'height' ), 10 );

														!isNaN( heightAttr ) && this.setValue( heightAttr );
														!isNaN( heightStyle ) && this.setValue( heightStyle );
													},
													commit : function( element )
													{
														var value = parseInt( this.getValue(), 10 );

														if ( !isNaN( value ) )
															element.setStyle( 'height', CKEDITOR.tools.cssLength( value ) );
														else
															element.removeStyle( 'height' );

														element.removeAttribute( 'height' );
													}
												},
												{
													id : 'htmlHeightType',
													type : 'html',
													html : '<br />'+ langTable.widthPx
												}
											]
										},
										spacer,
										{
											type : 'select',
											id : 'wordWrap',
											label : langCell.wordWrap,
											'default' : 'yes',
											items :
											[
												[ langCell.yes, 'yes' ],
												[ langCell.no, 'no' ]
											],
											setup : function( element )
											{
												var wordWrapAttr = element.getAttribute( 'noWrap' ),
														wordWrapStyle = element.getStyle( 'white-space' );

												if ( wordWrapStyle == 'nowrap' || wordWrapAttr )
													this.setValue( 'no' );
											},
											commit : function( element )
											{
												if ( this.getValue() == 'no' )
													element.setStyle( 'white-space', 'nowrap' );
												else
													element.removeStyle( 'white-space' );

												element.removeAttribute( 'noWrap' );
											}
										},
										spacer,
										{
											type : 'select',
											id : 'hAlign',
											label : langCell.hAlign,
											'default' : '',
											items :
											[
												[ langCommon.notSet, '' ],
												[ langCommon.alignLeft, 'left' ],
												[ langCommon.alignCenter, 'center' ],
												[ langCommon.alignRight, 'right' ]
											],
											setup : function( element )
											{
												var alignAttr = element.getAttribute( 'align' ),
														textAlignStyle = element.getStyle( 'text-align');

												this.setValue(  textAlignStyle || alignAttr || '' );
											},
											commit : function( selectedCell )
											{
												var value = this.getValue();

												if ( value )
													selectedCell.setStyle( 'text-align', value );
												else
													selectedCell.removeStyle( 'text-align' );

												selectedCell.removeAttribute( 'align' );
											}
										},
										{
											type : 'select',
											id : 'vAlign',
											label : langCell.vAlign,
											'default' : '',
											items :
											[
												[ langCommon.notSet, '' ],
												[ langCommon.alignTop, 'top' ],
												[ langCommon.alignMiddle, 'middle' ],
												[ langCommon.alignBottom, 'bottom' ],
												[ langCell.alignBaseline, 'baseline' ]
											],
											setup : function( element )
											{
												var vAlignAttr = element.getAttribute( 'vAlign' ),
														vAlignStyle = element.getStyle( 'vertical-align' );

												switch( vAlignStyle )
												{
													// Ignore all other unrelated style values..
													case 'top':
													case 'middle':
													case 'bottom':
													case 'baseline':
														break;
													default:
														vAlignStyle = '';
												}

												this.setValue( vAlignStyle || vAlignAttr || '' );
											},
											commit : function( element )
											{
												var value = this.getValue();

												if ( value )
													element.setStyle( 'vertical-align', value );
												else
													element.removeStyle( 'vertical-align' );

												element.removeAttribute( 'vAlign' );
											}
										}
									]
								},
								spacer,
								{
									type : 'vbox',
									padding : 0,
									children :
									[
										{
											type : 'select',
											id : 'cellType',
											label : langCell.cellType,
											'default' : 'td',
											items :
											[
												[ langCell.data, 'td' ],
												[ langCell.header, 'th' ]
											],
											setup : function( selectedCell )
											{
												this.setValue( selectedCell.getName() );
											},
											commit : function( selectedCell )
											{
												selectedCell.renameNode( this.getValue() );
											}
										},
										spacer,
										{
											type : 'text',
											id : 'rowSpan',
											label : langCell.rowSpan,
											'default' : '',
											validate : validate.integer( langCell.invalidRowSpan ),
											setup : function( selectedCell )
											{
												var attrVal = parseInt( selectedCell.getAttribute( 'rowSpan' ), 10 );
												if ( attrVal && attrVal  != 1 )
												 	this.setValue(  attrVal );
											},
											commit : function( selectedCell )
											{
												var value = parseInt( this.getValue(), 10 );
												if ( value && value != 1 )
													selectedCell.setAttribute( 'rowSpan', this.getValue() );
												else
													selectedCell.removeAttribute( 'rowSpan' );
											}
										},
										{
											type : 'text',
											id : 'colSpan',
											label : langCell.colSpan,
											'default' : '',
											validate : validate.integer( langCell.invalidColSpan ),
											setup : function( element )
											{
												var attrVal = parseInt( element.getAttribute( 'colSpan' ), 10 );
												if ( attrVal && attrVal  != 1 )
												 	this.setValue(  attrVal );
											},
											commit : function( selectedCell )
											{
												var value = parseInt( this.getValue(), 10 );
												if ( value && value != 1 )
													selectedCell.setAttribute( 'colSpan', this.getValue() );
												else
													selectedCell.removeAttribute( 'colSpan' );
											}
										},
										spacer,
										{
											type : 'select',
											id : 'selCellsUpdate',
											label : '',
											'default' : '',
											items :
											[
												[ langCell.updateSelected, 'selected' ],
												[ langCell.updateRow, 'row' ],
												[ langCell.updateColumn, 'column' ],
												[ langCell.updateTable, 'table' ]
											],
											setup : function( selectedCell )
											{
												this.setValue( 'selected' );
											}
										}
									]
								}
							]
						}
					]
				},

				{
					id : 'advanced',
					label : langCommon.advancedTab,
					accessKey : 'A',
					elements :
					[
						{
							type : 'hbox',
							widths : [ '30%', '40%', '30%' ],
							children :
							[
								{
									id : 'cmbStyle',
									type : 'select',
									'default' : '',
									label : langCommon.styles,
									style : 'width:8em',
									// Options are loaded dynamically.
									items :
									[
										[ langCommon.notSet , '' ]
									],
									onChange : function()
									{
										if ( this.getValue().length )
										{
											commitInternally.call( this,
												[
													'info:width',
													'info:widthType',
													'info:height',
													'info:wordWrap',
													'info:hAlign',
													'info:vAlign',
													'advanced:txtGenClass',
													'advanced:txtId',
													'advanced:borderWidth',
													'advanced:borderStyle',
													'advanced:bgImage',
													'advanced:borderColor',
													'advanced:bgColor'
												] );
										}
									},
									setup : function( selectedCell )
									{
										for ( var name in styles )
											styles[ name ].checkElementRemovable( selectedCell, true ) && this.setValue( name );
									},
									commit: function( selectedCell )
									{
										var styleName;
										if ( ( styleName = this.getValue() ) )
											styles[ styleName ].applyToObject( selectedCell );
									}
								},

								{
									type : 'text',
									id : 'txtGenClass',
									label : langCommon.cssClasses,
									'default' : '',
									style : 'width:13em',
									setup : function( selectedCell )
									{
										this.setValue( selectedCell.getAttribute( 'class' ) );
									},
									commit : function( selectedCell )
									{
										if ( this.getValue() )
											selectedCell.setAttribute( 'class', this.getValue() );
										else
											selectedCell.removeAttribute( 'class' );
									}
								},
								{
									type : 'text',
									id : 'txtId',
									label : langCommon.id,
									'default' : '',
									setup : function( selectedCell )
									{
										this.setValue( selectedCell.getAttribute( 'id' ) );
									}
								}
							]
						},
						{
							type : 'hbox',
							widths : [ '30%', '20%', '50%' ],
							padding : 0,
							children :
							[
								{
									type : 'hbox',
									padding : 0,
									widths : [ '5em' ],
									children :
									[
										{
											type : 'text',
											id : 'borderWidth',
											style : 'width:5em; margin-right:.5em',
											label : langTable.borderWidth,
											'default' : '',
											setup : function( selectedCell )
											{
												var width = CKEDITOR.style.getNormalizedValue( selectedCell, 'border-width' );
												this.setValue( width || '' );
											},
											commit : function( selectedCell )
											{
												if ( this.getValue() )
													selectedCell.setStyle( 'border-width', this.getValue() + 'px' );
												else
													selectedCell.removeStyle( 'border-width' );
											}
										},
										{
											type : 'html',
											html : '<br />' + langTable.widthPx
										}
									]
								},
								{
									type : 'select',
									id : 'borderStyle',
									label : langTable.borderStyle,
									'default' : '',
									items :
									[
										[ 'none', '' ],
										[ 'solid', 'solid' ],
										[ 'dashed', 'dashed' ],
										[ 'dotted', 'dotted' ],
										[ 'double', 'double' ],
										[ 'hidden', 'hidden' ],
										[ 'groove', 'groove' ],
										[ 'ridge', 'ridge' ],
										[ 'inset', 'inset' ],
										[ 'outset', 'outset' ]
									],
									setup : function( selectedCell )
									{
										var style = CKEDITOR.style.getNormalizedValue( selectedCell, 'border-style' );
										this.setValue( style || '' );
									},
									commit : function( selectedCell )
									{
										if ( this.getValue() )
											selectedCell.setStyle( 'border-style', this.getValue() );
										else
											selectedCell.removeStyle( 'border-style' );
									}
								},
								{
									type : 'text',
									id : 'bgImage',
									label : langTable.bgImage,
									'default' : '',
									setup : function( selectedCell )
									{
										var image = CKEDITOR.style.getNormalizedValue( selectedCell, 'background-image' );
										this.setValue( image || '' );
									},
									commit : function( selectedCell )
									{
										if ( this.getValue() )
											selectedCell.setStyle( 'background-image', "url('" + this.getValue() + "')" );
										else
											selectedCell.removeStyle( 'background-image' );
									}
								}

							]
						},
						{
							type : 'hbox',
							padding : 0,
							widths : [ '50%', '50%' ],
							children :
							[
								{
									type : 'text',
									id : 'borderColor',
									label : langCell.borderColor,
									'default' : '',
									setup : function( selectedCell )
									{
										var color = CKEDITOR.style.getNormalizedValue( selectedCell, 'border-color' );
										this.setValue( color || '' );
									},
									commit : function( selectedCell )
									{
										if ( this.getValue() )
											selectedCell.setStyle( 'border-color', CKEDITOR.style.convertHexToRGB( this.getValue() ) );
										else
											selectedCell.removeStyle( 'border-color' );

										selectedCell.removeAttribute( 'borderColor');
									}
								},
								{
									type : 'button',
									id : 'borderColorChoose',
									"class" : 'colorChooser',
									label : langCell.chooseColor,
									onLoad : function()
									{
										// Stick the element to the bottom (#5587)
										this.getElement().getParent().setStyle( 'vertical-align', 'bottom' );
									},
									onClick : function()
									{
										var self = this;
										getDialogValue( 'colordialog', function( colorDialog )
										{
											self.getDialog().getContentElement( 'advanced', 'borderColor' ).setValue(
												colorDialog.getContentElement( 'picker', 'selectedColor' ).getValue()
											);
										} );
									}
								}
							]
						},
						{
							type : 'hbox',
							padding : 0,
							widths : [ '50%', '50%' ],
							children :
							[
								{
									type : 'text',
									id : 'bgColor',
									label : langCell.bgColor,
									'default' : '',
									setup : function( selectedCell )
									{
										var color = CKEDITOR.style.getNormalizedValue( selectedCell, 'background-color' );
										this.setValue( color || '' );
									},
									commit : function( selectedCell )
									{
										if ( this.getValue() )
											selectedCell.setStyle( 'background-color', CKEDITOR.style.convertHexToRGB( this.getValue() ) );
										else
											selectedCell.removeStyle( 'background-color' );

										selectedCell.removeAttribute( 'bgColor');
									}
								},
								{
									type : 'button',
									id : 'bgColorChoose',
									"class" : 'colorChooser',
									label : langCell.chooseColor,
									onLoad : function()
									{
										// Stick the element to the bottom (#5587)
										this.getElement().getParent().setStyle( 'vertical-align', 'bottom' );
									},
									onClick : function()
									{
										var self = this;
										getDialogValue( 'colordialog', function( colorDialog )
										{
											self.getDialog().getContentElement( 'advanced', 'bgColor' ).setValue(
												colorDialog.getContentElement( 'picker', 'selectedColor' ).getValue()
											);
										} );
									}
								}
							]
						}
					]
				}

			],
			onLoad : function()
			{
				// Preparing for the 'elementStyle' field.
				var dialog = this,
					 stylesField = this.getContentElement( 'advanced', 'cmbStyle' );

				editor.getStylesSet( function( stylesDefinitions )
				{
					var styleName;

					if ( stylesDefinitions )
					{
						// Digg only those styles that apply to table cell.
						for ( var i = 0 ; i < stylesDefinitions.length ; i++ )
						{
							var styleDefinition = stylesDefinitions[ i ];
							if ( styleDefinition.element &&
								( styleDefinition.element == 'td' || styleDefinition.element == 'th' ) )
							{
								styleName = styleDefinition.name;
								styles[ styleName ] = new CKEDITOR.style( styleDefinition );

								// Populate the styles field options with style name.
								stylesField.items.push( [ styleName, styleName ] );
								stylesField.add( styleName, styleName );
							}
						}
					}

					// We should disable the content element
					// it if no options are available at all.
					stylesField[ stylesField.items.length > 1 ? 'enable' : 'disable' ]();

					// Now setup the field value manually.
					setTimeout( function() { stylesField.setup( dialog._element ); }, 0 );
				} );
			},
			onShow : function()
			{
				this.cells = CKEDITOR.plugins.tabletools.getSelectedCells(
					this._.editor.getSelection() );
				this.setupContent( this.cells[ 0 ] );
			},
			onOk : function()
			{
				var cellsToUpdate = this.getContentElement( 'info', 'selCellsUpdate' ).getValue(),
					cells = [];

				var selection = this._.editor.getSelection(),
					bookmarks = selection.createBookmarks();

				switch ( cellsToUpdate )
				{
					case 'selected':
						cells = this.cells;
						break;
					case 'row':
						if ( this.cells.length )
						{
							var row = this.cells[ 0 ].getAscendant( 'tr' );
							if ( row )
							{
								cells = row.$.cells;
							}
						}
						break;
					case 'column':
						var table = this.cells[ 0 ].getAscendant( 'table' ),
							i, j, cellIndexes = [];
						
						for ( i = 0 ; i < this.cells.length ; i++ )
						{
							cellIndexes.push( this.cells[ i ].$.cellIndex );
						}

						if ( table )
						{
							for ( i = 0 ; i < table.$.rows.length ; i++ )
							{
								for ( j = 0 ; j < cellIndexes.length ; j++ )
								{
									cells.push( table.$.rows[ i ].cells[ cellIndexes[ j ] ] );
								}
							}
						}
						break;
					case 'table':
						if ( this.cells.length )
						{
							var table = this.cells[ 0 ].getAscendant( 'table' ),
								i, j;

							if ( table )
							{
								for ( i = 0 ; i < table.$.rows.length ; i++ )
								{
									for ( j = 0 ; j < table.$.rows[ i ].cells.length ; j++ )
									{
										cells.push( table.$.rows[ i ].cells[ j ] );
									}
								}
							}
						}
						break;
				}

				for ( var i = 0 ; i < cells.length ; i++ )
				{
					var cell = cells[ i ];

					if ( !(cell instanceof CKEDITOR.dom.element) )
						cell = new CKEDITOR.dom.element( cell );

					this.commitContent( cell );

					// apply id to the first cell only
					if ( i == 0 )
					{
						var id = this.getContentElement( 'advanced', 'txtId' ).getValue();

						if ( id )
							cell.setAttribute( 'id', id );
						else
							cell.removeAttribute( 'id' );
					}

					// Remove empty 'style' attribute.
					!cell.getAttribute( 'style' ) && cell.removeAttribute( 'style' );
				}

				selection.selectBookmarks( bookmarks );

				// Force selectionChange event because of alignment style.
				var firstElement = selection.getStartElement();
				var currentPath = new CKEDITOR.dom.elementPath( firstElement );

				this._.editor._.selectionPreviousPath = currentPath;
				this._.editor.fire( 'selectionChange', { selection : selection, path : currentPath, element : firstElement } );

			},
			onHide : function()
			{
				delete this._element;
			}
		};
	} );
