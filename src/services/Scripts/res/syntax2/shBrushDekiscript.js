SyntaxHighlighter.brushes.Dekiscript = function()
{
    var datatypes = 'nil bool num str map list xml uri';
    var keywords = 'break case continue default else false foreach if in is let nil not null switch true typeof var where';

    this.regexList = [
        { regex: SyntaxHighlighter.regexLib.singleLineCComments,	css: 'comments' },
        { regex: SyntaxHighlighter.regexLib.multiLineCComments,		css: 'comments' },
        { regex: SyntaxHighlighter.regexLib.doubleQuotedString,		css: 'string' },
        { regex: SyntaxHighlighter.regexLib.singleQuotedString,		css: 'string' },
        { regex: new RegExp(this.getKeywords(datatypes), 'gmi'),	css: 'color2' },
        { regex: new RegExp(this.getKeywords(keywords), 'gmi'),		css: 'keyword'}
    ];
}

SyntaxHighlighter.brushes.Dekiscript.prototype	= new SyntaxHighlighter.Highlighter();
SyntaxHighlighter.brushes.Dekiscript.aliases	= ['dekiscript'];
