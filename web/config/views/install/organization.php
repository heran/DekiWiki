<?php
/* @var $this DekiInstallerView */
?>

<h1>Your Organization</h1> 
<fieldset id="form-organization"> 
	<h2 class="first">Tell us a little about you.</h2> 
	<div class="table">
		<?php
			$selectUsers = array('0' => $this->msg('Page.Install.tell-us-select'), '25' => '0 - 25', '150' => '26 - 150', '500' => '151 - 500', '2500' => '501 - 2500', '2501' => '2500+');
			$this->inputOption('RegistrarCount', $selectUsers, 'Page.Install.tell-us-ppl');
		?>
		<?php
			$departments = array('' => wfMsg('Page.Install.tell-us-select'), 'Engineering' => 'Engineering', 'Product' => 'Product', 'IT' => 'IT', 'Marketing' => 'Marketing', 'Finance' => 'Finance', 'Operations' => 'Operations', 'Sales' => 'Sales', 'Multiple' => 'Multiple', 'Other' => 'Other');
			$this->inputOption('RegistrarDept', $departments, 'Page.Install.form-department');
		?>
	</div>	
	
	<h2><?php echo $this->msg('Page.Install.tell-us-how'); ?></h2>
	<div class="table"> 
		<ul class="usagelist">
		<?php
			$howtouse = array('wiki', 'collaboration', 'mashups', 'intranet', 'extranet', 'knowledge-base', 'community', 'dms', 'project-management', 'portal', 'si', 'sharepoint', 'dashboard', 'dev-platform', 'reporting', 'other');
			$values = isset($_POST['RegistrarUsage']) ? $_POST['RegistrarUsage']: array(); 
			foreach ($howtouse as $type)
			{
				echo '<li>';
				echo DekiForm::singleInput('checkbox', 'RegistrarUsage['.$type.']', 1, array('checked' => array_key_exists($type, $values)), $this->msg('Page.Install.how-'.$type));
				echo '</li>';
			}
		?>
		</ul>
	</div>
	
	<div class="navButtons"> 
		<div class="backButton"> 
			<a href="#">Back</a>
		</div> 
		<div class="nextButton"> 
			<input type="button" value="Next" class="submit_form"> 
		</div> 
	</div> 
</fieldset>
