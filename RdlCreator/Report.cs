using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Majorsilence.Reporting.RdlCreator
{
    // Report class representing the root element
    [XmlRoot(ElementName = "Report", Namespace = "http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition")]
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class Report
    {
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

        [XmlElement(ElementName = "Width")]
        public ReportItemSize Width { get; set; }

        [XmlElement(ElementName = "TopMargin")]
        public ReportItemSize TopMargin { get; set; }

        [XmlElement(ElementName = "LeftMargin")]
        public ReportItemSize LeftMargin { get; set; }

        [XmlElement(ElementName = "RightMargin")]
        public ReportItemSize RightMargin { get; set; }

        [XmlElement(ElementName = "BottomMargin")]
        public ReportItemSize BottomMargin { get; set; }

        [XmlElement(ElementName = "DataSets")]
        public DataSets DataSets { get; set; }

        [XmlElement(ElementName = "PageHeader")]
        public PageHeader PageHeader { get; set; }

        [XmlElement(ElementName = "Body")]
        public Body Body { get; set; }

        [XmlElement(ElementName = "PageFooter")]
        public PageFooter PageFooter { get; set; }

        // Fluid style methods

        public Report WithDescription(string description)
        {
            Description = description;
            return this;
        }

        public Report WithAuthor(string author)
        {
            Author = author;
            return this;
        }

        public Report WithName(string name)
        {
            Name = name;
            return this;
        }

        public Report WithPageHeight(ReportItemSize pageHeight)
        {
            PageHeight = pageHeight;
            return this;
        }

        public Report WithPageWidth(ReportItemSize pageWidth)
        {
            PageWidth = pageWidth;
            return this;
        }

        public Report WithDataSources(DataSources dataSources)
        {
            DataSources = dataSources;
            return this;
        }

        public Report WithWidth(ReportItemSize width)
        {
            Width = width;
            return this;
        }

        public Report WithTopMargin(ReportItemSize topMargin)
        {
            TopMargin = topMargin;
            return this;
        }

        public Report WithLeftMargin(ReportItemSize leftMargin)
        {
            LeftMargin = leftMargin;
            return this;
        }

        public Report WithRightMargin(ReportItemSize rightMargin)
        {
            RightMargin = rightMargin;
            return this;
        }

        public Report WithBottomMargin(ReportItemSize bottomMargin)
        {
            BottomMargin = bottomMargin;
            return this;
        }

        public Report WithDataSets(DataSets dataSets)
        {
            DataSets = dataSets;
            return this;
        }

        public Report WithPageHeader(PageHeader pageHeader)
        {
            PageHeader = pageHeader;
            return this;
        }

        public Report WithBody(ReportItemSize height)
        {
            Body = new Body()
            {
                Height = height
            };
            return this;
        }

        public Report WithBody(Body body)
        {
            Body = body;
            return this;
        }

        public Report WithPageBreak()
        {
            return WithPageBreak("1pt");
        }
        
        public Report WithPageBreak(ReportItemSize yPos)
        {
            var pageBreakCard = new Card(this)
            {
                CanGrow = false,
                Height = "0pt",
                Width = "1pt",
                PageBreakAtEnd = true,
                Top=yPos
            };
            this.Body.ReportItems.Cards.Add(pageBreakCard);
            return this;
        }

        public Report WithPageFooter(PageFooter pageFooter)
        {
            PageFooter = pageFooter;
            return this;
        }

        public Report WithTable()
        {
            InitReportItemBody(true);
            return this;
        }

        public Report WithReportText(Text textbox)
        {
            InitReportItemBody(false);
            this.Body.ReportItems.Text.Add(textbox);

            return this;
        }

        private void InitReportItemBody(bool includeTable)
        {
            if (this.Body.ReportItems == null)
            {
                this.Body.ReportItems = new ReportItemsBody()
                {
                    Text = new List<Text>(),
                    Cards = new List<Card>(),
                    CustomReportItems = new List<CustomReportItems>()
                };
            }

            if (includeTable && this.Body.ReportItems.Table == null)
            {
                this.Body.ReportItems.Table = new Table();
            }
        }

        public Report WithCard(Card card)
        {
            InitReportItemBody(false);

            this.Body.ReportItems.Cards.Add(card);

            return this;
        }

        public Report WithTableColumns(TableColumns tableColumns)
        {
            this.Body.ReportItems.Table.TableColumns = tableColumns;
            return this;
        }

        public Report WithTableHeader(TableRow header, string repeatOnNewPage = "true")
        {
            this.Body.ReportItems.Table.Header = new Header()
            {
                TableRows = new TableRows()
                {
                    TableRow = header
                },
                RepeatOnNewPage = repeatOnNewPage
            };
            return this;
        }

        public Report WithTableDetails(TableRow row)
        {
            this.Body.ReportItems.Table.Details = new Details();
            this.Body.ReportItems.Table.Details.TableRows = new TableRows()
            {
                TableRow = row
            };
            return this;
        }

        public Report WithTableDataSetName(string dataSetName)
        {
            this.Body.ReportItems.Table.DataSetName = dataSetName;
            return this;
        }

        public Report WithTableNoRows(string noRows)
        {
            this.Body.ReportItems.Table.NoRows = noRows;
            return this;
        }

        public Report WithTableName(string tableName)
        {
            this.Body.ReportItems.Table.TableName = tableName;
            return this;
        }

        public Report WithImage(ReportItemImage image)
        {
            InitReportItemBody(false);
            if (this.Body.ReportItems.Images == null)
            {
                this.Body.ReportItems.Images = new List<ReportItemImage>();
            }
            this.Body.ReportItems.Images.Add(image);
            return this;
        }

        /// <summary>
        /// Create a PDF document from the Report definition.
        /// If you wish to create an RDL report use the Create class.
        /// </summary>
        /// <param name="ouput"></param>
        public async Task Create(Stream ouput)
        {
            await Create(ouput, Majorsilence.Reporting.Rdl.OutputPresentationType.PDF);
        }
        
        /// <summary>
        /// Create a PDF document from the Report definition.
        /// If you wish to create an RDL report use the Create class.
        /// </summary>
        /// <param name="ouput"></param>
        public async Task Create(string filepath)
        {
            await using var ouput = File.Create(filepath);
            await Create(ouput, Majorsilence.Reporting.Rdl.OutputPresentationType.PDF);
        }

        /// <summary>
        /// Create a report in PDF/Html/Excel or other support types from the Report definition.
        /// If you wish to create an RDL report use the Create class.
        /// </summary>
        /// <param name="ouput"></param>
        public async Task Create(Stream ouput, Majorsilence.Reporting.Rdl.OutputPresentationType outputType)
        {
            var create = new Create();
            using var report = await create.GenerateRdl(this);
            using var streamGen = new Majorsilence.Reporting.Rdl.MemoryStreamGen();
            await report.RunRender(streamGen, outputType);
            await using var reportData = streamGen.GetStream();
            reportData.Position = 0;
            await reportData.CopyToAsync(ouput);
        }
    }
}