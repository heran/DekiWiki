<?php
/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
 * please review the licensing section.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 * http://www.gnu.org/copyleft/gpl.html
 */

class SpecialPageForm
{
	/**
	 * @param Title $Title - should be the title to generate links for
	 */
	static public function getLanguageFilter(Title $Title, $hidden = array(), $selected = null)
	{
		$html = '';

		if (DekiLanguage::isSitePolyglot()) 
		{
			$options = wfAllowedLanguages(wfMsg('Form.language.filter.all'));

			$html = '<form method="get" action="' . $Title->getLocalUrl() . '">';
				foreach ($hidden as $field => $value)
				{
					$html .= DekiForm::singleInput('hidden', $field, $value);
				}
				$html .= '<label for="select-language">' . wfMsg('Form.language.filter.label') . '</label> ';
				$html .= DekiForm::multipleInput('select', 'language', $options, $selected);
				$html .= ' <input type="submit" value="' . wfMsg('Form.language.filter.submit') . '" />';
			$html .= '</form>';
		}
		
		return $html;		
	}
	
	static function getUserAutocomplete(Title $Title, $label, $inputName, $submitValue)
	{
		$form = '<form method="get" action="' . $Title->getLocalUrl() . '" class="deki-user-autocomplete">';
		
			// label
			$form .= '<table cellspacing="0" cellpadding="0"><tr><td>';
				$form .= '<label for="autoCompInput">' . $label . ' </label>';

			// input field with autocomplete container
			$form .= '</td><td>';
				$form .= '<div id="autoComplete">';
					$form .= DekiForm::singleInput('text', $inputName, null, array('id' => 'autoCompInput'));
					$form .= '<div id="autoCompContainer"></div>';
				$form .= '</div>';

			// submit button
			$form .= '</td><td>';
				$form .= DekiForm::singleInput('submit', null, $submitValue);
			$form .= '</td></tr></table>';
			
			// YUI autocomplete
			$form .= '<script type="text/javascript">
				YAHOO.util.Event.onDOMReady(
					function()
					{
						var addAutocomplete = function()
						{
							var dataSource	= new YAHOO.util.XHRDataSource("/deki/gui/usergroupsearch.php");
							dataSource.responseType = YAHOO.util.XHRDataSource.TYPE_JSON;
							dataSource.responseSchema = {
								resultsList : "results",
								fields : [
									"item",
									{key: "id"}
								]
							};
							
							var autoComplete 		= new YAHOO.widget.AutoComplete("autoCompInput", "autoCompContainer", dataSource);
							autoComplete.animVert 	= false;
							
							autoComplete.generateRequest = function(sQuery) {
								return "?mode=users&query=" + sQuery;
							};
							
							var itemSelectHandler = function(sType, aArgs) {
								var oAcInstance = aArgs[0];
								var oData = aArgs[2];
								
								if ( oData && oData[0] )
								{
									oAcInstance._elTextbox.value = Deki.$.htmlDecode(oData[1] ? oData[1] : oData[0]);
								}
							};
							
							autoComplete.itemSelectEvent.subscribe(itemSelectHandler);
						}
						
						var aURL = [
							Deki.PathCommon + "/yui/datasource/datasource.js",
							Deki.PathCommon + "/yui/autocomplete/autocomplete.js"
						];
					
						YAHOO.util.Get.css(
							Deki.PathCommon + "/yui/autocomplete/autocomplete.css",
							{
								onSuccess : function()
								{
									YAHOO.util.Get.script(aURL, {onSuccess : addAutocomplete});
								}
							}
						);					
					}
				);
				</script>
			';
		$form .= '</form>';
		
		return $form;				
	}
}
