"""
Unit tests for report_native.py — the Python FFI wrapper for rdlnative.

The tests require a published rdlnative shared library and the SimpleTest1.rdl
sample report.  Set the environment variable RDLNATIVE_LIB to point at the
rdlnative.so (or .dylib / .dll) to run.  Without it the tests are skipped.

Example:

    dotnet publish RdlNative/... -o /tmp/rdlnative-pub
    RDLNATIVE_LIB=/tmp/rdlnative-pub/rdlnative.so python -m pytest LanguageWrappers/python/
"""

import os
import sys
import tempfile
import unittest

# Path to the shared library under test (set via env var or skip).
LIB_PATH = os.environ.get("RDLNATIVE_LIB", "")

# Paths to the sample RDL and SQLite database shipped in the repo.
REPO_ROOT      = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
RDL_PATH       = os.path.join(REPO_ROOT, "Examples", "SqliteExamples", "SimpleTest1.rdl")
DB_PATH        = os.path.join(REPO_ROOT, "Examples", "northwindEF.db")
DB_CS          = f"Data Source={DB_PATH}"
SALES_RDL_PATH = os.path.join(REPO_ROOT, "Examples", "SetDataFromCode", "SalesReport.rdl")

SALES_DATA = [
    {"Product": "Chai",  "Region": "North America", "Amount": "1250.00", "Quantity": "50"},
    {"Product": "Chang", "Region": "Europe",         "Amount":  "980.50", "Quantity": "42"},
    {"Product": "Tofu",  "Region": "Asia Pacific",   "Amount":  "560.00", "Quantity": "40"},
]


def setUpModule():
    if not LIB_PATH:
        raise unittest.SkipTest(
            "RDLNATIVE_LIB not set — skipping rdlnative tests.\n"
            "Set it to the path of rdlnative.so built with:\n"
            "  dotnet publish RdlNative/... -p:PublishAot=true"
        )
    if not os.path.isfile(LIB_PATH):
        raise unittest.SkipTest(f"RDLNATIVE_LIB={LIB_PATH!r} does not exist")
    if not os.path.isfile(RDL_PATH):
        raise unittest.SkipTest(f"Sample RDL not found at {RDL_PATH!r}")
    if not os.path.isfile(DB_PATH):
        raise unittest.SkipTest(f"Sample DB not found at {DB_PATH!r}")


# Shared library handle — loaded once for the whole module.
_lib = None


def _get_lib():
    global _lib
    if _lib is None:
        sys.path.insert(0, os.path.dirname(__file__))
        from report_native import load_library
        _lib = load_library(LIB_PATH)
    return _lib


class TestBasicRender(unittest.TestCase):
    """Core render-to-memory and render-to-file tests."""

    def _rpt(self):
        from report_native import Report
        rpt = Report(_get_lib(), RDL_PATH)
        rpt.set_connection_string(DB_CS)
        return rpt

    def test_pdf_memory(self):
        data = self._rpt().export_to_memory("pdf")
        self.assertGreater(len(data), 1000)
        self.assertTrue(data[:4] == b"%PDF", "Expected PDF magic bytes")

    def test_html_memory(self):
        data = self._rpt().export_to_memory("html")
        self.assertGreater(len(data), 100)
        text = data.decode("utf-8", errors="replace")
        self.assertIn("<html", text.lower())

    def test_csv_memory(self):
        data = self._rpt().export_to_memory("csv")
        self.assertGreater(len(data), 0)
        text = data.decode("utf-8", errors="replace")
        self.assertIn("Simple Test", text)

    def test_xml_memory(self):
        data = self._rpt().export_to_memory("xml")
        self.assertGreater(len(data), 0)
        text = data.decode("utf-8", errors="replace")
        self.assertIn("<?xml", text)

    def test_pdf_to_file(self):
        rpt = self._rpt()
        with tempfile.NamedTemporaryFile(suffix=".pdf", delete=False) as f:
            out = f.name
        try:
            rpt.export("pdf", out)
            self.assertGreater(os.path.getsize(out), 1000)
        finally:
            if os.path.isfile(out):
                os.unlink(out)

    def test_multiple_renders_same_report(self):
        rpt = self._rpt()
        pdf1 = rpt.export_to_memory("pdf")
        pdf2 = rpt.export_to_memory("pdf")
        self.assertEqual(len(pdf1), len(pdf2))


