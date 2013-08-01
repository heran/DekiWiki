using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

using MindTouch.Deki.Script;
using MindTouch.Deki.Script.Expr;



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

namespace MindTouch.Deki.Script.Compiler {



internal class Parser {
	public const int _EOF = 0;
	public const int _name = 1;
	public const int _magicid = 2;
	public const int _rawstring = 3;
	public const int _quotedstring = 4;
	public const int _number = 5;
	public const int _hexnumber = 6;
	public const int _htmlentity = 7;
	public const int maxT = 74;

	const bool T = true;
	const bool x = false;
	const int minErrDist = 2;
	
	public Scanner scanner;
	public Errors  errors;

	public Token t;    // last recognized token
	public Token la;   // lookahead token
	int errDist = minErrDist;
	
internal DekiScriptExpression result = DekiScriptNil.Value;



	public Parser(Scanner scanner) {
		this.scanner = scanner;
		errors = new Errors();
	}

	void SynErr (int n) {
		if (errDist >= minErrDist) errors.SynErr(la.origin, la.line, la.col, n);
		errDist = 0;
	}

	public void SemErr (string msg) {
		if (errDist >= minErrDist) errors.SemErr(la.origin, t.line, t.col, msg);
		errDist = 0;
	}
	
	void Get () {
		for (;;) {
			t = la;
			la = scanner.Scan();
			if (la.kind <= maxT) { ++errDist; break; }

			la = t;
		}
	}
	
	void Expect (int n) {
		if (la.kind==n) Get(); else { SynErr(n); }
	}
	
	bool StartOf (int s) {
		return set[s, la.kind];
	}
	
	void ExpectWeak (int n, int follow) {
		if (la.kind == n) Get();
		else {
			SynErr(n);
			while (!StartOf(follow)) Get();
		}
	}


	bool WeakSeparator(int n, int syFol, int repFol) {
		int kind = la.kind;
		if (kind == n) {Get(); return true;}
		else if (StartOf(repFol)) {return false;}
		else {
			SynErr(n);
			while (!(set[syFol, kind] || set[repFol, kind] || set[0, kind])) {
				Get();
				kind = la.kind;
			}
			return StartOf(syFol);
		}
	}

	
	void DekiScript() {
		result = DekiScriptNil.Value; 
		if (StartOf(1)) {
			Statements(out result);
		}
	}

	void Statements(out DekiScriptExpression expr) {
		DekiScriptExpression list = DekiScriptNil.Value; expr = DekiScriptNil.Value; Location location = t.Location; 
		if (la.kind == 19) {
			DefineStatement(out expr);
			if (la.kind == 8) {
				Get();
				if (StartOf(1)) {
					Statements(out list);
				}
			}
			expr = DekiScriptExpression.Block(location, new[] { expr, list }); 
		} else if (la.kind == 11) {
			AssignStatement(out expr);
			if (la.kind == 8) {
				Get();
				if (StartOf(1)) {
					Statements(out list);
				}
			}
			expr = DekiScriptExpression.Block(location, new[] { expr, list }); 
		} else if (la.kind == 21) {
			IfElseStatement(out expr);
			if (StartOf(1)) {
				Statements(out list);
			}
			expr = DekiScriptExpression.Block(location, new[] { expr, list }); 
		} else if (la.kind == 28) {
			ForeachStatement(out expr);
			if (StartOf(1)) {
				Statements(out list);
			}
			expr = DekiScriptExpression.Block(location, new[] { expr, list }); 
		} else if (la.kind == 29) {
			SwitchStatement(out expr);
			if (StartOf(1)) {
				Statements(out list);
			}
			expr = DekiScriptExpression.Block(location, new[] { expr, list }); 
		} else if (la.kind == 33 || la.kind == 34 || la.kind == 35) {
			FlowControlStatement(out expr);
			if (la.kind == 8) {
				Get();
				if (StartOf(1)) {
					Statements(out list);
				}
			}
			expr = DekiScriptExpression.Block(location, new[] { expr, list }); 
		} else if (la.kind == 25) {
			TryCatchFinallyStatement(out expr);
			if (la.kind == 8) {
				Get();
				if (StartOf(1)) {
					Statements(out list);
				}
			}
			expr = DekiScriptExpression.Block(location, new[] { expr, list }); 
		} else if (la.kind == 45) {
			XmlStatement(out expr);
			if (StartOf(1)) {
				Statements(out list);
			}
			expr = DekiScriptExpression.Block(location, new[] { expr, list }); 
		} else if (StartOf(2)) {
			Expression(out expr);
			if (la.kind == 8) {
				Get();
				if (StartOf(1)) {
					Statements(out list);
				}
			}
			expr = DekiScriptExpression.Block(location, new[] { expr, list }); 
		} else if (la.kind == 8) {
			Get();
			if (StartOf(1)) {
				Statements(out expr);
			}
		} else SynErr(75);
	}

	void DefineStatement(out DekiScriptExpression expr) {
		List<DekiScriptExpression> definitions = new List<DekiScriptExpression>(); Location location = t.Location; 
		Expect(19);
		DefineConstruct(out expr);
		definitions.Add(expr); 
		while (la.kind == 20) {
			Get();
			DefineConstruct(out expr);
			definitions.Add(expr); 
		}
		expr = DekiScriptExpression.Block(location, definitions.ToArray()); 
	}

