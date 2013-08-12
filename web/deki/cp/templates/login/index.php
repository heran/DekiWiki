<?php
/**
 * Build the flash message markup
 */
// TODO: move to view object?
$hasFlash = DekiMessage::hasFlash();
$hasResponse = DekiMessage::hasApiResponse();
if ($hasFlash || $hasResponse)
{
	$html = '';
	$html .= '<div class="dekiFlash">';
	if ($hasFlash)
	{
		$html .= DekiMessage::fetchFlash();
	}
	if ($hasResponse)
	{
		// hide/show for api responses
		$javascript = "Deki.$('#apierror').find('div.response').toggle().end().find('span').toggle(); return false;";

		$html .= '<div id="apierror" class="apierror' . ($hasFlash ? ' witherror' : '') . '">';
			$html .= '<a href="#" onclick="'. $javascript .'">';
				$html .= '<span class="expand">' . $this->msg('Common.error.expand') . '</span>';
				$html .= '<span class="contract">' . $this->msg('Common.error.contract') . '</span>';
			$html .= '</a>';
			$html .= '<div class="response">';
				$html .= DekiMessage::fetchApiResponse();
			$html .= '</div>';
		$html .= '</div>';
	}
	$html .= '</div>';

	$this->set('template.flash', $html);
}
$this->includeCss('login.css'); 
$this->includeJavascript('login.js');
?>

<!-- todo: consolidate this in the main template -->
<div class="return">
	<a href="/"><?php echo($this->msg('Common.tpl.return', DekiSite::getName()));?></a>
</div>

<?php $this->html('template.flash'); ?>

<div class="login-logo"><span><?php echo $this->msg('Login.form.title'); ?></span></div>

<div class="login">
	<form method="post">
		<div class="block">
			<?php echo $this->msg('Login.form.username'); ?><br />
			<?php echo DekiForm::singleInput('text', 'username'); ?>
		</div>
		<div class="block">
			<?php echo $this->msg('Login.form.password'); ?><br />
			<?php echo DekiForm::singleInput('password', 'password'); ?>
		</div>
	
	
		<?php if ($this->has('authOptions')) : ?>
		<div class="block">
			<?php echo $this->msg('Login.form.authentication'); ?><br />
			<div class="block">
				<?php echo DekiForm::multipleInput('select', 'auth_id', $this->get('authOptions'), $this->get('defaultAuthId')); ?>
			</div>
		</div>
		<?php else : ?>
			<?php echo DekiForm::singleInput('hidden', 'auth_id', $this->get('defaultAuthId')); ?>
		<?php endif; ?>
		
		<?php echo DekiForm::singleInput('hidden', 'returnurl', $this->get('returnurl')); ?>
		
		<div class="submit">
			<?php echo DekiForm::singleInput('button', 'submit', 'login', array(), $this->msg('Login.form.submit')); ?>
		</div>
	</form>
</div>
