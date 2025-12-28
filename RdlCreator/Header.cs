using System.Xml.Serialization;


namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class Header
    {
        [XmlElement(ElementName = "TableRows")]
        public TableRows TableRows { get; set; }

        [XmlElement(ElementName = "RepeatOnNewPage")]
        public string RepeatOnNewPage { get; set; }
    }
}