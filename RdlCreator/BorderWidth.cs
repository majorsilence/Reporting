using System.Xml.Serialization;

namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class BorderWidth
    {
        [XmlElement(ElementName = "Top")]
        public ReportItemSize Top { get; set; }

        [XmlElement(ElementName = "Bottom")]
        public ReportItemSize Bottom { get; set; }

        [XmlElement(ElementName = "Left")]
        public ReportItemSize Left { get; set; }

        [XmlElement(ElementName = "Right")]
        public ReportItemSize Right { get; set; }
    }
}