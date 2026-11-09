using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Majorsilence.Spreadsheet.Xls;

namespace Majorsilence.Spreadsheet
{
    /// <summary>
    /// Writes the legacy Excel 97-2003 binary format (.xls: BIFF8 in an OLE2 compound file), with the
    /// same API shape as <see cref="XlsxWriter"/> for the features BIFF8 shares: text, numbers, booleans,
    /// dates, cell formats (fonts, fills, borders, alignment, number formats, protection flags), merges,
    /// column widths, row heights, outlining, hidden rows/columns, frozen panes, print setup and
    /// multiple sheets. BIFF8 limits apply: 65,536 rows, 256 columns, about 4,000 distinct cell formats,
    /// 56 colours (others map to the nearest) and 31-character sheet names.
    /// </summary>
    public sealed class XlsWriter
    {
        public const int MaxRows = 65536;
        public const int MaxColumns = 256;
        private const int MaxCellXfs = 4000;
        private const int MaxStringLength = 32767;
        private const int FirstCustomFormat = 164;
        private const int DefaultCellXf = 15;
        private static readonly DateTime ExcelEpoch = new DateTime(1899, 12, 30);

        private readonly List<Sheet> _sheets = new List<Sheet>();
        private readonly List<string> _sst = new List<string>();
        private readonly Dictionary<string, int> _sstIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        private int _sstTotal;

        private readonly List<FontKey> _fonts = new List<FontKey>();
        private readonly List<string> _formats = new List<string>();
        private readonly List<XfKey> _xfs = new List<XfKey>();
        private readonly Dictionary<XlsxCellFormat, int> _xfByFormat = new Dictionary<XlsxCellFormat, int>(ReferenceEqualityComparer.Instance);
        private readonly int[] _palette = (int[])DefaultPalette.Clone();
        private readonly bool[] _paletteUsed = new bool[56];

        public XlsWriter(string sheetName)
        {
            AddSheet(sheetName);
        }

        /// <summary>Adds a worksheet and returns its index. The name is cut to 31 characters, with []:*?/\ replaced, and made unique.</summary>
        public int AddSheet(string sheetName)
        {
            string name = string.IsNullOrEmpty(sheetName) ? "Sheet" + (_sheets.Count + 1) : sheetName;
            var sb = new StringBuilder(name.Length);
            foreach (char c in name) sb.Append("[]:*?/\\".IndexOf(c) >= 0 ? '_' : c);
            name = sb.ToString().Trim('\'');
            if (name.Length == 0) name = "Sheet" + (_sheets.Count + 1);
            if (name.Length > 31) name = name.Substring(0, 31);
            string unique = name;
            for (int n = 2; _sheets.Exists(s => string.Equals(s.Name, unique, StringComparison.OrdinalIgnoreCase)); n++)
            {
                string suffix = " (" + n + ")";
                unique = (name.Length + suffix.Length > 31 ? name.Substring(0, 31 - suffix.Length) : name) + suffix;
            }
            _sheets.Add(new Sheet(unique));
            return _sheets.Count - 1;
        }

        public string GetSheetName(int sheetIndex) => _sheets[sheetIndex].Name;

        public void SetColumnWidth(int colIndex, double widthInChars, int sheetIndex = 0)
            => _sheets[sheetIndex].Column(CheckCol(colIndex)).Width = widthInChars;

        public void SetRowHeight(int rowIndex, double heightInPoints, int sheetIndex = 0)
            => _sheets[sheetIndex].Row(CheckRow(rowIndex)).Height = heightInPoints;

        public void SetColumnOutlineLevel(int colIndex, int level, int sheetIndex = 0)
            => _sheets[sheetIndex].Column(CheckCol(colIndex)).Level = Math.Max(0, Math.Min(7, level));

        public void SetRowOutlineLevel(int rowIndex, int level, int sheetIndex = 0)
            => _sheets[sheetIndex].Row(CheckRow(rowIndex)).Level = Math.Max(0, Math.Min(7, level));

        public void SetColumnHidden(int colIndex, bool hidden, int sheetIndex = 0)
            => _sheets[sheetIndex].Column(CheckCol(colIndex)).Hidden = hidden;

