<?php
/**
 * @var $this beechbetaTemplate
 */
?>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="<?php $this->text('lang') ?>" lang="<?php $this->text('lang') ?>" dir="<?php $this->text('dir')?>">
<head>
	<?php 
	//Include CSS, JavaScript and MindTouch Settings.  ## DO NOT REMOVE ##
	require_once('head.php');?>    
</head>

<body class="<?php $this->PageLanguage()?> <?php $this->PageType()?> <?php $this->PageClass()?>">
<?php $this->html('pageheader'); ?>
<table class="header">
	<tr>
		<td class="logo">
			<?php $this->SiteLogo();?>
		</td>
		<td class="right">
			<span class="search-label"><?php $this->Msg('Page.Search.search'); ?></span>
			<div class="search">
				<?php $this->SiteSearch();?>
			</div>
		</td>
	</tr>
</table>

<div class="site-nav">
	<ul>
		<li class="user">
			<?php if($this->UserIsAnonymous()) : ?>
				<?php $this->UserLogin();?>, <?php $this->UserRegister();?>
			<?php else : ?>
				<?php $this->Msg('Skin.Common.logged-in'); ?> <?php $this->UserUri(); ?>, <?php $this->UserLogout(); ?>
			<?php endif; ?>
		</li>
		<li><?php $this->UserPage();?></li>
		<li><?php $this->SiteRecentChanges();?></li>
		<li>
			<span class="tools a">
				<?php $this->Message('Skin.Common.header-tools');?>
			</span>
			<ul class="dropdown">
				<li class="watched"><?php $this->UserWatchedPages();?></li>
				<li class="contributions"><?php $this->UserContributions();?></li>
				<li class="preferences"><?php $this->UserPreferences();?></li>
				<li class="split"></li>
				<li class="controlpanel"><?php $this->SiteControlPanel();?></li>
				<li class="rss"><?php $this->SiteRSS();?></li>
				<li class="templates"><?php $this->SiteTemplates();?></li>
				<li class="split"></li>
				<li class="sitemap"><?php $this->SiteMap();?></li>
				<li class="popular"><?php $this->SitePopular();?></li>
				<li class="desktopconnector"><?php $this->SiteDesktopConnector();?></li>
			</ul>
		</li>
		<li><?php $this->SiteHelp();?></li>
	</ul>
</div>

<table class="content">
	<tr>
		<td class="highlight">
			<div class="navigation">
				<?php $this->SiteNavigation();?>
			</div>
		</td>
		<td class="body">
			<div class="page-nav">
				<div class="info">
					<div class="history">
						<!--last modified -->
						<?php $this->PageHistory(); /*--Last Modified Line*/?>
						<!--end last modified-->
					</div>
					<ul>
						<li class="filecount"><a href="#pageFiles"><?php $this->PageFilesCount()?></a></li>
						<?php if($this->PageHasToc()) :?>
						<li class="toc">
							<span class="a"><?php $this->Message('Skin.Common.table-of-contents');?></span>
							<div class="dropdown">
								 <?php $this->PageToc();?>
							</div>
						</li>
						<?php endif;?>
						<li class="notifications">
							<?php $this->PageAlerts(); ?>
						</li>
						<?php // TODO: Fix up this area?>
						<li class="restrict"><?php $this->PageRestrict();?></li>
						<li class="watchedpages"><?php $this->UserWatchedPages();?></li>
					</ul>
				</div>
				<ul class="edit-bar">
					<li class="edit"><?php $this->PageEdit();?></li>
					<li class="new"><?php $this->PageAdd();?></li>
					<li class="print"><?php $this->PagePrint();?></li>
					<li class="more">
						<span class="a"><?php $this->Message('Skin.Common.more');?></span>
						<ul class="dropdown">
							<li class="watchtoggle"><?php $this->PageWatch();?></li>
							<li class="attach"><?php $this->PageAttach();?></li>
							<li class="restrict"><?php $this->PageRestrict();?></li>
							<li class="move"><?php $this->PageMove();?></li>
							<li class="delete"><?php $this->PageDelete();?></li>
							<li class="tag"><?php $this->PageAddTag();?></li>
							<li class="email"><?php $this->PageEmail();?></li>
							<li class="properties"><?php $this->PageProperties();?></li>
							<li class="talk"><?php $this->PageTalk();?></li>
						</ul>
					</li>
				</ul>
			</div>
			
			<div class="text-frame">
				<?php $this->PageBreadcrumbs();?>
				
				<?php $this->PageTitle();?>
				
				<?php $this->PageSubNav(); /*--Subnav, used on RSS & Special pages*/?>
					
				<?php $this->PageStatus(); /*--Page Status, Error and Success messages*/?>
				
		 		<?php $this->PageEditor(); /*--Page Content*/?>
		 				
			</div>

			<?php if ($this->PageIsEditable()) : ?>
				<div class="meta">
				
					<h2 class="files">
						<span class="tab">
							<span class="icon"><?php $this->Message('Skin.Common.header-files'); ?> (<?php $this->PageFilesCount()?>)</span>
						</span>
					</h2>
		 			<?php $this->PageFiles();?>
					
	 				<h2 class="comments">
	 					<span class="tab">
	 						<span class="icon"><?php $this->Message('Skin.Common.header-comments');?> (<?php $this->PageCommentsCount()?>)</span>
	 					</span>
	 				</h2>
	 				<?php  $this->PageComments();?>
 				</div>
			<?php endif; ?>
		</td>
	</tr>
	<tr class="footer">
		<td class="left">&nbsp;</td>
		<td class="right">
			<?php $this->html('poweredbytext'); ?>
		</td>
	</tr>
</table>
	
	<?php $this->PageFooter(); /*--Include Page Footer Content*/?>

	</body>
	<?php $this->PageTail(); /*--Required for JS.  ## DO NOT REMOVE ##*/?>  
</html>
