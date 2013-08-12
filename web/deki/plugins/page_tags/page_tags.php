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

// @TODO kalida: Show spinner when tag submitting
// @TODO kalida: highlight newly-added tags when added
// @TODO kalida: cleanup 'tagstext' template variable

if (defined('MINDTOUCH_DEKI')) :

class MindTouchTagPlugin extends DekiPlugin
{
	const AJAX_FORMATTER = 'PageTags';

	/**
	 * Initialize the plugin and hooks into the application
	 */
	public static function load()
	{
		DekiPlugin::registerHook(Hooks::PAGE_RENDER_TAGS, array('MindTouchTagPlugin', 'renderHook'));
		DekiPlugin::registerHook(Hooks::AJAX_FORMAT . self::AJAX_FORMATTER, array('MindTouchTagPlugin', 'ajaxHook'));

		DekiPlugin::registerHook(Hooks::SKIN_OVERRIDE_VARIABLES, array('MindTouchTagPlugin', 'renderSkinHook'));
	}

	/*
 	 * @param Title $Title - title corresponding to the rendered article
 	 * @param string &$pluginHtml - tags html
 	 * @return N/A
 	 */
	public static function renderHook($Title, &$pluginHtml)
	{
		$Article = self::getArticle();
		$pluginHtml = self::getViewHtml($Article);
	}

	/**
	 * Set skin variable for tag edit link
	 * @param $Template
	 * @return unknown_type
	 */
	public static function renderSkinHook(&$Template)
	{
		$Article = self::getArticle();
		$editTagsHtml = self::getEditTagsHtml($Article);
		$Template->set('tagsedit', $editTagsHtml);
	}

	/**
	 * Called when the ajax formatter for tags is hit; acts like a controller and returns
	 * appropriate view to client. Encodes parameters to json by default
	 *
	 * @param string &$body
	 * @param string &$message
	 * @param bool &$success
	 * @return N/A
	 */
	public static function ajaxHook(&$body, &$message, &$success)
	{
		$Request = DekiRequest::getInstance();

		$action = $Request->getVal('action');
		$pageId = $Request->getVal('pageId');
		$Article = self::getArticle($pageId);

		// default to failure
		$body = '';
		$success = false;

		// could not find article, or title does not exist
		if ( is_null($Article) || is_null($Article->getTitle()) )
		{
			return;
		}

		switch ($action)
		{
			default:
			case 'view':
				$body = self::getViewHtml($Article);
				break;

			case 'edit':
				$body = self::getEditHtml($Article);
				break;

			case 'bulksave':
				// old dialog takes newline-separated list of tags
				$tags = $Request->getVal('tags');
				$tagsArray = explode("\n", $tags);

				// replace tag list, don't append
				$result = self::saveTags($pageId, $tagsArray, false);
				$message = $result['message'];
				if (!$result['success']){ return; }

				break;

			case 'save':
				// get newline-separated list of tags
				$tags = $Request->getVal('tags');
				$tagsArray = explode("\n", $tags);

				$result = self::saveTags($pageId, $tagsArray);
				$message = $result['message'];
				if (!$result['success']){ return; }

				// include edit list html to avoid additonal roundtrip
				$body = self::getEditListHtml($Article);
				break;

			case 'delete':
				$tagIds = $Request->getVal('tagIds');
				$tagIdArray = explode(',', $tagIds);

				$result = self::deleteTags($pageId, $tagIdArray);
				$message = $result['message'];
				if (!$result['success']){ return; }

				$body = self::getEditListHtml($Article);
				break;

			case 'getsitelist':
				// full list of tags, for autocomplete
				$prefix = $Request->getVal('q');
				$body = self::getSiteList($prefix);
				break;
		}

		// if we made it here then it was a successful request
		$success = true;
	}

