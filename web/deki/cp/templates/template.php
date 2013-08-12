<?php
// encapsulates basic template functionality
class DekiTemplate
{
	/*
	 * This array determines what group a controller/page falls under
	 */	
	public static $menuItems = array(
		'dashboard' => array(
			'dashboard' => array('index'), 
		), 
		'users' => array(
			'user_management' => array('listing', 'deactivated', 'add', 'add_multiple'), 
			'group_management' => array('listing', 'add'), 
			'role_management' => array(), 
			'bans' => array('listing', 'add')
		), 
		'custom' => array(
			'skinning' => array(), 
			'custom_css' => array(), 
			'custom_html' => array()
		), 
		'maint' => array(
			'package_importer' => array(), 
			'page_restore' => array(), 
			'file_restore' => array(), 
			'cache_management' => array(), 
		), 
		'settings' => array(
			'product_activation' => array(),
			'configuration' => array('settings', 'listing'), 
			'extensions' => array('listing', 'add_script', 'add'), 
			'authentication' => array('listing', 'add'), 
			'email_settings' => array(), 
			'analytics' => array(), 
			'editor_config' => array(), 
			'kaltura_video' => array()
		)
	);
		
	// given a page name, will try to find its group
	static function getGroup($items, $name = null) 
	{
		static $flatGroups = array();
		
		// flatten the array for an easier lookup
		if (empty($flatGroups)) 
		{
			foreach ($items as $group => $section) 
			{
				foreach ($section as $pages => $junk) 
				{
					$flatGroups[$pages] = $group;
				}
			}
		}
		
		return $flatGroups[$name];
	}
	
	static function menu($items, $curpage = null) 
	{
		$Request = DekiRequest::getInstance();
		$html = '<ul>';
		foreach ($items as $section => $pages) 
		{
			// for the headers, if they have pages, then use the first page as the link
			$html.= '<li><a href="'.$Request->getLocalUrl(!empty($pages) ? key($pages): $section).'">'.DekiView::msg('Common.title.'.$section).'</a>';
			if (!empty($pages) && $section != 'dashboard') //dashboard is a special case where we just show the header, and not the link
			{
				$html.= '<ul>';
				foreach ($pages as $page => $tabs) 
				{
					$html.= '<li><a href="'.$Request->getLocalUrl($page).'"'.($page == $curpage ? ' class="selected"': '').'>'.DekiView::msg('Common.title.'.$page).'</a></li>';
				}	
				$html.= '</ul>';
			}
			$html.= '</li>';
		}
		$html.= '</ul>';
		return $html;
	}
	
	static function tabs($menuItems, $curpage = null) 
	{
		$Request = DekiRequest::getInstance();
		
		$html = '';
		$tabs = $menuItems[DekiTemplate::getGroup($menuItems, $curpage)][$curpage];
		
		$html = '<ul>';
		if (!empty($tabs)) 
		{
			$cur_param = $Request->getVal('params');
			
			// if a parameter it not set, pick the default one from the beginning of the list
			if (is_null($cur_param)) 
			{
				$cur_param = current($tabs);
			}
			foreach ($tabs as $param) 
			{
				$isCurrent = $cur_param == $param || strpos($cur_param, $param.'/') !== false; /* Primary driven by the auth preloading URls; add/{blah} */
				$html .= 
				'<li>'
					.'<a href="'. $Request->getLocalUrl($curpage, $param, array('page'), true) .'"'.($isCurrent ? ' class="selected"': '').'>'
						.DekiView::msg('Common.title.'.$curpage.'.'.$param)
					.'</a>'
				.'</li>';
			}
		}
		else 
		{
			$html.= '<li><a href="'.$Request->getLocalUrl($curpage).'" class="selected">'.DekiView::msg('Common.title.'.$curpage).'</a></li>';
		}
		$html.= '</ul>';
		return $html;
	}
	
