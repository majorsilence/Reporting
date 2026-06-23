

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Reflection;
using System.Threading.Tasks;


namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// CodeModules definition and processing.
	///</summary>
	[Serializable]
	internal class CodeModules : ReportLink, IEnumerable
	{
        List<CodeModule> _Items;			// list of code module

		internal CodeModules(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
            _Items = new List<CodeModule>();
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				if (xNodeLoop.Name == "CodeModule")
				{
					CodeModule cm = new CodeModule(r, this, xNodeLoop);
					_Items.Add(cm);
				}
				else
				{
					// don't know this element - log it
					OwnerReport.rl.LogError(4, "Unknown CodeModules element '" + xNodeLoop.Name + "' ignored.");
				}
			}
			if (_Items.Count == 0)
				OwnerReport.rl.LogError(8, "For CodeModules at least one CodeModule is required.");
			else
                _Items.TrimExcess();
		}
		/// <summary>
		/// Return the Type given a class name.  Checks the AOT pre-registration first,
		/// then searches the CodeModules that are specified in the report.
		/// </summary>
		internal Type this[string s]
		{
			get
			{
				if (s == string.Empty)
					return null;

				// AOT path: check pre-registered types before attempting assembly scan
				if (RdlEngineConfig.TryGetRegisteredType(s, out Type? registered))
					return registered;

				Type tp = null;
				try
				{
					// loop thru all the codemodules looking for the assembly
					//  that contains this type
					foreach (CodeModule cm in _Items)
					{
						Assembly a = cm.LoadedAssembly();
						if (a != null)
						{
							tp = a.GetType(s, false, true);
							if (tp != null)
								break;
						}
					}
				}
				catch (Exception ex)
				{
					OwnerReport.rl.LogError(4, string.Format("Exception finding type. {0}", ex.Message));
				}
				return tp;
			}
		}
		
		async override internal Task FinalPass()
		{
			foreach (CodeModule cm in _Items)
			{
                await cm.FinalPass();
			}
			return;
		}

		internal void LoadModules()
		{
			foreach (CodeModule cm in _Items)
			{
				cm.LoadedAssembly();
			}
		}

        internal List<CodeModule> Items
		{
			get { return  _Items; }
		}
		
		#region IEnumerable Members

		public IEnumerator GetEnumerator()
		{
			return _Items.GetEnumerator();
		}

		#endregion
	}
}
