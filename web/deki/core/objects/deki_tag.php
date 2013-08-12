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
 * Tag management
 */
class DekiTag implements IDekiApiObject
{
	const TYPE_TEXT = 'text';
	const TYPE_DEFINE = 'define';
	const TYPE_DATE = 'date';
	const TYPE_USER = 'user';

	private static $TAG_TYPES = array(
		self::TYPE_DEFINE	=> array(
			'type'		=>	self::TYPE_DEFINE,
			'prefix'	=>	'define:',
			'sortOrder'	=>	1
		),

		self::TYPE_DATE		=> array(
			'type'		=>	self::TYPE_DATE,
			'prefix'	=>	'date:',
			'sortOrder'	=>	2
		),

		self::TYPE_USER		=> array(
			'type'		=>	self::TYPE_USER,
			'prefix'	=>	'@',
			'sortOrder'	=>	3
		),

		self::TYPE_TEXT		=> array(
			'type'		=>	self::TYPE_TEXT,
			// dummy prefix so not always matched in prefix search
			'prefix'	=>	'text:',
			'sortOrder'	=>	4
		)
	);

	// returned properties on the tag object
	protected $id = null;
	protected $count = null;
	protected $value = null;
	protected $href = null;
	protected $uri = null;
	protected $type = null;
	protected $title = null;
	protected $pages = array();

	/**
	 * Create new tag given the database id
	 * @param int $id
	 * @return DekiTag
	 */
	public static function newFromId($id)
	{
		$Tag = self::load($id);
		return $Tag;
	}

	/**
	 * Attempt to load tag from text value
	 * @param string $tag - text used lookup tag
	 * @return DekiTag
	 */
	public static function newFromText($tag)
	{
		$Tag = self::load($tag, true);
		return $Tag;
	}

	/**
	 * Create tag object from data array
	 * @param array $result
	 * @return DekiTag
	 */
	public static function newFromArray(&$result)
	{
		$Tag = new DekiTag();
		self::populateObject($Tag, $result);
		return $Tag;
	}

	/**
	 * Get array of DekiTags for a page
	 * @param int $pageId - page to load
	 * @return DekiTag[]
	 */
	public static function getPageList($pageId)
	{
		$Plug = DekiPlug::getInstance()->At('pages')->At($pageId)->At('tags');
		$Result = $Plug->Get();

		return self::getListFromResult($Result);
	}

	/**
	 * Get pages related to current one
	 * @param int $pageId - page to check
	 * @return DekiPageInfo[] - array of related pages
	 */
	public static function getRelatedPages($pageId)
	{
		$pages = array();

		// @TODO kalida: move this to a central related api
		$Plug = DekiPlug::getInstance()->At('pages')->At($pageId)->At('tags');
		$Result = $Plug->Get();

		if (!$Result->isSuccess())
		{
			return $pages;
		}

		$tags = $Result->getAll('body/tags/tag', array());

		if (empty($tags))
		{
			return $pages;
		}

		foreach ($tags as $tag)
		{
			$tagX = new XArray($tag);
			$relatedPages = $tagX->getAll('related/page', array());

			if (empty($relatedPages))
			{
				continue;
			}

			foreach ($relatedPages as $page)
			{
				$id = $page['@id'];
				if (!isset($pages[$id]))
				{
					$pages[$id] = DekiPageInfo::newFromArray($page);
				}
			}
		}

		usort($pages, array(__CLASS__, 'compareRelatedPages'));
		return $pages;
	}

