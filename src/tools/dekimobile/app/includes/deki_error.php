<?php
/*
 * MindTouch Deki - enterprise collaboration and integration platform
 * Copyright (C) 2006 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
 *  please review the licensing section.
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
 * File defines and initializes the error handler & exception handler
 * Also sets up a special controller for beautifying hard error states
 *
 * @requires Deki MVC framework, Files {templates}/debug_error.php,error.php
 */

final class DekiError extends DekiController
{
	public static function handleError($errno, $errstr, $errfile = '', $errline = 0, $errcontext = array())
	{
		// check for @ suppression & debug error handling
		if (ini_get('error_reporting') == 0 || !(Config::$DEBUG_REPORTING & $errno))
		{
			return;
		}
			
		// throwing an exception within an error doesn't work, don't throw it
		self::handleException(new Exception($errstr, $errno));
		// halt execution
		exit();
	}
	
	public static function handleException(Exception $Exception)
	{
		// find the active controller
		global $wgAdminController;

		if (isset($wgAdminController))
		{
			$View = &$wgAdminController->View;
			// purge the partial view
			if ($View->isRendering())
			{
				ob_end_clean();
			}
		}
		else
		{
			// create a view
			$View = new DekiView(Config::$APP_ROOT . '/templates');
		}
		
		$file = (Config::DEBUG) ? 'debug_error.php' : 'error.php';
		$View->setViewFile('/' . $file);
		$View->setRef('error.exception', $Exception);

		$View->output();
	}
}

set_error_handler(array('DekiError', 'handleError'));
set_exception_handler(array('DekiError', 'handleException'));
