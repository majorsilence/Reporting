using Majorsilence.Reporting.RdlDesign.RdlProperties;
using System.ComponentModel;
using System.Threading;

namespace Majorsilence.Reporting.RdlDesign
{
	internal class LocalizedCategoryAttribute : CategoryAttribute
	{
		public LocalizedCategoryAttribute(string category)
			: base(category)
		{
		}

		protected override string GetLocalizedString(string value)
		{
			return Categories.ResourceManager.GetString(value, Categories.Culture) ?? base.GetLocalizedString(value);
		}
	}
}
