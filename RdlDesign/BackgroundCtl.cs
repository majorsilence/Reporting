
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Majorsilence.Forms;
using System.Xml;
using Majorsilence.Reporting.RdlDesign.Resources;

namespace Majorsilence.Reporting.RdlDesign
{
	/// <summary>
	/// Summary description for StyleCtl.
	/// </summary>
	internal class BackgroundCtl : Majorsilence.Forms.UserControl, IProperty
	{
        private List<XmlNode> _ReportItems;
		private DesignXmlDraw _Draw;
		// flags for controlling whether syntax changed for a particular property
		private bool fEndColor, fBackColor, fGradient, fBackImage;
		private Majorsilence.Forms.GroupBox groupBox1;
		private Majorsilence.Forms.Label label3;
		private Majorsilence.Forms.Button bBackColor;
		private Majorsilence.Forms.Label label10;
		private Majorsilence.Forms.Label label15;
		private Majorsilence.Forms.ComboBox cbEndColor;
		private Majorsilence.Forms.ComboBox cbBackColor;
		private Majorsilence.Forms.Button bEndColor;
        private Majorsilence.Forms.ComboBox cbGradient;
		private Majorsilence.Forms.Button bGradient;
		private Majorsilence.Forms.Button bExprBackColor;
        private Majorsilence.Forms.Button bExprEndColor;
        private GroupBox groupBox2;
        private Button bExternalExpr;
        private Button bEmbeddedExpr;
        private Button bMimeExpr;
        private Button bDatabaseExpr;
        private Button bEmbedded;
        private Button bExternal;
        private TextBox tbValueExternal;
        private ComboBox cbValueDatabase;
        private ComboBox cbMIMEType;
        private ComboBox cbValueEmbedded;
        private RadioButton rbDatabase;
        private RadioButton rbEmbedded;
        private RadioButton rbExternal;
        private RadioButton rbNone;
        private ComboBox cbRepeat;
        private Label label1;
        private Button bRepeatExpr;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private string[] _names;

        internal BackgroundCtl(DesignXmlDraw dxDraw, string[] names, List<XmlNode> reportItems)
		{
			_ReportItems = reportItems;
			_Draw = dxDraw;
            _names = names;
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Initialize form using the style node values
			InitValues(_ReportItems[0]);			
		}

