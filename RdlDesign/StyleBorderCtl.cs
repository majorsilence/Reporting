
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Majorsilence.Forms;
using System.Xml;
using System.Globalization;
using Majorsilence.Reporting.RdlDesign.Resources;

namespace Majorsilence.Reporting.RdlDesign
{
	/// <summary>
	/// Summary description for StyleCtl.
	/// </summary>
	internal class StyleBorderCtl : Majorsilence.Forms.UserControl, IProperty
	{
        private List<XmlNode> _ReportItems;
		private DesignXmlDraw _Draw;
		// flags for controlling whether syntax changed for a particular property
		private bool fStyleDefault, fStyleLeft, fStyleRight, fStyleTop, fStyleBottom;
		private bool fColorDefault, fColorLeft, fColorRight, fColorTop, fColorBottom;
		private bool fWidthDefault, fWidthLeft, fWidthRight, fWidthTop, fWidthBottom;
		private Majorsilence.Forms.Label label2;
		private Majorsilence.Forms.Label label6;
		private Majorsilence.Forms.Label label7;
		private Majorsilence.Forms.ComboBox cbStyleLeft;
		private Majorsilence.Forms.Label label8;
		private Majorsilence.Forms.ComboBox cbStyleBottom;
		private Majorsilence.Forms.ComboBox cbStyleTop;
		private Majorsilence.Forms.ComboBox cbStyleRight;
		private Majorsilence.Forms.Button bColorLeft;
		private Majorsilence.Forms.ComboBox cbColorLeft;
		private Majorsilence.Forms.Button bColorRight;
		private Majorsilence.Forms.ComboBox cbColorRight;
		private Majorsilence.Forms.Button bColorTop;
		private Majorsilence.Forms.ComboBox cbColorTop;
		private Majorsilence.Forms.Button bColorBottom;
		private Majorsilence.Forms.ComboBox cbColorBottom;
		private Majorsilence.Forms.TextBox tbWidthLeft;
		private Majorsilence.Forms.TextBox tbWidthRight;
		private Majorsilence.Forms.TextBox tbWidthTop;
		private Majorsilence.Forms.TextBox tbWidthBottom;
		private Majorsilence.Forms.TextBox tbWidthDefault;
		private Majorsilence.Forms.Button bColorDefault;
		private Majorsilence.Forms.ComboBox cbColorDefault;
		private Majorsilence.Forms.ComboBox cbStyleDefault;
		private Majorsilence.Forms.Label lLeft;
		private Majorsilence.Forms.Label lBottom;
		private Majorsilence.Forms.Label lTop;
		private Majorsilence.Forms.Label lRight;
		private Majorsilence.Forms.Button bSD;
		private Majorsilence.Forms.Button bSL;
		private Majorsilence.Forms.Button bSR;
		private Majorsilence.Forms.Button bST;
		private Majorsilence.Forms.Button bSB;
		private Majorsilence.Forms.Button bCD;
		private Majorsilence.Forms.Button bCT;
		private Majorsilence.Forms.Button bCB;
		private Majorsilence.Forms.Button bWB;
		private Majorsilence.Forms.Button bWT;
		private Majorsilence.Forms.Button bWR;
		private Majorsilence.Forms.Button bCR;
		private Majorsilence.Forms.Button bWL;
		private Majorsilence.Forms.Button bWD;
		private Majorsilence.Forms.Button bCL;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private string[] _names;

        public StyleBorderCtl(DesignXmlDraw dxDraw, string[] names, List<XmlNode> reportItems)
		{
			_ReportItems = reportItems;
			_Draw = dxDraw;
            _names = names;
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Initialize form using the style node values
			InitBorders(reportItems[0]);			
		}

