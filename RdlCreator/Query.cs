using System.Xml.Serialization;


namespace Majorsilence.Reporting.RdlCreator
{
    #if AOT
        [DotWrap.DotWrapExpose] 
    #endif
    public class Query
    {
        [XmlElement(ElementName = "DataSourceName")]
        public string DataSourceName { get; set; }

        [XmlElement(ElementName = "CommandText")]
        public string CommandText { get; set; }
    }
}