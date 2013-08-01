<?php
require_once("KalturaClientBase.php");

class KalturaEntryStatus
{
	const ERROR_CONVERTING = -1;
	const IMPORT = 0;
	const PRECONVERT = 1;
	const READY = 2;
	const DELETED = 3;
	const PENDING = 4;
	const MODERATE = 5;
	const BLOCKED = 6;
}

class KalturaEntryType
{
	const MEDIA_CLIP = 1;
	const MIX = 2;
	const PLAYLIST = 5;
}

class KalturaLicenseType
{
	const UNKNOWN = -1;
	const NONE = 0;
	const CC25 = 1;
	const CC3 = 2;
}

class KalturaMediaType
{
	const VIDEO = 1;
	const IMAGE = 2;
	const AUDIO = 5;
}

class KalturaSourceType
{
	const FILE = 1;
	const WEBCAM = 2;
	const URL = 5;
	const SEARCH_PROVIDER = 6;
}

class KalturaSearchProviderType
{
	const FLICKR = 3;
	const YOUTUBE = 4;
	const MYSPACE = 7;
	const PHOTOBUCKET = 8;
	const JAMENDO = 9;
	const CCMIXTER = 10;
	const NYPL = 11;
	const CURRENT = 12;
	const MEDIA_COMMONS = 13;
	const KALTURA = 20;
	const KALTURA_USER_CLIPS = 21;
	const ARCHIVE_ORG = 22;
	const KALTURA_PARTNER = 23;
	const METACAFE = 24;
	const SEARCH_PROXY = 28;
}

class KalturaMediaEntry extends KalturaObjectBase
{
	/**
	 * Auto generated 10 characters alphanumeric string
	 *
	 * @var string
	 * @readonly
	 */
	public $id = null;

	/**
	 * Entry name
	 *
	 * @var string
	 */
	public $name = null;

	/**
	 * Entry description
	 *
	 * @var string
	 */
	public $description = null;

	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $partnerId = null;

	/**
	 * The ID of the user who is the owner of this entry 
	 *
	 * @var string
	 */
	public $userId = null;

	/**
	 * Entry tags
	 *
	 * @var string
	 */
	public $tags = null;

	/**
	 * Entry admin tags can be updated only by administrators and are not visible to the user
	 *
	 * @var string
	 */
	public $adminTags = null;

	/**
	 * 
	 *
	 * @var KalturaEntryStatus
	 * @readonly
	 */
	public $status = null;

	/**
	 * The type of the entry, this is auto filled by the derived entry object
	 *
	 * @var KalturaEntryType
	 * @readonly
	 */
	public $type = null;

	/**
	 * Entry creation date as Unix timestamp (In seconds)
	 *
	 * @var int
	 * @readonly
	 */
	public $createdAt = null;

	/**
	 * Calculated rank
	 *
	 * @var int
	 * @readonly
	 */
	public $rank = null;

	/**
	 * The total (sum) of all votes
	 *
	 * @var int
	 * @readonly
	 */
	public $totalRank = null;

	/**
	 * Number of votes
	 *
	 * @var int
	 * @readonly
	 */
	public $votes = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $groupId = null;

	/**
	 * Can be used to store various partner related data as a string 
	 *
	 * @var string
	 */
	public $partnerData = null;

	/**
	 * Download URL for the entry
	 *
	 * @var string
	 * @readonly
	 */
	public $downloadUrl = null;

	/**
	 * License type used for this entry
	 *
	 * @var KalturaLicenseType
	 */
	public $licenseType = null;

	/**
	 * Number of plays
	 *
	 * @var int
	 * @readonly
	 */
	public $plays = null;

	/**
	 * Number of views
	 *
	 * @var int
	 * @readonly
	 */
	public $views = null;

	/**
	 * The width in pixels
	 *
	 * @var int
	 * @readonly
	 */
	public $width = null;

	/**
	 * The height in pixels
	 *
	 * @var int
	 * @readonly
	 */
	public $height = null;

	/**
	 * Thumbnail URL
	 *
	 * @var string
	 * @readonly
	 */
	public $thumbnailUrl = null;

	/**
	 * The duration in seconds
	 *
	 * @var int
	 * @readonly
	 */
	public $duration = null;

	/**
	 * The media type of the entry
	 *
	 * @var KalturaMediaType
	 * @insertonly
	 */
	public $mediaType = null;

	/**
	 * Override the default conversion quality  
	 *
	 * @var string
	 * @insertonly
	 */
	public $conversionQuality = null;

	/**
	 * The source type of the entry 
	 *
	 * @var KalturaSourceType
	 * @readonly
	 */
	public $sourceType = null;

	/**
	 * The search provider type used to import this entry
	 *
	 * @var KalturaSearchProviderType
	 * @readonly
	 */
	public $searchProviderType = null;

	/**
	 * The ID of the media in the importing site
	 *
	 * @var string
	 * @readonly
	 */
	public $searchProviderId = null;

	/**
	 * The user name used for credits
	 *
	 * @var string
	 */
	public $creditUserName = null;

	/**
	 * The URL for credits
	 *
	 * @var string
	 */
	public $creditUrl = null;

	/**
	 * The media date extracted from EXIF data (For images) as Unix timestamp (In seconds)
	 *
	 * @var int
	 * @readonly
	 */
	public $mediaDate = null;

	/**
	 * The URL used for playback. This is not the download URL.
	 *
	 * @var string
	 * @readonly
	 */
	public $dataUrl = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "id", $this->id);
		$this->addIfNotNull($kparams, "name", $this->name);
		$this->addIfNotNull($kparams, "description", $this->description);
		$this->addIfNotNull($kparams, "partnerId", $this->partnerId);
		$this->addIfNotNull($kparams, "userId", $this->userId);
		$this->addIfNotNull($kparams, "tags", $this->tags);
		$this->addIfNotNull($kparams, "adminTags", $this->adminTags);
		$this->addIfNotNull($kparams, "status", $this->status);
		$this->addIfNotNull($kparams, "type", $this->type);
		$this->addIfNotNull($kparams, "createdAt", $this->createdAt);
		$this->addIfNotNull($kparams, "rank", $this->rank);
		$this->addIfNotNull($kparams, "totalRank", $this->totalRank);
		$this->addIfNotNull($kparams, "votes", $this->votes);
		$this->addIfNotNull($kparams, "groupId", $this->groupId);
		$this->addIfNotNull($kparams, "partnerData", $this->partnerData);
		$this->addIfNotNull($kparams, "downloadUrl", $this->downloadUrl);
		$this->addIfNotNull($kparams, "licenseType", $this->licenseType);
		$this->addIfNotNull($kparams, "plays", $this->plays);
		$this->addIfNotNull($kparams, "views", $this->views);
		$this->addIfNotNull($kparams, "width", $this->width);
		$this->addIfNotNull($kparams, "height", $this->height);
		$this->addIfNotNull($kparams, "thumbnailUrl", $this->thumbnailUrl);
		$this->addIfNotNull($kparams, "duration", $this->duration);
		$this->addIfNotNull($kparams, "mediaType", $this->mediaType);
		$this->addIfNotNull($kparams, "conversionQuality", $this->conversionQuality);
		$this->addIfNotNull($kparams, "sourceType", $this->sourceType);
		$this->addIfNotNull($kparams, "searchProviderType", $this->searchProviderType);
		$this->addIfNotNull($kparams, "searchProviderId", $this->searchProviderId);
		$this->addIfNotNull($kparams, "creditUserName", $this->creditUserName);
		$this->addIfNotNull($kparams, "creditUrl", $this->creditUrl);
		$this->addIfNotNull($kparams, "mediaDate", $this->mediaDate);
		$this->addIfNotNull($kparams, "dataUrl", $this->dataUrl);
		return $kparams;
	}
}

class KalturaSearchResult extends KalturaObjectBase
{
	/**
	 * 
	 *
	 * @var string
	 */
	public $keyWords = null;

	/**
	 * 
	 *
	 * @var KalturaSearchProviderType
	 */
	public $searchSource = null;

	/**
	 * 
	 *
	 * @var KalturaMediaType
	 */
	public $mediaType = null;

	/**
	 * Use this field to pass dynamic data for searching
	 * For example - if you set this field to "mymovies_$partner_id"
	 * The $partner_id will be automatically replcaed with your real partner Id
	 *
	 * @var string
	 */
	public $extraData = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $id = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $title = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $thumbUrl = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $description = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tags = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $url = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $sourceLink = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $credit = null;

	/**
	 * 
	 *
	 * @var KalturaLicenseType
	 */
	public $licenseType = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $flashPlaybackType = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "keyWords", $this->keyWords);
		$this->addIfNotNull($kparams, "searchSource", $this->searchSource);
		$this->addIfNotNull($kparams, "mediaType", $this->mediaType);
		$this->addIfNotNull($kparams, "extraData", $this->extraData);
		$this->addIfNotNull($kparams, "id", $this->id);
		$this->addIfNotNull($kparams, "title", $this->title);
		$this->addIfNotNull($kparams, "thumbUrl", $this->thumbUrl);
		$this->addIfNotNull($kparams, "description", $this->description);
		$this->addIfNotNull($kparams, "tags", $this->tags);
		$this->addIfNotNull($kparams, "url", $this->url);
		$this->addIfNotNull($kparams, "sourceLink", $this->sourceLink);
		$this->addIfNotNull($kparams, "credit", $this->credit);
		$this->addIfNotNull($kparams, "licenseType", $this->licenseType);
		$this->addIfNotNull($kparams, "flashPlaybackType", $this->flashPlaybackType);
		return $kparams;
	}
}

class KalturaMediaEntryFilter extends KalturaObjectBase
{
	/**
	 * 
	 *
	 * @var string
	 */
	public $orderBy = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $idEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $idIn = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $userIdEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $typeIn = null;

	/**
	 * 
	 *
	 * @var KalturaEntryStatus
	 */
	public $statusEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $statusIn = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $nameLike = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $nameMultiLikeOr = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $nameMultiLikeAnd = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $nameEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsLike = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsMultiLikeOr = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsMultiLikeAnd = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $adminTagsLike = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $adminTagsMultiLikeOr = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $adminTagsMultiLikeAnd = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $groupIdEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $createdAtGreaterThanEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $createdAtLessThenEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $updatedAtGreaterThanEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $updatedAtLessThenEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $modifiedAtGreaterThanEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $modifiedAtLessThenEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $partnerIdEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $partnerIdIn = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $moderationStatusEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $moderationStatusIn = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsAndNameMultiLikeOr = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsAndAdminTagsMultiLikeOr = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsAndAdminTagsAndNameMultiLikeOr = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsAndNameMultiLikeAnd = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsAndAdminTagsMultiLikeAnd = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsAndAdminTagsAndNameMultiLikeAnd = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $searchTextMatchAnd = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $searchTextMatchOr = null;

