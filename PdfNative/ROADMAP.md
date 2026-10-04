# PdfNative roadmap

Notes on future C-ABI surface, kept here so wrapper authors (pdf-python/php/ruby/rust) can
comment before any of this is built. Nothing in this file is implemented yet.

## PdfLayout (fluent Row/Column cursor layout)

`Majorsilence.Pdf.Layout.PdfLayout` (added in the managed library) is not exposed over the C
ABI yet. It was deliberately left out of this pass:

- The managed API is builder-lambda-heavy (`Action<PdfCanvas> draw`, `Action<LayoutRow>
  configure`), which doesn't translate to a flat C ABI — there's no natural equivalent to a
  C# closure for FFI callers.
- A C-ABI version would need to be a retained-handle, imperative sequence of calls instead,
  roughly:

  ```c
  pdf_layout_handle pdf_layout_begin(pdf_doc_handle doc, float page_w, float page_h);
  void pdf_layout_set_margins(pdf_layout_handle layout, float l, float t, float r, float b);
  void pdf_layout_set_footer(pdf_layout_handle layout, pdf_footer_callback cb, float height);
  void pdf_layout_text(pdf_layout_handle layout, const char* text, pdf_style_handle style);
  void pdf_layout_spacer(pdf_layout_handle layout, float height);
  void pdf_layout_line(pdf_layout_handle layout, pdf_stroke_style_handle style, float gap_after);
  void pdf_layout_table(pdf_layout_handle layout, pdf_table_handle table);
  pdf_row_handle pdf_layout_row_begin(pdf_layout_handle layout);
  pdf_column_handle pdf_row_column_begin(pdf_row_handle row, float width_fraction);
  void pdf_column_text(pdf_column_handle column, const char* text, pdf_style_handle style);
  void pdf_column_end(pdf_column_handle column);
  void pdf_row_end(pdf_row_handle row); // draws the row, replaying pdf_layout_row_begin's measure pass
  void pdf_layout_page_break(pdf_layout_handle layout);
  pdf_doc_handle pdf_layout_end(pdf_layout_handle layout); // returns the doc handle for saving
  ```

  Each language wrapper would then rebuild its own fluent `Row`/`Column` builder on top of this
  handle sequence in idiomatic code, the same way the existing wrappers rebuild `PdfDocument`/
  `PdfCanvas` on top of the flat document/canvas/style/table functions in `pdfnative.h` today.
- The measure/render two-pass design (each `Row`/`Column` configuration callback runs twice)
  is straightforward in C# but would require careful handle lifetime management across an FFI
  boundary — deferred until there's a concrete wrapper-language need for it.

## Markdown rendering

`Majorsilence.Pdf.Markdown` (planned; not yet built) will depend on Markdig, which is out of
scope for `pdfnative`'s minimal-footprint AOT build. If markdown rendering is ever exposed over
the C ABI, it should ship as a **separate native library** (e.g. `libpdfnative_markdown.so`)
rather than folding a Markdig dependency into the core `pdfnative` binary that every wrapper
loads today.

## Sync policy

`pdfnative.h` is the source of truth for the C ABI. Any change to it must land alongside updates
to all four language wrappers (pdf-python, pdf-php, pdf-ruby, pdf-rust) in the same release —
none of the above is exempt from that policy once it's actually implemented.
