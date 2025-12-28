using System.Xml.Serialization;

namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class Style
    {
        [XmlElement(ElementName = "FontSize")]
        public ReportItemSize FontSize { get; set; }

        [XmlElement(ElementName = "FontWeight")]
        public string FontWeight { get; set; }

        [XmlElement(ElementName = "TextAlign")]
        public string TextAlign { get; set; }

        [XmlElement(ElementName = "BorderStyle")]
        public BorderStyle BorderStyle { get; set; }

        [XmlElement(ElementName = "BorderColor")]
        public BorderColor BorderColor { get; set; }

        [XmlElement(ElementName = "BorderWidth")]
        public BorderWidth BorderWidth { get; set; }

        [XmlElement(ElementName = "BackgroundColor")]
        public string BackgroundColor { get; set; }

    }
}