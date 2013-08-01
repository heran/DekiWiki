Deki.$(function() {
	// search
    Deki.$('#text-query').autocomplete('group_management.php', {
		extraParams: { 'params': 'find' },
		onItemSelect: function(elLi) { Deki.$('#text-query').parents('form').submit(); } // is there a better way?
	});
	// set users
    Deki.$('#text-set_value').autocomplete('user_management.php', {
		extraParams: { 'params': 'find' }
	}).focus();
});
