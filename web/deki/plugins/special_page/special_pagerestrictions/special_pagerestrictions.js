Deki.PageRestrict = Deki.PageRestrict || {};

$('#deki-addgrant').ready(function()
{
	var $el = Deki.$('form.page-restrict');
	// set the root page restrict node
	Deki.PageRestrict.$el = $el;
	// add a hidden input to signify PE
	$el.append('<input type="hidden" name="progressive" value="1" />');
	
	// behavior for the cascading
	$el.find('#checkbox-subpages').click(function() {
		if ($el.find('input:checkbox[name=subpages]').is(':checked')) {
			$el.find('#deki-cascade').show();
		}
		else {
			$el.find('#deki-cascade').hide();
		}
	});
	
	// hide the grant list for public grant pages
	Deki.PageRestrict.toggleGrantList($el); 
	
	$el.find('input:radio[name=restrictType]').click(function() { 
		Deki.PageRestrict.toggleGrantList($el); 
	}); 
		
	// attach grant click
	$el.find('#deki-addgrant').click(function()
	{
		var sName = Deki.PageRestrict.$el.find('#autoCompInput').val();
		if (String(sName).length == 0)
			return false;
		var sRoleName = Deki.PageRestrict.$el.find('#select-role').val();
		
		var sUrl = [
			'/deki/gui/usergroupsearch.php?mode=userorgroup&format=json&name=',
			encodeURIComponent(sName)
		];
		
		// show ajax loading
		Deki.PageRestrict.setAjaxLoading(true);
		
		Deki.$.ajax({
				url: sUrl.join(''),
				type: 'POST',
				data: {},
				dataType: 'json',
				timeout: 10000,
				error: function()
				{
				    // remove ajax loading
				    Deki.PageRestrict.setAjaxLoading(false);
				},
				success: function(data)
				{
					Deki.PageRestrict.setAjaxLoading(false);
	
					if (data.success)
					{
						$el.find('#deki-validuser').hide();
						var type, id;
						if (data.user)
						{
							type = 'u';
							id = parseInt(data.user);
						}
						else
						{
							type = 'g';
							id = parseInt(data.group);
						}
						
						Deki.PageRestrict.addGrant(type, id, sName, sRoleName);
						// clear out the input now that the user has been added
						Deki.$('#autoCompInput').val('');
					}
					else
					{
						$el.find('#deki-validuser').show();
					}
				}
		});
		
		// stop form submit
		return false;
	});
	
	// hide the remove grant input & checkboxes
	$el.find('.remove-grant').hide();
	$el.find('a.remove-grant').show().click(Deki.PageRestrict.removeGrantEvent);
// end ready
});

// stores the root page restrict element
Deki.PageRestrict.$el = null;

Deki.PageRestrict.setAjaxLoading = function(bLoading)
{
	var bLoading = bLoading || false;
	
	if (bLoading)
	{
		Deki.PageRestrict.$el.addClass('loading')
			.find('#deki-addgrant').attr('disabled', 'disabled');
	}
	else
	{
		Deki.PageRestrict.$el.removeClass('loading')
			.find('#deki-addgrant').removeAttr('disabled');
	}
};

Deki.PageRestrict.TYPE_PUBLIC = 'Public'; 

Deki.PageRestrict.toggleGrantList = function($el)
{
	var restrictType = $el.find('input:radio[name=restrictType]:checked').val();
	if (restrictType == Deki.PageRestrict.TYPE_PUBLIC) {
		$el.find('div.grants').hide();
	}
	else {
		$el.find('div.grants').show();
	}
};

/* this = a.remove-grant */
Deki.PageRestrict.removeGrantEvent = function(e)
{
	var $input = Deki.$(this).parent().siblings('.remove-grant:checkbox');

	var name = $input.attr('name'); // list[xx][xx]
	var value = $input.val();
	name = 'remove_grants' + String(name).substring(4);
	
	// keep track of the grants that are removed
	Deki.PageRestrict.$el.append('<input type="hidden" name="'+ name +'" value="'+ value +'" />');
	
	$li = $input.parent();
	$li.remove();
	
	return false;
};

// creates a new grant node in the form
Deki.PageRestrict.addGrant = function(type, id, sName, sRole)
{
	var $grants = Deki.PageRestrict.$el.find('.grantlist ul');
	var $li = $grants.find('li:first').clone();
	
	var sInputName = 'list['+ type +']['+ id +']';
	// create the dangerous portions safely
	var sSafeRole = Deki.$(document.createElement('span')).text(sRole).html();
	
	var $current = $grants.find('input[name="'+ sInputName +'"]');
	if ($current.length > 0)
	{
		// grant already exists, check for different role
		if ($current.val() == sRole)
		{
			return;
		}
		else
		{
			$current.parent().remove();
		}
	}
	
	// add the appropriate values
	$li.attr('class', type == 'g' ? 'group' : 'user');
	$li.find(':checkbox').attr('id', 'checkbox-'+ sInputName).attr('name', sInputName).val(sRole);
	$li.find('label').attr('for', 'checkbox-'+ sInputName)
		.find('span.name').text(sName).end()
		.find('span.role').text(sRole).end();
	$li.append('<input type="hidden" name="new_grants['+ type +']['+ id +']" value="'+ sSafeRole +'" />');
	
	$li.appendTo($grants);
	// hook the remove event
	$li.find('.remove-grant').click(Deki.PageRestrict.removeGrantEvent);
	// show the list item
	$li.show();
};
