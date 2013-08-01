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

class SpecialSearch extends SpecialPagePlugin
{
	const RESULTS_PER_PAGE = 10;
	// language options
	const OPTION_LANG_ALL = 'all';
	// namespace filtering options
	const OPTION_NS_ALL = 'all';
	const OPTION_NS_MAIN = 'main';
	const OPTION_NS_USER = 'user';
	const OPTION_NS_TEMPLATE = 'template';
	const OPTION_SUBPAGES = 'subpages';

	const SKINVAR_NAMESPACES = 'search.namespaces';

	const DEFAULT_SORT = '-rank';
	const SORT_RANK = 'rank';
	const SORT_TITLE = 'title';
	const SORT_MODIFIED = 'modified';

	/**
	 * Set the default sort on a per field basis
	 * (empty): asc sorting
	 * (slash): desc sorting
	 * @var array
	 */
	protected static $defaultSorts = array(
		self::SORT_RANK => '-',
		self::SORT_TITLE => '',
		self::SORT_MODIFIED => '-'
	);

	/**
	 * @var array
	 */
	protected static $types = array(
		'' => 'any type',
		'wiki' => 'wiki pages',
		'document' => 'documents',
		'image' => 'images',
		'comment' => 'comments'
	);

	/**
	 * Stores the last queryId for use in the skin template
	 * @var int
	 */
	protected static $lastQueryId = null;

	/**
	 * @var DekiRequest
	 */
	protected $Request;

	protected $pageName = 'Search';
	// we are not executing out of the special folder
	protected $specialFolder = '';


	public static function init()
	{
		// inject additional skinning variables for the skins
		DekiPlugin::registerHook(Hooks::SKIN_OVERRIDE_VARIABLES, array(__CLASS__, 'skinHook'));

		DekiPlugin::registerHook(Hooks::SPECIAL_SEARCH, array(__CLASS__, 'specialHook'));
	}
	
	/**
	 * Inject new variables into the template
	 * @param SkinTemplate $Template
	 * @return
	 */
	public static function skinHook(&$Template)
	{
		global $wgRequest, $wgArticle;
		
		$PageInfo = $wgArticle->getInfo();
		$defaultPath = !is_null($PageInfo) ? $PageInfo->getPath(true) : '';
		
		// set the default select state
		$defaultNs = self::OPTION_NS_MAIN;
		
		// special case when you're in the template namespace - else we always utilize "main page" search
		if (!is_null($PageInfo) && $PageInfo->getNamespace() == DekiPageInfo::NS_TEMPLATE)
		{
			$defaultNs = self::OPTION_NS_TEMPLATE;	
		}
		$defaultNs = $wgRequest->getVal('ns', $defaultNs); 
		
		// preserving the state of the contextual subpage search path in the search page
		if ($wgRequest->getVal('path') && $defaultNs == self::OPTION_SUBPAGES) 
		{
			$defaultPath = $wgRequest->getVal('path');
		}
		
		// generate the namespaces input
		$Template->set(self::SKINVAR_NAMESPACES,
			(is_null(self::$lastQueryId) ? '' : DekiForm::singleInput('hidden', 'qid', self::$lastQueryId)) .
			DekiForm::multipleInput(
				'select',
				'ns',
				self::getSearchNamespaces(),
				$defaultNs,
				array('id' => '', 'class' => 'deki-search-namespaces')
			) . 
			DekiForm::singleInput('hidden', 'path', $defaultPath)
		);
	}

	public static function specialHook($pageName, &$pageTitle, &$html, &$subhtml)
	{
		$Special = new self($pageName, basename(__FILE__, '.php'));

		// set the page title
		$pageTitle = $Special->getPageTitle();
		$html = $Special->output();
	}

	public function output()
	{
		$this->Request = DekiRequest::getInstance();

		DekiPlugin::requirePhp('special_search', 'deki_logged_search_builder.php');
		DekiPlugin::requirePhp('special_search', 'deki_logged_search_result.php');
		DekiPlugin::requirePhp('special_search', 'deki_logged_search.php');

		$this->includeSpecialCss('search.css');
		$this->includeSpecialJavascript('search.js');

		// standard or advanced query exists?
		$query = $this->Request->getVal('search');
		foreach(array('all', 'exact', 'any', 'tags') as $param)
		{
			$value = $this->Request->getVal($param);
			if(!is_null($value))
			{
				$query .= $value;
			}
		}

		if (is_null($query) || (trim($query) == ''))
		{
			if (!is_null($query))
			{
				// user input an empty query
				DekiMessage::error(wfMsg('Page.Search.error.empty'));
			}
			return $this->form();
		}
		else
		{
			return $this->search();
		}
	}
	
