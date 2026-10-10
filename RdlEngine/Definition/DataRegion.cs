

using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// DataRegion base class definition and processing.
	/// Warning if you inherit from DataRegion look at Expression.cs first.
	///</summary>
	[Serializable]
	internal class DataRegion : ReportItem
	{
		bool _KeepTogether;		// Indicates the entire data region (all
								// repeated sections) should be kept
								// together on one page if possible.
		Expression _NoRows;		// (string) Message to display in the DataRegion
								// (instead of the region layout) when
								// no rows of data are available.
								// Note: Style information on the data region applies to this text
		string _DataSetName;	// Indicates which data set to use for this data region.
								//Mandatory for top level DataRegions
								//(not contained within another
								//DataRegion) if there is not exactly
								//one data set in the report. If there is
								//exactly one data set in the report, the
								//data region uses that data set. (Note:
								//If there are zero data sets in the
								//report, data regions can not be used,
								//as there is no valid DataSetName to
								//use) Ignored for DataRegions that are
								//not top level.
		DataSetDefn _DataSetDefn;	//  resolved data set name;
		bool _PageBreakAtStart; // Indicates the report should page break
								//  at the start of the data region.
		bool _PageBreakAtEnd;	// Indicates the report should page break
								// at the end of the data region.
		Filters _Filters;		// Filters to apply to each row of data in the data region.
		DataRegion _ParentDataRegion;   // when DataRegions are nested; the nested regions have the parent set 
        Expression _PageBreakCondition;
        ReportDefn MyReport;
        internal DataRegion(ReportDefn r, ReportLink p, XmlNode xNode):base(r,p,xNode)
		{
			_KeepTogether=false;
			_NoRows=null;
			_DataSetName=null;
			_DataSetDefn=null;
			_PageBreakAtStart=false;
			_PageBreakAtEnd=false;
			_Filters=null;
            _PageBreakCondition = null;
            MyReport = r;
        }

		internal bool DataRegionElement(XmlNode xNodeLoop)
		{
			switch (xNodeLoop.Name.ToLowerInvariant())
			{
				case "keeptogether":
					_KeepTogether = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
					break;
				case "norows":
					_NoRows = new Expression(OwnerReport, this, xNodeLoop, ExpressionType.String);
					break;
				case "datasetname":
					_DataSetName = xNodeLoop.InnerText;
					break;
				case "pagebreakatstart":
					_PageBreakAtStart = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
					break;
				case "pagebreakatend":
					_PageBreakAtEnd = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
					break;
                case "pagebreakcondition":
                    _PageBreakCondition = new Expression(OwnerReport, this, xNodeLoop, ExpressionType.Boolean);
                    break;
                case "filters":
					_Filters = new Filters(OwnerReport, this, xNodeLoop);
					break;
				default:	// Will get many that are handled by the specific
							//  type of data region: ie  list,chart,matrix,table
					if (ReportItemElement(xNodeLoop))	// try at ReportItem level
						break;
					return false;
			}
			return true;
		}

		// Handle parsing of function in final pass
		async override internal Task FinalPass()
		{
            await base.FinalPass();

            if (this is Table)
            {   // Grids don't have any data responsibilities
                Table t = this as Table;
                if (t.IsGrid)
                    return;
            }

			// DataRegions aren't allowed in PageHeader or PageFooter; 
			if (this.InPageHeaderOrFooter())
				OwnerReport.rl.LogError(8, String.Format("The DataRegion '{0}' is not allowed in a PageHeader or PageFooter", this.Name == null? "unknown": Name.Nm) );

			ResolveNestedDataRegions();

			if (_ParentDataRegion != null)		// when nested we use the dataset of the parent
			{
				_DataSetDefn = _ParentDataRegion.DataSetDefn;
			}
			else if (_DataSetName != null)
			{
				if (OwnerReport.DataSetsDefn != null)
					_DataSetDefn = (DataSetDefn) OwnerReport.DataSetsDefn.Items[_DataSetName];
				if (_DataSetDefn == null)
				{
					OwnerReport.rl.LogError(8, String.Format("DataSetName '{0}' not specified in DataSets list.", _DataSetName));
				}
			}
			else
			{		// No name but maybe we can default to a single Dataset
				if (_DataSetDefn == null && OwnerReport.DataSetsDefn != null &&
					OwnerReport.DataSetsDefn.Items.Count == 1)
				{
					foreach (DataSetDefn d in OwnerReport.DataSetsDefn.Items.Values) 
					{	
						_DataSetDefn = d;
						break;	// since there is only 1 this will obtain it
					}
				}
				if (_DataSetDefn == null)
					OwnerReport.rl.LogError(8, string.Format("{0} must specify a DataSetName.",this.Name == null? "DataRegions": this.Name.Nm));
			}

			if (_NoRows != null)
                await _NoRows.FinalPass();
			if (_Filters != null)
                await _Filters.FinalPass();
            if (_PageBreakCondition != null)
                await _PageBreakCondition.FinalPass();

            return;
		}

		void ResolveNestedDataRegions()
		{
			ReportLink rl = this.Parent;
			while (rl != null)
			{
				if (rl is DataRegion)
				{
					this._ParentDataRegion = rl as DataRegion;
					break;
				}
				rl = rl.Parent;
			}
			return;
		}

		async override internal Task Run(IPresent ip, Row row)
		{
            await base.Run(ip, row);
		}

		internal void RunPageRegionBegin(Pages pgs)
		{
			if (this.TC == null && this.PageBreakAtStart && PageBreakCondition && !pgs.CurrentPage.IsEmpty())
			{	// force page break at beginning of dataregion
				pgs.NextOrNew();
				pgs.CurrentPage.YOffset = OwnerReport.TopOfPage;
			}
		}

		internal void RunPageRegionEnd(Pages pgs)
		{
			if (this.TC == null && this.PageBreakAtEnd && !pgs.CurrentPage.IsEmpty())
			{	// force page break at beginning of dataregion
				pgs.NextOrNew();
				pgs.CurrentPage.YOffset = OwnerReport.TopOfPage;
			}
		}

		internal async Task<bool> AnyRows(IPresent ip, Rows data)
		{
			if (data == null || data.Data == null ||
				data.Data.Count <= 0)
			{
				string msg;
				if (this.NoRows != null)
					msg = await this.NoRows.EvaluateString(ip.Report(), null);
				else
					msg = null;
                await ip.DataRegionNoRows(this, msg);
				return false;
			}

			return true;
		}

		internal async Task<bool> AnyRowsPage(Pages pgs, Rows data)
		{
			if (data != null && data.Data != null &&
				data.Data.Count > 0)
				return true;

			string msg;
			if (this.NoRows != null)
				msg = await this.NoRows.EvaluateString(pgs.Report, null);
			else
				msg = null;

			if (msg == null)
				return false;

			// OK we have a message we need to put out
			RunPageRegionBegin(pgs);				// still perform page break if needed

			PageText pt = new PageText(msg);
            await SetPagePositionAndStyle(pgs.Report, pt, null);

			if (pt.SI.BackgroundImage != null)
				pt.SI.BackgroundImage.H = pt.H;		//   and in the background image

			pgs.CurrentPage.AddObject(pt);

			RunPageRegionEnd(pgs);					// perform end page break if needed

            SetPagePositionEnd(pgs, pt.Y + pt.H);
            
            return false;
		}

		internal async Task<Rows> GetFilteredData(Report rpt, Row row)
		{
			try
			{
				Rows data;
				if (this._Filters == null)
				{
					if (this._ParentDataRegion == null)
					{
						data = DataSetDefn.Query.GetMyData(rpt);
						return data == null? null: new Rows(rpt, data);	// We need to copy in case DataSet is shared by multiple DataRegions
					}
					else
						return GetNestedData(rpt, row);
				}

				if (this._ParentDataRegion == null)
				{
					data = DataSetDefn.Query.GetMyData(rpt);
					if (data != null)
						data = new Rows(rpt, data);
				}
				else
					data = GetNestedData(rpt, row);

				if (data == null)
					return null;

				List<Row> ar = new List<Row>();
				foreach (Row r in data.Data)
				{
					if (await _Filters.Apply(rpt, r))
						ar.Add(r);
				}
                ar.TrimExcess();
				data.Data = ar;
                await _Filters.ApplyFinalFilters(rpt, data, true);

				// Adjust the rowcount
				int rCount = 0;
				foreach (Row r in ar)
				{
					r.RowNumber = rCount++;
				}
				return data;
			}
			catch (Exception e)
			{
				this.OwnerReport.rl.LogError(8, e.Message);
				return null;
			}
		}

		Rows GetNestedData(Report rpt, Row row)
		{
			if (row == null)
				return null;

			ReportLink rl = this.Parent;
			var inDetails = false;
			var inTableSection = false;
			while (rl != null)
			{
				if (rl is TableGroup || rl is List || rl is MatrixCell)
					break;
				if (rl is Details)
					inDetails = true;
				else if (rl is Header || rl is Footer)
					inTableSection = true;
				rl = rl.Parent;
			}
			if (rl == null)
			{
				// Not inside a group, list or matrix cell: a chart in a plain table cell (how RDL 2008
				// stores a sparkline). Its scope is that of the cell -- the current row in the detail
				// section, the table's rows in the header or footer.
				if (inDetails && row.R != null)
				{
					int at = row.R.Data.IndexOf(row);
					if (at >= 0)
						return new Rows(rpt, row.R, at, at, null);
				}
				else if (inTableSection && row.R != null)
				{
					return new Rows(rpt, row.R, 0, row.R.Data.Count - 1, null);
				}
				return null;			// should have been caught as an error
			}

			Grouping g=null;
			if (rl is TableGroup)
			{
				TableGroup tg = rl as TableGroup;
				g = tg.Grouping;
			}
			else if (rl is List)
			{
				List l = rl as List;
				g = l.Grouping;
			}
			else if (rl is MatrixCell)
			{
				MatrixCellEntry mce = this.GetMC(rpt);
				return new Rows(rpt, mce.Data);
			}
			if (g == null)
				return null;

			GroupEntry ge = row.R.CurrentGroups[g.GetIndex(rpt)];

			return new Rows(rpt, row.R, ge.StartRow, ge.EndRow, null);
		}

		internal void DataRegionFinish()
		{
			// All dataregion names need to be saved!
			if (this.Name != null)
			{
				try
				{
					OwnerReport.LUAggrScope.Add(this.Name.Nm, this);		// add to referenceable regions
				}
				catch // wish duplicate had its own exception
				{
					OwnerReport.rl.LogError(8, "Duplicate name '" + this.Name.Nm + "'.");
				}
			}
			return;
		}

		internal bool KeepTogether
		{
			get { return  _KeepTogether; }
			set {  _KeepTogether = value; }
		}

		internal Expression NoRows
		{
			get { return  _NoRows; }
			set {  _NoRows = value; }
		}

        internal bool PageBreakCondition
        {
            get
            {
                return false;
            }
            //set { _PageBreakCondition = value; }	
        }

        internal string DataSetName
		{
			get { return  _DataSetName; }
			set {  _DataSetName = value; }
		}

		internal DataSetDefn DataSetDefn
		{
			get { return  _DataSetDefn; }
			set {  _DataSetDefn = value; }
		}

		internal bool PageBreakAtStart
		{
			get { return  _PageBreakAtStart; }
			set {  _PageBreakAtStart = value; }
		}

		internal bool PageBreakAtEnd
		{
			get { return  _PageBreakAtEnd; }
			set {  _PageBreakAtEnd = value; }
		}

		internal Filters Filters
		{
			get { return  _Filters; }
			set {  _Filters = value; }
		}
	}
}
