using System;
using Majorsilence.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    internal partial class MDIChild : Form
	{
		#region Windows Form Designer generated code
        private RdlEditPreview rdlDesigner;
TabPage _Tab;

		private void InitializeComponent()
		{
            this.DoubleBuffered = true;
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(MDIChild));
			this.rdlDesigner = new Majorsilence.Reporting.RdlDesign.RdlEditPreview();
			this.SuspendLayout();
			// 
			// rdlDesigner
			// 
			resources.ApplyResources(this.rdlDesigner, "rdlDesigner");
			this.rdlDesigner.CurrentInsert = null;
			this.rdlDesigner.Modified = false;
			this.rdlDesigner.Name = "rdlDesigner";
			this.rdlDesigner.SelectedText = "";
			this.rdlDesigner.SelectionTool = false;
			this.rdlDesigner.Zoom = 1F;
			this.rdlDesigner.ZoomMode = Majorsilence.Reporting.RdlViewer.ZoomEnum.UseZoom;
			this.rdlDesigner.OnRdlChanged += new Majorsilence.Reporting.RdlDesign.RdlEditPreview.RdlChangeHandler(this.rdlDesigner_RdlChanged);
			this.rdlDesigner.OnHeightChanged += this.rdlDesigner_HeightChanged;
			this.rdlDesigner.OnSelectionChanged += new Majorsilence.Reporting.RdlDesign.RdlEditPreview.RdlChangeHandler(this.rdlDesigner_SelectionChanged);
			this.rdlDesigner.OnSelectionMoved += new Majorsilence.Reporting.RdlDesign.RdlEditPreview.RdlChangeHandler(this.rdlDesigner_SelectionMoved);
			this.rdlDesigner.OnReportItemInserted += new Majorsilence.Reporting.RdlDesign.RdlEditPreview.RdlChangeHandler(this.rdlDesigner_ReportItemInserted);
			this.rdlDesigner.OnDesignTabChanged += new Majorsilence.Reporting.RdlDesign.RdlEditPreview.RdlChangeHandler(this.rdlDesigner_DesignTabChanged);
			this.rdlDesigner.OnOpenSubreport += this.rdlDesigner_OpenSubreport;
            this.rdlDesigner.SaveRequested += new Majorsilence.Reporting.RdlDesign.RdlEditPreview.RdlChangeHandler(this.rdlDesigner_SaveRequested);
            // 
            // MDIChild
            // 
            resources.ApplyResources(this, "$this");
			this.AutoScaleMode = Majorsilence.Forms.AutoScaleMode.None;
			this.Controls.Add(this.rdlDesigner);
			this.Name = "MDIChild";
			this.Load += this.MDIChild_Load;
			this.ResumeLayout(false);

		}
		#endregion

        

		
	}
}
