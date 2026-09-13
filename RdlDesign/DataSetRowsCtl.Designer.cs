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
			System.Windows.Forms.ComponentResourceManager resources = new System.Windows.Forms.ComponentResourceManager(typeof(DataSetRowsCtl));
            this.DoubleBuffered = true;
			this.dgRows = new System.Windows.Forms.DataGridView();
			this.bDelete = new System.Windows.Forms.Button();
			this.bUp = new System.Windows.Forms.Button();
			this.bDown = new System.Windows.Forms.Button();
			this.chkRowsFile = new System.Windows.Forms.CheckBox();
			this.tbRowsFile = new System.Windows.Forms.TextBox();
			this.bRowsFile = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.bLoad = new System.Windows.Forms.Button();
			this.bClear = new System.Windows.Forms.Button();
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
		
		private System.Windows.Forms.Button bDelete;
		private System.Windows.Forms.Button bUp;
		private System.Windows.Forms.Button bDown;
		private System.Windows.Forms.CheckBox chkRowsFile;
		private System.Windows.Forms.Button bRowsFile;
		private System.Windows.Forms.DataGridView dgRows;
		private System.Windows.Forms.TextBox tbRowsFile;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button bLoad;
		private System.Windows.Forms.Button bClear;
	}
}