	/**
	 * 
	 *
	 * @var KalturaMediaType
	 */
	public $mediaTypeEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $mediaTypeIn = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $mediaDateGreaterThanEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $mediaDateLessThanEqual = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "orderBy", $this->orderBy);
		$this->addIfNotNull($kparams, "idEqual", $this->idEqual);
		$this->addIfNotNull($kparams, "idIn", $this->idIn);
		$this->addIfNotNull($kparams, "userIdEqual", $this->userIdEqual);
		$this->addIfNotNull($kparams, "typeIn", $this->typeIn);
		$this->addIfNotNull($kparams, "statusEqual", $this->statusEqual);
		$this->addIfNotNull($kparams, "statusIn", $this->statusIn);
		$this->addIfNotNull($kparams, "nameLike", $this->nameLike);
		$this->addIfNotNull($kparams, "nameMultiLikeOr", $this->nameMultiLikeOr);
		$this->addIfNotNull($kparams, "nameMultiLikeAnd", $this->nameMultiLikeAnd);
		$this->addIfNotNull($kparams, "nameEqual", $this->nameEqual);
		$this->addIfNotNull($kparams, "tagsLike", $this->tagsLike);
		$this->addIfNotNull($kparams, "tagsMultiLikeOr", $this->tagsMultiLikeOr);
		$this->addIfNotNull($kparams, "tagsMultiLikeAnd", $this->tagsMultiLikeAnd);
		$this->addIfNotNull($kparams, "adminTagsLike", $this->adminTagsLike);
		$this->addIfNotNull($kparams, "adminTagsMultiLikeOr", $this->adminTagsMultiLikeOr);
		$this->addIfNotNull($kparams, "adminTagsMultiLikeAnd", $this->adminTagsMultiLikeAnd);
		$this->addIfNotNull($kparams, "groupIdEqual", $this->groupIdEqual);
		$this->addIfNotNull($kparams, "createdAtGreaterThanEqual", $this->createdAtGreaterThanEqual);
		$this->addIfNotNull($kparams, "createdAtLessThenEqual", $this->createdAtLessThenEqual);
		$this->addIfNotNull($kparams, "updatedAtGreaterThanEqual", $this->updatedAtGreaterThanEqual);
		$this->addIfNotNull($kparams, "updatedAtLessThenEqual", $this->updatedAtLessThenEqual);
		$this->addIfNotNull($kparams, "modifiedAtGreaterThanEqual", $this->modifiedAtGreaterThanEqual);
		$this->addIfNotNull($kparams, "modifiedAtLessThenEqual", $this->modifiedAtLessThenEqual);
		$this->addIfNotNull($kparams, "partnerIdEqual", $this->partnerIdEqual);
		$this->addIfNotNull($kparams, "partnerIdIn", $this->partnerIdIn);
		$this->addIfNotNull($kparams, "moderationStatusEqual", $this->moderationStatusEqual);
		$this->addIfNotNull($kparams, "moderationStatusIn", $this->moderationStatusIn);
		$this->addIfNotNull($kparams, "tagsAndNameMultiLikeOr", $this->tagsAndNameMultiLikeOr);
		$this->addIfNotNull($kparams, "tagsAndAdminTagsMultiLikeOr", $this->tagsAndAdminTagsMultiLikeOr);
		$this->addIfNotNull($kparams, "tagsAndAdminTagsAndNameMultiLikeOr", $this->tagsAndAdminTagsAndNameMultiLikeOr);
		$this->addIfNotNull($kparams, "tagsAndNameMultiLikeAnd", $this->tagsAndNameMultiLikeAnd);
		$this->addIfNotNull($kparams, "tagsAndAdminTagsMultiLikeAnd", $this->tagsAndAdminTagsMultiLikeAnd);
		$this->addIfNotNull($kparams, "tagsAndAdminTagsAndNameMultiLikeAnd", $this->tagsAndAdminTagsAndNameMultiLikeAnd);
		$this->addIfNotNull($kparams, "searchTextMatchAnd", $this->searchTextMatchAnd);
		$this->addIfNotNull($kparams, "searchTextMatchOr", $this->searchTextMatchOr);
		$this->addIfNotNull($kparams, "mediaTypeEqual", $this->mediaTypeEqual);
		$this->addIfNotNull($kparams, "mediaTypeIn", $this->mediaTypeIn);
		$this->addIfNotNull($kparams, "mediaDateGreaterThanEqual", $this->mediaDateGreaterThanEqual);
		$this->addIfNotNull($kparams, "mediaDateLessThanEqual", $this->mediaDateLessThanEqual);
		return $kparams;
	}
}

class KalturaFilterPager extends KalturaObjectBase
{
	/**
	 * 
	 *
	 * @var int
	 */
	public $pageSize = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $pageIndex = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "pageSize", $this->pageSize);
		$this->addIfNotNull($kparams, "pageIndex", $this->pageIndex);
		return $kparams;
	}
}

class KalturaMediaListResponse extends KalturaObjectBase
{
	/**
	 * 
	 *
	 * @var KalturaMediaEntries
	 * @readonly
	 */
	public $objects;

	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $totalCount = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "totalCount", $this->totalCount);
		return $kparams;
	}
}

class KalturaEditorType
{
	const SIMPLE = 1;
	const ADVANCED = 2;
}

class KalturaMixEntry extends KalturaObjectBase
{
	/**
	 * Auto generated 10 characters alphanumeric string
	 *
	 * @var string
	 * @readonly
	 */
	public $id = null;

	/**
	 * Entry name
	 *
	 * @var string
	 */
	public $name = null;

	/**
	 * Entry description
	 *
	 * @var string
	 */
	public $description = null;

	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $partnerId = null;

	/**
	 * The ID of the user who is the owner of this entry 
	 *
	 * @var string
	 */
	public $userId = null;

	/**
	 * Entry tags
	 *
	 * @var string
	 */
	public $tags = null;

	/**
	 * Entry admin tags can be updated only by administrators and are not visible to the user
	 *
	 * @var string
	 */
	public $adminTags = null;

	/**
	 * 
	 *
	 * @var KalturaEntryStatus
	 * @readonly
	 */
	public $status = null;

	/**
	 * The type of the entry, this is auto filled by the derived entry object
	 *
	 * @var KalturaEntryType
	 * @readonly
	 */
	public $type = null;

	/**
	 * Entry creation date as Unix timestamp (In seconds)
	 *
	 * @var int
	 * @readonly
	 */
	public $createdAt = null;

	/**
	 * Calculated rank
	 *
	 * @var int
	 * @readonly
	 */
	public $rank = null;

	/**
	 * The total (sum) of all votes
	 *
	 * @var int
	 * @readonly
	 */
	public $totalRank = null;

	/**
	 * Number of votes
	 *
	 * @var int
	 * @readonly
	 */
	public $votes = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $groupId = null;

	/**
	 * Can be used to store various partner related data as a string 
	 *
	 * @var string
	 */
	public $partnerData = null;

	/**
	 * Download URL for the entry
	 *
	 * @var string
	 * @readonly
	 */
	public $downloadUrl = null;

	/**
	 * License type used for this entry
	 *
	 * @var KalturaLicenseType
	 */
	public $licenseType = null;

	/**
	 * Number of plays
	 *
	 * @var int
	 * @readonly
	 */
	public $plays = null;

	/**
	 * Number of views
	 *
	 * @var int
	 * @readonly
	 */
	public $views = null;

	/**
	 * The width in pixels
	 *
	 * @var int
	 * @readonly
	 */
	public $width = null;

	/**
	 * The height in pixels
	 *
	 * @var int
	 * @readonly
	 */
	public $height = null;

	/**
	 * Thumbnail URL
	 *
	 * @var string
	 * @readonly
	 */
	public $thumbnailUrl = null;

	/**
	 * The duration in seconds
	 *
	 * @var int
	 * @readonly
	 */
	public $duration = null;

	/**
	 * Indicates whether the user has submited a real thumbnail to the mix (Not the one that was generated automaticaly)
	 *
	 * @var bool
	 * @readonly
	 */
	public $hasRealThumbnail = null;

	/**
	 * The editor type used to edit the metadata
	 *
	 * @var KalturaEditorType
	 */
	public $editorType = null;

	/**
	 * The xml data of the mix
	 *
	 * @var string
	 */
	public $dataContent = null;

	/**
	 * The version of the mix
	 *
	 * @var int
	 * @readonly
	 */
	public $version = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "id", $this->id);
		$this->addIfNotNull($kparams, "name", $this->name);
		$this->addIfNotNull($kparams, "description", $this->description);
		$this->addIfNotNull($kparams, "partnerId", $this->partnerId);
		$this->addIfNotNull($kparams, "userId", $this->userId);
		$this->addIfNotNull($kparams, "tags", $this->tags);
		$this->addIfNotNull($kparams, "adminTags", $this->adminTags);
		$this->addIfNotNull($kparams, "status", $this->status);
		$this->addIfNotNull($kparams, "type", $this->type);
		$this->addIfNotNull($kparams, "createdAt", $this->createdAt);
		$this->addIfNotNull($kparams, "rank", $this->rank);
		$this->addIfNotNull($kparams, "totalRank", $this->totalRank);
		$this->addIfNotNull($kparams, "votes", $this->votes);
		$this->addIfNotNull($kparams, "groupId", $this->groupId);
		$this->addIfNotNull($kparams, "partnerData", $this->partnerData);
		$this->addIfNotNull($kparams, "downloadUrl", $this->downloadUrl);
		$this->addIfNotNull($kparams, "licenseType", $this->licenseType);
		$this->addIfNotNull($kparams, "plays", $this->plays);
		$this->addIfNotNull($kparams, "views", $this->views);
		$this->addIfNotNull($kparams, "width", $this->width);
		$this->addIfNotNull($kparams, "height", $this->height);
		$this->addIfNotNull($kparams, "thumbnailUrl", $this->thumbnailUrl);
		$this->addIfNotNull($kparams, "duration", $this->duration);
		$this->addIfNotNull($kparams, "hasRealThumbnail", $this->hasRealThumbnail);
		$this->addIfNotNull($kparams, "editorType", $this->editorType);
		$this->addIfNotNull($kparams, "dataContent", $this->dataContent);
		$this->addIfNotNull($kparams, "version", $this->version);
		return $kparams;
	}
}

