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
 * @file Plugin with overridden functions of CKEditor core.
 */

(function()
{	
	CKEDITOR.plugins.add( 'ckoverrides',
	{
		beforeInit : function( editor )
		{
			CKEDITOR.tools.extend( CKEDITOR.dom.node.prototype,
				{
					clone : function( includeChildren, cloneId )
					{
						var $clone = this.$.cloneNode( includeChildren );

						var removeIds = function( node )
						{
							if ( node.nodeType != CKEDITOR.NODE_ELEMENT )
								return;

							if ( !cloneId )
								node.removeAttribute( 'id', false );
							node.removeAttribute( 'data-cke-expando', false );

							/**
							 * Remove DekiScript attributes
							 *
							 * @author MindTouch
							 * @see #5768
							 * @link http://bugs.developer.mindtouch.com/view.php?id=5768
							 */
							node.removeAttribute( 'function', false );
							node.removeAttribute( 'block', false );
							node.removeAttribute( 'init', false );
							node.removeAttribute( 'foreach', false );
							node.removeAttribute( 'if', false );
							node.removeAttribute( 'where', false );
							node.removeAttribute( 'ctor', false );
							/* END */


							if ( includeChildren )
							{
								var childs = node.childNodes;
								for ( var i=0; i < childs.length; i++ )
									removeIds( childs[ i ] );
							}
						};

						// The "id" attribute should never be cloned to avoid duplication.
						removeIds( $clone );

						return new CKEDITOR.dom.node( $clone );
					}
				}, true
			);
		},
		
		init : function( editor )
		{
		}
	});
})();
