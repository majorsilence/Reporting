using System.ComponentModel;
using Majorsilence.Reporting.RdlDesign.RdlProperties;

namespace Majorsilence.Reporting.RdlDesign
{
	public class LocalizedDescriptionAttribute : DescriptionAttribute
	{
		public LocalizedDescriptionAttribute(string description)
			: base(description)
		{
		}

		public override string Description
		{
			get
			{
				return Descriptions.ResourceManager.GetString(DescriptionValue, DisplayNames.Culture) ?? base.Description;
			}
		}
	}
}