class KalturaMixEntryFilter extends KalturaObjectBase
{
	/**
	 * 
	 *
	 * @var string
	 */
	public $orderBy = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $idEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $idIn = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $userIdEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $typeIn = null;

	/**
	 * 
	 *
	 * @var KalturaEntryStatus
	 */
	public $statusEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $statusIn = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $nameLike = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $nameMultiLikeOr = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $nameMultiLikeAnd = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $nameEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsLike = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsMultiLikeOr = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsMultiLikeAnd = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $adminTagsLike = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $adminTagsMultiLikeOr = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $adminTagsMultiLikeAnd = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $groupIdEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $createdAtGreaterThanEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $createdAtLessThenEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $updatedAtGreaterThanEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $updatedAtLessThenEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $modifiedAtGreaterThanEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $modifiedAtLessThenEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $partnerIdEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $partnerIdIn = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $moderationStatusEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $moderationStatusIn = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsAndNameMultiLikeOr = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsAndAdminTagsMultiLikeOr = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsAndAdminTagsAndNameMultiLikeOr = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsAndNameMultiLikeAnd = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsAndAdminTagsMultiLikeAnd = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsAndAdminTagsAndNameMultiLikeAnd = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $searchTextMatchAnd = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $searchTextMatchOr = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "orderBy", $this->orderBy);
		$this->addIfNotNull($kparams, "idEqual", $this->idEqual);
		$this->addIfNotNull($kparams, "idIn", $this->idIn);
		$this->addIfNotNull($kparams, "userIdEqual", $this->userIdEqual);
		$this->addIfNotNull($kparams, "typeIn", $this->typeIn);
		$this->addIfNotNull($kparams, "statusEqual", $this->statusEqual);
		$this->addIfNotNull($kparams, "statusIn", $this->statusIn);
		$this->addIfNotNull($kparams, "nameLike", $this->nameLike);
		$this->addIfNotNull($kparams, "nameMultiLikeOr", $this->nameMultiLikeOr);
		$this->addIfNotNull($kparams, "nameMultiLikeAnd", $this->nameMultiLikeAnd);
		$this->addIfNotNull($kparams, "nameEqual", $this->nameEqual);
		$this->addIfNotNull($kparams, "tagsLike", $this->tagsLike);
		$this->addIfNotNull($kparams, "tagsMultiLikeOr", $this->tagsMultiLikeOr);
		$this->addIfNotNull($kparams, "tagsMultiLikeAnd", $this->tagsMultiLikeAnd);
		$this->addIfNotNull($kparams, "adminTagsLike", $this->adminTagsLike);
		$this->addIfNotNull($kparams, "adminTagsMultiLikeOr", $this->adminTagsMultiLikeOr);
		$this->addIfNotNull($kparams, "adminTagsMultiLikeAnd", $this->adminTagsMultiLikeAnd);
		$this->addIfNotNull($kparams, "groupIdEqual", $this->groupIdEqual);
		$this->addIfNotNull($kparams, "createdAtGreaterThanEqual", $this->createdAtGreaterThanEqual);
		$this->addIfNotNull($kparams, "createdAtLessThenEqual", $this->createdAtLessThenEqual);
		$this->addIfNotNull($kparams, "updatedAtGreaterThanEqual", $this->updatedAtGreaterThanEqual);
		$this->addIfNotNull($kparams, "updatedAtLessThenEqual", $this->updatedAtLessThenEqual);
		$this->addIfNotNull($kparams, "modifiedAtGreaterThanEqual", $this->modifiedAtGreaterThanEqual);
		$this->addIfNotNull($kparams, "modifiedAtLessThenEqual", $this->modifiedAtLessThenEqual);
		$this->addIfNotNull($kparams, "partnerIdEqual", $this->partnerIdEqual);
		$this->addIfNotNull($kparams, "partnerIdIn", $this->partnerIdIn);
		$this->addIfNotNull($kparams, "moderationStatusEqual", $this->moderationStatusEqual);
		$this->addIfNotNull($kparams, "moderationStatusIn", $this->moderationStatusIn);
		$this->addIfNotNull($kparams, "tagsAndNameMultiLikeOr", $this->tagsAndNameMultiLikeOr);
		$this->addIfNotNull($kparams, "tagsAndAdminTagsMultiLikeOr", $this->tagsAndAdminTagsMultiLikeOr);
		$this->addIfNotNull($kparams, "tagsAndAdminTagsAndNameMultiLikeOr", $this->tagsAndAdminTagsAndNameMultiLikeOr);
		$this->addIfNotNull($kparams, "tagsAndNameMultiLikeAnd", $this->tagsAndNameMultiLikeAnd);
		$this->addIfNotNull($kparams, "tagsAndAdminTagsMultiLikeAnd", $this->tagsAndAdminTagsMultiLikeAnd);
		$this->addIfNotNull($kparams, "tagsAndAdminTagsAndNameMultiLikeAnd", $this->tagsAndAdminTagsAndNameMultiLikeAnd);
		$this->addIfNotNull($kparams, "searchTextMatchAnd", $this->searchTextMatchAnd);
		$this->addIfNotNull($kparams, "searchTextMatchOr", $this->searchTextMatchOr);
		return $kparams;
	}
}

class KalturaMixListResponse extends KalturaObjectBase
{
	/**
	 * 
	 *
	 * @var KalturaMixEntries
	 * @readonly
	 */
	public $objects;

	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $totalCount = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "totalCount", $this->totalCount);
		return $kparams;
	}
}

class KalturaBaseEntry extends KalturaObjectBase
{
	/**
	 * Auto generated 10 characters alphanumeric string
	 *
	 * @var string
	 * @readonly
	 */
	public $id = null;

	/**
	 * Entry name
	 *
	 * @var string
	 */
	public $name = null;

	/**
	 * Entry description
	 *
	 * @var string
	 */
	public $description = null;

	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $partnerId = null;

	/**
	 * The ID of the user who is the owner of this entry 
	 *
	 * @var string
	 */
	public $userId = null;

	/**
	 * Entry tags
	 *
	 * @var string
	 */
	public $tags = null;

	/**
	 * Entry admin tags can be updated only by administrators and are not visible to the user
	 *
	 * @var string
	 */
	public $adminTags = null;

	/**
	 * 
	 *
	 * @var KalturaEntryStatus
	 * @readonly
	 */
	public $status = null;

	/**
	 * The type of the entry, this is auto filled by the derived entry object
	 *
	 * @var KalturaEntryType
	 * @readonly
	 */
	public $type = null;

	/**
	 * Entry creation date as Unix timestamp (In seconds)
	 *
	 * @var int
	 * @readonly
	 */
	public $createdAt = null;

	/**
	 * Calculated rank
	 *
	 * @var int
	 * @readonly
	 */
	public $rank = null;

	/**
	 * The total (sum) of all votes
	 *
	 * @var int
	 * @readonly
	 */
	public $totalRank = null;

	/**
	 * Number of votes
	 *
	 * @var int
	 * @readonly
	 */
	public $votes = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $groupId = null;

	/**
	 * Can be used to store various partner related data as a string 
	 *
	 * @var string
	 */
	public $partnerData = null;

	/**
	 * Download URL for the entry
	 *
	 * @var string
	 * @readonly
	 */
	public $downloadUrl = null;

	/**
	 * License type used for this entry
	 *
	 * @var KalturaLicenseType
	 */
	public $licenseType = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "id", $this->id);
		$this->addIfNotNull($kparams, "name", $this->name);
		$this->addIfNotNull($kparams, "description", $this->description);
		$this->addIfNotNull($kparams, "partnerId", $this->partnerId);
		$this->addIfNotNull($kparams, "userId", $this->userId);
		$this->addIfNotNull($kparams, "tags", $this->tags);
		$this->addIfNotNull($kparams, "adminTags", $this->adminTags);
		$this->addIfNotNull($kparams, "status", $this->status);
		$this->addIfNotNull($kparams, "type", $this->type);
		$this->addIfNotNull($kparams, "createdAt", $this->createdAt);
		$this->addIfNotNull($kparams, "rank", $this->rank);
		$this->addIfNotNull($kparams, "totalRank", $this->totalRank);
		$this->addIfNotNull($kparams, "votes", $this->votes);
		$this->addIfNotNull($kparams, "groupId", $this->groupId);
		$this->addIfNotNull($kparams, "partnerData", $this->partnerData);
		$this->addIfNotNull($kparams, "downloadUrl", $this->downloadUrl);
		$this->addIfNotNull($kparams, "licenseType", $this->licenseType);
		return $kparams;
	}
}

class KalturaBaseEntryFilter extends KalturaObjectBase
{
	/**
	 * 
	 *
	 * @var string
	 */
	public $orderBy = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $idEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $idIn = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $userIdEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $typeIn = null;