	/**
	 * Complete list of tags on the site, used for autocomplete
	 * @return array - tag data stored in json format
	 */
	protected static function getSiteList($prefix = '')
	{
		// One-off conversion; need array which is later JSON encoded
		// want count, type, etc. returned, not just value
		$jsonTags = array();

		// autocompleting entire tag list not supported
		if (empty($prefix))
		{
			return $jsonTags;
		}

		// autocompleting entire user list not supported
		if ($prefix == '@')
		{
			return $jsonTags;
		}

		// autocomplete user names
		$Tag = new DekiTag($prefix);

		if ($Tag->getType() == DekiTag::TYPE_USER)
		{
			// extract name to search: @foo => foo
			$name = $Tag->getValue(false);
			$filters = array('usernamefilter' => $name);
			$users = DekiUser::getSiteList($filters);

			foreach ($users as $User)
			{
				$tagText = '@' . $User->toHtml();
				$jsonTags[] = array(
					'title' => $tagText,
					'value' => $tagText,
					'type'	 => DekiTag::TYPE_USER,
					'count' => 1
				);
			}
		}
		else
		{
			// assume text tag; "t" becomes "t%" query
			$query = $prefix . '%';
			$tags = DekiTag::getSiteList(null, $query);

			foreach ($tags as $Tag)
			{
				// do not show define: tags in autocomplete list
				if ($Tag->getType() == DekiTag::TYPE_DEFINE)
				{
					continue;
				}

				$jsonTags[] = array(
					'title' => $Tag->getTitle(),
					'value' => $Tag->getValue(),
					'type'	 => $Tag->getType(),
					'count' => $Tag->getCount()
				);
			}
		}

		return $jsonTags;
	}

	/**
	 * Return ordered list of tags for current page
	 * @param $pageId
	 * @return DekiTag[]
	 */
	protected static function getTags($pageId = null)
	{
		$tags = DekiTag::getPageList($pageId);
		DekiTag::sort($tags);

		return $tags;
	}

	// @TODO kalida: Would be good to have a global method to grab article, pageId, etc.
	protected static function getArticle($pageId = null)
	{
		if (!is_null($pageId))
		{
			$PageTitle = Title::newFromID($pageId);
			$Article = new Article($PageTitle);
		}
		else
		{
			global $wgArticle;
			$Article = $wgArticle;
		}

		return $Article;
	}

	/**
	 * Convert array of tags to html
	 * @param $tags - list of tags
	 * @param $lilist - list of <li></li> tags ready to be wrapped
	 * @param $rawlinks - array of inner <li> contents
	 * @return N/A
	 */
	protected static function getTagListHtml($tags, &$lilist, &$rawlinks)
	{
		$html = '';
		$list = array();

		foreach ($tags as $Tag)
		{
			$uri = $Tag->getUri();

			$typehtml = '';
			if ($Tag->getType() != DekiTag::TYPE_TEXT)
			{
				$typehtml .= ' ';
				$typehtml .= '<span class="tag-type">' . $Tag->getType() . '</span>';
			}

			if (!empty($uri))
			{
				$taghtml = '<a href="' . $uri . '" title="' . $Tag->toDetailedHtml()
								. '" tagid="' . $Tag->getId() . '" class="' . $Tag->getType() . '">'
								. $Tag->toHtml() . $typehtml . '</a>';
			}
			else
			{
				$taghtml = '<span title="' . $Tag->toDetailedHtml()
								. '" tagid="' . $Tag->getId() . '" class="' . $Tag->getType() . '">'
								. $Tag->toHtml() . $typehtml . '</span>';
			}

			$list[] = $taghtml;
			$html .= '<li>' . $taghtml . '</li>';
		}

		$lilist = $html;
		$rawlinks = $list;
	}

	/**
	 * Create html markup for the tag edit link
	 * @param Article $Article
	 * @return string
	 */
	protected static function &getEditTagsHtml($Article)
	{
		$html = '';

		// newly-created page
		if ($Article->getId() == 0)
		{
			$html .= '<span id="deki-page-tags-edit-link">';
			$html .= '(' . wfMsg('Page.Tags.save-page') . ')';
			$html .= '</span>';
			return $html;
		}

		// edit tags link
		$disabled = '';
		if (!$Article->userCanTag())
		{
			$disabled = ' class="disabled" onclick="return false;" ';
		}

		// link to Special:Tags by default; javascript overrides onclick when enabled
		$Title = Title::newFromText('Tags', NS_SPECIAL);

		$html .= ' (' . '<a href="' . $Title->getLocalURL('pageId=' . $Article->getID()) . '"'
					 . $disabled . ' id="deki-page-tags-toggleview"' . '>'
					. wfMsg('Page.Tags.edit-tags') . '</a>)';

		$html .= '</span>';

		return $html;
	}

