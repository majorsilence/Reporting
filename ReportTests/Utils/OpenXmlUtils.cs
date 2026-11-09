using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

namespace ReportTests.Utils
{
    public static class OpenXmlUtils
    {


        public static bool ValidateSpreadsheetDocument(string calcfile)
        {
            return ValidateOpenXmlDocument(SpreadsheetDocument.Open(calcfile, true));
        }

        /// <summary>
        /// Validate a Word document (.docx) against the Open XML schemas; throws with the errors found
        /// </summary>
        public static bool ValidateWordprocessingDocument(System.IO.Stream docx)
        {
            using (WordprocessingDocument doc = WordprocessingDocument.Open(docx, false))
                return ValidateOpenXmlDocument(doc);
        }


        private static bool ValidateOpenXmlDocument(OpenXmlPackage package )
        {
            string errorDesc = "";
            try
            {
                OpenXmlValidator validator = new OpenXmlValidator();
                int count = 0;
                foreach (ValidationErrorInfo error in validator.Validate(package))
                {
                    count++;
                    errorDesc += String.Format("Error: {0}", count) + Environment.NewLine;
                    errorDesc += String.Format("Description: {0}", error.Description) + Environment.NewLine;
                    errorDesc += String.Format("Path: {0}", error.Path.XPath) + Environment.NewLine;
                    errorDesc += String.Format("Part: {0}", error.Part.Uri) + Environment.NewLine;
                    errorDesc += "-------------------------------------------" + Environment.NewLine;
                }
            }
            catch (Exception ex)
            {
                errorDesc += ex.Message;
            }
            if (!string.IsNullOrEmpty(errorDesc))
            {
                throw new Exception(errorDesc);
            }
            return true;
        }
    }
}
