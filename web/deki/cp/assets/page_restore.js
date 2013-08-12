Deki.$(function()
{
	var AdminRestore = {};
	AdminRestore.showhide = function(e)
	{
		var jAnchor = Deki.$(e.target);
		var nId = jAnchor.attr('restoreId');
		var sHref = jAnchor.attr('href') + '&html=table';

		// add the loading image
		Deki.$(jAnchor).find('img').hide();
		Deki.$(jAnchor).after('<img class="loader" src="/skins/common/icons/anim-wait-circle.gif" />');
		
		Deki.$("#restore-listing").load(sHref, null, AdminRestore.registerEvents);
		
		// stop the event
		e.preventDefault();	
		return false;
	};

	Deki.$('#restore-listing table a.expand').click(AdminRestore.showhide);
});