	/**
	 * @return string - html comments
	 */
	static public function reportApiTime()
	{
		global $wgPlugProfile, $wgProfileApi;
		
		$report = isset($wgProfileApi) && $wgProfileApi === true && isset($wgPlugProfile);
		if (!$report)
		{
			return '';
		}
	
		$total_time = 0;
		$optimized_time = 0;
		
		$output = '';
		$logged = array();
		foreach ($wgPlugProfile as $val) 
		{
			$uri = parse_url($val['url']);
			$verb = isset($val['verb']) ? '('. $val['verb'] .')' : '';
			if (!in_array($uri, $logged))
			{
				$optimized_time = $optimized_time + $val['diff'];
				$logged[] = $uri;
			}
			
			$output .= '<!-- '. $verb . $uri['path'] .': '. $val['diff'] .' -->'."\n";
			$total_time = $total_time + $val['diff'];
		}
	
		$output .= "<!-- Total: ".$total_time."-->\n";
		$output .= "<!-- Real: ".$optimized_time."-->\n";

		return $output;
	}
	
	function getHelpUrl($cur_page) 
	{
		$Request = DekiRequest::getInstance();
		$anchor = !is_null($Request->getVal('params')) ? '#' . $Request->getVal('params'): '';
		$key = DekiTemplate::getGroup(self::$menuItems, $cur_page) . '/' . $cur_page;
		
		if (array_key_exists($key . $anchor, Config::$DEKI_CP_HELP_LINKS))
		{
			return ProductURL::HELP_CP.'/'.Config::$DEKI_CP_HELP_LINKS[$key . $anchor];
		}
		
		// try one level up, without anchor
		if (array_key_exists($key, Config::$DEKI_CP_HELP_LINKS))
		{
			return ProductURL::HELP_CP.'/'.Config::$DEKI_CP_HELP_LINKS[$key];
		}
		return ProductURL::HELP_CP;
	}
	
	function getLicenseStatusText() 
	{
		$Request = DekiRequest::getInstance(); 
		
		$html = '';
		if (DekiSite::isCore()) 
		{
			$html.= '<div class="dekistatus community"><p><a href="'.ProductURL::COMMERCIAL.'" target="_blank">'.DekiView::msg('Common.status.community').'</a></p></div>';
		}
		
		$days = DekiSite::willExpire(); 
		if ($days > 0 && !DekiSite::isDeactivated()) 
		{
			$html.= '<div class="dekistatus expirywarn"><p><a href="'.ProductURL::ACTIVATION.'" target="_blank">'.DekiView::msg('Common.status.willexpire', $days).'</a></p></div>';
		}
		
		if (DekiSite::isExpired() || DekiSite::isInvalid())
		{
			$html.= '<div class="dekistatus invalid"><p><a href="'.ProductURL::ACTIVATION.'" target="_blank">'.DekiView::msg('Common.status.expired').'</a></p></div>';
		}
		elseif (DekiSite::isInactive())
		{
			$html.= '<div class="dekistatus invalid"><p><a href="'.$Request->getLocalUrl('product_activation').'">'.DekiView::msg('Common.status.inactive').'</a></p></div>';
		}
		return $html;
	}
	
	/**
	 * Call out to plugins for additional head includes
	 */
	public function outputPluginHead($Template = null)
	{
		$html = "";
		$result = DekiPlugin::executeHook('ControlPanel:Template:RenderHead', array(&$html, $Template));
		if ($result == DekiPlugin::HANDLED) {
			echo $html;
			return true;
		}
		return false;
	}
	
	/**
	 * Check if a plugin wants to render it's own header for the control panel
	 * @return boolean
	 */
	public function outputPluginHeader($Template = null) 
	{
		$html = "";
		$result = DekiPlugin::executeHook('ControlPanel:Template:RenderHeader', array(&$html, $Template));
		if ($result == DekiPlugin::HANDLED) {
			echo $html;
			return true;
		}
		return false;
	}

	/**
	 * Check if a plugin wants to render it's own navigation for the control panel
	 * @return boolean
	 */
	public function outputPluginMenu($Template = null) 
	{
		$html = "";
		$result = DekiPlugin::executeHook('ControlPanel:Template:RenderMenu', array(&$html, $Template));
		if ($result == DekiPlugin::HANDLED) {
			echo $html;
			return true;
		}
		return false;
	}

	/**
	 * Check if a plugin wants to render it's own tabs for the control panel
	 * @return boolean
	 */
	public function outputPluginTabs($Template = null) 
	{
		$html = "";
		$result = DekiPlugin::executeHook('ControlPanel:Template:RenderTabs', array(&$html, $Template));
		if ($result == DekiPlugin::HANDLED) {
			echo $html;
			return true;
		}
		return false;
	}
	
