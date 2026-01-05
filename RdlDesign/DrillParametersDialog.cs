
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Xml;
using System.Text;
using System.IO;
using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RdlDesign.Resources;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// Drillthrough reports; pick report and specify parameters
    /// </summary>
    internal partial class DrillParametersDialog 
    {
        private string _DrillReport;
        private DataTable _DataTable;

        internal DrillParametersDialog(string report, List<DrillParameter> parameters)
        {
            _DrillReport = report;

            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            // Initialize form using the style node values
            InitValues(parameters);
        }

        private void InitValues(List<DrillParameter> parameters)
        {
            this.tbReportFile.Text = _DrillReport;

            // Initialize the DataTable
            _DataTable = new DataTable();

            _DataTable.Columns.Add(new DataColumn("ParameterName", typeof(string)));
            _DataTable.Columns.Add(new DataColumn("Value", typeof(string)));
            _DataTable.Columns.Add(new DataColumn("Omit", typeof(string)));

            string[] rowValues = new string[3];

            if (parameters != null)
                foreach (DrillParameter dp in parameters)
                {
                    rowValues[0] = dp.ParameterName;
                    rowValues[1] = dp.ParameterValue;
                    rowValues[2] = dp.ParameterOmit;

                    _DataTable.Rows.Add(rowValues);
                }
            // Don't allow new rows; do this by creating a DataView over the DataTable
            //			DataView dv = new DataView(_DataTable);	// this has bad side effects
            //			dv.AllowNew = false;
            this.dgParms.DataSource = _DataTable;

            dgParms.Columns[0].Width = 140;
            dgParms.Columns[0].ReadOnly = true;
            dgParms.Columns[1].Width = 140;
            dgParms.Columns[2].Width = 70;
        }

        public string DrillthroughReport
        {
            get { return this._DrillReport; }
        }

        public List<DrillParameter> DrillParameters
        {
            get
            {
                List<DrillParameter> parms = new List<DrillParameter>();

                // Loop thru and add all the filters
                foreach (DataRow dr in _DataTable.Rows)
                {
                    if (dr[0] == DBNull.Value || dr[1] == DBNull.Value)
                        continue;
                    string name = (string)dr[0];
                    string val = (string)dr[1];
                    string omit = dr[2] == DBNull.Value ? "false" : (string)dr[2];
                    if (name.Length <= 0 || val.Length <= 0)
                        continue;
                    DrillParameter dp = new DrillParameter(name, val, omit);
                    parms.Add(dp);
                }
                if (parms.Count == 0)
                    return null;
                return parms;
            }
        }

        private void bFile_Click(object sender, System.EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = Strings.DrillParametersDialog_bFile_Click_ReportFilesFilter;
            ofd.FilterIndex = 1;
            ofd.FileName = "*.rdl";

            ofd.Title = Strings.DrillParametersDialog_bFile_Click_ReportFilesTitle;
            ofd.DefaultExt = "rdl";
            ofd.AddExtension = true;
            try
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string file = Path.GetFileNameWithoutExtension(ofd.FileName);

                    tbReportFile.Text = file;
                }
            }
            finally
            {
                ofd.Dispose();
            }
        }

        private async void bRefreshParms_Click(object sender, System.EventArgs e)
        {
            // Obtain the source
            Cursor savec = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;	// this can take some time
            try
            {
                string filename = "";
                if (tbReportFile.Text.Length > 0)
                    filename = tbReportFile.Text + ".rdl";

                filename = GetFileNameWithPath(filename);

                string source = this.GetSource(filename);
                if (source == null)
                    return;						// error: message already displayed

                // Compile the report
                Report report = await this.GetReport(source, filename);
                if (report == null)
                    return;					// error: message already displayed

                var rps = report.UserReportParameters;
                string[] rowValues = new string[3];
                _DataTable.Rows.Clear();
                foreach (UserReportParameter rp in rps)
                {
                    rowValues[0] = rp.Name;
                    rowValues[1] = "";
                    rowValues[2] = "false";

                    _DataTable.Rows.Add(rowValues);
                }
                this.dgParms.Refresh();
                this.dgParms.Focus();
            }
            finally
            {
                Cursor.Current = savec;
            }
        }

        private string GetFileNameWithPath(string file)
        {	// todo: should prefix this with the path of the open file

            return file;
        }

        private string GetSource(string file)
        {
            StreamReader fs = null;
            string prog = null;

            try
            {
                fs = new StreamReader(file);
                prog = fs.ReadToEnd();
            }
            catch (Exception e)
            {
                prog = null;
                MessageBox.Show(e.Message, Strings.DrillParametersDialog_Show_ErrorReading);
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
            return prog;
        }

        private async Task<Report> GetReport(string prog, string file)
        {
            // Now parse the file
            RDLParser rdlp;
            Report r;
            try
            {
                rdlp = new RDLParser(prog);
                string folder = Path.GetDirectoryName(file);
                if (folder == "")
                {
                    folder = Environment.CurrentDirectory;
                }
                rdlp.Folder = folder;

                r = await rdlp.Parse();
                if (r.ErrorMaxSeverity > 4)
                {
                    MessageBox.Show(Strings.DrillParametersDialog_ShowC_ReportHasErrors);
                    r = null;			// don't return when severe errors
                }
            }
            catch (Exception e)
            {
                r = null;
                MessageBox.Show(e.Message, Strings.DrillParametersDialog_Show_ReportLoadFailed);
            }
            return r;
        }

        private void DrillParametersDialog_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (DataRow dr in _DataTable.Rows)
            {
                if (dr[1] == DBNull.Value)
                {
                    e.Cancel = true;
                    break;
                }
                string val = (string)dr[1];
                if (val.Length <= 0)
                {
                    e.Cancel = true;
                    break;
                }
            }
            if (e.Cancel)
            {
                MessageBox.Show(Strings.DrillParametersDialog_Show_ValueMustSpecified, Text);
            }
        }

        private void bOK_Click(object sender, System.EventArgs e)
        {
            CancelEventArgs ce = new CancelEventArgs();
            DrillParametersDialog_Validating(this, ce);
            if (ce.Cancel)
            {
                DialogResult = DialogResult.None;
                return;
            }
            DialogResult = DialogResult.OK;
        }

    }
    internal class DrillParameter
    {
        internal string ParameterName;
        internal string ParameterValue;
        internal string ParameterOmit;

        internal DrillParameter(string name, string pvalue, string omit)
        {
            ParameterName = name;
            ParameterValue = pvalue;
            ParameterOmit = omit;
        }
    }
}