	void AssignStatement(out DekiScriptExpression expr) {
		expr = null; Location location = t.Location; DekiScriptExpression var; DekiScriptBinary.Op op = DekiScriptBinary.Op.LeftValue; 
		Expect(11);
		AssignLHS(out var);
		switch (la.kind) {
		case 12: {
			Get();
			break;
		}
		case 13: {
			Get();
			op = DekiScriptBinary.Op.Addition; 
			break;
		}
		case 14: {
			Get();
			op = DekiScriptBinary.Op.Subtraction; 
			break;
		}
		case 15: {
			Get();
			op = DekiScriptBinary.Op.Multiplication; 
			break;
		}
		case 16: {
			Get();
			op = DekiScriptBinary.Op.Division; 
			break;
		}
		case 17: {
			Get();
			op = DekiScriptBinary.Op.Modulo; 
			break;
		}
		case 18: {
			Get();
			op = DekiScriptBinary.Op.Concat; 
			break;
		}
		default: SynErr(76); break;
		}
		location = t.Location; 
		Expression(out expr);
		expr = DekiScriptExpression.LetStatement(location, var, (op == DekiScriptBinary.Op.LeftValue) ? expr : DekiScriptExpression.BinaryOp(expr.Location, op, var, expr)); 
	}

	void IfElseStatement(out DekiScriptExpression expr) {
		DekiScriptExpression left = null, right = DekiScriptNil.Value; Location location = Location.None; 
		Expect(21);
		location = t.Location; 
		Expect(22);
		Expression(out expr);
		Expect(23);
		BlockStatement(out left);
		if (la.kind == 24) {
			Get();
			BlockStatement(out right);
		}
		expr = DekiScriptExpression.IfElseStatement(location, expr, left, right); 
	}

	void ForeachStatement(out DekiScriptExpression expr) {
		DekiScriptExpression block = null; Location location = Location.None; DekiScriptGenerator gen = null; 
		Expect(28);
		location = t.Location; 
		Expect(22);
		GeneratorHead(out gen);
		Expect(23);
		BlockStatement(out block);
		expr = DekiScriptExpression.ForeachStatement(location, gen, block); 
	}

	void SwitchStatement(out DekiScriptExpression expr) {
		DekiScriptSwitch.CaseBlock caseStatement; List<DekiScriptSwitch.CaseBlock> cases = new List<DekiScriptSwitch.CaseBlock>(); Location location = Location.None; 
		Expect(29);
		location = t.Location; 
		Expect(22);
		Expression(out expr);
		Expect(23);
		Expect(9);
		while (la.kind == 30 || la.kind == 32) {
			CaseStatement(out caseStatement);
			cases.Add(caseStatement); 
		}
		Expect(10);
		expr = DekiScriptExpression.SwitchStatement(location, expr, cases.ToArray()); 
	}

	void FlowControlStatement(out DekiScriptExpression expr) {
		expr = DekiScriptNil.Value; Location location = t.Location; 
		if (la.kind == 33) {
			Get();
			expr = DekiScriptExpression.BreakStatement(location); 
		} else if (la.kind == 34) {
			Get();
			expr = DekiScriptExpression.ContinueStatement(location); 
		} else if (la.kind == 35) {
			Get();
			if (StartOf(2)) {
				Expression(out expr);
			}
			expr = DekiScriptExpression.ReturnStatement(location, expr); 
		} else SynErr(77);
	}

	void TryCatchFinallyStatement(out DekiScriptExpression expr) {
		DekiScriptExpression tryStatement = DekiScriptNil.Value;
		DekiScriptExpression catchStatement = DekiScriptNil.Value; 
		DekiScriptExpression finallyStatement = DekiScriptNil.Value;
		Location location = Location.None;
		
		Expect(25);
		BlockStatement(out tryStatement);
		location = t.Location; 
		if (la.kind == 26) {
			Get();
			BlockStatement(out catchStatement);
		}
		if (la.kind == 27) {
			Get();
			BlockStatement(out finallyStatement);
		}
		expr = DekiScriptExpression.TryCatchFinally(location, tryStatement, catchStatement, finallyStatement); 
	}

	void XmlStatement(out DekiScriptExpression expr) {
		expr = null; 
		Xml(out expr);
	}

	void Expression(out DekiScriptExpression expr) {
		DekiScriptExpression inner = null; Location location = Location.None; 
		TernaryExpression(out expr);
		while (la.kind == 36) {
			Get();
			location = t.Location; 
			TernaryExpression(out inner);
			expr = DekiScriptExpression.TryCatchFinally(location, expr, inner, DekiScriptNil.Value); 
		}
	}

	void BlockStatement(out DekiScriptExpression expr) {
		expr = DekiScriptNil.Value; 
		if (la.kind == 9) {
			Get();
			if (StartOf(1)) {
				Statements(out expr);
			}
			Expect(10);
		} else if (la.kind == 19) {
			DefineStatement(out expr);
			Expect(8);
		} else if (la.kind == 11) {
			AssignStatement(out expr);
			Expect(8);
		} else if (la.kind == 21) {
			IfElseStatement(out expr);
		} else if (la.kind == 28) {
			ForeachStatement(out expr);
		} else if (la.kind == 29) {
			SwitchStatement(out expr);
		} else if (la.kind == 33 || la.kind == 34 || la.kind == 35) {
			FlowControlStatement(out expr);
			Expect(8);
		} else if (la.kind == 25) {
			TryCatchFinallyStatement(out expr);
			Expect(8);
		} else if (la.kind == 45) {
			XmlStatement(out expr);
			Expect(8);
		} else if (StartOf(3)) {
			if (StartOf(2)) {
				Expression(out expr);
			}
			Expect(8);
		} else SynErr(78);
	}

