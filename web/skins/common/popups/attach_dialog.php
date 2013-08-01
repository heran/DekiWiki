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

define( 'MINDTOUCH_DEKI', true );

$dekiRoot = '../../../';
require_once($dekiRoot . 'includes/Defines.php');
require_once($dekiRoot . 'LocalSettings.php');
require_once($dekiRoot . 'includes/Setup.php');
require_once($dekiRoot . 'deki/core/deki_file_upload.php');

$_SESSION['swfupload_token'] = DekiToken::get();

$wgUploaders = (array) $wgUploaders;
$wgUploaders = array_unique($wgUploaders);
$wgUploaders = array_slice($wgUploaders, 0, 2);
$wgUploaders = array_filter($wgUploaders, "filterUploaders");

if ( count($wgUploaders) == 0 )
{
    $wgUploaders = array( 'classic' );
}

function filterUploaders($uploader)
{
    switch ($uploader)
    {
        case 'classic':
            // break;
        case 'flash':
            return true;
        default:
            return false;
    }
}

function formatBytes($aNumber)
{
    $aNumber = ($aNumber) ? (float) $aNumber : 0 ;

    if ( $aNumber < 1024 )
    {
        return round($aNumber) . " b";
    }

    $aUnits = array('TB', 'GB', 'MB', 'KB');
    $sUnit = null;

    while ( $aNumber > 875 && count($aUnits) )
    {
        $aNumber /= 1024;
        $sUnit = array_pop($aUnits);
    }

    return round($aNumber, 2) . " " . $sUnit;
}

function getFilter()
{
	$fileTypes = "*.*";
	$fileTypesDescription = wfEncodeJSString(wfMsg('Dialog.AttachFlash.all-files'));
	
	if ( isset($_GET['filter']) )
	{
		switch ($_GET['filter'])
		{
			case 'images':
				$fileTypes = "*.jpg;*.gif;*.png;*.bmp;*.jpeg";
				$fileTypesDescription = wfEncodeJSString(wfMsg('Dialog.AttachFlash.img-files'));
				break;
		}
	}
	
	return json_encode(array('fileTypes' => $fileTypes, 'fileTypesDescription' => $fileTypesDescription));
}

