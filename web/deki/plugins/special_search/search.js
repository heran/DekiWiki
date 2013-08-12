$(function() {
    var standardSearch = $('#text-search').val();
    var advancedSearch = $('.deki-advanced-search-form input[name="all"]').val();
    if(standardSearch == "") {
        $('#text-search').val(advancedSearch);
    }
    else if(advancedSearch == "") {
        $('.deki-advanced-search-form input[name="all"]').val(standardSearch);
    }

    updateAdvancedSearchToggle();

	$('#deki-advanced-toggle').click(function() {
		$('.deki-advanced-search-form').slideToggle('fast', function() {
			$('.deki-search-form:first .inputs').toggle();
			updateSearchText();
			updateAdvancedSearchToggle();
		});
	});

	$('#deki-search-results a.go').click(function(e) {
		var linkUrl = Deki.Plugin.SpecialSearch._lastLink;
		var trackUrl = Deki.Plugin.SpecialSearch._lastTracker;
		
		var middleClick = (e.which == 2);
		if (e.altKey || e.metaKey || e.shiftKey || middleClick)
			linkUrl = null;
		
		var handler = function() {
			if (linkUrl)
				document.location = linkUrl;
		};
		
		$.ajax({
			type: 'post',
			url: trackUrl,
			async: false,
			success: handler,
			error: handler
		});
	});
});

function updateAdvancedSearchToggle() {
	var toggle = $('.deki-advanced-search-form').is(':visible') ? wfMsg('standard-search') : wfMsg('advanced-search');
	$('#deki-advanced-toggle').html(toggle);
}

function updateSearchText() {
	var s = $('#text-search');
	var a = $('.deki-advanced-search-form input[name="all"]');
	if($('.deki-advanced-search-form').is(':visible')) {
		a.val(s.val());
	}
	else {
		s.val(a.val());
	}
}

if (typeof Deki == "undefined")
	var Deki = {};
if (typeof Deki.Plugin == "undefined")
	Deki.Plugin = {};
Deki.Plugin.SpecialSearch = {};

Deki.Plugin.SpecialSearch._lastLink = null;
Deki.Plugin.SpecialSearch._lastTracker = null;

function __deki_search_results(e, url, track) {
	// register the result urls
	Deki.Plugin.SpecialSearch._lastLink = url;
	Deki.Plugin.SpecialSearch._lastTracker = track;
};
