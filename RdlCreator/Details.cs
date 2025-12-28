using System.Xml.Serialization;


namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class Details
    {
        [XmlElement(ElementName = "TableRows")]
        public TableRows TableRows { get; set; }
    }
}