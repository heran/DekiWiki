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

if (defined('MINDTOUCH_DEKI')) :

require_once('includes/SkinTemplate.php');

// Inherit main code from SkinTemplate, set the CSS and template filter.
class Skinbeech extends SkinTemplate
{
	const TEMPLATE_NAME = 'beech';
	
	function initPage(&$out) 
	{
		global $wgBeechTemplate, $wgActiveSkin;
		
		SkinTemplate::initPage($out);
		$this->skinname  = 'beech';
		$this->stylename = 'beech';
		
		if (isset($wgBeechTemplate))
		{
			require_once('skins/'. Skinbeech::TEMPLATE_NAME .'/'. $wgActiveSkin .'/template.php');
			$this->template = $wgBeechTemplate;
		}
		else
		{
			$this->template = 'beechTemplate';
		}
	}
}

class beechTemplate extends QuickTemplate
{
	public function execute()
	{
		global $wgActiveSkin;

		// Allow variable overrides
		DekiPlugin::executeHook(Hooks::SKIN_OVERRIDE_VARIABLES, array(&$this));
		
		// Include the HTML Markup
		require_once('skins/'. Skinbeech::TEMPLATE_NAME .'/'. $wgActiveSkin .'/html.php');  
	}
	
	// GLOBAL FUNCTIONS
	public function MsgLinkControl($linkHref, $linkText, $linkAttr = array(), $linkDisabled = false)
	{
		$message =  wfMsg($linkText);
		if (strncmp($message, "[MISSING:", 9) != 0)
		{
			$linkText = $message;
		}
		$this->LinkControl($linkHref, $linkText, $linkAttr, $linkDisabled);
	}

	/**
	 * Responsible for outputting localized links and managing text, classes and disabled status
	 * 
	 * @param string $url - skin template key or valid href
	 * @param string $key - The attribute key for the different link attributes.  Available in SkinTemplate.php, available for limited functions
	 * @param string $languageKey - The localization key for the outputted text
	 * @param string $linkText - The text value for the outputted hyperlink, superior to $languageKey
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @param boolean $linkDisabled- A boolean for disabling or enabling the specified link
	 * @return
	 */
	protected function LinkControl($linkHref, $linkText, $linkAttr = array(), $linkDisabled = false) 
	{
		$text = htmlspecialchars($linkText); 

		$attr = array(
			'title' => $text,
			'class' => ''
		);
		
		// Backward compatible
		if (!is_array($linkAttr))
		{
			$linkAttr = array('class' => $linkAttr);
		}
		
		// automagical href detection
		$href = $this->haveHref($linkHref);
		if (!empty($href))
		{
			$attr['href'] = $href;
			$attr['onclick'] = $this->haveOnClick($linkHref);
			$attr['class'] .= ' ' . $this->haveCssClass($linkHref);
		}
		else if (!$linkDisabled)
		{
			$attr['href'] = $linkHref;
		}
		
		$class = (isset($linkAttr['class']) ? $linkAttr['class'] . ' ' : '') . $attr['class'];
		// combine attributes
		$attr = array_merge($attr, $linkAttr);
		// special handling for the class attribute
		$attr['class'] = $class;
		
		if ($linkDisabled)
		{
			$attr['class'] .= ' disabled';
		}
		
		// output the link
		echo '<a';
		foreach ($attr as $name => $value)
		{
			echo ' ' . $name . '="' . htmlspecialchars($value) . '"';
		}
		echo '>' . $text . '</a>' . "\n";
	}
	
			
	// SITE FUNCTIONS
	
	/**
	 * Outputs a hyperlink for administrators to access the control panel
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function SiteControlPanel($linkText = null, $linkClass = null) 
	{
		global $wgUser;
		if ($wgUser->canViewControlPanel()) 
		{ 
			if (is_null($linkText))
			{
				$linkText = 'Admin.ControlPanel.page-title';
			}
			$href = $wgUser->getSkin()->makeAdminUrl('');
			$this->MsgLinkControl($href, $linkText, $linkClass);
		}	
	}

	/**
	 * Outputs a hyperlink that links to the Deskttop Connect page at www.MindTouch.com
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function SiteDesktopConnector($linkText = null, $linkClass = null) 
	{
		$href = 'http://www.mindtouch.com/Products/Desktop_Suite';
		if (is_null($linkText))
		{
			$linkText = 'Skin.Common.desktop-suite';
		}
		$this->MsgLinkControl($href, $linkText, $linkClass);
	}
	
	/**
	 * Outputs a hyperlink that links to the predefined or user defined help url.  The help url can be manually altered in the Control Panel by the administrator
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function SiteHelp($linkText = null, $linkClass = null) 
	{
		global $wgArticle, $wgHelpUrl;
		if (is_null($linkText))
		{
			$linkText = 'Skin.Common.header-help';
		}
		$this->MsgLinkControl($wgHelpUrl, $linkText, $linkClass);
	}
	
	/**
	 * Outputs the formatted site logo.  The site logo can be customized in the admin control panel
	 * @param string $adminClass - A string of classes that will be added to the logo if the user is an admin
	 * @param string $userClass - A string of classes that will be added to the logo if the user is a standard user
	 * @param string $anonClass - A string of classes that will be added to the logo if the user is an anonymous user
	 * @return
	 */
	protected function SiteLogo() 
	{
		global $wgSitename, $wgUser;
		$siteUrl = $wgUser->getSkin()->makeUrl('');
		$siteName = htmlspecialchars($wgSitename);
		$class = '';
		
		if ($wgUser->isAnonymous()) 
		{
			$class = htmlspecialchars('logo-anonymous');
		}
		else if (!$wgUser->canViewControlPanel()  && !$wgUser->isAnonymous()) 
		{
			$class = htmlspecialchars('logo-user');
		}
		else if ($wgUser->canViewControlPanel()) 
		{
			$class = htmlspecialchars('logo-admin');
		}

		echo '<a'. ($class ? ' class="' . $class . '" ' : '') .' href="'. $siteUrl .'" title="'. $siteName .'">'.
				'<img src="'. wfGetSiteLogo() .'" alt="'. $siteName .'" title="'. $siteName .'"/>'.
			'</a>';
	}
	
