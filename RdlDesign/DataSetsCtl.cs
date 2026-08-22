
using Majorsilence.Reporting.RdlDesign.Resources;
using Majorsilence.Reporting.RdlDesign.Syntax;
using System;
using System.Collections.Generic;
using System.Data;
using Majorsilence.Forms;
using System.Xml;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// Summary description for StyleCtl.
    /// </summary>
    internal partial class DataSetsCtl : Majorsilence.Forms.UserControl, IProperty
    {
        private bool _UseTypenameQualified = false;
        private DesignXmlDraw _Draw;
        private XmlNode _dsNode;
        private DataSetValues _dsv;

        internal DataSetsCtl(DesignXmlDraw dxDraw, XmlNode dsNode)
        {
            _Draw = dxDraw;
            _dsNode = dsNode;
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            // Initialize form using the style node values
            InitValues();
            SetupScintilla();
        }

        internal DataSetValues DSV
        {
            get { return _dsv; }
        }

        private void InitValues()
        {
            //// cbDataSource
            cbDataSource.Items.AddRange(_Draw.DataSourceNames);

            //
            // Obtain the existing DataSet info
            //
            XmlNode dNode = this._dsNode;
            XmlAttribute nAttr = dNode.Attributes["Name"];

            _dsv = new DataSetValues(nAttr == null ? "" : nAttr.Value);
            _dsv.Node = dNode;
				
            XmlNode ctNode = DesignXmlDraw.FindNextInHierarchy(dNode, "Query", "CommandText");
            _dsv.CommandText = ctNode == null ? "" : ctNode.InnerText;
				
            XmlNode datasource = DesignXmlDraw.FindNextInHierarchy(dNode, "Query", "DataSourceName");
            _dsv.DataSourceName = datasource == null ? "" : datasource.InnerText;

            XmlNode timeout = DesignXmlDraw.FindNextInHierarchy(dNode, "Query", "Timeout");
            try
            {
                _dsv.Timeout = timeout == null ? 0 : Convert.ToInt32(timeout.InnerText);
            }
            catch		// we don't stop just because timeout isn't convertable
            {
                _dsv.Timeout = 0;
            }

            // Get QueryParameters; they are loaded here but used by the QueryParametersCtl
            _dsv.QueryParameters = new DataTable();
            _dsv.QueryParameters.Columns.Add(new DataColumn("Name", typeof(string)));
            _dsv.QueryParameters.Columns.Add(new DataColumn("Value", typeof(string)));
            XmlNode qpNode = DesignXmlDraw.FindNextInHierarchy(dNode, "Query", "QueryParameters");
            if (qpNode != null)
            {
                string[] rowValues = new string[2];
                foreach (XmlNode qNode in qpNode.ChildNodes)
                {
                    if (qNode.Name != "QueryParameter")
                        continue;
                    XmlAttribute xAttr = qNode.Attributes["Name"];
                    if (xAttr == null)
                        continue;
                    rowValues[0] = xAttr.Value;
                    rowValues[1] = _Draw.GetElementValue(qNode, "Value", "");
                    _dsv.QueryParameters.Rows.Add(rowValues);
                }
            }

            // Get Fields
            _dsv.Fields = new DataTable();
            _dsv.Fields.Columns.Add(new DataColumn("Name", typeof(string)));
            _dsv.Fields.Columns.Add(new DataColumn("QueryName", typeof(string)));
            _dsv.Fields.Columns.Add(new DataColumn("Value", typeof(string)));
            _dsv.Fields.Columns.Add(new DataColumn("TypeName", typeof(string)));

            XmlNode fsNode = _Draw.GetNamedChildNode(dNode, "Fields");
            if (fsNode != null)
            {
                string[] rowValues = new string[4];
                foreach (XmlNode fNode in fsNode.ChildNodes)
                {
                    if (fNode.Name != "Field")
                        continue;
                    XmlAttribute xAttr = fNode.Attributes["Name"];
                    if (xAttr == null)
                        continue;
                    rowValues[0] = xAttr.Value;
                    rowValues[1] = _Draw.GetElementValue(fNode, "DataField", "");
                    rowValues[2] = _Draw.GetElementValue(fNode, "Value", "");
                    string typename = null;
                    typename = _Draw.GetElementValue(fNode, "TypeName", null);
                    if (typename == null)
                    {
                        typename = _Draw.GetElementValue(fNode, "rd:TypeName", null);
                        if (typename != null)
                            _UseTypenameQualified = true;	// we got it qualified so we'll generate qualified
                    }
                    if (typename != null && !dgtbTypeName.Items.Contains(typename))
                    {
                        dgtbTypeName.Items.Add(typename);
                    }
                    rowValues[3] = typename == null ? "" : typename;

                    _dsv.Fields.Rows.Add(rowValues);
                }
            }
            this.tbDSName.Text = _dsv.Name;
            this.scintillaSQL.Text = _dsv.CommandText.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
            this.cbDataSource.Text = _dsv.DataSourceName;
			this.tbTimeout.Value = _dsv.Timeout;
            dgFields.DataSource = _dsv.Fields;
        }

        private void SetupScintilla()
        {
            new ScintillaSqlStyle(scintillaSQL);
        }

        public bool IsValid()
        {
            string nerr = _Draw.NameError(this._dsNode, this.tbDSName.Text);
            if (nerr != null)
            {
                MessageBox.Show(nerr, Strings.DataSetsCtl_Show_Name, MessageBoxButtons.OK,MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        public void Apply()
        {
            XmlNode rNode = _Draw.GetReportNode();
            XmlNode dsNode = _Draw.GetNamedChildNode(rNode, "DataSets");

            XmlNode dNode = this._dsNode;
            // Create the name attribute
            _Draw.SetElementAttribute(dNode, "Name", _dsv.Name);

            _Draw.RemoveElement(dNode, "Query");	// get rid of old query
            XmlNode qNode = _Draw.CreateElement(dNode, "Query", null);
            _Draw.SetElement(qNode, "DataSourceName", _dsv.DataSourceName);
            if (_dsv.Timeout > 0)
                _Draw.SetElement(qNode, "Timeout", _dsv.Timeout.ToString());

            _Draw.SetElement(qNode, "CommandText", _dsv.CommandText);

            // Handle QueryParameters
            _Draw.RemoveElement(qNode, "QueryParameters");	// get rid of old QueryParameters
            XmlNode qpsNode = _Draw.CreateElement(qNode, "QueryParameters", null);
            foreach (DataRow dr in _dsv.QueryParameters.Rows)
            {
                if (dr[0] == DBNull.Value || dr[1] == null || dr[1] == DBNull.Value)
                    continue;
                string name = (string)dr[0];
                if (name.Length <= 0)
                    continue;
                XmlNode qpNode = _Draw.CreateElement(qpsNode, "QueryParameter", null);
                _Draw.SetElementAttribute(qpNode, "Name", name);
                _Draw.SetElement(qpNode, "Value", (string)dr[1]);	
            }
            if (!qpsNode.HasChildNodes)	// if no parameters we don't need to define them
				_Draw.RemoveElement(qNode, "QueryParameters");

            // Handle Fields
            _Draw.RemoveElement(dNode, "Fields");	// get rid of old Fields
            XmlNode fsNode = _Draw.CreateElement(dNode, "Fields", null);
            foreach (DataRow dr in _dsv.Fields.Rows)
            {
                if (dr[0] == DBNull.Value)
                    continue;
                if (dr[1] == DBNull.Value && dr[2] == DBNull.Value)
                    continue;
                XmlNode fNode = _Draw.CreateElement(fsNode, "Field", null);
                _Draw.SetElementAttribute(fNode, "Name", (string)dr[0]);
                if (dr[1] != DBNull.Value &&
                    dr[1] is string &&
                    (string)dr[1] != string.Empty)
                    _Draw.SetElement(fNode, "DataField", (string)dr[1]);
                else if (dr[2] != DBNull.Value &&
                         dr[2] is string &&
                         (string)dr[2] != string.Empty)
                    _Draw.SetElement(fNode, "Value", (string)dr[2]);
                else
                    _Draw.SetElement(fNode, "DataField", (string)dr[0]);	// make datafield same as name

                // Handle typename if any
                if (dr[3] != DBNull.Value &&
                    dr[3] is string &&
                    (string)dr[3] != string.Empty)
                {
                    _Draw.SetElement(fNode, _UseTypenameQualified ? "rd:TypeName" : "TypeName", (string)dr[3]);
                }
            }
        }

        private void tbDSName_TextChanged(object sender, System.EventArgs e)
        {
            _dsv.Name = tbDSName.Text;
        }

        private void cbDataSource_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            _dsv.DataSourceName = cbDataSource.Text;
        }

        private void tbSQL_TextChanged(object sender, System.EventArgs e)
        {
            _dsv.CommandText = scintillaSQL.Text;
        }

        private void bDeleteField_Click(object sender, System.EventArgs e)
        {
            if (this.dgFields.CurrentRow.Index < 0)
                return; 
            _dsv.Fields.Rows.RemoveAt(this.dgFields.CurrentRow.Index);
        }

        private void bRefresh_Click(object sender, System.EventArgs e)
        {
            // Need to clear all the fields and then replace with the columns 
            //   of the SQL statement

            List<SqlColumn> cols = DesignerUtility.GetSqlColumns(_Draw, cbDataSource.Text, scintillaSQL.Text, _dsv.QueryParameters);
            if (cols == null || cols.Count <= 0)
                return;				// something didn't work right
			
            _dsv.Fields.Rows.Clear();
            string[] rowValues = new string[4];
            foreach (SqlColumn sc in cols)
            {
                rowValues[0] = sc.Name;
                rowValues[1] = sc.Name;
                rowValues[2] = "";
                DataGridViewComboBoxColumn TypeColumn = (dgFields.Columns[3] as DataGridViewComboBoxColumn);
                if (!TypeColumn.Items.Contains(sc.DataType.FullName))
                {
                    TypeColumn.Items.Add(sc.DataType.FullName);
                }
                rowValues[3] = sc.DataType.FullName;
                _dsv.Fields.Rows.Add(rowValues);
            }
        }

        private void bEditSQL_Click(object sender, System.EventArgs e)
        {
            SQLCtl sc = new SQLCtl(_Draw, cbDataSource.Text, this.scintillaSQL.Text, _dsv.QueryParameters);
            try
            {
                DialogResult dr = sc.ShowDialog(this);
                if (dr == DialogResult.OK)
                {
                    scintillaSQL.Text = sc.SQL;
                }
            }
            finally
            {
                sc.Dispose();
            }
        }

        private void tbTimeout_ValueChanged(object sender, System.EventArgs e)
        {
            _dsv.Timeout = Convert.ToInt32(tbTimeout.Value);
        }
    }

    internal class DataSetValues
    {
        string _Name;
        string _DataSourceName;
        string _CommandText;
        int _Timeout;
        DataTable _QueryParameters;
        // of type DSQueryParameter
        DataTable _Fields;
        XmlNode _Node;

        internal DataSetValues(string name)
        {
            _Name = name;
        }

        internal string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        internal string DataSourceName
        {
            get { return _DataSourceName; }
            set { _DataSourceName = value; }
        }

        internal string CommandText
        {
            get { return _CommandText; }
            set { _CommandText = value; }
        }

        internal int Timeout
        {
            get { return _Timeout; }
            set { _Timeout = value; }
        }

        internal DataTable QueryParameters
        {
            get { return _QueryParameters; }
            set { _QueryParameters = value; }
        }

        internal XmlNode Node
        {
            get { return _Node; }
            set { _Node = value; }
        }

        internal DataTable Fields
        {
            get { return _Fields; }
            set { _Fields = value; }
        }

        override public string ToString()
        {
            return _Name;
        }
    }
}
