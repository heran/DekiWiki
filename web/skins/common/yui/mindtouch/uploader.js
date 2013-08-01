/*
 * MindTouch Deki - enterprise collaboration and integration platform
 * Copyright (C) 2006-2009 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
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

YAHOO.namespace("YAHOO.mindtouch.Uploader");
YAHOO.namespace("YAHOO.mindtouch.ClassicUploader");
YAHOO.namespace("YAHOO.mindtouch.FlashUploader");

YAHOO.mindtouch.Uploader = function(oConfig)
{
	this.oConfig = oConfig || {};
	this._tabsLocked = false;

	this._oTabs = {};

	for ( var i = 0; i < oConfig.tabs.length; i++ )
	{
		this.addTab(oConfig.tabs[i]);
	}

	this._cookieUploader = YAHOO.util.Cookie.get("uploader");

	if ( this._cookieUploader && ! this.getTab(this._cookieUploader) )
	{
		this._cookieUploader = null;
	}

	this._sUploader = this._cookieUploader || oConfig.defaultUploader;

	Popup.init({
		handlers : {
			submit : function() {
				switch ( this._sUploader )
				{
					case "flash": FlashUploader.startUpload(); break;
					case "classic": ClassicUploader.startUpload(); break;
				}
			},
			cancel : function() {
				switch ( this._sUploader )
				{
					case "flash": FlashUploader.cancelUpload(); break;
					case "classic": ClassicUploader.cancelUpload(); break;
				}
			}
		},
		autoClose : false,
		scope : this
	});

	var oParams = Popup.getParams();
	this.oConfig.uploaderUrl = "/deki/gui/fileupload.php?pageId=" + oParams.titleID;
	this.oConfig.flashUrl = oParams.commonPath + "/swfupload/swfupload.swf";
	this.oConfig.processImg = oParams.commonPath + "/popups/images/uploading.gif";
	this.oConfig.flashButtonImage = oParams.commonPath + "/swfupload/XPButtonNoText_61x22.png";

	this.selectTab(this._sUploader);
}

YAHOO.mindtouch.Uploader.prototype.addTab = function(tabName)
{
	this._oTabs[tabName] = {};

	this._oTabs[tabName].tab = YAHOO.util.Dom.get(tabName + "-tab");
	this._oTabs[tabName].container = YAHOO.util.Dom.get(tabName + "Uploader");

	YAHOO.util.Event.addListener(this._oTabs[tabName].tab, "click", function(ev, tabName) {
		this.selectTab(tabName);
	}, tabName, this);
}

YAHOO.mindtouch.Uploader.prototype.getTab = function(tabName)
{
	return this._oTabs[tabName] || null;
}

YAHOO.mindtouch.Uploader.prototype.selectTab = function(tabName)
{
	if ( this._tabsLocked )
		return;

	if ( this._cookieUploader || (this._sUploader != tabName) )
	{
		var date = new Date();
		date.setMonth(date.getMonth() + 1);
		YAHOO.util.Cookie.set("uploader", tabName, {expires : date});
	}

	this._sUploader = tabName;

	var flash = this.getTab("flash");
	var classic = this.getTab("classic");

	switch ( tabName )
	{
		case "flash":

			if ( classic )
			{
				YAHOO.util.Dom.removeClass(classic.tab.parentNode, "active");
				YAHOO.util.Dom.addClass(classic.container, "hidden");
			}

			if ( flash )
			{
				YAHOO.util.Dom.addClass(flash.tab.parentNode, "active");
				YAHOO.util.Dom.removeClass(flash.container, "hidden");
			}

			if ( ! FlashUploader )
			{
				FlashUploader = new YAHOO.mindtouch.FlashUploader(this.oConfig);
				Popup.disableButton(Popup.BTN_OK);
				FlashUploader.loadUploader();
			}

			FlashUploader.updateButtons();

			break;
		case "classic":

			if ( flash )
			{
				YAHOO.util.Dom.removeClass(flash.tab.parentNode, "active");
				YAHOO.util.Dom.addClass(flash.container, "hidden");
			}

			if ( classic )
			{
				YAHOO.util.Dom.addClass(classic.tab.parentNode, "active");
				YAHOO.util.Dom.removeClass(classic.container, "hidden");
			}

			if ( ! ClassicUploader )
			{
				ClassicUploader = new YAHOO.mindtouch.ClassicUploader(this.oConfig);
			}

			ClassicUploader.updateButtons();

			break;
		default:
			break;
	}

	window.setTimeout( function() {
		Popup.resize({height: "auto"});
	}, 0);
}

YAHOO.mindtouch.Uploader.prototype.lockTabs = function()
{
	this._tabsLocked = true;
}

YAHOO.mindtouch.Uploader.prototype.unlockTabs = function()
{
	this._tabsLocked = false;
}

YAHOO.mindtouch.Uploader.prototype.formatBytes = function(aNumber)
{
	aNumber = (aNumber) ? Number(aNumber) : 0 ;

	if ( aNumber < 1024 )
	{
		return aNumber.toFixed(0) + " b";
	}

	var aUnits = ['TB','GB','MB','KB'];
	var sUnit;

	while ( aNumber > 875 && aUnits.length )
	{
		aNumber /= 1024;
		sUnit = aUnits.pop();
	}

	return aNumber.toFixed(2) + " " + sUnit;
}

/**
 * Classic Uploader
 */

