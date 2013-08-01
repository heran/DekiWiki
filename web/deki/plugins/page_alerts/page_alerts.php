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

define("PAGE_ALERTS", true);

class DekiPageAlertsPlugin extends SpecialPagePlugin
{
	const AJAX_FORMATTER = 'pagealerts';

	/**
	 * Triggered before page alert status has been set
	 *
	 * @param Article $Article - Page
	 * @param DekiUser $User - Current user
	 * @param string $currentStatus - Current page alert status
	 * @param string &$status - Page alert status to set
	 */
	const HOOK_PRE_SET_STATUS = 'PageAlert:PreSetStatus';

	/**
	 * Triggered after page alert set status has been requested
	 *
	 * @param Article $Article - Page
	 * @param DekiUser $User - Current user
	 * @param string $status - Page alert status to set
	 * @param bool $statusChange - Did status change
	 */
	const HOOK_POST_SET_STATUS = 'PageAlert:PostSetStatus';
	
	const SPECIAL_PAGE = 'PageAlerts';

	protected $pageName = self::SPECIAL_PAGE;
	protected $specialFolder = '';

	public static function init()
	{
		DekiPlugin::registerHook(Hooks::SPECIAL_PAGE . self::SPECIAL_PAGE, array(__CLASS__, 'specialHook'));
		DekiPlugin::registerHook(Hooks::SKIN_OVERRIDE_VARIABLES, array(__CLASS__, 'skinHook'));
		DekiPlugin::registerHook(Hooks::AJAX_FORMAT . self::AJAX_FORMATTER, array(__CLASS__, 'ajaxHook'));
	}

	/**
	 * specialHook
	 *
	 * @param string $pageName
	 * @param string &$pageTitle
	 * @param string &$html
	 */
	public static function specialHook($pageName, &$pageTitle, &$html)
	{
		$Special = new DekiPageAlertsPlugin($pageName, basename(__FILE__, '.php'));
		$pageTitle = $Special->getPageTitle();
		$html = $Special->output();
	}

	/**
	 * skinHook
	 *
	 * @param Template &$Template
	 */
	public static function skinHook(&$Template)
	{
		global $wgArticle, $wgTitle, $wgUser;
		
		// page alerts: views pages, talk pages, and logged in users with subscribe
		// doesn't make sense for the anon user to subscribe to alerts
		if (($wgArticle->isViewPage() || $wgTitle->isTalkPage()) && !$wgTitle->isTemplateHomepage())
		{
			$enablePageAlerts = !$wgUser->isAnonymous() && $wgUser->canSubscribe() && ($wgArticle->getId() > 0);
			$Template->set('page.alerts', self::getPageAlertsButton($wgArticle, $enablePageAlerts));
		}
	}

	/**
	 * ajaxHook
	 *
	 * @param string &$body
	 * @param string &$message
	 * @param bool &$success
	 */
	public static function ajaxHook(&$body, &$message, &$success)
	{
		$Request = DekiRequest::getInstance();
		$action = $Request->getVal('action');
		$pageId = $Request->getVal('pageId');

		$Article = Article::newFromId($pageId);
		
		// default to failure
		$success = false;

		// could not find article, or title does not exist
		if ( is_null($Article) || is_null($Article->getTitle()) )
		{
			return array(
				'success' => false,
				'message' => 'Invalid page specified'
			);
		}

		switch ($action)
		{
			default:
			case 'setstatus':
				$status = $Request->getVal('status');
				extract(self::setPageAlertsStatus($Article, $status));
				break;
		}
	}

