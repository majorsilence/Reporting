using System;
using System.Xml.Serialization;


namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class Card
    {
        private readonly Report _rpt = null;

        public Card() { }

        public Card(Report rpt)
        {
            _rpt = rpt;
        }

        [XmlElement(ElementName = "Value")] public Value Value { get; set; }

        [XmlElement(ElementName = "Style")] public Style Style { get; set; }

        [XmlAttribute(AttributeName = "Name")] public string Name { get; set; }

        [XmlElement(ElementName = "Top")] public ReportItemSize Top { get; set; }

        [XmlElement(ElementName = "Left")] public ReportItemSize Left { get; set; }

        [XmlElement(ElementName = "Width")] public ReportItemSize Width { get; set; }

        [XmlElement(ElementName = "Height")] public ReportItemSize Height { get; set; }

        [XmlElement(ElementName = "CanGrow")] public bool CanGrow { get; set; }

        public bool PageBreakAtEnd { get; set; }

        public bool PageBreakAtStart { get; set; }

        public void Add(Text text)
        {
            // Calculate the relative position of the Text
            // TODO: Handle units other than "pt"
            float relativeTop = this.Top.Value + text.Top.Value;
            float relativeLeft = this.Left.Value + text.Left.Value;

            // Set the Text's position relative to the Card
            text.Top = relativeTop.ToString();
            text.Left = relativeLeft.ToString();

            _rpt?.Body?.ReportItems?.Text?.Add(text);
        }

        public void Add(CustomReportItems cri)
        {
            // Calculate the relative position of the Text
            // TODO: Handle units other than "pt"
            float relativeTop = this.Top.Value + cri.Top.Value;
            float relativeLeft = this.Left.Value + cri.Left.Value;

            // Set the Text's position relative to the Card
            cri.Top = relativeTop.ToString();
            cri.Left = relativeLeft.ToString();

            _rpt?.Body?.ReportItems?.CustomReportItems?.Add(cri);
        }
    }
}