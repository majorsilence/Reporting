// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.Collections.Generic;

namespace Majorsilence.Pdf.Text
{
    /// <summary>The Unicode bidirectional character types (UAX #9, table 4).</summary>
    internal enum BidiClass : byte
    {
        L, R, AL, EN, ES, ET, AN, CS, NSM, BN, B, S, WS, ON, LRE, LRO, RLE, RLO, PDF, LRI, RLI, FSI, PDI
    }

    /// <summary>A run of text at one embedding level, in the order it is displayed.</summary>
    internal readonly struct BidiRun
    {
        internal BidiRun(int start, int length, byte level)
        {
            Start = start;
            Length = length;
            Level = level;
        }

        /// <summary>Offset of the run's first UTF-16 code unit in the text.</summary>
        internal int Start { get; }

        /// <summary>Length of the run in UTF-16 code units.</summary>
        internal int Length { get; }

        /// <summary>Resolved embedding level; odd is right to left.</summary>
        internal byte Level { get; }

        internal bool IsRightToLeft => (Level & 1) != 0;
    }

    /// <summary>
    /// The Unicode Bidirectional Algorithm (UAX #9) for one line of text: explicit embeddings,
    /// overrides and isolates (X1-X10), weak and neutral types including bracket pairs (W1-W7,
    /// N0-N2), implicit levels (I1-I2), the line-end reset (L1) and reordering (L2). Mirroring (L3/L4)
    /// is left to the shaper, or done with <see cref="Mirror"/> when there is none.
    /// </summary>
    /// <remarks>
    /// A line is resolved on its own, as a paragraph: text wrapped into several lines is resolved
    /// line by line. For a box of text with one base direction that differs from resolving the
    /// whole paragraph first only in how neutrals at the very ends of a line take their direction.
    /// </remarks>
    internal static partial class UnicodeBidi
    {
        private const int MaxDepth = 125;

        /// <summary>The bidirectional type of a code point.</summary>
        internal static BidiClass GetClass(int codePoint)
        {
            // Binary search for the last range starting at or before codePoint.
            int lo = 0, hi = RangeStarts.Length - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int first = (int)(RangeStarts[mid] >> 8);
                if (first > codePoint) hi = mid - 1;
                else if ((int)RangeEnds[mid] < codePoint) lo = mid + 1;
                else return (BidiClass)(RangeStarts[mid] & 0xFF);
            }
            return BidiClass.L;
        }

