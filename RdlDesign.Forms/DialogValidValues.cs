
using System;
using System.Collections;
using System.Collections.Generic;
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
    /// DialogValidValues allow user to provide ValidValues: Value and Label lists
    /// </summary>
    internal partial class DialogValidValues 
    {
        private DataTable _DataTable;

        internal DialogValidValues(List<ParameterValueItem> list)
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            // Initialize form using the style node values
            InitValues(list);
        }

        internal List<ParameterValueItem> ValidValues
        {
            get
            {
                List<ParameterValueItem> list = new List<ParameterValueItem>();
                foreach (DataRow dr in _DataTable.Rows)
                {
                    if (dr[0] == DBNull.Value)
                        continue;
                    string val = (string)dr[0];

                    if (val.Length <= 0)
                        continue;

                    string label;
                    if (dr[1] == DBNull.Value)
                        label = null;
                    else
                        label = (string)dr[1];

                    ParameterValueItem pvi = new ParameterValueItem();
                    pvi.Value = val;
                    pvi.Label = label;
                    list.Add(pvi);
                }
                return list.Count > 0 ? list : null;
            }
        }

        private void InitValues(List<ParameterValueItem> list)
        {
            // Initialize the DataGrid
            //this.dgParms.DataSource = _dsv.QueryParameters;

            _DataTable = new DataTable();
            _DataTable.Columns.Add(new DataColumn("Value", typeof(string)));
            _DataTable.Columns.Add(new DataColumn("Label", typeof(string)));

            string[] rowValues = new string[2];
            if (list != null)
                foreach (ParameterValueItem pvi in list)
                {
                    rowValues[0] = pvi.Value;
                    rowValues[1] = pvi.Label;

                    _DataTable.Rows.Add(rowValues);
                }

            this.dgParms.DataSource = _DataTable;

            ////
            dgParms.Columns[0].Width = 140;
            dgParms.Columns[1].Width = 140;
        }

        private void bDelete_Click(object sender, System.EventArgs e)
        {
            this._DataTable.Rows.RemoveAt(this.dgParms.CurrentRow.Index);
        }


    }
}