	void AssignLHS(out DekiScriptExpression expr) {
		Location location = t.Location; expr = null; 
		Expect(1);
		expr = DekiScriptExpression.Id(t.Location, t.val); 
	}

	void DefineConstruct(out DekiScriptExpression expr) {
		expr = null; DekiScriptExpression var; Location location = Location.None; 
		AssignLHS(out var);
		if (la.kind == 12) {
			Get();
			location = t.Location; 
			Expression(out expr);
			expr = DekiScriptExpression.VarStatement(location, var, expr); 
		} else if (StartOf(4)) {
			expr = DekiScriptExpression.VarStatement(location, var, DekiScriptNil.Value);
		} else SynErr(79);
	}

	void GeneratorHead(out DekiScriptGenerator gen) {
		Location location = Location.None;
		Location wherelocation = Location.None;
		DekiScriptExpression where = null; 
		List<string> names = new List<string>(); 
		DekiScriptExpression expr = null; 
		string value = null;
		gen = null;
		
		Expect(19);
		location = t.Location; 
		Expect(1);
		names.Add(t.val); 
		if (la.kind == 31) {
			Get();
			Expect(1);
			value = t.val; 
			Expect(52);
			Expression(out expr);
			if (la.kind == 70) {
				Get();
				wherelocation = t.Location; 
				Expression(out where);
			}
		} else if (la.kind == 20 || la.kind == 52) {
			while (la.kind == 20) {
				Get();
				Expect(1);
				names.Add(t.val); 
			}
			Expect(52);
			Expression(out expr);
			if (la.kind == 70) {
				Get();
				wherelocation = t.Location; 
				Expression(out where);
			}
		} else SynErr(80);
		if (la.kind == 20) {
			Get();
			GeneratorNext(out gen);
		}
		if(where != null) gen = new DekiScriptGeneratorIf(wherelocation, where, gen);
		if(value == null) gen = new DekiScriptGeneratorForeachValue(location, names.ToArray(), expr, gen);
		else gen = new DekiScriptGeneratorForeachKeyValue(location, names[0], value, expr, gen);
		
	}

	void CaseStatement(out DekiScriptSwitch.CaseBlock block) {
		List<DekiScriptExpression> conditions = new List<DekiScriptExpression>();
		DekiScriptExpression caseexpr = null;
		DekiScriptExpression expr = DekiScriptNil.Value;
		Location location = t.Location;
		
		if (la.kind == 30) {
			Get();
			Expression(out caseexpr);
			Expect(31);
			conditions.Add(caseexpr); 
		} else if (la.kind == 32) {
			Get();
			Expect(31);
			conditions.Add(null); 
		} else SynErr(81);
		while (la.kind == 30 || la.kind == 32) {
			if (la.kind == 30) {
				Get();
				Expression(out caseexpr);
				Expect(31);
				conditions.Add(caseexpr); 
			} else {
				Get();
				Expect(31);
				conditions.Add(null); 
			}
		}
		if (StartOf(1)) {
			if (la.kind == 9) {
				Get();
				if (StartOf(1)) {
					Statements(out expr);
				}
				Expect(10);
			} else {
				Statements(out expr);
			}
		}
		block = DekiScriptExpression.SwitchCaseBlock(location, conditions, expr); 
	}

	void Xml(out DekiScriptExpression expr) {
		XmlNode(out expr);
	}

	void TernaryExpression(out DekiScriptExpression expr) {
		DekiScriptExpression left = null, right = null; Location location = Location.None; 
		NullCoalescingExpr(out expr);
		if (la.kind == 37) {
			Get();
			location = t.Location; 
			Expression(out left);
			Expect(31);
			Expression(out right);
			expr = DekiScriptExpression.TernaryOp(location, expr, left, right); 
		}
	}

	void NullCoalescingExpr(out DekiScriptExpression expr) {
		DekiScriptExpression inner = null; Location location = Location.None; 
		OrExpr(out expr);
		while (la.kind == 38) {
			Get();
			location = t.Location; 
			OrExpr(out inner);
			expr = DekiScriptExpression.BinaryOp(location, DekiScriptBinary.Op.NullCoalesce, expr, inner); 
		}
	}

	void OrExpr(out DekiScriptExpression expr) {
		DekiScriptExpression inner = null; Location location = Location.None; 
		AndExpr(out expr);
		while (la.kind == 39) {
			Get();
			location = t.Location; 
			AndExpr(out inner);
			expr = DekiScriptExpression.BinaryOp(location, DekiScriptBinary.Op.LogicalOr, expr, inner); 
		}
	}

	void AndExpr(out DekiScriptExpression expr) {
		DekiScriptExpression inner = null; Location location = Location.None; 
		EqlExpr(out expr);
		while (la.kind == 40) {
			Get();
			location = t.Location; 
			EqlExpr(out inner);
			expr = DekiScriptExpression.BinaryOp(location, DekiScriptBinary.Op.LogicalAnd, expr, inner); 
		}
	}