        /// <summary>
        /// Whether a line needs the algorithm at all: it has right-to-left or Arabic number
        /// characters, or explicit formatting characters. Text that is all left to right (or
        /// neutral) displays in its logical order when the base direction is left to right.
        /// </summary>
        internal static bool HasRightToLeftOrControls(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c < 0x0590) continue;       // nothing below Hebrew is R, AL or AN, or explicit
                int cp = c;
                if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                    cp = char.ConvertToUtf32(c, text[++i]);
                switch (GetClass(cp))
                {
                    case BidiClass.R: case BidiClass.AL: case BidiClass.AN:
                    case BidiClass.LRE: case BidiClass.LRO: case BidiClass.RLE: case BidiClass.RLO:
                    case BidiClass.PDF: case BidiClass.LRI: case BidiClass.RLI: case BidiClass.FSI: case BidiClass.PDI:
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The runs of <paramref name="text"/> in display order, left to right.
        /// </summary>
        /// <param name="text">One line of text.</param>
        /// <param name="rightToLeft">The base (paragraph) direction.</param>
        /// <param name="overrideDirection">
        /// Every character takes the base direction, whatever its own (Bidi-Override): the line is
        /// one run at the paragraph level.
        /// </param>
        internal static List<BidiRun> GetVisualRuns(string text, bool rightToLeft, bool overrideDirection = false)
        {
            var runs = new List<BidiRun>();
            if (string.IsNullOrEmpty(text))
                return runs;
            byte paragraphLevel = (byte)(rightToLeft ? 1 : 0);
            if (overrideDirection)
            {
                runs.Add(new BidiRun(0, text.Length, paragraphLevel));
                return runs;
            }

            // Code points, and where each one starts in the text.
            var cps = new List<int>(text.Length);
            var offsets = new List<int>(text.Length + 1);
            for (int i = 0; i < text.Length; i++)
            {
                offsets.Add(i);
                char c = text[i];
                if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    cps.Add(char.ConvertToUtf32(c, text[i + 1]));
                    i++;
                }
                else
                {
                    cps.Add(c);
                }
            }
            offsets.Add(text.Length);

            byte[] levels = ResolveLevels(cps, paragraphLevel);

            // Maximal runs of one level in logical order, then L2: from the highest level down to
            // the lowest odd one, reverse every sequence of runs at that level or above.
            var logical = new List<BidiRun>();
            int runStart = 0;
            for (int i = 1; i <= levels.Length; i++)
            {
                if (i == levels.Length || levels[i] != levels[runStart])
                {
                    logical.Add(new BidiRun(offsets[runStart], offsets[i] - offsets[runStart], levels[runStart]));
                    runStart = i;
                }
            }

            byte highest = 0, lowestOdd = byte.MaxValue;
            foreach (var r in logical)
            {
                if (r.Level > highest) highest = r.Level;
                if ((r.Level & 1) != 0 && r.Level < lowestOdd) lowestOdd = r.Level;
            }
            var order = logical.ToArray();
            for (int level = highest; level >= lowestOdd && level > 0; level--)
            {
                for (int i = 0; i < order.Length; )
                {
                    if (order[i].Level < level) { i++; continue; }
                    int j = i;
                    while (j < order.Length && order[j].Level >= level) j++;
                    Array.Reverse(order, i, j - i);
                    i = j;
                }
            }
            runs.AddRange(order);
            return runs;
        }

        /// <summary>
        /// The resolved embedding level of each code point of a line, after rule L1.
        /// </summary>
        internal static byte[] ResolveLevels(IList<int> cps, byte paragraphLevel)
        {
            int n = cps.Count;
            var original = new BidiClass[n];
            for (int i = 0; i < n; i++)
                original[i] = GetClass(cps[i]);
            var types = (BidiClass[])original.Clone();
            var levels = new byte[n];

            int[] matchingPdi = MatchIsolates(original, out int[] matchingInitiator);

            ApplyExplicit(original, types, levels, paragraphLevel, matchingPdi, cps);

            // X9: the explicit embedding and override characters and BN are removed from here on.
            var removed = new bool[n];
            for (int i = 0; i < n; i++)
            {
                switch (original[i])
                {
                    case BidiClass.RLE: case BidiClass.LRE: case BidiClass.RLO: case BidiClass.LRO:
                    case BidiClass.PDF: case BidiClass.BN:
                        removed[i] = true;
                        break;
                }
            }

            // sos and eos come from the explicit levels, which resolving a sequence changes.
            var explicitLevels = (byte[])levels.Clone();
            foreach (var seq in IsolatingRunSequences(explicitLevels, original, removed, matchingPdi, matchingInitiator))
                ResolveSequence(seq, cps, original, types, explicitLevels, levels, removed, paragraphLevel);

            // The removed characters take the level of the character before them, so they stay
            // inside the run they were typed in.
            for (int i = 0; i < n; i++)
                if (removed[i])
                    levels[i] = i > 0 ? levels[i - 1] : paragraphLevel;

            // L1: segment separators, paragraph separators and the whitespace (and isolate
            // formatting characters) before them or at the end of the line go to the paragraph level.
            bool trailing = true;
            for (int i = n - 1; i >= 0; i--)
            {
                BidiClass t = original[i];
                if (t == BidiClass.S || t == BidiClass.B)
                {
                    levels[i] = paragraphLevel;
                    trailing = true;
                }
                else if (trailing && (t == BidiClass.WS || IsIsolateControl(t) || removed[i]))
                {
                    levels[i] = paragraphLevel;
                }
                else
                {
                    trailing = false;
                }
            }
            return levels;
        }

