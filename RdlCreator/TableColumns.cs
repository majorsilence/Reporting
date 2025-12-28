using System.Collections.Generic;
using System.Xml.Serialization;


namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class TableColumns
    {
        [XmlElement(ElementName = "TableColumn")]
        public List<TableColumn> TableColumn { get; set; }
    }
}