        public void SetRowHidden(int rowIndex, bool hidden, int sheetIndex = 0)
            => _sheets[sheetIndex].Row(CheckRow(rowIndex)).Hidden = hidden;

        public void SetCell(int rowIndex, int colIndex, string value, XlsxCellFormat? format = null, int sheetIndex = 0)
        {
            string v = value ?? "";
            if (v.Length > MaxStringLength) v = v.Substring(0, MaxStringLength);
            if (!_sstIndex.TryGetValue(v, out int idx))
            {
                idx = _sst.Count;
                _sst.Add(v);
                _sstIndex[v] = idx;
            }
            Put(sheetIndex, rowIndex, colIndex, new Cell(CellKind.String, idx, ResolveXf(format)));
        }

        public void SetCell(int rowIndex, int colIndex, double value, XlsxCellFormat? format = null, int sheetIndex = 0)
            => Put(sheetIndex, rowIndex, colIndex, new Cell(CellKind.Number, value, ResolveXf(format)));

        public void SetCell(int rowIndex, int colIndex, bool value, XlsxCellFormat? format = null, int sheetIndex = 0)
            => Put(sheetIndex, rowIndex, colIndex, new Cell(CellKind.Boolean, value ? 1 : 0, ResolveXf(format)));

        /// <summary>Written as a date serial with a short date (or date and time) format unless a format is given.</summary>
        public void SetCell(int rowIndex, int colIndex, DateTime value, XlsxCellFormat? format = null, int sheetIndex = 0)
        {
            format ??= value.TimeOfDay == TimeSpan.Zero ? DefaultDate : DefaultDateTime;
            SetCell(rowIndex, colIndex, (value - ExcelEpoch).TotalDays, format, sheetIndex);
        }

        private static readonly XlsxCellFormat DefaultDate = new XlsxCellFormat { NumberFormatCode = XlsxNumberFormats.ShortDate };
        private static readonly XlsxCellFormat DefaultDateTime = new XlsxCellFormat { NumberFormatCode = XlsxNumberFormats.ShortDateTime };

        public void AddMerge(int r1, int c1, int r2, int c2, int sheetIndex = 0)
        {
            CheckRow(r1); CheckRow(r2); CheckCol(c1); CheckCol(c2);
            _sheets[sheetIndex].Merges.Add((Math.Min(r1, r2), Math.Min(c1, c2), Math.Max(r1, r2), Math.Max(c1, c2)));
        }

        public void FreezePanes(int frozenRows, int frozenCols, int sheetIndex = 0)
        {
            _sheets[sheetIndex].FrozenRows = Math.Max(0, Math.Min(frozenRows, MaxRows - 1));
            _sheets[sheetIndex].FrozenCols = Math.Max(0, Math.Min(frozenCols, MaxColumns - 1));
        }

        public void SetPrintSetup(double leftIn, double topIn, double rightIn, double bottomIn, bool landscape, int sheetIndex = 0)
        {
            var s = _sheets[sheetIndex];
            s.Margins = (leftIn, topIn, rightIn, bottomIn);
            s.Landscape = landscape;
        }

        public void Write(Stream output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            var globals = new BiffStream();
            var boundSheetOffsets = WriteGlobals(globals);

            var sheetStreams = new List<BiffStream>();
            for (int i = 0; i < _sheets.Count; i++)
            {
                var s = new BiffStream();
                WriteSheet(s, _sheets[i], i == 0);
                sheetStreams.Add(s);
            }

            long offset = globals.Position;
            for (int i = 0; i < sheetStreams.Count; i++)
            {
                globals.PatchUInt32(boundSheetOffsets[i], (uint)offset);
                offset += sheetStreams[i].Position;
            }
            foreach (var s in sheetStreams) globals.Append(s);

            CompoundFileWriter.Write(output, "Workbook", globals.ToArray());
        }

        // ── Globals substream ─────────────────────────────────────────────────

