// create the services namespace
var Services = Services ? Services : [];


Deki.$(document).ready(function()
{
	Services.toggleInitType();
	Services.addEvents();
	
	Services.checkForm();
});

/*
 * Modify the preconfigure form to autosubmit on select change and hide the submit button
 */
Services.checkForm = function()
{
	Deki.$('form.preconfigure')
		.find('button, input.button').hide().end()
		.find('select').change(function()
		{
			document.preconfigure.submit();
		})
	;
}

/*
 * Hook on to init_type form elements to hide/show sid/uri
 */
Services.toggleInitType = function(event)
{
	var value = this.value;

	if (!event)
	{
		Deki.$('[name=init_type]:radio').click(Services.toggleInitType);
		value = Deki.$('[name=init_type]:radio:checked').val();
	}

	switch (value) {
		case 'remote':
			Deki.$('[name=sid]:input').parent().hide();
			Deki.$('[name=uri]:input').parent().show();
			
			Deki.$('.extension-native').hide();
			Deki.$('.extension-remote').show();
			break;

		case 'native':
		default:
			Deki.$('[name=sid]:input').parent().show();
			Deki.$('[name=uri]:input').parent().hide();
			
			Deki.$('.extension-native').show();
			Deki.$('.extension-remote').hide();
			break;
	}
};

Services.addEvents = function()
{
    Deki.$("div.configtable button.add").click(Services.addConfigRow);
    Deki.$("div.configtable button.remove").click(Services.removeConfigRow);
}

Services.addConfigRow = function()
{
	var oTable = Deki.$(this).parents("div.configtable").find("table.config");
	var oLastRow = oTable.find("tr:last");
	var oNewRow = Deki.$(oLastRow).clone(true).insertAfter(oLastRow);
	
	Deki.$(oNewRow).show().find("input[type=text]").val("");
    Services.updateTable(oTable);
    
    Deki.$(oNewRow).find("input[type=text]:first").focus();
	
	return false;
}

Services.removeConfigRow = function()
{
    var oTable = Deki.$(this).parents("table.config");
    var oRows = oTable.find("tr");

    var oCurrentRow = Deki.$(this).parents("tr").get(0);
    
    if ( oRows.length > 1 )
    {
    	Deki.$(oCurrentRow).remove();
    }
    else
    {
    	Deki.$(oCurrentRow).find("input[type=text]").val("");
    }
    
    Services.updateTable();
    
    return false;
}

Services.updateTable = function(oTable)
{
	var rMatch = /-?\d+/;

	Deki.$(oTable).find("tr:visible").each(function(i) {
		
		if ( (i % 2) == 0 ) // i starts from 0!
		{
            Deki.$(this).removeClass("bg2");
            Deki.$(this).addClass("bg1");
		}
		else
		{
            Deki.$(this).removeClass("bg1");
            Deki.$(this).addClass("bg2");
		}
		
		Deki.$(this).find('input, button').each(function(j) {
			
			this.name = this.name.replace(rMatch, i);
			this.id   = this.id.replace(rMatch, i);
			
            if ( this.nodeName.toLowerCase() == 'button' )
            {
            	this.value = this.value.replace(rMatch, i);
            }
		});
	});
}
