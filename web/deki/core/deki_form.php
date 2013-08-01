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

/**
 * General purpose form handling class
 * @note class used for encapsulation
 */
class DekiForm 
{
	// @const determines the key to post button values to
	const BUTTON_ARRAY = 'deki_buttons';
	const TOKEN = 'csrf_token';

	/**
	 * Validates presence of correct anti-CSRF token
	 * 
	 * @return bool
	 */
	public static function hasValidToken()
	{
		return DekiRequest::getInstance()->getVal(self::TOKEN, null) == $_SESSION[self::TOKEN];
	}

	/**
	 * Generates hidden input with anti-CSRF token; sets session as needed
	 * 
	 * @return string - HTML for token input element
	 */
	public static function tokenInput()
	{
		if (!isset($_SESSION[self::TOKEN])) {
			$token = md5(uniqid(rand(), true));
			$_SESSION[self::TOKEN] = $token;
		}
		
		return self::singleInput('hidden', self::TOKEN, $_SESSION[self::TOKEN]);
	}

	public static function singleInput($type, $name, $value = null, $params = array(), $labeltext = '') 
	{		
		$Request = DekiRequest::getInstance();
		if (is_null($value) && !is_null($Request) && $Request->getVal($name) != '' && $type != 'password') 
		{
			$value = $Request->getVal($name);
		}
		// setup post through types here
		else if (($type == 'text' || $type == 'textarea') && !is_null($Request->getVal($name)))
		{
			$value = $Request->getVal($name);
		}
			
		//text and password options
		if ($type == 'text' || $type == 'password') 
		{
			if (!isset($params['size'])) 
			{
				$params['size'] = '24';
			}

			if (!isset($params['spellcheck'])) 
			{
				$params['spellcheck'] = 'false';
			}
			
			// no magical autocomplete for passwords
			if ( !isset($params['autocomplete']) && ($type != 'password') ) 
			{
				$params['autocomplete'] = 'off';
			}
		}
		
		//textarea options
		if ($type == 'textarea') 
		{
			if (!isset($params['rows'])) 
			{
				$params['rows'] = '10';
			}
			if (!isset($params['cols'])) 
			{
				$params['cols'] = '36';
			}
			if (!isset($params['spellcheck'])) 
			{
				$params['spellcheck'] = 'false';
			}
			if (!isset($params['autocomplete'])) 
			{
				$params['autocomplete'] = 'off';
			}
		}
		
		//checkbox & radio options
		if ($type == 'checkbox' || $type == 'radio') 
		{
			if (isset($params['checked']) && ($params['checked'] === true || $params['checked'] == 'checked'))
			{
				$params['checked'] = 'checked';
			}
			else 
			{
				unset($params['checked']);
			}
			if (is_null($value)) 
			{
				$value = 'checked';
			}
		}
		
		//define class and id
		if (!isset($params['class']) && $type != 'hidden') 
		{
			$params['class'] = 'input-'.$type;
		}
		
		if (!isset($params['id']) && $type != 'submit' && $type != 'button') 
		{
			if ($type != 'radio') 
			{
				$params['id'] = $type.'-'.$name;
			}
			else
			{
				$params['id'] = $type.'-'.$name.'-'.$value;
			}
		}
		
		// IE6 hack; butons get converted to input type="submit", but let's attach an extra class
		if ($type == 'button' && strpos($_SERVER['HTTP_USER_AGENT'], 'MSIE 6.0') !== false) 
		{
			if (isset($params['class'])) {
				$params['class'] = $params['class'].' button';
			}
			else
			{
				$params['class'] = 'button';	
			}
		}
		
		$html = array();
		$paras = array();
		
		if (isset($params['disabled']) && ($params['disabled'] === true || $params['disabled'] == 'disabled')) 
		{
			$params['disabled'] = 'disabled';
			$params['class'] = $params['class'].' disabled';
			unset($params['onclick']);
		}
		else 
		{
			unset($params['disabled']);
		}
		
		foreach ($params as $key => $param) 
		{
			if (is_null($param))
			{
				continue;	
			}
			$paras[]= $key.'="'.$param.'"';
		}
		// make the inputs safe for html
		$_escape_types = array('textarea', 'text', 'hidden');
		if (in_array($type, $_escape_types)) 
		{ 
			$value = htmlspecialchars($value);	
		}
		
		if ($type == 'button') 
		{
			if ($Request->isIE6())
			{
				// guerric: the 'else' hack below does not work for IE6, check the useragent
				$html = '<input type="submit" name="'. self::BUTTON_ARRAY .'['.$name.']['. $value .']" value="'. htmlspecialchars($labeltext) .'" '.implode(' ', $paras).'/>';
			}
			else
			{
				// guerric: due to IE's inability to handle name/values for buttons, the following is used
				// DekiRequest parses out these inputs to normal name/value pairs
				$html = '<button type="submit" name="'. self::BUTTON_ARRAY .'['.$name.']['. $value .']" '.
						'value="'.$value.'" '.implode(' ', $paras).'><span>'. htmlspecialchars($labeltext) .'</span></button>';
			}
		}
		else if ($type != 'textarea') 
		{
			$html = '<input '.
				'type="'. $type .'"' .' '.
				(!is_null($value) ? 'value="'.$value.'"': '') .' '.
				($name != '' ? 'name="'.$name.'"': '') .' '.
				implode(' ', $paras) .' />'
			;
		}
		else if ($type == 'textarea') 
		{
			// force autocomplete off for textareas
			$paras['autocomplete'] = 'off';
			$html = '<textarea name="'. $name .'" '. implode(' ', $paras) .'>'. $value .'</textarea>';
		}
		// add the field label
		if ($labeltext != '' && $type != 'button') 
		{
			$autoclass = 'label-'. (isset($params['id']) ? $params['id'] : $type);
			$label = '<label for="'.$params['id'].'" class="'. $autoclass .'">'. $labeltext .'</label>';
			if ($type == 'text' || $type == 'password' || $type == 'file')
			{
				// prepend the label for text inputs
				$html = $label . ' ' . $html;
			}
			else
			{
				// append for other types
				$html .= ' ' . $label;
			}
		}

		return $html;
	}
	