	/**
	 * Outputs a hyperlink that links the 'Sitemap' for your MindTouch
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function SiteMap($linkText = null, $linkClass = null) 
	{
		global $wgUser;
		$href = $wgUser->getSkin()->makeSpecialUrl('Sitemap');
		if (is_null($linkText))
		{
			$linkText = 'Page.SiteMap.page-title';
		}
		$this->MsgLinkControl($href, $linkText, $linkClass);
	}
	
	/**
	 * Outputs the name of your site in plain text
	 * @return 
	 */
	protected function SiteName() 
	{
		global $wgSitename;
		echo htmlspecialchars($wgSitename);
	}
	
	/**
	 * Outputs the formatted 'left side' navigation in its entirety
	 * @return
	 */
	protected function SiteNavigation()
	{
		$this->html('sitenavtext');	
	}
	
	/**
	 * Outputs a hyperlink that links to the 'Popular pages' page
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function SitePopular($linkText = null, $linkClass = null) 
	{
		global $wgUser;
		$href = $wgUser->getSkin()->makeSpecialUrl('Popularpages');
		if (is_null($linkText))
		{
			$linkText = 'Page.Popularpages.page-title';
		}
		$this->MsgLinkControl($href, $linkText, $linkClass);
	}
	
	/**
	 * Outputs a hyperlink that links the 'Recent Changes' for your MindTouch
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function SiteRecentChanges($linkText = null, $linkClass = null) 
	{	
		global $wgUser;
		$href = $wgUser->getSkin()->makeSpecialUrl('Recentchanges');
		if (is_null($linkText))
		{
			$linkText = 'Page.RecentChanges.page-title';
		}
		$this->MsgLinkControl($href, $linkText, $linkClass);
	}
	
	/**
	 * Outputs a hyperlink that links to the RSS feed for the site
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function SiteRSS($linkText = null, $linkClass = null) 
	{
		global $wgUser;
		$href = $wgUser->getSkin()->makeSpecialUrl('ListRss');
		if (is_null($linkText))
		{
			$linkText = 'Page.ListRss.page-title';
		}
		$this->MsgLinkControl($href, $linkText, $linkClass);
	}
	
	/**
	 * Outputs a search textbox and search button
	 * @param array $textParams - html attributes for the textbox element
	 * @param array $buttonParams - html attributes for the button element
	 * @return
	 */
	protected function SiteSearch($textAttrs = array(), $buttonAttrs = array(), $namespaceAttrs = array()) 
	{
		global $wgRequest;
	?>	
		<form action="<?php $this->text('searchaction') ?>">
			<input 
			 	name="search" 
				type="text" 
				value="<?php $this->text('search'); ?>" 
				<?php
					foreach ($textAttrs as $key => $value) 
					{
						echo $key . '="' . htmlspecialchars($value) . '" ';
					}
				?>
			/>
			<input 
				type="submit" 
				<?php
					foreach ($buttonAttrs as $key => $value) 
					{
						echo $key . '="' . htmlspecialchars($value) . '" ';
					}
					if (!isset($buttonAttrs['value']) || empty($buttonAttrs['value'])) 
					{
						echo ' value="' . wfMsg('Dialog.Common.search') . '"';
					}
				?>
			/>
			<?php
				if ($this->hasData('search.namespaces')) 
				{
					$this->html('search.namespaces');
				}
			?>
		</form>
	<?php
	}
	
	/**
	 * Outputs a hyperlink that links the 'Templates' for your MindTouch
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function SiteTemplates($linkText = null, $linkClass = null) 
	{
		global $wgUser;
		$sk = $wgUser->getSkin();
		$href = $sk->makeNSUrl('', '', NS_TEMPLATE);
		if (is_null($linkText))
		{
			$linkText = 'Page.ListTemplates.page-title';
		}
		$this->MsgLinkControl($href, $linkText, $linkClass);
	}
	
	
	/**
	 * Identifies a area of the markup that can have dynamic MindTouch templates embedded.  Called "Template Targeting"
	 * Requires addition to LocalSettings.php,  $wgTargetSkinVars = array_merge($wgTargetSkinVars, array('target id'));
	 * @return
	 */
	protected function SiteTemplateTarget($id = null) 
	{
		global $wgTitle;
		if ($id)
		{
			$this->html($id);
		}
	}
	
