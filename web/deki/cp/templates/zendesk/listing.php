<?php echo $this->msg('Zendesk.label.help'); ?> <a href="<?php echo ProductUrl::INTEGRATION_ZENDESK ?>"><?php echo $this->msg('Zendesk.label.help.link'); ?></a>

<style type="text/css">
#zendesk-widgets {
	margin-top: 10px;
}

#zendesk-widgets textarea {
	width: 400px
	height: 150px;
	margin-bottom: 20px;
	font-family: monospace;
}

#zendesk-widgets h2 {
	font-weight: bold;
	font-size: 1.0em;
}
</style>
<div id="zendesk-widgets">
<h2><?php echo $this->msg('Zendesk.label.widget.commonjs'); ?></h2>
<textarea readonly="readonly">
var mtURL = new String('<?php echo $this->get('host'); ?>');

/**
 * Zendesk MindTouch Connector
 * @version 0.1
 * @package zendesk-connector
 * @copyright MindTouch, Inc.
 */

var zdTitle = new String();

/* define mindtouch specific endpoints */
var mtSearchURL		= new String();
var mtPageURL		= new String();
var mtPagePostURL	= new String();
var mtQueue		= new String();
var mtRes		= new String();
mtSearchURL		= mtURL + "@api/deki/site/search";

/**
 * mtDoSearch
 *
 * Search for a specific string on a MT site and callback
 *
 * @param string String to search for
 * @optional string dom id for where the spinner should be displayed
 * @optional string Callback function to process results with
 * @return string 
 */

function mtDoSearch(search, resultsId, callback) {
	if (!search.length) return;
	if (resultsId) {
		document.getElementById(resultsId).innerHTML = "<br/><img src='<?php echo $this->get('host'); ?>skins/common/icons/anim-circle.gif' alt='Searching' class='mtSearchSpinner'>";
	}
	if (!callback) { callback = "mtProcessSearch"; }
	searchURL = mtSearchURL + "?dream.out.format=jsonp&dream.out.pre="+callback+"&limit=5&sortby=-date,-rank&q=" +encodeURIComponent(search) + "&constraint=+namespace:main";

	$j.getScript(searchURL); 
}

/**
 * mtDoSearchInterval
 *
 * Add a MT search to the queue
 *
 * @param string String to search for
 * @optional string dom id for where the spinner should be displayed
 * @optional string Callback function to process results with
 * @return string 
 */

function mtDoSearchInterval(search, resultsId) {
	if (search != mtQueue) {
		mtQueue = search;
		mtRes = resultsId;
	} else {
		mtQueue = undefined;
	}
}

/**
 * mtInterval
 *
 * Run a MT search from the queue
 *
 */

function mtInterval() {
	if (mtQueue !=undefined) {
		mtDoSearch(mtQueue);
	}
}

/**
 * mtProcessSearch
 *
 * Convert JSON object into a HTML representation
 *
 * @param string data JSON object 
 * @return string 
 */
function mtProcessSearch(data) {
	obj = eval(data);
	html = "";	
	for (var i in obj.search.page) { 
		t = obj.search.page[i].title+"";
		if (t != "undefined") {
			p = obj.search.page[i]["uri.ui"]+"";
			html = html + "<li><a target=\"search\" href=\""+p+"\" title='"+t+"'>"+t+"</a></li>";	 
		}
	}
	document.getElementById("mtSearchResults").innerHTML = "<br/>"+html;
}

setInterval("mtInterval()", 250);
</textarea>

<h2><?php echo $this->msg('Zendesk.label.widget.post'); ?></h2>
<textarea readonly="readonly">
<!-- post to mindtouch -->
<form method="post" action="<?php echo $this->get('host'); ?>Special:NewPage">
	<input type="submit" name="Send to MindTouch" value="Send to MindTouch" />
	<input type="hidden" name="newpage_title" id="zdTitle" value="{{ticket.title}}" />
	<input type="hidden" name="newpage_body"  value="{{ticket.description}}" />
	<input type="hidden" name="newpage_tags"  value="{{ticket.tags}}" />
</form>
</textarea>

<h2><?php echo $this->msg('Zendesk.label.widget.search'); ?></h2>
<textarea readonly="readonly">
<!-- search mindtouch -->

<div id="mtSearch">
	<input type="text" name="mtSearchBox" id="mtSearchBox" value="" onKeyUp="mtDoSearchInterval(this.value,'mtSearchResults')" />
	<input onClick='window.open(mtURL + "Special:Search?type=fulltext&search=" + document.getElementById("mtSearchBox").value, "search");' type="submit" name="Search" value="Search" />
</div>
<div id="mtSearchResults"></div>
<script type="text/javascript">
$(document).observe('widgets:load', function(){ 
	var searchURL = new String();
	qs = $j.queryParameters();

	if (qs['query']) {
		document.getElementById("mtSearchBox").value=qs['query'];
		mtDoSearch(qs['query'],"mtSearchResults");
	} else if (document.getElementById("zdTitle").value) {
		zdTitle = document.getElementById("zdTitle").value;
		document.getElementById("mtSearchBox").value=zdTitle;
		mtDoSearch(zdTitle,"mtSearchResults");
	}
});
</script>
</textarea>
</div>