	/**
	 * 
	 *
	 * @var KalturaEntryStatus
	 */
	public $statusEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $statusIn = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $nameLike = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $nameMultiLikeOr = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $nameMultiLikeAnd = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $nameEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsLike = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsMultiLikeOr = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsMultiLikeAnd = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $adminTagsLike = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $adminTagsMultiLikeOr = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $adminTagsMultiLikeAnd = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $groupIdEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $createdAtGreaterThanEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $createdAtLessThenEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $updatedAtGreaterThanEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $updatedAtLessThenEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $modifiedAtGreaterThanEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $modifiedAtLessThenEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $partnerIdEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $partnerIdIn = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $moderationStatusEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $moderationStatusIn = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsAndNameMultiLikeOr = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsAndAdminTagsMultiLikeOr = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsAndAdminTagsAndNameMultiLikeOr = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsAndNameMultiLikeAnd = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsAndAdminTagsMultiLikeAnd = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsAndAdminTagsAndNameMultiLikeAnd = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $searchTextMatchAnd = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $searchTextMatchOr = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "orderBy", $this->orderBy);
		$this->addIfNotNull($kparams, "idEqual", $this->idEqual);
		$this->addIfNotNull($kparams, "idIn", $this->idIn);
		$this->addIfNotNull($kparams, "userIdEqual", $this->userIdEqual);
		$this->addIfNotNull($kparams, "typeIn", $this->typeIn);
		$this->addIfNotNull($kparams, "statusEqual", $this->statusEqual);
		$this->addIfNotNull($kparams, "statusIn", $this->statusIn);
		$this->addIfNotNull($kparams, "nameLike", $this->nameLike);
		$this->addIfNotNull($kparams, "nameMultiLikeOr", $this->nameMultiLikeOr);
		$this->addIfNotNull($kparams, "nameMultiLikeAnd", $this->nameMultiLikeAnd);
		$this->addIfNotNull($kparams, "nameEqual", $this->nameEqual);
		$this->addIfNotNull($kparams, "tagsLike", $this->tagsLike);
		$this->addIfNotNull($kparams, "tagsMultiLikeOr", $this->tagsMultiLikeOr);
		$this->addIfNotNull($kparams, "tagsMultiLikeAnd", $this->tagsMultiLikeAnd);
		$this->addIfNotNull($kparams, "adminTagsLike", $this->adminTagsLike);
		$this->addIfNotNull($kparams, "adminTagsMultiLikeOr", $this->adminTagsMultiLikeOr);
		$this->addIfNotNull($kparams, "adminTagsMultiLikeAnd", $this->adminTagsMultiLikeAnd);
		$this->addIfNotNull($kparams, "groupIdEqual", $this->groupIdEqual);
		$this->addIfNotNull($kparams, "createdAtGreaterThanEqual", $this->createdAtGreaterThanEqual);
		$this->addIfNotNull($kparams, "createdAtLessThenEqual", $this->createdAtLessThenEqual);
		$this->addIfNotNull($kparams, "updatedAtGreaterThanEqual", $this->updatedAtGreaterThanEqual);
		$this->addIfNotNull($kparams, "updatedAtLessThenEqual", $this->updatedAtLessThenEqual);
		$this->addIfNotNull($kparams, "modifiedAtGreaterThanEqual", $this->modifiedAtGreaterThanEqual);
		$this->addIfNotNull($kparams, "modifiedAtLessThenEqual", $this->modifiedAtLessThenEqual);
		$this->addIfNotNull($kparams, "partnerIdEqual", $this->partnerIdEqual);
		$this->addIfNotNull($kparams, "partnerIdIn", $this->partnerIdIn);
		$this->addIfNotNull($kparams, "moderationStatusEqual", $this->moderationStatusEqual);
		$this->addIfNotNull($kparams, "moderationStatusIn", $this->moderationStatusIn);
		$this->addIfNotNull($kparams, "tagsAndNameMultiLikeOr", $this->tagsAndNameMultiLikeOr);
		$this->addIfNotNull($kparams, "tagsAndAdminTagsMultiLikeOr", $this->tagsAndAdminTagsMultiLikeOr);
		$this->addIfNotNull($kparams, "tagsAndAdminTagsAndNameMultiLikeOr", $this->tagsAndAdminTagsAndNameMultiLikeOr);
		$this->addIfNotNull($kparams, "tagsAndNameMultiLikeAnd", $this->tagsAndNameMultiLikeAnd);
		$this->addIfNotNull($kparams, "tagsAndAdminTagsMultiLikeAnd", $this->tagsAndAdminTagsMultiLikeAnd);
		$this->addIfNotNull($kparams, "tagsAndAdminTagsAndNameMultiLikeAnd", $this->tagsAndAdminTagsAndNameMultiLikeAnd);
		$this->addIfNotNull($kparams, "searchTextMatchAnd", $this->searchTextMatchAnd);
		$this->addIfNotNull($kparams, "searchTextMatchOr", $this->searchTextMatchOr);
		return $kparams;
	}
}

class KalturaBaseEntryListResponse extends KalturaObjectBase
{
	/**
	 * 
	 *
	 * @var KalturaBaseEntries
	 * @readonly
	 */
	public $objects;

	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $totalCount = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "totalCount", $this->totalCount);
		return $kparams;
	}
}

class KalturaSessionType
{
	const USER = 0;
	const ADMIN = 2;
}

class KalturaUiConfObjType
{
	const PLAYER = 1;
	const CONTRIBUTION_WIZARD = 2;
	const SIMPLE_EDITOR = 3;
	const ADVANCED_EDITOR = 4;
	const PLAYLIST = 5;
	const APP_STUDIO = 6;
}

class KalturaUiConfCreationMode
{
	const WIZARD = 2;
	const ADVANCED = 3;
}

class KalturaUiConf extends KalturaObjectBase
{
	/**
	 * 
	 *
	 * @var string
	 * @readonly
	 */
	public $id = null;

	/**
	 * Name of the uiConf, this is not a primary key
	 *
	 * @var string
	 */
	public $name = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $description = null;

	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $partnerId = null;

	/**
	 * 
	 *
	 * @var KalturaUiConfObjType
	 */
	public $objType = null;

	/**
	 * 
	 *
	 * @var string
	 * @readonly
	 */
	public $objTypeAsString = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $width = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $height = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $htmlParams = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $swfUrl = null;

	/**
	 * 
	 *
	 * @var string
	 * @readonly
	 */
	public $confFilePath = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $confFile = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $confFileFeatures = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $confVars = null;

	/**
	 * 
	 *
	 * @var bool
	 */
	public $useCdn = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tags = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $swfUrlVersion = null;

	/**
	 * Entry creation date as Unix timestamp (In seconds)
	 *
	 * @var int
	 * @readonly
	 */
	public $createdAt = null;

	/**
	 * Entry creation date as Unix timestamp (In seconds)
	 *
	 * @var int
	 * @readonly
	 */
	public $updatedAt = null;

	/**
	 * Enter description here...
	 *
	 * @var KalturaUiConfCreationMode
	 */
	public $creationMode = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "id", $this->id);
		$this->addIfNotNull($kparams, "name", $this->name);
		$this->addIfNotNull($kparams, "description", $this->description);
		$this->addIfNotNull($kparams, "partnerId", $this->partnerId);
		$this->addIfNotNull($kparams, "objType", $this->objType);
		$this->addIfNotNull($kparams, "objTypeAsString", $this->objTypeAsString);
		$this->addIfNotNull($kparams, "width", $this->width);
		$this->addIfNotNull($kparams, "height", $this->height);
		$this->addIfNotNull($kparams, "htmlParams", $this->htmlParams);
		$this->addIfNotNull($kparams, "swfUrl", $this->swfUrl);
		$this->addIfNotNull($kparams, "confFilePath", $this->confFilePath);
		$this->addIfNotNull($kparams, "confFile", $this->confFile);
		$this->addIfNotNull($kparams, "confFileFeatures", $this->confFileFeatures);
		$this->addIfNotNull($kparams, "confVars", $this->confVars);
		$this->addIfNotNull($kparams, "useCdn", $this->useCdn);
		$this->addIfNotNull($kparams, "tags", $this->tags);
		$this->addIfNotNull($kparams, "swfUrlVersion", $this->swfUrlVersion);
		$this->addIfNotNull($kparams, "createdAt", $this->createdAt);
		$this->addIfNotNull($kparams, "updatedAt", $this->updatedAt);
		$this->addIfNotNull($kparams, "creationMode", $this->creationMode);
		return $kparams;
	}
}

class KalturaUiConfFilter extends KalturaObjectBase
{
	/**
	 * 
	 *
	 * @var string
	 */
	public $orderBy = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $id = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $idGreaterThanEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $status = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $nameLike = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsMultiLikeOr = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $createdAtGreaterThanEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $createdAtLessThanEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $updatedAtGreaterThanEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $updatedAtLessThanEqual = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "orderBy", $this->orderBy);
		$this->addIfNotNull($kparams, "id", $this->id);
		$this->addIfNotNull($kparams, "idGreaterThanEqual", $this->idGreaterThanEqual);
		$this->addIfNotNull($kparams, "status", $this->status);
		$this->addIfNotNull($kparams, "nameLike", $this->nameLike);
		$this->addIfNotNull($kparams, "tagsMultiLikeOr", $this->tagsMultiLikeOr);
		$this->addIfNotNull($kparams, "createdAtGreaterThanEqual", $this->createdAtGreaterThanEqual);
		$this->addIfNotNull($kparams, "createdAtLessThanEqual", $this->createdAtLessThanEqual);
		$this->addIfNotNull($kparams, "updatedAtGreaterThanEqual", $this->updatedAtGreaterThanEqual);
		$this->addIfNotNull($kparams, "updatedAtLessThanEqual", $this->updatedAtLessThanEqual);
		return $kparams;
	}
}

class KalturaPlaylistType
{
	const DYNAMIC = 10;
	const STATIC_LIST = 3;
	const EXTERNAL = 101;
}

class KalturaPlaylist extends KalturaObjectBase
{
	/**
	 * Auto generated 10 characters alphanumeric string
	 *
	 * @var string
	 * @readonly
	 */
	public $id = null;

	/**
	 * Entry name
	 *
	 * @var string
	 */
	public $name = null;

	/**
	 * Entry description
	 *
	 * @var string
	 */
	public $description = null;

	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $partnerId = null;

	/**
	 * The ID of the user who is the owner of this entry 
	 *
	 * @var string
	 */
	public $userId = null;

	/**
	 * Entry tags
	 *
	 * @var string
	 */
	public $tags = null;

	/**
	 * Entry admin tags can be updated only by administrators and are not visible to the user
	 *
	 * @var string
	 */
	public $adminTags = null;

	/**
	 * 
	 *
	 * @var KalturaEntryStatus
	 * @readonly
	 */
	public $status = null;

	/**
	 * The type of the entry, this is auto filled by the derived entry object
	 *
	 * @var KalturaEntryType
	 * @readonly
	 */
	public $type = null;

	/**
	 * Entry creation date as Unix timestamp (In seconds)
	 *
	 * @var int
	 * @readonly
	 */
	public $createdAt = null;

	/**
	 * Calculated rank
	 *
	 * @var int
	 * @readonly
	 */
	public $rank = null;

	/**
	 * The total (sum) of all votes
	 *
	 * @var int
	 * @readonly
	 */
	public $totalRank = null;

	/**
	 * Number of votes
	 *
	 * @var int
	 * @readonly
	 */
	public $votes = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $groupId = null;

	/**
	 * Can be used to store various partner related data as a string 
	 *
	 * @var string
	 */
	public $partnerData = null;

	/**
	 * Download URL for the entry
	 *
	 * @var string
	 * @readonly
	 */
	public $downloadUrl = null;

