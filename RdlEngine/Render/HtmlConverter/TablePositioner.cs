

using System;
using Majorsilence.Reporting.Rdl;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
#if DRAWINGCOMPAT
using Majorsilence.Forms.Drawing;
using System.Drawing;  // value types (Color, Point, Size, Rectangle, ...) come from System.Drawing.Primitives
using Majorsilence.Forms.Drawing.Drawing2D;
#else
using System.Drawing;
using System.Drawing.Drawing2D;
#endif

namespace Majorsilence.Reporting.Rdl
{
	
	///<summary>
	/// HTML Table for positioning report items
	///</summary>
	internal class TablePositioner
	{
        Report _rpt;                // current report
        ReportItems _ris;           // report items contained in this position
        string[] _values;           //  values for each report item
        string _TableSyntax;        //  syntax for the table position; table contains {x} items holding a
                                    //   spot for each _ris;
        public TablePositioner(Report rpt, ReportItems ris)
        {
            _rpt = rpt;
            _ris = ris;
            _values = new string[ris.Items.Count];

            _TableSyntax = BuildTable();
        }

        private string BuildTable()
        {
            StringBuilder sb = new StringBuilder();
            float width = TableWidth();
            sb.AppendFormat("<table style=\"table-layout:fixed;width:{0}pt;border-style:none;border-collapse:collapse;\">", width);

            // Define the columns
            sb.Append("<colgroup>");
            List<ReportItem> riSort = new List<ReportItem>(_ris.Items);
            riSort.Sort(CompareRIsByX);

          //  float last_offset=0;
            float offset;
            float pt;
            for (int i = 0; i < riSort.Count; i++)
            {
                pt = riSort[i].LeftCalc(_rpt);

                sb.AppendFormat("<col style=\"width:{0}pts;\">", pt);

                offset = pt + riSort[i].WidthOrOwnerWidth(_rpt);

             //   if (last_offset < offset)
            }
            sb.Append("</colgroup>");

            // Define the rows
            riSort.Sort(CompareRIsByY);

            sb.Append("</table>");
            return sb.ToString();
        }

        private float TableWidth()
        {
            // Find the right most item in the report items
            float maxRight=0;
            foreach (ReportItem ri in _ris)
            {
                float right = ri.LeftCalc(_rpt) + ri.WidthOrOwnerWidth(_rpt);
                if (right > maxRight)
                    maxRight = right;
            }
            return maxRight;
        }

        private static int CompareRIsByX(ReportItem x, ReportItem y)
        {
            // order by left position
            int l1 = x.Left == null ? 0 : x.Left.Size;
            int l2 = y.Left == null ? 0 : y.Left.Size;

            int rc = l1 - l2;
            if (rc != 0)
                return rc;
            // then by width
            l1 = x.Width == null ? int.MaxValue : x.Width.Size;
            l2 = y.Width == null ? int.MaxValue : y.Width.Size;

            rc = l1 - l2;
            if (rc != 0)
                return rc;
            // then by y position
            int t1 = x.Top == null ? 0 : x.Top.Size;
            int t2 = y.Top == null ? 0 : y.Top.Size;

            return t1 - t2;
        }
    
        private static int CompareRIsByY(ReportItem x, ReportItem y)
        {
            // order by y position
            int t1 = x.Top == null ? 0 : x.Top.Size;
            int t2 = y.Top == null ? 0 : y.Top.Size;

            int rc = t1 - t2;
            if (rc != 0)
                return rc;
            // then by height
            t1 = x.Height == null ? int.MaxValue : x.Height.Size;
            t2 = y.Height == null ? int.MaxValue : y.Height.Size;

            rc = t1 - t2;
            if (rc != 0)
                return rc;

            // then by x position
            int l1 = x.Left == null ? 0 : x.Left.Size;
            int l2 = y.Left == null ? 0 : y.Left.Size;

            return l1 - l2;
        }
    }
}
