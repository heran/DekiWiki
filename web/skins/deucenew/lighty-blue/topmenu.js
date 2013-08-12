jQuery(document).ready(function(){
    jQuery('.ddown > a').live('click',function(event){
        event&&event.preventDefault();
        event&&event.stopPropagation();
        var $parent=jQuery(event.currentTarget).parent();
        if($parent.hasClass('on')){
            $parent.removeClass('on');
        }else{
            jQuery('.ddown').removeClass("on");
            $parent.addClass('on');
        }
        return false;
    });
    jQuery('html, .ddown.on > a').live('click', function(event){
        if(event.target.id=='adminsearchquery'){
           return;
        }
        var inUl = jQuery(event.target).parents('ul').siblings('a');
        if(typeof inUl != 'undefined' && inUl.length >0){
            var href = jQuery(event.target).attr('href');
            if(typeof href != 'undefined' && (href == '#' || href== '')){
                return false;
            }
        }
        jQuery(".ddown.on").removeClass("on");
    });

    jQuery('.ddown ul>li').live('mouseenter',function(){
        if(jQuery(this).children('ul').length == 0){
            return;
        }
        var y  = jQuery(this).children('a').position().top;
        if(jQuery(this).parents('.ddown').hasClass('down-right')){
            jQuery(this).children('ul').show().css({top:y,left:0-jQuery(this).children('ul').width()});
        }else{            
            var x = jQuery(this).width();
            jQuery(this).children('ul').show().css({top:y,left:x});
        }

    });
    jQuery('.ddown ul>li').live('mouseleave',function(){
        var f =1;
        jQuery(this).children('ul').hide()

    })


});