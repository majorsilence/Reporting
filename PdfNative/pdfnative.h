/*
 * pdfnative.h — C API for the Majorsilence PDF native shared library.
 *
 * Library names by platform:
 *   Linux:   libpdfnative.so
 *   macOS:   libpdfnative.dylib
 *   Windows: pdfnative.dll
 *
 * Quick-start example (C):
 *
 *   pdf_init();
 *
 *   // Create a document
 *   void* doc = pdf_doc_create();
 *   pdf_doc_set_title(doc, "My Report");
 *   pdf_doc_set_author(doc, "Example Corp");
 *
 *   // Add a page (A4 = 595.28 x 841.89 points)
 *   void* canvas = pdf_doc_add_page(doc, 595.28f, 841.89f);
 *
 *   // Draw text with default style
 *   pdf_canvas_draw_text(canvas, "Hello, PDF!", 72, 100, NULL);
 *
 *   // Draw text with a custom style
 *   void* style = pdf_style_create();
 *   pdf_style_set_size(style, 18);
 *   pdf_style_set_bold(style, 1);
 *   pdf_style_set_color(style, 0, 0, 128);   // dark blue
 *   pdf_canvas_draw_text(canvas, "Bold Blue Heading", 72, 140, style);
 *   pdf_style_close(style);
 *
 *   // Render to a file
 *   pdf_canvas_close(canvas);
 *   pdf_doc_save_file(doc, "/tmp/output.pdf");
 *
 *   // -or- render to an in-memory buffer
 *   uint8_t* buf = NULL;
 *   int      len = 0;
 *   pdf_doc_save_buffer(doc, &buf, &len);
 *   // ... use buf[0..len-1] ...
 *   pdf_free(buf);
 *
 *   pdf_doc_close(doc);
 *
 * Table example:
 *
 *   float col_widths[] = { 150, 250, 100 };
 *   void* table = pdf_table_create(col_widths, 3);
 *   pdf_table_set_header_bg(table, 30, 80, 160);  // blue header
 *   pdf_table_set_alternate_bg(table, 230, 240, 255);
 *   pdf_table_set_border(table, 0, 0, 0, 0.5f);
 *
 *   // Header row
 *   pdf_table_stage_cell(table, "Name");
 *   pdf_table_stage_cell(table, "Address");
 *   pdf_table_stage_cell(table, "Phone");
 *   pdf_table_commit_row(table);
 *
 *   // Data row
 *   pdf_table_stage_cell(table, "Alice");
 *   pdf_table_stage_cell(table, "1 Main St");
 *   pdf_table_stage_cell(table, "555-0100");
 *   pdf_table_commit_row(table);
 *
 *   pdf_canvas_draw_table(canvas, table, 36, 200);
 *   pdf_table_close(table);
 *
 * Merge example:
 *
 *   void* merger = pdf_merge_create();
 *   pdf_merge_add_bytes(merger, doc1_bytes, doc1_len);
 *   pdf_merge_add_bytes(merger, doc2_bytes, doc2_len);
 *   pdf_merge_save_file(merger, "/tmp/merged.pdf");
 *   pdf_merge_close(merger);
 *
 * Security example:
 *
 *   // AES-256 (default) with print-only permissions
 *   pdf_doc_set_security(doc, "userpass", "ownerpass",
 *                        4 | 2048,  // Print | PrintHighQuality
 *                        0);        // 0 = AES-256, 1 = AES-128
 *
 * Coordinate system:
 *   All coordinates are in PDF points (1 pt = 1/72 inch).
 *   The origin is at the top-left corner of the page; Y increases downward.
 *
 * Standard page sizes (points):
 *   A4      = 595.28 x 841.89    Letter  = 612 x 792
 *   A3      = 841.89 x 1190.55   Legal   = 612 x 1008
 *   A5      = 419.53 x 595.28    Tabloid = 792 x 1224
 */

#ifndef PDFNATIVE_H
#define PDFNATIVE_H

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

/*
 * Initialize the PDF library and set up the P/Invoke resolver.
 * Optional — the library self-initializes on first use.
 * Set PDFNATIVE_LIB_DIR to the directory containing pdfnative.so before
 * calling this function if sibling native libraries must be found there.
 * Returns 0 on success, -1 on error (call pdf_last_error() for details).
 */
int pdf_init(void);

/* ── Document ──────────────────────────────────────────────────────────────── */

/*
 * Create a new PDF document.
 * Returns an opaque handle on success, or NULL on error.
 */
void* pdf_doc_create(void);

/*
 * Set document metadata.  All values are UTF-8 strings.
 * Returns 0 on success, -1 on error.
 */
int pdf_doc_set_title(void* handle, const char* title);
int pdf_doc_set_author(void* handle, const char* author);
int pdf_doc_set_subject(void* handle, const char* subject);
int pdf_doc_set_creator(void* handle, const char* creator);