        private List<long> WriteGlobals(BiffStream b)
        {
            WriteBof(b, 0x0005);
            b.Record(0x0042, w => w.Write((ushort)1200));                      // CODEPAGE: UTF-16
            b.Record(0x003D, w =>                                               // WINDOW1
            {
                w.Write((ushort)0); w.Write((ushort)0); w.Write((ushort)0x4000); w.Write((ushort)0x2000);
                w.Write((ushort)0x0038); w.Write((ushort)0); w.Write((ushort)0); w.Write((ushort)1); w.Write((ushort)0x0258);
            });
            b.Record(0x0022, w => w.Write((ushort)0));                         // DATEMODE: 1900

            // Font index 4 doesn't exist in BIFF; four copies of the default fill 0-3 so ours start at 5.
            var defaultFont = DefaultFont();
            for (int i = 0; i < 4; i++) WriteFont(b, defaultFont);
            foreach (var f in _fonts) WriteFont(b, f);

            for (int i = 0; i < _formats.Count; i++)
            {
                int id = FirstCustomFormat + i;
                b.Record(0x041E, w => { w.Write((ushort)id); BiffStream.WriteString(w, _formats[id - FirstCustomFormat]); });
            }

            var defaultXf = new XfKey { FontIndex = 0, FormatIndex = 0, Locked = true, BorderColors = new int[] { 0x40, 0x40, 0x40, 0x40 }, FillColor = 0x40 };
            for (int i = 0; i < 15; i++) WriteXf(b, defaultXf, style: true);
            WriteXf(b, defaultXf, style: false);
            foreach (var xf in _xfs) WriteXf(b, xf, style: false);

            b.Record(0x0293, w => { w.Write((ushort)0x8000); w.Write((byte)0); w.Write((byte)0xFF); }); // STYLE: Normal

            b.Record(0x0092, w =>                                               // PALETTE
            {
                w.Write((ushort)56);
                foreach (int rgb in _palette)
                {
                    w.Write((byte)(rgb >> 16)); w.Write((byte)(rgb >> 8)); w.Write((byte)rgb); w.Write((byte)0);
                }
            });

            var offsets = new List<long>();
            foreach (var sheet in _sheets)
            {
                var w = b.Begin(0x0085);                                        // BOUNDSHEET
                offsets.Add(b.Position + 4);
                w.Write(0u);                                                    // patched with the sheet's BOF position
                w.Write((ushort)0);                                             // visible worksheet
                BiffStream.WriteShortString(w, sheet.Name);
                b.End();
            }

            WriteSst(b);
            b.Record(0x000A, w => { });                                         // EOF
            return offsets;
        }

        private static void WriteBof(BiffStream b, ushort type) => b.Record(0x0809, w =>
        {
            w.Write((ushort)0x0600); w.Write(type); w.Write((ushort)0x0DBB); w.Write((ushort)0x07CC);
            w.Write(0x000100C1u); w.Write(0x00000006u);
        });

        private void WriteFont(BiffStream b, FontKey f) => b.Record(0x0031, w =>
        {
            w.Write((ushort)Math.Round(f.SizePt * 20));
            w.Write((ushort)((f.Italic ? 0x0002 : 0) | (f.Strikeout ? 0x0008 : 0)));
            w.Write((ushort)f.Color);
            w.Write((ushort)(f.Bold ? 700 : 400));
            w.Write((ushort)0);                                                 // super/subscript
            w.Write((byte)(f.Underline ? 1 : 0));
            w.Write((byte)0);                                                   // family
            w.Write((byte)0);                                                   // charset
            w.Write((byte)0);
            BiffStream.WriteShortString(w, f.Name);
        });

