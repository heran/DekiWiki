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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MindTouch.Deki.Script.Compiler;
using MindTouch.Deki.Script.Runtime;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Expr {
    public struct Location {
        
        //--- Class Fields ---
        public static readonly Location None = new Location(null, 0, 0);
        public static readonly Location Start = new Location(null, 1, 0);

        //--- Fields ---
        public readonly string Origin;
        public readonly int Line;
        public readonly int Column;

        //--- Constructors ---
        public Location(string origin) {
            this.Origin = origin;
            this.Line = 1;
            this.Column = 0;
        }

        public Location(int line, int column) {
            this.Origin = null;
            this.Line = line;
            this.Column = column;
        }

        public Location(string origin, int line, int column) {
            this.Origin = origin;
            this.Line = line;
            this.Column = column;
        }

        //--- Properties ---
        public bool HasValue { get { return (Origin != null) || (Line != 0) || (Column != 0); } }

        //--- Methods ---
        public override string ToString() {
            if((Origin != null) || (Line != 0) || (Column != 0)) {
                StringBuilder result = new StringBuilder();
                bool first = true;
                if(Origin != null) {
                    result.Append(Origin);
                    first = false;
                }
                if((Line != 0) || (Column != 0)) {
                    if(!first) {
                        result.Append(", ");
                    }
                    result.AppendFormat("line {0}, column {1}", Line, Column);
                }
                return result.ToString();
            }
            return string.Empty;
        }
    }

    public abstract class DekiScriptExpression {

        //--- Class Fields ---
        private static DekiScriptRuntime _evalRuntime;

        //--- Class Properties ---
        internal static DekiScriptRuntime EvalRuntime {
            get {
                if(_evalRuntime == null) {
                    _evalRuntime = new DekiScriptRuntime();
                }
                return _evalRuntime;
            }
        }

        //--- Class Methods ---
        public static DekiScriptLiteral Constant(string value) {
            if(value == null) {
                return DekiScriptNil.Value;
            }
            if(value.Length == 0) {
                return DekiScriptString.Empty;
            }
            return new DekiScriptString(value);
        }

        public static DekiScriptLiteral Constant(double value) {
            if(value == 0.0) {
                return DekiScriptNumber.Zero;
            }
            return new DekiScriptNumber(value);
        }

        public static DekiScriptLiteral Constant(int value) {
            if(value == 0) {
                return DekiScriptNumber.Zero;
            }
            return new DekiScriptNumber(value);
        }

        public static DekiScriptLiteral Constant(bool value) {
            return value ? DekiScriptBool.True : DekiScriptBool.False;
        }

        public static DekiScriptLiteral Constant(double? value) {
            if(value == null) {
                return DekiScriptNil.Value;
            }
            return Constant(value.Value);
        }

        public static DekiScriptLiteral Constant(int? value) {
            if(value == null) {
                return DekiScriptNil.Value;
            }
            return Constant(value.Value);
        }

        public static DekiScriptLiteral Constant(bool? value) {
            if(value == null) {
                return DekiScriptNil.Value;
            }
            return Constant(value.Value);
        }

        public static DekiScriptLiteral Constant(XUri value) {
            return Constant(value, true);
        }

        public static DekiScriptLiteral Constant(XUri value, bool curried) {
            return curried ? Constant(value, new DekiScriptLiteral[0]) : new DekiScriptUri(value, DekiScriptNil.Value);
        }

        public static DekiScriptLiteral Constant(XUri value, DekiScriptLiteral[] items) {
            DekiScriptLiteral args = DekiScriptNil.Value;
            if(!ArrayUtil.IsNullOrEmpty(items)) {
                args = List(items);
            } else {

                // BUGBUGBUG (steveb): we should NOT do execution when building the model!
                if(value.Fragment != null) {
                    DekiScriptExpression expr = DekiScriptParser.TryParse(value.Fragment) ?? DekiScriptNil.Value;
                    args = EvalRuntime.Evaluate(expr, DekiScriptEvalMode.EvaluateSafeMode, new DekiScriptEnv());
                }
            }
            if((args.ScriptType != DekiScriptType.NIL) && (args.ScriptType != DekiScriptType.LIST) && (args.ScriptType != DekiScriptType.MAP)) {
                throw new DekiScriptBadTypeException(Location.None, args.ScriptType, new[] { DekiScriptType.NIL, DekiScriptType.LIST, DekiScriptType.MAP });
            }
            return new DekiScriptUri(value.WithoutFragment(), args);
        }

        public static DekiScriptLiteral Constant(XUri value, DekiScriptList args) {
            return new DekiScriptUri(value.WithoutFragment(), args);
        }

        public static DekiScriptLiteral Constant(XUri value, DekiScriptMap args) {
            return new DekiScriptUri(value.WithoutFragment(), args);
        }

        public static DekiScriptLiteral Constant(XDoc doc) {
            return new DekiScriptXml(doc);
        }

        public static DekiScriptExpression ReturnScope(Location location, DekiScriptExpression value) {
            return new DekiScriptReturnScope(value) { Location = location };
        }

        public static DekiScriptExpression Call(Location location, DekiScriptExpression prefix, DekiScriptExpression arguments) {
            return new DekiScriptCall(prefix, arguments, false) { Location = location };
        }

        public static DekiScriptExpression Curry(Location location, DekiScriptExpression prefix, DekiScriptExpression arguments) {
            return new DekiScriptCall(prefix, arguments, true) { Location = location };
        }

        public static DekiScriptExpression BreakStatement(Location location) {
            return new DekiScriptAbort(DekiScriptAbort.Kind.Break) { Location = location };
        }

        public static DekiScriptExpression ContinueStatement(Location location) {
            return new DekiScriptAbort(DekiScriptAbort.Kind.Continue) { Location = location };
        }

        public static DekiScriptExpression ReturnStatement(Location location, DekiScriptExpression expr) {
            return new DekiScriptReturn(expr) { Location = location };
        }

        public static DekiScriptExpression Access(Location location, DekiScriptExpression prefix, DekiScriptExpression index) {
            return new DekiScriptAccess(prefix, index) { Location = location };
        }

        public static DekiScriptExpression LetStatement(Location location, DekiScriptExpression target, DekiScriptExpression value) {
            if(!(target is DekiScriptVar)) {
                throw new ArgumentException("must be a variable name", "target");
            }
            string variable = ((DekiScriptVar)target).Name;
            return new DekiScriptAssign(variable, value, false) { Location = location };
        }

        public static DekiScriptExpression VarStatement(Location location, DekiScriptExpression target, DekiScriptExpression value) {
            if(!(target is DekiScriptVar)) {
                throw new ArgumentException("must be a variable name", "target");
            }
            string variable = ((DekiScriptVar)target).Name;
            return new DekiScriptAssign(variable, value, true) { Location = location };
        }

        public static DekiScriptExpression BinaryOp(Location location, DekiScriptBinary.Op opcode, DekiScriptExpression left, DekiScriptExpression right) {
            return new DekiScriptBinary(opcode, left, right) { Location = location };
        }

        public static DekiScriptExpression DiscardStatement(Location location, DekiScriptExpression value) {
            return new DekiScriptDiscard(value) { Location = location };
        }

        public static DekiScriptExpression ForeachStatement(Location location, DekiScriptGenerator generator, IEnumerable<DekiScriptExpression> body) {
            return new DekiScriptForeach(generator, Block(location, body)) { Location = location };
        }

        public static DekiScriptExpression ForeachStatement(Location location, DekiScriptGenerator generator, DekiScriptExpression body) {
            return new DekiScriptForeach(generator, body) { Location = location };
        }

        public static DekiScriptLiteral List(params DekiScriptLiteral[] items) {
            var result = new DekiScriptList();
            foreach(var item in items) {
                result.Add(item);
            }
            return result;
        }

        public static DekiScriptExpression List(Location location, IEnumerable<DekiScriptExpression> items) {
            return new DekiScriptListConstructor(null, items.ToArray()) { Location = location };
        }

        public static DekiScriptExpression List(Location location, DekiScriptGenerator generator, params DekiScriptExpression[] items) {
            return new DekiScriptListConstructor(generator, items) { Location = location };
        }

        public static DekiScriptExpression MagicId(Location location, string name ) {
            return new DekiScriptMagicId(name) { Location = location };
        }

        public static DekiScriptExpression Map(Location location, params DekiScriptMapConstructor.FieldConstructor[] fields) {
            return new DekiScriptMapConstructor(null, fields) { Location = location };
        }

        public static DekiScriptExpression Map(Location location, DekiScriptGenerator generator, params DekiScriptMapConstructor.FieldConstructor[] fields) {
            return new DekiScriptMapConstructor(generator, fields) { Location = location };
        }

        public static DekiScriptExpression SwitchStatement(Location location, DekiScriptExpression value, DekiScriptSwitch.CaseBlock[] cases) {
            return new DekiScriptSwitch(value, cases) { Location = location };
        }

        public static DekiScriptSwitch.CaseBlock SwitchCaseBlock(Location location, IEnumerable<DekiScriptExpression> conditions, DekiScriptExpression body) {
            return new DekiScriptSwitch.CaseBlock(location, conditions.ToArray(), body);
        }

        public static DekiScriptExpression TernaryOp(Location location, DekiScriptExpression test, DekiScriptExpression left, DekiScriptExpression right) {
            return new DekiScriptTernary(test, left, right, false) { Location = location };
        }

        public static DekiScriptExpression UnaryOp(Location location, DekiScriptUnary.Op opcode, DekiScriptExpression value) {
            return new DekiScriptUnary(opcode, value) { Location = location };
        }

        public static DekiScriptExpression Id(Location location, string name) {
            if(name.StartsWithInvariant("$") && (name.Length >= 2)) {
                if(char.IsDigit(name[1])) {
                    return Access(location, Id(location, "$"), Constant(double.Parse(name.Substring(1))));
                }
                return Access(location, Id(location, "$"), Constant(name.Substring(1)));
            }
            return new DekiScriptVar(name);
        }

        public static DekiScriptExpression XmlElement(Location location, string prefix, DekiScriptExpression name, DekiScriptXmlElement.Attribute[] attributes, DekiScriptExpression value) {
            return new DekiScriptXmlElement(prefix, name, attributes, value) { Location = location };
        }

        public static DekiScriptExpression IfElseStatement(Location location, DekiScriptExpression test, DekiScriptExpression left, DekiScriptExpression right) {
            return new DekiScriptTernary(test, left, right, true) { Location = location };
        }

        public static DekiScriptExpression IfElseStatements(Location location, IEnumerable<Tuplet<DekiScriptExpression, DekiScriptExpression>> conditionals) {
            DekiScriptExpression result = DekiScriptNil.Value;
            foreach(var cond in conditionals.Reverse()) {
                result = (cond.Item1 != null) ? new DekiScriptTernary(cond.Item1, cond.Item2, result, true) { Location = location } : cond.Item2;
            }
            return result;
        }

        public static DekiScriptExpression BlockWithDeclaration(Location location, DekiScriptExpression declaration, DekiScriptExpression body) {
            List<DekiScriptExpression> items = new List<DekiScriptExpression> {
                DiscardStatement(location, declaration), 
                body
            };
            return Block(location, items);
        }

        public static DekiScriptExpression BlockWithDeclaration(Location location, DekiScriptExpression declaration, IEnumerable<DekiScriptExpression> body) {
            List<DekiScriptExpression> items = new List<DekiScriptExpression> { DiscardStatement(declaration.Location, declaration) };
            items.AddRange(body);
            return Block(location, items);
        }

        public static DekiScriptExpression Block(Location location, IEnumerable<DekiScriptExpression> body) {
            List<DekiScriptExpression> accumulator = new List<DekiScriptExpression>();
            Flatten(accumulator, body);
            switch(accumulator.Count) {
            case 0:
                return DekiScriptNil.Value;
            case 1: {
                    return accumulator[0];
                }
            default:
                return new DekiScriptSequence(accumulator.ToArray()) { Location = location };
            }
        }

        public static DekiScriptExpression TryCatchFinally(Location location, DekiScriptExpression @try, DekiScriptExpression @catch, DekiScriptExpression @finally) {
            return new DekiScriptTryCatchFinally(@try, @catch, null, @finally) { Location = location };
        }

        private static void Flatten(ICollection<DekiScriptExpression> accumulator, IEnumerable<DekiScriptExpression> list) {
            foreach(var expression in list) {
                if(expression is DekiScriptNil) {

                    // ignore expression
                } else if(expression is DekiScriptSequence) {
                    DekiScriptSequence expr = (DekiScriptSequence)expression;
                    Flatten(accumulator, expr.List);
                } else {
                    accumulator.Add(expression);
                }
            }
        }

        //--- Fields ---
        private Location _location = Location.None;

        //--- Properties ---
        public Location Location {
            get {
                return _location;
            }
            private set {
                _location = value;
            }
        }

        //--- Abstract Methods ---
        public abstract TReturn VisitWith<TState, TReturn>(IDekiScriptExpressionVisitor<TState, TReturn> visitor, TState state);

        //--- Methods ---
        public override string ToString() {
            StringBuilder builder = new StringBuilder();
            VisitWith(DekiScriptExpressionTextWriter.Instance, builder);
            return builder.ToString();
        }
    }
}