		private void InitValues(XmlNode node)
		{
            cbEndColor.Items.AddRange(StaticLists.ColorList);
            cbBackColor.Items.AddRange(StaticLists.ColorList);
            cbGradient.Items.AddRange(StaticLists.GradientList);

            if (_names != null)
            {
                node = _Draw.FindCreateNextInHierarchy(node, _names);
            }

            XmlNode sNode = _Draw.GetNamedChildNode(node, "Style");

			this.cbBackColor.Text = _Draw.GetElementValue(sNode, "BackgroundColor", "");
			this.cbEndColor.Text = _Draw.GetElementValue(sNode, "BackgroundGradientEndColor", "");
			this.cbGradient.Text = _Draw.GetElementValue(sNode, "BackgroundGradientType", "None");
			if (node.Name != "Chart")
			{   // only chart support gradients
				this.cbEndColor.Enabled = bExprEndColor.Enabled =
					cbGradient.Enabled = bGradient.Enabled = 
					this.bEndColor.Enabled = bExprEndColor.Enabled = false;
			}

            cbValueEmbedded.Items.AddRange(_Draw.ReportNames.EmbeddedImageNames);
            string[] flds = _Draw.GetReportItemDataRegionFields(node, true);
            if (flds != null)
                this.cbValueDatabase.Items.AddRange(flds);

            XmlNode iNode = _Draw.GetNamedChildNode(sNode, "BackgroundImage");
            if (iNode != null)
            {
                string source = _Draw.GetElementValue(iNode, "Source", "Embedded");
                string val = _Draw.GetElementValue(iNode, "Value", "");
                switch (source)
                {
                    case "Embedded":
                        this.rbEmbedded.Checked = true;
                        this.cbValueEmbedded.Text = val;
                        break;
                    case "Database":
                        this.rbDatabase.Checked = true;
                        this.cbValueDatabase.Text = val;
                        this.cbMIMEType.Text = _Draw.GetElementValue(iNode, "MIMEType", "image/png");
                        break;
                    case "External":
                    default:
                        this.rbExternal.Checked = true;
                        this.tbValueExternal.Text = val;
                        break;
                }
                this.cbRepeat.Text = _Draw.GetElementValue(iNode, "BackgroundRepeat", "Repeat");
            }
            else
                this.rbNone.Checked = true;

            rbSource_CheckedChanged(null, null);

			// nothing has changed now
			fEndColor = fBackColor = fGradient = fBackImage = false;
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
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(BackgroundCtl));
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
			this.groupBox2 = new Majorsilence.Forms.GroupBox();
			this.bRepeatExpr = new Majorsilence.Forms.Button();
			this.rbNone = new Majorsilence.Forms.RadioButton();
			this.cbRepeat = new Majorsilence.Forms.ComboBox();
			this.label1 = new Majorsilence.Forms.Label();
			this.bExternalExpr = new Majorsilence.Forms.Button();
			this.bEmbeddedExpr = new Majorsilence.Forms.Button();
			this.bMimeExpr = new Majorsilence.Forms.Button();
			this.bDatabaseExpr = new Majorsilence.Forms.Button();
			this.bEmbedded = new Majorsilence.Forms.Button();
			this.bExternal = new Majorsilence.Forms.Button();
			this.tbValueExternal = new Majorsilence.Forms.TextBox();
			this.cbValueDatabase = new Majorsilence.Forms.ComboBox();
			this.cbMIMEType = new Majorsilence.Forms.ComboBox();
			this.cbValueEmbedded = new Majorsilence.Forms.ComboBox();
			this.rbDatabase = new Majorsilence.Forms.RadioButton();
			this.rbEmbedded = new Majorsilence.Forms.RadioButton();
			this.rbExternal = new Majorsilence.Forms.RadioButton();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
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
			// groupBox2
			// 
			resources.ApplyResources(this.groupBox2, "groupBox2");
			this.groupBox2.Controls.Add(this.bRepeatExpr);
			this.groupBox2.Controls.Add(this.rbNone);
			this.groupBox2.Controls.Add(this.cbRepeat);
			this.groupBox2.Controls.Add(this.label1);
			this.groupBox2.Controls.Add(this.bExternalExpr);
			this.groupBox2.Controls.Add(this.bEmbeddedExpr);
			this.groupBox2.Controls.Add(this.bMimeExpr);
			this.groupBox2.Controls.Add(this.bDatabaseExpr);
			this.groupBox2.Controls.Add(this.bEmbedded);
			this.groupBox2.Controls.Add(this.bExternal);
			this.groupBox2.Controls.Add(this.tbValueExternal);
			this.groupBox2.Controls.Add(this.cbValueDatabase);
			this.groupBox2.Controls.Add(this.cbMIMEType);
			this.groupBox2.Controls.Add(this.cbValueEmbedded);
			this.groupBox2.Controls.Add(this.rbDatabase);
			this.groupBox2.Controls.Add(this.rbEmbedded);
			this.groupBox2.Controls.Add(this.rbExternal);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.TabStop = false;
			// 
			// bRepeatExpr
			// 
			resources.ApplyResources(this.bRepeatExpr, "bRepeatExpr");
			this.bRepeatExpr.Name = "bRepeatExpr";
			this.bRepeatExpr.Tag = "repeat";
			// 
			// rbNone
			// 
			resources.ApplyResources(this.rbNone, "rbNone");
			this.rbNone.Name = "rbNone";
			this.rbNone.CheckedChanged += this.rbSource_CheckedChanged;
			// 
			// cbRepeat
			// 
			resources.ApplyResources(this.cbRepeat, "cbRepeat");
			this.cbRepeat.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbRepeat.Items.AddRange(new object[] {
            resources.GetString("cbRepeat.Items"),
            resources.GetString("cbRepeat.Items1"),
            resources.GetString("cbRepeat.Items2"),
            resources.GetString("cbRepeat.Items3")});
			this.cbRepeat.Name = "cbRepeat";
			this.cbRepeat.SelectedIndexChanged += this.BackImage_Changed;
			this.cbRepeat.TextChanged += this.BackImage_Changed;
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// bExternalExpr
			// 
			resources.ApplyResources(this.bExternalExpr, "bExternalExpr");
			this.bExternalExpr.Name = "bExternalExpr";
			this.bExternalExpr.Tag = "external";
			this.bExternalExpr.Click += this.bExpr_Click;
			// 
			// bEmbeddedExpr
			// 
			resources.ApplyResources(this.bEmbeddedExpr, "bEmbeddedExpr");
			this.bEmbeddedExpr.Name = "bEmbeddedExpr";
			this.bEmbeddedExpr.Tag = "embedded";
			this.bEmbeddedExpr.Click += this.bExpr_Click;
			// 
			// bMimeExpr
			// 
			resources.ApplyResources(this.bMimeExpr, "bMimeExpr");
			this.bMimeExpr.Name = "bMimeExpr";
			this.bMimeExpr.Tag = "mime";
			this.bMimeExpr.Click += this.bExpr_Click;
			// 
			// bDatabaseExpr
			// 
			resources.ApplyResources(this.bDatabaseExpr, "bDatabaseExpr");
			this.bDatabaseExpr.Name = "bDatabaseExpr";
			this.bDatabaseExpr.Tag = "database";
			this.bDatabaseExpr.Click += this.bExpr_Click;
			// 
			// bEmbedded
			// 
			resources.ApplyResources(this.bEmbedded, "bEmbedded");
			this.bEmbedded.Name = "bEmbedded";
			this.bEmbedded.Click += this.bEmbedded_Click;
			// 
			// bExternal
			// 
			resources.ApplyResources(this.bExternal, "bExternal");
			this.bExternal.Name = "bExternal";
			this.bExternal.Click += this.bExternal_Click;
			// 
			// tbValueExternal
			// 
			resources.ApplyResources(this.tbValueExternal, "tbValueExternal");
			this.tbValueExternal.Name = "tbValueExternal";
			this.tbValueExternal.TextChanged += this.BackImage_Changed;
			// 
			// cbValueDatabase
			// 
			resources.ApplyResources(this.cbValueDatabase, "cbValueDatabase");
			this.cbValueDatabase.Name = "cbValueDatabase";
			this.cbValueDatabase.SelectedIndexChanged += this.BackImage_Changed;
			this.cbValueDatabase.TextChanged += this.BackImage_Changed;
			// 
			// cbMIMEType
			// 
			resources.ApplyResources(this.cbMIMEType, "cbMIMEType");
			this.cbMIMEType.Items.AddRange(new object[] {
            resources.GetString("cbMIMEType.Items"),
            resources.GetString("cbMIMEType.Items1"),
            resources.GetString("cbMIMEType.Items2"),
            resources.GetString("cbMIMEType.Items3"),
            resources.GetString("cbMIMEType.Items4")});
			this.cbMIMEType.Name = "cbMIMEType";
			this.cbMIMEType.SelectedIndexChanged += this.BackImage_Changed;
			this.cbMIMEType.TextChanged += this.BackImage_Changed;
			// 
			// cbValueEmbedded
			// 
			resources.ApplyResources(this.cbValueEmbedded, "cbValueEmbedded");
			this.cbValueEmbedded.Name = "cbValueEmbedded";
			this.cbValueEmbedded.SelectedIndexChanged += this.BackImage_Changed;
			this.cbValueEmbedded.TextChanged += this.BackImage_Changed;
			// 
			// rbDatabase
			// 
			resources.ApplyResources(this.rbDatabase, "rbDatabase");
			this.rbDatabase.Name = "rbDatabase";
			this.rbDatabase.CheckedChanged += this.rbSource_CheckedChanged;
			// 
			// rbEmbedded
			// 
			resources.ApplyResources(this.rbEmbedded, "rbEmbedded");
			this.rbEmbedded.Name = "rbEmbedded";
			this.rbEmbedded.CheckedChanged += this.rbSource_CheckedChanged;
			// 
			// rbExternal
			// 
			resources.ApplyResources(this.rbExternal, "rbExternal");
			this.rbExternal.Name = "rbExternal";
			this.rbExternal.CheckedChanged += this.rbSource_CheckedChanged;
			// 
			// BackgroundCtl
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Name = "BackgroundCtl";
			this.groupBox1.ResumeLayout(false);
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
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
            fEndColor = fBackColor = fGradient = fBackImage = false;
		}

