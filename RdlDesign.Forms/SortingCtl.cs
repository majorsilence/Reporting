
using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using Majorsilence.Forms;
using System.Xml;
using System.Text;

namespace Majorsilence.Reporting.RdlDesign
{
	/// <summary>
	/// Sorting specification
	/// </summary>
	internal class SortingCtl : Majorsilence.Forms.UserControl, IProperty
	{
		private DesignXmlDraw _Draw;
		private XmlNode _SortingParent;
		private DataGridViewTextBoxColumn dgtbExpr;
		private DataGridViewCheckBoxColumn dgtbDir;

		private Majorsilence.Forms.Button bDelete;
		private Majorsilence.Forms.Button bUp;
		private Majorsilence.Forms.Button bDown;
		private Majorsilence.Forms.DataGridView dgSorting;
		private Majorsilence.Forms.Button bValueExpr;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public SortingCtl(DesignXmlDraw dxDraw, XmlNode sortingParent)
		{
			_Draw = dxDraw;
			_SortingParent = sortingParent;
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Initialize form using the style node values
			InitValues();			
		}

		private void InitValues()
		{
			// Initialize the DataGrid columns
			dgtbExpr = new DataGridViewTextBoxColumn();
		
			dgtbDir = new DataGridViewCheckBoxColumn();

            dgSorting.Columns.Add(dgtbExpr);
            dgSorting.Columns.Add(dgtbDir);

			// 
			// dgtbExpr
			// 
			
			dgtbExpr.HeaderText = "Sort Expression";
            dgtbExpr.Width = 240;
			// Get the parent's dataset name
//			string dataSetName = _Draw.GetDataSetNameValue(_SortingParent);
//
//			string[] fields = _Draw.GetFields(dataSetName, true);
//			if (fields != null)
//				dgtbExpr.CB.Items.AddRange(fields);
			// 
			// dgtbDir
			// 
			dgtbDir.HeaderText = "Sort Ascending";
			dgtbDir.Width = 90;

			XmlNode sorts = _Draw.GetNamedChildNode(_SortingParent, "Sorting");

			if (sorts != null)
			foreach (XmlNode sNode in sorts.ChildNodes)
			{
				if (sNode.NodeType != XmlNodeType.Element || 
						sNode.Name != "SortBy")
					continue;
                dgSorting.Rows.Add(_Draw.GetElementValue(sNode, "SortExpression", ""),
				   _Draw.GetElementValue(sNode, "Direction", "Ascending") == "Ascending");
			}

            if (dgSorting.Rows.Count == 0)
                dgSorting.Rows.Add("", true);
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
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(SortingCtl));
			this.dgSorting = new Majorsilence.Forms.DataGridView();
			this.bDelete = new Majorsilence.Forms.Button();
			this.bUp = new Majorsilence.Forms.Button();
			this.bDown = new Majorsilence.Forms.Button();
			this.bValueExpr = new Majorsilence.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.dgSorting)).BeginInit();
			this.SuspendLayout();
			// 
			// dgSorting
			// 
			resources.ApplyResources(this.dgSorting, "dgSorting");
			this.dgSorting.Name = "dgSorting";
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
			// bValueExpr
			// 
			resources.ApplyResources(this.bValueExpr, "bValueExpr");
			this.bValueExpr.Name = "bValueExpr";
			this.bValueExpr.Tag = "value";
			this.bValueExpr.Click += this.bValueExpr_Click;
			// 
			// SortingCtl
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.bValueExpr);
			this.Controls.Add(this.bDown);
			this.Controls.Add(this.bUp);
			this.Controls.Add(this.bDelete);
			this.Controls.Add(this.dgSorting);
			this.Name = "SortingCtl";
			((System.ComponentModel.ISupportInitialize)(this.dgSorting)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion
  
		public bool IsValid()
		{
			return true;
		}

		public void Apply()
		{
			// Remove the old filters
			XmlNode sorts = null;
			_Draw.RemoveElement(_SortingParent, "Sorting");
			// Loop thru and add all the filters
			foreach (DataGridViewRow dr in dgSorting.Rows)
			{
                string expr = dr.Cells[0].Value as string;
                bool dir = dr.Cells[1].Value == null? true: (bool) dr.Cells[1].Value;
				
				if (expr == null || expr.Length <= 0)
					continue;

				if (sorts == null)
					sorts = _Draw.CreateElement(_SortingParent, "Sorting", null);

				XmlNode sNode = _Draw.CreateElement(sorts, "SortBy", null);
				_Draw.CreateElement(sNode, "SortExpression", expr);
				_Draw.CreateElement(sNode, "Direction", dir?"Ascending":"Descending");
			}
		}

		private void bDelete_Click(object sender, System.EventArgs e)
		{
            if (dgSorting.CurrentRow == null)
                return;

            if (!dgSorting.Rows[dgSorting.CurrentRow.Index].IsNewRow)   // can't delete the new row
                dgSorting.Rows.RemoveAt(this.dgSorting.CurrentRow.Index);
            else
            {   // just empty out the values
                DataGridViewRow dgrv = dgSorting.Rows[this.dgSorting.CurrentRow.Index];
                dgrv.Cells[0].Value = null;
                dgrv.Cells[1].Value = null;
            }
        }

		private void bUp_Click(object sender, System.EventArgs e)
		{
            int cr = dgSorting.CurrentRow == null ? 0 : dgSorting.CurrentRow.Index;
			if (cr <= 0)		// already at the top
				return;
			
			SwapRow(dgSorting.Rows[cr - 1], dgSorting.Rows[cr]);
            dgSorting.CurrentCell =
                dgSorting.Rows[cr - 1].Cells[dgSorting.CurrentCell.ColumnIndex];
        }

		private void bDown_Click(object sender, System.EventArgs e)
		{
            int cr = dgSorting.CurrentRow == null ? 0 : dgSorting.CurrentRow.Index;
			if (cr < 0)			// invalid index
				return;
			if (cr + 1 >= dgSorting.Rows.Count)
				return;			// already at end
			
			SwapRow(dgSorting.Rows[cr + 1], dgSorting.Rows[cr]);
            dgSorting.CurrentCell =
                dgSorting.Rows[cr + 1].Cells[dgSorting.CurrentCell.ColumnIndex];
        }

		private void SwapRow(DataGridViewRow tdr, DataGridViewRow fdr)
		{
            // column 1
            object save = tdr.Cells[0].Value;
            tdr.Cells[0].Value = fdr.Cells[0].Value;
            fdr.Cells[0].Value = save;
            // column 2
            save = tdr.Cells[1].Value;
            tdr.Cells[1].Value = fdr.Cells[1].Value;
            fdr.Cells[1].Value = save;
            return;
        }

		private void bValueExpr_Click(object sender, System.EventArgs e)
		{
            if (dgSorting.CurrentCell == null)
                dgSorting.Rows.Add("",true);

            DataGridViewCell dgc = dgSorting.CurrentCell;
            int cc = dgc.ColumnIndex;

            // >>>>>>>>>> 
            // the only column that should be edited is the first one ( "Sort expression" ) 
            if (cc != 0) 
                dgc = dgSorting.CurrentCell = dgSorting.CurrentRow.Cells[0];
            // <<<<<<<<<< 
            string cv = dgc.Value as string;

            using (DialogExprEditor ee = new DialogExprEditor(_Draw, cv, _SortingParent, false))
            {
                DialogResult dlgr = ee.ShowDialog();
                if (dlgr == DialogResult.OK)
                    dgc.Value = ee.Expression;
            }
		}
	}
}
