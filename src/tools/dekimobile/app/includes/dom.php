<?php
/*
 * MindTouch Deki - enterprise collaboration and integration platform
 *  derived from MediaWiki (www.mediawiki.org)
 * Copyright (C) 2006 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
 *  please review the licensing section.
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

/*
 * DwDomElement
 * Used to create standalone dom based elements
 */
class DwDomElement
{
	protected $tag = '';
	protected $nodeValue = null;

	protected $children = array();
	protected $attributes = array();

	public function DwDomElement($tag, $value = null)
	{
		$this->tag = $tag;
		$this->nodeValue = $value;
	}

	public function &setAttribute($name, $value = null)
	{
		if (is_null($value))
		{
			unset($this->attributes[$name]);
		}
		else
		{
			$this->attributes[$name] = $value;
		}

		return $this;
	}
	
	public function getAttribute($name, $default = null)
	{
		return isset($this->attributes[$name]) ? $this->attributes[$name] : $default;
	}

	public function &addClass($class)
	{
		$classes = $this->getAttribute('class', '');
		$classArray = explode(' ', $classes);
		
		if (!in_array($class, $classArray))
		{
			$this->setAttribute('class', trim($classes . ' ' . $class));
		}

		return $this;
	}

	public function &removeClass($class)
	{
		$classes = $this->getAttribute('class', '');
		$classArray = explode(' ', $classes);
		
		$index = array_search($class, $classArray);
		if ($index !== false)
		{
			unset($classArray[$index]);
			$this->setAttribute('class', trim(implode(' ', $classArray)));
		}

		return $this;
	}

	public function innerHtml($html)
	{
		$this->nodeValue = $html;
	}

	public function innerText($text)
	{
		$this->nodeValue = htmlspecialchars($text);
	}

	public function appendChild(DwDomElement &$Node)
	{
		$this->children[] = $Node;
	}

	public function createElement($tag, $value = null)
	{
		return new DwDomElement($tag, $value);
	}

	public function &saveHtml()
	{
		// generate the attributes string
		$attributeHtml = '';
		foreach ($this->attributes as $name => $value)
		{
			if (!is_null($value))
			{
				$attributeHtml .= sprintf(' %s="%s"', $name, $value);
			}
		}
		
		// generate the children's html
		$childrenHtml = '';
		foreach ($this->children as &$Node)
		{
			$childrenHtml .= $Node->saveHtml();
		}
		unset($Node);

		$html = sprintf('<%s%s>%s%s</%s>', $this->tag, $attributeHtml, $childrenHtml, $this->nodeValue, $this->tag);

		return $html;
	}

}

class DomFragment extends DwDomElement
{
	public function DomFragment()
	{
		parent::__construct('');
	}
	
	public function &setAttribute($name, $value = null)
	{
		return $this;
	}

	public function saveHtml()
	{
		$html = parent::saveHtml();

		return substr($html, 2, -3); // <></>
	}
}

class DomTable extends DwDomElement
{
	private $ColGroup = null;
	private $TBody = null;
	private $CurrentRow = null;

	/*
	 * These variables are for assigning row & column styles
	 */
	private $rowCount = 0;
	private $colCount = 0;

	private $rowClasses = array();
	private $rowClassCount = 0;
	
	private $colClasses = array();
	private $colClassCount = 0;


	public function DomTable()
	{
		parent::__construct('table');

		$this->ColGroup = $this->createElement('colgroup');
		$this->appendChild($this->ColGroup);

		$this->TBody = $this->createElement('tbody');
		$this->appendChild($this->TBody);

		$this->addClass('table');
		$this->setAttribute('width', '100%');
		$this->setAttribute('cellspacing', 0);
		$this->setAttribute('cellpadding', 0);
		$this->setAttribute('border', 0);

		$this->setRowClasses('bg1', 'bg2');
	}

	/*
	 * Column & row preferences, need to be setup before working on the table
	 */
	public function setColWidths(/* widths[] */)
	{
		// remove any of the current column groups
		$this->ColGroup->nodeValue = '';

		$args = func_get_args();
		for ($i = 0, $i_m = func_num_args(); $i < $i_m; $i++)
		{
			$Col = $this->createElement('col');
			$Col->setAttribute('width', $args[$i]);
			$this->ColGroup->appendChild($Col);
		}
	}
	
	/*
	 * @example $Table->setRowClasses('row1', 'row2'); // table with 2 alternating row classes
	 */
	public function setRowClasses(/* classes[] */)
	{
		$this->rowClasses = func_get_args();
		$this->rowClassCount = count($this->rowClasses);
	}

	/*
	 * @example $Table->setColClasses('col1', 'col2', 'col3'); // table with 3 alternating column classes
	 */
	public function setColClasses(/* classes[] */)
	{
		$this->colClasses = func_get_args();
		$this->colClassCount = count($this->colClasses);
	}

