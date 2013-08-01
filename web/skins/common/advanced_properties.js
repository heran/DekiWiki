
var Deki = Deki || {};
Deki.Properties = {};

Deki.$(document).ready(function()
{
	Deki.Properties.$Table = Deki.$('table.deki-properties');
	
	Deki.Properties.$Table.find('td.edit a').click(Deki.Properties.edit);
});

/*
 * Stores the loaded markup
 */
Deki.Properties.$markup = null;
Deki.Properties.sBaseUrl = '/deki/gui/properties.php?';

/*
 * Events
 */
Deki.Properties.save = function(event)
{
	var $tr = Deki.$(this).parent().parent();
	Deki.Properties.saveEditRow($tr);
	return false;
};

Deki.Properties.cancel = function(event)
{
	var $tr = Deki.$(this).parent().parent();
	Deki.Properties.setEditRowVisibility($tr, false);
	return false;
};

Deki.Properties.edit = function(event)
{
	// this == <table><tr><td></td><td><a />
	var $this = Deki.$(this);
	var $tr = Deki.Properties.getEditRowHtml($this.parent().parent());
	if ($tr && $tr.length > 0)
	{
		// html is already loaded
		Deki.Properties.setEditRowVisibility($tr, true);

	}
	
	return false;
};
/*
 * /Events
 */


Deki.Properties.getViewRow = function($editTr)
{
	var id = String($editTr.attr('id'));
	var viewId = id.substr(0, id.length-5); //-edit
	var $tr = Deki.$('#' + viewId);
	
	return $tr;
};

Deki.Properties.setEditRowVisibility = function($editTr, bVisible)
{
	var $tr = Deki.Properties.getViewRow($editTr);

	if (bVisible)
	{
		var sValue = $tr.find('td.value').text();
		$tr.hide();
		$editTr.show().find('td.value :input').val(sValue).focus();
	}
	else
	{
		$tr.show();
		$editTr.hide();	
	}
};

Deki.Properties.setLoadingStatus = function($editTr, bLoading)
{
	if (bLoading)
	{
		$editTr.addClass('loading');
		$editTr.find(':input').attr('disabled', 'disabled');
	}
	else
	{
		$editTr.removeClass('loading');
		$editTr.find(':input').removeAttr('disabled');
	}
};

Deki.Properties.saveEditRow = function($editTr)
{
	var sName = $editTr.find('td.name').text();
	var sValue = $editTr.find('td.value :input').val();
	
	// check if the value changed
	var $tr = Deki.Properties.getViewRow($editTr);
	var sOldValue = $tr.find('td.value').text();	
	if (sValue == sOldValue)
	{
		// no change
		Deki.Properties.setEditRowVisibility($editTr, false);
		return false;
	}
	
	
	var sUrl = [
		Deki.Properties.sBaseUrl,
		'action=save',
		'&id=',
		encodeURIComponent(Deki.SpecialPropertiesId),
		'&type=',
		encodeURIComponent(Deki.SpecialPropertiesType)
	];
	
	// load it up!
	Deki.Properties.setLoadingStatus($editTr, true);

	// post to deki/gui to perform the status change
	Deki.$.ajax({
		url: sUrl.join(''),
		type: 'POST',
		data: {'name': sName, 'value': sValue},
		dataType: 'json',
		timeout: 10000,
		error: function()
		{
			Deki.Properties.setLoadingStatus($editTr, false);
			MTMessage.Show(wfMsg('error'), wfMsg('internal-error'));
		},
		success: function(data)
		{
			Deki.Properties.setLoadingStatus($editTr, false);
			
			if (data.success)
			{
				// looks good, close the input
				var $tr = Deki.Properties.getViewRow($editTr);
				Deki.Properties.setEditRowVisibility($editTr, false);
				// update the contents of the row
				var sNewValue = $editTr.find('td.value :input').val();
				$tr.find('td.value').text(sNewValue);
			}
			else
			{
				// TODO: localize and provide a better error message from deki/gui
				console.log(data);
				MTMessage.Show(wfMsg('error'), data.message);
			}			    
		}
	});
};

Deki.Properties.getEditRowHtml = function($viewTr)
{
	// id = deki-pageproperties-XXX-edit
	var sViewId = $viewTr.attr('id');
	var sEditId = sViewId + '-edit';
	
	// try to find the editing row
	var $tr = Deki.Properties.$Table.find('#' + sEditId);
	if ($tr.length == 0)
	{
		// check for a local version of the markup
		if (Deki.Properties.$markupTr)
		{
			// need to create the editing row
			$editTr = Deki.Properties.$markupTr.clone()
				.attr('id', sEditId)
				.attr('class', $viewTr.attr('class'))
			;
			
			// setup the name input
			var sName = $viewTr.find('td.name label').text();
			$editTr.find('td.name').text(sName);
			
			// setup the value input
			$editTr.find('td.value')
				.html('<input type="text" value="" />')
				.find(':input')
				// halt ENTER key press
				.keypress(function(event){
					if (event.keyCode == 13)
					{
						var $tr = Deki.$(this).parent().parent();
						Deki.Properties.saveEditRow($tr);
						return false;
					} 
				})
			;
			
			// setup the buttons
			$editTr.find('td.edit')
				.find('button.save').click(Deki.Properties.save).end()
				.find('button.cancel').click(Deki.Properties.cancel)
			;
			
			// add to the table
			$editTr.insertBefore($viewTr).hide();
			return $editTr;	
		}
		else
		{
			// need to fetch the edit row markup remotely
			var sUrl = [
				Deki.Properties.sBaseUrl,
				'action=edit',
				'&id=',
				encodeURIComponent(Deki.SpecialPropertiesId),
				'&type=',
				encodeURIComponent(Deki.SpecialPropertiesType)
			];
			
			$viewTr.addClass('loading');
			// fetch the markup from deki/gui
			Deki.$.ajax({
				url: sUrl.join(''),
				type: 'GET',
				data: {},
				dataType: 'json',
				timeout: 10000,
				error: function()
				{
					$viewTr.removeClass('loading');
					// fire off the normal edit link click, non-js action
					var sHref = $viewTr.find('td.edit a').attr('href');
					document.location = sHref;
				},
				success: function(data)
				{
					$viewTr.removeClass('loading');
					// add the edit markup to the table and hide it
					Deki.Properties.$markupTr = Deki.$(data.html);
					//$markupTr.find('tr.deki-pageproperties-edit').hide();
					// trigger fires: Deki.Properties.edit
					$viewTr.find('td.edit a').triggerHandler('click');
				}
			});
		}
		return false;
	}
	
	// $tr is now the editing row
	return $tr;
};
