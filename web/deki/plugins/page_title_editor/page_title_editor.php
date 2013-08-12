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

class PageTitleEditorPlugin extends DekiPlugin
{
	const AJAX_FORMATTER = 'page_title_editor';


	public static function init()
	{
		DekiPlugin::registerHook(Hooks::AJAX_FORMAT . self::AJAX_FORMATTER, array(__CLASS__, 'ajaxHook'));
		DekiPlugin::registerHook(Hooks::SKIN_OVERRIDE_VARIABLES, array(__CLASS__, 'skinHook'));
	}

	public static function ajaxHook(&$body, &$message, &$success)
	{
		$Request = DekiRequest::getInstance();
		// validate request
		$pageId = $Request->getInt('pageId');
		$Info = DekiPageInfo::loadFromId($pageId, $Request->getInt('redirects'));
		
		if (is_null($Info))
		{
			$body = 'Page not found.';
			return;
		}
		
		switch ($Request->getVal('action'))
		{
			case 'update':
				if (!$Request->isPost())
				{
					$message = 'HTTP POST Required';
					$success = false;
					return;
				}

				// update the specified page
				$title = $Request->getVal('title');
				$name = $Request->getVal('name');

				// save to determine if the page moved
				$currentUri = $Info->uriUi;

				$Result = DekiPageInfo::move($Info, null, $title, $name);
				if (!$Result->isSuccess())
				{
					$success = false;
					$message = $Result->getError();
					return;
				}
				
				// success!
				if ($Info->uriUi != $currentUri)
				{
					// page moved
					$message = wfMsg(
						'Dialog.PageTitleEditor.success.moved',
						'<a href="'. $Info->uriUi .'">'. htmlspecialchars($Info->uriUi) .'</a>'
					);
				}
				else
				{
					// title updated
					$message = wfMsg('Dialog.PageTitleEditor.success');
				}
				
				// push into dekimessage when we reload the page
				if (!$Request->getBool('inlinerefresh'))
				{
					DekiMessage::success($message);
				}

				$body = array(
					'uri' => $Info->uriUi,
					'title' => $Info->title,
					'name' => $Info->getPathName()
				);
				break;

			default:
				// render editor html
				$body = array(
					'html' => self::renderTitleEditor($Info->title, $Info, true),
					// page title
					'title' => $Info->title,
					// page path name
					'name' => $Info->getPathName(),
					// path type
					'type' => $Info->pathType
				);
		}

		$success = true;
	}

	public static function skinHook(&$Template)
	{
		global $wgArticle, $wgTitle, $wgRequest;
		
		// only show the title for existing pages, not new pages
		$action = $wgRequest->getVal('action');
		$subpage = $wgRequest->getBool('subpage');
		
		$Template->set('page.title', htmlspecialchars($Template->haveData('title')));
		
		// enable the title editor, disable on special pages
		if (!$subpage && ($wgArticle->getId() > 0) && ($action != 'submit') && $wgArticle->userCanEdit() && ($wgTitle->getNamespace() != NS_SPECIAL))
		{
			$Template->set('page.title', self::renderTitleEditor($Template->haveData('title')));
		}
		else if ($wgRequest->getVal('action') == 'edit')
		{
			$Template->set('page.title', '');
		}
	}

	/**
	 * @TODO guerrics: localize
	 * @param string $title - page title
	 * @param DekiPageInfo $Info
	 * @param bool $edit - if true, render the editing html. default: view
	 * @return string
	 */
	protected static function renderTitleEditor($title, $Info = null, $edit = false)
	{
		if ($edit)
		{
			// only retrieve the editing div
			$host = 'http://' . DekiRequest::getInstance()->getHost() . '/';
			
			$parents = $Info->getParents();
			
			// determine which segments to show
			if (empty($parents))
			{
				$path = '';
			}
			else if (count($parents) == 1)
			{
				$path =  htmlspecialchars(current($parents)) . '/';
			}
			else
			{
				$path = '<span class="collapsed-url">'. htmlspecialchars(array_shift($parents)) . '</span>';
			}
			
			return
				'<div class="state-edit">'.
					'<div class="fields">'.
						'<div class="title">'.
							DekiForm::singleInput('text', 'page_title', $title, array('class' => 'edit-title')).
							'<a class="toggle-link" title="'.wfMsg('Dialog.PageTitleEditor.tooltip').'"><span /></a>'.
						'</div>'.
						'<div class="path">'.
							'<span title="'. htmlspecialchars($Info->uriUi) .'">'. htmlspecialchars($host) . $path . '</span>'.
							DekiForm::singleInput('text', 'page_segment', null, array('class' => 'edit-path')).
							'/' .
						'</div>'.
					'</div>'.
					'<div class="submit">'.
						DekiForm::singleInput(
							'button',
							'page_title_action',
							'update',
							array('class' => 'edit-update'),
							wfMsg('Dialog.PageTitleEditor.button.update')
						).
						'<span class="or">'.
							' ' . wfMsg('Dialog.PageTitleEditor.text.or') . ' '.
							'<a class="cancel">'. wfMsg('Dialog.PageTitleEditor.button.cancel') .'</a>'.
						'</span>'.
					'</div>'.
				'</div>'
			;	
		}

		return 
			'<div id="deki-page-title">'.
				'<div class="state-view">'.
					'<span class="title">'. htmlspecialchars($title) .'</span>'.
				'</div>'.
				'<div class="state-hover">'.
					'<span class="title">'. htmlspecialchars($title) .'</span>'.
					'<a class="edit">'. wfMsg('Dialog.PageTitleEditor.button.edit') .'</a>'.
				'</div>'.
			'</div>'
		;
	}
}
PageTitleEditorPlugin::init();

endif;