        private static void WriteXf(BiffStream b, XfKey x, bool style) => b.Record(0x00E0, w =>
        {
            w.Write((ushort)(x.FontIndex < 4 ? x.FontIndex : x.FontIndex + 1));
            w.Write((ushort)x.FormatIndex);
            int prot = (x.Locked ? 0x0001 : 0) | (x.Hidden ? 0x0002 : 0);
            w.Write((ushort)(style ? (prot | 0x0004 | 0xFFF0) : prot));         // style XFs have parent 0xFFF
            w.Write((byte)(x.HAlign | (x.Wrap ? 0x08 : 0) | (x.VAlign << 4)));
            w.Write((byte)x.Rotation);
            w.Write((byte)0);                                                   // indent, shrink, reading order
            w.Write((byte)(style ? 0 : 0xFC));                                  // cell XFs set every attribute group
            uint border1 = (uint)(x.BorderStyles[0] | (x.BorderStyles[1] << 4) | (x.BorderStyles[2] << 8) | (x.BorderStyles[3] << 12))
                           | ((uint)x.BorderColors[0] << 16) | ((uint)x.BorderColors[1] << 23);
            uint border2 = (uint)x.BorderColors[2] | ((uint)x.BorderColors[3] << 7) | (x.Solid ? (1u << 26) : 0);
            w.Write(border1);
            w.Write(border2);
            w.Write((ushort)(x.FillColor | (0x41 << 7)));
        });

        // SST: a string's 3-byte header never splits; its characters may continue in a CONTINUE record,
        // which then starts with a fresh flags byte. EXTSST indexes every dsst-th string.
        private void WriteSst(BiffStream b)
        {
            if (_sst.Count == 0) return;
            int dsst = Math.Max(8, _sst.Count / 128 + 1);
            var buckets = new List<(long Position, int Offset)>();

            var w = b.Begin(0x00FC);
            w.Write((uint)_sstTotal);
            w.Write((uint)_sst.Count);
            for (int i = 0; i < _sst.Count; i++)
            {
                string s = _sst[i];
                if (b.RecordLength + 3 + Math.Min(2, s.Length * 2) > BiffStream.MaxRecordData)
                {
                    b.End();
                    w = b.Begin(0x003C);
                }
                if (i % dsst == 0)
                    buckets.Add((b.Position + 4 + b.RecordLength, 4 + b.RecordLength));
                w.Write((ushort)s.Length);
                w.Write((byte)0x01);
                byte[] chars = Encoding.Unicode.GetBytes(s);
                int written = 0;
                while (true)
                {
                    int room = (BiffStream.MaxRecordData - b.RecordLength) & ~1;
                    int n = Math.Min(room, chars.Length - written);
                    w.Write(chars, written, n);
                    written += n;
                    if (written == chars.Length) break;
                    b.End();
                    w = b.Begin(0x003C);
                    w.Write((byte)0x01);
                }
            }
            b.End();

            b.Record(0x00FF, x =>
            {
                x.Write((ushort)dsst);
                foreach (var bucket in buckets)
                {
                    x.Write((uint)bucket.Position);
                    x.Write((ushort)bucket.Offset);
                    x.Write((ushort)0);
                }
            });
        }

        // ── Worksheet substream ───────────────────────────────────────────────

