<?php

/*
 * General class for handling advanced custom properties
 */
abstract class SpecialAdvancedProperties extends SpecialPagePlugin
{
	// identifier used when generating hrefs
	protected $propertiesId = null;
	// used for progressive enhanced version, sent to gui/properties.php
	// @type enum {page, user}
	protected $propertiesType = 'page';
	/*
	 * Used to generate the url when cancel is clicked, if
	 * left null then returns to form=NULL view
	 */
	protected $CancelTitle = null;
	
	protected function &outputProperties($Request, $Properties)
	{
		// add the progressive enhancement javascript
		$this->includeSpecialJavascript('/skins/common/advanced_properties.js', true);
		
		$html = '';
		$form = $Request->getEnum('form', array('simple', 'advanced', 'edit'), 'simple');
		
		switch ($form)
		{
			default:
			case 'simple':
				if ($Request->isPost())
				{
					$this->processSimpleForm($Request, $Properties);
				}
				$html = $this->getSimpleForm($Properties);
				break;
				
			case 'advanced':
				if ($Request->isPost())
				{
					$this->processAdvancedForm($Request, $Properties);
				}
				$html = $this->getAdvancedForm($Properties);
				break;
			// part of the advanced form
			case 'edit':
				if ($Request->isPost())
				{
					$this->processEditForm($Request, $Properties);
				}
				$name = $Request->getVal('name', null);
				$html = $this->getEditForm($Properties, $name);
				break;
		}
		
		return $html;
	}
	
	/**
	 * Processing methods
	 */
	protected function processEditForm($Request, $Properties)
	{	
  		$name = $Request->getVal('property_name');
  		$value = $Request->getVal('property_value');

  		$Properties->setCustom($name, $value);
  
  		$Result = $Properties->update();
  		// no multistatus is possible here
  		if (!$Result->handleResponse())
  		{
  			DekiMessage::error($Result->getError());
  			return;
  		}

  		DekiMessage::success(wfMsg('Page.Properties.success'));
		
		// redirect to self
		$this->redirect($this->getLocalUrl('advanced'));
		return;
	}
	
	protected function processAdvancedForm($Request, $Properties)
	{
		$action = $Request->getVal('action', null);
		
		switch ($action)
		{				
			case 'delete':
				// delete a batch of properties
				$names = $Request->getArray('property_names');
				if (empty($names))
				{
					// no selection made
					DekiMessage::error(wfMsg('Page.Properties.data.no-selection'));
					break;
				}
				// delete these properties!
				foreach ($names as $name)
				{
					$Properties->removeCustom($name);
				}
				
				$Result = $Properties->update();
				if (!$Result->handleResponse())
				{
					DekiMessage::error($Result->getError());
					return;
				}
				DekiMessage::success(wfMsg('Page.Properties.success.deleted'));
				break;
				
			case 'new':
				// create a new property
				$name = $Request->getVal('property_name');
				$value = $Request->getVal('property_value');
				
				if ($Properties->addCustom($name, $value))
				{
					$Result = $Properties->update();
					if (!$Result->handleResponse())
					{
						DekiMessage::error($Result->getError());
						return;
					}
					DekiMessage::success(wfMsg('Page.Properties.success.created'));
					break;
				}
				else
				{
					DekiMessage::error(wfMsg('Page.Properties.error.created'));
					return;
				}

			// unknown action, do nothing
			default:
		}

		// redirect to advanced
		$this->redirect($this->getLocalUrl('advanced'));
		return;
	}	

	/**
	 * Display methods
	 */

	/**
	 * @param $propertyName - name of the property to edit
	 */
	protected function &getEditForm($Properties, $propertyName)
	{
		$Title = $this->getTitle();
		
		// make sure the property is valid?
		if ($propertyName == '' || !$Properties->hasCustom($propertyName))
		{
			// redirect to advanced
			$this->redirect($this->getLocalUrl('advanced'));
			return;
		}
		
		$html =		
		'<div class="mode">'.
			'<span class="basic">'.
				'<a href="'. $this->getLocalUrl() .'">'.
					wfMsg('Page.Properties.basic').
				'</a>'.
			'</span>'.
			'<span>'.
				'<a href="'. $this->getLocalUrl('advanced') .'">'.
					wfMsg('Page.Properties.advanced').
				'</a>'.
			'</span>'.
		'</div>';
				
		// edit key table
		$Table = new DomTable();
		$Table->addClass('inputs');
		$Table->setColWidths('100', '100');
		
		$Table->addRow();
		$Table->addHeading(wfMsg('Page.Properties.data.name'));
		$Table->addHeading(wfMsg('Page.Properties.data.value'));
		
		$Table->addRow();
		$Table->addCol(
			htmlspecialchars($propertyName) .
			DekiForm::singleInput('hidden', 'property_name', $propertyName, array())
		);
		$Table->addCol(
			DekiForm::singleInput('text', 'property_value', $Properties->getCustom($propertyName), array())
		);
		
		// build some urls
		$actionUrl = $this->getLocalUrl('edit', 'name=' . urlencode($propertyName));
		$cancelUrl = $this->getLocalUrl('advanced');
		
		$html .= '<div class="edit">';
			$html .= '<form method="post" action="'. $actionUrl .'" class="advanced">';
				$html .= '<h3>'. wfMsg('Page.Properties.form.edit') .'</h3>';
				$html .= $Table->saveHtml();
				$html .= DekiForm::singleInput('button', 'action', 'edit', array(), wfMsg('Page.Properties.form.submit'));
				$html .= '<span class="or">'.wfMsg('Page.Properties.form.cancel', $cancelUrl).'</span>';
			$html .= '</form>';
		$html .= '</div>';
		
		return $html;
	}
	
