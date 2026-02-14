

using System;
using System.Xml;
using System.Collections.Generic;
using System.Threading.Tasks;

#if DRAWINGCOMPAT
using Majorsilence.Drawing;
#else
using System.Drawing;
#endif

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Represents the report item for a List (i.e. absolute positioning)
	///</summary>
	[Serializable]
	internal class List : DataRegion
	{
		Grouping _Grouping;		//The expressions to group the data by
								// Required if there are any DataRegions
								// contained within this List
		Sorting _Sorting;		// The expressions to sort the repeated list regions by
		ReportItems _ReportItems;	// The elements of the list layout
		string _DataInstanceName;	// The name to use for the data element for the
								// each instance of this list. Ignored if there is a
								// grouping for the list.
								// Default: "Item"
		DataInstanceElementOutputEnum _DataInstanceElementOutput;
							// Indicates whether the list instances should
							// appear in a data rendering. Ignored if there is
							// a grouping for the list.  Default: output
		bool _CanGrow;			// indicates that row height can increase in size
		bool _CanShrink;		// indicates that row height can decrease in size
		List<Textbox> _GrowList;	// list of TextBox's that need to be checked for growth
		List<Textbox> _ShrinkList;	// list of TextBox's that need to be checked for shrinking

		internal List(ReportDefn r, ReportLink p, XmlNode xNode):base(r,p,xNode)
		{
			_Grouping=null;
			_Sorting=null;
			_ReportItems=null;
			_DataInstanceName="Item";
			_DataInstanceElementOutput=DataInstanceElementOutputEnum.Output;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "grouping":
						_Grouping = new Grouping(r, this, xNodeLoop);
						break;
					case "sorting":
						_Sorting = new Sorting(r, this, xNodeLoop);
						break;
					case "reportitems":
						_ReportItems = new ReportItems(r, this, xNodeLoop);
						break;
					case "datainstancename":
						_DataInstanceName = xNodeLoop.InnerText;
						break;
					case "datainstanceelementoutput":
						_DataInstanceElementOutput = Majorsilence.Reporting.Rdl.DataInstanceElementOutput.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					default:	
						if (DataRegionElement(xNodeLoop))	// try at DataRegion level
							break;
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown List element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
			DataRegionFinish();			// Tidy up the DataRegion
		}

		async override internal Task FinalPass()
		{
            await base.FinalPass();

			if (_Grouping != null)
                await _Grouping.FinalPass();
			if (_Sorting != null)
                await _Sorting.FinalPass();
			if (_ReportItems != null)
                await _ReportItems.FinalPass();

			// determine if the size is dynamic depending on any of its
			//   contained textbox have cangrow true
			if (ReportItems == null)	// no report items in List region
				return;

			foreach (ReportItem ri in this.ReportItems.Items)
			{
				if (!(ri is Textbox tb))
					continue;
				if (tb.CanGrow)
				{
					if (this._GrowList == null)
                        _GrowList = new List<Textbox>();
					_GrowList.Add(tb);
					_CanGrow = true;
				}
				if (tb.CanShrink)
				{
					if (this._ShrinkList == null)
						_ShrinkList = new List<Textbox>();
					_ShrinkList.Add(tb);
					_CanShrink = true;
				}
			}

			if (_CanGrow)				// shrink down the resulting list
                _GrowList.TrimExcess();
			if (_CanShrink)
				_ShrinkList.TrimExcess();

			return;
		}

		async override internal Task Run(IPresent ip, Row row)
		{
			Report r = ip.Report();
			WorkClass wc = GetValue(r);

			wc.Data = await GetFilteredData(r, row);

			if (!await AnyRows(ip, wc.Data))		// if no rows return
				return;                 //   nothing left to do

            await RunSetGrouping(r, wc);

            await base.Run(ip, row);

			if (!await ip.ListStart(this, row))	
				return;                         // renderer doesn't want to continue

            await RunGroups(ip, wc, wc.Groups);

            await ip.ListEnd(this, row);
			RemoveValue(r);
		}

		async override internal Task RunPage(Pages pgs, Row row)
		{
			Report r = pgs.Report;
			if (await IsHidden(r, row))
				return;

			WorkClass wc = GetValue(r);
			wc.Data = await GetFilteredData(r, row);

			SetPagePositionBegin(pgs);

			if (!await AnyRowsPage(pgs, wc.Data))		// if no rows return
				return;						//   nothing left to do

			RunPageRegionBegin(pgs);

            await RunSetGrouping(pgs.Report, wc);

            await RunPageGroups(pgs, wc, wc.Groups);

			RunPageRegionEnd(pgs);
			SetPagePositionEnd(pgs, pgs.CurrentPage.YOffset);
			RemoveValue(r);

			return;
		}

		private async Task RunSetGrouping(Report rpt, WorkClass wc)
		{
			GroupEntry[] currentGroups; 

			// We have some data
			if (_Grouping != null || 
				_Sorting != null)		// fix up the data
			{
				Rows saveData = wc.Data;
				wc.Data = new Rows(rpt, null, _Grouping, _Sorting);
				wc.Data.Data = saveData.Data;
				wc.Data.Sort();
                await PrepGroups(rpt, wc);
			}
			// If we haven't formed any groups then form one with all rows
			if (wc.Groups == null)
			{
                wc.Groups = new List<GroupEntry>();
				GroupEntry ge = new GroupEntry(null, null, 0);
				if (wc.Data.Data != null)		// Do we have any data?
					ge.EndRow = wc.Data.Data.Count-1;	// yes
				else
					ge.EndRow = -1;					// no
				wc.Groups.Add(ge);			// top group
				currentGroups = new GroupEntry[1];
			}
			else
				currentGroups = new GroupEntry[1];

			wc.Data.CurrentGroups = currentGroups;

			return;
		}

		private async Task PrepGroups(Report rpt, WorkClass wc)
		{
			if (_Grouping == null)
				return;

			int i=0;
			// 1) Build array of all GroupExpression objects
			List<GroupExpression> gea = _Grouping.GroupExpressions.Items;
			GroupEntry[] currentGroups = new GroupEntry[1];
			_Grouping.SetIndex(rpt, 0);	// set the index of this group (so we can find the GroupEntry)
			currentGroups[0] = new GroupEntry(_Grouping, _Sorting, 0);

			// Save the typecodes, and grouping by groupexpression; for later use
			TypeCode[] tcs = new TypeCode[gea.Count];
			Grouping[] grp = new Grouping[gea.Count];
			i=0;
			foreach (GroupExpression ge in gea)
			{
				grp[i] = (Grouping) (ge.Parent.Parent);			// remember the group
				tcs[i++] = ge.Expression.GetTypeCode();	// remember type of expression
			}

			// 2) Loop thru the data, then loop thru the GroupExpression list
			wc.Groups = new List<GroupEntry>();
			object[] savValues=null;
			object[] grpValues=null;
			int rowCurrent = 0;

			foreach (Row row in wc.Data.Data)
			{
				// Get the values for all the group expressions
				if (grpValues == null)
					grpValues = new object[gea.Count];

				i=0;
				foreach (GroupExpression ge in gea)  // Could optimize to only calculate as needed in comparison loop below??
				{
					grpValues[i++] = await ge.Expression.Evaluate(rpt, row);
				}

				// For first row we just primed the pump; action starts on next row
				if (rowCurrent == 0)			// always start new group on first row
				{
					rowCurrent++;
					savValues = grpValues;
					grpValues = null;
					continue;
				}

				// compare the values; if change then we have a group break
				for (i = 0; i < savValues.Length; i++)
				{
					if (Filter.ApplyCompare(tcs[i], savValues[i], grpValues[i]) != 0)
					{
						// start a new group; and force a break on every subgroup
						GroupEntry saveGe=null;	
						for (int j = grp[i].GetIndex(rpt); j < currentGroups.Length; j++)
						{
							currentGroups[j].EndRow = rowCurrent-1;
							if (j == 0)
								wc.Groups.Add(currentGroups[j]);		// top group
							else if (saveGe == null)
								currentGroups[j-1].NestedGroup.Add(currentGroups[j]);
							else 
								saveGe.NestedGroup.Add(currentGroups[j]);

							saveGe = currentGroups[j];	// retain this GroupEntry
							currentGroups[j] = new GroupEntry(currentGroups[j].Group,currentGroups[j].Sort, rowCurrent);
						}
						savValues = grpValues;
						grpValues = null;
						break;		// break out of the value comparison loop
					}
				}
				rowCurrent++;
			}

			// End of all rows force break on end of rows
			for (i = 0; i < currentGroups.Length; i++)
			{
				currentGroups[i].EndRow = rowCurrent-1;
				if (i == 0)
					wc.Groups.Add(currentGroups[i]);		// top group
				else
					currentGroups[i-1].NestedGroup.Add(currentGroups[i]);
			}
			return;
		}

		private async Task RunGroups(IPresent ip, WorkClass wc, List<GroupEntry> groupEntries)
		{
			foreach (GroupEntry ge in groupEntries)
			{
				// set the group entry value
				int index;
				if (ge.Group != null)	// groups?
				{
					ge.Group.ResetHideDuplicates(ip.Report());	// reset duplicate checking
					index = ge.Group.GetIndex(ip.Report());	// yes
				}
				else					// no; must be main dataset
					index = 0;
				wc.Data.CurrentGroups[index] = ge;
				if (ge.NestedGroup.Count > 0)
					RunGroupsSetGroups(ip.Report(), wc, ge.NestedGroup);

				if (ge.Group == null)
				{	// need to run all the rows since no group defined
					for (int r=ge.StartRow; r <= ge.EndRow; r++)
					{
						await ip.ListEntryBegin(this,  wc.Data.Data[r]);
                        if (_ReportItems != null)
                            await _ReportItems.Run(ip, wc.Data.Data[r]);
						ip.ListEntryEnd(this, wc.Data.Data[r]);
					}
				}
				else
				{	// need to process just whole group as a List entry
					await ip.ListEntryBegin(this,  wc.Data.Data[ge.StartRow]);

					// pass the first row of the group
                    if (_ReportItems != null)
                        await _ReportItems.Run(ip, wc.Data.Data[ge.StartRow]);

					ip.ListEntryEnd(this, wc.Data.Data[ge.StartRow]);
				}
			}
		}

		private async Task RunPageGroups(Pages pgs, WorkClass wc, List<GroupEntry> groupEntries)
		{
			Report rpt = pgs.Report;
			Page p = pgs.CurrentPage;
			float pagebottom = OwnerReport.BottomOfPage;
//			p.YOffset += (Top == null? 0: this.Top.Points);
            p.YOffset += this.RelativeY(rpt);
            float listoffset = GetOffsetCalc(pgs.Report) + LeftCalc(pgs.Report);

			float height;	
			Row row;

			foreach (GroupEntry ge in groupEntries)
			{
				// set the group entry value
				int index;
				if (ge.Group != null)	// groups?
				{
					ge.Group.ResetHideDuplicates(rpt);	// reset duplicate checking
					index = ge.Group.GetIndex(rpt);	// yes
				}
				else					// no; must be main dataset
					index = 0;
				wc.Data.CurrentGroups[index] = ge;
				if (ge.NestedGroup.Count > 0)
					RunGroupsSetGroups(rpt, wc, ge.NestedGroup);

				if (ge.Group == null)
				{	// need to run all the rows since no group defined
					for (int r=ge.StartRow; r <= ge.EndRow; r++)
					{
						row = wc.Data.Data[r];
						height = await HeightOfList(rpt, pgs.G, row);
                        
                        if (p.YOffset + height > pagebottom && !p.IsEmpty())		// need another page for this row?
                            p = RunPageNew(pgs, p);					// yes; if at end this page is empty

                        float saveYoffset = p.YOffset;              // this can be affected by other page items
                        PageRectangle border = null;
                        if (Style != null) {
	                        border = new PageRectangle();
                            await SetPagePositionAndStyle(rpt, border, row);
	                        p.AddObject(border);
                        }

                        if (_ReportItems != null)
                            await _ReportItems.RunPage(pgs, row, listoffset);

                        if (p == pgs.CurrentPage)       // did subitems force new page?
                        {   // no use the height of the list
                            p.YOffset = saveYoffset + height;
                        }
                        else
                        {   // got forced to new page; just add the padding on
                            p = pgs.CurrentPage;        // set to new page
                            if (this.Style != null)
                            {
                                p.YOffset += await this.Style.EvalPaddingBottom(rpt, row);
                            }
                        }
                        
                        if(border != null) // fix up the border height
	                        border.H = p.YOffset - border.Y;
					}
				}
				else
				{	// need to process just whole group as a List entry
					if (ge.Group.PageBreakAtStart && !p.IsEmpty())
						p = RunPageNew(pgs, p);

					// pass the first row of the group
					row = wc.Data.Data[ge.StartRow];
					height = await HeightOfList(rpt, pgs.G, row);
                    
                    if (p.YOffset + height > pagebottom && !p.IsEmpty())		// need another page for this row?
                        p = RunPageNew(pgs, p);					// yes; if at end this page is empty
                    float saveYoffset = p.YOffset;              // this can be affected by other page items
                    
                    if (_ReportItems != null)
                        await _ReportItems.RunPage(pgs, row, listoffset);


                    if (p == pgs.CurrentPage)       // did subitems force new page?
                    {   // no use the height of the list
                        p.YOffset = saveYoffset + height;
                    }
                    else
                    {   // got forced to new page; just add the padding on
                        p = pgs.CurrentPage;        // set to new page
                        if (this.Style != null)
                        {
                            p.YOffset += await this.Style.EvalPaddingBottom(rpt, row);
                        }
                    }
					
                    if (ge.Group.PageBreakAtEnd ||					// need another page for next group?
						p.YOffset + height > pagebottom)
					{
						p = RunPageNew(pgs, p);						// yes; if at end empty page will be cleaned up later
					}
				}
                RemoveWC(rpt);
			}
		}

        internal override void RemoveWC(Report rpt)
        {
            base.RemoveWC(rpt);
            
            if (this._ReportItems == null)
                return;

            foreach (ReportItem ri in this._ReportItems.Items)
            {
                ri.RemoveWC(rpt);
            }
        }

		private void RunGroupsSetGroups(Report rpt, WorkClass wc, List<GroupEntry> groupEntries)
		{
			// set the group entry value
			GroupEntry ge = groupEntries[0];
			wc.Data.CurrentGroups[ge.Group.GetIndex(rpt)] = ge;

			if (ge.NestedGroup.Count > 0)
				RunGroupsSetGroups(rpt, wc, ge.NestedGroup);
		}

		internal Grouping Grouping
		{
			get { return  _Grouping; }
			set {  _Grouping = value; }
		}

		internal async Task<float> HeightOfList(Report rpt, Graphics g, Row r)
		{		   
			WorkClass wc = GetValue(rpt);

			float defnHeight = this.HeightOrOwnerHeight;
			
			// If neither CanGrow nor CanShrink, use defined height
			if (!_CanGrow && !_CanShrink)
				return defnHeight;

			float height = defnHeight;
			
			// Calculate height for CanGrow textboxes
			if (_CanGrow)
			{
				foreach (Textbox tb in this._GrowList)
				{
					float top = (float) (tb.Top == null? 0.0 : tb.Top.Points);
					float calcHeight = top + await tb.RunTextCalcHeight(rpt, g, r);
					if (tb.Style != null)
						calcHeight += (await tb.Style.EvalPaddingBottom(rpt, r) + await tb.Style.EvalPaddingTop(rpt, r));
					height = Math.Max(height, calcHeight);
				}
			}
			
			// Calculate height for CanShrink textboxes
			if (_CanShrink)
			{
				float minHeight = defnHeight;  // Start with defined height
				foreach (Textbox tb in this._ShrinkList)
				{
					float top = (float) (tb.Top == null? 0.0 : tb.Top.Points);
					float calcHeight = top + await tb.RunTextCalcHeight(rpt, g, r);
					if (tb.Style != null)
						calcHeight += (await tb.Style.EvalPaddingBottom(rpt, r) + await tb.Style.EvalPaddingTop(rpt, r));
					minHeight = Math.Min(minHeight, calcHeight);
				}
				
				// If we have both CanGrow and CanShrink textboxes
				if (_CanGrow)
				{
					// Allow both growing and shrinking
					height = Math.Max(height, minHeight);
				}
				else
				{
					// Only CanShrink
					height = minHeight;
				}
			}
			
			// Apply the final height calculation
			if (_CanGrow && !_CanShrink)
			{
				// Only CanGrow: height can be larger than defined but not smaller
				wc.CalcHeight = Math.Max(height, defnHeight);
			}
			else if (_CanShrink && !_CanGrow)
			{
				// Only CanShrink: height can be smaller than defined but not larger
				wc.CalcHeight = Math.Min(height, defnHeight);
			}
			else
			{
				// Both CanGrow and CanShrink: height can be either larger or smaller
				wc.CalcHeight = height;
			}
			
			return wc.CalcHeight;
		}

		internal Sorting Sorting
		{
			get { return  _Sorting; }
			set {  _Sorting = value; }
		}

		internal ReportItems ReportItems
		{
			get { return  _ReportItems; }
			set {  _ReportItems = value; }
		}

		internal string DataInstanceName
		{
			get { return  _DataInstanceName == null? "Item": _DataInstanceName; }
			set {  _DataInstanceName = value; }
		}

		internal DataInstanceElementOutputEnum DataInstanceElementOutput
		{
			get { return  _DataInstanceElementOutput; }
			set {  _DataInstanceElementOutput = value; }
		}

		private WorkClass GetValue(Report rpt)
		{
			WorkClass wc = rpt.Cache.Get(this, "wc") as WorkClass;
			if (wc == null)
			{
				wc = new WorkClass(this);
				rpt.Cache.Add(this, "wc", wc);
			}
			return wc;
		}

		private void RemoveValue(Report rpt)
		{
			rpt.Cache.Remove(this, "wc");
		}

		class WorkClass
		{
			internal float CalcHeight;		// dynamic when CanGrow true
			internal Rows Data;	// Runtime data; either original query if no groups
						// or sorting or a copied version that is grouped/sorted
            internal List<GroupEntry> Groups;			// Runtime groups; array of GroupEntry
			internal WorkClass(List l)
			{
				CalcHeight = l.Height == null? 0: l.Height.Points;
				Data=null;
				Groups=null;
			}
		}
	}
}