/*
 * Apply password-based AES encryption to the document.
 *
 * user_password   — password required to open the document (empty string = no password).
 * owner_password  — full-control password; pass NULL to use user_password.
 * permissions     — bitmask of allowed operations when opened with user_password.
 *                   Pass -1 for all permissions.  Individual flags:
 *                     Print            =    4
 *                     ModifyContent    =    8
 *                     CopyText         =   16
 *                     AddAnnotations   =   32
 *                     FillForms        =  256
 *                     ExtractText      =  512
 *                     Assemble         = 1024
 *                     PrintHighQuality = 2048
 * enc_version     — 0 = AES-256 (PDF 2.0, default), 1 = AES-128 (PDF 1.5+).
 *
 * Returns 0 on success, -1 on error.
 */
int pdf_doc_set_security(void* handle,
                         const char* user_password,
                         const char* owner_password,
                         int         permissions,
                         int         enc_version);

/*
 * Add a page to the document and return a canvas handle for drawing.
 * width and height are in points (1 pt = 1/72 inch).
 * Returns a canvas handle on success, or NULL on error.
 */
void* pdf_doc_add_page(void* handle, float width, float height);

/*
 * Write the completed document to a file.
 * Returns 0 on success, -1 on error.
 */
int pdf_doc_save_file(void* handle, const char* path);

/*
 * Write the completed document to a heap-allocated buffer.
 * On success, *out_data points to a buffer of *out_size bytes.
 * The caller must free the buffer with pdf_free().
 * Returns 0 on success, -1 on error.
 */
int pdf_doc_save_buffer(void*     handle,
                        uint8_t** out_data,
                        int*      out_size);

/*
 * Close a document handle and release all associated resources.
 */
void pdf_doc_close(void* handle);

/* ── Canvas ────────────────────────────────────────────────────────────────── */

/*
 * Draw text with its baseline at (x, y).
 * style may be NULL to use the default style (Helvetica 12 pt black).
 * Returns 0 on success, -1 on error.
 */
int pdf_canvas_draw_text(void*       canvas,
                         const char* text,
                         float       x,
                         float       y,
                         void*       style);

/*
 * Draw word-wrapped text inside a box whose top-left corner is (x, y).
 * line_spacing is a multiplier relative to font size (pass 0 for default 1.2).
 * Returns the character offset of the first character that did not fit
 * (equals strlen(text) when all text was rendered), or -1 on error.
 */
int pdf_canvas_draw_textbox(void*       canvas,
                            const char* text,
                            float       x,
                            float       y,
                            float       width,
                            float       height,
                            void*       style,
                            float       line_spacing);

/*
 * Draw a straight line from (x1, y1) to (x2, y2).
 * r, g, b are byte values 0-255.
 * Returns 0 on success, -1 on error.
 */
int pdf_canvas_draw_line(void* canvas,
                         float x1, float y1,
                         float x2, float y2,
                         uint8_t r, uint8_t g, uint8_t b,
                         float width);

/*
 * Draw a rectangle with top-left at (x, y) and the given width and height.
 * has_fill / has_stroke: 0 = false, non-zero = true.
 * Fill and stroke colours use byte values 0-255.
 * Returns 0 on success, -1 on error.
 */
int pdf_canvas_draw_rect(void*   canvas,
                         float   x,      float   y,
                         float   width,  float   height,
                         uint8_t fill_r, uint8_t fill_g,   uint8_t fill_b,   int has_fill,
                         uint8_t stroke_r, uint8_t stroke_g, uint8_t stroke_b,
                         float   stroke_width, int has_stroke);

/*
 * Draw an ellipse bounded by the rectangle at (x, y) with given width and height.
 * has_fill / has_stroke: 0 = false, non-zero = true.
 * Returns 0 on success, -1 on error.
 */
int pdf_canvas_draw_ellipse(void*   canvas,
                            float   x,      float   y,
                            float   width,  float   height,
                            uint8_t fill_r, uint8_t fill_g,   uint8_t fill_b,   int has_fill,
                            uint8_t stroke_r, uint8_t stroke_g, uint8_t stroke_b,
                            float   stroke_width, int has_stroke);

/*
 * Draw an image with its top-left corner at (x, y), scaled to (w x h) points.
 * is_jpeg: non-zero if data contains JPEG bytes;
 *          0 if data is raw RGB24 bytes (pixel_width * pixel_height * 3 bytes).
 * Returns 0 on success, -1 on error.
 */
int pdf_canvas_draw_image(void*          canvas,
                          const uint8_t* data,
                          int            data_len,
                          int            pixel_width,
                          int            pixel_height,
                          int            is_jpeg,
                          float          x,
                          float          y,
                          float          w,
                          float          h);

/*
 * Draw a table with its top-left corner at (x, y).
 * Returns 0 on success, -1 on error.
 */
int pdf_canvas_draw_table(void* canvas, void* table, float x, float y);

/*
 * Add a clickable hyperlink over the given rectangular area.
 * Returns 0 on success, -1 on error.
 */
int pdf_canvas_add_link(void*       canvas,
                        float       x,
                        float       y,
                        float       width,
                        float       height,
                        const char* uri);

/*
 * Release a canvas handle.  The drawn content is retained inside the document.
 * Call pdf_doc_save_file / pdf_doc_save_buffer after closing all canvases.
 */
void pdf_canvas_close(void* handle);

