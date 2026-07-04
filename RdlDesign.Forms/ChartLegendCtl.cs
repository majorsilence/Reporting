
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
	/// Summary description for ChartCtl.
	/// </summary>
	internal class ChartLegendCtl : Majorsilence.Forms.UserControl, IProperty
	{
        private List<XmlNode> _ReportItems;
		private DesignXmlDraw _Draw;
		bool fVisible, fLayout, fPosition, fInsidePlotArea;
		private Majorsilence.Forms.Label label1;
		private Majorsilence.Forms.Label label2;
		private Majorsilence.Forms.ComboBox cbPosition;
		private Majorsilence.Forms.ComboBox cbLayout;
		private Majorsilence.Forms.CheckBox chkVisible;
		private Majorsilence.Forms.CheckBox chkInsidePlotArea;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        internal ChartLegendCtl(DesignXmlDraw dxDraw, List<XmlNode> ris)
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
			XmlNode node = _ReportItems[0];

			this.cbPosition.Text = _Draw.GetElementValue(node, "Position", "RightTop");
			this.cbLayout.Text = _Draw.GetElementValue(node, "Layout", "Column");
			this.chkVisible.Checked = _Draw.GetElementValue(node, "Visible", "false").ToLower() == "true"? true: false;
			this.chkInsidePlotArea.Checked = _Draw.GetElementValue(node, "InsidePlotArea", "false").ToLower() == "true"? true: false;

			fVisible = fLayout = fPosition = fInsidePlotArea = false;
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
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(ChartLegendCtl));
            this.DoubleBuffered = true;
			this.label1 = new Majorsilence.Forms.Label();
			this.label2 = new Majorsilence.Forms.Label();
			this.cbPosition = new Majorsilence.Forms.ComboBox();
			this.cbLayout = new Majorsilence.Forms.ComboBox();
			this.chkVisible = new Majorsilence.Forms.CheckBox();
			this.chkInsidePlotArea = new Majorsilence.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// label2
			// 
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			// 
			// cbPosition
			// 
			resources.ApplyResources(this.cbPosition, "cbPosition");
			this.cbPosition.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbPosition.Items.AddRange(new object[] {
            resources.GetString("cbPosition.Items"),
            resources.GetString("cbPosition.Items1"),
            resources.GetString("cbPosition.Items2"),
            resources.GetString("cbPosition.Items3"),
            resources.GetString("cbPosition.Items4"),
            resources.GetString("cbPosition.Items5"),
            resources.GetString("cbPosition.Items6"),
            resources.GetString("cbPosition.Items7"),
            resources.GetString("cbPosition.Items8"),
            resources.GetString("cbPosition.Items9"),
            resources.GetString("cbPosition.Items10"),
            resources.GetString("cbPosition.Items11")});
			this.cbPosition.Name = "cbPosition";
			this.cbPosition.SelectedIndexChanged += this.cbPosition_SelectedIndexChanged;
			// 
			// cbLayout
			// 
			resources.ApplyResources(this.cbLayout, "cbLayout");
			this.cbLayout.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbLayout.Items.AddRange(new object[] {
            resources.GetString("cbLayout.Items"),
            resources.GetString("cbLayout.Items1"),
            resources.GetString("cbLayout.Items2")});
			this.cbLayout.Name = "cbLayout";
			this.cbLayout.SelectedIndexChanged += this.cbLayout_SelectedIndexChanged;
			// 
			// chkVisible
			// 
			resources.ApplyResources(this.chkVisible, "chkVisible");
			this.chkVisible.Name = "chkVisible";
			this.chkVisible.CheckedChanged += this.chkVisible_CheckedChanged;
			// 
			// chkInsidePlotArea
			// 
			resources.ApplyResources(this.chkInsidePlotArea, "chkInsidePlotArea");
			this.chkInsidePlotArea.Name = "chkInsidePlotArea";
			this.chkInsidePlotArea.CheckedChanged += this.chkInsidePlotArea_CheckedChanged;
			// 
			// ChartLegendCtl
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.chkInsidePlotArea);
			this.Controls.Add(this.chkVisible);
			this.Controls.Add(this.cbLayout);
			this.Controls.Add(this.cbPosition);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Name = "ChartLegendCtl";
			this.ResumeLayout(false);

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
			fVisible = fLayout = fPosition = fInsidePlotArea = false;
		}

		public void ApplyChanges(XmlNode node)
		{
			if (fVisible)
			{
				_Draw.SetElement(node, "Visible", this.chkVisible.Checked? "true": "false");
			}
			if (fLayout)
			{
				_Draw.SetElement(node, "Layout", this.cbLayout.Text);
			}
			if (fPosition)
			{
				_Draw.SetElement(node, "Position", this.cbPosition.Text);
			}
			if (fInsidePlotArea)
			{
				_Draw.SetElement(node, "InsidePlotArea", this.chkInsidePlotArea.Checked? "true": "false");
			}
		}

		private void cbPosition_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			fPosition = true;
		}

		private void cbLayout_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			fLayout = true;
		}

		private void chkVisible_CheckedChanged(object sender, System.EventArgs e)
		{
			fVisible = true;
		}

		private void chkInsidePlotArea_CheckedChanged(object sender, System.EventArgs e)
		{
			fInsidePlotArea = true;
		}

	}
}
