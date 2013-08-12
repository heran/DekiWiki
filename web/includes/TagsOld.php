<?php
/*
 * MindTouch Core - open source enterprise collaborative networking
 *  derived from MediaWiki (www.mediawiki.org)
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

/***
 * markup generation of all tags on a given page
 * @param int $titleId - int
 */
function wfShowTags($titleId = null)
{
	if ( !is_null($titleId) )
	{
		$PageTitle = Title::newFromID($titleId);
		$Article = new Article($PageTitle);
	}
	else
	{
		global $wgArticle;
		$Article = $wgArticle;
		$titleId = $Article->getId();
	}

	$tags = DekiTag::getPageList($titleId);
	$tags = DekiTag::sort($tags);

	$list = array();
	$relatedlines = array();

	$html = '<div class="pageTagList"><div class="item taglist">';
	
	$allrelated = array();
	if ( !empty($tags) )
	{
		//as we loop through each tag...
		foreach ($tags as $Tag)
		{
			$related = array();

			//go through the list of related pages, and display them
			$pages = $Tag->getRelatedPages();

			if ( count($pages) > 0 )
			{
				foreach ($pages as $Title)
				{
					//don't show the page itself in the list of defined pages
					if (strcmp($Title->getArticleID(), $titleId) == 0)
					{
						continue;
					}
					$related[] = $allrelated[] = '<a href="' . $Title->getLocalUrl() . '">' . $Title->getDisplayText() . '</a>';
				}

				$relatedlines[] = '<div class="item relatedpages">' . wfMsg('Page.Tags.related-pages-for', $Tag->getTitle(),
					empty($related)
						? wfMsg('Page.Tags.no-other-pages')
						:  implode(', ',$related)).'</div>';
			}

			//store the tag
			$uri = $Tag->getUri();
			if ( !empty($uri) )
			{
				$list[] = '<a href="' . $uri . '" title="' . $Tag->toHtml() . '">' . $Tag->toHtml() . '</a>';
			}
			else
			{
				$list[] = $Tag->toHtml();
			}
		}

		$html .= implode(', ', $list);
	}

	$html .= '</div>';

	$disabled = '';
	
	if ( !$Article->userCanTag() )
	{
		$disabled = 'class="disabled" onclick="return false;"';
	}
	
	$html .= '<div><a href="' . Title::newFromText('Tags', NS_SPECIAL)->getLocalURL('pageId=' . $Article->getID()) . '"' . $disabled . '" id="deki-page-tags-toggleview">' . wfMsg('Page.Tags.edit-tags') . '</a></div>';	

	$html .= implode('', $relatedlines); //display related pages html
	$html .= '</div>';

	//set the related pages
	$allrelated = array_unique($allrelated);

	global $wgOut;
	$wgOut->setRelated($allrelated);
	$wgOut->setTags($list);

	return $html;
}
