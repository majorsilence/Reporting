require 'tempfile'
require 'fileutils'

# Report class for use with the AOT or self-contained RdlCmd binary.
#
# Unlike Report in report.rb, this class does not accept a path_to_dotnet argument.
# rdl_cmd_path must point to the native AOT or self-contained executable
# (e.g. RdlCmd on Linux/macOS, RdlCmd.exe on Windows) — no .NET runtime is required.
#
# Usage:
#   require_relative 'report_aot'
#
#   rpt = ReportAot.new('/path/to/report.rdl', '/path/to/RdlCmd')
#   rpt.set_parameter('Country', 'Germany')
#   rpt.set_connection_string('Data Source=myserver.db')
#   rpt.export('pdf', '/tmp/output.pdf')
#
# Supported export types: "pdf", "csv", "xlsx", "xlsx_table", "xml", "rtf", "tif", "tifb", "html", "mht".
class ReportAot

  VALID_TYPES = %w[pdf csv xlsx xlsx_table xml rtf tif tifb html mht].freeze
  BINARY_TYPES = %w[pdf tif tifb rtf xlsx xlsx_table].freeze

  def initialize(report_path, rdl_cmd_path)
    @report_path   = report_path
    @rdl_cmd_path  = rdl_cmd_path
    @parameters    = {}
    @connection_string = nil
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

  # Render the report and save it to a file.
  #   type        - output format: "pdf", "csv", "xlsx", "xlsx_table", "xml",
  #                 "rtf", "tif", "tifb", "html", "mht". Defaults to "pdf".
  #   export_path - destination file path
  def export(type, export_path)
    type = 'pdf' unless VALID_TYPES.include?(type)

    tmp = Tempfile.new('majorsilencereporting')
    temp_name   = tmp.path
    temp_folder = File.dirname(temp_name)
    tmp.close
    FileUtils.cp(@report_path, temp_name)

    rdl_arg = '/f' + temp_name
    @parameters.each_with_index do |(key, value), i|
      rdl_arg += (i == 0 ? '?' : '&') + key + '=' + value
    end

    cmd = [@rdl_cmd_path, rdl_arg, '/t' + type, '/o' + temp_folder]
    cmd << '/c' + @connection_string if @connection_string

    IO.popen(cmd) { |io| io.readlines }

    temp_out = File.join(temp_folder, File.basename(temp_name) + '.' + type)
    FileUtils.cp(temp_out, export_path)
    File.delete(temp_name)
    File.delete(temp_out)
  end

  # Render the report and return the output.
  # Returns binary String for pdf/tif/tifb/rtf/xlsx; UTF-8 String for text formats.
  #   type - same values as export(). Defaults to "pdf".
  def export_to_memory(type)
    type = 'pdf' unless VALID_TYPES.include?(type)

    tmp = Tempfile.new('majorsilencereporting')
    temp_name = tmp.path
    tmp.close
    tmp.unlink

    export(type, temp_name)

    data = if BINARY_TYPES.include?(type)
      File.binread(temp_name)
    else
      File.read(temp_name, encoding: 'UTF-8')
    end

    File.delete(temp_name) if File.exist?(temp_name)
    data
  end

end
