
using System;
using System.ComponentModel;            // need this for the properties metadata
using System.Drawing;
using System.Text;
using System.Xml;

namespace Majorsilence.Reporting.RdlMapFile
{
    /// <summary>
    /// PropertyReportItem - The ReportItem Properties
    /// </summary>
    [DefaultProperty("Polygon")]
    internal class PropertyPolygon : PropertyBase
    {

        internal PropertyPolygon(DesignXmlDraw d):base(d)
        {
        }

        [LocalizedCategory("Polygon")]
		[LocalizedDisplayName("Polygon_FillColor")]
		[LocalizedDescription("Polygon_FillColor")]
        public Color FillColor
        {
            get
            {
                string sc = GetTextValue("FillColor");
                if (sc == null)
                    return Color.Empty;
                Color c = Color.Empty;
			    try 
			    {
				    c = ColorTranslator.FromHtml(sc);
			    }
			    catch 
			    {       // if bad color just ignore and handle as empty color
			    }
    			return c;
            }
  
            set
            {
                string sc = value.Name;
                SetTextValue("FillColor", sc);
            }
        }

		[LocalizedCategory("Polygon")]
		[LocalizedDisplayName("Polygon_Keys")]
		[LocalizedDescription("Polygon_Keys")]
        public string[] Keys
        {
            get 
            {
                string k = GetTextValue("Keys");
                string[] keys = k.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                return keys;
            }
            set
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < value.Length; i++)
                {
                    if (i > 0)
                        sb.Append(',');
                    sb.Append(value[i]);
                }
                SetTextValue("Keys", sb.ToString()); 
            }
        }

		[LocalizedCategory("Polygon")]
		[LocalizedDisplayName("Polygon_Points")]
		[LocalizedDescription("Polygon_Points")]
        public Point[] Points
        {
            get 
            {
                XmlNode v = Draw.SelectedItem;
                return Draw.GetPolygon(v, true);
            }
            set 
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < value.Length; i++)
                {
                    if (i > 0)
                        sb.Append(',');
                    sb.AppendFormat("{0},{1}", value[i].X, value[i].Y);
                }
                SetTextValue("Points", sb.ToString()); 
            }
        }

    }
}
