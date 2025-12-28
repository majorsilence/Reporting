using System.Xml.Serialization;


namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class PageFooter
    {
        [XmlElement(ElementName = "Height")]
        public ReportItemSize Height { get; set; }

        [XmlElement(ElementName = "ReportItems")]
        public ReportItemsFooter ReportItems { get; set; }

        [XmlElement(ElementName = "PrintOnFirstPage")]
        public string PrintOnFirstPage { get; set; }

        [XmlElement(ElementName = "PrintOnLastPage")]
        public string PrintOnLastPage { get; set; }
    }
}