?><!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en" dir="ltr">
    <head>
        <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
        <title><?php echo(wfMsg('Dialog.AttachFlash.page-title')); ?></title>        

        <link rel="stylesheet" type="text/css" href="<?php echo $wgStylePath ?>/common/popups/css/styles.css" />
		<link rel="stylesheet" type="text/css" href="<?php echo $wgStylePath ?>/common/fonts.css" />
        <link rel="stylesheet" type="text/css" href="<?php echo $wgStylePath ?>/common/icons.css" />
        
        <link rel="stylesheet" type="text/css" href="<?php echo $wgStylePath ?>/common/yui/datatable/assets/skins/sam/datatable.css" />
        <link rel="stylesheet" type="text/css" href="<?php echo $wgStylePath ?>/common/yui/paginator/assets/skins/sam/paginator.css" />
        <link rel="stylesheet" type="text/css" href="<?php echo $wgStylePath ?>/common/yui/button/assets/skins/sam/button.css" />
        <link rel="stylesheet" type="text/css" href="<?php echo $wgStylePath ?>/common/yui/mindtouch/css/flash_uploader.css" />
        
        <script type="text/javascript" src="<?php echo $wgStylePath ?>/common/yui/yahoo-dom-event/yahoo-dom-event.js"></script>
        <script type="text/javascript" src="<?php echo $wgStylePath ?>/common/yui/element/element.js"></script>
        <script type="text/javascript" src="<?php echo $wgStylePath ?>/common/yui/datasource/datasource.js"></script>
        <script type="text/javascript" src="<?php echo $wgStylePath ?>/common/yui/datatable/datatable.js"></script>
        <script type="text/javascript" src="<?php echo $wgStylePath ?>/common/yui/paginator/paginator.js"></script>
        <script type="text/javascript" src="<?php echo $wgStylePath ?>/common/yui/button/button.js"></script>
        <script type="text/javascript" src="<?php echo $wgStylePath ?>/common/yui/json/json.js"></script>
        <script type="text/javascript" src="<?php echo $wgStylePath ?>/common/yui/cookie/cookie.js"></script>
        <script type="text/javascript" src="<?php echo $wgStylePath ?>/common/yui/connection/connection.js"></script>
        
        <?php /*include this to use the logger*/ //echo '<script type="text/javascript" src="/skins/common/yui/logger/logger.js"></script>'; ?>
        
        <script type="text/javascript" src="<?php echo $wgStylePath ?>/common/popups/popup.js"></script>
        <script type="text/javascript" src="<?php echo $wgStylePath ?>/common/yui/mindtouch/uploader.js"></script>
        
        <script type="text/javascript" src="<?php echo $wgStylePath ?>/common/swfupload/swfupload.js"></script>
        
        <script type="text/javascript">
        
            if (YAHOO.lang.isObject(YAHOO.widget.Logger))
            {
                //oLogReader = new YAHOO.widget.LogReader();
                // Enable logging to firebug
                YAHOO.widget.Logger.enableBrowserConsole();
            }
            
            var Uploader = null,
                FlashUploader = null,
                ClassicUploader = null,
                nFileSizeLimit = <?php echo DekiFileUpload::getUploadLimit(); ?>;
            
            function init()
            {
                var aTabs = [];
                
                <?php
                    foreach ($wgUploaders as $uploader)
                    {
                        echo "aTabs.push('{$uploader}');";
                    }
                ?>
                
                var filter = <?php echo getFilter(); ?>;
                
                var oConfig = {
                    tabs : aTabs,
                    defaultUploader : "<?php echo $wgUploaders[0] ?>",
                    fileSizeLimit : "<?php echo DekiFileUpload::getUploadLimit() . 'B' ?>",
                    fileTypes : filter.fileTypes,
                    fileTypesDescription : filter.fileTypesDescription
                };

				<?php if (in_array('flash', $wgUploaders)): ?>
				oConfig.sid = "<?php echo session_id() ?>";
				<?php endif; ?>
                
                Uploader = new YAHOO.mindtouch.Uploader(oConfig);                
            }
            
            YAHOO.mindtouch.FlashUploader.Lang = {
                
                FILE_NAME : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.file-name')); ?>", // Head
                PAGES     : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.pages')); ?>", // Paginator
                ALL_FILES : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.all-files')); ?>", // Select dialog
                IMG_FILES : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.img-files')); ?>", // Select dialog
                
                // Upload errors
                HTTP_ERROR : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.http-error')); ?>",
                MISSING_UPLOAD_URL : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.missing-upload-url')); ?>",
                IO_ERROR : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.io-error')); ?>",
                SECURITY_ERROR : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.security-error')); ?>",
                UPLOAD_LIMIT_EXCEEDED : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.upload-limit-exceeded')); ?>",
                UPLOAD_FAILED : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.upload-failed')); ?>",
                SPECIFIED_FILE_ID_NOT_FOUND : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.specified-file-id-not-found')); ?>",
                FILE_VALIDATION_FAILED : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.file-validation-failed')); ?>",
                FILE_CANCELLED : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.file-cancelled')); ?>",
                UPLOAD_STOPPED : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.upload-stopped')); ?>",
                UNKNOWN : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.unknown')); ?>",
                
                // Queue errors
                QUEUE_LIMIT_EXCEEDED : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.queue-limit-exceeded')); ?>",
                FILE_EXCEEDS_SIZE_LIMIT : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.file-exceeds-size-limit', formatBytes(DekiFileUpload::getUploadLimit()))); ?>",
                ZERO_BYTE_FILE : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.zero-byte-file')); ?>",
                INVALID_FILETYPE : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.invalid-filetype')); ?>",
                
                // Buttons
                SELECT_FILES : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.select-files')); ?>",
                CANCEL_FILE : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.cancel-file')); ?>",
                CANCEL_FILES : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.cancel-files')); ?>",
                CLOSE : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.close')); ?>",
            
                // Titles
                FILE_PROPERTIES : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.file-properties')); ?>",
                SELECTED_FILES : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.selected-files')); ?>",
                UPLOADING : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.uploading')); ?>",
                STATISTIC : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.statistic')); ?>",
                COMPLETE_WITH_ERRORS : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.complete-with-errors')); ?>",
            
                // Messages
                MSG_ADD_ERROR : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.msg-add-error')); ?>",
                MSG_UPLOAD_ERROR : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.msg-upload-error')); ?>",
                MSG_OTHER_FILES : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.msg-other-files')); ?>",
                MSG_MORE_INFO : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.msg-more-info')); ?>",
                
                // File status
                READY_TO_UPLOAD : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.ready-to-upload')); ?>", 
                IN_PROGRESS : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.in-progress')); ?>",
                ERROR : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.error')); ?>",
                UPLOADED : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.uploaded')); ?>",
                CANCELLED : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.cancelled')); ?>",
                
                // Labels
                SIZE : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.size')); ?>",
                TYPE : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.type')); ?>",
                CREATION_DATE : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.creation-date')); ?>",
                MODIFICATION_DATE : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.modification-date')); ?>",
                STATUS : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.status')); ?>",
                
                // Statistic
                FILE_SUCCESSFULLY_ADDED  : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.file-successfully-added')); ?>",
                FILES_SUCCESSFULLY_ADDED : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.files-successfully-added')); ?>",
                FILE_TOTALLY_ADDED : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.file-totally-added')); ?>",
                FILES_TOTALLY_ADDED : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.files-totally-added')); ?>",
                FILE_READY_TO_UPLOAD : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.file-ready-to-upload')); ?>",
                FILES_READY_TO_UPLOAD : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.files-ready-to-upload')); ?>",
                FILE_ADDED_WITH_ERROR : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.file-added-with-error')); ?>",
                FILES_ADDED_WITH_ERROR : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.files-added-with-error')); ?>",
                FILE_CANCELLED_BY_USER : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.file-cancelled-by-user')); ?>",
                FILES_CANCELLED_BY_USER : "<?php echo wfEncodeJSString(wfMsg('Dialog.AttachFlash.files-cancelled-by-user')); ?>"
            }
            
            YAHOO.util.Event.onDOMReady(init);
        </script>
        
    </head>
    <body class=" yui-skin-sam">
        <div class="tabs">
            <ul>
                <?php
                    foreach ($wgUploaders as $uploader) {
                        echo '<li class="' . $uploader . '"><a id="' . $uploader . '-tab" href="#"><span>' . wfMsg('Dialog.Attach.' . $uploader) . '</span></a></li>';
                    }
                ?>
            </ul>
            <div class="clearfix"></div>
        </div>
        <div id="mainContainer">
            <?php if ( array_search("flash", $wgUploaders) !== false ): ?>
            <div id="flashUploader" class="hidden">
                <div id="flashError"><?php echo(wfMsg('Dialog.AttachFlash.getflash')); ?></div>
                <div id="flashButton"></div>
                <div id="tableContainer">
                    <table class="container">
                        <tr class="yui-dt-even yui-dt-first">
                            <td><div id="selectedFiles"></div></td>
                            <td id="infoPanel" class="yui-dt yui-dt-noop">
                                <div class="yui-dt-bd">
                                    <table>
                                        <thead>
                                            <tr class="yui-dt-first yui-dt-last">
                                                <th rowspan="1" colspan="1" class="yui-dt-first yui-dt-last">
                                                  <div class="yui-dt-col-filename yui-dt-col-0 yui-dt-liner">
                                                      <span class="yui-dt-label" id="panel-title">&nbsp;</span>
                                                    </div>
                                                </th>
                                            </tr>
                                        </thead>
                                        <tbody class="">
                                            <tr style="" class="yui-dt-even yui-dt-first">
                                                <td>
                                                    <div class="yui-dt-col-0 yui-dt-liner" id="panel"></div>
                                                </td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </div>
                            </td>
                        </tr>
                    </table>
                </div>
                <div id="paginator"></div>
            </div> <!-- #flashUploader -->
            <?php endif; ?>
            
            <?php if ( array_search("classic", $wgUploaders) !== false ): ?>
            <div id="classicUploader" class="hidden">
                <div id="form">
                    <form id="frmAttach" method="post" enctype="multipart/form-data" action="">
                        <div class="table">
                            <table cellspacing="2" cellpadding="0" border="0" id="attachFiles" class="table" width="100%">
                            <colgroup>
                                <col width="40"/>
                                <col width="250"/>
                                <col width="275"/>
                            </colgroup>
                            <tr>
                                <th>&nbsp;</th>
                                <th><?php echo wfMsg('Dialog.Attach.table-header-file'); ?></th>
                                <th><?php echo wfMsg('Dialog.Attach.table-header-description'); ?></th>
                            </tr>
                            <?php
                                $tabindex = 1;
                                $html = '';
                                for ($i = 1; $i < 4; $i++) {
                                    $html .= '<tr>
                                            <td><a href="#" onclick="return ClassicUploader.addRow()" title="' . wfMsg('Dialog.Attach.attach-another-file').'">' . Skin::iconify('attach-add') . '</a><a 
                                                href="#"  onclick="return ClassicUploader.delRow(this.parentNode.parentNode.rowIndex)" title="' . wfMsg('Dialog.Attach.delete').'">' . Skin::iconify('attach-remove') . '</a></td>
                                            <td><input name="file_' . $i . '" id="file_' . $i . '" size="40" type="file" style="font: 11px;" tabindex="' . $tabindex . '"/></td>
                                            <td><input type="text" name="filedesc_' . $i . '" id="filedesc_' . $i . '" tabindex="' . (++$tabindex) . '" style="width:98%"/></td>
                                        </tr>';
                                }
                                echo $html;
                            ?>
                            </table>
                        </div>
                    </form>
                </div>
                <div id="waiting" style="display: none; font: 16px Verdana;">
                    <img src="/skins/common/icons/anim-circle.gif" alt="" />&nbsp;<?php echo wfMsg('Dialog.Attach.wait-while-files-upload'); ?>
                </div>
            </div> <!-- #classicUploader -->
            <?php endif; ?>
        </div>
    </body>
</html>