	void EqlExpr(out DekiScriptExpression expr) {
		DekiScriptExpression inner = null; DekiScriptBinary.Op op = DekiScriptBinary.Op.LeftValue; Location location = Location.None; 
		RelExpr(out expr);
		while (StartOf(5)) {
			if (la.kind == 41) {
				Get();
				op = DekiScriptBinary.Op.NotEqual; 
			} else if (la.kind == 42) {
				Get();
				op = DekiScriptBinary.Op.Equal; 
			} else if (la.kind == 43) {
				Get();
				op = DekiScriptBinary.Op.IdentityNotEqual; 
			} else {
				Get();
				op = DekiScriptBinary.Op.IdentityEqual; 
			}
			location = t.Location; 
			RelExpr(out inner);
			expr = DekiScriptExpression.BinaryOp(location, op, expr, inner); 
		}
	}

	void RelExpr(out DekiScriptExpression expr) {
		DekiScriptExpression inner = null; 
		DekiScriptBinary.Op op = DekiScriptBinary.Op.LeftValue; 
		Location location = Location.None;
		bool negate = false;
		
		ConcatExpr(out expr);
		while (StartOf(6)) {
			if (StartOf(7)) {
				if (la.kind == 45) {
					Get();
					op = DekiScriptBinary.Op.LessThan; 
				} else if (la.kind == 46) {
					Get();
					op = DekiScriptBinary.Op.GreaterThan; 
				} else if (la.kind == 47) {
					Get();
					op = DekiScriptBinary.Op.LessOrEqual; 
				} else {
					Get();
					op = DekiScriptBinary.Op.GreaterOrEqual; 
				}
				location = t.Location; 
				ConcatExpr(out inner);
				expr = DekiScriptExpression.BinaryOp(location, op, expr, inner); 
			} else if (la.kind == 49) {
				Get();
				location = t.Location; 
				if (la.kind == 50) {
					Get();
					negate = true; 
				}
				if (la.kind == 51) {
					Get();
					expr = DekiScriptExpression.BinaryOp(location, DekiScriptBinary.Op.IsType, expr, DekiScriptExpression.Constant("nil")); 
				} else if (la.kind == 1) {
					Get();
					expr = DekiScriptExpression.BinaryOp(location, DekiScriptBinary.Op.IsType, expr, DekiScriptExpression.Constant(t.val)); 
				} else SynErr(82);
			} else {
				if (la.kind == 50) {
					Get();
					negate = true; 
				}
				Expect(52);
				location = t.Location; 
				ConcatExpr(out inner);
				expr = DekiScriptExpression.BinaryOp(location, DekiScriptBinary.Op.InCollection, expr, inner); 
			}
			if(negate) expr = DekiScriptExpression.UnaryOp(location, DekiScriptUnary.Op.LogicalNot, expr); 
		}
	}

	void ConcatExpr(out DekiScriptExpression expr) {
		DekiScriptExpression inner = null; Location location = Location.None; DekiScriptBinary.Op op = DekiScriptBinary.Op.LeftValue; 
		AddExpr(out expr);
		while (la.kind == 53 || la.kind == 54) {
			if (la.kind == 53) {
				Get();
				op = DekiScriptBinary.Op.Concat; 
			} else {
				Get();
				op = DekiScriptBinary.Op.UriAppend; 
			}
			location = t.Location; 
			AddExpr(out inner);
			expr = DekiScriptExpression.BinaryOp(location, op, expr, inner); 
		}
	}

	void AddExpr(out DekiScriptExpression expr) {
		DekiScriptExpression inner = null; DekiScriptBinary.Op op = DekiScriptBinary.Op.LeftValue; Location location = Location.None; 
		MulExpr(out expr);
		while (la.kind == 55 || la.kind == 56) {
			if (la.kind == 55) {
				Get();
				op = DekiScriptBinary.Op.Addition; 
			} else {
				Get();
				op = DekiScriptBinary.Op.Subtraction; 
			}
			location = t.Location; 
			MulExpr(out inner);
			expr = DekiScriptExpression.BinaryOp(location, op, expr, inner); 
		}
	}

	void MulExpr(out DekiScriptExpression expr) {
		DekiScriptExpression inner = null; DekiScriptBinary.Op op = DekiScriptBinary.Op.LeftValue; Location location = Location.None; 
		Unary(out expr);
		while (la.kind == 57 || la.kind == 58 || la.kind == 59) {
			if (la.kind == 57) {
				Get();
				op = DekiScriptBinary.Op.Multiplication; 
			} else if (la.kind == 58) {
				Get();
				op = DekiScriptBinary.Op.Division; 
			} else {
				Get();
				op = DekiScriptBinary.Op.Modulo; 
			}
			location = t.Location; 
			Unary(out inner);
			expr = DekiScriptExpression.BinaryOp(location, op, expr, inner); 
		}
	}

	void Unary(out DekiScriptExpression expr) {
		Stack<Tuplet<Location, DekiScriptUnary.Op>> stack = new Stack<Tuplet<Location, DekiScriptUnary.Op>>(); 
		while (StartOf(8)) {
			if (la.kind == 56) {
				Get();
				stack.Push(new Tuplet<Location, DekiScriptUnary.Op>(t.Location, DekiScriptUnary.Op.Negate)); 
			} else if (la.kind == 55) {
				Get();
				
			} else if (la.kind == 60) {
				Get();
				stack.Push(new Tuplet<Location, DekiScriptUnary.Op>(t.Location, DekiScriptUnary.Op.LogicalNot)); 
			} else if (la.kind == 61) {
				Get();
				stack.Push(new Tuplet<Location, DekiScriptUnary.Op>(t.Location, DekiScriptUnary.Op.TypeOf)); 
			} else {
				Get();
				stack.Push(new Tuplet<Location, DekiScriptUnary.Op>(t.Location, DekiScriptUnary.Op.Length)); 
			}
		}
		Primary(out expr);
		while(stack.Count > 0) { 
		var item = stack.Pop(); 
		expr = DekiScriptExpression.UnaryOp(item.Item1, item.Item2, expr); 
		} 
		
	}

