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
 * Interface all API objects need to implement
 * @TODO: update interface according to actual use cases
 */
interface IDekiApiObject
{
	/**
	 * @param array &$result - reference to an API result array section/stub
	 * @return &Object - reference to a new API object
	 */
	static function newFromArray(&$result);
	static function newFromId($id);
	static function newFromText($text);

	public function getId();
	public function getName();

	public function toArray();
	public function toXml();
	/*
	 * Renders the object to semantic html
	 */
	public function toHtml();
}
