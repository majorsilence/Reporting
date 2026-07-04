// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.
//
// Compatibility shim for fernandreu.ScintillaNET (the WinForms-only Scintilla code-editor
// control), which has no equivalent in Majorsilence.Forms -- only a plain RichTextBox exists.
// This is a RichTextBox subclass exposing the subset of the real Scintilla API surface that
// RdlDesign actually uses (see MIGRATION-NOTES.md's D4 catalogue), so the calling code
// (RdlEditPreview.cs, DialogExprEditor.cs, DataSetsCtl.cs, Syntax/*.cs) keeps compiling with
// minimal changes.
//
// `reporting.map.json` maps the whole `ScintillaNET` namespace to
// `Majorsilence.Reporting.RdlDesign.Syntax` (this namespace), so `ScintillaNET.Scintilla` becomes
// `Majorsilence.Reporting.RdlDesign.Syntax.Scintilla` post-migration -- hence this class is named
// `Scintilla`, not `ScintillaCompat` (the file is named ScintillaCompat.cs for clarity, but the
// class itself has to keep the original type name for the namespace-prefix rewrite to work).
//
// D4 scope: text editing (Text, undo/redo, selection, search-in-target) is real, backed by the
// underlying RichTextBox. Everything styling/lexer/margin/marker/indicator-related is a
// documented no-op -- see D5 ("ScintillaCompat made real") for actually wiring up syntax
// colorization via Majorsilence.Forms.RichTextBox's SelectionColor/SelectionFont per-span API.

using System;
using System.Collections.Generic;
using Majorsilence.Forms;

namespace Majorsilence.Reporting.RdlDesign.Syntax
{
    public class Scintilla : RichTextBox
    {
        public const int InvalidPosition = -1;

        private readonly List<string> _undoStack = new();
        private readonly List<string> _redoStack = new();
        private bool _suppressUndoCapture;
        private string _lastCapturedText = string.Empty;

        public Scintilla()
        {
            base.TextChanged += OnScintillaTextChanged;
        }

        private void OnScintillaTextChanged(object sender, EventArgs e)
        {
            if (_suppressUndoCapture) return;
            _undoStack.Add(_lastCapturedText);
            _redoStack.Clear();
            _lastCapturedText = Text;
        }

        // --- Real: text-editing core -------------------------------------------------------

        public new int TextLength => Text.Length;

        public bool Modified { get; set; }

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public void Undo()
        {
            if (!CanUndo) return;
            _redoStack.Add(Text);
            _suppressUndoCapture = true;
            Text = _undoStack[^1];
            _undoStack.RemoveAt(_undoStack.Count - 1);
            _lastCapturedText = Text;
            _suppressUndoCapture = false;
        }

        public void Redo()
        {
            if (!CanRedo) return;
            _undoStack.Add(Text);
            _suppressUndoCapture = true;
            Text = _redoStack[^1];
            _redoStack.RemoveAt(_redoStack.Count - 1);
            _lastCapturedText = Text;
            _suppressUndoCapture = false;
        }

        public void EmptyUndoBuffer()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        public void SetSavePoint() => Modified = false;

        public void ClearSelections() => SelectionLength = 0;

        // --- Real: selection / caret / position --------------------------------------------

        public int CurrentPosition
        {
            get => SelectionStart;
            set => SelectionStart = value;
        }

        public int CurrentLine => GetLineFromCharIndex(SelectionStart);

        public int GetColumn(int position)
        {
            int line = GetLineFromCharIndex(position);
            int lineStart = GetFirstCharIndexFromLine(line);
            return position - lineStart;
        }

        public void GotoPosition(int position)
        {
            SelectionStart = Math.Clamp(position, 0, Text.Length);
            SelectionLength = 0;
        }

        // --- Real: search/replace (target range) --------------------------------------------

        public SearchFlags SearchFlags { get; set; } = SearchFlags.None;
        public int TargetStart { get; set; }
        public int TargetEnd { get; set; }

