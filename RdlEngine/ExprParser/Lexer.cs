
using System;
using System.IO;
using System.Collections;
using System.Text;
using Majorsilence.Reporting.RdlEngine.Resources;

namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// A simple Lexer that is used by Parser.
	/// </summary>
	internal class Lexer
	{
		private TokenList tokens;
		private CharReader reader;

		/// <summary>
		/// Initializes a new instance of the Lexer class with the specified
		/// expression syntax to lex.
		/// </summary>
		/// <param name="expr">An expression to lex.</param>
		internal Lexer(string expr)
			: this(new StringReader(expr))
		{
			// use this
		}

		/// <summary>
		/// Initializes a new instance of the Lexer class with the specified
		/// TextReader to lex.
		/// </summary>
		/// <param name="source">A TextReader to lex.</param>
		internal Lexer(TextReader source)
		{
			// token queue
			tokens = new TokenList();

			// read the file contents
			reader = new CharReader(source);
		}

		/// <summary>
		/// Breaks the input stream onto the tokens list and returns it.
		/// </summary>
		/// <returns>The tokens list.</returns>
		internal TokenList Lex()
		{
			Token token = GetNextToken();
			while(true)
			{
				if(token != null)
					tokens.Add(token);
				else
				{
					tokens.Add(new Token(null, reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.EOF));
					return tokens;
				}

				token = GetNextToken();
			}
		}

		private Token GetNextToken()
		{
			while(!reader.EndOfInput())
			{
				char ch = reader.GetNext();

				// skipping whitespaces
				if(Char.IsWhiteSpace(ch))
				{
					continue;
				}
				switch(ch)
				{
					case '=':
						return new Token(ch.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.EQUAL);
					case '+':
						return new Token(ch.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.PLUS);
					case '-':
						return new Token(ch.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.MINUS);
					case '(':
						return new Token(ch.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.LPAREN);
					case ')':
						return new Token(ch.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.RPAREN);
					case ',':
						return new Token(ch.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.COMMA);
					case '^':
						return new Token(ch.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.EXP);
					case '%':
						return new Token(ch.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.MODULUS);
					case '!':
						if (reader.Peek() == '=')
						{
							reader.GetNext();	// go past the equal
							return new Token(ch.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.NOTEQUAL);
						}
						else
							return new Token(ch.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.NOT);
					case '&':
						return new Token(ch.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.PLUSSTRING);
					case '|':
						if (reader.Peek() == '|')
						{
							reader.GetNext();	// go past the '|'
							return new Token(ch.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.OR);
						}
						break;
					case '>':
						if (reader.Peek() == '=')
						{
							reader.GetNext();	// go past the equal
							return new Token(ch.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.GREATERTHANOREQUAL);
						}
						else
							return new Token(ch.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.GREATERTHAN);
					case '/':
						if (reader.Peek() == '*')
						{	// beginning of a comment of form /* a comment */
							reader.GetNext();	// go past the '*'
							ReadComment();
							continue;
						}
						else
							return new Token(ch.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.FORWARDSLASH);
					case '<':
						if (reader.Peek() == '=')
						{
							reader.GetNext();	// go past the equal
							return new Token(ch.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.LESSTHANOREQUAL);
						}
                        else if (reader.Peek() == '>')
                        {
                            reader.GetNext();	// go past the >
                            return new Token(ch.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.NOTEQUAL);
                        }
                        else
							return new Token(ch.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.LESSTHAN);
					case '*':
						return new Token(ch.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.STAR);
					case '"':
					case '\'':
						return ReadQuoted(ch);
                    case '{':
                        return ReadIdentifier(ch, 4); 
					default:
						break;
				} // end of swith
				if (Char.IsDigit(ch))
					return ReadNumber(ch);
                else if (ch == '.')
                {
                    char tc = reader.Peek();
                    if (Char.IsDigit(tc))
                        return ReadNumber(ch);
                    return new Token(ch.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.DOT);
                }
				else if (Char.IsLetter(ch) || ch == '_')
					return ReadIdentifier(ch);
				else
					return new Token(ch.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.OTHER);
			}
			return null;
		}

		// Reads a decimal number with optional exponentiation 
		private Token ReadNumber(char ch)
		{
			const char separator = '.'; // maybe CurrentCulture.NumberFormat.NumberDecimalSeparator ??? TODO
			int startLine = reader.Line;
			int startCol = reader.Column;
			bool bDecimal = ch == separator ? true : false;
			bool bDecimalType=false;	// found d or D in number
			bool bFloat=false;			// found e or E in number
			char cPeek;

			string number = ch.ToString();
			while(!reader.EndOfInput() )
			{
				cPeek = reader.Peek();
				if (Char.IsWhiteSpace(cPeek))
					break;

				if (Char.IsDigit(cPeek))
					number += reader.GetNext();
				else if ((cPeek == 'd' || cPeek == 'D') && !bFloat)
				{
					reader.GetNext();				// skip the 'd'
					bDecimalType = true;
					break;
				}
				else if ((cPeek == 'e' || cPeek == 'E') && !bFloat)
				{
					number += reader.GetNext();		// add the 'e'
					cPeek = reader.Peek();
					if (cPeek == '-' || cPeek == '+')   // +/- after e is optional assumes +
						number += reader.GetNext();
					bFloat = true;

                    if (Char.IsDigit(reader.Peek()))
						continue;
					
                    throw new ParserException(Strings.Lexer_ErrorP_InvalidNumberConstant);
				}
				else if (!bDecimal && !bFloat && cPeek == separator)	// can't already be decimal or float
				{
					bDecimal = true;
					number += reader.GetNext();
				}
				else
					break;	// another character
			}

			if (number.CompareTo(separator.ToString()) == 0)
				throw new ParserException(string.Format(Strings.Lexer_Error_SeparatorMustFollowedNumber, separator));

			TokenTypes t;
			if (bDecimalType)
				t = TokenTypes.NUMBER;
			else if (bFloat || bDecimal)
				t = TokenTypes.DOUBLE;
			else
				t = TokenTypes.INTEGER;

			return new Token(number, startLine, startCol, reader.Line, reader.Column, t);
		}


        // Reads an identifier:
        // Must consist of letters, digits, "_". "!", "." are allowed
        // but have special meaning that is disambiguated later
        private Token ReadIdentifier(char ch)
        {
            return ReadIdentifier(ch, 1);
        }

        // Reads an identifier:
        // Must consist of letters, digits, "_". "!", "." are allowed
        // but have special meaning that is disambiguated later
        // Josh: 6:21:10 overloaded to allow for setting initial state.
        private Token ReadIdentifier(char ch, int initialState) 
		{
			int startLine = reader.Line;
			int startCol = reader.Column;
			char cPeek;

			StringBuilder identifier = new StringBuilder(30);	// initial capacity 30 characters
			identifier.Append(ch.ToString());

            int state = initialState;      // state=1 means accept letter,digit,'.','!','_'
                                // state=2 means accept whitespace ends with '.' or '!'
                                // state=3 means accept letter to start new qualifier
            while (!reader.EndOfInput())
			{
				cPeek = reader.Peek();
                if (state == 1)
                {
                    if (Char.IsLetterOrDigit(cPeek) || cPeek == '.' ||
                        cPeek == '!' || cPeek == '_')
                        identifier.Append(reader.GetNext());
                    else if (Char.IsWhiteSpace(cPeek))
                    {
                        reader.GetNext();   // skip space
                        if (identifier[identifier.Length - 1] == '.' ||
                            identifier[identifier.Length - 1] == '!')
                            state = 3;  // need to have an identfier next
                        else
                            state = 2;  // need to get '.' or '!' next
                    }
                    else
                        break;
                }
                else if (state == 2)
                {   // state must equal 2
                    if (cPeek == '.' || cPeek == '!')
                    {
                        state = 3;
                        identifier.Append(reader.GetNext());
                    }
                    else if (Char.IsWhiteSpace(cPeek))
                        reader.GetNext();
                    else 
                        break;
                }
                else if (state == 3)
                {   // state must equal 3
                    if (Char.IsLetter(cPeek) || cPeek == '_')
                    {
                        state = 1;
                        identifier.Append(reader.GetNext());
                    }
                    else if (Char.IsWhiteSpace(cPeek))
                    {
                        reader.GetNext();
                    }
                    else
                        break;
                }
                else if (state == 4)
                { // state must equal 4 Josh: 6:21:10 added state 4 for field/param shortcuts
					if (Char.IsLetterOrDigit(cPeek) || cPeek == '@' ||
					cPeek == '?' || cPeek == '_' || cPeek == '}' ||
					cPeek == '!')
					{
						identifier.Append(reader.GetNext());

						if (cPeek == '}')
							break;
					}
					else if (Char.IsWhiteSpace(cPeek))
					{
						reader.GetNext(); // skip space
					}
					else
						break;
                } 
			}

			string key = identifier.ToString().ToLower();
			if (key == "and" || key == "andalso")   // technically 'and' and 'andalso' mean different things; but we treat the same
				return new Token(identifier.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.AND);
			else if (key == "or" || key == "orelse")    // technically 'or' and 'orelse' mean different things; but we treat the same
				return new Token(identifier.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.OR);
            else if (key == "not")
                return new Token(identifier.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.NOT);
			else if (key == "like")
				return new Token(identifier.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.LIKE);
			else if (key == "mod")
                return new Token(identifier.ToString(), reader.Line, reader.Column, reader.Line, reader.Column, TokenTypes.MODULUS);

            //Shortcut identifier
            if (state == 4)
            {
                if (identifier[identifier.Length - 1] != '}')
                    throw new ParserException(Strings.Parser_ErrorP_CurlyBracketExpected);

                identifier = new StringBuilder(ParseShortcut(identifier.ToString()));
            } 

			// normal identifier
			return new Token(identifier.ToString(), startLine, startCol, reader.Line, reader.Column, TokenTypes.IDENTIFIER);
		}

		// Quoted string like " asdf " or ' asdf '
		private Token ReadQuoted(char ch)
		{
			char qChar = ch;
			int startLine = reader.Line;
			int startCol = reader.Column;
			StringBuilder quoted = new StringBuilder();

			while(!reader.EndOfInput())
			{
				ch = reader.GetNext();
				// A backslash is an ordinary character here. RDL expressions are VB.NET,
				// which has no backslash escapes at all - the only escape is the doubled
				// quote handled below. Treating it as an escape swallowed the closing quote
				// of any literal ending in one, so a Windows path or a hierarchy separator
				// failed to lex and was reported as an unterminated string.
				if (ch == qChar)
                {
                    if (reader.Peek() == ch)            // did user double the quote?
                        ch = reader.GetNext();          //  yes, we just append one character
                    else
                        return new Token(quoted.ToString(), startLine, startCol, reader.Line, reader.Column, TokenTypes.QUOTE);
                }
    			quoted.Append(ch);
			}
			throw new ParserException(Strings.Lexer_ErrorP_UnterminatedString);
		}

		// Comment string like /* this is a comment */
		private void ReadComment()
		{
			char ch;

			while(!reader.EndOfInput())
			{
				ch = reader.GetNext();
				if (ch == '*' && reader.Peek() == '/')
				{
					reader.GetNext();			// skip past the '/'
					return;
				}
			}
			throw new ParserException(Strings.Lexer_Error_UnterminatedComment);
		}

        // fields, parameters, and globals
        // Shortcuts for fields, parameters, globals
        private string ParseShortcut(string identifier)
        {

            if (identifier.StartsWith("{?"))
            {
                identifier = identifier.Replace("{?", "Parameters!");
                identifier = identifier.Replace("}", ".Value");
            }
            else if (identifier.StartsWith("{@"))
            {
                identifier = identifier.Replace("{@", "Globals!");
                identifier = identifier.Replace("}", "");
            }
            else if (identifier.StartsWith("{!"))
            {
                identifier = identifier.Replace("{!", "User!");
                identifier = identifier.Replace("}", "");
            }
            else if (identifier.StartsWith("{"))
            {
                identifier = identifier.Replace("{", "Fields!");
                identifier = identifier.Replace("}", ".Value");
            }

            return identifier;
        }


//		// Handles case of "<", "<=", and "<! ... xml string  !>
//		private Token ReadXML(char ch)
//		{
//			int startLine = reader.Line;
//			int startCol = reader.Column;
//
//			if (reader.EndOfInput())
//				return  new Token(ch.ToString(), startLine, startCol, startLine, startCol, TokenTypes.LESSTHAN);
//			ch = reader.GetNext();
//			if (ch == '=')
//				return  new Token("<=", startLine, startCol, reader.Line, reader.Column, TokenTypes.LESSTHANOREQUAL);
//			if (ch != '!')					// If it's not '!' then it's not XML
//			{
//				reader.UnGet();				// put back the character
//				return  new Token("<", startLine, startCol, reader.Line, reader.Column, TokenTypes.LESSTHAN);
//			}
//
//			string xml = "";				// intialize our string
//
//			while(!reader.EndOfInput())
//			{
//				ch = reader.GetNext();
//
//				if(ch == '!')				// check for end of XML denoted by "!>"
//				{
//					if (!reader.EndOfInput() && reader.Peek() == '>')
//					{
//						reader.GetNext();	// pull the '>' off the input
//						return new Token(xml, startLine, startCol, reader.Line, reader.Column, TokenTypes.XML);
//					}
//				}
//
//				xml += ch.ToString();
//			}
//			throw new ParserException("Unterminated XML clause!");
//		}

	}
}