	// PAGE FUNCTIONS
	
	/**
	 * Outputs a hyperlink that is responsible for creating a new subpage
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @param string $template - The name of a MindTouch template to be inserted in the newly create page
	 * @return
	 */
	protected function PageAdd($linkText = null, $linkClass = null, $templatePath = null) 
	{
		// TODO:  Add $templateParams
		global $wgArticle, $wgTitle;
		$href = 'pageadd';
		if (is_null($linkText))
		{
			$linkText = 'Skin.Common.new-page';
		}
 		if (!is_null($templatePath))
 		{
	 		$canCreate = ($wgArticle->userCanCreate() && !Skin::isNewPage() && !Skin::isEditPage());
 			$href = $wgTitle->getFullUrl('action=addsubpage&template=' . $templatePath); 
 			$this->MsgLinkControl($href, $linkText, $linkClass, !$canCreate);
		}
		else
		{
			$this->MsgLinkControl($href, $linkText, $linkClass);
		}
	}
	
	/**
	 * Outputs a hyperlink that opens up the 'Tag Page' dialog
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function PageAddTag($linkText = null, $linkClass = null) 
	{
		global $wgArticle;
		if (is_null($linkText))
		{
			$linkText = 'Skin.Common.tags-page';
		}
		$this->MsgLinkControl('pagetags', $linkText, $linkClass);
	}
	
	/**
	 * Outputs the 'Page Alerts' link and dialog for users to receive page notifications
	 * @return
	 */
	protected function PageAlerts()
	{
		if ($this->hasData('page.alerts') && !Skin::isNewPage() && !Skin::isEditPage())
		{
			$this->html('page.alerts');
		}
	}
	
	/**
	 * Outputs a hyperlink that opens up the Attach File dialog
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function PageAttach($linkText = null, $linkClass = null)
	{
		global $wgArticle;
		if (is_null($linkText))
		{
			$linkText = 'Skin.Common.attach-file';
		}
		$this->MsgLinkControl('pageattach', $linkText, $linkClass);
	}
	
	/**
	 * Outputs the 'Page Breadcrumbs', a visual representation of a pages hierarchical location.  I.E. MindTouch > Engineering > Projects > Skinning > Beech 
	 * @return
	 */
	protected function PageBreadcrumbs()
	{
		global $wgTitle;
	
		if ($this->hasData('hierarchy') && $wgTitle->isEditable() && Skin::isViewPage())
		{
			$this->html('hierarchy');
		}
	}
	
	/**
	 * Outputs an number of characters in the content of the current page.
	 * @return 
	 */
	protected function PageChars()
	{
		echo $this->haveData('pagecharcount');
	}

	/**
	 * Outputs a prefixed classname for the current page based on the page name.  Useful to identify specific pages with CSS selectors. (PageDW-partners)
	 * @return
	 */
	protected function PageClass()
	{
		global $wgTitle;
		if ($wgTitle->isHomepage())
		{
			echo "homepage";	
		}
		else
		{
			echo $wgTitle->getClassNameText();
		}
	}
	
	/**
	 * Outputs the page comments markup including:  Add a new comment form, Edit / Delete existing comments
	 * @return 
	 */
	protected function PageComments() 
	{
		$this->html('comments');	
	}
	
	/**
	 * Outputs the number of comments on the page. 
	 * @return
	 */
	protected function PageCommentsCount() 
	{
		echo $this->haveData('commentcount');	
	}
	
	/**
	 * Outputs a hyperlink that links to the 'Email page link' page
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function PageDelete($linkText = null, $linkClass = null) 
	{
		global $wgArticle;
		if (is_null($linkText))
		{
			$linkText = 'Skin.Common.delete-page';
		}
		$this->MsgLinkControl('pagedelete', $linkText, $linkClass);
	}
	
	/**
	 * Outputs a hyperlink that initiates the 'Edit page' action
	 * @return
	 */
	protected function PageEdit($linkText = null, $linkClass = null) 
	{
		if (is_null($linkText))
		{
			$linkText = 'Skin.Common.edit-page';
		}
		global $wgArticle;
		$this->MsgLinkControl('pageedit', $linkText, $linkClass);
	}
	
	/**
	 * Outputs the date of the last edit
	 * @return
	 */
	protected function PageEditDate() 
	{
		global $wgLang, $wgArticle;
		$timestamp = $wgArticle->getTimestamp();
		$formattedts = $wgLang->timeanddate($timestamp, true);
		echo $formattedts;
	}
	
	/**
	 * Outputs the page content and all of the elements required for the editor to work.
	 * @return
	 */
	protected function PageEditor() 
	{
		$this->html('bodytext');
	}
	
	/**
	 * Outputs the number of edits to the current page.
	 * @return 
	 */
	protected function PageEdits() 
	{
		echo $this->haveData('pagerevisions');
	}
	
