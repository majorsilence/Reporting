using RdlMapFile.Resources;
using System.ComponentModel;            // need this for the properties metadata

using System.Drawing;
using Majorsilence.Forms.Drawing;
using System.Globalization;
using System.Xml;

namespace Majorsilence.Reporting.RdlMapFile
{
    /// <summary>
    /// PropertyReportItem - The ReportItem Properties
    /// </summary>
    [DefaultProperty("Text")]
    internal class PropertyText : PropertyBase
    {

        internal PropertyText(DesignXmlDraw d):base(d)
        {
        }

        [LocalizedCategory("Text")]
		[LocalizedDisplayName("Text_Value")]
		[LocalizedDescription("Text_Value")]
        public string Value
        {
            get {return GetTextValue("Value"); }
            set{SetTextValue("Value", value); }
        }

        [RefreshProperties(RefreshProperties.Repaint)]
		[LocalizedCategory("Text")]
		[LocalizedDisplayName("Text_Location")]
		[LocalizedDescription("Text_Location")]
        public Point Location
        {
            get 
            {
                XmlNode v = Draw.SelectedItem;
                return Draw.GetTextPoint(v, true);
            }
            set 
            {
                string l = string.Format("{0},{1}", value.X, value.Y);
                SetTextValue("Location", l); 
            }
        }

		[LocalizedCategory("Text")]
		[LocalizedDisplayName("Text_Color")]
		[LocalizedDescription("Text_Color")]
        public Color Color
        {
            get
            {
                XmlNode cn = Draw.SelectedItem;
                return Draw.GetTextColor(cn);
            }

            set
            {
                string sc = value.Name;
                SetTextValue("Color", sc);
            }
        }

		[LocalizedCategory("Text")]
		[LocalizedDisplayName("Text_Font")]
		[LocalizedDescription("Text_Font")]
        public Font Font
        {
            get
            {
                XmlNode v = Draw.SelectedItem;
                return Draw.GetTextFont(v);
            }
            set
            {
                string family = value.FontFamily.GetName(0);
                string fs = string.Format(NumberFormatInfo.InvariantInfo, "{0}", value.SizeInPoints);

                Draw.StartUndoGroup(Strings.PropertyText_Undo_FontChange);
                XmlNode xn = Draw.SelectedItem;
                foreach (XmlNode n in Draw.SelectedList)
                {
                    if (xn.Name != n.Name)
                        continue;
                    
                    Draw.SetElement(n, "FontFamily", family);
                    Draw.SetElement(n, "FontSize", fs);
                    Draw.SetElement(n, "FontWeight", value.Bold? "Bold": "Normal");
                    Draw.SetElement(n, "FontStyle", value.Italic? "Italic": "Normal");
                    if (value.Underline)
                        Draw.SetElement(n, "TextDecoration", "Underline");
                    else if (value.Strikeout)
                        Draw.SetElement(n, "TextDecoration", "LineThrough");
                    else
                        Draw.SetElement(n, "TextDecoration", "None");
                }
                Draw.EndUndoGroup();

                Draw.SignalXmlChanged();
                Draw.Invalidate();

            }
        }
    }
}
