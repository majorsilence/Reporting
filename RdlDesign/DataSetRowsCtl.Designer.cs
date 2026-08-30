namespace Majorsilence.Reporting.RdlDesign
{
	partial class DataSetRowsCtl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		
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
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(DataSetRowsCtl));
            this.DoubleBuffered = true;
			this.dgRows = new Majorsilence.Forms.DataGridView();
			this.bDelete = new Majorsilence.Forms.Button();
			this.bUp = new Majorsilence.Forms.Button();
			this.bDown = new Majorsilence.Forms.Button();
			this.chkRowsFile = new Majorsilence.Forms.CheckBox();
			this.tbRowsFile = new Majorsilence.Forms.TextBox();
			this.bRowsFile = new Majorsilence.Forms.Button();
			this.label1 = new Majorsilence.Forms.Label();
			this.bLoad = new Majorsilence.Forms.Button();
			this.bClear = new Majorsilence.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.dgRows)).BeginInit();
			this.SuspendLayout();
			// 
			// dgRows
			// 
			resources.ApplyResources(this.dgRows, "dgRows");
			this.dgRows.DataMember = "";
			this.dgRows.Name = "dgRows";
			// 
			// bDelete
			// 
			resources.ApplyResources(this.bDelete, "bDelete");
			this.bDelete.Name = "bDelete";
			this.bDelete.Click += this.bDelete_Click;
			// 
			// bUp
			// 
			resources.ApplyResources(this.bUp, "bUp");
			this.bUp.Name = "bUp";
			this.bUp.Click += this.bUp_Click;
			// 
			// bDown
			// 
			resources.ApplyResources(this.bDown, "bDown");
			this.bDown.Name = "bDown";
			this.bDown.Click += this.bDown_Click;
			// 
			// chkRowsFile
			// 
			resources.ApplyResources(this.chkRowsFile, "chkRowsFile");
			this.chkRowsFile.Name = "chkRowsFile";
			this.chkRowsFile.CheckedChanged += this.chkRowsFile_CheckedChanged;
			// 
			// tbRowsFile
			// 
			resources.ApplyResources(this.tbRowsFile, "tbRowsFile");
			this.tbRowsFile.Name = "tbRowsFile";
			// 
			// bRowsFile
			// 
			resources.ApplyResources(this.bRowsFile, "bRowsFile");
			this.bRowsFile.Name = "bRowsFile";
			this.bRowsFile.Click += this.bRowsFile_Click;
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.ForeColor = System.Drawing.Color.Maroon;
			this.label1.Name = "label1";
			// 
			// bLoad
			// 
			resources.ApplyResources(this.bLoad, "bLoad");
			this.bLoad.Name = "bLoad";
			this.bLoad.Click += this.bLoad_Click;
			// 
			// bClear
			// 
			resources.ApplyResources(this.bClear, "bClear");
			this.bClear.Name = "bClear";
			this.bClear.Click += this.bClear_Click;
			// 
			// DataSetRowsCtl
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.bClear);
			this.Controls.Add(this.bLoad);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.bRowsFile);
			this.Controls.Add(this.tbRowsFile);
			this.Controls.Add(this.chkRowsFile);
			this.Controls.Add(this.bDown);
			this.Controls.Add(this.bUp);
			this.Controls.Add(this.bDelete);
			this.Controls.Add(this.dgRows);
			this.Name = "DataSetRowsCtl";
			this.VisibleChanged += this.DataSetRowsCtl_VisibleChanged;
			((System.ComponentModel.ISupportInitialize)(this.dgRows)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
		
		private Majorsilence.Forms.Button bDelete;
		private Majorsilence.Forms.Button bUp;
		private Majorsilence.Forms.Button bDown;
		private Majorsilence.Forms.CheckBox chkRowsFile;
		private Majorsilence.Forms.Button bRowsFile;
		private Majorsilence.Forms.DataGridView dgRows;
		private Majorsilence.Forms.TextBox tbRowsFile;
		private Majorsilence.Forms.Label label1;
		private Majorsilence.Forms.Button bLoad;
		private Majorsilence.Forms.Button bClear;
	}
}