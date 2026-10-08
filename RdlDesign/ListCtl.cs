
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Majorsilence.Forms;
using System.Xml;

namespace Majorsilence.Reporting.RdlDesign
{
	/// <summary>
	/// Summary description for ReportCtl.
	/// </summary>
	internal class ListCtl : Majorsilence.Forms.UserControl, IProperty
	{
        private List<XmlNode> _ReportItems;
		private DesignXmlDraw _Draw;
		bool fDataSet, fPBBefore, fPBAfter, fNoRows, fDataInstanceElementOutput, fDataInstanceName;
		private Majorsilence.Forms.Label label2;
		private Majorsilence.Forms.ComboBox cbDataSet;
		private Majorsilence.Forms.GroupBox groupBox1;
		private Majorsilence.Forms.CheckBox chkPBBefore;
		private Majorsilence.Forms.CheckBox chkPBAfter;
		private Majorsilence.Forms.Button bGroups;
		private Majorsilence.Forms.Label label1;
		private Majorsilence.Forms.TextBox tbNoRows;
		private Majorsilence.Forms.TextBox tbDataInstanceName;
		private Majorsilence.Forms.CheckBox chkXmlInstances;
		private Majorsilence.Forms.Label label3;
		private Majorsilence.Forms.GroupBox groupBox2;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        public ListCtl(DesignXmlDraw dxDraw, List<XmlNode> ris)
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
			this.chkXmlInstances.Checked = _Draw.GetElementValue(riNode, "DataInstanceElementOutput", "Output")=="Output"?true:false;
			this.tbDataInstanceName.Text =  _Draw.GetElementValue(riNode, "DataInstanceName", "Item");

			fNoRows = fDataSet = fPBBefore = fPBAfter = fDataInstanceElementOutput = fDataInstanceName = false;
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
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(ListCtl));
            this.DoubleBuffered = true;
			this.label2 = new Majorsilence.Forms.Label();
			this.cbDataSet = new Majorsilence.Forms.ComboBox();
			this.groupBox1 = new Majorsilence.Forms.GroupBox();
			this.chkPBAfter = new Majorsilence.Forms.CheckBox();
			this.chkPBBefore = new Majorsilence.Forms.CheckBox();
			this.bGroups = new Majorsilence.Forms.Button();
			this.label1 = new Majorsilence.Forms.Label();
			this.tbNoRows = new Majorsilence.Forms.TextBox();
			this.tbDataInstanceName = new Majorsilence.Forms.TextBox();
			this.chkXmlInstances = new Majorsilence.Forms.CheckBox();
			this.label3 = new Majorsilence.Forms.Label();
			this.groupBox2 = new Majorsilence.Forms.GroupBox();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
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
			// bGroups
			// 
			resources.ApplyResources(this.bGroups, "bGroups");
			this.bGroups.Name = "bGroups";
			this.bGroups.Click += this.bGroups_Click;
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
			// tbDataInstanceName
			// 
			resources.ApplyResources(this.tbDataInstanceName, "tbDataInstanceName");
			this.tbDataInstanceName.Name = "tbDataInstanceName";
			this.tbDataInstanceName.TextChanged += this.tbDataInstanceName_TextChanged;
			// 
			// chkXmlInstances
			// 
			resources.ApplyResources(this.chkXmlInstances, "chkXmlInstances");
			this.chkXmlInstances.Name = "chkXmlInstances";
			this.chkXmlInstances.CheckedChanged += this.chkXmlInstances_CheckedChanged;
			// 
			// label3
			// 
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			// 
			// groupBox2
			// 
			resources.ApplyResources(this.groupBox2, "groupBox2");
			this.groupBox2.Controls.Add(this.tbDataInstanceName);
			this.groupBox2.Controls.Add(this.chkXmlInstances);
			this.groupBox2.Controls.Add(this.label3);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.TabStop = false;
			// 
			// ListCtl
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.tbNoRows);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.bGroups);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.cbDataSet);
			this.Controls.Add(this.label2);
			this.Name = "ListCtl";
			this.groupBox1.ResumeLayout(false);
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		public bool IsValid()
		{
			return true;
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
			fNoRows = fDataSet = fPBBefore = fPBAfter= fDataInstanceElementOutput = fDataInstanceName = false;
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
			if (fDataInstanceElementOutput)
				_Draw.SetElement(node, "DataInstanceElementOutput", this.chkXmlInstances.Checked? "Output":"NoOutput");
			if (fDataInstanceName)
			{
				if (this.tbDataInstanceName.Text.Length > 0)
					_Draw.SetElement(node, "DataInstanceName", this.tbDataInstanceName.Text);
				else
					_Draw.RemoveElement(node, "DataInstanceName");
			}
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

		private void bGroups_Click(object sender, System.EventArgs e)
		{
			PropertyDialog pd = new PropertyDialog(_Draw, _ReportItems, PropertyTypeEnum.Grouping);
            try
            {
                DialogResult dr = pd.ShowDialog();
                if (pd.Changed || dr == DialogResult.OK)
                {
                    //				_DrawPanel.Invalidate();   TODO need to force change somehow?????
                }
            }
            finally
            {
                pd.Dispose();
            }
		}

		private void chkXmlInstances_CheckedChanged(object sender, System.EventArgs e)
		{
			fDataInstanceElementOutput = true;
		}

		private void tbDataInstanceName_TextChanged(object sender, System.EventArgs e)
		{
			fDataInstanceName = true;
		}
	}
}
