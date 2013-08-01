Deki.$(document).ready(function() {
	var disableAuth = function() {
		Deki.$('fieldset.external').hide();
		Deki.$('#select-external_auth_id').attr('disabled', 'disabled');
	};
	// onload
	if (Deki.$('form #deki-external-auth input.input-radio:checked').val() == 'local') {
		disableAuth();
	}
	
	// events
	Deki.$('#radio-auth_type-external').click(function() {
		Deki.$('fieldset.external').show();	
		Deki.$('#select-external_auth_id').attr('disabled', '');
	});
	Deki.$('#radio-auth_type-local').click(disableAuth);
});