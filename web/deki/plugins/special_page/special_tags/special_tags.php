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

if (defined('MINDTOUCH_DEKI'))
{
	DekiPlugin::registerHook(Hooks::SPECIAL_TAGS, 'wfSpecialTags');
}

function wfSpecialTags($pageName, &$pageTitle, &$html, &$subhtml)
{
	// include the form helper
	DekiPlugin::requirePhp('special_page', 'special_form.php');

	$Special = new SpecialTags($pageName, basename(__FILE__, '.php'));

	// set the page title
	$pageTitle = $Special->getPageTitle();
	$Special->output($html, $subhtml);
}

class SpecialTags extends SpecialPagePlugin
{
	protected $pageName = 'Tags';

	protected $Request;

	public function getPageTitle()
	{
		$this->Request = DekiRequest::getInstance();
		$tag = $this->Request->getVal('tag');

		if (!empty($tag))
		{
			return wfMsg('Page.Tags.page-title-search');
		}
		else if ($this->Request->has('pageId'))
		{
			$Title = Title::newFromID($this->Request->getVal('pageId'));
			return wfMsg('Page.Tags.editing-tags-for', $Title->getDisplayText());
		}

		return wfMsg('Page.Tags.page-title');
	}

	public function output(&$html, &$subhtml)
	{
		$this->includeSpecialCss('special_tags.css');

		$html = '';

		$this->Request = DekiRequest::getInstance();

		$tag = $this->Request->getVal('tag');

		if (!empty($tag))
		{
			$language = $this->Request->getVal('language');

			$html .= $this->getPages($tag, $language);
			$subhtml = $this->getFormHtml($this->getTitle(), $tag, $language);
		}
		else if ($this->Request->has('pageId'))
		{
			$pageId = $this->Request->getVal('pageId');
			$html .= $this->getTags($pageId);
		}
		else
		{
			$html .= $this->getSiteTagsHtml();
			$subhtml = $this->getFormHtml($this->getTitle());
		}
	}

	private function &getTagListHtml($tags)
	{
		$html = '';

		foreach($tags as $Tag)
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

			$html .= '<li>' . $taghtml . '</li>';
		}

