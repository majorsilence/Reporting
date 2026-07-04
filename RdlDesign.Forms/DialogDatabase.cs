using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Majorsilence.Forms;
using System.Text;
using System.Data;
using System.Data.OleDb;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.IO;
using System.Globalization;
using System.Xml;
using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RdlDesign.Resources;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// Summary description for DialogDatabase.
    /// </summary>
    public partial class DialogDatabase 
    {
        RdlDesigner _rDesigner = null;
        RdlUserControl _rUserControl = null;
        static private readonly string SHARED_CONNECTION = "Shared Data Source";
        string _StashConnection = null;

        private List<SqlColumn> _ColumnList = null;
        private string _TempFileName = null;
        private string _ResultReport = "nothing";

        private readonly string _Schema2003 =
"xmlns=\"http://schemas.microsoft.com/sqlserver/reporting/2003/10/reportdefinition\" xmlns:rd=\"http://schemas.microsoft.com/SQLServer/reporting/reportdesigner\"";
        private readonly string _Schema2005 =
"xmlns=\"http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition\" xmlns:rd=\"http://schemas.microsoft.com/SQLServer/reporting/reportdesigner\"";

        private string _TemplateChart = " some junk";
        private string _TemplateMatrix = " some junk";
        private string _TemplateTable = @"<?xml version='1.0' encoding='UTF-8'?>
<Report |schema| > 
	<Description>|description|</Description>
	<Author>|author|</Author>
	|orientation|
	<DataSources>
		<DataSource Name='DS1'>
			|connectionproperties|
		</DataSource>
	</DataSources>
	<Width>7.5in</Width>
	<TopMargin>.25in</TopMargin>
	<LeftMargin>.25in</LeftMargin>
	<RightMargin>.25in</RightMargin>
	<BottomMargin>.25in</BottomMargin>
	|reportparameters|
	<DataSets>
		<DataSet Name='Data'>
			<Query>
				<DataSourceName>DS1</DataSourceName>
				<CommandText>|sqltext|</CommandText>
				|queryparameters|
			</Query>
			<Fields>
			|sqlfields|
			</Fields>
		</DataSet>
	</DataSets>
	<PageHeader>
		<Height>.5in</Height>
|ifdef reportname|
		<ReportItems>
			<Textbox><Top>.1in</Top><Left>.1in</Left><Width>6in</Width><Height>.25in</Height><Value>|reportnameasis|</Value><Style><FontSize>15pt</FontSize><FontWeight>Bold</FontWeight></Style></Textbox> 
		</ReportItems>
|endif|
        <PrintOnFirstPage>true</PrintOnFirstPage>
        <PrintOnLastPage>true</PrintOnLastPage>
	</PageHeader>
	<Body>
		<ReportItems>
			<Table>
				<DataSetName>Data</DataSetName>
				<NoRows>Query returned no rows!</NoRows>
				<Style><BorderStyle><Default>Solid</Default></BorderStyle></Style>
				<TableColumns>
					|tablecolumns|
				</TableColumns>
				<Header>
					<TableRows>
						<TableRow>
							<Height>12pt</Height>
							<TableCells>|tableheaders|</TableCells>
						</TableRow>
					</TableRows>
					<RepeatOnNewPage>true</RepeatOnNewPage>
				</Header>
|ifdef grouping|
				<TableGroups>
				<TableGroup>
				<Header>
					<TableRows>
						<TableRow>
							<Height>12pt</Height>
							<TableCells>
								<TableCell>
									<ColSpan>|columncount|</ColSpan>
									<ReportItems><Textbox><Value>=Fields.|groupbycolumn|.Value</Value><Style><PaddingLeft>2pt</PaddingLeft><BorderStyle><Default>Solid</Default></BorderStyle><FontWeight>Bold</FontWeight></Style></Textbox></ReportItems>
								</TableCell>
							</TableCells>
						</TableRow>
					</TableRows>
					<RepeatOnNewPage>true</RepeatOnNewPage>
				</Header>
				<Footer>
					<TableRows>
						<TableRow>
							<Height>12pt</Height>
							<TableCells>|gtablefooters|</TableCells>
						</TableRow>
					</TableRows>
				</Footer>
		<Grouping Name='|groupbycolumn|Group'><GroupExpressions><GroupExpression>=Fields!|groupbycolumn|.Value</GroupExpression></GroupExpressions></Grouping>
		</TableGroup>
		</TableGroups>
|endif|
				<Details>
					<TableRows>
						<TableRow>
							<Height>12pt</Height>
							<TableCells>|tablevalues|</TableCells>
						</TableRow>
					</TableRows>
				</Details>
|ifdef footers|
				<Footer>
					<TableRows>
						<TableRow>
							<Height>12pt</Height>
							<TableCells>|tablefooters|</TableCells>
						</TableRow>
					</TableRows>
				</Footer>
|endif|
			</Table>
		</ReportItems>
		<Height>|bodyheight|</Height>
	</Body>
	<PageFooter>
		<Height>14pt</Height>
		<ReportItems>
			<Textbox><Top>1pt</Top><Left>10pt</Left><Height>12pt</Height><Width>3in</Width>
				<Value>=Globals!PageNumber + ' of ' + Globals!TotalPages</Value>
				<Style><FontSize>10pt</FontSize><FontWeight>Normal</FontWeight></Style>
			</Textbox> 	
		</ReportItems>
        <PrintOnFirstPage>true</PrintOnFirstPage>
        <PrintOnLastPage>true</PrintOnLastPage>
	</PageFooter>
</Report>";

        private string _TemplateList = @"<?xml version='1.0' encoding='UTF-8'?>
<Report |schema| > 
	<Description>|description|</Description>
	<Author>|author|</Author>
	|orientation|
	<DataSources>
		<DataSource Name='DS1'>
			|connectionproperties|
		</DataSource>
	</DataSources>
	<Width>7.5in</Width>
	<TopMargin>.25in</TopMargin>
	<LeftMargin>.25in</LeftMargin>
	<RightMargin>.25in</RightMargin>
	<BottomMargin>.25in</BottomMargin>
	|reportparameters|
	<DataSets>
		<DataSet Name='Data'>
			<Query>
				<DataSourceName>DS1</DataSourceName>
				<CommandText>|sqltext|</CommandText>
				|queryparameters|
			</Query>
			<Fields>
				|sqlfields|
			</Fields>
		</DataSet>
	</DataSets>
	<PageHeader>
		<Height>.5in</Height>
		<ReportItems>
|ifdef reportname|
			<Textbox><Top>.02in</Top><Left>.1in</Left><Width>6in</Width><Height>.25in</Height><Value>|reportname|</Value><Style><FontSize>15pt</FontSize><FontWeight>Bold</FontWeight></Style></Textbox> 
|endif|
			|listheaders|
		</ReportItems>
        <PrintOnFirstPage>true</PrintOnFirstPage>
        <PrintOnLastPage>true</PrintOnLastPage>
	</PageHeader>
	<Body><Height>25pt</Height>
		<ReportItems>
			<List>
				<DataSetName>Data</DataSetName>
				<Height>24pt</Height>
				<NoRows>Query returned no rows!</NoRows>
				<ReportItems>
					|listvalues|
				</ReportItems>
				<Width>|listwidth|</Width>
			</List>
		</ReportItems>
		</Body>
	<PageFooter>
		<Height>14pt</Height>
		<ReportItems>
			<Textbox><Top>1pt</Top><Left>10pt</Left><Height>12pt</Height><Width>3in</Width>
				<Value>=Globals!PageNumber + ' of ' + Globals!TotalPages</Value>
				<Style><FontSize>10pt</FontSize><FontWeight>Normal</FontWeight></Style>
			</Textbox> 	
		</ReportItems>
        <PrintOnFirstPage>true</PrintOnFirstPage>
        <PrintOnLastPage>true</PrintOnLastPage>
	</PageFooter>
		</Report>";

        private string _TemplateEmpty = @"<?xml version='1.0' encoding='UTF-8'?>
<Report |schema| > 
	<Description>|description|</Description>
	<Author>|author|</Author>
	|orientation|
	<Width>7.5in</Width>
	<TopMargin>.25in</TopMargin>
	<LeftMargin>.25in</LeftMargin>
	<RightMargin>.25in</RightMargin>
	<BottomMargin>.25in</BottomMargin>
	|reportparameters|
	<PageHeader>
		<Height>.5in</Height>
        <PrintOnFirstPage>true</PrintOnFirstPage>
        <PrintOnLastPage>true</PrintOnLastPage>
	</PageHeader>
	<Body><Height>25pt</Height></Body>
	<PageFooter>
		<Height>14pt</Height>
		<ReportItems>
			<Textbox><Top>1pt</Top><Left>10pt</Left><Height>12pt</Height><Width>3in</Width>
				<Value>=Globals!PageNumber + ' of ' + Globals!TotalPages</Value>
				<Style><FontSize>10pt</FontSize><FontWeight>Normal</FontWeight></Style>
			</Textbox> 	
		</ReportItems>
        <PrintOnFirstPage>true</PrintOnFirstPage>
        <PrintOnLastPage>true</PrintOnLastPage>
	</PageFooter>
		</Report>";


        public DialogDatabase(RdlDesigner rDesigner)
        {
            _rDesigner = rDesigner;
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            string[] items = RdlEngineConfig.GetProviders();
            Array.Sort(items);
            cbConnectionTypes.Items.Add(SHARED_CONNECTION);
            cbConnectionTypes.Items.AddRange(items);
            cbConnectionTypes.SelectedIndex = 1;
            cbOrientation.SelectedIndex = 0;
        }


        public DialogDatabase(RdlUserControl rDesigner)
        {
            _rUserControl = rDesigner;
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            string[] items = RdlEngineConfig.GetProviders();
            cbConnectionTypes.Items.Add(SHARED_CONNECTION);
            cbConnectionTypes.Items.AddRange(items);
            cbConnectionTypes.SelectedIndex = 1;
            cbOrientation.SelectedIndex = 0;
        }

        public enum ConnectionType
        {
            MYSQL,
            MSSQL,
            SQLITE,
            POSTGRESQL,
            XML,
            WEBSERVICE
        }

        /// <summary>
        /// Pre set a connection string.  This is useful when you need to create
        /// a new report from a program other then RdlDesigner. 
        /// </summary>
        /// <param name="connectionString">A full and correct database connection string</param>
        /// <param name="hideConnectionTab">If true this will hide the connection string.  This is useful
        /// when you do not want the users to need to know this information.</param>
        /// <param name="ct">The database connection type.</param>
        public void SetConnection(string connectionString, bool hideConnectionTab, ConnectionType ct)
        {

            if (ct == ConnectionType.MSSQL)
            {
                cbConnectionTypes.SelectedItem = "SQL";
            }
            else if (ct == ConnectionType.MYSQL)
            {
                cbConnectionTypes.SelectedItem = "MySQL.NET";
            }
            else if (ct == ConnectionType.POSTGRESQL)
            {
                cbConnectionTypes.SelectedItem = "PostgreSQL";
            }
            else if (ct == ConnectionType.SQLITE)
            {
                cbConnectionTypes.SelectedItem = "SQLite";
            }
            else if (ct == ConnectionType.WEBSERVICE)
            {
                cbConnectionTypes.SelectedItem = "WebService";
            }
            else if (ct == ConnectionType.XML)
            {
                cbConnectionTypes.SelectedItem = "XML";
            }
            else
            {
                throw new Exception("You should not have reached this far in the SetConnection function.");
            }

            tbConnection.Text = connectionString;

            if (hideConnectionTab == true)
            {
                tcDialog.TabPages.Remove(tcDialog.TabPages["DBConnection"]);
            }

        }

        public string ResultReport
        {
            get { return _ResultReport; }
        }

        private void btnOK_Click(object sender, System.EventArgs e)
        {
            if (!DoReportSyntax(false))
                return;
            DialogResult = DialogResult.OK;
            _ResultReport = tbReportSyntax.Text;
            this.Close();
        }

        private async void tabControl1_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            TabControl tc = (TabControl)sender;
            string tag = (string)tc.TabPages[tc.SelectedIndex].Tag;
            switch (tag)
            {
                case "type":	// nothing to do here
                    break;
                case "connect":	// nothing to do here
                    break;
                case "sql":		// obtain table and column information
                    DoSqlSchema();
                    break;
                case "group":	// obtain group by information using connection & sql
                    DoGrouping();
                    break;
                case "syntax":	// obtain report using connection, sql, 
                    DoReportSyntax(false);
                    break;
                case "preview":	// run report using generated report syntax
                    await DoReportPreview();
                    break;
                default:
                    break;
            }
        }

        // Fill out tvTablesColumns
        private void DoSqlSchema()
        {
            // TODO be more efficient and remember schema info;
            //   need to mark changes to connections
            if (tvTablesColumns.Nodes.Count > 0)
                return;

            // suppress redraw until tree view is complete
            tvTablesColumns.BeginUpdate();

            // Get the schema information
            List<SqlSchemaInfo> si = DesignerUtility.GetSchemaInfo(GetDataProvider(), GetDataConnection());
            TreeNode ndRoot = new TreeNode("Tables");
            tvTablesColumns.Nodes.Add(ndRoot);
            bool bView = false;
            foreach (SqlSchemaInfo ssi in si)
            {
                if (!bView && ssi.Type == "VIEW")
                {	// Switch over to views
                    ndRoot = new TreeNode("Views");
                    tvTablesColumns.Nodes.Add(ndRoot);
                    bView = true;
                }

                // Add the node to the tree
                TreeNode aRoot = new TreeNode(ssi.Name);
                ndRoot.Nodes.Add(aRoot);
                aRoot.Nodes.Add("");
            }

            // Now do parameters
            //if (lbParameters.Items.Count > 0)
            //{
            //    ndRoot = new TreeNode("Parameters");
            //    tvTablesColumns.Nodes.Add(ndRoot);
            //    foreach (ReportParm rp in lbParameters.Items)
            //    {
            //        string paramName;

            //        // force the name to start with @
            //        if (rp.Name[0] == '@')
            //            paramName = rp.Name;
            //        else
            //            paramName = "@" + rp.Name;

            //        // Add the node to the tree
            //        TreeNode aRoot = new TreeNode(paramName);
            //        ndRoot.Nodes.Add(aRoot);
            //    }
            //}

            tvTablesColumns.EndUpdate();
        }

        private void DoGrouping()
        {
            if (cbColumnList.Items.Count > 0)		// We already have the columns?
                return;
            if (!rbEmpty.Checked)
            {
                if (_ColumnList == null)
                    _ColumnList = DesignerUtility.GetSqlColumns(GetDataProvider(), GetDataConnection(), tbSQL.Text, reportParameterCtl1.lbParameters.Items);

                foreach (SqlColumn sq in _ColumnList)
                {
                    cbColumnList.Items.Add(sq);
                    clbSubtotal.Items.Add(sq);
                }

                SqlColumn sqc = new SqlColumn();
                sqc.Name = "";
                cbColumnList.Items.Add(sqc);
            }
            return;
        }

        private bool DoReportSyntax(bool UseFullSharedDSName)
        {
            string template;

            if (rbList.Checked)
                template = _TemplateList;
            else if (rbTable.Checked)
                template = _TemplateTable;
            else if (rbMatrix.Checked)
                template = _TemplateMatrix;
            else if (rbChart.Checked)
                template = _TemplateChart;
            else if (rbEmpty.Checked)
                template = _TemplateEmpty;
            else
                template = _TemplateTable;	// default to table- should never reach

            if (!rbEmpty.Checked)
            {
                if (_ColumnList == null)
                    _ColumnList = DesignerUtility.GetSqlColumns(GetDataProvider(), GetDataConnection(), tbSQL.Text, reportParameterCtl1.lbParameters.Items);

                if (_ColumnList.Count == 0)     // can only happen by an error
                    return false;
            }

            string[] parts = template.Split('|');
            StringBuilder sb = new StringBuilder(template.Length);
            decimal left = 0m;
            decimal width;
            decimal bodyHeight = 0;
            string name;
            int skip = 0;					// skip is used to allow nesting of ifdef 
            string args;
            string canGrow;
            string align;
            // handle the group by column
            string gbcolumn;
            if (this.cbColumnList.Text.Length > 0)
                gbcolumn = GetFieldName(this.cbColumnList.Text);
            else
                gbcolumn = null;

            CultureInfo cinfo = new CultureInfo("", false);

            foreach (string p in parts)
            {
                // Handle conditional special
                int length = 5;
                if (p.Length < 5)
                {
                    length = p.Length;
                }
                if (p.Substring(0, length) == "ifdef")
                {
                    args = p.Substring(6);
                    switch (args)
                    {
                        case "reportname":
                            if (tbReportName.Text.Length == 0)
                                skip++;
                            break;
                        case "description":
                            if (tbReportDescription.Text.Length == 0)
                                skip++;
                            break;
                        case "author":
                            if (tbReportAuthor.Text.Length == 0)
                                skip++;
                            break;
                        case "grouping":
                            if (gbcolumn == null)
                                skip++;
                            break;
                        case "footers":
                            if (!ckbGrandTotal.Checked)
                                skip++;
                            else if (clbSubtotal.CheckedItems.Count <= 0)
                                skip++;
                            break;
                        default:
                            throw new Exception(String.Format(Strings.DialogDatabase_Error_UnknownIfdef, args));
                    }
                    continue;
                }

                // if skipping lines (due to ifdef) then go to next endif
                if (skip > 0 && p != "endif")
                    continue;

                switch (p)
                {
                    case "endif":
                        if (skip > 0)
                            skip--;
                        break;
                    case "schema":
                        if (this.rbSchema2003.Checked)
                            sb.Append(_Schema2003);
                        else if (this.rbSchema2005.Checked)
                            sb.Append(_Schema2005);
                        break;
                    case "reportname":
                        sb.Append(tbReportName.Text.Replace('\'', '_'));
                        break;
                    case "reportnameasis":
                        sb.Append(tbReportName.Text);
                        break;
                    case "description":
                        sb.Append(tbReportDescription.Text);
                        break;
                    case "author":
                        sb.Append(tbReportAuthor.Text);
                        break;
                    case "connectionproperties":
                        if (this.cbConnectionTypes.Text == SHARED_CONNECTION)
                        {
                            string file = this.tbConnection.Text;
                            if (!UseFullSharedDSName)
                                file = Path.GetFileNameWithoutExtension(file);      // when we save report we use qualified name
                            sb.AppendFormat("<DataSourceReference>{0}</DataSourceReference>", file);
                        }
                        else
                            sb.AppendFormat("<ConnectionProperties><DataProvider>{0}</DataProvider><ConnectString>{1}</ConnectString></ConnectionProperties>",
                                GetDataProvider(), GetDataConnection());
                        break;
                    case "dataprovider":
                        sb.Append(GetDataProvider());
                        break;
                    case "connectstring":
                        sb.Append(tbConnection.Text);
                        break;
                    case "columncount":
                        sb.Append(_ColumnList.Count);
                        break;
                    case "orientation":
                        if (this.cbOrientation.SelectedIndex == 0)
                        {	// Portrait is first in the list
                            sb.Append("<PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth>");
                        }
                        else
                        {
                            sb.Append("<PageHeight>8.5in</PageHeight><PageWidth>11in</PageWidth>");
                        }
                        break;
                    case "groupbycolumn":
                        sb.Append(gbcolumn);
                        break;
                    case "reportparameters":
                        DoReportSyntaxParameters(cinfo, sb);
                        break;
                    case "queryparameters":
                        DoReportSyntaxQParameters(cinfo, sb, tbSQL.Text);
                        break;
                    case "sqltext":
                        sb.Append(tbSQL.Text.Replace("<", "&lt;"));
                        break;
                    case "sqlfields":
                        foreach (SqlColumn sq in _ColumnList)
                        {
                            name = GetFieldName(sq.Name);
                            string type = sq.DataType.FullName;
                            if (this.rbSchemaNo.Checked)
                                sb.AppendFormat(cinfo, "<Field Name='{0}'><DataField>{1}</DataField><TypeName>{2}</TypeName></Field>", name, sq.Name, type);
                            else
                                sb.AppendFormat(cinfo, "<Field Name='{0}'><DataField>{1}</DataField><rd:TypeName>{2}</rd:TypeName></Field>", name, sq.Name, type);
                        }
                        break;
                    case "listheaders":
                        left = .0m;
                        foreach (SqlColumn sq in _ColumnList)
                        {
                            name = sq.Name;
                            width = name.Length / 8m;
                            if (width < 1)
                                width = 1;
                            sb.AppendFormat(cinfo, @"
		<Textbox><Top>.3in</Top><Left>{0}in</Left><Width>{1}in</Width><Height>.2in</Height><Value>{2}</Value>
			<Style><FontWeight>Bold</FontWeight><BorderStyle><Bottom>Solid</Bottom></BorderStyle>
				<BorderWidth><Bottom>3pt</Bottom></BorderWidth></Style>
		</Textbox>",
                                left,
                                width,
                                name);
                            left += width;
                        }
                        break;
                    case "listvalues":
                        left = .0m;
                        foreach (SqlColumn sq in _ColumnList)
                        {
                            name = GetFieldName(sq.Name);
                            DoAlignAndCanGrow(sq.DataType, out canGrow, out align);
                            width = name.Length / 8m;
                            if (width < 1)
                                width = 1;
                            sb.AppendFormat(cinfo, @"
		<Textbox Name='{2}'><Top>.1in</Top><Left>{0}in</Left><Width>{1}in</Width><Height>.25in</Height><Value>=Fields!{2}.Value</Value><CanGrow>{3}</CanGrow><Style>{4}</Style></Textbox>",
                                left, width, name, canGrow, align);
                            left += width;
                        }
                        bodyHeight = .4m;
                        break;
                    case "listwidth":		// in template list width must follow something that sets left
                        sb.AppendFormat(cinfo, "{0}in", left);
                        break;
                    case "tableheaders":
                        // the group by column is always the first one in the table
                        if (gbcolumn != null)
                        {
                            bodyHeight += 12m;
                            sb.AppendFormat(cinfo, @"
							<TableCell>
								<ReportItems><Textbox><Value>{0}</Value><Style><TextAlign>Center</TextAlign><BorderStyle><Default>Solid</Default></BorderStyle><FontWeight>Bold</FontWeight></Style></Textbox></ReportItems>
							</TableCell>",
                                this.cbColumnList.Text);
                        }
                        bodyHeight += 12m;
                        foreach (SqlColumn sq in _ColumnList)
                        {
                            name = sq.Name;
                            if (name == this.cbColumnList.Text)
                                continue;
                            sb.AppendFormat(cinfo, @"
							<TableCell>
								<ReportItems><Textbox><Value>{0}</Value><Style><TextAlign>Center</TextAlign><BorderStyle><Default>Solid</Default></BorderStyle><FontWeight>Bold</FontWeight></Style></Textbox></ReportItems>
							</TableCell>",
                                name);
                        }
                        break;
                    case "tablecolumns":
                        if (gbcolumn != null)
                        {
                            bodyHeight += 12m;
                            width = gbcolumn.Length / 8m;		// TODO should really use data value
                            if (width < 1)
                                width = 1;
                            sb.AppendFormat(cinfo, @"<TableColumn><Width>{0}in</Width></TableColumn>", width);
                        }
                        bodyHeight += 12m;
                        foreach (SqlColumn sq in _ColumnList)
                        {
                            name = GetFieldName(sq.Name);
                            if (name == gbcolumn)
                                continue;
                            width = name.Length / 8m;		// TODO should really use data value
                            if (width < 1)
                                width = 1;
                            sb.AppendFormat(cinfo, @"<TableColumn><Width>{0}in</Width></TableColumn>", width);
                        }
                        break;
                    case "tablevalues":
                        bodyHeight += 12m;
                        if (gbcolumn != null)
                        {
                            sb.Append(@"<TableCell>
								<ReportItems><Textbox><Value></Value><Style><BorderStyle><Default>None</Default><Left>Solid</Left></BorderStyle></Style></Textbox></ReportItems>
							</TableCell>");
                        }
                        foreach (SqlColumn sq in _ColumnList)
                        {
                            name = GetFieldName(sq.Name);
                            if (name == gbcolumn)
                                continue;
                            DoAlignAndCanGrow(sq.DataType, out canGrow, out align);
                            sb.AppendFormat(cinfo, @"
							<TableCell>
								<ReportItems><Textbox Name='{0}'><Value>=Fields!{0}.Value</Value><CanGrow>{1}</CanGrow><Style><BorderStyle><Default>Solid</Default></BorderStyle>{2}</Style></Textbox></ReportItems>
							</TableCell>",
                                name, canGrow, align);
                        }
                        break;
                    case "gtablefooters":
                    case "tablefooters":
                        bodyHeight += 12m;
                        canGrow = "false";
                        align = "";
                        string nameprefix = p == "gtablefooters" ? "gf" : "tf";
                        if (gbcolumn != null)	// handle group by column first
                        {
                            int i = clbSubtotal.FindStringExact(this.cbColumnList.Text);
                            SqlColumn sq = i < 0 ? null : (SqlColumn)clbSubtotal.Items[i];
                            if (i >= 0 && clbSubtotal.GetItemChecked(i))
                            {
                                string funct = DesignerUtility.IsNumeric(sq.DataType) ? "Sum" : "Count";

                                DoAlignAndCanGrow(((object)0).GetType(), out canGrow, out align);
                                sb.AppendFormat(cinfo, @"
							<TableCell>
								<ReportItems><Textbox Name='{4}_{0}'><Value>={1}(Fields!{0}.Value)</Value><CanGrow>{2}</CanGrow><Style><BorderStyle><Default>Solid</Default></BorderStyle>{3}</Style></Textbox></ReportItems>
							</TableCell>",
                                    gbcolumn, funct, canGrow, align, nameprefix);
                            }
                            else
                            {
                                sb.AppendFormat(cinfo, "<TableCell><ReportItems><Textbox><Value></Value><Style><BorderStyle><Default>Solid</Default></BorderStyle></Style></Textbox></ReportItems></TableCell>");
                            }
                        }
                        for (int i = 0; i < this.clbSubtotal.Items.Count; i++)
                        {
                            SqlColumn sq = (SqlColumn)clbSubtotal.Items[i];
                            name = GetFieldName(sq.Name);
                            if (name == gbcolumn)
                                continue;
                            if (clbSubtotal.GetItemChecked(i))
                            {
                                string funct = DesignerUtility.IsNumeric(sq.DataType) ? "Sum" : "Count";

                                DoAlignAndCanGrow(((object)0).GetType(), out canGrow, out align);
                                sb.AppendFormat(cinfo, @"
							<TableCell>
								<ReportItems><Textbox Name='{4}_{0}'><Value>={1}(Fields!{0}.Value)</Value><CanGrow>{2}</CanGrow><Style><BorderStyle><Default>Solid</Default></BorderStyle>{3}</Style></Textbox></ReportItems>
							</TableCell>",
                                    name, funct, canGrow, align, nameprefix);
                            }
                            else
                            {
                                sb.AppendFormat(cinfo, "<TableCell><ReportItems><Textbox><Value></Value><Style><BorderStyle><Default>Solid</Default></BorderStyle></Style></Textbox></ReportItems></TableCell>");
                            }
                        }
                        break;
                    case "bodyheight":	// Note: this must follow the table definition
                        sb.AppendFormat(cinfo, "{0}pt", bodyHeight);
                        break;
                    default:
                        sb.Append(p);
                        break;
                }
            }

            try
            {
                tbReportSyntax.Text = DesignerUtility.FormatXml(sb.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.DialogDatabase_Show_InternalError);
                tbReportSyntax.Text = sb.ToString();
            }
            return true;
        }

        private string GetFieldName(string sqlName)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in sqlName)
            {
                if (Char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
                else
                    sb.Append('_');
            }
            return sb.ToString();
        }

        private void DoAlignAndCanGrow(Type t, out string canGrow, out string align)
        {
            string st = t.ToString();
            switch (st)
            {
                case "System.String":
                    canGrow = "true";
                    align = "<PaddingLeft>2pt</PaddingLeft>";
                    break;
                case "System.Int16":
                case "System.Int32":
                case "System.Single":
                case "System.Double":
                case "System.Decimal":
                    canGrow = "false";
                    align = "<PaddingRight>2pt</PaddingRight><TextAlign>Right</TextAlign>";
                    break;
                default:
                    canGrow = "false";
                    align = "<PaddingLeft>2pt</PaddingLeft>";
                    break;
            }
            return;
        }


        private void DoReportSyntaxParameters(CultureInfo cinfo, StringBuilder sb)
        {
            if (reportParameterCtl1.lbParameters.Items.Count <= 0)
                return;

            sb.Append("<ReportParameters>");
            foreach (ReportParm rp in reportParameterCtl1.lbParameters.Items)
            {
                sb.AppendFormat(cinfo, "<ReportParameter Name=\"{0}\">", rp.Name);
                sb.AppendFormat(cinfo, "<DataType>{0}</DataType>", rp.DataType);
                sb.AppendFormat(cinfo, "<Nullable>{0}</Nullable>", rp.AllowNull.ToString());
                if (rp.DefaultValue != null && rp.DefaultValue.Count > 0)
                {
                    sb.AppendFormat(cinfo, "<DefaultValue><Values>");
                    foreach (string dv in rp.DefaultValue)
                    {
                        sb.AppendFormat(cinfo, "<Value>{0}</Value>", XmlUtil.XmlAnsi(dv));
                    }
                    sb.AppendFormat(cinfo, "</Values></DefaultValue>");
                }
                sb.AppendFormat(cinfo, "<AllowBlank>{0}</AllowBlank>", rp.AllowBlank);
                if (rp.Prompt != null && rp.Prompt.Length > 0)
                    sb.AppendFormat(cinfo, "<Prompt>{0}</Prompt>", rp.Prompt);
                if (rp.ValidValues != null && rp.ValidValues.Count > 0)
                {
                    sb.Append("<ValidValues><ParameterValues>");
                    foreach (ParameterValueItem pvi in rp.ValidValues)
                    {
                        sb.Append("<ParameterValue>");
                        sb.AppendFormat(cinfo, "<Value>{0}</Value>", XmlUtil.XmlAnsi(pvi.Value));
                        if (pvi.Label != null)
                            sb.AppendFormat(cinfo, "<Label>{0}</Label>", XmlUtil.XmlAnsi(pvi.Label));
                        sb.Append("</ParameterValue>");
                    }
                    sb.Append("</ParameterValues></ValidValues>");
                }
                sb.Append("</ReportParameter>");
            }
            sb.Append("</ReportParameters>");
        }

        private void DoReportSyntaxQParameters(CultureInfo cinfo, StringBuilder sb, string sql)
        {
            if (reportParameterCtl1.lbParameters.Items.Count <= 0)
                return;

            bool bFirst = true;
            foreach (ReportParm rp in reportParameterCtl1.lbParameters.Items)
            {
                // force the name to start with @
                string paramName;
                if (rp.Name[0] == '@')
                    paramName = rp.Name;
                else
                    paramName = "@" + rp.Name;

                // Only create a query parameter if parameter is used in the query
                if (sql.IndexOf(paramName) >= 0)
                {
                    if (bFirst)
                    {	// Only put out queryparameters if we actually have one
                        sb.Append("<QueryParameters>");
                        bFirst = false;
                    }
                    sb.AppendFormat(cinfo, "<QueryParameter Name=\"{0}\">", rp.Name);
                    sb.AppendFormat(cinfo, "<Value>=Parameters!{0}</Value>", rp.Name);
                    sb.Append("</QueryParameter>");
                }
            }
            if (!bFirst)
                sb.Append("</QueryParameters>");
        }

        private async Task<bool> DoReportPreview()
        {
            if (!DoReportSyntax(true))
                return false;

            if (_rDesigner != null)
            {
                rdlViewer1.GetDataSourceReferencePassword = _rDesigner.SharedDatasetPassword;
            }
            else if (_rUserControl != null)
            {
                rdlViewer1.GetDataSourceReferencePassword = _rUserControl.SharedDatasetPassword;
            }
            await rdlViewer1.SetSourceRdl(tbReportSyntax.Text);
            return true;
        }

        private string GetDataProvider()
        {
            string cType = cbConnectionTypes.Text;
            _StashConnection = null;
            if (cType == SHARED_CONNECTION)
            {
                if (_rDesigner != null)
                {
                    if (!DesignerUtility.GetSharedConnectionInfo(_rDesigner, tbConnection.Text, out cType, out _StashConnection))
                        return null;
                }
                else if(_rUserControl != null)
                {
                    if (!DesignerUtility.GetSharedConnectionInfo(_rUserControl, tbConnection.Text, out cType, out _StashConnection))
                        return null;
                }
            }
            else
            {
                _StashConnection = tbConnection.Text;
            }
            return cType;
        }

        private string GetDataConnection()
        {	// GetDataProvider must be called first to ensure the DataConnection is correct.
            return _StashConnection;
        }

        private void tvTablesColumns_BeforeExpand(object sender, Majorsilence.Forms.TreeViewCancelEventArgs e)
        {
            tvTablesColumns_ExpandTable(e.Node);
        }

        private void tvTablesColumns_ExpandTable(TreeNode tNode)
        {
            if (tNode.Parent == null)	// Check for Tables or Views
                return;

            if (tNode.FirstNode.Text != "")	// Have we already filled it out?
                return;

            // Need to obtain the column information for the requested table/view
            // suppress redraw until tree view is complete
            tvTablesColumns.BeginUpdate();

            string sql = "SELECT * FROM " + DesignerUtility.NormalizeSqlName(tNode.Text);
            List<SqlColumn> tColumns = DesignerUtility.GetSqlColumns(GetDataProvider(), GetDataConnection(), sql, null);
            bool bFirstTime = true;
            foreach (SqlColumn sc in tColumns)
            {
                if (bFirstTime)
                {
                    bFirstTime = false;
                    tNode.FirstNode.Text = sc.Name;
                }
                else
                    tNode.Nodes.Add(sc.Name);
            }

            tvTablesColumns.EndUpdate();
        }

        private void tbSQL_DragEnter(object sender, Majorsilence.Forms.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text))	// only accept text
                e.Effect = DragDropEffects.Copy;
        }

        private void tbSQL_DragDrop(object sender, Majorsilence.Forms.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text))
                tbSQL.SelectedText = (string)e.Data.GetData(DataFormats.Text);
        }

        private void tvTablesColumns_MouseDown(object sender, Majorsilence.Forms.MouseEventArgs e)
        {
            TreeNode node = tvTablesColumns.GetNodeAt(e.X, e.Y);
            if (node == null || node.Parent == null)
                return;

            string dragText;
            if (tbSQL.Text == "")
            {
                if (node.Parent.Parent == null)
                {	// select table; generate full select for table
                    tvTablesColumns_ExpandTable(node);	// make sure we've obtained the columns

                    dragText = "SELECT ";
                    TreeNode next = node.FirstNode;
                    while (true)
                    {
                        dragText += DesignerUtility.NormalizeSqlName(next.Text);
                        next = next.NextNode;
                        if (next == null)
                            break;
                        dragText += ", ";
                    }
                    dragText += (" FROM " + DesignerUtility.NormalizeSqlName(node.Text));
                }
                else
                {	// select column; generate select of that column	
                    dragText = "SELECT " + DesignerUtility.NormalizeSqlName(node.Text) + " FROM " + DesignerUtility.NormalizeSqlName(node.Parent.Text);
                }
            }
            else
                dragText = node.Text;

            tvTablesColumns.DoDragDrop(dragText, DragDropEffects.Copy);
        }


        private void DialogDatabase_Closed(object sender, Majorsilence.Forms.FormClosedEventArgs e)
        {
            if (_TempFileName != null)
                File.Delete(_TempFileName);
        }

        private void tbSQL_TextChanged(object sender, System.EventArgs e)
        {
            tbReportSyntax.Text = "";	// when SQL changes get rid of report syntax
            _ColumnList = null;			// get rid of any column list as well
            cbColumnList.Items.Clear();	// and clear out other places where columns show
            cbColumnList.Text = "";
            clbSubtotal.Items.Clear();
        }

        private void tbReportName_TextChanged(object sender, System.EventArgs e)
        {
            tbReportSyntax.Text = "";	// when SQL changes get rid of report syntax
        }

        private void tbReportDescription_TextChanged(object sender, System.EventArgs e)
        {
            tbReportSyntax.Text = "";	// when SQL changes get rid of report syntax
        }

        private void tbReportAuthor_TextChanged(object sender, System.EventArgs e)
        {
            tbReportSyntax.Text = "";	// when SQL changes get rid of report syntax
        }

        private void rbTable_CheckedChanged(object sender, System.EventArgs e)
        {
            tbReportSyntax.Text = "";	// when SQL changes get rid of report syntax

            if (rbTable.Checked)
            {
                TabularGroup.Enabled = true;
            }
            else
            {
                TabularGroup.Enabled = false;
            }
        }

        private void rbList_CheckedChanged(object sender, System.EventArgs e)
        {
            tbReportSyntax.Text = "";	// when SQL changes get rid of report syntax
        }

        private void rbMatrix_CheckedChanged(object sender, System.EventArgs e)
        {
            tbReportSyntax.Text = "";	// when SQL changes get rid of report syntax
        }

        private void rbChart_CheckedChanged(object sender, System.EventArgs e)
        {
            tbReportSyntax.Text = "";	// when SQL changes get rid of report syntax
        }

        //private void bAdd_Click(object sender, System.EventArgs e)
        //{
        //    ReportParm rp = new ReportParm("newparm");
        //    int cur = reportParameterCtl1.lbParameters.Items.Add(rp);
        //    reportParameterCtl1.lbParameters.SelectedIndex = cur;
        //    this.tbParmName.Focus();
        //}

        //private void bRemove_Click(object sender, System.EventArgs e)
        //{
        //    int cur = lbParameters.SelectedIndex;
        //    if (cur < 0)
        //        return;
        //    lbParameters.Items.RemoveAt(cur);
        //    if (lbParameters.Items.Count <= 0)
        //        return;
        //    cur--;
        //    if (cur < 0)
        //        cur = 0;
        //    lbParameters.SelectedIndex = cur;
        //}

        //private void lbParameters_SelectedIndexChanged(object sender, System.EventArgs e)
        //{
        //    int cur = lbParameters.SelectedIndex;
        //    if (cur < 0)
        //        return;

        //    ReportParm rp = lbParameters.Items[cur] as ReportParm;
        //    if (rp == null)
        //        return;

        //    tbParmName.Text = rp.Name;
        //    cbParmType.Text = rp.DataType;
        //    tbParmPrompt.Text = rp.Prompt;
        //    tbParmDefaultValue.Text = rp.DefaultValueDisplay;
        //    ckbParmAllowBlank.Checked = rp.AllowBlank;
        //    tbParmValidValues.Text = rp.ValidValuesDisplay;
        //    ckbParmAllowNull.Checked = rp.AllowNull;
        //}

        //private void lbParameters_MoveItem(int curloc, int newloc)
        //{
        //    ReportParm rp = lbParameters.Items[curloc] as ReportParm;
        //    if (rp == null)
        //        return;

        //    lbParameters.BeginUpdate();
        //    lbParameters.Items.RemoveAt(curloc);
        //    lbParameters.Items.Insert(newloc, rp);
        //    lbParameters.SelectedIndex = newloc;
        //    lbParameters.EndUpdate();
        //}

        //private void tbParmName_TextChanged(object sender, System.EventArgs e)
        //{
        //    int cur = lbParameters.SelectedIndex;
        //    if (cur < 0)
        //        return;

        //    ReportParm rp = lbParameters.Items[cur] as ReportParm;
        //    if (rp == null)
        //        return;

        //    if (rp.Name == tbParmName.Text)
        //        return;

        //    rp.Name = tbParmName.Text;
        //    // text doesn't change in listbox; force change by removing and re-adding item
        //    lbParameters_MoveItem(cur, cur);
        //}

        //private void cbParmType_SelectedIndexChanged(object sender, System.EventArgs e)
        //{
        //    int cur = lbParameters.SelectedIndex;
        //    if (cur < 0)
        //        return;

        //    ReportParm rp = lbParameters.Items[cur] as ReportParm;
        //    if (rp == null)
        //        return;

        //    rp.DataType = cbParmType.Text;
        //}

        //private void tbParmPrompt_TextChanged(object sender, System.EventArgs e)
        //{
        //    int cur = lbParameters.SelectedIndex;
        //    if (cur < 0)
        //        return;

        //    ReportParm rp = lbParameters.Items[cur] as ReportParm;
        //    if (rp == null)
        //        return;

        //    rp.Prompt = tbParmPrompt.Text;
        //}

        //private void ckbParmAllowNull_CheckedChanged(object sender, System.EventArgs e)
        //{
        //    int cur = lbParameters.SelectedIndex;
        //    if (cur < 0)
        //        return;

        //    ReportParm rp = lbParameters.Items[cur] as ReportParm;
        //    if (rp == null)
        //        return;

        //    rp.AllowNull = ckbParmAllowNull.Checked;
        //}

        //private void ckbParmAllowBlank_CheckedChanged(object sender, System.EventArgs e)
        //{
        //    int cur = lbParameters.SelectedIndex;
        //    if (cur < 0)
        //        return;

        //    ReportParm rp = lbParameters.Items[cur] as ReportParm;
        //    if (rp == null)
        //        return;

        //    rp.AllowBlank = ckbParmAllowBlank.Checked;
        //}

        //private void bParmUp_Click(object sender, System.EventArgs e)
        //{
        //    int cur = lbParameters.SelectedIndex;
        //    if (cur <= 0)
        //        return;

        //    lbParameters_MoveItem(cur, cur - 1);
        //}

        //private void bParmDown_Click(object sender, System.EventArgs e)
        //{
        //    int cur = lbParameters.SelectedIndex;
        //    if (cur + 1 >= lbParameters.Items.Count)
        //        return;

        //    lbParameters_MoveItem(cur, cur + 1);
        //}

        //private void tbParmDefaultValue_TextChanged(object sender, System.EventArgs e)
        //{
        //    int cur = lbParameters.SelectedIndex;
        //    if (cur < 0)
        //        return;

        //    ReportParm rp = lbParameters.Items[cur] as ReportParm;
        //    if (rp == null)
        //        return;

        //    if (tbParmDefaultValue.Text.Length > 0)
        //    {
        //        if (rp.DefaultValue == null)
        //            rp.DefaultValue = new List<string>();
        //        else
        //            rp.DefaultValue.Clear();
        //        rp.DefaultValue.Add(tbParmDefaultValue.Text);
        //    }
        //    else
        //        rp.DefaultValue = null;

        //}

        private void tbConnection_TextChanged(object sender, System.EventArgs e)
        {
            tvTablesColumns.Nodes.Clear();
        }

        private void emptyReportSyntax(object sender, System.EventArgs e)
        {
            tbReportSyntax.Text = "";		// need to generate another report
        }

        private void bMove_Click(object sender, System.EventArgs e)
        {
            if (tvTablesColumns.SelectedNode == null ||
                tvTablesColumns.SelectedNode.Parent == null)
                return;		// this is the Tables/Views node

            TreeNode node = tvTablesColumns.SelectedNode;
            string t = node.Text;
            if (tbSQL.Text == "")
            {
                if (node.Parent.Parent == null)
                {	// select table; generate full select for table
                    tvTablesColumns_ExpandTable(node);	// make sure we've obtained the columns

                    StringBuilder sb = new StringBuilder("SELECT ");
                    TreeNode next = node.FirstNode;
                    while (true)
                    {
                        sb.Append(DesignerUtility.NormalizeSqlName(next.Text));
                        next = next.NextNode;
                        if (next == null)
                            break;
                        sb.Append(", ");
                    }
                    sb.Append(" FROM ");
                    sb.Append(DesignerUtility.NormalizeSqlName(node.Text));
                    t = sb.ToString();
                }
                else
                {	// select column; generate select of that column	
                    t = "SELECT " + DesignerUtility.NormalizeSqlName(node.Text) + " FROM " + DesignerUtility.NormalizeSqlName(node.Parent.Text);
                }
            }

            tbSQL.SelectedText = t;
        }

        //private void bValidValues_Click(object sender, System.EventArgs e)
        //{
        //    int cur = lbParameters.SelectedIndex;
        //    if (cur < 0)
        //        return;

        //    ReportParm rp = lbParameters.Items[cur] as ReportParm;
        //    if (rp == null)
        //        return;

        //    DialogValidValues dvv = new DialogValidValues(rp.ValidValues);
        //    try
        //    {
        //        if (dvv.ShowDialog() != DialogResult.OK)
        //            return;
        //        rp.ValidValues = dvv.ValidValues;
        //        this.tbParmValidValues.Text = rp.ValidValuesDisplay;
        //    }
        //    finally
        //    {
        //        dvv.Dispose();
        //    }
        //}

        private void cbConnectionTypes_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            groupBoxSqlServer.Visible = false;
            buttonSqliteSelectDatabase.Visible = false;

            if (cbConnectionTypes.Text == SHARED_CONNECTION)
            {
                lConnection.Text = Strings.DialogDatabase_cbConnectionTypes_SelectedIndexChanged_Shared_Data_Source_File;
                bShared.Visible = true;
            }
            else
            {
                lConnection.Text = Strings.DialogDatabase_cbConnectionTypes_SelectedIndexChanged_Connection;
                bShared.Visible = false;
            }

            if (cbConnectionTypes.Text == "ODBC")
            {
                lODBC.Visible = cbOdbcNames.Visible = true;
                DesignerUtility.FillOdbcNames(cbOdbcNames);
            }
            else
            {
                lODBC.Visible = cbOdbcNames.Visible = false;
            }

            

            // this is only for ease of testing
            switch (cbConnectionTypes.Text)
            {
                case "SQL":
                    tbConnection.Text = "Server=(local)\\ServerInstance;DataBase=DatabaseName;User Id=myUsername;Password=myPassword;Connect Timeout=5";
                    groupBoxSqlServer.Visible = true;
                    break;
                case "ODBC":
                    tbConnection.Text = "dsn=world;UID=user;PWD=password;";
                    break;
                case "Oracle":
                    tbConnection.Text = "User Id=SYSTEM;Password=password;Data Source=server";
                    break;
                case "Firebird.NET":
                    tbConnection.Text = @"Dialect=3;User Id=SYSDBA;Database=daabaseFile.fdb;Data Source=localhost;Password=password";
                    break;
                case "MySQL.NET":
                    tbConnection.Text = "database=world;user id=user;password=password;";
                    break;
                case "iAnywhere.NET":
                    tbConnection.Text = "Data Source=ASA 9.0 Sample;UID=DBA;PWD=password";
                    break;
                case "SQLite":
                    tbConnection.Text = "Data Source=filename;Version=3;Password=myPassword;Pooling=True;Max Pool Size=100;";
                    buttonSqliteSelectDatabase.Visible = true;
                    break;
                case "PostgreSQL":
                case "PostgreSQL_Devart":
                    tbConnection.Text = "Server=127.0.0.1;Port=5432;Database=myDataBase;User Id=myUsername;Password=myPassword;";
                    break;
                case "Json":
                    tbConnection.Text = "file=/path/to/file/or/http/call";
                    break;
                default:
                    tbConnection.Text = "";
                    break;
            }

        }

        private void cbOdbcNames_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            string name = "dsn=" + cbOdbcNames.Text + ";";
            this.tbConnection.Text = name;
        }

        private void bTestConnection_Click(object sender, System.EventArgs e)
        {
            if (string.IsNullOrEmpty(tbConnection.Text))
            {
                MessageBox.Show(Strings.DialogDatabase_ShowD_SelectDataProvider, Strings.DesignerUtility_Show_TestConnection, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            string cType = GetDataProvider();
            if (cType == null)
                return;

            if (DesignerUtility.TestConnection(cType, GetDataConnection()))
                MessageBox.Show(Strings.DialogDatabase_Show_ConnectionSuccessful, Strings.DesignerUtility_Show_TestConnection);
        }

        private void DBConnection_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!DesignerUtility.TestConnection(this.GetDataConnection(), GetDataConnection()))
                e.Cancel = true;
        }

        private void bShared_Click(object sender, System.EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = Strings.DialogDatabase_bShared_Click_DSRFilter;
            ofd.FilterIndex = 1;
            if (tbConnection.Text.Length > 0)
                ofd.FileName = tbConnection.Text;
            else
                ofd.FileName = "*.dsr";

            ofd.Title = Strings.DialogDatabase_bShared_Click_DSRTitle;
            ofd.CheckFileExists = true;
            ofd.DefaultExt = "dsr";
            ofd.AddExtension = true;
            try
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                    tbConnection.Text = ofd.FileName;
            }
            finally
            {
                ofd.Dispose();
            }
        }

        private void buttonSqliteSelectDatabase_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            try
            {
                ofd.Filter = Strings.DialogDatabase_buttonSqliteSelectDatabase_Click_AllFilesFilter;
                ofd.CheckFileExists = true;
                
                try
                {
                    if (ofd.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    if (tbConnection.Text.Trim() == "")
                    {
                        tbConnection.Text = "Data Source=" + ofd.FileName;
                    }
                    else
                    {
                        string[] sections = tbConnection.Text.Split(';');

                        foreach (string section in sections)
                        {
                            if (section.ToLower().Contains("data source"))
                            {
                                string dataSource = string.Format("Data Source={0}", ofd.FileName);
                                tbConnection.Text = tbConnection.Text.Replace(section, dataSource);
                                break;
                            }
                        }
                    }
            
                }
                finally
                {
                    ofd.Dispose();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.DialogDatabase_ShowD_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonSearchSqlServers_Click(object sender, EventArgs e)
        {
            try
            {
#if NET6_0_OR_GREATER
                Microsoft.Data.Sql.SqlDataSourceEnumerator instance = Microsoft.Data.Sql.SqlDataSourceEnumerator.Instance;
#else
                System.Data.Sql.SqlDataSourceEnumerator instance = System.Data.Sql.SqlDataSourceEnumerator.Instance;
#endif
                DataTable dt;
                dt = instance.GetDataSources();

                Dictionary<string, string> dict = new Dictionary<string, string>();
                foreach (System.Data.DataRow row in dt.Rows)
                {
                    string server = row["ServerName"].ToString();
                    if (row["InstanceName"].ToString() != "")
                    {
                        server = server + @"\" + row["InstanceName"].ToString();
                    }
                    dict.Add(server, server);
                }
                comboServerList.ValueMember = "Value";
                comboServerList.DisplayMember = "Key";
                comboServerList.DataSource = new BindingSource(dict, null);
            }
            catch(Exception ex)
            {
				MessageBox.Show(ex.Message, Strings.DialogDatabase_ShowD_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonDatabaseSearch_Click(object sender, EventArgs e)
        {
            System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand();
            System.Data.SqlClient.SqlDataAdapter da;

            DataTable dt = new DataTable();

            string connectionString = string.Format("server={0}; Database={1};User ID={2}; Password={3}; Trusted_Connection=False;",
                comboServerList.SelectedValue.ToString(), "master", textBoxSqlUser.Text, textBoxSqlPassword.Text);

            SqlConnection cn = new System.Data.SqlClient.SqlConnection(connectionString); ;
            try
            {
                
                cn.Open();

                cmd.Connection = cn;
                cmd.CommandText = "sp_databases";
                cmd.CommandType = CommandType.StoredProcedure;

                da = new System.Data.SqlClient.SqlDataAdapter(cmd);
                da.Fill(dt);
                dt.TableName = "master";

            }
            catch (Exception ex)
            {
				MessageBox.Show(ex.Message, Strings.DialogDatabase_ShowD_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                cn.Close();
            }


            try
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
                foreach (System.Data.DataRow row in dt.Rows)
                {
                    string database = row["DATABASE_NAME"].ToString();
                    dict.Add(database, database);
                }

                comboDatabaseList.ValueMember = "Value";
                comboDatabaseList.DisplayMember = "Key";
                comboDatabaseList.DataSource = new BindingSource(dict, null);
            }
            catch (Exception ex)
            {
				MessageBox.Show(ex.Message, Strings.DialogDatabase_ShowD_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void tbSQL_KeyDown(object sender, KeyEventArgs e)
        {
             if (e.Control && e.KeyCode == Keys.A)
             {
                tbSQL.SelectAll();
             }

        }

        private void DialogDatabase_Load(object sender, EventArgs e)
        {

        }

        private void comboServerList_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (comboServerList.Items.Count == 0)
            {
                return;
            }

            string[] sections = tbConnection.Text.Split(';');

            foreach (string section in sections)
            {
                if (section.ToLower().Contains("server="))
                {
                    string dataSource = string.Format("server={0}", comboServerList.SelectedValue.ToString());
                    tbConnection.Text = tbConnection.Text.Replace(section, dataSource);
                    break;
                }
            }

        }

        private void comboDatabaseList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboDatabaseList.Items.Count == 0)
            {
                return;
            }

            string[] sections = tbConnection.Text.Split(';');

            foreach (string section in sections)
            {
                if (section.ToLower().Contains("database="))
                {
                    string dataSource = string.Format("database={0}", comboDatabaseList.SelectedValue.ToString());
                    tbConnection.Text = tbConnection.Text.Replace(section, dataSource);
                    break;
                }
            }

        }

        private void rbEmpty_CheckedChanged(object sender, EventArgs e)
        {
            tbReportSyntax.Text = "";	// when SQL changes get rid of report syntax
            TabularGroup.Enabled = !rbEmpty.Checked;
            DBConnection.Enabled = !rbEmpty.Checked;
            DBSql.Enabled= !rbEmpty.Checked;
        }
    }
}
