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
 
class DekiLoggedSearch
{
	/**
	 * Hook allows the query & constraints to be customized by a plugin
	 * @param string &$query
	 * @param string &$constraints
	 */
	const HOOK_DATA_PROCESS_QUERY = 'Data:Search:ProcessQuery';
	
	// @var string - string input by the user
	protected $query = null;
	// @var int - id for the latest executed query
	protected $queryId = null;
	// @var int - id for the last query executed
	protected $incomingQueryId = null;
	// @var bool - determines incomingQueryId handling
	protected $isRequery = false;

	// supported constraints
	protected $language = null;
	protected $namespace = null;
	protected $author = null;
	protected $type = null;
	protected $tags = array();
	protected $paths = array();
	protected $subpaths = array();

	// @var DekiLoggedSearchResult[]
	protected $results = null;
	// search result information
	protected $parsedQuery = null;
	protected $queryCount = null;

	protected $queryPage = null;
	protected $queryLimit = null;
	// computed total pages
	protected $totalPages = null;

	/**
	 * @param string $query - user specified query string
	 * @param int $queryId - API generated query id
	 */
	public function __construct($query, $queryId = null)
	{
		$this->query = $query;
		$this->incomingQueryId = $queryId;
	}

	public function getQuery() { return $this->query; }

	public function hasResults() { return $this->queryTotal > 0; }

	public function getQueryId() { return $this->queryId; }
	public function getQueryParsed() { return $this->parsedQuery; }

	public function getQueryStart() { return $this->queryOffset + 1; }
	public function getQueryEnd() { return $this->queryOffset + $this->queryCount; }
	public function getQueryTotal() { return $this->queryTotal; }

	public function getPage() { return $this->queryPage; }
	public function getPageTotal() { return ceil($this->queryTotal / $this->queryLimit); }

	public function &getSearchResults()
	{
		return $this->results;
	}

	// set constraints
	public function setLanguage($language) { $this->language = empty($language) ? null : $language; }
	public function setNamespace($namespace) { $this->namespace = empty($namespace) ? null : $namespace; }
	public function setType($type) { $this->type = empty($type) ? null : $type; }
	public function setAuthor($author) { $this->author = empty($author) ? null : $author; }
	public function addTags($tags) { $this->tags = empty($tags) ? array() : $tags; }
	public function addPath($path, $includeSubpages = false)
	{
		if (!is_null($path))
		{
			$this->paths[] = $path;
			if ($includeSubpages)
			{
				$this->subpaths[] = $path;
			}
		}
	}
	/**
	 * Fetches an updated result set for an existing query
	 * @see self::query()
	 * @return DekiResult
	 */
	public function requery($sort, $page = 1, $limit = 25)
	{
		$this->isRequery = true;
		return $this->query($sort, $page, $limit);
	}

	/**
	 * Performs a search query, generating a new query id
	 * @param string $sort
	 * @param int $page
	 * @param int $limit
	 * @return DekiResult
	 */
	public function query($sort = null, $page = 1, $limit = 25)
	{
		// save the page that was requested
		$this->queryPage = $page > 1 ? (int)$page : 1;
		$this->queryLimit = (int)$limit;
		$this->queryOffset = ($this->queryPage - 1) * $this->queryLimit;
		
		// page results
		$Plug = DekiPlug::getInstance()
			->At('site', 'query')
			->With('limit', $this->queryLimit)
			->With('offset', $this->queryOffset)
		;
		
		// preprocess the query and constraints
		$query = $this->query;
		$constraints = $this->getConstraints();
		
		// fire hook to allow query & constraint updates
		if (class_exists('DekiPlugin'))
		{
			DekiPlugin::executeHook(self::HOOK_DATA_PROCESS_QUERY, array(&$query, &$constraints));
		}
		
		// add the query
		$Plug = $Plug->With('q', $query);
		
		// add the constraints
		if (!empty($constraints))
		{
			$Plug = $Plug->With('constraint', $constraints);
		}
		
		if (!is_null($sort))
		{
			$Plug = $Plug->With('sortby', $sort);
		}
		
		if (!is_null($this->incomingQueryId))
		{
			// if page is not provided, then it's a new search
			$queryIdType = $this->isRequery ? 'queryid' : 'previousqueryid';

			$Plug = $Plug->With($queryIdType, $this->incomingQueryId);
		}
		
		// execute the query
		$Result = $Plug->Get();
		
		if (!$Result->isSuccess())
		{
			return $Result;
		}

		// set additional query meta information
		$this->parsedQuery = $Result->getVal('body/search/parsedQuery');

		$this->queryCount = $Result->getVal('body/search/@count', 0);
		$this->queryTotal = $Result->getVal('body/search/@querycount');
		// logged search specific
		$this->queryId = $Result->getVal('body/search/@queryid');

		// compute total pages
		$this->totalPages = floor($this->queryCount / $limit);

		$this->results = array();
		$searchResults = $Result->getAll('body/search/result', array());
		foreach ($searchResults as &$result)
		{
			$this->results[] = DekiLoggedSearchResult::newFromArray($result);
		}

		return $Result;
	}
	
	/**
	 * Generate the constraints for the query
	 * @return string
	 */
	public function getConstraints()
	{
		$constraints = array();

		if (!is_null($this->language))
		{
			$constraints[] = '(language:' . self::escape($this->language) . ' language:neutral)';
		}

		if ($this->namespace)
		{
			$constraints[] = '+namespace:' . self::escape($this->namespace);
		}
		
		if (!empty($this->paths) || !empty($this->subpaths))
		{
			$paths = array();
			foreach ($this->paths as $path)
			{
				$paths[] = 'path:' . self::escape($path);
			}
			
			foreach ($this->subpaths as $path)
			{
				$paths[] = 'path:' . self::escape($path) . '/*';
			}

			$constraints[] = '(' . implode(' OR ', $paths) . ')';
		}

		if (!empty($this->type))
		{
			$constraints[] = '+type:' . self::escape($this->type);
		}

		if (!empty($this->author))
		{
			$constraints[] = '+author:' . self::escape($this->author);
		}

		if (!empty($this->tags))
		{
			$tags = array();
			foreach($this->tags as $tag)
			{
				$tags[] = '+tag:' . self::escape($tag);
			}
			$constraints[] = implode(' ', $tags);
		}

		return implode(' AND ', $constraints);
	}
	
	/**
	 * Safely escape the provided terms for use in the query
	 * @param string $terms
	 * @return string
	 */
	public static function escape($terms)
	{
		$search = array(
			'\\',
			'"',
			':',
			'+',
			'-',
			'&&',
			'||',
			'!',
			'(',
			')',
			'{',
			'}',
			'[',
			']',
			'^',
			'"',
			'~',
			'*',
			'?'
		);
		$replace = array(
			'\\',
			'\\"',
			'\:',
			'\+',
			'\-',
			'\&&',
			'\||',
			'\!',
			'\(',
			'\)',
			'\{',
			'\}',
			'\[',
			'\]',
			'\^',
			'\~',
			'\*',
			'\?'
		);

		return str_replace($search, $replace, $terms);
	}
}