	/**
	 * Outputs a hyperlink that links to the 'Email page link' page
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function PageEmail($linkText = null, $linkClass = null) 
	{
		global $wgArticle;
		if (is_null($linkText))
		{
			$linkText = 'Skin.Common.email-page';
		}
		$this->MsgLinkControl('pageemail', $linkText, $linkClass);
	}
	
	/**
	 * Outputs the entire markup for listing files that are attached to the current page along with menus to:  Edit description, Move files, Delete files
	 * @return 
	 */
	protected function PageFiles() 
	{
		$this->html('filestext');
	}
	
	/**
	 * Outputs the number of files on the page. 
	 * @return
	 */
	protected function PageFilesCount() 
	{
		echo $this->haveData('filecount');
	}
	
	/**
	 * Outputs required elements for popup dialogs.
	 * @return 
	 */
	protected function PageFooter() 
	{
		global $wgOut;
		echo MTMessage::output() . Skin::getAnalytics() . "\n" . $wgOut->reportTime() . $wgOut->reportApiTime();
	}
	
	/**
	 * Outputs the image gallery and all of the associated the markup
	 * @return 
	 */
	protected function PageGallery()
	{
		$this->html('gallerytext');
	}
	
	/**
	 * Boolean, returns true if the page has files attached to it.  Returns false if the page has no files.
	 * @return boolean - returns true if the page has files attached to it
	 */
	protected function PageHasFiles() 
	{
		$filecount = $this->haveData('filecount');
		return !empty($filecount) && ($filecount > 0);
	}
	
	/**
	 * Boolean, returns true if the page is available in multiple languages.  Returns false if the page is only available in one language.
	 * @return boolean - returns true if the page is available in multiple languages
	 */
	protected function PageHasLanguages() 
	{
		global $wgTitle;
		return $this->hasData('languages') && $wgTitle->isEditable();
	}
	
	/**
	 * This function is being removed
	 * @return
	 */
	protected function PageHasRelated()
	{
		if ($this->hasData('related'))
		{ 
			$this->html('related');
		}
	}
	
	/**
	 * Boolean, returns true if the page has a page view count, character count and edit count
	 * @return boolean - returns true if the page has a page view count, character count and edit count
	 */
	protected function PageHasStatistics() 
	{
		global $wgTitle;
		return $wgTitle->isEditable();
	}
	
	/**
	 * Boolean, returns true if the page has one or more tags.  Returns false if the page has no tags.
	 * @return boolean - returns true if the page has one or more tags
	 */
	protected function PageHasTags()
	{
		$tagcount = $this->haveData('tagcount');
		return !empty($tagcount) && ($tagcount > 0);
	}
	
	/**
	 * Boolean, returns true if the page data for a table of contents.  Returns false if the page has no table of contents
	 * @return boolean - returns true if the page data for a table of contents
	 */
	protected function PageHasToc()
	{
		global $wgOut;
		$toc = $wgOut->getTarget('toc');
		return !(empty($toc) || strcmp(strip_tags($toc), wfMsg('System.API.no-headers')) == 0);
	}
	
	/**
	 * Outputs a localized string that also links to the Revision History of a page.  I.E. Page last modified 08:31, 13 Jan 2010 by Admin
	 * @return
	 */
	protected function PageHistory() 
	{
		if (!Skin::isNewPage() && !Skin::isEditPage() && !Skin::isSpecialPage()) 
		{
			echo $this->haveData('pagemodified');
		}
	}
	
	/**
	 * Outputs the current page ID
	 * @return
	 */
	protected function PageId() 
	{
		global $wgArticle, $wgTitle;
		if ($wgTitle->canEditNamespace())
		{ 
			echo $wgArticle->getId();
		}
	}
	
	/**
	 * Boolean, returns true if the page is in the /User:Draft namespace.
	 * @return boolean - returns true if the page is in the /User:Draft namespace.
	 */
	protected function PageIsDraft() 
	{
		return (bool)$this->haveData('mindtouch_drafts.isDraft');
	}
	
	/**
	 * Boolean, returns true if the page is editable.  Would return false on /special: pages.
	 * @return boolean - true if the page is editable
	 */
	protected function PageIsEditable() 
	{
		global $wgTitle;
		return ( $wgTitle->canEditNamespace() && Skin::isViewPage() && !Skin::isSpecialPage() );
	}
	
	/**
	 * Boolean, returns true if the page is the homepage.
	 * @return boolean - returns true if the page is the homepage.
	 */
	protected function PageIsHome() 
	{
		global $wgTitle;
		return $wgTitle->isHomepage();
	}
	
	/**
	 * Boolean, returns true if the page is in the /Special: namespace
	 * @return boolean - returns true if the page is in the /Special: namespace
	 */
	protected function PageIsSpecial() 
	{
		return Skin::isSpecialPage();
	}
	
	/**
	 * Boolean, returns true if the page is a staged draft
	 * @return boolean - returns true if the page is a staged draft
	 */
	protected function PageIsStagedDraft() 
	{
		return (bool)$this->haveData('mindtouch_drafts.isStaged');
	}
	
	/**
	 * Boolean, returns true if the page is in the /Talk: namespace
	 * @return boolean - returns true if the page is in the /Talk: namespace
	 */
	protected function PageIsTalk() 
	{
		return Skin::isTalkPage();
	}
	
