dp.sh.Brushes.Dekiscript=function()
{
    var datatypes='nil bool num str map list';
    var keywords='break case continue default else false foreach if in is let nil not null switch true typeof var where';

    this.regexList=[{regex:dp.sh.RegexLib.SingleLineCComments,css:'comment'},
                    {regex:dp.sh.RegexLib.MultiLineCComments,css:'comment'},
                    {regex:dp.sh.RegexLib.DoubleQuotedString,css:'string'},
                    {regex:dp.sh.RegexLib.SingleQuotedString,css:'string'},
                    {regex:new RegExp(this.GetKeywords(datatypes),'gm'),css:'datatypes'},
                    {regex:new RegExp(this.GetKeywords(keywords),'gm'),css:'keyword'}];
    
    this.CssClass='dp-dekiscript';this.Style='.dp-dekiscript .datatypes { color: #2E8B57; font-weight: bold; }';
}

dp.sh.Brushes.Dekiscript.prototype=new dp.sh.Highlighter();dp.sh.Brushes.Dekiscript.Aliases=['dekiscript','script'];

