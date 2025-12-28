using System.Collections.Generic;
using System.Xml.Serialization;


namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class ReportItemsBody
    {
      
        [XmlElement(ElementName = "Table")]
        public Table Table { get; set; }

        [XmlElement("Textbox", typeof(Text))]
        public List<Text> Text { get; set; }

        [XmlElement("CustomReportItem", typeof(CustomReportItems))]
        public List<CustomReportItems> CustomReportItems { get; set; }

        [XmlElement("Rectangle", typeof(Card))]
        public List<Card> Cards { get; set; }

        [XmlElement("Image", typeof(ReportItemImage))]
        public List<ReportItemImage> Images { get; set; }

    }
}