        /// <summary>P2/P3: the paragraph level its first strong character gives (0 when it has none).</summary>
        internal static byte FirstStrongLevel(IList<int> cps)
        {
            var t = new BidiClass[cps.Count];
            for (int i = 0; i < t.Length; i++) t[i] = GetClass(cps[i]);
            return FirstStrongLevel(t, 0, t.Length, MatchIsolates(t, out _), 0);
        }

        private static bool IsIsolateInitiator(BidiClass t) => t == BidiClass.LRI || t == BidiClass.RLI || t == BidiClass.FSI;

        private static bool IsIsolateControl(BidiClass t) => IsIsolateInitiator(t) || t == BidiClass.PDI;

        // BD9: the PDI matching each isolate initiator (-1 for none), and the initiator matching each PDI.
        private static int[] MatchIsolates(BidiClass[] t, out int[] matchingInitiator)
        {
            int n = t.Length;
            var pdi = new int[n];
            matchingInitiator = new int[n];
            for (int i = 0; i < n; i++) { pdi[i] = -1; matchingInitiator[i] = -1; }
            var open = new Stack<int>();
            for (int i = 0; i < n; i++)
            {
                if (IsIsolateInitiator(t[i]))
                {
                    open.Push(i);
                }
                else if (t[i] == BidiClass.PDI && open.Count > 0)
                {
                    int init = open.Pop();
                    pdi[init] = i;
                    matchingInitiator[i] = init;
                }
                else if (t[i] == BidiClass.B)
                {
                    open.Clear();
                }
            }
            return pdi;
        }

        // P2/P3 over text[start, end): 1 when its first strong character (skipping isolates) is R or AL.
        private static byte FirstStrongLevel(BidiClass[] t, int start, int end, int[] matchingPdi, byte fallback)
        {
            for (int i = start; i < end; i++)
            {
                switch (t[i])
                {
                    case BidiClass.L: return 0;
                    case BidiClass.R: case BidiClass.AL: return 1;
                    case BidiClass.LRI: case BidiClass.RLI: case BidiClass.FSI:
                        if (matchingPdi[i] < 0) return fallback;
                        i = matchingPdi[i];
                        break;
                    case BidiClass.B: return fallback;
                }
            }
            return fallback;
        }

        private struct StackEntry
        {
            internal byte Level;
            internal BidiClass Override;   // ON for none
            internal bool Isolate;
        }

