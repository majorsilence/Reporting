using Majorsilence.Reporting.RdlDesign.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Majorsilence.Reporting.RdlDesign.Syntax
{
	public class RdlScriptLexer
	{
		public enum Style
		{
			Default,
			UserInfo,
			Keyword,
			Identifier,
			Number,
			String,
			Error,
			Globals,
			Parameter,
			Method,
			AggrMethod,
			Operator,
			Field
		}

		enum LexerState{
			Unknown,
			Identifier,
			Number,
			String,
			Braces,
			Operator
		}

		enum IdentiferType
		{
			Field,
			ClassMethod
		}

		private HashSet<string> userInfo;
		private HashSet<string> globals;
		private HashSet<string> parameters;
		private HashSet<string> simpleMethods;
		private Dictionary<string, HashSet<string>> calssMethods;
		private HashSet<string> aggrMethods;
		private HashSet<string> operators;
		private HashSet<string> fields;

		public void StyleText(Scintilla scintilla, int startPos, int endPos)
		{
			if(scintilla.GetCharAt(0) != '=') //Not Expression
				return;

			// Back up to the line start
			var line = scintilla.LineFromPosition(startPos);
			startPos = scintilla.Lines[line].Position;

			var length = 0;
			char stringStartChar = '"';
			var state = LexerState.Unknown;
			var EOF = false;

			// Start styling
			scintilla.StartStyling(startPos);
			while (startPos < endPos)
			{
				var c = (char)scintilla.GetCharAt(startPos);
				//lastSymbol = startPos == endPos - 1;

			REPROCESS:
				switch (state)
				{
					case LexerState.Unknown:
						if (c == '"' || c == '\'')
						{
							// Start of "string"
							stringStartChar = c;
							scintilla.SetStyling(1, (int)Style.String);
							state = LexerState.String;
						}
						else if (c == '{')
						{
							state = LexerState.Braces;
							goto REPROCESS;
						}
						else if (Char.IsDigit(c))
						{
							state = LexerState.Number;
							goto REPROCESS;
						}
						else if (Char.IsLetter(c))
						{
							state = LexerState.Identifier;
							goto REPROCESS;
						}
						else if (operators.Any(x => x[0] == c))
						{
							state = LexerState.Operator;
							goto REPROCESS;
						}
						else
						{
							// Everything else
							scintilla.SetStyling(1, (int)Style.Default);
						}
						break;

					case LexerState.String:
						if (c == stringStartChar)
						{
							length++;
							scintilla.SetStyling(length, (int)Style.String);
							length = 0;
							state = LexerState.Unknown;
						}
						else
						{
							length++;
						}
						break;

					case LexerState.Braces:
						if (c == '}')
						{
							length++;
							var style = Style.Identifier;
							var identifier = scintilla.GetTextRange(startPos - length + 2, length - 2);
							if (identifier.Length == 0)
							{
								style = Style.Error;
							}
							else if (identifier[0] == '!')
							{
								if (userInfo.Contains(identifier.Substring(1)))
									style = Style.UserInfo;
								else
									style = Style.Error;
							}
							else if (identifier[0] == '@')
							{
								if (globals.Contains(identifier.Substring(1)))
									style = Style.Globals;
								else
									style = Style.Error;
							}
							else if (identifier[0] == '?' && parameters != null)
							{
								if (parameters.Contains(identifier.Substring(1)))
									style = Style.Parameter;
								else
									style = Style.Error;
							}
							else
							{
								if (fields.Contains(identifier))
									style = Style.Field;
								else
									style = Style.Error;
							}

							scintilla.SetStyling(length, (int)style);
							length = 0;
							state = LexerState.Unknown;
						}
						else
							length++;
						break;

					case LexerState.Number:
						if (Char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F') || c == 'x')
						{
							length++;
						}
						else
						{
							scintilla.SetStyling(length, (int)Style.Number);
							length = 0;
							state = LexerState.Unknown;
							goto REPROCESS;
						}
						break;

					case LexerState.Identifier:
						if (!EOF && (Char.IsLetterOrDigit(c) || c == '.' || c == '!' || c == '_'))
						{
							length++;
						}
						else
						{
							var style = Style.Identifier;
							var identifier = scintilla.GetTextRange(startPos - length, length);
							
							var endFirstWord = identifier.IndexOf('.');
							if (endFirstWord != -1)
							{
								var firstWord = identifier.Substring(0, endFirstWord);
								var secondWord = identifier.Substring(endFirstWord + 1);
								if (calssMethods.ContainsKey(firstWord) && calssMethods[firstWord].Contains(secondWord))
								{
									style = Style.Method;
								}
								else
									style = Style.Error;
							}

							endFirstWord = identifier.IndexOf('!');
							if (endFirstWord != -1)
							{
								var firstWord = identifier.Substring(0, endFirstWord);
								var secondWord = identifier.Substring(endFirstWord +1);
								if (firstWord == "User")
								{
									if (userInfo.Contains(secondWord))
										style = Style.UserInfo;
									else
										style = Style.Error;
								}
								if (firstWord == "Globals")
								{
									if (globals.Contains(secondWord))
										style = Style.Globals;
									else
										style = Style.Error;
								}
								if (firstWord == "Parameters")
								{
									if (globals.Contains(secondWord))
										style = Style.Parameter;
									else
										style = Style.Error;
								}
								if (firstWord == "Fields")
								{
									var field = secondWord.Split('.');
									if (field.Length == 2 && fields.Contains(field[0]) 
										&& (field[1] == "Value" || field[1] == "IsMissing"))
										style = Style.Field;
									else
										style = Style.Error;
								}

							}

							if (simpleMethods.Contains(identifier))
								style = Style.Method;

							if (aggrMethods.Contains(identifier))
								style = Style.AggrMethod;

							scintilla.SetStyling(length, (int)style);
							length = 0;
							state = LexerState.Unknown;
							if(!EOF)
								goto REPROCESS;
						}
						break;
					case LexerState.Operator:
						var cur = scintilla.GetTextRange(startPos - length, length +1);
						if (operators.Any(x => x.StartsWith(cur)))
						{
							length++;
						}
						else
						{
							cur = scintilla.GetTextRange(startPos - length, length);
							Style style;
							if (operators.Contains(cur))
							{
								//length++;
								style = Style.Operator;
							}
							else
							{
								style = Style.Error;
							}
							scintilla.SetStyling(length, (int)style);
							length = 0;
							state = LexerState.Unknown;
							goto REPROCESS;
						}
						break;
				}

				startPos++;
				if (!EOF && startPos == endPos && state == LexerState.Identifier)
				{
					EOF = true;
					goto REPROCESS;
				}

			}
		}

		public RdlScriptLexer()
		{
			userInfo = new HashSet<string>(StaticLists.ArrayToFormattedList(StaticLists.UserList, "", ""));
			globals = new HashSet<string>(StaticLists.ArrayToFormattedList(StaticLists.GlobalList, "", ""));
			//Methods
			var methodsList = StaticLists.FunctionList.Select(x => x.Substring(0, x.IndexOf("("))).ToList();
			Rdl.FontStyleEnum fsi = Majorsilence.Reporting.Rdl.FontStyleEnum.Italic;	// just want a class from RdlEngine.dll assembly
            Assembly a = Assembly.GetAssembly(fsi.GetType());
            if (a == null)
                return;
            Type ft = a.GetType("Majorsilence.Reporting.Rdl.VBFunctions");
            BuildMethods(methodsList, ft);
			simpleMethods = new HashSet<string>(methodsList);
			
			// build list of methods in class
			calssMethods = new Dictionary<string, HashSet<string>>();
			methodsList = new List<string>();
			ft = a.GetType("Majorsilence.Reporting.Rdl.Financial");
			BuildMethods(methodsList, ft);
			calssMethods.Add("Financial", new HashSet<string>(methodsList));

			methodsList = new List<string>();
			a = Assembly.GetAssembly("".GetType());
			ft = a.GetType("System.Math");
			BuildMethods(methodsList, ft);
			calssMethods.Add("Math", new HashSet<string>(methodsList));

			methodsList = new List<string>();
			ft = a.GetType("System.Convert");
			BuildMethods(methodsList, ft);
			calssMethods.Add("Convert", new HashSet<string>(methodsList));

			methodsList = new List<string>();
			ft = a.GetType("System.String");
			BuildMethods(methodsList, ft);
			calssMethods.Add("String", new HashSet<string>(methodsList));

			//Aggregate Methods
			aggrMethods = new HashSet<string>(StaticLists.AggrFunctionList.Select(x => x.Substring(0, x.IndexOf("("))));

			//Opertors
			operators = new HashSet<string>(StaticLists.OperatorList.Select(x => x.Trim()));
		}

		public void SetParameters(IEnumerable<string> parametersList)
		{
			parameters = new HashSet<string>(parametersList);
		}

		public void SetFields(IEnumerable<string> fieldsList)
		{
			fields = new HashSet<string>(fieldsList);
		}

		void BuildMethods(List<string> ar, Type ft)
		{
			if (ft == null)
				return;
			MethodInfo[] mis = ft.GetMethods(BindingFlags.Static | BindingFlags.Public);
			foreach (MethodInfo mi in mis)
			{
				if (mi.Name != null)
					ar.Add(mi.Name);
			}
		}
	}
}