		private void InitBorders(XmlNode node)
		{
            cbColorDefault.Items.AddRange(StaticLists.ColorList);
            cbColorLeft.Items.AddRange(StaticLists.ColorList);
            cbColorRight.Items.AddRange(StaticLists.ColorList);
            cbColorTop.Items.AddRange(StaticLists.ColorList);
            cbColorBottom.Items.AddRange(StaticLists.ColorList);

            if (_names != null)
            {
                node = _Draw.FindCreateNextInHierarchy(node, _names);
            }

            XmlNode sNode = _Draw.GetCreateNamedChildNode(node, "Style");

			// Handle BorderStyle
			XmlNode bsNode = _Draw.SetElement(sNode, "BorderStyle", null);
			cbStyleDefault.Text = _Draw.GetElementValue(bsNode, "Default", "None");
			cbStyleLeft.Text = _Draw.GetElementValue(bsNode, "Left", cbStyleDefault.Text);
			cbStyleRight.Text = _Draw.GetElementValue(bsNode, "Right", cbStyleDefault.Text);
			cbStyleTop.Text = _Draw.GetElementValue(bsNode, "Top", cbStyleDefault.Text);
			cbStyleBottom.Text = _Draw.GetElementValue(bsNode, "Bottom", cbStyleDefault.Text);

			// Handle BorderColor
			XmlNode bcNode = _Draw.SetElement(sNode, "BorderColor", null);
			cbColorDefault.Text = _Draw.GetElementValue(bcNode, "Default", "Black");
			cbColorLeft.Text = _Draw.GetElementValue(bcNode, "Left", cbColorDefault.Text);
			cbColorRight.Text = _Draw.GetElementValue(bcNode, "Right", cbColorDefault.Text);
			cbColorTop.Text = _Draw.GetElementValue(bcNode, "Top", cbColorDefault.Text);
			cbColorBottom.Text = _Draw.GetElementValue(bcNode, "Bottom", cbColorDefault.Text);

			// Handle BorderWidth
			XmlNode bwNode = _Draw.SetElement(sNode, "BorderWidth", null);
			tbWidthDefault.Text = _Draw.GetElementValue(bwNode, "Default", "1pt");
			tbWidthLeft.Text = _Draw.GetElementValue(bwNode, "Left", tbWidthDefault.Text);
			tbWidthRight.Text = _Draw.GetElementValue(bwNode, "Right", tbWidthDefault.Text);
			tbWidthTop.Text = _Draw.GetElementValue(bwNode, "Top", tbWidthDefault.Text);
			tbWidthBottom.Text = _Draw.GetElementValue(bwNode, "Bottom", tbWidthDefault.Text);
		
			if (node.Name == "Line")
			{
				cbColorLeft.Visible =
					cbColorRight.Visible =
					cbColorTop.Visible =
					cbColorBottom.Visible =
					bColorLeft.Visible =
					bColorRight.Visible =
					bColorTop.Visible =
					bColorBottom.Visible =
					cbStyleLeft.Visible =
					cbStyleRight.Visible =
					cbStyleTop.Visible =
					cbStyleBottom.Visible =
					lLeft.Visible =
					lRight.Visible =
					lTop.Visible =
					lBottom.Visible =
					tbWidthLeft.Visible =
					tbWidthRight.Visible =
					tbWidthTop.Visible =
					tbWidthBottom.Visible = 
					bCR.Visible = bCL.Visible = bCT.Visible = bCB.Visible =
					bSR.Visible = bSL.Visible = bST.Visible = bSB.Visible =
					bWR.Visible = bWL.Visible = bWT.Visible = bWB.Visible =
					false;
			}
			fStyleDefault = fStyleLeft = fStyleRight = fStyleTop = fStyleBottom =
				fColorDefault = fColorLeft = fColorRight = fColorTop = fColorBottom =
				fWidthDefault = fWidthLeft = fWidthRight = fWidthTop = fWidthBottom= false;
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
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(StyleBorderCtl));
			this.lLeft = new Majorsilence.Forms.Label();
			this.lBottom = new Majorsilence.Forms.Label();
			this.lTop = new Majorsilence.Forms.Label();
			this.lRight = new Majorsilence.Forms.Label();
			this.label2 = new Majorsilence.Forms.Label();
			this.label6 = new Majorsilence.Forms.Label();
			this.label7 = new Majorsilence.Forms.Label();
			this.cbStyleLeft = new Majorsilence.Forms.ComboBox();
			this.cbStyleBottom = new Majorsilence.Forms.ComboBox();
			this.cbStyleTop = new Majorsilence.Forms.ComboBox();
			this.cbStyleRight = new Majorsilence.Forms.ComboBox();
			this.bColorLeft = new Majorsilence.Forms.Button();
			this.cbColorLeft = new Majorsilence.Forms.ComboBox();
			this.bColorRight = new Majorsilence.Forms.Button();
			this.cbColorRight = new Majorsilence.Forms.ComboBox();
			this.bColorTop = new Majorsilence.Forms.Button();
			this.cbColorTop = new Majorsilence.Forms.ComboBox();
			this.bColorBottom = new Majorsilence.Forms.Button();
			this.cbColorBottom = new Majorsilence.Forms.ComboBox();
			this.tbWidthLeft = new Majorsilence.Forms.TextBox();
			this.tbWidthRight = new Majorsilence.Forms.TextBox();
			this.tbWidthTop = new Majorsilence.Forms.TextBox();
			this.tbWidthBottom = new Majorsilence.Forms.TextBox();
			this.tbWidthDefault = new Majorsilence.Forms.TextBox();
			this.bColorDefault = new Majorsilence.Forms.Button();
			this.cbColorDefault = new Majorsilence.Forms.ComboBox();
			this.cbStyleDefault = new Majorsilence.Forms.ComboBox();
			this.label8 = new Majorsilence.Forms.Label();
			this.bSD = new Majorsilence.Forms.Button();
			this.bSL = new Majorsilence.Forms.Button();
			this.bSR = new Majorsilence.Forms.Button();
			this.bST = new Majorsilence.Forms.Button();
			this.bSB = new Majorsilence.Forms.Button();
			this.bCD = new Majorsilence.Forms.Button();
			this.bCT = new Majorsilence.Forms.Button();
			this.bCB = new Majorsilence.Forms.Button();
			this.bWB = new Majorsilence.Forms.Button();
			this.bWT = new Majorsilence.Forms.Button();
			this.bWR = new Majorsilence.Forms.Button();
			this.bCR = new Majorsilence.Forms.Button();
			this.bWL = new Majorsilence.Forms.Button();
			this.bWD = new Majorsilence.Forms.Button();
			this.bCL = new Majorsilence.Forms.Button();
			this.SuspendLayout();
			// 
			// lLeft
			// 
			resources.ApplyResources(this.lLeft, "lLeft");
			this.lLeft.Name = "lLeft";
			// 
			// lBottom
			// 
			resources.ApplyResources(this.lBottom, "lBottom");
			this.lBottom.Name = "lBottom";
			// 
			// lTop
			// 
			resources.ApplyResources(this.lTop, "lTop");
			this.lTop.Name = "lTop";
			// 
			// lRight
			// 
			resources.ApplyResources(this.lRight, "lRight");
			this.lRight.Name = "lRight";
			// 
			// label2
			// 
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			// 
			// label6
			// 
			resources.ApplyResources(this.label6, "label6");
			this.label6.Name = "label6";
			// 
			// label7
			// 
			resources.ApplyResources(this.label7, "label7");
			this.label7.Name = "label7";
			// 
			// cbStyleLeft
			// 
			resources.ApplyResources(this.cbStyleLeft, "cbStyleLeft");
			this.cbStyleLeft.Items.AddRange(new object[] {
            resources.GetString("cbStyleLeft.Items"),
            resources.GetString("cbStyleLeft.Items1"),
            resources.GetString("cbStyleLeft.Items2"),
            resources.GetString("cbStyleLeft.Items3"),
            resources.GetString("cbStyleLeft.Items4"),
            resources.GetString("cbStyleLeft.Items5"),
            resources.GetString("cbStyleLeft.Items6"),
            resources.GetString("cbStyleLeft.Items7"),
            resources.GetString("cbStyleLeft.Items8"),
            resources.GetString("cbStyleLeft.Items9")});
			this.cbStyleLeft.Name = "cbStyleLeft";
			this.cbStyleLeft.SelectedIndexChanged += this.cbStyle_SelectedIndexChanged;
			// 
			// cbStyleBottom
			// 
			resources.ApplyResources(this.cbStyleBottom, "cbStyleBottom");
			this.cbStyleBottom.Items.AddRange(new object[] {
            resources.GetString("cbStyleBottom.Items"),
            resources.GetString("cbStyleBottom.Items1"),
            resources.GetString("cbStyleBottom.Items2"),
            resources.GetString("cbStyleBottom.Items3"),
            resources.GetString("cbStyleBottom.Items4"),
            resources.GetString("cbStyleBottom.Items5"),
            resources.GetString("cbStyleBottom.Items6"),
            resources.GetString("cbStyleBottom.Items7"),
            resources.GetString("cbStyleBottom.Items8"),
            resources.GetString("cbStyleBottom.Items9")});
			this.cbStyleBottom.Name = "cbStyleBottom";
			this.cbStyleBottom.SelectedIndexChanged += this.cbStyle_SelectedIndexChanged;
			// 
			// cbStyleTop
			// 
			resources.ApplyResources(this.cbStyleTop, "cbStyleTop");
			this.cbStyleTop.Items.AddRange(new object[] {
            resources.GetString("cbStyleTop.Items"),
            resources.GetString("cbStyleTop.Items1"),
            resources.GetString("cbStyleTop.Items2"),
            resources.GetString("cbStyleTop.Items3"),
            resources.GetString("cbStyleTop.Items4"),
            resources.GetString("cbStyleTop.Items5"),
            resources.GetString("cbStyleTop.Items6"),
            resources.GetString("cbStyleTop.Items7"),
            resources.GetString("cbStyleTop.Items8"),
            resources.GetString("cbStyleTop.Items9")});
			this.cbStyleTop.Name = "cbStyleTop";
			this.cbStyleTop.SelectedIndexChanged += this.cbStyle_SelectedIndexChanged;
			// 
			// cbStyleRight
			// 
			resources.ApplyResources(this.cbStyleRight, "cbStyleRight");
			this.cbStyleRight.Items.AddRange(new object[] {
            resources.GetString("cbStyleRight.Items"),
            resources.GetString("cbStyleRight.Items1"),
            resources.GetString("cbStyleRight.Items2"),
            resources.GetString("cbStyleRight.Items3"),
            resources.GetString("cbStyleRight.Items4"),
            resources.GetString("cbStyleRight.Items5"),
            resources.GetString("cbStyleRight.Items6"),
            resources.GetString("cbStyleRight.Items7"),
            resources.GetString("cbStyleRight.Items8"),
            resources.GetString("cbStyleRight.Items9")});
			this.cbStyleRight.Name = "cbStyleRight";
			this.cbStyleRight.SelectedIndexChanged += this.cbStyle_SelectedIndexChanged;
			// 
			// bColorLeft
			// 
			resources.ApplyResources(this.bColorLeft, "bColorLeft");
			this.bColorLeft.Name = "bColorLeft";
			this.bColorLeft.Click += this.bColor_Click;
			// 
			// cbColorLeft
			// 
			resources.ApplyResources(this.cbColorLeft, "cbColorLeft");
			this.cbColorLeft.Name = "cbColorLeft";
			this.cbColorLeft.SelectedIndexChanged += this.cbColor_SelectedIndexChanged;
			// 
			// bColorRight
			// 
			resources.ApplyResources(this.bColorRight, "bColorRight");
			this.bColorRight.Name = "bColorRight";
			this.bColorRight.Click += this.bColor_Click;
			// 
			// cbColorRight
			// 
			resources.ApplyResources(this.cbColorRight, "cbColorRight");
			this.cbColorRight.Name = "cbColorRight";
			this.cbColorRight.SelectedIndexChanged += this.cbColor_SelectedIndexChanged;
			// 
			// bColorTop
			// 
			resources.ApplyResources(this.bColorTop, "bColorTop");
			this.bColorTop.Name = "bColorTop";
			this.bColorTop.Click += this.bColor_Click;
			// 
			// cbColorTop
			// 
			resources.ApplyResources(this.cbColorTop, "cbColorTop");
			this.cbColorTop.Name = "cbColorTop";
			this.cbColorTop.SelectedIndexChanged += this.cbColor_SelectedIndexChanged;
			// 
			// bColorBottom
			// 
			resources.ApplyResources(this.bColorBottom, "bColorBottom");
			this.bColorBottom.Name = "bColorBottom";
			this.bColorBottom.Click += this.bColor_Click;
			// 
			// cbColorBottom
			// 
			resources.ApplyResources(this.cbColorBottom, "cbColorBottom");
			this.cbColorBottom.Name = "cbColorBottom";
			this.cbColorBottom.SelectedIndexChanged += this.cbColor_SelectedIndexChanged;
			// 
			// tbWidthLeft
			// 
			resources.ApplyResources(this.tbWidthLeft, "tbWidthLeft");
			this.tbWidthLeft.Name = "tbWidthLeft";
			this.tbWidthLeft.TextChanged += this.tbWidth_Changed;
			// 
			// tbWidthRight
			// 
			resources.ApplyResources(this.tbWidthRight, "tbWidthRight");
			this.tbWidthRight.Name = "tbWidthRight";
			this.tbWidthRight.TextChanged += this.tbWidth_Changed;
			// 
			// tbWidthTop
			// 
			resources.ApplyResources(this.tbWidthTop, "tbWidthTop");
			this.tbWidthTop.Name = "tbWidthTop";
			this.tbWidthTop.TextChanged += this.tbWidth_Changed;
			// 
			// tbWidthBottom
			// 
			resources.ApplyResources(this.tbWidthBottom, "tbWidthBottom");
			this.tbWidthBottom.Name = "tbWidthBottom";
			this.tbWidthBottom.TextChanged += this.tbWidth_Changed;
			// 
			// tbWidthDefault
			// 
			resources.ApplyResources(this.tbWidthDefault, "tbWidthDefault");
			this.tbWidthDefault.Name = "tbWidthDefault";
			this.tbWidthDefault.TextChanged += this.tbWidthDefault_TextChanged;
			// 
			// bColorDefault
			// 
			resources.ApplyResources(this.bColorDefault, "bColorDefault");
			this.bColorDefault.Name = "bColorDefault";
			this.bColorDefault.Click += this.bColor_Click;
			// 
			// cbColorDefault
			// 
			resources.ApplyResources(this.cbColorDefault, "cbColorDefault");
			this.cbColorDefault.Name = "cbColorDefault";
			this.cbColorDefault.SelectedIndexChanged += this.cbColorDefault_SelectedIndexChanged;
			// 
			// cbStyleDefault
			// 
			resources.ApplyResources(this.cbStyleDefault, "cbStyleDefault");
			this.cbStyleDefault.Items.AddRange(new object[] {
            resources.GetString("cbStyleDefault.Items"),
            resources.GetString("cbStyleDefault.Items1"),
            resources.GetString("cbStyleDefault.Items2"),
            resources.GetString("cbStyleDefault.Items3"),
            resources.GetString("cbStyleDefault.Items4"),
            resources.GetString("cbStyleDefault.Items5"),
            resources.GetString("cbStyleDefault.Items6"),
            resources.GetString("cbStyleDefault.Items7"),
            resources.GetString("cbStyleDefault.Items8"),
            resources.GetString("cbStyleDefault.Items9")});
			this.cbStyleDefault.Name = "cbStyleDefault";
			this.cbStyleDefault.SelectedIndexChanged += this.cbStyleDefault_SelectedIndexChanged;
			// 
			// label8
			// 
			resources.ApplyResources(this.label8, "label8");
			this.label8.Name = "label8";
			// 
			// bSD
			// 
			resources.ApplyResources(this.bSD, "bSD");
			this.bSD.Name = "bSD";
			this.bSD.Tag = "sd";
			this.bSD.Click += this.bExpr_Click;
			// 
			// bSL
			// 
			resources.ApplyResources(this.bSL, "bSL");
			this.bSL.Name = "bSL";
			this.bSL.Tag = "sl";
			this.bSL.Click += this.bExpr_Click;
			// 
			// bSR
			// 
			resources.ApplyResources(this.bSR, "bSR");
			this.bSR.Name = "bSR";
			this.bSR.Tag = "sr";
			this.bSR.Click += this.bExpr_Click;
			// 
			// bST
			// 
			resources.ApplyResources(this.bST, "bST");
			this.bST.Name = "bST";
			this.bST.Tag = "st";
			this.bST.Click += this.bExpr_Click;
			// 
			// bSB
			// 
			resources.ApplyResources(this.bSB, "bSB");
			this.bSB.Name = "bSB";
			this.bSB.Tag = "sb";
			this.bSB.Click += this.bExpr_Click;
			// 
			// bCD
			// 
			resources.ApplyResources(this.bCD, "bCD");
			this.bCD.Name = "bCD";
			this.bCD.Tag = "cd";
			this.bCD.Click += this.bExpr_Click;
			// 
			// bCT
			// 
			resources.ApplyResources(this.bCT, "bCT");
			this.bCT.Name = "bCT";
			this.bCT.Tag = "ct";
			this.bCT.Click += this.bExpr_Click;
			// 
			// bCB
			// 
			resources.ApplyResources(this.bCB, "bCB");
			this.bCB.Name = "bCB";
			this.bCB.Tag = "cb";
			this.bCB.Click += this.bExpr_Click;
			// 
			// bWB
			// 
			resources.ApplyResources(this.bWB, "bWB");
			this.bWB.Name = "bWB";
			this.bWB.Tag = "wb";
			this.bWB.Click += this.bExpr_Click;
			// 
			// bWT
			// 
			resources.ApplyResources(this.bWT, "bWT");
			this.bWT.Name = "bWT";
			this.bWT.Tag = "wt";
			this.bWT.Click += this.bExpr_Click;
			// 
			// bWR
			// 
			resources.ApplyResources(this.bWR, "bWR");
			this.bWR.Name = "bWR";
			this.bWR.Tag = "wr";
			this.bWR.Click += this.bExpr_Click;
			// 
			// bCR
			// 
			resources.ApplyResources(this.bCR, "bCR");
			this.bCR.Name = "bCR";
			this.bCR.Tag = "cr";
			this.bCR.Click += this.bExpr_Click;
			// 
			// bWL
			// 
			resources.ApplyResources(this.bWL, "bWL");
			this.bWL.Name = "bWL";
			this.bWL.Tag = "wl";
			this.bWL.Click += this.bExpr_Click;
			// 
			// bWD
			// 
			resources.ApplyResources(this.bWD, "bWD");
			this.bWD.Name = "bWD";
			this.bWD.Tag = "wd";
			this.bWD.Click += this.bExpr_Click;
			// 
			// bCL
			// 
			resources.ApplyResources(this.bCL, "bCL");
			this.bCL.Name = "bCL";
			this.bCL.Tag = "cl";
			this.bCL.Click += this.bExpr_Click;
			// 
			// StyleBorderCtl
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.bCL);
			this.Controls.Add(this.bWD);
			this.Controls.Add(this.bWL);
			this.Controls.Add(this.bCR);
			this.Controls.Add(this.bWR);
			this.Controls.Add(this.bWT);
			this.Controls.Add(this.bWB);
			this.Controls.Add(this.bCB);
			this.Controls.Add(this.bCT);
			this.Controls.Add(this.bCD);
			this.Controls.Add(this.bSB);
			this.Controls.Add(this.bST);
			this.Controls.Add(this.bSR);
			this.Controls.Add(this.bSL);
			this.Controls.Add(this.bSD);
			this.Controls.Add(this.tbWidthDefault);
			this.Controls.Add(this.bColorDefault);
			this.Controls.Add(this.cbColorDefault);
			this.Controls.Add(this.cbStyleDefault);
			this.Controls.Add(this.label8);
			this.Controls.Add(this.tbWidthBottom);
			this.Controls.Add(this.tbWidthTop);
			this.Controls.Add(this.tbWidthRight);
			this.Controls.Add(this.tbWidthLeft);
			this.Controls.Add(this.bColorBottom);
			this.Controls.Add(this.cbColorBottom);
			this.Controls.Add(this.bColorTop);
			this.Controls.Add(this.cbColorTop);
			this.Controls.Add(this.bColorRight);
			this.Controls.Add(this.cbColorRight);
			this.Controls.Add(this.bColorLeft);
			this.Controls.Add(this.cbColorLeft);
			this.Controls.Add(this.cbStyleRight);
			this.Controls.Add(this.cbStyleTop);
			this.Controls.Add(this.cbStyleBottom);
			this.Controls.Add(this.cbStyleLeft);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.lRight);
			this.Controls.Add(this.lTop);
			this.Controls.Add(this.lBottom);
			this.Controls.Add(this.lLeft);
			this.Name = "StyleBorderCtl";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
  
		public bool IsValid()
		{
			string name="";
			try
			{
				if (fWidthDefault && !tbWidthDefault.Text.StartsWith("="))
				{
					name = Strings.StyleBorderCtl_Show_DefaultWidth;
					DesignerUtility.ValidateSize(tbWidthDefault.Text, true, false);
				}
				if (fWidthLeft && !tbWidthLeft.Text.StartsWith("="))
				{
					name = Strings.StyleBorderCtl_Show_LeftWidth;
					DesignerUtility.ValidateSize(tbWidthLeft.Text, true, false);
				}
				if (fWidthTop && !tbWidthTop.Text.StartsWith("="))
				{
					name = Strings.StyleBorderCtl_Show_TopWidth;
					DesignerUtility.ValidateSize(tbWidthTop.Text, true, false);
				}
				if (fWidthBottom && !tbWidthBottom.Text.StartsWith("="))
				{
					name = Strings.StyleBorderCtl_Show_BottomWidth;
					DesignerUtility.ValidateSize(tbWidthBottom.Text, true, false);
				}
				if (fWidthRight && !tbWidthRight.Text.StartsWith("="))
				{
					name = Strings.StyleBorderCtl_Show_RightWidth;
					DesignerUtility.ValidateSize(tbWidthRight.Text, true, false);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, name + " " + Strings.StyleBorderCtl_Show_SizeInvalid);
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

			fStyleDefault = fStyleLeft = fStyleRight = fStyleTop = fStyleBottom =
				fColorDefault = fColorLeft = fColorRight = fColorTop = fColorBottom =
				fWidthDefault = fWidthLeft = fWidthRight = fWidthTop = fWidthBottom= false;
		}

		private void ApplyChanges(XmlNode xNode)
		{
            if (_names != null)
            {
                xNode = _Draw.FindCreateNextInHierarchy(xNode, _names);
            }

            bool bLine = xNode.Name == "Line";
			XmlNode sNode = _Draw.GetCreateNamedChildNode(xNode, "Style");

			// Handle BorderStyle
			XmlNode bsNode = _Draw.SetElement(sNode, "BorderStyle", null);
			if (fStyleDefault)
				_Draw.SetElement(bsNode, "Default", cbStyleDefault.Text);
			if (fStyleLeft && !bLine)
				_Draw.SetElement(bsNode, "Left", cbStyleLeft.Text);
			if (fStyleRight && !bLine)
				_Draw.SetElement(bsNode, "Right", cbStyleRight.Text);
			if (fStyleTop && !bLine)
				_Draw.SetElement(bsNode, "Top", cbStyleTop.Text);
			if (fStyleBottom && !bLine)
				_Draw.SetElement(bsNode, "Bottom", cbStyleBottom.Text);

			// Handle BorderColor
			XmlNode csNode = _Draw.SetElement(sNode, "BorderColor", null);
			if (fColorDefault)
				_Draw.SetElement(csNode, "Default", cbColorDefault.Text);
			if (fColorLeft && !bLine)
				_Draw.SetElement(csNode, "Left", cbColorLeft.Text);
			if (fColorRight && !bLine)
				_Draw.SetElement(csNode, "Right", cbColorRight.Text);
			if (fColorTop && !bLine)
				_Draw.SetElement(csNode, "Top", cbColorTop.Text);
			if (fColorBottom && !bLine)
				_Draw.SetElement(csNode, "Bottom", cbColorBottom.Text);

			// Handle BorderWidth
			XmlNode bwNode = _Draw.SetElement(sNode, "BorderWidth", null);
			if (fWidthDefault)
				_Draw.SetElement(bwNode, "Default", GetSize(tbWidthDefault.Text));
			if (fWidthLeft && !bLine)
				_Draw.SetElement(bwNode, "Left", GetSize(tbWidthLeft.Text));
			if (fWidthRight && !bLine)
				_Draw.SetElement(bwNode, "Right", GetSize(tbWidthRight.Text));
			if (fWidthTop && !bLine)
				_Draw.SetElement(bwNode, "Top", GetSize(tbWidthTop.Text));
			if (fWidthBottom && !bLine)
				_Draw.SetElement(bwNode, "Bottom", GetSize(tbWidthBottom.Text));
		}

		private string GetSize(string sz)
		{
			if (sz.Trim().StartsWith("="))		// Don't mess with expressions
				return sz;

			float size = DesignXmlDraw.GetSize(sz);
			if (size <= 0)
			{
				size = DesignXmlDraw.GetSize(sz+"pt");	// Try assuming pt
				if (size <= 0)	// still no good
					size = 10;	// just set default value
			}
			string rs = string.Format(NumberFormatInfo.InvariantInfo, "{0:0.#}pt", size);
			return rs;
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
                if (sender == this.bColorDefault)
                {
                    cbColorDefault.Text = Majorsilence.Forms.ColorTranslator.ToHtml(cd.Color);
                    cbColorLeft.Text = Majorsilence.Forms.ColorTranslator.ToHtml(cd.Color);
                    cbColorRight.Text = Majorsilence.Forms.ColorTranslator.ToHtml(cd.Color);
                    cbColorTop.Text = Majorsilence.Forms.ColorTranslator.ToHtml(cd.Color);
                    cbColorBottom.Text = Majorsilence.Forms.ColorTranslator.ToHtml(cd.Color);
                }
                else if (sender == this.bColorLeft)
                    cbColorLeft.Text = Majorsilence.Forms.ColorTranslator.ToHtml(cd.Color);
                else if (sender == this.bColorRight)
                    cbColorRight.Text = Majorsilence.Forms.ColorTranslator.ToHtml(cd.Color);
                else if (sender == this.bColorTop)
                    cbColorTop.Text = Majorsilence.Forms.ColorTranslator.ToHtml(cd.Color);
                else if (sender == this.bColorBottom)
                    cbColorBottom.Text = Majorsilence.Forms.ColorTranslator.ToHtml(cd.Color);
            }
		
			return;
		}

		private void cbStyleDefault_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			cbStyleLeft.Text = cbStyleRight.Text = 
				cbStyleTop.Text = cbStyleBottom.Text = cbStyleDefault.Text;
			fStyleDefault = fStyleLeft = fStyleRight = fStyleTop = fStyleBottom = true;
		}

		private void cbColorDefault_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			cbColorLeft.Text = cbColorRight.Text = 
				cbColorTop.Text = cbColorBottom.Text = cbColorDefault.Text;
			fColorDefault = fColorLeft = fColorRight = fColorTop = fColorBottom = true;
		}

		private void tbWidthDefault_TextChanged(object sender, System.EventArgs e)
		{
			tbWidthLeft.Text = tbWidthRight.Text = 
				tbWidthTop.Text = tbWidthBottom.Text = tbWidthDefault.Text;
			fWidthDefault = fWidthLeft = fWidthRight = fWidthTop = fWidthBottom = true;
		}

		private void cbStyle_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (sender == cbStyleLeft)
				fStyleLeft = true;
			else if (sender == cbStyleRight)
				fStyleRight = true;
			else if (sender == cbStyleTop)
				fStyleTop = true;
			else if (sender == cbStyleBottom)
				fStyleBottom = true;
		}

		private void cbColor_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (sender == cbColorLeft)
				fColorLeft = true;
			else if (sender == cbColorRight)
				fColorRight = true;
			else if (sender == cbColorTop)
				fColorTop = true;
			else if (sender == cbColorBottom)
				fColorBottom = true;
		}

		private void tbWidth_Changed(object sender, System.EventArgs e)
		{
			if (sender == tbWidthLeft)
				fWidthLeft = true;
			else if (sender == tbWidthRight)
				fWidthRight = true;
			else if (sender == tbWidthTop)
				fWidthTop = true;
			else if (sender == tbWidthBottom)
				fWidthBottom = true;
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
				case "sd":
					c = cbStyleDefault;
					break;
				case "cd":
					c = cbColorDefault;
					bColor = true;
					break;
				case "wd":
					c = tbWidthDefault;
					break;
				case "sl":
					c = cbStyleLeft;
					break;
				case "cl":
					c = cbColorLeft;
					bColor = true;
					break;
				case "wl":
					c = tbWidthLeft;
					break;
				case "sr":
					c = cbStyleRight;
					break;
				case "cr":
					c = cbColorRight;
					bColor = true;
					break;
				case "wr":
					c = tbWidthRight;
					break;
				case "st":
					c = cbStyleTop;
					break;
				case "ct":
					c = cbColorTop;
					bColor = true;
					break;
				case "wt":
					c = tbWidthTop;
					break;
				case "sb":
					c = cbStyleBottom;
					break;
				case "cb":
					c = cbColorBottom;
					bColor = true;
					break;
				case "wb":
					c = tbWidthBottom;
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
