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
 * Helper class for fetching array values and retrieving xml converted arrays
 * @TODO: refactor to return references and not copies, easier on memory
 */
class XArray
{
	private $array = array();
	private $copy = false;

	/*
	 * @param array &$array - reference to the array to be accessed
	 * @param bool $copy - determines whether returned values are copies or references in the original array
	 */
	public function __construct(&$array, $copy = true)
	{
		$this->array = $array;
		// copy setting doesn't actually work yet
		$this->copy = $copy;
	}

	/***
	 * Given an array $array, will try to find $key, which is delimited by /
	 * if $key itself is an array of multiple values which has a key of '0', will return the first value
	 * this is useful for getting stuff back from the api and to avoid the "cannot use string offset as array" error, 
	 * see http://www.zend.com/forums/index.php?S=ab6bd42e992e7497c9b0ba4a33b01dd9&t=msg&th=1556
	 *
	 * @param string $key - the array path to return, i.e. /pages/content
	 * @param mixed $default - if the key is not found, this value will be returned
	 */
	public function getVal($key = '', $default = null)
	{
		$array = $this->array;

		if ($key == '') {
			return $array;
		}
		$keys = explode('/', $key);
		$count = count($keys);
		$i = 0;
		foreach ($keys as $k => $val) {
			$i++;
			if ($val == '') {
				continue;
			}
			if (isset($array[$val]) && !is_array($array[$val])) {
				// see bugfix 4974; this used to do an empty string check, but that leads to ambiguity between empty string and null
				if ((is_string($array[$val]) || is_int($array[$val])) && $i == $count) {
					 return $array[$val];
				}
				return $default; 
			}
			if (isset($array[$val])) {
				$array = $array[$val];
			}
			else {
				return $default;
			}
			if (is_array($array) && key($array) == '0') {
				$array = current($array);
			}
		}
		return $array;
	}

	public function getAll($key = '', $default = null)
	{
		$array = $this->array;

		if ($key == '') {
			return $array;
		}
		$keys = explode('/', $key);
		$count = count($keys);
		$i = 0;
		foreach ($keys as $val) {
			$i++;
			if ($val == '') {
				continue;
			}
			if (!isset($array[$val]) || !is_array($array[$val])) {
				return $default; 
			}
			$array = $array[$val]; 
			if ($i == $count) {
				if (key($array) != '0') {
					$array = array($array);
				}
			}
		}
		return $array;
	}
	
	// debugging function
	public function debug($exit = false)
	{
		if (!$exit && function_exists('fb'))
		{
			// firephp debugging enabled
			fb($this->array);
		}
		else
		{
			echo '<pre>';
			print_r($this->array);
			echo '</pre>';
		}

		if ($exit)
		{
			exit();
		}
	}

	public function toXml()
	{
		return encode_xml($this->array);
	}
	
	/**
	 * Accessor for the array
	 * @return array
	 */	
	public function toArray()
	{
		return $this->array;
	}
}
