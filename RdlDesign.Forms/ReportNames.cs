
using System;
using System.Xml;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using Majorsilence.Reporting.RdlDesign.Resources;

namespace Majorsilence.Reporting.RdlDesign
{
	/// <summary>
	/// ReportNames is used to control the names of objects in the report
	/// </summary>
	internal class ReportNames
	{
		XmlDocument _doc;
		List<XmlNode> _ReportNodes;		// array of report nodes; used for tabbing around the nodes
		Dictionary<string, XmlNode> _ReportItems;		// name/xmlnode pairs of report items
        Dictionary<string, XmlNode> _Groupings;		// name/xmlnode pairs of grouping names
		public ReportNames(XmlDocument rDoc)
		{
			_doc = rDoc;
			BuildNames();			// build the name hash tables
		}

		private void BuildNames()
		{
            _ReportItems = new Dictionary<string, XmlNode>(StringComparer.InvariantCultureIgnoreCase);
            _Groupings = new Dictionary<string, XmlNode>(StringComparer.InvariantCultureIgnoreCase);
			_ReportNodes = new List<XmlNode>();
			BuildNamesLoop(_doc.LastChild);
		}

		private void BuildNamesLoop(XmlNode xNode)
		{
			if (xNode == null)
				return;
			foreach (XmlNode cNode in xNode)
			{
				// this is not a complete list of object names. It doesn't
				//  need to be complete but can be optimized so subobjects aren't 
				//  pursued unnecessarily.  However, all reportitems and
				//  grouping must be traversed to get at all the names.
				// List should be built in paint order so
				//  that list of nodes is in correct tab order.
				switch (cNode.Name)
				{
					case "Report":
						BuildNamesLoop(DesignXmlDraw.FindNextInHierarchy(cNode, "PageHeader", "ReportItems"));
						BuildNamesLoop(DesignXmlDraw.FindNextInHierarchy(cNode, "Body", "ReportItems"));
						BuildNamesLoop(DesignXmlDraw.FindNextInHierarchy(cNode, "PageFooter", "ReportItems"));
						break;
						// have a name but no subobjects
					case "Textbox":
					case "Image":
					case "Line":
					case "Subreport":
                    case "CustomReportItem":
						this.AddNode(cNode);
						break;
					case "Chart":
						this.AddNode(cNode);
						BuildNamesLoop(DesignXmlDraw.FindNextInHierarchy(cNode, "SeriesGroupings"));
						BuildNamesLoop(DesignXmlDraw.FindNextInHierarchy(cNode, "CategoryGroupings"));
						break;
						// named object having subobjects
					case "Table":
						this.AddNode(cNode);
						BuildNamesLoop(DesignXmlDraw.FindNextInHierarchy(cNode, "Header", "TableRows"));
						BuildNamesLoop(DesignXmlDraw.FindNextInHierarchy(cNode, "TableGroups"));
						BuildNamesLoop(DesignXmlDraw.FindNextInHierarchy(cNode, "Details"));
						BuildNamesLoop(DesignXmlDraw.FindNextInHierarchy(cNode, "Footer", "TableRows"));
						break;
                    case "fyi:Grid":
                    case "Grid":
                        this.AddNode(cNode);
                        BuildNamesLoop(DesignXmlDraw.FindNextInHierarchy(cNode, "Header", "TableRows"));
                        BuildNamesLoop(DesignXmlDraw.FindNextInHierarchy(cNode, "Details"));
                        BuildNamesLoop(DesignXmlDraw.FindNextInHierarchy(cNode, "Footer", "TableRows"));
                        break;
                    case "List":
						this.AddNode(cNode);
						BuildNamesLoop(DesignXmlDraw.FindNextInHierarchy(cNode, "Grouping"));
						BuildNamesLoop(DesignXmlDraw.FindNextInHierarchy(cNode, "ReportItems"));
						break;
					case "Rectangle":
						this.AddNode(cNode);
						BuildNamesLoop(DesignXmlDraw.FindNextInHierarchy(cNode, "ReportItems"));
						break;
					case "Matrix":
						this.AddNode(cNode);
						BuildNamesLoop(cNode);
						break;
						// don't have a name and don't have named subobjects with names
					case "Style":
					case "Filters":
						break;
					case "Grouping":
						XmlAttribute xAttr = cNode.Attributes["Name"];
                        if (xAttr == null || _Groupings.ContainsKey(xAttr.Value))
							this.GenerateGroupingName(cNode);
                        else
							_Groupings.Add(xAttr.Value, cNode);
						break;
						// don't have a name but could have subobjects with names 
					default:
						BuildNamesLoop(cNode);		// recursively go thru entire report
						break;
				}
			}
			return;			
		}

