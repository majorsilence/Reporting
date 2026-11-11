

using System;
using System.Collections;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Parsing name lookup.  Fields, parameters, report items, globals, user, aggregate scopes, grouping,...
	///</summary>
	internal class NameLookup
	{
		IDictionary fields;
		IDictionary parameters;
		IDictionary reportitems;
		IDictionary globals;
		IDictionary user;
		IDictionary aggrScope;
		Grouping g;					// if expression in a table group or detail group
									//   used to default aggregates to the right group
		Matrix m;					// if expression used in a Matrix
									//   used to default aggregate to the right matrix
		Classes instances;
		CodeModules cms;
		Type codeType;
		DataSetsDefn dsets;
		ReportLink _PageFooterHeader;	// when expression is in page header or footer this is set
		string _ExprName;			// name of the expression; this isn't always set

		internal NameLookup(IDictionary f, IDictionary p, IDictionary r, 
			IDictionary gbl, IDictionary u, IDictionary ascope, 
			Grouping ag, Matrix mt, CodeModules cm, Classes i, DataSetsDefn ds, Type ct)
		{
			fields=f;
			parameters=p;
			reportitems=r;
			globals=gbl;
			user=u;
			aggrScope = ascope;
			g=ag;
			m = mt;
			cms = cm;
			instances = i;
			dsets = ds;
			codeType = ct;
		}

		internal ReportLink PageFooterHeader
		{
			get {return _PageFooterHeader;}
			set {_PageFooterHeader = value;}
		}
		
		internal bool IsPageScope
		{
			get {return _PageFooterHeader != null;}
		}

		internal string ExpressionName
		{
			get {return _ExprName;}
			set {_ExprName = value;}
		}

		internal IDictionary Fields
		{
			get { return fields; }
		}
		internal IDictionary Parameters
		{
			get { return parameters; }
		}
		internal IDictionary ReportItems
		{
			get { return reportitems; }
		}
		internal IDictionary User
		{
			get { return user; }
		}
		internal IDictionary Globals
		{
			get { return globals; }
		}

		internal ReportClass LookupInstance(string name)
		{
			if (instances == null)
				return null;
			return instances[name];
		}

		internal Field LookupField(string name)
		{	
			if (fields == null)
				return null;

			return (Field) fields[name];
		}

		internal ReportParameter LookupParameter(string name)
		{	
			if (parameters == null)
				return null;
			return (ReportParameter) parameters[name];
		}

		internal Textbox LookupReportItem(string name)
		{	
			if (reportitems == null)
				return null;
			return (Textbox) reportitems[name];
		}

		internal IExpr LookupGlobal(string name)
		{	
			if (globals == null)
				return null;
			return (IExpr) globals[name];
		}

		internal Type LookupType(string clsname)
		{
			if (clsname == string.Empty)
				return null;
			if (cms != null)
				return cms[clsname];
			// No <CodeModules> element — still check AOT pre-registrations
			RdlEngineConfig.TryGetRegisteredType(clsname, out Type? t);
			return t;
		}

		internal CodeModules CMS
		{
			get{return cms;}
		}

		internal Type CodeClassType
		{
			get{return codeType;}
		}

		internal IExpr LookupUser(string name)
		{	
			if (user == null)
				return null;
			return (IExpr) user[name];
		}

		/// <summary>Where the expression is: what Variables! names are looked up from.</summary>
		internal ReportLink Context { get; set; }

		/// <summary>
		/// A variable visible where the expression is: of a group it is within, innermost
		/// first, else of the report. Null when there is none of that name.
		/// </summary>
		internal Variable LookupVariable(string name)
		{
			TableGroup viaGroup = null;		// the table group the expression is in, if any
			bool viaDetails = false;		// or the table's details
			for (ReportLink rl = Context; rl != null; rl = rl.Parent)
			{
				Variable v = VariableOf(GroupingOf(rl), name);
				if (v != null)
					return v;
				if (rl is TableGroup tg)
					viaGroup ??= tg;
				else if (rl is Details)
					viaDetails = true;
				else if (rl is Table t && t.TableGroups != null && (viaGroup != null || viaDetails))
				{	// a table's groups and details are siblings: a detail row is within every
					// group, a group's header or footer within the groups outside it
					var groups = t.TableGroups.Items;
					int last = viaDetails && viaGroup == null ? groups.Count - 1 : groups.IndexOf(viaGroup) - 1;
					for (int i = last; i >= 0; i--)
						if ((v = VariableOf(groups[i].Grouping, name)) != null)
							return v;
				}
				else if (rl is Matrix mx)
				{	// a matrix cell is in every row and column group of the matrix
					foreach (Grouping mg in mx.AllGroupings())
						if ((v = VariableOf(mg, name)) != null)
							return v;
				}
				if (rl is DataRegion)
				{	// a region nested in another's cell is within that cell's groups too
					viaGroup = null;
					viaDetails = false;
				}
			}
			return Context?.OwnerReport?.Variables?[name];
		}

		private static Grouping GroupingOf(ReportLink rl)
		{
			switch (rl)
			{
				case Grouping g: return g;
				case TableGroup tg: return tg.Grouping;
				case Details d: return d.Grouping;
				case List l: return l.Grouping;
				default: return null;
			}
		}

		private static Variable VariableOf(Grouping g, string name) => g?.Variables?[name];

		internal Grouping LookupGrouping()
		{
			return g;
		}

		// Where an expression sits in a table, for InScope(): the table, and whether it is in the
		// detail section (as opposed to a group header or footer, or the table's own header/footer).
		internal Table ContextTable { get; set; }
		internal bool InTableDetails { get; set; }

		internal Matrix LookupMatrix()
		{
			return m;
		}

		internal object LookupScope(string name)
		{
			if (aggrScope == null)
				return null;
			return aggrScope[name];
		}

		internal DataSetDefn ScopeDataSet(string name)
		{
			if (name == null)
			{	// Only allowed when there is only one dataset
				if (dsets.Items.Count != 1)
					return null;
                
				foreach (DataSetDefn ds in dsets.Items.Values)	// No easy way to get the item by index
					return ds;
				return null;
			}
			return dsets[name];
		}

	}
}
