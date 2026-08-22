
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Majorsilence.Forms;
using System.Xml;
using System.IO;

namespace Majorsilence.Reporting.RdlDesign
{
	/// <summary>
	/// Summary description for StyleCtl.
	/// </summary>
	internal class VisibilityCtl : Majorsilence.Forms.UserControl, IProperty
	{
        private List<XmlNode> _ReportItems;
		private DesignXmlDraw _Draw;
		// flags for controlling whether syntax changed for a particular property
        private bool fHidden, fToggle;
        private Majorsilence.Forms.GroupBox grpBoxVisibility;
		private Majorsilence.Forms.Label label2;
		private Majorsilence.Forms.Label label3;
		private Majorsilence.Forms.TextBox tbHidden;
        private Majorsilence.Forms.ComboBox cbToggle;
        private Majorsilence.Forms.Button bHidden;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        internal VisibilityCtl(DesignXmlDraw dxDraw, List<XmlNode> reportItems)
		{
			_ReportItems = reportItems;
			_Draw = dxDraw;
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Initialize form using the style node values
			InitValues(_ReportItems[0]);			
		}

		private void InitValues(XmlNode node)
		{
			
			
			// Handle Visiblity definition
			XmlNode visNode = _Draw.GetNamedChildNode(node, "Visibility");
			if (visNode != null)
			{
				XmlNode hNode = _Draw.GetNamedChildNode(node, "Visibility");
				this.tbHidden.Text = _Draw.GetElementValue(visNode, "Hidden", "");
				this.cbToggle.Text = _Draw.GetElementValue(visNode, "ToggleItem", "");
			}
			IEnumerable list = _Draw.GetReportItems("//Textbox");
			if (list != null)
			{
				foreach (XmlNode tNode in list)
				{
					XmlAttribute name = tNode.Attributes["Name"];
					if (name != null && name.Value != null && name.Value.Length > 0)
						cbToggle.Items.Add(name.Value);
				}
			}
			// nothing has changed now
			fHidden = fToggle = false;
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
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(VisibilityCtl));
            this.DoubleBuffered = true;
			this.grpBoxVisibility = new Majorsilence.Forms.GroupBox();
			this.bHidden = new Majorsilence.Forms.Button();
			this.cbToggle = new Majorsilence.Forms.ComboBox();
			this.tbHidden = new Majorsilence.Forms.TextBox();
			this.label3 = new Majorsilence.Forms.Label();
			this.label2 = new Majorsilence.Forms.Label();
			this.grpBoxVisibility.SuspendLayout();
			this.SuspendLayout();
			// 
			// grpBoxVisibility
			// 
			resources.ApplyResources(this.grpBoxVisibility, "grpBoxVisibility");
			this.grpBoxVisibility.Controls.Add(this.bHidden);
			this.grpBoxVisibility.Controls.Add(this.cbToggle);
			this.grpBoxVisibility.Controls.Add(this.tbHidden);
			this.grpBoxVisibility.Controls.Add(this.label3);
			this.grpBoxVisibility.Controls.Add(this.label2);
			this.grpBoxVisibility.Name = "grpBoxVisibility";
			this.grpBoxVisibility.TabStop = false;
			// 
			// bHidden
			// 
			resources.ApplyResources(this.bHidden, "bHidden");
			this.bHidden.Name = "bHidden";
			this.bHidden.Tag = "visibility";
			this.bHidden.Click += this.bExpr_Click;
			// 
			// cbToggle
			// 
			resources.ApplyResources(this.cbToggle, "cbToggle");
			this.cbToggle.Name = "cbToggle";
			this.cbToggle.SelectedIndexChanged += this.cbToggle_SelectedIndexChanged;
			this.cbToggle.TextChanged += this.cbToggle_SelectedIndexChanged;
			// 
			// tbHidden
			// 
			resources.ApplyResources(this.tbHidden, "tbHidden");
			this.tbHidden.Name = "tbHidden";
			this.tbHidden.TextChanged += this.tbHidden_TextChanged;
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
			// VisibilityCtl
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.grpBoxVisibility);
			this.Name = "VisibilityCtl";
			this.grpBoxVisibility.ResumeLayout(false);
			this.grpBoxVisibility.PerformLayout();
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

			// nothing has changed now
			fHidden = fToggle = false;
		}

		private void ApplyChanges(XmlNode rNode)
		{
			if (fHidden || fToggle)
			{
				XmlNode visNode = _Draw.SetElement(rNode, "Visibility", null);

                if (fHidden)
                {
                    if (tbHidden.Text.Length == 0)
                        _Draw.RemoveElement(visNode, "Hidden");
                    else
                        _Draw.SetElement(visNode, "Hidden", tbHidden.Text);
                }
				if (fToggle)
					_Draw.SetElement(visNode, "ToggleItem", this.cbToggle.Text); 
			}
		}

		private void tbHidden_TextChanged(object sender, System.EventArgs e)
		{
			fHidden = true;
		}

		private void cbToggle_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			fToggle = true;
		}

		private void bExpr_Click(object sender, System.EventArgs e)
		{
			Button b = sender as Button;
			if (b == null)
				return;
			Control c = null;
			switch (b.Tag as string)
			{
				case "visibility":
					c = tbHidden;
					break;
			}

			if (c == null)
				return;

			XmlNode sNode = _ReportItems[0];

            using (DialogExprEditor ee = new DialogExprEditor(_Draw, c.Text, sNode))
            {
                DialogResult dr = ee.ShowDialog();
                if (dr == DialogResult.OK)
                    c.Text = ee.Expression;
                return;
            }
		}

	}
}
