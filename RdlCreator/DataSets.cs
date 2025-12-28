using System.Xml.Serialization;


namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class DataSets
    {
        [XmlElement(ElementName = "DataSet")]
        public DataSet DataSet { get; set; }
    }
}