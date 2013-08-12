$(function(){
	// attach close button for session messages
	var $flashMsg = $('ul.flashMsg');	
	if ($flashMsg.length > 0)
	{
		var $dismiss = $('<a href="#" class="dismiss"></a>');
		$dismiss.click(function(){
			$flashMsg.hide();
		});
		
		$flashMsg.prepend($dismiss);
	}
});
