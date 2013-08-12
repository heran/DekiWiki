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
class DekiLoggedSearchBuilder
{
	/**
	 * @var array
	 */
	protected $andTerms = array();
	protected $orTerms = array();
	protected $notTerms = array();
	protected $tags = array();

	// prefix, relevance
	protected $termModifiers = array(
		'content'     => '',
		'title'       => '10.0',
		'path.title'  => '4.0',
		'description' => '3.0'
	);

	/**
	 * @var string
	 */
	protected $author = null;
	protected $language = null;
	protected $type = null;
	protected $termModifier = null;

	/**
	 * @var int
	 */
	protected $queryId;

	/**
	 * All these words
	 *
	 * @param mixed $terms
	 * @return void
	 */
	public function hasAllTerms($terms = array())
	{
		if (!is_array($terms))
		{
			$terms = $this->splitTerms($terms);
		}
		foreach ($terms as $term)
		{
			$this->andTerms[] = $term;
		}
	}

	/**
	 * This exact phrase
	 *
	 * @param string $term
	 * @return void
	 */
	public function hasExactTerm($term = null)
	{
		if(!empty($term))
		{
			$this->andTerms[] = $term;
		}
	}

	/**
	 * Any of these words
	 *
	 * @param array|string $terms
	 * @return void
	 */
	public function hasAnyTerms($terms = array())
	{
		if (!is_array($terms))
		{
			$terms = $this->splitTerms($terms);
		}
		foreach ($terms as $term)
		{
			$this->orTerms[] = $term;
		}
	}


	/**
	 * Been tagged with
	 *
	 * @param array|string $tags
	 * @return void
	 */
	public function hasTags($tags = array())
	{
		if (!is_array($tags))
		{
			$tags = $this->splitTerms($tags);
		}
		foreach ($tags as $tag)
		{
			$this->tags[] = $tag;
		}
	}

	/**
	 * Do not have these words
	 *
	 * @param array|string $terms
	 * @return void
	 */
	public function doesNotHaveTerms($terms = array())
	{
		if (!is_array($terms))
		{
			$terms = $this->splitTerms($terms);
		}
		foreach ($terms as $term)
		{
			$this->notTerms[] = $term;
		}
	}

	/**
	 * @param mixed queryId
	 * @return void
	 */
	public function hasQueryId($queryId = null)
	{
		if(!empty($queryId))
		{
			$this->queryId = !is_int($queryId) ? intval($queryId) : $queryId;
		}
	}

	/**
	 * Authored by
	 *
	 * @param string $author
	 * @return void
	 */
	public function isAuthoredBy($author = null)
	{
		if(!empty($author))
		{
			$this->author = $author;
		}
	}

	/**
	 * @param string $language
	 * @return void
	 */
	public function isLanguage($language = null)
	{
		if(!empty($language))
		{
			$this->language = $language;
		}
	}

	/**
	 * @param string $namespace
	 * @return void
	 */
	public function inNamespace($namespace = null)
	{
		if(!empty($namespace))
		{
			$this->namespace = $namespace;
		}
	}

	/**
	 * @param string $type
	 * @return void
	 */
	public function isType($type = null)
	{
		if(!is_null($type))
		{
			$this->type = $type;
			switch ($type)
			{
				case 'image':
				case 'wiki':
				case 'document':
					break;
				case 'comment':
					$this->termModifier = 'comments';
					break;
			}
			if(!empty($this->termModifier))
			{
				$this->termModifier .= ':';
			}
		}
	}

	/**
	 * Build search query and return search object
	 * @return DekiLoggedSearch
	 */
	public function getSearch()
	{
		$query = null;
		$query .= $this->buildSubQuery($this->andTerms, 'AND');
		$query .= $this->buildSubQuery($this->orTerms, 'OR');
		foreach ($this->notTerms as $term)
		{
			$query .= ' -' . $this->termModifier . '"' . DekiLoggedSearch::escape($term) . '"';
		}
		$Search = new DekiLoggedSearch(trim($query), empty($this->queryId) ? null : $this->queryId);
		$Search->setLanguage($this->language);
		$Search->setType($this->type);
		$Search->setAuthor($this->author);
		$Search->addtags($this->tags);
		return $Search;
	}

	/**
	 * @param string $terms
	 * @return array
	 */
	protected static function splitTerms($termString)
	{
		// If no split was specified try to use comma, if no
		// commas are present use space.
		$splitString = substr_count($termString, ',') > 0 ? ',' : ' ';
		$terms = explode($splitString, $termString);
		foreach ($terms as $key => $term)
		{
			$term = trim ($term);
			if(empty($term))
			{
				unset($terms[$key]);
			}
		}
		return $terms;
	}
	
	/**
	 * @param array $terms
	 * @param string $conjunction
	 * @return string
	 */
	protected function buildSubQuery($terms, $conjunction)
	{
		$queries = array();
		foreach ($terms as $term)
		{
			if (!empty($this->termModifier))
			{
				$queries[] = $this->termModifier . '"' . DekiLoggedSearch::escape($term) . '"';
			}
			else
			{
				// boost more relevant queries
				$subQueries = array();
				foreach($this->termModifiers as $modifier => $boost)
				{
					$q = $modifier . ':"' . DekiLoggedSearch::escape($term) . '"';
					if(!empty($boost))
					{
						$q .= '^' . $boost;
					}
					$subQueries[] = $q;
				}
				$subQuery = implode($subQueries, ' OR ');
				if(!empty($subQuery))
				{
					$queries[] = '(' . $subQuery . ')';
				}
			}
		}
		$query = implode($queries, ' ' . $conjunction . ' ');
		return !empty($query) ? '(' . $query . ')' : null;
	}
}