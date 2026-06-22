require 'fiddle'
require 'fiddle/import'
require 'tempfile'

# report_native.rb — Ruby Fiddle wrapper for the rdlnative shared library.
#
# Loads the Majorsilence Reporting engine in-process via Fiddle — no subprocess
# is spawned, no .NET runtime is required on the host.
#
# Platform-specific library filenames:
#   Linux:   librdlnative.so
#   macOS:   librdlnative.dylib
#   Windows: rdlnative.dll
#
# Usage:
#   require_relative 'report_native'
#
#   lib = RdlLibrary.load('/path/to/librdlnative.so')
#
#   rpt = ReportNative.new(lib, '/path/to/report.rdl')
#   rpt.set_parameter('Country', 'Germany')
#   rpt.set_connection_string('Data Source=myserver.db')
#
#   # Export to a file
#   rpt.export('pdf', '/tmp/output.pdf')
#
#   # Export to bytes
#   data = rpt.export_to_memory('pdf')
#
# Supported export types: "pdf", "csv", "xlsx", "xlsx_table", "xml", "rtf",
#                         "tif", "tifb", "html", "mht"

module RdlLibrary
  extend Fiddle::Importer

  # Load the rdlnative shared library and initialize the engine.
  # Returns a Hash of bound Fiddle::Function objects.
  # Call this once per process before creating any ReportNative instances.
  def self.load(lib_path)
    lib_path = File.expand_path(lib_path)
    lib_dir  = File.dirname(lib_path)

    # Tell the C# resolver where to find P/Invoke sibling libraries (libSkiaSharp.so,
    # libe_sqlite3.so, etc.) — must be set before rdl_init() is called.
    ENV['RDLNATIVE_LIB_DIR'] = lib_dir

    # Pre-load all shared libraries in the directory with RTLD_GLOBAL (Fiddle's
    # default).  On .NET 10+, libSystem.Native.so and sibling P/Invoke targets are
    # shared libraries — they must be globally visible before rdlnative.so runs.
    ext = RUBY_PLATFORM =~ /darwin/ ? '*.dylib' : '*.so'
    Dir.glob(File.join(lib_dir, ext)).sort.each do |f|
      begin Fiddle.dlopen(f) rescue Fiddle::DLError; end
    end

    handle = Fiddle.dlopen(lib_path)

    fns = {
      rdl_init: Fiddle::Function.new(
        handle['rdl_init'], [], Fiddle::TYPE_INT
      ),
      rdl_report_open: Fiddle::Function.new(
        handle['rdl_report_open'],
        [Fiddle::TYPE_VOIDP, Fiddle::TYPE_VOIDP],
        Fiddle::TYPE_VOIDP
      ),
      rdl_report_set_param: Fiddle::Function.new(
        handle['rdl_report_set_param'],
        [Fiddle::TYPE_VOIDP, Fiddle::TYPE_VOIDP, Fiddle::TYPE_VOIDP],
        Fiddle::TYPE_INT
      ),
      rdl_report_render_file: Fiddle::Function.new(
        handle['rdl_report_render_file'],
        [Fiddle::TYPE_VOIDP, Fiddle::TYPE_VOIDP, Fiddle::TYPE_VOIDP],
        Fiddle::TYPE_INT
      ),
      rdl_free: Fiddle::Function.new(
        handle['rdl_free'], [Fiddle::TYPE_VOIDP], Fiddle::TYPE_VOID
      ),
      rdl_report_close: Fiddle::Function.new(
        handle['rdl_report_close'], [Fiddle::TYPE_VOIDP], Fiddle::TYPE_VOID
      ),
      rdl_last_error: Fiddle::Function.new(
        handle['rdl_last_error'], [], Fiddle::TYPE_VOIDP
      ),
    }

    ret = fns[:rdl_init].call
    raise "rdl_init failed: #{rdl_last_error_str(fns)}" unless ret.zero?

    fns
  end

  def self.rdl_last_error_str(fns)
    ptr = fns[:rdl_last_error].call
    return 'unknown error' if ptr.nil? || ptr.zero?
    Fiddle::Pointer.new(ptr).to_s
  end
end

class ReportNative
  VALID_TYPES   = %w[pdf csv xlsx xlsx_table xml rtf tif tifb html mht].freeze

  def initialize(fns, report_path)
    @fns              = fns
    @report_path      = report_path
    @connection_string = nil
    @parameters       = {}
  end

  # Set a report parameter value.
  #   name  - parameter name as declared in the RDL
  #   value - parameter value (string)
  def set_parameter(name, value)
    @parameters[name] = value
  end

  # Override the connection string defined in the RDL.
  def set_connection_string(connection_string)
    @connection_string = connection_string
  end

  # Render the report and save it to export_path.
  #   type        - output format (defaults to "pdf")
  #   export_path - destination file path
  def export(type, export_path)
    fmt = VALID_TYPES.include?(type) ? type : 'pdf'
    with_handle do |h|
      ret = @fns[:rdl_report_render_file].call(h, c_str(export_path), c_str(fmt))
      raise_last_error('rdl_report_render_file') unless ret.zero?
    end
  end

  # Render the report and return the output.
  # Returns a binary String for all formats.
  #   type - output format (defaults to "pdf")
  def export_to_memory(type)
    fmt = VALID_TYPES.include?(type) ? type : 'pdf'
    tmp = Tempfile.new(['rdlnative', ".#{fmt}"], binmode: true)
    tmp_path = tmp.path
    tmp.close
    begin
      export(fmt, tmp_path)
      File.binread(tmp_path)
    ensure
      File.delete(tmp_path) if File.exist?(tmp_path)
    end
  end

  private

  # Open a native handle, yield it with params applied, then close it.
  def with_handle
    cs_ptr = @connection_string ? c_str(@connection_string) : Fiddle::NULL
    h = @fns[:rdl_report_open].call(c_str(@report_path), cs_ptr)
    raise_last_error('rdl_report_open') if h.nil? || h.zero?
    begin
      @parameters.each do |name, value|
        ret = @fns[:rdl_report_set_param].call(h, c_str(name), c_str(value))
        raise_last_error('rdl_report_set_param') unless ret.zero?
      end
      yield h
    ensure
      @fns[:rdl_report_close].call(h)
    end
  end

  def c_str(str)
    Fiddle::Pointer[str.encode('UTF-8') + "\0"]
  end

  def raise_last_error(fn)
    ptr = @fns[:rdl_last_error].call
    msg = (ptr && !ptr.zero?) ? Fiddle::Pointer.new(ptr).to_s : 'unknown error'
    raise "#{fn} failed: #{msg}"
  end
end
