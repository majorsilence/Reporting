import tempfile
import os
import shutil
import subprocess


class Report:
	"""
	Report class for use with the AOT or self-contained RdlCmd binary.

	Unlike report.py, this class does not accept a path_to_dotnet argument.
	The rdl_cmd_path must point to the native AOT or self-contained executable
	(e.g. RdlCmd on Linux/macOS, RdlCmd.exe on Windows) — no .NET runtime is required.

	Usage:
	1. Copy report_aot.py into your project.
	2. Import and instantiate:
		```python
		from report_aot import Report
		rpt = Report(report_path="path/to/report.rdl", rdl_cmd_path="path/to/RdlCmd")
		```
	3. Optionally set parameters and/or a connection string:
		```python
		rpt.set_parameter("Country", "Germany")
		rpt.set_connection_string("Data Source=myserver.db")
		```
	4. Export to a file:
		```python
		rpt.export(type="pdf", export_path="output.pdf")
		```
	5. Or export to memory (returns bytes for binary formats, str for text formats):
		```python
		data = rpt.export_to_memory(type="pdf")
		```

	Supported export types: "pdf", "csv", "xlsx", "xlsx_table", "xml", "rtf", "tif", "tifb", "html", "mht".
	"""

	__report_path = ""
	__parameters = {}
	__connection_string = None
	__rdl_cmd_path = ""

	def __init__(self, report_path: str, rdl_cmd_path: str):
		self.__report_path = report_path
		self.__rdl_cmd_path = rdl_cmd_path
		self.__parameters = {}
		self.__connection_string = None

	def set_parameter(self, name: str, value: str):
		"""
		Set a report parameter value.
		  name  - the parameter name as declared in the RDL
		  value - the parameter value (as a string)
		"""
		self.__parameters[name] = value

	def set_connection_string(self, connection_string: str):
		"""
		Override the connection string defined in the RDL.
		  connection_string - the connection string to use
		"""
		self.__connection_string = connection_string

	def export(self, type: str, export_path: str):
		"""
		Render the report and save it to a file.
		  type        - output format: "pdf", "csv", "xlsx", "xlsx_table", "xml",
		                "rtf", "tif", "tifb", "html", "mht". Defaults to "pdf".
		  export_path - destination file path
		"""
		valid = {"pdf", "csv", "xlsx", "xlsx_table", "xml", "rtf", "tif", "tifb", "html", "mht"}
		if type not in valid:
			type = "pdf"

		fd, temp_name = tempfile.mkstemp()
		os.close(fd)
		temp_folder = os.path.dirname(temp_name)
		shutil.copyfile(self.__report_path, temp_name)

		rdl_path = "/f" + temp_name
		for i, (key, val) in enumerate(self.__parameters.items()):
			rdl_path += ("?" if i == 0 else "&") + key + "=" + val

		cmd = [self.__rdl_cmd_path, rdl_path, "/t" + type, "/o" + temp_folder]

		if self.__connection_string:
			cmd.append("/c" + self.__connection_string)

		subprocess.run(cmd, check=True)

		temp_out = os.path.join(temp_folder, os.path.basename(temp_name) + "." + type)
		shutil.copyfile(temp_out, export_path)
		os.remove(temp_name)
		os.remove(temp_out)

	def export_to_memory(self, type: str) -> bytes | str:
		"""
		Render the report and return the output as bytes (binary formats) or str (text formats).
		  type - same values as export(). Defaults to "pdf".
		"""
		valid = {"pdf", "csv", "xlsx", "xlsx_table", "xml", "rtf", "tif", "tifb", "html", "mht"}
		if type not in valid:
			type = "pdf"

		fd, temp_name = tempfile.mkstemp()
		os.close(fd)
		self.export(type, temp_name)

		binary_types = {"pdf", "tif", "tifb", "rtf", "xlsx", "xlsx_table"}
		with open(temp_name, "rb" if type in binary_types else "r") as f:
			data = f.read()

		os.remove(temp_name)
		return data
