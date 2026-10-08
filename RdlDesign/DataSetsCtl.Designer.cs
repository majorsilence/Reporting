namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// Summary description for StyleCtl.
    /// </summary>
    partial class DataSetsCtl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
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

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(DataSetsCtl));
            Majorsilence.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new Majorsilence.Forms.DataGridViewCellStyle();
            this.DoubleBuffered = true;
            this.splitContainer1 = new Majorsilence.Forms.SplitContainer();
            this.panel2 = new Majorsilence.Forms.Panel();
            this.panel4 = new Majorsilence.Forms.Panel();
            this.scintillaSQL = new Majorsilence.Reporting.RdlDesign.Syntax.Scintilla();
            this.panel3 = new Majorsilence.Forms.Panel();
            this.bRefresh = new Majorsilence.Forms.Button();
            this.bEditSQL = new Majorsilence.Forms.Button();
            this.panel1 = new Majorsilence.Forms.Panel();
            this.lDataSetName = new Majorsilence.Forms.Label();
            this.tbDSName = new Majorsilence.Forms.TextBox();
            this.tbTimeout = new Majorsilence.Forms.NumericUpDown();
            this.label3 = new Majorsilence.Forms.Label();
            this.label1 = new Majorsilence.Forms.Label();
            this.cbDataSource = new Majorsilence.Forms.ComboBox();
            this.lDataSource = new Majorsilence.Forms.Label();
            this.bDeleteField = new Majorsilence.Forms.Button();
            this.dgFields = new Majorsilence.Forms.DataGridView();
            this.dgtbName = new Majorsilence.Forms.DataGridViewTextBoxColumn();
            this.dgtbQueryName = new Majorsilence.Forms.DataGridViewTextBoxColumn();
            this.dgtbValue = new Majorsilence.Forms.DataGridViewTextBoxColumn();
            this.dgtbTypeName = new Majorsilence.Forms.DataGridViewComboBoxColumn();
            this.label2 = new Majorsilence.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel4.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbTimeout)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgFields)).BeginInit();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            resources.ApplyResources(this.splitContainer1, "splitContainer1");
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.panel2);
            this.splitContainer1.Panel1.Controls.Add(this.panel1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.bDeleteField);
            this.splitContainer1.Panel2.Controls.Add(this.dgFields);
            this.splitContainer1.Panel2.Controls.Add(this.label2);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.panel4);
            this.panel2.Controls.Add(this.panel3);
            resources.ApplyResources(this.panel2, "panel2");
            this.panel2.Name = "panel2";
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.scintillaSQL);
            resources.ApplyResources(this.panel4, "panel4");
            this.panel4.Name = "panel4";
            // 
            // scintillaSQL
            // 
            resources.ApplyResources(this.scintillaSQL, "scintillaSQL");
            this.scintillaSQL.Lexer = Majorsilence.Reporting.RdlDesign.Syntax.Lexer.Sql;
            this.scintillaSQL.Name = "scintillaSQL";
            this.scintillaSQL.UseTabs = false;
            this.scintillaSQL.TextChanged += this.tbSQL_TextChanged;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.bRefresh);
            this.panel3.Controls.Add(this.bEditSQL);
            resources.ApplyResources(this.panel3, "panel3");
            this.panel3.Name = "panel3";
            // 
            // bRefresh
            // 
            resources.ApplyResources(this.bRefresh, "bRefresh");
            this.bRefresh.Name = "bRefresh";
            this.bRefresh.Click += this.bRefresh_Click;
            // 
            // bEditSQL
            // 
            resources.ApplyResources(this.bEditSQL, "bEditSQL");
            this.bEditSQL.Name = "bEditSQL";
            this.bEditSQL.Click += this.bEditSQL_Click;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lDataSetName);
            this.panel1.Controls.Add(this.tbDSName);
            this.panel1.Controls.Add(this.tbTimeout);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.cbDataSource);
            this.panel1.Controls.Add(this.lDataSource);
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.Name = "panel1";
            // 
            // lDataSetName
            // 
            resources.ApplyResources(this.lDataSetName, "lDataSetName");
            this.lDataSetName.Name = "lDataSetName";
            // 
            // tbDSName
            // 
            resources.ApplyResources(this.tbDSName, "tbDSName");
            this.tbDSName.Name = "tbDSName";
            this.tbDSName.TextChanged += this.tbDSName_TextChanged;
            // 
            // tbTimeout
            // 
            resources.ApplyResources(this.tbTimeout, "tbTimeout");
            this.tbTimeout.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.tbTimeout.Name = "tbTimeout";
            this.tbTimeout.ValueChanged += this.tbTimeout_ValueChanged;
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // cbDataSource
            // 
            resources.ApplyResources(this.cbDataSource, "cbDataSource");
            this.cbDataSource.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
            this.cbDataSource.Name = "cbDataSource";
            this.cbDataSource.SelectedIndexChanged += this.cbDataSource_SelectedIndexChanged;
            // 
            // lDataSource
            // 
            resources.ApplyResources(this.lDataSource, "lDataSource");
            this.lDataSource.Name = "lDataSource";
            // 
            // bDeleteField
            // 
            resources.ApplyResources(this.bDeleteField, "bDeleteField");
            this.bDeleteField.Name = "bDeleteField";
            this.bDeleteField.Click += this.bDeleteField_Click;
            // 
            // dgFields
            // 
            resources.ApplyResources(this.dgFields, "dgFields");
            this.dgFields.AutoSizeColumnsMode = Majorsilence.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewCellStyle1.Alignment = Majorsilence.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = Majorsilence.Forms.SystemColors.ControlText;
            dataGridViewCellStyle1.Font = new Majorsilence.Forms.Drawing.Font("Microsoft Sans Serif", 8.25F, Majorsilence.Forms.Drawing.FontStyle.Regular, Majorsilence.Forms.Drawing.GraphicsUnit.Point, ((byte)(204)));
            dataGridViewCellStyle1.ForeColor = Majorsilence.Forms.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = Majorsilence.Forms.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = Majorsilence.Forms.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = Majorsilence.Forms.DataGridViewTriState.True;
            this.dgFields.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgFields.Columns.AddRange(new Majorsilence.Forms.DataGridViewColumn[] {
            this.dgtbName,
            this.dgtbQueryName,
            this.dgtbValue,
            this.dgtbTypeName});
            this.dgFields.Name = "dgFields";
            // 
            // dgtbName
            // 
            this.dgtbName.DataPropertyName = "Name";
            resources.ApplyResources(this.dgtbName, "dgtbName");
            this.dgtbName.Name = "dgtbName";
            // 
            // dgtbQueryName
            // 
            this.dgtbQueryName.DataPropertyName = "QueryName";
            resources.ApplyResources(this.dgtbQueryName, "dgtbQueryName");
            this.dgtbQueryName.Name = "dgtbQueryName";
            // 
            // dgtbValue
            // 
            this.dgtbValue.DataPropertyName = "Value";
            resources.ApplyResources(this.dgtbValue, "dgtbValue");
            this.dgtbValue.Name = "dgtbValue";
            // 
            // dgtbTypeName
            // 
            this.dgtbTypeName.DataPropertyName = "TypeName";
            resources.ApplyResources(this.dgtbTypeName, "dgtbTypeName");
            this.dgtbTypeName.Items.AddRange(new object[] {
            "System.String",
            "System.Int16",
            "System.Int32",
            "System.Int64",
            "System.UInt16",
            "System.UInt32",
            "System.UInt64",
            "System.Single",
            "System.Double",
            "System.Decimal",
            "System.DateTime",
            "System.Char",
            "System.Boolean",
            "System.Byte"});
            this.dgtbTypeName.Name = "dgtbTypeName";
            this.dgtbTypeName.Resizable = Majorsilence.Forms.DataGridViewTriState.True;
            this.dgtbTypeName.SortMode = Majorsilence.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // DataSetsCtl
            // 
            this.Controls.Add(this.splitContainer1);
            resources.ApplyResources(this, "$this");
            this.Name = "DataSetsCtl";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbTimeout)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgFields)).EndInit();
            this.ResumeLayout(false);

        }
        #endregion

        private Majorsilence.Forms.SplitContainer splitContainer1;
        private Majorsilence.Forms.NumericUpDown tbTimeout;
        private Majorsilence.Forms.Label label3;
        private Majorsilence.Forms.Button bRefresh;
        private Majorsilence.Forms.Button bEditSQL;
        private Majorsilence.Forms.Label label1;
        private Majorsilence.Forms.ComboBox cbDataSource;
        private Majorsilence.Forms.Label lDataSource;
        private Majorsilence.Forms.TextBox tbDSName;
        private Majorsilence.Forms.Label lDataSetName;
        private Majorsilence.Forms.Button bDeleteField;
        private Majorsilence.Forms.DataGridView dgFields;
		private Majorsilence.Forms.Label label2;
		private Majorsilence.Forms.DataGridViewTextBoxColumn dgtbName;
		private Majorsilence.Forms.DataGridViewTextBoxColumn dgtbQueryName;
		private Majorsilence.Forms.DataGridViewTextBoxColumn dgtbValue;
		private Majorsilence.Forms.DataGridViewComboBoxColumn dgtbTypeName;
        private Majorsilence.Forms.Panel panel2;
        private Majorsilence.Forms.Panel panel4;
        private Majorsilence.Reporting.RdlDesign.Syntax.Scintilla scintillaSQL;
        private Majorsilence.Forms.Panel panel3;
        private Majorsilence.Forms.Panel panel1;
    }
}