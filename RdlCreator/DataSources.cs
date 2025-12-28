using System.Xml.Serialization;


namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class DataSources
    {
        [XmlElement(ElementName = "DataSource")]
        public DataSource DataSource { get; set; }
    }
}