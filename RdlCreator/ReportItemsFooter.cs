using System.Xml.Serialization;


namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class ReportItemsFooter
    {
        [XmlElement(ElementName = "Textbox")]
        public Text Textbox { get; set; }

    }
}