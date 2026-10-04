
using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using Majorsilence.Forms;
using System.Xml;
using System.Text;
using System.IO;
using System.Globalization;
using Majorsilence.Reporting.RdlDesign.Resources;

namespace Majorsilence.Reporting.RdlDesign
{
	/// <summary>
	/// Control supports the properties for DataSet/Rows elements.  This is an extension to 
	/// the RDL specification allowing data to be defined within a report.
	/// </summary>
	internal partial class DataSetRowsCtl : Majorsilence.Forms.UserControl, IProperty
	{
		private DesignXmlDraw _Draw;
		private DataSetValues _dsv;
		private XmlNode _dsNode;
		private DataTable _DataTable;

		internal DataSetRowsCtl(DesignXmlDraw dxDraw, XmlNode dsNode, DataSetValues dsv)
		{
			_Draw = dxDraw;
			_dsv = dsv;
			_dsNode = dsNode;
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Initialize form using the style node values
			InitValues();			
		}

		private void InitValues()
		{
			CreateDataTable();		// create data table based on the existing fields

			XmlNode rows = _Draw.GetNamedChildNode(_dsNode, "Rows");
			if (rows == null)
				rows = _Draw.GetNamedChildNode(_dsNode, "fyi:Rows");
			string file=null;
			if (rows != null)
			{
				file = _Draw.GetElementAttribute(rows, "File", null);
				PopulateRows(rows);
			}

			this.dgRows.DataSource = _DataTable;
			if (file != null)
			{
				tbRowsFile.Text = file;
				this.chkRowsFile.Checked = true;
			}

			chkRowsFile_CheckedChanged(this, new EventArgs());
		}

		private void CreateDataTable()
		{
			_DataTable = new DataTable();

			foreach (DataRow dr in _dsv.Fields.Rows)
			{
				if (dr[0] == DBNull.Value)
					continue;
				if (dr[2] == DBNull.Value)
				{}
				else if (((string) dr[2]).Length > 0)
					continue;
				string name = (string) dr[0];

                string type = dr["TypeName"] as string;
                Type t = type == null || type.Length == 0? typeof(string): 
                    Majorsilence.Reporting.Rdl.DataType.GetStyleType(type);
				_DataTable.Columns.Add(new DataColumn(name,t));
			}
		}

		private void PopulateRows(XmlNode rows)
		{
			object[] rowValues = new object[_DataTable.Columns.Count];

            bool bSkipMsg = false;
            foreach (XmlNode rNode in rows.ChildNodes)
			{
				if (rNode.Name != "Row")
					continue;
				int col=0;
				bool bBuiltRow=false;	// if all columns will be null we won't add the row
				foreach (DataColumn dc in _DataTable.Columns)
				{
					XmlNode dNode = _Draw.GetNamedChildNode(rNode, dc.ColumnName);
					if (dNode != null)
						bBuiltRow = true;

                    if (dNode == null)
                        rowValues[col] = null;
                    else if (dc.DataType == typeof(string))
                        rowValues[col] = dNode.InnerText;
                    else
                    {
                        object box;
                        try
                        {
                            if (dc.DataType == typeof(int))
                                box = Convert.ToInt32(dNode.InnerText, NumberFormatInfo.InvariantInfo);
                            else if (dc.DataType == typeof(decimal))
                                box = Convert.ToDecimal(dNode.InnerText, NumberFormatInfo.InvariantInfo);
                            else if (dc.DataType == typeof(long))
                                box = Convert.ToInt64(dNode.InnerText, NumberFormatInfo.InvariantInfo);
                            else if (DesignerUtility.IsNumeric(dc.DataType))    // catch all numeric
                                box = Convert.ToDouble(dNode.InnerText, NumberFormatInfo.InvariantInfo);
                            else if (dc.DataType == typeof(DateTime))
                            {
                                box = Convert.ToDateTime(dNode.InnerText,
                                    System.Globalization.DateTimeFormatInfo.InvariantInfo);
                            }
                            else
                            {
                                box = dNode.InnerText;
                            }
                            rowValues[col] = box;
                        }
                        catch (Exception e)
                        {
                            if (!bSkipMsg)
                            {
                                if (MessageBox.Show(string.Format(Strings.DataSetRowsCtl_ShowB_UnableConvert,
                                        dc.DataType, dNode.InnerText, e.Message) + Environment.NewLine + Strings.DataSetRowsCtl_ShowB_WantSeeErrors,
                                        Strings.DataSetRowsCtl_ShowB_ErrorReadingDataRows, MessageBoxButtons.YesNo) == DialogResult.No)
                                    bSkipMsg = true;
                            }
                            rowValues[col] = dNode.InnerText;
                        }
                    }
					col++;
				}
				if (bBuiltRow)
					_DataTable.Rows.Add(rowValues);
			}
		}		