	/**
	 * Return array of DekiTags for the site
	 * @param $type - restrict tags to certain type (date, user, text, etc.)
	 * @param $query - Additional 'q' query parameter for tag prefix ("foo%")
	 * @param $from - start date for type date
	 * @param $to - end date for type date (default now + 30 days)
	 * @param $pages - show pages with each tag
	 * @return DekiTag[]
	 */
	public static function getSiteList($type = null, $query = null, $from = null, $to = null, $pages = false)
	{
		if (!empty($from))
		{
			$from = strtotime($from);
			if ($from === false)
			{
				$from = mktime();
			}
			$from = date('Y-m-d', $from);
		}

		if (!empty($to))
		{
			$to = strtotime($to);
			if ($to === false)
			{
				// 30 days in seconds
				$to = $from + 2592000;
			}
			$to = date('Y-m-d', $to);
		}

		$Plug = DekiPlug::getInstance()
			->At('site', 'tags')
			->With('pages', $pages ? 'true' : 'false')
			->With('type', $type)
			->With('to', $to)
			->With('from', $from)
			->With('q', $query);

		$Result = $Plug->Get();

		return self::getListFromResult($Result);
	}

	/**
	 * Get pages sharing this tag
	 * @param DekiTag $Tag
	 * @return DekiPageInfo[]
	 */
	public static function getTaggedPages(DekiTag $Tag, $language = null)
	{
		// load tag from api if needed
		$id = $Tag->getId();
		if (empty($id))
		{
			$Tag = DekiTag::newFromText($Tag->getValue());

			if (is_null($Tag))
			{
				return array();
			}
		}

		return $Tag->getPages($language);
	}

	/**
	 * Update the full list of tags on the site
	 * @param $pageId
	 * @param DekiTag[] $tags - array of DekiTags to save
	 * @param boolean $append - append to existing list of tags (default true), or replace
	 * @return DekiResult - true if success, api result otherwise
	 */
	public static function update($pageId, $tags, $append = true)
	{
		$tagsToSave = array();

		// include existing tags if not already added
		if ($append)
		{
			$existingTags = self::getPageList($pageId);
			$tags = array_merge($tags, $existingTags);
		}

		// prepare data for api: duplicate tags overwritten
		$tagsData = array();
		foreach ($tags as $Tag)
		{
			// @TODO kalida: ignore tags that do not have values, as '@' breaks future tags
			// Waiting on bug: 7881 to filter on the api side
			$contents = $Tag->getValue(false);
			if (strlen($contents) == 0)
			{
				continue;
			}

			$tagsData[$Tag->getValue()] = $Tag->toArray();
		}

		$Plug = DekiPlug::getInstance()->At('pages', $pageId, 'tags')->With('redirects', 0);
		$xml = array(
			'tags' => array(
				'tag' => array_values($tagsData)
			)
		);

		$Result = $Plug->Put($xml);

		if ($Result->isSuccess())
		{
			return true;
		}

		return $Result;
	}

	/**
	 * Delete tag from page
	 * @param int $pageId - page to remove from
	 * @param int[] $tagIds - tags to remove
	 * @return boolean - true if success, api result otherwise
	 */
	public static function delete($pageId, $tagIds)
	{
		// Tag API doesn't have delete, so prune existing tag list and save
		$existingTags = self::getPageList($pageId);

		foreach ($tagIds as $id)
		{
			if (isset($existingTags[$id]))
			{
				unset($existingTags[$id]);
			}
		}

		$tags = array_values($existingTags);
		return self::update($pageId, $tags, false);
	}

	/**
	 * Sort tags, by type (define, user, date, etc.) and then tag value
	 * @param DekiTag[] &$tags - items to sort
	 * @return void
	 */
	public static function sort(&$tags)
	{
		$tagValues = array();
		$tagSortOrders = array();
		$sortedTags = array();

		foreach ($tags as $Tag)
		{
			$tagValues[] = $Tag->getValue();
			$tagSortOrders[] = self::$TAG_TYPES[$Tag->getType()]['sortOrder'];
			$sortedTags[] = $Tag;
		}

		// order by sortOrder, then tag value
		array_multisort($tagSortOrders, SORT_NUMERIC, $tagValues, SORT_ASC, $sortedTags);

		$tags = $sortedTags;
	}

	/**
	 * Build a new tag
	 * @param $value - initial tag text
	 * @return void
	 */
	public function __construct($value = null)
	{
		$this->setValue($value);
	}