	/**
	 * Renders the search form, standalone and reused view
	 * 
	 * @param int $queryId - id of the query being shown
	 * @param bool $renderFilters - render the search filters
	 * @param bool $renderToggle - render the advanced search toggle
	 * @param bool $renderAdvanced - render the advanced search form
	 * @return string
	 */
	protected function form($queryId = null, $renderFilters = false, $renderToggle = true, $renderAdvanced = true)
	{
		$View = $this->createView('form');

		$View->set('form.queryId', $queryId);
		/**
		 * Save the queryId for reuse
		 * @see self::skinHook()
		 */
		self::$lastQueryId = $queryId;

		// namespace options
		$View->setRef('form.namespaces', self::getSearchNamespaces());

		// default namespace
		$View->set('form.namespace', $this->Request->getVal('namespace', self::OPTION_NS_MAIN));
		
		// determines if the search filters should be rendered
		$View->set('form.filters', $renderFilters);

		$View->Set('form.advanced', $renderAdvanced);
		$View->set('form.advanced.toggle', $renderToggle);
		$view = $this->Request->getVal('view');
		$View->set('form.advanced.show', strtolower($view) == 'advanced');
		$View->set('form.options.type', self::$types);

		$License = DekiLicense::getCurrent();
		if ($License->hasCapabilitySearch())
		{
			$View->set('commercial', true);
		}
		$View->set('commecial.message', $License->displayCommercialMessaging());
		$View->set('commercial.url', ProductURL::ADAPTIVE_SEARCH);
		
		if (DekiLanguage::isSitePolyglot()) 
		{
			$View->set('form.language',  wfLanguageActive(''));
			$View->set('form.languages', wfAllowedLanguages(wfMsg('Form.language.filter.all'), self::OPTION_LANG_ALL));
		}


		$Title = $this->getTitle();
		$View->set('form.action', $Title->getFullURL());

		// sorting links
		$sortBy = $sort = '';
		$this->getCurrentSort($sortBy, $sort);

		if ($sortBy == self::SORT_RANK)
		{
			$View->set('sort.ranking', 'sort-'.($sort == '-' ? 'desc' : 'asc'));
		}
		$get = $this->getQueryParams($queryId, self::SORT_RANK);
		$View->set('href.sort.ranking', $Title->getFullUrl(http_build_query($get)));

		if ($sortBy == self::SORT_TITLE)
		{
			$View->set('sort.title', 'sort-'.($sort == '-' ? 'desc' : 'asc'));
		}
		$get = $this->getQueryParams($queryId, self::SORT_TITLE);
		$View->set('href.sort.title', $Title->getFullUrl(http_build_query($get)));

		if ($sortBy == self::SORT_MODIFIED)
		{
			$View->set('sort.modified', 'sort-'. ($sort == '-' ? 'desc' : 'asc'));
		}
		$get = $this->getQueryParams($queryId, self::SORT_MODIFIED);
		$View->set('href.sort.modified', $Title->getFullUrl(http_build_query($get)));
		
		return $View->render();
	}
	
