/* Define the global Deki.$ for the control panel */
var Deki = Deki || {};
Deki.$ = $;


function setSelectionRange(input, selectionStart, selectionEnd) {
  if (input.setSelectionRange) {
    input.focus();
    input.setSelectionRange(selectionStart, selectionEnd);
  }
  else if (input.createTextRange) {
    var range = input.createTextRange();
    range.collapse(true);
    range.moveEnd('character', selectionEnd);
    range.moveStart('character', selectionStart);
    range.select();
  }
}

function replaceSelection (input, replaceString) {
	if (input.setSelectionRange) {
		var selectionStart = input.selectionStart;
		var selectionEnd = input.selectionEnd;
		input.value = input.value.substring(0, selectionStart)+ replaceString + input.value.substring(selectionEnd);
    
		if (selectionStart != selectionEnd){ 
			setSelectionRange(input, selectionStart, selectionStart + 	replaceString.length);
		}else{
			setSelectionRange(input, selectionStart + replaceString.length, selectionStart + replaceString.length);
		}

	}else if (document.selection) {
		var range = document.selection.createRange();

		if (range.parentElement() == input) {
			var isCollapsed = range.text == '';
			range.text = replaceString;

			 if (!isCollapsed)  {
				range.moveStart('character', -replaceString.length);
				range.select();
			}
		}
	}
}

// We are going to catch the TAB key so that we can use it, Hooray!
function catchTab(item,e){
	c = navigator.userAgent.match("Gecko") ? e.which: e.keyCode;
	if ( c == 9 ) {
		var nTop = Deki.$(item).scrollTop();
		replaceSelection(item,String.fromCharCode(9));
		Deki.$('#'+item.id).scrollTop(nTop);
		setTimeout(function() {
			Deki.$('#'+item.id).scrollTop(nTop).focus();
		}, 0);
		return false;
	}
	return true;
}


Deki.$(document).ready(function()
{
	// add row highlighting for selected rows
	Deki.$('th > :checkbox').click(function()
	{
		var $checkboxes = Deki.$("input[type='checkbox']", Deki.$(this).parents('form'));
		var bChecked = this.checked ? true : false;
		$checkboxes.each(function()
		{
			var $this = Deki.$(this);
			if (bChecked)
			{
				$this.check();
				$this.parents('tr').addClass('selected');
			}
			else
			{
				$this.uncheck();
				$this.parents('tr').removeClass('selected');
			}
		});
	});

	Deki.$('td > :checkbox').change(function()
	{
		var $this = Deki.$(this);

		if ($this.attr('checked'))
		{
			$this.parents('tr').addClass('selected');
		}
		else
		{
			$this.parents('tr').removeClass('selected');
		}
	});
	
	// selection event for group select
	Deki.$('div.groups > span.field :checkbox').change(function()
	{
		var $this = Deki.$(this);

		if ($this.attr('checked'))
		{
			$this.parents('span').addClass('selected');
		}
		else
		{
			$this.parents('span').removeClass('selected');
		}
	}).each(function()
	{ // initialize
		var $this = Deki.$(this);
		if ($this.attr('checked'))
		{
			$this.parents('span').addClass('selected');
		}		
	});

	// hook the defaultValue onto search boxes
	Deki.$('#text-query').defaultValue();
	
	// textarea resizer - may not be loaded on control panel login screen (bug #7785)
	if (Deki.$.TextAreaResizer) {
		Deki.$('textarea.resizable:not(.processed)').TextAreaResizer();
	}
});