/* ── Style ─────────────────────────────────────────────────────────────────── */

/*
 * Create a text style handle.
 * Defaults: Helvetica, 12 pt, black, left-aligned, no decoration.
 * Returns a handle on success, or NULL on error.
 */
void* pdf_style_create(void);

/*
 * Set the font family name (e.g. "Helvetica", "Times-Roman", "Courier").
 * Clears any previously set font file path.
 * Returns 0 on success, -1 on error.
 */
int pdf_style_set_font_family(void* style, const char* family);

/*
 * Embed the TrueType/OpenType font at the given file path.
 * When set, the font is read and embedded in the PDF.
 * Returns 0 on success, -1 on error.
 */
int pdf_style_set_font_file(void* style, const char* path);

/*
 * Set the font size in points.
 * Returns 0 on success, -1 on error.
 */
int pdf_style_set_size(void* style, float points);

/*
 * Set the text colour using byte values 0-255.
 * Returns 0 on success, -1 on error.
 */
int pdf_style_set_color(void* style, uint8_t r, uint8_t g, uint8_t b);

/*
 * Set bold. is_bold: 0 = false, non-zero = true.
 * Returns 0 on success, -1 on error.
 */
int pdf_style_set_bold(void* style, int is_bold);

/*
 * Set italic. is_italic: 0 = false, non-zero = true.
 * Returns 0 on success, -1 on error.
 */
int pdf_style_set_italic(void* style, int is_italic);

/*
 * Set text alignment.  alignment: 0 = left, 1 = center, 2 = right.
 * Returns 0 on success, -1 on error.
 */
int pdf_style_set_alignment(void* style, int alignment);

/*
 * Set text decoration.
 * decoration: 0 = none, 1 = underline, 2 = strikethrough, 3 = overline.
 * Returns 0 on success, -1 on error.
 */
int pdf_style_set_decoration(void* style, int decoration);

/*
 * Release a style handle.
 */
void pdf_style_close(void* handle);

/* ── Table ─────────────────────────────────────────────────────────────────── */

/*
 * Create a table handle with the specified column widths (in points).
 * col_widths is an array of n_cols float values.
 * Returns a handle on success, or NULL on error.
 */
void* pdf_table_create(const float* col_widths, int n_cols);

/*
 * Set the header background colour (first row).  Byte values 0-255.
 * Returns 0 on success, -1 on error.
 */
int pdf_table_set_header_bg(void* handle, uint8_t r, uint8_t g, uint8_t b);

/*
 * Set the alternating row background colour.  Byte values 0-255.
 * Returns 0 on success, -1 on error.
 */
int pdf_table_set_alternate_bg(void* handle, uint8_t r, uint8_t g, uint8_t b);

/*
 * Set the border colour and stroke width.  Pass width = 0 to suppress borders.
 * Returns 0 on success, -1 on error.
 */
int pdf_table_set_border(void* handle, uint8_t r, uint8_t g, uint8_t b, float width);

/*
 * Set the padding inside each cell in points (default 4).
 * Returns 0 on success, -1 on error.
 */
int pdf_table_set_cell_padding(void* handle, float padding);

/*
 * Stage one cell for the current row.  Call once per column.
 * Then call pdf_table_commit_row() to append the row to the table.
 * Returns 0 on success, -1 on error.
 */
int pdf_table_stage_cell(void* handle, const char* text);

/*
 * Commit all staged cells as a new table row and clear the staged cells.
 * Returns 0 on success, -1 on error.
 */
int pdf_table_commit_row(void* handle);

/*
 * Release a table handle.
 */
void pdf_table_close(void* handle);

/* ── Merge ─────────────────────────────────────────────────────────────────── */

/*
 * Create a PDF merger handle.
 * Returns a handle on success, or NULL on error.
 */
void* pdf_merge_create(void);

/*
 * Add a PDF document supplied as a byte buffer to the merge queue.
 * Returns 0 on success, -1 on error.
 */
int pdf_merge_add_bytes(void* handle, const uint8_t* data, int data_len);

/*
 * Merge all added PDFs and write the result to a file.
 * Returns 0 on success, -1 on error.
 */
int pdf_merge_save_file(void* handle, const char* path);

/*
 * Merge all added PDFs and write the result to a heap-allocated buffer.
 * On success, *out_data points to a buffer of *out_size bytes.
 * The caller must free the buffer with pdf_free().
 * Returns 0 on success, -1 on error.
 */
int pdf_merge_save_buffer(void*     handle,
                          uint8_t** out_data,
                          int*      out_size);

/*
 * Release a merger handle.
 */
void pdf_merge_close(void* handle);

/* ── Shared ────────────────────────────────────────────────────────────────── */

/*
 * Free memory allocated by pdf_doc_save_buffer or pdf_merge_save_buffer.
 */
void pdf_free(void* ptr);

/*
 * Return a UTF-8 string describing the most recent error.
 * The returned pointer is valid until the next error occurs.
 * Never returns NULL; returns an empty string when no error has occurred.
 */
const char* pdf_last_error(void);

#ifdef __cplusplus
}
#endif

#endif /* PDFNATIVE_H */
