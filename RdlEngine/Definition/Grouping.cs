

using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Grouping definition: expressions forming group, paging forced when group changes, ...
	///</summary>
	[Serializable]
	internal class Grouping : ReportLink
	{
		Name _Name;		// Name of the Grouping (for use in
						// RunningValue and RowNumber)
						// No two grouping elements may have the
						// same name. No grouping element may
						// have the same name as a data set or a data
						// region
		Expression _Label;	// (string) A label to identify an instance of the group
						//within the client UI (to provide a userfriendly
						// label for searching). See ReportItem.Label
		GroupExpressions _GroupExpressions;	//The expressions to group the data by
		bool _PageBreakAtStart;	// Indicates the report should page break at
						// the start of the group.
						// Not valid for column groupings in Matrix regions.
		bool _PageBreakAtEnd;	// Indicates the report should page break at
						// the end of the group.
						// Not valid for column groupings in Matrix regions.
		Custom _Custom; // Custom information to be passed to the
						// report output component.
		Filters _Filters;	// Filters to apply to each instance of the group.
		Expression _ParentGroup; //(Variant)
						//An expression that identifies the parent
						//group in a recursive hierarchy. Only
						//allowed if the group has exactly one group
						//expression.
						//Indicates the following:
						//1. Groups should be sorted according
						//to the recursive hierarchy (Sort is
						//still used to sort peer groups).
						//2. Labels (in the document map)
						//should be placed/indented
						//according to the recursive
						//hierarchy.
						//3. Intra-group show/hide should
						//toggle items according to the
						//recursive hierarchy (see
						//ToggleItem)
						//If filters on the group eliminate a group
						// instance�s parent, it is instead treated as a
						// child of the parent�s parent.
		string _DataElementName;	// The name to use for the data element for
									// instances of this group.
									// Default: Name of the group
		string _DataCollectionName;	// The name to use for the data element for
						// the collection of all instances of this group.
						// Default: �DataElementName_Collection�
		DataElementOutputEnum _DataElementOutput;	// Indicates whether the group should appear
						// in a data rendering.  Default: Output
		List<Textbox> _HideDuplicates;	// holds any textboxes that use this as a hideduplicate scope
		bool _InMatrix; // true if grouping is in a matrix
        Expression _PageBreakCondition;

        internal Grouping(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Name=null;
			_Label=null;
			_GroupExpressions=null;
			_PageBreakAtStart=false;
			_PageBreakAtEnd=false;
			_Custom=null;
			_Filters=null;
			_ParentGroup=null;
			_DataElementName=null;
			_DataCollectionName=null;
			_DataElementOutput=DataElementOutputEnum.Output;
			_HideDuplicates=null;
            _PageBreakCondition = null;
            // Run thru the attributes
            foreach (XmlAttribute xAttr in xNode.Attributes)
			{
				switch (xAttr.Name.ToLowerInvariant())
				{
					case "name":
						_Name = new Name(xAttr.Value);
						break;
				}
			}
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "label":
						_Label = new Expression(r, this, xNodeLoop, ExpressionType.String);
						break;
					case "groupexpressions":
						_GroupExpressions = new GroupExpressions(r, this, xNodeLoop);
						break;
					case "pagebreakatstart":
						_PageBreakAtStart = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "pagebreakatend":
						_PageBreakAtEnd = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
                    case "pagebreakcondition":
                    case "fyi:pagebreakcondition":
                        _PageBreakCondition = new Expression(OwnerReport, this, xNodeLoop, ExpressionType.Boolean);
                        break;
                    case "custom":
						_Custom = new Custom(r, this, xNodeLoop);
						break;
					case "filters":
						_Filters = new Filters(r, this, xNodeLoop);
						break;
					case "parent":
						_ParentGroup = new Expression(r, this, xNodeLoop, ExpressionType.Variant);
						break;
					case "dataelementname":
						_DataElementName = xNodeLoop.InnerText;
						break;
					case "datacollectionname":
						_DataCollectionName = xNodeLoop.InnerText;
						break;
					case "dataelementoutput":
						_DataElementOutput = Majorsilence.Reporting.Rdl.DataElementOutput.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					default:	
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown Grouping element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
			if (this.Name != null)
			{
				try
				{
					OwnerReport.LUAggrScope.Add(this.Name.Nm, this);		// add to referenceable Grouping's
				}
				catch	// wish duplicate had its own exception
				{
					OwnerReport.rl.LogError(8, "Duplicate Grouping name '" + this.Name.Nm + "'.");
				}
			}
			if (_GroupExpressions == null)
				OwnerReport.rl.LogError(8, "Group Expressions are required within group '" + (this.Name==null? "unnamed": this.Name.Nm) + "'.");
		}

		// Handle parsing of function in final pass
		async override internal Task FinalPass()
		{
			if (_Label != null)
                await _Label.FinalPass();
			if (_GroupExpressions != null)
                await _GroupExpressions.FinalPass();
			if (_Custom != null)
                await _Custom.FinalPass();
			if (_Filters != null)
                await _Filters.FinalPass();
			if (_ParentGroup != null)
                await _ParentGroup.FinalPass();
            if (_PageBreakCondition != null)
                await _PageBreakCondition.FinalPass();

            // Determine if group is defined inside of a Matrix;  these get
            //   different runtime expression handling in FunctionAggr
            _InMatrix = false;
			for (ReportLink rl = this.Parent; rl != null; rl = rl.Parent)
			{
				if (rl is Matrix)
				{
					_InMatrix = true;
					break;
				}
				if (rl is Table || rl is List || rl is Chart)
					break;
			}

			return;
		}

		internal void AddHideDuplicates(Textbox tb)
		{
			if (_HideDuplicates == null)
                _HideDuplicates = new List<Textbox>();
			_HideDuplicates.Add(tb);
		}

		internal void ResetHideDuplicates(Report rpt)
		{
			if (_HideDuplicates == null)
				return;

			foreach (Textbox tb in _HideDuplicates)
				tb.ResetPrevious(rpt);
		}

		internal bool InMatrix
		{
			get { return _InMatrix; }
		}

		internal Name Name
		{
			get { return  _Name; }
			set {  _Name = value; }
		}

		internal Expression Label
		{
			get { return  _Label; }
			set {  _Label = value; }
		}

		internal GroupExpressions GroupExpressions
		{
			get { return  _GroupExpressions; }
			set {  _GroupExpressions = value; }
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

        internal async Task<bool> PageBreakCondition(Report r, Row row, bool SeMancaDefinizione)
        {
            bool result;
            if (_PageBreakCondition != null)
                result = await _PageBreakCondition.EvaluateBoolean(r, row);
            else
                result = SeMancaDefinizione;
            return result;
        }

        internal Custom Custom
		{
			get { return  _Custom; }
			set {  _Custom = value; }
		}

		internal Filters Filters
		{
			get { return  _Filters; }
			set {  _Filters = value; }
		}

		internal Expression ParentGroup
		{
			get { return  _ParentGroup; }
			set {  _ParentGroup = value; }
		}

		internal string DataElementName
		{
			get 
			{ 
				if (_DataElementName == null)
				{
					if (this.Name != null)
						return this.Name.Nm;
				}
				return  _DataElementName; 
			}
			set {  _DataElementName = value; }
		}

		internal string DataCollectionName
		{
			get 
			{
				if (_DataCollectionName == null)
					return DataElementName + "_Collection";
				return  _DataCollectionName; 
			}
			set {  _DataCollectionName = value; }
		}

		internal DataElementOutputEnum DataElementOutput
		{
			get { return  _DataElementOutput; }
			set {  _DataElementOutput = value; }
		}

		internal int GetIndex(Report rpt)
		{
			WorkClass wc = GetValue(rpt);
			return wc.index;
		}

		internal void SetIndex(Report rpt, int i)
		{
			WorkClass wc = GetValue(rpt);
			wc.index = i;
			return;
		}

		internal Rows GetRows(Report rpt)
		{
			WorkClass wc = GetValue(rpt);
			return wc.rows;
		}

		internal void SetRows(Report rpt, Rows rows)
		{
			WorkClass wc = GetValue(rpt);
			wc.rows = rows;
			return;
		}

		private WorkClass GetValue(Report rpt)
		{
			WorkClass wc = rpt.Cache.Get(this, "wc") as WorkClass;
			if (wc == null)
			{
				wc = new WorkClass();
				rpt.Cache.Add(this, "wc", wc);
			}
			return wc;
		}

		class WorkClass
		{
			internal int index;			// used by tables (and others) to set grouping values
			internal Rows rows;			// used by matrixes to get/set grouping values
			internal WorkClass()
			{
				index = -1;
			}
		}

	}
}
