using System.Collections.Generic;
using System.Xml.Serialization;
using System;


namespace Majorsilence.Reporting.RdlCreator
{

#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class Body
    {
        [XmlElement("ReportItems", typeof(ReportItemsBody))]
        public ReportItemsBody ReportItems { get; set; }

        [XmlElement(ElementName = "Height")]
        public ReportItemSize Height { get; set; }

        [XmlElement(ElementName = "Width")]
        public ReportItemSize Width { get; set; }
    }
}