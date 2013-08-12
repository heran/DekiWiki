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

class DekiPageRating
{	
	const REQUIRES_LOGIN = 'login';
	const REQUIRES_COMMERCIAL = 'commercial';
	
	const RATING_DOWN = 0;
	const RATING_UP = 1;
	
	// returned properties from the rating object
	protected $score;
	protected $count;
	
	// @note kalida: null and 0 are easily casted
	protected $userRating = -1;

	/**
	 * Create the rating object for a given page
	 * @param int $pageId - page to load
	 * @param DekiPageRating - object to save to
	 * @return mixed - true, or DekiResult if error
	 */
	public static function loadPageRating($pageId, &$Rating)
	{
		$Rating = null;
		$Plug = DekiPlug::getInstance()->At('pages', $pageId, 'ratings');
		$Result = $Plug->Get();

		if ($Result->isSuccess())
		{
			$result = $Result->getVal('body/rating');
			$Rating = self::newFromArray($result);
		}

		return $Result;
	}
	
	/**
	 * Load ratings from api result array
	 * @param array $result - api results
	 * @return DekiPageRating
	 */
	public static function newFromArray(&$result)
	{
		$Rating = new self();
		self::populateObject($Rating, $result);
		return $Rating;
	}
	
	/**
	 * Record a rating for the page. Updates local score.
	 * @param int $pageId - page to rate
	 * @param DekiPageRating $Rating - rating to set (set userRating property)
	 * @return DekiResult
	 */
	public static function savePageRating($pageId, &$Rating)
	{
		$Plug = DekiPlug::getInstance()->At('pages', $pageId, 'ratings');
		$Result = $Plug->With('score', $Rating->getUserRating())->Post();
		
		if ($Result->isSuccess())
		{
			$Rating = self::newFromArray($Result->getVal('body/rating'));
		}
		
		return $Result;
	}
	
	/**
	 * Determine whether the current user can rate a page
	 * @param DekiUser $User - user making request
	 * @param DekiPageInfo $PageInfo - page to rate
	 * @return mixed - true if can rate, otherwise string with error type ('login', 'commercial')
	 */
	public static function userCanRate($User, $PageInfo)
	{
		global $wgArticle;

		$License = DekiLicense::getCurrent();
		if (!$License->hasCapabilityRating())
		{
			return self::REQUIRES_COMMERCIAL;
		}
		
		// avoid article creation if we're rating $wgArticle
		$Article = ($wgArticle && $PageInfo->id == $wgArticle->getId()) ? $wgArticle : Article::newFromId($PageInfo->id);
			
		if (!is_null($Article) && $Article->userCanRead() && !$User->isAnonymous())
		{
			return true;
		}
		else
		{
			return self::REQUIRES_LOGIN;	
		}
	}
	
	protected static function populateObject(&$Rating, &$result)
	{
		$X = new XArray($result);

		$Rating->score = $X->getVal('@score', null);
		$Rating->count = $X->getVal('@count', 0);
		
		// @note kalida: rating for current user. Later versions may list scores for all users
		$Rating->userRating = $X->getVal('user.ratedby/@score', -1);
	}
	
	public function getScore()	{ return $this->score; }
	public function getCount()	{ return $this->count; }
	
	/**
	 * Return number of votes of a certain type (default: count upvotes)
	 * 
	 * @param bool $downvote - if true, count downvotes (default false)
	 * @return int
	 */
	public function getVoteCount($downvote = false)
	{
		$upvotes = round($this->count * $this->score);
		return $downvote ? $this->count - $upvotes : $upvotes;
	}
	
	public function getUserRating()	{ return $this->userRating; }
	
	public function setUserRating($rating) { $this->userRating = $rating; }
	
	public function toArray()
	{
		return array(
			'@count' => $this->getCount(),
			'@score' => $this->getScore(),
			'userRating' => $this->getUserRating()
		);
	}
	
}
