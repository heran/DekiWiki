(function($) {
	
	/* editable plugin */
	$.fn.editable = function(options)
	{ 	
		var defaults = {
			onDisplayValue: null,		// called when clicked
			url: null,					// ajax endpoint
			method: 'post',
			dataType: 'json',
			field: 'text', 				// name of the text post field
			fields: {}, 				// additional post fields
			onGenerateRequest: null,	// allow field configuration
			onSuccess: null, 			// ajax handlers
			onError: null,
			editingClass: 'editing',	// editing class
			savingClass: 'saving',		// ajax save class
			multiLine: false,			// true will change to textarea
			convertNewlines: false		// experimental
		};  
		var options = $.extend(defaults, options);
	
	    return this.each(function()
	    {	
			var $this = $(this);
			var $input = null;
			var value = $this.text();
	
			$this.click(function()
			{
				// check if the input is already created
				if ($input)
					return false;
				$this.html(options.multiLine ? '<textarea></textarea>' : '<input type="text" value="" />');
				$input = $this.children(':first');
				$this.addClass(options.editingClass);
			   	
				var displayValue = value;
				if (options.onDisplayValue)
					displayValue = options.onDisplayValue($this, displayValue);
				
				if (options.multiLine) {
					$input.text(displayValue);
				} else {
					$input.attr('value', displayValue);
				}
				$input.focus().select();
				
				// events
				$input.blur(function() { saveEdit() });
				
				$input.keydown(function(e) {
					
					// esc
					if (e.which === 27)
						cancelEdit(value);
					
					// enter/tab
					if (options.multiLine && e.shiftKey && (e.which == 13))
						return;
					if (e.which === 13 || e.which === 9) {
			    		e.preventDefault();
						saveEdit();
			    	}
				});
				
				// helpers
				function cancelEdit(setValue) {
					if (setValue)
						value = setValue;
					$this.text(value);
					if (options.convertNewlines)
						$this.html(nl2br($this.html()));
					$this.removeClass(options.editingClass);
					$input = null;
				}
				
				function saveEdit() {
					var request = options;
					request.fields = request.fields || {};
					request.fields[request.field] = $input.val();
					
					if (options.onGenerateRequest)
						options.onGenerateRequest($this, request);
					if (!request.url)
						throw 'jquery.editable: No ajax endpoint configured!';
					
					$input.addClass(request.savingClass);
					$.ajax({
					    type: request.method,
					   	data: request.fields,
					   	dataType: request.dataType,
			    		url: request.url,
			    		
			    		success: function(data) {
							$input.removeClass(request.savingClass);
			    			if (options.onSuccess) {
			    				cancelEdit(options.onSuccess($this, value, data));
			    			} else {
			    				cancelEdit(options.multiLine ? $input.text() : $input.val());
			    			}
					    },
	
						error: function(xhr, textError) {
					    	$input.removeClass(request.savingClass);
					    	if (options.onError) {
					    		options.onError($this, value, xhr, textError);
					    	} else {
					    		$this.html(textError);
					    	}
						}
			  		});					
				}
				
				function nl2br(s) {
					return (s + '').replace(/([^>\r\n]?)(\r\n|\n\r|\r|\n)/g, '$1<br/>$2');
				}
				
				// halt click default
				return false;
			});		  
	    });
	};
	/* /editable plugin */
})(jQuery);