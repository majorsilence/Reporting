/*
 * rdlnative.h — C API for the Majorsilence Reporting native shared library.
 *
 * Library names by platform:
 *   Linux:   librdlnative.so
 *   macOS:   librdlnative.dylib
 *   Windows: rdlnative.dll
 *
 * Quick-start example (C):
 *
 *   rdl_init();
 *   void* h = rdl_report_open("/path/to/report.rdl", NULL);
 *   rdl_report_set_param(h, "Country", "Germany");
 *
 *   // Render to a file
 *   rdl_report_render_file(h, "/tmp/output.pdf", "pdf");
 *
 *   // -or- render to an in-memory buffer
 *   uint8_t* buf = NULL;
 *   int      len = 0;
 *   rdl_report_render_buffer(h, "pdf", &buf, &len);
 *   // ... use buf[0..len-1] ...
 *   rdl_free(buf);
 *
 *   rdl_report_close(h);
 *
 * Supported format strings: "pdf", "html", "xml", "csv", "xlsx", "xlsx_table",
 *                            "rtf", "tif", "tifb", "mht"
 */

#ifndef RDLNATIVE_H
#define RDLNATIVE_H

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

/*
 * Initialize the reporting engine.
 * Must be called once at startup before any other function.
 * Returns 0 on success, -1 on error (call rdl_last_error() for details).
 */
int rdl_init(void);

/*
 * Open a report from an RDL file path.
 * connection_string may be NULL to use the connection string embedded in the RDL.
 * Returns an opaque handle on success, or NULL on error.
 */
void* rdl_report_open(const char* rdl_path, const char* connection_string);

/*
 * Set a named parameter on an open report.
 * Returns 0 on success, -1 on error.
 */
int rdl_report_set_param(void* handle, const char* name, const char* value);

/*
 * Render the report to a file.
 * Returns 0 on success, -1 on error.
 */
int rdl_report_render_file(void* handle, const char* output_path, const char* format);

/*
 * Render the report to a heap-allocated buffer.
 * On success, *out_data points to a buffer of *out_size bytes.
 * The caller must free the buffer with rdl_free().
 * Returns 0 on success, -1 on error.
 */
int rdl_report_render_buffer(void*    handle,
                              const char* format,
                              uint8_t**   out_data,
                              int*        out_size);

/*
 * Free memory allocated by rdl_report_render_buffer.
 */
void rdl_free(void* ptr);

/*
 * Close a report handle and release all associated resources.
 */
void rdl_report_close(void* handle);

/*
 * Return a UTF-8 error message describing the most recent failure.
 * The returned pointer is valid until the next error occurs.
 * Never returns NULL; returns an empty string when no error has occurred.
 */
const char* rdl_last_error(void);

#ifdef __cplusplus
}
#endif

#endif /* RDLNATIVE_H */
