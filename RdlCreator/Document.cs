// SPDX-License-Identifier: LGPL-2.1-or-later
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Majorsilence.Pdf;

namespace Majorsilence.Reporting.RdlCreator
{
    /// <summary>
    /// Programmatically creates multi-page PDF documents by composing RDL report pages
    /// and merging the rendered PDFs using <see cref="PdfMerger"/>.
    /// </summary>
    [XmlRoot(ElementName = "Report", Namespace = "http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition")]
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

        [XmlElement(ElementName = "TopMargin")]
        public ReportItemSize TopMargin { get; set; }

        [XmlElement(ElementName = "LeftMargin")]
        public ReportItemSize LeftMargin { get; set; }

        [XmlElement(ElementName = "RightMargin")]
        public ReportItemSize RightMargin { get; set; }

        [XmlElement(ElementName = "BottomMargin")]
        public ReportItemSize BottomMargin { get; set; }

        public PageFooter PageFooter { get; set; }

        // ── fluent builder ────────────────────────────────────────────────────

        public Document WithDescription(string description) { Description = description; return this; }
        public Document WithAuthor(string author)           { Author = author;           return this; }
        public Document WithName(string name)               { Name = name;               return this; }
        public Document WithPageHeight(string pageHeight)   { PageHeight = pageHeight;   return this; }
        public Document WithPageWidth(string pageWidth)     { PageWidth = pageWidth;     return this; }
        public Document WithTopMargin(string topMargin)     { TopMargin = topMargin;     return this; }
        public Document WithLeftMargin(string leftMargin)   { LeftMargin = leftMargin;   return this; }
        public Document WithRightMargin(string rightMargin) { RightMargin = rightMargin; return this; }
        public Document WithBottomMargin(string botMargin)  { BottomMargin = botMargin;  return this; }

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

        // ── rendering ─────────────────────────────────────────────────────────

        /// <summary>Renders all pages and returns the merged PDF as a byte array.</summary>
        public async Task<byte[]> Create()
        {
            using var ms = new MemoryStream();
            await Create(ms);
            return ms.ToArray();
        }

        /// <summary>Renders all pages and writes the merged PDF to <paramref name="output"/>.</summary>
        public async Task Create(Stream output)
        {
            var merger = new PdfMerger()
                .WithAuthor(Author)
                .WithTitle(Name)
                .WithSubject(Description)
                .WithCreator("Majorsilence Reporting - RdlCreator");

            foreach (var page in Pages)
            {
                // Build the RDL report model for this page
                var report = new Report()
                    .WithTopMargin(TopMargin)
                    .WithLeftMargin(LeftMargin)
                    .WithRightMargin(RightMargin)
                    .WithBottomMargin(BottomMargin)
                    .WithPageHeader(page.PageHeader)
                    .WithPageFooter(page.PageFooter)
                    .WithBody(page.Body);

                // Render via the RDL engine to a PDF byte stream
                var create    = new RdlCreator.Create();
                var fyiReport = await create.GenerateRdl(report);
                using var ms  = new Majorsilence.Reporting.Rdl.MemoryStreamGen();
                await fyiReport.RunGetData();
                await fyiReport.RunRender(ms, Majorsilence.Reporting.Rdl.OutputPresentationType.PDF);

                var pdf = ms.GetStream();
                pdf.Position = 0;
                using var pagePdf = new MemoryStream();
                await pdf.CopyToAsync(pagePdf);
                merger.Add(pagePdf.ToArray());
            }

            merger.Merge(output);
        }
    }
}
