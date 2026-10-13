

using System;
using Majorsilence.Reporting.Rdl;
using System.IO;
using System.Collections;
using System.Text;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{

    ///<summary>
    ///The primary class to "run" a report to XML
    ///</summary>
    internal class RenderCsv : IPresent
    {
        Report report;					// report
        DelimitedTextWriter tw;				// where the output is going

        public RenderCsv(Report report, IStreamGen sg)
        {
            this.report = report;
            tw = new DelimitedTextWriter(sg.GetTextWriter(), ",");
        }

        public void Dispose() { } 

        public Report Report()
        {
            return report;
        }

        public bool IsPagingNeeded()
        {
            return false;
        }

        public void Start()
        {
        }

        public Task End()
        {
            return Task.CompletedTask;
        }									

        public Task RunPages(Pages pgs)
        {
            return Task.CompletedTask;
        }					

        public void BodyStart(Body b)
        {
        }

        public void BodyEnd(Body b)
        {
        }

        public void PageHeaderStart(PageHeader ph)
        {
        }

        public void PageHeaderEnd(PageHeader ph)
        {
            if (ph.PrintOnFirstPage||ph.PrintOnLastPage) {tw.WriteLine();}           
        }

        public void PageFooterStart(PageFooter pf)
        {
        }

        public void PageFooterEnd(PageFooter pf)
        {
            if (pf.PrintOnLastPage || pf.PrintOnFirstPage) {tw.WriteLine();}
        }

        public Task Textbox(Textbox tb, string t, Row r)
        {
            if (tb.TryEvaluate(report, r, out object value))     // the usual plain field: no Task per cell
            {
                tw.Write(value);
                return Task.CompletedTask;
            }
            return TextboxAsync(tb, r);
        }

        private async Task TextboxAsync(Textbox tb, Row r)
        {
            object value = await tb.Evaluate(report, r).ConfigureAwait(false);
            tw.Write(value);
        }	
        
        public Task DataRegionNoRows(DataRegion d, string noRowsMsg)
        {
            return Task.CompletedTask;
        }

        public Task<bool> ListStart(List l, Row r)
        {
            return Task.FromResult(true);
        }
        
        public Task ListEnd(List l, Row r)
        {
            tw.WriteLine();
            return Task.CompletedTask;
        }
        
        public Task ListEntryBegin(List l, Row r)
        {
            return Task.CompletedTask;
        }
        public void ListEntryEnd(List l, Row r)
        {
            tw.WriteLine();
        }

        public Task<bool> TableStart(Table t, Row r)
        {
            return Task.FromResult(true);
        }
        
        public Task TableEnd(Table t, Row r)
        {
            return Task.CompletedTask;
        }
        
        public void TableBodyStart(Table t, Row r)
        {
        }
        
        public void TableBodyEnd(Table t, Row r)
        {
        }
        
        public void TableFooterStart(Footer f, Row r)
        {
        }
        
        public void TableFooterEnd(Footer f, Row r)
        {
        }
        
        public void TableHeaderStart(Header h, Row r)
        {
        }
        
        public void TableHeaderEnd(Header h, Row r)
        {
        }
        
        public Task TableRowStart(TableRow tr, Row r)
        {
            return Task.CompletedTask;
        }
        
        public void TableRowEnd(TableRow tr, Row r)
        {
            tw.WriteLine();
        }
        
        public Task TableCellStart(TableCell t, Row r)
        {
            return Task.CompletedTask;
        }
        
        public void TableCellEnd(TableCell t, Row r)
        {
        }

        public Task<bool> MatrixStart(Matrix m, MatrixCellEntry[,] matrix, Row r, int headerRows, int maxRows, int maxCols)
        {
            return Task.FromResult(true);
        }
        
        public void MatrixColumns(Matrix m, MatrixColumns mc)
        {
        }

        public void MatrixRowStart(Matrix m, int row, Row r)
        {
        }
        
        public void MatrixRowEnd(Matrix m, int row, Row r)
        {
            tw.WriteLine();
        }
        
        public Task MatrixCellStart(Matrix m, ReportItem ri, int row, int column, Row r, float h, float w, int colSpan)
        {
            return Task.CompletedTask;
        }

        public Task MatrixCellEnd(Matrix m, ReportItem ri, int row, int column, Row r)
        {
            return Task.CompletedTask;
        }

        public Task MatrixEnd(Matrix m, Row r)
        {
            return Task.CompletedTask;
        }
        
        public Task Chart(Chart c, Row r, ChartBase cb)
        {
            return Task.CompletedTask;
        }

        public Task Image(Image i, Row r, string mimeType, Stream io)
        {
            return Task.CompletedTask;
        }

        public Task Line(Line l, Row r)
        {
            return Task.CompletedTask;
        }

        public Task<bool> RectangleStart(Rectangle rect, Row r)
        {
            return Task.FromResult(true);
        }
        
        public Task RectangleEnd(Rectangle rect, Row r)
        {
            return Task.CompletedTask;
        }	
        
        public Task Subreport(Subreport s, Row r)
        {
            return Task.CompletedTask;
        }

        public void GroupingStart(Grouping g)
        {
        }
        
        public void GroupingInstanceStart(Grouping g)
        {
        }
        
        public void GroupingInstanceEnd(Grouping g)
        {
        }
        
        public void GroupingEnd(Grouping g)
        {
        }
    }
}
