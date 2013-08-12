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

function MTComments() {};
MTComments.ViewingAll = false;

MTComments.HookBehavior = function() {
	MTComments.HookSubmitOnclick();
};

//will make adding comments an inline experience
MTComments.HookSubmitOnclick = function() {
	Deki.$('input[name=commentSubmit]').click(function() {
		var cn = document.getElementById('wpCommentNum');
		if (cn && cn.value > 0) {
			return;
		}
		MTComments.PostComment(this);
		return false;
	});
};

MTComments.PostComment = function(submitButton, commentnum) {
	
	var comment = Deki.$(submitButton).parents('form').find('textarea').val();
	Deki.$.post( '/deki/gui/comments.php', {

		'action'  : 'post',
		'titleId' : Deki.PageId,
		'comment' : comment,
		'showAll' : MTComments.ViewingAll,
		'commentNum' : commentnum ? commentnum : null

	}, function( data ) {

		MTComments.SetComment( data );

	}, 'html' );
};

MTComments.ShowComment = function(commentnum) {
	var commentform = document.getElementById('commentTextForm'+commentnum);
	var commenttext = document.getElementById('commentText'+commentnum);
	
	if (!commentform || !commenttext)
		return false;
		
	commenttext.style.display = 'block';
	commentform.style.display = 'none';
};

MTComments.EditComment = function(commentnum) {
	var commentform = document.getElementById('commentTextForm'+commentnum);
	var commenttext = document.getElementById('commentText'+commentnum);
	
	if (!commentform || !commenttext)
		return false;
		
	//if the comment form hasn't already been loaded
	if (commentform.innerHTML == '') {

		Deki.$.get( '/deki/gui/comments.php', {
	
			'action'  : 'edit',
			'titleId' : Deki.PageId,
			'commentNum' : commentnum
	
		}, function( data ) {
	
			commentform.innerHTML = data;
			//hook behavior to cancel link
			Deki.$('form#commentEditForm textarea[name=wpComment]').focus();
			document.getElementById('commentCancel'+commentnum).onclick = function() {
				MTComments.ShowComment(commentnum);
				return false;	
			};
			Deki.$('form#commentEditForm input[name=commentSubmit]').click(function() {
				MTComments.PostComment(this, commentnum);
				return false;
			});
	
		}, 'html' );
	}
	
	commenttext.style.display = 'none';
	commentform.style.display = 'block';
	
	return false;
};

MTComments.DeleteComment = function(commentnum) {
	if (confirm(wfMsg('comment-delete'))) {
		Deki.$.get( '/deki/gui/comments.php', {
	
			'action'  : 'delete',
			'titleId' : Deki.PageId,
			'commentNum' : commentnum
	
		}, function( data ) {
	
			Deki.$( '#comment' + commentnum ).html( data );
	
		}, 'html' );
	}
	return false;
};

MTComments.GetComments = function(commentcount) {
	if (commentcount == 'all') {
		MTComments.ViewingAll = true;
	}
	Deki.$.get( '/deki/gui/comments.php', {

		'action'  : 'show',
		'titleId' : Deki.PageId,
		'commentCount' : commentcount

	}, function( data ) {

		MTComments.SetComment( data );

	}, 'html' );
	return false;
};

MTComments.SetComment = function(markup) {
	document.getElementById('comments').innerHTML = markup;	
	new MTComments.HookBehavior;
};

//hook is in /skins/common/javascript.php