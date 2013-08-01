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

(function()
{
	function getMousePosition( ev )
	{
		var posx = 0,
			posy = 0,
			domEvent = ev.data.$;

		if ( domEvent.pageX || domEvent.pageY )
		{
			posx = domEvent.pageX;
			posy = domEvent.pageY;
		}
		else if ( domEvent.clientX || domEvent.clientY )
		{
			var	target = ev.data.getTarget(),
				doc = target && target.getDocument();

			posx = domEvent.clientX;
			posy = domEvent.clientY;

			if ( doc )
			{
				var scrollPosition = doc.getWindow().getScrollPosition();

				posx += scrollPosition.x;
				posy += scrollPosition.y;
			}
		}

		return {x : posx, y : posy};
	}

	var dimensionPicker = CKEDITOR.tools.createClass(
	{
		$ :	function( container, panel, onPick )
		{
			this._.minCols = 5;
			this._.minRows = 5;
			this._.lastCols = 0;
			this._.lastRows = 0;

			this._.container = container;
			this._.panel = panel;

			this.onPick = onPick;

			this._.init();
		},

		_ :
		{
			setDimensions : function( element, cols, rows )
			{
				element.setStyle( 'width', ( 18 * cols ) + 'px' );
				element.setStyle( 'height', ( 18 * rows ) + 'px' );
			},

			init : function()
			{
				var doc = this._.container.getDocument();

				this._.mouseDiv = new CKEDITOR.dom.element( 'div', doc );
				this._.mouseDiv.addClass( 'dimension-picker-mouse' );

				this._.uhDiv = new CKEDITOR.dom.element( 'div', doc );
				this._.uhDiv.addClass( 'dimension-picker-unhighlighted' );

				this._.hDiv = new CKEDITOR.dom.element( 'div', doc );
				this._.hDiv.addClass( 'dimension-picker-highlighted' );

				this._.statusDiv = new CKEDITOR.dom.element( 'div', doc );
				this._.statusDiv.addClass( 'dimension-picker-status' );

				this._.picker = new CKEDITOR.dom.element( 'div', doc );
				this._.picker.setAttribute( 'id', 'dimension-picker' );

				this._.container.append( this._.picker );
				this._.container.append( this._.statusDiv );

				this._.picker.append( this._.mouseDiv );
				this._.picker.append( this._.uhDiv );
				this._.picker.append( this._.hDiv );

				this._.mouseDiv.on( 'mousemove', function( ev )
					{
						var dimensions = this._.getDimensions( ev );

						if ( this._.isChanged( dimensions.cols, dimensions.rows ) )
						{
							this._.pick( dimensions.cols, dimensions.rows );
						}
					}, this );

				this._.picker.on( 'click', function( ev )
					{
						var dimensions = this._.getDimensions( ev );

						if ( typeof this.onPick == 'function'  )
						{
							this.onPick( dimensions );
						}
					}, this );
			},

			pick : function( cols, rows )
			{
				var uhCols = Math.max( this._.minCols, cols ),
					uhRows = Math.max( this._.minRows, rows );

				// highlighted cells
				this._.setDimensions( this._.hDiv, cols, rows );
				// not highlighted cells
				this._.setDimensions( this._.uhDiv, uhCols, uhRows );

				this._.statusDiv.setHtml( rows + 'x' + cols );

				if ( CKEDITOR.env.ie )
				{
					this._.mouseDiv.setStyle( 'width', ( this._.container.$.offsetWidth + 18 ) + 'px' );
					this._.mouseDiv.setStyle( 'height', this._.container.$.offsetHeight + 'px' );
				}

				var pickerWidth = this._.uhDiv.$.offsetWidth,
					pickerHeight = this._.uhDiv.$.offsetHeight + this._.statusDiv.$.offsetHeight;

				pickerWidth += 8;
				pickerHeight += 14;

				if ( CKEDITOR.env.ie )
				{
					this._.panel._.iframe.setStyle( 'width', pickerWidth + 'px' );
					this._.panel._.iframe.setStyle( 'height', ( pickerHeight + 18 ) + 'px' );
				}

				var panelHolderElement = CKEDITOR.document.getById( 'cke_' + this._.panel._.panel.id );

				// block.autoSize = true adds 4px
				// remove them on panel opening
				if ( !panelHolderElement.getStyle( 'width' ).length )
				{
					pickerWidth -= 4;
				}

				panelHolderElement.setStyle( 'width', pickerWidth + 'px' );
				panelHolderElement.setStyle( 'height', pickerHeight + 'px' );

				this._.container.setStyle( 'width', pickerWidth + 'px' );
				this._.container.setStyle( 'height', pickerHeight + 'px' );
			},

			getDimensions : function( ev )
			{
				var mousePos = getMousePosition( ev );
				var x = mousePos.x;
				var y = mousePos.y;

				var cols = Math.ceil( x / 18.0 );
				var rows = Math.ceil( y / 18.0 );

				return { 'cols' : cols, 'rows'	: rows };
			},

			isChanged : function( cols, rows )
			{
				if ( cols != this._.lastCols || rows != this._.lastRows )
				{
					this._.lastCols = cols;
					this._.lastRows = rows;
					return true;
				}

				return false;
			}
		},

		proto :
		{
			show : function()
			{
				this._.pick( 0, 0 );
			}
		}
	} );

	CKEDITOR.plugins.add( 'tableoneclick',
	{
		init : function( editor )
		{
			var plugin = this,
				picker;

			editor.ui.add( 'TableOneClick', CKEDITOR.UI_PANELBUTTON,
				{
					label : editor.lang.table.toolbar,
					title : editor.lang.table.toolbar,
					className : 'cke_button_tableoneclick',
					icon : editor.skinPath + 'icons.png',
					iconOffset : 38,
					modes : {wysiwyg : 1},

					panel :
					{
						css : editor.skin.editor.css.concat( plugin.path + 'css/style.css' ),
						attributes : {role : 'listbox', 'aria-label' : editor.lang.table.toolbar}
					},

					onBlock : function( panel, block )
					{
						block.autoSize = true;
						block.element.addClass( 'cke_tableoneclickblock' );

						var pickerContainer = new CKEDITOR.dom.element( 'div', block.element.getDocument() );
						block.element.append( pickerContainer );

						// The block should not have scrollbars (#5933, #6056)
						block.element.getDocument().getBody().setStyle( 'overflow', 'hidden' );

						picker = new dimensionPicker( pickerContainer, panel, function( dimensions )
							{
								editor.focus();
								panel.hide();

								if ( dimensions.cols > 0 && dimensions.rows > 0 )
								{
									var table = new CKEDITOR.dom.element( 'table', editor.document );

									table.setStyle( 'width', '100%' );
									table.setStyle( 'table-layout', 'fixed' );

									table.setAttributes(
										{
											'cellPadding' : 1,
											'cellSpacing' : 1,
											'border' : 1
										} );

									var tbody = new CKEDITOR.dom.element( 'tbody', editor.document );
									table.append( tbody );

									var firstCell;

									for ( var i = 0 ; i < dimensions.rows ; i++ )
									{
										var row = new CKEDITOR.dom.element( 'tr', editor.document );
										tbody.append( row );

										for ( var j = 0 ; j < dimensions.cols ; j++ )
										{
											var cell = new CKEDITOR.dom.element( 'td', editor.document );
											row.append( cell );
											
											if ( !CKEDITOR.env.ie )
												cell.append( 'br' );

											if ( i == 0 && j == 0 )
											{
												firstCell = cell;
											}
										}
									}

									editor.insertElement( table );

									var sel = editor.getSelection(),
										ranges = sel && sel.getRanges(),
										range = ranges && ranges[0];

									if ( range )
									{
										range.moveToElementEditStart( firstCell );
										range.collapse( true );
										range.select();
									}
								}
							} );
					},

					onOpen : function()
					{
						picker.show();
					}
				});
		}
	} );
})();
