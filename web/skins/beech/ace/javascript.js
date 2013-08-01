$('body').ready(function() {
	
	// Add .droplink to each dropdown link
	$(".dropdown").each( function() {
		$(this).parent().find(".a").addClass("droplink");	
	});
	
	// Show dropdown on mouseover
	$(".droplink").click( function() {
		$(this).find("~ .dropdown").slideDown(50);	
	});
	
	// Hide dropdown on body click
	$("body").click( function() {
		$(".dropdown").hide();	
	});
});
