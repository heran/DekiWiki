<?php
/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com oss@mindtouch.com
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

// define('DEKI_ADMIN', true); // entry point variable only to be set in calling scripts

class DekiControlPanelConfig
{
	const DEBUG				= false;
	const PRETTY_URLS		= false;

	// file to use as the template layout. layout => /templates/layout.php
	const TEMPLATE_NAME		= 'template';
	// location of the admin directory as seen in the url => http://site[/webroot]
	const WEB_ROOT			= '/deki/cp';
	
	const DREAM_SERVER = 'http://localhost:8081';
	const DEKI_API = 'deki';

	// customization values
	// determines the number of results to display in the listing views
	const RESULTS_PER_PAGE	= 20;
	const SERVICES_PER_PAGE	= 50;
	// determines the number of paging elements to show, see bottom of results
	const RESULTS_PAGING_COUNT = 7;

	
	// api key
	static $API_KEY = '';
	// sets the root application folder
	static $APP_ROOT = '';
	// sets the deki root folder
	static $DEKI_ROOT = '';
	// sets the mask of levels to debug against
	static $DEBUG_REPORTING = E_ERROR;
	
	// map group & controller to URLs to the user manual
	static $DEKI_CP_HELP_LINKS = array(
		'dashboard/dashboard' => 'Dashboard',
		'dashboard/dashboard#index' => 'Dashboard',
		'dashboard/dashboard#blog' => 'Dashboard#Blog',
		'dashboard/dashboard#videos' => 'Dashboard#Technical_News', 
		'users/user_management' => 'Users_and_Groups/Users', 
		'users/user_management#listing' => 'Users_and_Groups/Users', 
		'users/user_management#add' => 'Users_and_Groups/Users#Add_User', 
		'users/user_management#add_multiple' => 'Users_and_Groups/Users#Add_Multiple_Users',
		'users/group_management' => 'Users_and_Groups/Groups', 
		'users/group_management#add' => 'Users_and_Groups/Groups#Add_Group',		
		'users/role_management' => 'Users_and_Groups/Roles', 
		'users/bans' => 'Users_and_Groups/Bans', 
		'users/bans#add' => 'Users_and_Groups/Bans#Add_Ban', 
		'custom/skinning' => 'Customize/Logos_and_Skins', 
		'custom/custom_css' => 'Customize/Custom_CSS', 
		'custom/custom_html' => 'Customize/Custom_HTML', 
		'maint/page_restore' => 'Maintenance_and_History/Deleted_Pages', 
		'maint/file_restore' => 'Maintenance_and_History/Deleted_Files',
		'maint/cache_management' => 'Maintenance_and_History/Cache_Management', 
		'settings/configuration' => 'System_Settings/Configuration', 
		'settings/configuration#listing' => 'System_Settings/Configuration#Advanced_Settings', 
		'settings/extensions' => 'System_Settings/Extensions', 
		'settings/extensions#add_script' => 'System_Settings/Extensions#Add_Script', 
		'settings/extensions#add' => 'System_Settings/Extensions#Add_Extension', 
		'settings/kaltura_video' => 'System_Settings/Kaltura_video', 
		'settings/authentication' => 'System_Settings/Authentication', 
		'settings/authentication#add' => 'System_Settings/Authentication#Add_Authentication_Service', 
		'settings/email_settings' => 'System_Settings/Email_Settings', 
		'settings/analytics' => 'System_Settings/Google_Analytics', 
		'settings/editor_config' => 'System_Settings/Editor', 
		'settings/product_activation' => 'System_Settings/Product_Activation'		
	);
}

// below assumes the application is located in a subdirectory of web root
DekiControlPanelConfig::$APP_ROOT = dirname(dirname(__FILE__));
DekiControlPanelConfig::$DEKI_ROOT = dirname(dirname(DekiControlPanelConfig::$APP_ROOT));