        public int SearchInTarget(string text)
        {
            if (string.IsNullOrEmpty(text) || TargetStart < 0 || TargetEnd > Text.Length || TargetStart > TargetEnd)
                return InvalidPosition;

            var comparison = (SearchFlags & SearchFlags.MatchCase) != 0
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            string haystack = Text.Substring(TargetStart, TargetEnd - TargetStart);
            int idx = haystack.IndexOf(text, comparison);
            if (idx < 0) return InvalidPosition;

            int found = TargetStart + idx;
            TargetStart = found;
            TargetEnd = found + text.Length;
            return found;
        }

        public void ReplaceTarget(string text)
        {
            if (TargetStart < 0 || TargetEnd > Text.Length || TargetStart > TargetEnd) return;
            string current = Text;
            Text = current.Substring(0, TargetStart) + text + current.Substring(TargetEnd);
            TargetEnd = TargetStart + text.Length;
        }

        public new void ReplaceSelection(string text)
        {
            int start = SelectionStart;
            string current = Text;
            int len = SelectionLength;
            Text = current.Substring(0, start) + text + current.Substring(start + len);
            SelectionStart = start + text.Length;
            SelectionLength = 0;
        }

        // --- Real: Lines collection ----------------------------------------------------------

        public LineCollection Lines => new LineCollection(this);

        public sealed class LineCollection
        {
            private readonly Scintilla _owner;
            internal LineCollection(Scintilla owner) => _owner = owner;

            public int Count => _owner.GetLineFromCharIndex(_owner.Text.Length) + 1;

            public Line this[int index] => new Line(_owner, index);
        }

        public sealed class Line
        {
            private readonly Scintilla _owner;
            private readonly int _index;
            internal Line(Scintilla owner, int index)
            {
                _owner = owner;
                _index = index;
            }

            public int Position => _owner.GetFirstCharIndexFromLine(_index);

            public int EndPosition
            {
                get
                {
                    int next = _owner.GetFirstCharIndexFromLine(_index + 1);
                    return next >= 0 ? next : _owner.Text.Length;
                }
            }

            public void Goto() => _owner.GotoPosition(Position);
        }

        // --- Cosmetic: lexer/styling/margins/markers/indicators -- all no-op ----------------

        public Lexer Lexer { get; set; }
        public new bool UseTabs { get; set; }
        public bool HScrollBar { get; set; }
        public WrapMode WrapMode { get; set; }
        public IndentView IndentationGuides { get; set; }
        public int HighlightGuide { get; set; }
        public AutomaticFold AutomaticFold { get; set; }

        public void StyleResetDefault() { }
        public void StyleClearAll() { }
        public void SetKeywords(int keywordSet, string keywords) { }
        public void SetProperty(string name, string value) { }

        public int BraceMatch(int position) => InvalidPosition;
        public void BraceBadLight(int position) { }
        public void BraceHighlight(int position1, int position2) { }

        public int GetCharAt(int position) => position >= 0 && position < Text.Length ? Text[position] : -1;
        public int LineFromPosition(int position) => GetLineFromCharIndex(Math.Clamp(position, 0, Math.Max(Text.Length - 1, 0)));
        public string GetTextRange(int position, int length)
        {
            if (position < 0 || length <= 0 || position >= Text.Length) return string.Empty;
            return Text.Substring(position, Math.Min(length, Text.Length - position));
        }
        public void StartStyling(int position) { }
        public void SetStyling(int length, int style) { }
        public int GetEndStyled() => 0;

        public IndicatorCollection Indicators { get; } = new IndicatorCollection();
        public int IndicatorCurrent { get; set; }
        public void IndicatorClearRange(int position, int length) { }
        public void IndicatorFillRange(int position, int length) { }

        public MarginCollection Margins { get; } = new MarginCollection();
        public StyleCollection Styles { get; } = new StyleCollection();
        public MarkerCollection Markers { get; } = new MarkerCollection();

        public event EventHandler<StyleNeededEventArgs>? StyleNeeded;
        public event EventHandler<UpdateUIEventArgs>? UpdateUI;

        // Referenced so the compiler doesn't warn these events are "never used" -- they exist
        // purely for source compatibility; real syntax-highlighting/brace-match-on-caret-move
        // wiring is D5's job.
        private void RaiseStyleNeeded(int position) => StyleNeeded?.Invoke(this, new StyleNeededEventArgs(position));
        private void RaiseUpdateUI(UpdateChange change) => UpdateUI?.Invoke(this, new UpdateUIEventArgs(change));
    }

