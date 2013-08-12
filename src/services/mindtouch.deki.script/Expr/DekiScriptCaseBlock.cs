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

using System;
using System.Text;

namespace MindTouch.Deki.Script {
    public class DekiScriptCaseBlock {

        //--- Fields ---
        public readonly DekiScriptExpression[] Conditions;
        public readonly DekiScriptExpression Body;
        public readonly bool IsBlock;

        //--- Constructors ---
        public DekiScriptCaseBlock(DekiScriptExpression[] conditions, DekiScriptExpression body, bool isBlock) {
            if(conditions == null) {
                throw new ArgumentNullException("conditions");
            }
            if(body == null) {
                throw new ArgumentNullException("body");
            }
            this.Conditions = conditions;
            this.Body = body;
            IsBlock = isBlock;
        }

        public void ToExprString(StringBuilder code) {
            foreach(DekiScriptExpression condition in Conditions) {
                if(condition != null) {
                    code.Append("case ").Append(condition.ToExprString()).Append(": ");
                } else {
                    code.Append("default: ");
                }
            }
            if(Body == null) {
                return;
            }
            if(IsBlock) {
                code.Append("{ ");
                code.Append(Body.ToExprString());
                code.Append("; } ");
            } else {
                code.Append(Body.ToExprString());
                code.Append("; ");
            }
        }
    }
}