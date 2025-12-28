using System.Xml.Serialization;

namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class TableCell
    {
        [XmlElement(ElementName = "ReportItems")]
        public TableCellReportItems ReportItems { get; set; }
    }
}