	void Primary(out DekiScriptExpression expr) {
		DekiScriptExpression inner = null; expr = null; string name = null; Location location = t.Location; 
		if (StartOf(9)) {
			Literal(out expr);
		} else if (la.kind == 22) {
			Get();
			Statements(out expr);
			Expect(23);
			expr = DekiScriptExpression.Block(location, new[] { expr }); 
		} else if (la.kind == 1) {
			Get();
			expr = DekiScriptExpression.Id(location, t.val); 
		} else SynErr(83);
		while (StartOf(10)) {
			if (la.kind == 63) {
				Get();
				location = t.Location; 
				if (StartOf(11)) {
					AnyName(out name);
					expr = DekiScriptExpression.Access(location, expr, DekiScriptExpression.Constant(name)); 
				} else if (la.kind == 22) {
					ArgList(out location, out inner);
					expr = DekiScriptExpression.Curry(location, expr, inner); 
				} else if (la.kind == 9) {
					Map(out location, out inner);
					expr = DekiScriptExpression.Curry(location, expr, inner); 
				} else SynErr(84);
			} else if (la.kind == 64) {
				Get();
				location = t.Location; 
				Expression(out inner);
				Expect(65);
				expr = DekiScriptExpression.Access(location, expr, inner); 
			} else if (la.kind == 22) {
				ArgList(out location, out inner);
				expr = DekiScriptExpression.Call(location, expr, inner); 
			} else {
				Map(out location, out inner);
				expr = DekiScriptExpression.Call(location, expr, inner); 
			}
		}
	}

	void Literal(out DekiScriptExpression expr) {
		expr = null; Location location = Location.None; 
		switch (la.kind) {
		case 51: case 66: case 67: {
			Nil(out expr);
			break;
		}
		case 68: case 69: {
			Bool(out expr);
			break;
		}
		case 5: case 6: {
			Number(out expr);
			break;
		}
		case 3: case 4: {
			String(out expr);
			break;
		}
		case 7: {
			EntityString(out expr);
			break;
		}
		case 2: {
			MagicId(out expr);
			break;
		}
		case 9: {
			Map(out location, out expr);
			break;
		}
		case 64: {
			List(out expr);
			break;
		}
		case 45: {
			Xml(out expr);
			break;
		}
		default: SynErr(85); break;
		}
	}

	void AnyName(out string name) {
		name = null; 
		switch (la.kind) {
		case 1: {
			Get();
			name = t.val; 
			break;
		}
		case 33: {
			Get();
			name = "break"; 
			break;
		}
		case 30: {
			Get();
			name = "case"; 
			break;
		}
		case 34: {
			Get();
			name = "continue"; 
			break;
		}
		case 32: {
			Get();
			name = "default"; 
			break;
		}
		case 24: {
			Get();
			name = "else"; 
			break;
		}
		case 69: {
			Get();
			name = "false"; 
			break;
		}
		case 28: {
			Get();
			name = "foreach"; 
			break;
		}
		case 21: {
			Get();
			name = "if"; 
			break;
		}
		case 52: {
			Get();
			name = "in"; 
			break;
		}
		case 49: {
			Get();
			name = "is"; 
			break;
		}
		case 11: {
			Get();
			name = "let"; 
			break;
		}
		case 51: {
			Get();
			name = "nil"; 
			break;
		}
		case 50: {
			Get();
			name = "not"; 
			break;
		}
		case 67: {
			Get();
			name = "null"; 
			break;
		}
		case 29: {
			Get();
			name = "switch"; 
			break;
		}
		case 68: {
			Get();
			name = "true"; 
			break;
		}
		case 61: {
			Get();
			name = "typeof"; 
			break;
		}
		case 19: {
			Get();
			name = "var"; 
			break;
		}
		case 70: {
			Get();
			name = "where"; 
			break;
		}
		default: SynErr(86); break;
		}
	}

	void ArgList(out Location location, out DekiScriptExpression expr) {
		List<DekiScriptExpression> list = new List<DekiScriptExpression>(); location = Location.None; 
		Expect(22);
		location = t.Location; 
		if (StartOf(2)) {
			Expression(out expr);
			list.Add(expr); 
			while (la.kind == 20) {
				Get();
				Expression(out expr);
				list.Add(expr); 
			}
		}
		Expect(23);
		expr = DekiScriptExpression.List(location, list); 
	}

	void Map(out Location location, out DekiScriptExpression expr) {
		List<DekiScriptMapConstructor.FieldConstructor> list = new List<DekiScriptMapConstructor.FieldConstructor>(); 
		DekiScriptMapConstructor.FieldConstructor field = null; 
		DekiScriptGenerator gen = null; 
		location = Location.None;
		
		Expect(9);
		location = t.Location; 
		if (StartOf(12)) {
			Field(out field);
			list.Add(field); 
			while (la.kind == 20) {
				Get();
				Field(out field);
				list.Add(field); 
			}
			if (la.kind == 28) {
				Get();
				GeneratorHead(out gen);
			}
		}
		Expect(10);
		expr = DekiScriptExpression.Map(location, gen, list.ToArray()); 
	}

