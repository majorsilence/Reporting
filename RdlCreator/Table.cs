using System.Xml.Serialization;


namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class Table
    {
        [XmlElement(ElementName = "DataSetName")]
        public string DataSetName { get; set; }

        [XmlElement(ElementName = "NoRows")]
        public string NoRows { get; set; }

        [XmlElement(ElementName = "TableColumns")]
        public TableColumns TableColumns { get; set; }

        [XmlElement(ElementName = "Header")]
        public Header Header { get; set; }

        [XmlElement(ElementName = "Details")]
        public Details Details { get; set; }

        [XmlAttribute(AttributeName = "Name")]
        public string TableName { get; set; }

    }
}