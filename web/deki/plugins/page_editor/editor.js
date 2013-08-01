
(function(){

Deki.Plugin = Deki.Plugin || {};

/**
 * @param sEditAreaId String Editor area element id
 * @abstract
 */
Deki.Plugin.Editor = function( sEditAreaId )
{
	this.EditArea = sEditAreaId;

	this.Instance = null;

	this.ReadOnly = false;

	this.$SectionToEdit = null;
	this.CurrentSection = null;
	this.OldContent = null;
	this.InitContent = null;

	this.Container = null;

	this.CheckDirtyFunctions = [];

	this.Init();
}

Deki.EDITOR_STATUS_UNLOADED = 1;
Deki.EDITOR_STATUS_CONTENT_LOADING = 2;
Deki.EDITOR_STATUS_CONTENT_LOADED = 3;
Deki.EDITOR_STATUS_LOADED = 4;
Deki.EDITOR_STATUS_STARTED = 5;

Deki.EditorInstance = null;
Deki.Plugin.Editor._formatter = 'page_editor';
Deki.Plugin.Editor.Status = Deki.EDITOR_STATUS_UNLOADED;

Deki.Plugin.Editor._preloadedContent = null;

Deki.Plugin.Editor.prototype =
{
	Init: function()
	{
	},

	BeforeStart : function()
	{
	},

	Start : function( editorContent, $sectionToEdit )
	{
		this.$SectionToEdit = ( $sectionToEdit ) ? $sectionToEdit : $( "#pageText" ); // edit page

		if ( this.$SectionToEdit.length == 0 )
		{
			alert( 'You did not define the ID "pageText" in your skin.' );
			this.Cancel();
			return;
		}

		if ( $sectionToEdit )
		{
			this.CurrentSection = $sectionToEdit.attr( 'id' ).substr( 8 );
		}

		this.BeforeStart();

		this.OldContent = this.$SectionToEdit.html();

		if ( editorContent )
		{
			if ( this.$SectionToEdit.find( '#' + this.EditArea ).length == 0 )
			{
				this.$SectionToEdit.html( editorContent.content );
			}

			this.$SectionToEdit.append( editorContent.script );

			$( '#wpEditTime' ).val( editorContent.edittime );
			$( '#wpSection' ).val( this.CurrentSection || '' );
		}

		var $wait = $( "#formLoading" );
		$wait.show();

		this.ReadOnly = Deki.EditorReadOnly || false;
		this.Instance = null;

		var saveFailed = $( '#wpArticleSaveFailed' ).val() === 'true';
		this.AddCheckDirtyFunction(function()
			{
				// editor is always dirty if save was failed
				return saveFailed;
			});

		var oSelf = this;

		// onbeforeunload sometimes fires twice in IE
		var onBeforeUnloadFired = false;
		$( window ).bind( 'beforeunload.editor', function()
			{
				var result;
				if ( !onBeforeUnloadFired )
				{
					onBeforeUnloadFired = true;
					if ( oSelf.CheckDirty() )
					{
						result = wfMsg('GUI.Editor.alert-changes-made-without-saving');
					}
				}
				window.setTimeout(function() {onBeforeUnloadFired = false;}, 1000);
				return result;
			});

		if ( this.IsSupported() && Deki.EditorWysiwyg !== false )
		{
			this.CreateEditor( editorContent );
		}
		else
		{
			var $textarea = $( "#" + this.EditArea );

			this.AddCheckDirtyFunction(function()
				{
					return this.InitContent && this.InitContent != $textarea.val();
				});

			$( "#wpFormButtons input[name=doSave]" ).click(function() {
				var form = this.form;

				oSelf.CheckPermissions( function()
					{
						this.Save();
						form.submit();

					}, oSelf );

				return false;
			});

			$( "#wpFormButtons input[name=doCancel]" ).click(function() {
				if ( oSelf.ConfirmCancel() )
				{
					oSelf.Cancel();
				}
			});

			$textarea.show();
			this.InitContent = $textarea.val();
			$( "#wpFormButtons" ).show();
		}

		if (!Deki.PageNotRedirected)
		{
			$('#deki-page-title').addClass('ui-state-with-editor');
		}

		$wait.hide();

		if ( !this.CurrentSection )
		{
			$( '.hideforedit' ).hide();
		}

		Deki.Plugin.Editor.Status = Deki.EDITOR_STATUS_STARTED;
	},
	
	IsStarted : function()
	{
		return Deki.Plugin.Editor.Status == Deki.EDITOR_STATUS_STARTED;
	},

	/**
	 * Creates the editor instance
	 * @abstract
	 */
	CreateEditor : function( data )
	{
	},

	IsSupported : function()
	{
		return false;
	},

	/**
	 * Do an AJAX request to ensure that server is still up
	 * and user has permissions to save the page
	 *
	 * @param successCallback - function to call if check is success
	 * @param scope - the scope for successCallback
	 *
	 */
	CheckPermissions : function( successCallback, scope )
	{
		if ( this.ReadOnly )
		{
			return false;
		}

		scope = scope || this;

		$( '#quicksavewait' ).show();
		$.ajax(
			{
				url : Deki.Plugin.AJAX_URL,
				data :
					{
						formatter: Deki.Plugin.Editor._formatter,
						method : 'checkPermissions',
						pageId : Deki.PageId,
						pageTitle : Deki.PageTitle
					},
				dataType : 'json',
				success : function( data, status )
				{
					if ( status === 'success' && data.success === true )
					{
						successCallback.call( scope );
					}
					else
					{
						$( '#quicksavewait' ).hide();
						Deki.Ui.Message( data.message, data.body );
					}
				},
				error : function()
				{
					$( '#quicksavewait' ).hide();
					Deki.Ui.Message( 'We are unable to save this page', 'A server error has occurred. To avoid losing your work, copy the page contents to a new file and retry saving again.' );
				}
			}
		);

		return true;
	},

	BeforeSave : function()
	{
	},

	Save : function()
	{
		if ( this.ReadOnly )
		{
			return;
		}

		this.BeforeSave();

		$( window ).unbind( 'beforeunload.editor' );
		$( '#quicksavewait' ).show();
		Deki.Plugin.Editor.Status = Deki.EDITOR_STATUS_UNLOADED;
	},

	BeforeCancel : function()
	{
	},

	ConfirmCancel : function()
	{
		if ( !this.ReadOnly && Deki.EditorInstance && Deki.EditorInstance.CheckDirty() )
		{
			var cancelMessage = "Are you sure you want to navigate away from the editor?\n\n"
						+ wfMsg('GUI.Editor.alert-changes-made-without-saving')
						+ "\n\nPress OK to continue, or Cancel to stay on the current editor.";

			if ( !confirm(cancelMessage) )
			{
				return false;
			}
		}

		return true;
	},

	Cancel : function()
	{
		this.BeforeCancel();

		$( window ).unbind( 'beforeunload.editor' );
		$( '#title' ).show(); // if we're editing an existing page
		$('.hideforedit').show();

		if (typeof Deki.CancelUrl != 'undefined')
		{
			window.location = Deki.CancelUrl;
		}
		else if ( this.$SectionToEdit )
		{
			if ( this.OldContent !== null )
			{
				this.$SectionToEdit.html( this.OldContent );
			}

			this.$SectionToEdit = null;
			this.Instance = null;
			this.CurrentSection = null;
			this.OldContent = null;
			this.InitContent = null;
			this.CheckDirtyFunctions = [];
			
			Deki.Plugin.Editor._preloadedContent = null;
		}
		else
		{
			window.history.back();
		}
		
		Deki.Plugin.Editor.Status = Deki.EDITOR_STATUS_UNLOADED;
		$('#deki-page-title').removeClass('ui-state-with-editor');
	},

	AddCheckDirtyFunction : function( func )
	{
		jQuery.isFunction( func ) && this.CheckDirtyFunctions.push( func );
	},

	CheckDirty : function()
	{
		for ( var i = 0 ; i < this.CheckDirtyFunctions.length ; i++ )
		{
			if ( this.CheckDirtyFunctions[i].call( this ) )
			{
				return true;
			}
		}

		return false;
	}
};

Deki.Plugin.Editor.HookEditSection = function()
{
	var href = window.location.href;

	if ( window.location.hash.length )
	{
		href.substring(0, href.indexOf( '#' ));
	}

	href += ( window.location.search.length === 0 ) ? '?' : '&';

	var $sections = $( 'h2.editable,h3.editable,h4.editable,h5.editable,h6.editable', $('#pageText') );

	$sections.each(function()
		{
			var $this = $(this),
				$section = $this.parent();

			var editSectionLink = href + 'action=edit&sectionId=' + $section.attr( 'id' ).substr( 8 );

			if ( window.location.hash.length )
			{
				editSectionLink += window.location.hash;
			}

			var $editLink = $( document.createElement( 'a' ) )
				.attr( 'href', editSectionLink )
				.attr( 'title', wfMsg('wikibits-edit-section') );

			var $editIcon = $( document.createElement( 'img' ) )
				.attr( 'src', Deki.PathCommon + '/icons/icon-trans.gif' )
				.attr( 'class', 'sectionedit' )
				.attr( 'alt', wfMsg('wikibits-edit-section') );

			$editLink.append( $editIcon );
			$editIcon.wrap( '<span class="icon"></span>' );

			$this.wrapInner('<span></span>')
				.append($editLink);

			$editLink.wrap( '<div class="editIcon"></div>' );
		})
		.live( 'mouseover mouseout', function( evt )
			{
				$('div.editIcon', this).css('visibility', evt.type == 'mouseover' ? 'visible' : 'hidden');
			});

		$( 'div.editIcon > a',  $( '#pageText' ) ).live( 'click', function()
		{
			Deki.Plugin.Publish('Editor.load', [ $(this).parent().parent().parent() ]);
			return false;
		});
}

Deki.Plugin.Subscribe( 'Editor.start', function( evt, editorData, $section )
	{
		if ( Deki.EditorInstance && Deki.Plugin.Editor.Status < Deki.EDITOR_STATUS_LOADED )
		{
			var loadScripts = function( scripts )
			{
				if ( jQuery.isArray( scripts ) && scripts.length > 0 )
				{
					jQuery.getScript( scripts.shift(), function ()
						{
							loadScripts( scripts );
						});
				}
				else
				{
					Deki.Plugin.Editor.Status = Deki.EDITOR_STATUS_LOADED;
					Deki.EditorInstance.Start( editorData, $section );
				}
			};

			loadScripts( editorData.scripts );
		}

		return true;
	} );

Deki.Plugin.Subscribe( 'Editor.save', function()
	{
		if ( Deki.EditorInstance )
		{
			return Deki.EditorInstance.Save();
		}

		return true;
	});

Deki.Plugin.Subscribe( 'Editor.cancel', function()
	{
		if ( Deki.EditorInstance )
		{
			Deki.EditorInstance.Cancel();
		}

		return true;
	});

Deki.Plugin.Subscribe( 'Editor.checkPermissions', function( evt, callback, scope )
	{
		if ( Deki.EditorInstance )
		{
			return Deki.EditorInstance.CheckPermissions( callback, scope );
		}

		return true;
	});

var contentParams,
	getContentParams = function( $section, action )
	{
		if ( contentParams && contentParams.$section == $section && contentParams.action == action )
		{
			return contentParams.params;
		}

		contentParams = {};

		contentParams.$section = $section;
		contentParams.action = action;

		var sectionId = $section ? $section.attr( 'id' ).substr( 8 ) : null;
		var params = {}, param;

		if ( window.location.search.length > 0 )
		{
			var query = window.location.search.substring(1).split('&');

			for ( var i = 0 ; i < query.length ; i++ )
			{
				param = query[i].split('=');
				params[param[0]] = decodeURIComponent( param[1] ) || '';
			}
		}

		params.text = params.text || Deki.PageTitle;
		params.pageId = params.pageId || Deki.PageId;
		params.sectionId = params.sectionId || sectionId || '';
		params.source = ( action == 'source' );

		params.method = 'load';

		// Article::loadContent stops to work with this params in some cases
		params.action && delete params.action;
		params.diff && delete params.diff;
		params.revision && delete params.revision;

		contentParams.params = params;
		return params;
	};

Deki.Plugin.Subscribe( 'Editor.loadContent', function( evt, $section, action )
	{
		if ( !$section && Deki.Plugin.Editor.Status > Deki.EDITOR_STATUS_UNLOADED &&
			 Deki.Plugin.Editor.Status != Deki.EDITOR_STATUS_CONTENT_LOADED )
		{
			return;
		}

		Deki.Plugin.Editor.Status = Deki.EDITOR_STATUS_CONTENT_LOADING;

		var params = getContentParams( $section, action );

		if ( !$section && params.sectionId.length )
		{
			$section = $( '#' + params.sectionId );
		}

		Deki.Plugin.AjaxRequest( Deki.Plugin.Editor._formatter,
			{
			    timeout : 3 * 60 * 1000 /* up to 3mins */,
				data : params,
				success : function( data, status )
				{
					if ( status == 'success' && data.success === true )
					{
						if ( !$section )
						{
							Deki.Plugin.Editor._preloadedContent = data.body;
						}
						
						Deki.Plugin.Editor.Status = Deki.EDITOR_STATUS_CONTENT_LOADED;
						Deki.Plugin.Publish( 'Editor.contentLoaded', [ data.body, $section ] );
					}
					else
					{
						Deki.Plugin.Editor.Status = Deki.EDITOR_STATUS_UNLOADED;
						Deki.Ui.Message( wfMsg('error'), data.message );
					}
				},
				error : function()
				{
					Deki.Plugin.Editor.Status = Deki.EDITOR_STATUS_UNLOADED;
					Deki.Ui.Message(wfMsg('error'), wfMsg('internal-error'));
				}
			}
		);
	});

Deki.Plugin.Subscribe( 'Editor.load', function( evt, $section, action )
	{
		if ( !Deki.PageEditable )
		{
			return false;
		}
		
		if ( Deki.Plugin.Editor.Status == Deki.EDITOR_STATUS_STARTED )
		{
			if ( Deki.EditorInstance.CurrentSection && Deki.EditorInstance.ConfirmCancel() )
			{
				Deki.EditorInstance.Cancel();
				// if we have a message, hide it
				$( '#sessionMsg' ).hide();
			}
			else
			{
				return false;
			}
		}
		
		Deki.Plugin.Editor.StartLoadTime = new Date().getTime();
		
		!$section && $( '.hideforedit' ).hide();

		var startEditor = function( evt, content, $section )
		{
			Deki.Plugin.Unsubscribe( 'Editor.contentLoaded', startEditor );
			Deki.Plugin.Publish( 'Editor.start', [ content, $section ] );
		};

		if ( !Deki.Plugin.Editor._preloadedContent || $section )
		{
			Deki.Plugin.Subscribe( 'Editor.contentLoaded', startEditor );
			Deki.Plugin.Publish( 'Editor.loadContent', [ $section, action ] );
		}
		else
		{
			startEditor( null, Deki.Plugin.Editor._preloadedContent );
		}

		return false;
	});

/**
 * Support of autosave plugin
 * Auto start editor if draft is available
 */
$(function()
{
	if ( window.localStorage && !Deki.UserIsAnonymous )
	{
		try
		{
			if ( window.localStorage.getItem( 'cke_' + Deki.PageId ) )
			{
				Deki.Plugin.Publish( 'Editor.load' );
			}
		}
		catch (ex) {}
	}
});

})();
