var Deki = Deki || {};

if (typeof Deki.Plugin == 'undefined') {
	Deki.Plugin = {};
}

(function(){
	$(function() {
		Deki.Plugin.SpecialListTemplates._attachEvents();
	});
})();

Deki.Plugin.SpecialListTemplates = {};

Deki.Plugin.SpecialListTemplates._attachEvents = function() {
	$('a.edit-template').click(function() {
		Deki.QuickPopup.Show({
			url: $(this).attr('href'),
			title: $(this).attr('title'),
			width: 550,
			height: 335
		});
				
		return false;
	});
};

Deki.Plugin.SpecialListTemplates.Refresh = function() {
	Deki.Plugin.AjaxRequest('ListTemplates', {
		data: {
			action: 'refresh', 
			namespace: 'special'
		},
		success: function(data) {
			parent.$('#SpecialListTemplates table').replaceWith(data.body);
			parent.Deki.Ui.EmptyFlash();
			parent.Deki.Plugin.SpecialListTemplates._attachEvents();
		}
	});
};
