using System;
using System.Collections;
using System.ComponentModel;
using Majorsilence.Forms;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using Majorsilence.Reporting.RdlDesign.Resources;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// Summary description for DialogAbout.
    /// </summary>
    public partial class DialogValidateRdl
    {
        internal const string SCHEMA2025 =
            "https://reporting.majorsilence.com/schemas/reporting/2025/12/reportdefinition";

        private const string SCHEMA2025NAME =
            "https://reporting.majorsilence.com/schemas/reporting/2025/12/reportdefinition/ReportDefinition.xsd";

        private int _ValidationErrorCount;
        private int _ValidationWarningCount;

        public DialogValidateRdl(RdlDesigner designer)
        {
            _RdlDesigner = designer;
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            return;
        }

        // csharp
        private void bValidate_Click(object sender, System.EventArgs e)
        {
            MDIChild mc = _RdlDesigner.ActiveMdiChild as MDIChild;
            if (mc == null || mc.DesignTab != DesignTabs.Edit)
            {
                MessageBox.Show(Strings.DialogValidateRdl_ShowC_SelectRDLTab);
                return;
            }

            string syntax = mc.SourceRdl;
            Cursor saveCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            StringReader sr = null;
            XmlTextReader tr = null;
            XmlReader vr = null;

            try
            {
                _ValidationErrorCount = 0;
                _ValidationWarningCount = 0;
                this.lbSchemaErrors.Items.Clear();
                sr = new StringReader(syntax);
                tr = new XmlTextReader(sr);
                XmlReaderSettings xrs = new XmlReaderSettings();
                xrs.ValidationEventHandler += ValidationHandler;
                xrs.ValidationFlags = XmlSchemaValidationFlags.AllowXmlAttributes |
                                      XmlSchemaValidationFlags.ProcessIdentityConstraints |
                                      XmlSchemaValidationFlags.ProcessSchemaLocation |
                                      XmlSchemaValidationFlags.ProcessInlineSchema;
                
                string designerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReportDefinition.xsd");

                using (var fs = File.OpenRead(designerPath))
                using (var dsr = XmlReader.Create(fs))
                {
                    XmlSchema designerSchema = XmlSchema.Read(dsr, new ValidationEventHandler(ValidationHandler));
                    if (designerSchema != null)
                    {
                        // Add the schema object so its TargetNamespace and includes/imports are preserved
                        xrs.Schemas.Add(designerSchema);
                    }
                }

                vr = XmlReader.Create(tr, xrs);

                while (vr.Read()) ;

                this.lbSchemaErrors.Items.Add(string.Format("Validation completed with {0} warnings and {1} errors.",
                    _ValidationWarningCount, _ValidationErrorCount));
            }
            catch (Exception ex)
            {
                this.lbSchemaErrors.Items.Add(ex.Message + "  Processing terminated.");
            }
            finally
            {
                Cursor.Current = saveCursor;
                if (sr != null)
                    sr.Close();
                if (tr != null)
                    tr.Close();
                if (vr != null)
                    vr.Close();
            }
        }


        public void ValidationHandler(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Error)
                this._ValidationErrorCount++;
            else
                this._ValidationWarningCount++;

            this.lbSchemaErrors.Items.Add(string.Format("{0}: {1} ({2}, {3})",
                args.Severity, args.Message, args.Exception.LineNumber, args.Exception.LinePosition));
        }

        private void lbSchemaErrors_DoubleClick(object sender, System.EventArgs e)
        {
            RdlEditPreview rep = _RdlDesigner.GetEditor();

            if (rep == null || this.lbSchemaErrors.SelectedIndex < 0)
                return;
            try
            {
                // line numbers are reported as (line#, character offset) e.g. (110, 32)  
                string v = this.lbSchemaErrors.Items[lbSchemaErrors.SelectedIndex] as string;
                int li = v.LastIndexOf("(");
                if (li < 0)
                    return;
                v = v.Substring(li + 1);
                li = v.IndexOf(","); // find the
                v = v.Substring(0, li);

                int nLine = Int32.Parse(v);
                rep.Goto(this, nLine);
                this.BringToFront();
            }
#if DEBUG
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message); // developer might care about this error??
            }
#else
			catch 
			{}		// user doesn't really care if something went wrong
#endif
        }

        private void bClose_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }

        private void DialogValidateRdl_Closing(object sender, Majorsilence.Forms.FormClosingEventArgs e)
        {
            this._RdlDesigner.ValidateSchemaClosing();
        }
    }
}