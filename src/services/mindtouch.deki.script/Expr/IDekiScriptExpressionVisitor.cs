/*
 * MindTouch DekiScript - embeddable web-oriented scripting runtime
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit wiki.developer.mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace MindTouch.Deki.Script.Expr {
    public interface IDekiScriptExpressionVisitor<TState, TReturn> {

        //--- Methods ---
        TReturn Visit(DekiScriptAbort expr, TState state);
        TReturn Visit(DekiScriptAccess expr, TState state);
        TReturn Visit(DekiScriptAssign expr, TState state);
        TReturn Visit(DekiScriptBinary expr, TState state);
        TReturn Visit(DekiScriptReturnScope expr, TState state);
        TReturn Visit(DekiScriptBool expr, TState state);
        TReturn Visit(DekiScriptCall expr, TState state);
        TReturn Visit(DekiScriptDiscard expr, TState state);
        TReturn Visit(DekiScriptForeach expr, TState state);
        TReturn Visit(DekiScriptList expr, TState state);
        TReturn Visit(DekiScriptListConstructor expr, TState state);
        TReturn Visit(DekiScriptMagicId expr, TState state);
        TReturn Visit(DekiScriptMap expr, TState state);
        TReturn Visit(DekiScriptMapConstructor expr, TState state);
        TReturn Visit(DekiScriptNil expr, TState state);
        TReturn Visit(DekiScriptNumber expr, TState state);
        TReturn Visit(DekiScriptReturn expr, TState state);
        TReturn Visit(DekiScriptSequence expr, TState state);
        TReturn Visit(DekiScriptString expr, TState state);
        TReturn Visit(DekiScriptSwitch expr, TState state);
        TReturn Visit(DekiScriptTernary expr, TState state);
        TReturn Visit(DekiScriptTryCatchFinally expr, TState state);
        TReturn Visit(DekiScriptUnary expr, TState state);
        TReturn Visit(DekiScriptUnknown expr, TState state);
        TReturn Visit(DekiScriptUri expr, TState state);
        TReturn Visit(DekiScriptVar expr, TState state);
        TReturn Visit(DekiScriptXml expr, TState state);
        TReturn Visit(DekiScriptXmlElement expr, TState state);
    }
}