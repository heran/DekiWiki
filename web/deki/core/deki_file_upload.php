<?php
/**
 * Class handles file uploading and file upload eventing
 * 
 * TODO: should this be moved out of core since it has hooks?
 */
class DekiFileUpload
{
	/**
	 * Triggered after the API has acknowledged a successful file upload via the UI
	 * array('file', $fileId, &$fileName, &$filePath, &$fileType)
	 * @param string $destinationType - ['file', 'page']
	 * @param int $id - identifier for the destination
	 * @param $fileName - filename for the uploaded file
	 * @param $filePath - location of the file to upload on disk
	 * @param $fileType - mime-type of the file being uploaded
	 * @param $HaltReturn - if the hook is halted, this will be returned to the caller. expected DekiFile return.
	 *  
	 * @return HANDLED_HALT to halt default upload process. UPLOAD_COMPLETE will not be fired.
	 */
	const HOOK_FILE_UPLOAD = 'DekiFileUpload:FileUpload';

	/**
	 * Triggered after the API has acknowledged a successful file upload via the UI
	 * 
	 * @param DekiFile &$File - object corresponding to the file that was just uploaded
	 * @return N/A
	 */
	const HOOK_FILE_UPLOAD_COMPLETE = 'DekiFileUpload:FileUploadComplete';


	/**
	 * Attachs a new file to a page. Might update an existing file revision
	 * 
	 * @param string $postField - name of the file's post field
	 * @param int $pageId - page to attach the file to
	 * @param string $fileDescription
	 * 
	 * @return DekiFile
	 */
	public static function newFromPost($postField, $pageId, $fileDescription)
	{
		$fileName = $filePath = $fileType = '';
		if (!self::validateFileUpload($postField, $fileName, $filePath, $fileType))
		{
			return null;
		}
		
		$HaltReturn = null;
		// fire the file upload hook
		$result = DekiPlugin::executeHook(DekiFileUpload::HOOK_FILE_UPLOAD, array('page', $pageId, &$fileName, &$filePath, &$fileType, &$HaltReturn));
		if ($result == DekiPlugin::HANDLED_HALT)
		{
			return $HaltReturn;
		}

		$Plug = DekiPlug::getInstance()->At('pages', $pageId, 'files', '='.$fileName);
		
		return self::uploadTo($Plug, $fileName, $filePath, $fileType, $fileDescription);		
	}
	
	/**
	 *  Attachs a file to an existing file
	 *  
	 * @param string $postField - name of the file's post field
	 * @param int $fileId
	 * @param string $fileDescription
	 * 
	 * @return DekiFile
	 */
	public static function updateFromPost($postField, $fileId, $fileDescription)
	{
		$fileName = $filePath = $fileType = '';
		if (!self::validateFileUpload($postField, $fileName, $filePath, $fileType))
		{
			return null;
		}
		
		$HaltReturn = null;
		// fire the file upload hook
		$result = DekiPlugin::executeHook(DekiFileUpload::HOOK_FILE_UPLOAD, array('file', $fileId, &$fileName, &$filePath, &$fileType, &$HaltReturn));
		if ($result == DekiPlugin::HANDLED_HALT)
		{
			return $HaltReturn;
		}

		$Plug = DekiPlug::getInstance()->At('files', $fileId, '='.$fileName);
		
		return self::uploadTo($Plug, $fileName, $filePath, $fileType, $fileDescription);	
	}

	/**
	 * Returns maximum file size allowed to upload
	 * 
	 * @return int file size limit in bytes
	 */
	public static function getUploadLimit()
	{
		$postMaxSize = self::memorySizeToBytes(ini_get("post_max_size"));
		$uploadMaxFilesize = self::memorySizeToBytes(ini_get("upload_max_filesize"));
		$maxFileSize = (int) wfGetConfig('files/max-file-size');

		return min($postMaxSize, $uploadMaxFilesize, $maxFileSize);
	}

