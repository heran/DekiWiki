//global variables
initialPageLoad = true;
    
// options for ajaxForm; global because bind() function needs to see them     
var postOptions = { 
  target: '',   // target element(s) to be updated with server response 
  beforeSubmit:  showPostRequest,  // pre-submit callback 
  success:       showPostResponse,  // post-submit callback 
  resetForm: true,        // reset the form after successful submit 
  error: showPostError
  
}; 

var deleteOptions = {
  target: '',
  beforeSubmit: showDeleteRequest,
  success: showDeleteResponse,
  error: showDeleteError
};


function clearValue(e)
{// for clearing the 'add a comment' area onfocus
  if(initialPageLoad)
  {
   e.value = "";
   e.style.color = "#000";
   e.style.textAlign = "left";
  }
  initialPageLoad = false;
}

/*
 * delete-a-comment callbacks and ancillary functions
 */

function resetCommentList(responseText)
{//deletes comment, recolors comment list 
  //delete deleted comment
  $('#' + responseText).remove();

  //recolor the remaining comments
  nextClass = 'comments';
  count = 0;
  $('#commentlist').children().each( function(i) {
    count = i;
    if( i %2 != 0)
    {  
      $(this).removeClass('white').addClass('comments');
    }
    else 
    {
      $(this).removeClass('comments').addClass('white');
    }
  });

  //change the comment count
//  $('a#commentHeading').text('Comments (' + count + ')');
  $('#commentcount').text('(' + count + ')' );
}

function showDeleteRequest(formData, jqForm, options)
{// presubmit
  var queryString = $.param(formData); 
  return true; 
}

function showDeleteResponse(responseText, statusText)
{//postsubmit

  $('div.commenterror').remove();
  resetCommentList(responseText);
  // rebind event handler to new forms
  $('#commentlist').bind('submit',$('.deleteForm').ajaxForm(deleteOptions));
  initialPageLoad = true;
}

function showDeleteError(error)
{
  $('div.commentlist').insertAfter('<div class="commenterror"> Error deleting comment. Please try again.</div>');  
}

/*
 * callbacks for 'add a comment' form
 */

function showPostRequest(formData, jqForm, options) { 
  var queryString = $.param(formData); 
  return true; 
} 
 
function showPostResponse(responseText, statusText)  { 

  // if there was an error message previously, remove it
  $('div.commenterror').remove();

  // clear the 'add a comment box' (undo clearValue())
  $('#comment').css('color', '#9b9b9b').css('text-align', 'center');
  initialPageLoad = true;

  // make recent comment visible to user (will be in response) 
	$('div#commentlist').append(responseText);
  // rebind event handler to new forms
  $('#commentlist').bind('submit',$('.deleteForm').ajaxForm(deleteOptions));

  // update counter
  count = 0;
  $('#commentlist').children().each( function(i) {
    count = i;
  });

  $('#commentcount').text('(' + count + ')' );
  //$('a#commentHeading').trigger('click');
  if($('a.toggleLink').parent().hasClass('toggle'))
  {
      $('a.toggleLink').parent().removeClass('toggle').addClass('down');
            $( '#' + $('a.toggleLink').attr('name')).show();
  }
} 

function showPostError(error)
{

  $('div.commentlist').insertAfter('<div class="commenterror"> Error adding comment. Please try again.');  
}

// "main"
$(document).ready(function() { 

    // bind form using 'ajaxForm' 
    $('.commentForm').ajaxForm(postOptions);
    $('.deleteForm').ajaxForm(deleteOptions);

}); 
