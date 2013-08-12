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
 * @file Plug-in with styles tools.
 */

(function()
{
	CKEDITOR.plugins.add( 'styletools',
	{
		init : function( editor )
		{
			CKEDITOR.tools.extend( CKEDITOR.style,
				{
					getNormalizedValue : function( el, property, attr )
					{
						var value = '';
						attr = attr || property;
						
						switch ( property )
						{
							case 'background-image' :
								value = el.getStyle( property ).replace( /url\('?([^']*)'?\)/gi, '$1');
								break;
							case 'background-color' :
							case 'border-color' :
								if ( el.getStyle( property ).length > 0 )
								{
									value = CKEDITOR.style.convertRGBToHex( el.getStyle( property ) );
								}
								else if ( property == 'background-color' && el.hasAttribute( 'bgColor' ) )
								{
									value = el.getAttribute( 'bgColor' );
								}
								else if ( property == 'border-color' && el.hasAttribute( 'borderColor' ) )
								{
									value = el.getAttribute( 'borderColor' );
								}
								break;
							case 'border-style' :
								value = el.getStyle( 'border-style' );
			
								if ( value && value.match( /([^\s]*)\s/ ) )
									value = RegExp.$1;
			
								break;
							case 'border-width' :
								value = CKEDITOR.style.getNum( el.getStyle( 'border-width' ) );
								break;
							case 'white-space' :
								if ( el.getStyle( 'white-space' ).length )
								{
									value = el.getStyle( 'white-space' );
								}
								else
								{
									if ( el.hasAttribute( 'noWrap' ) )
										value = 'nowrap';
								}
								break;
							default :
								value = CKEDITOR.style.getValue( el, property, attr );
						}
						
						return value;
					},
					
					getValue : function( el, property, attr )
					{
						var value = '', st = el.style;
						attr = attr || property;
						
						if ( el.hasAttribute( attr ) )
							value = el.getAttribute( attr );
						else
							value = el.getStyle( property );
							
						return value;
					},
					
					convertRGBToHex : function( color )
					{
						var re = /rgb\s*\(\s*([0-9]+).*,\s*([0-9]+).*,\s*([0-9]+).*\)/gi,
							rgb = color.replace( re, '$1,$2,$3' ).split( ',' ),
							r, g, b;
						
						if ( rgb.length == 3 )
						{
							r = parseInt( rgb[0] ).toString( 16 );
							g = parseInt( rgb[1] ).toString( 16 );
							b = parseInt( rgb[2] ).toString( 16 );
							
							r = r.length == 1 ? '0' + r : r;
							g = g.length == 1 ? '0' + g : g;
							b = b.length == 1 ? '0' + b : b;
							
							color = "#" + r + g + b;
						}
						
						return color;
					},
				        
					convertHexToRGB : function( color )
					{
						if ( color.indexOf('#') == 0 )
						{
							color = color.replace( /[^0-9A-F]/gi, '' );
							                
							r = parseInt( color.substring( 0, 2 ), 16 );
							g = parseInt( color.substring( 2, 4 ), 16 );
							b = parseInt( color.substring( 4, 6 ), 16 );
							
							color = "rgb(" + r + "," + g + "," + b + ")";
						}
						else
						{
							color = '';
						}
						
						return color;
					},
					
					getUnit : function( size )
					{
						var unit = size.replace( /[0-9]+(px|%|in|cm|mm|em|ex|pt|pc)?/, '$1' );
						
						if ( !unit.length )
							unit = 'px';
						
						return unit;
					},
					
					getNum : function( val )
					{
						val = parseInt( val );
			
						if ( isNaN( val ) )
						{
							val = '';
						}
						
						return val;
					}
				});
		}
	});
})();