YAHOO.mindtouch.ClassicUploader = function(oConfig)
{
	this.oConfig = oConfig ? oConfig : {};
	this.rowCount = 3;
	this.table = YAHOO.util.Dom.get('attachFiles');

	this.oConn = null;
}

YAHOO.mindtouch.ClassicUploader.prototype.startUpload = function()
{
	var elForm = YAHOO.util.Dom.get('frmAttach');
	var elLoading = YAHOO.util.Dom.get('waiting');
	var uploadedFiles = [];
	
	elForm.style.display = 'none';
	elLoading.style.display = 'block';

	Uploader.lockTabs();
	Popup.disableButton(Popup.BTN_OK);

	//the second argument of setForm is crucial,
	//which tells Connection Manager this is a file upload form
	YAHOO.util.Connect.setForm('frmAttach', true);

	var closeDialog = function()
	{
		// wait when YUI.Connect finishes itself operations
		// and close the dialog
		setTimeout(function() { Popup.close(uploadedFiles); }, 500)
	}

	YAHOO.util.Connect.uploadEvent.subscribe(closeDialog);
	YAHOO.util.Connect.abortEvent.subscribe(closeDialog);

	var uploadHandler = {
		upload: function(o) {
			try
			{
				var response = YAHOO.lang.JSON.parse( o.responseText );
				if ( response.success )
				{
					uploadedFiles = response.files;
				}
			}
			catch (ex) {}
		}
	};

	this.oConn = YAHOO.util.Connect.asyncRequest('POST', this.oConfig.uploaderUrl, uploadHandler);
}

YAHOO.mindtouch.ClassicUploader.prototype.cancelUpload = function()
{
	if ( this.oConn && YAHOO.util.Connect.isCallInProgress(this.oConn) )
	{
		YAHOO.util.Connect.abort(this.oConn);
	}
	else
	{
		Popup.close();
	}
}

YAHOO.mindtouch.ClassicUploader.prototype.addRow = function()
{
	var newRow = this.table.insertRow(this.table.rows.length);

	++this.rowCount;

	newRow.insertCell(0).innerHTML = '<a href="#" onclick="return ClassicUploader.addRow()"><span class="icon"><img src="/skins/common/icons/icon-trans.gif" class="attach-add" alt=""></span></a><a href="#" onclick="return ClassicUploader.delRow(this.parentNode.parentNode.rowIndex)"><span class="icon"><img src="/skins/common/icons/icon-trans.gif" class="attach-remove"></span></a>';
	newRow.insertCell(1).innerHTML = '<input name="file_' + this.rowCount + '" id="file_' + this.rowCount + '" size="40" type="file"/>';
	newRow.insertCell(2).innerHTML = '<input type="text" name="filedesc_' + this.rowCount + '" style="width:98%"/>';

	Popup.resize({height: "auto"});
	return false;
};

YAHOO.mindtouch.ClassicUploader.prototype.delRow = function(indexKey)
{
	if ( this.table.rows.length > 2 )
	{
		this.table.deleteRow(indexKey);

		for ( var j = indexKey + 1; j < this.table.rows.length; j++ )
		{
			if (this.table.rows[j] && typeof(this.table.moveRow) == 'function')
			{
				this.table.moveRow(j, j - 1);
			}
		}

		Popup.resize({height: "auto"});
	}
	else
	{
		// close the dialog
		Popup.close();
	}

	return false;
};

YAHOO.mindtouch.ClassicUploader.prototype.updateButtons = function()
{
	Popup.enableButton(Popup.BTN_OK);
	Popup.enableButton(Popup.BTN_CANCEL);
}

/**
 * Flash Uploader
 */

YAHOO.mindtouch.FlashUploader = function(oConfig)
{
	this.init(oConfig);
}

