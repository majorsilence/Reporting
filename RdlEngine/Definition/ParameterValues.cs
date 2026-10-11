
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
    ///<summary>
    /// Collection of parameter values.
    ///</summary>
    [Serializable]
    internal class ParameterValues : ReportLink
    {
        List<ParameterValue> _Items;            // list of ParameterValue

        internal ParameterValues(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            ParameterValue pv;
            _Items = new List<ParameterValue>();
            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name)
                {
                    case "ParameterValue":
                        pv = new ParameterValue(r, this, xNodeLoop);
                        break;
                    default:
                        pv = null;      // don't know what this is
                                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown ParameterValues element '" + xNodeLoop.Name + "' ignored.");
                        break;
                }
                if (pv != null)
                    _Items.Add(pv);
            }

            if (_Items.Count == 0)
                OwnerReport.rl.LogError(8, "For ParameterValues at least one ParameterValue is required.");
            else
                _Items.TrimExcess();
        }

        async override internal Task FinalPass()
        {
            foreach (ParameterValue pv in _Items)
            {
                await pv.FinalPass().ConfigureAwait(false);
            }
            return;
        }

        internal List<ParameterValue> Items
        {
            get { return _Items; }
        }

        internal async Task<(string[] displayValues, object[] dataValues)> SupplyValues(Report rpt)
        {
            string[] displayValues = new string[_Items.Count];
            object[] dataValues = new object[_Items.Count];
            int index = 0;
            // go thru the parameters extracting the data values
            foreach (ParameterValue pv in _Items)
            {
                if (pv.Value == null)
                    dataValues[index] = null;
                else
                    dataValues[index] = await pv.Value.Evaluate(rpt, null).ConfigureAwait(false);
                if (pv.Label == null)
                {   // if label is null use the data value; if not provided use ""
                    if (dataValues[index] == null)
                        displayValues[index] = "";
                    else
                        displayValues[index] = dataValues[index].ToString();
                }
                else
                {
                    displayValues[index] = await pv.Label.EvaluateString(rpt, null).ConfigureAwait(false);
                    if (displayValues[index] == null)
                        displayValues[index] = "";
                }
                index++;
            }
            return (displayValues, dataValues);
        }
    }
}
