using System.ComponentModel;
using RdlMapFile.RdlProperties;

namespace Majorsilence.Reporting.RdlMapFile
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