        private static void WriteSheet(BiffStream b, Sheet sheet, bool first)
        {
            WriteBof(b, 0x0010);

            // Record order: GUTS, WSBOOL, page setup, DEFCOLWIDTH, COLINFO, DIMENSIONS, rows, WINDOW2.
            int rowLevels = 0, colLevels = 0;
            foreach (var r in sheet.Rows.Values) rowLevels = Math.Max(rowLevels, r.Level);
            foreach (var c in sheet.Columns.Values) colLevels = Math.Max(colLevels, c.Level);
            if (rowLevels > 0 || colLevels > 0)
            {
                b.Record(0x0080, w =>                                           // GUTS
                {
                    w.Write((ushort)(rowLevels > 0 ? 12 * (rowLevels + 1) : 0));   // row gutter width
                    w.Write((ushort)(colLevels > 0 ? 12 * (colLevels + 1) : 0));   // column gutter height
                    w.Write((ushort)(rowLevels > 0 ? rowLevels + 1 : 0));
                    w.Write((ushort)(colLevels > 0 ? colLevels + 1 : 0));
                });
            }
            b.Record(0x0081, w => w.Write((ushort)0x04C1));                     // WSBOOL: outline symbols, summaries below/right
            b.Record(0x0026, w => w.Write(sheet.Margins.Left));
            b.Record(0x0027, w => w.Write(sheet.Margins.Right));
            b.Record(0x0028, w => w.Write(sheet.Margins.Top));
            b.Record(0x0029, w => w.Write(sheet.Margins.Bottom));
            b.Record(0x00A1, w =>                                               // SETUP
            {
                w.Write((ushort)9);                                             // A4, as XlsxWriter
                w.Write((ushort)100); w.Write((ushort)1); w.Write((ushort)1); w.Write((ushort)1);
                w.Write((ushort)(sheet.Landscape ? 0 : 0x0002));
                w.Write((ushort)600); w.Write((ushort)600);
                w.Write(0.5); w.Write(0.5);
                w.Write((ushort)1);
            });

            b.Record(0x0055, w => w.Write((ushort)8));                          // DEFCOLWIDTH

            foreach (var kv in sheet.Columns)
            {
                var c = kv.Value;
                b.Record(0x007D, w =>                                           // COLINFO
                {
                    w.Write((ushort)kv.Key); w.Write((ushort)kv.Key);
                    w.Write((ushort)Math.Min(65535, Math.Round((c.Width ?? 8.43) * 256)));
                    w.Write((ushort)DefaultCellXf);
                    w.Write((ushort)((c.Hidden ? 0x0001 : 0) | (c.Level << 8)));
                    w.Write((ushort)0);
                });
            }

            var cellsByRow = new SortedDictionary<int, SortedDictionary<int, Cell>>();
            foreach (var kv in sheet.Cells)
            {
                if (!cellsByRow.TryGetValue(kv.Key.Row, out var row))
                    cellsByRow[kv.Key.Row] = row = new SortedDictionary<int, Cell>();
                row[kv.Key.Col] = kv.Value;
            }

            int rMin = int.MaxValue, rMax = -1, cMin = int.MaxValue, cMax = -1;
            foreach (var kv in sheet.Cells)
            {
                rMin = Math.Min(rMin, kv.Key.Row); rMax = Math.Max(rMax, kv.Key.Row);
                cMin = Math.Min(cMin, kv.Key.Col); cMax = Math.Max(cMax, kv.Key.Col);
            }
            b.Record(0x0200, w =>                                               // DIMENSIONS
            {
                bool empty = rMax < 0;
                w.Write((uint)(empty ? 0 : rMin)); w.Write((uint)(empty ? 0 : rMax + 1));
                w.Write((ushort)(empty ? 0 : cMin)); w.Write((ushort)(empty ? 0 : cMax + 1));
                w.Write((ushort)0);
            });

            var rowIndexes = new SortedSet<int>(cellsByRow.Keys);
            foreach (var k in sheet.Rows.Keys) rowIndexes.Add(k);
            foreach (int r in rowIndexes)
            {
                sheet.Rows.TryGetValue(r, out var info);
                cellsByRow.TryGetValue(r, out var cells);
                b.Record(0x0208, w =>                                           // ROW
                {
                    w.Write((ushort)r);
                    w.Write((ushort)(cells != null ? FirstKey(cells) : 0));
                    w.Write((ushort)(cells != null ? LastKey(cells) + 1 : 0));
                    bool custom = info?.Height != null;
                    w.Write((ushort)(custom ? Math.Min(8191, Math.Round(info!.Height!.Value * 20)) : 255));
                    w.Write((ushort)0); w.Write((ushort)0);
                    w.Write((ushort)((info?.Level ?? 0) | (info?.Hidden == true ? 0x0020 : 0) | (custom ? 0x0040 : 0) | 0x0100));
                    w.Write((ushort)DefaultCellXf);
                });
                if (cells == null) continue;
                foreach (var kv in cells)
                    WriteCell(b, r, kv.Key, kv.Value);
            }

            b.Record(0x023E, w =>                                               // WINDOW2
            {
                bool frozen = sheet.FrozenRows > 0 || sheet.FrozenCols > 0;
                ushort grbit = (ushort)(0x00B6 | (first ? 0x0600 : 0) | (frozen ? 0x0108 : 0));
                w.Write(grbit); w.Write((ushort)0); w.Write((ushort)0);
                w.Write(0x40u); w.Write((ushort)0); w.Write((ushort)0); w.Write(0u);
            });
            if (sheet.FrozenRows > 0 || sheet.FrozenCols > 0)
            {
                b.Record(0x0041, w =>                                           // PANE
                {
                    w.Write((ushort)sheet.FrozenCols); w.Write((ushort)sheet.FrozenRows);
                    w.Write((ushort)sheet.FrozenRows); w.Write((ushort)sheet.FrozenCols);
                    w.Write((byte)(sheet.FrozenRows > 0 && sheet.FrozenCols > 0 ? 0 : sheet.FrozenRows > 0 ? 2 : 1));
                    w.Write((byte)0);
                });
            }

            for (int start = 0; start < sheet.Merges.Count; start += 1026)
            {
                int count = Math.Min(1026, sheet.Merges.Count - start);
                b.Record(0x00E5, w =>                                           // MERGEDCELLS
                {
                    w.Write((ushort)count);
                    for (int i = start; i < start + count; i++)
                    {
                        var m = sheet.Merges[i];
                        w.Write((ushort)m.r1); w.Write((ushort)m.r2); w.Write((ushort)m.c1); w.Write((ushort)m.c2);
                    }
                });
            }

            b.Record(0x000A, w => { });
        }