	void Nil(out DekiScriptExpression expr) {
		if (la.kind == 66) {
			Get();
		} else if (la.kind == 51) {
			Get();
		} else if (la.kind == 67) {
			Get();
		} else SynErr(87);
		expr = DekiScriptNil.Value; 
	}

	void Bool(out DekiScriptExpression expr) {
		expr = null; 
		if (la.kind == 68) {
			Get();
			expr = DekiScriptBool.True; 
		} else if (la.kind == 69) {
			Get();
			expr = DekiScriptBool.False; 
		} else SynErr(88);
	}

	void Number(out DekiScriptExpression expr) {
		expr = null; 
		if (la.kind == 5) {
			Get();
			expr = DekiScriptExpression.Constant(SysUtil.ChangeType<double>(t.val)); 
		} else if (la.kind == 6) {
			Get();
			expr = DekiScriptExpression.Constant(long.Parse(t.val.Substring(2), System.Globalization.NumberStyles.AllowHexSpecifier)); 
		} else SynErr(89);
	}

	void String(out DekiScriptExpression expr) {
		string value = null; bool block; 
		AnyString(out value, out block);
		expr = DekiScriptExpression.Constant(value); 
	}

	void EntityString(out DekiScriptExpression expr) {
		string value = null; 
		Expect(7);
		expr = DekiScriptExpression.Constant(StringUtil.DecodeHtmlEntities(t.val)); 
	}

	void MagicId(out DekiScriptExpression expr) {
		Location location = t.Location; 
		Expect(2);
		expr = DekiScriptExpression.MagicId(location, t.val.Substring(1)); 
	}

	void List(out DekiScriptExpression expr) {
		List<DekiScriptExpression> list = new List<DekiScriptExpression>(); 
		Location location = Location.None;
		DekiScriptGenerator gen = null; 
		
		Expect(64);
		location = t.Location; 
		if (StartOf(2)) {
			Expression(out expr);
			list.Add(expr); 
			while (la.kind == 20) {
				Get();
				Expression(out expr);
				list.Add(expr); 
			}
			if (la.kind == 28) {
				Get();
				GeneratorHead(out gen);
			}
		}
		Expect(65);
		expr = DekiScriptExpression.List(location, gen, list.ToArray()); 
	}

	void AnyString(out string value, out bool block) {
		value = null; int start; block = false; 
		if (la.kind == 3) {
			Get();
			value = StringUtil.UnescapeString(t.val.Substring(3, t.val.Length - 6)); 
		} else if (la.kind == 4) {
			Get();
			value = StringUtil.UnescapeString(t.val.Substring(1, t.val.Length - 2)); 
		} else SynErr(90);
	}

	void Field(out DekiScriptMapConstructor.FieldConstructor field) {
		DekiScriptExpression expr = null; DekiScriptExpression key = null; string name = null; Location location = Location.None; 
		if (StartOf(11)) {
			AnyName(out name);
			key = DekiScriptExpression.Constant(name); 
		} else if (la.kind == 3 || la.kind == 4) {
			String(out key);
		} else if (la.kind == 5) {
			Get();
			key = DekiScriptExpression.Constant(t.val); 
		} else if (la.kind == 22) {
			Get();
			Expression(out key);
			Expect(23);
		} else SynErr(91);
		Expect(31);
		location = t.Location; 
		Expression(out expr);
		field = new DekiScriptMapConstructor.FieldConstructor(location, key, expr); 
	}

	void GeneratorNext(out DekiScriptGenerator gen) {
		Location location = Location.None;
		Location wherelocation = Location.None;
		DekiScriptExpression where = null; 
		List<string> names = new List<string>(); 
		DekiScriptExpression expr = null; 
		bool assign = false;
		string value = null;
		gen = null;
		
		if (la.kind == 19) {
			Get();
			location = t.Location; 
			Expect(1);
			names.Add(t.val); 
			if (la.kind == 31) {
				Get();
				Expect(1);
				value = t.val; 
				Expect(52);
				Expression(out expr);
				if (la.kind == 70) {
					Get();
					wherelocation = t.Location; 
					Expression(out where);
				}
			} else if (la.kind == 20 || la.kind == 52) {
				while (la.kind == 20) {
					Get();
					Expect(1);
					names.Add(t.val); 
				}
				Expect(52);
				Expression(out expr);
				if (la.kind == 70) {
					Get();
					wherelocation = t.Location; 
					Expression(out where);
				}
			} else if (la.kind == 12) {
				Get();
				Expression(out expr);
				assign = true; 
			} else SynErr(92);
		} else if (la.kind == 21) {
			Get();
			location = t.Location; 
			Expression(out expr);
		} else SynErr(93);
		if (la.kind == 20) {
			Get();
			GeneratorNext(out gen);
		}
		if(where != null) gen = new DekiScriptGeneratorIf(wherelocation, where, gen);
		if(names.Count == 0) gen = new DekiScriptGeneratorIf(location, expr, gen);
		else if(assign) gen = new DekiScriptGeneratorVar(location, names[0], expr, gen);
		else if(value == null) gen = new DekiScriptGeneratorForeachValue(location, names.ToArray(), expr, gen);
		else gen = new DekiScriptGeneratorForeachKeyValue(location, names[0], value, expr, gen);
		
	}