		private void bColor_Click(object sender, System.EventArgs e)
		{
			ColorDialog cd = new ColorDialog();
			cd.AnyColor = true;
			cd.FullOpen = true;
			cd.CustomColors = RdlDesigner.GetCustomColors();

            try
            {
                if (cd.ShowDialog() != DialogResult.OK)
                    return;

                RdlDesigner.SetCustomColors(cd.CustomColors);
                if (sender == this.bEndColor)
                    cbEndColor.Text = Majorsilence.Forms.ColorTranslator.ToHtml(cd.Color);
                else if (sender == this.bBackColor)
                    cbBackColor.Text = Majorsilence.Forms.ColorTranslator.ToHtml(cd.Color);
            }
            finally
            {
                cd.Dispose();
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

        private void BackImage_Changed(object sender, System.EventArgs e)
        {
            fBackImage = true;
        }

        private void rbSource_CheckedChanged(object sender, System.EventArgs e)
        {
            fBackImage = true;
            this.cbValueDatabase.Enabled = this.cbMIMEType.Enabled =
                this.bDatabaseExpr.Enabled = this.rbDatabase.Checked;
            this.cbValueEmbedded.Enabled = this.bEmbeddedExpr.Enabled =
                this.bEmbedded.Enabled = this.rbEmbedded.Checked;
            this.tbValueExternal.Enabled = this.bExternalExpr.Enabled =
                this.bExternal.Enabled = this.rbExternal.Checked;
        }
		
		private void ApplyChanges(XmlNode rNode)
		{
            if (_names != null)
            {
                rNode = _Draw.FindCreateNextInHierarchy(rNode, _names);
            }
            
            XmlNode xNode = _Draw.GetNamedChildNode(rNode, "Style");
            if (xNode == null)
            {
                _Draw.SetElement(rNode, "Style", "");
                xNode = _Draw.GetNamedChildNode(rNode, "Style");
            } 
			if (fEndColor)
			{ _Draw.SetElement(xNode, "BackgroundGradientEndColor", cbEndColor.Text); }
			if (fBackColor)
			{ _Draw.SetElement(xNode, "BackgroundColor", cbBackColor.Text); }
			if (fGradient)
			{ _Draw.SetElement(xNode, "BackgroundGradientType", cbGradient.Text); }
            if (fBackImage)
            {
                _Draw.RemoveElement(xNode, "BackgroundImage");
                if (!rbNone.Checked)
                {
                    XmlNode bi = _Draw.CreateElement(xNode, "BackgroundImage", null);
                    if (rbDatabase.Checked)
                    {
                        _Draw.SetElement(bi, "Source", "Database");
                        _Draw.SetElement(bi, "Value", cbValueDatabase.Text);
                        _Draw.SetElement(bi, "MIMEType", cbMIMEType.Text);
                    }
                    else if (rbExternal.Checked)
                    {
                        _Draw.SetElement(bi, "Source", "External");
                        _Draw.SetElement(bi, "Value", tbValueExternal.Text);
                    }
                    else if (rbEmbedded.Checked)
                    {
                        _Draw.SetElement(bi, "Source", "Embedded");
                        _Draw.SetElement(bi, "Value", cbValueEmbedded.Text);
                    }
                    _Draw.SetElement(bi, "BackgroundRepeat", cbRepeat.Text);
                }
            }
		}

        private void bExternal_Click(object sender, System.EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = Strings.BackgroundCtl_bExternal_Click_ImageFilesFilter;
            ofd.FilterIndex = 6;
            ofd.CheckFileExists = true;
            try
            {
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    tbValueExternal.Text = ofd.FileName;
                }
            }
            finally
            {
                ofd.Dispose();
            }
        }

        private void bEmbedded_Click(object sender, System.EventArgs e)
        {
            DialogEmbeddedImages dlgEI = new DialogEmbeddedImages(this._Draw);
            dlgEI.StartPosition = FormStartPosition.CenterParent;
            try
            {
                DialogResult dr = dlgEI.ShowDialog();
                if (dr != DialogResult.OK)
                    return;

                // Populate the EmbeddedImage names
                cbValueEmbedded.Items.Clear();
                cbValueEmbedded.Items.AddRange(_Draw.ReportNames.EmbeddedImageNames);
            }
            finally
            {
                dlgEI.Dispose();
            }
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
                case "database":
                    c = cbValueDatabase;
                    break;
                case "embedded":
                    c = cbValueEmbedded;
                    break;
                case "external":
                    c = tbValueExternal;
                    break;
                case "repeat":
                    c = cbRepeat;
                    break;
                case "mime":
                    c = cbMIMEType;
                    break;
            }

			if (c == null)
				return;

			XmlNode sNode = _ReportItems[0];

			DialogExprEditor ee = new DialogExprEditor(_Draw, c.Text, sNode, bColor);
			DialogResult dr = ee.ShowDialog();
			if (dr == DialogResult.OK)
				c.Text = ee.Expression;
			return;
		}

	}
}
