
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Majorsilence.Forms;
using System.Xml;
using Majorsilence.Reporting.RdlDesign.Resources;

namespace Majorsilence.Reporting.RdlDesign
{
	/// <summary>
	/// Summary description for ReportCtl.
	/// </summary>
	internal class TableCtl : Majorsilence.Forms.UserControl, IProperty
	{
        private List<XmlNode> _ReportItems;
		private DesignXmlDraw _Draw;
		bool fDataSet, fPBBefore, fPBAfter, fNoRows;
		bool fDetailElementName, fDetailCollectionName, fRenderDetails;
		bool fCheckRows;
		private Majorsilence.Forms.Label label2;
		private Majorsilence.Forms.ComboBox cbDataSet;
		private Majorsilence.Forms.GroupBox groupBox1;
		private Majorsilence.Forms.CheckBox chkPBBefore;
		private Majorsilence.Forms.CheckBox chkPBAfter;
		private Majorsilence.Forms.Label label1;
		private Majorsilence.Forms.TextBox tbNoRows;
		private Majorsilence.Forms.GroupBox groupBox2;
		private Majorsilence.Forms.Label label3;
		private Majorsilence.Forms.Label label4;
		private Majorsilence.Forms.CheckBox chkRenderDetails;
		private Majorsilence.Forms.TextBox tbDetailElementName;
		private Majorsilence.Forms.TextBox tbDetailCollectionName;
		private Majorsilence.Forms.GroupBox groupBox3;
		private Majorsilence.Forms.CheckBox chkDetails;
		private Majorsilence.Forms.CheckBox chkHeaderRows;
		private Majorsilence.Forms.CheckBox chkFooterRows;
        private CheckBox chkFooterRepeat;
        private CheckBox chkHeaderRepeat;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        public TableCtl(DesignXmlDraw dxDraw, List<XmlNode> ris)
		{
			_ReportItems = ris;
			_Draw = dxDraw;
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Initialize form using the style node values
			InitValues();			
		}

