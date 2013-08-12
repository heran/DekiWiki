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

namespace MindTouch.Deki.Script {

    // Script Types
    // ------------
    //      any     - any of the script types
    //      nil     - nil type (nil)
    //      bool    - boolean type (true, false)
    //      num     - numeric type (e.g. 0, -2, 3.1415, ...)
    //      str     - string type (e.g. "", "hello world!")
    //      uri     - uri type (e.g. http://www.acme.com/primenumbers, ...)
    //      map     - map type (e.g. { a : true, "hi" : "bye", ...)
    //      list    - list type (e.g. [ 1, 2, 3 ])
    //      xml     - xml type (e.g. <html><body>Hi!</body></html>)

    public enum DekiScriptType {
        ANY,
        NIL,
        BOOL,
        NUM,
        STR,
        MAP,
        LIST,
        URI,
        XML
    }

    public enum DekiScriptEvalMode {
        None,
        Verify,
        Evaluate,
        EvaluateSafeMode,
        EvaluateSaveOnly,
        EvaluateEditOnly
    }
}