        private static int FirstKey(SortedDictionary<int, Cell> d) { foreach (var k in d.Keys) return k; return 0; }
        private static int LastKey(SortedDictionary<int, Cell> d) { int last = 0; foreach (var k in d.Keys) last = k; return last; }

        private static void WriteCell(BiffStream b, int row, int col, Cell cell)
        {
            switch (cell.Kind)
            {
                case CellKind.String:
                    b.Record(0x00FD, w => { CellHeader(w, row, col, cell.Xf); w.Write((uint)cell.Value); });   // LABELSST
                    break;
                case CellKind.Number:
                    b.Record(0x0203, w => { CellHeader(w, row, col, cell.Xf); w.Write(cell.Value); });         // NUMBER
                    break;
                case CellKind.Boolean:
                    b.Record(0x0205, w => { CellHeader(w, row, col, cell.Xf); w.Write((byte)cell.Value); w.Write((byte)0); }); // BOOLERR
                    break;
            }
        }

        private static void CellHeader(BinaryWriter w, int row, int col, int xf)
        {
            w.Write((ushort)row); w.Write((ushort)col); w.Write((ushort)xf);
        }

        // ── Styles ────────────────────────────────────────────────────────────

        private int ResolveXf(XlsxCellFormat? format)
        {
            if (format == null) return DefaultCellXf;
            if (_xfByFormat.TryGetValue(format, out int xf)) return xf;

            var font = new FontKey
            {
                Name = string.IsNullOrEmpty(format.FontName) ? "Calibri" : format.FontName,
                SizePt = format.FontSizePt > 0 ? format.FontSizePt : 11,
                Bold = format.Bold, Italic = format.Italic, Strikeout = format.Strikeout, Underline = format.Underline,
                Color = ColorIndex(format.FontColor),
            };
            var key = new XfKey
            {
                FontIndex = 4 + IndexOf(_fonts, font),
                FormatIndex = FormatIndex(format.NumberFormatCode),
                HAlign = format.HorizontalAlign switch { "left" => 1, "center" => 2, "right" => 3, "justify" => 5, _ => 0 },
                VAlign = format.VerticalAlign switch { "center" => 1, "bottom" => 2, _ => 0 },
                Wrap = format.WrapText,
                Rotation = format.Rotation >= 0 ? Math.Min(90, format.Rotation) : 90 + Math.Min(90, -format.Rotation),
                Locked = format.Locked,
                Hidden = format.FormulaHidden,
                BorderStyles = new[] { LineStyle(format.BorderLeft), LineStyle(format.BorderRight), LineStyle(format.BorderTop), LineStyle(format.BorderBottom) },
                BorderColors = new[] { BorderColor(format.BorderLeft), BorderColor(format.BorderRight), BorderColor(format.BorderTop), BorderColor(format.BorderBottom) },
                Solid = format.HasBackground,
                FillColor = format.HasBackground ? ColorIndex(format.BackgroundColor) : 0x40,
            };
            int index = IndexOf(_xfs, key);
            if (index >= MaxCellXfs)
                throw new InvalidOperationException($"An .xls file holds at most {MaxCellXfs} distinct cell formats.");
            xf = DefaultCellXf + 1 + index;
            _xfByFormat[format] = xf;
            return xf;
        }