		internal bool ChangeName(XmlNode xNode, string newName)
		{
			XmlNode fNode;
            _ReportItems.TryGetValue(newName, out fNode);
			if (fNode != null)
			{
				if (fNode != xNode)
					return false;				// this would cause a duplicate
				return true;					// newName and oldName are the same
			}

			XmlAttribute xAttr = xNode.Attributes["Name"];
			
			// Remove the old name (if one exists)
			if (xAttr != null)	
			{
				string oldName = xAttr.Value;
				this._ReportItems.Remove(oldName);
			}
			
			// Set the new name
			SetElementAttribute(xNode, "Name", newName);
			_ReportItems.Add(newName, xNode);	
			return true;
		}

		internal bool ChangeGroupName(XmlNode xNode, string newName)
		{
			XmlNode fNode;
            _Groupings.TryGetValue(newName, out fNode);
			if (fNode != null)
			{
				if (fNode != xNode)
					return false;				// this would cause a duplicate
				return true;					// newName and oldName are the same
			}

			XmlAttribute xAttr = xNode.Attributes["Name"];
			
			// Remove the old name (if one exists)
			if (xAttr != null)	
			{
				string oldName = xAttr.Value;
				this._Groupings.Remove(oldName);
			}
			
			// Set the new name
			SetElementAttribute(xNode, "Name", newName);
			_Groupings.Add(newName, xNode);	
			return true;
		}
        internal XmlNode GetRINodeFromName(string ri_name)
        {
            try
            {
                return _ReportItems[ri_name];
            }
            catch
            {
                return null;
            }
        }
		internal XmlNode FindNext(XmlNode xNode)
		{
			if (_ReportNodes.Count <= 0)
				return null;
			if (xNode == null)
				return _ReportNodes[0];
			bool bNext = false;
			foreach (XmlNode nNode in _ReportNodes)
			{
				if (bNext)
					return nNode;
				if (nNode == xNode)
					bNext = true;
			}
			return _ReportNodes[0];
		}
		
		internal XmlNode FindPrior(XmlNode xNode)
		{
			if (_ReportItems.Count <= 0)
				return null;
			if (xNode == null)
				return _ReportNodes[0];
			
			XmlNode previous=null;
			foreach (XmlNode nNode in _ReportNodes)
			{
				if (nNode == xNode)
				{
					if (previous == null)
						return _ReportNodes[_ReportNodes.Count-1];
					else
						return previous;
				}
				previous = nNode;
			}
			return _ReportNodes[_ReportNodes.Count-1];
		}

		internal ICollection ReportItemNames
		{
			get 
			{
				return _ReportItems.Keys;
			}
		}
		
		internal ICollection ReportItems
		{
			get 
			{
				return _ReportItems.Values;
			}
		}

		internal void AddNode(XmlNode xNode)
		{
			XmlAttribute xAttr = xNode.Attributes["Name"];
			if (xAttr == null)
				GenerateName(xNode);	// when no name; we generate one
			else if (_ReportItems.ContainsKey(xAttr.Value))
				GenerateName(xNode);	// when duplicate name; we generate another; this can be a problem but...
			else
			{
				this._ReportItems.Add(xAttr.Value, xNode);
				this._ReportNodes.Add(xNode);
			}
		}

		/// <summary>
		/// Generates a new name based on the object type.   Replaces the old name in the node but 
		/// does not delete it from the hash.   Use when you're copying nodes and need another name.
		/// </summary>
		/// <param name="xNode"></param>
		/// <returns></returns>
		internal string GenerateName(XmlNode xNode)
		{
			string basename = xNode.Name;
            if (basename.StartsWith("fyi:"))
                basename = basename.Substring(4);
			string name;
			int index=1;
			while (true)
			{
				name = basename + index.ToString();
				if (!_ReportItems.ContainsKey(name))
				{
					SetElementAttribute(xNode, "Name", name);
					break;
				}
				index++;
			}
			_ReportItems.Add(name, xNode);		// add generated name 
			this._ReportNodes.Add(xNode);
			return name;
		}

