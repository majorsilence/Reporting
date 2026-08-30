
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Majorsilence.Forms.Drawing;
using System.Data;
using Majorsilence.Forms;
using System.Xml;
using Majorsilence.Reporting.RdlDesign.Resources;

namespace Majorsilence.Reporting.RdlDesign
{
	/// <summary>
	/// Summary description for StyleCtl.
	/// </summary>
	internal class StyleCtl : Majorsilence.Forms.UserControl, IProperty
	{
        private List<XmlNode> _ReportItems;
		private DesignXmlDraw _Draw;
		// flags for controlling whether syntax changed for a particular property
		private bool fPadLeft, fPadRight, fPadTop, fPadBottom;
		private bool fEndColor, fBackColor, fGradient, fDEName, fDEOutput;

		private Majorsilence.Forms.Label label11;
		private Majorsilence.Forms.Label label12;
		private Majorsilence.Forms.Label label13;
		private Majorsilence.Forms.Label label14;
		private Majorsilence.Forms.TextBox tbPadLeft;
		private Majorsilence.Forms.TextBox tbPadRight;
		private Majorsilence.Forms.TextBox tbPadTop;
		private Majorsilence.Forms.GroupBox grpBoxPadding;
		private Majorsilence.Forms.GroupBox groupBox1;
		private Majorsilence.Forms.Label label3;
		private Majorsilence.Forms.Button bBackColor;
		private Majorsilence.Forms.Label label10;
		private Majorsilence.Forms.Label label15;
		private Majorsilence.Forms.ComboBox cbEndColor;
		private Majorsilence.Forms.ComboBox cbBackColor;
		private Majorsilence.Forms.Button bEndColor;
		private Majorsilence.Forms.ComboBox cbGradient;
		private Majorsilence.Forms.TextBox tbPadBottom;
		private Majorsilence.Forms.Label label1;
		private Majorsilence.Forms.Label label2;
		private Majorsilence.Forms.TextBox tbDEName;
		private Majorsilence.Forms.ComboBox cbDEOutput;
		private Majorsilence.Forms.GroupBox gbXML;
		private Majorsilence.Forms.Button bValueExpr;
		private Majorsilence.Forms.Button button1;
		private Majorsilence.Forms.Button button2;
		private Majorsilence.Forms.Button button3;
		private Majorsilence.Forms.Button bGradient;
		private Majorsilence.Forms.Button bExprBackColor;
		private Majorsilence.Forms.Button bExprEndColor;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        internal StyleCtl(DesignXmlDraw dxDraw, List<XmlNode> reportItems)
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
            cbEndColor.Items.AddRange(StaticLists.ColorList);
            cbBackColor.Items.AddRange(StaticLists.ColorList);

			XmlNode sNode = _Draw.GetNamedChildNode(node, "Style");

			// Handle padding
			tbPadLeft.Text = _Draw.GetElementValue(sNode, "PaddingLeft", "0pt");
			tbPadRight.Text = _Draw.GetElementValue(sNode, "PaddingRight", "0pt");
			tbPadTop.Text = _Draw.GetElementValue(sNode, "PaddingTop", "0pt");
			tbPadBottom.Text = _Draw.GetElementValue(sNode, "PaddingBottom", "0pt");

			this.cbBackColor.Text = _Draw.GetElementValue(sNode, "BackgroundColor", "");
			this.cbEndColor.Text = _Draw.GetElementValue(sNode, "BackgroundGradientEndColor", "");
			this.cbGradient.Text = _Draw.GetElementValue(sNode, "BackgroundGradientType", "None");
			this.tbDEName.Text = _Draw.GetElementValue(node, "DataElementName", "");
			this.cbDEOutput.Text = _Draw.GetElementValue(node, "DataElementOutput", "Auto");
			if (node.Name != "Chart")
			{   // only chart support gradients
				this.cbEndColor.Enabled = bExprEndColor.Enabled =
					cbGradient.Enabled = bGradient.Enabled = 
					this.bEndColor.Enabled = bExprEndColor.Enabled = false;
			}
			if (node.Name == "Line" || node.Name == "Image")
			{
				gbXML.Visible = false;
			}

