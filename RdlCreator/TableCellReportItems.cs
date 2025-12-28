using System.Xml.Serialization;

namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class TableCellReportItems
    {
        [XmlElement("Textbox", typeof(Text))]
        [XmlElement("CustomReportItem", typeof(CustomReportItems))]
        public object ReportItem { get; set; }
    }
}