<?php
namespace MajorsilenceReporting;

/**
 * Report class for use with the AOT or self-contained RdlCmd binary.
 *
 * Unlike Report in report.php, this class does not accept a $dotnet_path argument.
 * $rdl_cmd_path must point to the native AOT or self-contained executable
 * (e.g. RdlCmd on Linux/macOS, RdlCmd.exe on Windows) — no .NET runtime is required.
 *
 * Usage:
 *   require_once 'report_aot.php';
 *   use MajorsilenceReporting\ReportAot;
 *
 *   $rpt = new ReportAot('/path/to/report.rdl', '/path/to/RdlCmd');
 *   $rpt->set_parameter('Country', 'Germany');
 *   $rpt->set_connection_string('Data Source=myserver.db');
 *   $rpt->export('pdf', '/tmp/output.pdf');
 *
 * Supported export types: "pdf", "csv", "xlsx", "xlsx_table", "xml", "rtf", "tif", "tifb", "html", "mht".
 */
class ReportAot {

	private $report_path = "";
	private $rdl_cmd_path = "";
	private $rdlcmd_dir = "";
	private $connection_string = "";
	private $parameters = [];

	public function __construct(string $report_path, string $rdl_cmd_path) {
		$this->report_path  = $report_path;
		$this->rdl_cmd_path = $rdl_cmd_path;
		$this->rdlcmd_dir   = dirname($rdl_cmd_path);
	}

	/**
	 * Set a report parameter value.
	 * @param string $name  - parameter name as declared in the RDL
	 * @param string $value - parameter value
	 */
	public function set_parameter(string $name, string $value): void {
		$this->parameters[$name] = $value;
	}

	/**
	 * Override the connection string defined in the RDL.
	 * @param string $connection_string
	 */
	public function set_connection_string(string $connection_string): void {
		$this->connection_string = $connection_string;
	}

	/**
	 * Render the report and save it to a file.
	 * @param string $type        - output format: "pdf", "csv", "xlsx", "xlsx_table", "xml",
	 *                              "rtf", "tif", "tifb", "html", "mht". Defaults to "pdf".
	 * @param string $export_path - destination file path
	 */
	public function export(string $type, string $export_path): void {
		$valid = ["pdf", "csv", "xlsx", "xlsx_table", "xml", "rtf", "tif", "tifb", "html", "mht"];
		if (!in_array($type, $valid)) {
			$type = "pdf";
		}

		$temp_folder = sys_get_temp_dir();
		$temp_name   = tempnam($temp_folder, "majorsilencereporting");
		copy($this->report_path, $temp_name);

		$rdl_arg = '/f' . $temp_name;
		$count = 0;
		foreach ($this->parameters as $key => $value) {
			$rdl_arg .= ($count === 0 ? '?' : '&') . $key . '=' . $value;
			$count++;
		}

		$cmd = '"' . $this->rdl_cmd_path . '" '
		     . '"' . $rdl_arg             . '" '
		     . '"/t' . $type              . '" '
		     . '"/o' . $temp_folder       . '" ';

		if (!empty($this->connection_string)) {
			$cmd .= '"/c' . $this->connection_string . '" ';
		}

		$cdir = getcwd();
		chdir($this->rdlcmd_dir);
		shell_exec($cmd);
		chdir($cdir);

		$sep      = DIRECTORY_SEPARATOR;
		$temp_out = rtrim($temp_folder, $sep) . $sep . basename($temp_name) . '.' . $type;
		copy($temp_out, $export_path);
		unlink($temp_name);
		unlink($temp_out);
	}

	/**
	 * Render the report and return the output as a string.
	 * Binary formats (pdf, tif, rtf, xlsx) are returned as raw binary strings.
	 * @param string $type - same values as export(). Defaults to "pdf".
	 * @return string
	 */
	public function export_to_memory(string $type): string {
		$valid = ["pdf", "csv", "xlsx", "xlsx_table", "xml", "rtf", "tif", "tifb", "html", "mht"];
		if (!in_array($type, $valid)) {
			$type = "pdf";
		}

		$temp_name = tempnam(sys_get_temp_dir(), "majorsilencereporting");
		$this->export($type, $temp_name);
		$data = file_get_contents($temp_name);
		unlink($temp_name);
		return $data;
	}
}
