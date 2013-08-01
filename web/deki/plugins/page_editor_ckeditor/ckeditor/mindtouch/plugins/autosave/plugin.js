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

/**
 * @file Autosave plugin.
 * @link {http://developer.mindtouch.com/User:dev/Specs/Autosave/Autosave_with_HTML5_Web_Storage:_editor_plugin}
 */

(function()
{
	// allow additional checks on dirty
	function checkDirty( editor )
	{
		return editor.fire( 'checkDirty', {isDirty : false} ).isDirty;
	}

	var Draft = CKEDITOR.tools.createClass(
	{
		$ : function( editor )
		{
			this.editor = editor;
			this.storage = null;
			this.params = {};

			if ( this.editor.config.mindtouch.pageId > 0 )
			{
				this.key = 'cke_' + this.editor.config.mindtouch.pageId;
			}
			else
			{
				this.key = 'cke_' + encodeURIComponent( this.editor.config.mindtouch.pageTitle );
			}
			
			this.initStorage();
			this.addUnloadHandler();
		},

		proto :
		{
			initStorage : function()
			{
				if ( window.localStorage )
				{
					try
					{
						window.localStorage.setItem( 'cke_test', 'test' );

						if ( window.localStorage.getItem( 'cke_test' ) === 'test' )
						{
							window.localStorage.removeItem( 'cke_test' );
							this.storage = window.localStorage;
						}
					}
					catch (ex) {}
				}
			},

			addUnloadHandler : function()
			{
				if ( this.storage && jQuery )
				{
					var me = this;

					this.removeUnloadHandler();
					
					$( window ).bind( 'beforeunload.editor', function()
						{
							if ( checkDirty( me.editor ) )
							{
								me.save();
							}
						});
				}
			},

			removeUnloadHandler : function()
			{
				if ( this.storage && jQuery )
				{
					$( window ).unbind( 'beforeunload.editor' );
				}
			},

			save : function()
			{
				if ( this.storage )
				{
					this.storage.setItem( this.key, this.editor.getData() );
					this.saveParam( 'timestamp', new Date().getTime() );
					this.editor.fire( 'draftSaved' );

					return true;
				}

				return false;
			},

			saveParam : function( name, value )
			{
				if ( this.storage )
				{
					this.storage.setItem( this.key + '_' + name, value );
					this.params[ name ] = value;
					this.editor.fire( 'paramSaved', {name : name} );

					return true;
				}

				return false;
			},

			getParam : function( name )
			{
				var param = this.params[ name ] || null;

				if ( this.storage && !param )
				{
					param = this.storage.getItem( this.key + '_' + name );

					if ( param )
					{
						this.params[ name ] = param;
					}
				}

				return param;
			},

			getTimestamp : function()
			{
				var timestamp = this.getParam( 'timestamp' );

				if ( timestamp )
				{
					timestamp = parseInt( timestamp );
				}

				return timestamp;
			},

			load : function()
			{
				if ( this.storage )
				{
					var data = this.storage.getItem( this.key );

					if ( data )
					{
						this.editor.setData( data, function()
							{
								this.fire( 'draftLoaded' );
							});
					}

					return true;
				}

				return false;
			},

			isExists : function()
			{
				if ( this.storage )
				{
					return !!this.storage.getItem( this.key );
				}

				return false;
			},

			remove : function()
			{
				if ( this.storage )
				{
					this.storage.removeItem( this.key );

					// remove all params
					for ( var i in this.params )
					{
						this.storage.removeItem( this.key + '_' + i );
					}

					this.editor.fire( 'draftRemoved' );
					return true;
				}

				return false;
			}
		}
	});

	CKEDITOR.plugins.add( 'autosave',
	{
		lang : [ 'en' ], // @Packager.RemoveLine

		requires : [ 'infobar', 'infopanel' ],

		init : function( editor )
		{
			if ( editor.config.mindtouch.isReadOnly ||
				editor.config.mindtouch.sectionId ||
				editor.config.mindtouch.userIsAnonymous )
			{
				return;
			}

			var draft = new Draft( editor );

			if ( !draft.storage )
			{
				return;
			}

			var lang = editor.lang.autosave,
				prevSnapshot,
				autosaveInterval,
				updateLabelInterval;

			var infoPanel = new CKEDITOR.ui.infoPanel( CKEDITOR.document,
				{
					className : 'cke_autosavepanel',
					onHide : function()
					{
						if ( jQuery )
						{
							jQuery( this.getContainer().$ ).fadeOut( 200 );
							return true;
						}

						return false;
					}
				});

			var autosave = function()
				{
					if ( checkDirty( editor ) )
					{
						var snapshot = editor.getSnapshot();
						if ( Math.abs( snapshot.length - prevSnapshot.length ) > editor.config.autosave_minLength )
						{
							if ( draft.save() )
							{
								prevSnapshot = snapshot;
							}
						}
					}
				};

			autosaveInterval = window.setInterval( autosave, editor.config.autosave_interval * 1000 );
			editor.on( 'saveSnapshot', autosave, null, null, 100 );

			var getFormattedTime = function( date )
				{
					var hrs = date.getHours(),
						mins = date.getMinutes(),
						postfix = '';

					if ( editor.config.autosave_timeFormat == '24H' )
					{
						hrs = ( hrs < 10 ) ? '0' + hrs : hrs;
					}
					else
					{
						if ( hrs > 11 )
						{
							hrs = hrs - 12;
							postfix = ' PM';
						}
						else
						{
							postfix = ' AM';
						}

						if ( hrs === 0 )
						{
							hrs = 12;
						}
					}
					
					mins = ( mins < 10 ) ? '0' + mins : mins;

					return hrs + ':' + mins + postfix;
				};

			var getFromattedDate = function( date )
				{
					var month = date.getMonth() + 1,
						day = date.getDate(),
						year = date.getFullYear();

					month = ( month < 10 ) ? '0' + month : month;
					day = ( day < 10 ) ? '0' + day : day;

					var formattedDate = editor.config.autosave_dateFormat.replace( '%m', month )
						.replace( '%d', day )
						.replace( '%y', year );

					return formattedDate;
				};

			var timeAgo = function()
			{
				var diff = new Date().getTime() - draft.getTimestamp(),
					seconds = diff / 1000,
					minutes = seconds / 60,
					hours = minutes / 60,
					days = hours / 24,
					years = days / 365;

				var label = seconds < 45 && lang.timeago.seconds ||
					seconds < 90 && lang.timeago.minute ||
					minutes < 45 && lang.timeago.minutes.replace( '%1', Math.round( minutes ) ) ||
					minutes < 90 && lang.timeago.hour ||
					hours < 24 && lang.timeago.hours.replace( '%1', Math.round( hours ) ) ||
					hours < 48 && lang.timeago.day ||
					days < 30 && lang.timeago.days.replace( '%1', Math.floor( days ) ) ||
					days < 60 && lang.timeago.month ||
					days < 365 && lang.timeago.months.replace( '%1', Math.floor( days / 30 ) ) ||
					years < 2 && lang.timeago.year ||
					lang.timeago.years.replace( '%1', Math.floor( years ) );

				label = ' (' + label + ' ' + lang.timeago.suffixAgo + ')';

				return label;
			};

			var updateTimeAgo = function( initLabel )
			{
				editor.ui.infobar.updateLabel( 'autosave', 'timeAgo', initLabel );
				editor.ui.infobar.showGroup( 'autosave' );

				updateLabelInterval && window.clearInterval( updateLabelInterval );
				updateLabelInterval = window.setInterval( function()
					{
						editor.ui.infobar.updateLabel( 'autosave', 'timeAgo', lang.localSave + timeAgo() );
					}, 15000 );
			};

			var stopAutosaving = function()
			{
				window.clearInterval( autosaveInterval );
				updateLabelInterval && window.clearInterval( updateLabelInterval );
			};

			var onExitEditor = function()
			{
				stopAutosaving();
				draft.remove();
				draft.removeUnloadHandler();
			};

			editor.on( 'save', onExitEditor );
			editor.on( 'cancel', onExitEditor );

			editor.on( 'destroy', function( evt )
				{
					stopAutosaving();
				});

			editor.on( 'instanceReady', function( evt )
				{
					if ( draft.isExists() )
					{
						var currentData = editor.getData();
						draft.load();

						var continueFn, discardFn, onEsc;

						continueFn = function()
							{
								editor.removeListener( 'focus', continueFn );
								CKEDITOR.document.removeListener( 'keydown', onEsc );
								infoPanel.hide();
								updateTimeAgo( lang.localSave + timeAgo() );
								editor.focus();
							};

						discardFn = function()
							{
								editor.removeListener( 'focus', continueFn );
								CKEDITOR.document.removeListener( 'keydown', onEsc );
								editor.setData( currentData, function()
									{
										editor.resetDirty();
										draft.remove();
										infoPanel.hide();
										editor.focus();
									});
							};

						onEsc = function( evt )
							{
								if ( evt.data.getKeystroke() == 27 )
								{
									setTimeout( discardFn, 0 );
								}
							};

						var continueFnRef = CKEDITOR.tools.addFunction( continueFn ),
							discardFnRef = CKEDITOR.tools.addFunction( discardFn );

						editor.on( 'destroy', function( evt )
							{
								CKEDITOR.tools.removeFunction( continueFnRef );
								CKEDITOR.tools.removeFunction( discardFnRef );
							});


						var draftDate = new Date( draft.getTimestamp() ),
							pageRevision = parseInt( editor.config.mindtouch.pageRevision ),
							draftRevision = parseInt( draft.getParam( 'pageRevision' ) ),
							continueLinkLabel = lang.continueEditing,
							discardLinkLabel = lang.discardChanges,
							linksDelimiter = '',
							notificationLabel;

						if ( pageRevision > draftRevision )
						{
							// draft is outdated

							notificationLabel = lang.draftOutdated.replace( '%1', draftRevision )
								.replace( '%2', pageRevision );

							continueLinkLabel = lang.editVersion.replace( '%1', draftRevision );
							discardLinkLabel = lang.editVersion.replace( '%1', pageRevision );
							linksDelimiter = '<span class="delimiter">' + lang.or + '</span>';

							infoPanel.getContainer().addClass( 'cke_autosave_outdated' );
						}
						else
						{
							notificationLabel = lang.draftExists.replace( '%1', getFromattedDate( draftDate ) )
								.replace( '%2', getFormattedTime( draftDate ) );
						}

						var continueLink = '<a href="javascript:void(0)"' +
							' onclick="CKEDITOR.tools.callFunction(' + continueFnRef + '); return false;"' +
							'>' + continueLinkLabel + '</a>';

						var discardLink = '<a href="javascript:void(0)"' +
							' onclick="CKEDITOR.tools.callFunction(' + discardFnRef + '); return false;"' +
							'>' + discardLinkLabel + '</a>';


						infoPanel.updateLabel( 'draft', 'notification', notificationLabel );
						infoPanel.updateLabel( 'draft', 'links', continueLink + linksDelimiter + discardLink );

						infoPanel.showGroup( 'draft' );

						editor.focusManager.blur();
						
						infoPanel.getLabelElement( 'draft', 'links' ).getFirst().focus();

						editor.on( 'focus', continueFn );
						CKEDITOR.document.on( 'keydown', onEsc );
					}
				});

			editor.on( 'dataReady', function()
				{
					setTimeout(function()
						{
							prevSnapshot = editor.getSnapshot();
						}, 0);
				});

			editor.on( 'draftSaved', function()
				{
					draft.saveParam( 'pageRevision', editor.config.mindtouch.pageRevision );
					updateTimeAgo( lang.localSave + ' (' + lang.timeago.justNow + ')' );
				});

			editor.on( 'themeSpace', function( evt )
				{
					if ( evt.data.space == 'top' )
					{
						evt.data.html += infoPanel.renderHtml( evt.editor );
					}
				});

			editor.on( 'themeLoaded', function()
				{
					var infoBar = editor.ui.infobar;
					infoBar.addGroup( 'autosave', 1 );
					infoBar.addLabel( 'autosave', 'timeAgo' );

					infoPanel.addGroup( 'draft' );
					infoPanel.addLabel( 'draft', 'notification' );
					infoPanel.addLabel( 'draft', 'links' );
				});

			editor.on( 'checkDirty', function( ev )
				{
					ev.data.isDirty = editor.checkDirty();
					ev.stop();
				}, null, null, 100 );
		}
	});
})();

/**
 * Autosave interval in seconds.
 */
CKEDITOR.config.autosave_interval = 25;

/**
 * Draft won't be saved if the differences between length of previous and current
 * content less then this value.
 */
CKEDITOR.config.autosave_minLength = 20;

/**
 * Date format
 * %m - month, %d - day, %y - year
 * @default %m/%d/%y
 */
CKEDITOR.config.autosave_dateFormat = '%m/%d/%y';

/**
 * Time format
 * Possible values: 12H or 24H
 * @default 12H
 */
CKEDITOR.config.autosave_timeFormat = '12H';
