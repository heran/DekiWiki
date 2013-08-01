/*
Copyright (c) 2003-2009, CKSource - Frederico Knabben. All rights reserved.
For licensing, see LICENSE.html or http://ckeditor.com/license
*/

/**
 * @file Floating toolbar plugin.
 */

(function()
{
	var dockToolbar = function( ev )
	{
		var editor = ev.data;
		
		var $toolbar = Deki.$( editor.getThemeSpace( 'top' ).$ );
		var $toolbarContainer = Deki.$( editor.container.$ );
		
		var docScrollY = Deki.$( CKEDITOR.document.$ ).scrollTop();
		
		var padding = parseInt( $toolbarContainer.css( 'padding-top' ) ) || 0;
		var containerY = $toolbarContainer.position().top + padding;
		
		var newToolbarY = null;

		if ( docScrollY > containerY )
		{
			var $editorContents = $( '#cke_contents_' + editor.name );
			var maxY = $editorContents.position().top + $editorContents.height() - $toolbar.height();

			if ( docScrollY < maxY )
			{
				newToolbarY = docScrollY - containerY;
			}
		}
		else
		{
			var toolbarY = $toolbar.position().top;

			if ( toolbarY != containerY )
			{
				newToolbarY = 0;
			}
		}
		
		if ( newToolbarY !== null )
		{
			$toolbar.css( 'top', newToolbarY + 'px' );
		}
	};
	
	function scrollToTop( ev )
	{
		var editor = ev.editor;
		
		if ( editor.config.floating_toolbar )
		{
			var win = CKEDITOR.document.getWindow(),
				container = editor.container,
				position = container.getDocumentPosition(),
				scroll = win.getScrollPosition(),
				$toolbar = Deki.$( editor.getThemeSpace( 'top' ).$ ),
				y = position.y - $toolbar.height();

			if ( y < 0 )
			{
				y = 0;
			}
			
			if ( scroll.y > position.y )
			{
				win.$.scrollTo( 0, y );
			}
		}
	}
	
	CKEDITOR.plugins.add( 'floatingtoolbar',
	{
		init : function( editor )
		{
			if ( editor.config.floating_toolbar )
			{
				var win = CKEDITOR.document.getWindow().$;
				
				editor.on( 'themeLoaded', function( ev )
		            {
		            	Deki.$( win ).bind( 'scroll', editor, dockToolbar );
		            });
				editor.on( 'destroy', function( ev )
		            {
		            	Deki.$( win ).unbind( 'scroll', dockToolbar );
		            });
				editor.on( 'mode', scrollToTop, null, null, 1 );
				editor.on( 'contentDom', scrollToTop, null, null, 1 );
				editor.on( 'save', scrollToTop, null, null, 1 );
				editor.on( 'cancel', scrollToTop, null, null, 1 );
				editor.on( 'scrollToTop', scrollToTop, null, null, 1 );
			}
		}
	});
})();

/**
 * Whether to enable the floating toolbar feature.
 * @type Boolean
 * @default true
 * @see config.autogrow
 * @example
 * config.floating_toolbar = false;
 */
CKEDITOR.config.floating_toolbar = true;
