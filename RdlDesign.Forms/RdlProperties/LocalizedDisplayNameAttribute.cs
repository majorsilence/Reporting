using System.ComponentModel;
using Majorsilence.Reporting.RdlDesign.RdlProperties;

namespace Majorsilence.Reporting.RdlDesign
{
	internal class LocalizedDisplayNameAttribute : DisplayNameAttribute
	{
		public LocalizedDisplayNameAttribute(string displayName)
			: base(displayName)
		{
		}

		public override string DisplayName
		{
			get { return DisplayNames.ResourceManager.GetString(DisplayNameValue, DisplayNames.Culture) ?? base.DisplayName; }
		}
	}
}
