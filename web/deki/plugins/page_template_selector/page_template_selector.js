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

var Deki = Deki || {};

if (typeof Deki.Plugin == 'undefined') {
	Deki.Plugin = {};
}

(function($) {
	$(function() {
		Deki.Plugin.PageTemplateSelector._attachEvents();
	});
})(Deki.$);

Deki.Plugin.PageTemplateSelector = {};
Deki.Plugin.PageTemplateSelector.SpecialPage = 'Special:PageTemplateSelector';

Deki.Plugin.PageTemplateSelector._attachEvents = function() {

	// on the popup page
	var $popup = $('#deki-pagetemplates-embed');
	if ($popup.length > 0) {
		
		$popup.removeClass('loading');
		
		// disable links within page items
		$popup.find('.page-item a').click(function() {
			return false;
		});

		$popup.find('li.page-item').click(function() {
			
			var $link = $(this).find('a');
			
			// clicking previously highlighted item - templatepath has already been set
			if ($(this).hasClass('highlight')) {
				Deki.Plugin.PageTemplateSelector.DefaultAction();
				return false;
			}
			
			// new template selection
			$popup.find('.page-item').removeClass('highlight');
			$(this).addClass('highlight');
			
			// set the hidden template field (link of format "#path/to/template")
			$('#deki-pagetemplates-templatepath').val($link.attr('href').substring(1));
		});

		// set default
		$default = $popup.find('li.page-item-default');
		$default.addClass('highlight');
		
		// Bug #8288: pressing enter should click highlighted item
		$(document).keypress(function(e) {
			if (e.keyCode == '13') {
				e.preventDefault();
				
				Deki.Plugin.PageTemplateSelector.DefaultAction();
			}
		});
	}
};

// Triggered for default select (i.e., submitting current form via enter)
Deki.Plugin.PageTemplateSelector.DefaultAction = function() {
	var createPageUri = $('#deki-pagetemplates-create').attr('href') + '&template=' + $('#deki-pagetemplates-templatepath').val();
	
	// cannot trigger native link click via javascript - manually redirect
	Deki.QuickPopup.Redirect(createPageUri);
}

Deki.Plugin.PageTemplateSelector.ShowPopup = function(title, url) {	
	var width = 690;
	var height = 380;
	
	Deki.QuickPopup.Show({
		'title': title,
		'url': url,
		width: width,
		height: height
	});
	
	return false;
};
