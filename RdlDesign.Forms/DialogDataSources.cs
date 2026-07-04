using System;
using System.Collections;
using System.ComponentModel;
using Majorsilence.Forms;
using System.Text;
using System.Xml;
using System.IO;
using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RdlDesign.Resources;


namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// Summary description for DialogDataSourceRef.
    /// </summary>
    internal partial class DialogDataSources 
    {

        Uri _FileName;           // file name of open file; used to obtain directory

        internal DialogDataSources(Uri filename, DesignXmlDraw draw)
        {
            _Draw = draw;
            _FileName = filename;
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            InitValues();
        }

        private void InitValues()
        {
            // Populate the DataProviders
            cbDataProvider.Items.Clear();
            string[] items = RdlEngineConfig.GetProviders();
            cbDataProvider.Items.AddRange(items);

            //
            // Obtain the existing DataSets info
            //
            XmlNode rNode = _Draw.GetReportNode();
            XmlNode dsNode = _Draw.GetNamedChildNode(rNode, "DataSources");
            if (dsNode == null)
                return;
            foreach (XmlNode dNode in dsNode)
            {
                if (dNode.Name != "DataSource")
                    continue;
                XmlAttribute nAttr = dNode.Attributes["Name"];
                if (nAttr == null)	// shouldn't really happen
                    continue;

                DataSourceValues dsv = new DataSourceValues(nAttr.Value);
                dsv.Node = dNode;

                dsv.DataSourceReference = _Draw.GetElementValue(dNode, "DataSourceReference", null);
                if (dsv.DataSourceReference == null)
                {	// this is not a data source reference
                    dsv.bDataSourceReference = false;
                    dsv.DataSourceReference = "";

                    XmlNode cpNode = DesignXmlDraw.FindNextInHierarchy(dNode, "ConnectionProperties", "ConnectString");
                    dsv.ConnectionString = cpNode == null ? "" : cpNode.InnerText;

                    XmlNode datap = DesignXmlDraw.FindNextInHierarchy(dNode, "ConnectionProperties", "DataProvider");
                    dsv.DataProvider = datap == null ? "" : datap.InnerText;

                    XmlNode p = DesignXmlDraw.FindNextInHierarchy(dNode, "ConnectionProperties", "Prompt");
                    dsv.Prompt = p == null ? "" : p.InnerText;
                }
                else
                {	// we have a data source reference
                    dsv.bDataSourceReference = true;
                    dsv.ConnectionString = "";
                    dsv.DataProvider = "";
                    dsv.Prompt = "";
                }

                this.lbDataSources.Items.Add(dsv);
            }
            if (lbDataSources.Items.Count > 0)
                lbDataSources.SelectedIndex = 0;
            else
                this.bOK.Enabled = false;
        }

        public void Apply()
        {
            XmlNode rNode = _Draw.GetReportNode();
            _Draw.RemoveElement(rNode, "DataSources");	// remove old DataSources
            if (this.lbDataSources.Items.Count <= 0)
                return;			// nothing in list?  all done

            XmlNode dsNode = _Draw.SetElement(rNode, "DataSources", null);
            foreach (DataSourceValues dsv in lbDataSources.Items)
            {
                if (dsv.Name == null || dsv.Name.Length <= 0)
                    continue;					// shouldn't really happen
                XmlNode dNode = _Draw.CreateElement(dsNode, "DataSource", null);

                // Create the name attribute
                _Draw.SetElementAttribute(dNode, "Name", dsv.Name);

                if (dsv.bDataSourceReference)
                {
                    _Draw.SetElement(dNode, "DataSourceReference", dsv.DataSourceReference);
                    continue;
                }
                // must fill out the connection properties
                XmlNode cNode = _Draw.CreateElement(dNode, "ConnectionProperties", null);
                _Draw.SetElement(cNode, "DataProvider", dsv.DataProvider);
                _Draw.SetElement(cNode, "ConnectString", dsv.ConnectionString);
                _Draw.SetElement(cNode, "IntegratedSecurity", dsv.IntegratedSecurity ? "true" : "false");
                if (dsv.Prompt != null && dsv.Prompt.Length > 0)
                    _Draw.SetElement(cNode, "Prompt", dsv.Prompt);
            }
        }

        private void bGetFilename_Click(object sender, System.EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = Strings.DialogDataSources_bGetFilename_Click_DSRFilter;
            ofd.FilterIndex = 1;
            if (tbFilename.Text.Length > 0)
                ofd.FileName = tbFilename.Text;
            else
                ofd.FileName = "*.dsr";

            ofd.Title = Strings.DialogDataSources_bGetFilename_Click_DSRTitle;
            ofd.DefaultExt = "dsr";
            ofd.AddExtension = true;

            try
            {
                if (_FileName != null)
                {
                    ofd.InitialDirectory = Path.GetDirectoryName(_FileName.LocalPath);
                }
            }
            catch
            {
            }
            try
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string dsr = DesignerUtility.RelativePathTo(
                            Path.GetDirectoryName(_FileName.LocalPath), Path.GetDirectoryName(ofd.FileName));

                        string f = Path.GetFileNameWithoutExtension(ofd.FileName);

                        tbFilename.Text = dsr == "" ? f : dsr + Path.DirectorySeparatorChar + f;
                    }
                    catch
                    {
                        tbFilename.Text = Path.GetFileNameWithoutExtension(ofd.FileName);
                    }
                }
            }
            finally
            {
                ofd.Dispose();
            }
        }

        private void tbFilename_TextChanged(object sender, System.EventArgs e)
        {
            int cur = lbDataSources.SelectedIndex;
            if (cur < 0)
                return;

            DataSourceValues dsv = lbDataSources.Items[cur] as DataSourceValues;
            if (dsv == null)
                return;

            dsv.DataSourceReference = tbFilename.Text;
            return;
        }

        private void bOK_Click(object sender, System.EventArgs e)
        {
            // Verify there are no duplicate names in the data sources
            Hashtable ht = new Hashtable(this.lbDataSources.Items.Count + 1);
            foreach (DataSourceValues dsv in lbDataSources.Items)
            {
                if (dsv.Name == null || dsv.Name.Length == 0)
                {
                    MessageBox.Show(this, Strings.DialogDataSources_ShowE_NameMustSpecified, Strings.DialogDataSources_ShowE_DataSources);
                    return;
                }

                if (!ReportNames.IsNameValid(dsv.Name))
                {
                    MessageBox.Show(this,
						string.Format(Strings.DialogDataSources_ShowE_NameInvalid, dsv.Name), Strings.DialogDataSources_ShowE_DataSources);
                    return;
                }

                string name = (string)ht[dsv.Name];
                if (name != null)
                {
                    MessageBox.Show(this,
						string.Format(Strings.DialogDataSources_ShowE_DataSourceMustUniqueN, dsv.Name), Strings.DialogDataSources_ShowE_DataSources);
                    return;
                }
                ht.Add(dsv.Name, dsv.Name);
            }

            // apply the result
            Apply();
            DialogResult = DialogResult.OK;
        }

        private void bTestConnection_Click(object sender, System.EventArgs e)
        {
            if (string.IsNullOrEmpty(this.cbDataProvider.Text))
            {
                MessageBox.Show(Strings.DialogDatabase_ShowD_SelectDataProvider, Strings.DesignerUtility_Show_TestConnection, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var connectionString = tbConnection.Text;
            if (tbConnection.Text.StartsWith("=Parameters!"))
            {
                connectionString = _Draw.GetReportParameterDefaultValue(tbConnection.Text);
            }

            if (DesignerUtility.TestConnection(this.cbDataProvider.Text, connectionString))
                MessageBox.Show(Strings.DialogDatabase_Show_ConnectionSuccessful, Strings.DesignerUtility_Show_TestConnection);
        }

        private void tbDSName_TextChanged(object sender, System.EventArgs e)
        {
            int cur = lbDataSources.SelectedIndex;
            if (cur < 0)
                return;

            DataSourceValues dsv = lbDataSources.Items[cur] as DataSourceValues;
            if (dsv == null)
                return;

            if (dsv.Name == tbDSName.Text)
                return;

            dsv.Name = tbDSName.Text;
            // text doesn't change in listbox; force change by removing and re-adding item
            lbDataSources.BeginUpdate();
            lbDataSources.Items.RemoveAt(cur);
            lbDataSources.Items.Insert(cur, dsv);
            lbDataSources.SelectedIndex = cur;
            lbDataSources.EndUpdate();
        }

        private void chkSharedDataSource_CheckedChanged(object sender, System.EventArgs e)
        {
            int cur = lbDataSources.SelectedIndex;
            if (cur < 0)
                return;

            DataSourceValues dsv = lbDataSources.Items[cur] as DataSourceValues;
            if (dsv == null)
                return;
            dsv.bDataSourceReference = chkSharedDataSource.Checked;

            bool bEnableDataSourceRef = chkSharedDataSource.Checked;
            // shared data source fields
            tbFilename.Enabled = bEnableDataSourceRef;
            bGetFilename.Enabled = bEnableDataSourceRef;
            // in report data source
            cbDataProvider.Enabled = !bEnableDataSourceRef;
            tbConnection.Enabled = !bEnableDataSourceRef;
            ckbIntSecurity.Enabled = !bEnableDataSourceRef;
            tbPrompt.Enabled = !bEnableDataSourceRef;
            bTestConnection.Enabled = !bEnableDataSourceRef;
            lDataProvider.Enabled = !bEnableDataSourceRef;
            lConnectionString.Enabled = !bEnableDataSourceRef;
            lPrompt.Enabled = !bEnableDataSourceRef;
        }

        private void lbDataSources_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            int cur = lbDataSources.SelectedIndex;
            if (cur < 0)
                return;

            DataSourceValues dsv = lbDataSources.Items[cur] as DataSourceValues;
            if (dsv == null)
                return;

            tbDSName.Text = dsv.Name;
            cbDataProvider.Text = dsv.DataProvider;
            tbConnection.Text = dsv.ConnectionString;
            ckbIntSecurity.Checked = dsv.IntegratedSecurity;
            this.tbFilename.Text = dsv.DataSourceReference;
            tbPrompt.Text = dsv.Prompt;
            this.chkSharedDataSource.Checked = dsv.bDataSourceReference;
            chkSharedDataSource_CheckedChanged(this.chkSharedDataSource, e);	// force message
        }

        private void bAdd_Click(object sender, System.EventArgs e)
        {
            DataSourceValues dsv = new DataSourceValues("datasource");
            int cur = this.lbDataSources.Items.Add(dsv);

            lbDataSources.SelectedIndex = cur;

            this.tbDSName.Focus();
        }

        private void bRemove_Click(object sender, System.EventArgs e)
        {
            int cur = lbDataSources.SelectedIndex;
            if (cur < 0)
                return;
            lbDataSources.Items.RemoveAt(cur);
            if (lbDataSources.Items.Count <= 0)
                return;
            cur--;
            if (cur < 0)
                cur = 0;
            lbDataSources.SelectedIndex = cur;
        }

        private void tbDSName_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            int cur = lbDataSources.SelectedIndex;
            if (cur < 0)
                return;

            if (!ReportNames.IsNameValid(tbDSName.Text))
            {
                e.Cancel = true;
                MessageBox.Show(this,
					string.Format(Strings.DialogDataSources_ShowE_NameInvalid, tbDSName.Text), Strings.DialogDataSources_ShowE_DataSources);
            }

        }

        private void tbConnection_TextChanged(object sender, System.EventArgs e)
        {
            int cur = lbDataSources.SelectedIndex;
            if (cur < 0)
                return;

            DataSourceValues dsv = lbDataSources.Items[cur] as DataSourceValues;
            if (dsv == null)
                return;

            dsv.ConnectionString = tbConnection.Text;
        }

        private void tbPrompt_TextChanged(object sender, System.EventArgs e)
        {
            int cur = lbDataSources.SelectedIndex;
            if (cur < 0)
                return;

            DataSourceValues dsv = lbDataSources.Items[cur] as DataSourceValues;
            if (dsv == null)
                return;

            dsv.Prompt = tbPrompt.Text;
        }

        private void cbDataProvider_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            int cur = lbDataSources.SelectedIndex;
            if (cur < 0)
                return;

            DataSourceValues dsv = lbDataSources.Items[cur] as DataSourceValues;
            if (dsv == null)
                return;

            dsv.DataProvider = cbDataProvider.Text;
        }

        private void ckbIntSecurity_CheckedChanged(object sender, System.EventArgs e)
        {
            int cur = lbDataSources.SelectedIndex;
            if (cur < 0)
                return;

            DataSourceValues dsv = lbDataSources.Items[cur] as DataSourceValues;
            if (dsv == null)
                return;

            dsv.IntegratedSecurity = ckbIntSecurity.Checked;
        }

        private void bExprConnect_Click(object sender, System.EventArgs e)
        {
            DialogExprEditor ee = new DialogExprEditor(_Draw, this.tbConnection.Text, null, false);
            try
            {
                DialogResult dr = ee.ShowDialog();
                if (dr == DialogResult.OK)
                    tbConnection.Text = ee.Expression;
            }
            finally
            {
                ee.Dispose();
            }
        }
    }

    class DataSourceValues
    {
        string _Name;
        bool _bDataSourceReference;
        string _DataSourceReference;
        string _DataProvider;
        string _ConnectionString;
        bool _IntegratedSecurity;
        string _Prompt;
        XmlNode _Node;

        internal DataSourceValues(string name)
        {
            _Name = name;
        }

        internal string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        internal bool bDataSourceReference
        {
            get { return _bDataSourceReference; }
            set { _bDataSourceReference = value; }
        }

        internal string DataSourceReference
        {
            get { return _DataSourceReference; }
            set { _DataSourceReference = value; }
        }

        internal string DataProvider
        {
            get { return _DataProvider; }
            set { _DataProvider = value; }
        }

        internal string ConnectionString
        {
            get { return _ConnectionString; }
            set { _ConnectionString = value; }
        }

        internal bool IntegratedSecurity
        {
            get { return _IntegratedSecurity; }
            set { _IntegratedSecurity = value; }
        }

        internal string Prompt
        {
            get { return _Prompt; }
            set { _Prompt = value; }
        }

        internal XmlNode Node
        {
            get { return _Node; }
            set { _Node = value; }
        }

        override public string ToString()
        {
            return _Name;
        }
    }
}