		internal string GenerateGroupingName(XmlNode xNode)
		{
			string basename=xNode.ParentNode.Name + "Group";
			string name;
			int index=1;
			List<string> dsets = new List<string>(this.DataSetNames);
			while (true)
			{
				name = basename + index.ToString();
				if (_Groupings.ContainsKey(name) == false && 
					dsets.IndexOf(name) < 0 &&
					_ReportItems.ContainsKey(name) == false)
				{
					SetElementAttribute(xNode, "Name", name);
					break;
				}
				index++;
			}
			_Groupings.Add(name, xNode);
			return name;
		}

		internal string NameError(XmlNode xNode, string name)
		{
			if (name == null || name.Trim().Length <= 0)
				return "Name must be provided.";
			if (!IsNameValid(name))
				return "Invalid characters in name.";

            XmlNode fNode;
            _ReportItems.TryGetValue(name, out fNode);
			if (fNode == xNode)
				return null;

            if (fNode != null)
                return DescriptionDuplicateNameError(name, fNode);

            // Grouping; also restrict to not being same name as any group or dataset
            if (xNode.Name == "Grouping")
			{
				_Groupings.TryGetValue(name, out fNode);
				if (fNode != null)
					return DescriptionDuplicateNameError(name, fNode);
                List<string> dsets = new List<string>(this.DataSetNames);
				if (dsets.IndexOf(name) >= 0)
					return "Duplicate name of the dataset :"+name;
			}

			return null;
		}

        private string DescriptionDuplicateNameError(string name, XmlNode fNode)
        {
            return String.Format(Strings.ReportNames_DescriptionDuplicateNameError, name, fNode.Name);
        }

		internal string GroupingNameCheck(XmlNode xNode, string name)
		{
			if (name == null || name.Trim().Length <= 0)
				return "Name must be provided.";
			if (!IsNameValid(name))
				return "Invalid characters in name.";

			// Grouping; also restrict to not being same name as any group or dataset
			XmlNode fNode;
            _Groupings.TryGetValue(name, out fNode);
			if (fNode != null && fNode != xNode)
				return "Duplicate name.";
            List<string> dsets = new List<string>(this.DataSetNames);
			if (dsets.IndexOf(name) >= 0)
				return "Duplicate name.";

			return null;
		}

		static internal bool IsNameValid(string name)
		{
			if (name == null || name.Length == 0)
				return false;

			// TODO use algorithm in http://www.unicode.org/unicode/reports/tr15/tr15-18.html#Programming%20Language%20Identifiers
			//  below regular expression isn't completely correct but matches most ascii language users
			//  expectations
			Match m = Regex.Match(name, @"\A[a-zA-Z_]+[a-zA-Z_0-9]*\Z");
			return m.Success;
		}

		internal void RemoveName(XmlNode xNode)
		{
			if (xNode == null)
				return;
			XmlAttribute xAttr = xNode.Attributes["Name"];
			if (xAttr == null)	
				return;

			_ReportItems.Remove(xAttr.Value);
			_ReportNodes.Remove(xNode);
			RemoveChildren(xNode);
		}

		private void RemoveChildren(XmlNode xNode)
		{
			XmlAttribute xAttr;
			foreach (XmlNode cNode in xNode.ChildNodes)
			{
				switch (cNode.Name)
				{
						// have a name but no subobjects
					case "Textbox":
					case "Image":
					case "Line":
					case "Subreport":
					case "Chart":
						xAttr = cNode.Attributes["Name"];
						if (xAttr != null)
						{
							_ReportItems.Remove(xAttr.Value);
							_ReportNodes.Remove(cNode);
						}
						break;
						// named object having subobjects
					case "Table":
					case "List":
					case "Rectangle":
					case "Matrix":
						RemoveChildren(cNode);
						xAttr = cNode.Attributes["Name"];
						if (xAttr != null)
						{
							_ReportItems.Remove(xAttr.Value);
							_ReportNodes.Remove(cNode);
						}
						break;
						// don't have a name and don't have named subobjects with names
					case "Style":
					case "Filters":
						break;
						// don't have a name but could have subobjects with names 
					default:
						RemoveChildren(cNode);		// recursively go down the hierarchy
						break;
				}
			}
		}
		
		private void SetElementAttribute(XmlNode parent, string name, string val)
		{
			XmlAttribute attr = parent.Attributes[name];
			if (attr != null)
			{
				attr.Value = val;
			}
			else
			{
				attr = _doc.CreateAttribute(name);
				attr.Value = val;
				parent.Attributes.Append(attr);
			}
			return;

		}

		/// <summary>
		/// Returns a collection of the GroupingNames
		/// </summary>
		internal string[] GroupingNames
		{
			get
			{
				if (_Groupings == null ||
					_Groupings.Count == 0)
					return null;
				string[] gn = new string[_Groupings.Count];
				int i=0;
				foreach (string o in _Groupings.Keys)
					gn[i++] = o;
				return gn;
			}
		}

