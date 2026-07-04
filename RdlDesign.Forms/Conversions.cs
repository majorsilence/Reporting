using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Majorsilence.Reporting.RdlDesign
{
	public class Conversions
	{

		public static int MeasurementTypeAsHundrethsOfAnInch(string value)
		{
			string measurementType = value.Trim().ToLower();
			string measurementValue = "0";
			if (measurementType.Length >= 2)
			{
				measurementValue = measurementType.Substring(0, measurementType.Length - 2);
				measurementType = measurementType.Substring(measurementType.Length - 2);
			}

			if (measurementType == "mm")
			{
				// metric.  Convert to imperial for now
				return (int)((decimal.Parse(measurementValue) / 25.4m) * 100);
			}
			else if(measurementType == "in")
			{
				// assume imperial
				return (int)(decimal.Parse(measurementValue) * 100);
			}

			throw new Exception("Invalid measurment type.  mm and in are only supported types");



		}


	}
}