	/**
	 * License type used for this entry
	 *
	 * @var KalturaLicenseType
	 */
	public $licenseType = null;

	/**
	 * Content of the playlist - 
	 *
	 * @var string
	 */
	public $playlistContent = null;

	/**
	 * Type of playlist  
	 *
	 * @var KalturaPlaylistType
	 */
	public $playlistType = null;

	/**
	 * Number of plays
	 *
	 * @var int
	 * @readonly
	 */
	public $plays = null;

	/**
	 * Number of views
	 *
	 * @var int
	 * @readonly
	 */
	public $views = null;

	/**
	 * The duration in seconds
	 *
	 * @var int
	 * @readonly
	 */
	public $duration = null;

	/**
	 * The version of the file
	 *
	 * @var string
	 * @readonly
	 */
	public $version = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "id", $this->id);
		$this->addIfNotNull($kparams, "name", $this->name);
		$this->addIfNotNull($kparams, "description", $this->description);
		$this->addIfNotNull($kparams, "partnerId", $this->partnerId);
		$this->addIfNotNull($kparams, "userId", $this->userId);
		$this->addIfNotNull($kparams, "tags", $this->tags);
		$this->addIfNotNull($kparams, "adminTags", $this->adminTags);
		$this->addIfNotNull($kparams, "status", $this->status);
		$this->addIfNotNull($kparams, "type", $this->type);
		$this->addIfNotNull($kparams, "createdAt", $this->createdAt);
		$this->addIfNotNull($kparams, "rank", $this->rank);
		$this->addIfNotNull($kparams, "totalRank", $this->totalRank);
		$this->addIfNotNull($kparams, "votes", $this->votes);
		$this->addIfNotNull($kparams, "groupId", $this->groupId);
		$this->addIfNotNull($kparams, "partnerData", $this->partnerData);
		$this->addIfNotNull($kparams, "downloadUrl", $this->downloadUrl);
		$this->addIfNotNull($kparams, "licenseType", $this->licenseType);
		$this->addIfNotNull($kparams, "playlistContent", $this->playlistContent);
		$this->addIfNotNull($kparams, "playlistType", $this->playlistType);
		$this->addIfNotNull($kparams, "plays", $this->plays);
		$this->addIfNotNull($kparams, "views", $this->views);
		$this->addIfNotNull($kparams, "duration", $this->duration);
		$this->addIfNotNull($kparams, "version", $this->version);
		return $kparams;
	}
}

class KalturaPlaylistFilter extends KalturaObjectBase
{
	/**
	 * 
	 *
	 * @var string
	 */
	public $orderBy = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $idGreaterThanEqual = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $statusEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $nameLike = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsMultiLikeOr = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $createdAtGreaterThanEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $createdAtLessThanEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $updatedAtGreaterThanEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $updatedAtLessThanEqual = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "orderBy", $this->orderBy);
		$this->addIfNotNull($kparams, "idGreaterThanEqual", $this->idGreaterThanEqual);
		$this->addIfNotNull($kparams, "statusEqual", $this->statusEqual);
		$this->addIfNotNull($kparams, "nameLike", $this->nameLike);
		$this->addIfNotNull($kparams, "tagsMultiLikeOr", $this->tagsMultiLikeOr);
		$this->addIfNotNull($kparams, "createdAtGreaterThanEqual", $this->createdAtGreaterThanEqual);
		$this->addIfNotNull($kparams, "createdAtLessThanEqual", $this->createdAtLessThanEqual);
		$this->addIfNotNull($kparams, "updatedAtGreaterThanEqual", $this->updatedAtGreaterThanEqual);
		$this->addIfNotNull($kparams, "updatedAtLessThanEqual", $this->updatedAtLessThanEqual);
		return $kparams;
	}
}

class KalturaUser extends KalturaObjectBase
{
	/**
	 * 
	 *
	 * @var string
	 * @readonly
	 */
	public $id = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $screenName = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $fullName = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $email = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $dateOfBirth = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $country = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $state = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $city = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $zip = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $urlList = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $picture = null;

	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $icon = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $aboutMe = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tags = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $mobileNum = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $gender = null;

	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $views = null;

	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $fans = null;

	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $entries = null;

	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $producedKshows = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $status = null;

	/**
	 * Entry creation date as Unix timestamp (In seconds)
	 *
	 * @var int
	 * @readonly
	 */
	public $createdAt = null;

	/**
	 * Entry update date as Unix timestamp (In seconds)
	 *
	 * @var int
	 * @readonly
	 */
	public $updatedAt = null;

	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $partnerId = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $displayInSearch = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $partnerData = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "id", $this->id);
		$this->addIfNotNull($kparams, "screenName", $this->screenName);
		$this->addIfNotNull($kparams, "fullName", $this->fullName);
		$this->addIfNotNull($kparams, "email", $this->email);
		$this->addIfNotNull($kparams, "dateOfBirth", $this->dateOfBirth);
		$this->addIfNotNull($kparams, "country", $this->country);
		$this->addIfNotNull($kparams, "state", $this->state);
		$this->addIfNotNull($kparams, "city", $this->city);
		$this->addIfNotNull($kparams, "zip", $this->zip);
		$this->addIfNotNull($kparams, "urlList", $this->urlList);
		$this->addIfNotNull($kparams, "picture", $this->picture);
		$this->addIfNotNull($kparams, "icon", $this->icon);
		$this->addIfNotNull($kparams, "aboutMe", $this->aboutMe);
		$this->addIfNotNull($kparams, "tags", $this->tags);
		$this->addIfNotNull($kparams, "mobileNum", $this->mobileNum);
		$this->addIfNotNull($kparams, "gender", $this->gender);
		$this->addIfNotNull($kparams, "views", $this->views);
		$this->addIfNotNull($kparams, "fans", $this->fans);
		$this->addIfNotNull($kparams, "entries", $this->entries);
		$this->addIfNotNull($kparams, "producedKshows", $this->producedKshows);
		$this->addIfNotNull($kparams, "status", $this->status);
		$this->addIfNotNull($kparams, "createdAt", $this->createdAt);
		$this->addIfNotNull($kparams, "updatedAt", $this->updatedAt);
		$this->addIfNotNull($kparams, "partnerId", $this->partnerId);
		$this->addIfNotNull($kparams, "displayInSearch", $this->displayInSearch);
		$this->addIfNotNull($kparams, "partnerData", $this->partnerData);
		return $kparams;
	}
}

class KalturaUserFilter extends KalturaObjectBase
{
	/**
	 * 
	 *
	 * @var string
	 */
	public $orderBy = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $status = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $screenNameLike = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $tagsLike = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $emailLike = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $countryLike = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $emailLikeRegexp = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $createdAtGreaterThanEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $createdAtLessThanEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $updatedAtGreaterThanEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $updatedAtLessThanEqual = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "orderBy", $this->orderBy);
		$this->addIfNotNull($kparams, "status", $this->status);
		$this->addIfNotNull($kparams, "screenNameLike", $this->screenNameLike);
		$this->addIfNotNull($kparams, "tagsLike", $this->tagsLike);
		$this->addIfNotNull($kparams, "emailLike", $this->emailLike);
		$this->addIfNotNull($kparams, "countryLike", $this->countryLike);
		$this->addIfNotNull($kparams, "emailLikeRegexp", $this->emailLikeRegexp);
		$this->addIfNotNull($kparams, "createdAtGreaterThanEqual", $this->createdAtGreaterThanEqual);
		$this->addIfNotNull($kparams, "createdAtLessThanEqual", $this->createdAtLessThanEqual);
		$this->addIfNotNull($kparams, "updatedAtGreaterThanEqual", $this->updatedAtGreaterThanEqual);
		$this->addIfNotNull($kparams, "updatedAtLessThanEqual", $this->updatedAtLessThanEqual);
		return $kparams;
	}
}

class KalturaWidgetSecurityType
{
	const NONE = 1;
	const TIMEHASH = 2;
}

class KalturaWidget extends KalturaObjectBase
{
	/**
	 * 
	 *
	 * @var string
	 * @readonly
	 */
	public $id = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $sourceWidgetId = null;

	/**
	 * 
	 *
	 * @var string
	 * @readonly
	 */
	public $rootWidgetId = null;

	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $partnerId = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $kshowId = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $entryId = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $uiConfId = null;

	/**
	 * 
	 *
	 * @var KalturaWidgetSecurityType
	 */
	public $securityType = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $securityPolicy = null;

	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $createdAt = null;

	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $updatedAt = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $partnerData = null;

	/**
	 * 
	 *
	 * @var string
	 * @readonly
	 */
	public $widgetHTML = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "id", $this->id);
		$this->addIfNotNull($kparams, "sourceWidgetId", $this->sourceWidgetId);
		$this->addIfNotNull($kparams, "rootWidgetId", $this->rootWidgetId);
		$this->addIfNotNull($kparams, "partnerId", $this->partnerId);
		$this->addIfNotNull($kparams, "kshowId", $this->kshowId);
		$this->addIfNotNull($kparams, "entryId", $this->entryId);
		$this->addIfNotNull($kparams, "uiConfId", $this->uiConfId);
		$this->addIfNotNull($kparams, "securityType", $this->securityType);
		$this->addIfNotNull($kparams, "securityPolicy", $this->securityPolicy);
		$this->addIfNotNull($kparams, "createdAt", $this->createdAt);
		$this->addIfNotNull($kparams, "updatedAt", $this->updatedAt);
		$this->addIfNotNull($kparams, "partnerData", $this->partnerData);
		$this->addIfNotNull($kparams, "widgetHTML", $this->widgetHTML);
		return $kparams;
	}
}

class KalturaWidgetFilter extends KalturaObjectBase
{
	/**
	 * 
	 *
	 * @var string
	 */
	public $orderBy = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $id = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $sourceWidgetId = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $rootWidgetId = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $entryId = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $uiConfId = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $partnerData = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $createdAtGreaterThanEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $createdAtLessThanEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $updatedAtGreaterThanEqual = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $updatedAtLessThanEqual = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "orderBy", $this->orderBy);
		$this->addIfNotNull($kparams, "id", $this->id);
		$this->addIfNotNull($kparams, "sourceWidgetId", $this->sourceWidgetId);
		$this->addIfNotNull($kparams, "rootWidgetId", $this->rootWidgetId);
		$this->addIfNotNull($kparams, "entryId", $this->entryId);
		$this->addIfNotNull($kparams, "uiConfId", $this->uiConfId);
		$this->addIfNotNull($kparams, "partnerData", $this->partnerData);
		$this->addIfNotNull($kparams, "createdAtGreaterThanEqual", $this->createdAtGreaterThanEqual);
		$this->addIfNotNull($kparams, "createdAtLessThanEqual", $this->createdAtLessThanEqual);
		$this->addIfNotNull($kparams, "updatedAtGreaterThanEqual", $this->updatedAtGreaterThanEqual);
		$this->addIfNotNull($kparams, "updatedAtLessThanEqual", $this->updatedAtLessThanEqual);
		return $kparams;
	}
}

