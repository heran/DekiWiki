/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit developer.mindtouch.com;
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
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Tests.Util;
using NUnit.Framework;


namespace MindTouch.Deki.Script.Tests.ScriptTests {

    [TestFixture]
    public class DekiScriptLibraryIteratorTests {

        //--- Fields ---
        private DekiScriptTester _t;

        [SetUp]
        public void Setup() {
            _t = new DekiScriptTester();
        }

        [Test]
        public void Can_call_iterator_on_list_to_build_list() {
            _t.Test(@"
var x = [1,2,3];
[ (v2)
    foreach 
        var v in x,
        var v2 = v*v
];",
                "[ 1, 4, 9 ]",
                typeof(DekiScriptList));
        }

        [Test]
        public void Can_call_iterator_with_where_clause() {
            _t.Test(@"
var x = [1,2,3];
[ (v2)
    foreach 
        var v in x where v == 2,
        var v2 = v*v
];",
                "[ 4 ]",
                typeof(DekiScriptList));
        }

        [Test]
        public void Can_call_iterator_with_if_statement() {
            _t.Test(@"
var x = [1,2,3];
[ (v2)
    foreach 
        var v in x,
        if( v == 2),
        var v2 = v*v
];",
                "[ 4 ]",
                typeof(DekiScriptList));
        }

        [Test]
        public void Can_call_iterator_with_var_before_if_statement() {
            _t.Test(@"
var x = [1,2,3];
[ (v2)
    foreach 
        var v in x,
        var v2 = v*v,
        if( v2 == 4)
];",
                "[ 4 ]",
                typeof(DekiScriptList));
        }

        [Test]
        public void Can_call_simple_iterator_on_map_to_build_map() {
            _t.Test(@"
var x = {
    a: 1,
    b: 2,
    c: 3
};
{ (k2): v2
    foreach
        var k : v in x,
        var k2 = 'key'..k,
        var v2 = v*v
};",
                "{ keya : 1, keyb : 4, keyc : 9 }",
                typeof(DekiScriptMap));
        }

        [Test]
        public void Can_call_iterator_on_map_using_keys_to_build_map() {
            _t.Test(@"
var x = {
    a: 1,
    b: 2,
    c: 3
};
{ (k2): v2
    foreach
        var k in map.keys(x),
        var v = x[k],
        var k2 = 'key'..k,
        var v2 = v*v
};",
                "{ keya : 1, keyb : 4, keyc : 9 }",
                typeof(DekiScriptMap));
        }

    }
}