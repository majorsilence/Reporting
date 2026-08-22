
using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RdlDesign.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Majorsilence.Forms.Drawing;
using System.Reflection;
using System.Text;
using Majorsilence.Forms;
using System.Xml;
using System.Linq;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// DialogListOfStrings: puts up a dialog that lets a user enter a list of strings
    /// </summary>
    public partial class DialogExprEditor
    {
        Type[] BASE_TYPES = new Type[] 
      { 
         typeof(string), 
         typeof(double), 
         typeof(Single), 
         typeof(decimal), 
         typeof(DateTime), 
         typeof(char), 
         typeof(bool), 
         typeof(int), 
         typeof(short), 
         typeof(long), 
         typeof(byte), 
         typeof(UInt16), 
         typeof(UInt32), 
         typeof(UInt64) 
      };

        // design draw 
        private bool _Color;				// true if color list should be displayed

		RdlScriptLexer rdlLexer = new RdlScriptLexer();

		string resultExpression = null; //It workaround System.AccessViolationException geting from Scintilla after form closed

        internal DialogExprEditor(DesignXmlDraw dxDraw, string expr, XmlNode node) :
            this(dxDraw, expr, node, false)
        {
        }

        internal DialogExprEditor(DesignXmlDraw dxDraw, string expr, XmlNode node, bool bColor)
        {
            _Draw = dxDraw;
            _Color = bColor;
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            var styling = new ScintillaExprStyle(rdlLexer, scintilla1);
			styling.ConfigureScintillaStyle();

            // Fill out the fields list 
            string[] fields = null;
            // Find the dataregion that contains the item (if any)
            for (XmlNode pNode = node; pNode != null; pNode = pNode.ParentNode)
            {
                if (pNode.Name == "List" ||
                    pNode.Name == "Table" ||
                    pNode.Name == "Matrix" ||
                    pNode.Name == "Chart")
                {
                    string dsname = _Draw.GetDataSetNameValue(pNode);
                    if (dsname != null)	// found it
                    {
                        fields = _Draw.GetFields(dsname, true);
                    }
                }
            }
            BuildTree(fields);
			FillLexerByFields(_Draw);

			if (expr != null)
				scintilla1.Text = expr.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);

            return;
        }

		void FillLexerByFields(DesignXmlDraw dxDraw)
		{
			var fields = new List<string>();
			foreach (var dataSet in dxDraw.DataSetNames)
			{
				var datasetFields = dxDraw.GetFields((string)dataSet, false);
				if(datasetFields != null)
					fields.AddRange(datasetFields);
			}
			rdlLexer.SetFields(fields);
		}

        void BuildTree(string[] flds)
        {
            // suppress redraw until tree view is complete
            tvOp.BeginUpdate();

            //AJM GJL Adding Missing 'User' Menu
            // Handle the user
            InitUsers();
            // Handle the globals
            InitGlobals();
            // Fields - only when a dataset is specified
            InitFields(flds);
			// Fields - is missing
			InitFieldsIsMissing(flds);
            // Report parameters
            InitReportParameters();
            // Handle the functions
            InitFunctions();
            // Aggregate functions
            InitAggrFunctions();
            // Operators
            InitOperators();
            // Colors (if requested)
            InitColors();
            // Modules and class (if any)
            // EBN 30/03/2014
            InitReportModulesAndClass();

            tvOp.EndUpdate();

        }
        // Josh: 6:22:10 Added as a uniform method of addind nodes to the TreeView.
        void InitTreeNodes(string node, IEnumerable<string> list)
        {
            TreeNode ndRoot = new TreeNode(node);
            tvOp.Nodes.Add(ndRoot);
            foreach (string item in list)
            {
                TreeNode aRoot = new TreeNode(item);
                ndRoot.Nodes.Add(aRoot);
            }
        }

        // Josh: 6:22:10 Begin Init Methods.
        // Methods have been changed to use InitTreeNodes
        // and ArrayToFormattedList methods
        // Initializes the user functions
        void InitUsers()
        {
            List<string> users = StaticLists.ArrayToFormattedList(StaticLists.UserList, "{!", "}");
            InitTreeNodes("User", users);
        }
        // Initializes the Globals
        void InitGlobals()
        {
            List<string> globals = StaticLists.ArrayToFormattedList(StaticLists.GlobalList, "{@", "}");
            InitTreeNodes("Globals", globals);
        }
        // Initializes the database fields
        void InitFields(string[] flds)
        {
            if (flds != null && flds.Length > 0)
            {
                List<string> fields = StaticLists.ArrayToFormattedList(flds, "{", "}");
                InitTreeNodes("Fields", fields);
            }
        }
		// Initializes the database fields is missing
		void InitFieldsIsMissing(string[] flds)
		{
			if (flds != null && flds.Length > 0)
			{
				List<string> fields = StaticLists.ArrayToFormattedList(flds, "Fields!", ".IsMissing");
				InitTreeNodes("Check for null", fields);
				//fieldsKeys.AddRange(fields);
			}
		}
        // Initializes the aggregate functions
        void InitAggrFunctions()
        {
            InitTreeNodes("Aggregate Functions", StaticLists.AggrFunctionList);
        }
        // Initializes the operators
        void InitOperators()
        {
            InitTreeNodes("Operators", StaticLists.OperatorList);
        }
        // Initializes the colors
        void InitColors()
        {
            if (_Color)
            {
                InitTreeNodes("Colors", StaticLists.ColorList);
            }
        }
        /// <summary>
        /// Populate tree view with the report parameters (if any)
        /// </summary>
        void InitReportParameters()
        {
            string[] ps = _Draw.GetReportParameters(true);

            if (ps != null && ps.Length != 0)
            {
                List<string> parameters = StaticLists.ArrayToFormattedList(ps, "{?", "}");
                InitTreeNodes("Parameters", parameters);
				rdlLexer.SetParameters(StaticLists.ArrayToFormattedList(ps, "", ""));
            }
        }


        /// <summary>
        /// Populate tree view with the Module and Class parameters (if any)
        /// EBN 30/03/2014
        /// </summary>
        void InitReportModulesAndClass()
        {
            string[] ms = _Draw.GetReportModules(false);
            string[] qs = _Draw.GetReportClasses(false);
            List<string> mc = new List<string>();
            if (ms == null) return;
            if (qs == null) return;

            // Try to load the assembly if not already loaded
            foreach (string s in ms)
            {
                try
                {
                    Assembly a = Assembly.LoadFrom(s);
                    foreach (string c in qs)
                    {
                        Type t = a.GetType(c);
                        if (t != null)
                        {
                            // Class is found in this assembly
                            BuildMethods(mc, t, c + ".");
                        }
                    }
                }
                catch
                {
                    // Nothing to do, we can not load the assembly...
                }
            }


            if (qs != null && qs.Length != 0)
            {
                InitTreeNodes("Modules", mc);
            }
        }

        //Done: Look at grouping items, such as Math, Financial, etc
        // Josh: 6:21:10 added grouping for common items.
        void InitFunctions()
        {
            List<string> ar = new List<string>();

            ar.AddRange(StaticLists.FunctionList);

            // Build list of methods in the VBFunctions class
            Majorsilence.Reporting.Rdl.FontStyleEnum fsi = FontStyleEnum.Italic; // just want a class from RdlEngine.dll assembly
            Assembly a = Assembly.GetAssembly(fsi.GetType());
            if (a == null)
                return;
            Type ft = a.GetType("Majorsilence.Reporting.Rdl.VBFunctions");
            BuildMethods(ar, ft, "");

            // build list of financial methods in Financial class
            ft = a.GetType("Majorsilence.Reporting.Rdl.Financial");
            BuildMethods(ar, ft, "Financial.");

            a = Assembly.GetAssembly("".GetType());
            ft = a.GetType("System.Math");
            BuildMethods(ar, ft, "Math.");

            ft = a.GetType("System.Convert");
            BuildMethods(ar, ft, "Convert.");

            ft = a.GetType("System.String");
            BuildMethods(ar, ft, "String.");

            ar.Sort();

            TreeNode ndRoot = new TreeNode("Functions");
            tvOp.Nodes.Add(ndRoot);

            foreach (TreeNode node in GroupMethods(RemoveDuplicates(ar)))
            {
                ndRoot.Nodes.Add(node);
            }
        }
        // Josh: 6:22:10 End Init Methods

        List<string> RemoveDuplicates(IEnumerable<string> ar)
        {
            List<string> newAr = new List<string>();
            string previous = "";
            foreach (string str in ar)
            {
                if (str != previous)
                    newAr.Add(str);

                previous = str;
            }

            return newAr;
        }

        List<TreeNode> GroupMethods(List<string> ar)
        {
            List<TreeNode> nodeList = new List<TreeNode>();

            string group = " ";
            foreach (string str in ar)
            {
                if (!str.StartsWith(group))
                {
                    if (str.Contains("."))
                    {
                        if (str.IndexOf("(") > str.IndexOf("."))
                        {
                            group = str.Split('.')[0];
                            TreeNode aRoot = new TreeNode(group);
                            List<string> groupList = ar.FindAll(
                            delegate(string methodName)
                            {
                                return methodName.StartsWith(group);
                            }
                            );

                            if (groupList != null)
                            {
                                foreach (string method in groupList)
                                {
                                    aRoot.Nodes.Add(new TreeNode(
                                    method.Replace(group, string.Empty)
                                    .Replace(".", string.Empty)));
                                }
                            }
                            nodeList.Add(aRoot);
                        }
                        else
                        {
                            nodeList.Add(new TreeNode(str));
                        }
                    }
                    else
                    {
                        nodeList.Add(new TreeNode(str));
                    }
                }
            }
            return nodeList;
        }

        void InitFunctions(TreeNode ndRoot)
        {
            List<string> ar = new List<string>();

            ar.AddRange(StaticLists.FunctionList);

            // Build list of methods in the  VBFunctions class
            Majorsilence.Reporting.Rdl.FontStyleEnum fsi = FontStyleEnum.Italic;	// just want a class from RdlEngine.dll assembly
            Assembly a = Assembly.GetAssembly(fsi.GetType());
            if (a == null)
                return;
            Type ft = a.GetType("Majorsilence.Reporting.Rdl.VBFunctions");
            BuildMethods(ar, ft, "");

            // build list of financial methods in Financial class
            ft = a.GetType("Majorsilence.Reporting.Rdl.Financial");
            BuildMethods(ar, ft, "Financial.");

            a = Assembly.GetAssembly("".GetType());
            ft = a.GetType("System.Math");
            BuildMethods(ar, ft, "Math.");

            ft = a.GetType("System.Convert");
            BuildMethods(ar, ft, "Convert.");

            ft = a.GetType("System.String");
            BuildMethods(ar, ft, "String.");

            ar.Sort();
            string previous = "";
            foreach (string item in ar)
            {
                if (item != previous)	// don't add duplicates
                {
                    // Add the node to the tree
                    TreeNode aRoot = new TreeNode(item);
                    ndRoot.Nodes.Add(aRoot);
                }
                previous = item;
            }

        }

        void BuildMethods(List<string> ar, Type ft, string prefix)
        {
            if (ft == null)
                return;
            MethodInfo[] mis = ft.GetMethods(BindingFlags.Static | BindingFlags.Public);
            foreach (MethodInfo mi in mis)
            {
                // Add the node to the tree
                string name = BuildMethodName(mi);
                if (name != null)
                    ar.Add(prefix + name);
            }
        }

        string BuildMethodName(MethodInfo mi)
        {
            StringBuilder sb = new StringBuilder(mi.Name);
            sb.Append("(");
            ParameterInfo[] pis = mi.GetParameters();
            bool bFirst = true;
            foreach (ParameterInfo pi in pis)
            {
                if (!IsBaseType(pi.ParameterType))
                    return null;
                if (bFirst)
                    bFirst = false;
                else
                    sb.Append(", ");
                sb.Append(pi.Name);
            }
            sb.Append(")");
            return sb.ToString();
        }

        // Determines if underlying type is a primitive
        bool IsBaseType(Type t)
        {
            foreach (Type bt in BASE_TYPES)
            {
                if (bt == t)
                    return true;
            }

            return false;
        }

        public string Expression
        {
            get { return resultExpression != null ? resultExpression : scintilla1.Text; }
        }

        private void bCopy_Click(object sender, System.EventArgs e)
        {
            if (tvOp.SelectedNode == null ||
            tvOp.SelectedNode.Parent == null ||
            tvOp.SelectedNode.Nodes.Count > 0)
                return; // this is the top level nodes (Fields, Parameters, ...)

            TreeNode node = (TreeNode)tvOp.SelectedNode;
            string t = string.Empty;
            // Josh: 6:21:10 Changed to add parent node name for grouped nodes (eg: Convert.ToByte(value))
            // and not to add it for the root functions (the non grouped).
            if (tvOp.Nodes.Contains(node.Parent))
                t = node.Text;
            else
                t = node.Parent.Text + "." + node.Text;

            if (scintilla1.TextLength == 0)
                t = "=" + t;
            scintilla1.ReplaceSelection(t);
        }

		private void tvOp_DoubleClick(object sender, EventArgs e)
		{
			bCopy.PerformClick();
		}

		private void bOK_Click(object sender, EventArgs e)
		{
			resultExpression = scintilla1.Text;
		}
    }

}