class KalturaSearch extends KalturaObjectBase
{
	/**
	 * 
	 *
	 * @var string
	 */
	public $keyWords = null;

	/**
	 * 
	 *
	 * @var KalturaSearchProviderType
	 */
	public $searchSource = null;

	/**
	 * 
	 *
	 * @var KalturaMediaType
	 */
	public $mediaType = null;

	/**
	 * Use this field to pass dynamic data for searching
	 * For example - if you set this field to "mymovies_$partner_id"
	 * The $partner_id will be automatically replcaed with your real partner Id
	 *
	 * @var string
	 */
	public $extraData = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "keyWords", $this->keyWords);
		$this->addIfNotNull($kparams, "searchSource", $this->searchSource);
		$this->addIfNotNull($kparams, "mediaType", $this->mediaType);
		$this->addIfNotNull($kparams, "extraData", $this->extraData);
		return $kparams;
	}
}

class KalturaPartner extends KalturaObjectBase
{
	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $id = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $name = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $website = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $notificationUrl = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $appearInSearch = null;

	/**
	 * 
	 *
	 * @var string
	 * @readonly
	 */
	public $createdAt = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $adminName = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $adminEmail = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $description = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $commercialUse = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $landingPage = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $userLandingPage = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $contentCategories = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $type = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $phone = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $describeYourself = null;

	/**
	 * 
	 *
	 * @var bool
	 */
	public $adultContent = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $defConversionProfileType = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $notify = null;

	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $status = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $allowQuickEdit = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $mergeEntryLists = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $notificationsConfig = null;

	/**
	 * 
	 *
	 * @var int
	 */
	public $maxUploadSize = null;

	/**
	 * readonly
	 *
	 * @var int
	 */
	public $partnerPackage = null;

	/**
	 * readonly
	 *
	 * @var string
	 */
	public $secret = null;

	/**
	 * readonly
	 *
	 * @var string
	 */
	public $adminSecret = null;

	/**
	 * 
	 *
	 * @var string
	 * @readonly
	 */
	public $cmsPassword = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "id", $this->id);
		$this->addIfNotNull($kparams, "name", $this->name);
		$this->addIfNotNull($kparams, "website", $this->website);
		$this->addIfNotNull($kparams, "notificationUrl", $this->notificationUrl);
		$this->addIfNotNull($kparams, "appearInSearch", $this->appearInSearch);
		$this->addIfNotNull($kparams, "createdAt", $this->createdAt);
		$this->addIfNotNull($kparams, "adminName", $this->adminName);
		$this->addIfNotNull($kparams, "adminEmail", $this->adminEmail);
		$this->addIfNotNull($kparams, "description", $this->description);
		$this->addIfNotNull($kparams, "commercialUse", $this->commercialUse);
		$this->addIfNotNull($kparams, "landingPage", $this->landingPage);
		$this->addIfNotNull($kparams, "userLandingPage", $this->userLandingPage);
		$this->addIfNotNull($kparams, "contentCategories", $this->contentCategories);
		$this->addIfNotNull($kparams, "type", $this->type);
		$this->addIfNotNull($kparams, "phone", $this->phone);
		$this->addIfNotNull($kparams, "describeYourself", $this->describeYourself);
		$this->addIfNotNull($kparams, "adultContent", $this->adultContent);
		$this->addIfNotNull($kparams, "defConversionProfileType", $this->defConversionProfileType);
		$this->addIfNotNull($kparams, "notify", $this->notify);
		$this->addIfNotNull($kparams, "status", $this->status);
		$this->addIfNotNull($kparams, "allowQuickEdit", $this->allowQuickEdit);
		$this->addIfNotNull($kparams, "mergeEntryLists", $this->mergeEntryLists);
		$this->addIfNotNull($kparams, "notificationsConfig", $this->notificationsConfig);
		$this->addIfNotNull($kparams, "maxUploadSize", $this->maxUploadSize);
		$this->addIfNotNull($kparams, "partnerPackage", $this->partnerPackage);
		$this->addIfNotNull($kparams, "secret", $this->secret);
		$this->addIfNotNull($kparams, "adminSecret", $this->adminSecret);
		$this->addIfNotNull($kparams, "cmsPassword", $this->cmsPassword);
		return $kparams;
	}
}

class KalturaPartnerUsage extends KalturaObjectBase
{
	/**
	 * 
	 *
	 * @var float
	 * @readonly
	 */
	public $hostingGB = null;

	/**
	 * 
	 *
	 * @var float
	 * @readonly
	 */
	public $Percent = null;

	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $packageBW = null;

	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $usageGB = null;

	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $reachedLimitDate = null;

	/**
	 * 
	 *
	 * @var string
	 * @readonly
	 */
	public $usageGraph = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "hostingGB", $this->hostingGB);
		$this->addIfNotNull($kparams, "Percent", $this->Percent);
		$this->addIfNotNull($kparams, "packageBW", $this->packageBW);
		$this->addIfNotNull($kparams, "usageGB", $this->usageGB);
		$this->addIfNotNull($kparams, "reachedLimitDate", $this->reachedLimitDate);
		$this->addIfNotNull($kparams, "usageGraph", $this->usageGraph);
		return $kparams;
	}
}

class KalturaAdminUser extends KalturaObjectBase
{
	/**
	 * 
	 *
	 * @var string
	 * @readonly
	 */
	public $password = null;

	/**
	 * 
	 *
	 * @var string
	 * @readonly
	 */
	public $email = null;

	/**
	 * 
	 *
	 * @var string
	 */
	public $screenName = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "password", $this->password);
		$this->addIfNotNull($kparams, "email", $this->email);
		$this->addIfNotNull($kparams, "screenName", $this->screenName);
		return $kparams;
	}
}

class KalturaAdminLoginResponse extends KalturaObjectBase
{
	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $partnerId = null;

	/**
	 * 
	 *
	 * @var int
	 * @readonly
	 */
	public $subpId = null;

	/**
	 * 
	 *
	 * @var string
	 * @readonly
	 */
	public $ks = null;

	/**
	 * 
	 *
	 * @var string
	 * @readonly
	 */
	public $uid = null;

	/**
	 * 
	 *
	 * @var KalturaAdminUser
	 * @readonly
	 */
	public $adminUser;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "partnerId", $this->partnerId);
		$this->addIfNotNull($kparams, "subpId", $this->subpId);
		$this->addIfNotNull($kparams, "ks", $this->ks);
		$this->addIfNotNull($kparams, "uid", $this->uid);
		return $kparams;
	}
}

class KalturaNotificationType
{
	const ENTRY_ADD = 1;
	const ENTR_UPDATE_PERMISSIONS = 2;
	const ENTRY_DELETE = 3;
	const ENTRY_BLOCK = 4;
	const ENTRY_UPDATE = 5;
	const ENTRY_UPDATE_THUMBNAIL = 6;
	const ENTRY_UPDATE_MODERATION = 7;
	const USER_ADD = 21;
	const USER_BANNED = 26;
}

class KalturaClientNotification extends KalturaObjectBase
{
	/**
	 * The URL where the notification should be sent to 
	 *
	 * @var string
	 */
	public $url = null;

	/**
	 * The serialized notification data to send
	 *
	 * @var string
	 */
	public $data = null;


	public function toParams()
	{
		$kparams = array();
		$this->addIfNotNull($kparams, "url", $this->url);
		$this->addIfNotNull($kparams, "data", $this->data);
		return $kparams;
	}
}


class KalturaMediaService extends KalturaServiceBase
{
	function __construct(KalturaClient $client)
	{
		parent::__construct($client);
	}

