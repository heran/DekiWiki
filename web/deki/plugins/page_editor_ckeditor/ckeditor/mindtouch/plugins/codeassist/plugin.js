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
 * @file Code assist for DekiScript
 */

(function()
{
	function initContentAssist( functions )
	{
		var editor = this,
			words = [],
			additionalInfo = {};

		for ( var i in functions )
		{
			var func = functions[ i ];
			words.push( func.name );

			var info = [];
			for ( var j in func.info )
			{
				if ( func.info[ j ].length )
				{
					info.push( '<div>' + func.info[ j ] + '</div>' );
				}
			}

			additionalInfo[ func.name ] = info.join( '' );
		}

		var viewer = new CKEditorViewer( editor ),
			processor = new DekiScriptAssistProcessor( words ),
			contentAssist = new ContentAssist( viewer, processor );

		processor.setAdditionalInfo( additionalInfo );

		editor.document.on( 'keyup', function( evt )
			{
				var keyCode = evt.data.getKeystroke();

				if ( keyCode == 27 || ( contentAssist.is_visible && keyCode in {38 : 1, 40 : 1} ) )
				{
					return;
				}

				var sel = editor.getSelection(),
					firstElement = sel && sel.getStartElement(),
					path = firstElement && new CKEDITOR.dom.elementPath( firstElement );

				if ( path && path.block && path.block.is( 'pre' ) && !path.block.hasClass( 'script' ) )
				{
					viewer.dispatcher.dispatchEvent( 'modify' );
				}
			});


		editor.document.on( 'mousedown', function()
			{
				contentAssist.hidePopup();
			});

		editor.on( 'key', function( ev )
			{
				if ( contentAssist.is_visible )
				{
					switch ( ev.data.keyCode )
					{
						case 38: //up
							contentAssist.selectProposal(Math.max(contentAssist.selected_proposal - 1, 0));
							contentAssist.lockHover();
							ev.cancel();
							break;
						case 40: //down
							contentAssist.selectProposal(Math.min(contentAssist.selected_proposal + 1, contentAssist.popup_content.childNodes.length - 1));
							contentAssist.lockHover();
							ev.cancel();
							break;
						case 13: //enter
							contentAssist.applyProposal(contentAssist.selected_proposal);
							contentAssist.hidePopup();
							ev.cancel();
							break;
						case 27: // escape
							contentAssist.hidePopup();
							ev.cancel();
							break;
					}
				}
				else if ( ev.data.keyCode == CKEDITOR.CTRL + 32 || ev.data.keyCode == CKEDITOR.ALT + 32 )
				{
					// ctrl+space or alt+space
					// explicitly show content assist
					contentAssist.showContentAssist();
					ev.cancel();
				}
			});
	}

	CKEDITOR.plugins.add( 'codeassist',
	{
		requires : [ 'keystrokes' ],
		
		init : function( editor )
		{
			var contentAssistLoaded = false,
				loadedFunctions;

			editor.on( 'contentDom', function()
				{
					(function loop()
						{
							setTimeout(function()
								{
									if ( contentAssistLoaded )
									{
										loadedFunctions && initContentAssist.call( editor, loadedFunctions );
									}
									else
									{
										loop();
									}

								}, 100);
						})();
				});


			// Load tx-content-assist files.
			CKEDITOR.scriptLoader.load( [
				this.path + 'tx-content-assist/tx_utils.js',
				this.path + 'tx-content-assist/EventDispatcher.js',
				this.path + 'tx-content-assist/CKEditorViewer.js',
				this.path + 'tx-content-assist/ContentAssist.js',
				this.path + 'tx-content-assist/DekiScriptAssistProcessor.js',
				this.path + 'tx-content-assist/CompletionProposal.js'
			], function()
				{
					Deki.Plugin.AjaxRequest( 'json_site_functions',
						{
							data :
							{
								'method' : 'functions'
							},
							success : function( data, status )
							{
								if ( !data.success )
								{
									return;
								}

								loadedFunctions = data.body;
							},

							complete : function()
							{
								contentAssistLoaded = true;
							}
						});
				});

			// Load tx-content-assist css files.
			editor.element.getDocument().appendStyleSheet( this.path + 'content-assist.css' );
		}
	});
})();