	/**
	 * Renders the search results view
	 * 
	 * @return string
	 */
	protected function search()
	{
		global $wgUser;

		$query = $this->Request->getVal('search');
		$queryId = $this->Request->getInt('qid');
		$page = $this->Request->getVal('page', null);

		// default to the main namespace
		$namespace = $this->Request->getVal('ns', self::OPTION_NS_MAIN);
		$language = $this->Request->getVal('language');
		$path = $this->Request->getVal('path');

		// special case for the homepage since generating its path is tricksy
		if (empty($path) && $namespace == self::OPTION_SUBPAGES)
		{
			$namespace = self::OPTION_NS_MAIN;
		}

		// create a new search
		if(empty($query))
		{
			// build advanced search
			$Builder = new DekiLoggedSearchBuilder();
			$Builder->hasAllTerms($this->Request->getVal('all'));
			$Builder->hasExactTerm($this->Request->getVal('exact'));
			$Builder->hasAnyTerms($this->Request->getVal('any'));
			$Builder->hasTags($this->Request->getVal('tags'));
			$Builder->doesNotHaveTerms($this->Request->getVal('notwords'));
			$Builder->isAuthoredBy($this->Request->getVal('author'));
			$Builder->isType($this->Request->getVal('type'));
			$Builder->isLanguage($language);
			$Search = $Builder->getSearch();
			$query = $Search->getQuery();
		}
		else
		{
			$Search = new DekiLoggedSearch($query, $queryId);
			$Search->setLanguage($language);
		}

		// special namespace handling
		switch ($namespace)
		{
			default:
			case self::OPTION_NS_MAIN:
				$Search->setNamespace($namespace);
				break;

			case self::OPTION_NS_USER:
				$Search->setNamespace($namespace);
				$path = 'User:' . urlencode($wgUser->getUsername());

				// search the user page + subpages
				$Search->addPath($path, true);
				break;

			case self::OPTION_NS_TEMPLATE:
				$Search->setNamespace($namespace);
				break;

			case self::OPTION_SUBPAGES:
				$Search->setNamespace(self::OPTION_NS_MAIN);
				// search current page + subpages
				$Search->addPath($path, true);
				break;

			case self::OPTION_NS_ALL:
		}

		// use the sort helper to determine active sort
		$sortBy = $sort = '';
		$this->getCurrentSort($sortBy, $sort);
		$sort = $sort . $sortBy;

		// requery if the page or sort param is found
		$Result = !is_null($page) || !is_null($this->Request->getVal('sort')) ?
			$Search->requery($sort, $page, self::RESULTS_PER_PAGE) :
			$Search->query($sort, $page, self::RESULTS_PER_PAGE);
		if (!$Result->isSuccess())
		{
			// search failed
			return $this->searchError($query, $Result->getError());
		}


		// begin setting up the view
		$View = $this->createView('results');
		
		$searchResults = $Search->getSearchResults();
		$View->setRef('results', $searchResults);

		// results context
		$View->set('results.start', $Search->getQueryStart());
		$View->set('results.end', $Search->getQueryEnd());
		$View->set('results.count', $Search->getQueryTotal());

		// render the search form
		$View->setRef('form.header', $this->form($Search->getQueryId(), true));
		$footer = empty($searchResults)
			? $this->form($Search->getQueryId(), false, true, true)
			: $this->form($Search->getQueryId(), false, false, false);

		$View->setRef('form.footer', $footer);

		$View->set('results.parsedQuery', $Search->getQueryParsed());
		$View->set('results.query', $query);
		
		$License = DekiLicense::getCurrent();
		if ($License->hasCapabilitySearch())
		{
			$View->set('commercial', true);
		}
		
		// rss href
		$constraints = $Search->getConstraints();
		$params = array('q' => $query);
		if (!empty($sort))
		{
			$params['sortby'] = $sort;
		}
		if (!empty($constraints))
		{
			$params['constraint'] = $constraints;
		}
		$View->set('href.subscribe', '/deki/gui/opensearch.php?'.http_build_query($params));

		if ($language != self::OPTION_LANG_ALL)
		{
			// are the results being filtered by language?
			$get = $this->getQueryParams($Search->getQueryId());
			$get['language'] = self::OPTION_LANG_ALL;

			$View->set('href.allLanguages', $this->getTitle()->getFullUrl(http_build_query($get)));
		}
		
		if ($namespace != self::OPTION_NS_ALL)
		{
			// are the results being filtered by language?
			$get = $this->getQueryParams($Search->getQueryId());
			$get['ns'] = self::OPTION_NS_ALL;
			$View->set('href.allNamespaces', $this->getTitle()->getFullUrl(http_build_query($get)));
		}

		// pagination
		$get = $this->getQueryParams($Search->getQueryId());
		// remove the current page param
		unset($get['page']);
		$baseHref = $this->getTitle()->getFullUrl(http_build_query($get));

		$Pagination = new DomListingPagination(
			$baseHref,
			$Search->getPageTotal(),
			'page'
		);
		$View->set('pagination', $Pagination->saveHtml());

		return $View->render();
	}
	
