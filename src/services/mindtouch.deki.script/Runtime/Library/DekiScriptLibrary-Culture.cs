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

using System.Globalization;
using MindTouch.Dream;

namespace MindTouch.Deki.Script.Runtime.Library {
    public static partial class DekiScriptLibrary {

        //--- Class Methods ---
        [DekiScriptFunction("culture.englishname", "Get culture name in English.", IsIdempotent = true)]
        public static string CultureEnglishName(
            [DekiScriptParam("culture code")] string culture
        ) {
            return new CultureInfo(culture).EnglishName;
        }

        [DekiScriptFunction("culture.nativename", "Get culture name in its own language.", IsIdempotent = true)]
        public static string CultureNativeName(
            [DekiScriptParam("culture code")] string culture
        ) {
            return new CultureInfo(culture).NativeName;
        }

        [DekiScriptFunction("culture.iso2code", "Get ISO 639-1 two-letter code for culture.", IsIdempotent = true)]
        public static string CultureIso2Code(
            [DekiScriptParam("culture code")] string culture
        ) {
            return new CultureInfo(culture).TwoLetterISOLanguageName;
        }

        [DekiScriptFunction("culture.iso3code", "Get ISO 639-2 three-letter code for culture.", IsIdempotent = true)]
        public static string CultureIso3Code(
            [DekiScriptParam("culture code")] string culture
        ) {
            return new CultureInfo(culture).ThreeLetterISOLanguageName;
        }

        private static CultureInfo GetCulture() {
            DreamContext context = DreamContext.CurrentOrNull;
            return (context == null) ? CultureInfo.CurrentCulture : context.Culture;
        }
    }
}
