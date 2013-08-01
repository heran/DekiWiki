// 0.3: 
// cleaned up global variables
// Initialisation parameters passed in an object.
// added slide_selector parameter: function taking 2 parameters: number_of_slides and current frame index, and returns an integer being the index of the next frame.
// default frame selector is now taking the next slide (was random)
// added interval parameter: interval in milliseconds between 2 transitions when looping


YAHOO.namespace("myowndb");
YAHOO.myowndb.slideshow = function (container, o) {
	this.container = YAHOO.util.Dom.get(container);
	this.effect = o.effect;
	this.frames = [];
	
	//add cached frames
	var frames = o.frames;
	var cached_frames = YAHOO.util.Dom.getElementsByClassName("yui-sldshw-frame", null, this.container);
	
	for (var i=0; i<cached_frames.length; i++)
	{
		this.frames[i] = { id: i, type: 'cached', value: cached_frames[i]};
	}

	if (frames != null && frames!=undefined)
	{
		for (var i=0; i<o.frames.length; i++)
		{
			this.frames[i+cached_frames.length] = o.frames[i];
		}
	}

	//set slide selector
	if (! o.slide_selector)
	{
		this.slide_selector = function(number_of_slides, current_index) { return (current_index+1) % number_of_slides; }
	}
	else
	{
		this.slide_selector = o.slide_selector;
	}

	if (o.interval)
		this.interval = o.interval
	else
		this.interval = 5000

	this.init();
}


YAHOO.myowndb.slideshow.prototype = {
	init: function()
		{
			if (! this.effect)
			{
				this.effect = YAHOO.myowndb.slideshow.effects.slideup;
			}
			
			this.active_frame = this.get_active_frame();
			this.choose_next_frame();
		},
	get_active_frame: function()
		{
			var current_frame =  YAHOO.util.Dom.getElementsByClassName("yui-sldshw-active", null, this.container)[0];
			return current_frame;
		},
	get_frame_index: function(frame)
		{
			for(var i=0; i<this.frames.length;i++)
			{
				if (this.frames[i].value==frame)
					return i;
			}
			return -1;
		},
	choose_next_frame : function()
		{
			var current_index = this.get_frame_index(this.get_active_frame());
			if (current_index<0)
				current_index=0;
			var all_frames = this.frames;
			var next_index = this.slide_selector(all_frames.length, current_index);
			var next = all_frames[next_index];
			var next_frame;
			
			//possible infinite loop....
			while (next.value==this.active_frame || next.type=="broken")
			{   
				next = all_frames[this.slide_selector(all_frames.length, next_index)];
			}
			if (next.type=='cached')
			{
				next_frame = next.value;
				YAHOO.util.Dom.replaceClass(next_frame, "yui-sldshw-cached", "yui-sldshw-next");
				this.next_frame = next_frame;
				this.effect.setup(this.next_frame);
			}
			else if ( next.type=='image_url')
			{
				next_frame = document.createElement('img');
				next_frame.setAttribute('src',next.value);
				//next_frame.setAttribute('id','frame_'+next.id);
				next.type='cached';
				next.value=next_frame;
				YAHOO.util.Dom.addClass(next_frame, "yui-sldshw-frame");
				YAHOO.util.Dom.addClass(next_frame, "yui-sldshw-next");
				this.container.appendChild(next_frame);
				this.next_frame = next_frame;
				this.effect.setup(this.next_frame);
			}
			else if (next.type=='remote_html')
			{
				var callback = { 
					success: function(o) {
						var next_frame = document.createElement('div');
						next_frame.innerHTML = o.responseText;
						next_frame.setAttribute('id','frame_'+o.argument.id);
						o.argument.type='cached';
						o.argument.value=next_frame;
						YAHOO.util.Dom.addClass(next_frame, "yui-sldshw-frame");
						YAHOO.util.Dom.addClass(next_frame, "yui-sldshw-next");
						this.container.appendChild(next_frame);
						this.next_frame = o.argument.value;
						this.effect.setup(this.next_frame);
					},
					failure: function(o) {
						this.type='broken';
						this.choose_next_frame();
					},
					scope: this,
					argument: next
				}
				var transaction = YAHOO.util.Connect.asyncRequest('GET', next.value , callback,  null); 
			}
		},
	clean_up_transition : function() 
		{ 
			YAHOO.util.Dom.replaceClass(this.active_frame, "yui-sldshw-active", "yui-sldshw-cached");
			YAHOO.util.Dom.replaceClass(this.next_frame, "yui-sldshw-next", "yui-sldshw-active");
			this.active_frame = this.next_frame; 
			this.choose_next_frame();
		},
	transition: function()
		{
			var hide = this.effect.get_animation(this.active_frame);
			hide.onComplete.subscribe(this.clean_up_transition, this, true);
			hide.animate();
		},
	loop: function()
		{
			var self = this;
			this.loop_interval = setInterval( function(){ self.transition(); }, this.interval );
		}
 }	


