using System.Xml.Serialization;


namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class DataSet
    {
        [XmlElement(ElementName = "Query")]
        public Query Query { get; set; }

        [XmlElement(ElementName = "Fields")]
        public Fields Fields { get; set; }

        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }
    }
}