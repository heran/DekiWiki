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
		Deki.Plugin.PageContentRating._attachEvents();
	});
})(Deki.$);

Deki.Plugin.PageContentRating = {};
Deki.Plugin.PageContentRating._formatter = 'page_content_rating';
Deki.Plugin.PageContentRating._$lastClicked = null;

Deki.Plugin.PageContentRating._attachEvents = function() {
	
	// if disabled, ignore existing onclick events
	$('.deki-page-rating-buttons.disabled a').removeAttr('onclick');
	
	// attach to page rating events. May be called multiple times, so unbind previous handlers
	$('.deki-page-rating-buttons a').each(function() {
		// have we already attached events?
		if ($(this).data('pagerating.events'))
			return;
		else
			$(this).data('pagerating.events', true);
		
		$(this).click(function() {
			Deki.Plugin.PageContentRating._$lastClicked = $(this);
			
			var rating = $(this).hasClass('content-rate-up') ? 1 : 0;
			Deki.Plugin.PageContentRating._rate(rating);
			return false;
		});
	});
	
	$('#deki-page-rating-comment .input-button').click(function() {
		var comment = $('#textarea-rating').val();
		parent.Deki.Plugin.PageContentRating._comment(comment);
		Deki.QuickPopup.Hide();
		return false;
	});
	
	$('#deki-page-rating-comment .secondary').click(function() {
		Deki.QuickPopup.Hide();
		return false;
	});
};

Deki.Plugin.PageContentRating._updatePage = function(ratingData) {
		
	$('#deki-page-rating-score').text(ratingData.score_text);
	$('.deki-page-rating-buttons').replaceWith(ratingData.button_html);
	
	Deki.Plugin.PageContentRating._attachEvents();
};

Deki.Plugin.PageContentRating.ShowPopup = function(title, url) {
	var width = 400;
	var height = 190;

	Deki.QuickPopup.Show({
		'title': title,
		'url': url,
		width: width,
		height: height
	});
	
	return false;
};

Deki.Plugin.PageContentRating._rate = function(rating) {
	Deki.Plugin.AjaxRequest(Deki.Plugin.PageContentRating._formatter,
		{
			data: {
				action: 'rate',
				pageId: Deki.PageId,
				rating: rating
			},
			success: function(data) {
				if (data.body["popup_url"])
				{
					Deki.Plugin.PageContentRating.ShowPopup(wfMsg('contentrating-title-popup'), data.body["popup_url"]);
				}
				
				Deki.Plugin.PageContentRating._updatePage(data.body);
			},
			context: Deki.Plugin.PageContentRating._$lastClicked
		}
	);
};

Deki.Plugin.PageContentRating._comment = function(comment) {
	Deki.Plugin.AjaxRequest(Deki.Plugin.PageContentRating._formatter,
		{
			data: {
				action: 'comment',
				pageId: Deki.PageId,
				comment: comment
			},
			success: function(data) {
				var $comments = $(parent.document.body).find('#comments');
				parent.Deki.Plugin.Comments.Update($comments);
			},
			context: Deki.Plugin.PageContentRating._$lastClicked
		}
	);
};
