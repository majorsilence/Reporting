using System;
using Majorsilence.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    internal partial class SQLCtl : Majorsilence.Forms.Form
	{
		#region Windows Form Designer generated code
		DesignXmlDraw _Draw;
private Majorsilence.Forms.Panel panel1;
private Majorsilence.Forms.Button bOK;
private Majorsilence.Forms.Button bCancel;
private SplitContainer splitContainer1;
private TreeView tvTablesColumns;
private TextBox tbSQL;
private Button bMove;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(SQLCtl));
			this.splitContainer1 = new Majorsilence.Forms.SplitContainer();
			this.tvTablesColumns = new Majorsilence.Forms.TreeView();
			this.tbSQL = new Majorsilence.Forms.TextBox();
			this.bMove = new Majorsilence.Forms.Button();
			this.panel1 = new Majorsilence.Forms.Panel();
			this.bOK = new Majorsilence.Forms.Button();
			this.bCancel = new Majorsilence.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// splitContainer1
			// 
			resources.ApplyResources(this.splitContainer1, "splitContainer1");
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.tvTablesColumns);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.tbSQL);
			this.splitContainer1.Panel2.Controls.Add(this.bMove);
			// 
			// tvTablesColumns
			// 
			resources.ApplyResources(this.tvTablesColumns, "tvTablesColumns");
			this.tvTablesColumns.FullRowSelect = true;
			this.tvTablesColumns.Name = "tvTablesColumns";
			this.tvTablesColumns.BeforeExpand += this.tvTablesColumns_BeforeExpand;
			// 
			// tbSQL
			// 
			this.tbSQL.AcceptsReturn = true;
			this.tbSQL.AcceptsTab = true;
			this.tbSQL.AllowDrop = true;
			resources.ApplyResources(this.tbSQL, "tbSQL");
			this.tbSQL.Name = "tbSQL";
			this.tbSQL.KeyDown += this.tbSQL_KeyDown;
			// 
			// bMove
			// 
			resources.ApplyResources(this.bMove, "bMove");
			this.bMove.Name = "bMove";
			this.bMove.Click += this.bMove_Click;
			// 
			// panel1
			// 
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.Controls.Add(this.bOK);
			this.panel1.Controls.Add(this.bCancel);
			this.panel1.Name = "panel1";
			// 
			// bOK
			// 
			resources.ApplyResources(this.bOK, "bOK");
			this.bOK.Name = "bOK";
			this.bOK.Click += this.bOK_Click;
			// 
			// bCancel
			// 
			resources.ApplyResources(this.bCancel, "bCancel");
			this.bCancel.CausesValidation = false;
			this.bCancel.DialogResult = Majorsilence.Forms.DialogResult.Cancel;
			this.bCancel.Name = "bCancel";
			// 
			// SQLCtl
			// 
			this.AcceptButton = this.bOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.bCancel;
			this.ControlBox = false;
			this.Controls.Add(this.splitContainer1);
			this.Controls.Add(this.panel1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SQLCtl";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = Majorsilence.Forms.SizeGripStyle.Show;
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			this.splitContainer1.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

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
	}
}
