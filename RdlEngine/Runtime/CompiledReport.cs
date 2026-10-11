using System;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Settings that take effect when a report definition is compiled, and so are fixed for every report made from it.
	/// </summary>
	public sealed class CompiledReportOptions
	{
		/// <summary>The folder subreports, data files and shared data sources are found in.</summary>
		public string Folder { get; set; }
		/// <summary>Replaces the connection string of every data source.</summary>
		public string OverwriteConnectionString { get; set; }
		/// <summary>Applies <see cref="OverwriteConnectionString"/> to subreports too.</summary>
		public bool OverwriteInSubreport { get; set; }
		/// <summary>Do not run the dataset queries at compile time to learn the field names and types.</summary>
		public bool SkipDatabaseSchemaValidation { get; set; }
		/// <summary>Supplies the pass phrase for shared data source files.</summary>
		public NeedPassword DataSourceReferencePassword { get; set; }
	}

	/// <summary>
	/// A report definition parsed once and used for any number of runs, on any number of threads. Parsing (XML,
	/// expressions, any <c>Code</c> compilation, subreports and the database schema queries) is the cost of this
	/// step; <see cref="CreateReport"/> then only makes the small per-run state.
	/// </summary>
	/// <remarks>
	/// Every <see cref="Report"/> made by <see cref="CreateReport"/> must be used by one thread at a time, as always,
	/// and disposed when finished. Disposing it leaves the compiled definition alone. Settings that are part of the
	/// definition (see <see cref="CompiledReportOptions"/>) cannot be changed through such a report. Dispose the
	/// compiled report itself only after every report made from it has finished.
	/// </remarks>
	public sealed class CompiledReport : IDisposable
	{
		private ReportDefn _definition;

		private CompiledReport(ReportDefn definition)
		{
			_definition = definition;
		}

		/// <summary>Parses report definition XML.</summary>
		public static async Task<CompiledReport> CompileAsync(string rdlXml, CompiledReportOptions options = null)
		{
			if (rdlXml == null)
				throw new ArgumentNullException(nameof(rdlXml));

			var parser = new RDLParser(rdlXml);
			if (options != null)
			{
				parser.Folder = options.Folder;
				parser.OverwriteConnectionString = options.OverwriteConnectionString;
				parser.OverwriteInSubreport = options.OverwriteInSubreport;
				parser.SkipDatabaseSchemaValidation = options.SkipDatabaseSchemaValidation;
				parser.DataSourceReferencePassword = options.DataSourceReferencePassword;
			}

			Report parsed = await parser.Parse().ConfigureAwait(false);
			return new CompiledReport(parsed.ReportDefinition);
		}

		/// <summary>Makes a report to run once. Safe to call from any thread.</summary>
		public Report CreateReport()
		{
			ReportDefn definition = _definition;
			if (definition == null)
				throw new ObjectDisposedException(nameof(CompiledReport));
			return new Report(definition, false);
		}

		/// <summary>Releases the definition.</summary>
		public void Dispose()
		{
			ReportDefn definition = System.Threading.Interlocked.Exchange(ref _definition, null);
			definition?.Dispose();
		}
	}
}