	function addFromUrl(KalturaMediaEntry $mediaEntry, $url)
	{
		$kparams = array();
		$this->client->addParam($kparams, "mediaEntry", $mediaEntry->toParams());
		$this->client->addParam($kparams, "url", $url);
		$resultObject = $this->client->callService("media", "addFromUrl", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaMediaEntry");
		return $resultObject;
	}

	function addFromSearchResult(KalturaMediaEntry $mediaEntry = null, KalturaSearchResult $searchResult)
	{
		$kparams = array();
		if ($mediaEntry !== null)
			$this->client->addParam($kparams, "mediaEntry", $mediaEntry->toParams());
		$this->client->addParam($kparams, "searchResult", $searchResult->toParams());
		$resultObject = $this->client->callService("media", "addFromSearchResult", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaMediaEntry");
		return $resultObject;
	}

	function addFromUploadedFile(KalturaMediaEntry $mediaEntry, $uploadTokenId)
	{
		$kparams = array();
		$this->client->addParam($kparams, "mediaEntry", $mediaEntry->toParams());
		$this->client->addParam($kparams, "uploadTokenId", $uploadTokenId);
		$resultObject = $this->client->callService("media", "addFromUploadedFile", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaMediaEntry");
		return $resultObject;
	}

	function get($entryId)
	{
		$kparams = array();
		$this->client->addParam($kparams, "entryId", $entryId);
		$resultObject = $this->client->callService("media", "get", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaMediaEntry");
		return $resultObject;
	}

	function update($entryId, KalturaMediaEntry $mediaEntry)
	{
		$kparams = array();
		$this->client->addParam($kparams, "entryId", $entryId);
		$this->client->addParam($kparams, "mediaEntry", $mediaEntry->toParams());
		$resultObject = $this->client->callService("media", "update", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaMediaEntry");
		return $resultObject;
	}

	function delete($entryId)
	{
		$kparams = array();
		$this->client->addParam($kparams, "entryId", $entryId);
		$resultObject = $this->client->callService("media", "delete", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "null");
		return $resultObject;
	}

	function listAction(KalturaMediaEntryFilter $filter = null, KalturaFilterPager $pager = null)
	{
		$kparams = array();
		if ($filter !== null)
			$this->client->addParam($kparams, "filter", $filter->toParams());
		if ($pager !== null)
			$this->client->addParam($kparams, "pager", $pager->toParams());
		$resultObject = $this->client->callService("media", "list", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaMediaListResponse");
		return $resultObject;
	}

	function updateThumbnail($entryId, $timeOffset)
	{
		$kparams = array();
		$this->client->addParam($kparams, "entryId", $entryId);
		$this->client->addParam($kparams, "timeOffset", $timeOffset);
		$resultObject = $this->client->callService("media", "updateThumbnail", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaMediaEntry");
		return $resultObject;
	}

	function requestConversion($entryId, $fileFormat)
	{
		$kparams = array();
		$this->client->addParam($kparams, "entryId", $entryId);
		$this->client->addParam($kparams, "fileFormat", $fileFormat);
		$resultObject = $this->client->callService("media", "requestConversion", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "int");
		return $resultObject;
	}
}

class KalturaMixingService extends KalturaServiceBase
{
	function __construct(KalturaClient $client)
	{
		parent::__construct($client);
	}

	function add(KalturaMixEntry $mixEntry)
	{
		$kparams = array();
		$this->client->addParam($kparams, "mixEntry", $mixEntry->toParams());
		$resultObject = $this->client->callService("mixing", "add", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaMixEntry");
		return $resultObject;
	}

	function get($entryId)
	{
		$kparams = array();
		$this->client->addParam($kparams, "entryId", $entryId);
		$resultObject = $this->client->callService("mixing", "get", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaMixEntry");
		return $resultObject;
	}

	function update($entryId, KalturaMixEntry $mixEntry)
	{
		$kparams = array();
		$this->client->addParam($kparams, "entryId", $entryId);
		$this->client->addParam($kparams, "mixEntry", $mixEntry->toParams());
		$resultObject = $this->client->callService("mixing", "update", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaMixEntry");
		return $resultObject;
	}

	function delete($entryId)
	{
		$kparams = array();
		$this->client->addParam($kparams, "entryId", $entryId);
		$resultObject = $this->client->callService("mixing", "delete", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "null");
		return $resultObject;
	}

	function listAction(KalturaMixEntryFilter $filter = null, KalturaFilterPager $pager = null)
	{
		$kparams = array();
		if ($filter !== null)
			$this->client->addParam($kparams, "filter", $filter->toParams());
		if ($pager !== null)
			$this->client->addParam($kparams, "pager", $pager->toParams());
		$resultObject = $this->client->callService("mixing", "list", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaMixListResponse");
		return $resultObject;
	}

	function cloneAction($entryId)
	{
		$kparams = array();
		$this->client->addParam($kparams, "entryId", $entryId);
		$resultObject = $this->client->callService("mixing", "clone", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaMixEntry");
		return $resultObject;
	}

	function appendMediaEntry($mixEntryId, $mediaEntryId)
	{
		$kparams = array();
		$this->client->addParam($kparams, "mixEntryId", $mixEntryId);
		$this->client->addParam($kparams, "mediaEntryId", $mediaEntryId);
		$resultObject = $this->client->callService("mixing", "appendMediaEntry", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaMixEntry");
		return $resultObject;
	}

	function requestFlattening($entryId, $fileFormat, $version = -1)
	{
		$kparams = array();
		$this->client->addParam($kparams, "entryId", $entryId);
		$this->client->addParam($kparams, "fileFormat", $fileFormat);
		$this->client->addParam($kparams, "version", $version);
		$resultObject = $this->client->callService("mixing", "requestFlattening", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "int");
		return $resultObject;
	}
}

class KalturaBaseEntryService extends KalturaServiceBase
{
	function __construct(KalturaClient $client)
	{
		parent::__construct($client);
	}

	function get($entryId)
	{
		$kparams = array();
		$this->client->addParam($kparams, "entryId", $entryId);
		$resultObject = $this->client->callService("baseentry", "get", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaBaseEntry");
		return $resultObject;
	}

	function delete($entryId)
	{
		$kparams = array();
		$this->client->addParam($kparams, "entryId", $entryId);
		$resultObject = $this->client->callService("baseentry", "delete", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "null");
		return $resultObject;
	}

	function listAction(KalturaBaseEntryFilter $filter = null, KalturaFilterPager $pager = null)
	{
		$kparams = array();
		if ($filter !== null)
			$this->client->addParam($kparams, "filter", $filter->toParams());
		if ($pager !== null)
			$this->client->addParam($kparams, "pager", $pager->toParams());
		$resultObject = $this->client->callService("baseentry", "list", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaBaseEntryListResponse");
		return $resultObject;
	}
}

class KalturaSessionService extends KalturaServiceBase
{
	function __construct(KalturaClient $client)
	{
		parent::__construct($client);
	}

	function start($secret, $userId, $type = 0, $partnerId = null, $expiry = 86400, $privileges = null)
	{
		$kparams = array();
		$this->client->addParam($kparams, "secret", $secret);
		$this->client->addParam($kparams, "userId", $userId);
		$this->client->addParam($kparams, "type", $type);
		$this->client->addParam($kparams, "partnerId", $partnerId);
		$this->client->addParam($kparams, "expiry", $expiry);
		$this->client->addParam($kparams, "privileges", $privileges);
		$resultObject = $this->client->callService("session", "start", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "string");
		return $resultObject;
	}

	function startWidgetSession($widgetId, $expiry = 86400)
	{
		$kparams = array();
		$this->client->addParam($kparams, "widgetId", $widgetId);
		$this->client->addParam($kparams, "expiry", $expiry);
		$resultObject = $this->client->callService("session", "startWidgetSession", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "string");
		return $resultObject;
	}
}

class KalturaUiConfService extends KalturaServiceBase
{
	function __construct(KalturaClient $client)
	{
		parent::__construct($client);
	}

	function add(KalturaUiConf $uiConf)
	{
		$kparams = array();
		$this->client->addParam($kparams, "uiConf", $uiConf->toParams());
		$resultObject = $this->client->callService("uiconf", "add", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaUiConf");
		return $resultObject;
	}

	function update($id, KalturaUiConf $uiConf)
	{
		$kparams = array();
		$this->client->addParam($kparams, "id", $id);
		$this->client->addParam($kparams, "uiConf", $uiConf->toParams());
		$resultObject = $this->client->callService("uiconf", "update", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaUiConf");
		return $resultObject;
	}

	function get($id)
	{
		$kparams = array();
		$this->client->addParam($kparams, "id", $id);
		$resultObject = $this->client->callService("uiconf", "get", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaUiConf");
		return $resultObject;
	}

	function delete($id)
	{
		$kparams = array();
		$this->client->addParam($kparams, "id", $id);
		$resultObject = $this->client->callService("uiconf", "delete", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "null");
		return $resultObject;
	}

	function cloneAction($id)
	{
		$kparams = array();
		$this->client->addParam($kparams, "id", $id);
		$resultObject = $this->client->callService("uiconf", "clone", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaUiConf");
		return $resultObject;
	}

	function listAction(KalturaUiConfFilter $filter = null, KalturaFilterPager $pager = null)
	{
		$kparams = array();
		if ($filter !== null)
			$this->client->addParam($kparams, "filter", $filter->toParams());
		if ($pager !== null)
			$this->client->addParam($kparams, "pager", $pager->toParams());
		$resultObject = $this->client->callService("uiconf", "list", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "array");
		return $resultObject;
	}
}

class KalturaPlaylistService extends KalturaServiceBase
{
	function __construct(KalturaClient $client)
	{
		parent::__construct($client);
	}

	function add(KalturaPlaylist $playlist, $updateStats = false)
	{
		$kparams = array();
		$this->client->addParam($kparams, "playlist", $playlist->toParams());
		$this->client->addParam($kparams, "updateStats", $updateStats);
		$resultObject = $this->client->callService("playlist", "add", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaPlaylist");
		return $resultObject;
	}

	function get($id)
	{
		$kparams = array();
		$this->client->addParam($kparams, "id", $id);
		$resultObject = $this->client->callService("playlist", "get", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaPlaylist");
		return $resultObject;
	}

	function update($id, KalturaPlaylist $playlist, $updateStats = false)
	{
		$kparams = array();
		$this->client->addParam($kparams, "id", $id);
		$this->client->addParam($kparams, "playlist", $playlist->toParams());
		$this->client->addParam($kparams, "updateStats", $updateStats);
		$resultObject = $this->client->callService("playlist", "update", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaUiConf");
		return $resultObject;
	}

	function delete($id)
	{
		$kparams = array();
		$this->client->addParam($kparams, "id", $id);
		$resultObject = $this->client->callService("playlist", "delete", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaPlaylist");
		return $resultObject;
	}

	function listAction(KalturaPlaylistFilter $filter = null, KalturaFilterPager $pager = null)
	{
		$kparams = array();
		if ($filter !== null)
			$this->client->addParam($kparams, "filter", $filter->toParams());
		if ($pager !== null)
			$this->client->addParam($kparams, "pager", $pager->toParams());
		$resultObject = $this->client->callService("playlist", "list", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "array");
		return $resultObject;
	}

	function execute($id, $detailed = false)
	{
		$kparams = array();
		$this->client->addParam($kparams, "id", $id);
		$this->client->addParam($kparams, "detailed", $detailed);
		$resultObject = $this->client->callService("playlist", "execute", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "array");
		return $resultObject;
	}

	function executeFromContent($playlistType, $playlistContent, $detailed = false)
	{
		$kparams = array();
		$this->client->addParam($kparams, "playlistType", $playlistType);
		$this->client->addParam($kparams, "playlistContent", $playlistContent);
		$this->client->addParam($kparams, "detailed", $detailed);
		$resultObject = $this->client->callService("playlist", "executeFromContent", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "array");
		return $resultObject;
	}

	function getStatsFromContent($playlistType, $playlistContent)
	{
		$kparams = array();
		$this->client->addParam($kparams, "playlistType", $playlistType);
		$this->client->addParam($kparams, "playlistContent", $playlistContent);
		$resultObject = $this->client->callService("playlist", "getStatsFromContent", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "array");
		return $resultObject;
	}
}

class KalturaUserService extends KalturaServiceBase
{
	function __construct(KalturaClient $client)
	{
		parent::__construct($client);
	}

	function add($id, KalturaUser $user)
	{
		$kparams = array();
		$this->client->addParam($kparams, "id", $id);
		$this->client->addParam($kparams, "user", $user->toParams());
		$resultObject = $this->client->callService("user", "add", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaUser");
		return $resultObject;
	}

	function update($id, KalturaUser $user)
	{
		$kparams = array();
		$this->client->addParam($kparams, "id", $id);
		$this->client->addParam($kparams, "user", $user->toParams());
		$resultObject = $this->client->callService("user", "update", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaUser");
		return $resultObject;
	}

	function updateid($id, $newId)
	{
		$kparams = array();
		$this->client->addParam($kparams, "id", $id);
		$this->client->addParam($kparams, "newId", $newId);
		$resultObject = $this->client->callService("user", "updateid", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaUser");
		return $resultObject;
	}

	function get($id)
	{
		$kparams = array();
		$this->client->addParam($kparams, "id", $id);
		$resultObject = $this->client->callService("user", "get", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaUser");
		return $resultObject;
	}

	function delete($id)
	{
		$kparams = array();
		$this->client->addParam($kparams, "id", $id);
		$resultObject = $this->client->callService("user", "delete", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaUser");
		return $resultObject;
	}

	function listAction(KalturaUserFilter $filter = null, KalturaFilterPager $pager = null)
	{
		$kparams = array();
		if ($filter !== null)
			$this->client->addParam($kparams, "filter", $filter->toParams());
		if ($pager !== null)
			$this->client->addParam($kparams, "pager", $pager->toParams());
		$resultObject = $this->client->callService("user", "list", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "array");
		return $resultObject;
	}
}

class KalturaWidgetService extends KalturaServiceBase
{
	function __construct(KalturaClient $client)
	{
		parent::__construct($client);
	}

	function add(KalturaWidget $widget)
	{
		$kparams = array();
		$this->client->addParam($kparams, "widget", $widget->toParams());
		$resultObject = $this->client->callService("widget", "add", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaWidget");
		return $resultObject;
	}

	function update($id, KalturaWidget $widget)
	{
		$kparams = array();
		$this->client->addParam($kparams, "id", $id);
		$this->client->addParam($kparams, "widget", $widget->toParams());
		$resultObject = $this->client->callService("widget", "update", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaWidget");
		return $resultObject;
	}

	function get($id)
	{
		$kparams = array();
		$this->client->addParam($kparams, "id", $id);
		$resultObject = $this->client->callService("widget", "get", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaWidget");
		return $resultObject;
	}

	function cloneAction(KalturaWidget $widget)
	{
		$kparams = array();
		$this->client->addParam($kparams, "widget", $widget->toParams());
		$resultObject = $this->client->callService("widget", "clone", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaWidget");
		return $resultObject;
	}

	function listAction(KalturaWidgetFilter $filter = null, KalturaFilterPager $pager = null)
	{
		$kparams = array();
		if ($filter !== null)
			$this->client->addParam($kparams, "filter", $filter->toParams());
		if ($pager !== null)
			$this->client->addParam($kparams, "pager", $pager->toParams());
		$resultObject = $this->client->callService("widget", "list", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "array");
		return $resultObject;
	}
}

class KalturaSearchService extends KalturaServiceBase
{
	function __construct(KalturaClient $client)
	{
		parent::__construct($client);
	}

	function search(KalturaSearch $search, KalturaFilterPager $pager = null)
	{
		$kparams = array();
		$this->client->addParam($kparams, "search", $search->toParams());
		if ($pager !== null)
			$this->client->addParam($kparams, "pager", $pager->toParams());
		$resultObject = $this->client->callService("search", "search", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "array");
		return $resultObject;
	}

	function getMediaInfo(KalturaSearchResult $searchResult)
	{
		$kparams = array();
		$this->client->addParam($kparams, "searchResult", $searchResult->toParams());
		$resultObject = $this->client->callService("search", "getMediaInfo", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaSearchResult");
		return $resultObject;
	}

	function searchUrl($mediaType, $url)
	{
		$kparams = array();
		$this->client->addParam($kparams, "mediaType", $mediaType);
		$this->client->addParam($kparams, "url", $url);
		$resultObject = $this->client->callService("search", "searchUrl", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaSearchResult");
		return $resultObject;
	}
}

class KalturaPartnerService extends KalturaServiceBase
{
	function __construct(KalturaClient $client)
	{
		parent::__construct($client);
	}

	function register(KalturaPartner $partner, $cmsPassword = "")
	{
		$kparams = array();
		$this->client->addParam($kparams, "partner", $partner->toParams());
		$this->client->addParam($kparams, "cmsPassword", $cmsPassword);
		$resultObject = $this->client->callService("partner", "register", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaPartner");
		return $resultObject;
	}

	function update(KalturaPartner $partner, $allowEmpty = false)
	{
		$kparams = array();
		$this->client->addParam($kparams, "partner", $partner->toParams());
		$this->client->addParam($kparams, "allowEmpty", $allowEmpty);
		$resultObject = $this->client->callService("partner", "update", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaPartner");
		return $resultObject;
	}

	function getsecrets($partnerId, $adminEmail, $cmsPassword)
	{
		$kparams = array();
		$this->client->addParam($kparams, "partnerId", $partnerId);
		$this->client->addParam($kparams, "adminEmail", $adminEmail);
		$this->client->addParam($kparams, "cmsPassword", $cmsPassword);
		$resultObject = $this->client->callService("partner", "getsecrets", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaPartner");
		return $resultObject;
	}

	function getinfo()
	{
		$kparams = array();
		$resultObject = $this->client->callService("partner", "getinfo", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaPartner");
		return $resultObject;
	}

	function getusage($year, $month = 1, $resolution = "days")
	{
		$kparams = array();
		$this->client->addParam($kparams, "year", $year);
		$this->client->addParam($kparams, "month", $month);
		$this->client->addParam($kparams, "resolution", $resolution);
		$resultObject = $this->client->callService("partner", "getusage", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaPartnerUsage");
		return $resultObject;
	}
}

class KalturaAdminuserService extends KalturaServiceBase
{
	function __construct(KalturaClient $client)
	{
		parent::__construct($client);
	}

	function updatepassword($email, $password, $newEmail = "", $newPassword = "")
	{
		$kparams = array();
		$this->client->addParam($kparams, "email", $email);
		$this->client->addParam($kparams, "password", $password);
		$this->client->addParam($kparams, "newEmail", $newEmail);
		$this->client->addParam($kparams, "newPassword", $newPassword);
		$resultObject = $this->client->callService("adminuser", "updatepassword", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaAdminUser");
		return $resultObject;
	}

	function resetpassword($email)
	{
		$kparams = array();
		$this->client->addParam($kparams, "email", $email);
		$resultObject = $this->client->callService("adminuser", "resetpassword", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "string");
		return $resultObject;
	}

	function login($email, $password)
	{
		$kparams = array();
		$this->client->addParam($kparams, "email", $email);
		$this->client->addParam($kparams, "password", $password);
		$resultObject = $this->client->callService("adminuser", "login", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaAdminLoginResponse");
		return $resultObject;
	}
}

class KalturaSystemService extends KalturaServiceBase
{
	function __construct(KalturaClient $client)
	{
		parent::__construct($client);
	}

	function ping()
	{
		$kparams = array();
		$resultObject = $this->client->callService("system", "ping", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "bool");
		return $resultObject;
	}
}

class KalturaNotificationService extends KalturaServiceBase
{
	function __construct(KalturaClient $client)
	{
		parent::__construct($client);
	}

	function getClientNotification($entryId, $type)
	{
		$kparams = array();
		$this->client->addParam($kparams, "entryId", $entryId);
		$this->client->addParam($kparams, "type", $type);
		$resultObject = $this->client->callService("notification", "getClientNotification", $kparams);
		$this->client->throwExceptionIfError($resultObject);
		$this->client->validateObjectType($resultObject, "KalturaClientNotification");
		return $resultObject;
	}
}

class KalturaClient extends KalturaClientBase
{
	/**
	 * Media Service
	 *
	 * @var KalturaMediaService
	 */
	public $media = null;

	/**
	 * Mixing Service
	 *
	 * @var KalturaMixingService
	 */
	public $mixing = null;

	/**
	 * Base Entry Service
	 *
	 * @var KalturaBaseEntryService
	 */
	public $baseEntry = null;

	/**
	 * Session service
	 *
	 * @var KalturaSessionService
	 */
	public $session = null;

	/**
	 * UiConf service lets you create and manage your UIConfs for the various flash components
	 * This service is used by the KMC-ApplicationStudio
	 *
	 * @var KalturaUiConfService
	 */
	public $uiConf = null;

	/**
	 * Playlist service lets you create,manage and play your playlists
	 * Playlists could be static (containing a fixed list of entries) or dynamic (baseed on a filter)
	 *
	 * @var KalturaPlaylistService
	 */
	public $playlist = null;

	/**
	 * Manage partner users on Kaltura's side
	 * The userId in kaltura is the unique Id in the partner's system, and the [partnerId,Id] couple are unique key in kaltura's DB
	 *
	 * @var KalturaUserService
	 */
	public $user = null;

	/**
	 * widget service for full widget management
	 *
	 * @var KalturaWidgetService
	 */
	public $widget = null;

	/**
	 * Search service allows you to search for media in various media providers
	 * This service is being used mostly by the CW component
	 *
	 * @var KalturaSearchService
	 */
	public $search = null;

	/**
	 * partner service allows you to change/manage your partner personal details and settings as well
	 *
	 * @var KalturaPartnerService
	 */
	public $partner = null;

	/**
	 * adminuser service
	 *
	 * @var KalturaAdminuserService
	 */
	public $adminuser = null;

	/**
	 * System Service
	 *
	 * @var KalturaSystemService
	 */
	public $system = null;

	/**
	 * Notification Service
	 *
	 * @var KalturaNotificationService
	 */
	public $notification = null;


	public function __construct(KalturaConfiguration $config)
	{
		parent::__construct($config);
		$this->media = new KalturaMediaService($this);
		$this->mixing = new KalturaMixingService($this);
		$this->baseEntry = new KalturaBaseEntryService($this);
		$this->session = new KalturaSessionService($this);
		$this->uiConf = new KalturaUiConfService($this);
		$this->playlist = new KalturaPlaylistService($this);
		$this->user = new KalturaUserService($this);
		$this->widget = new KalturaWidgetService($this);
		$this->search = new KalturaSearchService($this);
		$this->partner = new KalturaPartnerService($this);
		$this->adminuser = new KalturaAdminuserService($this);
		$this->system = new KalturaSystemService($this);
		$this->notification = new KalturaNotificationService($this);
	}
}