		private void InitValues()
		{
			XmlNode riNode = _ReportItems[0];

			tbNoRows.Text = _Draw.GetElementValue(riNode, "NoRows", "");
			cbDataSet.Items.AddRange(_Draw.DataSetNames);
			cbDataSet.Text = _Draw.GetDataSetNameValue(riNode);
			if (_Draw.GetReportItemDataRegionContainer(riNode) != null)
				cbDataSet.Enabled = false;
			chkPBBefore.Checked = _Draw.GetElementValue(riNode, "PageBreakAtStart", "false").ToLower()=="true"? true:false;
			chkPBAfter.Checked = _Draw.GetElementValue(riNode, "PageBreakAtEnd", "false").ToLower()=="true"? true:false;

			this.chkRenderDetails.Checked = _Draw.GetElementValue(riNode, "DetailDataElementOutput", "output").ToLower() == "output";
			this.tbDetailElementName.Text = _Draw.GetElementValue(riNode, "DetailDataElementName", "Details");
			this.tbDetailCollectionName.Text = _Draw.GetElementValue(riNode, "DetailDataCollectionName", "Details_Collection");

			this.chkDetails.Checked = _Draw.GetNamedChildNode(riNode, "Details") != null;
            XmlNode fNode = _Draw.GetNamedChildNode(riNode, "Footer");
			this.chkFooterRows.Checked = fNode != null;
            if (fNode != null)
            {
                chkFooterRepeat.Checked = _Draw.GetElementValue(fNode, "RepeatOnNewPage", "false").ToLower() == "true" ? true : false;
            }
            else
                chkFooterRepeat.Enabled = false;

            XmlNode hNode = _Draw.GetNamedChildNode(riNode, "Header");
            this.chkHeaderRows.Checked = hNode != null;
            if (hNode != null)
            {
                chkHeaderRepeat.Checked = _Draw.GetElementValue(hNode, "RepeatOnNewPage", "false").ToLower() == "true" ? true : false;
            }
            else
                chkHeaderRepeat.Enabled = false;

			fNoRows = fDataSet = fPBBefore = fPBAfter = 
				fDetailElementName = fDetailCollectionName = fRenderDetails =
				fCheckRows = false;
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
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(TableCtl));
            this.DoubleBuffered = true;
			this.label2 = new Majorsilence.Forms.Label();
			this.cbDataSet = new Majorsilence.Forms.ComboBox();
			this.groupBox1 = new Majorsilence.Forms.GroupBox();
			this.chkPBAfter = new Majorsilence.Forms.CheckBox();
			this.chkPBBefore = new Majorsilence.Forms.CheckBox();
			this.label1 = new Majorsilence.Forms.Label();
			this.tbNoRows = new Majorsilence.Forms.TextBox();
			this.groupBox2 = new Majorsilence.Forms.GroupBox();
			this.tbDetailCollectionName = new Majorsilence.Forms.TextBox();
			this.tbDetailElementName = new Majorsilence.Forms.TextBox();
			this.chkRenderDetails = new Majorsilence.Forms.CheckBox();
			this.label4 = new Majorsilence.Forms.Label();
			this.label3 = new Majorsilence.Forms.Label();
			this.groupBox3 = new Majorsilence.Forms.GroupBox();
			this.chkFooterRepeat = new Majorsilence.Forms.CheckBox();
			this.chkHeaderRepeat = new Majorsilence.Forms.CheckBox();
			this.chkFooterRows = new Majorsilence.Forms.CheckBox();
			this.chkHeaderRows = new Majorsilence.Forms.CheckBox();
			this.chkDetails = new Majorsilence.Forms.CheckBox();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.SuspendLayout();
			// 
			// label2
			// 
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			// 
			// cbDataSet
			// 
			resources.ApplyResources(this.cbDataSet, "cbDataSet");
			this.cbDataSet.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbDataSet.Name = "cbDataSet";
			this.cbDataSet.SelectedIndexChanged += this.cbDataSet_SelectedIndexChanged;
			// 
			// groupBox1
			// 
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.Controls.Add(this.chkPBAfter);
			this.groupBox1.Controls.Add(this.chkPBBefore);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabStop = false;
			// 
			// chkPBAfter
			// 
			resources.ApplyResources(this.chkPBAfter, "chkPBAfter");
			this.chkPBAfter.Name = "chkPBAfter";
			this.chkPBAfter.CheckedChanged += this.chkPBAfter_CheckedChanged;
			// 
			// chkPBBefore
			// 
			resources.ApplyResources(this.chkPBBefore, "chkPBBefore");
			this.chkPBBefore.Name = "chkPBBefore";
			this.chkPBBefore.CheckedChanged += this.chkPBBefore_CheckedChanged;
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// tbNoRows
			// 
			resources.ApplyResources(this.tbNoRows, "tbNoRows");
			this.tbNoRows.Name = "tbNoRows";
			this.tbNoRows.TextChanged += this.tbNoRows_TextChanged;
			// 
			// groupBox2
			// 
			resources.ApplyResources(this.groupBox2, "groupBox2");
			this.groupBox2.Controls.Add(this.tbDetailCollectionName);
			this.groupBox2.Controls.Add(this.tbDetailElementName);
			this.groupBox2.Controls.Add(this.chkRenderDetails);
			this.groupBox2.Controls.Add(this.label4);
			this.groupBox2.Controls.Add(this.label3);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.TabStop = false;
			// 
			// tbDetailCollectionName
			// 
			resources.ApplyResources(this.tbDetailCollectionName, "tbDetailCollectionName");
			this.tbDetailCollectionName.Name = "tbDetailCollectionName";
			this.tbDetailCollectionName.TextChanged += this.tbDetailCollectionName_TextChanged;
			// 
			// tbDetailElementName
			// 
			resources.ApplyResources(this.tbDetailElementName, "tbDetailElementName");
			this.tbDetailElementName.Name = "tbDetailElementName";
			this.tbDetailElementName.TextChanged += this.tbDetailElementName_TextChanged;
			// 
			// chkRenderDetails
			// 
			resources.ApplyResources(this.chkRenderDetails, "chkRenderDetails");
			this.chkRenderDetails.Name = "chkRenderDetails";
			this.chkRenderDetails.CheckedChanged += this.chkRenderDetails_CheckedChanged;
			// 
			// label4
			// 
			resources.ApplyResources(this.label4, "label4");
			this.label4.Name = "label4";
			// 
			// label3
			// 
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			// 
			// groupBox3
			// 
			resources.ApplyResources(this.groupBox3, "groupBox3");
			this.groupBox3.Controls.Add(this.chkFooterRepeat);
			this.groupBox3.Controls.Add(this.chkHeaderRepeat);
			this.groupBox3.Controls.Add(this.chkFooterRows);
			this.groupBox3.Controls.Add(this.chkHeaderRows);
			this.groupBox3.Controls.Add(this.chkDetails);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.TabStop = false;
			// 
			// chkFooterRepeat
			// 
			resources.ApplyResources(this.chkFooterRepeat, "chkFooterRepeat");
			this.chkFooterRepeat.Name = "chkFooterRepeat";
			this.chkFooterRepeat.CheckedChanged += this.chkRows_CheckedChanged;
			// 
			// chkHeaderRepeat
			// 
			resources.ApplyResources(this.chkHeaderRepeat, "chkHeaderRepeat");
			this.chkHeaderRepeat.Name = "chkHeaderRepeat";
			this.chkHeaderRepeat.CheckedChanged += this.chkRows_CheckedChanged;
			// 
			// chkFooterRows
			// 
			resources.ApplyResources(this.chkFooterRows, "chkFooterRows");
			this.chkFooterRows.Name = "chkFooterRows";
			this.chkFooterRows.CheckedChanged += this.chkRows_CheckedChanged;
			// 
			// chkHeaderRows
			// 
			resources.ApplyResources(this.chkHeaderRows, "chkHeaderRows");
			this.chkHeaderRows.Name = "chkHeaderRows";
			this.chkHeaderRows.CheckedChanged += this.chkRows_CheckedChanged;
			// 
			// chkDetails
			// 
			resources.ApplyResources(this.chkDetails, "chkDetails");
			this.chkDetails.Name = "chkDetails";
			this.chkDetails.CheckedChanged += this.chkRows_CheckedChanged;
			// 
			// TableCtl
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.tbNoRows);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.cbDataSet);
			this.Controls.Add(this.label2);
			this.Name = "TableCtl";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.groupBox3.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
  
		public bool IsValid()
		{
			if (chkDetails.Checked || chkFooterRows.Checked || chkHeaderRows.Checked)
				return true;

			MessageBox.Show(Strings.TableCtl_Show_TableMustHaveRegions, Strings.TableCtl_Show_Table);

			return false;
		}

		public void Apply()
		{
			// take information in control and apply to all the style nodes
			//  Only change information that has been marked as modified;
			//   this way when group is selected it is possible to change just
			//   the items you want and keep the rest the same.
				
			foreach (XmlNode riNode in this._ReportItems)
				ApplyChanges(riNode);

			// No more changes
			fNoRows = fDataSet = fPBBefore = fPBAfter = 
				fDetailElementName = fDetailCollectionName = fRenderDetails =
				fCheckRows = false;
		}

		public void ApplyChanges(XmlNode node)
		{
			if (fNoRows)
				_Draw.SetElement(node, "NoRows", this.tbNoRows.Text);
			if (fDataSet)
				_Draw.SetElement(node, "DataSetName", this.cbDataSet.Text);
			if (fPBBefore)
				_Draw.SetElement(node, "PageBreakAtStart", this.chkPBBefore.Checked? "true":"false");
			if (fPBAfter)
				_Draw.SetElement(node, "PageBreakAtEnd", this.chkPBAfter.Checked? "true":"false");
			if (fCheckRows)
			{
				if (this.chkDetails.Checked)
					CreateTableRow(node, "Details", false);
				else
					_Draw.RemoveElement(node, "Details");
				if (this.chkHeaderRows.Checked)
					CreateTableRow(node, "Header", chkHeaderRepeat.Checked);
				else
					_Draw.RemoveElement(node, "Header");
				if (this.chkFooterRows.Checked)
					CreateTableRow(node, "Footer", chkFooterRepeat.Checked);
				else
					_Draw.RemoveElement(node, "Footer");
			}
			if (fRenderDetails)
				_Draw.SetElement(node, "DetailDataElementOutput", this.chkRenderDetails.Checked? "Output":"NoOutput");
			if (this.fDetailElementName)
			{
				if (this.tbDetailElementName.Text.Length > 0)
					_Draw.SetElement(node, "DetailDataElementName", this.tbDetailElementName.Text);
				else
					_Draw.RemoveElement(node, "DetailDataElementName");
			}
			if (this.fDetailCollectionName)
			{
				if (this.tbDetailCollectionName.Text.Length > 0)
					_Draw.SetElement(node, "DetailDataCollectionName", this.tbDetailCollectionName.Text);
				else
					_Draw.RemoveElement(node, "DetailDataCollectionName");
			}
		}

		private void CreateTableRow(XmlNode tblNode, string elementName, bool bRepeatOnNewPage)
		{
			XmlNode node = _Draw.GetNamedChildNode(tblNode, elementName);
			if (node == null)
			{
				node = _Draw.CreateElement(tblNode, elementName, null);
				XmlNode tblRows = _Draw.CreateElement(node, "TableRows", null);
				_Draw.InsertTableRow(tblRows);
			}
            if (bRepeatOnNewPage)
                _Draw.SetElement(node, "RepeatOnNewPage", "true");
            else
                _Draw.RemoveElement(node, "RepeatOnNewPage");

			return;
		}

		private void cbDataSet_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			fDataSet = true;
		}

		private void chkPBBefore_CheckedChanged(object sender, System.EventArgs e)
		{
			fPBBefore = true;
		}

		private void chkPBAfter_CheckedChanged(object sender, System.EventArgs e)
		{
			fPBAfter = true;
		}

		private void tbNoRows_TextChanged(object sender, System.EventArgs e)
		{
			fNoRows = true;
		}

		private void chkRows_CheckedChanged(object sender, System.EventArgs e)
		{
			this.fCheckRows = true;
            chkFooterRepeat.Enabled = chkFooterRows.Checked;
            chkHeaderRepeat.Enabled = chkHeaderRows.Checked;
		}

		private void chkRenderDetails_CheckedChanged(object sender, System.EventArgs e)
		{
			fRenderDetails = true;
		}

		private void tbDetailElementName_TextChanged(object sender, System.EventArgs e)
		{
			fDetailElementName = true;
		}

		private void tbDetailCollectionName_TextChanged(object sender, System.EventArgs e)
		{
			fDetailCollectionName = true;
		}

	}
}
