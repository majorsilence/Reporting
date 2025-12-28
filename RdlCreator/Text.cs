using System.Xml.Serialization;


namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class Text
    {
        [XmlElement(ElementName = "Value")]
        public Value Value { get; set; }

        [XmlElement(ElementName = "Style")]
        public Style Style { get; set; }

        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "Top")]
        public ReportItemSize Top { get; set; }

        [XmlElement(ElementName = "Left")]
        public ReportItemSize Left { get; set; }

        [XmlElement(ElementName = "Width")]
        public ReportItemSize Width { get; set; }

        [XmlElement(ElementName = "Height")]
        public ReportItemSize Height { get; set; }

        [XmlElement(ElementName = "CanGrow")]
        public bool CanGrow { get; set; }
    }
}