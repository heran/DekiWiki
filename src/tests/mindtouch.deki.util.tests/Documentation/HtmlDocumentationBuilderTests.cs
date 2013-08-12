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
using log4net;
using MindTouch.Documentation.Util;
using MindTouch.Reflection;
using NUnit.Framework;

namespace MindTouch.Deki.Util.Tests.Documentation {

    [TestFixture]
    public class HtmlDocumentationBuilderTests {
        private static readonly ILog _log = LogUtils.CreateLog();

        [Ignore("meant for visual inspection only")]
        [Test]
        public void Output_Docs() {
            var assemblyPath = @"MindTouch.Deki.Util.Tests.dll";
            var builder = new HtmlDocumentationBuilder(new TypeInspector());
            builder.AddAssembly(assemblyPath);
            builder.BuildDocumenationPackage(@"c:\tmp\testdoc", "MindTouch.Deki.Util.Tests.Documentation.Types");
        }

        [Ignore("meant for visual inspection only")]
        [Test]
        public void Output_Sample() {
            var assemblyPath = @"MindTouch.Deki.Util.Tests.dll";
            var builder = new HtmlDocumentationBuilder(new TypeInspector());
            builder.AddAssembly(assemblyPath);
            builder.BuildDocumenationPackage(@"c:\tmp\sample", "MindTouch.Deki.Util.Tests.Documentation.Sample");
        }


        [Ignore("meant for visual inspection only")]
        [Test]
        public void Output_dream() {
            _log.Debug("generating dream docs");
            var builder = new HtmlDocumentationBuilder();
            builder.AddAssembly(@"mindtouch.dream.dll");
            builder.AddAssembly(@"mindtouch.dream.test.dll");
            builder.AddAssembly(@"mindtouch.core.dll");
            builder.BuildDocumenationPackage(@"c:\tmp\mindtouch");
            _log.Debug("generating dream docs");
        }

        [Ignore("meant for visual inspection only")]
        [Test]
        public void Output_dream_without_isolation() {
            _log.Debug("generating dream docs");
            var builder = new HtmlDocumentationBuilder(new TypeInspector());
            builder.AddAssembly(@"mindtouch.dream.dll");
            builder.AddAssembly(@"mindtouch.dream.test.dll");
            builder.AddAssembly(@"mindtouch.core.dll");
            builder.BuildDocumenationPackage(@"c:\tmp\mindtouch");
            _log.Debug("generating dream docs");
        }
    }
}
