using Majorsilence.Reporting.Rdl;

namespace RdlEngine.Render.ExcelConverter
{
    internal class ExcelCell
    {
        public ExcelCell(ReportItem reportItem, string value, ExcelRow row, ExcelColumn column)
        {
            ReportItem = reportItem;
            Value = value;
            Row = row;
            Column = column;
            Row.Cells.Add(this);
            Column.Cells.Add(this);
        }

        public ReportItem ReportItem { get; set; }
        public ExcelTable ExcelTable { get; set; }
        public string Value { get; set; }
        public ExcelRow Row { get; private set; }
        public ExcelColumn Column { get; private set; }

        public ExcelColumn RightAttachCol { get; set; }
        public ExcelRow BottomAttachRow { get; set; }

        public float GrowedBottomPosition { get; set; }
        public float OriginalBottomPosition { get; set; }

        public float ActualHeight
        {
            get
            {
                if (CorrectedHeight.HasValue)
                    return CorrectedHeight.Value;
                return OriginalHeight;
            }
        }

        public float ActualWidth
        {
            get
            {
                if (CorrectedWidth.HasValue)
                    return CorrectedWidth.Value;
                return OriginalWidth;
            }
        }

        float? originalWidth;
        public float OriginalWidth
        {
            get
            {
                if (originalWidth.HasValue)
                    return originalWidth.Value;
                if (ReportItem != null && ReportItem.Width != null)
                    return ReportItem.Width.Points;
                return 0f;
            }
            set { originalWidth = value; }
        }

        float? originalHeight;
        public float OriginalHeight
        {
            get
            {
                if (originalHeight.HasValue)
                    return originalHeight.Value;
                if (ReportItem != null && ReportItem.Height != null)
                    return ReportItem.Height.Points;
                return 0f;
            }
            set { originalHeight = value; }
        }

        public float? CorrectedWidth { get; set; }
        public float? CorrectedHeight { get; set; }

        public StyleInfo Style { get; set; }
    }
}
