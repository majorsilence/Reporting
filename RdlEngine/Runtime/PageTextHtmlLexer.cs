
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Majorsilence.Reporting.RdlEngine.Resources;

namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// A simple lexer that is used by PageTextHtml to break an HTML string into components.
	/// </summary>
	internal class PageTextHtmlLexer
	{
		private List<string> tokens;
		private CharReader reader;
		private readonly string html;
		private readonly StringBuilder lt = new StringBuilder();	// reused by ReadHtml / ReadWord
		// we reserve some of the strings for control purposes
		static internal string EOF = '\ufffe'.ToString();
		static internal string WHITESPACE = '\ufffd'.ToString();
		static internal char HTMLCMD = '\ufffc';
        static internal char NBSP = '\ufffb';
		static internal readonly string NBSPString = NBSP.ToString();
		static readonly string CommentToken = HTMLCMD + "<cmnt>";

		/// <summary>
		/// Initializes a new instance of the Lexer class with the specified
		/// TextReader to lex.
		/// </summary>
		/// <param name="source">A TextReader to lex.</param>
		internal PageTextHtmlLexer(string html)
		{
			// token queue
			tokens = new List<string>(html.Length / 4 + 2);

			// read the file contents
			this.html = html;
			reader = new CharReader(html);
		}

		/// <summary>
		/// Breaks the input stream onto the tokens list and returns it.
		/// </summary>
		/// <returns>The tokens list.</returns>
		internal List<string> Lex()
		{
			string token = GetNextToken();
			while(true)
			{
				if(token == null)
				{
					tokens.Add(EOF);	// add EOF token
					return tokens;
				}
				tokens.Add(token);
				token = GetNextToken();
			}
		}

		private string GetNextToken()
		{
			bool bWhiteSpace = false;
			while(!reader.EndOfInput())
			{
				char ch = reader.GetNext();

				// skipping whitespaces	at front of token
				if(Char.IsWhiteSpace(ch))
				{
					bWhiteSpace = true;
					continue;
				}

				// If we had any white space 
				if (bWhiteSpace)
				{
					reader.UnGet();
					return WHITESPACE;			// indicates white space in location
				}

				switch(ch)
				{
					case '<':
						return ReadHtml(ch, '>');
					default:
						return ReadWord(ch, '<');
				} // end of switch
			}
			return null;
		}

		// Read html command string
		private string ReadHtml(char firstc, char ch)
		{
			char qChar = ch;
			lt.Clear();
            if (IsHtmlComment())
            {       // HTML is a comment;   donated by jonh of the forum
                char a;
                char b = '\0';
                char c = '\0';
                a = firstc;
                while (!reader.EndOfInput())
                {
                    if (a == '>' && b == '-' && c == '-')
                    {
                        break;
                    }
                    c = b;
                    b = a;
                    a = reader.GetNext();

                }
                return CommentToken;
            }

            // Usual case: no entity before the closing character, so the token is the html command
            // marker followed by that part of the source text, made without copying it twice.
            int from = reader.Position;
            int close = html.IndexOf(qChar, from);
            if (close >= 0 && html.IndexOf('&', from, close - from) < 0)
            {
                reader.Position = close + 1;
                ReadOnlySpan<char> rest = html.AsSpan(from, close - from + 1);
                if (firstc == '<' && (rest.SequenceEqual("q>") || rest.SequenceEqual("/q>")))
                    return "\"";
                Span<char> head = stackalloc char[] { HTMLCMD, firstc };
                return string.Concat(head, rest);
            }

            lt.Append(HTMLCMD);	// mark as HTML command
            lt.Append(firstc);

            while (!reader.EndOfInput())
            {
                ch = reader.GetNext();
                if (ch == '&')
                    ch = ReadSpecial();
                lt.Append(ch);
                if (ch == qChar)
                {
                    string cmd = lt.ToString();
                    if (cmd.AsSpan(1).SequenceEqual("<q>") || cmd.AsSpan(1).SequenceEqual("</q>"))
                        return "\"";
                    return cmd;
                }
            }
			return lt.ToString();
		}
        /// <summary>
        /// Determine if HTML is part of a comment.   Donated by jonh of the forum
        /// </summary>
        /// <returns></returns>
        private bool IsHtmlComment()
        {
            char a = '\0';
            char b = '\0';
            char c = '\0';
            int start = reader.Position;
            if (!reader.EndOfInput()) a = reader.GetNext();
            if (!reader.EndOfInput()) b = reader.GetNext();
            if (!reader.EndOfInput()) c = reader.GetNext();

            bool ret = false;

            if (a == '!' && b == '-' && c == '-')
                ret = true;
            else
                reader.Position = start;	// put back only what was read: near the end of the text that is fewer than three
            return ret;
        } 

		// handles special characters; that is those beginning with &
		private char ReadSpecial()
		{
			StringBuilder lt = new StringBuilder();	// not the shared one: called while that one is in use
            int MAXSPECIAL = 8;             // should be the size of the maximum escaped character
			while(!reader.EndOfInput() && MAXSPECIAL-- > 0)
			{
				char ch = reader.GetNext();
				if (ch == ';')
					break;				 
				lt.Append(ch);
			}
              
            if (MAXSPECIAL <= 0)
            {   // can't be an escaped character; undo all the reading
                for (int i=1; i <= lt.Length; i++) // unget all the characters
                { 
                    reader.UnGet();
                }
                return '&';
            }
			string s = lt.ToString();
			if (s.Length == 0)			// not a valid character; return blank
				return ' ';
			if (s[0] == '#')			// number character
			{
				try
				{
					string num = s.Substring(1);	// this can cause an exception
					int nv = Convert.ToInt32(num);	//    and this can too
					if (nv > 255)
						return ' ';		// not a valid number; return blank
					return Convert.ToChar(nv);
				}
				catch
				{
					return ' ';			// not a valid number; return blank
				}
			}
			switch (s)
            {   // see http://www.w3.org/TR/html4/sgml/entities.html
				case "quot": return '"';
				case "amp": return '&';
				case "lt": return '<';
				case "gt": return '>';
				case "nbsp": return NBSP;
				case "iexcl": return Convert.ToChar(161);
				case "cent": return Convert.ToChar(162);
				case "pound": return Convert.ToChar(163);
				case "curren": return Convert.ToChar(164);
				case "yen": return Convert.ToChar(165);
				case "brvbar": return Convert.ToChar(166);
				case "sect": return Convert.ToChar(167);
				case "uml": return Convert.ToChar(168);
				case "copy": return Convert.ToChar(169);
				case "ordf": return Convert.ToChar(170);
				case "laquo": return Convert.ToChar(171);
				case "not": return Convert.ToChar(172);
				case "shy": return Convert.ToChar(173);
				case "reg": return Convert.ToChar(174);
				case "macr": return Convert.ToChar(175);
				case "deg": return Convert.ToChar(176);
				case "plusmn": return Convert.ToChar(177);
				case "sup2": return Convert.ToChar(178);
				case "sup3": return Convert.ToChar(179);
				case "acute": return Convert.ToChar(180);
				case "micro": return Convert.ToChar(181);
				case "para": return Convert.ToChar(182);
				case "middot": return Convert.ToChar(183);
				case "cedil": return Convert.ToChar(184);
				case "sup1": return Convert.ToChar(185);
				case "ordm": return Convert.ToChar(186);
				case "raquo": return Convert.ToChar(187);
				case "frac14": return Convert.ToChar(188);
				case "frac12": return Convert.ToChar(189);
				case "frac34": return Convert.ToChar(190);
				case "iquest": return Convert.ToChar(191);
				case "Agrave": return Convert.ToChar(192);
				case "Aacute": return Convert.ToChar(193);
				case "Acirc": return Convert.ToChar(194);
				case "Atilde": return Convert.ToChar(195);
				case "Auml": return Convert.ToChar(196);
				case "Aring": return Convert.ToChar(197);
				case "AElig": return Convert.ToChar(198);
				case "Ccedil": return Convert.ToChar(199);
				case "Egrave": return Convert.ToChar(200);
				case "Eacute": return Convert.ToChar(201);
				case "Ecirc": return Convert.ToChar(202);
				case "Euml": return Convert.ToChar(203);
				case "lgrave": return Convert.ToChar(204);
				case "lacute": return Convert.ToChar(205);
				case "lcirc": return Convert.ToChar(206);
				case "luml": return Convert.ToChar(207);
				case "EHT": return Convert.ToChar(208);
				case "Ntilde": return Convert.ToChar(209);
				case "Ograve": return Convert.ToChar(210);
				case "Oacute": return Convert.ToChar(211);
				case "Ocirc": return Convert.ToChar(212);
				case "Otilde": return Convert.ToChar(213);
				case "Ouml": return Convert.ToChar(214);
				case "times": return Convert.ToChar(215);
				case "Oslash": return Convert.ToChar(216);
				case "Ugrave": return Convert.ToChar(217);
				case "Uacute": return Convert.ToChar(218);
				case "Ucirc": return Convert.ToChar(219);
				case "Uuml": return Convert.ToChar(220);
				case "Yacute": return Convert.ToChar(221);
				case "THORN": return Convert.ToChar(222);
				case "szlig": return Convert.ToChar(223);
				case "agrave": return Convert.ToChar(224);
				case "aacute": return Convert.ToChar(225);
				case "acirc": return Convert.ToChar(226);
				case "atilde": return Convert.ToChar(227);
				case "auml": return Convert.ToChar(228);
				case "aring": return Convert.ToChar(229);
				case "aelig": return Convert.ToChar(230);
				case "ccedil": return Convert.ToChar(231);
				case "egrave": return Convert.ToChar(232);
				case "eacute": return Convert.ToChar(233);
				case "ecirc": return Convert.ToChar(234);
				case "euml": return Convert.ToChar(235);
				case "igrave": return Convert.ToChar(236);
				case "iacute": return Convert.ToChar(237);
				case "icirc": return Convert.ToChar(238);
				case "iuml": return Convert.ToChar(239);
				case "eth": return Convert.ToChar(240);
				case "ntilde": return Convert.ToChar(241);
				case "ograve": return Convert.ToChar(242);
				case "oacute": return Convert.ToChar(243);
				case "ocirc": return Convert.ToChar(244);
				case "otilde": return Convert.ToChar(245);
				case "ouml": return Convert.ToChar(246);
				case "divide": return Convert.ToChar(247);
				case "oslash": return Convert.ToChar(248);
				case "ugrave": return Convert.ToChar(249);
				case "uacute": return Convert.ToChar(250);
				case "ucirc": return Convert.ToChar(251);
				case "uuml": return Convert.ToChar(252);
				case "yacute": return Convert.ToChar(253);
				case "thorn": return Convert.ToChar(254);
				case "yuml": return Convert.ToChar(255);
                //  Arrows 
                case "larr": return Convert.ToChar(8592);
                case "uarr": return Convert.ToChar(8593);
                case "rarr": return Convert.ToChar(8594);
                case "darr": return Convert.ToChar(8595);
                case "harr": return Convert.ToChar(8596);
                case "crarr": return Convert.ToChar(8629);
                case "lArr": return Convert.ToChar(8657);
                case "rArr": return Convert.ToChar(8658);
                case "dArr": return Convert.ToChar(8659);
                case "hArr": return Convert.ToChar(8660);
                // Miscellaneous Symbols -->
                case "spades": return Convert.ToChar(9824);
                case "clubs": return Convert.ToChar(9827);
                case "hearts": return Convert.ToChar(9829);
                case "diams": return Convert.ToChar(9830);
                default: return ' ';			// not a valid special character
			}
		}

		// Read to next white space or to specified character
		private string ReadWord(char firstc, char ch)
		{
			char qChar = ch;
			int start = reader.Position - 1;
			if (firstc != '&')
			{	// usual case: no entity in the word, so it is a piece of the source text
				int i = reader.Position;
				while (i < html.Length)
				{
					char c = html[i];
					if (Char.IsWhiteSpace(c) || c == qChar || c == '>' || c == '&')
						break;
					i++;
				}
				if (i >= html.Length || html[i] != '&')
				{
					reader.Position = i;
					return html.Substring(start, i - start);
				}
			}

			lt.Clear();
			lt.Append(firstc=='&'? ReadSpecial(): firstc);

			while(!reader.EndOfInput())
			{
				ch = reader.GetNext();
				if (Char.IsWhiteSpace(ch) || ch == qChar || ch == '>')
				{
					reader.UnGet();		// put it back on the stack
					break;
				}

				if (ch == '&')
					ch = ReadSpecial();

				lt.Append(ch);
			}
			return lt.ToString();
		}

		class CharReader
		{
			string file = null;
			int    ptr  = 0;

			internal int Position
			{
				get => ptr;
				set => ptr = value;
			}

			/// <summary>
			/// Initializes a new instance of the CharReader class.
			/// </summary>
			internal CharReader(string text)
			{
				file = text;
			}
		
			/// <summary>
			/// Returns the next char from the stream.
			/// </summary>
			internal char GetNext()
			{
				if (EndOfInput()) 
				{
					return '\0';
				}
				char ch = file[ptr++];

				return ch;
			}
		
			/// <summary>
			/// Returns the next char from the stream without removing it.
			/// </summary>
			internal char Peek()
			{
				if (EndOfInput()) // ok to peek at end of file
					return '\0';

				return file[ptr];
			}
		
			/// <summary>
			/// Undoes the extracting of the last char.
			/// </summary>
			internal void UnGet()
			{
				--ptr;
				if (ptr < 0) 
					throw new Exception(Strings.CharReader_Error_FileReaderUnGetFirstChar);
			
				char ch = file[ptr];
			}
		
			/// <summary>
			/// Returns True if end of input was reached; otherwise False.
			/// </summary>
			internal bool EndOfInput()
			{
				return ptr >= file.Length;
			}
		}
	}
}
