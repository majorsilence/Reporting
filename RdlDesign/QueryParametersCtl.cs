
using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using Majorsilence.Forms;
using System.Xml;
using System.Text;
using System.IO;
using Majorsilence.Reporting.Rdl;

namespace Majorsilence.Reporting.RdlDesign
{
	/// <summary>
	/// QueryParametersCtl provides values for the DataSet Query QueryParameters rdl elements
	/// </summary>
	internal partial class QueryParametersCtl : Majorsilence.Forms.UserControl, IProperty
	{
		private DesignXmlDraw _Draw;
		private DataSetValues _dsv;
      
        internal QueryParametersCtl(DesignXmlDraw dxDraw, DataSetValues dsv)
		{
			_Draw = dxDraw;
			_dsv = dsv;
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Initialize form using the style node values
			InitValues();			
		}

		private void InitValues()
		{
            this.dgParms.DataSource = _dsv.QueryParameters; //QueryParameters are loaded in DataSetsCtl.InitValues()
		}		

		public bool IsValid()
		{
			return true;
		}

		public void Apply()
		{
			return;			// the apply is done as part of the DataSetsCtl.Apply()
		}

        private void bValueExpr_Click(object sender, EventArgs e)
        {
            if (dgParms.CurrentCell != null)
            {
                DataGridViewCell dgc = dgParms.CurrentCell;
                if (dgc.ColumnIndex == 1)
                {
                    string cv = dgc.Value as string;

                    DialogExprEditor ee = new DialogExprEditor(_Draw, cv, _dsv.Node, false);
                    try
                    {
                        DialogResult dlgr = ee.ShowDialog();
                        if (dlgr == DialogResult.OK)
                            dgc.Value = ee.Expression;
                    }
                    finally
                    {
                        ee.Dispose();
                    }
                }
            }

        }

        private void bDelete_Click(object sender, EventArgs e)
        {
            if (dgParms.CurrentRow == null)
                return;

             dgParms.Rows.RemoveAt(this.dgParms.CurrentRow.Index);

        }
    }
}
