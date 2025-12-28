using System.Xml.Serialization;


namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class TableRows
    {
        [XmlElement(ElementName = "TableRow")]
        public TableRow TableRow { get; set; }
    }
}