YAHOO.mindtouch.FlashUploader.prototype = {

	oConfig : {},
	oTable : null,
	oPaginator: null,
	oPanel : null,
	nFilesPerPage : 10,
	bFlashLoaded : false,
	bUploadStarted : false,
	bTableLocked : false,
	aUploadedFiles : [],
	aErrorFiles : [],
	nAddedFiles : 0,

	init : function(oConfig)
	{
		this.oConfig = oConfig ? oConfig : {};

		var oColumnDefs = [
			{key:"filename", label:YAHOO.mindtouch.FlashUploader.Lang.FILE_NAME, sortable:false}
		];

		this.oPaginator = new YAHOO.widget.Paginator({
			rowsPerPage: this.nFilesPerPage,
			pageLinks: 20,
			containers: ['paginator'],
			alwaysVisible: false,
			template: YAHOO.mindtouch.FlashUploader.Lang.PAGES + ": {PageLinks}"
		});

		var oTableConfigs = {
			paginator: this.oPaginator
		}

		var aData = [];
		for ( var i = 0; i < this.nFilesPerPage; i++ )
		{
			aData.push({filename: "&nbsp;", description: "", file: null});
		}

		oDataSource = new YAHOO.util.DataSource(aData);
		oDataSource.responseType = YAHOO.util.DataSource.TYPE_JSARRAY;
		oDataSource.responseSchema = {
			fields: ["filename","description","file"]
		};

		this.oTable = new YAHOO.widget.DataTable("selectedFiles", oColumnDefs, oDataSource, oTableConfigs);
		this.oTable.subscribe("rowClickEvent", onRowClick, this, true);
		this.oTable.subscribe("refreshEvent", onViewRefresh, this, true);

		function onRowClick(oArgs)
		{
			if ( this.bTableLocked )
				return false;

			var evt = oArgs.event;
			var elTarget = oArgs.target;

			var oTargetRecord = this.oTable.getRecord(elTarget);
			var oTargetData = oTargetRecord.getData();

			var aSelectedRows = this.oTable.getSelectedRows();
			this.saveFileDescription(aSelectedRows);
			this.oTable.onEventSelectRow(oArgs);

			if ( aSelectedRows.length > 0 && (evt.ctrlKey || evt.shiftKey) )
			{
				var aFiles = [];

				var aSelectedRows = this.oTable.getSelectedRows(); // update selected rows after onEventSelectRow
				for ( var i = 0; i < aSelectedRows.length; i++ )
				{
					var oData = this.oTable.getRecord(aSelectedRows[i]).getData();
					if ( oData.file )
					{
						aFiles.push(oData.file);
					}
				}

				this.oPanel.setFiles(aFiles);
			}
			else
			{
				this.oPanel.setFile(oTargetData.file, oTargetData.description);
			}
		}

		function onViewRefresh()
		{
			var aPageRecords = this.oPaginator.getPageRecords();

			for ( var i = aPageRecords[0]; i < aPageRecords[1] + 1; i++ )
			{
				this.refreshRow(this.oTable.getRecord(i));
			}
		}

		this.oPanel = new YAHOO.mindtouch.FlashUploader.Panel({el: "panel", elTitle: "panel-title"});
	},

	loadUploader : function()
	{
		var Handlers = YAHOO.mindtouch.FlashUploader.Handlers;

		this.oUploader = new SWFUpload({
			// Backend Settings
			upload_url: this.oConfig.uploaderUrl,

			post_params : { 'uploader' : 'flash', 'swfupload_sid' : this.oConfig.sid },

			// File Upload Settings
			file_size_limit : this.oConfig.fileSizeLimit,
			file_types : this.oConfig.fileTypes,
			file_types_description : this.oConfig.fileTypesDescription,
			file_upload_limit : 0, // no limit

			// Event Handler Settings
			swfupload_loaded_handler : Handlers.swfUploadLoaded,
			file_dialog_start_handler : Handlers.fileDialogStart,
			file_queued_handler : Handlers.fileQueued,
			file_queue_error_handler : Handlers.fileQueueError,
			file_dialog_complete_handler : Handlers.fileDialogComplete,
			upload_start_handler : Handlers.uploadStart,
			upload_success_handler : Handlers.uploadSuccess,
			upload_complete_handler : Handlers.uploadComplete,
			upload_progress_handler : Handlers.uploadProgress,
			upload_error_handler : Handlers.uploadError,

			// Flash Settings
			flash_url : this.oConfig.flashUrl,

			button_placeholder_id : 'flashButton',
			button_width : 61,
			button_height : 22,
			button_image_url : this.oConfig.flashButtonImage,
			button_text : YAHOO.mindtouch.FlashUploader.Lang.SELECT_FILES,
			button_action : SWFUpload.BUTTON_ACTION.SELECT_FILES,
			button_disabled : false,
			button_cursor : SWFUpload.CURSOR.HAND,
			button_window_mode : SWFUpload.WINDOW_MODE.TRANSPARENT,

			// Debug Settings
			debug: false
		});
	},

	saveFileDescription : function(aSelectedRows)
	{
		aSelectedRows = aSelectedRows || this.oTable.getSelectedRows();
		if ( aSelectedRows.length == 1 )
		{
			var oData = this.oTable.getRecord(aSelectedRows[0]).getData();
			var oFile = oData.file;
			if ( oFile )
			{
				var elDescription = YAHOO.util.Dom.get(YAHOO.mindtouch.FlashUploader.Panel.INPUT_DESC_ID);
				var sDescription = ( elDescription ) ? elDescription.value : "";
				
				try
				{
					this.oUploader.addFileParam(oFile.id, "filedescription", sDescription);
				}
				catch (ex)
				{
					this.oUploader.debug(ex);
				}
				
				this.setFile(oFile, sDescription);
			}
		}
	},

	startUpload : function()
	{
		try
		{
			var oStats = this.oUploader.getStats();
			if ( oStats && oStats.files_queued < 1 )
			{
				throw "There are no files in queue.";
			}

			this.bUploadStarted = true;
			this.lockTable();
			Uploader.lockTabs();

			this.saveFileDescription();
			this.aErrorFiles = [];

			this.oTable.unselectAllRows();
			this.updateButtons();

			this.oUploader.startUpload();
			FlashUploader.oPanel.setUploading();
		}
		catch (ex)
		{
			this.oUploader.debug(ex);
		}
	},

	cancelUpload : function()
	{
		if ( this.bFlashLoaded )
		{
			try
			{
				var nFilesLeft = this.oUploader.getStats().files_queued;
	
				while ( nFilesLeft > 0 )
				{
					this.oUploader.cancelUpload();
					nFilesLeft = this.oUploader.getStats().files_queued;
				}
			}
			catch (ex)
			{
				this.oUploader.debug(ex);
			}

			if ( this.bUploadStarted )
			{
				Popup.close(FlashUploader.aUploadedFiles);
			}
		}

		Popup.close();
	},

	cancelFiles : function()
	{
		var aSelectedFiles = this.oTable.getSelectedRows();

		for ( var i in aSelectedFiles )
		{
			var oData = this.oTable.getRecord(aSelectedFiles[i]).getData();
			
			try
			{
				this.oUploader.cancelUpload(oData.file.id);
			}
			catch (ex)
			{
				this.oUploader.debug(ex);
			}
		}

		if ( aSelectedFiles.length > 1 )
		{
			this.oTable.unselectAllRows();
			this.oPanel.setStat();
		}

		this.updateButtons();
	},

	setFile : function (oFile, sDescription)
	{
		var oRecord = this.oTable.getRecord(oFile.index);

		if ( oRecord )
		{
			// File is already in the queue
			this.updateFile(oFile, sDescription || oRecord.getData().description);
		}
		else
		{
			this.addFile(oFile);
		}

		oRecord = this.oTable.getRecord(oFile.index);
		this.refreshRow(oRecord);
	},

	addFile : function(oFile, sDescription)
	{
		sDescription = ( sDescription ) ? sDescription : "";
		var sFileName = ( oFile ) ? oFile.name : "&nbsp;";

		var oRow = {filename: sFileName, description: sDescription, file: oFile};

		this.oTable.addRow(oRow);
	},

	updateFile : function(oFile, sDescription)
	{
		var oNewData = {filename: oFile.name, description: sDescription, file: oFile};
		this.oTable.updateRow(oFile.index, oNewData);
	},

	refreshRow : function(oRecord)
	{
		var oFile = oRecord.getData().file;
		var oCol = this.oTable.getColumn(0);

		var oTr = this.oTable.getTrEl(oRecord);
		var oTd = this.oTable.getTdEl({record: oRecord, column: oCol});

		if ( oTr && oTd ) // file is on the current page
		{
			if ( oFile )
			{
				oTd.setAttribute("title", oFile.name);
			}

			if ( oFile && oFile.error && oFile.error.length > 0 )
			{
				YAHOO.util.Dom.addClass(oTr, "file-error");
			}
			else
			{
				YAHOO.util.Dom.removeClass(oTr, "file-error");
			}
		}
	},

	updateButtons : function()
	{
		var oStats;

		if ( ! this.bFlashLoaded )
		{
			Popup.disableButton(Popup.BTN_OK);
			return;
		}

		try
		{
			oStats = this.oUploader.getStats();
		}
		catch (ex)
		{
			this.oUplader.debug(ex);
		}
		
		if ( (oStats && oStats.files_queued < 1) || this.bUploadStarted )
		{
			Popup.disableButton(Popup.BTN_OK);
		}
		else
		{
			Popup.enableButton(Popup.BTN_OK);
		}
	},

	lockTable : function()
	{
		this.bTableLocked = true;
	},

	unlockTable : function()
	{
		this.bTableLocked = false;
	},

	checkFlashVersion : function()
	{
		  var bResult = false;
		  var nVersion = 0;

		  if ( YAHOO.env.ua.ie )
		  {
				try
				{
					if ( eval('new ActiveXObject("ShockwaveFlash.ShockwaveFlash.9")') )
					{
						  bResult = true;
					}
				} catch(e) {}
		  }
		  else
		  {
				for ( var i = 0; i < navigator.plugins.length; i++ )
				{
					  if ( navigator.plugins[i].name.indexOf('Flash') > -1 )
					  {
							var nFlashVer = parseInt(navigator.plugins[i].description.substr(16));
							nVersion = (nFlashVer > nVersion) ? nFlashVer : nVersion;
							break;
					  }
				}
				bResult = (nVersion > 8);
		  }
		  return bResult;
	}
}

