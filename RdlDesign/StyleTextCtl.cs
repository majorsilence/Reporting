
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using System.Globalization;
using Majorsilence.Reporting.RdlDesign.Resources;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// Summary description for StyleCtl.
    /// </summary>
    internal class StyleTextCtl : System.Windows.Forms.UserControl, IProperty
	{
        private List<XmlNode> _ReportItems;
		private DesignXmlDraw _Draw;
		private string _DataSetName;
		private bool fHorzAlign, fFormat, fDirection, fWritingMode, fTextDecoration;
		private bool fColor, fVerticalAlign, fFontStyle, fFontWeight, fFontSize, fFontFamily;
		private bool fValue;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label lFont;
		private System.Windows.Forms.Button bFont;
		private System.Windows.Forms.ComboBox cbHorzAlign;
		private System.Windows.Forms.ComboBox cbFormat;
		private System.Windows.Forms.ComboBox cbDirection;
		private System.Windows.Forms.ComboBox cbWritingMode;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ComboBox cbTextDecoration;
		private System.Windows.Forms.Button bColor;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.ComboBox cbColor;
		private System.Windows.Forms.ComboBox cbVerticalAlign;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ComboBox cbFontStyle;
		private System.Windows.Forms.ComboBox cbFontWeight;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.ComboBox cbFontSize;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.ComboBox cbFontFamily;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label lblValue;
        private ComboBox cbValue;
        private System.Windows.Forms.Button bValueExpr;
		private System.Windows.Forms.Button bFamily;
		private System.Windows.Forms.Button bStyle;
		private System.Windows.Forms.Button bColorEx;
		private System.Windows.Forms.Button bSize;
		private System.Windows.Forms.Button bWeight;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button bAlignment;
		private System.Windows.Forms.Button bDirection;
		private System.Windows.Forms.Button bVertical;
		private System.Windows.Forms.Button bWrtMode;
		private System.Windows.Forms.Button bFormat;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        internal StyleTextCtl(DesignXmlDraw dxDraw, List<XmlNode> styles, PropertyDialog myDialog)
		{
			_ReportItems = styles;
			_Draw = dxDraw;
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Initialize form using the style node values
			InitTextStyles();
            myDialog.Shown += MyDialog_Shown;
		}

        private void MyDialog_Shown(object sender, EventArgs e)
        {
            cbValue.Focus();
        }

        private void InitTextStyles()
		{
            cbColor.Items.AddRange(StaticLists.ColorList);

			XmlNode sNode = _ReportItems[0];
			if (_ReportItems.Count > 1)
			{
				cbValue.Text = Strings.PositionCtl_InitValues_GroupSelected;
				cbValue.Enabled = false;
				lblValue.Enabled = false;
			}
			else if (sNode.Name == "Textbox")
			{
				XmlNode vNode = _Draw.GetNamedChildNode(sNode, "Value");
				if (vNode != null)
					cbValue.Text = vNode.InnerText;
				// now populate the combo box
				// Find the dataregion that contains the Textbox (if any)
				for (XmlNode pNode = sNode.ParentNode; pNode != null; pNode = pNode.ParentNode)
				{
					if (pNode.Name == "List" ||
						pNode.Name == "Table" ||
						pNode.Name == "Matrix" ||
						pNode.Name == "Chart")
					{
						_DataSetName = _Draw.GetDataSetNameValue(pNode);
						if (_DataSetName != null)	// found it
						{
							string[] f = _Draw.GetFields(_DataSetName, true);
                            if (f != null)
							    cbValue.Items.AddRange(f);
						}
					}
				}
				// parameters
				string[] ps = _Draw.GetReportParameters(true);
				if (ps != null)
					cbValue.Items.AddRange(ps);
				// globals
				cbValue.Items.AddRange(StaticLists.GlobalList);
			}
			else if (sNode.Name == "Title" || sNode.Name == "fyi:Title2" || sNode.Name == "Title2")// 20022008 AJM GJL
			{
				lblValue.Text = Strings.StyleTextCtl_InitTextStyles_Caption;		// Note: label needs to equal the element name
				XmlNode vNode = _Draw.GetNamedChildNode(sNode, "Caption");
				if (vNode != null)
					cbValue.Text = vNode.InnerText;
			}
			else
			{
				lblValue.Visible = false;
				cbValue.Visible = false;
                bValueExpr.Visible = false;
			}

			sNode = _Draw.GetNamedChildNode(sNode, "Style");

			string sFontStyle="Normal";
			string sFontFamily="Arial";
			string sFontWeight="Normal";
			string sFontSize="10pt";
			string sTextDecoration="None";
			string sHorzAlign="General";
			string sVerticalAlign="Top";
			string sColor="Black";
			string sFormat="";
			string sDirection="LTR";
			string sWritingMode="lr-tb";
			foreach (XmlNode lNode in sNode)
			{
				if (lNode.NodeType != XmlNodeType.Element)
					continue;
				switch (lNode.Name)
				{
					case "FontStyle":
						sFontStyle = lNode.InnerText;
						break;
					case "FontFamily":
						sFontFamily = lNode.InnerText;
						break;
					case "FontWeight":
						sFontWeight = lNode.InnerText;
						break;
					case "FontSize":
						sFontSize = lNode.InnerText;
						break;
					case "TextDecoration":
						sTextDecoration = lNode.InnerText;
						break;
					case "TextAlign":
						sHorzAlign = lNode.InnerText;
						break;
					case "VerticalAlign":
						sVerticalAlign = lNode.InnerText;
						break;
					case "Color":
						sColor = lNode.InnerText;
						break;
					case "Format":
						sFormat = lNode.InnerText;
						break;
					case "Direction":
						sDirection = lNode.InnerText;
						break;
					case "WritingMode":
						sWritingMode = lNode.InnerText;
						break;
				}
			}

			// Population Font Family dropdown
			foreach (FontFamily ff in FontFamily.Families)
			{
				cbFontFamily.Items.Add(ff.Name);
			}

			this.cbFontStyle.Text = sFontStyle;
			this.cbFontFamily.Text = sFontFamily;
			this.cbFontWeight.Text = sFontWeight;
			this.cbFontSize.Text = sFontSize;
			this.cbTextDecoration.Text = sTextDecoration;
			this.cbHorzAlign.Text = sHorzAlign;
			this.cbVerticalAlign.Text = sVerticalAlign;
			this.cbColor.Text = sColor;
			this.cbFormat.Text = sFormat;
			this.cbDirection.Text = sDirection;
			this.cbWritingMode.Text = sWritingMode;

			fHorzAlign = fFormat = fDirection = fWritingMode = fTextDecoration =
				fColor = fVerticalAlign = fFontStyle = fFontWeight = fFontSize = fFontFamily =
				fValue = false;

			return;
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StyleTextCtl));
            this.DoubleBuffered = true;
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.lFont = new System.Windows.Forms.Label();
            this.bFont = new System.Windows.Forms.Button();
            this.cbVerticalAlign = new System.Windows.Forms.ComboBox();
            this.cbHorzAlign = new System.Windows.Forms.ComboBox();
            this.cbFormat = new System.Windows.Forms.ComboBox();
            this.cbDirection = new System.Windows.Forms.ComboBox();
            this.cbWritingMode = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cbTextDecoration = new System.Windows.Forms.ComboBox();
            this.bColor = new System.Windows.Forms.Button();
            this.label9 = new System.Windows.Forms.Label();
            this.cbColor = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cbFontStyle = new System.Windows.Forms.ComboBox();
            this.cbFontWeight = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.cbFontSize = new System.Windows.Forms.ComboBox();
            this.label11 = new System.Windows.Forms.Label();
            this.cbFontFamily = new System.Windows.Forms.ComboBox();
            this.lblValue = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.button2 = new System.Windows.Forms.Button();
            this.bWeight = new System.Windows.Forms.Button();
            this.bSize = new System.Windows.Forms.Button();
            this.bColorEx = new System.Windows.Forms.Button();
            this.bStyle = new System.Windows.Forms.Button();
            this.bFamily = new System.Windows.Forms.Button();
            this.cbValue = new System.Windows.Forms.ComboBox();
            this.bValueExpr = new System.Windows.Forms.Button();
            this.bAlignment = new System.Windows.Forms.Button();
            this.bDirection = new System.Windows.Forms.Button();
            this.bVertical = new System.Windows.Forms.Button();
            this.bWrtMode = new System.Windows.Forms.Button();
            this.bFormat = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
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
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            // 
            // lFont
            // 
            resources.ApplyResources(this.lFont, "lFont");
            this.lFont.Name = "lFont";
            // 
            // bFont
            // 
            resources.ApplyResources(this.bFont, "bFont");
            this.bFont.Name = "bFont";
            this.bFont.Click += new System.EventHandler(this.bFont_Click);
            // 
            // cbVerticalAlign
            // 
            this.cbVerticalAlign.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbVerticalAlign.Items.AddRange(new object[] {
            resources.GetString("cbVerticalAlign.Items"),
            resources.GetString("cbVerticalAlign.Items1"),
            resources.GetString("cbVerticalAlign.Items2")});
            resources.ApplyResources(this.cbVerticalAlign, "cbVerticalAlign");
            this.cbVerticalAlign.Name = "cbVerticalAlign";
            this.cbVerticalAlign.SelectedIndexChanged += new System.EventHandler(this.cbVerticalAlign_TextChanged);
            this.cbVerticalAlign.TextChanged += new System.EventHandler(this.cbVerticalAlign_TextChanged);
            // 
            // cbHorzAlign
            // 
            this.cbHorzAlign.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbHorzAlign.Items.AddRange(new object[] {
            resources.GetString("cbHorzAlign.Items"),
            resources.GetString("cbHorzAlign.Items1"),
            resources.GetString("cbHorzAlign.Items2"),
            resources.GetString("cbHorzAlign.Items3"),
            resources.GetString("cbHorzAlign.Items4")});
            resources.ApplyResources(this.cbHorzAlign, "cbHorzAlign");
            this.cbHorzAlign.Name = "cbHorzAlign";
            this.cbHorzAlign.SelectedIndexChanged += new System.EventHandler(this.cbHorzAlign_TextChanged);
            this.cbHorzAlign.TextChanged += new System.EventHandler(this.cbHorzAlign_TextChanged);
            // 
            // cbFormat
            // 
            this.cbFormat.Items.AddRange(new object[] {
            resources.GetString("cbFormat.Items"),
            resources.GetString("cbFormat.Items1"),
            resources.GetString("cbFormat.Items2"),
            resources.GetString("cbFormat.Items3"),
            resources.GetString("cbFormat.Items4"),
            resources.GetString("cbFormat.Items5"),
            resources.GetString("cbFormat.Items6"),
            resources.GetString("cbFormat.Items7"),
            resources.GetString("cbFormat.Items8"),
            resources.GetString("cbFormat.Items9"),
            resources.GetString("cbFormat.Items10"),
            resources.GetString("cbFormat.Items11"),
            resources.GetString("cbFormat.Items12"),
            resources.GetString("cbFormat.Items13"),
            resources.GetString("cbFormat.Items14"),
            resources.GetString("cbFormat.Items15"),
            resources.GetString("cbFormat.Items16"),
            resources.GetString("cbFormat.Items17"),
            resources.GetString("cbFormat.Items18"),
            resources.GetString("cbFormat.Items19")});
            resources.ApplyResources(this.cbFormat, "cbFormat");
            this.cbFormat.Name = "cbFormat";
            this.cbFormat.TextChanged += new System.EventHandler(this.cbFormat_TextChanged);
            // 
            // cbDirection
            // 
            this.cbDirection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDirection.Items.AddRange(new object[] {
            resources.GetString("cbDirection.Items"),
            resources.GetString("cbDirection.Items1")});
            resources.ApplyResources(this.cbDirection, "cbDirection");
            this.cbDirection.Name = "cbDirection";
            this.cbDirection.SelectedIndexChanged += new System.EventHandler(this.cbDirection_TextChanged);
            this.cbDirection.TextChanged += new System.EventHandler(this.cbDirection_TextChanged);
            // 
            // cbWritingMode
            // 
            this.cbWritingMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbWritingMode.Items.AddRange(new object[] {
            resources.GetString("cbWritingMode.Items"),
            resources.GetString("cbWritingMode.Items1"),
            resources.GetString("cbWritingMode.Items2"),
            resources.GetString("cbWritingMode.Items3")});
            resources.ApplyResources(this.cbWritingMode, "cbWritingMode");
            this.cbWritingMode.Name = "cbWritingMode";
            this.cbWritingMode.SelectedIndexChanged += new System.EventHandler(this.cbWritingMode_TextChanged);
            this.cbWritingMode.TextChanged += new System.EventHandler(this.cbWritingMode_TextChanged);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // cbTextDecoration
            // 
            this.cbTextDecoration.Items.AddRange(new object[] {
            resources.GetString("cbTextDecoration.Items"),
            resources.GetString("cbTextDecoration.Items1"),
            resources.GetString("cbTextDecoration.Items2"),
            resources.GetString("cbTextDecoration.Items3")});
            resources.ApplyResources(this.cbTextDecoration, "cbTextDecoration");
            this.cbTextDecoration.Name = "cbTextDecoration";
            this.cbTextDecoration.SelectedIndexChanged += new System.EventHandler(this.cbTextDecoration_TextChanged);
            this.cbTextDecoration.TextChanged += new System.EventHandler(this.cbTextDecoration_TextChanged);
            // 
            // bColor
            // 
            resources.ApplyResources(this.bColor, "bColor");
            this.bColor.Name = "bColor";
            this.bColor.Click += new System.EventHandler(this.bColor_Click);
            // 
            // label9
            // 
            resources.ApplyResources(this.label9, "label9");
            this.label9.Name = "label9";
            // 
            // cbColor
            // 
            resources.ApplyResources(this.cbColor, "cbColor");
            this.cbColor.Name = "cbColor";
            this.cbColor.TextChanged += new System.EventHandler(this.cbColor_TextChanged);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // cbFontStyle
            // 
            this.cbFontStyle.Items.AddRange(new object[] {
            resources.GetString("cbFontStyle.Items"),
            resources.GetString("cbFontStyle.Items1")});
            resources.ApplyResources(this.cbFontStyle, "cbFontStyle");
            this.cbFontStyle.Name = "cbFontStyle";
            this.cbFontStyle.TextChanged += new System.EventHandler(this.cbFontStyle_TextChanged);
            // 
            // cbFontWeight
            // 
            this.cbFontWeight.Items.AddRange(new object[] {
            resources.GetString("cbFontWeight.Items"),
            resources.GetString("cbFontWeight.Items1"),
            resources.GetString("cbFontWeight.Items2"),
            resources.GetString("cbFontWeight.Items3"),
            resources.GetString("cbFontWeight.Items4"),
            resources.GetString("cbFontWeight.Items5"),
            resources.GetString("cbFontWeight.Items6"),
            resources.GetString("cbFontWeight.Items7"),
            resources.GetString("cbFontWeight.Items8"),
            resources.GetString("cbFontWeight.Items9"),
            resources.GetString("cbFontWeight.Items10"),
            resources.GetString("cbFontWeight.Items11"),
            resources.GetString("cbFontWeight.Items12")});
            resources.ApplyResources(this.cbFontWeight, "cbFontWeight");
            this.cbFontWeight.Name = "cbFontWeight";
            this.cbFontWeight.TextChanged += new System.EventHandler(this.cbFontWeight_TextChanged);
            // 
            // label10
            // 
            resources.ApplyResources(this.label10, "label10");
            this.label10.Name = "label10";
            // 
            // cbFontSize
            // 
            this.cbFontSize.Items.AddRange(new object[] {
            resources.GetString("cbFontSize.Items"),
            resources.GetString("cbFontSize.Items1"),
            resources.GetString("cbFontSize.Items2"),
            resources.GetString("cbFontSize.Items3"),
            resources.GetString("cbFontSize.Items4"),
            resources.GetString("cbFontSize.Items5"),
            resources.GetString("cbFontSize.Items6"),
            resources.GetString("cbFontSize.Items7"),
            resources.GetString("cbFontSize.Items8"),
            resources.GetString("cbFontSize.Items9"),
            resources.GetString("cbFontSize.Items10"),
            resources.GetString("cbFontSize.Items11"),
            resources.GetString("cbFontSize.Items12"),
            resources.GetString("cbFontSize.Items13"),
            resources.GetString("cbFontSize.Items14"),
            resources.GetString("cbFontSize.Items15")});
            resources.ApplyResources(this.cbFontSize, "cbFontSize");
            this.cbFontSize.Name = "cbFontSize";
            this.cbFontSize.TextChanged += new System.EventHandler(this.cbFontSize_TextChanged);
            // 
            // label11
            // 
            resources.ApplyResources(this.label11, "label11");
            this.label11.Name = "label11";
            // 
            // cbFontFamily
            // 
            this.cbFontFamily.Items.AddRange(new object[] {
            resources.GetString("cbFontFamily.Items")});
            resources.ApplyResources(this.cbFontFamily, "cbFontFamily");
            this.cbFontFamily.Name = "cbFontFamily";
            this.cbFontFamily.TextChanged += new System.EventHandler(this.cbFontFamily_TextChanged);
            // 
            // lblValue
            // 
            resources.ApplyResources(this.lblValue, "lblValue");
            this.lblValue.Name = "lblValue";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.cbTextDecoration);
            this.groupBox1.Controls.Add(this.cbFontFamily);
            this.groupBox1.Controls.Add(this.button2);
            this.groupBox1.Controls.Add(this.bWeight);
            this.groupBox1.Controls.Add(this.bSize);
            this.groupBox1.Controls.Add(this.bColorEx);
            this.groupBox1.Controls.Add(this.bStyle);
            this.groupBox1.Controls.Add(this.bFamily);
            this.groupBox1.Controls.Add(this.lFont);
            this.groupBox1.Controls.Add(this.bFont);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.bColor);
            this.groupBox1.Controls.Add(this.label9);
            this.groupBox1.Controls.Add(this.cbColor);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.cbFontStyle);
            this.groupBox1.Controls.Add(this.cbFontWeight);
            this.groupBox1.Controls.Add(this.label10);
            this.groupBox1.Controls.Add(this.cbFontSize);
            this.groupBox1.Controls.Add(this.label11);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // button2
            // 
            resources.ApplyResources(this.button2, "button2");
            this.button2.Name = "button2";
            this.button2.Tag = "decoration";
            this.button2.Click += new System.EventHandler(this.bExpr_Click);
            // 
            // bWeight
            // 
            resources.ApplyResources(this.bWeight, "bWeight");
            this.bWeight.Name = "bWeight";
            this.bWeight.Tag = "weight";
            this.bWeight.Click += new System.EventHandler(this.bExpr_Click);
            // 
            // bSize
            // 
            resources.ApplyResources(this.bSize, "bSize");
            this.bSize.Name = "bSize";
            this.bSize.Tag = "size";
            this.bSize.Click += new System.EventHandler(this.bExpr_Click);
            // 
            // bColorEx
            // 
            resources.ApplyResources(this.bColorEx, "bColorEx");
            this.bColorEx.Name = "bColorEx";
            this.bColorEx.Tag = "color";
            this.bColorEx.Click += new System.EventHandler(this.bExpr_Click);
            // 
            // bStyle
            // 
            resources.ApplyResources(this.bStyle, "bStyle");
            this.bStyle.Name = "bStyle";
            this.bStyle.Tag = "style";
            this.bStyle.Click += new System.EventHandler(this.bExpr_Click);
            // 
            // bFamily
            // 
            resources.ApplyResources(this.bFamily, "bFamily");
            this.bFamily.Name = "bFamily";
            this.bFamily.Tag = "family";
            this.bFamily.Click += new System.EventHandler(this.bExpr_Click);
            // 
            // cbValue
            // 
            resources.ApplyResources(this.cbValue, "cbValue");
            this.cbValue.Name = "cbValue";
            this.cbValue.TextChanged += new System.EventHandler(this.cbValue_TextChanged);
            // 
            // bValueExpr
            // 
            resources.ApplyResources(this.bValueExpr, "bValueExpr");
            this.bValueExpr.Name = "bValueExpr";
            this.bValueExpr.Tag = "value";
            this.bValueExpr.Click += new System.EventHandler(this.bExpr_Click);
            // 
            // bAlignment
            // 
            resources.ApplyResources(this.bAlignment, "bAlignment");
            this.bAlignment.Name = "bAlignment";
            this.bAlignment.Tag = "halign";
            this.bAlignment.Click += new System.EventHandler(this.bExpr_Click);
            // 
            // bDirection
            // 
            resources.ApplyResources(this.bDirection, "bDirection");
            this.bDirection.Name = "bDirection";
            this.bDirection.Tag = "direction";
            this.bDirection.Click += new System.EventHandler(this.bExpr_Click);
            // 
            // bVertical
            // 
            resources.ApplyResources(this.bVertical, "bVertical");
            this.bVertical.Name = "bVertical";
            this.bVertical.Tag = "valign";
            this.bVertical.Click += new System.EventHandler(this.bExpr_Click);
            // 
            // bWrtMode
            // 
            resources.ApplyResources(this.bWrtMode, "bWrtMode");
            this.bWrtMode.Name = "bWrtMode";
            this.bWrtMode.Tag = "writing";
            this.bWrtMode.Click += new System.EventHandler(this.bExpr_Click);
            // 
            // bFormat
            // 
            resources.ApplyResources(this.bFormat, "bFormat");
            this.bFormat.Name = "bFormat";
            this.bFormat.Tag = "format";
            this.bFormat.Click += new System.EventHandler(this.bExpr_Click);
            // 
            // StyleTextCtl
            // 
            this.Controls.Add(this.bFormat);
            this.Controls.Add(this.bWrtMode);
            this.Controls.Add(this.bVertical);
            this.Controls.Add(this.bDirection);
            this.Controls.Add(this.bAlignment);
            this.Controls.Add(this.bValueExpr);
            this.Controls.Add(this.cbValue);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.lblValue);
            this.Controls.Add(this.cbWritingMode);
            this.Controls.Add(this.cbDirection);
            this.Controls.Add(this.cbFormat);
            this.Controls.Add(this.cbHorzAlign);
            this.Controls.Add(this.cbVerticalAlign);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Name = "StyleTextCtl";
            resources.ApplyResources(this, "$this");
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

		}
		#endregion
      

		public bool IsValid()
		{
			if (fFontSize)
			{
				try 
				{
					if (!cbFontSize.Text.Trim().StartsWith("="))
						DesignerUtility.ValidateSize(cbFontSize.Text, false, false);
				}
				catch (Exception e)
				{
					MessageBox.Show(e.Message, Strings.StyleTextCtl_Show_InvalidFontSize);
					return false;
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
			
			foreach (XmlNode riNode in this._ReportItems)
				ApplyChanges(riNode);

			fHorzAlign = fFormat = fDirection = fWritingMode = fTextDecoration =
				fColor = fVerticalAlign = fFontStyle = fFontWeight = fFontSize = fFontFamily =
				fValue = false;
		}

		public void ApplyChanges(XmlNode node)
		{
			if (cbValue.Enabled)
			{
				if (fValue)
					_Draw.SetElement(node, "Value", cbValue.Text);		// only adjust value when single item selected
			}

			XmlNode sNode = _Draw.GetNamedChildNode(node, "Style");

			if (fFontStyle)
				_Draw.SetElement(sNode, "FontStyle", cbFontStyle.Text);
			if (fFontFamily)
				_Draw.SetElement(sNode, "FontFamily", cbFontFamily.Text);
			if (fFontWeight)
				_Draw.SetElement(sNode, "FontWeight", cbFontWeight.Text);

			if (fFontSize)
			{
				if(cbFontSize.Text.StartsWith("="))
                    _Draw.SetElement(sNode, "FontSize", cbFontSize.Text);
                else
                {
                    float size = DesignXmlDraw.GetSize(cbFontSize.Text);
				    if (size <= 0){
					    size = DesignXmlDraw.GetSize(cbFontSize.Text+"pt");	// Try assuming pt
					    if (size <= 0)	// still no good
						    size = 10;	// just set default value
				    }
				    string rs = string.Format(NumberFormatInfo.InvariantInfo, "{0:0.#}pt", size);

				    _Draw.SetElement(sNode, "FontSize", rs);	// force to string
                }
			}
			if (fTextDecoration)
				_Draw.SetElement(sNode, "TextDecoration", cbTextDecoration.Text);    
			if (fHorzAlign)
				_Draw.SetElement(sNode, "TextAlign", cbHorzAlign.Text);
			if (fVerticalAlign)
				_Draw.SetElement(sNode, "VerticalAlign", cbVerticalAlign.Text);
			if (fColor)
				_Draw.SetElement(sNode, "Color", cbColor.Text);
			if (fFormat)
			{
				if (cbFormat.Text.Length == 0)		// Don't put out a format if no format value
					_Draw.RemoveElement(sNode, "Format");
				else
					_Draw.SetElement(sNode, "Format", cbFormat.Text);
			}
			if (fDirection)
				_Draw.SetElement(sNode, "Direction", cbDirection.Text);
			if (fWritingMode)
				_Draw.SetElement(sNode, "WritingMode", cbWritingMode.Text);
			
			return;
		}

		private void bFont_Click(object sender, System.EventArgs e)
		{
			FontDialog fd = new FontDialog();
			fd.ShowColor = true;

			// STYLE
			System.Drawing.FontStyle fs = 0;
			if (cbFontStyle.Text == "Italic")
				fs |= System.Drawing.FontStyle.Italic;

			if (cbTextDecoration.Text == "Underline")
				fs |= FontStyle.Underline;
			else if (cbTextDecoration.Text == "LineThrough")
				fs |= FontStyle.Strikeout;

			// WEIGHT
			switch (cbFontWeight.Text)
			{
				case "Bold":
				case "Bolder":
				case "500":
				case "600":
				case "700":
				case "800":
				case "900":
					fs |= System.Drawing.FontStyle.Bold;
					break;
				default:
					break;
			}
			float size=10;
			size = DesignXmlDraw.GetSize(cbFontSize.Text);
			if (size <= 0)
			{
				size = DesignXmlDraw.GetSize(cbFontSize.Text+"pt");	// Try assuming pt
				if (size <= 0)	// still no good
					size = 10;	// just set default value
			}
			Font drawFont = new Font(cbFontFamily.Text, size, fs);	// si.FontSize already in points


			fd.Font = drawFont;
			fd.Color = 
				DesignerUtility.ColorFromHtml(cbColor.Text, System.Drawing.Color.Black);
            try
            {
                DialogResult dr = fd.ShowDialog();
                if (dr != DialogResult.OK)
                {
                    drawFont.Dispose();
                    return;
                }

                // Apply all the font info
                cbFontWeight.Text = fd.Font.Bold ? "Bold" : "Normal";
                cbFontStyle.Text = fd.Font.Italic ? "Italic" : "Normal";
                cbFontFamily.Text = fd.Font.FontFamily.Name;
                cbFontSize.Text = fd.Font.Size.ToString() + "pt";
                cbColor.Text = ColorTranslator.ToHtml(fd.Color);
                if (fd.Font.Underline)
                    this.cbTextDecoration.Text = "Underline";
                else if (fd.Font.Strikeout)
                    this.cbTextDecoration.Text = "LineThrough";
                else
                    this.cbTextDecoration.Text = "None";
                drawFont.Dispose();
            }
            finally
            {
                fd.Dispose();
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
                cd.Color =
                    DesignerUtility.ColorFromHtml(cbColor.Text, System.Drawing.Color.Black);

                if (cd.ShowDialog() != DialogResult.OK)
                    return;

                RdlDesigner.SetCustomColors(cd.CustomColors);
                if (sender == this.bColor)
                    cbColor.Text = ColorTranslator.ToHtml(cd.Color);
            }		
			return;
		}

		private void cbValue_TextChanged(object sender, System.EventArgs e)
		{
			fValue = true;
		}

		private void cbFontFamily_TextChanged(object sender, System.EventArgs e)
		{
			fFontFamily = true;
		}

		private void cbFontSize_TextChanged(object sender, System.EventArgs e)
		{
			fFontSize = true;
		}

		private void cbFontStyle_TextChanged(object sender, System.EventArgs e)
		{
			fFontStyle = true;
		}

		private void cbFontWeight_TextChanged(object sender, System.EventArgs e)
		{
			fFontWeight = true;
		}

		private void cbColor_TextChanged(object sender, System.EventArgs e)
		{
			fColor = true;
		}

		private void cbTextDecoration_TextChanged(object sender, System.EventArgs e)
		{
			fTextDecoration = true;
		}

		private void cbHorzAlign_TextChanged(object sender, System.EventArgs e)
		{
			fHorzAlign = true;
		}

		private void cbVerticalAlign_TextChanged(object sender, System.EventArgs e)
		{
			fVerticalAlign = true;
		}

		private void cbDirection_TextChanged(object sender, System.EventArgs e)
		{
			fDirection = true;
		}

		private void cbWritingMode_TextChanged(object sender, System.EventArgs e)
		{
			fWritingMode = true;
		}

		private void cbFormat_TextChanged(object sender, System.EventArgs e)
		{
			fFormat = true;
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
				case "value":
					c = cbValue;
					break;
				case "family":
					c = cbFontFamily;
					break;
				case "style":
					c = cbFontStyle;
					break;
				case "color":
					c = cbColor;
					bColor = true;
					break;
				case "size":
					c = cbFontSize;
					break;
				case "weight":
					c = cbFontWeight;
					break;
				case "decoration":
					c = cbTextDecoration;
					break;
				case "halign":
					c = cbHorzAlign;
					break;
				case "valign":
					c = cbVerticalAlign;
					break;
				case "direction":
					c = cbDirection;
					break;
				case "writing":
					c = cbWritingMode;
					break;
				case "format":
					c = cbFormat;
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
