using Majorsilence.Reporting.Rdl;

namespace RdlEngine.Render.ExcelConverter
{
    internal class ExcelImage
    {
        public Image Image { get; set; }
        public byte[] Data { get; set; }

        public float AbsoluteTop { get; set; }
        public float AbsoluteLeft { get; set; }

        public float ImageWidth => Image.Width.Points;
        public float ImageHeight => Image.Height.Points;

        public ExcelImage(Image image, byte[] data)
        {
            Image = image;
            Data = data;
        }
    }
}
