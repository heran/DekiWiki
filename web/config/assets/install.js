
var MT = MT || {};
MT.Install = {};

// simple logging
MT.log = function(m) {
	if ((typeof console != "undefined") && console.log) {
		console.log(m);
	}
};

MT.Install = {
	_step: 0,
	_maxStep: 0,
	_onStep: {},
	_iframeUrls: {
		dependencies: 	'campaigns.mindtouch.com/install-error-dependencies',
		errors: 		'campaigns.mindtouch.com/install-error',
		select_type: 	'campaigns.mindtouch.com/install-select-edition_tcs.html',
		site_setup:		'campaigns.mindtouch.com/install-configuration',
		configuration: 	'campaigns.mindtouch.com/install-configuration',
		organization: 	'campaigns.mindtouch.com/install-organization',
		confirm_setup: 	'campaigns.mindtouch.com/install-confirmation',
		success: 		'campaigns.mindtouch.com/install-success'
	},
	_interval: null,
	CanStart: function() {
		return $('#environment-messages li.error').length == 0;
	},
	HasInputErrors: function() {
		return $('#input-messages').length != 0;
	},
	HasErrors: function() {
		return $('#installation-messages').length != 0;
	},
	Start: function(step) {
		var step = step || 0;
		
		// determine max steps
		this._maxStep = $('.navTable').find('td.title').length - 2;
		
		// attach anchors to the top for jumping between steps
		for (var i = 1; i <= this._maxStep; i++)
			$('body').prepend('<a id="step'+ i +'"></a>');
		$('#start .install-step').hide();
		$('#start').show();
		
		// show the required labels for fields with errors
		this._showRequiredLabels();
		this.GotoStep(step);
		$.history.init(this._changeHash);
		
		// initialize first iframe
		if (this.HasErrors()) {
			this.ShowIframe('errors');
		} else {
			
			// hook iframes
			this.OnStep(null, this.ShowIframe);
			this.ShowIframe(this._step);
		}
	},
	InputError: function(msg, name) {
		var $msg = $(msg);

		// highlight the input with the error
		MT.log('Selected field with error: ' + name);
		
		// find the step the field is in, hacky
		var $input = $('[name='+name+']');
		var $step = $input.parents('.install-step:first');
		for (var i = 1; i <= this._maxStep; i++) {
			if ($step.hasClass('step'+i)) {
				this.GotoStep(i);
				$input.focus();
				break;
			}
		}
		this._removeInputError($msg);
	},
	GotoStep: function(step) {
		step = parseInt(step);
		if (step < 1)
			step = 1;
		if (step > this._maxStep)
			step = this._maxStep;
		var next = step > this._step;
				
		if (step != this._step) {
			var $step = this._getStepElement(step);
			
			// scroll to window top
			//$.history.load('step'+step);
			document.location.hash = '#step'+step;
			
			// fire the step callbacks
			this._triggerStep(step);
			
			var currentClass = 'step'+this._step;
			var activeClass = 'step'+step;
			
			// update the progress bar
			$('.navTable td').removeClass('completed afterActive');
			for (var i = 1; i < step; i++) {
				$('.navTable .step'+i).removeClass('active').addClass('completed');
			}
			for (var i = step+1; i <= this._maxStep+1; i++) {
				$('.navTable .step'+i).removeClass('active');
				if (i == step+1)
					$('.navTable .step'+i).addClass('afterActive');
			}
			$('.navTable .'+activeClass).addClass('active');
			
			// change the step visibility
			$('#start .'+currentClass).css('visibility', 'hidden');
			$('#start .'+activeClass).fadeIn();
			$('#start .'+currentClass).hide().css('visibility', 'visible');
			this._step = step;
		}
	},
	PreviousStep: function() {
		var current = this._step;
		this.GotoStep(this._step-1);
		
		// did the step change?
		return current != this._step;
	},
	NextStep: function() {
		
		// hide any input errors from this step
		if (this.HasInputErrors()) {
			this._hideInputErrors(this._getStepElement(this._step));
		}
		var current = this._step;
		this.GotoStep(this._step+1);
		return current != this._step;
	},
	_showRequiredLabels: function() {
		if (typeof installInputErrors != "undefined") {
			$.each(installInputErrors, function(i, name) {
				var $input = $('#start').find('[name='+name+']');
				if ($input.length != 0) {
					$input.parent().find('.label-required').addClass('required');
				}
			});
		}
	},
	_hideInputErrors: function($step) {
		if (typeof installInputErrors != "undefined") {
			var that = this;
			$.each(installInputErrors, function(i, name) {
				
				if ($step.find('[name='+name+']').length != 0) {
					var $msg = $('#input-messages').find('li.input-error-'+name);
					if ($msg.length != 0)
						that._removeInputError($msg);
				}
			});
		}
	},
	_removeInputError: function($msg) {
		
		// clean up the messages
		if ($msg.parent().children().length <= 1) {
			$('#input-messages').remove();
		} else {
			$msg.remove();
		}
	},
	OnStep: function(step, callback) {
		for (var i = step || 1, iM = step || this._maxStep; i <= iM; i++) {
			if (typeof this._onStep[i] == "undefined")
				this._onStep[i] = [];
			this._onStep[i].push(callback);
		}
	},
	ShowIframe: function(step) {
		
		// do not use this; may be called w/o scope
		var $iframe = $('#install-iframes');
		var key = (typeof iframeStepMap != "undefined") ? (iframeStepMap[step] || step) : step;
		var url = MT.Install._iframeUrls[key];
		if (url) {
			MT.log('Changing iframe for step:' + step);
			$iframe.find('iframe').hide();
			var $step = $iframe.find('.step'+step);
			if ($step.length == 0) {
				MT.log('Creating a new iframe');
				var src = window.parent.document.location.protocol + '//' + url + '?ts=' + (new Date().getTime());
				$step = $iframe.append('<iframe class="step'+step+'" src="'+src+'" frameborder="0"></iframe>');
			}
			$step.show();
		}
	},
	_getStepElement: function(step) {
		return $('#start .step'+step);
	},
	_triggerStep: function(step) {
		if (typeof this._onStep[step] != "undefined") {
			$.each(this._onStep[step], function(i, fn) {
				fn(step);
			});
		}
		
		// special triggers
		if (step == 1) {
			this._triggerStep('first');
		}
		if (step == this._maxStep) {
			MT.log('Triggering last step');
			this._triggerStep('last');
		}
	},
	_changeHash: function(hash) {
		MT.log('Location hash changed: ' + hash);
		
		// validate the hash change
		if (String(hash).substr(0, 4) == 'step') {
			var step = String(hash).substring(4);
			MT.log('Step changed via hash: ' + step);
		}
	},
	
	// determine whether api is running
	PollInstallStatus: function() {
		if (!this._interval) {
			MT.log('Polling for api');
			this._pollApiStatus();
			this._interval = setInterval(this._pollApiStatus, 5000);
		} else {
			MT.log('Cancelling api polling');
			clearInterval(this._interval);
		}
	},
	_pollApiStatus: function() {
		$.ajax({
			url: '/@api/deki/users/current',
			success: function() {
				$('#mt-api-status .install-not-running').hide();
				$('#mt-api-status .install-running').show();
				
				// clear the interval
				MT.Install.PollInstallStatus();
			},
			error: function() {

				// continue polling
			}
		});
	}
};
