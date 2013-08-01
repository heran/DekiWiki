
$(function() {
	if (!MT.Install.CanStart()) {
		MT.log('Environment check failed');
		
		// Display the dependency check iframe
		MT.Install.ShowIframe('dependencies');
		return;
	}
	MT.log('Environment check passed');
	
	// JavaScript is enabled!
	$('#install-contents').show();

	var step = $('#start.posted').length > 0 ? 2 : null;
	if (MT.Install.HasErrors()) {
		
		// goto last step
		step = 99;
	}
	MT.Install.Start(step);
	
	// hook navigation buttons
	$('#start .backButton').click(function() {
		MT.Install.PreviousStep();
		return false;
	});
	$('#start .nextButton').click(function() {
		MT.Install.NextStep();
		return false;
	});
	
	// hook navigation clicks
	$('.navTable td').click(function(e) {
		var $this = $(this);
		if ($this.hasClass('completed')) {
			// we can go back to this step
			for (var step = 1; step < 10; step++) {
				if ($this.hasClass('step'+step)) {
					MT.Install.GotoStep(step);
					break;
				}
			}
		}
	});

	// intercept form submit
	$('form#start').bind('keypress', function(e) {
		if (e.which == '13') {
			MT.Install.NextStep();
			return false;
		}
		return true;
	});
});
