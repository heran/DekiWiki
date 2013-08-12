(function() {
$(document).ready(function() {
	var editor,
		originalConfigFn = CKEDITOR.editorConfig;
		
	delete CKEDITOR.editorConfig;
		
	var getConfigSample = function()
	{
		// read config
		var config = {};
		originalConfigFn(config);
		// format config as text
		var toolbar = $('#select-toolbar').val();
		var sample = "CKEDITOR.editorConfig = function( config )\n{\n\tconfig.toolbar_";
		sample += toolbar + " = \n" + dump(config['toolbar_' + toolbar], 2);
		sample += ";\n};";
		
		return sample;
	};
	
	$('#select-toolbar').change(function() {
		editor && $('#preview-editor').click();
	});
	
	$('#paste-config').click(function() {
		$('#textarea-config').val(getConfigSample()).focus();
		return false;
	});
	
	CKEDITOR.mindtouch_status = 'unloaded';
	
	$('#preview-editor').click(function() {
		var $this = $(this);
		
		if (editor)
		{
			editor.destroy();
			$('#editarea').remove();
			$this.html(aLt['EditorConfig.preview-config']);
			
			editor = null;
		}
		else
		{
			$this.html(aLt['EditorConfig.preview-loading']);
			var toolbar = $('#select-toolbar').val();
			
			var editorConfig =
			{
				toolbar : toolbar,
				customConfig : '',
				contentsCss : Deki.Plugin.AJAX_URL + '?formatter=page_editor_styles&format=custom',
				mindtouch :
				{
					editorPath : CKEDITOR.basePath + '../',
					commonPath : '/skins/common',
					userName : 'Anonymous',
					userIsAnonymous : true,
					today : new Date().toDateString(),
					isReadOnly : true,
					pageTitle : '',
					pageId : 0,
					pageRevision : 0,
					sectionId : null
				},
				on :
				{
					configLoaded : function( ev )
					{
						var editor = ev.editor;
	
						editor.config.extraPlugins = editor.config.extraPlugins.replace( /\s*,\s*/g, ',' );
	
						if ( Deki.atdEnabled === true )
						{
							editor.config.extraPlugins += ( editor.config.extraPlugins.length ? ',' : '' ) + 'atdspellchecker';
						}
					}
				}
			};
			
			var loadEditor = function()
			{
				var skipResources = CKEDITOR.mindtouch_status == 'loaded'
					config = {};
				
				originalConfigFn(config);
				CKEDITOR.tools.extend(editorConfig, config);
				
				eval($('#textarea-config').val());
				CKEDITOR.editorConfig && CKEDITOR.editorConfig(editorConfig);
				delete CKEDITOR.editorConfig;
				
				!skipResources && CKEDITOR.document.appendStyleSheet(CKEDITOR.basePath + '../skin.css');
				
				var startEditor = function()
				{
					var html = '<p style="background-color:#ff0; color: #f00; font-size: 24px; line-height: 1em;">NOTE: This is sample of the editor. It may be different from the editor on pages and has limited functionality!</p>';

					var $textarea = $(document.createElement( 'textarea' )).appendTo($('#eareaParent'));
					$textarea.attr('id', 'editarea').val(html);
					
					editor = CKEDITOR.replace('editarea', editorConfig);
					$this.html(aLt['EditorConfig.preview-hide']);
				};
				
				skipResources && startEditor();
				!skipResources && Deki.Plugin.AjaxRequest( 'page_editor_ckeditor_lang',
					{
						success : function( data, status )
						{
							if (status == 'success' && data.success)
							{
								$.getScript(CKEDITOR.getUrl( 'lang/en.js' ), function()
									{
										CKEDITOR.tools.extend(CKEDITOR.lang[ 'en' ], data.body);
										jQuery.getScript(CKEDITOR.getUrl('mindtouch.js'), function()
											{
												CKEDITOR.mindtouch_status = 'loaded';
												startEditor();
											});
									});
							}
						}
					});
			}
			
			if (CKEDITOR.status == 'loaded')
			{
				loadEditor();
			}
			else
			{
				CKEDITOR.on('loaded', loadEditor);
				CKEDITOR.loadFullCore && CKEDITOR.loadFullCore();
			}				
		}
		
		return false;
	});
});

function dump(v, level)
{
	level = level || 0;
	var text = '';
	
	var indent = function(level)
	{
		var tabs = '';
		for (var i = 0 ; i < level ; i++)
		{
			tabs += "\t";
		}
		return tabs;
	};
	
	if (typeof v == 'string')
	{
		text += "'" + v + "'";
	}
	else if (jQuery.isArray(v))
	{
		text += indent(level) + '[';
		
		if (v[0] && jQuery.isArray(v[0]))
		{
			text += "\n";
		}

		for (var i = 0 ; i < v.length ; i++)
		{
			if (typeof v[i] == 'string' && ((v[i-1] && jQuery.isArray(v[i-1])) || (v[i+1] && jQuery.isArray(v[i+1]))))
			{
				text += indent(level + 1);
			}
			
			text += dump(v[i], level + 1);
			
			if (i < v.length - 1)
			{
				text += ',';
			}
			
			if (jQuery.isArray(v[i]) || (v[i+1] && jQuery.isArray(v[i+1])))
			{
				text += "\n";
			}
		}
		
		if (jQuery.isArray(v[i-1]))
		{
			text += indent(level);
		}
		
		text += ']';
	}
	
	return text;
}
})();
