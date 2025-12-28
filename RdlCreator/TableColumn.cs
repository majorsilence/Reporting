using System.Xml.Serialization;


namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class TableColumn
    {
        [XmlElement(ElementName = "Width")]
        public ReportItemSize Width { get; set; }
    }
}