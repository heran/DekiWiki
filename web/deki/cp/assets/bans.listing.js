Deki.$(document).ready(function() {
	Deki.$('#expiryType').change(function() { hideCustomDate(); });
	Deki.$('input:radio[@name="type"]').click(function() { checkUserLabel(); });
	hideCustomDate();
	
	checkUserLabel();
});

var hideCustomDate = function() {
	var hasCustom = Deki.$('#expiryType').val() == 'custom';
	Deki.$('#custom-date').css('display', hasCustom ? 'inline': 'none');
	if (hasCustom) {
		Deki.$('#text-banYear').focus();	
	}
};

var checkUserLabel = function() {
	if (Deki.$('input:radio[@name="type"]:checked').val() == 'username') {
		Deki.$('#deki-banuser').show();	
		Deki.$('#deki-banip').hide();	
	}
	else {
		Deki.$('#deki-banuser').hide();	
		Deki.$('#deki-banip').show();	
	}
};