		return $html;
	}

	private function &getFormHtml(Title $Title, $tag = '', $language = '')
	{
		$html = '';
		$html = '<form method="get" action="' . $Title->getLocalUrl() . '">';

		$html .= '<label for="special-tag-search">' . wfMsg('Page.Tags.tags-search') . '</label> ';
		$html .= DekiForm::singleInput('text', 'tag', '', array('class' => 'input-text', 'id' => 'special-tag-search'));

		if (DekiLanguage::isSitePolyglot())
		{
			$options = wfAllowedLanguages(wfMsg('Form.language.filter.all'));

			$html .= ' <label for="select-language">' . wfMsg('Page.Tags.tags-filter-in') . '</label> ';
			$html .= DekiForm::multipleInput('select', 'language', $options, $language);
		}

		$html .= DekiForm::singleInput('button', 'tag_search', wfMsg('Page.Tags.tags-search-verb'), null, wfMsg('Page.Tags.tags-search-verb'));
		$html .= '</form>';

		return $html;
	}

	private function &getSiteTagsHtml()
	{
		$html = '';
		$types = array(DekiTag::TYPE_TEXT, DekiTag::TYPE_DEFINE, DekiTag::TYPE_USER);

		$html .= '<div id="deki-site-tags-list">';

		foreach ($types as $type)
		{
			$tags = DekiTag::getSiteList($type);

			if (empty($tags))
			{
				continue;
			}

			DekiTag::sort($tags);

			// split the tags into two lists
			$count = count($tags);
			$offset = ($count % 2 == 0) ? ($count / 2) : (($count + 1) / 2);

			$tagsLeft = array_slice($tags, 0, $offset);
			$tagsRight = array_slice($tags, $offset);

			$html .= '<div class="tag-list" id="deki-site-tags-' . $type . '"><h2>' . wfMsg('Page.Tags.type-' . $type) . '</h2>';

			$html .= '<ul class="deki-page-tags-left">' . $this->getTagListHtml($tagsLeft) . '</ul>';
			$html .= '<ul class="deki-page-tags-right">' . $this->getTagListHtml($tagsRight) . '</ul>';

			$html .= '</div>';
		}

		$html .= '</div>';

		return $html;
	}

	private function getPages($tag, $language = null)
	{
		$pages = array();
		$html = '';

		$Tag = new DekiTag($tag);
		$pages = DekiTag::getTaggedPages($Tag, $language);

		if (empty($pages))
		{
			$inLanguage = !empty($language) ? wfMsg('Page.Tags.no-tags-language') : '';
			$html .= '<p>' . wfMsg('Page.Tags.no-tags', htmlspecialchars($tag), $inLanguage) . '</p>';
		}
		else
		{
			$html .= '<ul>' . PHP_EOL;

			foreach ($pages as $PageInfo)
			{
				$html .= '<li><a href="' . $PageInfo->uriUi . '">' . htmlspecialchars($PageInfo->title) . '</a></li>' . PHP_EOL;
			}

			$html .= '</ul>';
		}

		return $html;
	}

	private function getTags($pageId)
	{
		$html = '';

		$Title = Title::newFromID($pageId);
		$Article = new Article($Title);

		if ( !$Article->userCanTag() || !$Title->getArticleID() )
		{
			DekiMessage::error(wfMsg('Article.Common.page-is-restricted'));
			return;
		}

		// add single tag
		if ($this->Request->has('doAddTag'))
		{
			$newtag = $this->Request->getVal('newtag');
			$error = '';

			try
			{
				$Tag = new DekiTag($newtag);
				$tagArray = array($Tag);
				$Result = DekiTag::update($pageId, $tagArray);

				if ($Result != true)
				{
					$error = $Result->getError();
				}
			}
			catch (Exception $e)
			{
				$error = $e->getMessage();
			}

			if( !empty($error) )
			{
				DekiMessage::error($error);
			}
		}

		// delete tags by id
		if ($this->Request->has('doRemoveTags'))
		{
			$removingTags = $this->Request->getArray('tags');
			$error = '';

			try
			{
				$Result = DekiTag::delete($pageId, $removingTags);

				if ($Result != true)
				{
					$error = $Result->getError();
				}
			}
			catch (Exception $e)
			{
				$error = $e->getMessage();
			}

			if( !empty($error) )
			{
				DekiMessage::error($error);
			}
		}

		$tags = DekiTag::getPageList($pageId);
		$hasTags = !empty($tags);

		DekiTag::sort($tags);

		$ul = '<ul class="tag-edit">';

		foreach ($tags as $Tag)
		{
			$tag = $Tag->toHtml();
			$value = htmlspecialchars($Tag->getValue());

			if ( empty($tag) )
			{
				$tag = $value;
			}

			$checkbox = DekiForm::singleInput('checkbox', 'tags[]', $Tag->getId(), array('id' => 'deki-tag-' . $Tag->getId()));
			$ul .= '<li title="' . $value . '">' . $checkbox . '<label for="deki-tag-' . $Tag->getId() . '">&nbsp;' . $tag;

			if ($Tag->getType() != DekiTag::TYPE_TEXT)
			{
				$ul .= ' ';
				$ul .= '<span class="tag-type">' . $Tag->getType() . '</span>';
			}

			$ul .= '</label></li>';
		}


		$ul .= '</ul>';

		if ($hasTags)
		{
			$ul .= '<div class="tag-remove">' . DekiForm::singleInput('button', 'doRemoveTags', wfMsg('Page.Tags.remove-tags'), null, wfMsg('Page.Tags.remove-tags')) . '</div>';
		}

		$Table = new DomTable();
		$Table->removeClass('table');
		$Table->setColWidths('20%', '50%', '30%');

		$Table->addRow();
		$Cell = $Table->addCol('<label for="deki-page-tags-add">' . wfMsg('Page.Tags.add-tag') . ':</label>');

		$input = '<input type="text" name="newtag" value="" class="input-text" id="deki-page-tags-add" />';
		$Table->addCol($input);

		$input = DekiForm::singleInput('submit', 'doAddTag', wfMsg('Page.Tags.add-tag'));
		$Table->addCol($input);

		if ($hasTags)
		{
			$Table->addRow();
			$Cell = $Table->addCol(wfMsg('Page.Tags.existing-tags') . ':');

			$ContainerCell = $Table->addCol($ul);
			$ContainerCell->setAttribute('id', 'deki-page-tags-existing');

			$Table->addCol('&nbsp;');
		}

		$Table->addRow();
		$Table->addCol('&nbsp;');
		$Table->addCol('&nbsp;');
		// <a> has a special id ("-page-tags-return") so regular tag js click events aren't attached
		$Table->addCol('<div><a href="' . $Title->getLocalURL() . '" id="deki-page-tags-return">' . wfMsg('Page.Tags.return-to-view-tags') . '</a></div>');

		$html .= '<div id="deki-page-tags"><form action="' . $this->getTitle()->getLocalUrl('pageId=' . $pageId) . '" method="post">' . $Table->saveHtml() . '</form></div>';

		return $html;
	}
}
