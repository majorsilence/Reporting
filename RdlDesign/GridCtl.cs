
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
	internal class GridCtl : Majorsilence.Forms.UserControl, IProperty
	{
        private List<XmlNode> _ReportItems;
		private DesignXmlDraw _Draw;
		bool fPBBefore, fPBAfter;
        bool fCheckRows;
		private Majorsilence.Forms.GroupBox groupBox1;
		private Majorsilence.Forms.CheckBox chkPBBefore;
        private Majorsilence.Forms.CheckBox chkPBAfter;
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

        public GridCtl(DesignXmlDraw dxDraw, List<XmlNode> ris)
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

			chkPBBefore.Checked = _Draw.GetElementValue(riNode, "PageBreakAtStart", "false").ToLower()=="true"? true:false;
			chkPBAfter.Checked = _Draw.GetElementValue(riNode, "PageBreakAtEnd", "false").ToLower()=="true"? true:false;

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

			fPBBefore = fPBAfter = fCheckRows = false;
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
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(GridCtl));
            this.DoubleBuffered = true;
			this.groupBox1 = new Majorsilence.Forms.GroupBox();
			this.chkPBAfter = new Majorsilence.Forms.CheckBox();
			this.chkPBBefore = new Majorsilence.Forms.CheckBox();
			this.groupBox3 = new Majorsilence.Forms.GroupBox();
			this.chkFooterRepeat = new Majorsilence.Forms.CheckBox();
			this.chkHeaderRepeat = new Majorsilence.Forms.CheckBox();
			this.chkFooterRows = new Majorsilence.Forms.CheckBox();
			this.chkHeaderRows = new Majorsilence.Forms.CheckBox();
			this.chkDetails = new Majorsilence.Forms.CheckBox();
			this.groupBox1.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.SuspendLayout();
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
			// GridCtl
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.groupBox1);
			this.Name = "GridCtl";
			this.groupBox1.ResumeLayout(false);
			this.groupBox3.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion
  
		public bool IsValid()
		{
			if (this.chkDetails.Checked || this.chkFooterRows.Checked || this.chkHeaderRows.Checked)
				return true;

			MessageBox.Show(Strings.GridCtl_Show_GridRegionsMustDefined, Strings.GridCtl_Show_Grid);

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
			fPBBefore = fPBAfter = fCheckRows = false;
		}

		public void ApplyChanges(XmlNode node)
		{
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

		private void chkPBBefore_CheckedChanged(object sender, System.EventArgs e)
		{
			fPBBefore = true;
		}

		private void chkPBAfter_CheckedChanged(object sender, System.EventArgs e)
		{
			fPBAfter = true;
		}

		private void chkRows_CheckedChanged(object sender, System.EventArgs e)
		{
			this.fCheckRows = true;
            chkFooterRepeat.Enabled = chkFooterRows.Checked;
            chkHeaderRepeat.Enabled = chkHeaderRows.Checked;
		}

	}
}
