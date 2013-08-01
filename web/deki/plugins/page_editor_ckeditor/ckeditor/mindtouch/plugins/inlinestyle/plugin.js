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
 * @file Style attribute protection plug-in.
 */

(function()
{
	var mouseClickTarget = null;

	function getSavedStyles( element )
	{
		return parseStyle( element.getAttribute( 'data-cke-saved-style' ) );
	}
	
	function getStyles( element )
	{
		return parseStyle( element.getStyleAttribute( true ) );
	}

	function parseStyle( inlineStyle )
	{
		var styleObject = {};

		if ( typeof inlineStyle !== 'string' || inlineStyle.length == 0 )
			return styleObject;

		inlineStyle = inlineStyle.replace( /[\n\r]/g, '' );
		inlineStyle = inlineStyle.replace( /\s+/g, ' ' );
		inlineStyle = inlineStyle.replace( /\/\*.*\*\//g, '' );

		var styles = inlineStyle.split( ';' ),
			i, style, name, value;

		for ( i = 0 ; i < styles.length ; i++ )
		{
			if ( styles[i].indexOf( ':' ) === -1 )
				continue;

			style = explode( ':', styles[i], 2 );

			name = CKEDITOR.tools.trim( style[0] ).toLowerCase();
			value = CKEDITOR.tools.trim( style[1] );

			styleObject[name] = value;
		}

		return styleObject;
	}

	function setSavedStyles( element, value )
	{
		if ( checkDocument( element ) )
			element.$.setAttribute( 'data-cke-saved-style', value );
	}

	function buildStyleText( styleObject )
	{
		var styles = [], name, styleText;

		for ( name in styleObject )
			styles.push( name + ': ' + styleObject[name] );

		styleText = styles.join( '; ' );

		if ( styleText.length )
			styleText = styleText + ';';

		return styleText;
	}

	/**
	 * Checks if the element's document is editor's document
	 */
	function checkDocument( element )
	{
		var result = false, i, instance;

		for ( i in CKEDITOR.instances )
		{
			instance = CKEDITOR.instances[i];
			if ( instance.document && instance.document.equals( element.getDocument() ) )
			{
				result = true;
				break;
			}
		}

		return result;
	}

	function explode ( delimiter, string, limit )
	{
		// Splits a string on string separator and return array of components. If limit is positive only limit number of components is returned. If limit is negative all components except the last abs(limit) are returned.
		//
		// version: 909.322
		// discuss at: http://phpjs.org/functions/explode
		// +     original by: Kevin van Zonneveld (http://kevin.vanzonneveld.net)
		// +     improved by: kenneth
		// +     improved by: Kevin van Zonneveld (http://kevin.vanzonneveld.net)
		// +     improved by: d3x
		// +     bugfixed by: Kevin van Zonneveld (http://kevin.vanzonneveld.net)
		// *     example 1: explode(' ', 'Kevin van Zonneveld');
		// *     returns 1: {0: 'Kevin', 1: 'van', 2: 'Zonneveld'}
		// *     example 2: explode('=', 'a=bc=d', 2);
		// *     returns 2: ['a', 'bc=d']

		var emptyArray = {0: ''};

		// third argument is not required
		if ( arguments.length < 2 ||
				typeof arguments[0] == 'undefined' ||
				typeof arguments[1] == 'undefined' )
		{
			return null;
		}

		if ( delimiter === '' ||
				delimiter === false ||
				delimiter === null )
		{
			return false;
		}

		if ( typeof delimiter == 'function' ||
				typeof delimiter == 'object' ||
				typeof string == 'function' ||
				typeof string == 'object' )
		{
			return emptyArray;
		}

		if ( delimiter === true ) {
			delimiter = '1';
		}

		if ( !limit )
		{
			return string.toString().split( delimiter.toString() );
		}
		else
		{
			// support for limit argument
			var splitted = string.toString().split( delimiter.toString() );
			var partA = splitted.splice( 0, limit - 1 );
			var partB = splitted.join( delimiter.toString() );
			partA.push( partB );
			return partA;
		}
	}

	function onMouseUp()
	{
		if ( mouseClickTarget )
		{
			var nodeList;

			if ( mouseClickTarget.is && mouseClickTarget.is( 'img' ) )
				nodeList = new CKEDITOR.dom.nodeList( [ mouseClickTarget.$ ] );
			else
				nodeList = mouseClickTarget.getChildren();

			updateSavedStyle( nodeList, 'img' );

			mouseClickTarget = null;
		}
	}

	function updateSavedStyle( nodeList )
	{
		var name, node, i, styles, savedStyles,
			nodeNames = CKEDITOR.tools.isArray( arguments[ 1 ] ) ? arguments[ 1 ] : [ arguments[ 1 ] ] ;

		for ( i = 0 ; i < nodeList.count() ; i++ )
		{
			node = nodeList.getItem( i );

			if ( node.type !== CKEDITOR.NODE_ELEMENT )
				continue;

			if ( node.is.apply( node, nodeNames ) )
			{
				savedStyles = getSavedStyles( node );
				styles = getStyles( node );

				for ( name in savedStyles )
					savedStyles[ name ] = node.getStyle( name ) || savedStyles[ name ];

				for ( name in styles )
					savedStyles[ name ] = styles[ name ];

				setSavedStyles( node, buildStyleText( savedStyles ) );
			}

			updateSavedStyle( node.getChildren() );
		}
	}

	CKEDITOR.plugins.add( 'inlinestyle',
	{
		requires : [ 'styles' ],

		beforeInit : function( editor )
		{
			CKEDITOR.tools.extend( CKEDITOR.dom.element.prototype,
				{
					getAttribute : (function()
					{
						var standard = function( name )
						{
							/**
							 * Style attribute protection
							 * @author MindTouch
							 */
							if ( name == 'style' )
							{
								return this.getStyleAttribute();
							}
							else
							/* END */
								return this.$.getAttribute( name, 2 );
						};

						if ( CKEDITOR.env.ie && ( CKEDITOR.env.ie7Compat || CKEDITOR.env.ie6Compat ) )
						{
							return function( name )
							{
								switch ( name )
								{
									case 'class':
										name = 'className';
										break;

									case 'tabindex':
										var tabIndex = standard.call( this, name );

										// IE returns tabIndex=0 by default for all
										// elements. For those elements,
										// getAtrribute( 'tabindex', 2 ) returns 32768
										// instead. So, we must make this check to give a
										// uniform result among all browsers.
										if ( tabIndex !== 0 && this.$.tabIndex === 0 )
											tabIndex = null;

										return tabIndex;
										break;

									case 'checked':
									{
										var attr = this.$.attributes.getNamedItem( name ),
											attrValue = attr.specified ? attr.nodeValue     // For value given by parser.
																		 : this.$.checked;  // For value created via DOM interface.

										return attrValue ? 'checked' : null;
									}

									/**
									 * Style attribute protection
									 * @author MindTouch
									 */
									// case 'style':
										// IE does not return inline styles via getAttribute(). See #2947.
										// return this.$.style.cssText;
									/* END */
								}

								return standard.call( this, name );
							};
						}
						else
							return standard;
					})(),

					/**
					 * Style attribute protection
					 * @author MindTouch
					 */
					getStyleAttribute : (function()
					{
						var standard = function( isOriginal )
						{
							if ( !isOriginal && this.hasAttribute( 'data-cke-saved-style' ) )
							{
								return this.$.getAttribute( 'data-cke-saved-style', 2 );
							}
							else
								return this.$.getAttribute( 'style', 2 );
						};

						if ( CKEDITOR.env.ie && ( CKEDITOR.env.ie7Compat || CKEDITOR.env.ie6Compat ) )
						{
							return function( isOriginal )
							{
								if ( !isOriginal && this.hasAttribute( 'data-cke-saved-style' ) )
								{
									return this.$.getAttribute( 'data-cke-saved-style' );
								}
								else
									// IE does not return inline styles via getAttribute(). See #2947.
									return this.$.style.cssText;
							};
						}
						else
							return standard;
					})(),
					/* END */

					setAttribute : (function()
					{
						var standard = function( name, value )
						{
							this.$.setAttribute( name, value );
							/**
							 * Style attribute protection
							 * @author MindTouch
							 */
							if ( name == 'style' )
							{
								setSavedStyles( this, value );
							}
							/* END */
							return this;
						};

						if ( CKEDITOR.env.ie && ( CKEDITOR.env.ie7Compat || CKEDITOR.env.ie6Compat ) )
						{
							return function( name, value )
							{
								if ( name == 'class' )
									this.$.className = value;
								else if ( name == 'style' )
								{
									this.$.style.cssText = value;
									/**
									 * Style attribute protection
									 * @author MindTouch
									 */
									setSavedStyles( this, value );
									/* END */
								}
								else if ( name == 'tabindex' )	// Case sensitive.
									this.$.tabIndex = value;
								else if ( name == 'checked' )
									this.$.checked = value;
								else
									standard.apply( this, arguments );
								return this;
							};
						}
						else
							return standard;
					})(),

					removeAttribute : (function()
					{
						var standard = function( name )
						{
							this.$.removeAttribute( name );
							/**
							 * Style attribute protection
							 * @author MindTouch
							 */
							if ( name == 'style' && this.hasAttribute( 'data-cke-saved-style' ) )
								this.$.removeAttribute( 'data-cke-saved-style' );
							/* END */
						};

						if ( CKEDITOR.env.ie && ( CKEDITOR.env.ie7Compat || CKEDITOR.env.ie6Compat ) )
						{
							return function( name )
							{
								if ( name == 'class' )
									name = 'className';
								else if ( name == 'tabindex' )
									name = 'tabIndex';
								standard.call( this, name );
							};
						}
						else
							return standard;
					})(),

					removeStyle : function( name )
					{
						/**
						 * Style attribute protection
						 * @author MindTouch
						 */
						var savedStyle;

						if ( !this.hasAttribute( 'data-cke-saved-style' ) )
						{
							this.setStyle( name, '' );
							if ( this.$.style.removeAttribute )
								this.$.style.removeAttribute( CKEDITOR.tools.cssStyleToDomStyle( name ) );

							savedStyle = this.$.style.cssText;
						}
						else
						{
							var savedStyles = getSavedStyles( this );

							if ( savedStyles[name] )
								delete savedStyles[name];

							savedStyle = buildStyleText( savedStyles );
							var	style = CKEDITOR.style.getStyleText( { 'styles' : savedStyles } );

							this.$.style.cssText = style;
						}

						if ( !savedStyle )
							this.removeAttribute( 'style' );
						else if ( checkDocument( this ) )
							this.$.setAttribute( 'data-cke-saved-style', savedStyle );
						/* END */
					},

					setStyle : function( name, value )
					{
						/**
						 * Style attribute protection
						 * @author MindTouch
						 */
						// this.$.style[ CKEDITOR.tools.cssStyleToDomStyle( name ) ] = value;

						var savedStyle;

						if ( !this.hasAttribute( 'data-cke-saved-style' ) )
						{
							this.$.style[ CKEDITOR.tools.cssStyleToDomStyle( name ) ] = value;
							savedStyle = this.$.style.cssText;
						}
						else
						{
							var savedStyles = getSavedStyles( this );
							savedStyles[name] = value;

							savedStyle = buildStyleText( savedStyles );
							var	style = CKEDITOR.style.getStyleText( {'styles' : savedStyles} );

							this.$.style.cssText = style;
						}

						if ( checkDocument( this ) )
							this.$.setAttribute( 'data-cke-saved-style', savedStyle );
						/* END */

						return this;
					},

					copyAttributes : function( dest, skipAttributes )
					{
						var attributes = this.$.attributes;
						skipAttributes = skipAttributes || {};

						for ( var n = 0 ; n < attributes.length ; n++ )
						{
							var attribute = attributes[n];

							// Lowercase attribute name hard rule is broken for
							// some attribute on IE, e.g. CHECKED.
							var attrName = attribute.nodeName.toLowerCase(),
								attrValue;

							// We can set the type only once, so do it with the proper value, not copying it.
							if ( attrName in skipAttributes )
								continue;

							if( attrName == 'checked' && ( attrValue = this.getAttribute( attrName ) ) )
								dest.setAttribute( attrName, attrValue );
							// IE BUG: value attribute is never specified even if it exists.
							else if ( attribute.specified ||
							  ( CKEDITOR.env.ie && attribute.nodeValue && attrName == 'value' ) )
							{
								attrValue = this.getAttribute( attrName );
								if ( attrValue === null )
									attrValue = attribute.nodeValue;

								dest.setAttribute( attrName, attrValue );
							}
						}

						// The style:
						/**
						 * Style attribute protection
						 * @author MindTouch
						 */
						if ( this.$.style.cssText.length || this.hasAttribute( 'data-cke-saved-style' ) )
							dest.$.style.cssText = this.getAttribute( 'data-cke-saved-style' ) || this.$.style.cssText;
						/* END */
					}

				}, true
			);

		},

		init : function( editor )
		{
			editor.on( 'contentDom', function( evt )
				{
					editor.document.on( 'mousedown', function( ev )
						{
							mouseClickTarget = ev.data.getTarget();
						});

					editor.document.on( 'mouseup', onMouseUp );
				});
		}
	});
})();
