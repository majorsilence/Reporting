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

LIB_PATH      = ENV.fetch('RDLNATIVE_LIB', '')
REPO_ROOT     = File.expand_path('../../..', __FILE__)
RDL_PATH      = File.join(REPO_ROOT, 'Examples', 'SqliteExamples', 'SimpleTest1.rdl')
DB_PATH       = File.join(REPO_ROOT, 'Examples', 'northwindEF.db')
DB_CS         = "Data Source=#{DB_PATH}"
SALES_RDL     = File.join(REPO_ROOT, 'Examples', 'SetDataFromCode', 'SalesReport.rdl')

SALES_DATA = [
  { 'Product' => 'Chai',  'Region' => 'North America', 'Amount' => '1250.00', 'Quantity' => '50' },
  { 'Product' => 'Chang', 'Region' => 'Europe',         'Amount' =>  '980.50', 'Quantity' => '42' },
  { 'Product' => 'Tofu',  'Region' => 'Asia Pacific',   'Amount' =>  '560.00', 'Quantity' => '40' },
].freeze

def library_available?
  LIB_PATH != '' && File.file?(LIB_PATH) &&
    File.file?(RDL_PATH) && File.file?(DB_PATH)
end

def sales_rdl_available?
  library_available? && File.file?(SALES_RDL)
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


class TestAddData < Minitest::Test
  # add_data injects in-memory rows — no database connection required.

  def setup
    skip 'RDLNATIVE_LIB not set or library / sample files not found' unless sales_rdl_available?
  end

  def sales_report
    require_relative 'report_native'
    rpt = ReportNative.new(fns, SALES_RDL)
    rpt.add_data('Data', SALES_DATA)
    rpt
  end

  def test_pdf_returns_valid_pdf
    data = sales_report.export_to_memory('pdf')
    assert data.bytesize > 1000
    assert_equal '%PDF', data[0, 4].force_encoding('BINARY')
  end

  def test_csv_contains_injected_rows
    text = sales_report.export_to_memory('csv').force_encoding('UTF-8')
    assert_includes text, 'Chai'
    assert_includes text, 'Chang'
    assert_includes text, 'Tofu'
  end

  def test_export_to_file
    Tempfile.create(['rdlnative_sales', '.pdf']) do |f|
      path = f.path
      sales_report.export('pdf', path)
      assert File.size(path) > 1000
    end
  end

  def test_no_connection_string_needed
    require_relative 'report_native'
    # add_data bypasses the DB entirely — no set_connection_string call needed
    rpt = ReportNative.new(fns, SALES_RDL)
    rpt.add_data('Data', SALES_DATA)
    data = rpt.export_to_memory('csv')
    assert data.bytesize > 0
  end

  def test_all_rows_present_in_csv
    text = sales_report.export_to_memory('csv').force_encoding('UTF-8')
    SALES_DATA.each do |row|
      assert_includes text, row['Product']
    end
  end

  def test_empty_dataset_does_not_crash
    require_relative 'report_native'
    rpt = ReportNative.new(fns, SALES_RDL)
    rpt.add_data('Data', [])
    data = rpt.export_to_memory('pdf')
    assert_equal '%PDF', data[0, 4].force_encoding('BINARY')
  end
end
