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
 
/**
 * DekiLoggedSearchResult
 * Presentation formatting class
 * @TODO guerrics: create and then extend DekiSearchResult or consider IDekiSearchResult
 */
class DekiLoggedSearchResult
{
	const TYPE_PAGE = 'page';
	const TYPE_FILE = 'file';
	const TYPE_COMMENT = 'comment';

	// general result information
	/**
	 * @var enum {TYPE_PAGE, TYPE_FILE, TYPE_COMMENT}
	 */
	protected $type = null;
	protected $title = '';
	protected $uriResult = '';
	protected $uriTrack = '';

	// user information
	protected $user = null; 
	
	// page information
	protected $pagePath = null;
	protected $pageTitle = null;

	// meta information
	protected $rank = null;
	protected $wordCount = null;
	protected $size = null;
	protected $textPreview = null;
	protected $dateUpdated = null;


	public static function newFromArray(&$result)
	{
		$X = new XArray($result);

		// create the new search result
		$Object = new self();
		self::populateObject($Object, $X);

		return $Object;
	}

	/**
	 * @param $SearchResult
	 * @param mixed $result - array or XArray
	 * @return
	 */
	protected static function populateObject($SearchResult, &$result)
	{
		$X = is_object($result) ? $result : new XArray($result);

		$SearchResult->uriResult = $X->getVal('uri');
		$SearchResult->uriTrack = $X->getVal('uri.track');

		// author information
		$SearchResult->author = $X->getVal('author');
		
		// page information
		$SearchResult->pageTitle = $X->getVal('page/title');
		$SearchResult->pagePath = $X->getVal('page/path');

		// search result title
		$SearchResult->title = $X->getVal('title');
		$SearchResult->type = $X->getVal('type');
		$SearchResult->rank = $X->getVal('rank');
		$SearchResult->size = $X->getVal('size');
		$SearchResult->id = $X->getVal('id');

		$SearchResult->dateUpdated = $X->getVal('date.modified');
		$SearchResult->textPreview = $X->getVal('content');
		$SearchResult->wordCount = $X->getVal('wordcount');

		//$SearchResult->tags = explode("\n", $X->getVal('tag'));
	}
	
	/**
	 * Only search results can create new search results. For now.
	 *
	 */
	protected function __construct() {}
	
	public function isPageResult() { return $this->type == self::TYPE_PAGE; }
	public function isFileResult() { return $this->type == self::TYPE_FILE; }
	public function isCommentResult() { return $this->type == self::TYPE_COMMENT; }

	/**
	 * Returns the result type.
	 * @return string
	 */
	public function getType() { return $this->type; }

	public function getUrl() { return $this->uriResult; }
	public function getTrackUrl() { return $this->uriTrack; }
	
	
	/**
	 * Returns the author object
	 * @TODO royk: follow-up with arne on returning an id instead
	 * @return DekiUser
	 */
	public function getUser() { return DekiUser::newFromText($this->author); }
	
	/**
	 * Returns the user link
	 * 
	 * @return string
	 */
	public function getUserLink()
	{
		global $wgUser;
		$User = $this->getUser();
 		return $wgUser->getSkin()->makeLinkObj($User->getUserTitle(), $User->toHtml());
 	}
	
	/**
	 * Adds search term highlighting if enabled.
	 * 
	 * @return string
	 */
	public function getHighlightedUrl($searchTerms)
	{
		global $wgUser;
		if (!$wgUser->canHighlightTerms() || !$this->isPageResult())
		{
			return $this->getUrl();
		}
		
		// add the highlight query param
		$url = parse_url($this->uriResult);
		$url['query'] = (isset($url['query']) ? $url['query'] . '&' : '') . 'highlight=' . urlencode($searchTerms);

		return 
			(isset($url['scheme']) ? $url['scheme'] . '://' : '') .
			(isset($url['user']) ? $url['user'] . (isset($url['pass']) ? ':' . $$url['pass'] : '') . '@' : '') .
			(isset($url['host']) ? $url['host'] : '') .
			(isset($url['port']) ? ':' . $url['port'] : '') .
			(isset($url['path']) ? $url['path'] : '') .
			(isset($url['query']) ? '?' . $url['query'] : '') .
			(isset($url['fragment']) ? '#' . $url['fragment'] : '')
		;
	}


