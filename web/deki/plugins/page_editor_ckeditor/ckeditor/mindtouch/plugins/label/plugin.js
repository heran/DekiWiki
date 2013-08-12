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
 * @file Label UI element.
 */

CKEDITOR.plugins.add( 'label',
{
	beforeInit : function( editor )
	{
		editor.ui.addHandler( CKEDITOR.UI_LABEL, CKEDITOR.ui.label.handler );
	}
});

/**
 * Label UI element.
 * @constant
 * @example
 */
CKEDITOR.UI_LABEL = 91;

/**
 * Represents a label UI element. This class should not be called directly. To
 * create new labels use {@link CKEDITOR.ui.prototype.addLabel} instead.
 * @constructor
 * @param {Object} definition The label definition.
 * @example
 */
CKEDITOR.ui.label = function( definition )
{
	// Copy all definition properties to this object.
	CKEDITOR.tools.extend( this, definition,
		// Set defaults.
		{
			title		: definition.label,
			className	: definition.className || ''
		} );

	this._ = {};
};

/**
 * Transforms a label definition in a {@link CKEDITOR.ui.label} instance.
 * @type Object
 * @example
 */
CKEDITOR.ui.label.handler =
{
	create : function( definition )
	{
		return new CKEDITOR.ui.label( definition );
	}
};

CKEDITOR.ui.label.prototype =
{
	canGroup : true,

	/**
	 * Renders the label.
	 * @param {CKEDITOR.editor} editor The editor instance which this label is
	 *		to be used by.
	 * @param {Array} output The output array to which append the HTML relative
	 *		to this label.
	 * @example
	 */
	render : function( editor, output )
	{
		var id = this._.id = CKEDITOR.tools.getNextId(),
			classes = '',
			command = this.command; // Get the command name

		this._.editor = editor;

		var instance =
		{
			id : id,
			label : this,
			editor : editor
		};

		instance.index = CKEDITOR.ui.label._.instances.push( instance ) - 1;

		if ( this.modes )
		{
			editor.on( 'mode', function()
				{
					this.setState( this.modes[ editor.mode ] ? CKEDITOR.TRISTATE_OFF : CKEDITOR.TRISTATE_DISABLED );
				}, this);
		}
		else if ( command )
		{
			// Get the command instance.
			command = editor.getCommand( command );

			if ( command )
			{
				command.on( 'state', function()
					{
						this.setState( command.state );
					}, this);

				classes += 'cke_' + (
					command.state == CKEDITOR.TRISTATE_ON ? 'on' :
					command.state == CKEDITOR.TRISTATE_DISABLED ? 'disabled' :
					'off' );
			}
		}

		if ( !command )
			classes	+= 'cke_off';

		if ( this.className )
			classes += ' ' + this.className;

		output.push(
			'<span class="cke_label">',
			'<span id="', id, '"' +
				' class="', classes, '"',
				' tabindex="-1"' +
			    ' role="label"' +
				' aria-labelledby="' + id + '_label">',
			this.title,
			'</span></span>' );

		if ( this.onRender )
			this.onRender();

		return instance;
	},

	setLabel : function( label )
	{
		var element = CKEDITOR.document.getById( this._.id );
		element.setHtml( label );
	},

	setState : function( state )
	{
		if ( this._.state == state )
			return false;

		this._.state = state;

		var element = CKEDITOR.document.getById( this._.id );

		if ( element )
		{
			element.setState( state );
			state == CKEDITOR.TRISTATE_DISABLED ?
				element.setAttribute( 'aria-disabled', true ) :
				element.removeAttribute( 'aria-disabled' );

			state == CKEDITOR.TRISTATE_ON ?
				element.setAttribute( 'aria-pressed', true ) :
				element.removeAttribute( 'aria-pressed' );

			return true;
		}
		else
			return false;
	}
};

CKEDITOR.ui.label._ =
{
	instances : []
};

/**
 * Adds a label definition to the UI elements list.
 * @param {String} The label name.
 * @param {Object} The label definition.
 * @example
 * editorInstance.ui.addLabel( 'MyBold',
 *     {
 *         label : 'My Bold',
 *         command : 'bold'
 *     });
 */
CKEDITOR.ui.prototype.addLabel = function( name, definition )
{
	this.add( name, CKEDITOR.UI_LABEL, definition );
};
