Deki.fullNav = function(){}
Deki.fullNav.prototype = 
{
	///Properties
	
	///Methods
	Toggle: function(elm,event)
	{
		if(YAHOO.util.Dom.hasClass(elm, "icon") )
			this._getChildren(elm);
	}
	
	,_getChildren: function(elm)
	{
		elm = YAHOO.util.Dom.getAncestorByClassName(elm, "node");

		var matches = /\d+/.exec(elm.id);
		if(matches.length==0)return;
		var id = matches[0];
		var requestURL = "/@api/deki/site/nav/"+id+"/children?dream.out.format=json&type=expandable";
		var context = this;
		
		elm = $(elm);
		
		///we already have the children 
		if(elm.attr("hasChildren") != undefined || elm.hasClass("selected") || elm.hasClass("ancestor"))
		{

			var nodes = elm.find("ul");
			var firstNode = nodes[0];
			var display = (firstNode.style.display == 'none') ? 'block' : 'none';
			
			for(var i=0;i<nodes.length;i++)
			{
				var node = nodes[i];
				if(node!=undefined)
				{
					node.style.display = display;
					var hide = (display=='none') ? true : false;
					
					if(hide)
						this._toggleChildren(node,hide,true);
					else
					{
						this._toggleParentClass(node,true);
					}
				}
			}
						
			this._toggleParentClass(elm,display=="none");
		}
		
		///get the children from the @api
		else
		{
			YAHOO.util.Connect.asyncRequest
			(
				"GET",
				requestURL,
				{
					scope: this,
					success: function(obj, args)
					{
						//Evaluate response	
						eval("var response = " + obj.responseText);

						var tempHolder = document.createElement("div");
						tempHolder.innerHTML = response.children.html;
						var children = YAHOO.util.Dom.getChildren(tempHolder);
						
						if(children.length>0)
						{
							$(elm).attr("hasChildren", "true"); //response.children.nodes
							var isLastNode = $(elm).hasClass("lastNode") || $(elm).hasClass("lastParentNode");
							
							var afterElm = $(elm[0]).children()[0];///get the context element.
							for(var i=0;i<children.length;i++)
							{
								curElm = children[i];
								YAHOO.util.Dom.insertAfter(curElm,afterElm);
								afterElm = curElm;
							}

							this._toggleParentClass(elm,false);
						}
						
					}
				}
			);
		}
	}
	
	,_toggleChildren: function(elm,hide,recursive)
	{
		var children = elm.getAttribute("c");
		if(children == undefined || children.length ==	-1)
			return;
		
		this._toggleParentClass(elm,!hide);
		
		children = children.split(",");
		for(var i=0;i<children.length;i++)
		{
			var node = document.getElementById(children[i]);
			if(node!=undefined)
				node.style.display = hide ? 'none' : 'block';
				
			if(recursive)
			{
				this._toggleChildren(node,hide,recursive);
			}
		}
	}
	
	,_toggleParentClass: function(elm,hide)
	{
		elm = $(elm);
		
		///skip a none parent element.
		if(!elm.hasClass("parentOpen") && !elm.hasClass("parentClosed"))
		{ return; }
			
		if(hide)
		{
			elm.removeClass("parentOpen");
			elm.addClass("parentClosed");
		}
		else
		{
			elm.removeClass("parentClosed");
			elm.addClass("parentOpen");
		}
	}
}
var DekiExpandableNav = new Deki.fullNav();

