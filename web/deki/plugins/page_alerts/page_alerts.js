var Deki = Deki || {};
Deki.PageAlerts = {};

//Deki.PageAlerts.init
$(function()
{
	var $Alerts = Deki.$('#deki-page-alerts');
	Deki.PageAlerts.$Toggle = $Alerts.find('div.toggle');
	Deki.PageAlerts.$A = $Alerts.find('div.toggle a');
	Deki.PageAlerts.$Form = $Alerts.find('form.options');
	Deki.PageAlerts._formatter = 'pagealerts';
	
	// only hook events if page alerts are enabled
	if (!$Alerts.hasClass('disabled'))
	{
		// hiding/showing the options
		Deki.PageAlerts.$A.click(Deki.PageAlerts.toggleOptions);
		// handle option selections
		Deki.PageAlerts.$Form.find(':radio').click(Deki.PageAlerts.changeStatus);
		// activate radio button when label clicked
		Deki.PageAlerts.$Form.find('label').click(function(){
			$(this).siblings(':radio').click();
		});
		// grab the current selection value
		Deki.PageAlerts.nLastStatus = Deki.PageAlerts.$Form.find(':checked').val();
	}
});

// hack for IE of course
// stores the last radio button value
Deki.PageAlerts.nLastStatus = null;

Deki.PageAlerts.toggleOptions = function(event)
{
	var bVisible = Deki.PageAlerts.$Toggle.hasClass('with-options');
	Deki.PageAlerts.setOptionsVisibility(!bVisible);
	if (!bVisible)
	{
		// hook the hide event, once
		Deki.$('body').one('click', function() {
			Deki.PageAlerts.setOptionsVisibility(false);
		});
	}
	
	return false;
};

Deki.PageAlerts.setOptionsVisibility = function(visible)
{
	if (visible)
	{
		// align the form to the right
		var tOffset = Deki.PageAlerts.$Toggle.offset();
		var tWidth = Deki.PageAlerts.$Toggle.outerWidth();
		var fWidth = Deki.PageAlerts.$Form.outerWidth();
		
		// if offset() == position(), we are relative to screen
		var newLeft;
		if (Deki.PageAlerts.$Toggle.position().left == tOffset.left) {
			newLeft = (tOffset.left + tWidth) - fWidth;
		}
		else
		{
			// relative to parent element
			var tPosition = Deki.PageAlerts.$Toggle.position();
			newLeft = (tPosition.left + tWidth) - fWidth;
		}
		
		Deki.PageAlerts.$Form.css('left', newLeft);
		
		Deki.PageAlerts.$Toggle.addClass('with-options');
		Deki.PageAlerts.$Form.show();
	}
	else
	{
		Deki.PageAlerts.$Toggle.removeClass('with-options');
		Deki.PageAlerts.$Form.hide();

		// only blur when hiding the options
		Deki.PageAlerts.$A.blur();
	}
};

Deki.PageAlerts.changeStatus = function(event)
{
	if (Deki.PageAlerts.nLastStatus != this.value)
	{
		Deki.PageAlerts.nLastStatus = this.value;
		
		// hide the menu so it seems like a "fast" operation
		Deki.PageAlerts.setOptionsVisibility(false);	
		
		// set the status to reflect an ajax request
		Deki.PageAlerts.$A.addClass('loading');

		// post to pagealerts ajax formatter to perform the status change
		Deki.Plugin.AjaxRequest(Deki.PageAlerts._formatter,
			{
				data: {
					pageId: Deki.PageId,
					status: this.value
				},
				complete: function()
				{
					Deki.PageAlerts.$A.removeClass('loading');
				},
				success: function(data) {
					if(data.success)
					{
						Deki.PageAlerts.setStatus(data.body, data.message);
					}
					else
					{
						Deki.Ui.Message(wfMsg('error'), data.message);
						Deki.PageAlerts.setOptionsVisibility(true);
					}
				}
			}
		);
	}
};

Deki.PageAlerts.setStatus = function(bSubscribed, sStatus)
{
	if (bSubscribed)
	{
		Deki.PageAlerts.$A.removeClass('off');
	}
	else
	{
		Deki.PageAlerts.$A.addClass('off');
	}
	
	// set the textual status if specified
	if (sStatus)
	{
		Deki.PageAlerts.$A.find('span.status').text(sStatus);
	}
};
