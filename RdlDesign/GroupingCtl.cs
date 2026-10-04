
using System;
using System.Collections;
using System.ComponentModel;
using Majorsilence.Forms.Drawing;
using System.Data;
using Majorsilence.Forms;
using System.Xml;
using System.Text;
using Majorsilence.Reporting.RdlDesign.Resources;

namespace Majorsilence.Reporting.RdlDesign
{
	/// <summary>
	/// Grouping specification: used for DataRegions (List, Chart, Table, Matrix), DataSets, group instances
	/// </summary>
	internal class GroupingCtl : Majorsilence.Forms.UserControl, IProperty
	{
		private DesignXmlDraw _Draw;
		private XmlNode _GroupingParent;
		private DataTable _DataTable;

		private Majorsilence.Forms.Button bDelete;
		private Majorsilence.Forms.Button bUp;
		private Majorsilence.Forms.Button bDown;
		private Majorsilence.Forms.DataGridView dgGroup;
		private Majorsilence.Forms.Label label1;
		private Majorsilence.Forms.TextBox tbName;
		private Majorsilence.Forms.Label label2;
		private Majorsilence.Forms.Label label3;
		private Majorsilence.Forms.ComboBox cbLabelExpr;
		private Majorsilence.Forms.ComboBox cbParentExpr;
		private Majorsilence.Forms.CheckBox chkPBS;
		private Majorsilence.Forms.CheckBox chkPBE;
		private Majorsilence.Forms.CheckBox chkRepeatHeader;
		private Majorsilence.Forms.CheckBox chkGrpHeader;
		private Majorsilence.Forms.CheckBox chkRepeatFooter;
		private Majorsilence.Forms.CheckBox chkGrpFooter;
		private Majorsilence.Forms.Label lParent;
		private Majorsilence.Forms.Button bValueExpr;
		private Majorsilence.Forms.Button bLabelExpr;
		private Majorsilence.Forms.Button bParentExpr;
        private Button BtnCercaFormulaEsclusione;
        private TextBox TxtPageBreakCondition;
        private Label label4;

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

		internal GroupingCtl(DesignXmlDraw dxDraw, XmlNode groupingParent)
		{
			_Draw = dxDraw;
			_GroupingParent = groupingParent;
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Initialize form using the style node values
			InitValues();			
		}

