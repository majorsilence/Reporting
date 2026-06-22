"""
report_native.py — Python FFI wrapper for the rdlnative shared library.

Loads the Majorsilence Reporting engine in-process via ctypes — no subprocess
is spawned, no .NET runtime is required on the host.

Platform-specific library filenames:
  Linux:   librdlnative.so
  macOS:   librdlnative.dylib
  Windows: rdlnative.dll

Usage:
    from report_native import load_library, Report

    lib = load_library('/path/to/librdlnative.so')

    rpt = Report(lib, '/path/to/report.rdl')
    rpt.set_parameter('Country', 'Germany')
    rpt.set_connection_string('Data Source=myserver.db')

    # Export to a file
    rpt.export('pdf', '/tmp/output.pdf')

    # Export to bytes in-memory (no temp file written)
    data = rpt.export_to_memory('pdf')

Supported export types: "pdf", "csv", "xlsx", "xlsx_table", "xml", "rtf",
                        "tif", "tifb", "html", "mht"
"""

import ctypes
import contextlib
import glob
import os
import platform


def load_library(lib_path: str) -> ctypes.CDLL:
    """
    Load the rdlnative shared library from *lib_path* and initialize the engine.

    Call this once per process before creating any Report instances.
    Returns the loaded CDLL object to pass to Report().
    """
    lib_path = os.path.abspath(lib_path)
    lib_dir  = os.path.dirname(lib_path)
    lib_name = os.path.basename(lib_path)

    # Set RDLNATIVE_LIB_DIR before loading so rdl_init() can register a DllImportResolver
    # that finds P/Invoke sibling libraries (libSkiaSharp.so etc.) in this directory.
    os.environ['RDLNATIVE_LIB_DIR'] = lib_dir

    # Pre-load all shared libraries in the directory (including rdlnative itself) with
    # RTLD_GLOBAL before the final load below.  On .NET 10+, runtime components
    # (libSystem.Native.so etc.) are shared libraries whose symbols must be globally
    # visible for rdlnative.so to load and resolve P/Invoke calls correctly.
    # Loading rdlnative.so with RTLD_GLOBAL is also required: it ensures .NET's
    # P/Invoke resolver finds sibling libraries (libSkiaSharp.so, libe_sqlite3.so)
    # through the OS SONAME cache rather than the host-process search path.
    ext = '.dylib' if platform.system() == 'Darwin' else '.so'
    for sibling in sorted(glob.glob(os.path.join(lib_dir, f'*{ext}'))):
        try:
            ctypes.CDLL(sibling, ctypes.RTLD_GLOBAL)
        except OSError:
            pass

    # Returns the already-loaded RTLD_GLOBAL handle (from the loop above).
    lib = ctypes.CDLL(lib_path)

    lib.rdl_init.restype = ctypes.c_int
    lib.rdl_init.argtypes = []

    lib.rdl_report_open.restype = ctypes.c_void_p
    lib.rdl_report_open.argtypes = [ctypes.c_char_p, ctypes.c_char_p]

    lib.rdl_report_set_param.restype = ctypes.c_int
    lib.rdl_report_set_param.argtypes = [ctypes.c_void_p, ctypes.c_char_p, ctypes.c_char_p]

    lib.rdl_dataset_set_field.restype = ctypes.c_int
    lib.rdl_dataset_set_field.argtypes = [ctypes.c_void_p, ctypes.c_char_p, ctypes.c_char_p, ctypes.c_char_p]

    lib.rdl_dataset_commit_row.restype = ctypes.c_int
    lib.rdl_dataset_commit_row.argtypes = [ctypes.c_void_p, ctypes.c_char_p]

    lib.rdl_report_render_file.restype = ctypes.c_int
    lib.rdl_report_render_file.argtypes = [ctypes.c_void_p, ctypes.c_char_p, ctypes.c_char_p]

    lib.rdl_report_render_buffer.restype = ctypes.c_int
    lib.rdl_report_render_buffer.argtypes = [
        ctypes.c_void_p,
        ctypes.c_char_p,
        ctypes.POINTER(ctypes.c_void_p),
        ctypes.POINTER(ctypes.c_int),
    ]

    lib.rdl_free.restype = None
    lib.rdl_free.argtypes = [ctypes.c_void_p]

    lib.rdl_report_close.restype = None
    lib.rdl_report_close.argtypes = [ctypes.c_void_p]

    lib.rdl_last_error.restype = ctypes.c_char_p
    lib.rdl_last_error.argtypes = []

    ret = lib.rdl_init()
    if ret != 0:
        err = lib.rdl_last_error()
        raise RuntimeError(f"rdl_init failed: {_decode(err)}")

    return lib