	void XmlNode(out DekiScriptExpression node) {
		var nodes = new List<DekiScriptExpression>(); 
		var attributes = new List<DekiScriptXmlElement.Attribute>(); 
		DekiScriptXmlElement.Attribute attribute = null; 
		string name = null;
		string name2 = null;
		DekiScriptExpression nameExpr = null;
		DekiScriptExpression expr = null;
		node = null; 
		Location location = Location.None;
		Location nodeslocation = Location.None;
		
		Expect(45);
		location = t.Location; 
		if (StartOf(11)) {
			AnyName(out name);
		} else if (la.kind == 22) {
			Get();
			Expression(out nameExpr);
			Expect(23);
		} else SynErr(94);
		while (StartOf(13)) {
			XmlAttribute(out attribute);
			attributes.Add(attribute); 
		}
		if (la.kind == 46) {
			Get();
			nodeslocation = t.Location; 
			if (StartOf(1)) {
				Statements(out expr);
				nodes.Add(expr); 
			}
			if (la.kind == 71) {
				Get();
				AnyName(out name2);
				if(nameExpr != null) { throw new DekiScriptParserException(string.Format("closing tag mismatch, found </{0}>, expected </>", t.val), t.Location); } 
				else if(!StringUtil.EqualsInvariant(name, name2)) { throw new DekiScriptParserException(string.Format("closing tag mismatch, found </{0}>, expected </{1}>", name2, name), t.Location); } 
				
				Expect(46);
			} else if (la.kind == 72) {
				Get();
			} else SynErr(95);
		} else if (la.kind == 73) {
			Get();
		} else SynErr(96);
		node = DekiScriptExpression.XmlElement(location, null, nameExpr ?? DekiScriptExpression.Constant(name), attributes.ToArray(), DekiScriptExpression.Block(nodeslocation, nodes)); 
	}

	void XmlAttribute(out DekiScriptXmlElement.Attribute attribute) {
		string name = null; 
		string text = null;
		bool block = false;
		DekiScriptExpression expr = null; 
		attribute = null; 
		DekiScriptExpression nameExpr = null; 
		Location location = Location.None;
		
		if (StartOf(11)) {
			AnyName(out name);
			location = t.Location; 
		} else if (la.kind == 22) {
			Get();
			Expression(out nameExpr);
			Expect(23);
			location = nameExpr.Location; 
		} else SynErr(97);
		Expect(12);
		if (la.kind == 3 || la.kind == 4) {
			AnyString(out text, out block);
			if(!block && StringUtil.StartsWithInvariant(text, "{{") && StringUtil.EndsWithInvariant(text, "}}")) {
			expr = DekiScriptParser.Parse(new Location(t.origin, t.line, t.col + 3), text.Substring(2, text.Length - 4)); 
			} else {
				expr = DekiScriptExpression.Constant(text); 
			}
			
		} else if (la.kind == 22) {
			Get();
			Statements(out expr);
			Expect(23);
		} else SynErr(98);
		attribute = new DekiScriptXmlElement.Attribute(location, null, nameExpr ?? DekiScriptExpression.Constant(name), expr); 
	}



	public void Parse() {
		la = new Token();
		la.val = "";		
		Get();
		DekiScript();

    Expect(0);
	}
	
