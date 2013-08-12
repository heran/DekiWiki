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

if ( CKEDITOR.editorConfig )
{
	CKEDITOR.customEditorConfigFn = CKEDITOR.editorConfig;
}

CKEDITOR.editorConfig = function( config )
{
	config.bodyId = 'topic';
	config.bodyClass = 'deki-content-edit';

	config.plugins = 'about,' +
		'a11yhelp,' +
		'basicstyles,' +
		'bidi,' +
		'blockquote,' +
		'button,' +
		'clipboard,' +
		'colorbutton,' +
		'colordialog,' +
		'contextmenu,' +
		'dialogadvtab,' +
		'div,' +
		'elementspath,' +
		'enterkey,' +
		'entities,' +
		'filebrowser,' +
		'find,' +
		'flash,' +
		'font,' +
		'format,' +
		'forms,' +
		'horizontalrule,' +
		'htmldataprocessor,' +
		'iframe,' +
		'image,' +
		'indent,' +
		'justify,' +
		'keystrokes,' +
		'link,' +
		'list,' +
		'liststyle,' +
		'maximize,' +
		'newpage,' +
		'pagebreak,' +
		'pastefromword,' +
		'pastetext,' +
		'popup,' +
		'preview,' +
		'print,' +
		'removeformat,' +
		'resize,' +
		'save,' +
//		'scayt,' +
		'smiley,' +
		'showblocks,' +
		'showborders,' +
		'sourcearea,' +
		'stylescombo,' +
		'table,' +
		'tabletools,' +
		'specialchar,' +
		'tab,' +
		'templates,' +
		'toolbar,' +
		'undo,' +
		'wysiwygarea,' +
//		'wsc,' +
		'attachimage,' +
		'autogrow,' +
		'autosave,' +
		'ckoverrides,' +
		'combobutton,' +
		'definitionlist,' +
		'dsbar,' +
		'extensions,' +
		'floatingtoolbar,' +
		'inlinestyle,' +
		'menubuttons,' +
		'mindtouch,' +
		'mindtouchdialog,' +
		'mindtouchimage,' +
		'mindtouchlink,' +
		'mindtouchkeystrokes,' +
		'mindtouchsave,' +
		'mindtouchtemplates,' +
		'styletools,' +
		'tableadvanced,' +
		'transformations,' +
		'video,' +
		'wrapstyle';

	config.theme = 'mindtouch';
	config.resize_enabled = false;

	config.entities = false;
	config.entities_greek = false;
	config.entities_latin = false;
	
	config.startupFocus = true;
	
	config.format_tags = 'p;pre;h1;h2;h3;h4;h5;h6';

	config.tabSpaces = 4;

	config.format_p = {element : 'p', attributes : {'class' : ''}};

	config.stylesSet = [
		{name : 'Normal'				, element : 'p', attributes : {'class' : ''}},
		{name : 'Formatted'			, element : 'pre'},
		{name : 'Block Quote'			, element : 'blockquote'},
		{name : 'Plaintext (nowiki)'	, element : 'span',	attributes : {'class' : 'plain'}}
	];

	config.toolbar_Everything =
		[
			['MindTouchSave','MindTouchCancel'],
			['ViewMenu'],
			['NewPage','Preview'],
			['Cut','Copy','Paste','PasteText','PasteFromWord','-','Print','ATDSpellChecker'],
			['Transformations'],
			['Undo','Redo','-','Find','Replace','-','SelectAll','RemoveFormat'],
			'/',
			['Bold','Italic','Underline','Strike','-','Subscript','Superscript','Code'],
			['NumberedList','BulletedList','-','DefinitionList','DefinitionTerm','DefinitionDescription','-','Outdent','Indent','CreateDiv','Iframe'],
			['JustifyLeft','JustifyCenter','JustifyRight','JustifyBlock'],
			['BidiLtr', 'BidiRtl'],
			'/',
			['Font','FontSize','-','TextColor','BGColor'],
			['Form', 'Checkbox', 'Radio', 'TextField', 'Textarea', 'Select', 'Button', 'ImageButton', 'HiddenField'],
			['HorizontalRule','Smiley','SpecialChar'],
			['Extensions'],
			'/',
			['Normal','H1','H2','H3','Hx','Styles'],
			['InsertMenu','MindTouchLink','Unlink','Anchor','AttachImage','Table','MindTouchImage','MindTouchTemplates','Video'],
			['Maximize','-','About']
		];
	
	config.toolbar_Default =
		[
			['MindTouchSave','MindTouchCancel'],
			['ViewMenu'],
			['Undo','Redo'],
			['Replace'],
			['Cut','Copy','Paste','PasteText','PasteFromWord'],
			['ATDSpellChecker'],
			['Transformations'],
			['Maximize'],
			'/',
			['Font','FontSize','-','TextColor','BGColor','-','RemoveFormat'],
			['Bold','Italic','Underline','Strike','Subscript','Superscript','Code'],
			['NumberedList','BulletedList','DefinitionList','-','Outdent','Indent'],
			['JustifyLeft','JustifyCenter','JustifyRight','JustifyBlock'],
			'/',
			['Normal','H1','H2','H3','Hx','Styles'],
			['InsertMenu','MindTouchLink','Unlink','Table','MindTouchImage','Extensions','MindTouchTemplates','Video']
		];

	config.toolbar_Advanced =
		[
			['MindTouchSave','MindTouchCancel'],
			['ViewMenu'],
			['Undo','Redo','-','Find','Replace','-','SelectAll','RemoveFormat'],
			['Cut','Copy','Paste','PasteText','PasteFromWord'],
			['ATDSpellChecker'],
			['Transformations'],
			['Maximize'],
			'/',
			['Font','FontSize'],
			['Bold','Italic','Underline','Strike','Subscript','Superscript','Code'],
			['NumberedList','BulletedList','-','DefinitionList','DefinitionTerm','DefinitionDescription','-','Outdent','Indent','CreateDiv'],
			['JustifyLeft','JustifyCenter','JustifyRight','JustifyBlock'],
			['TextColor','BGColor'],
			'/',
			['Normal','H1','H2','H3','Hx','Styles'],
			['InsertMenu','MindTouchLink','Unlink','AttachImage','Table','MindTouchImage','Extensions','MindTouchTemplates','Video']
		];

	config.toolbar_Simple =
		[
			['MindTouchSave','MindTouchCancel'],
			['ViewMenu'],
			['PasteText','PasteFromWord'],
			['ATDSpellChecker'],
			['Bold','Italic','Underline','Strike','Code'],
			['NumberedList','BulletedList'],
			['JustifyLeft','JustifyCenter','JustifyRight','JustifyBlock'],
			['TextColor','BGColor','-','RemoveFormat'],
			['Transformations'],
			'/',
			['Normal','H1','H2','H3','Hx','Styles'],
			['InsertMenu','MindTouchLink','Table','MindTouchImage','Extensions','MindTouchTemplates','Video'],			
			['Maximize']
		];

	config.toolbar_Basic =
		[
			['MindTouchSave','MindTouchCancel'],
			['ViewMenu'],
			['ATDSpellChecker'],
			['InsertMenu','MindTouchLink','Unlink','Table','MindTouchImage','Extensions','MindTouchTemplates','Video'],
			['Maximize']
		];

	config.toolbar_ReadOnly =
		[
			['MindTouchCancel','-','Source'],
			['Maximize']
		];
	
	config.keystrokes =
		[
			[ CKEDITOR.ALT + 121 /*F10*/, 'toolbarFocus' ],
			[ CKEDITOR.ALT + 122 /*F11*/, 'elementsPathFocus' ],

			[ CKEDITOR.SHIFT + 121 /*F10*/, 'contextMenu' ],
			[ CKEDITOR.CTRL + CKEDITOR.SHIFT + 121 /*F10*/, 'contextMenu' ],

			[ CKEDITOR.CTRL + 90 /*Z*/, 'undo' ],
			[ CKEDITOR.CTRL + 89 /*Y*/, 'redo' ],
			[ CKEDITOR.CTRL + CKEDITOR.SHIFT + 90 /*Z*/, 'redo' ],

			[ CKEDITOR.CTRL + 66 /*B*/, 'bold' ],
			[ CKEDITOR.CTRL + 73 /*I*/, 'italic' ],
			[ CKEDITOR.CTRL + 85 /*U*/, 'underline' ],

			[ CKEDITOR.ALT + 109 /*-*/, 'toolbarCollapse' ],
			[ CKEDITOR.ALT + 48 /*0*/, 'a11yHelp' ],
			
			[ CKEDITOR.CTRL + 76 /*L*/, 'justifyleft' ],
			[ CKEDITOR.CTRL + 69 /*E*/, 'justifycenter' ],
			[ CKEDITOR.CTRL + 82 /*R*/, 'justifyright' ],
			[ CKEDITOR.CTRL + 74 /*J*/, 'justifyblock' ],
			
			[ CKEDITOR.CTRL + CKEDITOR.ALT + 13 /*ENTER*/, 'maximize' ],
			[ 122 /*F11*/, 'maximize' ]
		];
		
	if ( CKEDITOR.plugins )
	{
		var mindtouchSourcePath = CKEDITOR.basePath + 'mindtouch/plugins';
		CKEDITOR.plugins.addExternal( 'atdspellchecker', mindtouchSourcePath + '/atd/' );
		CKEDITOR.plugins.addExternal( 'attachimage', mindtouchSourcePath + '/attachimage/' );
		CKEDITOR.plugins.addExternal( 'autogrow', mindtouchSourcePath + '/autogrow/' );
		CKEDITOR.plugins.addExternal( 'autosave', mindtouchSourcePath + '/autosave/' );
		CKEDITOR.plugins.addExternal( 'ckoverrides', mindtouchSourcePath + '/ckoverrides/' );
		CKEDITOR.plugins.addExternal( 'codeassist', mindtouchSourcePath + '/codeassist/' );
		CKEDITOR.plugins.addExternal( 'combobutton', mindtouchSourcePath + '/combobutton/' );
		CKEDITOR.plugins.addExternal( 'definitionlist', mindtouchSourcePath + '/definitionlist/' );
		CKEDITOR.plugins.addExternal( 'dsbar', mindtouchSourcePath + '/dsbar/' );
		CKEDITOR.plugins.addExternal( 'extensions', mindtouchSourcePath + '/extensions/' );
		CKEDITOR.plugins.addExternal( 'floatingtoolbar', mindtouchSourcePath + '/floatingtoolbar/' );
		CKEDITOR.plugins.addExternal( 'infobar', mindtouchSourcePath + '/infobar/' );
		CKEDITOR.plugins.addExternal( 'infopanel', mindtouchSourcePath + '/infopanel/' );
		CKEDITOR.plugins.addExternal( 'inlinestyle', mindtouchSourcePath + '/inlinestyle/' );
		CKEDITOR.plugins.addExternal( 'label', mindtouchSourcePath + '/label/' );
		CKEDITOR.plugins.addExternal( 'menubuttons', mindtouchSourcePath + '/menubuttons/' );
		CKEDITOR.plugins.addExternal( 'mindtouch', mindtouchSourcePath + '/mindtouch/' );
		CKEDITOR.plugins.addExternal( 'mindtouchdevtools', mindtouchSourcePath + '/devtools/' );
		CKEDITOR.plugins.addExternal( 'mindtouchdialog', mindtouchSourcePath + '/dialog/' );
		CKEDITOR.plugins.addExternal( 'mindtouchimage', mindtouchSourcePath + '/image/' );
		CKEDITOR.plugins.addExternal( 'mindtouchlink', mindtouchSourcePath + '/link/' );
		CKEDITOR.plugins.addExternal( 'mindtouchkeystrokes', mindtouchSourcePath + '/keystrokes/' );
		CKEDITOR.plugins.addExternal( 'mindtouchsave', mindtouchSourcePath + '/save/' );
		CKEDITOR.plugins.addExternal( 'mindtouchtemplates', mindtouchSourcePath + '/templates/' );
		CKEDITOR.plugins.addExternal( 'styletools', mindtouchSourcePath + '/style/' );
		CKEDITOR.plugins.addExternal( 'tableadvanced', mindtouchSourcePath + '/table/' );
		CKEDITOR.plugins.addExternal( 'tableconvert', mindtouchSourcePath + '/tableconvert/' );
		CKEDITOR.plugins.addExternal( 'tableoneclick', mindtouchSourcePath + '/tableoneclick/' );
		CKEDITOR.plugins.addExternal( 'tablesort', mindtouchSourcePath + '/tablesort/' );
		CKEDITOR.plugins.addExternal( 'transformations', mindtouchSourcePath + '/transformations/' );
		CKEDITOR.plugins.addExternal( 'video', mindtouchSourcePath + '/video/' );
		CKEDITOR.plugins.addExternal( 'wrapstyle', mindtouchSourcePath + '/wrapstyle/' );
	}
	
	CKEDITOR.themes && CKEDITOR.themes.addExternal( 'mindtouch', CKEDITOR.basePath + 'mindtouch/themes/mindtouch/' );


	if ( CKEDITOR.customEditorConfigFn )
	{
		CKEDITOR.customEditorConfigFn.call( this, config );
		delete CKEDITOR.customEditorConfigFn;
	}
};
