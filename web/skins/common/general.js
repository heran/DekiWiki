String.prototype.utf8ToCodepoint = function() {
  var z = this.charCodeAt(0), length;
  if (z & 0x80) {
    length = 0;
    while (z & 0x80) {
      length++;
      z <<= 1;
    }
  } else
    length = 1;

  if (length != this.length) return false;
  if (length == 1) return z;

  // Mask off the length-determining bits and shift back to the original location
  z &= 0xff;
  z >>= length;

  // Add in the free bits from subsequent bytes
  for ( var i=1; i < length; i++ ) {
    z <<= 6;
    z |= this.charCodeAt(i) & 0x3f;
  }
  return z;
};

String.prototype.utf8ToString = function() {
  var val = this.replace(/[\xc0-\xfd][\x80-\xbf]*/g, function(s) {
    return String.fromCharCode(s.utf8ToCodepoint());
  });;
  return val;
};

String.prototype.utf8URL = function() {
  var val = this.replace(/[ \?%\+&=#\.\u0080-\uFFFF]/g, function(s) {
    switch (s) {
    case ' ': return '_';
//    case ' ': return '%20';
    case '+': return '%2B';
    default:
      return escape(String.charToUtf8(s.charCodeAt(0)));
    }
  });
  return val;
};

String.prototype.utf8 = function() {
  var val = this.replace(/[\u0080-\uFFFF]/g, function(s) {
    return String.charToUtf8(s.charCodeAt(0));
  });
  return val;
};

String.charToUtf8 = function(codepoint) {
  if(codepoint < 0x80) return String.fromCharCode(codepoint);
  if(codepoint < 0x800) return String.fromCharCode(
    codepoint >> 6 & 0x3f | 0xc0,
    codepoint & 0x3f | 0x80);
  if(codepoint < 0x10000) return String.fromCharCode(
    codepoint >> 12 & 0x0f | 0xe0,
    codepoint >> 6 & 0x3f | 0x80,
    codepoint & 0x3f | 0x80);
  if(codepoint < 0x110000) return String.fromCharCode(
    codepoint >> 18 & 0x07 | 0xf0,
    codepoint >> 12 & 0x3f | 0x80,
    codepoint >> 6 & 0x3f | 0x80,
    codepoint & 0x3f | 0x80);

  // There should be no assigned code points outside this range, but
  return String.fromCharCode(codepoint);
};

var winX = null;
var winY = null;

clientWindow = function () {
	if (self.innerHeight) {
		winX = self.innerWidth;
		winY = self.innerHeight;
	}
	else if (document.documentElement && document.documentElement.clientHeight)	{
		winX = document.documentElement.clientWidth;
		winY = document.documentElement.clientHeight;
	}
	else if (document.body) {
		winX = document.body.clientWidth;
		winY = document.body.clientHeight;
	}
};

function mt_gen() {

}

/***
 * takes a string and escapes single quotes and encodes html
 */
mt_gen.htmlspecialchars = function(str) {
	// performs HTML encoding of some given string
	mt_gen.htmlEncode_regEx = [
		new RegExp().compile(/&/ig),
		new RegExp().compile(/</ig),
		new RegExp().compile(/>/ig),
		new RegExp().compile(/'/ig),
		new RegExp().compile(/\xA0/g),
	    // \x22 means '"' -- we use hex reprezentation so that we don't disturb
	    // JS compressors (well, at least mine fails.. ;)
		new RegExp().compile(/\x22/g),
		// special encode none-ASCII
		new RegExp().compile(/[\x80-\xFF]/g)
	];
	mt_gen.htmlEncode_regExR = [
		"&amp;",
		"&lt;",
		"&gt;",
		"\\'",
		"&nbsp;",
		"&quot;",
		function(s,b){return "&#"+s.charCodeAt(0)+";";}
	];

    if(typeof str.replace == 'undefined') str = str.toString();
    for (var i = 0; i < mt_gen.htmlEncode_regEx.length; ++i)
    	str = str.replace(mt_gen.htmlEncode_regEx[i], mt_gen.htmlEncode_regExR[i]);
    return str;
};

mt_gen.getUrlFromName = function(href) {
	href = mt_gen.extractName(href).replace(/ /g,'_');
    if (href.indexOf('&') > 0 || href.indexOf('?') > 0 || href.indexOf('+') > 0 || href.indexOf('#') > 0 ||
        href.indexOf('\\') > 0 || href.indexOf('//') > 0 || href.indexOf('%') > 0
    )
        href = 'index.php?title=' + encodeURIComponent(href);
    return '/' + href;
};

mt_gen.extractName = function(href) {
    if (href.charAt(0) == '/') href = href.substr(1);
    if (href.indexOf('index.php?title=') == 0)
        href = href.substr('index.php?title='.length);
    href = href.replace(/&action=.+$/i, '');
    try { href = unescape(href); } catch (e) {}
    return href;
};

function iconify(icon_class, parentClass) {
	if (!parentClass) parentClass = 'icon';
	var span = document.createElement('span');
	span.className = parentClass;
	var img = document.createElement('img');
	img.src = '/skins/common/icons/icon-trans.gif';
	if (typeof(icon_class) != 'undefined' && icon_class != '') {
		img.className = icon_class;
	}
	span.appendChild(img);
	return span;
};

//returns an XML document
function encode_xml(data, outer) {
	var result = '';
	if (typeof(data) == 'object') {
		for (key in data) {
			var value = data[key];
			if (strncmp(value,'@',1) == 0) {
			} else {
				var tag = outer != null ? outer: key;
				if (is_numeric_array(value)) {
					result += encode_xml(value, key);
				} else if (typeof(value) == 'object') {
					var attrs = '';
					for (attr_key in value) {
						var attr_value = value[attr_key];
						if(strncmp(attr_key, '@', 1) == 0) {
							attrs += ' '+attr_key.substr(0, 1)+ '="'+mt_gen.htmlspecialchars(attr_value)+'"';
						}
					}
					result += '<' + tag + attrs + '>' + encode_xml(value) + '</' + tag + '>'+"\n";
				} else if (tag != '#text') {
					result += '<' + tag + '>' + encode_xml(value) + '</' + tag + '>';
				} else {
					result += mt_gen.htmlspecialchars(value);
				}
			}
		}
	} else if (typeof(data) == 'string') {
		result = mt_gen.htmlspecialchars(data);
	} else {
		result = data;
	}
	return result;
}
function strncmp(str1, str2, len) {
	if (typeof(str1) != 'string' || typeof(str2) != 'string') {
		return 1;
	}
	return str1.substr(0, len) == str2 ? 0: 1; //not exactly like PHP implementation, but returns 0 if true
}
function is_numeric_array(data) {
	var construct = data.constructor;
	return construct == Array;
}
