using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Majorsilence.Forms.WinForms;
using Majorsilence.Pdf.Security;

namespace LibRdlWpfViewer
{
    /// <summary>
    /// Interaction logic for RdlWpfViewer.xaml
    /// </summary>
    public partial class RdlWpfViewer : UserControl
    {
        private readonly Majorsilence.Reporting.RdlViewer.RdlViewer reportViewer = new()
        {
            Dock = Majorsilence.Forms.DockStyle.Fill,
        };

        public RdlWpfViewer()
        {
            InitializeComponent();
            windowsFormsHost1.Child = reportViewer.ToWinFormsControl();
        }

        public async Task Rebuild()
        {
            await this.reportViewer.Rebuild();
        }

        /// <param name="security">Optional PDF encryption settings. Ignored for non-PDF output.</param>
        /// <param name="signature">Optional PKCS#7 digital signature. Ignored for non-PDF output.</param>
        public async Task SaveAs(string FileName, Majorsilence.Reporting.Rdl.OutputPresentationType type,
            PdfSecurity? security = null, PdfSignatureOptions? signature = null)
        {
            await this.reportViewer.SaveAs(FileName, type, security, signature);
        }

        public Uri SourceFile
        {
            get
            {
                return this.reportViewer.SourceFile;
            }
        }

        public async Task SetSourceFile(Uri value)
        {
            await this.reportViewer.SetSourceFile(value);
        }

        public string SourceRdl
        {
            get
            {
                return this.reportViewer.SourceRdl;
            }
        }

        public async Task SetSourceRdl(string value)
        {
            await this.reportViewer.SetSourceRdl(value);
        }

        public string Parameters
        {
            get
            {
                return this.reportViewer.Parameters;
            }
            set
            {
                this.reportViewer.Parameters = value;
            }
        }

        public async Task<Majorsilence.Reporting.Rdl.Report> Report()
        {
            return await this.reportViewer.Report();       
        }

    }

}

