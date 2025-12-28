using System.Collections.Generic;
using System.Xml.Serialization;

namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class TableCells
    {
        [XmlElement(ElementName = "TableCell")]
        public List<TableCell> TableCell { get; set; }
    }
}