/**
 * Handlers
 */


YAHOO.mindtouch.FlashUploader.Handlers = {

	swfUploadLoaded : function()
	{
		if ( FlashUploader.checkFlashVersion() )
		{
			YAHOO.util.Dom.get('flashError').style.display = 'none';
			FlashUploader.bFlashLoaded = true;
			FlashUploader.oPanel.setStat();
			Popup.resize({height: "auto"});
		}
	},

	fileDialogStart : function()
	{
	},

	fileQueued : function(oFile, sDescription)
	{
		FlashUploader.setFile.apply(FlashUploader, arguments);
	},

	fileDialogComplete : function(nFilesQueued)
	{
		try
		{
			if ( nFilesQueued > 0 )
			{
				var nFiles = FlashUploader.oTable.getRecordSet().getLength();

				if ( nFiles % FlashUploader.nFilesPerPage > 0 )
				{
					for ( var i = 0; i < (FlashUploader.nFilesPerPage - (nFiles % FlashUploader.nFilesPerPage)); i++ )
					{
						FlashUploader.addFile.call(FlashUploader, null);
					}
				}

				FlashUploader.nAddedFiles += nFilesQueued;

				FlashUploader.oTable.unselectAllRows();
				FlashUploader.oTable.refreshView();

				FlashUploader.oPanel.setStat(nFilesQueued);

				Popup.resize({height: "auto"});
			}

			FlashUploader.updateButtons();
		}
		catch (ex)
		{
			this.debug(ex);
		}
	},

	uploadStart : function(oFile)
	{
		try
		{
			FlashUploader.oPanel.setFileUploading(oFile, 0);
		}
		catch (ex) { this.debug(ex); }

		return true;
	},

	uploadProgress : function(oFile, nBytesLoaded)
	{
		try
		{
			var nPercent = Math.ceil((nBytesLoaded / oFile.size) * 100)
			if ( nPercent < 10 )
			{
				nPercent = "  " + nPercent;
			}
			else if ( nPercent < 100 )
			{
				nPercent = " " + nPercent;
			}

			FlashUploader.oPanel.setFileUploading(oFile, nPercent);

		} catch (ex) { this.debug(ex); }
	},

	uploadSuccess : function(oFile, sServerData)
	{
		try
		{
			var oResult = YAHOO.lang.JSON.parse(sServerData);

			if ( oResult.success )
			{
				FlashUploader.aUploadedFiles.push(oResult.files.shift());
				FlashUploader.setFile(oFile);
				FlashUploader.oPanel.setFileUploading(oFile, 100);
			}
		} catch (ex) { this.debug(ex); }
	},

	uploadComplete : function(oFile)
	{
		try
		{
			if ( this.getStats().files_queued > 0 )
			{
				FlashUploader.oUploader.startUpload();
			}
			else
			{
				if ( FlashUploader.aErrorFiles.length == 0 )
				{
					Popup.close(FlashUploader.aUploadedFiles);
				}
				else
				{
					var oButton = Popup.getButton(Popup.BTN_CANCEL);
					oButton.set("label", YAHOO.mindtouch.FlashUploader.Lang.CLOSE);
					FlashUploader.unlockTable();
					Uploader.unlockTabs();
					FlashUploader.oPanel.setUploadingError();
				}
			}
		}
		catch (ex) { this.debug(ex); }
	},

	uploadError : function(oFile, nErrorCode, sMessage) {
		try
		{
			var sError;

			switch( nErrorCode )
			{
				case SWFUpload.UPLOAD_ERROR.HTTP_ERROR:
					sError = YAHOO.mindtouch.FlashUploader.Lang.HTTP_ERROR;
				break;
				case SWFUpload.UPLOAD_ERROR.MISSING_UPLOAD_URL:
					sError = YAHOO.mindtouch.FlashUploader.Lang.MISSING_UPLOAD_URL;
					break;
				case SWFUpload.UPLOAD_ERROR.IO_ERROR:
					sError = YAHOO.mindtouch.FlashUploader.Lang.IO_ERROR;
					break;
				case SWFUpload.UPLOAD_ERROR.SECURITY_ERROR:
					sError = YAHOO.mindtouch.FlashUploader.Lang.SECURITY_ERROR;
					break;
				case SWFUpload.UPLOAD_ERROR.UPLOAD_LIMIT_EXCEEDED:
					sError = YAHOO.mindtouch.FlashUploader.Lang.UPLOAD_LIMIT_EXCEEDED;
					break;
				case SWFUpload.UPLOAD_ERROR.UPLOAD_FAILED:
					sError = YAHOO.mindtouch.FlashUploader.Lang.UPLOAD_FAILED;
					break;
				case SWFUpload.UPLOAD_ERROR.SPECIFIED_FILE_ID_NOT_FOUND:
					sError = YAHOO.mindtouch.FlashUploader.Lang.SPECIFIED_FILE_ID_NOT_FOUND;
					break;
				case SWFUpload.UPLOAD_ERROR.FILE_VALIDATION_FAILED:
					sError = YAHOO.mindtouch.FlashUploader.Lang.FILE_VALIDATION_FAILED;
					break;
				case SWFUpload.UPLOAD_ERROR.FILE_CANCELLED:
					sError = YAHOO.mindtouch.FlashUploader.Lang.FILE_CANCELLED;
					break;
				case SWFUpload.UPLOAD_ERROR.UPLOAD_STOPPED:
					sError = YAHOO.mindtouch.FlashUploader.Lang.FILE_STOPPED;
					break;
				default:
					sError = YAHOO.mindtouch.FlashUploader.Lang.UNKNOWN;
					break;
			}

			if ( FlashUploader.bUploadStarted )
			{
				FlashUploader.aErrorFiles.push(oFile);
			}

			oFile.error = sError;
			FlashUploader.setFile(oFile);

			if ( FlashUploader.oTable.getSelectedRows().length == 1 && ! FlashUploader.bUploadStarted )
			{
				var oData = FlashUploader.oTable.getRecord(oFile.index).getData();
				FlashUploader.oPanel.setFile(oData.file, oData.description);
			}

		} catch (ex) { this.debug(ex);}
	},

	fileQueueError : function(oFile, nErrorCode, sMessage) {
		try
		{
			var sError;
			switch( nErrorCode )
			{
				case SWFUpload.QUEUE_ERROR.QUEUE_LIMIT_EXCEEDED:
					sError = YAHOO.mindtouch.FlashUploader.Lang.QUEUE_LIMIT_EXCEEDED;
					break;
				case SWFUpload.QUEUE_ERROR.FILE_EXCEEDS_SIZE_LIMIT:
					sError = YAHOO.mindtouch.FlashUploader.Lang.FILE_EXCEEDS_SIZE_LIMIT;
					break;
				case SWFUpload.QUEUE_ERROR.ZERO_BYTE_FILE:
					sError = YAHOO.mindtouch.FlashUploader.Lang.ZERO_BYTE_FILE;
					break;
				case SWFUpload.QUEUE_ERROR.INVALID_FILETYPE:
					sError = YAHOO.mindtouch.FlashUploader.Lang.INVALID_FILE_TYPE;
					break;
				default:
					sError = YAHOO.mindtouch.FlashUploader.Lang.UNKNOWN;
					break;
			}

			FlashUploader.aErrorFiles.push(oFile);

			oFile.error = sError;
			FlashUploader.setFile(oFile);

		} catch (ex) { this.debug(ex);}
	}
}