	/**
	 * getPageAlertsButton
	 *
	 * Generates the markup for page alerts
	 * 
	 * @param Article $Article
	 * @param bool $enabled
	 * @return string html
	 */
	public static function getPageAlertsButton($Article, $enabled = true)
	{
		$Title = Title::newFromText(self::SPECIAL_PAGE, NS_SPECIAL);
		$specialUrl = ($Article->getId()) ? $Title->getLocalURL('id=' . $Article->getId()) : '#';

		if (!$enabled)
		{
			// disabled markup
			$html = 
			'<div id="deki-page-alerts" class="disabled">
				<div class="toggle">
					<a href="javascript:void(0);" class="off">
						<span>'. wfMsg('Page.PageAlerts.page-title') .'</span>
						<span class="status">' .
							wfMsg('Page.PageAlerts.status.off') .
						'</span>
					</a>
				</div>'.
			'</div>';
		}
		else
		{	
			// build the markup for the alerts area
			$status = $Article->getAlertStatus();
			$isSubscribed = ($status != DekiPageAlert::STATUS_OFF);
	
			$toggleHtml = '
			<div class="toggle">
				<a href="'. $specialUrl .'" class="'. ($isSubscribed ? '' : 'off') .'">
					<span>'. wfMsg('Page.PageAlerts.page-title') .'</span>
					<span class="status">' .
						($isSubscribed ? wfMsg('Page.PageAlerts.status.on') : wfMsg('Page.PageAlerts.status.off')) .
					'</span>
				</a>
			</div>';
			
			if ($status == DekiPageAlert::STATUS_PARENT)
			{
				$parentId = $Article->getAlertParentId();
				$Parent = Title::newFromId($parentId);
				// fail gracefully if the paretnt title cannot be found
				if (!is_null($Parent))
				{
					// special case, show link to parent
					$optionsHtml = 
					'<li class="parent">' .
						wfMsg(
							'Page.PageAlerts.notice.parent',
							'<a href="'. $Parent->getLocalUrl() .'">'. htmlspecialchars($Parent->getDisplayText()) .'</a>'
						) .
					'</li>';
				}
			}
			else
			{		
				$optionsHtml = '
				<li class="self">
					<input type="radio" name="alert" id="deki-page-alerts-self" '.
					($status == DekiPageAlert::STATUS_SELF ? 'checked="checked"' : '') .
					'value="'. DekiPageAlert::STATUS_SELF .'" />
					<label for="deki-page-alerts-self">'. wfMsg('Page.PageAlerts.form.self') .'</label>
				</li>
				<li class="tree">
					<input type="radio" name="alert" id="deki-page-alerts-tree" '.
					($status == DekiPageAlert::STATUS_TREE ? 'checked="checked"' : '') .
					'value="'. DekiPageAlert::STATUS_TREE .'" />
					<label for="deki-page-alerts-tree">'. wfMsg('Page.PageAlerts.form.tree') .'</label>
				</li>
				<li class="off">
					<input type="radio" name="alert" id="deki-page-alerts-off" '.
					($status == DekiPageAlert::STATUS_OFF ? 'checked="checked"' : '') .
					'value="'. DekiPageAlert::STATUS_OFF .'" />
					<label for="deki-page-alerts-off">'. wfMsg('Page.PageAlerts.form.off.verbose') .'</label>
				</li>';
			}
			
			// wrap the options elements in a list and form
			$html = 
			'<div id="deki-page-alerts">' .
				$toggleHtml .
				'<form class="options">' .
					'<div class="legend">' . wfMsg('Page.PageAlerts.form.legend') . '</div>' .
					'<ul>' . $optionsHtml . '</ul>' .
				'</form>' .
			'</div>';
		}
		return $html;
	}

	/**
	 * getAlertsForm
	 *
	 * @param Title $Title
	 * @param DekiPageAlert $Alert
	 * @return string
	 */
	public function getAlertsForm($Title, $Alert)
	{
		$status = $Alert->getStatus();
		$htmlPageTitle = htmlspecialchars($Title->getDisplayText());
		$html = '<h2>' . $htmlPageTitle . '</h2>';

		if ($status == DekiPageAlert::STATUS_PARENT)
		{
			$parentId = $Alert->getSubscriberId();
			$ParentTitle = Title::newFromId($parentId);
			$html .= wfMsg('Page.PageAlerts.notice.parent',
				'<a href="' . $this->getTitle()->getLocalUrl('id=' . $parentId) .'">' .
					$ParentTitle->getDisplayText() .
				'</a>'
			);
		}
		else
		{
			// used as the parameters array to set which radio is checked
			$checked = array('checked' => true);

			$html .= '<form method="post" class="page-alerts">';
				$html .= '<legend>'. wfMsg('Page.PageAlerts.form.legend') .'</legend>';
				$html .= '<div class="field">';
					$html .= DekiForm::singleInput('radio', 'status', DekiPageAlert::STATUS_SELF, $status == DekiPageAlert::STATUS_SELF ? $checked : null, wfMsg('Page.PageAlerts.form.self'));
				$html .= '</div>';
				$html .= '<div class="field">';
					$html .= DekiForm::singleInput('radio', 'status', DekiPageAlert::STATUS_TREE, $status == DekiPageAlert::STATUS_TREE ? $checked : null, wfMsg('Page.PageAlerts.form.tree'));
				$html .= '</div>';
				$html .= '<div class="field">';
					$html .= DekiForm::singleInput('radio', 'status', DekiPageAlert::STATUS_OFF, $status == DekiPageAlert::STATUS_OFF ? $checked : null, wfMsg('Page.PageAlerts.form.off.verbose'));
				$html .= '</div>';
				$html .= '<div class="submit">';
					$html .= DekiForm::singleInput('button', 'action', 'save', null, wfMsg('Page.PageAlerts.form.submit'));
					$html .= wfMsg('Page.PageAlerts.form.cancel', $Title->getLocalUrl(), $htmlPageTitle);
				$html .= '</div>';
			$html .= '</form>';
		}
		return $html;
	}

