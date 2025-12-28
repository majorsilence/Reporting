using System.Xml.Serialization;

namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class CustomProperties
    {
        [XmlElement(ElementName = "CustomProperty")]
        public CustomProperty CustomProperty { get; set; }
    }
}