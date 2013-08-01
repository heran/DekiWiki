
$("body").ready( function() {
	$("#text-username").keyup( function() {
	
		var q = $(this).val();

		clearTimeout($.data(this, "timer"));
		var ms = 400;
		var wait = setTimeout(function() {
			finduser(q);
		}, ms);

		$.data(this, "timer", wait);
	});
});
	
function finduser(q) {
	var qurl="/@api/deki/users/="+q;
	var cntuser=0;
	
	if (q.length == 0) {
		$('#createuser').find (':submit').attr ('disabled', 'disabled');
		$("#available").hide();
		return;
	}
	
	if (q.match(/^\/|^\.\.$|^\.$|^\.\/|^\.\.\/|\/\.\/|\/\.\.\/|\/\.$|\/\..$|\/$/)) {
		$('#createuser').find (':submit').attr ('disabled', 'disabled');
		$("#available").show();
		$("#available").text("Invalid title");
		return;
	}
	
	$.ajax({
		   type:'GET',
		   url:qurl,
		   dataType:'xml',
		   success:found,
		   error:error
		});

	function found(results) {
		$('#createuser').find (':submit').attr ('disabled', 'disabled');
		$("#available").show();
		$("#available").text("Name taken");
	}

	function error() {
		$('#createuser').find (':submit').removeAttr ('disabled');
		$("#available").show();
		$("#available").text("Name available");
	} 

}
