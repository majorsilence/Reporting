
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
	/// Summary description for TableColumnCtl.
	/// </summary>
	internal class TableColumnCtl : Majorsilence.Forms.UserControl, IProperty
	{
		private XmlNode _TableColumn;
		private DesignXmlDraw _Draw;
		// flags for controlling whether syntax changed for a particular property
		private bool fHidden, fToggle, fWidth, fFixedHeader;
		private Majorsilence.Forms.GroupBox grpBoxVisibility;
		private Majorsilence.Forms.Label label2;
		private Majorsilence.Forms.Label label3;
		private Majorsilence.Forms.TextBox tbHidden;
		private Majorsilence.Forms.ComboBox cbToggle;
		private Majorsilence.Forms.Button bHidden;
		private Majorsilence.Forms.Label label1;
		private Majorsilence.Forms.TextBox tbColumnWidth;
		private Majorsilence.Forms.Label label4;
		private Majorsilence.Forms.CheckBox chkFixedHeader;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public TableColumnCtl(DesignXmlDraw dxDraw, XmlNode tc)
		{
			_TableColumn = tc;
			_Draw = dxDraw;
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Initialize form using the style node values
			InitValues(tc);			
		}

		private void InitValues(XmlNode node)
		{
			// Handle Width definition
			this.tbColumnWidth.Text = _Draw.GetElementValue(node, "Width", "");
		
			this.chkFixedHeader.Checked = _Draw.GetElementValue(node, "FixedHeader", "false").ToLower() == "true"? true: false;

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
			fWidth = fHidden = fToggle = fFixedHeader = false;
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
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(TableColumnCtl));
            this.DoubleBuffered = true;
			this.grpBoxVisibility = new Majorsilence.Forms.GroupBox();
			this.bHidden = new Majorsilence.Forms.Button();
			this.cbToggle = new Majorsilence.Forms.ComboBox();
			this.tbHidden = new Majorsilence.Forms.TextBox();
			this.label3 = new Majorsilence.Forms.Label();
			this.label2 = new Majorsilence.Forms.Label();
			this.label1 = new Majorsilence.Forms.Label();
			this.tbColumnWidth = new Majorsilence.Forms.TextBox();
			this.chkFixedHeader = new Majorsilence.Forms.CheckBox();
			this.label4 = new Majorsilence.Forms.Label();
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
			// tbColumnWidth
			// 
			resources.ApplyResources(this.tbColumnWidth, "tbColumnWidth");
			this.tbColumnWidth.Name = "tbColumnWidth";
			this.tbColumnWidth.TextChanged += this.tbWidth_TextChanged;
			// 
			// chkFixedHeader
			// 
			resources.ApplyResources(this.chkFixedHeader, "chkFixedHeader");
			this.chkFixedHeader.Name = "chkFixedHeader";
			this.chkFixedHeader.CheckedChanged += this.cbFixedHeader_CheckedChanged;
			// 
			// label4
			// 
			resources.ApplyResources(this.label4, "label4");
			this.label4.Name = "label4";
			// 
			// TableColumnCtl
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.label4);
			this.Controls.Add(this.chkFixedHeader);
			this.Controls.Add(this.tbColumnWidth);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.grpBoxVisibility);
			this.Name = "TableColumnCtl";
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
				if (fWidth)
					DesignerUtility.ValidateSize(this.tbColumnWidth.Text, true, false);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Strings.TableColumnCtl_Show_WidthInvalid);
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
								MessageBox.Show(String.Format(Strings.TableColumnCtl_Show_ExpressionTrueFalse, tbHidden.Text), Strings.TableColumnCtl_Show_HiddenInvalid);
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

			ApplyChanges(this._TableColumn);

			// nothing has changed now
			fWidth = fHidden = fToggle = false;
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

			if (fWidth)	// already validated
				_Draw.SetElement(rNode, "Width", this.tbColumnWidth.Text); 

			if (fFixedHeader)
			{
				if (this.chkFixedHeader.Checked)
					_Draw.SetElement(rNode, "FixedHeader", "true");
				else
					_Draw.RemoveElement(rNode, "FixedHeader");		// just get rid of it
			}
		}

		private void tbHidden_TextChanged(object sender, System.EventArgs e)
		{
			fHidden = true;
		}

		private void tbWidth_TextChanged(object sender, System.EventArgs e)
		{
			fWidth = true;
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

            using (DialogExprEditor ee = new DialogExprEditor(_Draw, c.Text, _TableColumn))
            {
                DialogResult dr = ee.ShowDialog();
                if (dr == DialogResult.OK)
                    c.Text = ee.Expression;
            }
            return;
		}

		private void cbFixedHeader_CheckedChanged(object sender, System.EventArgs e)
		{
			fFixedHeader = true;
		}

	}
}
