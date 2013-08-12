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

Deki.Plugin = Deki.Plugin || {};

Deki.Plugin.CKEditor = function( editAreaId )
{
	Deki.Plugin.CKEditor.superclass.constructor.call( this, editAreaId );
	this.Name = "CKEditor";
}

Deki.Plugin.Editor && jQuery.extendClass( Deki.Plugin.CKEditor, Deki.Plugin.Editor );

Deki.Plugin.CKEditor.prototype.CreateEditor = function()
{
	var oSelf = this;

	if ( CKEDITOR.mindtouch_status && CKEDITOR.mindtouch_status != 'loaded' )
	{
		setTimeout(function() { oSelf.CreateEditor(); }, 50);
		return;
	}

	if ( CKEDITOR.status != 'loaded' && !Deki.EditorCKSource )
	{
		return;
	}

	this.AddCheckDirtyFunction(function()
		{
			if ( this.Instance )
			{
				return this.Instance.checkDirty();
			}

			return false;
		});

	// Configuration
	var config = 
		{
			customConfig : Deki.Plugin.AJAX_URL + '?formatter=page_editor_config&format=custom&tt=' + Deki.EditorConfigToken,
			contentsCss : Deki.Plugin.AJAX_URL + '?formatter=page_editor_styles&format=custom&tt=' + Deki.EditorStylesToken,
			mindtouch :
			{
				editorPath : Deki.EditorPath,
				commonPath : Deki.PathCommon,
				userName : Deki.UserName,
				userIsAnonymous : Deki.UserIsAnonymous,
				today : Deki.Today,
				isReadOnly : this.ReadOnly,
				pageTitle : Deki.PageTitle,
				pageId : Deki.PageId,
				pageRevision : Deki.PageRevision,
				sectionId : this.CurrentSection,
				startLoadTime : Deki.Plugin.Editor.StartLoadTime
			},
			
			on :
			{
	            save : function()
	            {
	            	oSelf.Save.call(oSelf);
	            },
	            
	            cancel : function( ev )
	            {
					oSelf.Cancel.call(oSelf);
	            },

				checkDirty : function( ev )
				{
					ev.data.isDirty = oSelf.CheckDirty();
					ev.stop();
				},

				configLoaded : function( ev )
				{
					var editor = ev.editor;

					editor.config.extraPlugins = editor.config.extraPlugins.replace( /\s*,\s*/g, ',' );

					if ( Deki.atdEnabled === true )
					{
						editor.config.extraPlugins += ( editor.config.extraPlugins.length ? ',' : '' ) + 'atdspellchecker';
					}
				},

				focus : function()
				{
					DWMenu.BodyClick();
				},

				destroy : function()
				{
					delete CKEDITOR.editorConfig;
				}
			}
		};

	if ( Deki.EditorLang )
	{
		var EditorLang = Deki.EditorLang;
		
		if ( !(EditorLang in CKEDITOR.lang.languages) )
		{
			EditorLang = Deki.EditorLang.split('-');
			EditorLang = EditorLang[0];
		}
		
		if ( EditorLang in CKEDITOR.lang.languages )
		{
			config.language = EditorLang;
		}
	}

	config.language = config.language || 'en';
	config.toolbar = ( this.ReadOnly ) ? 'ReadOnly' : Deki.EditorToolbarSet;
	
	var mindtouchSourcePath = Deki.EditorPath + '/ckeditor/mindtouch/plugins';
	
	if ( !Deki.EditorCKSource )
	{
		$.getScript(CKEDITOR.getUrl( 'lang/' + config.language + '.js' ), function()
			{
				CKEDITOR.tools.extend( CKEDITOR.lang[ config.language ], Deki.EditorLangs );

				oSelf.Instance = CKEDITOR.replace( oSelf.EditArea, config );
			});
	}
	else
	{
		this.Instance = CKEDITOR.replace( oSelf.EditArea, config );
	}

	var $newPageTitle = $( '#deki-new-page-title' );

	CKEDITOR.on( 'editorLoaded', function()
		{
			window.setTimeout( function()
			{
				$newPageTitle.focus();
				$newPageTitle.select();
			}, 0 );
		});

	$newPageTitle.keypress( function( ev )
		{
			if ( ev.which == 13 )
			{
				try
				{
					oSelf.Instance.focus();
				}
				catch( ex ) {}

				return false;
			}
			
			return true;
		});
}

Deki.Plugin.CKEditor.prototype.BeforeCancel = function()
{
	if ( this.Instance )
	{
		this.Instance.destroy();
		this.Instance = null;
	}
}

Deki.Plugin.CKEditor.prototype.IsSupported = function()
{
	return CKEDITOR.env.isCompatible;
}

Deki.EditorInstance = new Deki.Plugin.CKEditor( 'editarea' );

if (CKEDITOR)
{
	// override the original function to add our timestamp
	CKEDITOR.loadFullCore = function()
	{
		// If not the basic code is not ready it, just mark it to be loaded.
		if ( CKEDITOR.status != 'basic_ready' )
		{
			CKEDITOR.loadFullCore._load = 1;
			return;
		}
	
		// Destroy this function.
		delete CKEDITOR.loadFullCore;
	
		// Append the script to the head.
		var script = document.createElement( 'script' );
		script.type = 'text/javascript';
		script.src = CKEDITOR.basePath + 'ckeditor.js';
		
		if ( Deki.EditorTimestamp )
		{
			script.src += '?tt=' + Deki.EditorTimestamp;
		}
	
		document.getElementsByTagName( 'head' )[0].appendChild( script );
	}
}
