using System.ComponentModel;            // need this for the properties metadata

using System.Drawing;
using System.Xml;

namespace Majorsilence.Reporting.RdlMapFile
{
    /// <summary>
    /// PropertyReportItem - The ReportItem Properties
    /// </summary>
    [DefaultProperty("Line")]
    internal class PropertyLine : PropertyBase
    {

        internal PropertyLine(DesignXmlDraw d):base(d)
        {
        }

        [LocalizedCategory("Line")]
		[LocalizedDisplayName("Line_P1")]
		[LocalizedDescription("Line_P1")]
        public Point P1
        {
            get 
            {
                XmlNode v = Draw.SelectedItem;
                Point[] pts = Draw.GetLineCoord(v, true);
                return pts[0];
            }
            set 
            {
                XmlNode v = Draw.SelectedItem;
                Point[] pts = Draw.GetLineCoord(v, true);
                string l = string.Format("{0},{1},{2}, {3}", value.X, value.Y, pts[1].X, pts[1].Y);
                SetTextValue("Points", l); 
            }
        }

		[LocalizedCategory("Line")]
		[LocalizedDisplayName("Line_P2")]
		[LocalizedDescription("Line_P2")]
        public Point P2
        {
            get
            {
                XmlNode v = Draw.SelectedItem;
                Point[] pts = Draw.GetLineCoord(v, true);
                return pts[1];
            }
            set
            {
                XmlNode v = Draw.SelectedItem;
                Point[] pts = Draw.GetLineCoord(v, true);
                string l = string.Format("{0},{1},{2}, {3}", pts[1].X, pts[1].Y, value.X, value.Y);
                SetTextValue("Points", l);
            }
        }

    }
}
