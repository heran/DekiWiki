<?php echo '<?xml version="1.0" encoding="UTF-8"?>'; ?>
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
			<div class="site-head">
				<table>
					<tr>
						<td class="site-mast">
							<?php $this->SiteLogo();?>
						</td>
						<td class="site-nav">
							<ul>
								<li><?php $this->UserPage();?></li>
								<?php if ($this->UserIsAdmin()) {?>
									<li><?php $this->SiteControlPanel();?></li>
								<?php }?>
								<li><?php $this->SiteRecentChanges();?></li>
								<li><?php $this->SiteHelp();?></li>
								<li><a href="/Special:Reports"><?php echo wfMsg('Skin.Common.reports'); ?></a></li>
								<li class="drop-arrow">
									<ul class="drop-down">
										<?php if ($this->UserIsLoggedIn()) :?>
										<li><?php $this->UserWatchedPages();?></li>
										<li><?php $this->UserPreferences();?></li>
										<?php endif;?>
										<li><?php $this->UserContributions();?></li>
										<li><?php $this->SiteRSS();?></li>
										<li><?php $this->SiteTemplates();?></li>
										<li><?php $this->SiteMap();?></li>
										<li><?php $this->SitePopular();?></li>
										<li><?php $this->SiteDesktopConnector();?></li>
									</ul>
									<span class="a"><?php $this->Message('Skin.Common.header-tools');?></span>
								</li>
							</ul>
						</td>
						<td class="flex-logo"></td>
					</tr>
				</table>
			</div>
			<table class="content-table">
				<tr>
					<td class="site-side">
						<div class="site-auth">
							<?php if($this->UserIsLoggedIn()) {?>
								<span class="user-name"><?php $this->UserUri();?></span>
								<span class="user-logout"><?php $this->UserLogout();?></span>
							<?php } else {?>
								<span class="user-login"><?php $this->UserLogin();?></span>
								<span class="user-register"><?php $this->UserRegister();?></span>
							<?php }?>
						</div>
						
						<div class="site-search">
							<?php $this->SiteSearch(array('class' => 'text', 'resetval' => wfMsg("Dialog.Common.search")),array('class' => 'button'));?>
						</div>
						
						<?php $this->SiteNavigation(); /*--Dynamic Left Navigation*/?>
					
						<?php if ($this->PageHasLanguages()) :?>
							<h3><?php $this->TitleLanguages();?></h3>
							<?php $this->PageLanguages();?>		
						<?php endif;?>
					
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
					</td>
					<td class="site-content">
						<div class="page-nav">
							<div class="right-bar">
								<?php $this->PageRating();?>
								<?php $this->PageAlerts();?>
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
							
						<div class="content-frame">
						
							<?php $this->PageBreadcrumbs(); /*--the Page Breadcrumbs*/?>
							
							<?php /*START PAGE TITLE */?>
							<div class="pageTitle">
								<?php $this->PageTitle(); /*--Page Title:  <h1>*/?>
							</div>
							<!--last modified -->
								<?php $this->PageHistory(); /*--Last Modified Line*/?>
							<!--end last modified-->
							<?php /*END PAGE TITLE */?>	
							
							<?php $this->PageStatus(); /*--Page Status, Error and Success messages*/?>
							
							<span class="page-moved">
								<?php $this->PageMoved();?>
							</span>
									
							<?php $this->PageSubNav(); /*--Subnav, used on RSS & Special pages*/?>
							
					 		<?php $this->PageEditor(); /*--Page Content*/?>
					 				
							<?php if ($this->PageIsEditable()) : ?>
								<?php if($this->PageHasTags()) :?>
									<div class="page-meta-data">
										<?php /*START PAGE TAGS*/?>
												<span class="meta-link">
													<?php $this->PageAddTag();?>
												</span>
												<h3><?php $this->Message('Skin.Common.page-tags');/*--Tags Title*/?><span class="header-sub"><?php $this->PageTagsCount();?></span></h3>
												<?php $this->PageTags(); /*--Page Tags*/?>
										<?php /*END PAGE TAGS*/?>
									</div>
								<?php endif;?>
								
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
					</td>
				</tr>
			</table>
	</div>
	<div class="powered-by">
		<?php $this->PagePowered(); ?>
	</div>		
	
	<?php $this->PageFooter(); /*--Include Page Footer Content*/?>

	</body>
	<?php $this->PageTail(); /*--Required for JS.  ## DO NOT REMOVE ##*/?>  
</html>