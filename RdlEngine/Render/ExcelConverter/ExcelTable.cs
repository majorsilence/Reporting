using System;
using System.Linq;
using Majorsilence.Reporting.Rdl;
namespace RdlEngine.Render.ExcelConverter
{
	internal class ExcelTable
	{
		public Table Table { get; set; }
		public float GrowedBottomPosition { get; set; }
		public float OriginalBottomPosition { get; set; }

		// Detail rows include group header/footer rows; header and footer are the table's own.
		public System.Collections.Generic.List<ExcelRow> HeaderRows { get; } = new System.Collections.Generic.List<ExcelRow>();
		public System.Collections.Generic.List<ExcelRow> DetailRows { get; } = new System.Collections.Generic.List<ExcelRow>();
		public System.Collections.Generic.List<ExcelRow> FooterRows { get; } = new System.Collections.Generic.List<ExcelRow>();

		public float TableWidth {
			get{
				return Table.TableColumns.Items.Sum(x => x.Width.Points);
			}
		}
	}
}
