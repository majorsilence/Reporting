using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// The Variables of a report or a group (RDL 2008 and later): named values that expressions
	/// read as Variables!Name.Value.
	/// </summary>
	[Serializable]
	internal class Variables : ReportLink
	{
		readonly List<Variable> _Items;

		internal Variables(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Items = new List<Variable>();
			foreach (XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				if (xNodeLoop.LocalName == "Variable")
				{
					var v = new Variable(r, this, xNodeLoop);
					if (v.Name == null)
						OwnerReport.rl.LogError(8, "A Variable must have a Name.");
					else if (this[v.Name] != null)
						OwnerReport.rl.LogError(8, "Duplicate Variable name '" + v.Name + "'.");
					else
						_Items.Add(v);
				}
				else
					OwnerReport.rl.LogError(4, "Unknown Variables element '" + xNodeLoop.Name + "' ignored.");
			}
		}

		internal Variable this[string name]
		{
			get
			{
				foreach (Variable v in _Items)
					if (string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase))
						return v;
				return null;
			}
		}

		internal List<Variable> Items => _Items;

		internal override async Task FinalPass()
		{
			foreach (Variable v in _Items)
				await v.FinalPass().ConfigureAwait(false);
		}
	}

	/// <summary>
	/// A report variable is evaluated once per report run; a group variable in the scope of the
	/// group, so that an aggregate in it is the group instance's.
	/// </summary>
	[Serializable]
	internal class Variable : ReportLink
	{
		internal Variable(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			if (xNode.Attributes?["Name"] is XmlAttribute name)
				Name = name.Value;
			foreach (XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.LocalName)
				{
					case "Value":
						Value = new Expression(r, this, xNodeLoop, ExpressionType.Variant);
						break;
					case "Writable":
						Writable = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					default:
						OwnerReport.rl.LogError(4, "Unknown Variable element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
			if (Value == null)
				Value = new Expression(r, this, string.Empty, ExpressionType.Variant);
		}

		internal string Name { get; }
		internal Expression Value { get; }
		internal bool Writable { get; }

		/// <summary>True for a variable of the report rather than of a group.</summary>
		internal bool IsReportVariable => Parent?.Parent == null;

		internal override async Task FinalPass()
		{
			await Value.FinalPass().ConfigureAwait(false);
		}
	}
}
