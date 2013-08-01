Deki.$(function() {
    Deki.$('#text-query').autocomplete('user_management.php', {
		extraParams: { 'params': 'find' },
		onItemSelect: function(elLi) { Deki.$('#text-query').parents('form').submit(); } // is there a better way?
	});
});
