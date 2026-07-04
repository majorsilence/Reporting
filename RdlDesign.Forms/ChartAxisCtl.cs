
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
	internal class ChartAxisCtl : Majorsilence.Forms.UserControl, IProperty
	{
        private List<XmlNode> _ReportItems;
		private DesignXmlDraw _Draw;
		// change flags
		bool fMonth, fVisible, fMajorTickMarks, fMargin,fReverse,fInterlaced;
		bool fMajorGLWidth,fMajorGLColor,fMajorGLStyle;
		bool fMinorGLWidth,fMinorGLColor,fMinorGLStyle;
		bool fMajorInterval, fMinorInterval,fMax,fMin;
		bool fMinorTickMarks,fScalar,fLogScale,fMajorGLShow, fMinorGLShow, fCanOmit;
		
		private Majorsilence.Forms.Label label1;
		private Majorsilence.Forms.Label label2;
        private Majorsilence.Forms.CheckBox chkMonth;
		private Majorsilence.Forms.CheckBox chkVisible;
		private Majorsilence.Forms.ComboBox cbMajorTickMarks;
		private Majorsilence.Forms.CheckBox chkMargin;
		private Majorsilence.Forms.CheckBox chkReverse;
		private Majorsilence.Forms.CheckBox chkInterlaced;
		private Majorsilence.Forms.GroupBox groupBox1;
		private Majorsilence.Forms.TextBox tbMajorGLWidth;
		private Majorsilence.Forms.Button bMajorGLColor;
		private Majorsilence.Forms.ComboBox cbMajorGLColor;
		private Majorsilence.Forms.ComboBox cbMajorGLStyle;
		private Majorsilence.Forms.Label label7;
		private Majorsilence.Forms.Label label6;
		private Majorsilence.Forms.Label label3;
		private Majorsilence.Forms.GroupBox groupBox2;
		private Majorsilence.Forms.TextBox tbMinorGLWidth;
		private Majorsilence.Forms.Button bMinorGLColor;
		private Majorsilence.Forms.ComboBox cbMinorGLColor;
		private Majorsilence.Forms.ComboBox cbMinorGLStyle;
		private Majorsilence.Forms.Label label4;
		private Majorsilence.Forms.Label label5;
		private Majorsilence.Forms.Label label8;
		private Majorsilence.Forms.Label label9;
		private Majorsilence.Forms.TextBox tbMajorInterval;
		private Majorsilence.Forms.TextBox tbMinorInterval;
		private Majorsilence.Forms.Label label10;
		private Majorsilence.Forms.TextBox tbMax;
		private Majorsilence.Forms.Label label11;
		private Majorsilence.Forms.TextBox tbMin;
		private Majorsilence.Forms.Label label12;
		private Majorsilence.Forms.ComboBox cbMinorTickMarks;
		private Majorsilence.Forms.CheckBox chkScalar;
		private Majorsilence.Forms.CheckBox chkLogScale;
		private Majorsilence.Forms.CheckBox chkMajorGLShow;
		private Majorsilence.Forms.CheckBox chkMinorGLShow;
		private Majorsilence.Forms.Button bMinorIntervalExpr;
		private Majorsilence.Forms.Button bMajorIntervalExpr;
		private Majorsilence.Forms.Button bMinExpr;
		private Majorsilence.Forms.Button bMaxExpr;
        private CheckBox chkCanOmit;
        
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        internal ChartAxisCtl(DesignXmlDraw dxDraw, List<XmlNode> ris)
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
            cbMajorGLColor.Items.AddRange(StaticLists.ColorList);
            cbMinorGLColor.Items.AddRange(StaticLists.ColorList);

			XmlNode node = _ReportItems[0];
            chkMonth.Checked = _Draw.GetElementValue(node, "fyi:Month", "false").ToLower() == "true" ? true : false; //added checkbox for month category axis WP 12 may 2008
			chkVisible.Checked = _Draw.GetElementValue(node, "Visible", "false").ToLower() == "true"? true: false;
			chkMargin.Checked = _Draw.GetElementValue(node, "Margin", "false").ToLower() == "true"? true: false;
			chkReverse.Checked = _Draw.GetElementValue(node, "Reverse", "false").ToLower() == "true"? true: false;
			chkInterlaced.Checked = _Draw.GetElementValue(node, "Interlaced", "false").ToLower() == "true"? true: false;
			chkScalar.Checked = _Draw.GetElementValue(node, "Scalar", "false").ToLower() == "true"? true: false;
			chkLogScale.Checked = _Draw.GetElementValue(node, "LogScale", "false").ToLower() == "true"? true: false;
            chkCanOmit.Checked = _Draw.GetElementValue(node, "fyi:CanOmit", "false").ToLower() == "true" ? true : false;
            cbMajorTickMarks.Text = _Draw.GetElementValue(node, "MajorTickMarks", "None");
			cbMinorTickMarks.Text = _Draw.GetElementValue(node, "MinorTickMarks", "None");
			// Major Grid Lines
			InitGridLines(node, "MajorGridLines", chkMajorGLShow, cbMajorGLColor, cbMajorGLStyle, tbMajorGLWidth);
			// Minor Grid Lines
			InitGridLines(node, "MinorGridLines", chkMinorGLShow, cbMinorGLColor, cbMinorGLStyle, tbMinorGLWidth);

			tbMajorInterval.Text = _Draw.GetElementValue(node, "MajorInterval", "");
			tbMinorInterval.Text = _Draw.GetElementValue(node, "MinorInterval", "");
			tbMax.Text = _Draw.GetElementValue(node, "Max", "");
			tbMin.Text = _Draw.GetElementValue(node, "Min", "");

			    fMonth = fVisible = fMajorTickMarks = fMargin=fReverse=fInterlaced=
				fMajorGLWidth=fMajorGLColor=fMajorGLStyle=
				fMinorGLWidth=fMinorGLColor=fMinorGLStyle=
				fMajorInterval= fMinorInterval=fMax=fMin=
				fMinorTickMarks=fScalar=fLogScale=fMajorGLShow=fMinorGLShow=fCanOmit=false;
		}

		private void InitGridLines(XmlNode node, string type, CheckBox show, ComboBox color, ComboBox style, TextBox width)
		{
			XmlNode m = _Draw.GetNamedChildNode(node, type);
			if (m != null)
			{
				show.Checked = _Draw.GetElementValue(m, "ShowGridLines", "false").ToLower() == "true"? true: false;
				XmlNode st = _Draw.GetNamedChildNode(m, "Style");
				if (st != null)
				{
					XmlNode work = _Draw.GetNamedChildNode(st, "BorderColor");
					if (work != null)
						color.Text = _Draw.GetElementValue(work, "Default", "Black");
					work = _Draw.GetNamedChildNode(st, "BorderStyle");
					if (work != null)
						style.Text = _Draw.GetElementValue(work, "Default", "Solid");
					work = _Draw.GetNamedChildNode(st, "BorderWidth");
					if (work != null)
						width.Text = _Draw.GetElementValue(work, "Default", "1pt");
				}
			}
			if (color.Text.Length == 0)
				color.Text = "Black";
			if (style.Text.Length == 0)
				style.Text = "Solid";
			if (width.Text.Length == 0)
				width.Text = "1pt";
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
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(ChartAxisCtl));
            this.DoubleBuffered = true;
			this.label1 = new Majorsilence.Forms.Label();
			this.label2 = new Majorsilence.Forms.Label();
			this.cbMajorTickMarks = new Majorsilence.Forms.ComboBox();
			this.cbMinorTickMarks = new Majorsilence.Forms.ComboBox();
			this.chkVisible = new Majorsilence.Forms.CheckBox();
			this.chkMargin = new Majorsilence.Forms.CheckBox();
			this.chkReverse = new Majorsilence.Forms.CheckBox();
			this.chkInterlaced = new Majorsilence.Forms.CheckBox();
			this.chkScalar = new Majorsilence.Forms.CheckBox();
			this.chkLogScale = new Majorsilence.Forms.CheckBox();
			this.groupBox1 = new Majorsilence.Forms.GroupBox();
			this.chkMajorGLShow = new Majorsilence.Forms.CheckBox();
			this.tbMajorGLWidth = new Majorsilence.Forms.TextBox();
			this.bMajorGLColor = new Majorsilence.Forms.Button();
			this.cbMajorGLColor = new Majorsilence.Forms.ComboBox();
			this.cbMajorGLStyle = new Majorsilence.Forms.ComboBox();
			this.label7 = new Majorsilence.Forms.Label();
			this.label6 = new Majorsilence.Forms.Label();
			this.label3 = new Majorsilence.Forms.Label();
			this.groupBox2 = new Majorsilence.Forms.GroupBox();
			this.chkMinorGLShow = new Majorsilence.Forms.CheckBox();
			this.tbMinorGLWidth = new Majorsilence.Forms.TextBox();
			this.bMinorGLColor = new Majorsilence.Forms.Button();
			this.cbMinorGLColor = new Majorsilence.Forms.ComboBox();
			this.cbMinorGLStyle = new Majorsilence.Forms.ComboBox();
			this.label4 = new Majorsilence.Forms.Label();
			this.label5 = new Majorsilence.Forms.Label();
			this.label8 = new Majorsilence.Forms.Label();
			this.label9 = new Majorsilence.Forms.Label();
			this.tbMajorInterval = new Majorsilence.Forms.TextBox();
			this.tbMinorInterval = new Majorsilence.Forms.TextBox();
			this.label10 = new Majorsilence.Forms.Label();
			this.tbMax = new Majorsilence.Forms.TextBox();
			this.label11 = new Majorsilence.Forms.Label();
			this.tbMin = new Majorsilence.Forms.TextBox();
			this.label12 = new Majorsilence.Forms.Label();
			this.bMinorIntervalExpr = new Majorsilence.Forms.Button();
			this.bMajorIntervalExpr = new Majorsilence.Forms.Button();
			this.bMinExpr = new Majorsilence.Forms.Button();
			this.bMaxExpr = new Majorsilence.Forms.Button();
			this.chkCanOmit = new Majorsilence.Forms.CheckBox();
			this.chkMonth = new Majorsilence.Forms.CheckBox();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
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
			// cbMajorTickMarks
			// 
			resources.ApplyResources(this.cbMajorTickMarks, "cbMajorTickMarks");
			this.cbMajorTickMarks.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbMajorTickMarks.Items.AddRange(new object[] {
            resources.GetString("cbMajorTickMarks.Items"),
            resources.GetString("cbMajorTickMarks.Items1"),
            resources.GetString("cbMajorTickMarks.Items2"),
            resources.GetString("cbMajorTickMarks.Items3")});
			this.cbMajorTickMarks.Name = "cbMajorTickMarks";
			this.cbMajorTickMarks.SelectedIndexChanged += this.cbMajorTickMarks_SelectedIndexChanged;
			// 
			// cbMinorTickMarks
			// 
			resources.ApplyResources(this.cbMinorTickMarks, "cbMinorTickMarks");
			this.cbMinorTickMarks.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbMinorTickMarks.Items.AddRange(new object[] {
            resources.GetString("cbMinorTickMarks.Items"),
            resources.GetString("cbMinorTickMarks.Items1"),
            resources.GetString("cbMinorTickMarks.Items2"),
            resources.GetString("cbMinorTickMarks.Items3")});
			this.cbMinorTickMarks.Name = "cbMinorTickMarks";
			this.cbMinorTickMarks.SelectedIndexChanged += this.cbMinorTickMarks_SelectedIndexChanged;
			// 
			// chkVisible
			// 
			resources.ApplyResources(this.chkVisible, "chkVisible");
			this.chkVisible.Name = "chkVisible";
			this.chkVisible.CheckedChanged += this.chkVisible_CheckedChanged;
			// 
			// chkMargin
			// 
			resources.ApplyResources(this.chkMargin, "chkMargin");
			this.chkMargin.Name = "chkMargin";
			this.chkMargin.CheckedChanged += this.chkMargin_CheckedChanged;
			// 
			// chkReverse
			// 
			resources.ApplyResources(this.chkReverse, "chkReverse");
			this.chkReverse.Name = "chkReverse";
			this.chkReverse.CheckedChanged += this.chkReverse_CheckedChanged;
			// 
			// chkInterlaced
			// 
			resources.ApplyResources(this.chkInterlaced, "chkInterlaced");
			this.chkInterlaced.Name = "chkInterlaced";
			this.chkInterlaced.CheckedChanged += this.chkInterlaced_CheckedChanged;
			// 
			// chkScalar
			// 
			resources.ApplyResources(this.chkScalar, "chkScalar");
			this.chkScalar.Name = "chkScalar";
			this.chkScalar.CheckedChanged += this.chkScalar_CheckedChanged;
			// 
			// chkLogScale
			// 
			resources.ApplyResources(this.chkLogScale, "chkLogScale");
			this.chkLogScale.Name = "chkLogScale";
			this.chkLogScale.CheckedChanged += this.chkLogScale_CheckedChanged;
			// 
			// groupBox1
			// 
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.Controls.Add(this.cbMajorGLStyle);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.chkMajorGLShow);
			this.groupBox1.Controls.Add(this.tbMajorGLWidth);
			this.groupBox1.Controls.Add(this.bMajorGLColor);
			this.groupBox1.Controls.Add(this.cbMajorGLColor);
			this.groupBox1.Controls.Add(this.label7);
			this.groupBox1.Controls.Add(this.label6);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabStop = false;
			// 
			// chkMajorGLShow
			// 
			resources.ApplyResources(this.chkMajorGLShow, "chkMajorGLShow");
			this.chkMajorGLShow.Name = "chkMajorGLShow";
			this.chkMajorGLShow.CheckedChanged += this.chkMajorGLShow_CheckedChanged;
			// 
			// tbMajorGLWidth
			// 
			resources.ApplyResources(this.tbMajorGLWidth, "tbMajorGLWidth");
			this.tbMajorGLWidth.Name = "tbMajorGLWidth";
			this.tbMajorGLWidth.TextChanged += this.tbMajorGLWidth_TextChanged;
			// 
			// bMajorGLColor
			// 
			resources.ApplyResources(this.bMajorGLColor, "bMajorGLColor");
			this.bMajorGLColor.Name = "bMajorGLColor";
			this.bMajorGLColor.Click += this.bMajorGLColor_Click;
			// 
			// cbMajorGLColor
			// 
			resources.ApplyResources(this.cbMajorGLColor, "cbMajorGLColor");
			this.cbMajorGLColor.Name = "cbMajorGLColor";
			this.cbMajorGLColor.SelectedIndexChanged += this.cbMajorGLColor_SelectedIndexChanged;
			// 
			// cbMajorGLStyle
			// 
			resources.ApplyResources(this.cbMajorGLStyle, "cbMajorGLStyle");
			this.cbMajorGLStyle.Items.AddRange(new object[] {
            resources.GetString("cbMajorGLStyle.Items"),
            resources.GetString("cbMajorGLStyle.Items1"),
            resources.GetString("cbMajorGLStyle.Items2"),
            resources.GetString("cbMajorGLStyle.Items3"),
            resources.GetString("cbMajorGLStyle.Items4"),
            resources.GetString("cbMajorGLStyle.Items5"),
            resources.GetString("cbMajorGLStyle.Items6"),
            resources.GetString("cbMajorGLStyle.Items7"),
            resources.GetString("cbMajorGLStyle.Items8"),
            resources.GetString("cbMajorGLStyle.Items9")});
			this.cbMajorGLStyle.Name = "cbMajorGLStyle";
			this.cbMajorGLStyle.SelectedIndexChanged += this.cbMajorGLStyle_SelectedIndexChanged;
			// 
			// label7
			// 
			resources.ApplyResources(this.label7, "label7");
			this.label7.Name = "label7";
			// 
			// label6
			// 
			resources.ApplyResources(this.label6, "label6");
			this.label6.Name = "label6";
			// 
			// label3
			// 
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			// 
			// groupBox2
			// 
			resources.ApplyResources(this.groupBox2, "groupBox2");
			this.groupBox2.Controls.Add(this.cbMinorGLStyle);
			this.groupBox2.Controls.Add(this.label8);
			this.groupBox2.Controls.Add(this.chkMinorGLShow);
			this.groupBox2.Controls.Add(this.tbMinorGLWidth);
			this.groupBox2.Controls.Add(this.bMinorGLColor);
			this.groupBox2.Controls.Add(this.cbMinorGLColor);
			this.groupBox2.Controls.Add(this.label4);
			this.groupBox2.Controls.Add(this.label5);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.TabStop = false;
			// 
			// chkMinorGLShow
			// 
			resources.ApplyResources(this.chkMinorGLShow, "chkMinorGLShow");
			this.chkMinorGLShow.Name = "chkMinorGLShow";
			this.chkMinorGLShow.CheckedChanged += this.chkMinorGLShow_CheckedChanged;
			// 
			// tbMinorGLWidth
			// 
			resources.ApplyResources(this.tbMinorGLWidth, "tbMinorGLWidth");
			this.tbMinorGLWidth.Name = "tbMinorGLWidth";
			this.tbMinorGLWidth.TextChanged += this.tbMinorGLWidth_TextChanged;
			// 
			// bMinorGLColor
			// 
			resources.ApplyResources(this.bMinorGLColor, "bMinorGLColor");
			this.bMinorGLColor.Name = "bMinorGLColor";
			this.bMinorGLColor.Click += this.bMinorGLColor_Click;
			// 
			// cbMinorGLColor
			// 
			resources.ApplyResources(this.cbMinorGLColor, "cbMinorGLColor");
			this.cbMinorGLColor.Name = "cbMinorGLColor";
			this.cbMinorGLColor.SelectedIndexChanged += this.cbMinorGLColor_SelectedIndexChanged;
			// 
			// cbMinorGLStyle
			// 
			resources.ApplyResources(this.cbMinorGLStyle, "cbMinorGLStyle");
			this.cbMinorGLStyle.Items.AddRange(new object[] {
            resources.GetString("cbMinorGLStyle.Items"),
            resources.GetString("cbMinorGLStyle.Items1"),
            resources.GetString("cbMinorGLStyle.Items2"),
            resources.GetString("cbMinorGLStyle.Items3"),
            resources.GetString("cbMinorGLStyle.Items4"),
            resources.GetString("cbMinorGLStyle.Items5"),
            resources.GetString("cbMinorGLStyle.Items6"),
            resources.GetString("cbMinorGLStyle.Items7"),
            resources.GetString("cbMinorGLStyle.Items8"),
            resources.GetString("cbMinorGLStyle.Items9")});
			this.cbMinorGLStyle.Name = "cbMinorGLStyle";
			this.cbMinorGLStyle.SelectedIndexChanged += this.cbMinorGLStyle_SelectedIndexChanged;
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
			// label8
			// 
			resources.ApplyResources(this.label8, "label8");
			this.label8.Name = "label8";
			// 
			// label9
			// 
			resources.ApplyResources(this.label9, "label9");
			this.label9.Name = "label9";
			// 
			// tbMajorInterval
			// 
			resources.ApplyResources(this.tbMajorInterval, "tbMajorInterval");
			this.tbMajorInterval.Name = "tbMajorInterval";
			this.tbMajorInterval.TextChanged += this.tbMajorInterval_TextChanged;
			// 
			// tbMinorInterval
			// 
			resources.ApplyResources(this.tbMinorInterval, "tbMinorInterval");
			this.tbMinorInterval.Name = "tbMinorInterval";
			this.tbMinorInterval.TextChanged += this.tbMinorInterval_TextChanged;
			// 
			// label10
			// 
			resources.ApplyResources(this.label10, "label10");
			this.label10.Name = "label10";
			// 
			// tbMax
			// 
			resources.ApplyResources(this.tbMax, "tbMax");
			this.tbMax.Name = "tbMax";
			this.tbMax.TextChanged += this.tbMax_TextChanged;
			// 
			// label11
			// 
			resources.ApplyResources(this.label11, "label11");
			this.label11.Name = "label11";
			// 
			// tbMin
			// 
			resources.ApplyResources(this.tbMin, "tbMin");
			this.tbMin.Name = "tbMin";
			this.tbMin.TextChanged += this.tbMin_TextChanged;
			// 
			// label12
			// 
			resources.ApplyResources(this.label12, "label12");
			this.label12.Name = "label12";
			// 
			// bMinorIntervalExpr
			// 
			resources.ApplyResources(this.bMinorIntervalExpr, "bMinorIntervalExpr");
			this.bMinorIntervalExpr.Name = "bMinorIntervalExpr";
			this.bMinorIntervalExpr.Tag = "minorinterval";
			this.bMinorIntervalExpr.Click += this.bExpr_Click;
			// 
			// bMajorIntervalExpr
			// 
			resources.ApplyResources(this.bMajorIntervalExpr, "bMajorIntervalExpr");
			this.bMajorIntervalExpr.Name = "bMajorIntervalExpr";
			this.bMajorIntervalExpr.Tag = "majorinterval";
			this.bMajorIntervalExpr.Click += this.bExpr_Click;
			// 
			// bMinExpr
			// 
			resources.ApplyResources(this.bMinExpr, "bMinExpr");
			this.bMinExpr.Name = "bMinExpr";
			this.bMinExpr.Tag = "min";
			this.bMinExpr.Click += this.bExpr_Click;
			// 
			// bMaxExpr
			// 
			resources.ApplyResources(this.bMaxExpr, "bMaxExpr");
			this.bMaxExpr.Name = "bMaxExpr";
			this.bMaxExpr.Tag = "max";
			this.bMaxExpr.Click += this.bExpr_Click;
			// 
			// chkCanOmit
			// 
			resources.ApplyResources(this.chkCanOmit, "chkCanOmit");
			this.chkCanOmit.Name = "chkCanOmit";
			this.chkCanOmit.CheckedChanged += this.chkCanOmit_CheckedChanged;
			// 
			// chkMonth
			// 
			resources.ApplyResources(this.chkMonth, "chkMonth");
			this.chkMonth.Name = "chkMonth";
			this.chkMonth.CheckedChanged += this.chkMonth_CheckedChanged;
			// 
			// ChartAxisCtl
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.chkReverse);
			this.Controls.Add(this.chkMonth);
			this.Controls.Add(this.chkCanOmit);
			this.Controls.Add(this.bMaxExpr);
			this.Controls.Add(this.bMinExpr);
			this.Controls.Add(this.bMajorIntervalExpr);
			this.Controls.Add(this.bMinorIntervalExpr);
			this.Controls.Add(this.tbMax);
			this.Controls.Add(this.label11);
			this.Controls.Add(this.tbMin);
			this.Controls.Add(this.label12);
			this.Controls.Add(this.tbMinorInterval);
			this.Controls.Add(this.label10);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.chkLogScale);
			this.Controls.Add(this.chkScalar);
			this.Controls.Add(this.chkInterlaced);
			this.Controls.Add(this.chkMargin);
			this.Controls.Add(this.chkVisible);
			this.Controls.Add(this.cbMinorTickMarks);
			this.Controls.Add(this.cbMajorTickMarks);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.tbMajorInterval);
			this.Controls.Add(this.label9);
			this.Name = "ChartAxisCtl";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
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

			    fMonth = fVisible = fMajorTickMarks = fMargin=fReverse=fInterlaced=
				fMajorGLWidth=fMajorGLColor=fMajorGLStyle=
				fMinorGLWidth=fMinorGLColor=fMinorGLStyle=
				fMajorInterval= fMinorInterval=fMax=fMin=
				fMinorTickMarks=fScalar=fLogScale=fMajorGLShow=fMinorGLShow=fCanOmit=false;
		}

		public void ApplyChanges(XmlNode node)
		{
            if (fMonth)
            {
                _Draw.SetElement(node, "fyi:Month", this.chkMonth.Checked? "true" : "false");
            }
			if (fVisible)
			{
				_Draw.SetElement(node, "Visible", this.chkVisible.Checked? "true": "false");
			}
			if (fMajorTickMarks)
			{
				_Draw.SetElement(node, "MajorTickMarks", this.cbMajorTickMarks.Text);
			}
			if (fMargin)
			{
				_Draw.SetElement(node, "Margin", this.chkMargin.Checked? "true": "false");
			}
			if (fReverse)
			{
				_Draw.SetElement(node, "Reverse", this.chkReverse.Checked? "true": "false");
			}
			if (fInterlaced)
			{
				_Draw.SetElement(node, "Interlaced", this.chkInterlaced.Checked? "true": "false");
			}
			if (fMajorGLShow || fMajorGLWidth || fMajorGLColor || fMajorGLStyle)
			{
				ApplyGridLines(node, "MajorGridLines", chkMajorGLShow, cbMajorGLColor, cbMajorGLStyle, tbMajorGLWidth);
			}
			if (fMinorGLShow || fMinorGLWidth || fMinorGLColor || fMinorGLStyle)
			{
				ApplyGridLines(node, "MinorGridLines", chkMinorGLShow, cbMinorGLColor, cbMinorGLStyle, tbMinorGLWidth);
			}
			if (fMajorInterval)
			{
				_Draw.SetElement(node, "MajorInterval", this.tbMajorInterval.Text);
			}
			if (fMinorInterval)
			{
				_Draw.SetElement(node, "MinorInterval", this.tbMinorInterval.Text);
			}
			if (fMax)
			{
				_Draw.SetElement(node, "Max", this.tbMax.Text);
			}
			if (fMin)
			{
				_Draw.SetElement(node, "Min", this.tbMin.Text);
			}
			if (fMinorTickMarks)
			{
				_Draw.SetElement(node, "MinorTickMarks", this.cbMinorTickMarks.Text);
			}
			if (fScalar)
			{
				_Draw.SetElement(node, "Scalar", this.chkScalar.Checked? "true": "false");
			}
			if (fLogScale)
			{
				_Draw.SetElement(node, "LogScale", this.chkLogScale.Checked? "true": "false");
			}
            if (fCanOmit)
            {
                _Draw.SetElement(node, "fyi:CanOmit", this.chkCanOmit.Checked ? "true" : "false");
            }
        }

		private void ApplyGridLines(XmlNode node, string type, CheckBox show, ComboBox color, ComboBox style, TextBox width)
		{
			XmlNode m = _Draw.GetNamedChildNode(node, type);
			if (m == null)
			{
				m = _Draw.CreateElement(node, type, null);
			}

			_Draw.SetElement(m, "ShowGridLines", show.Checked? "true": "false");
			XmlNode st = _Draw.GetNamedChildNode(m, "Style");
			if (st == null)
				st = _Draw.CreateElement(m, "Style", null);

			XmlNode work = _Draw.GetNamedChildNode(st, "BorderColor");
			if (work == null)
				work = _Draw.CreateElement(st, "BorderColor", null);
			_Draw.SetElement(work, "Default", color.Text);

			work = _Draw.GetNamedChildNode(st, "BorderStyle");
			if (work == null)
				work = _Draw.CreateElement(st, "BorderStyle", null);
			_Draw.SetElement(work, "Default", style.Text);
			
			work = _Draw.GetNamedChildNode(st, "BorderWidth");
			if (work == null)
				work = _Draw.CreateElement(st, "BorderWidth", null);
			_Draw.SetElement(work, "Default", width.Text);
		}

		private void cbMajorTickMarks_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			fMajorTickMarks = true;
		}

		private void cbMinorTickMarks_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			fMinorTickMarks = true;
		}

		private void cbMajorGLStyle_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			fMajorGLStyle = true;
		}

		private void cbMajorGLColor_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			fMajorGLColor = true;
		}

		private void tbMajorGLWidth_TextChanged(object sender, System.EventArgs e)
		{
			fMajorGLWidth = true;
		}

		private void cbMinorGLStyle_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			fMinorGLStyle = true;
		}

		private void cbMinorGLColor_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			fMinorGLColor = true;
		}

		private void tbMinorGLWidth_TextChanged(object sender, System.EventArgs e)
		{
			fMinorGLWidth = true;
		}

		private void tbMajorInterval_TextChanged(object sender, System.EventArgs e)
		{
			fMajorInterval = true;
		}

		private void tbMinorInterval_TextChanged(object sender, System.EventArgs e)
		{
			fMinorInterval = true;
		}

		private void tbMin_TextChanged(object sender, System.EventArgs e)
		{
			fMin = true;
		}

		private void tbMax_TextChanged(object sender, System.EventArgs e)
		{
			fMax = true;
		}

        private void chkMonth_CheckedChanged(object sender, System.EventArgs e)
        {
            fMonth = true;
        }

		private void chkVisible_CheckedChanged(object sender, System.EventArgs e)
		{
			fVisible = true;
		}

		private void chkLogScale_CheckedChanged(object sender, System.EventArgs e)
		{
			fLogScale = true;
		}

        private void chkCanOmit_CheckedChanged(object sender, System.EventArgs e)
        {
            fCanOmit = true;
        }

		private void chkMargin_CheckedChanged(object sender, System.EventArgs e)
		{
			fMargin = true;
		}

		private void chkScalar_CheckedChanged(object sender, System.EventArgs e)
		{
			fScalar = true;
		}

		private void chkReverse_CheckedChanged(object sender, System.EventArgs e)
		{
			fReverse = true;
		}

		private void chkInterlaced_CheckedChanged(object sender, System.EventArgs e)
		{
			fInterlaced = true;
		}

		private void chkMajorGLShow_CheckedChanged(object sender, System.EventArgs e)
		{
			fMajorGLShow = true;
		}

		private void chkMinorGLShow_CheckedChanged(object sender, System.EventArgs e)
		{
			fMinorGLShow = true;
		}

		private void bMajorGLColor_Click(object sender, System.EventArgs e)
		{
			SetColor(this.cbMajorGLColor);		
		}

		private void bMinorGLColor_Click(object sender, System.EventArgs e)
		{
			SetColor(this.cbMinorGLColor);		
		}

		private void SetColor(ComboBox cbColor)
		{
			ColorDialog cd = new ColorDialog();
			cd.AnyColor = true;
			cd.FullOpen = true;
			
			cd.CustomColors = RdlDesigner.GetCustomColors();
			cd.Color = DesignerUtility.ColorFromHtml(cbColor.Text, System.Drawing.Color.Empty);

            try
            {
                if (cd.ShowDialog() != DialogResult.OK)
                    return;

                RdlDesigner.SetCustomColors(cd.CustomColors);
                cbColor.Text = ColorTranslator.ToHtml(cd.Color);
            }
            finally
            {
                cd.Dispose();
            }

			return;
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
				case "min":
					c = this.tbMin;
					break;
				case "max":
					c = this.tbMax;
					break;
				case "majorinterval":
					c = this.tbMajorInterval;
					break;
				case "minorinterval":
					c = this.tbMinorInterval;
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