YAHOO.myowndb.slideshow.effects = {
	slideright :{
			setup: function(frame){
				YAHOO.util.Dom.setStyle(frame, 'top', '0'); 
				YAHOO.util.Dom.setStyle(frame, 'left', '0'); 
			},
			get_animation: function(frame){
				var region = YAHOO.util.Dom.getRegion(frame);
				return new YAHOO.util.Motion(frame, { points: { by: [region.right-region.left,0] } }, 1, YAHOO.util.Easing.easeOut);
			}
		},
	slideleft: {
			setup: function(frame){
				YAHOO.util.Dom.setStyle(frame, 'top', '0'); 
				YAHOO.util.Dom.setStyle(frame, 'left', '0'); 
			},
			get_animation: function(frame){
				var region = YAHOO.util.Dom.getRegion(frame);
				return new YAHOO.util.Motion(frame, { points: { by: [region.left-region.right,0] } }, 1, YAHOO.util.Easing.easeOut);
			}
		},
	squeezeleft: {
			setup: function(frame){
				YAHOO.util.Dom.setStyle(frame, 'width', '100%'); 
			},
			get_animation: function(frame){
				var region = YAHOO.util.Dom.getRegion(frame);
				return new YAHOO.util.Anim(frame, { width: { to: 0 } }, 1, YAHOO.util.Easing.easeOut);
			}
		},
	squeezeright: {
			setup: function(frame){
				YAHOO.util.Dom.setStyle(frame, 'width', '100%'); 
				YAHOO.util.Dom.setStyle(frame, 'right', '0px'); 
			},
			get_animation: function(frame){
				var region = YAHOO.util.Dom.getRegion(frame);
				YAHOO.util.Dom.setStyle(frame, 'right', '0px'); 
				return new YAHOO.util.Anim(frame, { width: { to: 0 }}, 1, YAHOO.util.Easing.easeOut);
			}
		},
	squeezeup: {
			setup: function(frame){
				YAHOO.util.Dom.setStyle(frame, 'height', '100%'); 
			},
			get_animation: function(frame){
				var region = YAHOO.util.Dom.getRegion(frame);
				return new YAHOO.util.Anim(frame, { height: { to: 0 }}, 1, YAHOO.util.Easing.easeOut);
			}
		},
	squeezedown: {
			setup: function(frame){
				YAHOO.util.Dom.setStyle(frame, 'height', '100%'); 
			},
			get_animation: function(frame){
				var region = YAHOO.util.Dom.getRegion(frame);
				YAHOO.util.Dom.setStyle(frame, 'bottom', '0px'); 
				return new YAHOO.util.Anim(frame, { height: { to: 0 }}, 1, YAHOO.util.Easing.easeOut);
			}
		},
	fadeout: {
			setup: function(frame){
				YAHOO.util.Dom.setStyle(frame, 'opacity', '1'); 
			},
			get_animation: function(frame){
				return new YAHOO.util.Anim(frame, { opacity: { to: 0 }}, 1, YAHOO.util.Easing.easeOut);
			}
		},
	slideup: {
			setup: function(frame){
				YAHOO.util.Dom.setStyle(frame, 'top', '0'); 
				YAHOO.util.Dom.setStyle(frame, 'left', '0'); 
			},
			get_animation: function(frame){
				var region = YAHOO.util.Dom.getRegion(frame);
				return new YAHOO.util.Motion(frame, { points: { by: [0,region.top-region.bottom] } }, 1, YAHOO.util.Easing.easeOut);
			}
		}
}
