using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Majorsilence.Spreadsheet
{
    /// <summary>
    /// Renders a value through an Excel number format code the way Excel displays it, using
    /// en-US conventions ('.' decimal point, ',' grouping, English month and day names).
    /// </summary>
    /// <remarks>
    /// Supports General, digit placeholders (0 # ?), grouping and scaling commas, percent,
    /// scientific notation, fractions, literal text and escapes, up to four sections with
    /// optional [conditions], colours (ignored) and currency/locale tags, the text section (@),
    /// and date/time codes including elapsed time ([h]) and fractional seconds.
    /// </remarks>
    public static class XlsxNumberFormatter
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static readonly string[] MonthNames =
            { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
        private static readonly string[] DayNames =
            { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

        public static string Format(double value, string? formatCode, bool date1904 = false)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "#NUM!";
            if (IsGeneral(formatCode))
                return General(value);

            var sections = ParseSections(formatCode!);
            var numberSections = sections.FindAll(s => !s.IsText);
            if (numberSections.Count == 0)
                return General(value);

            Section section;
            bool signed;
            if (numberSections.Exists(s => s.Op != null))
            {
                section = numberSections.Find(s => s.Op != null && s.Matches(value))
                          ?? numberSections.Find(s => s.Op == null)
                          ?? numberSections[numberSections.Count - 1];
                // A condition that picks out negatives (e.g. [<0]) shows them without the sign, like a negative section.
                bool selectsNegatives = (section.Op == "<" || section.Op == "<=") && section.Operand <= 0;
                signed = !selectsNegatives;
            }
            else if (numberSections.Count == 1 || (value > 0) || (value == 0 && numberSections.Count == 2))
            {
                section = numberSections[0];
                signed = numberSections.Count == 1;
            }
            else if (value < 0)
            {
                section = numberSections[1];
                signed = false;
            }
            else
            {
                section = numberSections[2];
                signed = false;
            }

            return FormatSection(signed ? value : Math.Abs(value), section.Body, date1904);
        }

        /// <summary>Applies a format's text section (the fourth section, or a lone section containing @) to text.</summary>
        public static string FormatText(string text, string? formatCode)
        {
            if (IsGeneral(formatCode))
                return text;
            var sections = ParseSections(formatCode!);
            Section? textSection = sections.Count >= 4 ? sections[3] : sections.Find(s => s.IsText);
            if (textSection == null)
                return text;

            var sb = new StringBuilder();
            foreach (var tok in Tokenize(textSection.Body))
            {
                if (tok.Kind == TokenKind.At) sb.Append(text);
                else if (tok.Kind == TokenKind.Literal) sb.Append(tok.Text);
            }
            return sb.ToString();
        }

        private static bool IsGeneral(string? code) =>
            string.IsNullOrEmpty(code) || string.Equals(code, "General", StringComparison.OrdinalIgnoreCase);

        /// <summary>Excel's General format: up to 11 characters, switching to scientific for very large or small values.</summary>
        public static string General(double value)
        {
            if (value == 0) return "0";
            double abs = Math.Abs(value);
            if (abs >= 1e11 || abs < 1e-9)
            {
                string s = value.ToString("0.#####E+00", Inv);
                return s;
            }
            int intDigits = (int)Math.Floor(Math.Log10(abs)) + 1;
            int decimals = intDigits >= 1 ? Math.Max(0, 10 - intDigits) : 9;
            double rounded = Math.Round(value, Math.Min(decimals, 15), MidpointRounding.AwayFromZero);
            return rounded.ToString("0.###############", Inv);
        }

        // ---- sections ----

        private sealed class Section
        {
            public string Body = "";
            public string? Op;
            public double Operand;
            public bool IsText;

            public bool Matches(double v) => Op switch
            {
                "<" => v < Operand,
                "<=" => v <= Operand,
                ">" => v > Operand,
                ">=" => v >= Operand,
                "=" => v == Operand,
                "<>" => v != Operand,
                _ => true,
            };
        }

        private static List<Section> ParseSections(string code)
        {
            var result = new List<Section>();
            var body = new StringBuilder();
            var current = new Section();
            for (int i = 0; i < code.Length; i++)
            {
                char c = code[i];
                if (c == '"')
                {
                    int end = code.IndexOf('"', i + 1);
                    if (end < 0) end = code.Length - 1;
                    body.Append(code, i, end - i + 1);
                    i = end;
                }
                else if (c == '\\' || c == '_' || c == '*')
                {
                    body.Append(c);
                    if (i + 1 < code.Length) body.Append(code[++i]);
                }
                else if (c == '[')
                {
                    int end = code.IndexOf(']', i);
                    if (end < 0) { body.Append(c); continue; }
                    string inner = code.Substring(i + 1, end - i - 1);
                    if (inner.Length > 0 && "<>=".IndexOf(inner[0]) >= 0)
                        ParseCondition(inner, current);
                    else if (inner.StartsWith("$", StringComparison.Ordinal))
                    {
                        int dash = inner.IndexOf('-');
                        string symbol = dash < 0 ? inner.Substring(1) : inner.Substring(1, dash - 1);
                        if (symbol.Length > 0) body.Append('"').Append(symbol.Replace("\"", "")).Append('"');
                    }
                    else if (IsElapsed(inner))
                        body.Append('[').Append(inner).Append(']');
                    // colours ([Red], [Color10]) and other tags don't change the text
                    i = end;
                }
                else if (c == ';')
                {
                    current.Body = body.ToString();
                    result.Add(Finish(current));
                    current = new Section();
                    body.Clear();
                }
                else
                {
                    body.Append(c);
                }
            }
            current.Body = body.ToString();
            result.Add(Finish(current));
            return result;
        }

        private static Section Finish(Section s)
        {
            foreach (var tok in Tokenize(s.Body))
                if (tok.Kind == TokenKind.At) { s.IsText = true; break; }
            return s;
        }

        private static void ParseCondition(string inner, Section section)
        {
            int opLen = inner.StartsWith("<=", StringComparison.Ordinal) || inner.StartsWith(">=", StringComparison.Ordinal) ||
                        inner.StartsWith("<>", StringComparison.Ordinal) ? 2 : 1;
            if (double.TryParse(inner.Substring(opLen), NumberStyles.Float, Inv, out double operand))
            {
                section.Op = inner.Substring(0, opLen);
                section.Operand = operand;
            }
        }

        private static bool IsElapsed(string inner)
        {
            if (inner.Length == 0) return false;
            char first = char.ToLowerInvariant(inner[0]);
            if (first != 'h' && first != 'm' && first != 's') return false;
            foreach (char ch in inner)
                if (char.ToLowerInvariant(ch) != first) return false;
            return true;
        }

        // ---- tokens ----

        private enum TokenKind { Literal, Zero, Hash, Question, Dot, Comma, Percent, Exp, Slash, At, General, Date }

        private struct Token
        {
            public TokenKind Kind;
            public string Text;
            public Token(TokenKind kind, string text) { Kind = kind; Text = text; }
        }

        private static List<Token> Tokenize(string body)
        {
            var tokens = new List<Token>();
            for (int i = 0; i < body.Length; i++)
            {
                char c = body[i];
                switch (c)
                {
                    case '"':
                        int end = body.IndexOf('"', i + 1);
                        if (end < 0) end = body.Length;
                        tokens.Add(new Token(TokenKind.Literal, body.Substring(i + 1, Math.Max(0, end - i - 1))));
                        i = end;
                        break;
                    case '\\':
                        if (i + 1 < body.Length) tokens.Add(new Token(TokenKind.Literal, body[++i].ToString()));
                        break;
                    case '_':
                        if (i + 1 < body.Length) i++;
                        tokens.Add(new Token(TokenKind.Literal, " "));
                        break;
                    case '*':
                        if (i + 1 < body.Length) i++; // fill character: no fixed width to fill
                        break;
                    case '0': tokens.Add(new Token(TokenKind.Zero, "0")); break;
                    case '#': tokens.Add(new Token(TokenKind.Hash, "#")); break;
                    case '?': tokens.Add(new Token(TokenKind.Question, "?")); break;
                    case '.': tokens.Add(new Token(TokenKind.Dot, ".")); break;
                    case ',': tokens.Add(new Token(TokenKind.Comma, ",")); break;
                    case '%': tokens.Add(new Token(TokenKind.Percent, "%")); break;
                    case '/': tokens.Add(new Token(TokenKind.Slash, "/")); break;
                    case '@': tokens.Add(new Token(TokenKind.At, "@")); break;
                    case 'E':
                    case 'e':
                        if (i + 1 < body.Length && (body[i + 1] == '+' || body[i + 1] == '-'))
                        {
                            tokens.Add(new Token(TokenKind.Exp, body[i + 1].ToString()));
                            i++;
                        }
                        else
                            tokens.Add(new Token(TokenKind.Date, "e")); // 'e' is a year code in dates
                        break;
                    case '[':
                        int close = body.IndexOf(']', i);
                        if (close < 0)
                        {
                            tokens.Add(new Token(TokenKind.Literal, "["));
                            break;
                        }
                        tokens.Add(new Token(TokenKind.Date, body.Substring(i, close - i + 1).ToLowerInvariant()));
                        i = close;
                        break;
                    default:
                        if (string.Compare(body, i, "General", 0, 7, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            tokens.Add(new Token(TokenKind.General, "General"));
                            i += 6;
                        }
                        else if (string.Compare(body, i, "AM/PM", 0, 5, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            tokens.Add(new Token(TokenKind.Date, "AM/PM"));
                            i += 4;
                        }
                        else if (string.Compare(body, i, "A/P", 0, 3, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            tokens.Add(new Token(TokenKind.Date, body.Substring(i, 3)));
                            i += 2;
                        }
                        else if ("yYmMdDhHsS".IndexOf(c) >= 0)
                        {
                            int run = 1;
                            while (i + run < body.Length && char.ToLowerInvariant(body[i + run]) == char.ToLowerInvariant(c)) run++;
                            tokens.Add(new Token(TokenKind.Date, new string(char.ToLowerInvariant(c), run)));
                            i += run - 1;
                        }
                        else
                            tokens.Add(new Token(TokenKind.Literal, c.ToString()));
                        break;
                }
            }
            return tokens;
        }

        private static string FormatSection(double value, string body, bool date1904)
        {
            var tokens = Tokenize(body);
            if (tokens.Exists(t => t.Kind == TokenKind.Date))
                return FormatDate(value, tokens, date1904);
            if (tokens.Exists(t => t.Kind == TokenKind.General))
            {
                var sb = new StringBuilder();
                foreach (var t in tokens)
                    sb.Append(t.Kind == TokenKind.General ? General(value) : t.Kind == TokenKind.Literal ? t.Text : "");
                return sb.ToString();
            }
            if (!tokens.Exists(IsPlaceholder))
            {
                var sb = new StringBuilder(value < 0 ? "-" : "");
                foreach (var t in tokens)
                    if (t.Kind == TokenKind.Literal || t.Kind == TokenKind.Percent) sb.Append(t.Text);
                return sb.ToString();
            }
            int slash = tokens.FindIndex(t => t.Kind == TokenKind.Slash);
            if (slash > 0)
                return FormatFraction(value, tokens, slash);
            return FormatNumber(value, tokens);
        }

        private static bool IsPlaceholder(Token t) =>
            t.Kind == TokenKind.Zero || t.Kind == TokenKind.Hash || t.Kind == TokenKind.Question;

        // ---- numbers ----

        private static string FormatNumber(double value, List<Token> tokens)
        {
            bool negative = value < 0;
            double v = Math.Abs(value);

            int percentCount = tokens.FindAll(t => t.Kind == TokenKind.Percent).Count;
            for (int p = 0; p < percentCount; p++) v *= 100;

            int exp = tokens.FindIndex(t => t.Kind == TokenKind.Exp);
            int numberEnd = exp >= 0 ? exp : tokens.Count;
            int dot = tokens.FindIndex(0, numberEnd, t => t.Kind == TokenKind.Dot);
            int intEnd = dot >= 0 ? dot : numberEnd;

            var intPh = new List<int>();
            var fracPh = new List<int>();
            for (int i = 0; i < numberEnd; i++)
            {
                if (!IsPlaceholder(tokens[i])) continue;
                if (i < intEnd) intPh.Add(i); else fracPh.Add(i);
            }

            // A comma between integer placeholders turns on grouping; one after the last placeholder of
            // the integer part or of the whole number scales the value down by 1000.
            bool grouping = false;
            int scale = 0;
            for (int i = 0; i < numberEnd; i++)
            {
                if (tokens[i].Kind != TokenKind.Comma) continue;
                bool placeholderBefore = tokens.FindLastIndex(i, i + 1, IsPlaceholder) >= 0;
                if (!placeholderBefore) continue;
                bool placeholderAfterInInteger = i < intEnd && tokens.FindIndex(i, intEnd - i, IsPlaceholder) >= 0;
                bool placeholderAfterInNumber = tokens.FindIndex(i, numberEnd - i, IsPlaceholder) >= 0;
                if (placeholderAfterInInteger) grouping = true;
                else if (i < intEnd || !placeholderAfterInNumber) scale++;
            }
            for (int s = 0; s < scale; s++) v /= 1000;

            int exponent = 0;
            if (exp >= 0 && v != 0)
            {
                // "##0.0E+0" (leading #, several integer placeholders) is engineering notation:
                // the exponent is a multiple of the placeholder count.
                int k = Math.Max(1, intPh.Count);
                bool engineering = intPh.Count > 1 && tokens[intPh[0]].Kind == TokenKind.Hash;
                int step = engineering ? k : 1;
                int mag = (int)Math.Floor(Math.Log10(v));
                exponent = engineering ? (int)Math.Floor(mag / (double)k) * k : mag - (k - 1);
                v /= Math.Pow(10, exponent);
                if (Math.Round(v, fracPh.Count, MidpointRounding.AwayFromZero) >= Math.Pow(10, k))
                {
                    v /= Math.Pow(10, step);
                    exponent += step;
                }
            }

            string digits = RoundToString(v, fracPh.Count);
            int point = digits.IndexOf('.');
            string intDigits = point < 0 ? digits : digits.Substring(0, point);
            string fracDigits = point < 0 ? "" : digits.Substring(point + 1);
            if (intDigits == "0") intDigits = "";

            var sb = new StringBuilder();
            if (negative) sb.Append('-'); // Excel shows -0 for a negative that rounds to zero

            var intText = FillInteger(tokens, intPh, intDigits, grouping);
            var fracText = FillFraction(tokens, fracPh, fracDigits);
            int firstIntPh = intPh.Count > 0 ? intPh[0] : -1;
            int lastIntPh = intPh.Count > 0 ? intPh[intPh.Count - 1] : -1;

            for (int i = 0; i < tokens.Count; i++)
            {
                var t = tokens[i];
                if (i == firstIntPh)
                {
                    sb.Append(intText); // includes any literals between the integer placeholders
                    i = lastIntPh;
                    continue;
                }
                if (i < numberEnd && IsPlaceholder(t))
                {
                    sb.Append(fracText[fracPh.IndexOf(i)]);
                    continue;
                }
                switch (t.Kind)
                {
                    case TokenKind.Literal:
                        sb.Append(t.Text);
                        break;
                    case TokenKind.Percent:
                        sb.Append('%');
                        break;
                    case TokenKind.Dot:
                        if (i == dot && intPh.Count == 0) sb.Append(intDigits); // ".00" still shows the whole part
                        sb.Append('.');
                        break;
                    case TokenKind.Exp:
                        var expPh = tokens.GetRange(i + 1, tokens.Count - i - 1).FindAll(IsPlaceholder);
                        string e = Math.Abs(exponent).ToString(Inv).PadLeft(Math.Max(1, expPh.Count), '0');
                        sb.Append('E').Append(exponent < 0 ? "-" : t.Text == "+" ? "+" : "").Append(e);
                        for (int j = i + 1; j < tokens.Count; j++)
                            if (tokens[j].Kind == TokenKind.Literal) sb.Append(tokens[j].Text);
                        return sb.ToString();
                }
            }
            return sb.ToString();
        }

        // Excel works with 15 significant digits, so 2.675 shows as 2.68 rather than the binary 2.67499...
        private static string RoundToString(double v, int decimals)
        {
            string g15 = v.ToString("G15", Inv);
            double sig15 = double.Parse(g15, Inv);
            if (Math.Abs(sig15) < 7.9e27)
            {
                // Parse the G15 text straight to decimal rather than casting the double: from .NET 11
                // the (decimal)double cast converts the exact binary value (2.67499999...) instead of
                // rounding to 15 significant digits, which would round 2.675 down to 2.67.
                decimal d = Math.Round(decimal.Parse(g15, NumberStyles.Float, Inv), Math.Min(decimals, 28), MidpointRounding.AwayFromZero);
                return d.ToString("F" + decimals, Inv);
            }
            return sig15.ToString("F" + decimals, Inv);
        }

        private static string FillInteger(List<Token> tokens, List<int> intPh, string intDigits, bool grouping)
        {
            if (intPh.Count == 0)
                return intDigits;

            if (grouping)
            {
                int required = 0;
                bool seenRequired = false;
                foreach (int p in intPh)
                {
                    if (tokens[p].Kind != TokenKind.Hash) seenRequired = true;
                    if (seenRequired) required++;
                }
                string padded = intDigits.PadLeft(required, '0');
                var g = new StringBuilder();
                for (int i = 0; i < padded.Length; i++)
                {
                    if (i > 0 && (padded.Length - i) % 3 == 0) g.Append(',');
                    g.Append(padded[i]);
                }
                return g.ToString();
            }

            // Positional fill right to left, so literals between placeholders ("000-00-0000") stay in place.
            var outParts = new List<string>();
            int digitIndex = intDigits.Length - 1;
            int first = intPh[0], last = intPh[intPh.Count - 1];
            for (int i = last; i >= first; i--)
            {
                var t = tokens[i];
                if (IsPlaceholder(t))
                {
                    string piece;
                    if (i == first && digitIndex >= 0)
                        piece = intDigits.Substring(0, digitIndex + 1);
                    else if (digitIndex >= 0)
                        piece = intDigits[digitIndex].ToString();
                    else
                        piece = t.Kind == TokenKind.Zero ? "0" : t.Kind == TokenKind.Question ? " " : "";
                    digitIndex -= piece.Length > 0 && char.IsDigit(piece[0]) ? piece.Length : 0;
                    outParts.Add(piece);
                }
                else if (t.Kind == TokenKind.Literal)
                {
                    // A literal shows once a digit (or required placeholder) lies to its left.
                    outParts.Add(t.Text);
                }
            }
            outParts.Reverse();
            return string.Concat(outParts);
        }

        private static string[] FillFraction(List<Token> tokens, List<int> fracPh, string fracDigits)
        {
            var result = new string[fracPh.Count];
            bool trimming = true;
            for (int i = fracPh.Count - 1; i >= 0; i--)
            {
                char d = i < fracDigits.Length ? fracDigits[i] : '0';
                var kind = tokens[fracPh[i]].Kind;
                if (trimming && d == '0' && kind != TokenKind.Zero)
                {
                    result[i] = kind == TokenKind.Question ? " " : "";
                    continue;
                }
                trimming = false;
                result[i] = d.ToString();
            }
            return result;
        }

        private static string FormatFraction(double value, List<Token> tokens, int slash)
        {
            bool negative = value < 0;
            double v = Math.Abs(value);

            // Numerator placeholders are the run just before '/'; anything earlier is the whole-number part.
            int numStart = slash - 1;
            while (numStart > 0 && IsPlaceholder(tokens[numStart - 1])) numStart--;
            while (numStart < slash && !IsPlaceholder(tokens[numStart])) numStart++;
            bool hasWhole = tokens.FindIndex(0, numStart, IsPlaceholder) >= 0;

            int numDigits = slash - numStart;
            var denTokens = new List<Token>();
            int denEnd = slash + 1;
            while (denEnd < tokens.Count && (IsPlaceholder(tokens[denEnd]) ||
                   (tokens[denEnd].Kind == TokenKind.Literal && tokens[denEnd].Text.Length == 1 && char.IsDigit(tokens[denEnd].Text[0]))))
                denTokens.Add(tokens[denEnd++]);

            double whole = hasWhole ? Math.Floor(v) : 0;
            double frac = v - whole;
            long num, den;
            string fixedDen = string.Concat(denTokens.FindAll(t => t.Kind == TokenKind.Literal).ConvertAll(t => t.Text));
            if (fixedDen.Length > 0 && fixedDen.Length == denTokens.Count)
            {
                den = long.Parse(fixedDen, Inv);
                num = (long)Math.Round(frac * den, MidpointRounding.AwayFromZero);
            }
            else
            {
                long maxDen = (long)Math.Pow(10, Math.Max(1, denTokens.Count)) - 1;
                (num, den) = BestFraction(frac, maxDen);
            }
            if (hasWhole && num == den && den > 0)
            {
                whole++;
                num = 0;
            }

            var sb = new StringBuilder();
            if (negative && (whole != 0 || num != 0)) sb.Append('-');
            for (int i = 0; i < numStart; i++)
            {
                var t = tokens[i];
                if (IsPlaceholder(t))
                {
                    if (i == tokens.FindIndex(0, numStart, IsPlaceholder))
                        sb.Append(whole != 0 ? whole.ToString("0", Inv) : (num == 0 ? "0" : ""));
                }
                else if (t.Kind == TokenKind.Literal && (whole != 0 || num == 0))
                    sb.Append(t.Text);
            }

            if (num == 0 && hasWhole)
            {
                sb.Append(' ', numDigits + 1 + denTokens.Count);
            }
            else
            {
                sb.Append(num.ToString(Inv).PadLeft(numDigits));
                sb.Append('/');
                sb.Append(den.ToString(Inv).PadRight(denTokens.Count));
            }
            for (int i = denEnd; i < tokens.Count; i++)
                if (tokens[i].Kind == TokenKind.Literal) sb.Append(tokens[i].Text);
            return hasWhole ? sb.ToString().TrimEnd() : sb.ToString();
        }

        private static (long, long) BestFraction(double x, long maxDen)
        {
            long bestNum = 0, bestDen = 1;
            double bestErr = Math.Abs(x);
            for (long d = 1; d <= maxDen; d++)
            {
                long n = (long)Math.Round(x * d, MidpointRounding.AwayFromZero);
                double err = Math.Abs(x - (double)n / d);
                if (err < bestErr - 1e-12)
                {
                    bestErr = err;
                    bestNum = n;
                    bestDen = d;
                    if (err == 0) break;
                }
            }
            return (bestNum, bestDen);
        }

        // ---- dates ----

        private static string FormatDate(double serial, List<Token> tokens, bool date1904)
        {
            if (serial < 0)
                return new string('#', 11);

            int fracDigits = 0;
            for (int i = 0; i < tokens.Count - 1; i++)
            {
                if (tokens[i].Kind == TokenKind.Dot && tokens[i + 1].Kind == TokenKind.Zero)
                {
                    int j = i + 1;
                    while (j < tokens.Count && tokens[j].Kind == TokenKind.Zero) j++;
                    fracDigits = Math.Min(3, j - i - 1);
                    break;
                }
            }

            double unit = 86400.0 * Math.Pow(10, fracDigits);
            double ticks = Math.Round(serial * unit, MidpointRounding.AwayFromZero);
            double totalSeconds = ticks / Math.Pow(10, fracDigits);

            DateTime epoch = date1904 ? new DateTime(1904, 1, 1) : new DateTime(1899, 12, 30);
            // In the 1900 system serials before 61 count from 1899-12-31 (Excel's fictional 29 Feb 1900).
            if (!date1904 && serial < 61) epoch = epoch.AddDays(1);
            DateTime dt;
            try { dt = epoch.AddSeconds(Math.Floor(totalSeconds)); }
            catch (ArgumentOutOfRangeException) { return new string('#', 11); }
            double fraction = totalSeconds - Math.Floor(totalSeconds);

            bool twelveHour = tokens.Exists(t => t.Kind == TokenKind.Date && (t.Text == "AM/PM" || t.Text.Equals("a/p", StringComparison.OrdinalIgnoreCase)));

            var sb = new StringBuilder();
            for (int i = 0; i < tokens.Count; i++)
            {
                var t = tokens[i];
                switch (t.Kind)
                {
                    case TokenKind.Date:
                        sb.Append(DatePart(t.Text, tokens, i, dt, totalSeconds, twelveHour));
                        break;
                    case TokenKind.Dot:
                        if (fracDigits > 0 && i + 1 < tokens.Count && tokens[i + 1].Kind == TokenKind.Zero)
                        {
                            sb.Append('.');
                            string f = Math.Round(fraction * Math.Pow(10, fracDigits)).ToString(Inv).PadLeft(fracDigits, '0');
                            sb.Append(f, 0, Math.Min(f.Length, fracDigits));
                            while (i + 1 < tokens.Count && tokens[i + 1].Kind == TokenKind.Zero) i++;
                        }
                        else
                            sb.Append('.');
                        break;
                    case TokenKind.Literal:
                    case TokenKind.Slash:
                    case TokenKind.Comma:
                    case TokenKind.Percent:
                        sb.Append(t.Text);
                        break;
                    case TokenKind.Zero:
                        sb.Append('0');
                        break;
                }
            }
            return sb.ToString();
        }

        private static string DatePart(string code, List<Token> tokens, int index, DateTime dt, double totalSeconds, bool twelveHour)
        {
            switch (code[0])
            {
                case 'y':
                case 'e':
                    return code.Length <= 2 && code[0] == 'y' ? (dt.Year % 100).ToString("00", Inv) : dt.Year.ToString(Inv);
                case 'd':
                    switch (code.Length)
                    {
                        case 1: return dt.Day.ToString(Inv);
                        case 2: return dt.Day.ToString("00", Inv);
                        case 3: return DayNames[(int)dt.DayOfWeek].Substring(0, 3);
                        default: return DayNames[(int)dt.DayOfWeek];
                    }
                case 'h':
                    int hour = dt.Hour;
                    if (twelveHour) { hour %= 12; if (hour == 0) hour = 12; }
                    return code.Length >= 2 ? hour.ToString("00", Inv) : hour.ToString(Inv);
                case 's':
                    return code.Length >= 2 ? dt.Second.ToString("00", Inv) : dt.Second.ToString(Inv);
                case 'm':
                    if (code.Length <= 2 && IsMinute(tokens, index))
                        return code.Length == 2 ? dt.Minute.ToString("00", Inv) : dt.Minute.ToString(Inv);
                    switch (code.Length)
                    {
                        case 1: return dt.Month.ToString(Inv);
                        case 2: return dt.Month.ToString("00", Inv);
                        case 3: return MonthNames[dt.Month - 1].Substring(0, 3);
                        case 5: return MonthNames[dt.Month - 1].Substring(0, 1);
                        default: return MonthNames[dt.Month - 1];
                    }
                case '[':
                    string inner = code.Substring(1, code.Length - 2);
                    double elapsed = inner[0] == 'h' ? Math.Floor(totalSeconds / 3600)
                        : inner[0] == 'm' ? Math.Floor(totalSeconds / 60)
                        : Math.Floor(totalSeconds);
                    return elapsed.ToString(Inv).PadLeft(inner.Length, '0');
                case 'A':
                case 'a':
                    if (code.Equals("AM/PM", StringComparison.OrdinalIgnoreCase))
                        return dt.Hour < 12 ? "AM" : "PM";
                    // A/P keeps the case written in the code
                    return dt.Hour < 12 ? code.Substring(0, 1) : code.Substring(2, 1);
                default:
                    return "";
            }
        }

        // m is minutes right after an hour code or right before a seconds code.
        private static bool IsMinute(List<Token> tokens, int index)
        {
            for (int i = index - 1; i >= 0; i--)
            {
                if (tokens[i].Kind != TokenKind.Date) continue;
                char c = tokens[i].Text[0];
                if (c == 'h' || (c == '[' && tokens[i].Text[1] == 'h')) return true;
                break;
            }
            for (int i = index + 1; i < tokens.Count; i++)
            {
                if (tokens[i].Kind != TokenKind.Date) continue;
                char c = tokens[i].Text[0];
                return c == 's' || (c == '[' && tokens[i].Text[1] == 's');
            }
            return false;
        }
    }
}
