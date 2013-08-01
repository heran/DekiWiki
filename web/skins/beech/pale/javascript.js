/*
This file will contain all of your custom skin JavaScript.
*/


$("body").ready( function() {
	
	// SHOW LOGIN FORM
	$(".login-link").click( function() {
		$(".login-links").fadeOut( function() {
			$(".login form").fadeIn();	
		});	
		return false;
	});
	
	// Reset Text Inputs
	$("[resetval]").each( function() {
		var sval = $(this).val();
		var resetval = $(this).attr("resetval");
		if (sval == "")
		$(this).val(resetval);
	});
	$("[resetval]").focus( function() {
		var resetval = $(this).attr("resetval");
		var textval = $(this).val();
		if (textval == resetval)
		$(this).val('');
	});
	$("[resetval]").blur( function() {
		var resetval = $(this).attr("resetval");
		var textval = $(this).val();
		if (textval == resetval ||  textval=='')
		$(this).val(resetval);
	});
	
	
	//Dropdowns
	$(".dropdown").parents("span").find("span").addClass("drop-link");
	$("body").click( function() {
		$(".dropdown").hide();	
	});
	
	$(".drop-link").click( function() {
		$(".dropdown").hide();
		var d = $(this).find("~ .dropdown");
		var dwidth = d.outerWidth(); // width of dropdown
		var lwidth = $(this).outerWidth(); // width of drop-link
		var m = lwidth - dwidth; // calculate the margin for right aligned
		d.css("marginLeft",m);
		d.slideDown('fast');
	});
    
	// Button CSS for deeply rooted buttons
    $("input[type=submit]").addClass("btn");
    $("input[type=button]").addClass("btn");
    $(".commentActions input").addClass("btn");
    $(".commentActions a").addClass("btn");
    $("#deki-page-alerts div.toggle a").attr("title","Manage notifications");
    $(".page a.disabled").parent().hide();
    
});

