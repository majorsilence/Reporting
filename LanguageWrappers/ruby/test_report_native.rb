#!/usr/bin/env ruby
# Unit tests for report_native.rb — the Ruby Fiddle wrapper for rdlnative.
#
# Requires a published rdlnative shared library.  Set RDLNATIVE_LIB to the
# path of rdlnative.so/.dylib/.dll before running, or the tests are skipped.
#
# Example (Linux):
#   dotnet publish RdlNative/... -p:PublishAot=true -o /tmp/rdlnative-pub
#   RDLNATIVE_LIB=/tmp/rdlnative-pub/rdlnative.so ruby test_report_native.rb

require 'minitest/autorun'
require 'tempfile'

LIB_PATH  = ENV.fetch('RDLNATIVE_LIB', '')
REPO_ROOT = File.expand_path('../../..', __FILE__)
RDL_PATH  = File.join(REPO_ROOT, 'Examples', 'SqliteExamples', 'SimpleTest1.rdl')
DB_PATH   = File.join(REPO_ROOT, 'Examples', 'northwindEF.db')
DB_CS     = "Data Source=#{DB_PATH}"

def library_available?
  LIB_PATH != '' && File.file?(LIB_PATH) &&
    File.file?(RDL_PATH) && File.file?(DB_PATH)
end

# Load wrapper and library once, lazily.
$fns = nil
def fns
  return $fns if $fns
  require_relative 'report_native'
  $fns = RdlLibrary.load(LIB_PATH)
end

# Helper: build a connected report object
def make_report
  require_relative 'report_native'
  rpt = ReportNative.new(fns, RDL_PATH)
  rpt.set_connection_string(DB_CS)
  rpt
end


class TestBasicRender < Minitest::Test
  def setup
    skip 'RDLNATIVE_LIB not set or library / sample files not found' unless library_available?
  end

  def test_pdf_memory
    data = make_report.export_to_memory('pdf')
    assert data.bytesize > 1000
    assert_equal '%PDF', data[0, 4].force_encoding('BINARY')
  end

  def test_html_memory
    data = make_report.export_to_memory('html')
    assert data.bytesize > 100
    assert_match(/<html/i, data.force_encoding('UTF-8'))
  end

  def test_csv_memory
    data = make_report.export_to_memory('csv')
    assert data.bytesize > 0
    assert_includes data.force_encoding('UTF-8'), 'Simple Test'
  end

  def test_xml_memory
    data = make_report.export_to_memory('xml')
    assert data.bytesize > 0
    assert_match(/<\?xml/i, data.force_encoding('UTF-8'))
  end

  def test_pdf_to_file
    Tempfile.create(['rdlnative_test', '.pdf']) do |f|
      path = f.path
      make_report.export('pdf', path)
      assert File.size(path) > 1000
    end
  end

  def test_multiple_renders_same_report
    rpt  = make_report
    pdf1 = rpt.export_to_memory('pdf')
    pdf2 = rpt.export_to_memory('pdf')
    assert_equal pdf1.bytesize, pdf2.bytesize
  end
end


class TestConnectionAndParameters < Minitest::Test
  def setup
    skip 'RDLNATIVE_LIB not set or library / sample files not found' unless library_available?
  end

  def test_set_connection_string
    require_relative 'report_native'
    rpt = ReportNative.new(fns, RDL_PATH)
    rpt.set_connection_string(DB_CS)
    data = rpt.export_to_memory('csv')
    assert_includes data.force_encoding('UTF-8'), 'Simple Test'
  end

  def test_set_parameter_does_not_crash
    require_relative 'report_native'
    rpt = ReportNative.new(fns, RDL_PATH)
    rpt.set_connection_string(DB_CS)
    rpt.set_parameter('SomeParam', 'SomeValue')
    data = rpt.export_to_memory('csv')
    assert data.bytesize > 0
  end
end


class TestErrorHandling < Minitest::Test
  def setup
    skip 'RDLNATIVE_LIB not set or library / sample files not found' unless library_available?
  end

  def test_invalid_rdl_path_raises
    require_relative 'report_native'
    rpt = ReportNative.new(fns, '/nonexistent/report.rdl')
    assert_raises(RuntimeError) { rpt.export_to_memory('pdf') }
  end

  def test_unknown_format_defaults_to_pdf
    data = make_report.export_to_memory('not_a_format')
    assert_equal '%PDF', data[0, 4].force_encoding('BINARY')
  end
end
