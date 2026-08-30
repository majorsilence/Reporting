
using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using Majorsilence.Forms;
using System.Xml;

namespace Majorsilence.Reporting.RdlDesign
{
	/// <summary>
	/// Summary description for BodyCtl.
	/// </summary>
	internal class BodyCtl : Majorsilence.Forms.UserControl, IProperty
	{
		private DesignXmlDraw _Draw;
		private Majorsilence.Forms.Label label1;
		private Majorsilence.Forms.Label label2;
		private Majorsilence.Forms.Label label3;
		private Majorsilence.Forms.TextBox tbHeight;
		private Majorsilence.Forms.TextBox tbColumns;
		private Majorsilence.Forms.TextBox tbColumnSpacing;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		internal BodyCtl(DesignXmlDraw dxDraw)
		{
			_Draw = dxDraw;
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Initialize form using the style node values
			InitValues();			
		}

		private void InitValues()
		{
			XmlNode rNode = _Draw.GetReportNode();
			XmlNode bNode = _Draw.GetNamedChildNode(rNode, "Body");
			tbHeight.Text = _Draw.GetElementValue(bNode, "Height", "");
			tbColumns.Text = _Draw.GetElementValue(bNode, "Columns", "1");
			tbColumnSpacing.Text = _Draw.GetElementValue(bNode, "ColumnSpacing", "");
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
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(BodyCtl));
            this.DoubleBuffered = true;
			this.label1 = new Majorsilence.Forms.Label();
			this.label2 = new Majorsilence.Forms.Label();
			this.label3 = new Majorsilence.Forms.Label();
			this.tbHeight = new Majorsilence.Forms.TextBox();
			this.tbColumns = new Majorsilence.Forms.TextBox();
			this.tbColumnSpacing = new Majorsilence.Forms.TextBox();
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
			// label3
			// 
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			// 
			// tbHeight
			// 
			resources.ApplyResources(this.tbHeight, "tbHeight");
			this.tbHeight.Name = "tbHeight";
			// 
			// tbColumns
			// 
			resources.ApplyResources(this.tbColumns, "tbColumns");
			this.tbColumns.Name = "tbColumns";
			// 
			// tbColumnSpacing
			// 
			resources.ApplyResources(this.tbColumnSpacing, "tbColumnSpacing");
			this.tbColumnSpacing.Name = "tbColumnSpacing";
			// 
			// BodyCtl
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.tbColumnSpacing);
			this.Controls.Add(this.tbColumns);
			this.Controls.Add(this.tbHeight);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Name = "BodyCtl";
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
			XmlNode rNode = _Draw.GetReportNode();
			XmlNode bNode = _Draw.GetNamedChildNode(rNode, "Body");
			_Draw.SetElement(bNode, "Height", tbHeight.Text);
			_Draw.SetElement(bNode, "Columns", tbColumns.Text);
			if (tbColumnSpacing.Text.Length > 0)
				_Draw.SetElement(bNode, "ColumnSpacing", tbColumnSpacing.Text);
			else
				_Draw.RemoveElement(bNode, "ColumnSpacing");
		}
	}
}
