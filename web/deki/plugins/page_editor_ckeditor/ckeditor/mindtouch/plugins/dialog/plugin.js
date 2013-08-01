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
 * @file Wrapper for mindtouch dialog.
 */

(function()
{
	CKEDITOR.plugins.add( 'mindtouchdialog',
	{
		/**
		 * Dialog command. It opens a specific mindtouch dialog when executed.
		 * @constructor
		 * @param {string} dialogName The name of the dialog to open when executing
		 *		this command.
		 * @param {Object} dialogParams
		 * @example
		 * // Register the "link" command, which opens the "link" dialog.
		 * editor.addCommand( 'mindtouchlink', <b>new CKEDITOR.plgins.get( 'mindtouchdialog' ).openDialog( 'mindtouchlink', { url : '/skins/common/popups/link_dialog.php' } )</b> );
		 */
		openDialog : function( editor, dialogName, dialogParams )
		{
			if (!Deki.Dialog)
			{
				return;
			}
			
			this.dialogName = dialogName;
			this.dialogParams = CKEDITOR.tools.extend( dialogParams,
				{
					'height' : 'auto',
					'buttons' : [
						Deki.Dialog.BTN_OK,
						Deki.Dialog.BTN_CANCEL
					],
					extra : {}
				});
			
			var params =
				{
					'src' : this.dialogParams.url,
					'width' : this.dialogParams.width,
					'height' : this.dialogParams.height,
					'buttons' : this.dialogParams.buttons,
					'args' : this.dialogParams.params,
					'callback' : function()
						{
							editor.focus();

							if ( editor.mode == 'wysiwyg' && CKEDITOR.env.ie )
							{
								var selection = editor.getSelection();
								selection && selection.unlock( true );
							}

							if ( arguments[0] )
							{
								this.dialogParams.callback.apply( this.dialogParams.scope, arguments );
							}
						},
					'forceCallback' : true,
					'scope' : this
				};
				
			CKEDITOR.tools.extend( params, this.dialogParams.extra );

			var dialog = new Deki.Dialog( params ) ;

			dialog.render();

			if ( editor.mode == 'wysiwyg' && CKEDITOR.env.ie )
			{
				var selection = editor.getSelection();
				selection && selection.lock();

				/*
				 * IE BUG: If the initial focus went into a non-text element (e.g. button),
				 * then IE would still leave the caret inside the editing area.
				 */
				var $selection = editor.document.$.selection,
					$range = $selection.createRange();

				if ( $range )
				{
					if ( $range.parentElement && $range.parentElement().ownerDocument == editor.document.$
					  || $range.item && $range.item( 0 ).ownerDocument == editor.document.$ )
					{
						var $myRange = document.body.createTextRange();
						$myRange.moveToElementText( dialog._oContainer );
						$myRange.collapse( true );
						$myRange.select();
					}
				}
			}

			dialog.show();
		}
	});
})();
