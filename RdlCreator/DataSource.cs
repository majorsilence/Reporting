using System.Xml.Serialization;


namespace Majorsilence.Reporting.RdlCreator
{
    #if AOT
        [DotWrap.DotWrapExpose] 
    #endif
    public class DataSource
    {
        [XmlElement(ElementName = "ConnectionProperties")]
        public ConnectionProperties ConnectionProperties { get; set; }

        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }
    }
}