	/**
	 * Validates the POSTed file information
	 * 
	 * @param $postField - name of the file's post field, used to fetch the following
	 * @param &$fileName - returns filename of the uploaded file
	 * @param &$filePath - returns location of the file to upload on disk
	 * @param &$fileType - returns mime-type of the file
	 * 
	 * @return n/a - returns values by reference
	 */
	protected static function validateFileUpload($postField, &$fileName, &$filePath, &$fileType)
	{
		global $wgRequest;

		// @todo guerrics: consider an error message?
		$fileSize = $wgRequest->getFileSize($postField);
		if ($fileSize <= 0)
		{
			// ignore empty uploads
			return false;
		}
		
		// filename of uploaded file
		$fileName = $wgRequest->getFileName($postField);
		// fix a bug in PHP where ' gets translated into \' for <input> filenames
		$fileName = str_replace('\\', '', $fileName);

		// location of temp file
		$filePath = $wgRequest->getFileTempname($postField);

		// file mime-type
		$fileType = $wgRequest->getFileType($postField);
		/**
		 * If a file gets uploaded with no extension, then the content-type will be application/octet-stream, 
		 * which is too generic. In this case, try to grab the mimetype specifically
		 */
		if (empty($fileType) || $fileType == 'application/octet-stream')
		{
			global $wgMimeTypes;
			$File = new DekiFile($fileName);
			$extension = $File->getExtension();
			// see bug #3933; libmagic is buggy, so we have to manually maintain a list based on extension
			if (array_key_exists($extension, $wgMimeTypes))
			{
				$fileType = $wgMimeTypes[$extension];
			}
			else if (!is_null($filePath))
			{
				$fileType = mime_content_type($filePath);
			}
		}


		// validate file upload
		if (is_null($fileName) || $fileName === '')
		{
			return false;
		}

		$error = $wgRequest->getFileError($postField);
		if ($error != UPLOAD_ERR_OK)
		{
			$message = '';

			if (!defined('UPLOAD_ERR_NO_TMP_DIR'))
				define('UPLOAD_ERR_NO_TMP_DIR', 6);

			if (!defined('UPLOAD_ERR_CANT_WRITE'))
				define('UPLOAD_ERR_CANT_WRITE', 7);

			if (!defined('UPLOAD_ERR_EXTENSION'))
				define('UPLOAD_ERR_EXTENSION', 8);
			
			switch ($error)
			{
				case UPLOAD_ERR_INI_SIZE:
					$message = wfMsg('Article.Attach.file-exceeds-max-size', ini_get('upload_max_filesize'));
					break;
				case UPLOAD_ERR_FORM_SIZE:
					$message = wfMsg('Article.Attach.file-exceeds-max-size', $wgRequest->getVal('MAX_FILE_SIZE'));
					break;
				case UPLOAD_ERR_PARTIAL:
					$message = wfMsg('Article.Attach.error.partially-uploaded');
					break;
				case UPLOAD_ERR_NO_FILE:
					$message = wfMsg('Article.Attach.error.no-file-uploaded');
					break;
				case UPLOAD_ERR_NO_TMP_DIR:
					$message = wfMsg('Article.Attach.error.missing-temporary-folder');
					break;
				case UPLOAD_ERR_CANT_WRITE:
					$message = wfMsg('Article.Attach.error.failed-to-write-file');
					break;
				case UPLOAD_ERR_EXTENSION:
					$message = wfMsg('Article.Attach.error.stopped-by-extension');
					break;
			}

			MTMessage::Show($message, $fileName);
			return false;
		}

		if (filesize($filePath) == 0)
		{
			MTMessage::Show(wfMsg('Article.Attach.file-has-no-size', $fileName), '');
			return false;
		}

		return true;
	}
	
	/**
	 * Finalizes the Plug before upload and executes the file upload
	 * 
	 * @param DekiPlug $Plug
	 * @param string $fileName - name of the file being uploaded
	 * @param string $filePath
	 * @param string $fileType
	 * @param string $fileDescription
	 * 
	 * @return DekiFile - null if the upload fails
	 */
	protected static function uploadTo($Plug, $fileName, $filePath, $fileType = 'application/octet-stream', $fileDescription = null)
	{
		// add the file description
		if (!empty($fileDescription))
		{
			$Plug = $Plug->With('description', $fileDescription);
		}

		$Result = $Plug->PutFile($filePath, $fileType);
		if ($Result->is(400))
		{
			$message = $Result->getError();
			// need to check for a curl error. curl returns an internal error for 400's against localhost
			if ($Result->isCurlError() || empty($message)) 
			{
				$File = new DekiFile($fileName, $fileType);
				$extension = $File->getExtension();
				$message = wfMsg('System.API.Error.file_type_not_allowed', $extension);	
			}

			MTMessage::Show($message, $fileName);
			return null;
		}
		else if ($Result->handleResponse())
		{
			// file was successfully uploaded
			$File = DekiFile::newFromArray($Result->getVal('body/file'));
			// fire the file upload hook
			DekiPlugin::executeHook(DekiFileUpload::HOOK_FILE_UPLOAD_COMPLETE, array(&$File));
			
			return $File;
		}

		return null;
	}

	/**
	 * Converts memory size values like '8M' to bytes
	 *
	 * @link http://php.net/ini_get
	 *
	 * @param string $val
	 * @return int
	 */
	protected static function memorySizeToBytes($val)
	{
		$val = trim($val);
		$last = strtolower($val[strlen($val)-1]);
		switch($last) {
			// The 'G' modifier is available since PHP 5.1.0
			case 'g':
				$val *= 1024;
			case 'm':
				$val *= 1024;
			case 'k':
				$val *= 1024;
		}

		return $val;
	}
}
