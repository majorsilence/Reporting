using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RdlEngine.Render.ExcelConverter
{
    /// <summary>
    /// Turns a textbox's evaluated value and .NET format string into a typed Excel value and an
    /// equivalent Excel number format. When the format can't be expressed in Excel the value is
    /// not converted, so the caller keeps writing the formatted text and the cell looks the same.
    /// </summary>
    internal static class ExcelValueConverter
    {
        // Excel's 1900 date system treats 1900 as a leap year, so serials before March 1900 are off by a day.
        private static readonly DateTime FirstReliableDate = new DateTime(1900, 3, 1);

        /// <param name="numberFormatCode">The Excel format code, or null for General.</param>
        public static bool TryConvert(object value, string format, string language,
            out object typedValue, out string numberFormatCode)
        {
            typedValue = null;
            numberFormatCode = null;
            if (value == null)
                return false;

            var culture = CultureFor(language, format);
            switch (value)
            {
                case bool b:
                    if (!string.IsNullOrEmpty(format))
                        return false;
                    typedValue = b;
                    return true;
                case DateTime dt:
                    if (dt < FirstReliableDate || !TryDateFormat(format, culture, out numberFormatCode))
                        return false;
                    typedValue = dt;
                    return true;
                case double d when double.IsNaN(d) || double.IsInfinity(d):
                    return false;
                case float f when float.IsNaN(f) || float.IsInfinity(f):
                    return false;
                case sbyte _: case byte _: case short _: case ushort _: case int _: case uint _:
                case long _: case ulong _: case float _: case double _: case decimal _:
                    bool isInteger = !(value is float || value is double || value is decimal);
                    if (!TryNumberFormat(format, culture, isInteger, out numberFormatCode))
                        return false;
                    typedValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                    return true;
                default:
                    return false;
            }
        }

        // Mirrors Style.FormatValue: a format uses the textbox's Language, no format uses the current culture.
        private static CultureInfo CultureFor(string language, string format)
        {
            if (!string.IsNullOrEmpty(format) && !string.IsNullOrEmpty(language))
            {
                try { return CultureInfo.GetCultureInfo(language); }
                catch (CultureNotFoundException) { }
            }
            return CultureInfo.CurrentCulture;
        }

        internal static bool TryNumberFormat(string format, CultureInfo culture, bool isInteger, out string code)
        {
            code = null;
            if (string.IsNullOrEmpty(format))
                return true;

            if (format.Length <= 3 && char.IsLetter(format[0]) &&
                (format.Length == 1 || int.TryParse(format.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture, out _)))
            {
                int? digits = format.Length > 1 ? int.Parse(format.Substring(1), CultureInfo.InvariantCulture) : (int?)null;
                return TryStandardNumberFormat(char.ToUpperInvariant(format[0]), digits, culture, isInteger, out code);
            }
            return TryCustomNumberFormat(format, out code);
        }

        private static bool TryStandardNumberFormat(char specifier, int? digits, CultureInfo culture, bool isInteger, out string code)
        {
            var nfi = culture.NumberFormat;
            code = null;
            switch (specifier)
            {
                case 'G':
                case 'R':
                    return digits == null; // general; precision-limited G has no Excel equivalent
                case 'N':
                    code = "#,##0" + Decimals(digits ?? nfi.NumberDecimalDigits);
                    return true;
                case 'F':
                    code = "0" + Decimals(digits ?? nfi.NumberDecimalDigits);
                    return true;
                case 'P':
                    // Only patterns that put the sign before the number and % directly after it ("n%" / "-n%").
                    if (nfi.PercentPositivePattern != 1 || nfi.PercentNegativePattern != 1 || nfi.PercentSymbol != "%")
                        return false;
                    code = "0" + Decimals(digits ?? nfi.PercentDecimalDigits) + "%";
                    return true;
                case 'D':
                    if (!isInteger) return false;
                    code = new string('0', Math.Max(1, digits ?? 1));
                    return true;
                case 'E':
                    // .NET always writes a 3-digit exponent; Excel writes at least 2.
                    code = "0" + Decimals(digits ?? 6) + "E+000";
                    return true;
                case 'C':
                    return TryCurrencyFormat(digits ?? nfi.CurrencyDecimalDigits, nfi, out code);
                default:
                    return false;
            }
        }

        private static string Decimals(int digits) => digits > 0 ? "." + new string('0', digits) : "";

        private static bool TryCurrencyFormat(int digits, NumberFormatInfo nfi, out string code)
        {
            code = null;
            string n = "#,##0" + Decimals(digits);
            string sym = Quote(nfi.CurrencySymbol);
            if (sym == null) return false;

            string positive;
            switch (nfi.CurrencyPositivePattern)
            {
                case 0: positive = sym + n; break;
                case 1: positive = n + sym; break;
                case 2: positive = sym + "\" \"" + n; break;
                case 3: positive = n + "\" \"" + sym; break;
                default: return false;
            }

            string negative;
            switch (nfi.CurrencyNegativePattern)
            {
                case 0: negative = "(" + sym + n + ")"; break;
                case 1: negative = "-" + sym + n; break;
                case 2: negative = sym + "-" + n; break;
                case 3: negative = sym + n + "-"; break;
                case 4: negative = "(" + n + sym + ")"; break;
                case 5: negative = "-" + n + sym; break;
                case 6: negative = n + "-" + sym; break;
                case 7: negative = n + sym + "-"; break;
                case 8: negative = "-" + n + "\" \"" + sym; break;
                case 9: negative = "-" + sym + "\" \"" + n; break;
                case 12: negative = sym + "\" \"-" + n; break;
                case 14: negative = "(" + sym + "\" \"" + n + ")"; break;
                case 15: negative = "(" + n + "\" \"" + sym + ")"; break;
                default: return false;
            }
            code = positive + ";" + negative;
            return true;
        }

        // .NET custom numeric formats share Excel's digit placeholders, grouping, scaling, percent,
        // exponent and section syntax; everything else is written as quoted literal text.
        private static bool TryCustomNumberFormat(string format, out string code)
        {
            code = null;
            var sb = new StringBuilder();
            bool sawDigit = false;
            for (int i = 0; i < format.Length; i++)
            {
                char c = format[i];
                switch (c)
                {
                    case '0':
                    case '#':
                        sawDigit = true;
                        sb.Append(c);
                        break;
                    case '.':
                    case ',':
                    case '%':
                    case ';':
                        sb.Append(c);
                        break;
                    case 'E':
                    case 'e':
                        int j = i + 1;
                        string sign = "";
                        if (j < format.Length && (format[j] == '+' || format[j] == '-'))
                            sign = format[j++].ToString();
                        int zeros = 0;
                        while (j < format.Length && format[j] == '0') { zeros++; j++; }
                        if (zeros == 0)
                        {
                            sb.Append(Quote(c.ToString()));
                            break;
                        }
                        sb.Append("E").Append(sign == "-" ? "-" : "+").Append('0', zeros);
                        i = j - 1;
                        break;
                    case '\\':
                        if (i + 1 >= format.Length) return false;
                        var escaped = Quote(format[++i].ToString());
                        if (escaped == null) return false;
                        sb.Append(escaped);
                        break;
                    case '\'':
                    case '"':
                        int end = format.IndexOf(c, i + 1);
                        if (end < 0) return false;
                        var literal = Quote(format.Substring(i + 1, end - i - 1));
                        if (literal == null) return false;
                        sb.Append(literal);
                        i = end;
                        break;
                    case '‰': // per mille has no Excel equivalent
                        return false;
                    case '$': case '-': case '+': case '/': case '(': case ')': case ':': case ' ':
                        sb.Append(c); // shown literally by Excel without quoting
                        break;
                    default:
                        var other = Quote(c.ToString());
                        if (other == null) return false;
                        sb.Append(other);
                        break;
                }
            }
            if (!sawDigit) return false;
            code = sb.ToString();
            return true;
        }

        internal static bool TryDateFormat(string format, CultureInfo culture, out string code)
        {
            code = null;
            var dtf = culture.DateTimeFormat;
            string pattern;
            if (string.IsNullOrEmpty(format))
            {
                pattern = dtf.ShortDatePattern + " " + dtf.LongTimePattern; // DateTime.ToString() is "G"
            }
            else if (format.Length == 1)
            {
                switch (format[0])
                {
                    case 'd': pattern = dtf.ShortDatePattern; break;
                    case 'D': pattern = dtf.LongDatePattern; break;
                    case 't': pattern = dtf.ShortTimePattern; break;
                    case 'T': pattern = dtf.LongTimePattern; break;
                    case 'f': pattern = dtf.LongDatePattern + " " + dtf.ShortTimePattern; break;
                    case 'F': pattern = dtf.FullDateTimePattern; break;
                    case 'g': pattern = dtf.ShortDatePattern + " " + dtf.ShortTimePattern; break;
                    case 'G': pattern = dtf.ShortDatePattern + " " + dtf.LongTimePattern; break;
                    case 'M': case 'm': pattern = dtf.MonthDayPattern; break;
                    case 'Y': case 'y': pattern = dtf.YearMonthPattern; break;
                    case 's': pattern = dtf.SortableDateTimePattern; break;
                    default: return false; // o, r, u, U involve offsets/UTC conversion
                }
            }
            else
            {
                pattern = format.Length == 2 && format[0] == '%' ? format.Substring(1) : format;
            }
            return TryCustomDateFormat(pattern, dtf, out code);
        }

        private enum DatePart { Literal, Year, Month, Day, Hour12, Hour24, Minute, Second, Fraction, AmPm }

        private static bool TryCustomDateFormat(string pattern, DateTimeFormatInfo dtf, out string code)
        {
            code = null;
            var tokens = new List<(DatePart Part, int Count, string Text)>();
            for (int i = 0; i < pattern.Length; i++)
            {
                char c = pattern[i];
                int run = 1;
                while (i + run < pattern.Length && pattern[i + run] == c) run++;
                switch (c)
                {
                    case 'y': tokens.Add((DatePart.Year, run, null)); i += run - 1; break;
                    case 'M': tokens.Add((DatePart.Month, run, null)); i += run - 1; break;
                    case 'd': tokens.Add((DatePart.Day, run, null)); i += run - 1; break;
                    case 'h': tokens.Add((DatePart.Hour12, run, null)); i += run - 1; break;
                    case 'H': tokens.Add((DatePart.Hour24, run, null)); i += run - 1; break;
                    case 'm': tokens.Add((DatePart.Minute, run, null)); i += run - 1; break;
                    case 's': tokens.Add((DatePart.Second, run, null)); i += run - 1; break;
                    case 'f': tokens.Add((DatePart.Fraction, run, null)); i += run - 1; break;
                    case 't': tokens.Add((DatePart.AmPm, run, null)); i += run - 1; break;
                    case 'F': case 'g': case 'z': case 'K':
                        return false;
                    case '/': tokens.Add((DatePart.Literal, 1, dtf.DateSeparator)); break;
                    case ':': tokens.Add((DatePart.Literal, 1, dtf.TimeSeparator)); break;
                    case '%': break;
                    case '\\':
                        if (i + 1 >= pattern.Length) return false;
                        tokens.Add((DatePart.Literal, 1, pattern[++i].ToString()));
                        break;
                    case '\'':
                    case '"':
                        int end = pattern.IndexOf(c, i + 1);
                        if (end < 0) return false;
                        tokens.Add((DatePart.Literal, 1, pattern.Substring(i + 1, end - i - 1)));
                        i = end;
                        break;
                    default:
                        tokens.Add((DatePart.Literal, 1, c.ToString()));
                        break;
                }
            }

            bool hasAmPm = tokens.Exists(t => t.Part == DatePart.AmPm);
            // Excel's h is 12-hour exactly when an AM/PM marker is present, so .NET's h without tt
            // (or H with tt) can't be reproduced. AM/PM also has to be the culture's designators.
            if (!hasAmPm && tokens.Exists(t => t.Part == DatePart.Hour12))
                return false;
            if (hasAmPm && (tokens.Exists(t => t.Part == DatePart.Hour24) || dtf.AMDesignator != "AM" || dtf.PMDesignator != "PM"))
                return false;

            var sb = new StringBuilder();
            for (int i = 0; i < tokens.Count; i++)
            {
                var (part, count, text) = tokens[i];
                switch (part)
                {
                    case DatePart.Literal:
                        // '.' stays unquoted so "ss.fff" becomes Excel's fractional-seconds "ss.000".
                        if (text.Length == 1 && " -/:.,()".IndexOf(text[0]) >= 0)
                            sb.Append(text);
                        else if (text.Length > 0)
                        {
                            var quoted = Quote(text);
                            if (quoted == null) return false;
                            sb.Append(quoted);
                        }
                        break;
                    case DatePart.Year:
                        sb.Append(count <= 2 ? "yy" : "yyyy");
                        break;
                    case DatePart.Month:
                        // Excel reads m/mm next to an hour or seconds as minutes.
                        if (count <= 2 && IsTimeContext(tokens, i)) return false;
                        sb.Append('m', Math.Min(count, 4));
                        break;
                    case DatePart.Minute:
                        if (!IsTimeContext(tokens, i)) return false;
                        sb.Append('m', Math.Min(count, 2));
                        break;
                    case DatePart.Day:
                        sb.Append('d', Math.Min(count, 4));
                        break;
                    case DatePart.Hour12:
                    case DatePart.Hour24:
                        sb.Append('h', Math.Min(count, 2));
                        break;
                    case DatePart.Second:
                        sb.Append('s', Math.Min(count, 2));
                        break;
                    case DatePart.Fraction:
                        if (count > 3 || sb.Length == 0 || sb[sb.Length - 1] != '.' || PreviousPart(tokens, i) != DatePart.Second)
                            return false;
                        sb.Append('0', count);
                        break;
                    case DatePart.AmPm:
                        sb.Append(count == 1 ? "A/P" : "AM/PM");
                        break;
                }
            }
            code = sb.ToString();
            return true;
        }

        private static DatePart PreviousPart(List<(DatePart Part, int Count, string Text)> tokens, int index)
        {
            for (int i = index - 1; i >= 0; i--)
                if (tokens[i].Part != DatePart.Literal) return tokens[i].Part;
            return DatePart.Literal;
        }

        // Whether the nearest date/time token before is an hour, or the nearest after is seconds.
        private static bool IsTimeContext(List<(DatePart Part, int Count, string Text)> tokens, int index)
        {
            var before = PreviousPart(tokens, index);
            if (before == DatePart.Hour12 || before == DatePart.Hour24) return true;
            for (int i = index + 1; i < tokens.Count; i++)
                if (tokens[i].Part != DatePart.Literal) return tokens[i].Part == DatePart.Second;
            return false;
        }

        // Excel has no escape for '"' inside a quoted literal.
        private static string Quote(string text) =>
            text.IndexOf('"') >= 0 ? null : "\"" + text + "\"";
    }
}
