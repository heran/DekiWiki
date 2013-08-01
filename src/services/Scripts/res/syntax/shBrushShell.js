dp.sh.Brushes.Shell = function()
{

	var keywords='case do done elif else esac fi for function if in select then time until while';
	
	var builtins='alias bg bind break builtin cd command compgen complete continue declare dirs disown echo enable eval exec exit export fc fg getopts hash help history jobs kill let local logout popd printf pushd pwd read readonly return set shift shopt source suspend test times trap type typeset ulimit umask unalias unset wait';
	
	var commands='awk bash diff cat chmod chown chgrp cp cut date ed env file gawk grep gunzip gzip hostname kill ksh ln ls mail mailx mkdir mv nice ping ps pwd rm rmdir sed sleep sort su tar touch uname zcat';

	var special = 'true false MESG';

	this.regexList = [
		{ regex: dp.sh.RegexLib.DoubleQuotedString, css: 'string' },
		{ regex: dp.sh.RegexLib.SingleQuotedString, css: 'string' },
		{ regex: new RegExp('(\\`).*(\\`)', 'gm'), css: 'exec' },
		{ regex: new RegExp('(\\$\\{).*(\\})', 'g'), css: 'vars' },
		{ regex: new RegExp('(\\$)\\w+', 'g'), css: 'vars' },
		{ regex: new RegExp('\\w+\\=', 'g'), css: 'var-assignment' },
		{ regex: new RegExp(this.GetKeywords(builtins), 'g'), css: 'builtins' },
		{ regex: new RegExp(this.GetKeywords(keywords), 'g'), css: 'keyword' },
		{ regex: new RegExp(this.GetKeywords(special), 'gi'), css: 'special' },
		{ regex: new RegExp(this.GetKeywords(commands), 'g'), css: 'commands' },
		{ regex: dp.sh.RegexLib.SingleLinePerlComments, css: 'comment' }
	];

	this.CssClass = 'dp-shell';
	this.Style =	'.dp-shell .exec { color: maroon; }' +
			'.dp-shell .vars { color: navy; font-weight: bold; }' +
			'.dp-shell .var-assignment { color: navy; }' +
			'.dp-shell .builtins { color: purple; font-weight: bold; }' +
			'.dp-shell .commands { color: maroon; font-weight: bold; }' +
			'.dp-shell .special { font-weight: bold; }';
}

dp.sh.Brushes.Shell.prototype	= new dp.sh.Highlighter();
dp.sh.Brushes.Shell.Aliases	= ['sh', 'bash', 'ksh'];
