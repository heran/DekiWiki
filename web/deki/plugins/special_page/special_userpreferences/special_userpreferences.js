Deki.$(document).ready(function() {
	var disableAuth = function() {
		Deki.$('form.userpreferences #deki-timezone select').attr('disabled', 'disabled');
	};
	if (Deki.$('form.userpreferences #deki-timezone input.input-radio:checked').val() == 'site') {
		disableAuth();
	}
	Deki.$('#radio-tztype-override').click(function() {
		Deki.$('form.userpreferences #deki-timezone select').attr('disabled', '');
	});
	Deki.$('#radio-tztype-site').click(disableAuth);
});