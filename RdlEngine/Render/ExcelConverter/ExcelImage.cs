using Majorsilence.Reporting.Rdl;

namespace RdlEngine.Render.ExcelConverter
{
    internal class ExcelImage
    {
        public ReportItem Item { get; set; }
        public byte[] Data { get; set; }

        public float AbsoluteTop { get; set; }
        public float AbsoluteLeft { get; set; }

        public float ImageWidth => Item.Width.Points;
        public float ImageHeight => Item.Height.Points;

        public ExcelImage(ReportItem item, byte[] data)
        {
            Item = item;
            Data = data;
        }
    }
}
