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

class PageContentRatingPlugin extends DekiPlugin
{
	const PLUGIN_FOLDER = 'page_content_rating';
	const AJAX_FORMATTER = 'page_content_rating';
	const SPECIAL_PAGE = 'PageContentRating';

	public static function init()
	{
		DekiPlugin::registerHook(Hooks::AJAX_FORMAT . self::AJAX_FORMATTER, array(__CLASS__, 'ajaxHook'));
		DekiPlugin::registerHook(Hooks::SKIN_OVERRIDE_VARIABLES, array(__CLASS__, 'skinHook'));
		DekiPlugin::registerHook(Hooks::SPECIAL_PAGE . self::SPECIAL_PAGE, array(__CLASS__, 'specialHook'));
	}

	/**
	 * Create content for special page
	 * @param string $pageName - incoming page name
	 * @param string $pageTitle - page title to set
	 * @param string $html - html to output
	 * @return N/A
	 */
	public function specialHook($pageName, &$pageTitle, &$html)
	{
		global $wgDekiPluginPath;
		global $wgUser;
		
		$Request = DekiRequest::getInstance();
		$pageId = $Request->getInt('pageId');

		// html rating click: record vote & reload
		if (!$Request->getBool('popup'))
		{
			$PageInfo = DekiPageInfo::loadFromId($pageId);
			$canRate = DekiPageRating::userCanRate($wgUser, $PageInfo); 
			if ($canRate === true)
			{
				// clear user rating if submitted again
				$Rating = null;
				$Result = DekiPageRating::loadPageRating($pageId, $Rating);
				
				if ($Result->isSuccess())
				{
					$userRating = $Request->getVal('rating', null);
					
					if (!is_null($userRating))
					{
						$userRating = $userRating == $Rating->getUserRating() ? null : $userRating;
						$Rating->setUserRating($userRating);
					
						$Result = DekiPageRating::savePageRating($pageId, $Rating);
					}
				}
				
				// if any previous operations failed
				$Result->handleResponse();
			}
			else
			{
				$error = '';
				if ($canRate == DekiPageRating::REQUIRES_COMMERCIAL)
				{
					// show error message and continue to regular redirect
					$commercialLink = '<a href="' . ProductURL::CONTENT_RATING . '">' . wfMsg('System.Error.commercial-required-link') . '</a>';
					$error = wfMsg('System.Error.commercial-required', $commercialLink);
					DekiMessage::Error($error);
				}
				else if ($canRate == DekiPageRating::REQUIRES_LOGIN)
				{
					// send user to login page					
					$loginLink = wfMsg('System.Error.login-required-link');
					$error = wfMsg('System.Error.login-required', $loginLink);

					$ReturnTitle = Title::newFromId($pageId);
					$sk = $wgUser->getSkin();
					DekiMessage::Error($error);
					SpecialPagePlugin::redirect($sk->makeLoginUrl($ReturnTitle));
					return;
				}
			}

			$Title = Title::newFromId($pageId);
			SpecialPagePlugin::redirect($Title->getLocalUrl());
			return;
		}

		
		// render popup
		DekiPlugin::includeCss(self::PLUGIN_FOLDER, 'page_content_rating_popup.css');
		$View = self::createView(self::PLUGIN_FOLDER, 'popup');
		$View->set('pageId', $pageId);
		$html .= $View->render();
	}

	public static function ajaxHook(&$body, &$message, &$success, &$status)
	{
		$Request = DekiRequest::getInstance();

		$action = $Request->getVal('action');
		$pageId = $Request->getInt('pageId');
		$userRating = $Request->getVal('rating', null);
		$userComment = $Request->getVal('comment', null);

		$Rating = null;
		$Result = DekiPageRating::loadPageRating($pageId, $Rating);

		if (is_null($Rating))
		{
			$message = wfMsg('Page.ContentRating.error.page');
			$success = false;
			return;
		}

		$PageInfo = DekiPageInfo::loadFromId($pageId);
		$User = DekiUser::getCurrent();
		$popupUrl = null;

		// @note kalida: rating link should be disabled if user cannot rate -- block any direct ajax requests
		$canRate = DekiPageRating::userCanRate($User, $PageInfo);
		if ($canRate !== true)
		{
			// have a standard message if tooltips are disabled
			if ($canRate == DekiPageRating::REQUIRES_LOGIN)
			{
				$loginLink = wfMsg('System.Error.login-required-link');
				$message = wfMsg('System.Error.login-required', $loginLink);
				$status = DekiPluginsFormatter::STATUS_ERROR_LOGIN;
			}
			else if ($canRate == DekiPageRating::REQUIRES_COMMERCIAL)
			{
				$commercialLink = '<a href="' . ProductURL::CONTENT_RATING . '">' . wfMsg('System.Error.commercial-required-link') . '</a>';
				$message = wfMsg('System.Error.commercial-required', $commercialLink);
				$status = DekiPluginsFormatter::STATUS_ERROR_COMMERCIAL;
			}
			else {
				$message = wfMsg('Page.ContentRating.error.page');
			}
			
			$success = false;
			return;
		}

		switch ($action)
		{
			default:
			case 'view':
				// no actions, just return rating info
				break;

			case 'rate':
				if (!is_null($userRating))
				{
					// clear rating if submitted again
					$userRating = $userRating == $Rating->getUserRating() ? null : $userRating;
					
					// show popup if we aren't clearning, and rating down (careful: RATING_DOWN is 0 and casts to null)
					if (!is_null($userRating) && $userRating == DekiPageRating::RATING_DOWN)
					{
						$SpecialTitle = Title::newFromText(self::SPECIAL_PAGE, NS_SPECIAL);
						$popupUrl = $SpecialTitle->getLocalUrl('pageId=' . $pageId);
					}
					
					$Rating->setUserRating($userRating);
					$Result = DekiPageRating::savePageRating($pageId, $Rating);

					if (!$Result->isSuccess())
					{
						$message = wfMsg('System.Error.error');
						$body = $Result->getError();
						return;
					}
					
				}
				break;

			case 'comment':
				if (!empty($userComment))
				{
					$Result = self::addComment($pageId, $userComment);
					
					if (!$Result->isSuccess())
					{
						$message = wfMsg('System.Error.error');
						$body = $Result->getError();
						return;
					}
				}
				
				break;
		}

		$body = $Rating->toArray();

		$body['button_html'] = self::renderRatingButtons($PageInfo, $Rating);
		$body['score_text'] = self::renderScore($Rating);
		$body['popup_url'] = $popupUrl;

		$success = true;
	}

