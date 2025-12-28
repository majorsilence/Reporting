using System.Collections.Generic;
using System.Xml.Serialization;


namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class Fields
    {
        [XmlElement(ElementName = "Field")]
        public List<Field> Field { get; set; }
    }
}