class TestConnectionStringAndParameters(unittest.TestCase):
    """set_connection_string and set_parameter behaviour."""

    def test_connection_string_override(self):
        from report_native import Report
        rpt = Report(_get_lib(), RDL_PATH)
        rpt.set_connection_string(DB_CS)
        data = rpt.export_to_memory("csv")
        text = data.decode("utf-8", errors="replace")
        self.assertIn("Simple Test", text)

    def test_set_parameter_does_not_crash(self):
        from report_native import Report
        rpt = Report(_get_lib(), RDL_PATH)
        rpt.set_connection_string(DB_CS)
        rpt.set_parameter("SomeParam", "SomeValue")
        data = rpt.export_to_memory("csv")
        self.assertGreater(len(data), 0)


class TestErrorHandling(unittest.TestCase):
    """Error reporting for invalid inputs."""

    def test_invalid_rdl_path_raises(self):
        from report_native import Report
        rpt = Report(_get_lib(), "/nonexistent/report.rdl")
        with self.assertRaises(RuntimeError) as ctx:
            rpt.export_to_memory("pdf")
        self.assertTrue(len(str(ctx.exception)) > 0)

    def test_unknown_format_defaults_to_pdf(self):
        from report_native import Report
        rpt = Report(_get_lib(), RDL_PATH)
        rpt.set_connection_string(DB_CS)
        data = rpt.export_to_memory("not_a_format")
        self.assertTrue(data[:4] == b"%PDF", "Unknown format should default to PDF")


class TestBufferRender(unittest.TestCase):
    """export_to_memory uses the rdl_report_render_buffer C API (true in-memory)."""

    def test_buffer_pdf(self):
        from report_native import Report
        rpt = Report(_get_lib(), RDL_PATH)
        rpt.set_connection_string(DB_CS)
        data = rpt.export_to_memory("pdf")
        self.assertTrue(data[:4] == b"%PDF")

    def test_buffer_csv_content(self):
        from report_native import Report
        rpt = Report(_get_lib(), RDL_PATH)
        rpt.set_connection_string(DB_CS)
        text = rpt.export_to_memory("csv").decode("utf-8", errors="replace")
        self.assertIn("Hello World", text)


class TestAddData(unittest.TestCase):
    """add_data injects in-memory rows — no database connection required."""

    def setUp(self):
        if not os.path.isfile(SALES_RDL_PATH):
            self.skipTest(f"SalesReport.rdl not found at {SALES_RDL_PATH!r}")

    def _rpt(self):
        from report_native import Report
        rpt = Report(_get_lib(), SALES_RDL_PATH)
        rpt.add_data("Data", SALES_DATA)
        return rpt

    def test_pdf_returns_valid_pdf(self):
        data = self._rpt().export_to_memory("pdf")
        self.assertGreater(len(data), 1000)
        self.assertEqual(data[:4], b"%PDF", "Expected PDF magic bytes")

    def test_csv_contains_injected_rows(self):
        text = self._rpt().export_to_memory("csv").decode("utf-8", errors="replace")
        self.assertIn("Chai", text)
        self.assertIn("Chang", text)
        self.assertIn("Tofu", text)

    def test_export_to_file(self):
        rpt = self._rpt()
        with tempfile.NamedTemporaryFile(suffix=".pdf", delete=False) as f:
            out = f.name
        try:
            rpt.export("pdf", out)
            self.assertGreater(os.path.getsize(out), 1000)
        finally:
            if os.path.isfile(out):
                os.unlink(out)

    def test_no_connection_string_needed(self):
        from report_native import Report
        # add_data bypasses the DB entirely — no set_connection_string call needed
        rpt = Report(_get_lib(), SALES_RDL_PATH)
        rpt.add_data("Data", SALES_DATA)
        data = rpt.export_to_memory("csv")
        self.assertGreater(len(data), 0)

    def test_all_rows_present_in_csv(self):
        text = self._rpt().export_to_memory("csv").decode("utf-8", errors="replace")
        for row in SALES_DATA:
            self.assertIn(row["Product"], text)

    def test_empty_dataset_does_not_crash(self):
        from report_native import Report
        rpt = Report(_get_lib(), SALES_RDL_PATH)
        rpt.add_data("Data", [])
        data = rpt.export_to_memory("pdf")
        self.assertEqual(data[:4], b"%PDF")


if __name__ == "__main__":
    unittest.main()