	/*
	 * Column & row creation functions
	 */
	public function &addHeading($html, $colspan = 1)
	{
		return $this->addCol($html, $colspan, true);
	}

	public function &addCol($html, $colspan = 1, $asheader = false)
	{
		$Cell = $this->createElement($asheader ? 'th': 'td');
		$this->setColClass($Cell);
		if ($colspan > 1)
		{
			$Cell->setAttribute('colspan', $colspan);
			$this->colCount = $this->colCount + $colspan;
		}
		else {
			$this->colCount++;
		}
		
		//always fill the cell with something
		if (empty($html) && $html != '0')
		{
			$html = '&nbsp;';
		}
		$Cell->innerHtml($html);
		$this->CurrentRow->appendChild($Cell);

		return $Cell;
	}

	public function &addRow($setClass = true)
	{
		$Row = $this->createElement('tr');
		if ($setClass)
		{
			$this->setRowClass($Row);
			$this->rowCount++;
		}
		$this->TBody->appendChild($Row);
		$this->CurrentRow = &$Row;

		$this->colCount = 0;

		return $this->CurrentRow;
	}


	/*
	 * No touchy. Private methods for creating classed elements
	 */
	private function setColClass(DwDomElement &$Cell)
	{
		if ($this->colClassCount > 0)
		{
			$Cell->setAttribute('class', $this->colClasses[$this->colCount % $this->colClassCount]);
		}
		else
		{
			// by default we add an incremental col class, e.g. col1, col2, col3...
			$Cell->setAttribute('class', 'col' . ($this->colCount + 1));
		}
	}

	private function setRowClass(DwDomElement &$Row)
	{
		if ($this->rowClassCount > 0)
		{
			$Row->setAttribute('class', $this->rowClasses[$this->rowCount % $this->rowClassCount]);
		}
	}
}


class DomPagination extends DwDomElement
{
	/**
	 * @param string $baseHref An href that can have the page # appended to it
	 *				 e.g. http://www.site.com/something?foo=bar&page=
	 * @param int $currentPage The current page number
	 * @param int $totalPages ...
	 */
	public function DomPagination($baseHref, $currentPage, $totalPages)
	{
		parent::__construct('div');
		
		$this->addClass('pagination');

		$Element = $this->createElement('span');
		$Element->setAttribute('class', 'prev');
		$url = $baseHref . ($currentPage - 1);
		$html = $currentPage <= 1 ? wfMsg('System.Common.nav-prev') : sprintf('<a href="%s">&nbsp;%s</a>', $url, wfMsg('System.Common.nav-prev'));
		$Element->innerHtml($html);
		$this->appendChild($Element);

		$Element = $this->createElement('span', wfMsg('System.Common.nav-info', $currentPage, $totalPages));
		$Element->setAttribute('class', 'info');
		$this->appendChild($Element);

		$Element = $this->createElement('span');
		$Element->setAttribute('class', 'next');
		$url = $baseHref . ($currentPage + 1);
		$html = $currentPage >= $totalPages ? wfMsg('System.Common.nav-next') : sprintf('<a href="%s">%s&nbsp;</a>', $url, wfMsg('System.Common.nav-next'));
		$Element->innerHtml($html);
		$this->appendChild($Element);
	}
}

class DomSortTable extends DomTable
{
	const SORT_ASC = 'asc';
	const SORT_DESC = 'desc';

	// determine the get variables to use for generating the url
	private $getSortField = 'sortby';
	private $getSortMethod = 'sort';

	private $baseHref = null;
	private $currentField = null;
	private $currentMethod = null;

	/**
	 * string $baseHref - used to create the sort heading links
	 * string $field - determines which field is being sorted currently
	 * enum $method - determines how the current field is sorted, ASC/DESC
	 */
	public function __construct($baseHref, $field, $method)
	{
		parent::__construct();

		$this->baseHref = $baseHref;
		$this->currentField = $field;
		$this->currentMethod = $method;
	}
	
	/*
	 * Changes the GET param key from the defaults 'sortby' & 'sort'
	 */
	public function setGetKeys($getSortField, $getSortMethod)
	{
		$this->getSortField = $getSortField;
		$this->getSortMethod = $getSortMethod;
	}

	public function addSortHeading($title, $field, $colspan = 1)
	{
		// create the heading url
		$href = $this->baseHref;
		if (substr($href, -1) != '?')
		{
			$href .= '&';
		}
		$href .= $this->getSortField .'='. urlencode($field);
		$href .= '&' . $this->getSortMethod .'=';
		$sortMethod = self::SORT_ASC;
		if ($this->currentField == $field)
		{
			$sortMethod = $this->currentMethod == self::SORT_ASC ? self::SORT_DESC : self::SORT_ASC;
		}
		$href .= $sortMethod;
		
		// generate a link with class sort-asc or sort-desc
		$html = '<a href="'. $href .'" class="sort-'. $sortMethod .'"><span/>' . $title . '</a>';

		return parent::addHeading($html, $colspan);
	}
}

?>
