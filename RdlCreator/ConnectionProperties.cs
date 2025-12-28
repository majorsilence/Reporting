using System.Xml.Serialization;


namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class ConnectionProperties
    {
        [XmlElement(ElementName = "DataProvider")]
        public string DataProvider { get; set; }

        [XmlElement(ElementName = "ConnectString")]
        public string ConnectString { get; set; }
    }
}