﻿/*
Copyright (c) 2003-2010, CKSource - Frederico Knabben. All rights reserved.
For licensing, see LICENSE.html or http://ckeditor.com/license
@note MindTouch modified version of Office2003 skin
*/

CKEDITOR.skins.add( 'mindtouch', (function()
{
	return {
		editor		: { css : [ 'editor.css' ] },
		dialog		: { css : [ 'dialog.css' ] },
		templates	: { css : [ 'templates.css' ] },
		margins		: [ 0, 14, 18, 14 ]
	};
})() );

(function()
{
	CKEDITOR.dialog ? dialogSetup() : CKEDITOR.on( 'dialogPluginReady', dialogSetup );

	function dialogSetup()
	{
		CKEDITOR.dialog.on( 'resize', function( evt )
			{
				var data = evt.data,
					width = data.width,
					height = data.height,
					dialog = data.dialog,
					contents = dialog.parts.contents;

				if ( data.skin != 'mindtouch' )
					return;

				contents.setStyles(
					{
						width : width + 'px',
						height : height + 'px'
					});

				if ( !CKEDITOR.env.ie )
					return;

				// Fix the size of the elements which have flexible lengths.
				var fixSize = function()
					{
						var innerDialog = dialog.parts.dialog.getChild( [ 0, 0, 0 ] ),
							body = innerDialog.getChild( 0 ),
							bodyWidth = body.getSize( 'width' );
						height += body.getChild( 0 ).getSize( 'height' ) + 1;

						// tc
						var el = innerDialog.getChild( 2 );
						el.setSize( 'width', bodyWidth );

						// bc
						el = innerDialog.getChild( 7 );
						el.setSize( 'width', bodyWidth - 28 );

						// ml
						el = innerDialog.getChild( 4 );
						el.setSize( 'height', height );

						// mr
						el = innerDialog.getChild( 5 );
						el.setSize( 'height', height );
					};
				setTimeout( fixSize, 100 );

				// Ensure size is correct for RTL mode. (#4003)
				if ( evt.editor.lang.dir == 'rtl' )
					setTimeout( fixSize, 1000 );
			});
	}
})();