    // --- Stub collection/item types for the cosmetic (no-op) API surface -----------------------

    public sealed class IndicatorCollection
    {
        private readonly Dictionary<int, Indicator> _items = new();
        public Indicator this[int index]
        {
            get
            {
                if (!_items.TryGetValue(index, out var item))
                    _items[index] = item = new Indicator();
                return item;
            }
        }
    }

    public sealed class Indicator
    {
        public IndicatorStyle Style { get; set; }
        public bool Under { get; set; }
        public System.Drawing.Color ForeColor { get; set; }
        public int OutlineAlpha { get; set; }
        public int Alpha { get; set; }
    }

    public sealed class MarginCollection
    {
        private readonly Dictionary<int, Margin> _items = new();
        public Margin this[int index]
        {
            get
            {
                if (!_items.TryGetValue(index, out var item))
                    _items[index] = item = new Margin();
                return item;
            }
        }
    }

    public sealed class Margin
    {
        public int Width { get; set; }
        public MarginType Type { get; set; }
        public uint Mask { get; set; }
        public bool Sensitive { get; set; }
    }

    public sealed class StyleCollection
    {
        private readonly Dictionary<int, Style> _items = new();
        public Style this[int index]
        {
            get
            {
                if (!_items.TryGetValue(index, out var item))
                    _items[index] = item = new Style();
                return item;
            }
        }
    }

    public sealed class Style
    {
        public System.Drawing.Color ForeColor { get; set; }
        public System.Drawing.Color BackColor { get; set; }
        public bool Underline { get; set; }
        public bool Bold { get; set; }
        public string? Font { get; set; }
        public int Size { get; set; }

        public const int Default = 32;
        public const int LineNumber = 33;
        public const int BraceLight = 34;
        public const int BraceBad = 35;

        public static class Sql
        {
            public const int Comment = 1, CommentLine = 2, CommentLineDoc = 3, Number = 4, Word = 5,
                Word2 = 6, User1 = 7, User2 = 8, String = 9, Character = 10, Operator = 11;
        }

        public static class Xml
        {
            public const int Attribute = 1, Entity = 2, Comment = 3, Tag = 4, TagEnd = 5,
                DoubleString = 6, SingleString = 7;
        }
    }

    public sealed class MarkerCollection
    {
        private readonly Dictionary<int, Marker> _items = new();
        public Marker this[int index]
        {
            get
            {
                if (!_items.TryGetValue(index, out var item))
                    _items[index] = item = new Marker();
                return item;
            }
        }
    }

    public sealed class Marker
    {
        public MarkerSymbol Symbol { get; set; }
        public void SetForeColor(System.Drawing.Color color) { }
        public void SetBackColor(System.Drawing.Color color) { }

        public const uint MaskFolders = 0xFE000000;
        public const int Folder = 25, FolderOpenMid = 26, FolderMidTail = 27, FolderTail = 28,
            FolderSub = 29, FolderOpen = 30, FolderEnd = 31;
    }

    // --- Stub enums / event-args types matching what RdlDesign actually references --------------

    public enum Lexer { Container, Sql, Xml, VbScript }
    public enum WrapMode { None, Word, Char, WhiteSpace }
    public enum IndentView { None, Real, LookForward, LookBoth }
    [Flags] public enum AutomaticFold { None = 0, Show = 1, Click = 2, Change = 4 }
    public enum MarginType { Symbol, Number, BackColor, RText, Color }
    public enum MarkerSymbol { Circle, BoxPlus, BoxMinus, BoxPlusConnected, BoxMinusConnected, VLine, LCorner, TCorner }
    public enum IndicatorStyle { Plain, StraightBox }
    [Flags] public enum SearchFlags { None = 0, MatchCase = 1 }
    [Flags] public enum UpdateChange { None = 0, Content = 1, Selection = 2 }

    public sealed class StyleNeededEventArgs : EventArgs
    {
        public int Position { get; }
        public StyleNeededEventArgs(int position) => Position = position;
    }

    public sealed class UpdateUIEventArgs : EventArgs
    {
        public UpdateChange Change { get; }
        public UpdateUIEventArgs(UpdateChange change) => Change = change;
    }
}