	/**
	 * setPageAlertsStatus
	 *
	 * @param Article $Article
	 * @param string $status
	 */
	public static function setPageAlertsStatus($Article, $status)
	{
		if (is_null($status))
		{
			return array(
				'success' => false,

				//TODO (andyv): Localize error message
				'message' => 'Requested page alert status invalid'
			);
		}

		$Alert = new DekiPageAlert($Article->getId(), $Article->getParentIds());
		$currentStatus = $Alert->getStatus();

		if ($currentStatus == DekiPageAlert::STATUS_PARENT)
		{
			return array(
				'success' => false,

				//TODO (andyv): Localize error message
				'message' => 'A parent page is already subscribed'
			);
		}
		else if ($currentStatus == $status)
		{
			// no change
			$isSubscribed = $Alert->isSubscribed();
			return array(
				'success' => true,
				'body' => $isSubscribed,
				'message' => $isSubscribed ? wfMsg('Page.PageAlerts.status.on') : wfMsg('Page.PageAlerts.status.off')
			);
		}
		else
		{
			$User = DekiUser::getCurrent();
			DekiPlugin::executeHook(self::HOOK_PRE_SET_STATUS, array($Article, $User, $currentStatus, &$status));

			// perform status change
			$Result = $Alert->setStatus($status);
			$statusChange = (!is_null($Result) && $Result->isSuccess()) ? true : false;
			DekiPlugin::executeHook(self::HOOK_POST_SET_STATUS, array($Article, $User, $status, $statusChange));

			if ($Result->isSuccess())
			{
				$isSubscribed = $Alert->isSubscribed();
				return array(
					'success' => true,
					'body' => $isSubscribed,
					'message' => $isSubscribed ? wfMsg('Page.PageAlerts.status.on') : wfMsg('Page.PageAlerts.status.off')
				);
			}
			else
			{
				// some error occurred
				$isSubscribed = $Alert->isSubscribed();
				return array(
					'success' => false,
					'message' => $Result->getError()
				);
			}
		}
	}

	public function output()
	{
		$this->includeSpecialCss('special_page_alerts.css');

		$html = '';
		$Request = DekiRequest::getInstance();
		$pageId = $Request->getInt('id', 0);

		// attempt to fetch the page information
		$Title = Title::newFromId($pageId);

		if (is_null($Title))
		{
			self::redirectHome();
			return;
		}

		// make sure the user is not anonymous
		$User = DekiUser::getCurrent();

		if ($User->isAnonymous())
		{
			// notify the user that they must be logged in first
			DekiMessage::error(wfMsg('Page.PageAlerts.error.anonymous'));

			$UserLogin = Title::newFromText('UserLogin', NS_SPECIAL);
			self::redirectTo($UserLogin, $Title);
			return;
		}

		$Article = new Article($Title);

		if ($Request->isPost())
		{
			$results = self::setPageAlertsStatus($Article, $Request->getVal('status'));
			if($results['success'])
			{
				DekiMessage::success(wfMsg('Page.PageAlerts.success'));
				self::redirectTo($Title);
				return;
			}
			else
			{
				DekiMessage::error($results['message']);
			}
		}

		$Alert = new DekiPageAlert($Article->getId(), $Article->getParentIds());
		$html .= $this->getAlertsForm($Title, $Alert);

		return $html;
	}
}
DekiPageAlertsPlugin::init();