		public bool IsValid()
		{
			if (chkRowsFile.Checked && tbRowsFile.Text.Length == 0)
			{
				MessageBox.Show(Strings.DataSetRowsCtl_ShowC_FileNameRequired);
				return false;
			}
			return true;
		}

		public void Apply()
		{
			// Remove the old row
			XmlNode rows = _Draw.GetNamedChildNode(this._dsNode, "Rows");
			if (rows == null)
				rows =  _Draw.GetNamedChildNode(this._dsNode, "fyi:Rows");
			if (rows != null)
				_dsNode.RemoveChild(rows);
			// different result if we just want the file
			if (this.chkRowsFile.Checked)
			{
				rows = _Draw.GetCreateNamedChildNode(_dsNode, "fyi:Rows");
				_Draw.SetElementAttribute(rows, "File", this.tbRowsFile.Text);
			}
			else
			{
				rows = GetXmlData();
				if (rows.HasChildNodes)
					_dsNode.AppendChild(rows);
			}
		}

		private void bDelete_Click(object sender, System.EventArgs e)
		{
			this._DataTable.Rows.RemoveAt(this.dgRows.CurrentRow.Index);
		}

		private void bUp_Click(object sender, System.EventArgs e)
		{
			int cr = dgRows.CurrentRow.Index;
			if (cr <= 0)		// already at the top
				return;
			
			SwapRow(_DataTable.Rows[cr-1], _DataTable.Rows[cr]);
            if (cr >= 0 && cr < dgRows.Rows.Count)
            {
                dgRows.CurrentCell = dgRows.Rows[cr - 1].Cells[0];
            }
        }

		private void bDown_Click(object sender, System.EventArgs e)
		{
			int cr = dgRows.CurrentRow.Index;
			if (cr < 0)			// invalid index
				return;
			if (cr + 1 >= _DataTable.Rows.Count)
				return;			// already at end
			
			SwapRow(_DataTable.Rows[cr+1], _DataTable.Rows[cr]);
            if (cr >= 0 && cr < dgRows.Rows.Count)
            {
                dgRows.CurrentCell = dgRows.Rows[cr + 1].Cells[0];
            }
        }

		private void SwapRow(DataRow tdr, DataRow fdr)
		{
			// Loop thru all the columns in a row and swap the data
			for (int ci=0; ci < _DataTable.Columns.Count; ci++)
			{
				object save = tdr[ci];
				tdr[ci] = fdr[ci];
				fdr[ci] = save;
			}
			return;
		}

		private void chkRowsFile_CheckedChanged(object sender, System.EventArgs e)
		{
			this.tbRowsFile.Enabled = chkRowsFile.Checked;
			this.bRowsFile.Enabled = chkRowsFile.Checked;

			this.bDelete.Enabled = !chkRowsFile.Checked;
			this.bUp.Enabled = !chkRowsFile.Checked;
			this.bDown.Enabled = !chkRowsFile.Checked;
			this.dgRows.Enabled = !chkRowsFile.Checked;
		}

