

using System;
using System.Collections;
using Majorsilence.Reporting.Rdl;

namespace Majorsilence.Reporting.RdlAsp
{
	/// <summary>
	/// RdlSession holds some session specific information
	/// </summary>
	public class RdlSession
	{
		public static readonly string SessionStat="SessionStat";
		public int Count;
		public RdlSession()
		{
			Count = 0;
		}
	}
}