        private static readonly Dictionary<string, int> BuiltInFormats = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["General"] = 0, ["0"] = 1, ["0.00"] = 2, ["#,##0"] = 3, ["#,##0.00"] = 4, ["0%"] = 9, ["0.00%"] = 10,
            ["0.00E+00"] = 11, ["# ?/?"] = 12, ["# ??/??"] = 13, ["h:mm AM/PM"] = 18, ["h:mm:ss AM/PM"] = 19,
            ["h:mm"] = 20, ["h:mm:ss"] = 21, ["mm:ss"] = 45, ["[h]:mm:ss"] = 46, ["mmss.0"] = 47, ["##0.0E+0"] = 48, ["@"] = 49,
        };

        private int FormatIndex(string? code)
        {
            if (string.IsNullOrEmpty(code)) return 0;
            if (BuiltInFormats.TryGetValue(code!, out int builtIn)) return builtIn;
            int i = _formats.IndexOf(code!);
            if (i < 0) { _formats.Add(code!); i = _formats.Count - 1; }
            return FirstCustomFormat + i;
        }

        private static int LineStyle(XlsxBorderSide? side) => side?.Style switch
        {
            "thin" => 1, "medium" => 2, "dashed" => 3, "dotted" => 4, "thick" => 5, "double" => 6, "hair" => 7, _ => 0,
        };

        private int BorderColor(XlsxBorderSide? side) =>
            side == null || string.IsNullOrEmpty(side.Style) ? 0x40 : ColorIndex(side.Color);

        // The palette's 56 slots start as Excel's defaults; a colour not among them takes the first slot no
        // cell uses yet, and once all are taken it maps to the nearest colour in use.
        private int ColorIndex((byte R, byte G, byte B) c)
        {
            int rgb = (c.R << 16) | (c.G << 8) | c.B;
            int exact = Array.IndexOf(_palette, rgb);
            if (exact >= 0) { _paletteUsed[exact] = true; return exact + 8; }
            int free = Array.IndexOf(_paletteUsed, false);
            if (free >= 0)
            {
                _palette[free] = rgb;
                _paletteUsed[free] = true;
                return free + 8;
            }
            int best = 0;
            long bestDist = long.MaxValue;
            for (int i = 0; i < _palette.Length; i++)
            {
                int p = _palette[i];
                long dr = ((p >> 16) & 0xFF) - c.R, dg = ((p >> 8) & 0xFF) - c.G, db = (p & 0xFF) - c.B;
                long d = dr * dr * 3 + dg * dg * 4 + db * db * 2;
                if (d < bestDist) { bestDist = d; best = i; }
            }
            return best + 8;
        }

        private FontKey DefaultFont() => new FontKey { Name = "Calibri", SizePt = 11, Color = 0x7FFF };

