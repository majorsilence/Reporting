using System.Xml.Serialization;

namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class BorderColor
    {
        [XmlElement(ElementName = "Bottom")]
        public string Bottom { get; set; }

        [XmlElement(ElementName = "Top")]
        public string Top { get; set; }

        [XmlElement(ElementName = "Left")]
        public string Left { get; set; }

        [XmlElement(ElementName = "Right")]
        public string Right { get; set; }
    }
}