        // X1-X8.
        private static void ApplyExplicit(BidiClass[] original, BidiClass[] types, byte[] levels, byte paragraphLevel,
            int[] matchingPdi, IList<int> cps)
        {
            var stack = new List<StackEntry>(8) { new StackEntry { Level = paragraphLevel, Override = BidiClass.ON } };
            int overflowIsolates = 0, overflowEmbeddings = 0, validIsolates = 0;

            for (int i = 0; i < original.Length; i++)
            {
                StackEntry top = stack[stack.Count - 1];
                BidiClass t = original[i];
                switch (t)
                {
                    case BidiClass.RLE: case BidiClass.LRE: case BidiClass.RLO: case BidiClass.LRO:
                    {
                        bool rtl = t == BidiClass.RLE || t == BidiClass.RLO;
                        int newLevel = rtl ? (top.Level + 1) | 1 : (top.Level + 2) & ~1;
                        levels[i] = top.Level;
                        if (newLevel <= MaxDepth && overflowIsolates == 0 && overflowEmbeddings == 0)
                        {
                            stack.Add(new StackEntry
                            {
                                Level = (byte)newLevel,
                                Override = t == BidiClass.RLO ? BidiClass.R : t == BidiClass.LRO ? BidiClass.L : BidiClass.ON,
                            });
                        }
                        else if (overflowIsolates == 0)
                        {
                            overflowEmbeddings++;
                        }
                        break;
                    }
                    case BidiClass.RLI: case BidiClass.LRI: case BidiClass.FSI:
                    {
                        levels[i] = top.Level;
                        if (top.Override != BidiClass.ON)
                            types[i] = top.Override;
                        bool rtl = t == BidiClass.RLI;
                        if (t == BidiClass.FSI)
                        {
                            int end = matchingPdi[i] < 0 ? original.Length : matchingPdi[i];
                            rtl = FirstStrongLevel(original, i + 1, end, matchingPdi, 0) == 1;
                        }
                        int newLevel = rtl ? (top.Level + 1) | 1 : (top.Level + 2) & ~1;
                        if (newLevel <= MaxDepth && overflowIsolates == 0 && overflowEmbeddings == 0)
                        {
                            validIsolates++;
                            stack.Add(new StackEntry { Level = (byte)newLevel, Override = BidiClass.ON, Isolate = true });
                        }
                        else
                        {
                            overflowIsolates++;
                        }
                        break;
                    }
                    case BidiClass.PDI:
                    {
                        if (overflowIsolates > 0)
                        {
                            overflowIsolates--;
                        }
                        else if (validIsolates > 0)
                        {
                            overflowEmbeddings = 0;
                            while (!stack[stack.Count - 1].Isolate)
                                stack.RemoveAt(stack.Count - 1);
                            stack.RemoveAt(stack.Count - 1);
                            validIsolates--;
                        }
                        top = stack[stack.Count - 1];
                        levels[i] = top.Level;
                        if (top.Override != BidiClass.ON)
                            types[i] = top.Override;
                        break;
                    }
                    case BidiClass.PDF:
                    {
                        if (overflowIsolates == 0)
                        {
                            if (overflowEmbeddings > 0)
                                overflowEmbeddings--;
                            else if (!top.Isolate && stack.Count >= 2)
                                stack.RemoveAt(stack.Count - 1);
                        }
                        levels[i] = top.Level;
                        break;
                    }
                    case BidiClass.B:
                        levels[i] = paragraphLevel;
                        break;
                    case BidiClass.BN:
                        levels[i] = top.Level;
                        break;
                    default:
                        levels[i] = top.Level;
                        if (top.Override != BidiClass.ON)
                            types[i] = top.Override;
                        break;
                }
            }
        }

        // BD13 / X10: level runs (ignoring removed characters) chained across isolates.
        private static List<List<int>> IsolatingRunSequences(byte[] levels, BidiClass[] original, bool[] removed,
            int[] matchingPdi, int[] matchingInitiator)
        {
            int n = levels.Length;
            var runs = new List<List<int>>();
            List<int>? current = null;
            int currentLevel = -1;
            for (int i = 0; i < n; i++)
            {
                if (removed[i]) continue;
                if (current == null || levels[i] != currentLevel)
                {
                    current = new List<int>();
                    runs.Add(current);
                    currentLevel = levels[i];
                }
                current.Add(i);
            }

            var runOfIndex = new Dictionary<int, List<int>>();
            foreach (var r in runs)
                runOfIndex[r[0]] = r;

            var sequences = new List<List<int>>();
            foreach (var r in runs)
            {
                // A run that starts with a PDI matching an initiator is part of that initiator's sequence.
                int first = r[0];
                if (original[first] == BidiClass.PDI && matchingInitiator[first] >= 0)
                    continue;

                var seq = new List<int>(r);
                var last = r;
                while (true)
                {
                    int end = last[last.Count - 1];
                    if (!IsIsolateInitiator(original[end]) || matchingPdi[end] < 0)
                        break;
                    int pdi = matchingPdi[end];
                    if (!runOfIndex.TryGetValue(pdi, out var next))
                        break;
                    seq.AddRange(next);
                    last = next;
                }
                sequences.Add(seq);
            }
            return sequences;
        }

        private static BidiClass Direction(int level) => (level & 1) != 0 ? BidiClass.R : BidiClass.L;

