
using System;

namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Summary description for TokenTypes.
	/// </summary>
	internal enum TokenTypes
	{
		AND,
		OR,
		NOT,
		PLUS,
		PLUSSTRING,
		MINUS,
		LPAREN,
		RPAREN,
		QUOTE,
		IDENTIFIER,
		COMMA,
		NUMBER,
		DATETIME,
		DOUBLE,
		INTEGER,
		EQUAL,
		NOTEQUAL,
		GREATERTHAN,
		GREATERTHANOREQUAL,
		LESSTHAN,
		LESSTHANOREQUAL,
		LIKE,
		FORWARDSLASH,
		BACKSLASH,
		STAR,
		EXP,
		MODULUS,
        DOT,                // dot operator
		OTHER,
		EOF
	}
}