	public static function multipleInput($type, $name, $data, $value = null, $params = array(), $labeltext = '') 
	{
		$Request = DekiRequest::getInstance();
		$field = '';
		
		if (!isset($params['class'])) 
		{
			$params['class'] = 'input-'.$type;
		}
		// capture the post through
		if (!is_null($Request->getVal($name)))
		{
			$value = $Request->getVal($name);
		}
		
		if (!isset($params['id']) && $type != 'radio') 
		{
			$params['id'] = $type.'-'.$name;	
		}

		if (isset($params['disabled']) && $params['disabled'] === true) 
		{
			$params['disabled'] = 'disabled';
			$params['class'] = $params['class'].' disabled';
		}
		else 
		{
			unset($params['disabled']);
		}

		if (count($params) > 0) 
		{
			foreach ($params as $k => $v)
			{
				$field.= ' '.$k.'="'. htmlspecialchars($v) .'"';	
			}	
		}
		// check if we are trying to use a radio button > 3
		if ($type == 'radio' && count($data) > 3)
		{
			// show a select instead
			$type = 'select';
		}
		
		$html = '';
		if ($type == 'select') 
		{
			$html = '<select name="'.$name.'" '.$field.'>';
			if (count($data) > 0)
			{
				foreach($data as $k => $v) 
				{
					$selected = '';
					if (!is_null($value)) 
					{
						if ((is_array($value) && in_array($k, $value)) 
							|| (is_string($value) && $k == $value))
						{
							$selected = ' selected="selected"';
						}
					}
					if (!is_array($v)) 
					{
						$html .= '<option value="'. htmlspecialchars($k) .'"'.$selected.'>'. htmlspecialchars($v) .'</option>';	
					}
					else
					{
						$html .= '<optgroup label="'. htmlspecialchars($k) .'">';
						foreach ($value as $k => $dvalue) 
						{
							$html .= '<option value="'. htmlspecialchars($k) .'"'.$selected.'>'. htmlspecialchars($dvalue) .'</option>';	
						}
						$html.= '</optgroup>';
					}
				}
			}	
			$html.= '</select>';
			
			if ($labeltext != '')
			{
				$autoclass = 'label-'. (isset($params['id']) ? $params['id'] : $type);
				$label = '<label for="'.$params['id'].'" class="'. $autoclass .'">'. $labeltext .'</label>';
				// prepend the label
				$html = $label . ' ' . $html;
			}
		}
		else if ($type == 'radio')
		{
			$html = '';
			if (count($data) > 0) 
			{
				foreach($data as $k => $v) 
				{
					$id = $type.'-'.$name.'-'.$k;
					$idfield = ' id="'.$id.'"';

					$html.= '<span class="field field-radio">'.
							'<input type="'.$type.'" '.
							'name="'.$name.'" '.$field.' '.$idfield.' '.
							'value="'. htmlspecialchars($k) .'"'.($k == $value ? ' checked = "checked"': '').
							'class="input-radio"' .'>'.
							// royk: $v shouldn't be htmlspecialchar-ed; there are cases where we'll want HTML to render; like radio options
							'<label class="label-radio" for="'.$id.'">'. $v .'</label>'.
							'</span>';
				}
			}
		}
		// checked type does not honor params
		else if ($type == 'checkbox')
		{
			$html = '';
			if (count($data) > 0) 
			{
				$values = $Request->getArray($name);

				foreach($data as $value => $details) 
				{
					// handle the post through
					if ($Request->isPost())
					{
						$checked = in_array($value, $values);
					}
					else
					{
						$checked = isset($details['checked']) && $details['checked'];
					}
					$checked = $checked ? 'checked="checked" ' : '';

					if (is_array($details))
					{
						$disabled = isset($details['disabled']) && $details['disabled'] ? 'disabled="disabled" ' : '';
						$label = isset($details['label']) ? $details['label'] : '';
					}
					else
					{
						$label = $details;
					}

					$id = isset($details['id']) ? $details['id'] : $type.'-'.$name.'-'.$value;

					$html.= '<span class="field">'.
							'<input type="'. $type .'" '.
							'name="'. $name .'['. $value.']" '.
							'value="' . htmlspecialchars($value) . '" '.
							'id="'. $id .'" '.
							$checked .
							$disabled .
							'/>' .
							'<label class="label-checkbox" for="'. $id .'">'. htmlspecialchars($label) .'</label>'.
							'</span>';
				}
			}
		}

		return $html;
	}


