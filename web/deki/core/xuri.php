<?php

class XUri
{
	/**
	 * Simple check to see if a string is a valid url
	 * @param $string
	 * @return bool
	 */
	public static function isUrl($string)
	{
		$filtered = filter_var($string, FILTER_VALIDATE_URL);

		return ($filtered !== false);
	}
}