	/**
	 * Boolean, returns true if the page is in the /Template: namespace
	 * @return boolean - returns true if the page is in the /Template: namespace
	 */
	protected function PageIsTemplate() 
	{
		global $wgTitle;
		$namespace = $wgTitle->getNamespace();
		return $namespace == NS_TEMPLATE;
	}
	
	/**
	 * Checks if the current page is in the User: namespace
	 * @return boolean
	 */
	protected function PageIsUser() 
	{
		global $wgTitle;
		return $wgTitle->getNamespace() == NS_USER;
	}
	
	/**
	 * Checks if the current page is in the active user's hierarchy
	 * @return boolean
	 */
	protected function PageIsMine() 
	{
		global $wgTitle;
		return $wgTitle->isMyUserPage();
	}
	
	/**
	 * Checks if the current page is a user homepage => subpage of User:
	 * @return boolean
	 */
	protected function PageIsUserHomepage()
	{
		global $wgTitle;
		if (!$this->PageIsUser())
		{
			return false;
		}

		$segments = explode('/', ltrim($wgTitle->getLocalUrl(), '/'));
		return count($segments) == 1;
	}
	
	/**
	 * Outputs the language code of the current page.  (en-us)
	 * @return
	 */
	protected function PageLanguage() 
	{
		$this->html('language');
	}
	
	/**
	 * Outputs an unordered list of the languages that the current page is available in.
	 * @return
	 */
	protected function PageLanguages() 
	{
		global $wgTitle;
		if ($this->hasData('languages') && $wgTitle->isEditable()) 
		{
			$this->html('languages');
		}	
	}
	
	/**
	 * Outputs the hyperlink to return to the main namespace of a page
	 * @return 
	 */
	protected function PageMain($linkText = null, $linkClass = null) 
	{
		// TODO:  Add exception for /user_talk: pages
		global $wgArticle, $wgTitle;
		if (is_null($linkText))
		{
			$linkText = 'Skin.Common.view';
		}
		$mt = Title::newFromText($wgTitle->getText(), NS_MAIN);
		$href = $mt->getLocalUrl();
		$linkDisabled = ($wgArticle->isViewPage() || Skin::isSpecialPage());
		$this->MsgLinkControl($href, $linkText, $linkClass, $linkDisabled);
	}
	
	/**
	 * Outputs a hyperlink that opens up the 'Move Page' dialog
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function PageMove($linkText = null, $linkClass = null) 
	{
		global $wgArticle;
		if (is_null($linkText))
		{
			$linkText = 'Skin.Common.move-page';
		}
		$this->MsgLinkControl('pagemove', $linkText, $linkClass);
	}
	
	/**
	 * Outputs a localized hyperlink that links to the original location where the page was moved from
	 * @return
	 */
	protected function PageMoved() 
	{
		if ($this->haveData('pageismoved') && !Skin::isNewPage() && !Skin::isEditPage()) 
		{ 
			echo('<a class="pageMoved" href="' . $this->haveData('pagemovelocation') . '">' . $this->haveData('pagemovemessage') . '</a>'); 
		}
	}
	
	/**
	 * Outputs the Name of the page (not the title).  The Name is the filename as determined in the uri.
	 * @return 
	 */
	protected function PageName() 
	{
		global $wgTitle;
		echo $wgTitle->getPrefixedText();
	}
	
	/**
	 * Outputs a localized hyperlink identifying your current MindTouch version
	 * @return 
	 */
	protected function PagePowered() 
	{
		$this->html('poweredbytext');
	}
	
	/**
	 * Outputs a hyperlink that opens up the Print
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function PagePrint($linkText = null, $linkClass = null) 
	{
		if (is_null($linkText))
		{
			$linkText = 'Skin.Common.print-page';
		}
		$this->MsgLinkControl('pageprint', $linkText, $linkClass);
	}
	
	/**
	 * Outputs a hyperlink that links to the 'Page Properties' page
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function PageProperties($linkText = null, $linkClass = null) 
	{
		global $wgArticle;
		if (is_null($linkText))
		{
			$linkText = 'Skin.Common.page-properties';
		}
		$this->MsgLinkControl('pageproperties', $linkText, $linkClass);
	}
	
	/**
	 * Outputs the thumbsup/thumbsdown content rating functionality with the current content rating status
	 * @return
	 */
	protected function PageRating() 
	{
		if ($this->hasData('page.rating'))
		{
			$this->html('page.rating.header');
		}
	}
	
	/**
	 * Outputs the simple thumbsup/thumbsdown content rating functionality
	 * @return
	 */
	protected function PageRatingButtons() 
	{
		if ($this->hasData('page.rating'))
		{
			$this->html('page.rating');
		}
	}
	
	/**
	 * Outputs a hyperlink that links to the Restrict Access page
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function PageRestrict($linkText = null, $linkClass = null) 
	{
		global $wgArticle;
		if (is_null($linkText))
		{
			$linkText = 'Skin.Common.restrict-access';
		}
		$this->MsgLinkControl('pagerestrict', $linkText, $linkClass);
	}
	
	/**
	 * This Function returns the type of restriction of a page
	 * @return 
	 */
	protected function PageRestriction()
	{
		return $this->haveData('pageisrestricted');
	}
	