	public static function skinHook(&$Template)
	{
		global $wgArticle, $wgUser;
		$Rating = null;
		$Result = DekiPageRating::loadPageRating($wgArticle->getId(), $Rating);
		$Title = $wgArticle->getTitle();

		if ($Title->getNamespace() != NS_MAIN || !$wgArticle->isViewPage() || is_null($Rating) || !$Result->isSuccess())
		{
			return;
		}
		
		$PageInfo = DekiPageInfo::loadFromId($wgArticle->getId());
	
		$Template->set('page.rating.header', self::renderHeader($PageInfo, $Rating));
		$Template->set('page.rating', self::renderBasicForm($PageInfo, $Rating));

		return;
	}
	
	protected static function &renderHeader($PageInfo, $Rating)
	{
		$html = '';

		$html .= '<div id="deki-page-rating-bar" class="deki-page-rating-wrapper">';
		$html .= '<div id="deki-page-rating-score">' . self::renderScore($Rating) . '</div>';
		$html .= self::renderRatingButtons($PageInfo, $Rating);
		$html .= '</div>';

		return $html;
	}
	
	protected static function &renderBasicForm($PageInfo, $Rating)
	{
		$html = '';
		$html .= '<div id="deki-page-rating" class="deki-page-rating-wrapper">';
		$html .= self::renderRatingButtons($PageInfo, $Rating);
		$html .= '</div>';

		return $html;
	}

	protected static function renderScore($Rating)
	{
		if ($Rating->getCount() == 0)
		{
			return wfMsg('Page.ContentRating.display.rating.empty');
		}

		$liked = $Rating->getVoteCount();
		$total = $Rating->getCount();

		$text = wfMsg('Page.ContentRating.display.rating', $liked, $total);
		return $text;
	}

	protected static function &renderRatingButtons($PageInfo, $Rating)
	{
		global $wgUser;

		$View = self::createView(self::PLUGIN_FOLDER, 'ratingbuttons');
		$SpecialTitle = Title::newFromText(self::SPECIAL_PAGE, NS_SPECIAL);
		$pageId = $PageInfo->id;

		$disabled = '';
		
		$canRate = DekiPageRating::userCanRate($wgUser, $PageInfo); 
		if ($canRate !== true)
		{
			$disabled = 'disabled';
			if ($canRate == DekiPageRating::REQUIRES_COMMERCIAL)
			{
				$disabled .= ' disabled-commercial';
			}
			else if ($canRate == DekiPageRating::REQUIRES_LOGIN)
			{
				$disabled .= ' disabled-login';
			}
		}

		$View->set('disabled', $disabled);

		$href = $SpecialTitle->getLocalUrl('pageId=' . $pageId . '&rating=' . DekiPageRating::RATING_UP);
		$icon = $Rating->getUserRating() == DekiPageRating::RATING_UP ? 'deki-page-rating-up-highlight' : 'deki-page-rating-up';
		
		$View->set('href.rateup', $href);
		$View->set('icon.rateup', $icon);

		$href = $SpecialTitle->getLocalUrl('pageId=' . $pageId . '&rating=' . DekiPageRating::RATING_DOWN);
		$icon = $Rating->getUserRating() == DekiPageRating::RATING_DOWN ? 'deki-page-rating-down-highlight' : 'deki-page-rating-down';
		
		$View->set('href.ratedown', $href);
		$View->set('icon.ratedown', $icon);
		
		return $View->render();
	}

	/**
	 * Save feedback comment to page with elevated priviledges
	 * @param int $pageId - page receiving comment
	 * @param string $comment - comment to save
	 * @return DekiResult
	 */
	public static function addComment($pageId, $comment)
	{
		$Plug = DekiPlug::getInstance();
		$Plug = $Plug->At('pages', $pageId, 'comments');
		$Plug = $Plug->SetHeader('Content-Type', 'text/plain; charset=utf-8');

		$Result = $Plug->Post($comment);
		
		return $Result;
	}
}

PageContentRatingPlugin::init();

endif;
