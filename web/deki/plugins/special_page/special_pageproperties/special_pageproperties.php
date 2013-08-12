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

if (defined('MINDTOUCH_DEKI'))
{
	DekiPlugin::registerHook(Hooks::SPECIAL_PAGE_PROPERTIES, 'wfSpecialPageProperties');
}

// include the base advanced properties class
DekiPlugin::requirePhp('special_page', 'special_advanced_properties.php');

function wfSpecialPageProperties($pageName, &$pageTitle, &$html, &$subhtml)
{
	$Special = new SpecialPageProperties($pageName, basename(__FILE__, '.php'));
	
	// set the page title
	$pageTitle = $Special->getPageTitle();
	
	$Special->output($html, $subhtml);	
}

class SpecialPageProperties extends SpecialAdvancedProperties
{
	protected $pageName = 'PageProperties';

	
	public function output(&$html, &$subhtml)
	{
		// add some files
		$this->includeSpecialCss('special_pageproperties.css');
	
		$Request = DekiRequest::getInstance();
				
		$pageId = $Request->getInt('id', 0);
		// is this an advanced request?
		$form = $Request->getEnum('form', array('simple', 'advanced', 'edit'), 'simple');
		
		// configure the advanced properties
		$this->setPropertiesId($pageId);
		// attempt to fetch the page information
		$this->CancelTitle = Title::newFromId($pageId);	
		// TODO: check if the user has permission? or is this handled by title? 
		if (is_null($this->CancelTitle))
		{
			self::redirectHome();
			return;
		}
		
		$PageProperties = new DekiPageProperties($pageId);
		$html = '<div id="deki-pageproperties">' .$this->outputProperties($Request, $PageProperties) . '</div>';

		$subhtml = wfMsg(
			'Page.PageProperties.return-to',
			$this->CancelTitle->getLocalUrl(),
			htmlspecialchars($this->getCancelTitleText())
		);
 
		return $html;
	}
	
	protected function processSimpleForm($Request, $Properties)
	{
		$language = $Request->getVal('language');
		
		// validate page properties
		$Properties->setLanguage($language);

		$Result = $Properties->update();

		if (!$Result->handleResponse())
		{
			// general error
			DekiMessage::error($Result->getError());
			return;
		}
		else if ($Result->is(207))
		{
			// TODO: multistatus response
			$Result->debug(1);
			throw new Exception('Multistatus response');
		}
		
		DekiMessage::success(wfMsg('Page.PageProperties.success'));
		
		// redirect to self
		$this->redirectToSelf('id='. $Properties->getPageId());
		return;
	}
	
	protected function &getSimpleForm($PageProperties)
	{
		$Title = $this->getTitle();
		
		$html =				
		'<div class="mode">'.
			'<span class="basic selected">'.
				'<a>'.
					wfMsg('Page.Properties.basic').
				'</a>'.
			'</span>'.
			'<span>'.
				'<a href="'. $this->getLocalUrl('advanced') .'">'.
					wfMsg('Page.Properties.advanced').
				'</a>'.
			'</span>'.
		'</div>';
		
		// page language
		$html .= '<h3>'. wfMsg('Page.PageProperties.form.language') .'</h3>' .
			'<div class="field">' .
				self::getLanguageOptions($PageProperties->getLanguage()) .
			'</div>';

		// additional markup and form
		$actionUrl = $Title->getLocalUrl('id='. $PageProperties->getPageId());
		$cancelUrl = $this->CancelTitle->getLocalUrl();
		$cancelPageTitle = htmlspecialchars($this->getCancelTitleText());
		
		$html = 
			'<form method="post" action="'. $actionUrl .'" class="simple">'
				. $html
				. '<div class="submit">'
					. DekiForm::singleInput('button', 'action', 'save', array('class'=>'set'), wfMsg('Page.PageProperties.form.basic.submit'))
					. '<span class="or">'
					. wfMsg('Page.PageProperties.form.cancel', $cancelUrl, $cancelPageTitle)
					. '</span>'
				. '</div>'
			. '</form>';

		return $html;
	}
	

	protected static function getLanguageOptions($pageLanguage = null)
	{
		global $wgLanguagesAllowed;

		$options = array();
		if (!empty($wgLanguagesAllowed))
		{
			$options = wfAllowedLanguages();
		}
		// TODO: centralize where this language key is being pulled from
		$options = array('' => wfMsg('Page.UserPreferences.form.language.default')) + $options;
		
		return DekiForm::multipleInput('select', 'language', $options, $pageLanguage, array());		
	}
}