	/**
	 * This Function ouputs a localized hyperlink that links to the revision history page
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return 
	 */
	protected function PageRevisionHistory($linkText = null, $linkClass = null)
	{
		global $wgTitle;
		if (is_null($linkText))
		{
			$linkText = 'Skin.Common.history';
		}
		$href = $wgTitle->getLocalUrl('action=history');
		$linkDisabled = !($wgTitle->canEditNamespace() && Skin::isViewPage() && !Skin::isSpecialPage());
		$this->MsgLinkControl($href, $linkText, $linkClass, $linkDisabled);
	}
	
	/**
	 * Outputs a hyperlink to the RSS feed for the current page's changes
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	*/
	protected function PageRss($linkText = null, $linkClass = null)
	{
		// TODO:  Add Format type for rss feed (all, daily, raw, rawdaily);
		global $wgArticle;
		if (is_null($linkText))
		{
			$linkText = 'Page.ListRss.page-title';
		}
		$pageId = $wgArticle->getId();
		$href = "/@api/deki/pages/" . $pageId . "/feed";
		$canSubscribe = ($wgArticle->userCan('SUBSCRIBE') && $pageId > 0);
		$this->MsgLinkControl($href, $linkText, $linkClass, !$canSubscribe);
	}
	
	/**
	 * Outputs a hyperlink to the RSS feed for a page's comments
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	*/
	protected function PageRssComments($linkText = null, $linkClass = null)
	{
		global $wgArticle;
		if (is_null($linkText))
		{
			$linkText = 'Skin.Common.header-comments';
		}
		$pageId = $wgArticle->getId();
		$href = "/@api/deki/pages/" . $pageId . "/comments?format=atom";
		$canSubscribe = ($wgArticle->userCan('SUBSCRIBE') && $pageId > 0);
		$this->MsgLinkControl($href, $linkText, $linkClass, !$canSubscribe);
	}
	
	/**
	 * Outputs a hyperlink that saves a page to PDF
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function PageSavePdf($linkText = null, $linkClass = null) 
	{
		global $wgArticle;
		if (is_null($linkText))
		{
			$linkText = 'Skin.Common.header-comments';
		}
		$href = $wgArticle->getAlternateContent('application/pdf');
		$linkDisabled = empty($href);
		$this->MsgLinkControl($href, $linkText, $linkClass, $linkDisabled);
	}
	
	/**
	 * Outputs the status messages (error & success) for the current page.
	 * @return
	 */
	protected function PageStatus() 
	{
		echo wfMessagePrint();
	}
	
	/**
	 * Outputs the '/Special:' page sub navigation which can be seen on the Contributions page and others.
	 * @return
	 */
	protected function PageSubNav() 
	{
		$this->html('pagesubnav');
	}
	
	/**
	 * Outputs the list of tags for a given page.  Each tag links to the Tags pages that shows all pages with the same tag
	 * @param string $delimiter - A string that will be used to separated the outputted hyperlinks, defaults to a comma
	 * @return
	 */
	protected function PageTags($delimiter = null, $plainText = null)
	{
		// TODO:  Add $order param (ascending, descending), add a $htmlwrap parameter to wrap ('<li></li>')
		global $wgArticle;
		$titleId = $wgArticle->getId();
		$articleTags = $wgArticle->getTags();

		$tags = array();
		foreach ($articleTags as $tag)
		{
			$tags[] = DekiTag::newFromArray($tag);
		}
		DekiTag::sort($tags);

		// Define the delimiter
		if (is_null($delimiter))
		{
			$delimiter = ', ';
		}

		$list = array();
		$html = '';
		if (!empty($tags))
		{
			// Loop through each tag...
			foreach ($tags as $Tag)
			{
				// Store the tag
				$uri = $Tag->getUri();
				if (!empty($uri))
				{
					if ($plainText)
					{
						$list[] = $Tag->toHtml();
					}
					else
					{
						$list[] = '<a href="' . $uri . '" class="tag-' . $Tag->toHtml() . '" title="' . $Tag->toHtml() . '">' . $Tag->toHtml() . '</a>';
					}
				}
				else
				{
					$list[] = $Tag->toHtml();
				}
				
			}
			$html .= implode(htmlspecialchars($delimiter), $list);
		}

		$disabled = '';
		if (!$wgArticle->userCanTag())
		{
			$disabled = 'class="disabled" onclick="return false;"';
		}

		echo $html;
	}
	
	/**
	 * Outputs the number of tags on the page. 
	 * @return
	 */
	protected function PageTagsCount() 
	{
		echo $this->haveData('tagcount');
	}
	
	/**
	 * Outputs the Advanced Tagging interface that includes the ajax interface for adding/editing/deleting tags
	 * @return 
	 */
	protected function PageTagsEditor()
	{
		$this->html('tagstext');
	}
	
	/**
	 * Outputs necessary trailing JavaScript.
	 * @return 
	 */
	protected function PageTail() 
	{
		$this->html('customtail');
	}
	