	static readonly bool[,] set = {
		{T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
		{x,T,T,T, T,T,T,T, T,T,x,T, x,x,x,x, x,x,x,T, x,T,T,x, x,T,x,x, T,T,x,x, x,T,T,T, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,T, x,x,x,T, T,x,x,x, T,T,T,x, T,x,T,T, T,T,x,x, x,x,x,x},
		{x,T,T,T, T,T,T,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,T, x,x,x,T, T,x,x,x, T,T,T,x, T,x,T,T, T,T,x,x, x,x,x,x},
		{x,T,T,T, T,T,T,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,T, x,x,x,T, T,x,x,x, T,T,T,x, T,x,T,T, T,T,x,x, x,x,x,x},
		{T,x,x,x, x,x,x,x, T,x,T,x, x,x,x,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,T,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,T, T,T,T,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x},
		{x,x,T,T, T,T,T,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, T,x,T,T, T,T,x,x, x,x,x,x},
		{x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x},
		{x,T,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,T, x,T,x,x, T,x,x,x, T,T,T,x, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,T, T,x,x,x, x,x,x,x, x,T,x,x, x,x,x,T, T,T,T,x, x,x,x,x},
		{x,T,x,T, T,T,x,x, x,x,x,T, x,x,x,x, x,x,x,T, x,T,T,x, T,x,x,x, T,T,T,x, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,T, T,x,x,x, x,x,x,x, x,T,x,x, x,x,x,T, T,T,T,x, x,x,x,x},
		{x,T,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,T, x,T,T,x, T,x,x,x, T,T,T,x, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,T, T,x,x,x, x,x,x,x, x,T,x,x, x,x,x,T, T,T,T,x, x,x,x,x}

	};
} // end Parser


internal class Errors {
	public void SynErr (string origin, int line, int col, int n) {
		string s;
		switch (n) {
			case 0: s = "EOF expected"; break;
			case 1: s = "name expected"; break;
			case 2: s = "magicid expected"; break;
			case 3: s = "rawstring expected"; break;
			case 4: s = "quotedstring expected"; break;
			case 5: s = "number expected"; break;
			case 6: s = "hexnumber expected"; break;
			case 7: s = "htmlentity expected"; break;
			case 8: s = "\";\" expected"; break;
			case 9: s = "\"{\" expected"; break;
			case 10: s = "\"}\" expected"; break;
			case 11: s = "\"let\" expected"; break;
			case 12: s = "\"=\" expected"; break;
			case 13: s = "\"+=\" expected"; break;
			case 14: s = "\"-=\" expected"; break;
			case 15: s = "\"*=\" expected"; break;
			case 16: s = "\"/=\" expected"; break;
			case 17: s = "\"%=\" expected"; break;
			case 18: s = "\"..=\" expected"; break;
			case 19: s = "\"var\" expected"; break;
			case 20: s = "\",\" expected"; break;
			case 21: s = "\"if\" expected"; break;
			case 22: s = "\"(\" expected"; break;
			case 23: s = "\")\" expected"; break;
			case 24: s = "\"else\" expected"; break;
			case 25: s = "\"try\" expected"; break;
			case 26: s = "\"catch\" expected"; break;
			case 27: s = "\"finally\" expected"; break;
			case 28: s = "\"foreach\" expected"; break;
			case 29: s = "\"switch\" expected"; break;
			case 30: s = "\"case\" expected"; break;
			case 31: s = "\":\" expected"; break;
			case 32: s = "\"default\" expected"; break;
			case 33: s = "\"break\" expected"; break;
			case 34: s = "\"continue\" expected"; break;
			case 35: s = "\"return\" expected"; break;
			case 36: s = "\"!!\" expected"; break;
			case 37: s = "\"?\" expected"; break;
			case 38: s = "\"??\" expected"; break;
			case 39: s = "\"||\" expected"; break;
			case 40: s = "\"&&\" expected"; break;
			case 41: s = "\"!=\" expected"; break;
			case 42: s = "\"==\" expected"; break;
			case 43: s = "\"!==\" expected"; break;
			case 44: s = "\"===\" expected"; break;
			case 45: s = "\"<\" expected"; break;
			case 46: s = "\">\" expected"; break;
			case 47: s = "\"<=\" expected"; break;
			case 48: s = "\">=\" expected"; break;
			case 49: s = "\"is\" expected"; break;
			case 50: s = "\"not\" expected"; break;
			case 51: s = "\"nil\" expected"; break;
			case 52: s = "\"in\" expected"; break;
			case 53: s = "\"..\" expected"; break;
			case 54: s = "\"&\" expected"; break;
			case 55: s = "\"+\" expected"; break;
			case 56: s = "\"-\" expected"; break;
			case 57: s = "\"*\" expected"; break;
			case 58: s = "\"/\" expected"; break;
			case 59: s = "\"%\" expected"; break;
			case 60: s = "\"!\" expected"; break;
			case 61: s = "\"typeof\" expected"; break;
			case 62: s = "\"#\" expected"; break;
			case 63: s = "\".\" expected"; break;
			case 64: s = "\"[\" expected"; break;
			case 65: s = "\"]\" expected"; break;
			case 66: s = "\"_\" expected"; break;
			case 67: s = "\"null\" expected"; break;
			case 68: s = "\"true\" expected"; break;
			case 69: s = "\"false\" expected"; break;
			case 70: s = "\"where\" expected"; break;
			case 71: s = "\"</\" expected"; break;
			case 72: s = "\"</>\" expected"; break;
			case 73: s = "\"/>\" expected"; break;
			case 74: s = "??? expected"; break;
			case 75: s = "invalid Statements"; break;
			case 76: s = "invalid AssignStatement"; break;
			case 77: s = "invalid FlowControlStatement"; break;
			case 78: s = "invalid BlockStatement"; break;
			case 79: s = "invalid DefineConstruct"; break;
			case 80: s = "invalid GeneratorHead"; break;
			case 81: s = "invalid CaseStatement"; break;
			case 82: s = "invalid RelExpr"; break;
			case 83: s = "invalid Primary"; break;
			case 84: s = "invalid Primary"; break;
			case 85: s = "invalid Literal"; break;
			case 86: s = "invalid AnyName"; break;
			case 87: s = "invalid Nil"; break;
			case 88: s = "invalid Bool"; break;
			case 89: s = "invalid Number"; break;
			case 90: s = "invalid AnyString"; break;
			case 91: s = "invalid Field"; break;
			case 92: s = "invalid GeneratorNext"; break;
			case 93: s = "invalid GeneratorNext"; break;
			case 94: s = "invalid XmlNode"; break;
			case 95: s = "invalid XmlNode"; break;
			case 96: s = "invalid XmlNode"; break;
			case 97: s = "invalid XmlAttribute"; break;
			case 98: s = "invalid XmlAttribute"; break;

			default: s = "error " + n; break;
		}
		throw new DekiScriptParserException(s, new Location(origin, line, col));
	}

	public void SemErr (string origin, int line, int col, string s) {
		throw new DekiScriptParserException(s, new Location(origin, line, col));
	}
	
	public void SemErr (string s) {
		throw new DekiScriptParserException(s, Location.None);
	}
	
	public void Warning (int line, int col, string s) {
		// ignore warnings
	}
	
	public void Warning(string s) {
		// ignore warnings
	}
} // Errors


internal class FatalError : Exception {
	public FatalError(string m) : base(m) {}
}

}