		private void InitValues()
		{
			// Get the parent's dataset name
//			string dataSetName = _Draw.GetDataSetNameValue(_GroupingParent);
//
//			string[] fields = _Draw.GetFields(dataSetName, true);
//			if (fields != null)
//				dgtbGE.CB.Items.AddRange(fields);

			// Initialize the DataTable
			_DataTable = new DataTable();
			_DataTable.Columns.Add(new DataColumn("Expression", typeof(string)));

			string[] rowValues = new string[1];
			XmlNode grouping = _Draw.GetNamedChildNode(_GroupingParent, "Grouping");

			// Handle the group expressions
			XmlNode groupingExs = _Draw.GetNamedChildNode(grouping, "GroupExpressions");

			if (groupingExs != null)
			foreach (XmlNode gNode in groupingExs.ChildNodes)
			{
				if (gNode.NodeType != XmlNodeType.Element || 
						gNode.Name != "GroupExpression")
					continue;
				rowValues[0] = gNode.InnerText;

				_DataTable.Rows.Add(rowValues);
			}
			this.dgGroup.DataSource = _DataTable;
			dgGroup.Columns[0].Width = 330;

			//
			if (grouping == null)
			{
				this.tbName.Text = "";
				this.cbParentExpr.Text =  "";
				this.cbLabelExpr.Text =  "";
			}
			else
			{
				this.chkPBE.Checked = _Draw.GetElementValue(grouping, "PageBreakAtEnd", "false").ToLower() == "true";
				this.chkPBS.Checked = _Draw.GetElementValue(grouping, "PageBreakAtStart", "false").ToLower() == "true";

				this.tbName.Text = _Draw.GetElementAttribute(grouping, "Name", "");
				this.cbParentExpr.Text =  _Draw.GetElementValue(grouping, "Parent", "");
				this.cbLabelExpr.Text =  _Draw.GetElementValue(grouping, "Label", "");
                this.TxtPageBreakCondition.Text = _Draw.GetElementValue(grouping, "PageBreakCondition", "");
            }

			if (_GroupingParent.Name == "TableGroup")
			{
				XmlNode repeat;
				repeat = DesignXmlDraw.FindNextInHierarchy(_GroupingParent, "Header", "RepeatOnNewPage");
				if (repeat != null)
					this.chkRepeatHeader.Checked = repeat.InnerText.ToLower() == "true";
				repeat = DesignXmlDraw.FindNextInHierarchy(_GroupingParent, "Footer", "RepeatOnNewPage");
				if (repeat != null)
					this.chkRepeatFooter.Checked = repeat.InnerText.ToLower() == "true";
				this.chkGrpHeader.Checked = _Draw.GetNamedChildNode(_GroupingParent, "Header") != null;
				this.chkGrpFooter.Checked = _Draw.GetNamedChildNode(_GroupingParent, "Footer") != null;
			}
			else
			{
				this.chkRepeatFooter.Visible = false;
				this.chkRepeatHeader.Visible = false;
				this.chkGrpFooter.Visible = false;
				this.chkGrpHeader.Visible = false;
			}
			if (_GroupingParent.Name == "DynamicColumns" ||
				_GroupingParent.Name == "DynamicRows")
			{
				this.chkPBE.Visible = this.chkPBS.Visible = false;
			}
			else if (_GroupingParent.Name == "DynamicSeries" ||
				_GroupingParent.Name == "DynamicCategories")
			{
				this.chkPBE.Visible = this.chkPBS.Visible = false;
				this.cbParentExpr.Visible = this.lParent.Visible = false;
				this.cbLabelExpr.Text =  _Draw.GetElementValue(_GroupingParent, "Label", "");
                this.TxtPageBreakCondition.Text = _Draw.GetElementValue(grouping, "PageBreakCondition", "");
            }

			// load label and parent controls with fields
			string dsn = _Draw.GetDataSetNameValue(_GroupingParent);
			if (dsn != null)	// found it
			{
				string[] f = _Draw.GetFields(dsn, true);
                if (f != null)
                {
                    this.cbParentExpr.Items.AddRange(f);
                    this.cbLabelExpr.Items.AddRange(f);
                }
			}

		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(GroupingCtl));
            this.DoubleBuffered = true;
            this.dgGroup = new Majorsilence.Forms.DataGridView();
            this.bDelete = new Majorsilence.Forms.Button();
            this.bUp = new Majorsilence.Forms.Button();
            this.bDown = new Majorsilence.Forms.Button();
            this.label1 = new Majorsilence.Forms.Label();
            this.tbName = new Majorsilence.Forms.TextBox();
            this.label2 = new Majorsilence.Forms.Label();
            this.label3 = new Majorsilence.Forms.Label();
            this.cbLabelExpr = new Majorsilence.Forms.ComboBox();
            this.cbParentExpr = new Majorsilence.Forms.ComboBox();
            this.lParent = new Majorsilence.Forms.Label();
            this.chkPBS = new Majorsilence.Forms.CheckBox();
            this.chkPBE = new Majorsilence.Forms.CheckBox();
            this.chkRepeatHeader = new Majorsilence.Forms.CheckBox();
            this.chkGrpHeader = new Majorsilence.Forms.CheckBox();
            this.chkRepeatFooter = new Majorsilence.Forms.CheckBox();
            this.chkGrpFooter = new Majorsilence.Forms.CheckBox();
            this.bValueExpr = new Majorsilence.Forms.Button();
            this.bLabelExpr = new Majorsilence.Forms.Button();
            this.bParentExpr = new Majorsilence.Forms.Button();
            this.BtnCercaFormulaEsclusione = new Majorsilence.Forms.Button();
            this.TxtPageBreakCondition = new Majorsilence.Forms.TextBox();
            this.label4 = new Majorsilence.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dgGroup)).BeginInit();
            this.SuspendLayout();
            // 
            // dgGroup
            // 
            resources.ApplyResources(this.dgGroup, "dgGroup");
            this.dgGroup.Name = "dgGroup";
            // 
            // bDelete
            // 
            resources.ApplyResources(this.bDelete, "bDelete");
            this.bDelete.Name = "bDelete";
            this.bDelete.Click += this.bDelete_Click;
            // 
            // bUp
            // 
            resources.ApplyResources(this.bUp, "bUp");
            this.bUp.Name = "bUp";
            this.bUp.Click += this.bUp_Click;
            // 
            // bDown
            // 
            resources.ApplyResources(this.bDown, "bDown");
            this.bDown.Name = "bDown";
            this.bDown.Click += this.bDown_Click;
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // tbName
            // 
            resources.ApplyResources(this.tbName, "tbName");
            this.tbName.Name = "tbName";
            this.tbName.Validating += this.tbName_Validating;
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // cbLabelExpr
            // 
            resources.ApplyResources(this.cbLabelExpr, "cbLabelExpr");
            this.cbLabelExpr.Name = "cbLabelExpr";
            // 
            // cbParentExpr
            // 
            resources.ApplyResources(this.cbParentExpr, "cbParentExpr");
            this.cbParentExpr.Name = "cbParentExpr";
            // 
            // lParent
            // 
            resources.ApplyResources(this.lParent, "lParent");
            this.lParent.Name = "lParent";
            // 
            // chkPBS
            // 
            resources.ApplyResources(this.chkPBS, "chkPBS");
            this.chkPBS.Name = "chkPBS";
            // 
            // chkPBE
            // 
            resources.ApplyResources(this.chkPBE, "chkPBE");
            this.chkPBE.Name = "chkPBE";
            // 
            // chkRepeatHeader
            // 
            resources.ApplyResources(this.chkRepeatHeader, "chkRepeatHeader");
            this.chkRepeatHeader.Name = "chkRepeatHeader";
            // 
            // chkGrpHeader
            // 
            resources.ApplyResources(this.chkGrpHeader, "chkGrpHeader");
            this.chkGrpHeader.Name = "chkGrpHeader";
            // 
            // chkRepeatFooter
            // 
            resources.ApplyResources(this.chkRepeatFooter, "chkRepeatFooter");
            this.chkRepeatFooter.Name = "chkRepeatFooter";
            // 
            // chkGrpFooter
            // 
            resources.ApplyResources(this.chkGrpFooter, "chkGrpFooter");
            this.chkGrpFooter.Name = "chkGrpFooter";
            // 
            // bValueExpr
            // 
            resources.ApplyResources(this.bValueExpr, "bValueExpr");
            this.bValueExpr.Name = "bValueExpr";
            this.bValueExpr.Tag = "value";
            this.bValueExpr.Click += this.bValueExpr_Click;
            // 
            // bLabelExpr
            // 
            resources.ApplyResources(this.bLabelExpr, "bLabelExpr");
            this.bLabelExpr.Name = "bLabelExpr";
            this.bLabelExpr.Tag = "label";
            this.bLabelExpr.Click += this.bExpr_Click;
            // 
            // bParentExpr
            // 
            resources.ApplyResources(this.bParentExpr, "bParentExpr");
            this.bParentExpr.Name = "bParentExpr";
            this.bParentExpr.Tag = "parent";
            this.bParentExpr.Click += this.bExpr_Click;
            // 
            // BtnCercaFormulaEsclusione
            // 
            resources.ApplyResources(this.BtnCercaFormulaEsclusione, "BtnCercaFormulaEsclusione");
            this.BtnCercaFormulaEsclusione.Name = "BtnCercaFormulaEsclusione";
            this.BtnCercaFormulaEsclusione.Tag = "value";
            this.BtnCercaFormulaEsclusione.Click += this.BtnCercaFormulaEsclusione_Click;
            // 
            // TxtPageBreakCondition
            // 
            resources.ApplyResources(this.TxtPageBreakCondition, "TxtPageBreakCondition");
            this.TxtPageBreakCondition.Name = "TxtPageBreakCondition";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // GroupingCtl
            // 
            this.Controls.Add(this.BtnCercaFormulaEsclusione);
            this.Controls.Add(this.TxtPageBreakCondition);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.bParentExpr);
            this.Controls.Add(this.bLabelExpr);
            this.Controls.Add(this.bValueExpr);
            this.Controls.Add(this.chkRepeatFooter);
            this.Controls.Add(this.chkGrpFooter);
            this.Controls.Add(this.chkRepeatHeader);
            this.Controls.Add(this.chkGrpHeader);
            this.Controls.Add(this.chkPBE);
            this.Controls.Add(this.chkPBS);
            this.Controls.Add(this.cbParentExpr);
            this.Controls.Add(this.lParent);
            this.Controls.Add(this.cbLabelExpr);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tbName);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.bDown);
            this.Controls.Add(this.bUp);
            this.Controls.Add(this.bDelete);
            this.Controls.Add(this.dgGroup);
            this.Name = "GroupingCtl";
            resources.ApplyResources(this, "$this");
            ((System.ComponentModel.ISupportInitialize)(this.dgGroup)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		public bool IsValid()
		{
			// Check to see if we have an expression
			bool bRows=HasRows();

			// If no rows and no data 
			if (!bRows && this.tbName.Text.Trim().Length == 0)
			{
				if (_GroupingParent.Name == "Details" ||
					_GroupingParent.Name == "List")
					return true;

				MessageBox.Show(Strings.GroupingCtl_Show_GroupMustDefined, Strings.GroupingCtl_Show_Grouping);
				return false;
			}

			// Grouping must have name
			XmlNode grouping = _Draw.GetNamedChildNode(_GroupingParent, "Grouping");
			string nerr = _Draw.GroupingNameCheck(grouping, this.tbName.Text);
			if (nerr != null)
			{
				MessageBox.Show(nerr, Strings.GroupingCtl_Show_GroupNameError);
				return false;
			}

			if (!bRows)
			{
				MessageBox.Show(Strings.GroupingCtl_Show_NoExpressionsForGroup, Strings.GroupingCtl_Show_Group);
				return false;
			}

			if (_GroupingParent.Name != "DynamicSeries")
				return true;
			// DynamicSeries grouping must have a label for the legend
			if (this.cbLabelExpr.Text.Length > 0)
				return true;

			MessageBox.Show(Strings.GroupingCtl_Show_ChartSeriesMustHaveLabelForLegend, Strings.GroupingCtl_Show_Chart);

			return false;
		}

		private bool HasRows()
		{
			bool bRows=false;
			foreach (DataRow dr in _DataTable.Rows)
			{
				if (dr[0] == DBNull.Value)
					continue;
				string ge = (string) dr[0];
				if (ge.Length <= 0)
					continue;
				bRows = true;
				break;
			}
			return bRows;
		}

		public void Apply()
		{
			if (!HasRows())		// No expressions in grouping; get rid of grouping
			{
				_Draw.RemoveElement(_GroupingParent, "Grouping");	// can't have a grouping
				return;
			}

			// Get the group
			XmlNode grouping = _Draw.GetCreateNamedChildNode(_GroupingParent, "Grouping");

			_Draw.SetGroupName(grouping, tbName.Text.Trim());
			
			// Handle the label
			if (_GroupingParent.Name == "DynamicSeries" ||
				_GroupingParent.Name == "DynamicCategories")
			{
				if (this.cbLabelExpr.Text.Length > 0)
					_Draw.SetElement(_GroupingParent, "Label", cbLabelExpr.Text);
				else
					_Draw.RemoveElement(_GroupingParent, "Label");
			}
			else
			{
                // Entry point per formula di validazione
                _Draw.SetElement(grouping, "PageBreakCondition", TxtPageBreakCondition.Text.Trim());

                if (this.cbLabelExpr.Text.Length > 0)
					_Draw.SetElement(grouping, "Label", cbLabelExpr.Text);
				else
					_Draw.RemoveElement(grouping, "Label");

				_Draw.SetElement(grouping, "PageBreakAtStart", this.chkPBS.Checked? "true": "false");
				_Draw.SetElement(grouping, "PageBreakAtEnd", this.chkPBE.Checked? "true": "false");
				if (cbParentExpr.Text.Length > 0)
					_Draw.SetElement(grouping, "Parent", cbParentExpr.Text);
				else
					_Draw.RemoveElement(grouping, "Parent");
			}


			// Loop thru and add all the group expressions
			XmlNode grpExprs = _Draw.GetCreateNamedChildNode(grouping, "GroupExpressions");
			grpExprs.RemoveAll();
			string firstexpr=null;
			foreach (DataRow dr in _DataTable.Rows)
			{
				if (dr[0] == DBNull.Value)
					continue;
				string ge = (string) dr[0];
				if (ge.Length <= 0)
					continue;
				_Draw.CreateElement(grpExprs, "GroupExpression", ge);
				if (firstexpr == null)
					firstexpr = ge;
			}
			if (!grpExprs.HasChildNodes)
			{	// With no group expressions there are no groups
				grouping.RemoveChild(grpExprs);
				grouping.ParentNode.RemoveChild(grouping);
				grouping = null;
			}

			if (_GroupingParent.Name == "TableGroup" && grouping != null)
			{
				if (this.chkGrpHeader.Checked)
				{
					XmlNode header = _Draw.GetCreateNamedChildNode(_GroupingParent, "Header");
					_Draw.SetElement(header, "RepeatOnNewPage", chkRepeatHeader.Checked? "true": "false");
					XmlNode tblRows = _Draw.GetCreateNamedChildNode(header, "TableRows");
					if (!tblRows.HasChildNodes)
					{	// We need to create a row
						_Draw.InsertTableRow(tblRows);
					}
				}
				else
				{
					_Draw.RemoveElement(_GroupingParent, "Header");
				}

				if (this.chkGrpFooter.Checked)
				{
					XmlNode footer = _Draw.GetCreateNamedChildNode(_GroupingParent, "Footer");
					_Draw.SetElement(footer, "RepeatOnNewPage", chkRepeatFooter.Checked? "true": "false");
					XmlNode tblRows = _Draw.GetCreateNamedChildNode(footer, "TableRows");
					if (!tblRows.HasChildNodes)
					{	// We need to create a row
						_Draw.InsertTableRow(tblRows);
					}
				}
				else
				{
					_Draw.RemoveElement(_GroupingParent, "Footer");
				}
			}
			else if (_GroupingParent.Name == "DynamicColumns" ||
					 _GroupingParent.Name == "DynamicRows")
			{
				XmlNode ritems = _Draw.GetNamedChildNode(_GroupingParent, "ReportItems");
				if (ritems == null)
					ritems = _Draw.GetCreateNamedChildNode(_GroupingParent, "ReportItems");
				XmlNode item = ritems.FirstChild;
				if (item == null)
				{
					item = _Draw.GetCreateNamedChildNode(ritems, "Textbox");
					XmlNode vnode = _Draw.GetCreateNamedChildNode(item, "Value");
					vnode.InnerText = firstexpr == null? "": firstexpr;
				}
			}
		}

		private void bDelete_Click(object sender, System.EventArgs e)
		{
			int cr = dgGroup.CurrentRow.Index;
			if (cr < 0)		// already at the top
				return;
			else if (cr == 0)
			{
				DataRow dr = _DataTable.Rows[0];
				dr[0] = null;
			}

			this._DataTable.Rows.RemoveAt(cr);
		}

		private void bUp_Click(object sender, System.EventArgs e)
		{
			int cr = dgGroup.CurrentRow.Index;
			if (cr <= 0)		// already at the top
				return;
			
			SwapRow(_DataTable.Rows[cr-1], _DataTable.Rows[cr]);

            if (cr >= 0 && cr < dgGroup.Rows.Count)
            {
                dgGroup.CurrentCell = dgGroup.Rows[cr-1].Cells[0];
            }
        }

		private void bDown_Click(object sender, System.EventArgs e)
		{
			int cr = dgGroup.CurrentRow.Index;
			if (cr < 0)			// invalid index
				return;
			if (cr + 1 >= _DataTable.Rows.Count)
				return;			// already at end
			
			SwapRow(_DataTable.Rows[cr+1], _DataTable.Rows[cr]);
            if (cr >= 0 && cr < dgGroup.Rows.Count)
            {
                dgGroup.CurrentCell = dgGroup.Rows[cr + 1].Cells[0];
            }
        }

        private void SwapRow(DataRow tdr, DataRow fdr)
        {
            // column 1
            object save = tdr[0];
            tdr[0] = fdr[0];
            fdr[0] = save;

            int columnCount = _DataTable.Columns.Count;
            // column 2
            if (columnCount > 1)
            {
                save = tdr[1];
                tdr[1] = fdr[1];
                fdr[1] = save;
            }
            // column 3
            if (columnCount > 2)
            {
                save = tdr[2];
                tdr[2] = fdr[2];
                fdr[2] = save;
            }
            return;
        }

        private void tbName_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
			bool bRows=HasRows();

			// If no rows and no data in name it's ok
			if (!bRows && this.tbName.Text.Trim().Length == 0)
				return;

			if (!ReportNames.IsNameValid(tbName.Text))
			{
				e.Cancel = true;
				MessageBox.Show(string.Format(Strings.GroupingCtl_Show_InvalidName, tbName.Text), Strings.GroupingCtl_Show_Name);
			}
		}

		private void bValueExpr_Click(object sender, System.EventArgs e)
		{
            int cr = -1;
            if (dgGroup.CurrentRow != null)
            {
                cr = dgGroup.CurrentRow.Index;
            }
            if (cr < 0)
			{	// No rows yet; create one
				string[] rowValues = new string[1];
				rowValues[0] = null;

				_DataTable.Rows.Add(rowValues);
				cr = 0;
			}

            DataGridViewCell dgc = dgGroup.CurrentCell;
			int cc = dgc.ColumnIndex;
			DataRow dr = _DataTable.Rows[cr];
			string cv = dr[cc] as string;

			DialogExprEditor ee = new DialogExprEditor(_Draw, cv, _GroupingParent, false);
            try
            {
                DialogResult dlgr = ee.ShowDialog();
                if (dlgr == DialogResult.OK)
                    dr[cc] = ee.Expression;
            }
            finally
            {
                ee.Dispose();
            }
		}

		private void bExpr_Click(object sender, System.EventArgs e)
		{
			Button b = sender as Button;
			if (b == null)
				return;
			Control c = null;
			switch (b.Tag as string)
			{
				case "label":
					c = this.cbLabelExpr;
					break;
				case "parent":
					c = this.cbParentExpr;
					break;
			}

			if (c == null)
				return;

			DialogExprEditor ee = new DialogExprEditor(_Draw, c.Text, _GroupingParent, false);
            try
            {
                DialogResult dr = ee.ShowDialog();
                if (dr == DialogResult.OK)
                    c.Text = ee.Expression;
            }
            finally
            {
                ee.Dispose();
            }
			return;
		}

        private void BtnCercaFormulaEsclusione_Click(object sender, EventArgs e)
        {
            Control c = TxtPageBreakCondition;
            DialogExprEditor ee = new DialogExprEditor(_Draw, c.Text, _GroupingParent, false);
            try
            {
                DialogResult dr = ee.ShowDialog();
                if (dr == DialogResult.OK)
                    c.Text = ee.Expression;
            }
            finally
            {
                ee.Dispose();
            }
            return;
        }
    }
}
