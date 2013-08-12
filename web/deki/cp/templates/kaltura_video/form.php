<?php 

$this->includeCss('settings.css'); 
$this->includeCss('kaltura.css'); 
$enabled = wfGetConfig('kaltura/enabled');
if ($enabled == null || $enabled == 0 || $enabled == "no" || $enabled == "false") { 

?>
<form method="post" action="<?php $this->html('form.action'); ?>" class="kaltura">
	<p class="title"><?php echo $this->msg('KalturaVideo.form.account.register.title') ?><br/><br/></p>
	<p><?php echo $this->msg('KalturaVideo.form.account.register.text') ?><br/><br/></p>
	<table>
		<tr style="vertical-align:top">
			<td style="width:60%";>
				<table>
					<tr>
						<td colspan="2"><div class="legend"><?php echo $this->msg('KalturaVideo.form.personal'); ?></div></td>
					</tr>
					<tr>
						<td><div class="field"><?php echo $this->msg('KalturaVideo.form.personal.username'); ?>&nbsp;&nbsp;&nbsp;</div></td>
						<td><?php echo DekiForm::singleInput('text', 'kaltura-name'); ?></td>
					</tr>
					<tr>
						<td><div class="field"><?php echo $this->msg('KalturaVideo.form.personal.company'); ?>&nbsp;&nbsp;&nbsp;</div></td>
						<td><?php echo DekiForm::singleInput('text', 'kaltura-company'); ?></td>
					</tr>
					<tr>
						<td><div class="field"><?php echo $this->msg('KalturaVideo.form.personal.email'); ?>&nbsp;&nbsp;&nbsp;</div></td>
						<td><?php echo DekiForm::singleInput('text', 'kaltura-email', $this->get('form.email')); ?></td>
					</tr>
					<tr>
						<td><div class="field"><?php echo $this->msg('KalturaVideo.form.personal.phone'); ?>&nbsp;&nbsp;&nbsp;</div></td>
						<td><?php echo DekiForm::singleInput('text', 'kaltura-phone', $this->get('form.phone')); ?></td>
					</tr>
					<tr>
						<td><div class="field"><?php echo $this->msg('KalturaVideo.form.personal.describe'); ?>&nbsp;&nbsp;&nbsp;</div></td>
						<td>
							<select id="kaltura-describe" name="kaltura-describe">
								<option value="">Please select...</option>
								<option value="Integrator/Web developer" >Integrator/Web developer</option>
								<option value="Ad Agency" >Ad Agency</option>
								<option value="Kaltura Plugin/Extension/Module Distributor" >Kaltura Plugin/Extension/Module Distributor</option>
								<option value="Social Network" >Social Network</option>
								<option value="Personal Site" >Personal Site</option>
								<option value="Corporate Site" >Corporate Site</option>
								<option value="E-Commerce" >E-Commerce</option>
								<option value="E-Learning" >E-Learning</option>
								<option value="Media Company/ Producer" >Media Company/Producer</option>
								<option value="Other" >Other</option>
							</select>
						</td>
					</tr>
					<tr>
						<td colspan="2"><div class="legend"><?php echo $this->msg('KalturaVideo.form.website'); ?></div></td>
					</tr>
					<tr>
						<td><div class="field"><?php echo $this->msg('KalturaVideo.form.website.url'); ?>&nbsp;&nbsp;&nbsp;</div></td>
						<td><?php echo DekiForm::singleInput('text', 'kaltura-url', $this->get('form.smtp.username')); ?></td>
					</tr>
					<tr>
						<td><div class="field"><?php echo $this->msg('KalturaVideo.form.website.content'); ?>&nbsp;&nbsp;&nbsp;</div></td>
						<td>
							<select id="kaltura-content" name="kaltura-content[]" multiple="multiple" size="4">
								<optgroup label="select all that apply">
									<option value="Arts & Literature">Arts & Literature</option>
									<option value="Automotive">Automotive</option>
									<option value="Business">Business</option>
									<option value="Comedy">Comedy</option>
									<option value="Education">Education</option>
									<option value="Entertainment">Entertainment</option>
									<option value="Film & Animation">Film & Animation</option>
									<option value="Gaming">Gaming</option>
									<option value="Howto & Style">Howto & Style</option>
									<option value="Lifestyle">Lifestyle</option>
									<option value="Men">Men</option>
									<option value="Music">Music</option>
									<option value="News & Politics">News & Politics</option>
									<option value="Nonprofits & Activism">Nonprofits & Activism</option>
									<option value="People & Blogs">People & Blogs</option>
									<option value="Pets & Animals">Pets & Animals</option>
									<option value="Science & Technology">Science & Technology</option>
									<option value="Sports">Sports</option>
									<option value="Travel & Events">Travel & Events</option>
									<option value="Women">Women</option>
									<option value="N/A">N/A</option>
								</optgroup>
							</select>
						</td>
					</tr>
					<tr>
						<td><div class="field"><?php echo $this->msg('KalturaVideo.form.website.adult'); ?>&nbsp;&nbsp;&nbsp;</div></td>
						<td>
							<input type="radio" name="kaltura-adult" value="yes" class="checkbox"  /> Yes
							<input type="radio" name="kaltura-adult" value="no" class="checkbox" /> No
						</td>
					</tr>
					<tr>
						<td><div class="field"><?php echo $this->msg('KalturaVideo.form.website.purpose'); ?>&nbsp;&nbsp;&nbsp;</div></td>
						<td><textarea id="kaltura-description" name="kaltura-description"></textarea></div></td>
					</tr>
					<tr>
						<td colspan=2><label for="accept_terms"><input id="accept_terms" name="accept_terms" type="checkbox" class="checkbox" /> <?php echo $this->msg('KalturaVideo.form.registration.terms-of-use'); ?></label><br/><br/>
						<button type="submit" name="submit" id="submit" value="save"><div><?php echo $this->msg('KalturaVideo.form.registration.signup'); ?></div></button>
					</td>
					</tr>
				</table>
			</td>
			<td style="padding-left:30px">
				<?php echo $this->msg('KalturaVideo.form.registration.side'); ?>
			</td>
		</tr>
	</table>
</form>
<?php

} else { /* Kaltura is enabled */
	$kalServer = wfGetConfig('kaltura/server-uri');
	
?>
<div class="title">
	<h3><?php echo $this->msg('KalturaVideo.form.manage.title'); ?></h3>
</div>
<div class="kaltura">
	<p><?php echo $this->msg('KalturaVideo.form.manage.login', $kalServer); ?><br/><br/><?php echo $this->msg('KalturaVideo.form.manage.help'); ?><br/><br/></p>
</div>
<div class="title">
	<h3><?php echo $this->msg('KalturaVideo.form.account.title'); ?></h3>
</div>
<?php

	try {
		$kClient = new KalturaClient(KalturaHelpers::getServiceConfiguration());
		$kalturaUser = KalturaHelpers::getPlatformKey("user","");
		$kalturaSecret = KalturaHelpers::getPlatformKey("secret-admin","");
		$ksId = $kClient -> session -> start($kalturaSecret, KalturaHelpers::getSessionUser()->userId, KalturaSessionType::ADMIN);
		$kClient -> setKs($ksId);

		$kalInfo = $kClient -> partner -> getinfo();
		$kalUsage = $kClient -> partner -> getUsage(date('Y'));

		if (DekiSite::isCommercial()) {
			if ($kalInfo -> partnerPackage > 1) {

?>
<div class="kaltura">
	<p><?php echo $this->msg('KalturaVideo.form.account.general.paying') ?><br/><br/><?php echo $this->msg('KalturaVideo.form.account.paying'); ?></p>
</div>
<?php

			} else { /* not a paying account */
				if ($kalUsage -> Percent >= 100) {

?>
<div class="kaltura">
	<p class="title"><?php echo $this->msg('KalturaVideo.form.account.free.limit-exceeded') ?></p>
	<p class="title"><?php echo $this->msg('KalturaVideo.form.account.free.upgrade-now') ?></p>
	<p><?php 
		echo $this->msg('KalturaVideo.form.account.free.lockout-warning') .  
			'<br/><br/>' .
			$this->msg('KalturaVideo.form.account.general.paying') . 
			'<br/><br/>'  . 
			$this->msg('KalturaVideo.form.account.free.order-now')  . 
			'<br/><br/>';
	?></p>
</div>
<?php

				} else { /* account has not exceeded its limit */

?>
	<div class="kaltura">
		<p><?php 
			echo $this->msg('KalturaVideo.form.account.trial', $kalUsage -> Percent) .  
				'<br/><br/>' . 
				$this->msg('KalturaVideo.form.account.general.free') . 
				'<br/><br/>'  . 
				$this->msg('KalturaVideo.form.account.paying') . 
				'<br/><br/>';
		?> </p>
	</div>
<?php
				} /* if-else account limit check */
			} /* if-else account free vs. paying check */
		} else { /* deki is not commercial */
			if ($kalUsage -> Percent >= 100) {

?>
	<div class="kaltura">
		<p class="title"><?php echo $this->msg('KalturaVideo.form.account.free.limit-exceeded') ?></p>
		<p class="title"><?php echo $this->msg('KalturaVideo.form.account.free.upgrade-now') ?></p>
		<p><?php 
			echo $this->msg('KalturaVideo.form.account.free.lockout-warning') . 
				'<br/><br/>' .
				$this->msg('KalturaVideo.form.account.general.paying') . 
				'<br/><br/>'  . 
				$this->msg('KalturaVideo.form.account.free.order-now')  . 
				'<br/><br/>' .
				$this->msg('KalturaVideo.form.account.free.upgrade-mindtouch');
		?> </p>
	</div>
<?php

			} else { /* account has not exceeded its limit */
			
?>
	<div class="kaltura">
		<p><?php 
			echo $this->msg('KalturaVideo.form.account.trial', $kalUsage -> Percent) . 
				'<br/><br/>' . 
				$this->msg('KalturaVideo.form.account.general.free') . 
				'<br/><br/>'  . 
				$this->msg('KalturaVideo.form.account.paying') . 
				'<br/><br/>' . 
				$this->msg('KalturaVideo.form.account.free.upgrade-mindtouch'); 
		?> </p>
	</div>

<?php

			} /* if-else account limit check */
		} /* if-else deki commercial check */
	} catch (Exception $exp) {
		DekiMessage::error($this->msg('KalturaVideo.form.registration.failure.exception'));
	}
}

?>