	public function outputTitle($Template = null, $pageTitle = false)
	{
		$html = "";
		$result = DekiPlugin::executeHook('ControlPanel:Template:RenderTitle', array(&$html, $Template, $pageTitle));
		if ($result == DekiPlugin::HANDLED) {
			echo $html;
			return true;
		}
		return false;
	}
	
	/**
	 * Check if a plugin wants to add a footer. This is used to add
	 * scripts that should load at the end.
	 * @return boolean
	 */
	public function outputPluginFooter($Template = null)
	{
		$html = "";
		$result = DekiPlugin::executeHook('ControlPanel:Template:RenderFooter', array(&$html, $Template));
		if ($result == DekiPlugin::HANDLED) {
			echo $html;
			return true;
		}
		return false;
	}
}

?>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">

<head>
	<title><?php
		echo $this->msg('Common.title', DekiSite::getName());
		
		if ($this->has('template.title'))
		{
			// view has specified a title for the page
			echo ' - ' . $this->html('template.title');
		}
		else if (!DekiTemplate::outputTitle($this))
		{
			// autogenerate a title
			$section = $this->get('controller.name');
			$group = DekiTemplate::getGroup(DekiTemplate::$menuItems, $section);
			$page = $this->get('controller.action');
			
			$groupTitle = $this->msg('Common.title.'.$group);
			echo ' - ' . $groupTitle;
			
			$sectionPages = DekiTemplate::$menuItems[$group][$section];
			if (in_array($page, $sectionPages))
			{
				echo ' - ' . $this->msg('Common.title.'.$section.'.'.$page);
			}
			else
			{
				echo ' - ' . $this->msg('Common.title.'.$section);
			}
		}
	?></title>
	<meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
	<link rel="stylesheet" type="text/css" href="./assets/reset.css" />
	<link rel="stylesheet" type="text/css" href="./assets/common.css" />
	<?php foreach ($this->getCssIncludes() as $cssfile) { ?>
		<link rel="stylesheet" type="text/css" href="./assets/<?php echo $cssfile; ?>" />
	<?php } ?>
	    
	<!--[if IE 6]>
	<link rel="stylesheet" type="text/css" href="./assets/ie6.css" />
	<![endif]-->
	
	<!--[if IE 7]>
	<link rel="stylesheet" type="text/css" href="./assets/ie7.css" />
	<![endif]-->

    
	<script type="text/javascript" src="/skins/common/jquery/jquery.min.js"></script>
	<script type="text/javascript" src="/skins/common/jquery/jquery.plugins.js"></script>
	<script type="text/javascript" src="/skins/common/deki.js"></script>
	<script type="text/javascript" src="./assets/common.js"></script>
	<script type="text/javascript" src="./assets/jquery.textarearesizer.compressed.js"></script>
	<?php foreach ($this->getJavascriptIncludes() as $jsfile) { ?>
		<script type="text/javascript" src="./assets/<?php echo $jsfile; ?>"></script>
	<?php } ?>
	<?php DekiTemplate::outputPluginHead($this); ?>
</head>
<body id="<?php echo($this->get('controller.name'));?>">
<?php if (!DekiTemplate::outputPluginHeader($this)) : ?>
<div class="top-bar">
	<?php $licenseMessage = DekiTemplate::getLicenseStatusText(); ?>
	<div class="wrap <?php echo empty($licenseMessage) ? 'commercial' : ''; ?>">
		<div class="return">
			<p><a href="/"><?php echo($this->msg('Common.tpl.return', DekiSite::getName()));?></a></p>
		</div>
		
		<?php echo $licenseMessage; ?>
		
		<div class="welcome">
			<p><span class="welcome"><?php echo($this->msg('Common.tpl.welcome', DekiUser::getCurrent()->getName()));?></span> <a href="/Special:Userlogout"><?php echo($this->msg('Common.tpl.logout'));?></a></p>
		</div>
	</div>
</div>
<?php endif; ?>

<?php $Request = DekiRequest::getInstance(); ?>

