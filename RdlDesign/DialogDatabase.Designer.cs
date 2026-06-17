namespace Majorsilence.Reporting.RdlDesign
{
    public partial class DialogDatabase : System.Windows.Forms.Form
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DialogDatabase));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tvTablesColumns = new System.Windows.Forms.TreeView();
            this.tbSQL = new System.Windows.Forms.TextBox();
            this.bMove = new System.Windows.Forms.Button();
            this.tcDialog = new System.Windows.Forms.TabControl();
            this.ReportType = new System.Windows.Forms.TabPage();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.rbSchema2005 = new System.Windows.Forms.RadioButton();
            this.rbSchema2003 = new System.Windows.Forms.RadioButton();
            this.rbSchemaNo = new System.Windows.Forms.RadioButton();
            this.cbOrientation = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.tbReportAuthor = new System.Windows.Forms.TextBox();
            this.tbReportDescription = new System.Windows.Forms.TextBox();
            this.tbReportName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rbChart = new System.Windows.Forms.RadioButton();
            this.rbMatrix = new System.Windows.Forms.RadioButton();
            this.rbList = new System.Windows.Forms.RadioButton();
            this.rbTable = new System.Windows.Forms.RadioButton();
            this.DBConnection = new System.Windows.Forms.TabPage();
            this.groupBoxSqlServer = new System.Windows.Forms.GroupBox();
            this.textBoxSqlPassword = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.textBoxSqlUser = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.buttonDatabaseSearch = new System.Windows.Forms.Button();
            this.comboServerList = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this.buttonSearchSqlServers = new System.Windows.Forms.Button();
            this.comboDatabaseList = new System.Windows.Forms.ComboBox();
            this.buttonSqliteSelectDatabase = new System.Windows.Forms.Button();
            this.bShared = new System.Windows.Forms.Button();
            this.bTestConnection = new System.Windows.Forms.Button();
            this.cbOdbcNames = new System.Windows.Forms.ComboBox();
            this.lODBC = new System.Windows.Forms.Label();
            this.lConnection = new System.Windows.Forms.Label();
            this.cbConnectionTypes = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.tbConnection = new System.Windows.Forms.TextBox();
            this.ReportParameters = new System.Windows.Forms.TabPage();
            this.DBSql = new System.Windows.Forms.TabPage();
            this.panel2 = new System.Windows.Forms.Panel();
            this.TabularGroup = new System.Windows.Forms.TabPage();
            this.clbSubtotal = new System.Windows.Forms.CheckedListBox();
            this.ckbGrandTotal = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.cbColumnList = new System.Windows.Forms.ComboBox();
            this.ReportSyntax = new System.Windows.Forms.TabPage();
            this.tbReportSyntax = new System.Windows.Forms.TextBox();
            this.ReportPreview = new System.Windows.Forms.TabPage();
            this.rdlViewer1 = new Majorsilence.Reporting.RdlViewer.RdlViewer();
            this.btnCancel = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnOK = new System.Windows.Forms.Button();
            this.rbEmpty = new System.Windows.Forms.RadioButton();
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
            this.tvTablesColumns.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.tvTablesColumns_BeforeExpand);
            // 
            // tbSQL
            // 
            this.tbSQL.AllowDrop = true;
            resources.ApplyResources(this.tbSQL, "tbSQL");
            this.tbSQL.Name = "tbSQL";
            this.tbSQL.TextChanged += new System.EventHandler(this.tbSQL_TextChanged);
            this.tbSQL.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbSQL_KeyDown);
            // 
            // bMove
            // 
            resources.ApplyResources(this.bMove, "bMove");
            this.bMove.Name = "bMove";
            this.bMove.Click += new System.EventHandler(this.bMove_Click);
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
            this.tcDialog.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
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
            this.cbOrientation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbOrientation.Items.AddRange(new object[] {
            resources.GetString("cbOrientation.Items"),
            resources.GetString("cbOrientation.Items1")});
            resources.ApplyResources(this.cbOrientation, "cbOrientation");
            this.cbOrientation.Name = "cbOrientation";
            this.cbOrientation.SelectedIndexChanged += new System.EventHandler(this.emptyReportSyntax);
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
            this.tbReportAuthor.TextChanged += new System.EventHandler(this.tbReportAuthor_TextChanged);
            // 
            // tbReportDescription
            // 
            resources.ApplyResources(this.tbReportDescription, "tbReportDescription");
            this.tbReportDescription.Name = "tbReportDescription";
            this.tbReportDescription.TextChanged += new System.EventHandler(this.tbReportDescription_TextChanged);
            // 
            // tbReportName
            // 
            resources.ApplyResources(this.tbReportName, "tbReportName");
            this.tbReportName.Name = "tbReportName";
            this.tbReportName.TextChanged += new System.EventHandler(this.tbReportName_TextChanged);
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
            this.rbChart.CheckedChanged += new System.EventHandler(this.rbChart_CheckedChanged);
            // 
            // rbMatrix
            // 
            resources.ApplyResources(this.rbMatrix, "rbMatrix");
            this.rbMatrix.Name = "rbMatrix";
            this.rbMatrix.CheckedChanged += new System.EventHandler(this.rbMatrix_CheckedChanged);
            // 
            // rbList
            // 
            resources.ApplyResources(this.rbList, "rbList");
            this.rbList.Name = "rbList";
            this.rbList.CheckedChanged += new System.EventHandler(this.rbList_CheckedChanged);
            // 
            // rbTable
            // 
            this.rbTable.Checked = true;
            resources.ApplyResources(this.rbTable, "rbTable");
            this.rbTable.Name = "rbTable";
            this.rbTable.TabStop = true;
            this.rbTable.CheckedChanged += new System.EventHandler(this.rbTable_CheckedChanged);
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
            this.DBConnection.Validating += new System.ComponentModel.CancelEventHandler(this.DBConnection_Validating);
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
            this.buttonDatabaseSearch.Click += new System.EventHandler(this.buttonDatabaseSearch_Click);
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
            this.buttonSearchSqlServers.Click += new System.EventHandler(this.buttonSearchSqlServers_Click);
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
            this.buttonSqliteSelectDatabase.Click += new System.EventHandler(this.buttonSqliteSelectDatabase_Click);
            // 
            // bShared
            // 
            resources.ApplyResources(this.bShared, "bShared");
            this.bShared.Name = "bShared";
            this.bShared.Click += new System.EventHandler(this.bShared_Click);
            // 
            // bTestConnection
            // 
            resources.ApplyResources(this.bTestConnection, "bTestConnection");
            this.bTestConnection.Name = "bTestConnection";
            this.bTestConnection.Click += new System.EventHandler(this.bTestConnection_Click);
            // 
            // cbOdbcNames
            // 
            this.cbOdbcNames.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.cbOdbcNames, "cbOdbcNames");
            this.cbOdbcNames.Name = "cbOdbcNames";
            this.cbOdbcNames.Sorted = true;
            this.cbOdbcNames.SelectedIndexChanged += new System.EventHandler(this.cbOdbcNames_SelectedIndexChanged);
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
            this.cbConnectionTypes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.cbConnectionTypes, "cbConnectionTypes");
            this.cbConnectionTypes.Name = "cbConnectionTypes";
            this.cbConnectionTypes.SelectedIndexChanged += new System.EventHandler(this.cbConnectionTypes_SelectedIndexChanged);
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
            this.tbConnection.TextChanged += new System.EventHandler(this.tbConnection_TextChanged);
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
            this.clbSubtotal.SelectedIndexChanged += new System.EventHandler(this.emptyReportSyntax);
            // 
            // ckbGrandTotal
            // 
            resources.ApplyResources(this.ckbGrandTotal, "ckbGrandTotal");
            this.ckbGrandTotal.Name = "ckbGrandTotal";
            this.ckbGrandTotal.CheckedChanged += new System.EventHandler(this.emptyReportSyntax);
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
            this.cbColumnList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.cbColumnList, "cbColumnList");
            this.cbColumnList.Name = "cbColumnList";
            this.cbColumnList.SelectedIndexChanged += new System.EventHandler(this.emptyReportSyntax);
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
            this.rdlViewer1.Cursor = System.Windows.Forms.Cursors.Default;
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
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
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
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // rbEmpty
            // 
            resources.ApplyResources(this.rbEmpty, "rbEmpty");
            this.rbEmpty.Name = "rbEmpty";
            this.rbEmpty.CheckedChanged += new System.EventHandler(this.rbEmpty_CheckedChanged);
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
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.DialogDatabase_Closed);
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


        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.TabPage DBConnection;
        private System.Windows.Forms.TabPage DBSql;
        private System.Windows.Forms.TabPage ReportType;
        private System.ComponentModel.Container components = null;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rbTable;
        private System.Windows.Forms.RadioButton rbList;
        private System.Windows.Forms.RadioButton rbMatrix;
        private System.Windows.Forms.RadioButton rbChart;
        private System.Windows.Forms.TextBox tbConnection;
        private System.Windows.Forms.TabPage ReportSyntax;
        private System.Windows.Forms.TextBox tbReportSyntax;
        private System.Windows.Forms.TabPage ReportPreview;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbReportName;
        private System.Windows.Forms.TextBox tbReportDescription;
        private System.Windows.Forms.TextBox tbReportAuthor;
        private System.Windows.Forms.Panel panel2;
        private Majorsilence.Reporting.RdlViewer.RdlViewer rdlViewer1;
        private System.Windows.Forms.TabPage ReportParameters;
        private System.Windows.Forms.TabControl tcDialog;
        private System.Windows.Forms.TabPage TabularGroup;
        private System.Windows.Forms.ComboBox cbColumnList;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckBox ckbGrandTotal;
        private System.Windows.Forms.CheckedListBox clbSubtotal;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cbOrientation;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cbConnectionTypes;
        private System.Windows.Forms.Label lODBC;
        private System.Windows.Forms.ComboBox cbOdbcNames;
        private System.Windows.Forms.Button bTestConnection;
        private System.Windows.Forms.Label lConnection;
        private System.Windows.Forms.Button bShared;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton rbSchemaNo;
        private System.Windows.Forms.RadioButton rbSchema2003;
        private System.Windows.Forms.RadioButton rbSchema2005;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TreeView tvTablesColumns;
        private System.Windows.Forms.Button bMove;
        private System.Windows.Forms.TextBox tbSQL;
        private System.Windows.Forms.Button buttonSqliteSelectDatabase;
        internal System.Windows.Forms.Button buttonSearchSqlServers;
        internal System.Windows.Forms.ComboBox comboServerList;
        internal System.Windows.Forms.Label label8;
        internal System.Windows.Forms.Button buttonDatabaseSearch;
        internal System.Windows.Forms.Label label9;
        internal System.Windows.Forms.ComboBox comboDatabaseList;
        private System.Windows.Forms.GroupBox groupBoxSqlServer;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox textBoxSqlPassword;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox textBoxSqlUser;
        private Majorsilence.Reporting.RdlDesign.ReportParameterCtl reportParameterCtl1;
        private System.Windows.Forms.RadioButton rbEmpty;
    }
}
