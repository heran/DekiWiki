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
 * @file Save plugin.
 */

(function()
{
	function unmaximize( editor )
	{
		var maximizeCommand = editor.getCommand( 'maximize' );

		if ( maximizeCommand && maximizeCommand.state == CKEDITOR.TRISTATE_ON )
		{
			editor.execCommand( 'maximize' );
		}
	}

	var saveCmd =
	{
		modes : {wysiwyg : 1, source : 1},
		
		editorFocus : false,

		exec : function( editor )
		{
			this.editor = editor;

			unmaximize( editor );

			if ( Deki.Plugin )
			{
				Deki.Plugin.Publish( 'Editor.checkPermissions', [ this.save, this ] );
			}
			else
			{
				this.save();
			}
		},

		save : function()
		{
			var editor = this.editor,
				$form = editor.element.$.form;

			if ( $form && !editor.config.mindtouch.isReadOnly )
			{
				editor.fire( 'save' );

				try
				{
					$form.submit();
				}
				catch( e )
				{
					// If there's a button named "submit" then the form.submit
					// function is masked and can't be called in IE/FF, so we
					// call the click() method of that button.
					if ( $form.submit.click )
						$form.submit.click();
				}
			}
		},
		
		canUndo : false
	};
	
	var cancelCmd =
	{
		exec : function( editor )
		{
			// use checkDirty event instead of editor.checkDirty
			// to add custom checks on dirty
			var isDirty = editor.fire( 'checkDirty', {isDirty : false} ).isDirty;
			
			if ( isDirty )
			{
				editor.openDialog( 'confirmcancel' );
			}
			else
			{
				unmaximize( editor );
				editor.fire( 'cancel' );
			}
		},

		canUndo: false,

		editorFocus : false,

		modes : {wysiwyg : 1, source : 1}
	};


	CKEDITOR.plugins.add( 'mindtouchsave',
	{
		lang : [ 'en' ], // @Packager.RemoveLine

		requires : [ 'dialog' ],
		
		init : function( editor )
		{
			var lang = editor.lang.cancel;

			editor.addCommand( 'mindtouchsave', saveCmd );
			editor.addCommand( 'mindtouchcancel', cancelCmd );
			
			var keystrokes = editor.keystrokeHandler.keystrokes;
			keystrokes[ CKEDITOR.CTRL + CKEDITOR.SHIFT + 83 /*S*/ ] = 'mindtouchsave';

			editor.ui.addButton( 'MindTouchSave',
				{
					label : editor.lang.save,
					command : 'mindtouchsave'
				});
			
			editor.ui.addButton( 'MindTouchCancel',
					{
						label : lang.button,
						command : 'mindtouchcancel',
						icon : editor.config.mindtouch.editorPath + '/images/icons.png',
						iconOffset : 4
					});

			CKEDITOR.dialog.add( 'confirmcancel', function( editor )
			{
				return {
					title : lang.discardChangesTitle,
					minWidth : 290,
					minHeight : 90,
					onShow : function()
					{
						this.getButton( 'ok' ).focus();
					},
					contents : [
						{
							id : 'info',
							label : editor.lang.discardChangesTitle,
							title : editor.lang.discardChangesTitle,
							elements : [
								{
									id : 'warning',
									type : 'html',
									html : lang.confirmCancel
								}
							]
						}
					],
					buttons : [
						CKEDITOR.dialog.cancelButton.override(
							{
								label : lang.continueEditing,
								'class' : 'cke_dialog_ui_button_continue'
							}),
						// the default handler of OK button fires 'saveSnapshot' event
						// which is fired after editor destroying
						CKEDITOR.dialog.okButton.override(
							{
								label : lang.discardChanges,
								'class' : 'cke_dialog_ui_button_discard',
								onClick : function()
								{
									unmaximize( editor );
									editor.fire( 'cancel' );
								}
							})
					]
				};
			});
		}
	});
})();