		private void bRowsFile_Click(object sender, System.EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Filter = Strings.DataSetRowsCtl_bRowsFile_Click_XMLFilesFilter;
			ofd.FilterIndex = 1;
			ofd.FileName = "*.xml";

			ofd.Title = Strings.DataSetRowsCtl_bRowsFile_Click_XMLFilesTitle;
			ofd.DefaultExt = "xml";
			ofd.AddExtension = true;

            try
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string file = Path.GetFileName(ofd.FileName);

                    this.tbRowsFile.Text = file;
                }
            }
            finally
            {
                ofd.Dispose();
            }
		}

		private bool DidFieldsChange()
		{
			int col=0;
			foreach (DataRow dr in _dsv.Fields.Rows)
			{
				if (col >= _DataTable.Columns.Count)
					return true;

				if (dr[0] == DBNull.Value)
					continue;
				if (dr[2] == DBNull.Value)
				{}
				else if (((string) dr[2]).Length > 0)
					continue;
				string name = (string) (dr[1] == DBNull.Value? dr[0]: dr[1]);
				if (_DataTable.Columns[col].ColumnName != name)
					return true;
				col++;
			}

			if (col == _DataTable.Columns.Count)
				return false;
			else
				return true;
		}

		private XmlNode GetXmlData()
		{
			XmlDocumentFragment fDoc = _Draw.ReportDocument.CreateDocumentFragment();

			XmlNode rows = _Draw.CreateElement(fDoc, "fyi:Rows", null);
			foreach (DataRow dr in _DataTable.Rows)
			{
				XmlNode row = _Draw.CreateElement(rows, "Row", null);
				bool bRowBuilt=false;
				foreach (DataColumn dc in _DataTable.Columns)
				{
					if (dr[dc] == DBNull.Value)
						continue;
                    string val;
                    if (dc.DataType == typeof(DateTime))
                    {
                        val = Convert.ToString(dr[dc], 
                            System.Globalization.DateTimeFormatInfo.InvariantInfo);
                    }
                    else
                    {
                        val = Convert.ToString(dr[dc], NumberFormatInfo.InvariantInfo);
                    }
					if (val == null)
						continue;
					_Draw.CreateElement(row, dc.ColumnName, val);
					bRowBuilt = true;	// we've populated at least one column; so keep row
				}
				if (!bRowBuilt)
					rows.RemoveChild(row);
			}
			return rows;
		}

		private void DataSetRowsCtl_VisibleChanged(object sender, System.EventArgs e)
		{
			if (!DidFieldsChange())		// did the structure of the fields change
				return;

			// Need to reset the data; this assumes that some of the data rows are similar
			XmlNode rows = GetXmlData();	// get old data
			
			CreateDataTable();				// this recreates the datatable

			PopulateRows(rows);				// repopulate the datatable

			this.dgRows.DataSource = _DataTable;	// this recreates the datatable so reset grid
		}

		private void bClear_Click(object sender, System.EventArgs e)
		{
			this._DataTable.Rows.Clear();
		}

		private void bLoad_Click(object sender, System.EventArgs e)
		{
			// Load the data from the SQL; we append the data to what already exists
			try
			{
				// Obtain the connection information
				XmlNode rNode = _Draw.GetReportNode();
				XmlNode dsNode = _Draw.GetNamedChildNode(rNode, "DataSources");
				if (dsNode == null)
					return;
				XmlNode datasource=null;
				foreach (XmlNode dNode in dsNode)
				{	
					if (dNode.Name != "DataSource")
						continue;
					XmlAttribute nAttr = dNode.Attributes["Name"];
					if (nAttr == null)	// shouldn't really happen
						continue;
					if (nAttr.Value != _dsv.DataSourceName)
						continue;
					datasource = dNode;
					break;
				}
				if (datasource == null)
				{
					MessageBox.Show(string.Format(Strings.DataSetRowsCtl_Show_DatasourceNotFound, _dsv.DataSourceName), Strings.DataSetRowsCtl_Show_LoadFailed);
					return;
				}
                // get the connection information
                string connection = "";
                string dataProvider = "";

                string dataSourceReference = _Draw.GetElementValue(datasource, "DataSourceReference", null);
                if (dataSourceReference != null)
                {
                    // Was: walk _Draw.Parent looking for an MDIChild then an RdlDesigner ancestor
                    // -- Form isn't Control-derived in Majorsilence.Forms, so that chain can never
                    // reach either (see DesignerUtility.GetConnnectionInfo's identical fix).
                    MDIChild mc = _Draw.FindForm() as MDIChild;
                    RdlDesigner rd = mc?.MdiParent as RdlDesigner;
                    if (rd == null || mc == null || mc.SourceFile == null)
                    {
                        MessageBox.Show(Strings.DataSetRowsCtl_ShowC_UnableLocateDSR);
                        return;
                    }
                    Uri filename = new Uri(Path.GetDirectoryName(mc.SourceFile.LocalPath) + Path.DirectorySeparatorChar + dataSourceReference);
                    if (!DesignerUtility.GetSharedConnectionInfo(rd, filename.LocalPath, out dataProvider, out connection))
                    {
                        return;
                    }
                }
                else
                {
                    XmlNode cpNode = DesignXmlDraw.FindNextInHierarchy(datasource, "ConnectionProperties", "ConnectString");
                    connection = cpNode == null ? "" : cpNode.InnerText;

                    XmlNode datap = DesignXmlDraw.FindNextInHierarchy(datasource, "ConnectionProperties", "DataProvider");
                    dataProvider = datap == null ? "" : datap.InnerText;
                }
				// Populate the data table				
				DesignerUtility.GetSqlData(dataProvider, connection, _dsv.CommandText, null, _DataTable);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Strings.DataSetRowsCtl_Show_LoadFailed);
			}
		}
	}
}
