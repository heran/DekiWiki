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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MindTouch.Deki.Script.Runtime;
using MindTouch.Deki.Script.Runtime.TargetInvocation;
using MindTouch.Dream;

namespace MindTouch.Deki.Script.Tests.Util {
    public class DekiScriptTestRuntime : DekiScriptRuntime {

        //--- Fields ---
        private Dictionary<string, XUri> _funcMap = new Dictionary<string, XUri>();
        private TimeSpan _evaluationTimeout = TimeSpan.MaxValue;

        //--- Properties ---
        protected override TimeSpan EvaluationTimeout { get { return _evaluationTimeout; } }

        //--- Methods ---
        public void SetTimeout(TimeSpan evaluationTimeout) {
            _evaluationTimeout = evaluationTimeout;
        }

        public void RegisterFunction(string functionName, MethodInfo method, DekiScriptNativeInvocationTarget.Parameter[] parameters) {
            var target = new DekiScriptNativeInvocationTarget(null, method, parameters.ToArray());
            var function = new DekiScriptInvocationTargetDescriptor(target.Access, false, false, functionName, target.Parameters, target.ReturnType, "", "", target);
            var functionPointer = new XUri("native:///").At(function.SystemName);
            Functions[functionPointer] = function;
            _funcMap[functionName] = functionPointer;
        }

        public void RegisterFunction(string functionName, XUri uri) {
            _funcMap[functionName] = uri;
        }

        public override DekiScriptEnv CreateEnv() {
            var env = base.CreateEnv();
            foreach(var funcMap in _funcMap) {
                env.Vars.AddNativeValueAt(funcMap.Key, funcMap.Value);
            }
            return env;
        }
    }
}