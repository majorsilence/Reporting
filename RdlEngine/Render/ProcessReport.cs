
using System;
using Majorsilence.Reporting.RdlEngine.Resources;
using Majorsilence.Reporting.Rdl;
using System.IO;
using System.Collections;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	
	///<summary>
	///The primary class to "run" a report to the supported output presentation types
	///</summary>

	public enum OutputPresentationType
	{
		HTML,
        RenderPdf_iTextSharp,
		PDF,
        PDFOldStyle,
		XML,
		ASPHTML,
		Internal,
		MHTML,
        CSV,
        RTF,
        Word,
        ExcelTableOnly,
		Excel2007,
		Excel2007DataOnly,
        TIF,
        TIFBW,                   // black and white tif
        Excel2003,
        RenderPdf_Majorsilence
    }

	[Serializable]
	public class ProcessReport
	{
		Report r;					// report
		IStreamGen _sg;

		public ProcessReport(Report rep, IStreamGen sg)
		{
			if (rep.rl.MaxSeverity > 4)
				throw new Exception(Strings.ProcessReport_Error_ReportHasErrors);

			r = rep;
			_sg = sg;
		}

		public ProcessReport(Report rep)
		{
			if (rep.rl.MaxSeverity > 4)
				throw new Exception(Strings.ProcessReport_Error_ReportHasErrors);

			r = rep;
			_sg = null;
		}

		// Run the report passing the parameter values and the output
		public async Task Run(IDictionary parms, OutputPresentationType type)
		{
            await r.RunGetData(parms);

            await r.RunRender(_sg, type);

			return;
		}

	}
}