	/**
	 * Create HTML markup for all tags on a page
	 * @param Article $Article
	 */
	protected static function &getViewHtml($Article)
	{
		$tags = self::getTags($Article->getId());

		$list = array();
		$relatedlines = array();
		$allrelated = array();

		// note: output already wrapped in <div id="deki-page-tags">
		$html = '';

		// print tag list items
		$lilist = '';
		$class = 'tags';

		if (empty($tags))
		{
			$lilist = '<li>' . wfMsg('Page.Tags.page-no-tags') .'</li>';
			$class .= ' no-tags';
		}
		else
		{
			self::getTagListHtml($tags, $lilist, $list);
		}

		$html .= '<ul class="' . $class . '">' . $lilist . '</ul>';

		// @TODO kalida: Output data; where are these rendered? If going entirely plugin-based can we remove later?
		global $wgOut;
		$wgOut->setTags($list);

		return $html;
	}

	protected static function &getEditListHtml($Article)
	{
		$html = '';
		// print tags
		$tags = self::getTags($Article->getId());
		$list = '';
		$lilist = '';

		// split the tags into two lists
		$count = count($tags);
		$offset = ($count % 2 == 0) ? ($count / 2) : (($count + 1) / 2);

		$tagsLeft = array_slice($tags, 0, $offset);
		$tagsRight = array_slice($tags, $offset);

		self::getTagListHtml($tagsLeft, $lilist, $list);
		$html .= '<ul id="deki-page-tags-left" class="tags">' . $lilist . '</ul>';

		self::getTagListHtml($tagsRight, $lilist, $list);
		$html .= '<ul id="deki-page-tags-right" class="tags">' . $lilist . '</ul>';

		return $html;
	}

	protected static function &getEditHtml($Article){
		$html = '';

		// print edit box
		$html .= '<div class="tag-edit">';

		$Title = Title::newFromText('Tags', NS_SPECIAL);
		$html .= '<form action="' . $Title->getLocalURL('pageId=' . $Article->getID()) . '" method="post">';

		$html .= DekiForm::singleInput('text', 'tag', '', array('id' => 'deki-page-tags-add'));
		$html .= DekiForm::singleInput('button', 'tag_add', wfMsg('Page.Tags.add-tag'), null, wfMsg('Page.Tags.add-tag'));

		$html .= '<a href="#" id="deki-page-tags-close" class="container-close" '
					. 'title="' . wfMsg('Page.Tags.close-tag-editor') . '">';
		$html .= '<span>' . wfMsg('Page.Tags.close-tag-editor') . '</span>';
		$html .= '</a>';

		$html .= '</form>';

		$html .= '<div id="deki-page-tags-edit">';
		$html .= self::getEditListHtml($Article);
		$html .= '</div>';

		// close tag-edit
		$html .= '</div>';

		return $html;
	}

	/**
	 * Save list of tag strings to page
	 * @param $pageId - page to save to
	 * @param $tags - array of tag strings
	 * @param boolean $append - whether to append to list of tags (default true), or replace
	 * @return array - result array (success, message)
	 */
	protected static function saveTags($pageId, $tags, $append=true)
	{
		$result = array(
			'success' => false,
			'message' => ''
		);

		$dekiTags = array();
		foreach ($tags as $tag)
		{
			$dekiTags[] = new DekiTag($tag);
		}

		$Result = DekiTag::update($pageId, $dekiTags, $append);

		if ($Result === true)
		{
			$result['success'] = true;
		}
		else
		{
			$result['message'] = $Result->getError();
		}

		return $result;
	}

	/**
	 * Removes tag from page
	 * @param $pageId - Page to search for tags
	 * @param int[] $tagid - Tag to remove (id, not text because text can be generated [like "date:today" => Jan 1, 2010])
	 * @return array - results: 'success' and 'message'
	 */
	protected static function deleteTags($pageId, $tagIds)
	{
		$result = array(
			'success' => false,
			'message' => ''
		);

		$Result = DekiTag::delete($pageId, $tagIds);

		if ($Result == true)
		{
			$result['success'] = true;
		}
		else
		{
			$result['message'] = $Result->getError();
		}

		return $result;
	}
}

// initialize the plugin
MindTouchTagPlugin::load();

endif;

