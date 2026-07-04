
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
	/// Summary description for ReportCtl.
	/// </summary>
	internal class ImageCtl : Majorsilence.Forms.UserControl, IProperty
	{
        private List<XmlNode> _ReportItems;
		private DesignXmlDraw _Draw;
		bool fSource, fValue, fSizing, fMIMEType;
		private Majorsilence.Forms.GroupBox groupBox1;
		private Majorsilence.Forms.RadioButton rbExternal;
		private Majorsilence.Forms.RadioButton rbDatabase;
		private Majorsilence.Forms.Label label1;
		private Majorsilence.Forms.ComboBox cbSizing;
		private Majorsilence.Forms.ComboBox cbValueEmbedded;
		private Majorsilence.Forms.ComboBox cbMIMEType;
		private Majorsilence.Forms.ComboBox cbValueDatabase;
		private Majorsilence.Forms.TextBox tbValueExternal;
		private Majorsilence.Forms.Button bExternal;
		private Majorsilence.Forms.RadioButton rbEmbedded;
		private Majorsilence.Forms.Button bEmbedded;
		private Majorsilence.Forms.Button bDatabaseExpr;
		private Majorsilence.Forms.Button bMimeExpr;
		private Majorsilence.Forms.Button bEmbeddedExpr;
		private Majorsilence.Forms.Button bExternalExpr;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        public ImageCtl(DesignXmlDraw dxDraw, List<XmlNode> ris)
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
			XmlNode iNode =  _ReportItems[0];

			// Populate the EmbeddedImage names
			cbValueEmbedded.Items.AddRange(_Draw.ReportNames.EmbeddedImageNames);
			string[] flds = _Draw.GetReportItemDataRegionFields(iNode, true);
			if (flds != null)
				this.cbValueDatabase.Items.AddRange(flds);

			string source = _Draw.GetElementValue(iNode, "Source", "Embedded");
			string val =  _Draw.GetElementValue(iNode, "Value", "");
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
			this.cbSizing.Text = _Draw.GetElementValue(iNode, "Sizing", "AutoSize");

			fSource = fValue = fSizing = fMIMEType = false;
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
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(ImageCtl));
            this.DoubleBuffered = true;
			this.groupBox1 = new Majorsilence.Forms.GroupBox();
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
			this.label1 = new Majorsilence.Forms.Label();
			this.cbSizing = new Majorsilence.Forms.ComboBox();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.Controls.Add(this.bExternalExpr);
			this.groupBox1.Controls.Add(this.bEmbeddedExpr);
			this.groupBox1.Controls.Add(this.bMimeExpr);
			this.groupBox1.Controls.Add(this.bDatabaseExpr);
			this.groupBox1.Controls.Add(this.bEmbedded);
			this.groupBox1.Controls.Add(this.bExternal);
			this.groupBox1.Controls.Add(this.tbValueExternal);
			this.groupBox1.Controls.Add(this.cbValueDatabase);
			this.groupBox1.Controls.Add(this.cbMIMEType);
			this.groupBox1.Controls.Add(this.cbValueEmbedded);
			this.groupBox1.Controls.Add(this.rbDatabase);
			this.groupBox1.Controls.Add(this.rbEmbedded);
			this.groupBox1.Controls.Add(this.rbExternal);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabStop = false;
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
			this.tbValueExternal.TextChanged += this.Value_TextChanged;
			// 
			// cbValueDatabase
			// 
			resources.ApplyResources(this.cbValueDatabase, "cbValueDatabase");
			this.cbValueDatabase.Name = "cbValueDatabase";
			this.cbValueDatabase.TextChanged += this.Value_TextChanged;
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
			this.cbMIMEType.SelectedIndexChanged += this.cbMIMEType_SelectedIndexChanged;
			// 
			// cbValueEmbedded
			// 
			resources.ApplyResources(this.cbValueEmbedded, "cbValueEmbedded");
			this.cbValueEmbedded.Name = "cbValueEmbedded";
			this.cbValueEmbedded.TextChanged += this.Value_TextChanged;
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
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// cbSizing
			// 
			resources.ApplyResources(this.cbSizing, "cbSizing");
			this.cbSizing.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbSizing.Items.AddRange(new object[] {
            resources.GetString("cbSizing.Items"),
            resources.GetString("cbSizing.Items1"),
            resources.GetString("cbSizing.Items2"),
            resources.GetString("cbSizing.Items3")});
			this.cbSizing.Name = "cbSizing";
			this.cbSizing.SelectedIndexChanged += this.cbSizing_SelectedIndexChanged;
			// 
			// ImageCtl
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.cbSizing);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.groupBox1);
			this.Name = "ImageCtl";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
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
			fSource = fValue = fSizing = fMIMEType = false;
		}

		public void ApplyChanges(XmlNode node)
		{
			if (fSource || fValue || fMIMEType)
			{
				string source="";
				string val="";
				if (rbEmbedded.Checked)
				{
					val = cbValueEmbedded.Text;
					source = "Embedded";
				}
				else if (rbDatabase.Checked)
				{
					source = "Database";
					val = cbValueDatabase.Text;
					_Draw.SetElement(node, "MIMEType", this.cbMIMEType.Text);
				}
				else 
				{	// must be external
					source = "External";
					val = tbValueExternal.Text;
				}
				_Draw.SetElement(node, "Source", source);
				_Draw.SetElement(node, "Value", val);
			}
			if (fSizing)
				_Draw.SetElement(node, "Sizing", this.cbSizing.Text);
		}

		private void Value_TextChanged(object sender, System.EventArgs e)
		{
			fValue = true;
		}

		private void cbMIMEType_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			fMIMEType = true;
		}

		private void rbSource_CheckedChanged(object sender, System.EventArgs e)
		{
			fSource = true;
			this.cbValueDatabase.Enabled = this.cbMIMEType.Enabled = 
				this.bDatabaseExpr.Enabled = this.rbDatabase.Checked;
			this.cbValueEmbedded.Enabled = this.bEmbeddedExpr.Enabled = 
				this.bEmbedded.Enabled = this.rbEmbedded.Checked;
			this.tbValueExternal.Enabled = this.bExternalExpr.Enabled = 
				this.bExternal.Enabled = this.rbExternal.Checked;
		}

		private void cbSizing_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			fSizing = true;
		}

		private void bExternal_Click(object sender, System.EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Filter = Strings.ImageCtl_bExternal_Click_ImageFilesFilter;
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
            }
            finally
            {
                dlgEI.Dispose();
            }
			// Populate the EmbeddedImage names
			cbValueEmbedded.Items.Clear();
			cbValueEmbedded.Items.AddRange(_Draw.ReportNames.EmbeddedImageNames);

		}
		private void bExpr_Click(object sender, System.EventArgs e)
		{
			Button b = sender as Button;
			if (b == null)
				return;
			Control c = null;
			switch (b.Tag as string)
			{
				case "external":
					c = tbValueExternal;
					break;
				case "embedded":
					c = cbValueEmbedded;
					break;
				case "mime":
					c = cbMIMEType;
					break;
				case "database":
					c = cbValueDatabase;
					break;
			}

			if (c == null)
				return;

			XmlNode sNode = _ReportItems[0];

			DialogExprEditor ee = new DialogExprEditor(_Draw, c.Text, sNode);
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