	/**
	 * Outputs a hyperlink that links to the 'Talk'.  The Talk page is a designated mirror page intended for discussions
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function PageTalk($linkText = null, $linkClass = null) 
	{
		global $wgArticle;
		if (is_null($linkText))
		{
			$linkText = 'Skin.Common.page-talk';
		}
		$this->MsgLinkControl('pagetalk', $linkText, $linkClass);
	}
	
	/**
	 * Outputs the PageTitle with standard formatting which includes an <h1> wrapper with classes and an ID.
	 * @return
	 */
	protected function PageTitle($linkClass = null) 
	{
		if (!Skin::isNewPage() && !Skin::isEditPage()) 
		{
			$classes = array('first');
			if ($linkClass) 
			{
				$classes[] = htmlspecialchars($linkClass);
			}
			
			if ($this->haveData('pageisrestricted')) 
			{
				$classes[] = 'page-restricted';
			}
			
			$classes = implode(' ', $classes); 
			
			echo '<h1 class="' . $classes . '" id="title">';
			if ($this->hasData('page.title'))
			{
				// title editor is available
				$this->html('page.title');
			}
			else
			{
				$this->text('displaypagetitle');
			}
			echo '</h1>';
		}
	}
	
	/**
	 * Outputs the PageTitle in plaintext with no markup or formatting
	 * @return
	 */
	protected function PageTitlePlain() 
	{
		$this->text('displaypagetitle');
	}
	
	/**
	 * Outputs the Table Of Contents for a given page
	 * @return
	 */
	protected function PageToc() 
	{
		global $wgOut;
		echo $wgOut->getTarget('toc');
	}
	
	/**
	 * Outputs a string of descriptive types for each page.  Useful in the class of the body tag.  (page-home user-loggedin user-admin yui-skin-sam)
	 * @return
	 */
	protected function PageType() 
	{
		$this->html('pagetype');
	}
	
	/**
	 * Outputs the display name of the last editor/author
	 * @return
	 */
	protected function PageUserDisplayName() 
	{
		global $wgArticle;
		$User = $wgArticle->getDekiUser();
		echo $User ? htmlspecialchars($User->getFullname()) : wfMsg('Dialog.AttachFlash.Unknown');
	}
	
	/**
	 * Outputs the username of the last editor/author
	 * @return
	 */
	protected function PageUserName() 
	{
		global $wgArticle;
		$User = $wgArticle->getDekiUser();
		echo $User ? htmlspecialchars($User->getUsername()) : wfMsg('Dialog.AttachFlash.Unknown');
	}
	
	/**
	 * Outputs the number of page views for the current page
	 * @return 
	 */
	protected function PageViews() 
	{
		echo $this->haveData('pageviews');
	}
	
	/**
	 * Outputs a hyperlink that saves a page to PDF
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function PageViewSource($linkText = null, $linkClass = null) 
	{
		global $wgArticle;
		if (is_null($linkText))
		{
			$linkText = 'Skin.Common.view-page-source';
		}
		$this->MsgLinkControl('pagesource', $linkText, $linkClass);
	}
	
	/**
	 * Outputs a hyperlink that toggles the 'Watch Page' and 'Unwatch Page' options
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function PageWatch($watchText = null, $unwatchText = null, $linkClass = null) 
	{
		global $wgTitle;
		$linkText = $watchText;
		if (is_null($linkText))
		{
			$linkText = 'Skin.Common.watch-page';
		}
		if ($wgTitle->userIsWatching()) 
		{ 	
			$linkText = $unwatchText;
			if (is_null($linkText))
			{
				$linkText = 'Skin.Common.unwatch-page';
			}
		}
		$this->MsgLinkControl('pagewatch', $linkText, $linkClass);
	}
	
	// USER FUNCTIONS
	
	/**
	 * Boolean, returns true if the user is an Administrator
	 * @return boolean - returns true if the user is an Administrator
	 */
	protected function UserIsAdmin() 
	{
		global $wgUser;
		return $wgUser->isAdmin();
	}
	
	/**
	 * Boolean, returns true if the user is logged out.  Returns false if the user is logged in.
	 * @return boolean - returns true if the user is logged out
	 */
	protected function UserIsAnonymous() 
	{
		global $wgUser;
		return $wgUser->isAnonymous();
	}
	
	/**
	 * Boolean, returns true if the user is logged in.  Returns false if the user is logged out.
	 * @return boolean - returns true if the user is logged in
	 */
	protected function UserIsLoggedIn() 
	{
		global $wgUser;
		return !$wgUser->isAnonymous();
	}
	
	/**
	 * Boolean, returns true if the user is can edit the current page
	 * @return boolean - returns true if the user can edit the current page. 
	 */
	protected function UserCanEdit() 
	{
		global $wgArticle;
		return $wgArticle->userCanEdit();
	}
	
	/**
	 * Boolean, returns true if the user has the necessary permissions to create a draft
	 * @return boolean - returns true if the user can create a draft. 
	 */
	protected function UserCanCreateDraft() 
	{
		return (bool)$this->haveData('mindtouch_drafts.canCreate'); 
	}
	
