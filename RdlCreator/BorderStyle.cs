using System.Xml.Serialization;

namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class BorderStyle
    {
        [XmlElement(ElementName = "Default")]
        public BorderStyleType Default { get; set; }

        [XmlElement(ElementName = "Left")]
        public BorderStyleType Left { get; set; }

        [XmlElement(ElementName = "Right")]
        public BorderStyleType Right { get; set; }

        [XmlElement(ElementName = "Top")]
        public BorderStyleType Top { get; set; }

        [XmlElement(ElementName = "Bottom")]
        public BorderStyleType Bottom { get; set; }
    }
}