		/// <summary>
		/// Returns a collection of the DataSetNames
		/// </summary>
		internal string[] DataSetNames
		{
			get 
			{
				List<string> ds = new List<string>();
				XmlNode rNode = _doc.LastChild;
				XmlNode node = DesignXmlDraw.FindNextInHierarchy(rNode, "DataSets");
				if (node == null)
					return ds.ToArray();
				foreach (XmlNode cNode in node.ChildNodes)
				{
					if (cNode.NodeType != XmlNodeType.Element || 
						cNode.Name != "DataSet")
						continue;
					XmlAttribute xAttr = cNode.Attributes["Name"];
					if (xAttr != null)
						ds.Add(xAttr.Value);
				}

				return ds.ToArray();
			}
		}

		internal XmlNode DataSourceName(string dsn)
		{
			XmlNode rNode = _doc.LastChild;
			XmlNode node = DesignXmlDraw.FindNextInHierarchy(rNode, "DataSources");
			if (node == null)
				return null;
			foreach (XmlNode cNode in node.ChildNodes)
			{
				if (cNode.Name != "DataSource")
					continue;
				XmlAttribute xAttr = cNode.Attributes["Name"];
				if (xAttr != null && xAttr.Value == dsn)
					return cNode;
			}
			return null;
		}

		/// <summary>
		/// Returns a collection of the DataSourceNames
		/// </summary>
		internal string[] DataSourceNames
		{
			get 
			{
                List<string> ds = new List<string>();
				XmlNode rNode = _doc.LastChild;
				XmlNode node = DesignXmlDraw.FindNextInHierarchy(rNode, "DataSources");
				if (node == null)
					return ds.ToArray();
				foreach (XmlNode cNode in node.ChildNodes)
				{
					if (cNode.NodeType != XmlNodeType.Element || 
						cNode.Name != "DataSource")
						continue;
					XmlAttribute xAttr = cNode.Attributes["Name"];
					if (xAttr != null)
						ds.Add(xAttr.Value);
				}

				return ds.ToArray();
			}
		}

		/// <summary>
		/// Returns a collection of the EmbeddedImage names
		/// </summary>
		internal string[] EmbeddedImageNames
		{
			get 
			{
                List<string> ds = new List<string>();
				XmlNode rNode = _doc.LastChild;
				XmlNode node = DesignXmlDraw.FindNextInHierarchy(rNode, "EmbeddedImages");
				if (node == null)
					return ds.ToArray();
				foreach (XmlNode cNode in node.ChildNodes)
				{
					if (cNode.NodeType != XmlNodeType.Element || 
						cNode.Name != "EmbeddedImage")
						continue;
					XmlAttribute xAttr = cNode.Attributes["Name"];
					if (xAttr != null)
						ds.Add(xAttr.Value);
				}

				return ds.ToArray();
			}
		}


		/// <summary>
		/// Gets the fields within the requested dataset.  If dataset is null then the first
		/// dataset is used.
		/// </summary>
		/// <param name="dataSetName"></param>
		/// <param name="asExpression">When true names are returned as expressions.</param>
		/// <returns></returns>
		internal string[] GetFields(string dataSetName, bool asExpression)
		{
			XmlNode nodes = DesignXmlDraw.FindNextInHierarchy(_doc.LastChild, "DataSets");
			if (nodes == null || !nodes.HasChildNodes)
				return null;

			// Find the right dataset
			XmlNode dataSet=null;
			foreach (XmlNode ds in nodes.ChildNodes)
			{
				if (ds.Name != "DataSet")
					continue;
				XmlAttribute xAttr = ds.Attributes["Name"];
				if (xAttr == null)
					continue;
				if (xAttr.Value == dataSetName || 
					dataSetName == null || dataSetName == "")
				{
					dataSet = ds;
					break;
				}
			}
			if (dataSet == null)
				return null;
			
			// Find the fields
			XmlNode fields = DesignXmlDraw.FindNextInHierarchy(dataSet, "Fields");
			if (fields == null || !fields.HasChildNodes)
				return null;
			StringCollection st = new StringCollection();
			foreach (XmlNode f in fields.ChildNodes)
			{
				if (f.Attributes == null)
					continue;
				XmlAttribute xAttr = f.Attributes["Name"];
				if (xAttr == null)
					continue;
				if (asExpression)
					st.Add(string.Format("=Fields!{0}.Value", xAttr.Value));
				else
					st.Add(xAttr.Value);
			}
			if (st.Count <= 0)
				return null;

			string[] result = new string[st.Count];
			st.CopyTo(result, 0);

			return result;
		}



