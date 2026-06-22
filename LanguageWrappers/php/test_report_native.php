<?php
/**
 * Unit tests for report_native.php — the PHP FFI wrapper for rdlnative.
 *
 * Requires:
 *   - PHP FFI extension enabled (php.ini: extension=ffi, ffi.enable=true)
 *   - Published rdlnative shared library
 *   - RDLNATIVE_LIB environment variable set to the library path
 *   - LD_LIBRARY_PATH including the library directory (required before PHP starts
 *     because PHP's FFI loads with RTLD_LOCAL; .NET runtime libs must already
 *     be findable by the dynamic linker)
 *
 * Usage (Linux):
 *   dotnet publish RdlNative/... -p:PublishAot=true -o /tmp/rdlnative-pub
 *   export RDLNATIVE_LIB=/tmp/rdlnative-pub/rdlnative.so
 *   export LD_LIBRARY_PATH=/tmp/rdlnative-pub:$LD_LIBRARY_PATH
 *   php test_report_native.php
 *
 * Exit code 0 = all passed.  Each failure prints to stderr.
 */

declare(strict_types=1);

require_once __DIR__ . '/report_native.php';

use MajorsilenceReporting\RdlLibrary;
use MajorsilenceReporting\ReportNative;

// ─── Setup ────────────────────────────────────────────────────────────────────

$LIB_PATH  = (string)getenv('RDLNATIVE_LIB');
$REPO_ROOT = dirname(__DIR__, 2);
$RDL_PATH  = $REPO_ROOT . '/Examples/SqliteExamples/SimpleTest1.rdl';
$DB_PATH   = $REPO_ROOT . '/Examples/northwindEF.db';
$DB_CS     = "Data Source={$DB_PATH}";

function skip_if_unavailable(string $lib, string $rdl, string $db): void
{
    if ($lib === '') {
        fwrite(STDERR, "SKIP: RDLNATIVE_LIB not set.\n");
        exit(0);
    }
    if (!is_file($lib)) {
        fwrite(STDERR, "SKIP: RDLNATIVE_LIB={$lib} does not exist.\n");
        exit(0);
    }
    if (!is_file($rdl)) {
        fwrite(STDERR, "SKIP: Sample RDL not found at {$rdl}\n");
        exit(0);
    }
    if (!is_file($db)) {
        fwrite(STDERR, "SKIP: Sample DB not found at {$db}\n");
        exit(0);
    }
    if (!extension_loaded('ffi')) {
        fwrite(STDERR, "SKIP: PHP FFI extension not loaded.\n");
        exit(0);
    }
}

skip_if_unavailable($LIB_PATH, $RDL_PATH, $DB_PATH);

// ─── Mini test harness ────────────────────────────────────────────────────────

$passed = 0;
$failed = 0;

function run_test(string $name, callable $fn): void
{
    global $passed, $failed;
    try {
        $fn();
        echo "PASS  {$name}\n";
        $passed++;
    } catch (Throwable $e) {
        echo "FAIL  {$name}: {$e->getMessage()}\n";
        $failed++;
    }
}

function assert_true(mixed $value, string $msg = ''): void
{
    if (!$value) {
        throw new \AssertionError($msg ?: "Expected true, got false");
    }
}

function assert_greater(int $a, int $b, string $msg = ''): void
{
    if ($a <= $b) {
        throw new \AssertionError($msg ?: "Expected {$a} > {$b}");
    }
}

function assert_contains(string $needle, string $haystack, string $msg = ''): void
{
    if (strpos($haystack, $needle) === false) {
        throw new \AssertionError($msg ?: "Expected to find {$needle}");
    }
}

// ─── Shared library handle (loaded once) ─────────────────────────────────────

$ffi = null;
function get_ffi(): \FFI
{
    global $ffi, $LIB_PATH;
    if ($ffi === null) {
        $ffi = RdlLibrary::load($LIB_PATH);
    }
    return $ffi;
}

function make_report(): ReportNative
{
    global $RDL_PATH, $DB_CS;
    $rpt = new ReportNative(get_ffi(), $RDL_PATH);
    $rpt->set_connection_string($DB_CS);
    return $rpt;
}

// ─── Tests: Basic render ──────────────────────────────────────────────────────

run_test('test_pdf_memory', function () {
    $data = make_report()->export_to_memory('pdf');
    assert_greater(strlen($data), 1000);
    assert_true(substr($data, 0, 4) === '%PDF', 'Expected PDF magic bytes');
});

run_test('test_html_memory', function () {
    $data = make_report()->export_to_memory('html');
    assert_greater(strlen($data), 100);
    assert_contains('<html', strtolower($data));
});

run_test('test_csv_memory', function () {
    $data = make_report()->export_to_memory('csv');
    assert_greater(strlen($data), 0);
    assert_contains('Simple Test', $data);
});

run_test('test_xml_memory', function () {
    $data = make_report()->export_to_memory('xml');
    assert_greater(strlen($data), 0);
    assert_contains('<?xml', $data);
});

run_test('test_pdf_to_file', function () {
    $path = tempnam(sys_get_temp_dir(), 'rdlnative_') . '.pdf';
    try {
        make_report()->export('pdf', $path);
        assert_greater((int)filesize($path), 1000);
    } finally {
        if (file_exists($path)) unlink($path);
    }
});

run_test('test_multiple_renders_same_report', function () {
    $rpt  = make_report();
    $pdf1 = $rpt->export_to_memory('pdf');
    $pdf2 = $rpt->export_to_memory('pdf');
    if (strlen($pdf1) !== strlen($pdf2)) {
        throw new \AssertionError('Repeated render produced different sizes');
    }
});

// ─── Tests: Connection string and parameters ──────────────────────────────────

run_test('test_set_connection_string', function () {
    global $RDL_PATH, $DB_CS;
    $rpt = new ReportNative(get_ffi(), $RDL_PATH);
    $rpt->set_connection_string($DB_CS);
    $data = $rpt->export_to_memory('csv');
    assert_contains('Simple Test', $data);
});

run_test('test_set_parameter_does_not_crash', function () {
    $rpt = make_report();
    $rpt->set_parameter('SomeParam', 'SomeValue');
    $data = $rpt->export_to_memory('csv');
    assert_greater(strlen($data), 0);
});

// ─── Tests: Error handling ────────────────────────────────────────────────────

run_test('test_invalid_rdl_path_raises', function () {
    $rpt = new ReportNative(get_ffi(), '/nonexistent/report.rdl');
    $raised = false;
    try {
        $rpt->export_to_memory('pdf');
    } catch (\RuntimeException $e) {
        $raised = true;
        assert_greater(strlen($e->getMessage()), 0);
    }
    assert_true($raised, 'Expected RuntimeException for missing RDL');
});

run_test('test_unknown_format_defaults_to_pdf', function () {
    $data = make_report()->export_to_memory('not_a_format');
    assert_true(substr($data, 0, 4) === '%PDF', 'Unknown format should default to PDF');
});

// ─── Summary ──────────────────────────────────────────────────────────────────

echo "\n{$passed} passed, {$failed} failed.\n";
exit($failed > 0 ? 1 : 0);