	/**
	 * Generates a link to the search result
	 * 
	 * @return html
	 */
	public function getTitleLink($href = null, $html = null)
	{
		if (is_null($href))
		{
			$href = $this->getUrl();
		}
		if (is_null($html))
		{
			$html = htmlspecialchars($this->title);
		}

		// hardcoding onclick to mask track url in the markup
		$onClick = '__deki_search_results(event, '.
			"'". wfEncodeJSHTML($this->getUrl()) ."',".
			"'". wfEncodeJSHTML($this->getTrackUrl()) ."'".
		');';

		return '<a href="'. $href .'" title="'.htmlspecialchars($href).'" onclick="'. $onClick .'" class="go">'. $html .'</a>';
	}

	/**
	 * Beautifies the url for display
	 * 
	 * @return string
	 */
	public function getUrlDisplay()
	{
		$url = $this->getUrl();
		// chop scheme
		$parts = explode('://', $url);
		// @TODO: chop middle of url if length > X
		return $parts[1];
	}

	/**
	 * Creates a link to the result page
	 *
	 * @param string $html - html to wrap in a link 
	 * @return html
	 */
	public function getPageLink($html)
	{
		// need to build the ui uri
		$Title = Title::makeTitle(NS_MAIN, $this->pagePath);
		$pageUri = $Title->getFullUrl();

		return '<a href="'. $pageUri .'">'. $html .'</a>';
	}

	
	/**
	 * Determines the result's page namespace
	 *
	 * @return html
	 */
	public function getPageNamespace($return = 'main')
	{
		// need to build the ui uri
		$Title = Title::newFromUrl($this->pagePath);
		
		// null ref check
		if (is_null($Title))
		{
			return $return;
		}
		$ns = strtolower(DekiNamespace::getCanonicalName($Title->getNamespace())); 
		return empty($ns) ? $return : $ns;
	}
	
	/**
	 * Generates a link to the result's page
	 * 
	 * @return html
	 */
	public function getPagePathLink()
	{
		$html = htmlspecialchars($this->pagePath);
		if (empty($this->pagePath))
		{
			// current result page is the homepage
			$html = htmlspecialchars($this->pageTitle);
		}

		return $this->getPageLink($html);
	}

	/**
	 * @return string
	 */
	public function getLastUpdated()
	{
		global $wgLang;
		return $wgLang->timeanddate($this->dateUpdated, true);
	}
	
	/**
	 * Retrieve the last update information
	 * 
	 * @return string
	 */
	public function getLastUpdatedDiff()
	{
		$diff = Skin::humanReadableTime(wfTimestamp(TS_UNIX, time()) - wfTimestamp(TS_UNIX, $this->dateUpdated));
		return wfMsg('Page.Search.updated', $this->getLastUpdated(), wfMsg('Page.Search.time-ago', $diff));
	}

	/**
	 * @return string
	 */
	public function getWordCount()
	{
		return wfMsg('Page.Search.words', $this->wordCount);
	}
	
	/***
	 * Returns the ID of the search result item
	 * 
	 * @return int
	 */
	public function getId() 
	{
		return $this->id; 
	}
	
	/**
	 * For files, returns the formatted size in bytes
	 * 
	 * @return string
	 */
	public function getSize()
	{
		return wfFormatSize($this->size);
	}
	
	/**
	 * @return string
	 */
	public function getLocation()
	{
		$link = $this->getPagePathLink();

		if ($this->isFileResult())
		{
			return wfMsg('Page.Search.result-file', $link);
		}
		else if ($this->isCommentResult())
		{
			return wfMsg('Page.Search.result-comment', $link);
		}
	}

	/** 
	 * @return string
	 */
	public function getTextPreview($maxLength = null)
	{
		if (empty($this->textPreview))
		{
			return null;
		}

		if (!is_null($maxLength) && strlen($this->textPreview) > $maxLength)
		{
			$textPreview = substr($this->textPreview, 0, $maxLength) . ' ...';
		}
		else
		{
			$textPreview = $this->textPreview;
		}

		return $textPreview; 
	}
	
	/**
	 * @return html
	 */
	public function getIcon()
	{
		switch ($this->type)
		{
			default:
			case self::TYPE_COMMENT:
				return Skin::iconify('comments');
				
			case self::TYPE_PAGE:
				$ns = $this->getPageNamespace(null);
				if (!is_null($ns)) 
				{
					return Skin::iconify('ns-' . $ns);
				}
				return '';

			case self::TYPE_FILE:
				$extension = '';
				if (strpos($this->uriResult, '.') !== false)
				{
					$extension = strtolower(end(explode('.', $this->uriResult)));
				}

				global $wgStyledExtensions;
				$extensionClass = 
					in_array($extension, $wgStyledExtensions) ?
					$extension :
					'unknown'
				;

				// icon html
				return Skin::iconify('mt-ext-' . $extensionClass);
		}
	}
}
