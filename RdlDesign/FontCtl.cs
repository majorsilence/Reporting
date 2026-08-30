
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using Majorsilence.Forms.Drawing;
using System.Data;
using Majorsilence.Forms;
using System.Xml;
using System.Globalization;
using Majorsilence.Reporting.RdlDesign.Resources;

namespace Majorsilence.Reporting.RdlDesign
{
	/// <summary>
	/// FontCtl
	/// </summary>
	internal class FontCtl : Majorsilence.Forms.UserControl, IProperty
	{
        private List<XmlNode> _ReportItems;
		private DesignXmlDraw _Draw;
		private bool fHorzAlign, fFormat, fDirection, fWritingMode, fTextDecoration;
		private bool fColor, fVerticalAlign, fFontStyle, fFontWeight, fFontSize, fFontFamily;
		private Majorsilence.Forms.Label label4;
		private Majorsilence.Forms.Label label5;
		private Majorsilence.Forms.Label label6;
		private Majorsilence.Forms.Label label7;
		private Majorsilence.Forms.Label label8;
		private Majorsilence.Forms.Label lFont;
		private Majorsilence.Forms.Button bFont;
		private Majorsilence.Forms.ComboBox cbHorzAlign;
		private Majorsilence.Forms.ComboBox cbFormat;
		private Majorsilence.Forms.ComboBox cbDirection;
		private Majorsilence.Forms.ComboBox cbWritingMode;
		private Majorsilence.Forms.Label label2;
		private Majorsilence.Forms.ComboBox cbTextDecoration;
		private Majorsilence.Forms.Button bColor;
		private Majorsilence.Forms.Label label9;
		private Majorsilence.Forms.ComboBox cbColor;
		private Majorsilence.Forms.ComboBox cbVerticalAlign;
		private Majorsilence.Forms.Label label3;
		private Majorsilence.Forms.ComboBox cbFontStyle;
		private Majorsilence.Forms.ComboBox cbFontWeight;
		private Majorsilence.Forms.Label label10;
		private Majorsilence.Forms.ComboBox cbFontSize;
		private Majorsilence.Forms.Label label11;
		private Majorsilence.Forms.ComboBox cbFontFamily;
        private Majorsilence.Forms.GroupBox groupBox1;
		private Majorsilence.Forms.Button bFamily;
		private Majorsilence.Forms.Button bStyle;
		private Majorsilence.Forms.Button bColorEx;
		private Majorsilence.Forms.Button bSize;
		private Majorsilence.Forms.Button bWeight;
		private Majorsilence.Forms.Button button2;
		private Majorsilence.Forms.Button bAlignment;
		private Majorsilence.Forms.Button bDirection;
		private Majorsilence.Forms.Button bVertical;
		private Majorsilence.Forms.Button bWrtMode;
		private Majorsilence.Forms.Button bFormat;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private string[] _names;

        public FontCtl(DesignXmlDraw dxDraw, string[] names, List<XmlNode> styles)
		{
			_ReportItems = styles;
			_Draw = dxDraw;
            _names = names;
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Initialize form using the style node values
			InitTextStyles();
		}