/**
 * Informational panel
 */


YAHOO.mindtouch.FlashUploader.Panel = function(oConfig) {
	if ( YAHOO.lang.isValue(oConfig) )
	{
		this.init(oConfig);
	}
}

YAHOO.mindtouch.FlashUploader.Panel.INPUT_DESC_ID = "file-description";

YAHOO.mindtouch.FlashUploader.Panel.prototype = {

	init : function(oConfig)
	{
		oConfig = this.oConfig = oConfig || {};

		this.oContainer = YAHOO.util.Dom.get(oConfig.el);
		this.oTitle = YAHOO.util.Dom.get(oConfig.elTitle);
	},

	setTitie : function(sTitle)
	{
		this.oTitle.innerHTML = sTitle;
	},

	clear : function()
	{
		var aChildern = YAHOO.util.Dom.getChildren(this.oContainer);

		for ( var i = 0; i < aChildern.length; i++ )
		{
			this.oContainer.removeChild(aChildern[i]);
		}
	},

	setFile : function(oFile, sDescription)
	{
		var elDiv, elInput, elLabel, elText, sFileStatus, oButtons,
			Lang = YAHOO.mindtouch.FlashUploader.Lang;

		this.clear();

		if ( oFile )
		{
			elDiv = document.createElement("div");
			elLabel = document.createElement("label");
			elLabel.appendChild(document.createTextNode("Description"));
			elLabel.className = "panel-inner-title panel-inner-title-description";
			elLabel.setAttribute("for", "file-description");

			elInput = document.createElement("input");

			elInput.value = sDescription;
			elInput.type = "text";
			elInput.id = YAHOO.mindtouch.FlashUploader.Panel.INPUT_DESC_ID;

			elDiv.appendChild(elLabel);
			elDiv.appendChild(elInput);

			this.oContainer.appendChild(elDiv);

			elInput.focus();
			YAHOO.util.Event.addListener(elInput, "blur", function(ev) { this.saveFileDescription() }, FlashUploader, true);

			// file size information
			elDiv = document.createElement("div");
			elDiv.className = "panel-inner-block";

			elLabel = document.createElement("label");
			elLabel.appendChild(document.createTextNode(Lang.SIZE + ": "));
			elLabel.className = "panel-inner-title";

			elText = document.createTextNode(Uploader.formatBytes(oFile.size));
			elDiv.appendChild(elLabel);
			elDiv.appendChild(elText);
			this.oContainer.appendChild(elDiv);

			// file type information
			elDiv = document.createElement("div");
			elDiv.className = "panel-inner-block";

			elLabel = document.createElement("label");
			elLabel.appendChild(document.createTextNode(Lang.TYPE + ": "));
			elLabel.className = "panel-inner-title";

			elText = document.createTextNode(oFile.type);
			elDiv.appendChild(elLabel);
			elDiv.appendChild(elText);
			this.oContainer.appendChild(elDiv);

			// file creation date information
			elDiv = document.createElement("div");
			elDiv.className = "panel-inner-block";

			elLabel = document.createElement("label");
			elLabel.appendChild(document.createTextNode(Lang.CREATION_DATE + ": "));
			elLabel.className = "panel-inner-title";

			elText = document.createTextNode(oFile.creationdate);
			elDiv.appendChild(elLabel);
			elDiv.appendChild(elText);
			this.oContainer.appendChild(elDiv);

			// file modification date information
			elDiv = document.createElement("div");
			elDiv.className = "panel-inner-block";

			elLabel = document.createElement("label");
			elLabel.appendChild(document.createTextNode(Lang.MODIFICATION_DATE + ": "));
			elLabel.className = "panel-inner-title";

			elText = document.createTextNode(oFile.modificationdate);
			elDiv.appendChild(elLabel);
			elDiv.appendChild(elText);
			this.oContainer.appendChild(elDiv);

			// file status information
			elDiv = document.createElement("div");
			elDiv.className = "panel-inner-block";

			elLabel = document.createElement("label");
			elLabel.appendChild(document.createTextNode(Lang.STATUS + ": "));
			elLabel.className = "panel-inner-title";

			switch ( oFile.filestatus )
			{
				case SWFUpload.FILE_STATUS.QUEUED: sFileStatus = Lang.READY_TO_UPLOAD; break;
				case SWFUpload.FILE_STATUS.IN_PROGRESS: sFileStatus = Lang.IN_PROGRESS; break;
				case SWFUpload.FILE_STATUS.ERROR: sFileStatus = Lang.ERROR; break;
				case SWFUpload.FILE_STATUS.COMPLETE: sFileStatus = Lang.UPLOADED; break;
				case SWFUpload.FILE_STATUS.CANCELLED: sFileStatus = Lang.CANCELLED; break;
			}

			var elText = document.createTextNode(sFileStatus);
			elDiv.appendChild(elLabel);
			elDiv.appendChild(elText);
			this.oContainer.appendChild(elDiv);

			if ( oFile.error && oFile.error.length > 0 )
			{
				// file error information
				elDiv = document.createElement("div");
				elDiv.className = "panel-inner-block";

				elLabel = document.createElement("label");
				elLabel.appendChild(document.createTextNode(Lang.ERROR + ": "));
				elLabel.className = "panel-inner-title";

				elText = document.createTextNode(oFile.error);
				elDiv.appendChild(elLabel);
				elDiv.appendChild(elText);
				this.oContainer.appendChild(elDiv);

				elInput.setAttribute("readOnly", "readonly"); // the case is important for IE
			}
			else
			{
				oButtons = { cancel : { label : Lang.CANCEL_FILE } };
			}

			this.createButtons(oButtons);
			this.setTitie(Lang.FILE_PROPERTIES);
		}
		else
		{
			this.setStat();
		}
	},

	setFiles : function(aFiles)
	{
		var elDiv,
			Lang = YAHOO.mindtouch.FlashUploader.Lang;

		this.clear();

		for ( var i = 0; i < aFiles.length; i++ )
		{
			elDiv = document.createElement("div");
			elDiv.innerHTML = aFiles[i].name;
			this.oContainer.appendChild(elDiv);
		}

		this.createButtons({
			cancel : { label : Lang.CANCEL_FILES }
		});
		this.setTitie(Lang.SELECTED_FILES);
	},

	setStat : function(nAddedFiles)
	{
		var elDiv, oStat, sMsg,
			Lang = YAHOO.mindtouch.FlashUploader.Lang;

		this.clear();
		this.createButtons();

		if ( nAddedFiles )
		{
			elDiv = document.createElement("div");
			elDiv.className = "panel-inner-title panel-inner-block";
			sMsg = ( nAddedFiles == 1 ) ? Lang.FILE_SUCCESSFULLY_ADDED : Lang.FILES_SUCCESSFULLY_ADDED.replace("$1", nAddedFiles);
			elDiv.appendChild(document.createTextNode(sMsg));
			this.oContainer.appendChild(elDiv);
		}
		else
		{
			elDiv = document.createElement("div");
			elDiv.className = "panel-inner-block";
			sMsg = ( FlashUploader.nAddedFiles == 1 ) ? Lang.FILE_TOTALLY_ADDED : Lang.FILES_TOTALLY_ADDED.replace("$1", FlashUploader.nAddedFiles);
			elDiv.appendChild(document.createTextNode(sMsg));
			this.oContainer.appendChild(elDiv);

			try
			{
				oStat = FlashUploader.oUploader.getStats();
			}
			catch (ex)
			{
				FlashUploader.oUploader.debug(ex);
			}

			if ( oStat )
			{
				elDiv = document.createElement("div");
				sMsg = ( oStat.files_queued == 1 ) ? Lang.FILE_READY_TO_UPLOAD : Lang.FILES_READY_TO_UPLOAD.replace("$1", oStat.files_queued);
				elDiv.appendChild(document.createTextNode(sMsg));
				this.oContainer.appendChild(elDiv);
	
				elDiv = document.createElement("div");
				sMsg = ( oStat.queue_errors == 1 ) ? Lang.FILE_ADDED_WITH_ERROR : Lang.FILES_ADDED_WITH_ERROR.replace("$1", oStat.queue_errors);
				elDiv.appendChild(document.createTextNode(sMsg));
				this.oContainer.appendChild(elDiv);
	
				elDiv = document.createElement("div");
				sMsg = ( oStat.upload_cancelled == 1 ) ? Lang.FILE_CANCELLED_BY_USER : Lang.FILES_CANCELLED_BY_USER.replace("$1", oStat.upload_cancelled);
				elDiv.appendChild(document.createTextNode(sMsg));
				this.oContainer.appendChild(elDiv);
			}
		}

		if ( nAddedFiles && FlashUploader.aErrorFiles.length > 0 )
		{
			elDiv = document.createElement("div");
			elDiv.className = "panel-inner-title panel-inner-block";
			elDiv.appendChild(document.createTextNode(Lang.MSG_ADD_ERROR));
			this.oContainer.appendChild(elDiv);

			this.setError();
		}

		if ( FlashUploader.nAddedFiles > 0 )
		{
			elDiv = document.createElement("div");
			elDiv.className = "panel-inner-title panel-inner-block";
			elDiv.appendChild(document.createTextNode(Lang.MSG_MORE_INFO));
			this.oContainer.appendChild(elDiv);
		}

		this.setTitie(Lang.STATISTIC);
	},

	setUploading : function()
	{
		var elDiv, elImg;

		this.clear();

		elDiv = document.createElement("div");
		elDiv.style.textAlign = "center";

		elImg = document.createElement("img");
		elImg.src = FlashUploader.oConfig.processImg;

		elDiv.appendChild(elImg);
		this.oContainer.appendChild(elDiv);

		elDiv = document.createElement("div");
		elDiv.style.textAlign = "center";
		elDiv.id = "file-uploading";

		this.oContainer.appendChild(elDiv);
		this.createButtons();
		this.setTitie(YAHOO.mindtouch.FlashUploader.Lang.UPLOADING);
	},

	setFileUploading : function(oFile, nPercentComplete)
	{
		var elFileUploading = YAHOO.util.Dom.get("file-uploading");

		if ( elFileUploading )
		{
			elFileUploading.innerHTML = "File: " + oFile.name + ". Complete: " + nPercentComplete + "%";
		}
	},

	setUploadingError : function ()
	{
		var elDiv,
			Lang = YAHOO.mindtouch.FlashUploader.Lang;

		this.clear();

		elDiv = document.createElement("div");
		elDiv.className = "panel-inner-title panel-inner-block";
		elDiv.appendChild(document.createTextNode(Lang.MSG_UPLOAD_ERROR));
		this.oContainer.appendChild(elDiv);

		this.setError();

		elDiv = document.createElement("div");
		elDiv.className = "panel-inner-title panel-inner-block";
		elDiv.appendChild(document.createTextNode(Lang.MSG_MORE_INFO));
		this.oContainer.appendChild(elDiv);

		this.setTitie(Lang.COMPLETE_WITH_ERRORS);
	},

	setError : function()
	{
		var elDiv;

		for ( var i = 0; i < FlashUploader.aErrorFiles.length; i++ )
		{
			elDiv = document.createElement("div");
			elDiv.innerHTML = FlashUploader.aErrorFiles[i].name;
			this.oContainer.appendChild(elDiv);

			if ( i > 7 )
			{
				elDiv = document.createElement("div");
				elDiv.innerHTML = YAHOO.mindtouch.FlashUploader.Lang.MSG_OTHER_FILES;
				this.oContainer.appendChild(elDiv);
				break;
			}
		}
	},

	createButtons : function(oConfig)
	{
		var elButton, elButtons;
		var elMainContainer = YAHOO.util.Dom.get("tableContainer");
		var elButtons  = YAHOO.util.Dom.get("buttons-panel");

		oConfig = oConfig || {};

		if ( elButtons && elMainContainer )
		{
			elMainContainer.removeChild(elButtons);
		}

		if ( FlashUploader && FlashUploader.bUploadStarted )
		{
			// restrict cancel of files after upload was started
			return;
		}

		elButtons = document.createElement("div");
		elButtons.className = "buttons";
		elButtons.id = "buttons-panel"

		if ( oConfig.cancel )
		{
			elButton = document.createElement("div");
			elButton.id = "exclude-button";
			elButtons.appendChild(elButton);
		}

		if ( elMainContainer )
		{
			elMainContainer.appendChild(elButtons);
		}

		if ( oConfig.cancel )
		{
			var oExcludeButton = new YAHOO.widget.Button("exclude-button", {
				label: oConfig.cancel.label,
				onclick: {
					fn: function() { FlashUploader.cancelFiles.call(FlashUploader) }
				}});
		}
	}
}