        private static int IndexOf<T>(List<T> list, T item) where T : IEquatable<T>
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].Equals(item)) return i;
            list.Add(item);
            return list.Count - 1;
        }

        private void Put(int sheetIndex, int row, int col, Cell cell)
        {
            if (cell.Kind == CellKind.String) _sstTotal++;
            _sheets[sheetIndex].Cells[(CheckRow(row), CheckCol(col))] = cell;
        }

        private static int CheckRow(int row) =>
            row >= 0 && row < MaxRows ? row : throw new ArgumentOutOfRangeException(nameof(row), $"An .xls sheet has rows 0-{MaxRows - 1}.");

        private static int CheckCol(int col) =>
            col >= 0 && col < MaxColumns ? col : throw new ArgumentOutOfRangeException(nameof(col), $"An .xls sheet has columns 0-{MaxColumns - 1}.");

        private static readonly int[] DefaultPalette =
        {
            0x000000, 0xFFFFFF, 0xFF0000, 0x00FF00, 0x0000FF, 0xFFFF00, 0xFF00FF, 0x00FFFF,
            0x800000, 0x008000, 0x000080, 0x808000, 0x800080, 0x008080, 0xC0C0C0, 0x808080,
            0x9999FF, 0x993366, 0xFFFFCC, 0xCCFFFF, 0x660066, 0xFF8080, 0x0066CC, 0xCCCCFF,
            0x000080, 0xFF00FF, 0xFFFF00, 0x00FFFF, 0x800080, 0x800000, 0x008080, 0x0000FF,
            0x00CCFF, 0xCCFFFF, 0xCCFFCC, 0xFFFF99, 0x99CCFF, 0xFF99CC, 0xCC99FF, 0xFFCC99,
            0x3366FF, 0x33CCCC, 0x99CC00, 0xFFCC00, 0xFF9900, 0xFF6600, 0x666699, 0x969696,
            0x003366, 0x339966, 0x003300, 0x333300, 0x993300, 0x993366, 0x333399, 0x333333,
        };

        // ── Data holders ──────────────────────────────────────────────────────

        private enum CellKind { String, Number, Boolean }

        private readonly struct Cell
        {
            public readonly CellKind Kind;
            public readonly double Value; // SST index for strings, 0/1 for booleans
            public readonly int Xf;
            public Cell(CellKind kind, double value, int xf) { Kind = kind; Value = value; Xf = xf; }
        }

        private sealed class RowInfo { public double? Height; public int Level; public bool Hidden; }
        private sealed class ColumnInfo { public double? Width; public int Level; public bool Hidden; }

        private sealed class Sheet
        {
            public readonly string Name;
            public readonly Dictionary<(int Row, int Col), Cell> Cells = new Dictionary<(int, int), Cell>();
            public readonly SortedDictionary<int, RowInfo> Rows = new SortedDictionary<int, RowInfo>();
            public readonly SortedDictionary<int, ColumnInfo> Columns = new SortedDictionary<int, ColumnInfo>();
            public readonly List<(int r1, int c1, int r2, int c2)> Merges = new List<(int, int, int, int)>();
            public int FrozenRows, FrozenCols;
            public (double Left, double Top, double Right, double Bottom) Margins = (0.75, 1.0, 0.75, 1.0);
            public bool Landscape;

            public Sheet(string name) { Name = name; }

            public RowInfo Row(int r) => Rows.TryGetValue(r, out var info) ? info : Rows[r] = new RowInfo();
            public ColumnInfo Column(int c) => Columns.TryGetValue(c, out var info) ? info : Columns[c] = new ColumnInfo();
        }

        private sealed class FontKey : IEquatable<FontKey>
        {
            public string Name = "Calibri";
            public double SizePt = 11;
            public bool Bold, Italic, Strikeout, Underline;
            public int Color;

            public bool Equals(FontKey? o) => o != null && Name == o.Name && SizePt == o.SizePt && Bold == o.Bold &&
                Italic == o.Italic && Strikeout == o.Strikeout && Underline == o.Underline && Color == o.Color;
        }

        private sealed class XfKey : IEquatable<XfKey>
        {
            public int FontIndex, FormatIndex, HAlign, VAlign, Rotation, FillColor;
            public bool Wrap, Locked = true, Hidden, Solid;
            public int[] BorderStyles = new int[4];
            public int[] BorderColors = new int[4];

            public bool Equals(XfKey? o) => o != null && FontIndex == o.FontIndex && FormatIndex == o.FormatIndex &&
                HAlign == o.HAlign && VAlign == o.VAlign && Rotation == o.Rotation && FillColor == o.FillColor &&
                Wrap == o.Wrap && Locked == o.Locked && Hidden == o.Hidden && Solid == o.Solid &&
                SequenceEqual(BorderStyles, o.BorderStyles) && SequenceEqual(BorderColors, o.BorderColors);

            private static bool SequenceEqual(int[] a, int[] b)
            {
                for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
                return true;
            }
        }
    }
}