	public function getId()			{ return $this->id; }
	public function getCount()		{ return $this->count; }

	// dummy method; required for IDekiApiObject
	public function getName()		{ return $this->getValue(); }

	/**
	 * Return the tag value as a string
	 * @param boolean $prefixed - include the tag prefix ('@', 'date:', etc.); default true
	 * @return string
	 */
	public function getValue($prefixed = true)
	{
		$type = $this->getType();

		// text has a dummy prefix we ignore
		if ($prefixed || ($type == self::TYPE_TEXT))
		{
			return $this->value;
		}
		else
		{
			// remove prefix
			$prefix = self::$TAG_TYPES[$type]['prefix'];
			$base = substr($this->value, strlen($prefix));
			return $base;
		}
	}

	public function getHref()		{ return $this->href; }
	public function getUri()
	{
		if ($this->getType() == self::TYPE_USER)
		{
			// TODO: better url generation for special pages (title dependency)
			$Title = Title::newFromText('Tags', NS_SPECIAL);
			$uri = $Title->getLocalURL('tag=' . $this->getValue());
			return $uri;
		}

		return $this->uri;
	}

	/**
	 * @return string
	 */
	public function getDate()
	{
		// Just Y-m-d, no "date:" prefix
		$date = $this->formatDateTag(false);
		return $date;
	}

	public function getTitle()
	{
		return !empty($this->title) ? $this->title : $this->getValue();
	}

	public function getType()
	{
		if (!empty($this->type))
		{
			return $this->type;
		}

		// lookup type based on prefix; default to text
		$value = $this->value;
		$type = self::TYPE_TEXT;

		foreach (self::$TAG_TYPES as $tagType)
		{
			$prefix = $tagType['prefix'];
			if (strncmp($value, $prefix, strlen($prefix)) == 0)
			{
				// new type found
				$type = $tagType['type'];
				break;
			}
		}

		$this->setType($type);

		return $this->type;
	}

	public function setValue($value)
	{
		$this->value = self::formatTag($value);

		// try to parse out date tags
		if ($this->getType() == self::TYPE_DATE)
		{
			$date = self::formatDateTag($this->value);
			if (!is_null($date))
			{
				// valid date tag; invalid dates stored as plain text tags in api
				$this->value = $date;
			}
		}
	}

	public function toArray()
	{
		$tagData = array(
			'@value' => $this->getValue()
		);

		return $tagData;
	}

	public function toXml()
	{
		return encode_xml($this->toArray());
	}

	public function toHtml()
	{
		return htmlspecialchars($this->getTitle());
	}

	/**
	 * Return tag + type pretty printed: January 1, 2010 (date)
	 * @return string
	 */
	public function toDetailedHtml()
	{
		$text = '';
		$text .= $this->toHtml();

		if ($this->getType() != self::TYPE_TEXT)
		{
			$text .= ' (' . $this->getType() . ')';
		}

		return $text;
	}

	/**
	 * Convert array of page arrays (from api) to pages
	 * @param array $pages - input array of pages from result object
	 * @return DekiPageInfo[]
	 */
	protected static function getPagesList($pages)
	{
		$pagesList = array();
		if (is_array($pages))
		{
			foreach ($pages as $page)
			{
				$PageInfo =	DekiPageInfo::newFromArray($page);

				if (is_object($PageInfo))
				{
					$pagesList[] = $PageInfo;
				}
			}
		}
		return $pagesList;
	}

	/**
	 * Attempt to load tag from numeric id or name
	 * @param $id - id of tag
	 * @param boolean $fromName - if true, id is a tag name ("test") to load
	 * @return DekiTag - Tag, or null if not found
	 */
	protected static function load($id, $fromName = false)
	{
		$Tag = null;

		if ($fromName)
		{
			$id = self::formatTag($id);
		}

		// lookup either /site/tags/=tagName or /site/tags/id
		$Plug = DekiPlug::getInstance()->At('site', 'tags')->At(($fromName ? '=' : '') . $id);
		$Result = $Plug->Get();

		if ($Result->isSuccess())
		{
			$result = $Result->getVal('body/tag');
			$Tag = self::newFromArray($result);
		}

		return $Tag;
	}