<div class="wrap">
	<div class="header">
		<div class="logo">
			<a href="./">
				<img id="logo" src="./assets/images/logo.gif" alt="<?php echo($this->msg('Common.cp'));?>" />
			</a>
		</div>
		<div class="page-title">
			<?php if (!DekiTemplate::outputTitle($this, true)) : ?>
				<?php echo $this->msg('Common.title.'.DekiTemplate::getGroup(DekiTemplate::$menuItems, $this->get('controller.name'))); ?>
			<?php endif; ?>
		</div>
		<div class="help">
			<a href="<?php echo ProductURL::FAQ;?>" target="_blank"><?php echo($this->msg('Common.tpl.faq'));?></a>
			|
			<a href="<?php echo DekiTemplate::getHelpUrl($this->get('controller.name')); ?>" target="_blank"><?php echo($this->msg('Common.tpl.help'));?></a>
		</div>
		<div class="clear"></div>
	</div>
	
	<div class="navigation">
		<?php if (!DekiTemplate::outputPluginMenu($this)) : ?>
			<?php echo DekiTemplate::menu(DekiTemplate::$menuItems, $this->get('controller.name')); ?>
		<?php endif; ?>
	</div>
	
	<div class="main">
	
		<!-- page specific -->
		<?php if ($this->has('template.search.action')): ?>
		<form method="get" action="<?php echo $this->get('template.search.action'); ?>" class="search">
			<div class="search">
				<?php
					echo DekiForm::singleInput(
						'text',
						'query',
						$this->get('template.search.query'),
						array('class' => 'search', 'title' => $this->get('template.search.title'))
					);
					echo DekiForm::singleInput('button', 'submit', 'submit');
					echo DekiForm::singleInput('hidden', 'params', 'listing');
				?>
			</div>
		</form>
		<?php endif; ?>
		
		<div class="tabs">
			<?php if (!DekiTemplate::outputPluginTabs($this)) : ?>
				<?php echo DekiTemplate::tabs(DekiTemplate::$menuItems, $this->get('controller.name')); ?>
			<?php endif; ?>
			<div class="clear"></div>
		</div>
		
		<?php $hasTemplateActions = $this->has('template.actions'); ?>
				
		<?php if ($hasTemplateActions && count($this->get('template.actions') > 0) && is_array($this->get('template.actions'))): ?>
			<form action="<?php echo $this->get('template.action.form'); ?>" method="post">
			<div class="actions">
				<ul>
					<li class="title"><?php echo $this->msg('Common.tpl.selected'); ?></li>
					<?php foreach ($this->get('template.actions') as $name => $msg): ?>
					<?php echo '<li>' . DekiForm::singleInput('button', 'action', $name, array('class' => $name), $msg) . '</li>'; ?>
					<?php endforeach; ?>
				</ul>
				<div class="clear"></div>
			</div>
		<?php endif; ?>
		
		<?php if ($this->has('template.subtitle')): ?>
		<div class="subtitle">
			<?php echo $this->get('template.subtitle'); ?>
		</div>
		<?php endif; ?>
		
		<div class="content">
			<?php
				$hasFlash = DekiMessage::hasFlash();
				$hasResponse = DekiMessage::hasApiResponse();
				if ($hasFlash || $hasResponse) :
			?>
			<div class="dekiFlash">
				<?php if ($hasFlash) : ?>
					<?php echo DekiMessage::fetchFlash(); ?>
				<?php endif; ?>
		
				<?php if ($hasResponse) : ?>
					<div id="apierror" class="<?php echo 'apierror' . ($hasFlash ? ' witherror' : ''); ?>">
						<a href="#" onclick="Deki.$('#apierror').find('div.response').toggle().end().find('span').toggle(); return false;">
							<span class="expand"><?php echo($this->msg('Common.error.expand'));?></span>
							<span class="contract"><?php echo($this->msg('Common.error.contract'));?></span>
						</a>
						<div class="response">
							<?php echo DekiMessage::fetchApiResponse(); ?>
						</div>
					</div>
				<?php endif; ?>
			</div>
			<?php endif; ?>
			
			<?php $this->html('view.contents'); ?>
			
			<?php if ($hasTemplateActions && count($this->get('template.actions') > 0) && is_array($this->get('template.actions'))): ?>
			</form>
			<?php endif; ?>
			
			<div class="br"></div>
		</div>
	</div>
	<div class="footer"><?php echo $this->msgRaw('Product.powered', DekiSite::getProductLink()); ?></div>
</div>
<div class="clear"></div>
<?php echo DekiTemplate::reportApiTime(); ?>
<?php DekiTemplate::outputPluginFooter(); ?>
</body>
</html>
