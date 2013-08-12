jQuery.fn.extend({
   check: function() {
     return this.each(function() { this.checked = true; });
   },
   uncheck: function() {
     return this.each(function() { this.checked = false; });
   }
});

jQuery.extend({
	htmlEncode : function(html)
	{
		if ( !html )
			return '';
	
		html = html.replace( /&/g, '&amp;' );
		html = html.replace( /</g, '&lt;' );
		html = html.replace( />/g, '&gt;' );
	
		return html;
	},
	
	htmlDecode : function(html)
	{
		if ( !html )
			return '' ;
	
		html = html.replace( /&gt;/g, '>' );
		html = html.replace( /&lt;/g, '<' );
		html = html.replace( /&amp;/g, '&' );
	
		return html;
	},

	getScript: function(url, callback)
	{
		var head = document.getElementsByTagName("head")[0];
		var script = document.createElement("script");
		script.src = url;

		// Handle Script loading
		{
			var done = false;

			// Attach handlers for all browsers
			script.onload = script.onreadystatechange = function()
			{
				if ( !done && (!this.readyState ||
					this.readyState == "loaded" || this.readyState == "complete") )
				{
					done = true;
					if (callback)
						callback();

					// Handle memory leak in IE
					script.onload = script.onreadystatechange = null;
				}
			};
		}

		head.appendChild(script);

		// We handle everything using the script element injection
		return undefined;
	},

	extendClass : function(Child, Parent)
	{
		var F = function() {};
		F.prototype = Parent.prototype;
		Child.prototype = new F();
		Child.prototype.constructor = Child;
		Child.superclass = Parent.prototype;

		if ( Parent.prototype.constructor == Object.prototype.constructor )
		{
			Parent.prototype.constructor = Parent;
		}
	}
});

(function($)
{
	/* defaultValue plugin */
	$.fn.defaultValue = function() 
	{
		var elements = this;
		var defaultArgs = arguments;
		
		return elements.each(function(index)
		{
			var $el = $(this);
			var defVal = defaultArgs[index] || $el.attr('title');
			var defClass = 'deki-default-value'; // make an arg?

			if ( ($el.val() == '') || ($el.val() == defVal) )
			{ // initialize only if no value
				$el.val(defVal).addClass(defClass);
			}

			$el.focus(function()
				{
					if ($el.hasClass(defClass))
					{
						$el.val('').removeClass(defClass);
					}
				})
				.blur(function()
				{
					if ($el.val() == '')
					{
						$el.val(defVal).addClass(defClass);
					}
				})
				// make sure we don't submit the default
				.parents('form:first').submit(function()
				{
					if ($el.hasClass(defClass))
					{
						$el.val('');
					}
				})
			; // end $el
		});
	};
	/* /defaultValue plugin */
})(jQuery);