	/**
	 * Populate $Tag with data from $result
	 * @param DekiTag $Tag - reference to empty tag object
	 * @param array $result - array of tag data
	 */
	protected static function populateObject(&$Tag, &$result)
	{
		$Result = new XArray($result);

		$Tag->id = $Result->getVal('@id');
		$Tag->count = $Result->getVal('@count');
		$Tag->value = $Result->getVal('@value');
		$Tag->href = $Result->getVal('@href');
		$Tag->uri = $Result->getVal('uri', '');
		$Tag->type = $Result->getVal('type');
		$Tag->title = $Result->getVal('title');
		$Tag->pages = $Result->getVal('pages');
	}

	/**
	 * Convert list of tags from DekiResult to array of DekiTag indexed by id
	 * @param DekiResult $Result
	 * @return DekiTag[] - DekiTag objects, indexed by id
	 */
	protected static function getListFromResult(DekiResult $Result)
	{
		$siteTags = array();

		if ($Result->isSuccess())
		{
			$tags = $Result->getAll('body/tags/tag');

			if (is_array($tags))
			{
				foreach ($tags as $tag)
				{
					$Tag = self::newFromArray($tag);
					$siteTags[$Tag->getId()] = $Tag;
				}
			}
		}

		return $siteTags;
	}

	/**
	 * Get list of pages with this tag
	 * @param string $language - language to filter on
	 * @return DekiPageInfo[]
	 */
	protected function getPages($language = null)
	{
		$pages = array();

		if (empty($language))
		{
			// use existing results
			$Result = new DekiResult($this->pages);
			$pages = $Result->getAll('page');
		}
		else
		{
			// language filter; re-query because existing page results aren't tagged with language
			$Plug = DekiPlug::getInstance();
			$Plug = $Plug->At('site', 'tags', '='. $this->getValue())->With('language', $language);
			$Result = $Plug->Get();
			if ($Result->handleResponse())
			{
				$pages = $Result->getAll('body/tag/pages/page', array());
			}
		}

		return self::getPagesList($pages);
	}

	protected function setName($name)	{ $this->setValue($name); }
	protected function setTitle($title)	{ $this->title = $title; }
	protected function setHref($href)	{ $this->href = $href; }
	protected function setUri($uri)		{ $this->uri = $uri; }
	protected function setPages($pages)	{ $this->pages = (array) $pages; }

	protected function setType($type)
	{
		$this->type = isset(self::$TAG_TYPES[$type]) ? $type : null;
	}

	/**
	 * Normalizes tag text (lowercase, trimmed, etc.) from raw string
	 * @param $text - input text
	 * @return string - formatted tag text
	 */
	private static function formatTag($text)
	{
		return strtolower(trim($text));
	}

	/**
	 * Convert candidate date tag ("date:10-10-2010", "date:today") to a date string (Y-m-d)
	 * @param string $tagValue - text value to convert ("date:today")
	 * @param bool $includePrefix - if true, return with "date:" prefix
	 * @return string - date string, or null if no conversion possible
	 */
	private static function formatDateTag($tagValue, $includePrefix = true)
	{
		$dateprefix = self::$TAG_TYPES[self::TYPE_DATE]['prefix'];
		$date = substr($tagValue, strlen($dateprefix));
		$timestamp = empty($date) ? mktime() : strtotime($date);

		if ($timestamp > -1)
		{
			$ret = date('Y-m-d', $timestamp);
			return $includePrefix ? $dateprefix . $ret : $ret;
		}

		return null;
	}

	/**
	 * Compare two related pages to see which is ordered first
	 * @param DekiPageInfo $first
	 * @param DekiPageInfo $second
	 * @return int
	 */
	private static function compareRelatedPages($first, $second)
	{
		return strcasecmp($first->title, $second->title);
	}
}
