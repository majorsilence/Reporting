
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;            // need this for the properties metadata
using System.Xml;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// PropertyChart - The Chart Properties
    /// </summary>
    internal class PropertyDataRegion : PropertyReportItem
    {
        public PropertyDataRegion(DesignXmlDraw d, DesignCtl dc, List<XmlNode> ris) : base(d, dc, ris)
        {
        }
        
		[LocalizedCategory("DataRegion")]
		[LocalizedDisplayName("DataRegion_NoRows")]
		[LocalizedDescription("DataRegion_NoRows")]
        public PropertyExpr NoRows
        {
            get { return new PropertyExpr(this.GetValue("NoRows", "")); }
            set
            {
                if (value.Expression == null || value.Expression.Length == 0)
                    this.RemoveValue("NoRows");
                else
                    this.SetValue("NoRows", value.Expression);
            }
        }

        [LocalizedCategory("DataRegion")]
		[LocalizedDisplayName("DataRegion_KeepTogether")]
		[LocalizedDescription("DataRegion_KeepTogether")]
        public bool KeepTogether
        {
            get { return this.GetValue("KeepTogether", "True").ToLower().Trim() == "true"; }
            set
            {
                this.SetValue("KeepTogether", value);
            }
        }

        [LocalizedCategory("DataRegion")]
		[TypeConverter(typeof(DataSetsConverter))]
		[LocalizedDisplayName("DataRegion_DataSetName")]
		[LocalizedDescription("DataRegion_DataSetName")]
        public string DataSetName
        {
            get { return this.GetValue("DataSetName", ""); }
            set
            {
                if (value == null || value.Length == 0)
                    this.RemoveValue("DataSetName");
                else
                    this.SetValue("DataSetName", value);
            }
        }

        [LocalizedCategory("DataRegion")]
		[LocalizedDisplayName("DataRegion_PageBreakAtStart")]
		[LocalizedDescription("DataRegion_PageBreakAtStart")]
        public bool PageBreakAtStart
        {
            get { return this.GetValue("PageBreakAtStart", "True").ToLower().Trim() == "true"; }
            set
            {
                this.SetValue("PageBreakAtStart", value);
            }
        }

        [LocalizedCategory("DataRegion")]
		[LocalizedDisplayName("DataRegion_PageBreakAtEnd")]
		[LocalizedDescription("DataRegion_PageBreakAtEnd")]
        public bool PageBreakAtEnd
        {
            get { return this.GetValue("PageBreakAtEnd", "True").ToLower().Trim() == "true"; }
            set
            {
                this.SetValue("PageBreakAtEnd", value);
            }
        }

        [LocalizedCategory("DataRegion")]
		[LocalizedDisplayName("DataRegion_Filters")]
		[LocalizedDescription("DataRegion_Filters")]
        public PropertyFilters Filters
        {
            get
            {
                return new PropertyFilters(this);
            }
        }

		[LocalizedCategory("DataRegion")]
		[LocalizedDisplayName("DataRegion_Appearance")]
		[LocalizedDescription("DataRegion_Appearance")]
        public PropertyAppearance Appearance
        {
            get { return new PropertyAppearance(this); }
        }
    }

    #region DataSets
    internal class DataSetsConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            PropertyReportItem pr = context.Instance as PropertyReportItem;
            if (pr == null)
                return base.GetStandardValues(context);

            // Populate with the list of datasets
            ArrayList ar = new ArrayList();
            ar.Add("");         // add an empty string to the collection
            object[] dsn = pr.Draw.DataSetNames;
            if (dsn != null)
                ar.AddRange(dsn);
            return new StandardValuesCollection(ar);
        }
    }
    #endregion

}