	/**
	 * Builds a configuration table with post back support
	 *
	 * @param array &$configuration - reference to an array of key/value pairs which can be overridden by post data
	 * @param int $minFields - the minimum number of configuration fields to show
	 * @param string $keysField - name of the keys post array
	 * @param string $valuesField - name of the values post array
	 * @param string $actionField - name of the action post field
	 *
	 * @return string - rendered configuration table html
	 */
	// TODO: localize
	public static function configTable(
		&$configuration,
		$minFields = 3,
		$keysField = 'df_config_keys',
		$valuesField = 'df_config_values',
		$actionField = 'df_action'
	)
	{
		$Request = DekiRequest::getInstance();
		
		// set the minimum number of fields to display
		$minFields = $minFields < 0 ? 3 : $minFields;

		// extract the config keys
		$configKeys = array_keys($configuration);
		// extract the config values
		$configValues = array_values($configuration);

		if ($Request->isPost())
		{
			// fetch the table's action
			@list($action, $index) = explode('[', $Request->getVal($actionField), 2);
			if (substr($index, -1) == ']')
			{
				$index = substr($index, 0, -1);
			}

			$configKeys = $Request->getVal($keysField, array());
			$configValues = $Request->getVal($valuesField, array());
			// we've added a hidden config key & value for javascript, remove it
			unset($configKeys[-1]);
			unset($configValues[-1]);

			// override the services config with the posted config
			$configuration = array();
			for ($i = 0, $i_m = count($configKeys); $i < $i_m; $i++)
			{
				if (!empty($configKeys[$i]))
				{
					$configuration[$configKeys[$i]] = $configValues[$i];
				}
			}
			
			switch ($action)
			{
				case $keysField . '_add':
					$configKeys[] = '';
					$configValues[] = '';
					break;

				case $keysField . '_remove':
					unset($configKeys[$index]);
					unset($configValues[$index]);
					// need to reindex the arrays to remove "hole"
					$newKeys = array();
					$newValues = array();
					for ($i = 0, $i_m = count($configKeys) + 1; $i < $i_m; $i++)
					{
						if ($i == $index)
						{
							continue;
						}
						$newKeys[] = $configKeys[$i];
						$newValues[] = $configValues[$i];
					}
					$configKeys = $newKeys;
					$configValues = $newValues;
					break;

				default:
			}
		}

		// create the configuration table		
		$Table = new DomTable();
		$Table->setAttribute('class', 'config');
		$Table->setColWidths('', '', '80');

		// always add a hidden row for javascript
		$Table->addRow()->setAttribute('style', 'display: none');
		$Table->addCol(
			'<label>'. wfMsg('Common.form.config.key') .'</label>' . DekiForm::singleInput('text', $keysField .'['. -1 .']', null, array('class' => 'short'))
		);
		$Table->addCol(
			'<label>'. wfMsg('Common.form.config.value') .'</label>' . DekiForm::singleInput('text', $valuesField .'['. -1 .']', null, array('class' => 'short'))
		);
		$Table->addCol(
			DekiForm::singleInput('button', $actionField, $keysField .'_remove['. -1 .']', array('class' => 'remove'), wfMsg('Common.form.config.remove'))
		);

		// make sure at least the min number of configuration fields are showing
		for ($i = 0, $i_m = count($configKeys); $i < $i_m || $i < $minFields; $i++)
		{
			$key = isset($configKeys[$i]) ? $configKeys[$i] : '';
			$value = isset($configValues[$i]) ? $configValues[$i] : '';

			$Table->addRow();
            $Table->addCol(
				'<label>'. wfMsg('Common.form.config.key') .'</label>' . DekiForm::singleInput('text', $keysField .'['. $i .']', $key, array('class' => 'short'))
			);
            $Table->addCol(
				'<label>'. wfMsg('Common.form.config.value') .'</label>' . DekiForm::singleInput('text', $valuesField .'['. $i .']', $value, array('class' => 'short'))
			);
            $Table->addCol(
				DekiForm::singleInput('button', $actionField, $keysField .'_remove['. $i .']', array('class' => 'remove'), wfMsg('Common.form.config.remove'))
			);
		}
		
		// create the new config key button
		$html = '<div class="options">'.
				DekiForm::singleInput('button', $actionField, $keysField .'_add', array('class' => 'add'),  wfMsg('Common.form.config.add-new-key')).
				'</div>';

		return '<div class="configtable">'.$html . $Table->saveHtml().'</div>';
	}

}