	/**
	 * Renders the error view
	 * 
	 * @param string $query - query that caused the error
	 * @param string $message - error message generated by the API
	 * @return html
	 */
	protected function searchError($query, $message)
	{
		// begin setting up the view
		$View = $this->createView('error');
		
		DekiMessage::error(wfMsg('Page.Search.error.query'));

		$View->set('error.query', $query);
		$View->set('error.message', $message);
		$View->setRef('form', $this->form());

		return $View->render();
	}

	/**
	 * Sorting helper
	 * @param string &$sortBy - name of the current field being sorted
	 * @param string &$sort - direction of the current sort
	 */
	protected function getCurrentSort(&$sortBy, &$sort)
	{
		// default to asc
		$sort = '';
		$sortBy = $this->Request->getVal('sort');

		if (empty($sortBy))
		{
			$sortBy = self::DEFAULT_SORT;
		}

		if (strncmp($sortBy, '-', 1) == 0)
		{
			$sort = '-';
			$sortBy = substr($sortBy, 1);
		}	
	}

	/**
	 * Create an array with all the current query params for url generation
	 * 
	 * @param int $queryId - current queryId
	 * @param string $sortField - name of the current field being sorted
	 * @return array
	 */
	protected function getQueryParams($queryId, $sortField = null)
	{
		// preserve the current values
		$params = array(
			'search',
			'ns',
			'language',
			'sort',
			'qid',
			'page',
			'all',
			'any',
			'exact',
			'tags',
			'notwords',
			'author',
			'type',
			'view'
		);
		$get = array();
		foreach($params as $param)
		{
			$get[$param] = $this->Request->getVal($param);
		}
		array_filter($get, 'strlen');

		if (!is_null($sortField))
		{
			$sortBy = $sort = '';
			$this->getCurrentSort($sortBy, $sort);
			
			// set the default field sort
			$sort = self::$defaultSorts[$sortField];
			
			$get['sort'] = $sort . $sortField;
		}

		if ($queryId)
		{
			$get['qid'] = $queryId;
		}

		return $get;
	}

	/**
	 * View generation helper
	 * @TODO guerrics: consider moving to plugin core or special plugin core
	 * 
	 * @param string $name
	 * @return DekiPluginView
	 */
	protected function createView($name)
	{
		$viewRoot = null;

		if (!empty($this->specialFolder))
		{
			$viewRoot = $this->specialFolder .'/'. $this->fileName;
		}
		else
		{
			$viewRoot = $this->fileName;
		}

		return new DekiPluginView($viewRoot, $name);
	}
	
	/**
	 * Retrieve the search namespace option array
	 * 
	 * @return array
	 */
	protected function getSearchNamespaces()
	{
		global $wgUser, $wgTitle, $wgRequest;
		$License = DekiLicense::getCurrent(); 
		
		$namespaces = array(
			self::OPTION_NS_ALL => wfMsg('Page.Search.form.namespaces.all'),
			self::OPTION_NS_MAIN => wfMsg('Page.Search.form.namespaces.main')
		);
		
		// if we're in the main namespace and it's a commercially enabled feature, add subpage search
		if ($License->hasCapabilitySearch())
		{
			// we only want to show the option for search subpages if you came in through that search subpage
			// request; otherwise it should suppress that option
			if ($wgTitle->getNamespace() == NS_MAIN 
				|| ($wgTitle->getNamespace() == NS_SPECIAL && $wgRequest->getVal('ns') == self::OPTION_SUBPAGES))
			{
				$namespaces[self::OPTION_SUBPAGES] = wfMsg('Page.Search.form.subpages');
			}
		}
		
		// if this is a logged-in user
		if (!$wgUser->isAnonymous())
		{
			$namespaces[self::OPTION_NS_USER] = wfMsg('Page.Search.form.namespaces.user');
		}
		
		// add the template namespace when you're in template: or the search page
		if ($wgTitle->getNamespace() == NS_TEMPLATE || $wgTitle->getNamespace() == NS_SPECIAL)
		{
			$namespaces[self::OPTION_NS_TEMPLATE] = wfMsg('Page.Search.form.namespaces.template');
		}
		
		return $namespaces;
	}
}
// initialize the special page plugin
SpecialSearch::init();

endif;