			// nothing has changed now
			fPadLeft = fPadRight = fPadTop = fPadBottom =
				fEndColor = fBackColor = fGradient = fDEName = fDEOutput = false;
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
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(StyleCtl));
            this.DoubleBuffered = true;
			this.label11 = new Majorsilence.Forms.Label();
			this.label12 = new Majorsilence.Forms.Label();
			this.label13 = new Majorsilence.Forms.Label();
			this.label14 = new Majorsilence.Forms.Label();
			this.tbPadLeft = new Majorsilence.Forms.TextBox();
			this.tbPadRight = new Majorsilence.Forms.TextBox();
			this.tbPadTop = new Majorsilence.Forms.TextBox();
			this.tbPadBottom = new Majorsilence.Forms.TextBox();
			this.grpBoxPadding = new Majorsilence.Forms.GroupBox();
			this.button3 = new Majorsilence.Forms.Button();
			this.button2 = new Majorsilence.Forms.Button();
			this.button1 = new Majorsilence.Forms.Button();
			this.bValueExpr = new Majorsilence.Forms.Button();
			this.groupBox1 = new Majorsilence.Forms.GroupBox();
			this.bGradient = new Majorsilence.Forms.Button();
			this.bExprBackColor = new Majorsilence.Forms.Button();
			this.bExprEndColor = new Majorsilence.Forms.Button();
			this.bEndColor = new Majorsilence.Forms.Button();
			this.cbBackColor = new Majorsilence.Forms.ComboBox();
			this.cbEndColor = new Majorsilence.Forms.ComboBox();
			this.label15 = new Majorsilence.Forms.Label();
			this.cbGradient = new Majorsilence.Forms.ComboBox();
			this.label10 = new Majorsilence.Forms.Label();
			this.bBackColor = new Majorsilence.Forms.Button();
			this.label3 = new Majorsilence.Forms.Label();
			this.gbXML = new Majorsilence.Forms.GroupBox();
			this.cbDEOutput = new Majorsilence.Forms.ComboBox();
			this.tbDEName = new Majorsilence.Forms.TextBox();
			this.label2 = new Majorsilence.Forms.Label();
			this.label1 = new Majorsilence.Forms.Label();
			this.grpBoxPadding.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.gbXML.SuspendLayout();
			this.SuspendLayout();
			// 
			// label11
			// 
			resources.ApplyResources(this.label11, "label11");
			this.label11.Name = "label11";
			// 
			// label12
			// 
			resources.ApplyResources(this.label12, "label12");
			this.label12.Name = "label12";
			// 
			// label13
			// 
			resources.ApplyResources(this.label13, "label13");
			this.label13.Name = "label13";
			// 
			// label14
			// 
			resources.ApplyResources(this.label14, "label14");
			this.label14.Name = "label14";
			// 
			// tbPadLeft
			// 
			resources.ApplyResources(this.tbPadLeft, "tbPadLeft");
			this.tbPadLeft.Name = "tbPadLeft";
			this.tbPadLeft.TextChanged += this.tbPadLeft_TextChanged;
			// 
			// tbPadRight
			// 
			resources.ApplyResources(this.tbPadRight, "tbPadRight");
			this.tbPadRight.Name = "tbPadRight";
			this.tbPadRight.TextChanged += this.tbPadRight_TextChanged;
			// 
			// tbPadTop
			// 
			resources.ApplyResources(this.tbPadTop, "tbPadTop");
			this.tbPadTop.Name = "tbPadTop";
			this.tbPadTop.TextChanged += this.tbPadTop_TextChanged;
			// 
			// tbPadBottom
			// 
			resources.ApplyResources(this.tbPadBottom, "tbPadBottom");
			this.tbPadBottom.Name = "tbPadBottom";
			this.tbPadBottom.TextChanged += this.tbPadBottom_TextChanged;
			// 
			// grpBoxPadding
			// 
			resources.ApplyResources(this.grpBoxPadding, "grpBoxPadding");
			this.grpBoxPadding.Controls.Add(this.button3);
			this.grpBoxPadding.Controls.Add(this.button2);
			this.grpBoxPadding.Controls.Add(this.button1);
			this.grpBoxPadding.Controls.Add(this.bValueExpr);
			this.grpBoxPadding.Controls.Add(this.label13);
			this.grpBoxPadding.Controls.Add(this.tbPadRight);
			this.grpBoxPadding.Controls.Add(this.label14);
			this.grpBoxPadding.Controls.Add(this.label11);
			this.grpBoxPadding.Controls.Add(this.tbPadBottom);
			this.grpBoxPadding.Controls.Add(this.label12);
			this.grpBoxPadding.Controls.Add(this.tbPadTop);
			this.grpBoxPadding.Controls.Add(this.tbPadLeft);
			this.grpBoxPadding.Name = "grpBoxPadding";
			this.grpBoxPadding.TabStop = false;
			// 
			// button3
			// 
			resources.ApplyResources(this.button3, "button3");
			this.button3.Name = "button3";
			this.button3.Tag = "pright";
			this.button3.Click += this.bExpr_Click;
			// 
			// button2
			// 
			resources.ApplyResources(this.button2, "button2");
			this.button2.Name = "button2";
			this.button2.Tag = "pbottom";
			this.button2.Click += this.bExpr_Click;
			// 
			// button1
			// 
			resources.ApplyResources(this.button1, "button1");
			this.button1.Name = "button1";
			this.button1.Tag = "ptop";
			this.button1.Click += this.bExpr_Click;
			// 
			// bValueExpr
			// 
			resources.ApplyResources(this.bValueExpr, "bValueExpr");
			this.bValueExpr.Name = "bValueExpr";
			this.bValueExpr.Tag = "pleft";
			this.bValueExpr.Click += this.bExpr_Click;
			// 
			// groupBox1
			// 
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.Controls.Add(this.bGradient);
			this.groupBox1.Controls.Add(this.bExprBackColor);
			this.groupBox1.Controls.Add(this.bExprEndColor);
			this.groupBox1.Controls.Add(this.bEndColor);
			this.groupBox1.Controls.Add(this.cbBackColor);
			this.groupBox1.Controls.Add(this.cbEndColor);
			this.groupBox1.Controls.Add(this.label15);
			this.groupBox1.Controls.Add(this.cbGradient);
			this.groupBox1.Controls.Add(this.label10);
			this.groupBox1.Controls.Add(this.bBackColor);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabStop = false;
			// 
			// bGradient
			// 
			resources.ApplyResources(this.bGradient, "bGradient");
			this.bGradient.Name = "bGradient";
			this.bGradient.Tag = "bgradient";
			this.bGradient.Click += this.bExpr_Click;
			// 
			// bExprBackColor
			// 
			resources.ApplyResources(this.bExprBackColor, "bExprBackColor");
			this.bExprBackColor.Name = "bExprBackColor";
			this.bExprBackColor.Tag = "bcolor";
			this.bExprBackColor.Click += this.bExpr_Click;
			// 
			// bExprEndColor
			// 
			resources.ApplyResources(this.bExprEndColor, "bExprEndColor");
			this.bExprEndColor.Name = "bExprEndColor";
			this.bExprEndColor.Tag = "bendcolor";
			this.bExprEndColor.Click += this.bExpr_Click;
			// 
			// bEndColor
			// 
			resources.ApplyResources(this.bEndColor, "bEndColor");
			this.bEndColor.Name = "bEndColor";
			this.bEndColor.Click += this.bColor_Click;
			// 
			// cbBackColor
			// 
			resources.ApplyResources(this.cbBackColor, "cbBackColor");
			this.cbBackColor.Name = "cbBackColor";
			this.cbBackColor.SelectedIndexChanged += this.cbBackColor_SelectedIndexChanged;
			this.cbBackColor.TextChanged += this.cbBackColor_SelectedIndexChanged;
			// 
			// cbEndColor
			// 
			resources.ApplyResources(this.cbEndColor, "cbEndColor");
			this.cbEndColor.Name = "cbEndColor";
			this.cbEndColor.SelectedIndexChanged += this.cbEndColor_SelectedIndexChanged;
			this.cbEndColor.TextChanged += this.cbEndColor_SelectedIndexChanged;
			// 
			// label15
			// 
			resources.ApplyResources(this.label15, "label15");
			this.label15.Name = "label15";
			// 
			// cbGradient
			// 
			resources.ApplyResources(this.cbGradient, "cbGradient");
			this.cbGradient.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbGradient.Items.AddRange(new object[] {
            resources.GetString("cbGradient.Items"),
            resources.GetString("cbGradient.Items1"),
            resources.GetString("cbGradient.Items2"),
            resources.GetString("cbGradient.Items3"),
            resources.GetString("cbGradient.Items4"),
            resources.GetString("cbGradient.Items5"),
            resources.GetString("cbGradient.Items6"),
            resources.GetString("cbGradient.Items7")});
			this.cbGradient.Name = "cbGradient";
			this.cbGradient.SelectedIndexChanged += this.cbGradient_SelectedIndexChanged;
			// 
			// label10
			// 
			resources.ApplyResources(this.label10, "label10");
			this.label10.Name = "label10";
			// 
			// bBackColor
			// 
			resources.ApplyResources(this.bBackColor, "bBackColor");
			this.bBackColor.Name = "bBackColor";
			this.bBackColor.Click += this.bColor_Click;
			// 
			// label3
			// 
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			// 
			// gbXML
			// 
			resources.ApplyResources(this.gbXML, "gbXML");
			this.gbXML.Controls.Add(this.cbDEOutput);
			this.gbXML.Controls.Add(this.tbDEName);
			this.gbXML.Controls.Add(this.label2);
			this.gbXML.Controls.Add(this.label1);
			this.gbXML.Name = "gbXML";
			this.gbXML.TabStop = false;
			// 
			// cbDEOutput
			// 
			resources.ApplyResources(this.cbDEOutput, "cbDEOutput");
			this.cbDEOutput.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbDEOutput.Items.AddRange(new object[] {
            resources.GetString("cbDEOutput.Items"),
            resources.GetString("cbDEOutput.Items1"),
            resources.GetString("cbDEOutput.Items2"),
            resources.GetString("cbDEOutput.Items3")});
			this.cbDEOutput.Name = "cbDEOutput";
			this.cbDEOutput.SelectedIndexChanged += this.cbDEOutput_SelectedIndexChanged;
			// 
			// tbDEName
			// 
			resources.ApplyResources(this.tbDEName, "tbDEName");
			this.tbDEName.Name = "tbDEName";
			this.tbDEName.TextChanged += this.tbDEName_TextChanged;
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
			// StyleCtl
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.grpBoxPadding);
			this.Controls.Add(this.gbXML);
			this.Name = "StyleCtl";
			this.grpBoxPadding.ResumeLayout(false);
			this.grpBoxPadding.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.gbXML.ResumeLayout(false);
			this.gbXML.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion
     
		public bool IsValid()
		{
			string name="";
			try
			{
				if (fPadLeft && !tbPadLeft.Text.StartsWith("="))
				{
					name = Strings.StyleCtl_Show_Left;
					DesignerUtility.ValidateSize(tbPadLeft.Text, true, false);
				}
				
				if (fPadRight && !tbPadRight.Text.StartsWith("="))
				{
					name = Strings.StyleCtl_Show_Right;
					DesignerUtility.ValidateSize(tbPadRight.Text, true, false);
				}
				
				if (fPadTop && !tbPadTop.Text.StartsWith("="))
				{
					name = Strings.StyleCtl_Show_Top;
					DesignerUtility.ValidateSize(tbPadTop.Text, true, false);
				}
				
				if (fPadBottom && !tbPadBottom.Text.StartsWith("="))
				{
					name = Strings.StyleCtl_Show_Bottom;
					DesignerUtility.ValidateSize(tbPadBottom.Text, true, false);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, string.Format(Strings.StyleCtl_Show_PaddingInvalid, name));
				return false;
			}
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
			fPadLeft = fPadRight = fPadTop = fPadBottom =
				fEndColor = fBackColor = fGradient = fDEName = fDEOutput = false;
		}

		private void bFont_Click(object sender, System.EventArgs e)
		{
            using (FontDialog fd = new FontDialog())
            {
                fd.ShowColor = true;
                if (fd.ShowDialog() != DialogResult.OK)
                    return;
            } 
            return;
		}

		private void bColor_Click(object sender, System.EventArgs e)
		{
            using (ColorDialog cd = new ColorDialog())
            {
                cd.AnyColor = true;
                cd.FullOpen = true;
                cd.CustomColors = RdlDesigner.GetCustomColors();

                if (cd.ShowDialog() != DialogResult.OK)
                    return;

                RdlDesigner.SetCustomColors(cd.CustomColors);
                if (sender == this.bEndColor)
                    cbEndColor.Text = Majorsilence.Forms.ColorTranslator.ToHtml(cd.Color);
                else if (sender == this.bBackColor)
                    cbBackColor.Text = Majorsilence.Forms.ColorTranslator.ToHtml(cd.Color);
            }
			return;
		}

		private void cbBackColor_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			fBackColor = true;
		}

		private void cbGradient_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			fGradient = true;
		}

		private void cbEndColor_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			fEndColor = true;
		}

		private void tbPadLeft_TextChanged(object sender, System.EventArgs e)
		{
			fPadLeft = true;
		}

		private void tbPadRight_TextChanged(object sender, System.EventArgs e)
		{
			fPadRight = true;
		}

		private void tbPadTop_TextChanged(object sender, System.EventArgs e)
		{
			fPadTop = true;
		}

		private void tbPadBottom_TextChanged(object sender, System.EventArgs e)
		{
			fPadBottom = true;
		}
		
		private void ApplyChanges(XmlNode rNode)
		{
			XmlNode xNode = _Draw.GetNamedChildNode(rNode, "Style");

			if (fPadLeft)
			{ _Draw.SetElement(xNode, "PaddingLeft", tbPadLeft.Text); }
			if (fPadRight)
			{ _Draw.SetElement(xNode, "PaddingRight", tbPadRight.Text); }
			if (fPadTop)
			{ _Draw.SetElement(xNode, "PaddingTop", tbPadTop.Text); }
			if (fPadBottom)
			{ _Draw.SetElement(xNode, "PaddingBottom", tbPadBottom.Text); }
			if (fEndColor)
			{ _Draw.SetElement(xNode, "BackgroundGradientEndColor", cbEndColor.Text); }
			if (fBackColor)
			{ _Draw.SetElement(xNode, "BackgroundColor", cbBackColor.Text); }
			if (fGradient)
			{ _Draw.SetElement(xNode, "BackgroundGradientType", cbGradient.Text); }
			if (fDEName)
			{ _Draw.SetElement(rNode, "DataElementName", tbDEName.Text); }
			if (fDEOutput)
			{ _Draw.SetElement(rNode, "DataElementOutput", cbDEOutput.Text); }
		}

		private void cbDEOutput_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			fDEOutput = true;
		}

		private void tbDEName_TextChanged(object sender, System.EventArgs e)
		{
			fDEName = true;
		}
		private void bExpr_Click(object sender, System.EventArgs e)
		{
			Button b = sender as Button;
			if (b == null)
				return;
			Control c = null;
			bool bColor=false;
			switch (b.Tag as string)
			{
				case "pleft":
					c = tbPadLeft;
					break;
				case "pright":
					c = tbPadRight;
					break;
				case "ptop":
					c = tbPadTop;
					break;
				case "pbottom":
					c = tbPadBottom;
					break;
				case "bcolor":
					c = cbBackColor;
					bColor = true;
					break;
				case "bgradient":
					c = cbGradient;
					break;
				case "bendcolor":
					c = cbEndColor;
					bColor = true;
					break;
			}

			if (c == null)
				return;

			XmlNode sNode = _ReportItems[0];

            using (DialogExprEditor ee = new DialogExprEditor(_Draw, c.Text, sNode, bColor))
            {
                DialogResult dr = ee.ShowDialog();
                if (dr == DialogResult.OK)
                    c.Text = ee.Expression;

            } 
            return;
		}

	}
}
