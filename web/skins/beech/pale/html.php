<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="<?php $this->text('lang') ?>" lang="<?php $this->text('lang') ?>" dir="<?php $this->text('dir')?>">
	<head>
		<?php 
		//Include CSS, JavaScript and MindTouch Settings.  ## DO NOT REMOVE ##
		require_once('head.php');
		?>
	</head>
	<body>
		<div class="head">
			<h1 class="logo">
				<?php $this->SiteLogo();?>
			</h1>
			<div class="login">
				<div class="login-links">
					<?php if($this->UserIsAnonymous()) :?>
						<a class="login-link" href="#">user login</a>
						<?php $this->UserRegister();?>
					<?php else :?>
						<?php $this->UserUri();?> <?php $this->UserLogout();?>
						<?php $this->UserPreferences();?>
						<?php $this->SiteControlPanel();?>
					<?php endif;?>
				</div>
				<form action="/Special:UserLogin" method="post">
					<div class="row">
						<span class="col">
							<input type="text" name="username" class="reset" resetval="username">
							<span class="title">username</span>
						</span>
						<span class="col">
							<input type="password" name="password">
							<span class="title">password</span>
						</span>
						<span class="col col-btn">
							<input type="submit" value="login" class="btn">
						</span>
					</div>
				</form>
			</div>
		</div>
		<div class="content">
			<ul class="nav">
				<li class="first"><?php $this->SiteRecentChanges();?></li>
				<li><?php $this->SiteRSS();?></li>
				<li><?php $this->SiteMap();?></li>
				<li ><?php $this->SitePopular();?></li>
				<li class="last"><a href="/Special:Reports"><?php echo wfMsg('Skin.Common.reports'); ?></a></li>
			</ul>
			
		<div class="splash">
				<div class="page-tools">
					<span class="quick-more box">
						<span><?php $this->Message('Skin.Common.more');?></span>
						<ul class="dropdown">
							<li><?php $this->PageAttach();?></li>
							<li><?php $this->PageRestrict();?></li>
							<li><?php $this->PageMove();?></li>
							<li><?php $this->PageDelete();?></li>
							<li><?php $this->PageAddTag();?></li>
							<li><?php $this->PageProperties();?></li>
							<li><?php $this->PageWatch();?></li>
						</ul>
					</span>
					
					<?php if (!$this->UserIsAnonymous()) :?>
						<?php $this->PageAlerts();?>
					<?php endif;?>
					
						<span class="quick-pdf box">
							<?php $this->PageSavePdf();?>
						</span>
					
						<span class="quick-print box">
							<?php $this->PagePrint();?>
						</span>
						<span class="quick-email box">
							<?php $this->PageEmail();?>
						</span>
						<span class="quick-add box" >
							<?php $this->PageAdd();?>
						</span>
						<span class="quick-edit box">
							<?php $this->PageEdit();?>
						</span>
				</div>
		</div>
		
		<table class="col-wrap">
			<tr>
				<td class="highlight col">
					<div class="wrap left-navigation">		
						<div class="site-search">
							<?php $this->SiteSearch(array('class' => 'text', 'resetval' => wfMsg("Dialog.Common.search")),array('class' => 'button'));?>
						</div>
							
						<?php $this->SiteNavigation();?>
									
						<?php /*START PAGE TAGS*/?>
							<?php if($this->PageHasTags()) :?>
								<h3><?php $this->Message('Skin.Common.page-tags');/*--Tags Title*/?><span class="header-sub"><?php $this->PageTagsCount();?></span></h3>
								<?php $this->PageTags(); /*--Page Tags*/?>
							<?php endif;?>
						<?php /*END PAGE TAGS*/?>
					</div>
					</td>
				<td class="page col">
					<div class="wrap">
						<?php $this->PageTitle();?>
						
						<?php $this->PageSubNav();?>
						
						<?php $this->PageStatus();?>
						
				 		<?php $this->PageEditor();?>
				 		
				 		<?php if ($this->PageIsEditable()) :?>
				 		<!--last modified -->
							<span class="site-history">
								<?php $this->PageHistory();?>
								<?php if (Skin::isEditPage()) :?> 
									| <a href="?action=history">Revision History</a>
								<?php endif;?>
							</span>
						<!--end last modified-->
						<?php endif;?>
						
				 		<?php $this->PageBreadcrumbs(); /*--the Page Breadcrumbs*/?>
				 		
				 		<?php if ($this->PageIsEditable()) : ?>
					 		<?php /*START PAGE COMMENTS*/?>
				 				<h2 class="meta-header"><?php $this->Message('Skin.Common.header-comments');?><span class="header-sub"><?php $this->PageCommentsCount();?></span></h2>
				 				<?php  $this->PageComments();/*--the page comments*/?>
			 				<?php /*END PAGE COMMENTS*/?>
		 				
		 				
			 				<?php /*START PAGE FILES*/?>
			 					<?php if ($this->PageIsEditable() && !$this->UserIsAnonymous()) :?>
			 					<span class="quick-attach">
					 				<?php $this->PageAttach(null, 'btn');?>
					 			</span>
					 			<?php endif;?>
								<h2 class="meta-header"><?php $this->Message('Skin.Common.header-files'); ?><span class="header-sub"><?php $this->PageFilesCount();?></span></h2>
					 			<?php $this->PageFiles();	/*--the Uploaded Files list*/?>
							<?php /*END PAGE FILES*/?>
						<?php endif; ?>
		 				
					</div>
				</td>
			</tr>
		</table>

		</div>
		<div class="foot">
			<?php $this->PagePowered();?>
		</div>
		
	<?php $this->PageFooter(); /*--Include Page Footer Content*/?>
		
	</body>
	<?php $this->PageTail(); /*--Required for JS.  ## DO NOT REMOVE ##*/?>  
</html>