        internal string GetReportParameterDefaultValue(string parameterExpression)
        {
            var root = _doc.DocumentElement;

            XmlNode rNode = _doc.LastChild;
            XmlNode rpsNode = DesignXmlDraw.FindNextInHierarchy(rNode, "ReportParameters");
            if (rpsNode == null)
                return null;

            var parameterName = DesignerUtility.ExtractParameterNameFromParameterExpression(parameterExpression);

            var parameter = rpsNode.ChildNodes.Cast<XmlNode>()
                .FirstOrDefault(n => n.Attributes["Name"].Value == parameterName);

            if (parameter == null)
                //ERROR, parameter not found;
                return null;

            var defaultValue = parameter.ChildNodes.Cast<XmlNode>()
                .FirstOrDefault(n => n.Name == "DefaultValue");
            if (defaultValue == null)
            {
                // ERROR, no default value;
                return null;
            }

            // selecting DefaultValue/Values/Value
            return defaultValue.FirstChild.FirstChild.InnerText;
        }

        internal string[] GetReportParameters(bool asExpression)
		{
			XmlNode rNode = _doc.LastChild;
			XmlNode rpsNode = DesignXmlDraw.FindNextInHierarchy(rNode, "ReportParameters");
			if (rpsNode == null)
				return null;
			StringCollection st = new StringCollection();
			foreach (XmlNode repNode in rpsNode)
			{	
				if (repNode.Name != "ReportParameter")
					continue;
				XmlAttribute nAttr = repNode.Attributes["Name"];
				if (nAttr == null)	// shouldn't really happen
					continue;
				if (asExpression)
					st.Add(string.Format("=Parameters!{0}.Value", nAttr.Value));
				else
					st.Add(nAttr.Value);
			}

			if (st.Count <= 0)
				return null;

			string[] result = new string[st.Count];
			st.CopyTo(result, 0);

			return result;
		}

        /// <summary>
        /// EBN 30/03/2014
        /// Get the modules defined in the report if any
        /// </summary>
        /// <param name="asExpression">When true names are returned as expressions.</param>
        /// <returns></returns>
        internal string[] GetReportModules(bool asExpression)
        {
            XmlNode rNode = _doc.LastChild;
            XmlNode rpsNode = DesignXmlDraw.FindNextInHierarchy(rNode, "CodeModules");
            if (rpsNode == null)
                return null;
            StringCollection st = new StringCollection();
            foreach (XmlNode repNode in rpsNode)
            {
                if (repNode.Name != "CodeModule")
                    continue;
                if (repNode.InnerText == "")	// shouldn't really happen
                    continue;
                if (asExpression)
                    st.Add(string.Format("=Module!{0}", repNode.InnerText));
                else
                    st.Add(repNode.InnerText);
            }

            if (st.Count <= 0)
                return null;

            string[] result = new string[st.Count];
            st.CopyTo(result, 0);

            return result;
        }

        /// <summary>
        /// EBN 30/03/2014
        /// Get the modules defined in the report if any
        /// </summary>
        /// <param name="asExpression">When true names are returned as expressions.</param>
        /// <returns></returns>
        internal string[] GetReportClasses(bool asExpression)
        {
            XmlNode rNode = _doc.LastChild;
            XmlNode rpsNode = DesignXmlDraw.FindNextInHierarchy(rNode, "Classes");
            if (rpsNode == null)
                return null;
            StringCollection st = new StringCollection();
            foreach (XmlNode repNode in rpsNode)
            {
                string ClassName = "";
                string InstanceName = "";

                if (repNode.Name != "Class")
                    continue;
                if (repNode.InnerText == "")	// shouldn't really happen
                    continue;
                foreach (XmlNode claNode in repNode)
                {
                    if (claNode.Name == "ClassName")
                        ClassName = claNode.InnerText;
                    if (claNode.Name == "InstanceName")
                        InstanceName = claNode.InnerText;
                }
                if (ClassName != "")
                {
                    if (asExpression)
                        st.Add(string.Format("=({0}){1}",ClassName,InstanceName));
                    else
                        st.Add(string.Format("{0}", ClassName));
                }
            }

            if (st.Count <= 0)
                return null;

            string[] result = new string[st.Count];
            st.CopyTo(result, 0);

            return result;
        }

    }

}
