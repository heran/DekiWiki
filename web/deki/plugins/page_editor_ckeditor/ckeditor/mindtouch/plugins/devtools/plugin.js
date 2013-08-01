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

CKEDITOR.plugins.add( 'mindtouchdevtools',
{
	lang : [ 'en' ],
	
	requires : [ 'infobar' ],

	init : function( editor )
	{
		if ( !editor.config.mindtouch.startLoadTime )
		{
			return;
		}
		
		editor.on( 'themeLoaded', function()
			{
				var infoBar = editor.ui.infobar;
				infoBar.addGroup( 'devtools', 2 );
				infoBar.addLabel( 'devtools', 'loadTime' );
			});
		
		editor.on( 'instanceReady', function()
			{
				var loadTime = new Date().getTime() - Deki.Plugin.Editor.StartLoadTime;
				
				editor.ui.infobar.updateLabel( 'devtools', 'loadTime', editor.lang.mindtouchdevtools.editorLoadTime.replace( '%1', loadTime ) );
				editor.ui.infobar.showGroup( 'devtools' );
			});
	}
});

CKEDITOR.plugins.setLang( 'mindtouchdevtools', 'en',
{
	mindtouchdevtools :
		{
			'editorLoadTime' : 'Editor load time: %1 ms'
		}
});
