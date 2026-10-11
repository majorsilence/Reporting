

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Majorsilence.Reporting.RdlEngine.Resources;


namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Error logging (parse and runtime) within report.
	///</summary>
	[Serializable]
	internal class ReportLog
	{
		List<string> _ErrorItems=null;			// list of report items
		int _MaxSeverity=0;				// maximum severity encountered				

		internal ReportLog()
		{
		}

		internal ReportLog(ReportLog rl)
		{
			if (rl != null && rl.ErrorItems != null)
			{
				_MaxSeverity = rl.MaxSeverity;
                _ErrorItems = new List<string>(rl.ErrorItems);
			}
		}

		internal void LogError(ReportLog rl)
		{
			if (rl.ErrorItems.Count == 0)
				return;
			LogError(rl.MaxSeverity, rl.ErrorItems);
		}

		// A log can be shared: run-time messages with no Report to hold them go to the definition's log, and a
		// definition may be used by several reports at once (CompiledReport).
		readonly object _lock = new object();

		internal void LogError(int severity, string item)
		{
			var msg = Strings.ReportLog_Error_Severity + ": " + Convert.ToString(severity) + " - " + item;

			lock (_lock)
			{
				if (_ErrorItems == null)			// create log if first time
					_ErrorItems = new List<string>();

				if (severity > _MaxSeverity)
					_MaxSeverity = severity;

				_ErrorItems.Add(msg);
			}

			if (severity >= 12)		
				throw new Exception(msg);		// terminate the processing
		}

		internal void LogError(int severity, List<string> list)
		{
			lock (_lock)
			{
				if (_ErrorItems == null)			// create log if first time
					_ErrorItems = new List<string>();

				if (severity > _MaxSeverity)
					_MaxSeverity = severity;

				_ErrorItems.AddRange(list);
			}
		}

		internal void Reset()
		{
			_ErrorItems=null;
			if (_MaxSeverity < 8)				// we keep the severity to indicate we can't run report
				_MaxSeverity=0;
		}

        internal List<string> ErrorItems
		{
			get { return  _ErrorItems; }
		}


		internal int MaxSeverity
		{
			get { return  _MaxSeverity; }
			set {  _MaxSeverity = value; }
		}
	}
}