        private static void ResolveSequence(List<int> seq, IList<int> cps, BidiClass[] original, BidiClass[] types,
            byte[] explicitLevels, byte[] levels, bool[] removed, byte paragraphLevel)
        {
            int n = seq.Count;
            int level = explicitLevels[seq[0]];

            // sos and eos: the higher of this level and the level beside the sequence.
            int before = seq[0] - 1;
            while (before >= 0 && removed[before]) before--;
            int beforeLevel = before >= 0 ? explicitLevels[before] : paragraphLevel;
            BidiClass sos = Direction(Math.Max(level, beforeLevel));

            int lastIndex = seq[n - 1];
            int afterLevel;
            if (IsIsolateInitiator(original[lastIndex]))
            {
                afterLevel = paragraphLevel;
            }
            else
            {
                int after = lastIndex + 1;
                while (after < levels.Length && removed[after]) after++;
                afterLevel = after < levels.Length ? explicitLevels[after] : paragraphLevel;
            }
            BidiClass eos = Direction(Math.Max(level, afterLevel));

            var t = new BidiClass[n];
            for (int i = 0; i < n; i++) t[i] = types[seq[i]];

            // W1: a non-spacing mark takes the type before it (ON after an isolate control).
            for (int i = 0; i < n; i++)
            {
                if (t[i] != BidiClass.NSM) continue;
                if (i == 0) t[i] = sos;
                else t[i] = IsIsolateControl(t[i - 1]) ? BidiClass.ON : t[i - 1];
            }

            // W2: European numbers after Arabic letters are Arabic numbers.  W3: AL is R.
            BidiClass lastStrong = sos;
            for (int i = 0; i < n; i++)
            {
                BidiClass c = t[i];
                if (c == BidiClass.EN) { if (lastStrong == BidiClass.AL) t[i] = BidiClass.AN; }
                else if (c == BidiClass.L || c == BidiClass.R || c == BidiClass.AL) lastStrong = c;
            }
            for (int i = 0; i < n; i++)
                if (t[i] == BidiClass.AL) t[i] = BidiClass.R;

            // W4: one separator between two numbers of a kind joins them.
            for (int i = 1; i < n - 1; i++)
            {
                if (t[i] == BidiClass.ES && t[i - 1] == BidiClass.EN && t[i + 1] == BidiClass.EN)
                    t[i] = BidiClass.EN;
                else if (t[i] == BidiClass.CS && t[i - 1] == BidiClass.EN && t[i + 1] == BidiClass.EN)
                    t[i] = BidiClass.EN;
                else if (t[i] == BidiClass.CS && t[i - 1] == BidiClass.AN && t[i + 1] == BidiClass.AN)
                    t[i] = BidiClass.AN;
            }

            // W5: terminators (currency, percent ...) beside a European number are part of it.
            for (int i = 0; i < n; i++)
            {
                if (t[i] != BidiClass.ET) continue;
                int j = i;
                while (j < n && t[j] == BidiClass.ET) j++;
                bool en = (i > 0 && t[i - 1] == BidiClass.EN) || (j < n && t[j] == BidiClass.EN);
                if (en)
                    for (int k = i; k < j; k++) t[k] = BidiClass.EN;
                i = j - 1;
            }

            // W6: the separators and terminators left are neutral.
            for (int i = 0; i < n; i++)
                if (t[i] == BidiClass.ES || t[i] == BidiClass.ET || t[i] == BidiClass.CS)
                    t[i] = BidiClass.ON;

            // W7: European numbers in left-to-right context are L.
            lastStrong = sos;
            for (int i = 0; i < n; i++)
            {
                BidiClass c = t[i];
                if (c == BidiClass.EN) { if (lastStrong == BidiClass.L) t[i] = BidiClass.L; }
                else if (c == BidiClass.L || c == BidiClass.R) lastStrong = c;
            }

            ResolveBrackets(seq, cps, original, t, level, sos);

            // N1/N2: neutrals between two strong types of one direction take it (numbers count as
            // R); the others take the embedding direction.
            BidiClass embedding = Direction(level);
            for (int i = 0; i < n; i++)
            {
                if (!IsNeutral(t[i])) continue;
                int j = i;
                while (j < n && IsNeutral(t[j])) j++;
                BidiClass leading = i == 0 ? sos : StrongOf(t[i - 1]);
                BidiClass trailing = j == n ? eos : StrongOf(t[j]);
                BidiClass resolved = leading == trailing ? leading : embedding;
                for (int k = i; k < j; k++) t[k] = resolved;
                i = j - 1;
            }

            // I1/I2.
            for (int i = 0; i < n; i++)
            {
                int idx = seq[i];
                BidiClass c = t[i];
                if ((level & 1) == 0)
                {
                    if (c == BidiClass.R) levels[idx] = (byte)(level + 1);
                    else if (c == BidiClass.AN || c == BidiClass.EN) levels[idx] = (byte)(level + 2);
                    else levels[idx] = (byte)level;
                }
                else
                {
                    if (c == BidiClass.L || c == BidiClass.EN || c == BidiClass.AN) levels[idx] = (byte)(level + 1);
                    else levels[idx] = (byte)level;
                }
                types[idx] = c;
            }
        }

