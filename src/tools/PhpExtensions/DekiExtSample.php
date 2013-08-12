<?php
/*
 * MindTouch Deki Wiki - a commercial grade open source wiki
 * Copyright (C) 2006, 2007 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
 *  please review the licensing section.
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 * http://www.gnu.org/copyleft/lesser.html
 */

//This is required in order to call the DekiExt function 
include('DekiExt.php');

/*
 * DekiExt function call is required in order to make your Php extensnon work in DekiWiki
 *
 *
 *	@param1 (type: string)- title of your Php service (required)
 * 	@param2 (type: array)- key (preset): description (optional), copyright(optional), uri.help(optional), 
 * 										 namespace (MUST INCLUDE, this is necessary info for your deki extension)
 * 	@param3 (type: array)- key: function information => value: php function name that it will call
 */
DekiExt(
		//title
		"Mindtouch Deki Extension Php Service", 

		//extension options
		array( 
			"description" => "description of my extension", 
			"copyright" => "my copyright", 
			"uri.help" => "http://mysite/myhelppage.html",
			"namespace" => "test"
		), 

		//function information to php function Name mapping 
		array(
			"Greeting(first:str, last:str):str" => "myGreeting", 
			"time(days:num):str" => "daysLater",
			"randLink(random:bool, link:str):str" => "randomTinyUrl",
			"philsOpen(day:str, open:str, closed:str):str" => "philsOpen",
			"send(to:str, subject:str, body:str):str" => "sendEmail"
		)
);

//call this function by writing it into your DekiWiki like this {{myNamespace.myGreeting("Tom")}}
//when you save your your DekiWiki, it will output this to the page: "Hello World, my name is Tom"
function myGreeting($args) 
{
	//the IXR_library only interprets parameters as 1 array so you can either
	//remap it like below or called the $args array by indicies to retrieve your parameters
	list($first, $last) = $args; 
	return 'Hello World, my name is '.$first.' '.$last;
}

function daysLater($arg)
{
	$offset = time() + ($arg * 24 * 60 * 60);
	return date('Y-m-d',$offset) ."\n";
}
function randomTinyUrl($args)
{
	list($isRandom, $uri) = $args;
	if($isRandom)
		$seed = rand(0, 5000);
	else
		$seed = $uri;
	return "http://tinyurl.com/".$seed;
}
function philsOpen($arg)
{
	list($day, $opening, $closing) = $arg;
	
	$timestamp = time() -7*3600;
	$now = gmdate('h a', $timestamp);
	$today = gmdate('D', $timestamp);
	$givenDay = gmdate('D', strtotime($day));
	$open = gmdate('h a',strtotime($opening));
	$close  = gmdate('h a',strtotime($closing));
	if($open == $close)
	{
		return 'closed';
	}
	else if((strtotime($today) == strtotime($givenDay)))
	{
		if(strtotime($close)-strtotime($now) < 0)
			return 'closed';
		else
			return 'open today, will close in: ' . gmdate('H',strtotime($close)-strtotime($now)). ' hours';
	}
	else
	{
		return $open . ' to ' . $close;
	}
	
}

//email function
function sendEmail($arg)
{
	list($to, $subject, $body) = $arg;

	// Additional Headers
	$headers = 'From: youremail@example.com'."\r\n".
				'Cc: yourContact@example.com'."\r\n";

	if(mail($to, $subject, $body, $headers))
	{
		return "Mail sent successfully.";
	}
	else
	{
		return "Was unable to send mail.";
	}
}
?>
