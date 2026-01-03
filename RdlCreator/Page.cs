using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Majorsilence.Reporting.RdlCreator
{
    
    internal static class AotPreserve
    {
        [ModuleInitializer]
        internal static void PreserveProviderTypes()
        {
            // Touch types to keep them from being trimmed.
            // Requires you to reference the provider packages at compile time.
            _ = typeof(Page);
            _ = typeof(Report);
            _ = typeof(Document);
        }
    }
    
#if AOT
    [DotWrap.DotWrapExpose] 
#endif
    public class Page
    {
        [XmlElement(ElementName = "PageHeader")]
        public PageHeader PageHeader { get; set; }

        [XmlElement(ElementName = "Body")]
        public Body Body { get; set; }

        [XmlElement(ElementName = "PageFooter")]
        public PageFooter PageFooter { get; set; }

        public Page WithHeight(ReportItemSize height)
        {
            if (this.Body == null)
            {
                this.Body = new Body();
            }
            this.Body.Height = height;
            return this;
        }

        public Page WithWidth(ReportItemSize width)
        {
            if (this.Body == null)
            {
                this.Body = new Body();
            }
            this.Body.Width = width;
            return this;
        }

        public Page WithPageHeader(PageHeader pageHeader)
        {
            PageHeader = pageHeader;
            return this;
        }
        public Page WithPageFooter(PageFooter pageFooter)
        {
            PageFooter = pageFooter;
            return this;
        }

        public Page WithText(Text textbox)
        {

            InitReportItemBody(false);
            this.Body.ReportItems.Text.Add(textbox);

            return this;
        }

        public Page WithCard(Card card)
        {
            InitReportItemBody(false);

            this.Body.ReportItems.Cards.Add(card);

            return this;
        }

        public Page WithTableColumns(TableColumns tableColumns)
        {
            InitReportItemBody(true);
            this.Body.ReportItems.Table.TableColumns = tableColumns;
            return this;
        }

        public Page WithTableHeader(TableRow header, string repeatOnNewPage = "true")
        {
            InitReportItemBody(true);
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

        public Page WithTableDetails(TableRow row)
        {
            InitReportItemBody(true);
            this.Body.ReportItems.Table.Details = new Details();
            this.Body.ReportItems.Table.Details.TableRows = new TableRows()
            {
                TableRow = row
            };
            return this;
        }

        public Page WithTableNoRows(string noRows)
        {
            InitReportItemBody(true);

            this.Body.ReportItems.Table.NoRows = noRows;
            return this;
        }

        public Page WithTableName(string tableName)
        {
            InitReportItemBody(true);
            this.Body.ReportItems.Table.TableName = tableName;
            return this;
        }

        public Page WithImage(ReportItemImage image)
        {
            InitReportItemBody(false);
            if (this.Body.ReportItems.Images == null)
            {
                this.Body.ReportItems.Images = new List<ReportItemImage>();
            }
            this.Body.ReportItems.Images.Add(image);
            return this;
        }

        private void InitReportItemBody(bool includeTable)
        {
            if (this.Body == null)
            {
                this.Body = new Body();
            }

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
    }
}
