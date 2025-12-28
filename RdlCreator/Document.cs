using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Majorsilence.Reporting.RdlCreator
{
    /// <summary>
    /// Programmatically create PDFs.
    /// </summary>
    /// <remarks>Internally it is a combination of RDL and https://github.com/VahidN/iTextSharp.LGPLv2.Core</remarks>
    [XmlRoot(ElementName = "Report", Namespace = "http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition")]
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class Document
    {
        public List<Page> Pages { get; private set; } = new List<Page>();

        [XmlElement(ElementName = "Description")]
        public string Description { get; set; }

        [XmlElement(ElementName = "Author")]
        public string Author { get; set; }

        [XmlElement(ElementName = "Name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "PageHeight")]
        public ReportItemSize PageHeight { get; set; }

        [XmlElement(ElementName = "PageWidth")]
        public ReportItemSize PageWidth { get; set; }

        [XmlElement(ElementName = "DataSources")]
        public DataSources DataSources { get; set; }

        //[XmlElement(ElementName = "Width")]
        //public string Width { get; set; }

        [XmlElement(ElementName = "TopMargin")]
        public ReportItemSize TopMargin { get; set; }

        [XmlElement(ElementName = "LeftMargin")]
        public ReportItemSize LeftMargin { get; set; }

        [XmlElement(ElementName = "RightMargin")]
        public ReportItemSize RightMargin { get; set; }

        [XmlElement(ElementName = "BottomMargin")]
        public ReportItemSize BottomMargin { get; set; }


        public PageFooter PageFooter { get; set; }

        public Document WithDescription(string description)
        {
            Description = description;
            return this;
        }
        public Document WithAuthor(string author)
        {
            Author = author;
            return this;
        }

        public Document WithName(string name)
        {
            Name = name;
            return this;
        }

        public Document WithPageHeight(string pageHeight)
        {
            PageHeight = pageHeight;
            return this;
        }

        public Document WithPageWidth(string pageWidth)
        {
            PageWidth = pageWidth;
            return this;
        }

        //public Document WithWidth(string width)
        //{
        //    Width = width;
        //    return this;
        //}

        public Document WithTopMargin(string topMargin)
        {
            TopMargin = topMargin;
            return this;
        }

        public Document WithLeftMargin(string leftMargin)
        {
            LeftMargin = leftMargin;
            return this;
        }

        public Document WithRightMargin(string rightMargin)
        {
            RightMargin = rightMargin;
            return this;
        }

        public Document WithBottomMargin(string bottomMargin)
        {
            BottomMargin = bottomMargin;
            return this;
        }

        public Document WithPage(Page page)
        {
            Pages.Add(page);
            return this;
        }

        public Document WithPage(Action<Page> options)
        {
            var p = new Page();
            options(p);
            Pages.Add(p);
            return this;
        }

        public async Task<byte[]>Create()
        {
            using var ms = new MemoryStream();
            await Create(ms);
            return ms.ToArray();
        }
        
        /// <summary>
        /// Create the PDF document.
        /// </summary>
        /// <param name="output">Stream where the pdf will be saved.</param>
        public async Task Create(Stream output)
        {
            using var itextDocument = new iTextSharp.text.Document();
            using var iTextCopy = new PdfCopy(itextDocument, output);
            itextDocument.Open();

            foreach (var page in Pages)
            {
                var report = new Report();
                report
                    //.WithWidth(Width)
                    .WithTopMargin(TopMargin)
                    .WithLeftMargin(LeftMargin)
                    .WithRightMargin(RightMargin)
                    .WithBottomMargin(BottomMargin)
                    .WithPageHeader(page.PageHeader)
                    .WithPageFooter(page.PageFooter)
                    .WithBody(page.Body);

                var create = new RdlCreator.Create();
                var fyiReport = await create.GenerateRdl(report);
                using var ms = new Majorsilence.Reporting.Rdl.MemoryStreamGen();
                await fyiReport.RunGetData(null);
                await fyiReport.RunRender(ms,  Majorsilence.Reporting.Rdl.OutputPresentationType.PDF);
                var documentData = ms.GetStream();
                documentData.Position = 0;

                using var reader = new PdfReader(documentData);
                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    iTextCopy.AddPage(iTextCopy.GetImportedPage(reader, i));
                }
                iTextCopy.FreeReader(reader);
                reader.Close();
            }

            itextDocument.AddAuthor(Author);
            itextDocument.AddCreationDate();
            itextDocument.AddCreator("Majorsilence Reporting - RenderPdf_iTextSharp");
            itextDocument.AddSubject(Description);
            itextDocument.AddTitle(Name);
            itextDocument.Close();
        }


    }
}
