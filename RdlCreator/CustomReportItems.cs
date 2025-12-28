using System.Xml.Serialization;

namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class CustomReportItems
    {
        public string Type { get; set; }
        public ReportItemSize Height { get; set; }
        public ReportItemSize Width { get; set; }
        public ReportItemSize Left { get; set; }
        public ReportItemSize Top { get; set; }

        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "CustomProperties")]
        public CustomProperties CustomProperties { get; set; }
        [XmlElement(ElementName = "Style")]
        public Style Style { get; set; }
        [XmlElement(ElementName = "CanGrow")]
        public string CanGrow { get; set; }
        [XmlElement(ElementName = "Source")]
        public string Source { get; set; } = "Embedded";
    }
}