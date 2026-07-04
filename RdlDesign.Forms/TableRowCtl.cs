
using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using Majorsilence.Forms;
using System.Xml;
using System.IO;
using Majorsilence.Reporting.RdlDesign.Resources;

namespace Majorsilence.Reporting.RdlDesign
{
	/// <summary>
	/// Summary description for TableRowCtl.
	/// </summary>
	internal class TableRowCtl : Majorsilence.Forms.UserControl, IProperty
	{
		private XmlNode _TableRow;
		private DesignXmlDraw _Draw;
		// flags for controlling whether syntax changed for a particular property
		private bool fHidden, fToggle, fHeight;
		private Majorsilence.Forms.GroupBox grpBoxVisibility;
		private Majorsilence.Forms.Label label2;
		private Majorsilence.Forms.Label label3;
		private Majorsilence.Forms.TextBox tbHidden;
		private Majorsilence.Forms.ComboBox cbToggle;
		private Majorsilence.Forms.Button bHidden;
		private Majorsilence.Forms.Label label1;
		private Majorsilence.Forms.TextBox tbRowHeight;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		internal TableRowCtl(DesignXmlDraw dxDraw, XmlNode tr)
		{
			_TableRow = tr;
			_Draw = dxDraw;
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Initialize form using the style node values
			InitValues(tr);			
		}

		private void InitValues(XmlNode node)
		{
			// Handle Width definition
			this.tbRowHeight.Text = _Draw.GetElementValue(node, "Height", "");
		
			// Handle Visiblity definition
			XmlNode visNode = _Draw.GetNamedChildNode(node, "Visibility");
			if (visNode != null)
			{
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
			fHeight = fHidden = fToggle = false;
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
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(TableRowCtl));
            this.DoubleBuffered = true;
			this.grpBoxVisibility = new Majorsilence.Forms.GroupBox();
			this.bHidden = new Majorsilence.Forms.Button();
			this.cbToggle = new Majorsilence.Forms.ComboBox();
			this.tbHidden = new Majorsilence.Forms.TextBox();
			this.label3 = new Majorsilence.Forms.Label();
			this.label2 = new Majorsilence.Forms.Label();
			this.label1 = new Majorsilence.Forms.Label();
			this.tbRowHeight = new Majorsilence.Forms.TextBox();
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
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// tbRowHeight
			// 
			resources.ApplyResources(this.tbRowHeight, "tbRowHeight");
			this.tbRowHeight.Name = "tbRowHeight";
			this.tbRowHeight.TextChanged += this.tbRowHeight_TextChanged;
			// 
			// TableRowCtl
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.tbRowHeight);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.grpBoxVisibility);
			this.Name = "TableRowCtl";
			this.grpBoxVisibility.ResumeLayout(false);
			this.grpBoxVisibility.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
       
		public bool IsValid()
		{
			try
			{
				if (fHeight)
					DesignerUtility.ValidateSize(this.tbRowHeight.Text, true, false);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Strings.TableRowCtl_Show_HeightInvalid);
				return false;
			}

			if (fHidden)
			{
				string vh = this.tbHidden.Text.Trim();
				if (vh.Length > 0)
				{
					if (vh.StartsWith("="))
					{}
					else
					{ 
						vh = vh.ToLower();
						switch (vh)
						{
							case "true":
							case "false":
								break;
							default:
								MessageBox.Show(String.Format(Strings.TableRowCtl_Show_ExpressionTrueFalse, tbHidden.Text), Strings.TableRowCtl_Show_HiddenInvalid);
								return false;
						}
					}

				}
			}

			return true;
		}

		public void Apply()
		{
			// take information in control and apply to all the style nodes
			//  Only change information that has been marked as modified;
			//   this way when group is selected it is possible to change just
			//   the items you want and keep the rest the same.

			ApplyChanges(this._TableRow);

			// nothing has changed now
			fHeight = fHidden = fToggle = false;
		}

		private void ApplyChanges(XmlNode rNode)
		{
			if (fHidden || fToggle)
			{
				XmlNode visNode = _Draw.SetElement(rNode, "Visibility", null);

				if (fHidden)
				{
					string vh = this.tbHidden.Text.Trim();
					if (vh.Length > 0)
						_Draw.SetElement(visNode, "Hidden", vh); 
					else
						_Draw.RemoveElement(visNode, "Hidden");

				}
				if (fToggle)
					_Draw.SetElement(visNode, "ToggleItem", this.cbToggle.Text); 
			}

			if (fHeight)	// already validated
				_Draw.SetElement(rNode, "Height", this.tbRowHeight.Text); 
		}

		private void tbHidden_TextChanged(object sender, System.EventArgs e)
		{
			fHidden = true;
		}

		private void tbRowHeight_TextChanged(object sender, System.EventArgs e)
		{
			fHeight = true;
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

            using (DialogExprEditor ee = new DialogExprEditor(_Draw, c.Text, _TableRow))
            {
                DialogResult dr = ee.ShowDialog();
                if (dr == DialogResult.OK)
                    c.Text = ee.Expression;
                return;
            }
		}
	}
}
