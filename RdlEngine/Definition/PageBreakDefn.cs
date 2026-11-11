using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// RDL 2008+ PageBreak on a data region, rectangle or group: where the break goes
	/// (BreakLocation), and, from 2008 R2, whether it is turned off (Disabled) and whether page
	/// numbering starts again after it (ResetPageNumber). The 2005 PageBreakAtStart and
	/// PageBreakAtEnd flags are the location alone.
	/// </summary>
	[Serializable]
	internal sealed class PageBreakDefn : ReportLink
	{
		internal PageBreakDefn(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			foreach (XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.LocalName)
				{
					case "BreakLocation":
						switch (xNodeLoop.InnerText.Trim())
						{
							case "Start": AtStart = true; break;
							case "End": AtEnd = true; break;
							case "StartAndEnd": AtStart = AtEnd = true; break;
							case "Between": AtEnd = Between = true; break;
							case "None": break;
							default:
								OwnerReport.rl.LogError(4, "Unknown BreakLocation '" + xNodeLoop.InnerText + "'; no page break.");
								break;
						}
						break;
					case "Disabled":
						Disabled = new Expression(r, this, xNodeLoop, ExpressionType.Boolean);
						break;
					case "ResetPageNumber":
						ResetPageNumber = new Expression(r, this, xNodeLoop, ExpressionType.Boolean);
						break;
					default:
						OwnerReport.rl.LogError(4, "Unknown PageBreak element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
		}

		internal bool AtStart { get; }
		internal bool AtEnd { get; }

		/// <summary>A group breaks between its instances only: after each but the last.</summary>
		internal bool Between { get; }

		internal Expression Disabled { get; }
		internal Expression ResetPageNumber { get; }

		internal async Task<bool> IsDisabled(Report rpt, Row row)
			=> Disabled != null && await Disabled.EvaluateBoolean(rpt, row).ConfigureAwait(false);

		internal async Task<bool> Resets(Report rpt, Row row)
			=> ResetPageNumber != null && await ResetPageNumber.EvaluateBoolean(rpt, row).ConfigureAwait(false);

		/// <summary>After a break this caused: starts the page numbering again if asked to.</summary>
		internal async Task AfterBreak(Pages pgs, Row row)
		{
			if (await Resets(pgs.Report, row).ConfigureAwait(false))
				pgs.ResetPageNumber();
		}

		internal override async Task FinalPass()
		{
			if (Disabled != null)
				await Disabled.FinalPass().ConfigureAwait(false);
			if (ResetPageNumber != null)
				await ResetPageNumber.FinalPass().ConfigureAwait(false);
		}
	}
}
