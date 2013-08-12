
Deki.$(document).ready(function()
{
	Deki.$('#previews input:radio').hide();

	Deki.$('#previews li').click(function()
	{
		$This = Deki.$(this);
		if (!$This.hasClass('selected'))
		{
			Deki.$('#previews li.selected').removeClass('selected');
			$This.addClass('selected');
			// select the input for IE
			$This.find('input:radio').attr('checked', 'checked');
			// add the medium image preview
			src = $This.find('img').attr('mediumsrc');
			Deki.$('#previews div.screenshot div.image').css('background-image', 'url('+src+')').show();
		}
		
		// toggle the customize css/html links
		if ($This.hasClass('selected-skin'))
		{
			Deki.$('#previews div.code-links').show();
		}
		else
		{
			Deki.$('#previews div.code-links').hide();
		}
	});

	// scroll the selected skin into view
	Deki.$('#previews div.thumbnails').scrollTo(Deki.$('#previews li.selected:first'), {duration: 800});
	
	// show/hide the obsolete and beta skins
	$('.skin-beta').hide();
	$('.skin-obsolete').hide();
	$('.selected-skin').show();
	
	$('#checkbox-beta').change(function(){
		$(this).is(':checked') ? $('.skin-beta').show() : $('.skin-beta').hide();
		
		// always display selected skin
		$('.selected-skin').show();
	});
	
	$('#checkbox-obsolete').change(function(){
		$(this).is(':checked') ? $('.skin-obsolete').show() : $('.skin-obsolete').hide();
		$('.selected-skin').show();
	});
});