        private static bool IsNeutral(BidiClass t) =>
            t == BidiClass.B || t == BidiClass.S || t == BidiClass.WS || t == BidiClass.ON || IsIsolateControl(t);

        // The direction a resolved type counts as for N1: numbers are R.
        private static BidiClass StrongOf(BidiClass t) =>
            t == BidiClass.L ? BidiClass.L : BidiClass.R;

        // N0: paired brackets take the direction of the strong text inside them, or of the text
        // before them when what is inside runs against the embedding direction.
        private static void ResolveBrackets(List<int> seq, IList<int> cps, BidiClass[] original, BidiClass[] t,
            int level, BidiClass sos)
        {
            int n = seq.Count;
            var pairs = new List<(int open, int close)>();
            var openers = new List<(int pos, int closing)>();
            for (int i = 0; i < n; i++)
            {
                if (t[i] != BidiClass.ON) continue;
                int cp = CanonicalBracket(cps[seq[i]]);
                if (OpeningToClosing.TryGetValue(cp, out int closing))
                {
                    if (openers.Count == 63) break;     // BD16: stop at 63 open brackets
                    openers.Add((i, closing));
                }
                else if (ClosingBrackets.Contains(cp))
                {
                    for (int k = openers.Count - 1; k >= 0; k--)
                    {
                        if (openers[k].closing != cp) continue;
                        pairs.Add((openers[k].pos, i));
                        openers.RemoveRange(k, openers.Count - k);
                        break;
                    }
                }
            }
            if (pairs.Count == 0) return;
            pairs.Sort((a, b) => a.open.CompareTo(b.open));

            BidiClass embedding = Direction(level);
            BidiClass opposite = embedding == BidiClass.L ? BidiClass.R : BidiClass.L;
            foreach (var (open, close) in pairs)
            {
                bool hasEmbedding = false, hasOpposite = false;
                for (int i = open + 1; i < close; i++)
                {
                    BidiClass s = StrongOrNone(t[i]);
                    if (s == embedding) { hasEmbedding = true; break; }
                    if (s == opposite) hasOpposite = true;
                }

                BidiClass resolved;
                if (hasEmbedding)
                {
                    resolved = embedding;
                }
                else if (hasOpposite)
                {
                    BidiClass context = sos;
                    for (int i = open - 1; i >= 0; i--)
                    {
                        BidiClass s = StrongOrNone(t[i]);
                        if (s != BidiClass.ON) { context = s; break; }
                    }
                    resolved = context == opposite ? opposite : embedding;
                }
                else
                {
                    continue;
                }

                t[open] = resolved;
                t[close] = resolved;
                // Marks on a bracket follow it.
                for (int i = open + 1; i < n && original[seq[i]] == BidiClass.NSM; i++) t[i] = resolved;
                for (int i = close + 1; i < n && original[seq[i]] == BidiClass.NSM; i++) t[i] = resolved;
            }
        }

