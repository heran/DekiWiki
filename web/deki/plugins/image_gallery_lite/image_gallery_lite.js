(function($)
{
	$(function()
	{
		Deki.Plugin.Subscribe('FilesTable.onRefreshTable', function()
		{
			Deki.Plugin.ImageGalleryLite.Refresh();
		});
	});
})(Deki.$);

if (typeof Deki.Plugin == 'undefined') {
	Deki.Plugin = {};
}

Deki.Plugin.ImageGalleryLite = {};
Deki.Plugin.ImageGalleryLite.ID = 'deki-image-gallery-lite';

Deki.Plugin.ImageGalleryLite.Refresh = function(pageId)
{
	var options = {
		type: 'get',
		url: Deki.Plugin.AJAX_URL,
		dataType: 'json',
		data: {
			'formatter': 'mindtouchimagegallerylite',
			'page_id': pageId || Deki.PageId
		},
		success: function(data, status) {
			var $container = $('#' + Deki.Plugin.ImageGalleryLite.ID);
			$container.html(data.body);
			// rehook thickbox
			tb_init('#'+ Deki.Plugin.ImageGalleryLite.ID +' a.lightbox');
		}		
	};

	Deki.Gui.AjaxRequest(options);
};