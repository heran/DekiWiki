var doPopupRestrict = function(titleID) {
    var dialog = new Deki.Dialog({
        src: '/skins/common/popups/restrict.php?titleID=' + titleID,
        width: '380px',
        height: "auto",
        buttons: [
            Deki.Dialog.BTN_OK,
            Deki.Dialog.BTN_CANCEL
        ],
        args: null,
        callback : function(params) {
            Deki.$.get('/deki/gui/pageactions.php?action=setrestrictions', params, function() { window.location.reload(); });
        }
    });
    
    dialog.render();
    dialog.show();
    return false;
};

var doPopupTags = function(titleID) {
    var dialog = new DekiWiki.Dialog({
        src: '/skins/common/popups/tags_dialog.php?titleID=' + titleID,
        width: '380px',
        height: '110px',
        buttons: [
            DekiWiki.Dialog.BTN_OK,
            DekiWiki.Dialog.BTN_CANCEL
        ],
        args: null,
        callback : function(params) { 
    		Deki.Plugin.PageTags.BulkSave(params, function(data) {
				Deki.Plugin.PageTags.Refresh('view');      	
            });
        }
    });
    
    dialog.render();
    dialog.show();

    return false;
};

var doPopupAttach = function(titleID)
{
    var params = {
        titleID : titleID,
        commonPath : Deki.PathCommon
    };
  
    var dialog = new Deki.Dialog();
    dialog.setConfig({
        src: Deki.PathCommon + "/popups/attach_dialog.php",
        width: "660px",
        buttons: [
            Deki.Dialog.BTN_OK,
            Deki.Dialog.BTN_CANCEL
        ],
        args : params,
        callback : function (param) {
            if (param) {
                setTimeout(function() {
                	if (Deki.Plugin && Deki.Plugin.FilesTable) {
                		document.location.hash = 'pageFiles';
                		Deki.Plugin.FilesTable.Refresh(titleID);
                	}
                }, 100);
            }
        }
    });
    dialog.render();
    dialog.show();
    
    return false;
}

var doPopupDeleteAttach = function(pageId, fileId) {
	if (confirm(wfMsg('menu-confirm-delete'))) {
		var oData = {
			'fileId' : fileId
		};

		Deki.$.get("/deki/gui/attachments.php?action=delete", oData, function () {
        	if (Deki.Plugin && Deki.Plugin.FilesTable) {
        		Deki.Plugin.FilesTable.Refresh(pageId);
        	}
		});
	}
};

var doPopupDelete = function(titleID) {
	var dialog = new Deki.Dialog({
		src: '/skins/common/popups/delete.php?titleID=' + titleID ,
		width: '380px',
		height: '124px',
		buttons: [
			Deki.Dialog.BTN_OK,
			Deki.Dialog.BTN_CANCEL
		],
		args: null,
		callback : function(params) {
			Deki.$.post('/deki/gui/pageactions.php?action=delete', params, function(data) {            	
					if (data.success) {
							window.location = data.redirectTo;
					} else {
						alert(data.message);
					}
				}, 'json'
			);
		}
	});

	dialog.render();
	dialog.show();

	return false;
};

var doPopupRename = function(titleID)
{
    var sUrl = '/skins/common/popups/move_page_dialog.php?titleID=' + titleID;
    var dialog = new Deki.Dialog({
        src: sUrl,
        width: '600px',
        height: '315px',
        buttons: [
            Deki.Dialog.BTN_OK,
            Deki.Dialog.BTN_CANCEL
        ],
        args: null,
        status: true,
        callback : function() { 
        }
    });
    
    dialog.render();
    dialog.show();

    return false;
};

function doPopupMoveAttach(titleID, attachID)
{
    var aUrl = ['/skins/common/popups/move_file_dialog.php',
                '?titleID=' + titleID,
                '&attachID=' +  attachID
                ];
    var dialog = new Deki.Dialog({
        src: aUrl.join(''),
        width: '600px',
        height: '315px',
        buttons: [
            Deki.Dialog.BTN_OK,
            Deki.Dialog.BTN_CANCEL
        ],
        args: null,
        status: true,
        callback : function() {
        	if (Deki.Plugin && Deki.Plugin.FilesTable) {
        		Deki.Plugin.FilesTable.Refresh(titleID);
        	}
        }
    });
    
    dialog.render();
    dialog.show();

    // stop the click event
    return false;
};

function doPopupKalturaEditor(entryID)
{
    var sUrl = '/skins/common/popups/kaltura/kaltura_editor_dialog.php?entryID=' + entryID;
    var dialog = new Deki.Dialog({
        src: sUrl,
        width: '900px',
        height: '560px',
        buttons: [
        ],
        args: null,
        callback : function() { 
        }
    });
    
    dialog.render();
    dialog.show();

    // stop the click event
    return false;
};