        // L or R for a strong type (numbers counting as R), ON for anything else.
        private static BidiClass StrongOrNone(BidiClass t)
        {
            switch (t)
            {
                case BidiClass.L: return BidiClass.L;
                case BidiClass.R: case BidiClass.AL: case BidiClass.EN: case BidiClass.AN: return BidiClass.R;
                default: return BidiClass.ON;
            }
        }

        // The paired brackets of BidiBrackets.txt (Unicode 16), opening to closing.
        private static readonly Dictionary<int, int> OpeningToClosing = BuildBracketPairs();
        private static readonly HashSet<int> ClosingBrackets = new HashSet<int>(OpeningToClosing.Values);

        private static Dictionary<int, int> BuildBracketPairs()
        {
            int[] pairs =
            {
                0x0028, 0x0029, 0x005B, 0x005D, 0x007B, 0x007D, 0x0F3A, 0x0F3B, 0x0F3C, 0x0F3D, 0x169B, 0x169C,
                0x2045, 0x2046, 0x207D, 0x207E, 0x208D, 0x208E, 0x2308, 0x2309, 0x230A, 0x230B, 0x2768, 0x2769,
                0x276A, 0x276B, 0x276C, 0x276D, 0x276E, 0x276F, 0x2770, 0x2771, 0x2772, 0x2773, 0x2774, 0x2775,
                0x27C5, 0x27C6, 0x27E6, 0x27E7, 0x27E8, 0x27E9, 0x27EA, 0x27EB, 0x27EC, 0x27ED, 0x27EE, 0x27EF,
                0x2983, 0x2984, 0x2985, 0x2986, 0x2987, 0x2988, 0x2989, 0x298A, 0x298B, 0x298C, 0x298D, 0x2990,
                0x298F, 0x298E, 0x2991, 0x2992, 0x2993, 0x2994, 0x2995, 0x2996, 0x2997, 0x2998, 0x29D8, 0x29D9,
                0x29DA, 0x29DB, 0x29FC, 0x29FD, 0x2E22, 0x2E23, 0x2E24, 0x2E25, 0x2E26, 0x2E27, 0x2E28, 0x2E29,
                0x2E55, 0x2E56, 0x2E57, 0x2E58, 0x2E59, 0x2E5A, 0x2E5B, 0x2E5C, 0x3008, 0x3009, 0x300A, 0x300B,
                0x300C, 0x300D, 0x300E, 0x300F, 0x3010, 0x3011, 0x3014, 0x3015, 0x3016, 0x3017, 0x3018, 0x3019,
                0x301A, 0x301B, 0xFE59, 0xFE5A, 0xFE5B, 0xFE5C, 0xFE5D, 0xFE5E, 0xFF08, 0xFF09, 0xFF3B, 0xFF3D,
                0xFF5B, 0xFF5D, 0xFF5F, 0xFF60, 0xFF62, 0xFF63,
            };
            var map = new Dictionary<int, int>(pairs.Length / 2);
            for (int i = 0; i < pairs.Length; i += 2)
                map[pairs[i]] = pairs[i + 1];
            return map;
        }

        // U+2329/U+232A are canonically the same brackets as U+3008/U+3009.
        private static int CanonicalBracket(int cp) => cp == 0x2329 ? 0x3008 : cp == 0x232A ? 0x3009 : cp;

        /// <summary>
        /// The mirrored form of a character drawn right to left (L4), for text drawn without a
        /// shaper: brackets and the common paired symbols. A shaper mirrors on its own.
        /// </summary>
        internal static int Mirror(int cp)
        {
            if (OpeningToClosing.TryGetValue(cp, out int closing)) return closing;
            foreach (var kv in OpeningToClosing)
                if (kv.Value == cp) return kv.Key;
            switch (cp)
            {
                case '<': return '>';
                case '>': return '<';
                case 0x00AB: return 0x00BB;
                case 0x00BB: return 0x00AB;
                case 0x2039: return 0x203A;
                case 0x203A: return 0x2039;
                case 0x2264: return 0x2265;
                case 0x2265: return 0x2264;
                case 0x2329: return 0x232A;
                case 0x232A: return 0x2329;
                default: return cp;
            }
        }
    }
}
