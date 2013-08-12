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

class DekiInstallerView extends DekiView
{
	/**
	 * Outputs a input box
	 * 
	 * @param string $name - name of the form field
	 * @param string $labelKey - resource key for the label
	 * @param string $descriptionKey - resource key for the description
	 * @param array $params - additional attributes for the input
	 * @return
	 */
	protected function inputText($name, $labelKey = null, $descriptionKey = null, $params = array())
	{
		$this->inputField('text', $name, $labelKey, $descriptionKey, $params);
	}
	
	protected function inputHidden($name, $params = array())
	{
		$this->inputField('hidden', $name, null, null, $params);
	}
	
	protected function inputPassword($name, $labelKey = null, $descriptionKey = null, $params = array())
	{
		$this->inputField('password', $name, $labelKey, $descriptionKey, $params);
	}
	
	protected function inputConfirm($name, $labelKey = null, $descriptionKey = null, $params = array())
	{
		$this->inputField('confirm', $name, $labelKey, $descriptionKey, $params);	
	}
	
	protected function inputField($type, $name, $labelKey = null, $descriptionKey = null, $params = array())
	{
		global $conf;
		$label = is_null($labelKey) ? null : $this->msg($labelKey);
		$value = isset($conf->$name) ? $conf->$name : null;
		
		// enable auto-complete for the installer
		$params['autocomplete'] = 'on';

		// special handling for confirm inputs
		if ($type == 'confirm')
		{
			$type = 'text';
			$name = '-'.$name;
			$params['disabled'] = 'disabled';
			$params['class'] = 'confirm';
		}
		
		
		echo '<div class="input">';
			echo DekiForm::singleInput($type, $name, $value, $params, $label);
			echo '<div class="label-required">' . 'Field is required' . '</div>';
			if (!is_null($descriptionKey))
			{
				echo '<div class="description">';
				echo $this->msg($descriptionKey);
				echo '</div>';
			}
		echo '</div>';
	}
	
	/**
	 * Outputs a select box
	 * 
	 * @param string $name - name of the form field
	 * @param array $options - field options
	 * @param string $labelKey - resource key for the label
	 * @param string $descriptionKey - resource key for the description
	 * @param array $params - additional attributes for the input
	 * @return
	 */
	protected function inputOption($name, $options, $labelKey = null, $descriptionKey = null, $params = array())
	{
		global $conf;
		$label = is_null($labelKey) ? null : $this->msg($labelKey);
		$value = isset($conf->$name) ? $conf->$name : null;
		
		echo '<div class="input">';
			echo DekiForm::multipleInput('select', $name, $options, $value, $params, $label);
			echo '<div class="label-required">' . 'Field is required' . '</div>';
			if (!is_null($descriptionKey))
			{
				echo '<div class="description">';
				echo $this->msg($descriptionKey);
				echo '</div>';
			}
		echo '</div>';	
	}
	
	/**
	 * Outputs formmatted error messages. Template variable should be an array of messages generated via the DekiInstaller controller.
	 * Method should only be called once per request due to javascript outputfor installInputErrors.
	 */
	protected function outputErrors($title, $key, $showRetry = false)
	{
		$installInputErrors = array();
		?>
			<h1><?php echo htmlspecialchars($title); ?></h1>
			<?php if (!is_null($showRetry)) : ?>
				<h1 class="error">
					Oops, there was a problem with your installation.
					<?php echo $showRetry ? ' <a href=".">Retry dependencies</a>' : ''; ?>
				</h1>
			<?php endif; ?>
			<fieldset>
				<ul class="envCheck">
					<?php foreach ($this->get($key) as $message) : ?>
						<?php
							$class = $message['type'];
							$onClick = '';
							if (isset($message['input']))
							{
								$class .= ' input input-error-'.$message['input'];
								$onClick = " onclick=\"MT.Install.InputError(this, '". $message['input'] ."')\"";
								
								// hacky way to determine invalid inputs
								$installInputErrors[] = $message['input'];
							}
						?>
						<?php if ($message['html']) : ?>
							<?php echo $message['contents']; ?>
						<?php else : ?>
							<li class="<?php echo $class; ?>" <?php echo $onClick; ?>>
								<?php echo htmlspecialchars($message['contents']); ?>
							</li>
						<?php endif; ?>
					<?php endforeach; ?>
				</ul>
				<script type="text/javascript">
					var installInputErrors = <?php echo json_encode($installInputErrors); ?>;
				</script>
			</fieldset>
	<?php
	}
}