	protected function &getAdvancedForm($Properties)
	{
		$html =				
		'<div class="mode">'.
			'<span class="basic">'.
				'<a href="'. $this->getLocalUrl() .'">'.
					wfMsg('Page.Properties.basic').
				'</a>'.
			'</span>'.
			'<span class="selected">'.
				'<a>'.
					wfMsg('Page.Properties.advanced').
				'</a>'.
			'</span>'.
		'</div>';
				
		// new key table
		$Table = new DomTable();
		$Table->addClass('inputs');
		$Table->setColWidths('100', '100');

		$Table->addRow();
		$Table->addHeading(wfMsg('Page.Properties.data.name'));
		$Table->addHeading(wfMsg('Page.Properties.data.value'));
		
		$Table->addRow();
		$Table->addCol(DekiForm::singleInput('text', 'property_name', null, array()));
		$Table->addCol(DekiForm::singleInput('text', 'property_value', null, array()));

		// additional markup and form
		$actionUrl = $this->getLocalUrl('advanced');
		$cancelUrl = !is_null($this->CancelTitle) ? $this->CancelTitle->getLocalUrl() : $this->getLocalUrl();
					
		$html .= '<div class="new">';
			$html .= '<form method="post" action="'. $actionUrl .'" class="advanced">';
				$html .= '<h3>'. wfMsg('Page.Properties.form.new') .'</h3>';
				$html .= $Table->saveHtml();
				$html .= '<span class="set-button">'.DekiForm::singleInput('button', 'action', 'new', array('class'=>'set'), wfMsg('Page.Properties.form.submit')).'</span>';
				$html .= '<span class="or">'.wfMsg('Page.Properties.form.cancel', $cancelUrl).'</span>';
			$html .= '</form>';
		$html .= '</div>';
		
		
		// build the properties display table
		$Table = new DomTable();
		$Table->addClass('deki-properties');
		// space the columns
		$Table->setColWidths('18', '', '', '');
		
		$Table->addRow();
		$Th = $Table->addHeading(
			DekiForm::singleInput('checkbox', 'all', '', array('onclick' => 'return select_checkboxes(this);'))
		);
		$Th->addClass('checkbox');	
		$Table->addHeading(wfMsg('Page.Properties.data.name'));
		
		$Table->addHeading(wfMsg('Page.Properties.data.value'));
		$Table->addHeading('&nbsp;');

		$properties = $Properties->getAllCustom();
		if (empty($properties))
		{
			// no page properties
			$Table->addRow();
			$Td = $Table->addCol(wfMsg('Page.Properties.data.empty'), 4);
			$Td->addClass('empty');
		}
		else
		{
			foreach ($properties as $name => &$value)
			{
				$Tr = $Table->addRow();
				$Tr->setAttribute('id', 'deki-pageproperties-'. md5($name));
				
				$name_encoded = htmlspecialchars($name);
				$Td = $Table->addCol(
					DekiForm::singleInput('checkbox', 'property_names[]', $name_encoded, array(), $name_encoded)
				);
				$Td->setAttribute('colspan', 2);
				$Td->addClass('name');
				
				$Td = $Table->addCol(htmlspecialchars($value));
				$Td->addClass('value');
				
				$editUrl = $this->getLocalUrl('edit', 'name='. urlencode($name));
				$Td = $Table->addCol(
					'<a href="'. $editUrl .'">'.
						wfMsg('Page.Properties.data.edit').
					'</a>'
				);
				$Td->addClass('edit');
			}
			unset($property);
		}
		
		$html .= 
		'<div class="existing">'.
			'<h3>'. wfMsg('Page.Properties.form.custom') .'</h3>'.
			'<form method="post" class="advanced">'.
				'<div class="actions">'.
					wfMsg('Page.Properties.selected'). 
					DekiForm::singleInput('button', 'action', 'delete', array('class'=>'delete'), wfMsg('Page.Properties.delete')).
				'</div>'.
				$Table->saveHtml().
			'</form>'.
			// required for the progressive enhancement
			'<script type="text/javascript">'.
				'var Deki = Deki || {};'.
				// can be user or page
				'Deki.SpecialPropertiesType = "' . $this->propertiesType . '";'.
				// can be userId or pageId depending on type
				(is_null($this->propertiesId) ? '' : 'Deki.SpecialPropertiesId = ' . (int)$this->propertiesId . ';').
			'</script>'.
		'</div>';

		return $html;
	}
	
	protected function setPropertiesId($id)
	{
		$this->propertiesId = $id;
	}

	protected function setPropertiesType($type = 'page')
	{
		$this->propertiesType = $type == 'user' ? 'user' : 'page';
	}
	
	protected function getCancelTitleText()
	{
		$cancelText = $this->CancelTitle->getPrefixedText();
		if ($this->CancelTitle->isHomepage())
		{
			global $wgSitename;
			$cancelText = $wgSitename;
		}
		
		return $cancelText;
	}

	/*
	 * Helper method for generating urls
	 */
	protected function getLocalUrl($form = null, $append = '')
	{
		$params = array();

		if (!is_null($this->propertiesId))
		{
			$params[] = 'id=' . $this->propertiesId;
		}
		if (!is_null($form))
		{
			$params[] = 'form=' . $form;
		}

		if (!empty($append))
		{
			$params[] = $append;
		}
		
		return $this->getTitle()->getLocalUrl(implode('&', $params));
	}
}
