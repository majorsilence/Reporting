
using System.Collections.Generic;
using System.Xml;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// PropertyRectangle - The Rectangle specific Properties
    /// </summary>
    internal class PropertyRectangle : PropertyReportItem
    {
        public PropertyRectangle(DesignXmlDraw d, DesignCtl dc, List<XmlNode> ris) : base(d, dc, ris)
        {
        }

        [LocalizedCategory("Rectangle")]
		[LocalizedDisplayName("Rectangle_PageBreakAtStart")]
		[LocalizedDescription("Rectangle_PageBreakAtStart")]
        public bool PageBreakAtStart
        {
            get { return Draw.GetElementValue(Node, "PageBreakAtStart", "false").ToLower() == "true"; }
            set
            {
                SetValue("PageBreakAtStart", value ? "true" : "false");
            }
        }

        [LocalizedCategory("Rectangle")]
		[LocalizedDisplayName("Rectangle_PageBreakAtEnd")]
		[LocalizedDescription("Rectangle_PageBreakAtEnd")]
        public bool PageBreakAtEnd
        {
            get { return Draw.GetElementValue(Node, "PageBreakAtEnd", "false").ToLower() == "true"; }
            set
            {
                SetValue("PageBreakAtEnd", value ? "true" : "false");
            }
        }
    }
}
