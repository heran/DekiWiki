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

CKEDITOR.plugins.add( 'infopanel',
{
});

CKEDITOR.ui.infoPanel = function( document, definition )
{
	// Copy all definition properties to this object.
	if ( definition )
	{
		CKEDITOR.tools.extend( this, definition );
	}

	// Set defaults.
	CKEDITOR.tools.extend( this,
		{
			hasClose : false
		});

	this.className = this.className || '';
	this.className += this.className.length ? ' cke_infopanel' : 'cke_infopanel';

	this.document = document;

	this._ =
	{
		groups : {},
		groupsList : []
	}
};

CKEDITOR.ui.infoPanel.prototype =
{
	renderHtml : function( editor )
	{
		var output = [];
		this.render( editor, output );
		return output.join( '' );
	},

	render : function( editor, output )
	{
		var id = this._.id = CKEDITOR.tools.getNextId();

		output.push( '<div style="display:none;" class="' +
			this.className +
			'" role="presentation" id="' +
			id + '">' );

		if ( this.hasClose )
		{
			var closeFn = CKEDITOR.tools.addFunction( function()
				{
					this.hide();
				}, this );

			output.push( '<a id="' + id + '_close"' +
				' class="close_button" href="javascript:void(0)"' +
				' onclick="CKEDITOR.tools.callFunction(', closeFn, ', this); return false;" title="' +
				editor.lang.common.close +
				'" role="button"><span class="cke_label">X</span></a>' );
		}

		output.push( '</div>' );

		return this;
	},

	getContainer : function()
	{
		return this.document.getById( this._.id );
	},

	addGroup : function( name, priority )
	{
		priority = priority || 10;

		var doc = this.document,
			div = new CKEDITOR.dom.element( 'div', doc );

		div.addClass( 'cke_infopanel_group' );
		div.setStyle( 'display', 'none' );

		var group =
			{
				name : name,
				element : div,
				priority : priority,
				visible : false,
				labels : {}
			};

		this._.groups[ name ] = group;
		this._.groupsList.push( group );
		this._.groupsList.sort( function( groupA, groupB )
			{
				return groupA.priority < groupB.priority ? -1 :
					groupA.priority > groupB.priority ? 1 : 0;
			});

		for ( var i = 0, count = this._.groupsList.length ; i < count ; i++ )
		{
			if ( this._.groupsList[ i ].name == name )
			{
				if ( i == 0 && this._.groupsList.length == 1 )
				{
					// only one group, append to container
					if ( this.hasClose )
					{
						div.insertBefore( doc.getById( this._.id + '_close' ) );
					}
					else
					{
						this.getContainer().append( div );
					}
				}
				else if ( i == 0 )
				{
					// insert before the next group
					div.insertBefore( this._.groupsList[ 1 ].element );
				}
				else
				{
					// insert after the previous group
					div.insertAfter( this._.groupsList[ i - 1 ].element );
				}

				break;
			}
		}

		return this;
	},

	showGroup : function( name )
	{
		if ( this._.groups[ name ] )
		{
			this.show();
			this._.groups[ name ].element.setStyle( 'display', '' );
			this._.groups[ name ].visible = true;

			this.updateGroupDelimiters();
		}

		return this;
	},

	hideGroup : function( name )
	{
		if ( this._.groups[ name ] )
		{
			this._.groups[ name ].element.setStyle( 'display', 'none' );
			this._.groups[ name ].visible = false;

			this.updateGroupDelimiters();

			var hidePanel = true;

			for ( var i in this._.groups )
			{
				if ( this._.groups[ i ].visible )
				{
					hidePanel = false;
					break;
				}
			}

			hidePanel && this.hide();
		}

		return this;
	},

	addLabel : function( group, name, label )
	{
		if ( !this._.groups[ group ] )
		{
			throw 'Group "' + group + '" does not exist.';
		}

		var span = new CKEDITOR.dom.element( 'span', this.document );

		this._.groups[ group ].element.append( span );
		this._.groups[ group ].labels[ name ] = span;

		if ( label )
		{
			this.updateLabel( name, group, label );
		}

		return this;
	},

	getLabelElement : function( group, name )
	{
		if ( !this._.groups[ group ] )
		{
			throw 'Group "' + group + '" does not exist.';
		}

		return this._.groups[ group ].labels[ name ] || null;
	},

	updateLabel : function( group, name, label )
	{
		this._.groups[ group ] && this._.groups[ group ].labels[ name ] &&
			this._.groups[ group ].labels[ name ].setHtml( label );
	},

	show : function()
	{
		this.getContainer().setStyle( 'display', '' );
		return this;
	},

	hide : function()
	{
		if ( typeof this.onHide === 'function' && this.onHide.call( this ) )
		{
			return this;
		}

		this.getContainer().setStyle( 'display', 'none' );
		return this;
	},

	updateGroupDelimiters : function()
	{
		var lastGroupFound = false;
		for ( var i = this._.groupsList.length - 1 ; i >= 0; i-- )
		{
			var group = this._.groupsList[ i ];
			if ( this._.groups[ group.name ].visible && !lastGroupFound )
			{
				group.element.setStyle( 'margin-right', '-1px' );
			}
			else
			{
				group.element.setStyle( 'margin-right', '' );
			}
		}
	}
};