	/**
	 * Outputs a localized hyperlink that links to the 'My Contributions' page for the current user.
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function UserContributions($linkText = null, $linkClass = null) 
	{
		global $wgUser;
		if (is_null($linkText))
		{
			$linkText = 'Page.Contributions.page-title';
		}
		$href = $wgUser->getSkin()->makeSpecialUrl('Contributions', 'target=' . $wgUser->getUsername());
		$linkDisabled = $wgUser->isAnonymous();
		$this->MsgLinkControl($href, $linkText, $linkClass, $linkDisabled);
	}
	
	/**
	 * Outputs the current users displayy name (can be anonymous) 
	 * @return
	 */
	protected function UserDisplayName() 
	{
		global $wgUser;
		echo htmlspecialchars($wgUser->getFullname());
	}
	
	/**
	 * Outputs a localized hyperlink that links to the 'User Login' page
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @return
	 */
	protected function UserLogin($linkText = null, $linkClass = null) 
	{
		global $wgUser;
		if (is_null($linkText))
		{
			$linkText = 'Page.UserLogin.page-title';
		}
		$href = $this->haveData('loginurl');
		$this->MsgLinkControl($href, $linkText, $linkClass);
	}
	
	/**
	 * Outputs a localized hyperlink that logs OUT a user.
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @return
	 */
	protected function UserLogout($linkText = null, $linkClass = null) 
	{
		global $wgUser;
		if (is_null($linkText))
		{
			$linkText = 'Page.UserLogout.page-title';
		}
		$href = $this->haveData('logouturl');
		$this->MsgLinkControl($href, $linkText, $linkClass);
	}
	
	/**
	 * Outputs the current users name (not the display name) 
	 * @return
	 */
	protected function UserName() 
	{
		global $wgUser;
		echo htmlspecialchars($wgUser->getUsername());
	}
	
	/**
	 * Outputs a localized hyperlink that links to /User: namespace for the current user.
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function UserPage($linkText = null, $linkClass = null) 
	{
		global $wgUser;
		if (is_null($linkText))
		{
			$linkText = 'Skin.Common.header-my-page';
		}
		$href = $this->haveData('userpageurl');
		$this->MsgLinkControl($href, $linkText, $linkClass);
	}
	
	/**
	 * Outputs a localized hyperlink that links to the 'My Preferences' page
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function UserPreferences($linkText = null, $linkClass = null) 
	{
		global $wgUser;
		if (is_null($linkText))
		{
			$linkText = 'Page.UserPreferences.page-title';
		}
		$linkDisabled = $wgUser->isAnonymous();
		$href = $wgUser->getSkin()->makeSpecialUrl('Preferences');
		$this->MsgLinkControl($href, $linkText, $linkClass, $linkDisabled);
	}
	
	/**
	 * Outputs a localized hyperlink that links to the 'User Registration' page
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @return
	 */
	protected function UserRegister($linkText = null, $linkClass = null) 
	{
		global $wgUser;
		if (is_null($linkText))
		{
			$linkText = 'Page.UserRegistration.page-title';
		}
		$linkDisabled = !$wgUser->isAnonymous();
		$href = $this->haveData('registerurl');
		$this->MsgLinkControl($href, $linkText, $linkClass, $linkDisabled);
		
	}
	
	/**
	 * Outputs the URI of the currently authenticated user's page
	 * @return
	 */
	protected function UserUri($linkText = null, $linkClass = null, $querystring = null) 
	{
		global $wgUser;
		$href = $this->haveData('userpageurl') . $querystring;
		$linkText = $linkText
			? $linkText
			: $wgUser->toHtml();
		$this->MsgLinkControl($href, $linkText, $linkClass);
	}
	
	/**
	 * Outputs a localized hyperlink that links to the 'My Watched Pages' page
	 * @param string $linkText - Text value for the outputted hyperlink
	 * @param string $linkClass - A string of classes to be appended to the system assigned classes
	 * @return
	 */
	protected function UserWatchedPages($linkText = null, $linkClass = null) 
	{
		global $wgUser;
		if (is_null($linkText))
		{
			$linkText = 'Page.WatchedPages.page-title';
		}
		$href = $wgUser->getSkin()->makeSpecialUrl('Watchedpages');
		$this->MsgLinkControl($href, $linkText, $linkClass);
	}
	
	
	// MESSAGE FUNCTIONS
	
	/**
	 * Outputs the localized string for 'Comments'. 
	 * @return
	 */
	protected function Message($key) 
	{
		echo wfMsg($key);
	}
	
	/*
	Examples of Message Keys - full documentation available at /var/www/dekiwiki/resources/resources.txt
	
		'Comments' 			-> Skin.Common.header-comments 
		'Files' 			-> Skin.Common.header-files
		'Languages' 		-> Article.Common.languages
		'More' 				-> Skin.Common.more
		'Statistics' 		-> Skin.Common.page-stats
		'Tags' 				-> Skin.Common.page-tags
		'Table of Contents' -> Skin.Common.table-of-contents
		'Tools'				-> Skin.Common.header-tools
	*/

	/**
	 * Helpers
	 */
	function haveHref($str)
	{
		$sk = $this->data['skin'];
		return isset($sk->href->$str) ? $sk->href->$str : null;
	}
}
endif;