VALID_TYPES = frozenset({"pdf", "csv", "xlsx", "xlsx_table", "xml", "rtf", "tif", "tifb", "html", "mht"})


class Report:
    """
    In-process report renderer backed by the rdlnative shared library.

    Unlike report.py / report_aot.py, rendering happens inside this process:
    no subprocess is created, no .NET runtime is required.
    """

    def __init__(self, lib: ctypes.CDLL, report_path: str):
        """
        Parameters
        ----------
        lib         : CDLL returned by load_library()
        report_path : path to the .rdl file
        """
        self._lib = lib
        self._report_path = report_path
        self._connection_string: str | None = None
        self._parameters: dict[str, str] = {}
        self._data_sets: dict[str, list[dict[str, str]]] = {}

    def set_parameter(self, name: str, value: str) -> None:
        """
        Set a report parameter value.
          name  - parameter name as declared in the RDL
          value - parameter value (string)
        """
        self._parameters[name] = value

    def set_connection_string(self, connection_string: str) -> None:
        """Override the connection string defined in the RDL."""
        self._connection_string = connection_string

    def add_data(self, dataset_name: str, rows: list[dict[str, str]]) -> None:
        """
        Supply in-memory data for a named dataset, bypassing any database query.

        Parameters
        ----------
        dataset_name : name of the DataSet element in the RDL (e.g. "Data")
        rows         : list of dicts mapping field name → value (all values as strings).
                       Field names must match the <Field Name="..."> values in the RDL.

        SkipDatabaseSchemaValidation is set automatically when any dataset rows are
        present, so no DB connection is needed at parse or render time.

        Example::

            rpt.add_data("Data", [
                {"Product": "Chai",  "Region": "North America", "Amount": "1250.00", "Quantity": "50"},
                {"Product": "Chang", "Region": "North America", "Amount":  "980.50", "Quantity": "42"},
            ])
        """
        self._data_sets[dataset_name] = list(rows)

    def export(self, type: str, export_path: str) -> None:
        """
        Render the report and save it to *export_path*.
          type        - output format (defaults to "pdf")
          export_path - destination file path
        """
        fmt = type if type in VALID_TYPES else "pdf"
        with self._handle() as h:
            ret = self._lib.rdl_report_render_file(
                h, export_path.encode("utf-8"), fmt.encode("utf-8")
            )
            if ret != 0:
                self._raise("rdl_report_render_file")

    def export_to_memory(self, type: str) -> bytes:
        """
        Render the report and return the output as bytes.
        No temporary files are written — the data is returned directly from
        the native library's in-memory buffer.
          type - output format (defaults to "pdf")
        """
        fmt = type if type in VALID_TYPES else "pdf"
        with self._handle() as h:
            out_data = ctypes.c_void_p(0)
            out_size = ctypes.c_int(0)
            ret = self._lib.rdl_report_render_buffer(
                h, fmt.encode("utf-8"),
                ctypes.byref(out_data), ctypes.byref(out_size),
            )
            if ret != 0:
                self._raise("rdl_report_render_buffer")
            try:
                return ctypes.string_at(out_data.value, out_size.value)
            finally:
                self._lib.rdl_free(out_data)

    # ─── Internal helpers ────────────────────────────────────────────────────

    @contextlib.contextmanager
    def _handle(self):
        cs = self._connection_string.encode("utf-8") if self._connection_string else None
        h = self._lib.rdl_report_open(self._report_path.encode("utf-8"), cs)
        if not h:
            self._raise("rdl_report_open")
        try:
            for name, value in self._parameters.items():
                ret = self._lib.rdl_report_set_param(
                    h, name.encode("utf-8"), value.encode("utf-8")
                )
                if ret != 0:
                    self._raise("rdl_report_set_param")
            for ds_name, rows in self._data_sets.items():
                ds_enc = ds_name.encode("utf-8")
                for row in rows:
                    for field, value in row.items():
                        ret = self._lib.rdl_dataset_set_field(
                            h, ds_enc, field.encode("utf-8"), str(value).encode("utf-8")
                        )
                        if ret != 0:
                            self._raise("rdl_dataset_set_field")
                    ret = self._lib.rdl_dataset_commit_row(h, ds_enc)
                    if ret != 0:
                        self._raise("rdl_dataset_commit_row")
            yield ctypes.c_void_p(h)
        finally:
            self._lib.rdl_report_close(h)

    def _raise(self, fn: str) -> None:
        err = self._lib.rdl_last_error()
        raise RuntimeError(f"{fn} failed: {_decode(err)}")


def _decode(raw) -> str:
    if raw is None:
        return "unknown error"
    if isinstance(raw, bytes):
        return raw.decode("utf-8")
    return str(raw)
