using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class ReportItemImage
    {
        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "Height")]
        public ReportItemSize Height { get; set; }

        [XmlElement(ElementName = "Width")]
        public ReportItemSize Width { get; set; }

        [XmlElement(ElementName = "Left")]
        public ReportItemSize Left { get; set; }

        [XmlElement(ElementName = "Top")]
        public ReportItemSize Top { get; set; }

        [XmlElement(ElementName = "Source")]
        public string Source { get; set; }

        [XmlElement(ElementName = "Value")]
        public string Value { get; set; }

        [XmlElement(ElementName = "Sizing")]
        public string Sizing { get; set; }

        [XmlElement(ElementName = "Style")]
        public Style ImageStyle { get; set; }
    }
}
