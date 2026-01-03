
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Xml;
using System.Reflection;
using Majorsilence.Reporting.RdlEngine.Resources;
using Majorsilence.Reporting.Rdl;
using System.Threading.Tasks;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// <p>Language parser.   Recursive descent parser.  Precedence of operators
	/// is handled by starting with lowest precedence and calling down recursively
	/// to the highest.</p>
	/// AND/OR
	/// NOT
	/// relational operators, eq, ne, lt, lte, gt, gte
	/// +, -
	/// *, /, %
	/// ^ (exponentiation)
	/// unary +, -
	/// parenthesis (...)
	/// <p>
	/// In BNF the language grammar is:</p>
	///	<code>
	/// Expr: Term ExprRhs
	/// ExprRhs: PlusMinusOperator Term ExprRhs
	/// Term: Factor TermRhs
	/// TermRhs: MultDivOperator Factor TermRhs
	/// Factor: ( Expr ) | BaseType | - BaseType | - ( Expr )
	/// BaseType: FuncIdent | NUMBER | QUOTE   
	/// FuncIDent: IDENTIFIER ( [Expr] [, Expr]*) | IDENTIFIER
	/// PlusMinusOperator: + | -
	/// MultDivOperator: * | /
	///	</code>
	///	
	/// </summary>
	internal class Parser
	{
		static internal long Counter;			// counter used for unique expression count
		private TokenList tokens;
		private Stack operandStack = new Stack();
		private Stack operatorStack = new Stack();
		private Token curToken=null;
		private NameLookup idLookup=null;
        private List<ICacheData> _DataCache;
		private bool _InAggregate;
		private DataSetDefn inAggregateDataSet = null;
		private bool _NoAggregate=false;

		/// <summary>
		/// Parse an expression.
		/// </summary>
        internal Parser(List<ICacheData> c) 
		{
			_DataCache = c;
		}

		/// <summary>
		/// Returns a parsed Expression
		/// </summary>
		/// <param name="lu">The NameLookUp class used to resolve names.</param>
		/// <param name="expr">The expression to be parsed.</param>
		/// <returns>An expression that can be run after validation and binding.</returns>
		internal async Task<IExpr> Parse(NameLookup lu, string expr)
		{
			_InAggregate = false;

			if (expr.Substring(0,1) != "=")		// if 1st char not '='
				return new Constant(expr);		//   this is a constant value

			idLookup = lu;	
			IExpr e = await this.ParseExpr(new StringReader(expr));
			
			if (e == null)					// Didn't get an expression?
				e = new Constant(expr);		//  then provide a constant

			return e;
		}

		internal bool NoAggregateFunctions
		{	// marked true when in an aggregate function 
			get {return _NoAggregate;}
			set {_NoAggregate = value;}
		}

		private static string GetLocationInfo(Token token)
		{
			return string.Format(Strings.Parser_ErrorP_AtColumn, token.StartCol);
		}

		private static string GetLocationInfoWithValue(Token token)
		{
			return string.Format(Strings.Parser_ErrorP_FoundValue, token.Value) + GetLocationInfo(token);
		}

		/// <summary>
		/// Returns a parsed DPL instance.
		/// </summary>
		/// <param name="reader">The TextReader value to be parsed.</param>
		/// <returns>A parsed Program instance.</returns>
		private async Task<IExpr> ParseExpr(TextReader reader)
		{
			IExpr result=null;
			Lexer lexer = new Lexer(reader);
			tokens = lexer.Lex();

			if (tokens.Peek().Type == TokenTypes.EQUAL)
			{
				tokens.Extract();		// skip over the equal
				curToken = tokens.Extract();	// set up the first token
				result = await MatchExprAndOr();	// start with lowest precedence and work up
			}

			if (curToken.Type != TokenTypes.EOF)
				throw new ParserException(Strings.Parser_ErrorP_EndExpressionExpected + GetLocationInfo(curToken));

			return result;
		}

		// ExprAndOr: 
		private async Task<IExpr> MatchExprAndOr()
		{
			TokenTypes t;			// remember the type

			IExpr lhs;
			lhs = await MatchExprNot();
			IExpr result = lhs;			// in case we get no matches
			while ((t = curToken.Type) == TokenTypes.AND || t == TokenTypes.OR)
			{
				curToken = tokens.Extract();
				IExpr rhs;
				rhs = await MatchExprNot();
				bool bBool = (rhs.GetTypeCode() == TypeCode.Boolean &&
					lhs.GetTypeCode() == TypeCode.Boolean);
				if (!bBool)
					throw new ParserException(Strings.Parser_ErrorP_AND_OR_RequiresBoolean + GetLocationInfo(curToken));

				switch(t)
				{
					case TokenTypes.AND:
						result = new FunctionAnd(lhs, rhs);
						break;
					case TokenTypes.OR:
						result = new FunctionOr(lhs, rhs);
						break;
				}
				lhs = result;		// in case we have more AND/OR s
			}

			return result;
		}
		
		private async Task<IExpr> MatchExprNot()
		{
			TokenTypes t;			// remember the type
			t = curToken.Type;
			if (t == TokenTypes.NOT)
			{
				curToken = tokens.Extract();
			}
			IExpr result = await MatchExprRelop();
			if (t == TokenTypes.NOT)
			{
				if (result.GetTypeCode() != TypeCode.Boolean)
					throw new ParserException(Strings.Parser_ErrorP_NOTRequiresBoolean + GetLocationInfo(curToken));
				result = new FunctionNot(result);
			}
			return result;
		}

		// ExprRelop: 
		private async Task<IExpr> MatchExprRelop()
		{
			TokenTypes t;           // remember the type
			IExpr result = null;
			IExpr lhs;
			lhs = await MatchExprAddSub();
			result = lhs;			// in case we get no matches
			while ((t = curToken.Type) == TokenTypes.EQUAL ||
				t == TokenTypes.NOTEQUAL ||
				t == TokenTypes.GREATERTHAN ||
				t == TokenTypes.GREATERTHANOREQUAL ||
				t == TokenTypes.LESSTHAN ||
				t == TokenTypes.LESSTHANOREQUAL)
			{
				curToken = tokens.Extract();
				IExpr rhs;
				rhs = await MatchExprAddSub();

				switch(t)
				{
					case TokenTypes.EQUAL:
						result = new FunctionRelopEQ(lhs, rhs);
						break;
					case TokenTypes.NOTEQUAL:
						result = new FunctionRelopNE(lhs, rhs);
						break;
					case TokenTypes.GREATERTHAN:
						result = new FunctionRelopGT(lhs, rhs);
						break;
					case TokenTypes.GREATERTHANOREQUAL:
						result = new FunctionRelopGTE(lhs, rhs);
						break;
					case TokenTypes.LESSTHAN:
						result = new FunctionRelopLT(lhs, rhs);
						break;
					case TokenTypes.LESSTHANOREQUAL:
						result = new FunctionRelopLTE(lhs, rhs);
						break;
				}
				lhs = result;		// in case we continue the loop
			}
			return result;
		}

		// ExprAddSub: PlusMinusOperator Term ExprRhs
		private async Task<IExpr> MatchExprAddSub()
		{
			TokenTypes t;           // remember the type
			IExpr result=null;
			IExpr lhs;
			lhs = await MatchExprMultDiv();
			result = lhs;			// in case we get no matches
			while ((t = curToken.Type) == TokenTypes.PLUS || t == TokenTypes.PLUSSTRING || t == TokenTypes.MINUS)
			{
				curToken = tokens.Extract();
				IExpr rhs;
				rhs = await MatchExprMultDiv();
				TypeCode lt = lhs.GetTypeCode();
				TypeCode rt = rhs.GetTypeCode();
				bool bDecimal = (rt == TypeCode.Decimal &&
					lt == TypeCode.Decimal);
                bool bInt32 = (rt == TypeCode.Int32 &&
                    lt == TypeCode.Int32);
				bool bString = (rt == TypeCode.String ||
					lt == TypeCode.String);

				switch(t)
				{
					case TokenTypes.PLUSSTRING:
						result = new FunctionPlusString(lhs, rhs);
						break;
					case TokenTypes.PLUS:
                        if (bDecimal)
                            result = new FunctionPlusDecimal(lhs, rhs);
                        else if (bString)
                            result = new FunctionPlusString(lhs, rhs);
                        else if (bInt32)
                            result = new FunctionPlusInt32(lhs, rhs);
                        else
                            result = new FunctionPlus(lhs, rhs);
						break;
					case TokenTypes.MINUS:
						if (bDecimal)
							result = new FunctionMinusDecimal(lhs, rhs);
						else if (bString)
							throw new ParserException(Strings.Parser_ErrorP_MinusNeedNumbers + GetLocationInfo(curToken));
                        else if (bInt32)
                            result = new FunctionMinusInt32(lhs, rhs);
                        else
							result = new FunctionMinus(lhs, rhs);
						break;
				}
				lhs = result;		// in case continue in the loop
			}
			return result;
		}

		// TermRhs: MultDivOperator Factor TermRhs
		private async Task<IExpr> MatchExprMultDiv()
		{
			IExpr result = null;
			TokenTypes t;			// remember the type
			IExpr lhs;
			lhs = await MatchExprExp();
			result = lhs;			// in case we get no matches
			while ((t = curToken.Type) == TokenTypes.FORWARDSLASH ||
				t == TokenTypes.STAR ||
				t == TokenTypes.MODULUS)
			{
				curToken = tokens.Extract();
				IExpr rhs;
				rhs = await MatchExprExp();
				bool bDecimal = (rhs.GetTypeCode() == TypeCode.Decimal &&
					lhs.GetTypeCode() == TypeCode.Decimal);
				switch (t)
				{
					case TokenTypes.FORWARDSLASH:
						if (bDecimal)
							result = new FunctionDivDecimal(lhs, rhs);	
						else
							result = new FunctionDiv(lhs, rhs);	
						break;
					case TokenTypes.STAR:
						if (bDecimal)
							result = new FunctionMultDecimal(lhs, rhs);
						else
							result = new FunctionMult(lhs, rhs);
						break;
					case TokenTypes.MODULUS:
						result = new FunctionModulus(lhs, rhs);
						break;
				}
				lhs = result;		// in case continue in the loop
			}

			return result;
        }

		// TermRhs: ExpOperator Factor TermRhs
		private async Task<IExpr> MatchExprExp()
		{
            IExpr result = null;
            IExpr lhs;
			lhs = await MatchExprUnary();
			if (curToken.Type == TokenTypes.EXP)
			{
				curToken = tokens.Extract();
				IExpr rhs;
				rhs = await MatchExprUnary();
				result = new FunctionExp(lhs, rhs);	
			}
			else
				result = lhs;

			return result;
		}
		
		private async Task<IExpr> MatchExprUnary()
		{
			IExpr result = null;
			TokenTypes t;			// remember the type
			t = curToken.Type;
			if (t == TokenTypes.PLUS || t == TokenTypes.MINUS)
			{
				curToken = tokens.Extract();
			}
            result = await MatchExprParen();
			if (t == TokenTypes.MINUS)
			{
				if (result.GetTypeCode() == TypeCode.Decimal)
					result = new FunctionUnaryMinusDecimal(result);
				else if (result.GetTypeCode() == TypeCode.Int32)
					result = new FunctionUnaryMinusInteger(result);
				else
					result = new FunctionUnaryMinus(result);
			}

			return result;
		}
		
		// Factor: ( Expr ) | BaseType | - BaseType | - ( Expr )
		private async Task<IExpr> MatchExprParen()
		{
			IExpr result = null;
            // Match- ( Expr )
            if (curToken.Type == TokenTypes.LPAREN)
			{	// trying to match ( Expr )
				curToken = tokens.Extract();
				result = await MatchExprAndOr();
				if (curToken.Type != TokenTypes.RPAREN)
					throw new ParserException(Strings.Parser_ErrorP_BracketExpected + GetLocationInfoWithValue(curToken));
				curToken = tokens.Extract();
			}
			else
				result = await MatchBaseType();

			return result;
		}

		// BaseType: FuncIdent | NUMBER | QUOTE   - note certain types are restricted in expressions
		private async Task<IExpr> MatchBaseType()
		{
			var r = await MatchFuncIDent();

            if (r.match)
				return r.result;

			switch (curToken.Type)
			{
				case TokenTypes.NUMBER:
					r.result = new ConstantDecimal(curToken.Value);
					break;
				case TokenTypes.DATETIME:
					r.result = new ConstantDateTime(curToken.Value);
					break;
				case TokenTypes.DOUBLE:
					r.result = new ConstantDouble(curToken.Value);
					break;
				case TokenTypes.INTEGER:
					r.result = new ConstantInteger(curToken.Value);
					break;
				case TokenTypes.QUOTE:
					r.result = new ConstantString(curToken.Value);
					break;
				default:
					throw new ParserException(Strings.Parser_ErrorP_IdentifierExpected + GetLocationInfoWithValue(curToken));
			}
			curToken = tokens.Extract();

			return r.result;
		}

		// FuncIDent: IDENTIFIER ( [Expr] [, Expr]*) | IDENTIFIER
		private async Task<(bool match, IExpr result)> MatchFuncIDent()
		{
			IExpr e;
			string fullname;			// will hold the full name
			string method;				// will hold method name or second part of name
			string firstPart;			// will hold the collection name
			string thirdPart;			// will hold third part of name
			bool bOnePart;				// simple name: no ! or . in name

			IExpr result = null;

			if (curToken.Type != TokenTypes.IDENTIFIER)
				return (false, result);

			// Disentangle method calls from collection references
			method = fullname = curToken.Value;
			curToken = tokens.Extract();

			// Break the name into parts
			char[] breakChars = new char[] {'!', '.'};

			int posBreak = method.IndexOfAny(breakChars);
			if (posBreak > 0)
			{
				bOnePart = false;
				firstPart = method.Substring(0, posBreak);
				method = method.Substring(posBreak+1);		// rest of expression
			}
			else
			{
				bOnePart = true;
				firstPart = method;
			}

			posBreak = method.IndexOf('.');
            if (posBreak > 0)
            {
                thirdPart = method.Substring(posBreak + 1);	// rest of expression
                method = method.Substring(0, posBreak);
            }
            else
            {
                thirdPart = null;
            }

			if (curToken.Type != TokenTypes.LPAREN) switch (firstPart.ToLowerInvariant())
			{
				case "fields":
					Field f = null;

					if (inAggregateDataSet != null)
					{
						f = inAggregateDataSet.Fields == null ? null : inAggregateDataSet.Fields[method];
						if (f == null)
							throw new ParserException(string.Format(Strings.Parser_ErrorP_FieldNotInDataSet, method, inAggregateDataSet.Name.Nm));
					}
					else
					{
						f = idLookup.LookupField(method);
						if (f == null)
						{
							throw new ParserException(string.Format(Strings.Parser_ErrorP_FieldNotFound, method));
						}
					}

					if (thirdPart == null || thirdPart == "Value")
					{
						result = new FunctionField(f);	
					}
					else if (thirdPart == "IsMissing")
					{
						result = new FunctionFieldIsMissing(f);
					}
					else
						throw new ParserException(string.Format(Strings.Parser_ErrorP_FieldSupportsValueAndIsMissing, method));
					return (true, result);
                case "parameters":  // see ResolveParametersMethod for resolution of MultiValue parameter function reference
					ReportParameter p = idLookup.LookupParameter(method);
					if (p == null)
						throw new ParserException(string.Format(Strings.Parser_ErrorP_ParameterNotFound, method));
                    int ci = thirdPart == null? -1: thirdPart.IndexOf(".Count");
                    if (ci > 0)
                        thirdPart = thirdPart.Substring(0, ci);
                    FunctionReportParameter r;                    
					if (thirdPart == null || thirdPart == "Value")
						r = new FunctionReportParameter(p);
					else if (thirdPart == "Label")
						r = new FunctionReportParameterLabel(p);
					else
						throw new ParserException(string.Format(Strings.Parser_ErrorP_ParameterSupportsValueAndLabel, method));
                    if (ci > 0)
                        r.SetParameterMethod("Count", null);
                    
                    result = r;
                    return (true, result);
				case "reportitems":
					Textbox t = idLookup.LookupReportItem(method);
					if (t == null)
						throw new ParserException(string.Format(Strings.Parser_ErrorP_ItemNotFound, method));
					if (thirdPart != null && thirdPart != "Value")
						throw new ParserException(string.Format(Strings.Parser_ErrorP_ItemSupportsValue, method));
					result = new FunctionTextbox(t, idLookup.ExpressionName);	
					return (true, result);
				case "globals":
					e = idLookup.LookupGlobal(method);
					if (e == null)
						throw new ParserException(string.Format(Strings.Parser_ErrorP_GlobalsNotFound, method));
					result = e;
					return (true, result);
				case "user":
					e = idLookup.LookupUser(method);
					if (e == null)
						throw new ParserException(string.Format(Strings.Parser_ErrorP_UserVarNotFound, method));
					result = e;
					return (true, result);
				case "recursive":	// Only valid for some aggregate functions
					result = new IdentifierKey(IdentifierKeyEnum.Recursive);
					return (true, result);
				case "simple":		// Only valid for some aggregate functions
					result = new IdentifierKey(IdentifierKeyEnum.Simple);
					return (true, result);
				default:
					if (!bOnePart)
						throw new ParserException(string.Format(Strings.Parser_ErrorP_UnknownIdentifer, fullname));

					switch (method.ToLower())		// lexer should probably mark these
					{
						case "true":
						case "false":
							result = new ConstantBoolean(method.ToLower());
							break;
						default:
							// usually this is enum that will be used in an aggregate 
							result = new Identifier(method);
							break;
					}
					return (true, result);
			}

			// We've got an function reference
			curToken = tokens.Extract();		// get rid of '('

			// Got a function now obtain the arguments
			int argCount=0;
			
			bool isAggregate = IsAggregate(method, bOnePart);
			if (_NoAggregate && isAggregate)
				throw new ParserException(string.Format(Strings.Parser_ErrorP_AggregateCannotUsedWithinGrouping, method));
			if (_InAggregate && isAggregate)
				throw new ParserException(string.Format(Strings.Parser_ErrorP_AggregateCannotNestedInAnotherAggregate, method));
			_InAggregate = isAggregate;
			if (_InAggregate)
			{
				int level = 0;
				bool nextScope = false;
				Token scopeToken = null;
				// For RunningValue, the scope is the third parameter (after expression and aggregate function)
				// So we need to skip the first comma and look for the second comma
				bool isRunningValue = method.ToLower() == "runningvalue";
				int commaCount = 0;
				
				foreach(Token tok in tokens)
				{
					if(nextScope)
					{
						scopeToken = tok;
						break;
					}
					
					if(level == 0 && tok.Type == TokenTypes.COMMA)
					{
						commaCount++;
						// For RunningValue, skip the first comma (between expression and aggregate function)
						// Only set nextScope when we reach the second comma (between aggregate function and scope)
						if (isRunningValue && commaCount == 1)
						{
							continue;
						}
						nextScope = true;
						continue;
					}
					if (tok.Type == TokenTypes.RPAREN)
						level--;
					if (tok.Type == TokenTypes.LPAREN)
						level++;
				}

				// For RunningValue, skip the DataSet scope validation as the scope parameter
				// typically refers to a group name, not a DataSet. The proper scope validation
				// happens later in ResolveAggrScope.
				if (scopeToken != null && !isRunningValue)
				{
					if (scopeToken.Type != TokenTypes.QUOTE)
						throw new ParserException(string.Format(Strings.Parser_ErrorP_ScopeMustConstant, scopeToken.Value));
					inAggregateDataSet = this.idLookup.ScopeDataSet(scopeToken.Value);
					if (inAggregateDataSet == null)
						throw new ParserException(string.Format(Strings.Parser_ErrorP_ScopeNotKnownDataSet, scopeToken.Value));
				}
			}

            List<IExpr> largs = new List<IExpr>();
			while(true)
			{
				if (curToken.Type == TokenTypes.RPAREN)
				{	// We've got our function
					curToken = tokens.Extract();
					break;
				}
				if (argCount == 0)
				{
					// don't need to do anything
				}
				else if (curToken.Type == TokenTypes.COMMA)
				{
					curToken = tokens.Extract();
				}
				else
					throw new ParserException(Strings.Parser_ErrorP_Invalid_function_arguments + GetLocationInfoWithValue(curToken));
				
				e = await MatchExprAndOr();
				if (e == null)
					throw new ParserException(Strings.Parser_ErrorP_ExpectingComma + GetLocationInfoWithValue(curToken));

				largs.Add(e);
				argCount++;
			}
			if (_InAggregate)
			{
				inAggregateDataSet = null;
				_InAggregate = false;
			}

            IExpr[] args = largs.ToArray();

			object scope;
			bool bSimple;
			if (!bOnePart)				
            {
                result = (string.Equals(firstPart, "Parameters", StringComparison.InvariantCultureIgnoreCase))?
                    ResolveParametersMethod(method, thirdPart, args):
                    ResolveMethodCall(fullname, args);	// throw exception when fails
            }
			else switch(method.ToLower())
			{
				case "iif":
					if (args.Length != 3)
						throw new ParserException(Strings.Parser_ErrorP_iff_function_requires_3_arguments + GetLocationInfo(curToken));
//  We allow any type for the first argument; it will get converted to boolean at runtime
//					if (args[0].GetTypeCode() != TypeCode.Boolean)
//						throw new ParserException("First argument to iif function must be boolean." + GetLocationInfo(curToken));
					result = new FunctionIif(args[0], args[1], args[2]);
					break;
				case "choose":
					if (args.Length <= 2)
						throw new ParserException(Strings.Parser_ErrorP_ChooseRequires2Arguments + GetLocationInfo(curToken));
					switch (args[0].GetTypeCode())
					{
						case TypeCode.Double:
						case TypeCode.Single:
						case TypeCode.Int32:
						case TypeCode.Decimal:
						case TypeCode.Int16:
						case TypeCode.Int64:
                        case TypeCode.UInt16:
                        case TypeCode.UInt32:
                        case TypeCode.UInt64:
							break;
						default:
							throw new ParserException(Strings.Parser_ErrorP_ChooseFirstArgumentMustNumeric + GetLocationInfo(curToken));
					}
					result = new FunctionChoose(args);
					break;
				case "switch":
					if (args.Length <= 2)
						throw new ParserException(Strings.Parser_ErrorP_SwitchRequires2Arguments + GetLocationInfo(curToken));
				    if (args.Length % 2 != 0)
						throw new ParserException(Strings.Parser_ErrorP_SwitchMustEvenArguments + GetLocationInfo(curToken));
					for (int i=0; i < args.Length; i = i+2)
					{
						if (args[i].GetTypeCode() != TypeCode.Boolean)
							throw new ParserException(Strings.Parser_ErrorP_SwitchMustBoolean + GetLocationInfo(curToken));
					}
					result = new FunctionSwitch(args);
					break;
				case "format":
					if (args.Length > 2 || args.Length < 1)
						throw new ParserException(Strings.Parser_ErrorP_FormatRequires2Arguments + GetLocationInfo(curToken));
					if (args.Length == 1)
					{
						result = new FunctionFormat(args[0], new ConstantString(""));
					}
					else
					{
						if (args[1].GetTypeCode() != TypeCode.String)
							throw new ParserException(Strings.Parser_ErrorP_SecondMustString + GetLocationInfo(curToken));
						result = new FunctionFormat(args[0], args[1]);
					}
					break;

				case "fields":
					if (args.Length != 1)
						throw new ParserException(Strings.Parser_ErrorP_FieldsRequires1Argument + GetLocationInfo(curToken));
					result = new FunctionFieldCollection(idLookup.Fields, args[0]);
                    if (curToken.Type == TokenTypes.DOT)
                    {	// user placed "."                  TODO: generalize this code
                        curToken = tokens.Extract();                // skip past dot operator
                        if (curToken.Type == TokenTypes.IDENTIFIER && curToken.Value.ToLowerInvariant() == "value")
                            curToken = tokens.Extract();            // only support "value" property for now
                        else
							throw new ParserException(string.Format(Strings.Parser_ErrorP_UnknownProperty, curToken.Value, "Fields") + GetLocationInfo(curToken));
                    }
                    break;
				case "parameters":
					if (args.Length != 1)
						throw new ParserException(Strings.Parser_ErrorP_ParametersRequires1Argument + GetLocationInfo(curToken));
					result = new FunctionParameterCollection(idLookup.Parameters, args[0]);
                    if (curToken.Type == TokenTypes.DOT)
                    {	// user placed "." 
                        curToken = tokens.Extract();                // skip past dot operator
                        if (curToken.Type == TokenTypes.IDENTIFIER && curToken.Value.ToLowerInvariant() == "value")
                            curToken = tokens.Extract();            // only support "value" property for now
                        else
                            throw new ParserException((string.Format(Strings.Parser_ErrorP_UnknownProperty, curToken.Value, "Parameters") + GetLocationInfo(curToken)));
                    }
                    break;
				case "reportitems":
					if (args.Length != 1)
						throw new ParserException(Strings.Parser_ErrorP_ReportItemsRequires1Argument + GetLocationInfo(curToken));
					result = new FunctionReportItemCollection(idLookup.ReportItems, args[0]);
                    if (curToken.Type == TokenTypes.DOT)
                    {	// user placed "." 
                        curToken = tokens.Extract();                // skip past dot operator
                        if (curToken.Type == TokenTypes.IDENTIFIER && curToken.Value.ToLowerInvariant() == "value")
                            curToken = tokens.Extract();            // only support "value" property for now
                        else
                            throw new ParserException((string.Format(Strings.Parser_ErrorP_UnknownProperty, curToken.Value, "ReportItems") + GetLocationInfo(curToken)));
                    }
                    break;
				case "globals":
					if (args.Length != 1)
						throw new ParserException(Strings.Parser_ErrorP_GlobalsRequires1Argument + GetLocationInfo(curToken));
					result = new FunctionGlobalCollection(idLookup.Globals, args[0]);
					break;
				case "user":
					if (args.Length != 1)
						throw new ParserException(Strings.Parser_ErrorP_UserRequires1Argument + GetLocationInfo(curToken));
					result = new FunctionUserCollection(idLookup.User, args[0]);
					break;
				case "sum":
					(scope, bSimple) = await ResolveAggrScope(args, 2);
					FunctionAggrSum aggrFS = new FunctionAggrSum(_DataCache, args[0], scope);
					aggrFS.LevelCheck = bSimple;
					result = aggrFS;
					break;
				case "avg":
					(scope, bSimple) = await ResolveAggrScope(args, 2);
					FunctionAggrAvg aggrFA = new FunctionAggrAvg(_DataCache, args[0], scope);
					aggrFA.LevelCheck = bSimple;
					result = aggrFA;
					break;
				case "min":
					(scope, bSimple) = await ResolveAggrScope(args, 2);
					FunctionAggrMin aggrFMin = new FunctionAggrMin(_DataCache, args[0], scope);
					aggrFMin.LevelCheck = bSimple;
					result = aggrFMin;
					break;
				case "max":
					(scope, bSimple) = await ResolveAggrScope(args, 2);
					FunctionAggrMax aggrFMax = new FunctionAggrMax(_DataCache, args[0], scope);
					aggrFMax.LevelCheck = bSimple;
					result = aggrFMax;
					break;
				case "first":
					(scope, bSimple) = await ResolveAggrScope(args, 2);
					result = new FunctionAggrFirst(_DataCache, args[0], scope);
					break;
				case "last":
					(scope, bSimple) = await ResolveAggrScope(args, 2);
					result = new FunctionAggrLast(_DataCache, args[0], scope);
					break;
				case "next":
					(scope, bSimple) = await ResolveAggrScope(args, 2);
					result = new FunctionAggrNext(_DataCache, args[0], scope);
					break;
				case "previous":
				    (scope, bSimple) = await ResolveAggrScope(args, 2);
					result = new FunctionAggrPrevious(_DataCache, args[0], scope);
					break;
				case "level":
					(scope, bSimple) = await ResolveAggrScope(args, 1);
					result = new FunctionAggrLevel(scope);
					break;
                case "aggregate":
                    (scope, bSimple) = await ResolveAggrScope(args, 2);
                    FunctionAggrArray aggr = new FunctionAggrArray(_DataCache, args[0], scope);
                    aggr.LevelCheck = bSimple;
                    result = aggr;
                    break;
                case "count":
					(scope, bSimple) = await ResolveAggrScope(args, 2);
					FunctionAggrCount aggrFC = new FunctionAggrCount(_DataCache, args[0], scope);
					aggrFC.LevelCheck = bSimple;
					result = aggrFC;
					break;
				case "countrows":
					(scope, bSimple) = await ResolveAggrScope(args, 1);
					FunctionAggrCountRows aggrFCR = new FunctionAggrCountRows(scope);
					aggrFCR.LevelCheck = bSimple;
					result = aggrFCR;
					break;
                case "countdistinct":
					(scope, bSimple) = await ResolveAggrScope(args, 2);
					FunctionAggrCountDistinct aggrFCD = new FunctionAggrCountDistinct(_DataCache, args[0], scope);
					aggrFCD.LevelCheck = bSimple;
					result = aggrFCD;
					break;
				case "rownumber":
					(scope, bSimple) = await ResolveAggrScope(args, 1);
					IExpr texpr = new ConstantDouble("0");
					result = new FunctionAggrRvCount(_DataCache, texpr, scope);
					break;
				case "runningvalue":
					if (args.Length < 2 || args.Length > 3)
						throw new ParserException(Strings.Parser_ErrorP_RunningValue_takes_2_or_3_arguments + GetLocationInfo(curToken));
					string aggrFunc = await args[1].EvaluateString(null, null);
					if (aggrFunc == null)
						throw new ParserException(Strings.Parser_ErrorP_RunningValueArgumentInvalid + GetLocationInfo(curToken));
					(scope, bSimple) = await ResolveAggrScope(args, 3);
					switch(aggrFunc.ToLower())
					{
						case "sum":
							result = new FunctionAggrRvSum(_DataCache, args[0], scope);
							break;
						case "avg":
							result = new FunctionAggrRvAvg(_DataCache, args[0], scope);
							break;
						case "count":
							result = new FunctionAggrRvCount(_DataCache, args[0], scope);
							break;
						case "max":
							result = new FunctionAggrRvMax(_DataCache, args[0], scope);
							break;
						case "min":
							result = new FunctionAggrRvMin(_DataCache, args[0], scope);
							break;
						case "stdev":
							result = new FunctionAggrRvStdev(_DataCache, args[0], scope);
							break;
						case "stdevp":
							result = new FunctionAggrRvStdevp(_DataCache, args[0], scope);
							break;
						case "var":
							result = new FunctionAggrRvVar(_DataCache, args[0], scope);
							break;
						case "varp":
							result = new FunctionAggrRvVarp(_DataCache, args[0], scope);
							break;
						default:
							throw new ParserException(string.Format(Strings.Parser_ErrorP_RunningValueNotSupported, aggrFunc) + GetLocationInfo(curToken));
					}
					break;
				case "stdev":
					(scope, bSimple) = await ResolveAggrScope(args, 2);
					FunctionAggrStdev aggrSDev = new FunctionAggrStdev(_DataCache, args[0], scope);
					aggrSDev.LevelCheck = bSimple;
					result = aggrSDev;
					break;
				case "stdevp":
					(scope, bSimple) = await ResolveAggrScope(args, 2);
					FunctionAggrStdevp aggrSDevP = new FunctionAggrStdevp(_DataCache, args[0], scope);
					aggrSDevP.LevelCheck = bSimple;
					result = aggrSDevP;
					break;
				case "var":
					(scope, bSimple) = await ResolveAggrScope(args, 2);
					FunctionAggrVar aggrVar = new FunctionAggrVar(_DataCache, args[0], scope);
					aggrVar.LevelCheck = bSimple;
					result = aggrVar;
					break;
				case "varp":
					(scope, bSimple) = await ResolveAggrScope(args, 2);
					FunctionAggrVarp aggrVarP = new FunctionAggrVarp(_DataCache, args[0], scope);
					aggrVarP.LevelCheck = bSimple;
					result = aggrVarP;
					break;
				default:
					result = ResolveMethodCall(fullname, args);		// through exception when fails
					break;
			}

			return (true, result);
		}

		private bool IsAggregate(string method, bool onePart)
		{
			if (!onePart)
				return false;
			bool rc;
			switch(method.ToLower())
			{	// this needs to include all aggregate functions
				case "sum":
				case "avg":
				case "min":
				case "max":
				case "first":
				case "last":
				case "next":
				case "previous":
                case "aggregate":
				case "count":
				case "countrows":
				case "countdistinct":
				case "stdev":
				case "stdevp":
				case "var":
				case "varp":
				case "rownumber":
				case "runningvalue":
					rc = true;
					break;
				default:
					rc = false;
					break;
			}
			return rc;
		}

		private async Task<(object scope, bool bSimple)> ResolveAggrScope(IExpr[] args, int indexOfScope)
		{
			object scope;
			
			bool bSimple = true;

            if (args.Length == 0 && indexOfScope > 1)
                throw new ParserException(Strings.Parser_ErrorP_AggregateMust1Argument);

			if (args.Length >= indexOfScope)
			{
				string n = await args[indexOfScope-1].EvaluateString(null, null);
				if (idLookup.IsPageScope)
					throw new ParserException(string.Format(Strings.Parser_ErrorP_ScopeNotSpecifiedInHeaderOrFooter,n));

				scope = idLookup.LookupScope(n);
				if (scope == null)
				{
					Identifier ie = args[indexOfScope-1] as Identifier;
					if (ie == null || ie.IsNothing == false)	// check for "nothing" identifier
						throw new ParserException(string.Format(Strings.Parser_ErrorP_ScopeNotKnownGrouping,n));
				}

				if (args.Length > indexOfScope)	// has recursive/simple been specified
				{
					IdentifierKey k = args[indexOfScope] as IdentifierKey;
					if (k == null)
						throw new ParserException(Strings.Parser_ErrorP_ScopeIdentifer + GetLocationInfo(curToken));
					if (k.Value == IdentifierKeyEnum.Recursive)
						bSimple = false;
				}
			}
			else if (idLookup.IsPageScope)
			{
				scope = "pghf";	// indicates page header or footer
			}
			else
			{
				scope = idLookup.LookupGrouping();
				if (scope == null)
				{
					scope = idLookup.LookupMatrix();
					if (scope == null)
						scope = idLookup.ScopeDataSet(null);
				}
			}

			return (scope, bSimple);
		}

        private IExpr ResolveParametersMethod(string pname, string vf, IExpr[] args)
        {
            FunctionReportParameter result;

            ReportParameter p = idLookup.LookupParameter(pname);
            if (p == null)
                throw new ParserException(string.Format(Strings.Parser_ErrorP_ParameterNotFound, pname));

            string arrayMethod;
            int posBreak = vf.IndexOf('.');
            if (posBreak > 0)
            {
                arrayMethod = vf.Substring(posBreak + 1);	// rest of expression
                vf = vf.Substring(0, posBreak);
            }
            else
                arrayMethod = null;

            if (vf == null || vf == "Value")
                result = new FunctionReportParameter(p);
            else if (vf == "Label")
                result = new FunctionReportParameterLabel(p);
            else
                throw new ParserException(string.Format(Strings.Parser_ErrorP_ParameterSupportsValueAndLabel, pname));

            result.SetParameterMethod(arrayMethod, args);

            return result;
        }

		private IExpr ResolveMethodCall(string fullname, IExpr[] args)
		{
			string cls, method;
			int idx = fullname.LastIndexOf('.');
			if (idx > 0)
			{
				cls = fullname.Substring(0, idx);
				method = fullname.Substring(idx+1);
			}
			else
			{
				cls = "";
				method = fullname;
			}

			// Fill out the argument types
			Type[] argTypes = new Type[args.Length];
			for (int i=0; i < args.Length; i++)
			{
				argTypes[i] = XmlUtil.GetTypeFromTypeCode(args[i].GetTypeCode());
			}
			// See if this is a function within the Code element
			Type cType=null;
			bool bCodeFunction = false;
			if (cls == "" || cls.ToLower() == "code")
			{
				cType = idLookup.CodeClassType;					// get the code class type
				if (cType != null)
				{
					if (XmlUtil.GetMethod(cType, method, argTypes) == null)
						cType = null;		// try for the method in the instance
					else 
						bCodeFunction = true;
				}
				if (cls != "" && !bCodeFunction)
					throw new ParserException(string.Format(Strings.Parser_ErrorP_NotCodeMethod, method));
			}

			// See if this is a function within the instance classes
			ReportClass rc=null;
			if (cType == null)
			{
				rc = idLookup.LookupInstance(cls);	// is this an instance variable name?
				if (rc == null)
				{
					cType=idLookup.LookupType(cls);				// no, must be a static class reference
				}
				else
				{
					cType= idLookup.LookupType(rc.ClassName);	// yes, use the classname of the ReportClass
				}
			}
			string syscls=null;

			if (cType == null)
			{	// ok try for some of the system functions

				switch(cls)
				{
					case "Math":
						syscls = "System.Math";
						break;
					case "String":
						syscls = "System.String";
						break;
					case "Convert":
						syscls = "System.Convert";
						break;
					case "Financial":
						syscls = "Majorsilence.Reporting.Rdl.Financial";
						break;
					default:
						syscls = "Majorsilence.Reporting.Rdl.VBFunctions";
						break;
				}
				if (syscls != null)
				{
					cType = Type.GetType(syscls, false, true);
				}
			}

			if (cType == null)
			{
				string err;
				if (cls == null || cls.Length == 0)
					err = String.Format(Strings.Parser_ErrorP_FunctionUnknown, method);
				else
					err = String.Format(Strings.Parser_ErrorP_ClassUnknown, cls);

				throw new ParserException(err);
			}

			IExpr result=null;

			MethodInfo mInfo = XmlUtil.GetMethod(cType, method, argTypes);
            if (mInfo == null)
            {
                string err;
                if (cls == null || cls.Length == 0)
					err = String.Format(Strings.Parser_ErrorP_FunctionUnknown, method);
                else
                    err = String.Format(Strings.Parser_ErrorP_FunctionOfClassUnknown, method, cls);

                throw new ParserException(err);
            }

            // when nullable object (e.g. DateTime?, int?,...)
            //    we need to get the underlying type
            Type t = mInfo.ReturnType;
            if (Type.GetTypeCode(t) == TypeCode.Object)
            {
                try
                {
                    t = Nullable.GetUnderlyingType(t);
                }
                catch { }  // ok if it fails; must not be nullable object
            }
            // obtain the TypeCode
			TypeCode tc = Type.GetTypeCode(t);
			if (bCodeFunction)
				result = new FunctionCode(method, args, tc);
			else if (syscls != null)
				result = new FunctionSystem(syscls, method, args, tc);
			else if (rc == null)
				result = new FunctionCustomStatic(idLookup.CMS, cls, method, args, tc);
			else
				result = new FunctionCustomInstance(rc, method, args, tc);

			return result;
		}
	}

}