		private void InitTextStyles()
		{
            cbColor.Items.AddRange(StaticLists.ColorList);
            cbFormat.Items.AddRange(StaticLists.FormatList);

			XmlNode sNode = _ReportItems[0];

            if (_names != null)
            {
                sNode = _Draw.FindCreateNextInHierarchy(sNode, _names);
            }

			sNode = _Draw.GetCreateNamedChildNode(sNode, "Style");

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
                fColor = fVerticalAlign = fFontStyle = fFontWeight = fFontSize = fFontFamily = false;

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
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(FontCtl));
            this.DoubleBuffered = true;
			this.label4 = new Majorsilence.Forms.Label();
			this.label5 = new Majorsilence.Forms.Label();
			this.label6 = new Majorsilence.Forms.Label();
			this.label7 = new Majorsilence.Forms.Label();
			this.label8 = new Majorsilence.Forms.Label();
			this.lFont = new Majorsilence.Forms.Label();
			this.bFont = new Majorsilence.Forms.Button();
			this.cbVerticalAlign = new Majorsilence.Forms.ComboBox();
			this.cbHorzAlign = new Majorsilence.Forms.ComboBox();
			this.cbFormat = new Majorsilence.Forms.ComboBox();
			this.cbDirection = new Majorsilence.Forms.ComboBox();
			this.cbWritingMode = new Majorsilence.Forms.ComboBox();
			this.label2 = new Majorsilence.Forms.Label();
			this.cbTextDecoration = new Majorsilence.Forms.ComboBox();
			this.bColor = new Majorsilence.Forms.Button();
			this.label9 = new Majorsilence.Forms.Label();
			this.cbColor = new Majorsilence.Forms.ComboBox();
			this.label3 = new Majorsilence.Forms.Label();
			this.cbFontStyle = new Majorsilence.Forms.ComboBox();
			this.cbFontWeight = new Majorsilence.Forms.ComboBox();
			this.label10 = new Majorsilence.Forms.Label();
			this.cbFontSize = new Majorsilence.Forms.ComboBox();
			this.label11 = new Majorsilence.Forms.Label();
			this.cbFontFamily = new Majorsilence.Forms.ComboBox();
			this.groupBox1 = new Majorsilence.Forms.GroupBox();
			this.button2 = new Majorsilence.Forms.Button();
			this.bWeight = new Majorsilence.Forms.Button();
			this.bSize = new Majorsilence.Forms.Button();
			this.bColorEx = new Majorsilence.Forms.Button();
			this.bStyle = new Majorsilence.Forms.Button();
			this.bFamily = new Majorsilence.Forms.Button();
			this.bAlignment = new Majorsilence.Forms.Button();
			this.bDirection = new Majorsilence.Forms.Button();
			this.bVertical = new Majorsilence.Forms.Button();
			this.bWrtMode = new Majorsilence.Forms.Button();
			this.bFormat = new Majorsilence.Forms.Button();
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
			this.bFont.Click += this.bFont_Click;
			// 
			// cbVerticalAlign
			// 
			resources.ApplyResources(this.cbVerticalAlign, "cbVerticalAlign");
			this.cbVerticalAlign.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbVerticalAlign.Items.AddRange(new object[] {
            resources.GetString("cbVerticalAlign.Items"),
            resources.GetString("cbVerticalAlign.Items1"),
            resources.GetString("cbVerticalAlign.Items2")});
			this.cbVerticalAlign.Name = "cbVerticalAlign";
			this.cbVerticalAlign.SelectedIndexChanged += this.cbVerticalAlign_TextChanged;
			this.cbVerticalAlign.TextChanged += this.cbVerticalAlign_TextChanged;
			// 
			// cbHorzAlign
			// 
			resources.ApplyResources(this.cbHorzAlign, "cbHorzAlign");
			this.cbHorzAlign.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbHorzAlign.Items.AddRange(new object[] {
            resources.GetString("cbHorzAlign.Items"),
            resources.GetString("cbHorzAlign.Items1"),
            resources.GetString("cbHorzAlign.Items2"),
            resources.GetString("cbHorzAlign.Items3")});
			this.cbHorzAlign.Name = "cbHorzAlign";
			this.cbHorzAlign.SelectedIndexChanged += this.cbHorzAlign_TextChanged;
			this.cbHorzAlign.TextChanged += this.cbHorzAlign_TextChanged;
			// 
			// cbFormat
			// 
			resources.ApplyResources(this.cbFormat, "cbFormat");
			this.cbFormat.Name = "cbFormat";
			this.cbFormat.TextChanged += this.cbFormat_TextChanged;
			// 
			// cbDirection
			// 
			resources.ApplyResources(this.cbDirection, "cbDirection");
			this.cbDirection.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbDirection.Items.AddRange(new object[] {
            resources.GetString("cbDirection.Items"),
            resources.GetString("cbDirection.Items1")});
			this.cbDirection.Name = "cbDirection";
			this.cbDirection.SelectedIndexChanged += this.cbDirection_TextChanged;
			this.cbDirection.TextChanged += this.cbDirection_TextChanged;
			// 
			// cbWritingMode
			// 
			resources.ApplyResources(this.cbWritingMode, "cbWritingMode");
			this.cbWritingMode.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbWritingMode.Items.AddRange(new object[] {
            resources.GetString("cbWritingMode.Items"),
            resources.GetString("cbWritingMode.Items1"),
            resources.GetString("cbWritingMode.Items2")});
			this.cbWritingMode.Name = "cbWritingMode";
			this.cbWritingMode.SelectedIndexChanged += this.cbWritingMode_TextChanged;
			this.cbWritingMode.TextChanged += this.cbWritingMode_TextChanged;
			// 
			// label2
			// 
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			// 
			// cbTextDecoration
			// 
			resources.ApplyResources(this.cbTextDecoration, "cbTextDecoration");
			this.cbTextDecoration.Items.AddRange(new object[] {
            resources.GetString("cbTextDecoration.Items"),
            resources.GetString("cbTextDecoration.Items1"),
            resources.GetString("cbTextDecoration.Items2"),
            resources.GetString("cbTextDecoration.Items3")});
			this.cbTextDecoration.Name = "cbTextDecoration";
			this.cbTextDecoration.SelectedIndexChanged += this.cbTextDecoration_TextChanged;
			this.cbTextDecoration.TextChanged += this.cbTextDecoration_TextChanged;
			// 
			// bColor
			// 
			resources.ApplyResources(this.bColor, "bColor");
			this.bColor.Name = "bColor";
			this.bColor.Click += this.bColor_Click;
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
			this.cbColor.TextChanged += this.cbColor_TextChanged;
			// 
			// label3
			// 
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			// 
			// cbFontStyle
			// 
			resources.ApplyResources(this.cbFontStyle, "cbFontStyle");
			this.cbFontStyle.Items.AddRange(new object[] {
            resources.GetString("cbFontStyle.Items"),
            resources.GetString("cbFontStyle.Items1")});
			this.cbFontStyle.Name = "cbFontStyle";
			this.cbFontStyle.TextChanged += this.cbFontStyle_TextChanged;
			// 
			// cbFontWeight
			// 
			resources.ApplyResources(this.cbFontWeight, "cbFontWeight");
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
			this.cbFontWeight.Name = "cbFontWeight";
			this.cbFontWeight.TextChanged += this.cbFontWeight_TextChanged;
			// 
			// label10
			// 
			resources.ApplyResources(this.label10, "label10");
			this.label10.Name = "label10";
			// 
			// cbFontSize
			// 
			resources.ApplyResources(this.cbFontSize, "cbFontSize");
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
			this.cbFontSize.Name = "cbFontSize";
			this.cbFontSize.TextChanged += this.cbFontSize_TextChanged;
			// 
			// label11
			// 
			resources.ApplyResources(this.label11, "label11");
			this.label11.Name = "label11";
			// 
			// cbFontFamily
			// 
			resources.ApplyResources(this.cbFontFamily, "cbFontFamily");
			this.cbFontFamily.Items.AddRange(new object[] {
            resources.GetString("cbFontFamily.Items")});
			this.cbFontFamily.Name = "cbFontFamily";
			this.cbFontFamily.TextChanged += this.cbFontFamily_TextChanged;
			// 
			// groupBox1
			// 
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.Controls.Add(this.cbFontFamily);
			this.groupBox1.Controls.Add(this.cbTextDecoration);
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
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabStop = false;
			// 
			// button2
			// 
			resources.ApplyResources(this.button2, "button2");
			this.button2.Name = "button2";
			this.button2.Tag = "decoration";
			this.button2.Click += this.bExpr_Click;
			// 
			// bWeight
			// 
			resources.ApplyResources(this.bWeight, "bWeight");
			this.bWeight.Name = "bWeight";
			this.bWeight.Tag = "weight";
			this.bWeight.Click += this.bExpr_Click;
			// 
			// bSize
			// 
			resources.ApplyResources(this.bSize, "bSize");
			this.bSize.Name = "bSize";
			this.bSize.Tag = "size";
			this.bSize.Click += this.bExpr_Click;
			// 
			// bColorEx
			// 
			resources.ApplyResources(this.bColorEx, "bColorEx");
			this.bColorEx.Name = "bColorEx";
			this.bColorEx.Tag = "color";
			this.bColorEx.Click += this.bExpr_Click;
			// 
			// bStyle
			// 
			resources.ApplyResources(this.bStyle, "bStyle");
			this.bStyle.Name = "bStyle";
			this.bStyle.Tag = "style";
			this.bStyle.Click += this.bExpr_Click;
			// 
			// bFamily
			// 
			resources.ApplyResources(this.bFamily, "bFamily");
			this.bFamily.Name = "bFamily";
			this.bFamily.Tag = "family";
			this.bFamily.Click += this.bExpr_Click;
			// 
			// bAlignment
			// 
			resources.ApplyResources(this.bAlignment, "bAlignment");
			this.bAlignment.Name = "bAlignment";
			this.bAlignment.Tag = "halign";
			this.bAlignment.Click += this.bExpr_Click;
			// 
			// bDirection
			// 
			resources.ApplyResources(this.bDirection, "bDirection");
			this.bDirection.Name = "bDirection";
			this.bDirection.Tag = "direction";
			this.bDirection.Click += this.bExpr_Click;
			// 
			// bVertical
			// 
			resources.ApplyResources(this.bVertical, "bVertical");
			this.bVertical.Name = "bVertical";
			this.bVertical.Tag = "valign";
			this.bVertical.Click += this.bExpr_Click;
			// 
			// bWrtMode
			// 
			resources.ApplyResources(this.bWrtMode, "bWrtMode");
			this.bWrtMode.Name = "bWrtMode";
			this.bWrtMode.Tag = "writing";
			this.bWrtMode.Click += this.bExpr_Click;
			// 
			// bFormat
			// 
			resources.ApplyResources(this.bFormat, "bFormat");
			this.bFormat.Name = "bFormat";
			this.bFormat.Tag = "format";
			this.bFormat.Click += this.bExpr_Click;
			// 
			// FontCtl
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.bFormat);
			this.Controls.Add(this.bWrtMode);
			this.Controls.Add(this.bVertical);
			this.Controls.Add(this.bDirection);
			this.Controls.Add(this.bAlignment);
			this.Controls.Add(this.groupBox1);
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
			this.Name = "FontCtl";
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
					if (!this.cbFontSize.Text.Trim().StartsWith("="))
						DesignerUtility.ValidateSize(this.cbFontSize.Text, false, false);
				}
				catch (Exception e)
				{
					MessageBox.Show(e.Message, Strings.FontCtl_Show_InvalidFontSize);
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
				fColor = fVerticalAlign = fFontStyle = fFontWeight = fFontSize = fFontFamily = false;
		}

		public void ApplyChanges(XmlNode node)
		{
            if (_names != null)
            {
                node = _Draw.FindCreateNextInHierarchy(node, _names);
            }

            XmlNode sNode = _Draw.GetCreateNamedChildNode(node, "Style");

			if (fFontStyle)
				_Draw.SetElement(sNode, "FontStyle", cbFontStyle.Text);
			if (fFontFamily)
				_Draw.SetElement(sNode, "FontFamily", cbFontFamily.Text);
			if (fFontWeight)
				_Draw.SetElement(sNode, "FontWeight", cbFontWeight.Text);

			if (fFontSize)
			{
				float size=10;
				size = DesignXmlDraw.GetSize(cbFontSize.Text);
				if (size <= 0)
				{
					size = DesignXmlDraw.GetSize(cbFontSize.Text+"pt");	// Try assuming pt
					if (size <= 0)	// still no good
						size = 10;	// just set default value
				}
				string rs = string.Format(NumberFormatInfo.InvariantInfo, "{0:0.#}pt", size);

				_Draw.SetElement(sNode, "FontSize", rs);	// force to string
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
			Majorsilence.Forms.Drawing.FontStyle fs = 0;
			if (cbFontStyle.Text == "Italic")
				fs |= Majorsilence.Forms.Drawing.FontStyle.Italic;

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
					fs |= Majorsilence.Forms.Drawing.FontStyle.Bold;
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
                cbColor.Text = Majorsilence.Forms.ColorTranslator.ToHtml(fd.Color);
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
			ColorDialog cd = new ColorDialog();
			cd.AnyColor = true;
			cd.FullOpen = true;
			
			cd.CustomColors = RdlDesigner.GetCustomColors();
			cd.Color = 
				DesignerUtility.ColorFromHtml(cbColor.Text, System.Drawing.Color.Black);
            try
            {
                if (cd.ShowDialog() != DialogResult.OK)
                    return;

                RdlDesigner.SetCustomColors(cd.CustomColors);
                if (sender == this.bColor)
                    cbColor.Text = Majorsilence.Forms.ColorTranslator.ToHtml(cd.Color);
            }
            finally
            {
                cd.Dispose();
            }
			return;
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

			DialogExprEditor ee = new DialogExprEditor(_Draw, c.Text, sNode, bColor);
            try
            {
                DialogResult dr = ee.ShowDialog();
                if (dr == DialogResult.OK)
                    c.Text = ee.Expression;
            }
            finally
            {
                ee.Dispose();
            }
			return;
		}

	}
}
