<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="<?php $this->text('lang') ?>" lang="<?php $this->text('lang') ?>" dir="<?php $this->text('dir')?>">
<head>
	<?php 
	//Include CSS, JavaScript and MindTouch Settings.  ## DO NOT REMOVE ##
	require_once('head.php');?>    
</head>

<body class="<?php $this->PageLanguage()?> <?php $this->PageType()?> <?php $this->PageClass()?>">
<?php $this->html('pageheader'); ?>
	<div class="global">
		<div class="globalWrap">
			<div class="site-side">
				<div class="site-mast">
					<?php $this->SiteLogo(); /*--Logo Uploaded to the Control Panel*/?>
				</div>
				<div class="site-auth">
					<?php if($this->UserIsLoggedIn()) {?>
						<span class="user-name"><?php $this->UserUri();?></span>
						<span class="user-logout"><?php $this->UserLogout();?></span>
					<?php } else {?>
						<span class="user-login"><?php $this->UserLogin();?></span>
						<span class="user-register"><?php $this->UserRegister();?></span>
					<?php }?>
				</div>
			<?php $this->SiteNavigation(); /*--Dynamic Left Navigation*/?>
			
			<?php /*START PAGE LANGUAGES*/?>
				<?php if ($this->PageHasLanguages()) :?>
					<h3><?php $this->TitleLanguages();?></h3>
					<?php $this->PageLanguages();?>		
				<?php endif;?>
			<?php /*END PAGE LANGUAGES*/?>
			
			<?php /*START PAGE STATISTICS*/?>
				<?php if ($this->PageHasStatistics()) :?>
					<div class="page-stats">
						<div class="page-meta-data">
							<h3><?php $this->Message('Skin.Common.page-stats');/*--Tags Title*/?></h3>
							<span class="page-views"><?php $this->PageViews(); /*--Related Pages*/?> view(s)</span>
							<span class="page-edits"><?php $this->PageEdits(); /*--Related Pages*/?> edit(s)</span>
							<span class="page-chars"><?php $this->PageChars(); /*--Related Pages*/?> characters(s)</span>
						</div>
					</div>
				<?php endif; ?>
			<?php /*END PAGE STATISTICS*/?>
			
		</div>
		<div class="site-content">
			<?php /*START SITE NAVIGATION */?>
			<div class="site-nav">
				<div class="site-search">
					<?php $this->SiteSearch(array('class' => 'text', 'resetval' => wfMsg("Dialog.Common.search")),array('class' => 'button'));?>
				</div>
				<ul>
					<li><?php $this->UserPage();?></li>
					<?php if ($this->UserIsAdmin()) {?>
						<li><?php $this->SiteControlPanel();?></li>
					<?php }?>
					<li class="recent"><?php $this->SiteRecentChanges();?></li>
					<li><a href="/Special:Reports"><?php echo wfMsg('Skin.Common.reports'); ?></a></li>
					<li class="drop-arrow">
						<ul class="drop-down">
							<li><?php $this->UserWatchedPages();?></li>
							<li><?php $this->UserContributions();?></li>
							<li><?php $this->UserPreferences();?></li>
							<li><?php $this->SiteRSS();?></li>
							<li><?php $this->SiteTemplates();?></li>
							<li><?php $this->SiteMap();?></li>
							<li><?php $this->SitePopular();?></li>
							<li><?php $this->SiteDesktopConnector();?></li>
						</ul>
						<span class="a"><?php $this->Message('Skin.Common.header-tools');?></span>
					</li>
					<li><?php $this->SiteHelp();?></li>
				</ul>
			</div>
			<?php /*END SITE NAVIGATION */?>
			
			<?php /*START PAGE NAVIGATION */?>
			<div class="page-nav">
				<div class="page-revision">
					<span>
						<!--last modified -->
							<?php $this->PageHistory(); /*--Last Modified Line*/?>
						<!--end last modified-->
					</span>
				</div>
				<ul>
					<li class="page-edit"><?php $this->PageEdit();?></li>
					<li class="page-add"><?php $this->PageAdd();?></li>
					<li class="page-print"><?php $this->PagePrint();?></li>
					<li class="drop-arrow">
						<ul class="drop-down">
							<li><?php $this->PageRestrict();?></li>
							<li><?php $this->PageAttach();?></li>
							<li><?php $this->PageMove();?></li>
							<li><?php $this->PageDelete();?></li>
							<li><?php $this->PageAddTag();?></li>
							<li><?php $this->PageEmail();?></li>
							<li><?php $this->PageProperties();?></li>
							<li><?php $this->PageTalk();?></li>
							<li><?php $this->PageWatch();?></li>
						</ul>
						<span class="a"><?php $this->Message('Skin.Common.more');?></span>
					</li>
					
					
				</ul>
			</div>		
			<?php /*END PAGE NAVIGATION */?>
				
				
			<div class="content-frame">
			
				<?php $this->PageBreadcrumbs(); /*--the Page Breadcrumbs*/?>
				
				<?php /*START PAGE TITLE */?>
				<div class="pageTitle">
					<?php $this->PageAlerts();?>
					<?php $this->PageTitle(); /*--Page Title:  <h1>*/?>
				</div>
				<?php /*END PAGE TITLE */?>	
				
				<span class="page-moved">
					<?php $this->PageMoved();?>
				</span>
						
				<?php $this->PageSubNav(); /*--Subnav, used on RSS & Special pages*/?>
						
				<?php $this->PageStatus(); /*--Page Status, Error and Success messages*/?>
				
		 		<?php $this->PageEditor(); /*--Page Content*/?>
		 				
				<?php if ($this->PageIsEditable()) : ?>
					<div class="page-meta-data">
						<?php /*START PAGE TAGS*/?>
							<?php if($this->PageHasTags()) :?>
								<span class="meta-link">
									<?php $this->PageAddTag();?>
								</span>
								<h3><?php $this->Message('Skin.Common.page-tags');/*--Tags Title*/?><span class="header-sub"><?php $this->PageTagsCount();?></span></h3>
								<?php $this->PageTags(); /*--Page Tags*/?>
							<?php endif;?>
						<?php /*END PAGE TAGS*/?>
					</div>
					<div class="page-meta-data">
						<?php /*START PAGE FILES*/?>
							<span class="meta-link">
								<?php $this->PageAttach();?>
							</span>
							<h3><?php $this->Message('Skin.Common.header-files'); ?><span class="header-sub"><?php $this->PageFilesCount();?></span></h3>
				 			<?php $this->PageFiles();	/*--the Uploaded Files list*/?>
						<?php /*END PAGE FILES*/?>
					</div>
					<div class="page-meta-data">
		 				<?php /*START PAGE COMMENTS*/?>
			 				<h3><?php $this->Message('Skin.Common.header-comments');?><span class="header-sub"><?php $this->PageCommentsCount();?></span></h3>
			 				<?php  $this->PageComments();/*--the page comments*/?>
		 				<?php /*END PAGE COMMENTS*/?>
	 				</div>
				<?php endif; ?>
			</div>
		</div>
		</div>
	</div>
	<div class="powered-by">
		<?php $this->PagePowered(); ?>
	</div>		
	
	<?php $this->PageFooter(); /*--Include Page Footer Content*/?>

	</body>
	<?php $this->PageTail(); /*--Required for JS.  ## DO NOT REMOVE ##*/?>  
</html>