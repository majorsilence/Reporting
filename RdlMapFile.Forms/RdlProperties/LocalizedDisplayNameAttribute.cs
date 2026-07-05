using System.ComponentModel;
using RdlMapFile.RdlProperties;

namespace Majorsilence.Reporting.RdlMapFile
{
	internal class LocalizedDisplayNameAttribute : DisplayNameAttribute
	{
		internal LocalizedDisplayNameAttribute(string displayName)
			: base(displayName)
		{
		}

		public override string DisplayName
		{
			get { return DisplayNames.ResourceManager.GetString(DisplayNameValue, DisplayNames.Culture) ?? base.DisplayName; }
		}
	}
}
