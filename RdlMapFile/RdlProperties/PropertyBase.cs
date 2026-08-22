
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;            // need this for the properties metadata
using System.Xml;
using System.Text.RegularExpressions;
using System.Drawing.Design;
using System.Globalization;
using Majorsilence.Forms;
using RdlMapFile.Resources;

namespace Majorsilence.Reporting.RdlMapFile
{
    /// <summary>
    /// PropertyReportItem - The ReportItem Properties
    /// </summary>
    [DefaultProperty("Text")]
    internal class PropertyBase
    {
   		private DesignXmlDraw _Draw;

        internal PropertyBase(DesignXmlDraw d)
        {
            _Draw = d;
        }

        internal DesignXmlDraw Draw
        {
            get { return _Draw; }
        }

        internal string GetTextValue(string l)
        {
            XmlNode v = Draw.SelectedItem;
            return Draw.GetElementValue(v, l, "");
        }

        internal void SetTextValue(string l, string v)
        {
            Draw.StartUndoGroup(l + " " + Strings.PropertyBase_Undo_change);
            XmlNode xn = Draw.SelectedItem;
            foreach (XmlNode n in Draw.SelectedList)
            {
                if (xn.Name == n.Name)
                    Draw.SetElement(n, l, v);
            }
            Draw.EndUndoGroup();

            Draw.SignalXmlChanged();
            Draw.Invalidate();
        }
        internal void SetTextValue(string l, float f)
        {
            string fs = string.Format(NumberFormatInfo.InvariantInfo, "{0}", f);
            SetTextValue(l, fs);
        }

    }
}
