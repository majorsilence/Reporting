namespace Majorsilence.Reporting.RdlDesign
{
    public partial class DialogDatabase : Majorsilence.Forms.Form
	{
				

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
		{
            Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(DialogDatabase));
            this.splitContainer1 = new Majorsilence.Forms.SplitContainer();
            this.tvTablesColumns = new Majorsilence.Forms.TreeView();
            this.tbSQL = new Majorsilence.Forms.TextBox();
            this.bMove = new Majorsilence.Forms.Button();
            this.tcDialog = new Majorsilence.Forms.TabControl();
            this.ReportType = new Majorsilence.Forms.TabPage();
            this.groupBox2 = new Majorsilence.Forms.GroupBox();
            this.rbSchema2005 = new Majorsilence.Forms.RadioButton();
            this.rbSchema2003 = new Majorsilence.Forms.RadioButton();
            this.rbSchemaNo = new Majorsilence.Forms.RadioButton();
            this.cbOrientation = new Majorsilence.Forms.ComboBox();
            this.label6 = new Majorsilence.Forms.Label();
            this.tbReportAuthor = new Majorsilence.Forms.TextBox();
            this.tbReportDescription = new Majorsilence.Forms.TextBox();
            this.tbReportName = new Majorsilence.Forms.TextBox();
            this.label3 = new Majorsilence.Forms.Label();
            this.label2 = new Majorsilence.Forms.Label();
            this.label1 = new Majorsilence.Forms.Label();
            this.groupBox1 = new Majorsilence.Forms.GroupBox();
            this.rbChart = new Majorsilence.Forms.RadioButton();
            this.rbMatrix = new Majorsilence.Forms.RadioButton();
            this.rbList = new Majorsilence.Forms.RadioButton();
            this.rbTable = new Majorsilence.Forms.RadioButton();
            this.DBConnection = new Majorsilence.Forms.TabPage();
            this.groupBoxSqlServer = new Majorsilence.Forms.GroupBox();
            this.textBoxSqlPassword = new Majorsilence.Forms.TextBox();
            this.label11 = new Majorsilence.Forms.Label();
            this.textBoxSqlUser = new Majorsilence.Forms.TextBox();
            this.label10 = new Majorsilence.Forms.Label();
            this.label8 = new Majorsilence.Forms.Label();
            this.buttonDatabaseSearch = new Majorsilence.Forms.Button();
            this.comboServerList = new Majorsilence.Forms.ComboBox();
            this.label9 = new Majorsilence.Forms.Label();
            this.buttonSearchSqlServers = new Majorsilence.Forms.Button();
            this.comboDatabaseList = new Majorsilence.Forms.ComboBox();
            this.buttonSqliteSelectDatabase = new Majorsilence.Forms.Button();
            this.bShared = new Majorsilence.Forms.Button();
            this.bTestConnection = new Majorsilence.Forms.Button();
            this.cbOdbcNames = new Majorsilence.Forms.ComboBox();
            this.lODBC = new Majorsilence.Forms.Label();
            this.lConnection = new Majorsilence.Forms.Label();
            this.cbConnectionTypes = new Majorsilence.Forms.ComboBox();
            this.label7 = new Majorsilence.Forms.Label();
            this.tbConnection = new Majorsilence.Forms.TextBox();
            this.ReportParameters = new Majorsilence.Forms.TabPage();
            this.DBSql = new Majorsilence.Forms.TabPage();
            this.panel2 = new Majorsilence.Forms.Panel();
            this.TabularGroup = new Majorsilence.Forms.TabPage();
            this.clbSubtotal = new Majorsilence.Forms.CheckedListBox();
            this.ckbGrandTotal = new Majorsilence.Forms.CheckBox();
            this.label5 = new Majorsilence.Forms.Label();
            this.label4 = new Majorsilence.Forms.Label();
            this.cbColumnList = new Majorsilence.Forms.ComboBox();
            this.ReportSyntax = new Majorsilence.Forms.TabPage();
            this.tbReportSyntax = new Majorsilence.Forms.TextBox();
            this.ReportPreview = new Majorsilence.Forms.TabPage();
            this.rdlViewer1 = new Majorsilence.Reporting.RdlViewer.RdlViewer();
            this.btnCancel = new Majorsilence.Forms.Button();
            this.panel1 = new Majorsilence.Forms.Panel();
            this.btnOK = new Majorsilence.Forms.Button();
            this.rbEmpty = new Majorsilence.Forms.RadioButton();
            this.reportParameterCtl1 = new Majorsilence.Reporting.RdlDesign.ReportParameterCtl();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tcDialog.SuspendLayout();
            this.ReportType.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.DBConnection.SuspendLayout();
            this.groupBoxSqlServer.SuspendLayout();
            this.ReportParameters.SuspendLayout();
            this.DBSql.SuspendLayout();
            this.panel2.SuspendLayout();
            this.TabularGroup.SuspendLayout();
            this.ReportSyntax.SuspendLayout();
            this.ReportPreview.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            resources.ApplyResources(this.splitContainer1, "splitContainer1");
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.tvTablesColumns);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tbSQL);
            this.splitContainer1.Panel2.Controls.Add(this.bMove);
            // 
            // tvTablesColumns
            // 
            resources.ApplyResources(this.tvTablesColumns, "tvTablesColumns");
            this.tvTablesColumns.FullRowSelect = true;
            this.tvTablesColumns.Name = "tvTablesColumns";
            this.tvTablesColumns.BeforeExpand += this.tvTablesColumns_BeforeExpand;
            // 
            // tbSQL
            // 
            this.tbSQL.AllowDrop = true;
            resources.ApplyResources(this.tbSQL, "tbSQL");
            this.tbSQL.Name = "tbSQL";
            this.tbSQL.TextChanged += this.tbSQL_TextChanged;
            this.tbSQL.KeyDown += this.tbSQL_KeyDown;
            // 
            // bMove
            // 
            resources.ApplyResources(this.bMove, "bMove");
            this.bMove.Name = "bMove";
            this.bMove.Click += this.bMove_Click;
            // 
            // tcDialog
            // 
            this.tcDialog.Controls.Add(this.ReportType);
            this.tcDialog.Controls.Add(this.DBConnection);
            this.tcDialog.Controls.Add(this.ReportParameters);
            this.tcDialog.Controls.Add(this.DBSql);
            this.tcDialog.Controls.Add(this.TabularGroup);
            this.tcDialog.Controls.Add(this.ReportSyntax);
            this.tcDialog.Controls.Add(this.ReportPreview);
            resources.ApplyResources(this.tcDialog, "tcDialog");
            this.tcDialog.Name = "tcDialog";
            this.tcDialog.SelectedIndex = 0;
            this.tcDialog.SelectedIndexChanged += this.tabControl1_SelectedIndexChanged;
            // 
            // ReportType
            // 
            this.ReportType.Controls.Add(this.groupBox2);
            this.ReportType.Controls.Add(this.cbOrientation);
            this.ReportType.Controls.Add(this.label6);
            this.ReportType.Controls.Add(this.tbReportAuthor);
            this.ReportType.Controls.Add(this.tbReportDescription);
            this.ReportType.Controls.Add(this.tbReportName);
            this.ReportType.Controls.Add(this.label3);
            this.ReportType.Controls.Add(this.label2);
            this.ReportType.Controls.Add(this.label1);
            this.ReportType.Controls.Add(this.groupBox1);
            resources.ApplyResources(this.ReportType, "ReportType");
            this.ReportType.Name = "ReportType";
            this.ReportType.Tag = "type";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.rbSchema2005);
            this.groupBox2.Controls.Add(this.rbSchema2003);
            this.groupBox2.Controls.Add(this.rbSchemaNo);
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // rbSchema2005
            // 
            this.rbSchema2005.Checked = true;
            resources.ApplyResources(this.rbSchema2005, "rbSchema2005");
            this.rbSchema2005.Name = "rbSchema2005";
            this.rbSchema2005.TabStop = true;
            // 
            // rbSchema2003
            // 
            resources.ApplyResources(this.rbSchema2003, "rbSchema2003");
            this.rbSchema2003.Name = "rbSchema2003";
            // 
            // rbSchemaNo
            // 
            resources.ApplyResources(this.rbSchemaNo, "rbSchemaNo");
            this.rbSchemaNo.Name = "rbSchemaNo";
            // 
            // cbOrientation
            // 
            this.cbOrientation.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
            this.cbOrientation.Items.AddRange(new object[] {
            resources.GetString("cbOrientation.Items"),
            resources.GetString("cbOrientation.Items1")});
            resources.ApplyResources(this.cbOrientation, "cbOrientation");
            this.cbOrientation.Name = "cbOrientation";
            this.cbOrientation.SelectedIndexChanged += this.emptyReportSyntax;
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // tbReportAuthor
            // 
            resources.ApplyResources(this.tbReportAuthor, "tbReportAuthor");
            this.tbReportAuthor.Name = "tbReportAuthor";
            this.tbReportAuthor.TextChanged += this.tbReportAuthor_TextChanged;
            // 
            // tbReportDescription
            // 
            resources.ApplyResources(this.tbReportDescription, "tbReportDescription");
            this.tbReportDescription.Name = "tbReportDescription";
            this.tbReportDescription.TextChanged += this.tbReportDescription_TextChanged;
            // 
            // tbReportName
            // 
            resources.ApplyResources(this.tbReportName, "tbReportName");
            this.tbReportName.Name = "tbReportName";
            this.tbReportName.TextChanged += this.tbReportName_TextChanged;
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rbEmpty);
            this.groupBox1.Controls.Add(this.rbChart);
            this.groupBox1.Controls.Add(this.rbMatrix);
            this.groupBox1.Controls.Add(this.rbList);
            this.groupBox1.Controls.Add(this.rbTable);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // rbChart
            // 
            resources.ApplyResources(this.rbChart, "rbChart");
            this.rbChart.Name = "rbChart";
            this.rbChart.CheckedChanged += this.rbChart_CheckedChanged;
            // 
            // rbMatrix
            // 
            resources.ApplyResources(this.rbMatrix, "rbMatrix");
            this.rbMatrix.Name = "rbMatrix";
            this.rbMatrix.CheckedChanged += this.rbMatrix_CheckedChanged;
            // 
            // rbList
            // 
            resources.ApplyResources(this.rbList, "rbList");
            this.rbList.Name = "rbList";
            this.rbList.CheckedChanged += this.rbList_CheckedChanged;
            // 
            // rbTable
            // 
            this.rbTable.Checked = true;
            resources.ApplyResources(this.rbTable, "rbTable");
            this.rbTable.Name = "rbTable";
            this.rbTable.TabStop = true;
            this.rbTable.CheckedChanged += this.rbTable_CheckedChanged;
            // 
            // DBConnection
            // 
            this.DBConnection.CausesValidation = false;
            this.DBConnection.Controls.Add(this.groupBoxSqlServer);
            this.DBConnection.Controls.Add(this.buttonSqliteSelectDatabase);
            this.DBConnection.Controls.Add(this.bShared);
            this.DBConnection.Controls.Add(this.bTestConnection);
            this.DBConnection.Controls.Add(this.cbOdbcNames);
            this.DBConnection.Controls.Add(this.lODBC);
            this.DBConnection.Controls.Add(this.lConnection);
            this.DBConnection.Controls.Add(this.cbConnectionTypes);
            this.DBConnection.Controls.Add(this.label7);
            this.DBConnection.Controls.Add(this.tbConnection);
            resources.ApplyResources(this.DBConnection, "DBConnection");
            this.DBConnection.Name = "DBConnection";
            this.DBConnection.Tag = "connect";
            this.DBConnection.Validating += this.DBConnection_Validating;
            // 
            // groupBoxSqlServer
            // 
            this.groupBoxSqlServer.Controls.Add(this.textBoxSqlPassword);
            this.groupBoxSqlServer.Controls.Add(this.label11);
            this.groupBoxSqlServer.Controls.Add(this.textBoxSqlUser);
            this.groupBoxSqlServer.Controls.Add(this.label10);
            this.groupBoxSqlServer.Controls.Add(this.label8);
            this.groupBoxSqlServer.Controls.Add(this.buttonDatabaseSearch);
            this.groupBoxSqlServer.Controls.Add(this.comboServerList);
            this.groupBoxSqlServer.Controls.Add(this.label9);
            this.groupBoxSqlServer.Controls.Add(this.buttonSearchSqlServers);
            this.groupBoxSqlServer.Controls.Add(this.comboDatabaseList);
            resources.ApplyResources(this.groupBoxSqlServer, "groupBoxSqlServer");
            this.groupBoxSqlServer.Name = "groupBoxSqlServer";
            this.groupBoxSqlServer.TabStop = false;
            // 
            // textBoxSqlPassword
            // 
            resources.ApplyResources(this.textBoxSqlPassword, "textBoxSqlPassword");
            this.textBoxSqlPassword.Name = "textBoxSqlPassword";
            // 
            // label11
            // 
            resources.ApplyResources(this.label11, "label11");
            this.label11.Name = "label11";
            // 
            // textBoxSqlUser
            // 
            resources.ApplyResources(this.textBoxSqlUser, "textBoxSqlUser");
            this.textBoxSqlUser.Name = "textBoxSqlUser";
            // 
            // label10
            // 
            resources.ApplyResources(this.label10, "label10");
            this.label10.Name = "label10";
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            // 
            // buttonDatabaseSearch
            // 
            resources.ApplyResources(this.buttonDatabaseSearch, "buttonDatabaseSearch");
            this.buttonDatabaseSearch.Name = "buttonDatabaseSearch";
            this.buttonDatabaseSearch.UseVisualStyleBackColor = true;
            this.buttonDatabaseSearch.Click += this.buttonDatabaseSearch_Click;
            // 
            // comboServerList
            // 
            this.comboServerList.FormattingEnabled = true;
            resources.ApplyResources(this.comboServerList, "comboServerList");
            this.comboServerList.Name = "comboServerList";
            // 
            // label9
            // 
            resources.ApplyResources(this.label9, "label9");
            this.label9.Name = "label9";
            // 
            // buttonSearchSqlServers
            // 
            resources.ApplyResources(this.buttonSearchSqlServers, "buttonSearchSqlServers");
            this.buttonSearchSqlServers.Name = "buttonSearchSqlServers";
            this.buttonSearchSqlServers.UseVisualStyleBackColor = true;
            this.buttonSearchSqlServers.Click += this.buttonSearchSqlServers_Click;
            // 
            // comboDatabaseList
            // 
            this.comboDatabaseList.FormattingEnabled = true;
            resources.ApplyResources(this.comboDatabaseList, "comboDatabaseList");
            this.comboDatabaseList.Name = "comboDatabaseList";
            // 
            // buttonSqliteSelectDatabase
            // 
            resources.ApplyResources(this.buttonSqliteSelectDatabase, "buttonSqliteSelectDatabase");
            this.buttonSqliteSelectDatabase.Name = "buttonSqliteSelectDatabase";
            this.buttonSqliteSelectDatabase.UseVisualStyleBackColor = true;
            this.buttonSqliteSelectDatabase.Click += this.buttonSqliteSelectDatabase_Click;
            // 
            // bShared
            // 
            resources.ApplyResources(this.bShared, "bShared");
            this.bShared.Name = "bShared";
            this.bShared.Click += this.bShared_Click;
            // 
            // bTestConnection
            // 
            resources.ApplyResources(this.bTestConnection, "bTestConnection");
            this.bTestConnection.Name = "bTestConnection";
            this.bTestConnection.Click += this.bTestConnection_Click;
            // 
            // cbOdbcNames
            // 
            this.cbOdbcNames.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.cbOdbcNames, "cbOdbcNames");
            this.cbOdbcNames.Name = "cbOdbcNames";
            this.cbOdbcNames.Sorted = true;
            this.cbOdbcNames.SelectedIndexChanged += this.cbOdbcNames_SelectedIndexChanged;
            // 
            // lODBC
            // 
            resources.ApplyResources(this.lODBC, "lODBC");
            this.lODBC.Name = "lODBC";
            // 
            // lConnection
            // 
            resources.ApplyResources(this.lConnection, "lConnection");
            this.lConnection.Name = "lConnection";
            // 
            // cbConnectionTypes
            // 
            this.cbConnectionTypes.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.cbConnectionTypes, "cbConnectionTypes");
            this.cbConnectionTypes.Name = "cbConnectionTypes";
            this.cbConnectionTypes.SelectedIndexChanged += this.cbConnectionTypes_SelectedIndexChanged;
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // tbConnection
            // 
            resources.ApplyResources(this.tbConnection, "tbConnection");
            this.tbConnection.Name = "tbConnection";
            this.tbConnection.TextChanged += this.tbConnection_TextChanged;
            // 
            // ReportParameters
            // 
            this.ReportParameters.Controls.Add(this.reportParameterCtl1);
            resources.ApplyResources(this.ReportParameters, "ReportParameters");
            this.ReportParameters.Name = "ReportParameters";
            this.ReportParameters.Tag = "parameters";
            // 
            // DBSql
            // 
            this.DBSql.Controls.Add(this.panel2);
            resources.ApplyResources(this.DBSql, "DBSql");
            this.DBSql.Name = "DBSql";
            this.DBSql.Tag = "sql";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.splitContainer1);
            resources.ApplyResources(this.panel2, "panel2");
            this.panel2.Name = "panel2";
            // 
            // TabularGroup
            // 
            this.TabularGroup.Controls.Add(this.clbSubtotal);
            this.TabularGroup.Controls.Add(this.ckbGrandTotal);
            this.TabularGroup.Controls.Add(this.label5);
            this.TabularGroup.Controls.Add(this.label4);
            this.TabularGroup.Controls.Add(this.cbColumnList);
            resources.ApplyResources(this.TabularGroup, "TabularGroup");
            this.TabularGroup.Name = "TabularGroup";
            this.TabularGroup.Tag = "group";
            // 
            // clbSubtotal
            // 
            this.clbSubtotal.CheckOnClick = true;
            resources.ApplyResources(this.clbSubtotal, "clbSubtotal");
            this.clbSubtotal.Name = "clbSubtotal";
            this.clbSubtotal.SelectedIndexChanged += this.emptyReportSyntax;
            // 
            // ckbGrandTotal
            // 
            resources.ApplyResources(this.ckbGrandTotal, "ckbGrandTotal");
            this.ckbGrandTotal.Name = "ckbGrandTotal";
            this.ckbGrandTotal.CheckedChanged += this.emptyReportSyntax;
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // cbColumnList
            // 
            this.cbColumnList.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.cbColumnList, "cbColumnList");
            this.cbColumnList.Name = "cbColumnList";
            this.cbColumnList.SelectedIndexChanged += this.emptyReportSyntax;
            // 
            // ReportSyntax
            // 
            this.ReportSyntax.Controls.Add(this.tbReportSyntax);
            resources.ApplyResources(this.ReportSyntax, "ReportSyntax");
            this.ReportSyntax.Name = "ReportSyntax";
            this.ReportSyntax.Tag = "syntax";
            // 
            // tbReportSyntax
            // 
            resources.ApplyResources(this.tbReportSyntax, "tbReportSyntax");
            this.tbReportSyntax.Name = "tbReportSyntax";
            this.tbReportSyntax.ReadOnly = true;
            // 
            // ReportPreview
            // 
            this.ReportPreview.Controls.Add(this.rdlViewer1);
            resources.ApplyResources(this.ReportPreview, "ReportPreview");
            this.ReportPreview.Name = "ReportPreview";
            this.ReportPreview.Tag = "preview";
            // 
            // rdlViewer1
            // 
            this.rdlViewer1.Cursor = Majorsilence.Forms.Cursors.Default;
            resources.ApplyResources(this.rdlViewer1, "rdlViewer1");
            this.rdlViewer1.dSubReportGetContent = null;
            this.rdlViewer1.Folder = null;
            this.rdlViewer1.HighlightAll = false;
            this.rdlViewer1.HighlightAllColor = System.Drawing.Color.Fuchsia;
            this.rdlViewer1.HighlightCaseSensitive = false;
            this.rdlViewer1.HighlightItemColor = System.Drawing.Color.Aqua;
            this.rdlViewer1.HighlightPageItem = null;
            this.rdlViewer1.HighlightText = null;
            this.rdlViewer1.Name = "rdlViewer1";
            this.rdlViewer1.PageCurrent = 1;
            this.rdlViewer1.Parameters = "";
            this.rdlViewer1.ReportName = null;
            this.rdlViewer1.ScrollMode = Majorsilence.Reporting.RdlViewer.ScrollModeEnum.Continuous;
            this.rdlViewer1.SelectTool = false;
            this.rdlViewer1.ShowFindPanel = false;
            this.rdlViewer1.ShowParameterPanel = true;
            this.rdlViewer1.ShowWaitDialog = true;
            this.rdlViewer1.UseTrueMargins = true;
            this.rdlViewer1.Zoom = 0.7061753F;
            this.rdlViewer1.ZoomMode = Majorsilence.Reporting.RdlViewer.ZoomEnum.FitWidth;
            // 
            // btnCancel
            // 
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.CausesValidation = false;
            this.btnCancel.DialogResult = Majorsilence.Forms.DialogResult.Cancel;
            this.btnCancel.Name = "btnCancel";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnOK);
            this.panel1.Controls.Add(this.btnCancel);
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.Name = "panel1";
            // 
            // btnOK
            // 
            resources.ApplyResources(this.btnOK, "btnOK");
            this.btnOK.Name = "btnOK";
            this.btnOK.Click += this.btnOK_Click;
            // 
            // rbEmpty
            // 
            resources.ApplyResources(this.rbEmpty, "rbEmpty");
            this.rbEmpty.Name = "rbEmpty";
            this.rbEmpty.CheckedChanged += this.rbEmpty_CheckedChanged;
            // 
            // reportParameterCtl1
            // 
            resources.ApplyResources(this.reportParameterCtl1, "reportParameterCtl1");
            this.reportParameterCtl1.Name = "reportParameterCtl1";
            // 
            // DialogDatabase
            // 
            this.AcceptButton = this.btnOK;
            resources.ApplyResources(this, "$this");
            this.CancelButton = this.btnCancel;
            this.Controls.Add(this.tcDialog);
            this.Controls.Add(this.panel1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DialogDatabase";
            this.ShowInTaskbar = false;
            this.FormClosed += this.DialogDatabase_Closed;
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.tcDialog.ResumeLayout(false);
            this.ReportType.ResumeLayout(false);
            this.ReportType.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.DBConnection.ResumeLayout(false);
            this.DBConnection.PerformLayout();
            this.groupBoxSqlServer.ResumeLayout(false);
            this.groupBoxSqlServer.PerformLayout();
            this.ReportParameters.ResumeLayout(false);
            this.DBSql.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.TabularGroup.ResumeLayout(false);
            this.ReportSyntax.ResumeLayout(false);
            this.ReportSyntax.PerformLayout();
            this.ReportPreview.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

		}
		#endregion		


        private Majorsilence.Forms.Button btnCancel;
        private Majorsilence.Forms.Panel panel1;
        private Majorsilence.Forms.Button btnOK;
        private Majorsilence.Forms.TabPage DBConnection;
        private Majorsilence.Forms.TabPage DBSql;
        private Majorsilence.Forms.TabPage ReportType;
        private System.ComponentModel.Container components = null;
        private Majorsilence.Forms.GroupBox groupBox1;
        private Majorsilence.Forms.RadioButton rbTable;
        private Majorsilence.Forms.RadioButton rbList;
        private Majorsilence.Forms.RadioButton rbMatrix;
        private Majorsilence.Forms.RadioButton rbChart;
        private Majorsilence.Forms.TextBox tbConnection;
        private Majorsilence.Forms.TabPage ReportSyntax;
        private Majorsilence.Forms.TextBox tbReportSyntax;
        private Majorsilence.Forms.TabPage ReportPreview;
        private Majorsilence.Forms.Label label1;
        private Majorsilence.Forms.Label label2;
        private Majorsilence.Forms.Label label3;
        private Majorsilence.Forms.TextBox tbReportName;
        private Majorsilence.Forms.TextBox tbReportDescription;
        private Majorsilence.Forms.TextBox tbReportAuthor;
        private Majorsilence.Forms.Panel panel2;
        private Majorsilence.Reporting.RdlViewer.RdlViewer rdlViewer1;
        private Majorsilence.Forms.TabPage ReportParameters;
        private Majorsilence.Forms.TabControl tcDialog;
        private Majorsilence.Forms.TabPage TabularGroup;
        private Majorsilence.Forms.ComboBox cbColumnList;
        private Majorsilence.Forms.Label label4;
        private Majorsilence.Forms.Label label5;
        private Majorsilence.Forms.CheckBox ckbGrandTotal;
        private Majorsilence.Forms.CheckedListBox clbSubtotal;
        private Majorsilence.Forms.Label label6;
        private Majorsilence.Forms.ComboBox cbOrientation;
        private Majorsilence.Forms.Label label7;
        private Majorsilence.Forms.ComboBox cbConnectionTypes;
        private Majorsilence.Forms.Label lODBC;
        private Majorsilence.Forms.ComboBox cbOdbcNames;
        private Majorsilence.Forms.Button bTestConnection;
        private Majorsilence.Forms.Label lConnection;
        private Majorsilence.Forms.Button bShared;
        private Majorsilence.Forms.GroupBox groupBox2;
        private Majorsilence.Forms.RadioButton rbSchemaNo;
        private Majorsilence.Forms.RadioButton rbSchema2003;
        private Majorsilence.Forms.RadioButton rbSchema2005;
        private Majorsilence.Forms.SplitContainer splitContainer1;
        private Majorsilence.Forms.TreeView tvTablesColumns;
        private Majorsilence.Forms.Button bMove;
        private Majorsilence.Forms.TextBox tbSQL;
        private Majorsilence.Forms.Button buttonSqliteSelectDatabase;
        internal Majorsilence.Forms.Button buttonSearchSqlServers;
        internal Majorsilence.Forms.ComboBox comboServerList;
        internal Majorsilence.Forms.Label label8;
        internal Majorsilence.Forms.Button buttonDatabaseSearch;
        internal Majorsilence.Forms.Label label9;
        internal Majorsilence.Forms.ComboBox comboDatabaseList;
        private Majorsilence.Forms.GroupBox groupBoxSqlServer;
        private Majorsilence.Forms.Label label10;
        private Majorsilence.Forms.TextBox textBoxSqlPassword;
        private Majorsilence.Forms.Label label11;
        private Majorsilence.Forms.TextBox textBoxSqlUser;
        private Majorsilence.Reporting.RdlDesign.ReportParameterCtl reportParameterCtl1;
        private Majorsilence.Forms.RadioButton rbEmpty;
    }
}
